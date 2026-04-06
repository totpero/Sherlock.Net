using System.Text.RegularExpressions;

namespace Sherlock.Net.Core.Services;

public sealed class WafDetector : IWafDetector
{
    private static readonly (string Name, Regex Pattern)[] Fingerprints =
    [
        ("Cloudflare", new Regex(
            @"<title>Just a moment\.\.\.</title>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("Cloudflare", new Regex(
            @"cf[-_]chl_opt",
            RegexOptions.Compiled)),
        ("Cloudflare", new Regex(
            @"Checking if the site connection is secure",
            RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("PerimeterX", new Regex(
            @"<title>Access to this page has been denied</title>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("PerimeterX", new Regex(
            @"px-captcha",
            RegexOptions.Compiled)),
        ("AWS CloudFront", new Regex(
            @"ERROR: The request could not be satisfied",
            RegexOptions.Compiled)),
        ("Akamai", new Regex(
            @"<title>Access Denied</title>.*akamai",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)),
    ];

    public bool IsWafResponse(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
            return false;

        foreach (var (_, pattern) in Fingerprints)
        {
            if (pattern.IsMatch(responseBody))
                return true;
        }

        return false;
    }
}
