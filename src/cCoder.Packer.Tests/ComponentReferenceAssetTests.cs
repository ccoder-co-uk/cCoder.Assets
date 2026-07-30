// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.RegularExpressions;

namespace cCoder.Packer.Tests;

public sealed partial class ComponentReferenceAssetTests
{
    [Fact]
    public void PageComponentReferencesShouldUseCanonicalComponentNames()
    {
        // Given
        string dataDirectory = FindDataDirectory();

        // When
        foreach (string appDirectory in Directory.GetDirectories(path: dataDirectory))
        {
            string[] assetPaths = Directory.GetFiles(
                path: appDirectory,
                searchPattern: "*.json",
                searchOption: SearchOption.AllDirectories);

            HashSet<string> componentNames = new(
                comparer: StringComparer.Ordinal);

            foreach (string componentPath in assetPaths)
            {
                if (IsInDirectory(
                    path: componentPath,
                    directoryName: "Components")
                    && ReadName(path: componentPath) is string name)
                {
                    componentNames.Add(item: name);
                }
            }

            foreach (string pagePath in assetPaths)
            {
                if (!IsInDirectory(
                    path: pagePath,
                    directoryName: "Pages"))
                {
                    continue;
                }

                // Then
                AssertCanonicalReferences(
                    pagePath: pagePath,
                    componentNames: componentNames);
            }
        }
    }

    private static void AssertCanonicalReferences(
        string pagePath,
        HashSet<string> componentNames)
    {
        using JsonDocument page = JsonDocument.Parse(
            json: File.ReadAllText(path: pagePath));

        foreach (JsonElement content in page.RootElement
            .GetProperty(propertyName: "Contents")
            .EnumerateArray())
        {
            string html = content
                .GetProperty(propertyName: "Html")
                .GetString() ?? string.Empty;

            foreach (Match match in Regex.Matches(
                input: html,
                pattern: @"\[component\[([^\]]+)\]\]"))
            {
                string reference = match.Groups[groupnum: 1].Value;

                Assert.True(
                    condition: componentNames.Contains(item: reference),
                    userMessage:
                        $"Page '{pagePath}' references component '{reference}' "
                        + "without matching its canonical name and casing.");
            }
        }
    }

    private static string? ReadName(
        string path)
    {
        using JsonDocument document = JsonDocument.Parse(
            json: File.ReadAllText(path: path));

        return document.RootElement.TryGetProperty(
            propertyName: "Name",
            value: out JsonElement name)
                ? name.GetString()
                : null;
    }

    private static bool IsInDirectory(
        string path,
        string directoryName) =>
        Path.GetDirectoryName(path: path)?
            .Split(separator: Path.DirectorySeparatorChar)
            .Contains(value: directoryName, comparer: StringComparer.Ordinal) == true;

    private static string FindDataDirectory()
    {
        DirectoryInfo? directory = new(path: AppContext.BaseDirectory);

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

        throw new DirectoryNotFoundException(
            message: "The cCoder.Assets Data directory could not be located.");
    }
}