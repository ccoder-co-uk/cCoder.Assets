// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using cCoder.Packer.Models.Exports;

namespace cCoder.Packer.Dependencies;

public sealed class PackerApiClientDependency(HttpClient httpClient, Uri source)
{
    private static readonly string[] CacheTypes =
    [
        "Core/Component",
        "Core/Resource",
        "Core/Script",
    ];

    public async Task LoginAsync(
        string user,
        string password,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
requestUri: new Uri(baseUri: source, relativeUri: "Api/Account/Login"),
value: new { User = user, Pass = password },
cancellationToken: cancellationToken);

        await EnsureSuccessAsync(response: response, cancellationToken: cancellationToken);

        using JsonDocument document = await JsonDocument.ParseAsync(
utf8Json: await response.Content.ReadAsStreamAsync(cancellationToken: cancellationToken),
            cancellationToken: cancellationToken);

        string token = document.RootElement
            .GetProperty(propertyName: "id")
            .GetString()
            ?? throw new InvalidOperationException(
message: "The login response did not contain a bearer token.");

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(scheme: "Bearer", parameter: token);
    }

    public async Task<IReadOnlyList<ExportRecord>> ExportCommonCacheAsync(
        CancellationToken cancellationToken = default)
    {
        List<ExportRecord> records = [];

        foreach (string type in CacheTypes)
        {
            string request =
                $"Api/ContentManagement/CommonObject/Latest()" +
                $"?type={type}&$orderby=Name asc";

            using JsonDocument response = await GetJsonAsync(
relativeUrl: request,
cancellationToken: cancellationToken);

            foreach (JsonElement item in GetProperty(
value: response.RootElement,
name: "value")
                .EnumerateArray())
            {
                using JsonDocument value = JsonDocument.Parse(
                    json: GetProperty(value: item, name: "Json")
                        .GetString() ?? "{}");

                records.Add(item: CreateExportRecord(
domain: "Common Cache",
                    entityType: type
                        .Split(separator: '/')
                        .Last(),
value: value.RootElement.Clone()));
            }
        }

        return records;
    }

    public async Task<IReadOnlyList<ExportRecord>> ExportAppAsync(
        int? requestedAppId,
        CancellationToken cancellationToken = default)
    {
        int appId = requestedAppId
            ?? await ResolveAppIdAsync(cancellationToken: cancellationToken);

        using JsonDocument response = await GetJsonAsync(
relativeUrl: $"Api/Core/Package/Export?appId={appId}",
cancellationToken: cancellationToken);

        List<ExportRecord> records = [];

        foreach (JsonElement package in response.RootElement.EnumerateArray())
        {
            if (!TryGetProperty(value: package, name: "Items", property: out JsonElement items))
            {
                continue;
            }

            foreach (JsonElement item in items.EnumerateArray())
            {
                string type = GetProperty(value: item, name: "Type")
                    .GetString()
                    ?? string.Empty;

                string entityType = type
                    .Split(separator: '/')
                    .LastOrDefault()
                    ?? throw new InvalidDataException(
                        message: $"Package item type '{type}' is invalid.");

                using JsonDocument data = JsonDocument.Parse(
                    json: GetProperty(value: item, name: "Data")
                        .GetString() ?? "[]");

                IEnumerable<JsonElement> values =
                    data.RootElement.ValueKind == JsonValueKind.Array
                        ? data.RootElement.EnumerateArray()
                        : [data.RootElement];

                records.AddRange(collection: values.Select(selector: value => CreateExportRecord(
domain: source.Host,
entityType: entityType,
value: value.Clone())));
            }
        }

        return records;
    }

    private async Task<int> ResolveAppIdAsync(
        CancellationToken cancellationToken)
    {
        string domain = source.Host.Replace(oldValue: "'", newValue: "''");

        using JsonDocument response = await GetJsonAsync(
relativeUrl: $"Api/ContentManagement/App?$filter=Domain eq '{domain}'&$top=2",
cancellationToken: cancellationToken);

        JsonElement[] apps = GetProperty(value: response.RootElement, name: "value")
            .EnumerateArray()
            .ToArray();

        return apps.Length switch
        {
            1 => GetProperty(value: apps[0], name: "Id")
                .GetInt32(),
            0 => throw new InvalidOperationException(
message: $"No app uses the domain '{source.Host}'. Use '-appId' explicitly."),
            _ => throw new InvalidOperationException(
message: $"Multiple apps use the domain '{source.Host}'. Use '-appId' explicitly."),
        };
    }

    private async Task<JsonDocument> GetJsonAsync(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
requestUri: new Uri(baseUri: source, relativeUri: relativeUrl),
cancellationToken: cancellationToken);

        await EnsureSuccessAsync(response: response, cancellationToken: cancellationToken);

        return await JsonDocument.ParseAsync(
utf8Json: await response.Content.ReadAsStreamAsync(cancellationToken: cancellationToken),
            cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string detail = await response.Content.ReadAsStringAsync(
cancellationToken: cancellationToken);

        throw new HttpRequestException(
message: $"{(int)response.StatusCode} {response.ReasonPhrase}: {detail}",
inner: null,
statusCode: response.StatusCode);
    }

    private static ExportRecord CreateExportRecord(
        string domain,
        string entityType,
        JsonElement value)
    {
        if (entityType.Equals(value: "Resource", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            string key = OptionalString(value: value, name: "Key", fallback: "Unkeyed");
            string culture = OptionalString(value: value, name: "Culture", fallback: "Default");

            return new ExportRecord(
Domain: domain,
Category: Path.Combine(path1: key, path2: "Resources"),
Name: culture,
Value: value,
                CombineValues: true);
        }

        string resourceKey = OptionalString(
value: value,
name: "ResourceKey",
fallback: OptionalString(value: value, name: "Key", fallback: "Default"));

        return new ExportRecord(
Domain: domain,
Category: Path.Combine(path1: resourceKey, path2: Pluralize(entityType: entityType)),
Name: BusinessObjectName(entityType: entityType, value: value),
Value: value);
    }

    private static string BusinessObjectName(
        string entityType,
        JsonElement value)
    {
        if (entityType is "Page" or "PageRole" or "FolderRole"
            && TryGetProperty(value: value, name: "Path", property: out JsonElement pathProperty))
        {
            string path = string.IsNullOrWhiteSpace(value: pathProperty.GetString())
                ? "Root"
                : pathProperty.GetString()!;

            string qualifier = entityType switch
            {
                "PageRole" => OptionalString(value: value, name: "Role", fallback: string.Empty),
                "FolderRole" => OptionalString(value: value, name: "Name", fallback: string.Empty),
                _ => string.Empty,
            };

            return string.IsNullOrWhiteSpace(value: qualifier)
                ? path
                : $"{path}-{qualifier}";
        }

        foreach (string propertyName in new[] { "Name", "Domain", "Key", "Id" })
        {
            if (TryGetString(value: value, name: propertyName, result: out string? candidate))
            {
                return candidate;
            }
        }

        throw new InvalidDataException(
message: $"An exported {entityType} did not contain a usable identity.");
    }

    private static string Pluralize(string entityType) =>
        entityType.EndsWith(value: 's')
            ? entityType
            : entityType.EndsWith(value: 'y')
                ? $"{entityType[..^1]}ies"
                : $"{entityType}s";

    private static string OptionalString(
        JsonElement value,
        string name,
        string fallback) =>
        TryGetString(value: value, name: name, result: out string? result)
            ? result
            : fallback;

    private static bool TryGetString(
        JsonElement value,
        string name,
        [NotNullWhen(true)]
        out string? result)
    {
        result = null;

        if (!TryGetProperty(value: value, name: name, property: out JsonElement property)
            || property.ValueKind is JsonValueKind.Null
                or JsonValueKind.Undefined)
        {
            return false;
        }

        result = property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();

        return !string.IsNullOrWhiteSpace(value: result);
    }

    private static JsonElement GetProperty(
        JsonElement value,
        string name) =>
        TryGetProperty(value: value, name: name, property: out JsonElement property)
            ? property
            : throw new InvalidDataException(
message: $"The API response did not contain '{name}'.");

    private static bool TryGetProperty(
        JsonElement value,
        string name,
        out JsonElement property)
    {
        if (value.TryGetProperty(propertyName: name, value: out property))
        {
            return true;
        }

        string camelCaseName =
            char.ToLowerInvariant(c: name[0]) + name[1..];

        return value.TryGetProperty(propertyName: camelCaseName, value: out property);
    }
}