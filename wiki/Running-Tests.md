# Running Tests

## All unit tests

```bash
dotnet test
```

This runs 20 unit tests per framework (net8.0, net9.0, net10.0) = 60 total.

## Exclude integration tests

Integration tests make real HTTP requests and may be blocked by WAF in CI:

```bash
dotnet test --filter "Category!=Integration"
```

## Run only integration tests

```bash
dotnet test --filter "Category=Integration"
```

## Run specific test class

```bash
dotnet test --filter "FullyQualifiedName~SiteDataProviderTests"
dotnet test --filter "FullyQualifiedName~DataSchemaValidation"
dotnet test --filter "FullyQualifiedName~SiteIntegrationTests"
```

## Test categories

| Category | Count | CI | Description |
|---|---|---|---|
| Unit tests | 20 | Yes | Schema validation, serialization, WAF detection |
| Integration | 9 | No | Real HTTP requests to DeviantArt, GitHub |

## What tests cover

- **DataSchemaValidationTests**: validates `data.json` against `data.schema.json`
- **SiteDataProviderTests**: loading and parsing site definitions
- **SiteCheckerTests**: regex validation, WAF detection
- **SherlockFactoryTests**: factory creation, site loading, illegal usernames
- **SiteIntegrationTests**: real HTTP checks against DeviantArt
