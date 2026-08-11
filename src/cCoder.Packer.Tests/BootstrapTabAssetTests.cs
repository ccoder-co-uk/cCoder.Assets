// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;

namespace cCoder.Packer.Tests;

public sealed partial class BootstrapTabAssetTests
{
    [Fact]
    public void FirstTimeSetupComponentsShouldNotInitializeKendoTabs()
    {
        // Given
        string componentDirectory = FindComponentDirectory();

        // When
        string[] legacyComponents = Directory.GetFiles(
                path: componentDirectory,
                searchPattern: "*.json",
                searchOption: SearchOption.AllDirectories)
            .Where(predicate: ContainsKendoTabInitialization)
            .Select(selector: ReadComponentName)
            .Order(comparer: StringComparer.Ordinal)
            .ToArray();

        // Then
        Assert.True(
            condition: legacyComponents.Length == 0,
            userMessage: "First-time-setup components still initialize legacy "
                + "Kendo tabs: "
                + string.Join(separator: ", ", value: legacyComponents));
    }

    [Fact]
    public void FirstTimeSetupPackageComponentsShouldNotInitializeKendoTabs()
    {
        // Given
        string packagePath = FindPackagePath();

        using JsonDocument package = JsonDocument.Parse(
            json: File.ReadAllText(path: packagePath));

        // When
        string[] legacyComponents = package.RootElement
            .GetProperty(propertyName: "Items")
            .EnumerateArray()
            .Where(predicate: item => string.Equals(
                a: item.GetProperty(propertyName: "Type")
                    .GetString(),
                b: "ContentManagement/Component",
                comparisonType: StringComparison.Ordinal))
            .SelectMany(selector: ReadPackageComponents)
            .Where(predicate: component => component
                .GetProperty(propertyName: "Script")
                .GetString()?
                .Contains(
                    value: ".kendoTabStrip(",
                    comparisonType: StringComparison.Ordinal) is true)
            .Select(selector: component => component
                .GetProperty(propertyName: "Name")
                .GetString() ?? string.Empty)
            .Order(comparer: StringComparer.Ordinal)
            .ToArray();

        // Then
        Assert.True(
            condition: legacyComponents.Length == 0,
            userMessage: "The generated first-time-setup package still contains "
                + "legacy Kendo tab initializers: "
                + string.Join(separator: ", ", value: legacyComponents));
    }

    private static bool ContainsKendoTabInitialization(string path)
    {
        if (!Path.GetDirectoryName(path: path)!
            .Split(separator: Path.DirectorySeparatorChar)
            .Contains(
                value: "Components",
                comparer: StringComparer.Ordinal))
        {
            return false;
        }

        using JsonDocument document = JsonDocument.Parse(
            json: File.ReadAllText(path: path));

        JsonElement root = document.RootElement;

        string script = root.TryGetProperty(
            propertyName: "Script",
            value: out JsonElement scriptElement)
                ? scriptElement.GetString() ?? string.Empty
                : string.Empty;

        return script.Contains(
            value: ".kendoTabStrip(",
            comparisonType: StringComparison.Ordinal);
    }

    private static string ReadComponentName(string path)
    {
        using JsonDocument document = JsonDocument.Parse(
            json: File.ReadAllText(path: path));

        return document.RootElement
            .GetProperty(propertyName: "Name")
            .GetString() ?? Path.GetFileNameWithoutExtension(path: path);
    }

    private static string FindComponentDirectory()
    {
        DirectoryInfo? directory = new(path: AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                path1: directory.FullName,
                path2: "Data",
                path3: "Default App",
                path4: "Common Cache");

            if (Directory.Exists(path: candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            message: "The first-time-setup Common Cache directory "
                + "could not be located.");
    }

    private static IEnumerable<JsonElement> ReadPackageComponents(
        JsonElement item)
    {
        using JsonDocument components = JsonDocument.Parse(
            json: item.GetProperty(propertyName: "Data")
                .GetString() ?? "[]");

        return components.RootElement
            .EnumerateArray()
            .Select(selector: component => component.Clone())
            .ToArray();
    }

    private static string FindPackagePath()
    {
        DirectoryInfo? directory = new(path: AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                path1: directory.FullName,
                path2: "Packages",
                path3: "First Time Setup",
                path4: "common-cache.json");

            if (File.Exists(path: candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            message: "The generated first-time-setup Common Cache package "
                + "could not be located.");
    }
}