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
                  "Path": "/About",
                  "Name": "About",
                  "PackageType": "ContentManagement/Page",
                  "IncludeInSubSequentImports": false
                }
                """);

        await File.WriteAllTextAsync(
            path: Path.Combine(path1: sourceDirectory, path2: "Login.json"),
            contents:
                """
                {
                  "Path": "Login",
                  "Name": "Login",
                  "PackageType": "ContentManagement/Page",
                  "IncludeInSubSequentImports": true
                }
                """);

        await File.WriteAllTextAsync(
            path: Path.Combine(path1: sourceDirectory, path2: "Admin.json"),
            contents:
                """
                {
                  "Path": "Admin",
                  "Name": "Admin",
                  "PackageType": "ContentManagement/Page",
                  "IncludeInSubSequentImports": true
                }
                """);

        await File.WriteAllTextAsync(
            path: Path.Combine(path1: sourceDirectory, path2: "ResetPassword.json"),
            contents:
                """
                {
                  "Path": "ResetPassword",
                  "Name": "ResetPassword",
                  "PackageType": "ContentManagement/Page",
                  "IncludeInSubSequentImports": true
                }
                """);

        await File.WriteAllTextAsync(
            path: Path.Combine(path1: sourceDirectory, path2: "Documentation.json"),
            contents:
                """
                {
                  "Path": "Documentation/Core",
                  "Name": "Documentation",
                  "PackageType": "ContentManagement/Page",
                  "IncludeInSubSequentImports": true
                }
                """);

        await File.WriteAllTextAsync(
            path: Path.Combine(path1: sourceDirectory, path2: "Tools.json"),
            contents:
                """
                {
                  "Path": "Tools/DeveloperTools",
                  "Name": "Tools",
                  "PackageType": "ContentManagement/Page",
                  "IncludeInSubSequentImports": true
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

        string pageRolesDirectory = Path.Combine(
            paths:
            [
                dataPath,
                "ccoder.co.uk",
                "Default",
                "PageRoles",
            ]);

        Directory.CreateDirectory(path: pageRolesDirectory);

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: pageRolesDirectory,
                path2: "Roles.json"),
            contents:
                """
                [
                  {
                    "Path": "Admin",
                    "Role": "Administrators",
                    "PackageType": "ContentManagement/PageRole",
                    "IncludeInSubSequentImports": true
                  },
                  {
                    "Path": "/About",
                    "Role": "Administrators",
                    "PackageType": "ContentManagement/PageRole",
                    "IncludeInSubSequentImports": true
                  }
                ]
                """);

        PackageBuilderProcessingService service = new();

        // When
        IReadOnlyList<string> files = await service.BuildPackagesAsync(
            dataPath: dataPath,
            packagesPath: packagesPath);

        // Then
        Assert.Equal(expected: 5, actual: files.Count);

        Assert.False(condition: File.Exists(path: stalePackageFile));

        Assert.Equal(
            expected:
            [
                "App",
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
                "App",
                "CMS.json",
            ]);

        Assert.True(condition: File.Exists(path: regularFile));

        string calendarEventsFile = Path.Combine(
            paths:
            [
                packagesPath,
                "App",
                "Default.json",
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

        Assert.Equal(expected: 3, actual: appBaselineItems.Length);

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

        string appBaselinePageRoles = appBaselineItems
            .Single(predicate: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "ContentManagement/PageRole")
            .GetProperty(propertyName: "Data")
            .GetString()!;

        Assert.Contains(
            expectedSubstring: "\"Path\": \"Admin\"",
            actualString: appBaselinePageRoles);

        Assert.DoesNotContain(
            expectedSubstring: "\"Path\": \"About\"",
            actualString: appBaselinePageRoles);

        string appBaselinePages = appBaselineItems
            .Single(predicate: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "ContentManagement/Page")
            .GetProperty(propertyName: "Data")
            .GetString()!;

        Assert.Contains(
            expectedSubstring: "\"Name\": \"Home\"",
            actualString: appBaselinePages);

        Assert.Contains(
            expectedSubstring: "\"Name\": \"Login\"",
            actualString: appBaselinePages);

        Assert.Contains(
            expectedSubstring: "\"Name\": \"ResetPassword\"",
            actualString: appBaselinePages);

        Assert.DoesNotContain(
            expectedSubstring: "\"Name\": \"About\"",
            actualString: appBaselinePages);

        Assert.DoesNotContain(
            expectedSubstring: "\"Name\": \"Documentation\"",
            actualString: appBaselinePages);

        Assert.DoesNotContain(
            expectedSubstring: "\"Name\": \"Tools\"",
            actualString: appBaselinePages);

        using JsonDocument regularPackage = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: regularFile));

        string regularData = regularPackage.RootElement.GetProperty(
            propertyName: "Items")
            .EnumerateArray()
            .Single(predicate: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "ContentManagement/Page")
            .GetProperty(propertyName: "Data")
            .GetString()!;

        using JsonDocument setupPackage = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: appBaselineFile));

        string setupData = setupPackage.RootElement
            .GetProperty(propertyName: "Items")
            .EnumerateArray()
            .Single(predicate: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "ContentManagement/Page")
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
            expected: 4,
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
                        value: "app-baseline.json",
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
                    source: await File.ReadAllBytesAsync(path: appBaselineFile))),
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