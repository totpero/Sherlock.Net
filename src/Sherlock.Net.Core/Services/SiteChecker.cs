using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Services;

public sealed class SiteChecker(IHttpClientFactory httpClientFactory, IWafDetector wafDetector)
    : ISiteChecker
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    public async Task<QueryResult> CheckAsync(SiteData site, string username, SherlockOptions options, CancellationToken cancellationToken = default)
    {
        var profileUrl = site.GetProfileUrl(username);

        // Regex validation with timeout to prevent ReDoS
        if (!string.IsNullOrEmpty(site.RegexCheck))
        {
            try
            {
                if (!Regex.IsMatch(username, site.RegexCheck, RegexOptions.None, RegexTimeout))
                {
                    return MakeResult(username, site, profileUrl, QueryStatus.Illegal,
                        context: "Username format not valid for this site");
                }
            }
            catch (RegexParseException)
            {
                // Skip regex validation if pattern is invalid
            }
            catch (RegexMatchTimeoutException)
            {
                // Skip regex validation if pattern takes too long
            }
        }

        var probeUrl = site.GetProbeUrl(username);
        var method = GetHttpMethod(site.RequestMethod);
        var sw = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(options.Timeout);

            var client = CreateHttpClient(options);

            using var request = new HttpRequestMessage(method, probeUrl);

            // Set per-request headers (not on shared client)
            if (site.Headers is not null)
            {
                foreach (var (key, value) in site.Headers)
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }

            if (site.RequestPayload.HasValue && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                var payloadJson = SubstitutePayload(site.RequestPayload.Value, username);
                request.Content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
            }

            // For ResponseUrl detection, we need to NOT follow redirects so we can see where the redirect goes
            // For other types, we follow redirects normally
            HttpResponseMessage response;
            string? finalUrl = null;

            if (site.ErrorType == SiteErrorType.ResponseUrl)
            {
                // Use a separate handler that doesn't follow redirects
                using var noRedirectHandler = CreateHandler(options, allowRedirects: false);
                using var noRedirectClient = new HttpClient(noRedirectHandler);
                CopyDefaultHeaders(client, noRedirectClient);

                response = await noRedirectClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);

                // Track redirect chain to find final URL
                if (response.StatusCode is HttpStatusCode.Moved or HttpStatusCode.Found
                    or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect
                    or HttpStatusCode.PermanentRedirect)
                {
                    finalUrl = response.Headers.Location?.ToString();
                    // Resolve relative URLs
                    if (finalUrl is not null && !finalUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        var baseUri = new Uri(probeUrl);
                        finalUrl = new Uri(baseUri, finalUrl).ToString();
                    }
                }
                else
                {
                    // No redirect happened - the URL resolved directly
                    finalUrl = probeUrl;
                }
            }
            else
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);
            }

            using (response)
            {
                sw.Stop();

                var body = await response.Content.ReadAsStringAsync(cts.Token);
                var statusCode = (int)response.StatusCode;

                // WAF check
                if (wafDetector.IsWafResponse(body))
                {
                    return MakeResult(username, site, profileUrl, QueryStatus.Waf,
                        queryTime: sw.Elapsed, httpStatusCode: statusCode,
                        context: "Blocked by bot detection (proxy may help)");
                }

                var status = EvaluateResponse(site, response, body, profileUrl, username, finalUrl);

                return MakeResult(username, site, profileUrl, status,
                    queryTime: sw.Elapsed, httpStatusCode: statusCode);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Per-request timeout (not user cancellation)
            sw.Stop();
            return MakeResult(username, site, profileUrl, QueryStatus.Unknown,
                queryTime: sw.Elapsed, context: "Request timed out");
        }
        catch (OperationCanceledException)
        {
            // User cancellation (Ctrl+C) - rethrow to stop the entire run
            throw;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            return MakeResult(username, site, profileUrl, QueryStatus.Unknown,
                queryTime: sw.Elapsed, context: $"Connection error: {ex.Message}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return MakeResult(username, site, profileUrl, QueryStatus.Unknown,
                queryTime: sw.Elapsed, context: $"Error: {ex.Message}");
        }
    }

    private HttpClient CreateHttpClient(SherlockOptions options)
    {
        HttpClient client;

        if (!string.IsNullOrEmpty(options.ProxyUrl))
        {
            // Proxy requires a custom handler - can't use factory's default
            var handler = CreateHandler(options, allowRedirects: true);
            client = new HttpClient(handler, disposeHandler: true);
        }
        else
        {
            client = httpClientFactory.CreateClient(SherlockDefaults.HttpClientName);
        }

        // Set default User-Agent if not configured by the factory
        if (client.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(SherlockDefaults.UserAgent);
        }

        return client;
    }

    private static QueryStatus EvaluateResponse(SiteData site, HttpResponseMessage response, string body,
        string profileUrl, string username, string? finalUrl)
    {
        return site.ErrorType switch
        {
            SiteErrorType.StatusCode => EvaluateStatusCode(site, response),
            SiteErrorType.Message => EvaluateMessage(site, body),
            SiteErrorType.ResponseUrl => EvaluateResponseUrl(site, profileUrl, username, finalUrl),
            _ => QueryStatus.Unknown
        };
    }

    private static QueryStatus EvaluateStatusCode(SiteData site, HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

        if (site.ErrorCode.HasValue)
        {
            return statusCode == site.ErrorCode.Value
                ? QueryStatus.Available
                : (response.IsSuccessStatusCode ? QueryStatus.Claimed : QueryStatus.Available);
        }

        return response.IsSuccessStatusCode ? QueryStatus.Claimed : QueryStatus.Available;
    }

    private static QueryStatus EvaluateMessage(SiteData site, string body)
    {
        if (site.ErrorMsg is null || site.ErrorMsg.Count == 0)
            return QueryStatus.Unknown;

        foreach (var msg in site.ErrorMsg)
        {
            if (body.Contains(msg, StringComparison.Ordinal))
                return QueryStatus.Available;
        }

        return QueryStatus.Claimed;
    }

    private static QueryStatus EvaluateResponseUrl(SiteData site, string profileUrl, string username, string? finalUrl)
    {
        if (finalUrl is null)
            return QueryStatus.Unknown;

        if (site.ErrorUrl is not null)
        {
            var expectedErrorUrl = site.ErrorUrl.Replace("{}", username);
            return UrlsMatch(finalUrl, expectedErrorUrl)
                ? QueryStatus.Available
                : QueryStatus.Claimed;
        }

        // If redirect happened (finalUrl differs from profile URL), user doesn't exist
        return UrlsMatch(finalUrl, profileUrl)
            ? QueryStatus.Claimed
            : QueryStatus.Available;
    }

    private static bool UrlsMatch(string url1, string url2)
    {
        // Normalize for comparison: trim trailing slashes, compare case-insensitive
        var normalized1 = url1.TrimEnd('/');
        var normalized2 = url2.TrimEnd('/');

        if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
            return true;

        // Try URI-based comparison to handle percent-encoding differences
        if (Uri.TryCreate(url1, UriKind.Absolute, out var uri1) &&
            Uri.TryCreate(url2, UriKind.Absolute, out var uri2))
        {
            return Uri.Compare(uri1, uri2,
                UriComponents.Host | UriComponents.Path | UriComponents.Query,
                UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;
        }

        return false;
    }

    private static HttpClientHandler CreateHandler(SherlockOptions options, bool allowRedirects)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = allowRedirects,
            MaxAutomaticRedirections = 10
        };

        if (!string.IsNullOrEmpty(options.ProxyUrl))
        {
            handler.Proxy = new WebProxy(options.ProxyUrl);
            handler.UseProxy = true;
        }

        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        return handler;
    }

    private static void CopyDefaultHeaders(HttpClient source, HttpClient target)
    {
        foreach (var header in source.DefaultRequestHeaders)
        {
            target.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private static string SubstitutePayload(JsonElement payload, string username)
    {
        return SubstituteJsonElement(payload, username).GetRawText();
    }

    private static JsonElement SubstituteJsonElement(JsonElement element, string username)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var str = element.GetString() ?? string.Empty;
                var replaced = str.Replace("{}", username);
                return JsonDocument.Parse($"\"{JsonEncodedText.Encode(replaced)}\"").RootElement.Clone();

            case JsonValueKind.Object:
                var dict = new Dictionary<string, JsonElement>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = SubstituteJsonElement(prop.Value, username);
                }
                var objJson = JsonSerializer.Serialize(dict);
                return JsonDocument.Parse(objJson).RootElement.Clone();

            case JsonValueKind.Array:
                var items = new List<JsonElement>();
                foreach (var item in element.EnumerateArray())
                {
                    items.Add(SubstituteJsonElement(item, username));
                }
                var arrJson = JsonSerializer.Serialize(items);
                return JsonDocument.Parse(arrJson).RootElement.Clone();

            default:
                return element.Clone();
        }
    }

    private static HttpMethod GetHttpMethod(string? method)
    {
        return method?.ToUpperInvariant() switch
        {
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "HEAD" => HttpMethod.Head,
            _ => HttpMethod.Get
        };
    }

    private static QueryResult MakeResult(string username, SiteData site, string profileUrl,
        QueryStatus status, TimeSpan? queryTime = null, int? httpStatusCode = null, string? context = null)
    {
        return new QueryResult
        {
            Username = username,
            SiteName = site.Name,
            SiteUrlMain = site.UrlMain,
            ProfileUrl = profileUrl,
            Status = status,
            QueryTime = queryTime,
            HttpStatusCode = httpStatusCode,
            Context = context
        };
    }
}
