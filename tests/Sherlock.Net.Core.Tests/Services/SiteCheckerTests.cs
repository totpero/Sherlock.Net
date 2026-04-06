using Microsoft.Extensions.DependencyInjection;
using Sherlock.Net.Core.Models;
using Sherlock.Net.Core.Services;
using Shouldly;

namespace Sherlock.Net.Core.Tests.Services;

public class SiteCheckerTests
{
    [Fact]
    public async Task CheckAsync_IllegalUsername_ReturnsIllegal()
    {
        var site = new SiteData
        {
            Name = "TestSite",
            Url = "https://example.com/{}",
            UrlMain = "https://example.com",
            ErrorType = SiteErrorType.StatusCode,
            RegexCheck = "^[a-zA-Z0-9]+$"
        };

        var checker = CreateChecker();
        var options = new SherlockOptions { Timeout = TimeSpan.FromSeconds(5) };

        var result = await checker.CheckAsync(site, "invalid user!", options);

        result.Status.ShouldBe(QueryStatus.Illegal);
        result.SiteName.ShouldBe("TestSite");
    }

    [Fact]
    public async Task CheckAsync_ValidUsername_PassesRegex()
    {
        var site = new SiteData
        {
            Name = "TestSite",
            Url = "https://httpbin.org/status/200?user={}",
            UrlMain = "https://httpbin.org",
            ErrorType = SiteErrorType.StatusCode,
            RegexCheck = "^[a-zA-Z0-9]+$"
        };

        var checker = CreateChecker();
        var options = new SherlockOptions { Timeout = TimeSpan.FromSeconds(10) };

        var result = await checker.CheckAsync(site, "validuser", options);

        result.Status.ShouldNotBe(QueryStatus.Illegal);
    }

    [Fact]
    public void WafDetector_DetectsCloudflare()
    {
        var detector = new WafDetector();
        var body = "<html><head><title>Just a moment...</title></head></html>";

        detector.IsWafResponse(body).ShouldBeTrue();
    }

    [Fact]
    public void WafDetector_DoesNotFalsePositive()
    {
        var detector = new WafDetector();
        var body = "<html><head><title>User Profile</title></head><body>Hello World</body></html>";

        detector.IsWafResponse(body).ShouldBeFalse();
    }

    private static SiteChecker CreateChecker()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("sherlock");
        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        return new SiteChecker(factory, new WafDetector());
    }
}
