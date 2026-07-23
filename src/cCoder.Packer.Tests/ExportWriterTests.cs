using System.Text.Json;
using cCoder.Packer.Brokers;
using cCoder.Packer.Models;

namespace cCoder.Packer.Tests;

public sealed class ExportWriterTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        $"ccoder-packer-{Guid.NewGuid():N}");

    [Fact]
    public async Task ShouldWriteBusinessObjectToExpectedPath()
    {
        using JsonDocument document = JsonDocument.Parse(
            """{"Name":"DetailedNav","Content":"<nav></nav>"}""");

        ExportWriter writer = new(directory);
        IReadOnlyList<string> files = await writer.WriteAsync(
        [
            new ExportRecord(
                "Common Cache",
                Path.Combine("Nav", "Components"),
                "DetailedNav",
                document.RootElement.Clone()),
        ]);

        string expected = Path.Combine(
            directory,
            "Common Cache",
            "Nav",
            "Components",
            "DetailedNav.json");

        Assert.Single(files);
        Assert.Equal(expected, files[0]);
        Assert.True(File.Exists(expected));
    }

    [Fact]
    public async Task ShouldGroupResourcesByKeyAndCulture()
    {
        using JsonDocument save = JsonDocument.Parse(
            """{"Name":"save","Culture":"en-GB","DisplayName":"Save"}""");
        using JsonDocument cancel = JsonDocument.Parse(
            """{"Name":"cancel","Culture":"en-GB","DisplayName":"Cancel"}""");

        ExportWriter writer = new(directory);
        await writer.WriteAsync(
        [
            new ExportRecord(
                "ContentManagement",
                Path.Combine("CMS", "Resources"),
                "en-GB",
                save.RootElement.Clone(),
                CombineValues: true),
            new ExportRecord(
                "ContentManagement",
                Path.Combine("CMS", "Resources"),
                "en-GB",
                cancel.RootElement.Clone(),
                CombineValues: true),
        ]);

        string file = Path.Combine(
            directory,
            "ContentManagement",
            "CMS",
            "Resources",
            "en-GB.json");

        using JsonDocument result = JsonDocument.Parse(
            await File.ReadAllTextAsync(file));

        Assert.Equal(2, result.RootElement.GetArrayLength());
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
