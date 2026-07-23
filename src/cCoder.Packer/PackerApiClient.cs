using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace cCoder.Packer;

public sealed class PackerApiClient(HttpClient httpClient, Uri source)
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
            new Uri(source, "Api/Account/Login"),
            new { User = user, Pass = password },
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        string token = document.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException(
                "The login response did not contain a bearer token.");

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
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
                request,
                cancellationToken);

            foreach (JsonElement item in GetProperty(
                response.RootElement,
                "value")
                .EnumerateArray())
            {
                using JsonDocument value = JsonDocument.Parse(
                    GetProperty(item, "Json").GetString() ?? "{}");

                records.Add(CreateExportRecord(
                    "Common Cache",
                    type.Split('/').Last(),
                    value.RootElement.Clone()));
            }
        }

        return records;
    }

    public async Task<IReadOnlyList<ExportRecord>> ExportAppAsync(
        int? requestedAppId,
        CancellationToken cancellationToken = default)
    {
        int appId = requestedAppId
            ?? await ResolveAppIdAsync(cancellationToken);

        using JsonDocument response = await GetJsonAsync(
            $"Api/Core/Package/Export?appId={appId}",
            cancellationToken);

        List<ExportRecord> records = [];

        foreach (JsonElement package in response.RootElement.EnumerateArray())
        {
            if (!TryGetProperty(package, "Items", out JsonElement items))
                continue;

            foreach (JsonElement item in items.EnumerateArray())
            {
                string type = GetProperty(item, "Type").GetString()
                    ?? string.Empty;
                string entityType = type.Split('/').LastOrDefault()
                    ?? throw new InvalidDataException(
                        $"Package item type '{type}' is invalid.");

                using JsonDocument data = JsonDocument.Parse(
                    GetProperty(item, "Data").GetString() ?? "[]");

                IEnumerable<JsonElement> values =
                    data.RootElement.ValueKind == JsonValueKind.Array
                        ? data.RootElement.EnumerateArray()
                        : [data.RootElement];

                records.AddRange(values.Select(value => CreateExportRecord(
                    source.Host,
                    entityType,
                    value.Clone())));
            }
        }

        return records;
    }

    private async Task<int> ResolveAppIdAsync(
        CancellationToken cancellationToken)
    {
        string domain = source.Host.Replace("'", "''");
        using JsonDocument response = await GetJsonAsync(
            $"Api/ContentManagement/App?$filter=Domain eq '{domain}'&$top=2",
            cancellationToken);

        JsonElement[] apps = GetProperty(response.RootElement, "value")
            .EnumerateArray()
            .ToArray();

        return apps.Length switch
        {
            1 => GetProperty(apps[0], "Id").GetInt32(),
            0 => throw new InvalidOperationException(
                $"No app uses the domain '{source.Host}'. Use '-appId' explicitly."),
            _ => throw new InvalidOperationException(
                $"Multiple apps use the domain '{source.Host}'. Use '-appId' explicitly."),
        };
    }

    private async Task<JsonDocument> GetJsonAsync(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            new Uri(source, relativeUrl),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string detail = await response.Content.ReadAsStringAsync(
            cancellationToken);

        throw new HttpRequestException(
            $"{(int)response.StatusCode} {response.ReasonPhrase}: {detail}",
            null,
            response.StatusCode);
    }

    private static ExportRecord CreateExportRecord(
        string domain,
        string entityType,
        JsonElement value)
    {
        if (entityType.Equals("Resource", StringComparison.OrdinalIgnoreCase))
        {
            string key = OptionalString(value, "Key", "Unkeyed");
            string culture = OptionalString(value, "Culture", "Default");

            return new ExportRecord(
                domain,
                Path.Combine("Resources", key),
                culture,
                value,
                CombineValues: true);
        }

        return new ExportRecord(
            domain,
            Pluralize(entityType),
            BusinessObjectName(entityType, value),
            value);
    }

    private static string BusinessObjectName(
        string entityType,
        JsonElement value)
    {
        if (entityType is "Page" or "PageRole" or "FolderRole"
            && TryGetProperty(value, "Path", out JsonElement pathProperty))
        {
            string path = string.IsNullOrWhiteSpace(pathProperty.GetString())
                ? "Root"
                : pathProperty.GetString()!;

            string qualifier = entityType switch
            {
                "PageRole" => OptionalString(value, "Role", string.Empty),
                "FolderRole" => OptionalString(value, "Name", string.Empty),
                _ => string.Empty,
            };

            return string.IsNullOrWhiteSpace(qualifier)
                ? path
                : $"{path}-{qualifier}";
        }

        foreach (string propertyName in new[] { "Name", "Domain", "Key", "Id" })
        {
            if (TryGetString(value, propertyName, out string? candidate))
                return candidate;
        }

        throw new InvalidDataException(
            $"An exported {entityType} did not contain a usable identity.");
    }

    private static string Pluralize(string entityType) =>
        entityType.EndsWith('s')
            ? entityType
            : entityType.EndsWith('y')
                ? $"{entityType[..^1]}ies"
                : $"{entityType}s";

    private static string OptionalString(
        JsonElement value,
        string name,
        string fallback) =>
        TryGetString(value, name, out string? result)
            ? result
            : fallback;

    private static bool TryGetString(
        JsonElement value,
        string name,
        [NotNullWhen(true)]
        out string? result)
    {
        result = null;
        if (!TryGetProperty(value, name, out JsonElement property)
            || property.ValueKind is JsonValueKind.Null
                or JsonValueKind.Undefined)
        {
            return false;
        }

        result = property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();

        return !string.IsNullOrWhiteSpace(result);
    }

    private static JsonElement GetProperty(
        JsonElement value,
        string name) =>
        TryGetProperty(value, name, out JsonElement property)
            ? property
            : throw new InvalidDataException(
                $"The API response did not contain '{name}'.");

    private static bool TryGetProperty(
        JsonElement value,
        string name,
        out JsonElement property)
    {
        if (value.TryGetProperty(name, out property))
            return true;

        string camelCaseName =
            char.ToLowerInvariant(name[0]) + name[1..];

        return value.TryGetProperty(camelCaseName, out property);
    }
}
