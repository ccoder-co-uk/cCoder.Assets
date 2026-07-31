// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;

namespace cCoder.Packer.Tests;

public sealed partial class WorkflowThemeAssetTests
{
    [Fact]
    public async Task ApplicationAssetsShouldDefineUsableDefaultWorkflowTheme()
    {
        // Given
        string dataDirectory = FindDataDirectory();

        string[] appAssets = Enumerable.ToArray(
            source: Enumerable.Where(
                source: Directory.GetFiles(
                    path: dataDirectory,
                    searchPattern: "*.json",
                    searchOption: SearchOption.AllDirectories),
                predicate: path => path.Contains(
                    value: $"{Path.DirectorySeparatorChar}Apps{Path.DirectorySeparatorChar}",
                    comparisonType: StringComparison.OrdinalIgnoreCase)));

        // When
        Assert.NotEmpty(collection: appAssets);

        // Then
        foreach (string appAsset in appAssets)
        {
            using JsonDocument app = JsonDocument.Parse(
                json: await File.ReadAllTextAsync(path: appAsset));

            string defaultTheme = app.RootElement
                .GetProperty(propertyName: "DefaultTheme")
                .GetString()
                ?? throw new InvalidOperationException(
                    message: $"{appAsset} has no default theme.");

            string configJson = app.RootElement
                .GetProperty(propertyName: "ConfigJson")
                .GetString()
                ?? throw new InvalidOperationException(
                    message: $"{appAsset} has no application configuration.");

            using JsonDocument config = JsonDocument.Parse(json: configJson);

            JsonElement theme = config.RootElement
                .GetProperty(propertyName: "Themes")
                .GetProperty(propertyName: defaultTheme);

            JsonElement colours = theme.GetProperty(propertyName: "colours");

            Assert.False(
                condition: string.IsNullOrWhiteSpace(
                    value: colours
                        .GetProperty(propertyName: "primary")
                        .GetString()),
                userMessage: $"{appAsset} has no primary workflow colour.");

            Assert.False(
                condition: string.IsNullOrWhiteSpace(
                    value: colours
                        .GetProperty(propertyName: "secondary")
                        .GetString()),
                userMessage: $"{appAsset} has no secondary workflow colour.");
        }
    }

    private static string FindDataDirectory()
    {
        foreach (string start in new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        })
        {
            DirectoryInfo? directory = new(path: start);

            while (directory is not null)
            {
                string candidate = Path.Combine(
                    path1: directory.FullName,
                    path2: "Data");

                if (Directory.Exists(path: candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            message: "The cCoder.Assets Data directory could not be located.");
    }
}