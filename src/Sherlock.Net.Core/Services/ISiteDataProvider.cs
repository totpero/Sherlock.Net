using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Services;

public interface ISiteDataProvider
{
    Task<IReadOnlyList<SiteData>> LoadSitesAsync(string? source = null, CancellationToken cancellationToken = default);
}
