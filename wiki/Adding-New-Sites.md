# Adding New Sites

## Step 1: Find the site configuration

Check `data.json` for the site entry or determine the detection method manually:

1. Visit the site with an **existing** username (e.g., `blue`)
2. Visit the site with a **non-existing** username (e.g., `blueqqqqq`)
3. Compare the responses:
   - Different HTTP status code? → `status_code`
   - Error text in the page? → `message`
   - Redirect to a different URL? → `response_url`

## Step 2: Add to data.json (if not present)

```json
"SiteName": {
  "url": "https://example.com/{}",
  "urlMain": "https://example.com",
  "errorType": "message",
  "errorMsg": "User not found",
  "username_claimed": "blue",
  "regexCheck": "^[a-zA-Z0-9_]+$"
}
```

### Fields

| Field | Required | Description |
|---|---|---|
| `url` | Yes | Profile URL with `{}` placeholder |
| `urlMain` | Yes | Site homepage |
| `errorType` | Yes | `status_code`, `message`, or `response_url` |
| `errorMsg` | If message | Error text(s) to search for |
| `errorCode` | If status_code | HTTP status code (e.g., 404) |
| `errorUrl` | If response_url | Redirect target URL |
| `username_claimed` | Yes | Known existing username |
| `regexCheck` | No | Username format regex |
| `urlProbe` | No | Alternative URL for probing |
| `request_method` | No | `GET`, `POST`, `HEAD`, `PUT` |
| `request_payload` | No | JSON body for POST/PUT |
| `headers` | No | Custom HTTP headers |
| `isNSFW` | No | Adult content flag |

## Step 3: Write integration tests

Create tests in `SiteIntegrationTests.cs`:

```csharp
private static readonly SiteData MySite = new()
{
    Name = "MySite",
    Url = "https://example.com/{}",
    UrlMain = "https://example.com",
    ErrorType = SiteErrorType.Message,
    ErrorMsg = ["User not found"],
    RegexCheck = "^[a-zA-Z0-9_]+$",
    UsernameClaimed = "blue"
};

[Fact]
public async Task MySite_ExistingUser_ReturnsClaimed()
{
    var checker = CreateChecker();
    var result = await checker.CheckAsync(MySite, "blue", DefaultOptions);
    result.Status.ShouldBe(QueryStatus.Claimed);
}

[Fact]
public async Task MySite_NonExistingUser_ReturnsAvailable()
{
    var checker = CreateChecker();
    var result = await checker.CheckAsync(MySite, "blueqqqqq", DefaultOptions);
    result.Status.ShouldBe(QueryStatus.Available);
}
```

## Step 4: Validate schema

Run schema validation tests to ensure `data.json` is valid:

```bash
dotnet test --filter "DataSchemaValidation"
```
