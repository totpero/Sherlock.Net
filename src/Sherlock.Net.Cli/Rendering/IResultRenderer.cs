using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Cli.Rendering;

public interface IResultRenderer
{
    void RenderBanner();
    void RenderSearchStart(string username, int siteCount);
    void RenderResult(QueryResult result, bool printAll);
    void RenderSearchComplete(string username, int claimedCount);
}
