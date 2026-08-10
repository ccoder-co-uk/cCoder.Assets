// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.AppSecurity;

public sealed partial class LoginTests
{
    [Fact]
    public async Task Authentication_ShouldCreateAuthenticatedSession()
    {
        // Given
        const string pagePath = "Login";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "Login",
            action: async page =>
            {
                await page.GetByLabel(text: "User =")
                    .FillAsync(value: "assets-acceptance@localhost");

                await page.GetByLabel(text: "Password =")
                    .FillAsync(value: "AssetsAcceptance123!");

                await page.GetByRole(
                    role: AriaRole.Button,
                    options: new() { Name = "Submit(details);" })
                    .ClickAsync();

                await page.WaitForURLAsync(
                    url: url => new Uri(uriString: url).AbsolutePath == "/");

                await Assertions.Expect(
                    locator: page.Locator(
                        selector: ".component[name='UserProfile']"))
                    .ToContainTextAsync(
                        expected: "Assets Acceptance Admin");

                await Assertions.Expect(
                    locator: page.Locator(
                        selector: ".component[name='UserProfile']"))
                    .Not
                    .ToContainTextAsync(expected: "Guest");
            });

        // Then
    }
}