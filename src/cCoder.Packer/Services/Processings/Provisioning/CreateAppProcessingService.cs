// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using cCoder.Packer.Brokers;
using cCoder.Packer.Models.Configurations;
using cCoder.Packer.Models.Packages;

namespace cCoder.Packer.Services.Processings.Provisioning;

internal sealed partial class CreateAppProcessingService(
    IPackerApiBroker apiBroker)
    : ICreateAppProcessingService
{
    public Task<int> ProvisionAppAsync(
        Uri api,
        string name,
        string tenantId,
        string user,
        string password,
        string baselinePath,
        CancellationToken cancellationToken = default) =>
        TryCatch(operation: async () =>
        {
            Validate(inputs:
            [
                api,
                name,
                tenantId,
                user,
                password,
                baselinePath,
                cancellationToken,
            ]);

            return await ProvisionAppWithBaselineAsync(
                api: api,
                name: name,
                tenantId: tenantId,
                user: user,
                password: password,
                baselinePath: baselinePath,
                cancellationToken: cancellationToken);
        });

    private async Task<int> ProvisionAppWithBaselineAsync(
        Uri api,
        string name,
        string tenantId,
        string user,
        string password,
        string baselinePath,
        CancellationToken cancellationToken)
    {
        ValidateBaseline(path: baselinePath);

        JsonElement loginResponse = await PostJsonElementAsync(
            requestUri: Endpoint(api: api, relativePath: "Api/Account/Login"),
            content: JsonSerializer.SerializeToElement(
                value: new
                {
                    User = user,
                    Pass = password,
                },
                options: JsonDefaults.Options),
            bearerToken: null,
            cancellationToken: cancellationToken);

        string bearerToken = RequiredString(
            value: loginResponse,
            propertyName: "id");

        string appDomain = $"{DnsLabel(value: name)}.{api.Host}";

        JsonElement appResponse = await PostJsonElementAsync(
            requestUri: Endpoint(
                api: api,
                relativePath: "Api/ContentManagement/App"),
            content: JsonSerializer.SerializeToElement(
                value: new
                {
                    TenantId = tenantId,
                    Domain = appDomain,
                    Name = name,
                    ConfigJson =
                        "{\"Themes\":{\"Default\":{}}}",
                    DefaultTheme = "Default",
                    DefaultCultureId = string.Empty,
                    Cultures = Array.Empty<object>(),
                },
                options: JsonDefaults.Options),
            bearerToken: bearerToken,
            cancellationToken: cancellationToken);

        int appId = RequiredInt32(
            value: appResponse,
            propertyName: "Id");

        await ImportAppPackagesAsync(
            api: api,
            bearerToken: bearerToken,
            appId: appId,
            path: Path.Combine(
                path1: baselinePath,
                path2: "App"),
            cancellationToken: cancellationToken);

        await ImportCommonCachePackagesAsync(
            api: api,
            bearerToken: bearerToken,
            path: Path.Combine(
                path1: baselinePath,
                path2: "Common Cache"),
            cancellationToken: cancellationToken);

        return appId;
    }

    private async Task ImportAppPackagesAsync(
        Uri api,
        string bearerToken,
        int appId,
        string path,
        CancellationToken cancellationToken)
    {
        foreach (string packageFile in PackageFiles(path: path))
        {
            JsonElement package = await ReadJsonAsync(
                path: packageFile,
                cancellationToken: cancellationToken);

            await PostJsonElementAsync(
                requestUri: Endpoint(
                    api: api,
                    relativePath:
                        $"Api/Core/Package/Import?appId={appId}"),
                content: package,
                bearerToken: bearerToken,
                cancellationToken: cancellationToken);
        }
    }

    private async Task ImportCommonCachePackagesAsync(
        Uri api,
        string bearerToken,
        string path,
        CancellationToken cancellationToken)
    {
        foreach (string packageFile in PackageFiles(path: path))
        {
            AssetPackage package =
                JsonSerializer.Deserialize<AssetPackage>(
                    json: await File.ReadAllTextAsync(
                        path: packageFile,
                        cancellationToken: cancellationToken),
                    options: JsonDefaults.Options)
                ?? throw new InvalidDataException(
                    message: $"Package '{packageFile}' is invalid.");

            JsonElement commonObjects = CreateJsonElement(
                package: package);

            await PostJsonElementAsync(
                requestUri: Endpoint(
                    api: api,
                    relativePath:
                        "Api/ContentManagement/CommonObject/Import"),
                content: commonObjects,
                bearerToken: bearerToken,
                cancellationToken: cancellationToken);
        }
    }

    private static JsonElement CreateJsonElement(
        AssetPackage package)
    {
        JsonArray values = [];

        foreach (AssetPackageItem item in package.Items)
        {
            using JsonDocument data = JsonDocument.Parse(
                json: item.Data);

            IEnumerable<JsonElement> businessObjects =
                data.RootElement.ValueKind == JsonValueKind.Array
                    ? data.RootElement.EnumerateArray()
                    : [data.RootElement];

            foreach (JsonElement businessObject in businessObjects)
            {
                values.Add(item: new JsonObject
                {
                    ["Name"] = RequiredString(
                        value: businessObject,
                        propertyName: "Name"),
                    ["Description"] = OptionalString(
                        value: businessObject,
                        propertyName: "Description"),
                    ["Version"] = 1,
                    ["Key"] = OptionalString(
                        value: businessObject,
                        propertyName: "ResourceKey")
                        ?? OptionalString(
                            value: businessObject,
                            propertyName: "Key")
                        ?? package.Category,
                    ["Type"] = item.Type,
                    ["Json"] = businessObject.GetRawText(),
                    ["Culture"] = OptionalString(
                        value: businessObject,
                        propertyName: "Culture")
                        ?? string.Empty,
                });
            }
        }

        return JsonSerializer.SerializeToElement(
            value: new JsonObject
            {
                ["value"] = values,
            },
            options: JsonDefaults.Options);
    }

    private async Task<JsonElement> PostJsonElementAsync(
        Uri requestUri,
        JsonElement content,
        string? bearerToken,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(
            method: HttpMethod.Post,
            requestUri: requestUri)
        {
            Content = JsonContent.Create(
                inputValue: content,
                options: JsonDefaults.Options),
        };

        request.Headers.Authorization =
            string.IsNullOrWhiteSpace(value: bearerToken)
                ? null
                : new AuthenticationHeaderValue(
                    scheme: "Bearer",
                    parameter: bearerToken);

        using HttpResponseMessage response = await apiBroker.SendAsync(
            request: request,
            cancellationToken: cancellationToken);

        await EnsureSuccessAsync(
            response: response,
            cancellationToken: cancellationToken);

        if (response.Content.Headers.ContentLength == 0)
        {
            return JsonSerializer.SerializeToElement(value: new { });
        }

        using JsonDocument document = await JsonDocument.ParseAsync(
            utf8Json: await response.Content.ReadAsStreamAsync(
                cancellationToken: cancellationToken),
            cancellationToken: cancellationToken);

        return document.RootElement.Clone();
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
            message: $"{(int)response.StatusCode} " +
                $"{response.ReasonPhrase}: {detail}",
            inner: null,
            statusCode: response.StatusCode);
    }

    private static IEnumerable<string> PackageFiles(string path) =>
        Directory.EnumerateFiles(
            path: path,
            searchPattern: "*.json",
            searchOption: SearchOption.AllDirectories)
            .Order(comparer: StringComparer.OrdinalIgnoreCase);

    private static async Task<JsonElement> ReadJsonAsync(
        string path,
        CancellationToken cancellationToken)
    {
        using JsonDocument document = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(
                path: path,
                cancellationToken: cancellationToken));

        return document.RootElement.Clone();
    }

    private static void ValidateBaseline(string path)
    {
        if (!Directory.Exists(path: path))
        {
            throw new DirectoryNotFoundException(
                message: $"Baseline directory '{path}' does not exist.");
        }

        foreach (string requiredDirectory in new[]
        {
            "App",
            "Common Cache",
        })
        {
            string requiredPath = Path.Combine(
                path1: path,
                path2: requiredDirectory);

            if (!Directory.Exists(path: requiredPath))
            {
                throw new DirectoryNotFoundException(
                    message: $"Baseline directory '{requiredPath}' " +
                        "does not exist.");
            }
        }
    }

    private static Uri Endpoint(
        Uri api,
        string relativePath) =>
        new(
            baseUri: api,
            relativeUri: relativePath);

    private static string DnsLabel(string value)
    {
        string label = value
            .Trim()
            .ToLowerInvariant()
            .Replace(oldChar: ' ', newChar: '-');

        if (label.Length is < 1 or > 63
            || label.StartsWith(value: '-')
            || label.EndsWith(value: '-')
            || label.Any(predicate: character =>
                !char.IsAsciiLetterOrDigit(c: character)
                && character != '-'))
        {
            throw new ArgumentException(
                message: $"App name '{value}' cannot form a DNS label.");
        }

        return label;
    }

    private static string RequiredString(
        JsonElement value,
        string propertyName) =>
        TryGetProperty(
            value: value,
            propertyName: propertyName,
            property: out JsonElement property)
            && property.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value: property.GetString())
                ? property.GetString()!
                : throw new InvalidDataException(
                    message: $"The response did not contain " +
                        $"'{propertyName}'.");

    private static int RequiredInt32(
        JsonElement value,
        string propertyName) =>
        TryGetProperty(
            value: value,
            propertyName: propertyName,
            property: out JsonElement property)
            && property.TryGetInt32(value: out int result)
                ? result
                : throw new InvalidDataException(
                    message: $"The response did not contain " +
                        $"'{propertyName}'.");

    private static string? OptionalString(
        JsonElement value,
        string propertyName) =>
        TryGetProperty(
            value: value,
            propertyName: propertyName,
            property: out JsonElement property)
            && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;

    private static bool TryGetProperty(
        JsonElement value,
        string propertyName,
        out JsonElement property)
    {
        if (value.TryGetProperty(
            propertyName: propertyName,
            value: out property))
        {
            return true;
        }

        string camelCaseName =
            char.ToLowerInvariant(c: propertyName[0])
            + propertyName[1..];

        return value.TryGetProperty(
            propertyName: camelCaseName,
            value: out property);
    }
}