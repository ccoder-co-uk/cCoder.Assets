// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Workflow;

public sealed partial class BusinessProcessEditorAssetTests
{
    [Theory]
    [InlineData("Default App")]
    [InlineData("localhost")]
    [InlineData("ccoder.co.uk")]
    public void WorkflowDesigner_AllCulturesShouldLoadHostedDesignerAssets(
        string baseline)
    {
        // Given
        string path = Path.Combine(paths:
        [
            FindRepositoryRoot(),
            "Data",
            baseline,
            "App",
            "Workflow",
            "Pages",
            "Admin_WorkflowDesigner.json"
        ]);

        using JsonDocument page = JsonDocument.Parse(
            json: File.ReadAllText(path: path));

        // When
        string[] contentVariants = page.RootElement
            .GetProperty(propertyName: "Contents")
            .EnumerateArray()
            .Select(selector: content => content
                .GetProperty(propertyName: "Html")
                .GetString()!)
            .ToArray();

        // Then
        foreach (string html in contentVariants)
        {
            Assert.Contains(
                expectedSubstring: "/code-editor.min.js",
                actualString: html,
                comparisonType: StringComparison.Ordinal);

            Assert.Contains(
                expectedSubstring: "/workflow.min.js",
                actualString: html,
                comparisonType: StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Documentation_ShouldNotEmbedOversizedImages()
    {
        // Given
        string path = Path.Combine(paths:
        [
            FindRepositoryRoot(),
            "Data",
            "localhost",
            "App",
            "Default",
            "Pages",
            "Documentation_Workflow_WorkflowDesigner.json"
        ]);

        using JsonDocument page = JsonDocument.Parse(
            json: File.ReadAllText(path: path));

        string html = page.RootElement
            .GetProperty(propertyName: "Contents")[0]
            .GetProperty(propertyName: "Html")
            .GetString()!;

        // When
        int largestEmbeddedImage = 0;
        MatchCollection matches = DataImageRegex().Matches(input: html);

        foreach (Match match in matches)
        {
            largestEmbeddedImage = Math.Max(
                val1: largestEmbeddedImage,
                val2: match.Groups[1].Value.Length);
        }

        // Then
        Assert.True(
            condition: largestEmbeddedImage < 250_000,
            userMessage: $"The largest embedded image is {largestEmbeddedImage:N0} characters.");

        Assert.True(
            condition: html.Length < 2_000_000,
            userMessage: $"The rendered page content is {html.Length:N0} characters.");
    }

    [Fact]
    public void Component_ShouldUseTheCoreHostedEditorAssets()
    {
        // Given
        string path = Path.Combine(paths:
        [
            FindRepositoryRoot(),
            "Data",
            "Default App",
            "Common Cache",
            "Core",
            "Components",
            "BPEditor.json"
        ]);

        using JsonDocument component = JsonDocument.Parse(
            json: File.ReadAllText(path: path));

        string content = component.RootElement
            .GetProperty(propertyName: "Content")
            .GetString()!;

        string script = component.RootElement
            .GetProperty(propertyName: "Script")
            .GetString()!;

        // When
        bool usesHostedEditorAssets =
            content.Contains(
                value: "/code-editor.min.js",
                comparisonType: StringComparison.Ordinal)
            && script.Contains(
                value: "new MonacoEditor",
                comparisonType: StringComparison.Ordinal);

        // Then
        Assert.DoesNotContain(
            expectedSubstring: "cdnjs.cloudflare.com",
            actualString: content,
            comparisonType: StringComparison.OrdinalIgnoreCase);

        Assert.True(condition: usesHostedEditorAssets);

        Assert.DoesNotContain(
            expectedSubstring: "CodeMirror.fromTextArea",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
    }

    [GeneratedRegex(
        pattern: "data:image/[^;]+;base64,([^\\\"']+)",
        options: RegexOptions.CultureInvariant)]
    private static partial Regex DataImageRegex();

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(path: AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(path: Path.Combine(paths:
            [
                directory.FullName,
                "Packages",
                "manifest.json"
            ])))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            message: "The cCoder.Assets repository root was not found.");
    }
}
