// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.AppSecurity;

[Collection(name: "Published Core UI")]
public sealed partial class LoginTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitialize()
    {
        // Given
        const string pagePath = "Login";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: "Login");

        // Then
    }

    [Fact]
    public async Task Rendering_ShouldNotRequestGlobalMetadata()
    {
        // Given
        IPage page = await fixture.NewPageAsync();
        List<string> requestedAddresses = [];

        page.Request += (_, request) =>
            requestedAddresses.Add(item: request.Url);

        // When
        await page.GotoAsync(
            url: new Uri(
                baseUri: fixture.WebBaseAddress,
                relativeUri: "Login")
                .ToString());

        await page.Locator(selector: ".component[name='Login']")
            .WaitForAsync();

        // Then
        Assert.DoesNotContain(
            collection: requestedAddresses,
            filter: address => address.Contains(
                value: "/Api/GetMetadata",
                comparisonType: StringComparison.OrdinalIgnoreCase));

        await fixture.ClosePageAsync(page: page);
    }
}