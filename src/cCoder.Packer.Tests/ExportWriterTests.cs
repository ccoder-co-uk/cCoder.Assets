using System.Text.Json;
using cCoder.Packer;

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
                "Components",
                "DetailedNav",
                document.RootElement.Clone()),
        ]);

        string expected = Path.Combine(
            directory,
            "Common Cache",
            "Components",
            "DetailedNav.json");

        Assert.Single(files);
        Assert.Equal(expected, files[0]);
        Assert.True(File.Exists(expected));
    }

    [Fact]
    public async Task ShouldKeepResourceTranslationsInOneNamedFile()
    {
        using JsonDocument english = JsonDocument.Parse(
            """{"Name":"save","Culture":"en-GB","DisplayName":"Save"}""");
        using JsonDocument french = JsonDocument.Parse(
            """{"Name":"save","Culture":"fr-FR","DisplayName":"Enregistrer"}""");

        ExportWriter writer = new(directory);
        await writer.WriteAsync(
        [
            new ExportRecord(
                "ContentManagement",
                "Resources",
                "save",
                english.RootElement.Clone()),
            new ExportRecord(
                "ContentManagement",
                "Resources",
                "save",
                french.RootElement.Clone()),
        ]);

        string file = Path.Combine(
            directory,
            "ContentManagement",
            "Resources",
            "save.json");

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
