# Architecture

## Project Structure

```
Sherlock.Net/
├── src/
│   ├── Sherlock.Net.Core/         # Class library (models, services, exporters)
│   │   ├── Models/                # SiteData, QueryResult, QueryStatus, SherlockOptions
│   │   ├── Serialization/         # JSON converters for data.json quirks
│   │   ├── Services/              # SiteChecker, SherlockService, WafDetector
│   │   └── Resources/             # Embedded data.json (400+ sites)
│   │
│   └── Sherlock.Net.Cli/          # Console app (thin CLI layer)
│       ├── Commands/              # SearchCommand, SearchCommandSettings
│       └── Rendering/             # Spectre.Console colored output
│
└── tests/
    └── Sherlock.Net.Core.Tests/   # xUnit tests + JSON Schema validation
```

## Components

| Component | Responsibility |
|---|---|
| `SherlockFactory` | Static factory for zero-config usage without DI |
| `SherlockDefaults` | Central constants: Version, UserAgent, HttpClientName, resource names |
| `SiteDataProvider` | Loads site definitions from embedded resource, URL, or file |
| `SiteChecker` | Executes HTTP requests and applies detection logic per site |
| `SherlockService` | Orchestrates parallel checks with `Channel<T>` + `SemaphoreSlim` |
| `WafDetector` | Identifies WAF blocks (Cloudflare, PerimeterX, Akamai, CloudFront) |
| `IResultExporter` | Exports results to TXT, CSV, or JSON |
| `SherlockBuilder` | Configuration builder for `AddSherlock()` DI extension |

## Request Flow

```
Username input
    │
    ▼
SherlockService.SearchAsync()
    │
    ├── Filter sites (NSFW, site filter)
    │
    ├── Create Channel<QueryResult> + SemaphoreSlim(maxConcurrency)
    │
    ├── For each site (parallel):
    │   │
    │   ▼
    │   SiteChecker.CheckAsync()
    │       │
    │       ├── Regex validation (RegexCheck)
    │       │   └── Illegal? → return Illegal
    │       │
    │       ├── Build HTTP request (URL, headers, payload)
    │       │
    │       ├── Execute HTTP request
    │       │
    │       ├── WAF detection (WafDetector)
    │       │   └── Blocked? → return Waf
    │       │
    │       └── Evaluate response (by ErrorType):
    │           ├── StatusCode → check HTTP status
    │           ├── Message → search body for errorMsg
    │           └── ResponseUrl → check redirect location
    │
    └── Stream results via IAsyncEnumerable<QueryResult>
```

## Key Design Decisions

- **IAsyncEnumerable** streaming: results arrive as they complete, not all at once
- **Channel&lt;T&gt;** producer-consumer: decouples HTTP execution from result consumption
- **SemaphoreSlim** throttling: configurable concurrency (default 20)
- **IHttpClientFactory**: proper connection pooling, no socket exhaustion
- **Embedded data.json**: no external file dependency, works out of the box
- **Multi-target**: supports .NET 8.0, 9.0, and 10.0
