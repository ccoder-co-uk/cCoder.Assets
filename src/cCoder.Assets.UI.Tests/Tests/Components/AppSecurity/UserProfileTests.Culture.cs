// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.AppSecurity;

public sealed partial class UserProfileTests
{
    [Fact]
    public async Task Culture_ShouldApplyProfileDefaultAfterLogin()
    {
        // Given
        const string componentName = "UserProfile";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: string.Empty,
            componentName: componentName,
            action: async page =>
            {
                string[] state = await page.EvaluateAsync<string[]>(
                    expression: "async () => { "
                        + "const appId = session.app.Id; "
                        + "const current = (await api.get("
                        + "'ContentManagement/AppCulture?$filter=AppId eq ' "
                        + "+ appId)).value.map(item => item.CultureId); "
                        + "for (const cultureId of ['en-GB', 'fr-FR']) { "
                        + "if (!current.includes(cultureId)) { "
                        + "await api.add('ContentManagement/AppCulture', "
                        + "{ AppId: appId, CultureId: cultureId }); "
                        + "} } "
                        + "const user = await api.get('AppSecurity/User/Me()'); "
                        + "const ssoUser = await api.get('Account/Me'); "
                        + "const target = 'fr-FR'; "
                        + "const userId = user.Id || user.id; "
                        + "const email = ssoUser.Email || ssoUser.email; "
                        + "const displayName = ssoUser.DisplayName "
                        + "|| ssoUser.displayName; "
                        + "await api.update(\"AppSecurity/User('\" + userId + \"')\", { "
                        + "Id: userId, Email: email, "
                        + "DisplayName: displayName, "
                        + "DefaultCultureId: target }); "
                        + "return [userId, user.DefaultCultureId "
                        + "|| user.defaultCultureId || '', target, "
                        + "email, displayName]; "
                        + "}");

                try
                {
                    await page.EvaluateAsync(
                        expression: "async () => api.logout()");

                    await page.GotoAsync(
                        url: new Uri(
                            baseUri: fixture.WebBaseAddress,
                            relativeUri: "Login").ToString());

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

                    await page.WaitForLoadStateAsync(
                        state: LoadState.NetworkIdle);

                    // Then
                    Assert.Equal(
                        expected: state[2].ToLowerInvariant(),
                        actual: await page.EvaluateAsync<string>(
                            expression: "() => window.session.culture"));

                    ILocator activeCulture = page.Locator(
                        selector: ".component[name='CultureFlags'] "
                            + "a[data-culture].active");

                    await Assertions.Expect(locator: activeCulture)
                        .ToHaveCountAsync(count: 1);

                    Assert.Equal(
                        expected: state[2].ToLowerInvariant(),
                        actual: (await activeCulture.GetAttributeAsync(
                            name: "data-culture"))?.ToLowerInvariant());
                }
                finally
                {
                    await page.EvaluateAsync(
                        expression: "async state => { "
                            + "await api.update(\"AppSecurity/User('\" "
                            + "+ state[0] + \"')\", { Id: state[0], "
                            + "Email: state[3], DisplayName: state[4], "
                            + "DefaultCultureId: state[1] }); "
                            + "}",
                        arg: state);
                }
            });
    }
}