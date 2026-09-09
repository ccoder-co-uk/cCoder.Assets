// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;

namespace cCoder.Packer.Tests;

public sealed partial class ArchitectureVisualiserAssetTests
{
    [Fact]
    public void ArchitectureVisualiser_WhenPackaged_UsesASeparateCommonCacheScript()
    {
        // Given
        string commonCache = FindCommonCacheDirectory();

        using JsonDocument component = Read(path: Path.Combine(
            path1: commonCache,
            path2: "Common",
            path3: "Components",
            path4: "ArchitectureVisualiser.json"));

        using JsonDocument script = Read(path: Path.Combine(
            path1: commonCache,
            path2: "Common",
            path3: "Scripts",
            path4: "ArchitectureVisualiser.json"));

        // When
        JsonElement componentRoot = component.RootElement;
        JsonElement scriptRoot = script.RootElement;

        // Then
        Assert.Equal(
            expected: "Common",
            actual: componentRoot.GetProperty(propertyName: "Key")
                .GetString());

        Assert.Equal(
            expected: "Common",
            actual: componentRoot.GetProperty(propertyName: "ResourceKey")
                .GetString());

        Assert.Equal(
            expected: "[script[ArchitectureVisualiser]]",
            actual: componentRoot.GetProperty(propertyName: "Script")
                .GetString());

        Assert.Contains(
            expectedSubstring: ".av-column-head",
            actualString: componentRoot.GetProperty(propertyName: "Content")
                .GetString(),
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: ".av-node.is-invalid",
            actualString: componentRoot.GetProperty(propertyName: "Content")
                .GetString(),
            comparisonType: StringComparison.Ordinal);

        Assert.Equal(
            expected: "Common",
            actual: scriptRoot.GetProperty(propertyName: "Key")
                .GetString());

        Assert.Contains(
            expectedSubstring: "buildDependencyForest",
            actualString: scriptRoot.GetProperty(propertyName: "Content")
                .GetString(),
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "layoutDependencyForest",
            actualString: scriptRoot.GetProperty(propertyName: "Content")
                .GetString(),
            comparisonType: StringComparison.Ordinal);
    }

    [Fact]
    public void ArchitectureVisualiser_WhenBuildingTrees_ExcludesModelsAndDefinesPositionedConnectors()
    {
        // Given
        string commonCache = FindCommonCacheDirectory();

        using JsonDocument script = Read(path: Path.Combine(
            path1: commonCache,
            path2: "Common",
            path3: "Scripts",
            path4: "ArchitectureVisualiser.json"));

        // When
        string content = script.RootElement
            .GetProperty(propertyName: "Content")
            .GetString()!;

        // Then
        Assert.Contains(
            expectedSubstring: "toLowerCase() !== \"model\"",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "!dependedUpon.has(name)",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "columnNames = [\"Exposure\", \"Aggregation\", \"Coordination\", \"Orchestration\", \"Processing\", \"Foundation\", \"Broker\", \"Dependency\"]",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "getLayerColumn(node.item)",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "(firstCentre + lastCentre) / 2",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "treeBottom + treeGap",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "layout.edges.map",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "definition.AnalysisItems || []",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "properties > 0",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "item.Interfaces || []",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "nodesByColumn",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "externalNames",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "External dependency",
            actualString: content,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "other.x === parent.x",
            actualString: content,
            comparisonType: StringComparison.Ordinal);
    }

    private static JsonDocument Read(string path) =>
        JsonDocument.Parse(json: File.ReadAllText(path: path));

    private static string FindCommonCacheDirectory()
    {
        DirectoryInfo? directory = new(path: AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                path1: directory.FullName,
                path2: "Data",
                path3: "Default App",
                path4: "Common Cache");

            if (Directory.Exists(path: candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            message: "The first-time-setup Common Cache directory could not be located.");
    }
}