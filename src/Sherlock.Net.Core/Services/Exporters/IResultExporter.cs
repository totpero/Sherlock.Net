using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Services.Exporters;

public interface IResultExporter
{
    string FileExtension { get; }
    Task ExportAsync(string username, IReadOnlyList<QueryResult> results, string outputPath, CancellationToken cancellationToken = default);
}
