// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.AppSecurity;

public sealed partial class LoginTests
{
    [Fact]
    public async Task Styling_ShouldPreserveTextboxFontWhenFocused()
    {
        // Given
        const string pagePath = "Login";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "Login",
            action: async page =>
            {
                ILocator userInput = page.GetByLabel(text: "User =");

                string fontBeforeFocus = await ReadComputedFontAsync(
                    locator: userInput);

                await userInput.FocusAsync();

                string fontAfterFocus = await ReadComputedFontAsync(
                    locator: userInput);

                Assert.Equal(
                    expected: fontBeforeFocus,
                    actual: fontAfterFocus);
            });

        // Then
    }

    private static Task<string> ReadComputedFontAsync(ILocator locator) =>
        locator.EvaluateAsync<string>(
            expression: "element => getComputedStyle(element).font");
}