using Microsoft.Extensions.DependencyInjection;
using Sherlock.Net.Core;
using Sherlock.Net.Core.Models;
using Sherlock.Net.Core.Services;
using Shouldly;

namespace Sherlock.Net.Core.Tests.Services;

/// <summary>
/// Integration tests that verify real site detection patterns from data.json.
/// These tests make real HTTP requests and may be blocked by WAF (Cloudflare, etc.)
/// in CI environments. They are excluded from CI via [Trait("Category", "Integration")].
///
/// Run locally with: dotnet test --filter "Category=Integration"
///
/// To add a new site test:
/// 1. Find the site entry in data.json for url, errorType, errorMsg, regexCheck
/// 2. Identify a username that EXISTS on the site
/// 3. Identify a username that does NOT exist on the site
/// 4. Create a test method following the pattern below
/// </summary>
[Trait("Category", "Integration")]
public class SiteIntegrationTests
{
    private static readonly SherlockOptions DefaultOptions = new()
    {
        Timeout = TimeSpan.FromSeconds(15),
        MaxConcurrency = 5
    };

    private static ISiteChecker CreateChecker()
    {
        var services = new ServiceCollection();
        services.AddSherlock();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<ISiteChecker>();
    }

    #region DeviantArt

    // Site: DeviantArt
    // URL pattern: https://www.deviantart.com/{}
    // Detection: errorType = "message", errorMsg = "Llama Not Found"
    // Existing user: iulianalexe (https://www.deviantart.com/iulianalexe)
    // Non-existing user: iulianalexexxx (https://www.deviantart.com/iulianalexexxx)

    private static readonly SiteData DeviantArt = new()
    {
        Name = "DeviantArt",
        Url = "https://www.deviantart.com/{}",
        UrlMain = "https://www.deviantart.com/",
        ErrorType = SiteErrorType.Message,
        ErrorMsg = ["Llama Not Found"],
        RegexCheck = "^[a-zA-Z][a-zA-Z0-9_-]*$",
        UsernameClaimed = "blue"
    };

    [Fact]
    public async Task DeviantArt_ExistingUser_ReturnsClaimed()
    {
        var checker = CreateChecker();

        var result = await checker.CheckAsync(DeviantArt, "iulianalexe", DefaultOptions);

        result.Status.ShouldBe(QueryStatus.Claimed);
        result.SiteName.ShouldBe("DeviantArt");
        result.ProfileUrl.ShouldBe("https://www.deviantart.com/iulianalexe");
    }

    [Fact]
    public async Task DeviantArt_NonExistingUser_ReturnsAvailable()
    {
        var checker = CreateChecker();

        var result = await checker.CheckAsync(DeviantArt, "iulianalexexxx", DefaultOptions);

        result.Status.ShouldBe(QueryStatus.Available);
        result.SiteName.ShouldBe("DeviantArt");
    }

    [Fact]
    public async Task DeviantArt_IllegalUsername_ReturnsIllegal()
    {
        var checker = CreateChecker();

        // DeviantArt regex: ^[a-zA-Z][a-zA-Z0-9_-]*$ — must start with letter, no spaces
        var result = await checker.CheckAsync(DeviantArt, "123invalid", DefaultOptions);

        result.Status.ShouldBe(QueryStatus.Illegal);
    }

    #endregion

    #region DeviantArt via SherlockFactory

    [Fact]
    public async Task DeviantArt_ViaFactory_ExistingUser_ReturnsClaimed()
    {
        var results = new List<QueryResult>();
        await foreach (var result in SherlockFactory.SearchAsync("iulianalexe", options =>
        {
            options.Timeout = TimeSpan.FromSeconds(15);
            options.SiteFilter = ["DeviantArt"];
        }))
        {
            results.Add(result);
        }

        results.ShouldNotBeEmpty();
        var deviantArt = results.First(r => r.SiteName == "DeviantArt");
        deviantArt.Status.ShouldBe(QueryStatus.Claimed);
        deviantArt.ProfileUrl.ShouldBe("https://www.deviantart.com/iulianalexe");
    }

    [Fact]
    public async Task DeviantArt_ViaFactory_NonExistingUser_ReturnsAvailable()
    {
        var results = new List<QueryResult>();
        await foreach (var result in SherlockFactory.SearchAsync("iulianalexexxx", options =>
        {
            options.Timeout = TimeSpan.FromSeconds(15);
            options.SiteFilter = ["DeviantArt"];
        }))
        {
            results.Add(result);
        }

        results.ShouldNotBeEmpty();
        var deviantArt = results.First(r => r.SiteName == "DeviantArt");
        deviantArt.Status.ShouldBe(QueryStatus.Available);
    }

    #endregion

    #region Helpers for adding new sites

    // =========================================================================
    // HOW TO ADD A NEW SITE TEST:
    // =========================================================================
    //
    // 1. Check data.json for the site's configuration:
    //    - url: The URL pattern with {} placeholder
    //    - errorType: "message", "status_code", or "response_url"
    //    - errorMsg: Error text(s) for "message" type
    //    - errorCode: HTTP status code for "status_code" type
    //    - errorUrl: Redirect URL for "response_url" type
    //    - regexCheck: Username format validation regex
    //
    // 2. Create a SiteData instance matching data.json
    //
    // 3. Write tests for:
    //    - Existing user -> QueryStatus.Claimed
    //    - Non-existing user -> QueryStatus.Available
    //    - Invalid username -> QueryStatus.Illegal (if regexCheck exists)
    //
    // Example for a "status_code" site:
    //
    //   private static readonly SiteData ExampleSite = new()
    //   {
    //       Name = "ExampleSite",
    //       Url = "https://example.com/user/{}",
    //       UrlMain = "https://example.com",
    //       ErrorType = SiteErrorType.StatusCode,
    //       ErrorCode = 404,
    //       RegexCheck = "^[a-zA-Z0-9_]+$"
    //   };
    //
    // Example for a "response_url" site:
    //
    //   private static readonly SiteData ExampleSite = new()
    //   {
    //       Name = "ExampleSite",
    //       Url = "https://example.com/{}",
    //       UrlMain = "https://example.com",
    //       ErrorType = SiteErrorType.ResponseUrl,
    //       ErrorUrl = "https://example.com/404"
    //   };
    //
    // =========================================================================

    #endregion
}
