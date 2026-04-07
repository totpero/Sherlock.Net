# Detection Mechanisms

Each site in `data.json` uses one of three strategies to determine if a username exists.

## StatusCode

Checks the HTTP status code of the response.

- **404** (or custom `errorCode`) → username **not found** (Available)
- **200** (success) → username **exists** (Claimed)

```json
{
  "errorType": "status_code",
  "errorCode": 404,
  "url": "https://example.com/user/{}"
}
```

## Message

Searches the response body for specific error text.

- Body **contains** `errorMsg` → username **not found** (Available)
- Body **does not contain** `errorMsg` → username **exists** (Claimed)

```json
{
  "errorType": "message",
  "errorMsg": "User not found",
  "url": "https://example.com/user/{}"
}
```

`errorMsg` can be a single string or an array of strings (any match = not found):

```json
{
  "errorMsg": ["User not found", "Page not found"]
}
```

## ResponseUrl

Checks if the server redirects to a different URL.

- **Redirected** to error URL → username **not found** (Available)
- **No redirect** (stays on profile URL) → username **exists** (Claimed)

```json
{
  "errorType": "response_url",
  "errorUrl": "https://example.com/404",
  "url": "https://example.com/user/{}"
}
```

## WAF Detection

Before evaluating the response, Sherlock.Net checks for Web Application Firewall blocks. Known fingerprints:

| WAF | Detection |
|---|---|
| Cloudflare | "Just a moment...", `cf_chl_opt` |
| PerimeterX | "Access to this page has been denied", `px-captcha` |
| AWS CloudFront | "ERROR: The request could not be satisfied" |
| Akamai | "Access Denied" + "akamai" |

When a WAF block is detected, the result is `QueryStatus.Waf` instead of a false positive.
