// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.AppSecurity;

public sealed partial class ClxLoginAssetContractTests
{
    [Fact]
    public void LocalizationContract_ShouldUseResourcesAndCurrentAuthenticationContract()
    {
        // Given
        string repositoryRoot = FindRepositoryRoot();

        string componentPath = Path.Combine(
            paths:
            [
                repositoryRoot,
                "Data",
                "localhost",
                "App",
                "AppSecurity",
                "Components",
                "Login.json"
            ]);

        using JsonDocument component = JsonDocument.Parse(
            json: File.ReadAllText(path: componentPath));

        string content = component.RootElement
            .GetProperty(propertyName: "Content")
            .GetString()!;

        string script = component.RootElement
            .GetProperty(propertyName: "Script")
            .GetString()!;

        // When
        string[] requiredContentResources =
        [
            "[resource_displayname[username]]",
            "[resource_displayname[password]]",
            "[resource_displayname[forgotpassword]]",
            "[resource_displayname[login]]",
            "[resource_displayname[email]]",
            "[resource_displayname[submit]]"
        ];

        // Then
        foreach (string resource in requiredContentResources)
        {
            Assert.Contains(
                expectedSubstring: resource,
                actualString: content,
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain(
            expectedSubstring: "$(\"[name=pass]\").val(),\n\t\t\t\ttrue",
            actualString: script,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "if (!result)",
            actualString: script,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "window.location.replace(Login.getRedirectUrl())",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
    }

    [Fact]
    public void FrenchLocalizationContract_ShouldBeCompleteUniqueAndUtf8()
    {
        // Given
        string repositoryRoot = FindRepositoryRoot();

        string resourcesPath = Path.Combine(
            paths:
            [
                repositoryRoot,
                "Data",
                "localhost",
                "App",
                "AppSecurity",
                "Resources",
                "fr-FR.json"
            ]);

        using JsonDocument resources = JsonDocument.Parse(
            json: File.ReadAllText(path: resourcesPath));

        JsonElement[] rows = resources.RootElement
            .EnumerateArray()
            .ToArray();

        string[] requiredNames =
        [
            "accessdenied",
            "checkyouremail",
            "email",
            "forgotpassword",
            "login",
            "password",
            "passwordresetemailtitle",
            "submit",
            "username"
        ];

        // When
        string[] names = rows
            .Select(
                selector: row => row.GetProperty(propertyName: "Name")
                    .GetString()!)
            .ToArray();

        string serializedRows = string.Join(
            separator: Environment.NewLine,
            values: rows.Select(selector: row => row.ToString()));

        // Then
        foreach (string requiredName in requiredNames)
        {
            Assert.Contains(
                expected: requiredName,
                collection: names,
                comparer: StringComparer.OrdinalIgnoreCase);
        }

        string[] duplicateNames = names
            .GroupBy(
                keySelector: name => name,
                comparer: StringComparer.OrdinalIgnoreCase)
            .Where(predicate: group => group.Count() > 1)
            .Select(selector: group => group.Key)
            .ToArray();

        Assert.Empty(collection: duplicateNames);
        Assert.DoesNotContain(expectedSubstring: "Ã", actualString: serializedRows);
        Assert.DoesNotContain(expectedSubstring: "Â", actualString: serializedRows);
        Assert.DoesNotContain(expectedSubstring: "�", actualString: serializedRows);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(path: AppContext.BaseDirectory);

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