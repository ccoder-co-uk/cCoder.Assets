// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;

namespace cCoder.Packer.Tests;

public sealed partial class WorkflowNestedComponentCspAssetTests
{
    [Theory]
    [InlineData("WorkflowManagement.json")]
    [InlineData("WorkflowList.json")]
    [InlineData("Scheduling.json")]
    [InlineData("FlowInstanceManagement.json")]
    public void NestedWorkflowComponents_ShouldCloneComponentMarkupWithoutScriptElements(
        string fileName)
    {
        // Given
        string repository = FindRepository();

        string path = Path.Combine(
            paths:
            [
                repository,
                "Data",
                "Default App",
                "Common Cache",
                "Workflow",
                "Components",
                fileName,
            ]);

        // When
        using JsonDocument document = JsonDocument.Parse(
            json: File.ReadAllText(path: path));

        string script = document.RootElement
            .GetProperty(propertyName: "Script")
            .GetString()
            ?? string.Empty;

        // Then
        Assert.DoesNotContain(
            expectedSubstring: "flowInstanceManagementComponent]\").first().html()",
            actualString: script,
            comparisonType: StringComparison.Ordinal);

        Assert.DoesNotContain(
            expectedSubstring: "flowInstanceDetailsComponent]\").first().html()",
            actualString: script,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: ".clone(false, false)",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
    }

    private static string FindRepository()
    {
        DirectoryInfo? directory = new(path: AppContext.BaseDirectory);

        while (directory is not null
            && !Directory.Exists(
                path: Path.Combine(
                    path1: directory.FullName,
                    path2: "Data")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                message: "Could not locate cCoder.Assets repository root.");
    }
}