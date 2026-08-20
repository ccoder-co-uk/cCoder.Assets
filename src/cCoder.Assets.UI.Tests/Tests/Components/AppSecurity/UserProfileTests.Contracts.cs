// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.AppSecurity;

public sealed partial class UserProfileContractTests
{
    [Fact]
    public void Contracts_ShouldUseAuthenticatedSelfServiceEndpoints()
    {
        // Given
        string componentPath = Path.Combine(paths:
        [
            FindRepositoryRoot(),
            "Data",
            "Default App",
            "Common Cache",
            "AppSecurity",
            "Components",
            "UserProfile.json"
        ]);

        // When
        using JsonDocument component = JsonDocument.Parse(
            json: File.ReadAllText(path: componentPath));

        string script = component.RootElement
            .GetProperty(propertyName: "Script")
            .GetString()!;

        // Then
        Assert.Contains(
            expectedSubstring: "api.post(\"Account/ChangePassword\"",
            actualString: script,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "api.update(\"Account/Me\"",
            actualString: script,
            comparisonType: StringComparison.Ordinal);

        Assert.DoesNotContain(
            expectedSubstring: "api.update(\"Security/SSOUser(",
            actualString: script,
            comparisonType: StringComparison.Ordinal);
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
