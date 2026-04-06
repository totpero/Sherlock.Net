<p align="center">
  <br>
  <img src="https://user-images.githubusercontent.com/27065646/53551960-ae4dff80-3b3a-11e9-9075-cef786c69364.png" width="200"/>
  <br>
  <b>Sherlock.Net</b>
  <br>
  <i>Hunt down social media accounts by username across 400+ social networks</i>
  <br>
  <i>A .NET port of the <a href="https://github.com/sherlock-project/sherlock">Sherlock Project</a></i>
  <br>
</p>

<p align="center">
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10.0"></a>
  <a href="https://github.com/totpero/Sherlock.Net/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green" alt="License: MIT"></a>
</p>

---

## About

**Sherlock.Net** is a cross-platform .NET rewrite of the popular [Sherlock](https://github.com/sherlock-project/sherlock) Python tool. It searches for usernames across 400+ social networks simultaneously, using parallel async HTTP requests for fast results.

### Why .NET?

- Native cross-platform binary (Windows, Linux, macOS)
- High-performance async I/O with `IAsyncEnumerable` streaming
- Connection pooling via `IHttpClientFactory` (no socket exhaustion)
- Clean architecture: use as a CLI tool or reference `Sherlock.Net.Core` as a library

---

## Installation

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Build from source

```bash
git clone https://github.com/totpero/Sherlock.Net.git
cd Sherlock.Net
dotnet build
```

### Run directly

```bash
dotnet run --project src/Sherlock.Net.Cli -- <username>
```

### Publish as single executable

```bash
dotnet publish src/Sherlock.Net.Cli -c Release -r win-x64 --self-contained
dotnet publish src/Sherlock.Net.Cli -c Release -r linux-x64 --self-contained
dotnet publish src/Sherlock.Net.Cli -c Release -r osx-arm64 --self-contained
```

---

## Usage

### Search for a single username

```bash
sherlock user123
```

### Search for multiple usernames

```bash
sherlock user1 user2 user3
```

### Limit to specific sites

```bash
sherlock --site GitHub --site Twitter user123
```

### Export results

```bash
sherlock --txt user123          # Save to user123.txt
sherlock --csv user123          # Save to user123.csv
sherlock --json-export user123  # Save to user123.json
```

### Use a proxy or Tor

```bash
sherlock --proxy socks5://127.0.0.1:9050 user123
```

### Username wildcards

Use `{?}` to try common separator variants (`_`, `-`, `.`):

```bash
sherlock john{?}doe
# Searches: john_doe, john-doe, john.doe
```

---

## Command-Line Options

```
USAGE:
    sherlock <USERNAMES> [OPTIONS]

ARGUMENTS:
    <USERNAMES>    One or more usernames to search for

OPTIONS:
                                 DEFAULT
    -h, --help                              Prints help information
    -v, --version                           Prints version information
        --timeout <SECONDS>      60         Time in seconds to wait for response
        --proxy <URL>                       Proxy URL (e.g., socks5://127.0.0.1:9050 for Tor)
        --site <NAME>                       Limit search to specific site(s)
        --json <PATH_OR_URL>                Custom data.json file path or URL
        --csv                               Export results as CSV
        --txt                               Export results as TXT
        --json-export                       Export results as JSON
    -o, --output <DIR>                      Output directory for export files
        --print-all                         Show all results, not just found accounts
        --nsfw                              Include NSFW sites in search
    -b, --browse                            Open found URLs in default browser
        --no-color                          Disable colored output
        --concurrency <COUNT>    20         Maximum concurrent requests
```

---

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

### Architecture

| Component | Responsibility |
|---|---|
| `SiteDataProvider` | Loads site definitions from embedded resource, URL, or file |
| `SiteChecker` | Executes HTTP requests and applies detection logic per site |
| `SherlockService` | Orchestrates parallel checks with `Channel<T>` + `SemaphoreSlim` |
| `WafDetector` | Identifies WAF blocks (Cloudflare, PerimeterX, Akamai) |
| `IResultExporter` | Exports results to TXT, CSV, or JSON |

### Detection Mechanisms

Each site in `data.json` uses one of three detection strategies:

| Type | How it works |
|---|---|
| **StatusCode** | HTTP 404 or specific error code = username not found |
| **Message** | Response body contains error string(s) = username not found |
| **ResponseUrl** | Server redirects to a different URL = username not found |

---

## Using as a Library

Reference `Sherlock.Net.Core` in your project to integrate username searching programmatically:

```csharp
var services = new ServiceCollection();
services.AddHttpClient("sherlock");
services.AddSingleton<ISiteDataProvider, SiteDataProvider>();
services.AddSingleton<IWafDetector, WafDetector>();
services.AddSingleton<ISiteChecker, SiteChecker>();
services.AddSingleton<ISherlockService, SherlockService>();

var provider = services.BuildServiceProvider();
var siteDataProvider = provider.GetRequiredService<ISiteDataProvider>();
var sherlockService = provider.GetRequiredService<ISherlockService>();

var sites = await siteDataProvider.LoadSitesAsync();
var options = new SherlockOptions { MaxConcurrency = 20 };

await foreach (var result in sherlockService.SearchAsync("username", sites, options))
{
    if (result.Status == QueryStatus.Claimed)
        Console.WriteLine($"[+] {result.SiteName}: {result.ProfileUrl}");
}
```

---

## Running Tests

```bash
dotnet test
```

Tests include JSON Schema validation of `data.json` against the upstream `data.schema.json`.

---

## Acknowledgements

This project is a .NET port of [Sherlock](https://github.com/sherlock-project/sherlock), originally created by [Siddharth Dushantha](https://github.com/sdushantha) and maintained by the Sherlock Project community. The site database (`data.json`) is sourced directly from the upstream project.

## License

MIT
