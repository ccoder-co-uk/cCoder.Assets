// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class PackageTransferContractTests
{
    [Fact]
    public void CommonCacheManagement_ShouldExportOnlyLatestAndImportPackages()
    {
        // Given
        string script = ReadComponentScript(
            scope: "App",
            componentName: "CommonCacheManagement");

        // When
        bool hasPackageTransferControls = script.Contains(
            value: "name='export'",
            comparisonType: StringComparison.Ordinal)
            && script.Contains(
                value: "name='import'",
                comparisonType: StringComparison.Ordinal);

        // Then
        Assert.True(condition: hasPackageTransferControls);
        Assert.Contains(
            expectedSubstring: "ContentManagement/CommonObject/Latest()?type=",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
        Assert.Contains(
            expectedSubstring: "&$top=",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
        Assert.Contains(
            expectedSubstring: "&$skip=",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
        Assert.Contains(
            expectedSubstring: "Packaging/Package/Import",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
        Assert.DoesNotContain(
            expectedSubstring: "ContentManagement/CommonObject?$filter=",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
    }

    [Fact]
    public void AppManagement_ShouldExportAndImportPackagesForSelectedApp()
    {
        // Given
        string script = ReadComponentScript(
            scope: "Common Cache",
            componentName: "AppManagement");

        // When
        bool hasPackageTransferControls = script.Contains(
            value: "name=\"appExport\"",
            comparisonType: StringComparison.Ordinal)
            && script.Contains(
                value: "name=\"appImport\"",
                comparisonType: StringComparison.Ordinal);

        // Then
        Assert.True(condition: hasPackageTransferControls);
        Assert.Contains(
            expectedSubstring: "Core/Package/Export?appId=",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
        Assert.Contains(
            expectedSubstring: "Packaging/Package/Import?appId=",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
    }

    private static string ReadComponentScript(string scope, string componentName)
    {
        string componentPath = Path.Combine(paths:
        [
            FindRepositoryRoot(),
            "Data",
            "Default App",
            scope,
            "ContentManagement",
            "Components",
            $"{componentName}.json"
        ]);

        using JsonDocument component = JsonDocument.Parse(
            json: File.ReadAllText(path: componentPath));

        return component.RootElement
            .GetProperty(propertyName: "Script")
            .GetString()!;
    }

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