using System.Reflection;
using System.Text.Json;
using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Services;

public sealed class SiteDataProvider(IHttpClientFactory httpClientFactory) : ISiteDataProvider
{
    public async Task<IReadOnlyList<SiteData>> LoadSitesAsync(string? source = null, CancellationToken cancellationToken = default)
    {
        var json = source switch
        {
            null => await LoadFromEmbeddedResourceAsync(cancellationToken),
            _ when source.StartsWith("http", StringComparison.OrdinalIgnoreCase) =>
                await LoadFromUrlAsync(source, cancellationToken),
            _ => await File.ReadAllTextAsync(source, cancellationToken)
        };

        return ParseSites(json);
    }

    private static async Task<string> LoadFromEmbeddedResourceAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Sherlock.Net.Core.Resources.data.json";

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private async Task<string> LoadFromUrlAsync(string url, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("sherlock");
        return await client.GetStringAsync(url, cancellationToken);
    }

    private static IReadOnlyList<SiteData> ParseSites(string json)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
            ?? throw new InvalidOperationException("Failed to deserialize data.json.");

        var sites = new List<SiteData>();

        foreach (var (key, value) in dict)
        {
            if (key.StartsWith('$'))
                continue;

            try
            {
                var site = value.Deserialize<SiteData>();
                if (site is null)
                    continue;

                site.Name = key;
                sites.Add(site);
            }
            catch (JsonException)
            {
                // Skip malformed entries
            }
        }

        return sites;
    }
}
