using System.Runtime.CompilerServices;
using Sherlock.Net.Core.Models;
using Sherlock.Net.Core.Services;

namespace Sherlock.Net.Core;

/// <summary>
/// Factory for creating Sherlock services without a DI container.
/// Provides the simplest way to use Sherlock.Net.Core programmatically.
/// </summary>
public static class SherlockFactory
{
    private static IReadOnlyList<SiteData>? _cachedSites;
    private static readonly SemaphoreSlim _sitesLock = new(1, 1);

    /// <summary>
    /// Search for a username across social networks with minimal setup.
    /// Sites are loaded and cached automatically on first call.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of query results as they arrive.</returns>
    /// <example>
    /// <code>
    /// await foreach (var result in SherlockFactory.SearchAsync("johndoe"))
    /// {
    ///     if (result.Status == QueryStatus.Claimed)
    ///         Console.WriteLine($"[+] {result.SiteName}: {result.ProfileUrl}");
    /// }
    /// </code>
    /// </example>
    public static IAsyncEnumerable<QueryResult> SearchAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(username, null, cancellationToken);
    }

    /// <summary>
    /// Search for a username with custom options.
    /// Sites are loaded and cached automatically on first call.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <param name="configure">Optional delegate to configure search options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of query results as they arrive.</returns>
    /// <example>
    /// <code>
    /// await foreach (var result in SherlockFactory.SearchAsync("johndoe", options =>
    /// {
    ///     options.Timeout = TimeSpan.FromSeconds(15);
    ///     options.MaxConcurrency = 10;
    ///     options.SiteFilter = ["GitHub", "Twitter"];
    /// }))
    /// {
    ///     Console.WriteLine($"{result.SiteName}: {result.Status}");
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<QueryResult> SearchAsync(
        string username,
        Action<SherlockOptions>? configure,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (service, _) = Create();
        var sites = await GetOrLoadSitesAsync(cancellationToken);

        var options = new SherlockOptions();
        configure?.Invoke(options);

        await foreach (var result in service.SearchAsync(username, sites, options, cancellationToken))
        {
            yield return result;
        }
    }

    /// <summary>
    /// Creates a ready-to-use <see cref="ISherlockService"/> and <see cref="ISiteDataProvider"/>
    /// with default configuration. No DI container or ServiceCollection required.
    /// </summary>
    public static (ISherlockService Service, ISiteDataProvider SiteProvider) Create()
    {
        var httpClientFactory = new SimpleHttpClientFactory();
        var wafDetector = new WafDetector();
        var siteChecker = new SiteChecker(httpClientFactory, wafDetector);
        var sherlockService = new SherlockService(siteChecker);
        var siteDataProvider = new SiteDataProvider(httpClientFactory);

        return (sherlockService, siteDataProvider);
    }

    private static async Task<IReadOnlyList<SiteData>> GetOrLoadSitesAsync(CancellationToken cancellationToken)
    {
        if (_cachedSites is not null)
            return _cachedSites;

        await _sitesLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedSites is not null)
                return _cachedSites;

            var (_, siteProvider) = Create();
            _cachedSites = await siteProvider.LoadSitesAsync(cancellationToken: cancellationToken);
            return _cachedSites;
        }
        finally
        {
            _sitesLock.Release();
        }
    }
}

/// <summary>
/// Minimal IHttpClientFactory implementation for use without DI.
/// </summary>
internal sealed class SimpleHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(SherlockDefaults.UserAgent);
        return client;
    }
}
