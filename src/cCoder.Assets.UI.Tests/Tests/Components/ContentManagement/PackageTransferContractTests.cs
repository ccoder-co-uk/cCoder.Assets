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

        // When / Then
        Assert.Contains("name='export'", script, StringComparison.Ordinal);
        Assert.Contains("name='import'", script, StringComparison.Ordinal);
        Assert.Contains(
            "ContentManagement/CommonObject/Latest()?type=",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "Packaging/Package/Import",
            script,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "ContentManagement/CommonObject?$filter=",
            script,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AppManagement_ShouldExportAndImportPackagesForSelectedApp()
    {
        // Given
        string script = ReadComponentScript(
            scope: "Common Cache",
            componentName: "AppManagement");

        // When / Then
        Assert.Contains("name=\"appExport\"", script, StringComparison.Ordinal);
        Assert.Contains("name=\"appImport\"", script, StringComparison.Ordinal);
        Assert.Contains("/Export()?$expand=Items", script, StringComparison.Ordinal);
        Assert.Contains(
            "Packaging/Package/Import?appId=",
            script,
            StringComparison.Ordinal);
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