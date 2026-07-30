// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;

namespace cCoder.Packer.Tests;

public sealed partial class AdminInformationArchitectureAssetTests
{
    [Fact]
    public async Task BaselinesShouldDefineCurrentAdminInformationArchitecture()
    {
        // Given
        string dataDirectory = FindDataDirectory();

        string[] baselines =
        [
            Path.Combine(path1: dataDirectory, path2: "Default App"),
            Path.Combine(path1: dataDirectory, path2: "ccoder.co.uk"),
        ];

        // When
        foreach (string baseline in baselines)
        {
            string appManagement = await ReadPropertyAsync(
                path: FindAsset(
                    baseline: baseline,
                    segments:
                    [
                        "Common Cache",
                        "ContentManagement",
                        "Components",
                        "AppManagement.json",
                    ]),
                propertyName: "Content");

            // Then
            Assert.Contains(
                expectedSubstring: "[component[PageManagement]]",
                actualString: appManagement);

            Assert.DoesNotContain(
                expectedSubstring: "[component[Scheduling]]",
                actualString: appManagement);

            Assert.DoesNotContain(
                expectedSubstring: "[component[WorkflowScheduling]]",
                actualString: appManagement);

            Assert.DoesNotContain(
                expectedSubstring: "[component[MailManagement]]",
                actualString: appManagement);

            Assert.DoesNotContain(
                expectedSubstring: "[component[LogStream]]",
                actualString: appManagement);

            string workflowAdmin = await ReadPropertyAsync(
                path: FindAsset(
                    baseline: baseline,
                    segments:
                    [
                        "Common Cache",
                        "Workflow",
                        "Components",
                        "WorkflowAdmin.json",
                    ]),
                propertyName: "Content");

            Assert.Contains(
                expectedSubstring: "[component[WorkflowList]]",
                actualString: workflowAdmin);

            Assert.Contains(
                expectedSubstring: "[component[WorkflowScheduling]]",
                actualString: workflowAdmin);

            await AssertPageAsync(
                baseline: baseline,
                fileName: "Admin_DocumentManagement.json",
                path: "Admin/DocumentManagement",
                component: "DocumentManagement");

            string documentManagementScript = await ReadPropertyAsync(
                path: FindAsset(
                    baseline: baseline,
                    segments:
                    [
                        "Common Cache",
                        "DocumentManagement",
                        "Components",
                        "DocumentManagement.json",
                    ]),
                propertyName: "Script");

            Assert.DoesNotContain(
                expectedSubstring: "tokens truncated",
                actualString: documentManagementScript);

            Assert.Contains(
                expectedSubstring: "refreshTreeAfterFolderChange",
                actualString: documentManagementScript);

            Assert.Contains(
                expectedSubstring: "bindFolderDragDrop",
                actualString: documentManagementScript);

            Assert.Contains(
                expectedSubstring: "bindFileDropToTree",
                actualString: documentManagementScript);

            Assert.DoesNotContain(
                expectedSubstring: "FolderManagement2",
                actualString: documentManagementScript);

            Assert.DoesNotContain(
                expectedSubstring: "Core/",
                actualString: documentManagementScript);

            AssertPromotedDocumentManagementComponent(
                baseline: baseline,
                componentName: "FolderManagement");

            AssertPromotedDocumentManagementComponent(
                baseline: baseline,
                componentName: "FileVersionGrid");

            await AssertPageAsync(
                baseline: baseline,
                fileName: "Admin_MailManagement.json",
                path: "Admin/MailManagement",
                component: "MailManagement");

            await AssertPageAsync(
                baseline: baseline,
                fileName: "Admin_PlatformAdmin_FullLogStream.json",
                path: "Admin/PlatformAdmin/FullLogStream",
                component: "LogStream");

            await AssertPageAsync(
                baseline: baseline,
                fileName: "Admin_Workflows.json",
                path: "Admin/Workflows",
                component: "WorkflowAdmin");
        }
    }

    private static void AssertPromotedDocumentManagementComponent(
        string baseline,
        string componentName)
    {
        string componentsDirectory = FindAsset(
            baseline: baseline,
            segments:
            [
                "Common Cache",
                "DocumentManagement",
                "Components",
            ]);

        Assert.True(
            condition: File.Exists(
                path: Path.Combine(
                    path1: componentsDirectory,
                    path2: $"{componentName}.json")));

        Assert.False(
            condition: File.Exists(
                path: Path.Combine(
                    path1: componentsDirectory,
                    path2: $"{componentName}2.json")));
    }

    private static async Task AssertPageAsync(
        string baseline,
        string fileName,
        string path,
        string component)
    {
        string pagePath = Directory.GetFiles(
                path: baseline,
                searchPattern: fileName,
                searchOption: SearchOption.AllDirectories)
            .Single();

        using JsonDocument page = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: pagePath));

        Assert.Equal(
            expected: path,
            actual: page.RootElement
                .GetProperty(propertyName: "Path")
                .GetString());

        Assert.True(
            condition: page.RootElement
                .GetProperty(propertyName: "ShowOnMenus")
                .GetBoolean());

        string html = page.RootElement
            .GetProperty(propertyName: "Contents")[0]
            .GetProperty(propertyName: "Html")
            .GetString()
            ?? string.Empty;

        Assert.Equal(
            expected: $"[component[{component}]]",
            actual: html);
    }

    private static string FindAsset(
        string baseline,
        string[] segments)
    {
        return Path.Combine(
            paths: Enumerable.Prepend(
                source: segments,
                element: baseline).ToArray());
    }

    private static async Task<string> ReadPropertyAsync(
        string path,
        string propertyName)
    {
        using JsonDocument document = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: path));

        return document.RootElement
            .GetProperty(propertyName: propertyName)
            .GetString()
            ?? string.Empty;
    }

    private static string FindDataDirectory()
    {
        foreach (string start in new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
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