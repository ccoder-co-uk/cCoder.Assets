// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using cCoder.Packer.Models.Configurations;
using cCoder.Packer.Models.Packages;

namespace cCoder.Packer.Services.Processings.Packing;

internal sealed partial class PackageBuilderProcessingService
    : IPackageBuilderProcessingService
{
    public Task<IReadOnlyList<string>> BuildPackagesAsync(
        string dataPath,
        string packagesPath,
        CancellationToken cancellationToken = default) =>
        TryCatch(operation: async () =>
        {
            Validate(
                inputs: [dataPath, packagesPath, cancellationToken]);

            return await BuildPackagesInternalAsync(
                dataPath: dataPath,
                packagesPath: packagesPath,
                cancellationToken: cancellationToken);
        });

    private static async Task<IReadOnlyList<string>> BuildPackagesInternalAsync(
        string dataPath,
        string packagesPath,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path: dataPath))
        {
            throw new DirectoryNotFoundException(
                message: $"Data directory '{dataPath}' does not exist.");
        }

        if (Directory.Exists(path: packagesPath))
        {
            Directory.Delete(path: packagesPath, recursive: true);
        }

        string firstTimeSetupPath = Path.Combine(
            path1: packagesPath,
            path2: "FirstTimeSetup");

        Directory.CreateDirectory(path: firstTimeSetupPath);

        List<PackageSourceItem> items = [];

        foreach (string file in Directory.EnumerateFiles(
                path: dataPath,
                searchPattern: "*.json",
                searchOption: SearchOption.AllDirectories)
            .Order(comparer: StringComparer.OrdinalIgnoreCase))
        {
            items.AddRange(collection: await ReadItemsAsync(
                dataPath: dataPath,
                file: file,
                cancellationToken: cancellationToken));
        }

        List<string> writtenFiles = [];

        foreach (IGrouping<
            (bool FirstTimeSetup, string Source, string Key, string Type),
            PackageSourceItem> group in items.GroupBy(
                keySelector: item => (
                    item.FirstTimeSetup,
                    item.Source,
                    item.Key,
                    item.Type))
                .OrderBy(
                    keySelector: group => group.Key.FirstTimeSetup)
                .ThenBy(
                    keySelector: group => group.Key.Source,
                    comparer: StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    keySelector: group => group.Key.Key,
                    comparer: StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    keySelector: group => group.Key.Type,
                    comparer: StringComparer.OrdinalIgnoreCase))
        {
            string sourceFolder =
                group.Key.Source.Equals(
                    value: "Common Cache",
                    comparisonType: StringComparison.OrdinalIgnoreCase)
                    ? "Common Cache"
                    : Path.Combine(
                        path1: "App Packages",
                        path2: SafeSegment(value: group.Key.Source));

            string directory = group.Key.FirstTimeSetup
                ? Path.Combine(
                    paths:
                    [
                        packagesPath,
                        "FirstTimeSetup",
                        sourceFolder,
                        SafeSegment(value: group.Key.Key),
                    ])
                : Path.Combine(
                    paths:
                    [
                        packagesPath,
                        sourceFolder,
                        SafeSegment(value: group.Key.Key),
                    ]);

            Directory.CreateDirectory(path: directory);

            string file = Path.Combine(
                path1: directory,
                path2: $"{SafeSegment(value: group.Key.Type)}.json");

            JsonElement[] values =
            [
                .. group
                    .Select(selector: item => item.Value)
                    .OrderBy(
                        keySelector: value => value.GetRawText(),
                        comparer: StringComparer.Ordinal),
            ];

            AssetPackage package = new(
                Name: $"{group.Key.Key} {group.Key.Type}",
                Description: $"Generated {group.Key.Type} package for " +
                    $"{group.Key.Key}.",
                Category: group.Key.Key,
                SourceApi: group.Key.Type
                    .Split(separator: '/')
                    .First(),
                Items:
                [
                    new AssetPackageItem(
                        Type: group.Key.Type,
                        Data: JsonSerializer.Serialize(
                            value: values,
                            options: JsonDefaults.Options)),
                ]);

            await File.WriteAllTextAsync(
                path: file,
                contents: JsonSerializer.Serialize(
                    value: package,
                    options: JsonDefaults.Options),
                cancellationToken: cancellationToken);

            writtenFiles.Add(item: file);
        }

        if (!items.Any(predicate: item => item.FirstTimeSetup))
        {
            await File.WriteAllTextAsync(
                path: Path.Combine(
                    path1: firstTimeSetupPath,
                    path2: ".gitkeep"),
                contents: string.Empty,
                cancellationToken: cancellationToken);
        }

        string manifestFile = await WriteManifestAsync(
            packagesPath: packagesPath,
            packageFiles: writtenFiles,
            cancellationToken: cancellationToken);

        writtenFiles.Add(item: manifestFile);

        return writtenFiles;
    }

    private static async Task<string> WriteManifestAsync(
        string packagesPath,
        IEnumerable<string> packageFiles,
        CancellationToken cancellationToken)
    {
        List<AssetPackageManifestItem> manifestItems = [];

        foreach (string packageFile in packageFiles.Order(
            comparer: StringComparer.OrdinalIgnoreCase))
        {
            byte[] packageBytes = await File.ReadAllBytesAsync(
                path: packageFile,
                cancellationToken: cancellationToken);

            AssetPackage package =
                JsonSerializer.Deserialize<AssetPackage>(
                    utf8Json: packageBytes,
                    options: JsonDefaults.Options)
                ?? throw new InvalidDataException(
                    message: $"Package '{packageFile}' is invalid.");

            string relativePath = Path.GetRelativePath(
                    relativeTo: packagesPath,
                    path: packageFile)
                .Replace(
                    oldChar: Path.DirectorySeparatorChar,
                    newChar: '/');

            string[] pathSegments = relativePath.Split(separator: '/');

            int scopeIndex = string.Equals(
                a: pathSegments[0],
                b: "FirstTimeSetup",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;

            string source = string.Equals(
                a: pathSegments[scopeIndex],
                b: "Common Cache",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? "Common Cache"
                : pathSegments[scopeIndex + 1];

            manifestItems.Add(item: new AssetPackageManifestItem(
                Path: relativePath,
                Sha256: Convert.ToHexString(
                    inArray: SHA256.HashData(source: packageBytes)),
                FirstTimeSetup: relativePath.StartsWith(
                    value: "FirstTimeSetup/",
                    comparisonType: StringComparison.OrdinalIgnoreCase),
                Source: source,
                Category: package.Category,
                ItemTypes:
                [
                    .. package.Items
                        .Select(selector: item => item.Type)
                        .Distinct(comparer: StringComparer.OrdinalIgnoreCase)
                        .Order(comparer: StringComparer.OrdinalIgnoreCase),
                ]));
        }

        AssetPackageManifest manifest = new(
            SchemaVersion: 1,
            Packages: [.. manifestItems]);

        string manifestFile = Path.Combine(
            path1: packagesPath,
            path2: "manifest.json");

        await File.WriteAllTextAsync(
            path: manifestFile,
            contents: JsonSerializer.Serialize(
                value: manifest,
                options: JsonDefaults.Options),
            cancellationToken: cancellationToken);

        return manifestFile;
    }

    private static async Task<IEnumerable<PackageSourceItem>> ReadItemsAsync(
        string dataPath,
        string file,
        CancellationToken cancellationToken)
    {
        string[] segments = Path.GetRelativePath(
            relativeTo: dataPath,
            path: file)
            .Split(
                separator:
                [
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar,
                ],
                options: StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 4)
        {
            return [];
        }

        using JsonDocument document = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(
                path: file,
                cancellationToken: cancellationToken));

        IEnumerable<JsonElement> values =
            document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray()
                : [document.RootElement];

        return
        [
            .. values.Select(selector: value =>
                CreatePackageSourceItem(
                    source: segments[0],
                    key: segments[1],
                    typeFolder: segments[2],
                    value: value)),
        ];
    }

    private static PackageSourceItem CreatePackageSourceItem(
        string source,
        string key,
        string typeFolder,
        JsonElement value)
    {
        JsonObject item =
            JsonNode.Parse(json: value.GetRawText())?.AsObject()
            ?? throw new InvalidDataException(
                message: "A split asset must be a JSON object.");

        string type = item["PackageType"]?.GetValue<string>()
            ?? InferPackageType(
                key: key,
                typeFolder: typeFolder);

        bool firstTimeSetup =
            item["IncludeInSubSequentImports"]?.GetValue<bool>()
            ?? false;

        item.Remove(propertyName: "PackageType");
        item.Remove(propertyName: "IncludeInSubSequentImports");

        return new PackageSourceItem(
            Source: source,
            Key: key,
            Type: type,
            FirstTimeSetup: firstTimeSetup,
            Value: JsonSerializer.SerializeToElement(
                value: item,
                options: JsonDefaults.Options));
    }

    private static string InferPackageType(
        string key,
        string typeFolder) =>
        typeFolder switch
        {
            "Apps" => "ContentManagement/App",
            "Components" => "ContentManagement/Component",
            "FolderRoles" => "DocumentManagement/FolderRole",
            "Layouts" => "ContentManagement/Layout",
            "PageRoles" => "ContentManagement/PageRole",
            "Pages" => "ContentManagement/Page",
            "Resources" => "ContentManagement/Resource",
            "Roles" => "AppSecurity/Role",
            "Scripts" => "ContentManagement/Script",
            "Templates" => "ContentManagement/Template",
            "FlowDefinitions" => "Workflow/FlowDefinition",
            "Calendars" => "Workflow/Calendar",
            "CalendarEvents" => "Workflow/CalendarEvent",
            _ => $"{key}/{Singularize(value: typeFolder)}",
        };

    private static string Singularize(string value) =>
        value.EndsWith(
            value: "ies",
            comparisonType: StringComparison.OrdinalIgnoreCase)
            ? value[..^3] + "y"
            : value.EndsWith(
                value: "s",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? value[..^1]
                : value;

    private static string SafeSegment(string value)
    {
        char[] invalidCharacters =
        [
            .. Path.GetInvalidFileNameChars(),
            '/',
            '\\',
        ];

        string result = new(value:
        [
            .. value.Select(selector: character =>
                invalidCharacters.Contains(value: character)
                    ? '_'
                    : character),
        ]);

        return result.Trim();
    }

}