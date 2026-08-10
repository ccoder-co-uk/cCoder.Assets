// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests;

public sealed partial class BaselineComponentCoverageTests
{
    private static readonly Regex ComponentPlaceholder = new(
        pattern: @"\[component\[([^\]]+)\]\]",
        options: RegexOptions.Compiled);

    [Fact]
    public void EveryConsumedComponent_ShouldHaveNamedTestSuite()
    {
        // Given
        string baselineRoot = Path.Combine(
            path1: FindRepositoryRoot(),
            path2: "Data",
            path3: "Default App");

        Dictionary<string, string> components = LoadComponents(
            baselineRoot: baselineRoot);

        HashSet<string> consumedComponents = ResolveConsumedComponents(
            baselineRoot: baselineRoot,
            components: components);

        HashSet<string> testSuiteNames = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Select(selector: type => type.Name)
            .ToHashSet(comparer: StringComparer.Ordinal);

        string componentTestsRoot = Path.Combine(
            paths:
            [
                FindRepositoryRoot(),
                "src",
                "cCoder.Assets.UI.Tests",
                "Tests",
                "Components"
            ]);

        // When
        string[] missingSuites = consumedComponents
            .Where(
                predicate: componentName =>
                    !testSuiteNames.Contains(
                        item: componentName + "Tests"))
            .Order()
            .ToArray();

        string[] missingRenderingPartials = consumedComponents
            .Where(
                predicate: componentName =>
                    !Directory.EnumerateFiles(
                        path: componentTestsRoot,
                        searchPattern: componentName + "Tests.Rendering.cs",
                        searchOption: SearchOption.AllDirectories)
                    .Any())
            .Order()
            .ToArray();

        // Then
        Assert.True(
            condition: missingSuites.Length == 0,
            userMessage: "Baseline components without named test suites: "
                + string.Join(separator: ", ", value: missingSuites));

        Assert.True(
            condition: missingRenderingPartials.Length == 0,
            userMessage: "Baseline components without Rendering partials: "
                + string.Join(
                    separator: ", ",
                    value: missingRenderingPartials));
    }

    private static Dictionary<string, string> LoadComponents(
        string baselineRoot)
    {
        Dictionary<string, string> components = new(
            comparer: StringComparer.Ordinal);

        foreach (string file in Directory.EnumerateFiles(
            path: baselineRoot,
            searchPattern: "*.json",
            searchOption: SearchOption.AllDirectories))
        {
            using JsonDocument document = JsonDocument.Parse(
                json: File.ReadAllText(path: file));

            JsonElement root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(
                    propertyName: "Name",
                    value: out JsonElement name)
                && root.TryGetProperty(
                    propertyName: "Content",
                    value: out JsonElement content)
                && root.TryGetProperty(
                    propertyName: "Script",
                    value: out JsonElement script))
            {
                components[name.GetString()!] =
                    content.GetString() + "\n" + script.GetString();
            }
        }

        return components;
    }

    private static HashSet<string> ResolveConsumedComponents(
        string baselineRoot,
        IReadOnlyDictionary<string, string> components)
    {
        HashSet<string> consumed = new(comparer: StringComparer.Ordinal);
        Queue<string> pending = new();
        string appRoot = Path.Combine(path1: baselineRoot, path2: "App");

        foreach (string file in Directory.EnumerateFiles(
            path: appRoot,
            searchPattern: "*.json",
            searchOption: SearchOption.AllDirectories)
            .Where(
                predicate: file =>
                    file.Contains(
                        value: $"{Path.DirectorySeparatorChar}Pages{Path.DirectorySeparatorChar}",
                        comparisonType: StringComparison.OrdinalIgnoreCase)
                    || file.Contains(
                        value: $"{Path.DirectorySeparatorChar}Layouts{Path.DirectorySeparatorChar}",
                        comparisonType: StringComparison.OrdinalIgnoreCase)
                    || file.Contains(
                        value: $"{Path.DirectorySeparatorChar}Templates{Path.DirectorySeparatorChar}",
                        comparisonType: StringComparison.OrdinalIgnoreCase)))
        {
            AddPlaceholders(
                source: File.ReadAllText(path: file),
                consumed: consumed,
                pending: pending);
        }

        while (pending.TryDequeue(result: out string? componentName))
        {
            if (components.TryGetValue(
                key: componentName,
                value: out string? componentSource))
            {
                AddPlaceholders(
                    source: componentSource,
                    consumed: consumed,
                    pending: pending);
            }
        }

        return consumed;
    }

    private static void AddPlaceholders(
        string source,
        ISet<string> consumed,
        Queue<string> pending)
    {
        foreach (Match match in ComponentPlaceholder.Matches(input: source))
        {
            string componentName = match.Groups[groupnum: 1].Value;

            if (consumed.Add(item: componentName))
            {
                pending.Enqueue(item: componentName);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(
                path: Path.Combine(
                    path1: directory.FullName,
                    path2: "Data"))
                && Directory.Exists(
                    path: Path.Combine(
                        path1: directory.FullName,
                        path2: "Packages")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            message: "The cCoder.Assets repository root could not be found.");
    }
}