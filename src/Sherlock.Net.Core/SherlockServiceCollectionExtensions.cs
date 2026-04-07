using Microsoft.Extensions.DependencyInjection;
using Sherlock.Net.Core.Models;
using Sherlock.Net.Core.Services;
using Sherlock.Net.Core.Services.Exporters;

namespace Sherlock.Net.Core;

public sealed class SherlockBuilder
{
    public string UserAgent { get; set; } = SherlockDefaults.UserAgent;

    public string HttpClientName { get; set; } = SherlockDefaults.HttpClientName;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
    public int MaxConcurrency { get; set; } = 20;
    public string? ProxyUrl { get; set; }
    public bool IncludeNsfw { get; set; }
    public bool PrintAll { get; set; }
}

public static class SherlockServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Sherlock.Net.Core services: site data provider, site checker,
    /// sherlock service, WAF detector, and result exporters (TXT, CSV, JSON).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSherlock(this IServiceCollection services)
    {
        return services.AddSherlock(_ => { });
    }

    /// <summary>
    /// Registers all Sherlock.Net.Core services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate to configure <see cref="SherlockBuilder"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSherlock(this IServiceCollection services, Action<SherlockBuilder> configure)
    {
        var builder = new SherlockBuilder();
        configure(builder);

        // Register default options from builder
        services.AddSingleton(new SherlockOptions
        {
            Timeout = builder.Timeout,
            MaxConcurrency = builder.MaxConcurrency,
            ProxyUrl = builder.ProxyUrl,
            IncludeNsfw = builder.IncludeNsfw,
            PrintAll = builder.PrintAll
        });

        // Register named HttpClient
        services.AddHttpClient(builder.HttpClientName, client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(builder.UserAgent);
        });

        // Core services
        services.AddSingleton<ISiteDataProvider, SiteDataProvider>();
        services.AddSingleton<IWafDetector, WafDetector>();
        services.AddSingleton<ISiteChecker, SiteChecker>();
        services.AddSingleton<ISherlockService, SherlockService>();

        // Exporters
        services.AddSingleton<IResultExporter, TxtResultExporter>();
        services.AddSingleton<IResultExporter, CsvResultExporter>();
        services.AddSingleton<IResultExporter, JsonResultExporter>();

        return services;
    }
}
