using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Services;

public sealed class SherlockService(ISiteChecker siteChecker) : ISherlockService
{
    public async IAsyncEnumerable<QueryResult> SearchAsync(
        string username,
        IReadOnlyList<SiteData> sites,
        SherlockOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var filteredSites = FilterSites(sites, options);
        if (filteredSites.Count == 0)
            yield break;

        var channel = Channel.CreateBounded<QueryResult>(
            new BoundedChannelOptions(filteredSites.Count)
            {
                SingleWriter = false,
                SingleReader = true
            });

        using var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);

        // Don't pass cancellationToken to Task.Run's scheduler - handle cancellation inside
        // the delegate to ensure channel.Writer.Complete() is always called
        _ = Task.Run(async () =>
        {
            Exception? caughtException = null;
            try
            {
                var tasks = filteredSites.Select(async site =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var result = await siteChecker.CheckAsync(site, username, options, cancellationToken);
                        await channel.Writer.WriteAsync(result, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToArray();

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
            finally
            {
                channel.Writer.Complete(caughtException);
            }
        }, cancellationToken);

        await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }
    }

    private static List<SiteData> FilterSites(IReadOnlyList<SiteData> sites, SherlockOptions options)
    {
        var filtered = new List<SiteData>(sites.Count);

        foreach (var site in sites)
        {
            if (!options.IncludeNsfw && site.IsNsfw)
                continue;

            if (options.SiteFilter is { Count: > 0 })
            {
                var match = options.SiteFilter.Any(f =>
                    site.Name.Contains(f, StringComparison.OrdinalIgnoreCase));
                if (!match)
                    continue;
            }

            filtered.Add(site);
        }

        return filtered;
    }
}
