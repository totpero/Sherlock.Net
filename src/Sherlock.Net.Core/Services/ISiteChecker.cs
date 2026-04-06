using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Services;

public interface ISiteChecker
{
    Task<QueryResult> CheckAsync(SiteData site, string username, SherlockOptions options, CancellationToken cancellationToken = default);
}
