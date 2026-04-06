using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Services.Exporters;

public sealed class TxtResultExporter : IResultExporter
{
    public string FileExtension => "txt";

    public async Task ExportAsync(string username, IReadOnlyList<QueryResult> results, string outputPath, CancellationToken cancellationToken = default)
    {
        var claimed = results.Where(r => r.Status == QueryStatus.Claimed).ToList();

        var lines = new List<string>
        {
            $"Sherlock.Net Results for: {username}",
            $"Found {claimed.Count} account(s)",
            ""
        };

        foreach (var result in claimed)
        {
            lines.Add(result.ProfileUrl);
        }

        await File.WriteAllLinesAsync(outputPath, lines, cancellationToken);
    }
}
