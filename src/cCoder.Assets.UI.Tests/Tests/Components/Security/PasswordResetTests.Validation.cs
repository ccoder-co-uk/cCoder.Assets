// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Security;

public sealed partial class PasswordResetTests
{
    [Fact]
    public async Task Validation_ShouldRequireBothPasswordFields()
    {
        // Given
        const string pagePath = "ResetPassword";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "PasswordReset",
            action: async page =>
            {
                ILocator component = page.Locator(
                    selector: ".component[name='PasswordReset']");

                ILocator password = component.Locator(
                    selectorOrLocator: "input[name='pass']");

                ILocator confirmation = component.Locator(
                    selectorOrLocator: "input[name='confirm']");

                await Assertions.Expect(locator: password)
                    .ToHaveAttributeAsync(name: "required", value: "");

                await Assertions.Expect(locator: confirmation)
                    .ToHaveAttributeAsync(name: "required", value: "");

                Assert.False(
                    condition: await password.EvaluateAsync<bool>(
                        expression: "element => element.checkValidity()"));

                Assert.False(
                    condition: await confirmation.EvaluateAsync<bool>(
                        expression: "element => element.checkValidity()"));
            });

        // Then
    }
}