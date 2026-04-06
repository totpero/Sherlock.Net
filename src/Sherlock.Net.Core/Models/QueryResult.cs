namespace Sherlock.Net.Core.Models;

public sealed class QueryResult
{
    public required string Username { get; init; }
    public required string SiteName { get; init; }
    public required string SiteUrlMain { get; init; }
    public required string ProfileUrl { get; init; }
    public required QueryStatus Status { get; init; }
    public TimeSpan? QueryTime { get; init; }
    public string? Context { get; init; }
    public int? HttpStatusCode { get; init; }
}
