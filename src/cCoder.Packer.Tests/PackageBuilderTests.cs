// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using System.Security.Cryptography;
using cCoder.Packer.Services.Processings.Packing;

namespace cCoder.Packer.Tests;

public sealed partial class PackageBuilderTests
{
    [Fact]
    public async Task ShouldBuildRegularAndFirstTimeSetupPackages()
    {
        // Given
        string root = Path.Combine(
            path1: Path.GetTempPath(),
            path2: $"ccoder-packages-{Guid.NewGuid():N}");

        string dataPath = Path.Combine(path1: root, path2: "Data");
        string packagesPath = Path.Combine(path1: root, path2: "Packages");

        Directory.CreateDirectory(path: packagesPath);

        string stalePackageFile = Path.Combine(
            path1: packagesPath,
            path2: "stale-package.json");

        await File.WriteAllTextAsync(
            path: stalePackageFile,
            contents: "{}");

        string sourceDirectory = Path.Combine(
            paths:
            [
                dataPath,
                "ccoder.co.uk",
                "CMS",
                "Components",
            ]);

        Directory.CreateDirectory(path: sourceDirectory);

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: sourceDirectory,
                path2: "Regular.json"),
            contents:
                """
                {
                  "Name": "Regular",
                  "PackageType": "ContentManagement/Component",
                  "IncludeInSubSequentImports": false
                }
                """);

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: sourceDirectory,
                path2: "Setup.json"),
            contents:
                """
                {
                  "Name": "Setup",
                  "PackageType": "ContentManagement/Component",
                  "IncludeInSubSequentImports": true
                }
                """);

        PackageBuilderProcessingService service = new();

        // When
        IReadOnlyList<string> files = await service.BuildPackagesAsync(
            dataPath: dataPath,
            packagesPath: packagesPath);

        // Then
        Assert.Equal(expected: 3, actual: files.Count);

        Assert.False(condition: File.Exists(path: stalePackageFile));

        string regularFile = Path.Combine(
            paths:
            [
                packagesPath,
                "App Packages",
                "ccoder.co.uk",
                "CMS",
                "ContentManagement_Component.json",
            ]);

        string setupFile = Path.Combine(
            paths:
            [
                packagesPath,
                "FirstTimeSetup",
                "App Packages",
                "ccoder.co.uk",
                "CMS",
                "ContentManagement_Component.json",
            ]);

        Assert.True(condition: File.Exists(path: regularFile));
        Assert.True(condition: File.Exists(path: setupFile));

        using JsonDocument package = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: setupFile));

        string data = package.RootElement.GetProperty(
            propertyName: "Items")[0]
            .GetProperty(propertyName: "Data")
            .GetString()!;

        Assert.DoesNotContain(
            expectedSubstring: "IncludeInSubSequentImports",
            actualString: data);

        string manifestFile = Path.Combine(
            path1: packagesPath,
            path2: "manifest.json");

        using JsonDocument manifest = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: manifestFile));

        Assert.Equal(
            expected: 1,
            actual: manifest.RootElement
                .GetProperty(propertyName: "SchemaVersion")
                .GetInt32());

        Assert.Equal(
            expected: 2,
            actual: manifest.RootElement
                .GetProperty(propertyName: "Packages")
                .GetArrayLength());

        JsonElement setupManifestItem = manifest.RootElement
            .GetProperty(propertyName: "Packages")
            .EnumerateArray()
            .Single(predicate: item =>
                item.GetProperty(propertyName: "FirstTimeSetup")
                    .GetBoolean());

        Assert.Equal(
            expected: "ccoder.co.uk",
            actual: setupManifestItem
                .GetProperty(propertyName: "Source")
                .GetString());

        Assert.Equal(
            expected: Convert.ToHexString(
                inArray: SHA256.HashData(
                    source: await File.ReadAllBytesAsync(path: setupFile))),
            actual: setupManifestItem
                .GetProperty(propertyName: "Sha256")
                .GetString());

        string firstManifest = await File.ReadAllTextAsync(
            path: manifestFile);

        await service.BuildPackagesAsync(
            dataPath: dataPath,
            packagesPath: packagesPath);

        Assert.Equal(
            expected: firstManifest,
            actual: await File.ReadAllTextAsync(path: manifestFile));

        Directory.Delete(path: root, recursive: true);
    }
}