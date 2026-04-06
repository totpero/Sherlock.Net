using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Services;

public interface ISherlockService
{
    IAsyncEnumerable<QueryResult> SearchAsync(
        string username,
        IReadOnlyList<SiteData> sites,
        SherlockOptions options,
        CancellationToken cancellationToken = default);
}
