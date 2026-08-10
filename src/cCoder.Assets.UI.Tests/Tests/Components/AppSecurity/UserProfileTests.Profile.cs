// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.AppSecurity;

public sealed partial class UserProfileTests
{
    [Fact]
    public async Task Profile_ShouldOpenStandardDialog()
    {
        // Given
        const string componentName = "UserProfile";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: string.Empty,
            componentName: componentName,
            action: async page =>
            {
                await page.Locator(
                    selector: ".component[name='UserProfile'] "
                        + "[name='userPrefs']")
                    .ClickAsync();

                ILocator dialog = page.Locator(
                    selector: ".k-window:visible, [role='dialog']:visible")
                    .Last;

                await dialog.WaitForAsync(
                    options: new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible
                    });

                // Then
                await Assertions.Expect(
                    locator: dialog.Locator(
                        selectorOrLocator: "input[name='displayName']"))
                    .ToBeVisibleAsync();

                await Assertions.Expect(
                    locator: dialog.Locator(
                        selectorOrLocator: "input[name='email']"))
                    .ToBeVisibleAsync();
            });
    }
}