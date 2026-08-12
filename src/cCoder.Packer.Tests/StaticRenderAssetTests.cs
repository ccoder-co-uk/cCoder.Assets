// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;

namespace cCoder.Packer.Tests;

public sealed partial class StaticRenderAssetTests
{
    [Fact]
    public async Task LayoutsShouldReferenceStaticFrameworkAndStyles()
    {
        // Given
        string dataDirectory = FindDataDirectory();

        string[] layouts = Directory.GetFiles(
                path: dataDirectory,
                searchPattern: "*.json",
                searchOption: SearchOption.AllDirectories)
            .Where(predicate: path => path.Contains(
                value: $"{Path.DirectorySeparatorChar}Layouts{Path.DirectorySeparatorChar}",
                comparisonType: StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // When
        Assert.NotEmpty(collection: layouts);

        // Then
        foreach (string layoutPath in layouts)
        {
            using JsonDocument layout = JsonDocument.Parse(
                json: await File.ReadAllTextAsync(path: layoutPath));

            string header = layout.RootElement
                .GetProperty(propertyName: "HeaderHtml")
                .GetString()
                ?? string.Empty;

            string html = layout.RootElement
                .GetProperty(propertyName: "Html")
                .GetString()
                ?? string.Empty;

            Assert.Contains(
                expectedSubstring: "href=\"/everything.min.css\"",
                actualString: header);

            Assert.Contains(
                expectedSubstring: "src=\"/framework.min.js\"",
                actualString: html);

            Assert.DoesNotContain(
                expectedSubstring: "[script[Dependency.",
                actualString: html);

            Assert.DoesNotContain(
                expectedSubstring: "[script[Widgets.",
                actualString: html);

            Assert.DoesNotContain(
                expectedSubstring: "[style[Dependency.",
                actualString: header);
        }
    }

    [Fact]
    public void CommonCacheShouldNotRetainBundledStaticSources()
    {
        // Given
        string dataDirectory = FindDataDirectory();

        string[] commonCacheAssets = Directory.GetFiles(
                path: dataDirectory,
                searchPattern: "*.json",
                searchOption: SearchOption.AllDirectories)
            .Where(predicate: path => path.Contains(
                value: $"{Path.DirectorySeparatorChar}Common Cache{Path.DirectorySeparatorChar}",
                comparisonType: StringComparison.OrdinalIgnoreCase))
            .Where(predicate: path => path.Contains(
                    value: $"{Path.DirectorySeparatorChar}Scripts{Path.DirectorySeparatorChar}",
                    comparisonType: StringComparison.OrdinalIgnoreCase)
                || path.Contains(
                    value: $"{Path.DirectorySeparatorChar}Styles{Path.DirectorySeparatorChar}",
                    comparisonType: StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // When
        string[] staticAssets = commonCacheAssets
            .Where(predicate: IsBundledStaticAsset)
            .ToArray();

        // Then
        Assert.Empty(collection: staticAssets);
    }

    private static bool IsBundledStaticAsset(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path: path);

        return name == "Background"
            || name.StartsWith(value: "Bundle.Monaco", comparisonType: StringComparison.Ordinal)
            || name.StartsWith(value: "Core.", comparisonType: StringComparison.Ordinal)
            || name.StartsWith(value: "Dependency.", comparisonType: StringComparison.Ordinal)
            || name.StartsWith(value: "Monaco.", comparisonType: StringComparison.Ordinal)
            || name.StartsWith(value: "Widgets.", comparisonType: StringComparison.Ordinal)
            || name.StartsWith(value: "Workflow.", comparisonType: StringComparison.Ordinal);
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