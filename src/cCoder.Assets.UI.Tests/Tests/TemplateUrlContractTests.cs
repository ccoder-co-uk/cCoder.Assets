// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests;

public sealed partial class TemplateUrlContractTests
{
    [Theory]
    [InlineData("Default App")]
    [InlineData("localhost")]
    [InlineData("demo.dev.localhost")]
    public void AppTemplates_ShouldUseCanonicalGeneratedUrls(string appDirectory)
    {
        // Given
        string appRoot = Path.Combine(paths:
        [
            FindRepositoryRoot(),
            "Data",
            appDirectory,
            "App"
        ]);

        string[] templatePaths = Directory
            .EnumerateFiles(
                path: appRoot,
                searchPattern: "*.json",
                searchOption: SearchOption.AllDirectories)
            .Where(predicate: path => path.Contains(
                value: $"{Path.DirectorySeparatorChar}Templates{Path.DirectorySeparatorChar}",
                comparisonType: StringComparison.Ordinal))
            .ToArray();

        // When
        string[] renderedTemplates = templatePaths
            .Select(selector: ReadRawString)
            .ToArray();

        // Then
        Assert.NotEmpty(collection: renderedTemplates);

        Assert.All(collection: renderedTemplates, action: template =>
        {
            Assert.DoesNotContain(
                expectedSubstring: "[app[root]]/",
                actualString: template,
                comparisonType: StringComparison.Ordinal);

            Assert.DoesNotContain(
                expectedSubstring: "[api[root]]/",
                actualString: template,
                comparisonType: StringComparison.Ordinal);

            Assert.DoesNotContain(
                expectedSubstring: "[api[root]]Core/App(",
                actualString: template,
                comparisonType: StringComparison.Ordinal);

            Assert.DoesNotContain(
                expectedSubstring: "/MyRegistrations?",
                actualString: template,
                comparisonType: StringComparison.Ordinal);

            Assert.All(
                collection: Regex.Matches(
                    input: template,
                    pattern: "(?:href|src|action)=\"([^\"]+)\"")
                    .Cast<Match>(),
                action: match => Assert.True(
                    condition: IsAbsoluteOrGeneratedUrl(
                        url: match.Groups[groupnum: 1].Value),
                    userMessage: $"Template URL is relative or malformed: {match.Value}"));
        });
    }

    [Theory]
    [InlineData("Default App")]
    [InlineData("localhost")]
    public void ConfirmRegistrationTemplate_ShouldTargetLoginConfirmationHandler(
        string appDirectory)
    {
        // Given
        string templatePath = Path.Combine(paths:
        [
            FindRepositoryRoot(),
            "Data",
            appDirectory,
            "App",
            "Register",
            "Templates",
            "ConfirmRegistration.json"
        ]);

        // When
        string template = ReadRawString(path: templatePath);

        // Then
        Assert.Contains(
            expectedSubstring: "href=\"[app[root]]Login?u=[model[SSOUser.Id]]&t=[model[Token]]\"",
            actualString: template,
            comparisonType: StringComparison.Ordinal);
    }

    private static string ReadRawString(string path)
    {
        using JsonDocument document = JsonDocument.Parse(
            json: File.ReadAllText(path: path));

        return document.RootElement
            .GetProperty(propertyName: "RawString")
            .GetString()!;
    }

    private static bool IsAbsoluteOrGeneratedUrl(string url)
    {
        return url.StartsWith(
            value: "[app[root]]",
            comparisonType: StringComparison.Ordinal) ||
            url.StartsWith(
                value: "[api[root]]",
                comparisonType: StringComparison.Ordinal) ||
            url.StartsWith(
                value: "https://",
                comparisonType: StringComparison.Ordinal) ||
            url.StartsWith(
                value: "http://",
                comparisonType: StringComparison.Ordinal) ||
            url.StartsWith(
                value: "/",
                comparisonType: StringComparison.Ordinal) ||
            url.StartsWith(
                value: "#",
                comparisonType: StringComparison.Ordinal) ||
            url.StartsWith(
                value: "mailto:",
                comparisonType: StringComparison.Ordinal) ||
            url.StartsWith(
                value: "data:",
                comparisonType: StringComparison.Ordinal);
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