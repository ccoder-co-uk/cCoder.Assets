// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using cCoder.Packer.Services.Processings.Packing;

namespace cCoder.Packer.Tests;

public sealed partial class PackageBuilderTests
{
    [Fact]
    public async Task ShouldPackageCommonCacheStyle()
    {
        // Given
        string root = Path.Combine(
            path1: Path.GetTempPath(),
            path2: $"ccoder-style-package-{Guid.NewGuid():N}");

        string sourcePath = Path.Combine(
            paths: [root, "Data", "Common Cache", "Common"]);

        string stylesPath = Path.Combine(
            path1: sourcePath,
            path2: "Styles");

        Directory.CreateDirectory(path: stylesPath);

        await File.WriteAllTextAsync(
            path: Path.Combine(path1: stylesPath, path2: "Baseline.json"),
            contents:
                """
                {
                  "Name": "Baseline",
                  "Key": "Common",
                  "Content": "body { margin: 0; }"
                }
                """);

        string destinationPath = Path.Combine(
            paths: [root, "Packages", "Common Cache", "Common.json"]);

        PackageBuilderProcessingService service = new();

        // When
        await service.BuildPackageAsync(
            sourcePath: sourcePath,
            destinationPath: destinationPath,
            packageName: "Common Common Cache",
            category: "Common");

        // Then
        using JsonDocument package = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: destinationPath));

        JsonElement style = Assert.Single(
            collection: package.RootElement
                .GetProperty(propertyName: "Items")
                .EnumerateArray());

        Assert.Equal(
            expected: "ContentManagement/Style",
            actual: style
                .GetProperty(propertyName: "Type")
                .GetString());

        Directory.Delete(path: root, recursive: true);
    }
}