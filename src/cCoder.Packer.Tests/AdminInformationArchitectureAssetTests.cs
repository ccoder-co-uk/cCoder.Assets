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

            string appManagementScript = await ReadPropertyAsync(
                path: FindAsset(
                    baseline: baseline,
                    segments:
                    [
                        "Common Cache",
                        "ContentManagement",
                        "Components",
                        "AppManagement.json",
                    ]),
                propertyName: "Script");

            foreach (string layoutName in new[] { "Default.json", "FullPage.json" })
            {
                string layout = await ReadPropertyAsync(
                    path: FindAsset(
                        baseline: baseline,
                        segments:
                        [
                            "App",
                            "Default",
                            "Layouts",
                            layoutName,
                        ]),
                    propertyName: "Html");

                Assert.DoesNotContain(
                    expectedSubstring:
                        "var iconName = iconClass.replace"
                        + "(/^k-i-/, \"\").replace(\"source-code\", \"code\");"
                        + "\n            })[0].replace",
                    actualString: layout);
            }

            foreach (string component in new[]
            {
                "CultureManagement",
                "LayoutManagement",
                "TemplateManagement",
                "ComponentManagement",
                "ResourceManagement",
                "RoleManagement",
            })
            {
                Assert.Contains(
                    expectedSubstring: $"[component[{component}]]",
                    actualString: appManagement);

                Assert.DoesNotContain(
                    expectedSubstring: $"{component}.init(",
                    actualString: appManagement);
            }

            Assert.Contains(
                expectedSubstring: "AppManagement.resizePane(pane)",
                actualString: appManagementScript);

            foreach ((string path, string domain, string type) in new[]
            {
                ("Core/Components/CultureManagement.json", "ContentManagement", "AppCulture"),
                ("ContentManagement/Components/LayoutManagement.json", "ContentManagement", "Layout"),
                ("ContentManagement/Components/TemplateManagement.json", "ContentManagement", "Template"),
                ("ContentManagement/Components/ComponentManagement.json", "ContentManagement", "Component"),
                ("ContentManagement/Components/ResourceManagement.json", "ContentManagement", "Resource"),
                ("AppSecurity/Components/RoleManagement.json", "AppSecurity", "Role"),
            })
            {
                string managerScript = await ReadPropertyAsync(
                    path: FindAsset(
                        baseline: baseline,
                        segments:
                        [
                            "Common Cache",
                            .. path.Split(separator: '/'),
                        ]),
                    propertyName: "Script");

                Assert.Contains(
                    expectedSubstring: $"[meta[{domain}/{type}]]",
                    actualString: managerScript);
            }

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

            string flowEditorScript = await ReadPropertyAsync(
                path: FindAsset(
                    baseline: baseline,
                    segments:
                    [
                        "Common Cache",
                        "Workflow",
                        "Components",
                        "FlowEditor.json",
                    ]),
                propertyName: "Script");

            Assert.Contains(
                expectedSubstring: "if (!id)",
                actualString: flowEditorScript);

            Assert.Contains(
                expectedSubstring:
                    "window.location.replace(\"/Admin/Workflows\")",
                actualString: flowEditorScript);

            await AssertPageAsync(
                baseline: baseline,
                fileName: "Admin_DocumentManagement.json",
                path: "Admin/DocumentManagement",
                component: "DocumentManagement",
                includeInSubsequentImports:
                    baseline.EndsWith(
                        value: "Default App",
                        comparisonType: StringComparison.Ordinal));

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

            string documentManagementContent = await ReadPropertyAsync(
                path: FindAsset(
                    baseline: baseline,
                    segments:
                    [
                        "Common Cache",
                        "DocumentManagement",
                        "Components",
                        "DocumentManagement.json",
                    ]),
                propertyName: "Content");

            Assert.Contains(
                expectedSubstring: "ccoder-document-management",
                actualString: documentManagementContent);

            Assert.Contains(
                expectedSubstring: "context-toolbar-hidden",
                actualString: documentManagementContent);

            Assert.Contains(
                expectedSubstring: "/icons/folder.svg",
                actualString: documentManagementScript);

            Assert.DoesNotContain(
                expectedSubstring: "DMSIcons",
                actualString: documentManagementScript);

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

            string formattingScript = await ReadPropertyAsync(
                path: FindAsset(
                    baseline: baseline,
                    segments:
                    [
                        "Common Cache",
                        "DocumentManagement",
                        "Components",
                        "DMSFormatting.json",
                    ]),
                propertyName: "Script");

            Assert.Contains(
                expectedSubstring: "type.dateAndTimeFormat",
                actualString: formattingScript);

            Assert.Contains(
                expectedSubstring: "return \"/icons/\"",
                actualString: formattingScript);

            Assert.DoesNotContain(
                expectedSubstring: "type.dateFormat",
                actualString: formattingScript);

            await AssertPageAsync(
                baseline: baseline,
                fileName: "Admin_MailManagement.json",
                path: "Admin/MailManagement",
                component: "MailManagement",
                includeInSubsequentImports:
                    baseline.EndsWith(
                        value: "Default App",
                        comparisonType: StringComparison.Ordinal));

            await AssertPageAsync(
                baseline: baseline,
                fileName: "Admin_LogStream.json",
                path: "Admin/LogStream",
                component: "LogStream",
                includeInSubsequentImports:
                    baseline.EndsWith(
                        value: "Default App",
                        comparisonType: StringComparison.Ordinal));

            Assert.True(
                condition: File.Exists(
                    path: FindAsset(
                        baseline: baseline,
                        segments:
                        [
                            "Common Cache",
                            "Core",
                            "Components",
                            "LogStream.json",
                        ])));

            Assert.Empty(
                collection: Directory.GetFiles(
                    path: Path.Combine(
                        path1: baseline,
                        path2: "App"),
                    searchPattern: "LogStream.json",
                    searchOption: SearchOption.AllDirectories));

            await AssertPageAsync(
                baseline: baseline,
                fileName: "Admin_PlatformAdmin_FullLogStream.json",
                path: "Admin/PlatformAdmin/FullLogStream",
                component: "FullLogStream",
                includeInSubsequentImports: false);

            Assert.True(
                condition: File.Exists(
                    path: FindAsset(
                        baseline: baseline,
                        segments:
                        [
                            "App",
                            "Logging",
                            "Components",
                            "FullLogStream.json",
                        ])));

            await AssertPageAsync(
                baseline: baseline,
                fileName: "Admin_Workflows.json",
                path: "Admin/Workflows",
                component: "WorkflowAdmin",
                includeInSubsequentImports:
                    baseline.EndsWith(
                        value: "Default App",
                        comparisonType: StringComparison.Ordinal));
        }

        string defaultApp = baselines[0];
        string ccoderApp = baselines[1];

        Assert.NotEmpty(
            collection: Directory.GetFiles(
                path: Path.Combine(
                    path1: defaultApp,
                    path2: "App"),
                searchPattern: "Admin_PlatformAdmin*.json",
                searchOption: SearchOption.AllDirectories));

        Assert.NotEmpty(
            collection: Directory.GetFiles(
                path: Path.Combine(
                    path1: ccoderApp,
                    path2: "App"),
                searchPattern: "Admin_PlatformAdmin*.json",
                searchOption: SearchOption.AllDirectories));

        foreach (string baseline in new[] { defaultApp, ccoderApp })
        {
            foreach (string component in new[]
            {
                "SSORoleManagement.json",
                "SSORolePrivManagement.json",
                "SSORoleUserManagement.json",
                "TenantManagement.json",
                "TenantAppManagement.json",
            })
            {
                Assert.True(
                    condition: File.Exists(
                        path: FindAsset(
                            baseline: baseline,
                            segments:
                            [
                                "App",
                                "Security",
                                "Components",
                                component,
                            ])));
            }
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
        string component,
        bool includeInSubsequentImports = true)
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

        Assert.Equal(
            expected: includeInSubsequentImports,
            actual: page.RootElement
                .GetProperty(propertyName: "IncludeInSubSequentImports")
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
        string[] segments) =>
        Path.Combine(
            paths: Enumerable.Prepend(
                source: segments,
                element: baseline)
            .ToArray());

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