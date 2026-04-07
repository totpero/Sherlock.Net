using System.Globalization;
using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Services.Exporters;

public sealed class CsvResultExporter : IResultExporter
{
    public string FileExtension => "csv";

    public async Task ExportAsync(string username, IReadOnlyList<QueryResult> results, string outputPath, CancellationToken cancellationToken = default)
    {
        var lines = new List<string>(results.Count + 1)
        {
            "SiteName,ProfileUrl,Status,HttpStatusCode,ResponseTime"
        };

        foreach (var result in results)
        {
            var responseTime = result.QueryTime?.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture) ?? string.Empty;
            var statusCode = result.HttpStatusCode?.ToString() ?? string.Empty;
            lines.Add($"{Escape(result.SiteName)},{Escape(result.ProfileUrl)},{result.Status},{statusCode},{responseTime}");
        }

        await File.WriteAllLinesAsync(outputPath, lines, cancellationToken);
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
