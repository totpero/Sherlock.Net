using System.Text.RegularExpressions;

namespace Sherlock.Net.Core.Services;

public sealed partial class WafDetector : IWafDetector
{
    private static readonly (string Name, Regex Pattern)[] Fingerprints =
    [
        ("Cloudflare", ClaudFlareRegex1()),
        ("Cloudflare", ClaudFlareRegex2()),
        ("Cloudflare", ClaudFlareRegex3()),
        ("PerimeterX", PerimeterX1()),
        ("PerimeterX", PerimeterX2()),
        ("AWS CloudFront", AwsCloudFront()),
        ("Akamai", Akamai()),
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

    [GeneratedRegex(@"<title>Just a moment\.\.\.</title>", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-GB")]
    private static partial Regex ClaudFlareRegex1();
    [GeneratedRegex(@"cf[-_]chl_opt", RegexOptions.Compiled)]
    private static partial Regex ClaudFlareRegex2();
    [GeneratedRegex(@"Checking if the site connection is secure", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-GB")]
    private static partial Regex ClaudFlareRegex3();
    [GeneratedRegex(@"<title>Access to this page has been denied</title>", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-GB")]
    private static partial Regex PerimeterX1();
    [GeneratedRegex(@"px-captcha", RegexOptions.Compiled)]
    private static partial Regex PerimeterX2();
    [GeneratedRegex(@"ERROR: The request could not be satisfied", RegexOptions.Compiled)]
    private static partial Regex AwsCloudFront();
    [GeneratedRegex(@"<title>Access Denied</title>.*akamai", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "en-GB")]
    private static partial Regex Akamai();
}
