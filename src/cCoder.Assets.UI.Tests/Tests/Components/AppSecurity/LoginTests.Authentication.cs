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

                await page.EvaluateAsync(
                    expression: "() => { "
                        + "const login = window.api.login.bind(window.api); "
                        + "window.api.login = async (...args) => { "
                        + "sessionStorage.setItem('login-keep-token', "
                        + "String(args[2] === true)); "
                        + "return await login(...args); }; }");

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

                string? retainedToken = await page.EvaluateAsync<string?>(
                    expression: "() => window.api?.token ?? null");

                Assert.Null(@object: retainedToken);

                Assert.Equal(
                    expected: "false",
                    actual: await page.EvaluateAsync<string>(
                        expression: "() => sessionStorage"
                            + ".getItem('login-keep-token')"));

                await page.GotoAsync(
                    url: new Uri(
                        baseUri: fixture.WebBaseAddress,
                        relativeUri: "Admin/AppManagement")
                        .ToString());

                await page.Locator(
                    selector: ".component[name='AppManagement']")
                    .WaitForAsync();

                ILocator protectedUserProfile = page.Locator(
                    selector: ".component[name='UserProfile']");

                await Assertions.Expect(locator: protectedUserProfile)
                    .ToContainTextAsync(
                        expected: "Assets Acceptance Admin");

                await Assertions.Expect(locator: protectedUserProfile)
                    .Not
                    .ToContainTextAsync(expected: "Guest");
            });

        // Then
    }

    [Fact]
    public async Task Authentication_ShouldRejectInvalidCredentials()
    {
        // Given
        IPage page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(
                url: new Uri(
                    baseUri: fixture.WebBaseAddress,
                    relativeUri: "Login")
                    .ToString());

            await page.GetByLabel(text: "User =")
                .FillAsync(value: "assets-acceptance@localhost");

            await page.GetByLabel(text: "Password =")
                .FillAsync(value: "NotTheAcceptancePassword!");

            // When
            await page.GetByRole(
                role: AriaRole.Button,
                options: new() { Name = "Submit(details);" })
                .ClickAsync();

            // Then
            ILocator failureNotification = page.Locator(
                selector: ".k-notification-error:visible, "
                    + ".alert-danger:visible");

            await failureNotification.First.WaitForAsync();

            Assert.True(
                condition: await failureNotification.CountAsync() > 0,
                userMessage: "Rejected login did not report a visible failure.");

            Assert.Equal(
                expected: "/Login",
                actual: new Uri(uriString: page.Url).AbsolutePath);

            string? retainedToken = await page.EvaluateAsync<string?>(
                expression: "() => window.api?.token ?? null");

            Assert.Null(@object: retainedToken);

            await page.GotoAsync(
                url: new Uri(
                    baseUri: fixture.WebBaseAddress,
                    relativeUri: "Admin/AppManagement")
                    .ToString());

            await page.WaitForURLAsync(
                url: url => new Uri(uriString: url).AbsolutePath == "/Login");
        }
        finally
        {
            await fixture.ClosePageAsync(page: page);
        }
    }
}