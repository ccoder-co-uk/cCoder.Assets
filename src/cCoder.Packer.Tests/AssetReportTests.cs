// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Services.Processings.Reports;

namespace cCoder.Packer.Tests;

public sealed partial class AssetReportTests
{
    [Fact]
    public async Task ShouldReportAssetsAcrossEveryDataDirectory()
    {
        // Given
        string testRoot = Path.Combine(
            path1: Path.GetTempPath(),
            path2: $"ccoder-packer-report-{Guid.NewGuid():N}");

        string directory = Path.Combine(
            path1: testRoot,
            path2: "Data");

        await WriteJsonAsync(
            directory: directory,
            relativePath: "First/Default/Layouts/Default.json",
            value: """
                {
                  "Name": "Default",
                  "Html": "[content[body]]"
                }
                """);

        await WriteJsonAsync(
            directory: directory,
            relativePath: "First/Default/Pages/Home.json",
            value: """
                {
                  "Name": "Home",
                  "Path": "",
                  "Layout": "Default",
                  "Contents": [
                    {
                      "Name": "body",
                      "Html": "[component[Home]]"
                    }
                  ]
                }
                """);

        await WriteJsonAsync(
            directory: directory,
            relativePath: "First/Public/Components/Home.json",
            value: """
                {
                  "Name": "Home",
                  "ResourceKey": "Public",
                  "Content": "[script[Home]]"
                }
                """);

        await WriteJsonAsync(
            directory: directory,
            relativePath: "First/Public/Scripts/Home.json",
            value: """
                {
                  "Name": "Home",
                  "Content": "window.home = true;"
                }
                """);

        // When
        AssetReportProcessingService service = new();

        string reportPath = await service.WriteAsync(
            dataPath: directory);

        string report = await File.ReadAllTextAsync(path: reportPath);

        // Then
        Assert.Contains(
            expectedSubstring: "- Directories scanned: 1",
            actualString: report);

        Assert.Contains(
            expectedSubstring: "Component `Home`",
            actualString: report);

        Assert.Contains(
            expectedSubstring: "Script `Home`",
            actualString: report);

        await WriteJsonAsync(
            directory: directory,
            relativePath: "Second/Public/Components/Home.json",
            value: """
                {
                  "Name": "Home",
                  "ResourceKey": "WrongSource"
                }
                """);

        reportPath = await service.WriteAsync(dataPath: directory);
        report = await File.ReadAllTextAsync(path: reportPath);

        Assert.DoesNotContain(
            expectedSubstring: "resource key: `WrongSource`",
            actualString: report);

        Directory.Delete(path: testRoot, recursive: true);
    }

    private static async Task WriteJsonAsync(
        string directory,
        string relativePath,
        string value)
    {
        string path = Path.Combine(
            path1: directory,
            path2: relativePath);

        string? parent = Path.GetDirectoryName(path: path);

        Directory.CreateDirectory(
            path: parent
                ?? throw new InvalidOperationException(
                    message: "A parent directory is required."));

        await File.WriteAllTextAsync(path: path, contents: value);
    }
}