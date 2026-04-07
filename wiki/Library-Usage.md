# Library Usage

Install the NuGet package:

```bash
dotnet add package Sherlock.Net
```

## Minimal usage (no DI)

One static call, no `ServiceCollection`, no setup:

```csharp
using Sherlock.Net.Core;
using Sherlock.Net.Core.Models;

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

## Custom data.json source

```csharp
var (sherlock, siteProvider) = SherlockFactory.Create();
var sites = await siteProvider.LoadSitesAsync("path/to/custom-data.json");
await foreach (var result in sherlock.SearchAsync("johndoe", sites, new SherlockOptions()))
{
    // ...
}
```

## With DI container (AddSherlock)

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

## Custom configuration

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

## ASP.NET Core integration

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
