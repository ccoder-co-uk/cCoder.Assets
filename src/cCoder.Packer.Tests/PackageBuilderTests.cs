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
    public async Task ShouldBuildOnePackageRecursivelyFromOneFolder()
    {
        // Given
        string root = Path.Combine(
            path1: Path.GetTempPath(),
            path2: $"ccoder-single-package-{Guid.NewGuid():N}");

        string sourcePath = Path.Combine(
            paths: [root, "Data", "Common Cache", "Common"]);

        string componentsPath = Path.Combine(
            path1: sourcePath,
            path2: "Components");

        Directory.CreateDirectory(path: componentsPath);

        await File.WriteAllTextAsync(
            path: Path.Combine(path1: componentsPath, path2: "Nav.json"),
            contents:
                """
                {
                  "Name": "Nav",
                  "IncludeInSubSequentImports": true
                }
                """);

        string destinationPath = Path.Combine(
            paths: [root, "Packages", "Common Cache", "Common.json"]);

        PackageBuilderProcessingService service = new();

        // When
        string result = await service.BuildPackageAsync(
            sourcePath: sourcePath,
            destinationPath: destinationPath,
            packageName: "Common Common Cache",
            category: "Common");

        // Then
        Assert.Equal(expected: destinationPath, actual: result);

        using JsonDocument package = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: destinationPath));

        Assert.Equal(
            expected: "Common Common Cache",
            actual: package.RootElement
                .GetProperty(propertyName: "Name")
                .GetString());

        Assert.Equal(
            expected: "Common",
            actual: package.RootElement
                .GetProperty(propertyName: "Category")
                .GetString());

        JsonElement item = Assert.Single(
            collection: package.RootElement
                .GetProperty(propertyName: "Items")
                .EnumerateArray());

        Assert.Equal(
            expected: "ContentManagement/Component",
            actual: item
                .GetProperty(propertyName: "Type")
                .GetString());

        Assert.DoesNotContain(
            expectedSubstring: "IncludeInSubSequentImports",
            actualString: item
                .GetProperty(propertyName: "Data")
                .GetString());

        Directory.Delete(path: root, recursive: true);
    }

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
                  "IncludeInSubSequentImports": false
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
                  "IncludeInSubSequentImports": false
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
                  "IncludeInSubSequentImports": false
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
                    "IncludeInSubSequentImports": false
                  }
                ]
                """);

        PackageBuilderProcessingService service = new();

        // When
        IReadOnlyList<string> files = await service.BuildPackagesAsync(
            dataPath: dataPath,
            packagesPath: packagesPath);

        // Then
        Assert.Equal(expected: 6, actual: files.Count);

        Assert.False(condition: File.Exists(path: stalePackageFile));

        Assert.Equal(
            expected:
            [
                "App",
                "Baseline New App",
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

        string firstAppFile = Path.Combine(
            paths:
            [
                packagesPath,
                "First Time Setup",
                "first-app.json",
            ]);

        Assert.True(condition: File.Exists(path: commonCacheBaselineFile));
        Assert.True(condition: File.Exists(path: firstAppFile));

        using JsonDocument firstAppPackage = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: firstAppFile));

        JsonElement[] firstAppItems =
        [
            .. firstAppPackage.RootElement
                .GetProperty(propertyName: "Items")
                .EnumerateArray(),
        ];

        Assert.Equal(expected: 5, actual: firstAppItems.Length);

        Assert.Contains(
            collection: firstAppItems,
            filter: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "ContentManagement/Page");

        Assert.Contains(
            collection: firstAppItems,
            filter: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "Workflow/CalendarEvent");

        Assert.Contains(
            collection: firstAppItems,
            filter: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "ContentManagement/App");

        Assert.Contains(
            collection: firstAppItems,
            filter: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "AppSecurity/Role");

        string firstAppPageRoles = firstAppItems
            .Single(predicate: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "ContentManagement/PageRole")
            .GetProperty(propertyName: "Data")
            .GetString()!;

        Assert.Contains(
            expectedSubstring: "\"Path\": \"Admin\"",
            actualString: firstAppPageRoles);

        Assert.Contains(
            expectedSubstring: "\"Path\": \"/About\"",
            actualString: firstAppPageRoles);

        string firstAppPages = firstAppItems
            .Single(predicate: item =>
                item.GetProperty(propertyName: "Type")
                    .GetString() == "ContentManagement/Page")
            .GetProperty(propertyName: "Data")
            .GetString()!;

        Assert.Contains(
            expectedSubstring: "\"Name\": \"Home\"",
            actualString: firstAppPages);

        Assert.Contains(
            expectedSubstring: "\"Name\": \"Login\"",
            actualString: firstAppPages);

        Assert.Contains(
            expectedSubstring: "\"Name\": \"ResetPassword\"",
            actualString: firstAppPages);

        Assert.Contains(
            expectedSubstring: "\"Name\": \"About\"",
            actualString: firstAppPages);

        Assert.Contains(
            expectedSubstring: "\"Name\": \"Documentation\"",
            actualString: firstAppPages);

        Assert.Contains(
            expectedSubstring: "\"Name\": \"Tools\"",
            actualString: firstAppPages);

        string baselineNewAppFile = Path.Combine(
            paths:
            [
                packagesPath,
                "Baseline New App",
                "baseline-new-app.json",
            ]);

        Assert.True(condition: File.Exists(path: baselineNewAppFile));

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
            json: await File.ReadAllTextAsync(path: baselineNewAppFile));

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
        Assert.DoesNotContain(expectedSubstring: "\"Name\": \"About\"", actualString: regularData);
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
            expected: 5,
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
                        value: "first-app.json",
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
                    source: await File.ReadAllBytesAsync(path: firstAppFile))),
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
