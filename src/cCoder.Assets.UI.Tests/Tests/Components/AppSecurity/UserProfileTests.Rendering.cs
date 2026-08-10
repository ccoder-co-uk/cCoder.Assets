// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.AppSecurity;

[Collection(name: "Published Core UI")]
public sealed partial class UserProfileTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldShowAuthenticatedUser()
    {
        // Given
        const string componentName = "UserProfile";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: string.Empty,
            componentName: componentName,
            action: async page =>
            {
                ILocator component = page.Locator(
                    selector: ".component[name='UserProfile']");

                await Assertions.Expect(
                    locator: component.Locator(
                        selectorOrLocator: "[name='userPrefs']"))
                    .Not.ToHaveTextAsync(expected: "Guest");

                // Then
                await Assertions.Expect(
                    locator: component.Locator(
                        selectorOrLocator: "a[name='login']"))
                    .ToBeHiddenAsync();

                await Assertions.Expect(
                    locator: component.Locator(
                        selectorOrLocator: "a[name='logout']"))
                    .ToBeVisibleAsync();
            });
    }
}