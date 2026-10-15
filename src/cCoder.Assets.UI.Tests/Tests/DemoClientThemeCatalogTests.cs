// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests;

public sealed partial class DemoClientThemeCatalogTests
{
    private static readonly string[] ExpectedClientThemes =
    [
        "Alvest",
        "Bel",
        "Bel - Invoice at Bel",
        "Celio",
        "Conforoma",
        "Credit Mutuel",
        "Demathieu Bard",
        "Documentation",
        "Forvia",
        "Grand Frais",
        "Inovie",
        "Laita",
        "MERICQ",
        "Moët Hennessy",
        "Printemps",
        "Rexel",
        "Roquette",
        "Savencia",
        "SNCF",
        "Sodexo",
        "Sodiaal-Demo",
        "Soufflet",
        "Spie Batignolles",
        "Stellantis",
        "Stellantis-old",
        "Valrhona",
    ];

    [Fact]
    public void DemoApp_ShouldExposeEveryRetainedClientTheme()
    {
        // Given
        using JsonDocument app = ReadDemoApp();

        // When

        JsonElement themes = ReadThemes(app: app);

        string[] actualClientThemes = themes
            .EnumerateObject()
            .Select(selector: property => property.Name)
            .Where(predicate: name =>
                name is not "Default" and not "Paul")
            .Order(comparer: StringComparer.Ordinal)
            .ToArray();

        // Then
        Assert.Equal(
            expected: ExpectedClientThemes.Order(
                comparer: StringComparer.Ordinal),
            actual: actualClientThemes);
    }

    [Fact]
    public void DemoApp_ShouldNotExposeDiscardedTestAppThemes()
    {
        // Given
        using JsonDocument app = ReadDemoApp();

        // When

        JsonElement themes = ReadThemes(app: app);

        // Then
        Assert.False(condition: themes.TryGetProperty(
            propertyName: "AshTest",
            value: out _));

        Assert.False(condition: themes.TryGetProperty(
            propertyName: "test2",
            value: out _));

        Assert.False(condition: themes.TryGetProperty(
            propertyName: "FunctionalityReview",
            value: out _));
    }

    [Theory]
    [InlineData("Forvia", "#0A23CA", "#81CEDD")]
    [InlineData("Valrhona", "#1A171B", "#BA934B")]
    [InlineData("Stellantis", "#243882", "#50bce4")]
    public void DemoApp_ClientTheme_ShouldRetainBrandColours(
        string themeName,
        string expectedPrimary,
        string expectedSecondary)
    {
        // Given
        using JsonDocument app = ReadDemoApp();

        // When
        JsonElement theme = ReadThemes(app: app)
            .GetProperty(propertyName: themeName);

        JsonElement colours = theme.GetProperty(propertyName: "colours");

        // Then
        Assert.Equal(
            expected: expectedPrimary,
            actual: colours
                .GetProperty(propertyName: "primary")
                .GetString());

        Assert.Equal(
            expected: expectedSecondary,
            actual: colours
                .GetProperty(propertyName: "secondary")
                .GetString());
    }

    [Fact]
    public void DemoUpgradePackage_ShouldCarrySelectableThemeCatalog()
    {
        // Given
        string packagePath = Path.Combine(
            paths:
            [
                FindRepositoryRoot(),
                "Packages",
                "CLX Upgrade",
                "demo-app-ui.json",
            ]);

        using JsonDocument package = JsonDocument.Parse(
            json: File.ReadAllText(path: packagePath));

        // When
        JsonElement appItem = package.RootElement
            .GetProperty(propertyName: "Items")
            .EnumerateArray()
            .Single(predicate: item =>
                item
                    .GetProperty(propertyName: "Type")
                    .GetString()
                    == "Core/App");

        using JsonDocument app = JsonDocument.Parse(
            json: appItem
                .GetProperty(propertyName: "Data")
                .GetString()!);

        JsonElement themes = ReadThemes(app: app);

        // Then
        Assert.Equal(
            expected: ExpectedClientThemes.Length + 2,
            actual: themes
                .EnumerateObject()
                .Count());
    }

    private static JsonDocument ReadDemoApp()
    {
        string path = Path.Combine(
            paths:
            [
                FindRepositoryRoot(),
                "Data",
                "demo.dev.localhost",
                "AppConfiguration.json",
            ]);

        return JsonDocument.Parse(json: File.ReadAllText(path: path));
    }

    private static JsonElement ReadThemes(JsonDocument app)
    {
        string configJson = app.RootElement
            .GetProperty(propertyName: "ConfigJson")
            .GetString()!;

        using JsonDocument config = JsonDocument.Parse(json: configJson);

        return config.RootElement
            .GetProperty(propertyName: "Themes")
            .Clone();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(path: AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(path: Path.Combine(
                path1: directory.FullName,
                path2: "Data")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            message: "Could not locate the cCoder.Assets repository root.");
    }
}