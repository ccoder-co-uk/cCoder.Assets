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
    public Task<string> BuildPackageAsync(
        string sourcePath,
        string destinationPath,
        string? packageName = null,
        string? category = null,
        CancellationToken cancellationToken = default) =>
        TryCatch(operation: async () =>
        {
            Validate(inputs: [sourcePath, destinationPath, cancellationToken]);

            if (string.IsNullOrWhiteSpace(value: sourcePath))
            {
                throw new ArgumentException(
                    message: "A source folder is required.",
                    paramName: nameof(sourcePath));
            }

            if (string.IsNullOrWhiteSpace(value: destinationPath))
            {
                throw new ArgumentException(
                    message: "A destination package path is required.",
                    paramName: nameof(destinationPath));
            }

            if (!Directory.Exists(path: sourcePath))
            {
                throw new DirectoryNotFoundException(
                    message: $"Source directory '{sourcePath}' does not exist.");
            }

            List<PackageSourceItem> items = [];

            foreach (string file in Directory.EnumerateFiles(
                    path: sourcePath,
                    searchPattern: "*.json",
                    searchOption: SearchOption.AllDirectories)
                .Order(comparer: StringComparer.OrdinalIgnoreCase))
            {
                items.AddRange(collection: await ReadFolderItemsAsync(
                    sourcePath: sourcePath,
                    file: file,
                    cancellationToken: cancellationToken));
            }

            if (items.Count == 0)
            {
                throw new InvalidDataException(
                    message: $"Source directory '{sourcePath}' contains no package items.");
            }

            string? destinationDirectory = Path.GetDirectoryName(
                path: destinationPath);

            if (!string.IsNullOrWhiteSpace(value: destinationDirectory))
            {
                Directory.CreateDirectory(path: destinationDirectory);
            }

            string resolvedCategory = category
                ?? items.Select(selector: item => item.Key)
                    .Distinct(comparer: StringComparer.OrdinalIgnoreCase)
                    .SingleOrDefault()
                ?? Path.GetFileName(path: sourcePath);

            return await WriteKeyPackageAsync(
                file: destinationPath,
                scope: "Package",
                key: resolvedCategory,
                sourceItems: items,
                cancellationToken: cancellationToken,
                packageName: packageName);
        });

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

        string commonCachePath = Path.Combine(
            path1: packagesPath,
            path2: "Common Cache");

        string firstTimeSetupPath = Path.Combine(
            path1: packagesPath,
            path2: "First Time Setup");

        string appPath = Path.Combine(
            path1: packagesPath,
            path2: "App");

        Directory.CreateDirectory(path: commonCachePath);
        Directory.CreateDirectory(path: firstTimeSetupPath);
        Directory.CreateDirectory(path: appPath);

        List<string> writtenFiles = [];

        foreach (IGrouping<(string Scope, string Key), PackageSourceItem> group
            in items
                .GroupBy(
                    keySelector: item => (
                        Scope: item.Source.Equals(
                            value: "Common Cache",
                            comparisonType: StringComparison.OrdinalIgnoreCase)
                                ? "Common Cache"
                                : "App",
                        item.Key))
                .OrderBy(
                    keySelector: group => group.Key.Scope,
                    comparer: StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    keySelector: group => group.Key.Key,
                    comparer: StringComparer.OrdinalIgnoreCase))
        {
            string file = Path.Combine(
                path1: Path.Combine(
                    path1: packagesPath,
                    path2: group.Key.Scope),
                path2: $"{SafeSegment(value: group.Key.Key)}.json");

            writtenFiles.Add(item: await WriteKeyPackageAsync(
                file: file,
                scope: group.Key.Scope,
                key: group.Key.Key,
                sourceItems: group,
                cancellationToken: cancellationToken));
        }

        writtenFiles.Add(
            item: await WriteFirstTimeSetupPackageAsync(
                file: Path.Combine(
                    path1: firstTimeSetupPath,
                    path2: "common-cache.json"),
                packageName: "First Time Setup Common Cache",
                sourceItems: items.Where(predicate: item =>
                    item.FirstTimeSetup
                    &&
                    item.Source.Equals(
                        value: "Common Cache",
                        comparisonType: StringComparison.OrdinalIgnoreCase)),
                cancellationToken: cancellationToken));

        writtenFiles.Add(
            item: await WriteFirstTimeSetupPackageAsync(
                file: Path.Combine(
                    path1: firstTimeSetupPath,
                    path2: "app-baseline.json"),
                packageName: "First Time Setup App Baseline",
                sourceItems: items.Where(predicate: item =>
                    item.FirstTimeSetup
                    &&
                    !item.Source.Equals(
                        value: "Common Cache",
                        comparisonType: StringComparison.OrdinalIgnoreCase)),
                cancellationToken: cancellationToken));

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

    private static async Task<string> WriteKeyPackageAsync(
        string file,
        string scope,
        string key,
        IEnumerable<PackageSourceItem> sourceItems,
        CancellationToken cancellationToken,
        string? packageName = null)
    {
        AssetPackageItem[] packageItems =
        [
            .. sourceItems
                .GroupBy(
                    keySelector: item => item.Type,
                    comparer: StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    keySelector: group => group.Key,
                    comparer: StringComparer.OrdinalIgnoreCase)
                .Select(selector: group => new AssetPackageItem(
                    Type: group.Key,
                    Data: JsonSerializer.Serialize(
                        value: group
                            .Select(selector: item => item.Value)
                            .OrderBy(
                                keySelector: value => value.GetRawText(),
                                comparer: StringComparer.Ordinal)
                            .ToArray(),
                        options: JsonDefaults.Options))),
        ];

        AssetPackage package = new(
            Name: packageName ?? $"{key} {scope}",
            Description: $"Generated {scope.ToLowerInvariant()} " +
                $"functionality package for {key}.",
            Category: key,
            SourceApi: "Multiple",
            Items: packageItems);

        await File.WriteAllTextAsync(
            path: file,
            contents: JsonSerializer.Serialize(
                value: package,
                options: JsonDefaults.Options),
            cancellationToken: cancellationToken);

        return file;
    }

    private static async Task<string> WriteFirstTimeSetupPackageAsync(
        string file,
        string packageName,
        IEnumerable<PackageSourceItem> sourceItems,
        CancellationToken cancellationToken)
    {
        AssetPackageItem[] packageItems =
        [
            .. sourceItems
                .GroupBy(
                    keySelector: item => item.Type,
                    comparer: StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    keySelector: group => group.Key,
                    comparer: StringComparer.OrdinalIgnoreCase)
                .Select(selector: group =>
                    new AssetPackageItem(
                        Type: group.Key,
                        Data: JsonSerializer.Serialize(
                            value: group
                                .Select(selector: item => item.Value)
                                .OrderBy(
                                    keySelector: value =>
                                        value.GetRawText(),
                                    comparer: StringComparer.Ordinal)
                                .ToArray(),
                            options: JsonDefaults.Options))),
        ];

        AssetPackage package = new(
            Name: packageName,
            Description: $"Generated {packageName.ToLowerInvariant()}.",
            Category: "First Time Setup",
            SourceApi: "Multiple",
            Items: packageItems);

        await File.WriteAllTextAsync(
            path: file,
            contents: JsonSerializer.Serialize(
                value: package,
                options: JsonDefaults.Options),
            cancellationToken: cancellationToken);

        return file;
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
                b: "First Time Setup",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;

            string source = relativePath.Equals(
                value: "First Time Setup/app-baseline.json",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                    ? "App"
                    : relativePath.Equals(
                        value: "First Time Setup/common-cache.json",
                        comparisonType: StringComparison.OrdinalIgnoreCase)
                            ? "Common Cache"
                            : pathSegments[scopeIndex];

            manifestItems.Add(item: new AssetPackageManifestItem(
                Path: relativePath,
                Sha256: Convert.ToHexString(
                    inArray: SHA256.HashData(source: packageBytes)),
                FirstTimeSetup: relativePath.StartsWith(
                    value: "First Time Setup/",
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

    private static async Task<IEnumerable<PackageSourceItem>> ReadFolderItemsAsync(
        string sourcePath,
        string file,
        CancellationToken cancellationToken)
    {
        string[] segments = Path.GetRelativePath(
            relativeTo: sourcePath,
            path: file)
            .Split(
                separator:
                [
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar,
                ],
                options: StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2)
        {
            return [];
        }

        string key = Path.GetFileName(
            path: Path.TrimEndingDirectorySeparator(path: sourcePath));

        string typeFolder = segments[^2];

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
                    source: Path.GetFileName(
                        path: Directory.GetParent(path: sourcePath)?.FullName
                            ?? sourcePath),
                    key: key,
                    typeFolder: typeFolder,
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