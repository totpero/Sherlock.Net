using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Sherlock.Net.Core;
using Sherlock.Net.Core.Models;
using Shouldly;

namespace Sherlock.Net.Core.Tests.Services;

public class DataSchemaValidationTests
{
    private static readonly Assembly CoreAssembly = typeof(SiteData).Assembly;

    private readonly string _dataJson;
    private readonly string _schemaJson;

    public DataSchemaValidationTests()
    {
        _dataJson = LoadEmbeddedResource(SherlockDefaults.DataResourceName);
        _schemaJson = LoadEmbeddedResource(SherlockDefaults.SchemaResourceName);
    }

    private static string LoadEmbeddedResource(string resourceName)
    {
        using var stream = CoreAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    public void DataJson_ValidatesAgainstSchema()
    {
        var schema = JsonSchema.FromText(_schemaJson);
        var dataNode = JsonNode.Parse(_dataJson);

        var result = schema.Evaluate(dataNode, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        });

        result.IsValid.ShouldBeTrue(
            $"data.json failed schema validation. First errors: {GetFirstErrors(result)}");
    }

    [Fact]
    public void DataJson_IsValidJsonObject()
    {
        var doc = JsonDocument.Parse(_dataJson);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public void DataJson_AllSitesHaveRequiredFields()
    {
        var doc = JsonDocument.Parse(_dataJson);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Name.StartsWith('$'))
                continue;

            var site = property.Value;
            var siteName = property.Name;

            site.TryGetProperty("url", out _)
                .ShouldBeTrue($"Site '{siteName}' missing required field 'url'");

            site.TryGetProperty("urlMain", out _)
                .ShouldBeTrue($"Site '{siteName}' missing required field 'urlMain'");

            site.TryGetProperty("errorType", out _)
                .ShouldBeTrue($"Site '{siteName}' missing required field 'errorType'");

            site.TryGetProperty("username_claimed", out _)
                .ShouldBeTrue($"Site '{siteName}' missing required field 'username_claimed'");
        }
    }

    [Fact]
    public void DataJson_AllSitesHavePlaceholderSomewhere()
    {
        var doc = JsonDocument.Parse(_dataJson);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Name.StartsWith('$'))
                continue;

            var siteName = property.Name;
            var site = property.Value;

            var url = site.GetProperty("url").GetString() ?? string.Empty;
            var hasPlaceholder = url.Contains("{}");

            if (!hasPlaceholder && site.TryGetProperty("urlProbe", out var probeEl))
            {
                hasPlaceholder = (probeEl.GetString() ?? string.Empty).Contains("{}");
            }

            if (!hasPlaceholder && site.TryGetProperty("request_payload", out var payloadEl))
            {
                hasPlaceholder = payloadEl.ToString().Contains("{}");
            }

            hasPlaceholder.ShouldBeTrue(
                $"Site '{siteName}' has no '{{}}' placeholder in url, urlProbe, or request_payload");
        }
    }

    [Fact]
    public void DataJson_ErrorTypeValuesAreValid()
    {
        var validTypes = new HashSet<string> { "status_code", "message", "response_url" };
        var doc = JsonDocument.Parse(_dataJson);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Name.StartsWith('$'))
                continue;

            var siteName = property.Name;
            var errorType = property.Value.GetProperty("errorType");

            if (errorType.ValueKind == JsonValueKind.String)
            {
                var value = errorType.GetString()!;
                validTypes.ShouldContain(value,
                    $"Site '{siteName}' has invalid errorType '{value}'");
            }
            else if (errorType.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in errorType.EnumerateArray())
                {
                    var value = item.GetString()!;
                    validTypes.ShouldContain(value,
                        $"Site '{siteName}' has invalid errorType array value '{value}'");
                }
            }
        }
    }

    [Fact]
    public void DataJson_RequestMethodValuesAreValid()
    {
        var validMethods = new HashSet<string> { "GET", "POST", "HEAD", "PUT" };
        var doc = JsonDocument.Parse(_dataJson);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Name.StartsWith('$'))
                continue;

            var siteName = property.Name;

            if (property.Value.TryGetProperty("request_method", out var method))
            {
                var value = method.GetString()!;
                validMethods.ShouldContain(value,
                    $"Site '{siteName}' has invalid request_method '{value}'");
            }
        }
    }

    [Fact]
    public void DataJson_MessageTypeSitesHaveErrorMsg()
    {
        var doc = JsonDocument.Parse(_dataJson);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Name.StartsWith('$'))
                continue;

            var siteName = property.Name;
            var errorType = property.Value.GetProperty("errorType");

            var isMessageType = errorType.ValueKind == JsonValueKind.String
                && errorType.GetString() == "message";

            if (isMessageType)
            {
                property.Value.TryGetProperty("errorMsg", out _)
                    .ShouldBeTrue($"Site '{siteName}' has errorType 'message' but missing 'errorMsg'");
            }
        }
    }

    [Fact]
    public void DataJson_ErrorMsgIsStringOrArray()
    {
        var doc = JsonDocument.Parse(_dataJson);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Name.StartsWith('$'))
                continue;

            var siteName = property.Name;

            if (property.Value.TryGetProperty("errorMsg", out var errorMsg))
            {
                var validKind = errorMsg.ValueKind == JsonValueKind.String
                    || errorMsg.ValueKind == JsonValueKind.Array;

                validKind.ShouldBeTrue(
                    $"Site '{siteName}' errorMsg should be string or array, got {errorMsg.ValueKind}");
            }
        }
    }

    [Fact]
    public void Schema_IsValidJsonSchema()
    {
        var schema = JsonSchema.FromText(_schemaJson);
        schema.ShouldNotBeNull("Schema should parse without errors");
    }

    private static string GetFirstErrors(EvaluationResults result, int maxErrors = 3)
    {
        if (result.IsValid)
            return string.Empty;

        var errors = result.Details
            .Where(d => d.Errors is not null && d.Errors.Count > 0)
            .Take(maxErrors)
            .Select(d =>
            {
                var path = d.InstanceLocation.ToString();
                var msgs = string.Join("; ", d.Errors!.Values);
                return $"  [{path}]: {msgs}";
            });

        return string.Join(Environment.NewLine, errors);
    }
}
