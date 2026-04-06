namespace Sherlock.Net.Core.Models;

public sealed class SherlockOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
    public int MaxConcurrency { get; set; } = 20;
    public string? ProxyUrl { get; set; }
    public bool IncludeNsfw { get; set; }
    public List<string>? SiteFilter { get; set; }
    public bool PrintAll { get; set; }
}
