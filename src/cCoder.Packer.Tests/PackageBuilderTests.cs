// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using System.Security.Cryptography;
using cCoder.Packer.Services.Processings.Packing;

namespace cCoder.Packer.Tests;

public sealed partial class PackageBuilderTests
{
    [Fact]
    public async Task ShouldBuildRegularAndFirstTimeSetupPackages()
    {
        // Given
        string root = Path.Combine(
            path1: Path.GetTempPath(),
            path2: $"ccoder-packages-{Guid.NewGuid():N}");

        string dataPath = Path.Combine(path1: root, path2: "Data");
        string packagesPath = Path.Combine(path1: root, path2: "Packages");

        Directory.CreateDirectory(path: packagesPath);

        string stalePackageFile = Path.Combine(
            path1: packagesPath,
            path2: "stale-package.json");

        await File.WriteAllTextAsync(
            path: stalePackageFile,
            contents: "{}");

        string sourceDirectory = Path.Combine(
            paths:
            [
                dataPath,
                "ccoder.co.uk",
                "CMS",
                "Pages",
            ]);

        Directory.CreateDirectory(path: sourceDirectory);

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: sourceDirectory,
                path2: "About.json"),
            contents:
                """
                {
                  "Path": "About",
                  "Name": "About",
                  "PackageType": "ContentManagement/Page",
                  "IncludeInSubSequentImports": false
                }
                """);

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: sourceDirectory,
                path2: "Root.json"),
            contents:
                """
                {
                  "Path": "",
                  "Name": "Home",
                  "PackageType": "ContentManagement/Page",
                  "IncludeInSubSequentImports": true
                }
                """);

        string calendarEventsDirectory = Path.Combine(
            paths:
            [
                dataPath,
                "ccoder.co.uk",
                "Default",
                "CalendarEvents",
            ]);

        Directory.CreateDirectory(path: calendarEventsDirectory);

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: calendarEventsDirectory,
                path2: "TestEvent.json"),
            contents:
                """
                {
                  "CalendarName": "TestAdminCalendar",
                  "Name": "TestEvent",
                  "IncludeInSubSequentImports": true
                }
                """);

        string rolesDirectory = Path.Combine(
            paths:
            [
                dataPath,
                "ccoder.co.uk",
                "Default",
                "Roles",
            ]);

        Directory.CreateDirectory(path: rolesDirectory);

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: rolesDirectory,
                path2: "Administrators.json"),
            contents:
                """
                {
                  "Name": "Administrators",
                  "PackageType": "AppSecurity/Role",
                  "IncludeInSubSequentImports": true
                }
                """);

        string appsDirectory = Path.Combine(
            paths:
            [
                dataPath,
                "ccoder.co.uk",
                "Default",
                "Apps",
            ]);

        Directory.CreateDirectory(path: appsDirectory);

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: appsDirectory,
                path2: "Default.json"),
            contents:
                """
                {
                  "Name": "Default",
                  "PackageType": "ContentManagement/App",
                  "IncludeInSubSequentImports": true
                }
                """);

        PackageBuilderProcessingService service = new();

        // When
        IReadOnlyList<string> files = await service.BuildPackagesAsync(
            dataPath: dataPath,
            packagesPath: packagesPath);

        // Then
        Assert.Equal(expected: 11, actual: files.Count);

        Assert.False(condition: File.Exists(path: stalePackageFile));

        Assert.Equal(
            expected:
            [
                "ccoder.co.uk",
                "Common Cache",
                "First Time Setup",
            ],
            actual: Directory
                .EnumerateDirectories(path: packagesPath)
                .Select(selector: Path.GetFileName)
                .Order(comparer: StringComparer.OrdinalIgnoreCase)
                .ToArray());

        string regularFile = Path.Combine(
            paths:
            [
                packagesPath,
                "ccoder.co.uk",
                "CMS",
                "ContentManagement_Page.json",
            ]);

        string setupFile = Path.Combine(
            paths:
            [
                packagesPath,
                "First Time Setup",
                "App",
                "CMS",
                "ContentManagement_Page.json",
            ]);

        Assert.True(condition: File.Exists(path: regularFile));
        Assert.True(condition: File.Exists(path: setupFile));

        string calendarEventsFile = Path.Combine(
            paths:
            [
                packagesPath,
                "First Time Setup",
                "App",
                "Default",
                "Workflow_CalendarEvent.json",
            ]);

        Assert.True(condition: File.Exists(path: calendarEventsFile));

        string commonCacheBaselineFile = Path.Combine(
            paths:
            [
                packagesPath,
                "First Time Setup",
                "common-cache.json",
            ]);

        string appBaselineFile = Path.Combine(
            paths:
            [
                packagesPath,
                "First Time Setup",
                "app-baseline.json",
            ]);

        Assert.True(condition: File.Exists(path: commonCacheBaselineFile));
        Assert.True(condition: File.Exists(path: appBaselineFile));

        using JsonDocument appBaselinePackage = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: appBaselineFile));

        JsonElement[] appBaselineItems =
        [
            .. appBaselinePackage.RootElement
                .GetProperty(propertyName: "Items")
                .EnumerateArray(),
        ];

        Assert.Equal(expected: 2, actual: appBaselineItems.Length);

        Assert.Contains(
            collection: appBaselineItems,
            filter: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "ContentManagement/Page");

        Assert.Contains(
            collection: appBaselineItems,
            filter: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "Workflow/CalendarEvent");

        Assert.DoesNotContain(
            collection: appBaselineItems,
            filter: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() is
                    "ContentManagement/App"
                    or "AppSecurity/Role");

        string appBaselinePages = appBaselineItems
            .Single(predicate: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "ContentManagement/Page")
            .GetProperty(propertyName: "Data")
            .GetString()!;

        Assert.Contains(
            expectedSubstring: "\"Name\": \"Home\"",
            actualString: appBaselinePages);

        Assert.DoesNotContain(
            expectedSubstring: "\"Name\": \"About\"",
            actualString: appBaselinePages);

        using JsonDocument regularPackage = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: regularFile));

        string regularData = regularPackage.RootElement.GetProperty(
            propertyName: "Items")[0]
            .GetProperty(propertyName: "Data")
            .GetString()!;

        using JsonDocument setupPackage = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: setupFile));

        string setupData = setupPackage.RootElement.GetProperty(
            propertyName: "Items")[0]
            .GetProperty(propertyName: "Data")
            .GetString()!;

        Assert.Contains(expectedSubstring: "\"Name\": \"Home\"", actualString: setupData);
        Assert.Contains(expectedSubstring: "\"Name\": \"Home\"", actualString: regularData);
        Assert.Contains(expectedSubstring: "\"Name\": \"About\"", actualString: regularData);
        Assert.DoesNotContain(expectedSubstring: "\"Name\": \"About\"", actualString: setupData);

        Assert.DoesNotContain(
            expectedSubstring: "IncludeInSubSequentImports",
            actualString: setupData);

        string manifestFile = Path.Combine(
            path1: packagesPath,
            path2: "manifest.json");

        using JsonDocument manifest = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: manifestFile));

        Assert.Equal(
            expected: 1,
            actual: manifest.RootElement
                .GetProperty(propertyName: "SchemaVersion")
                .GetInt32());

        Assert.Equal(
            expected: 10,
            actual: manifest.RootElement
                .GetProperty(propertyName: "Packages")
                .GetArrayLength());

        JsonElement setupManifestItem = manifest.RootElement
            .GetProperty(propertyName: "Packages")
            .EnumerateArray()
            .Single(predicate: item =>
            {
                string? path = item.GetProperty(
                    propertyName: "Path")
                    .GetString();

                return path?.StartsWith(
                    value: "First Time Setup/",
                    comparisonType: StringComparison.Ordinal) == true
                    && path.EndsWith(
                        value: "CMS/ContentManagement_Page.json",
                        comparisonType: StringComparison.Ordinal);
            });

        Assert.Equal(
            expected: "App",
            actual: setupManifestItem
                .GetProperty(propertyName: "Source")
                .GetString());

        Assert.Equal(
            expected: Convert.ToHexString(
                inArray: SHA256.HashData(
                    source: await File.ReadAllBytesAsync(path: setupFile))),
            actual: setupManifestItem
                .GetProperty(propertyName: "Sha256")
                .GetString());

        string firstManifest = await File.ReadAllTextAsync(
            path: manifestFile);

        await service.BuildPackagesAsync(
            dataPath: dataPath,
            packagesPath: packagesPath);

        Assert.Equal(
            expected: firstManifest,
            actual: await File.ReadAllTextAsync(path: manifestFile));

        Directory.Delete(path: root, recursive: true);
    }
}