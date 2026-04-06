using System.Text.Json;
using System.Text.Json.Serialization;
using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Services.Exporters;

public sealed class JsonResultExporter : IResultExporter
{
    public string FileExtension => "json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task ExportAsync(string username, IReadOnlyList<QueryResult> results, string outputPath, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(results, Options);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);
    }
}
