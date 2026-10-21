// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Tests;

public sealed partial class AdminInformationArchitectureAssetTests
{
    [Fact]
    public async Task ClxPortalAdministrationShouldRetainItsAppOwnedUiContract()
    {
        // Given
        string componentDirectory = Path.Combine(
            paths:
            [
                FindDataDirectory(),
                "CLX Upgrade",
                "localhost",
                "App",
                "B2B",
                "Components",
            ]);

        // When
        string portalContent = await ReadPropertyAsync(
            path: Path.Combine(
                path1: componentDirectory,
                path2: "PortalAdmin.json"),
            propertyName: "Content");

        string globalizationContent = await ReadPropertyAsync(
            path: Path.Combine(
                path1: componentDirectory,
                path2: "GlobalizationManagement.json"),
            propertyName: "Content");

        string systemsScript = await ReadPropertyAsync(
            path: Path.Combine(
                path1: componentDirectory,
                path2: "SystemsManagement.json"),
            propertyName: "Script");

        string companyScript = await ReadPropertyAsync(
            path: Path.Combine(
                path1: componentDirectory,
                path2: "CompanyManagement.json"),
            propertyName: "Script");

        // Then
        Assert.Contains(
            expectedSubstring: "padding-bottom: 56px",
            actualString: portalContent);

        Assert.DoesNotContain(
            expectedSubstring: "</input>",
            actualString: globalizationContent);

        Assert.DoesNotContain(
            expectedSubstring: "<input name=\"id\" type=\"text\"></button>",
            actualString: globalizationContent);

        Assert.Contains(
            expectedSubstring: "class='btn btn-primary btn-sm' name='create'",
            actualString: systemsScript);

        Assert.Contains(
            expectedSubstring: "class='btn btn-primary btn-sm' name='create'",
            actualString: companyScript);

        Assert.Contains(
            expectedSubstring: "await grid.init();",
            actualString: companyScript);

        Assert.Contains(
            expectedSubstring: "CompanyManagement.newCompany(e, grid)",
            actualString: companyScript);
    }
}