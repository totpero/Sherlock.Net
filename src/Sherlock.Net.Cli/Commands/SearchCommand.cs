using System.Diagnostics;
using Sherlock.Net.Core.Models;
using Sherlock.Net.Core.Services;
using Sherlock.Net.Core.Services.Exporters;
using Sherlock.Net.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Sherlock.Net.Cli.Commands;

public sealed class SearchCommand(
    ISherlockService sherlockService,
    ISiteDataProvider siteDataProvider,
    IResultRenderer renderer,
    IEnumerable<IResultExporter> exporters)
    : AsyncCommand<SearchCommandSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings, CancellationToken cancellationToken)
    {
        if (settings.NoColor)
        {
            AnsiConsole.Profile.Capabilities.ColorSystem = (ColorSystem)ColorSystemSupport.NoColors;
        }

        renderer.RenderBanner();

        var options = new SherlockOptions
        {
            Timeout = TimeSpan.FromSeconds(settings.Timeout),
            MaxConcurrency = settings.Concurrency,
            ProxyUrl = settings.Proxy,
            IncludeNsfw = settings.Nsfw,
            SiteFilter = settings.Site?.ToList(),
            PrintAll = settings.PrintAll
        };

        var sites = await siteDataProvider.LoadSitesAsync(settings.DataSource, cancellationToken);

        var usernames = ExpandUsernames(settings.Usernames);

        foreach (var username in usernames)
        {
            renderer.RenderSearchStart(username, sites.Count);

            var results = new List<QueryResult>();

            await foreach (var result in sherlockService.SearchAsync(username, sites, options, cancellationToken))
            {
                results.Add(result);
                renderer.RenderResult(result, settings.PrintAll);

                if (settings.Browse && result.Status == QueryStatus.Claimed)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(result.ProfileUrl) { UseShellExecute = true });
                    }
                    catch
                    {
                        // Ignore browser launch failures
                    }
                }
            }

            var claimedCount = results.Count(r => r.Status == QueryStatus.Claimed);
            renderer.RenderSearchComplete(username, claimedCount);

            await ExportResultsAsync(username, results, settings, cancellationToken);
        }

        return 0;
    }

    private static List<string> ExpandUsernames(string[] usernames)
    {
        var expanded = new List<string>();

        foreach (var username in usernames)
        {
            if (username.Contains("{?}"))
            {
                expanded.Add(username.Replace("{?}", "_"));
                expanded.Add(username.Replace("{?}", "-"));
                expanded.Add(username.Replace("{?}", "."));
            }
            else
            {
                expanded.Add(username);
            }
        }

        return expanded;
    }

    private async Task ExportResultsAsync(string username, List<QueryResult> results, SearchCommandSettings settings, CancellationToken cancellationToken = default)
    {
        var exportFormats = new List<string>();
        if (settings.Txt) exportFormats.Add("txt");
        if (settings.Csv) exportFormats.Add("csv");
        if (settings.JsonExport) exportFormats.Add("json");

        if (exportFormats.Count == 0)
            return;

        var outputDir = settings.OutputDir ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(outputDir);

        foreach (var format in exportFormats)
        {
            var exporter = exporters.FirstOrDefault(e =>
                e.FileExtension.Equals(format, StringComparison.OrdinalIgnoreCase));

            if (exporter is null)
                continue;

            var filePath = Path.Combine(outputDir, $"{username}.{exporter.FileExtension}");
            await exporter.ExportAsync(username, results, filePath, cancellationToken);
        }
    }
}
