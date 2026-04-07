using Sherlock.Net.Core;
using Sherlock.Net.Core.Models;
using Shouldly;

namespace Sherlock.Net.Core.Tests.Services;

public class SherlockFactoryTests
{
    [Fact]
    public void Create_ReturnsWorkingInstances()
    {
        var (service, siteProvider) = SherlockFactory.Create();

        service.ShouldNotBeNull();
        siteProvider.ShouldNotBeNull();
    }

    [Fact]
    public async Task Create_SiteProvider_LoadsSites()
    {
        var (_, siteProvider) = SherlockFactory.Create();

        var sites = await siteProvider.LoadSitesAsync();

        sites.ShouldNotBeEmpty();
        sites.Count.ShouldBeGreaterThan(100);
    }

    [Fact]
    public async Task Create_SearchAsync_FindsGitHubUser()
    {
        var (service, siteProvider) = SherlockFactory.Create();
        var sites = await siteProvider.LoadSitesAsync();

        var options = new SherlockOptions
        {
            Timeout = TimeSpan.FromSeconds(15),
            SiteFilter = ["GitHub"]
        };

        var results = new List<QueryResult>();
        await foreach (var result in service.SearchAsync("testuser123", sites, options))
        {
            results.Add(result);
        }

        results.ShouldNotBeEmpty();
        var github = results.FirstOrDefault(r => r.SiteName == "GitHub");
        github.ShouldNotBeNull();
        github.Status.ShouldBe(QueryStatus.Claimed);
        github.ProfileUrl.ShouldContain("testuser123");
    }

    [Fact]
    public async Task Create_SearchAsync_IllegalUsername_ReturnsIllegal()
    {
        var (service, siteProvider) = SherlockFactory.Create();
        var sites = await siteProvider.LoadSitesAsync();

        var options = new SherlockOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            SiteFilter = ["GitHub"]
        };

        var results = new List<QueryResult>();
        await foreach (var result in service.SearchAsync("invalid user with spaces!", sites, options))
        {
            results.Add(result);
        }

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(r => r.Status == QueryStatus.Illegal);
    }

    /// <summary>
    /// One-liner: just username, no options, no DI, no LoadSitesAsync.
    /// </summary>
    [Fact]
    public async Task SearchAsync_UsernameOnly_FindsResults()
    {
        var results = new List<QueryResult>();
        await foreach (var result in SherlockFactory.SearchAsync("testuser123"))
        {
            results.Add(result);
            if (results.Count >= 5) break; // limit for test speed
        }

        results.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Username + options lambda, no DI, no LoadSitesAsync.
    /// </summary>
    [Fact]
    public async Task SearchAsync_WithOptions_FindsGitHub()
    {
        var results = new List<QueryResult>();
        await foreach (var result in SherlockFactory.SearchAsync("testuser123", options =>
        {
            options.Timeout = TimeSpan.FromSeconds(15);
            options.SiteFilter = ["GitHub"];
        }))
        {
            results.Add(result);
        }

        results.ShouldNotBeEmpty();
        var github = results.FirstOrDefault(r => r.SiteName == "GitHub");
        github.ShouldNotBeNull();
        github.Status.ShouldBe(QueryStatus.Claimed);
    }

    /// <summary>
    /// Demonstrates the absolute minimal usage: one line to search.
    /// </summary>
    [Fact]
    public async Task MinimalUsageExample()
    {
        var found = new List<string>();

        await foreach (var result in SherlockFactory.SearchAsync("testuser123", options =>
        {
            options.SiteFilter = ["GitHub"];
        }))
        {
            if (result.Status == QueryStatus.Claimed)
                found.Add($"[+] {result.SiteName}: {result.ProfileUrl}");
        }

        found.ShouldNotBeEmpty();
    }
}
