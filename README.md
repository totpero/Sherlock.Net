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
  <a href="https://github.com/totpero/Sherlock.Net/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-Apache%202.0-blue" alt="License: Apache 2.0"></a>
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

### Install as .NET tool (recommended)

Install globally as a .NET tool — this makes the `sherlock` command available everywhere in your terminal:

```bash
dotnet tool install --global Sherlock.Net
```

After installation, simply run:

```bash
sherlock user123
```

To update to the latest version:

```bash
dotnet tool update --global Sherlock.Net
```

To uninstall:

```bash
dotnet tool uninstall --global Sherlock.Net
```

> **Note:** Make sure `~/.dotnet/tools` (Linux/macOS) or `%USERPROFILE%\.dotnet\tools` (Windows) is in your `PATH`. The .NET SDK adds this automatically on first tool install, but you may need to restart your terminal.

### Build from source

```bash
git clone https://github.com/totpero/Sherlock.Net.git
cd Sherlock.Net
dotnet build
```

Run directly from source (without installing):

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

## Quick Start

```bash
# Install the tool globally
dotnet tool install --global Sherlock.Net

# Search for a username across 400+ sites
sherlock johndoe

# Search multiple usernames and export to CSV
sherlock --csv johndoe janedoe

# Search only on GitHub and Twitter
sherlock --site GitHub --site Twitter johndoe
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

```bash
dotnet add package Sherlock.Net.Core
```

### Minimal usage (no DI container)

The simplest way — one static call, no `ServiceCollection`, no setup:

```csharp
using Sherlock.Net.Core;
using Sherlock.Net.Core.Models;

// Search across all 400+ sites with a single call
await foreach (var result in SherlockFactory.SearchAsync("johndoe"))
{
    if (result.Status == QueryStatus.Claimed)
        Console.WriteLine($"[+] {result.SiteName}: {result.ProfileUrl}");
}
```

With options:

```csharp
await foreach (var result in SherlockFactory.SearchAsync("johndoe", options =>
{
    options.Timeout = TimeSpan.FromSeconds(30);
    options.MaxConcurrency = 10;
    options.SiteFilter = ["GitHub", "Twitter", "Instagram"];
}))
{
    if (result.Status == QueryStatus.Claimed)
        Console.WriteLine($"[+] {result.SiteName}: {result.ProfileUrl}");
}
```

If you need more control (e.g., custom data.json source), use `SherlockFactory.Create()`:

```csharp
var (sherlock, siteProvider) = SherlockFactory.Create();
var sites = await siteProvider.LoadSitesAsync("path/to/custom-data.json");
await foreach (var result in sherlock.SearchAsync("johndoe", sites, new SherlockOptions()))
{
    // ...
}
```

### With DI container (`AddSherlock`)

Register all services with a single call to `AddSherlock()`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sherlock.Net.Core;
using Sherlock.Net.Core.Models;
using Sherlock.Net.Core.Services;

var services = new ServiceCollection();
services.AddSherlock();

var provider = services.BuildServiceProvider();
var siteDataProvider = provider.GetRequiredService<ISiteDataProvider>();
var sherlockService = provider.GetRequiredService<ISherlockService>();

var sites = await siteDataProvider.LoadSitesAsync();
var options = new SherlockOptions { MaxConcurrency = 20 };

await foreach (var result in sherlockService.SearchAsync("johndoe", sites, options))
{
    if (result.Status == QueryStatus.Claimed)
        Console.WriteLine($"[+] {result.SiteName}: {result.ProfileUrl}");
}
```

### Custom configuration

Use the lambda overload to configure defaults:

```csharp
services.AddSherlock(options =>
{
    options.UserAgent = "MyApp/1.0";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.MaxConcurrency = 10;
    options.ProxyUrl = "socks5://127.0.0.1:9050";
    options.IncludeNsfw = false;
});
```

### ASP.NET Core / Hosted services

`AddSherlock()` integrates with standard .NET dependency injection:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSherlock(options =>
{
    options.Timeout = TimeSpan.FromSeconds(15);
    options.MaxConcurrency = 5;
});

var app = builder.Build();

app.MapGet("/search/{username}", async (string username,
    ISiteDataProvider siteDataProvider, ISherlockService sherlockService) =>
{
    var sites = await siteDataProvider.LoadSitesAsync();
    var results = new List<object>();

    await foreach (var result in sherlockService.SearchAsync(username, sites, new SherlockOptions()))
    {
        if (result.Status == QueryStatus.Claimed)
            results.Add(new { result.SiteName, result.ProfileUrl });
    }

    return Results.Ok(results);
});

app.Run();
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

Apache 2.0
