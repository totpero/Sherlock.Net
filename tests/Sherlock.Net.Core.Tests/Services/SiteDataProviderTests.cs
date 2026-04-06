using Microsoft.Extensions.DependencyInjection;
using Sherlock.Net.Core.Models;
using Sherlock.Net.Core.Services;
using Shouldly;

namespace Sherlock.Net.Core.Tests.Services;

public class SiteDataProviderTests
{
    private readonly ISiteDataProvider _provider;

    public SiteDataProviderTests()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("sherlock");
        services.AddSingleton<ISiteDataProvider, SiteDataProvider>();
        var sp = services.BuildServiceProvider();
        _provider = sp.GetRequiredService<ISiteDataProvider>();
    }

    [Fact]
    public async Task LoadSitesAsync_FromEmbeddedResource_LoadsSites()
    {
        var sites = await _provider.LoadSitesAsync();

        sites.ShouldNotBeEmpty();
        sites.Count.ShouldBeGreaterThan(100);
    }

    [Fact]
    public async Task LoadSitesAsync_FromEmbeddedResource_SitesHaveRequiredFields()
    {
        var sites = await _provider.LoadSitesAsync();

        foreach (var site in sites)
        {
            site.Name.ShouldNotBeNullOrWhiteSpace();
            site.Url.ShouldNotBeNullOrWhiteSpace();
            site.UrlMain.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task LoadSitesAsync_FromEmbeddedResource_ParsesErrorTypes()
    {
        var sites = await _provider.LoadSitesAsync();

        var hasStatusCode = sites.Any(s => s.ErrorType == SiteErrorType.StatusCode);
        var hasMessage = sites.Any(s => s.ErrorType == SiteErrorType.Message);
        var hasResponseUrl = sites.Any(s => s.ErrorType == SiteErrorType.ResponseUrl);

        hasStatusCode.ShouldBeTrue("Should have sites with StatusCode error type");
        hasMessage.ShouldBeTrue("Should have sites with Message error type");
        hasResponseUrl.ShouldBeTrue("Should have sites with ResponseUrl error type");
    }

    [Fact]
    public async Task LoadSitesAsync_FromEmbeddedResource_ParsesGitHub()
    {
        var sites = await _provider.LoadSitesAsync();
        var github = sites.FirstOrDefault(s => s.Name == "GitHub");

        github.ShouldNotBeNull();
        github.Url.ShouldContain("{}");
        github.UrlMain.ShouldStartWith("https://www.github.com");
    }
}
