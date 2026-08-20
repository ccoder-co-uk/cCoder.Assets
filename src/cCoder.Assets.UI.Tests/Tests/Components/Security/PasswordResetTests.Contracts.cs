// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Security;

public sealed partial class PasswordResetContractTests
{
    [Fact]
    public void Contracts_ShouldUseTokenBackedResetEndpoint()
    {
        // Given
        string componentPath = Path.Combine(paths:
        [
            FindRepositoryRoot(),
            "Data",
            "Default App",
            "Common Cache",
            "Security",
            "Components",
            "PasswordReset.json"
        ]);

        // When
        using JsonDocument component = JsonDocument.Parse(
            json: File.ReadAllText(path: componentPath));

        string script = component.RootElement
            .GetProperty(propertyName: "Script")
            .GetString()!;

        string content = component.RootElement
            .GetProperty(propertyName: "Content")
            .GetString()!;

        // Then
        Assert.Contains(
            expectedSubstring: "api.post(\"Account/ConfirmForgotPassword\"",
            actualString: script,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "url.searchParams.get(\"uid\")",
            actualString: script,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "url.searchParams.get(\"token\")",
            actualString: script,
            comparisonType: StringComparison.Ordinal);

        Assert.Contains(
            expectedSubstring: "<style nonce=\"[request[nonce]]\">",
            actualString: content,
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