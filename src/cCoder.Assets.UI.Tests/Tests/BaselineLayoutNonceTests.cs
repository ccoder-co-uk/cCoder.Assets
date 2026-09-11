// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests;

public sealed partial class BaselineLayoutNonceTests
{
    [Fact]
    public void DefaultAppLayouts_ShouldNonceInlineExecutableContent()
    {
        // Given
        string layoutsPath = Path.Combine(paths:
        [
            FindRepositoryRoot(),
            "Data",
            "Default App",
            "App",
            "Default",
            "Layouts"
        ]);

        // When
        string[] layoutPaths = Directory.GetFiles(
            path: layoutsPath,
            searchPattern: "*.json");

        // Then
        foreach (string layoutPath in layoutPaths)
        {
            using JsonDocument layout = JsonDocument.Parse(
                json: File.ReadAllText(path: layoutPath));

            string content = $"{layout.RootElement
                    .GetProperty(propertyName: "HeaderHtml")
                    .GetString()}{layout.RootElement
                    .GetProperty(propertyName: "Html")
                    .GetString()}";

            Assert.DoesNotMatch(
                expectedRegex: new Regex(
                    pattern: "<(script|style)(?![^>]*nonce)[^>]*>",
                    options: RegexOptions.IgnoreCase
                        | RegexOptions.CultureInvariant),
                actualString: content);
        }
    }

    [Fact]
    public void ProfileAndCultureComponents_ShouldNonceInlineContent()
    {
        // Given
        string repositoryRoot = FindRepositoryRoot();

        string[] componentPaths =
        [
            Path.Combine(paths: [repositoryRoot, "Data", "Default App",
                "Common Cache", "AppSecurity", "Components", "UserProfile.json"]),
            Path.Combine(paths: [repositoryRoot, "Data", "Default App",
                "Common Cache", "ContentManagement", "Components", "CultureFlags.json"]),
            Path.Combine(paths: [repositoryRoot, "Data", "Default App",
                "Common Cache", "Security", "Components", "PasswordReset.json"])
        ];

        // When
        Regex unsafeInlineTag = new(
            pattern: "<(script|style)(?![^>]*nonce)[^>]*>",
            options: RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Then
        foreach (string componentPath in componentPaths)
        {
            using JsonDocument component = JsonDocument.Parse(
                json: File.ReadAllText(path: componentPath));

            string content = component.RootElement
                .GetProperty(propertyName: "Content")
                .GetString()!;

            Assert.DoesNotMatch(
                expectedRegex: unsafeInlineTag,
                actualString: content);
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(path: AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(path: Path.Combine(
                path1: directory.FullName,
                path2: "Packages",
                path3: "manifest.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            message: "The cCoder.Assets repository root was not found.");
    }
}