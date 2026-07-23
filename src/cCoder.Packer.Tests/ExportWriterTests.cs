// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using cCoder.Packer.Dependencies;
using cCoder.Packer.Models.Exports;

namespace cCoder.Packer.Tests;

public sealed partial class ExportWriterTests
{
    [Fact]
    public async Task ShouldWriteBusinessObjectToExpectedPath()
    {
        // Given
        string directory = CreateTestDirectory();

        using JsonDocument document = JsonDocument.Parse(
            json: """{"Name":"DetailedNav","Content":"<nav></nav>"}""");

        ExportWriterDependency writer = new(dataPath: directory);

        ExportRecord[] records =
        [
            new ExportRecord(
                Domain: "Common Cache",
                Category: Path.Combine(path1: "Nav", path2: "Components"),
                Name: "DetailedNav",
                Value: document.RootElement.Clone()),
        ];

        // When
        IReadOnlyList<string> files = await writer.WriteAsync(
            records: records);

        // Then
        string expected = Path.Combine(
            paths:
            [
                directory,
                "Common Cache",
                "Nav",
                "Components",
                "DetailedNav.json",
            ]);

        Assert.Single(collection: files);
        Assert.Equal(expected: expected, actual: files[0]);
        Assert.True(condition: File.Exists(path: expected));
        DeleteTestDirectory(directory: directory);
    }

    [Fact]
    public async Task ShouldGroupResourcesByKeyAndCulture()
    {
        // Given
        string directory = CreateTestDirectory();

        using JsonDocument save = JsonDocument.Parse(
            json: """{"Name":"save","Culture":"en-GB","DisplayName":"Save"}""");

        using JsonDocument cancel = JsonDocument.Parse(
            json: """{"Name":"cancel","Culture":"en-GB","DisplayName":"Cancel"}""");

        ExportWriterDependency writer = new(dataPath: directory);

        ExportRecord[] records =
        [
            new ExportRecord(
                Domain: "ContentManagement",
                Category: Path.Combine(path1: "CMS", path2: "Resources"),
                Name: "en-GB",
                Value: save.RootElement.Clone(),
                CombineValues: true),
            new ExportRecord(
                Domain: "ContentManagement",
                Category: Path.Combine(path1: "CMS", path2: "Resources"),
                Name: "en-GB",
                Value: cancel.RootElement.Clone(),
                CombineValues: true),
        ];

        // When
        await writer.WriteAsync(records: records);

        // Then
        string file = Path.Combine(
            paths:
            [
                directory,
                "ContentManagement",
                "CMS",
                "Resources",
                "en-GB.json",
            ]);

        using JsonDocument result = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: file));

        Assert.Equal(
            expected: 2,
            actual: result.RootElement.GetArrayLength());

        DeleteTestDirectory(directory: directory);
    }

    private static string CreateTestDirectory() =>
        Path.Combine(
            path1: Path.GetTempPath(),
            path2: $"ccoder-packer-{Guid.NewGuid():N}");

    private static void DeleteTestDirectory(string directory)
    {
        if (Directory.Exists(path: directory))
        {
            Directory.Delete(path: directory, recursive: true);
        }
    }
}