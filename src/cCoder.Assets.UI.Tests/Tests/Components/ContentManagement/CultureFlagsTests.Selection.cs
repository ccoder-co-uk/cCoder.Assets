// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class CultureFlagsTests
{
    [Fact]
    public async Task Selection_ShouldChangeAndPersistSessionCulture()
    {
        // Given
        const string componentName = "CultureFlags";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: string.Empty,
            componentName: componentName,
            action: async page =>
            {
                await page.EvaluateAsync(
                    expression: "async () => { "
                        + "const appId = session.app.Id; "
                        + "const current = (await api.get("
                        + "'ContentManagement/AppCulture?$filter=AppId eq ' "
                        + "+ appId)).value.map(item => item.CultureId); "
                        + "for (const cultureId of ['en-GB', 'fr-FR']) { "
                        + "if (!current.includes(cultureId)) { "
                        + "await api.add('ContentManagement/AppCulture', "
                        + "{ AppId: appId, CultureId: cultureId }); "
                        + "} } }");

                await page.ReloadAsync();
                await page.WaitForLoadStateAsync(state: LoadState.NetworkIdle);

                ILocator cultures = page.Locator(
                    selector: ".component[name='CultureFlags'] "
                        + "a[data-culture]");

                await cultures.First.WaitForAsync(
                    options: new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible
                    });

                await Assertions.Expect(locator: cultures.First)
                    .ToHaveCSSAsync(name: "cursor", value: "pointer");

                int cultureCount = await cultures.CountAsync();

                Assert.True(
                    condition: cultureCount > 1,
                    userMessage: "The baseline must expose at least two cultures "
                        + "to verify culture selection.");

                string sessionCulture = await page.EvaluateAsync<string>(
                    expression: "() => window.session.culture");

                string? selectedCulture = null;

                for (int index = 0; index < cultureCount; index++)
                {
                    string? candidate = await cultures.Nth(index: index)
                        .GetAttributeAsync(name: "data-culture");

                    if (!string.Equals(
                        a: candidate,
                        b: sessionCulture,
                        comparisonType: StringComparison.OrdinalIgnoreCase))
                    {
                        selectedCulture = candidate;
                        break;
                    }
                }

                Assert.False(
                    condition: string.IsNullOrWhiteSpace(
                        value: selectedCulture));

                Task navigation = page.WaitForURLAsync(
                    url: url => new Uri(uriString: url).Query.Contains(
                        value: $"culture={selectedCulture}",
                        comparisonType: StringComparison.OrdinalIgnoreCase));

                await page.Locator(
                        selector: ".component[name='CultureFlags'] "
                            + $"a[data-culture='{selectedCulture}']")
                    .ClickAsync();

                await navigation;
                await page.WaitForLoadStateAsync(state: LoadState.NetworkIdle);

                // Then
                Assert.Equal(
                    expected: selectedCulture?.ToLowerInvariant(),
                    actual: await page.EvaluateAsync<string>(
                        expression: "() => window.session.culture"));

                await Assertions.Expect(
                    locator: page.Locator(
                        selector: ".component[name='CultureFlags'] "
                            + $"a[data-culture='{selectedCulture}'].active"))
                    .ToHaveCountAsync(count: 1);

                await page.GotoAsync(
                    url: new Uri(
                        baseUri: fixture.WebBaseAddress,
                        relativeUri: string.Empty).ToString());

                await page.WaitForLoadStateAsync(state: LoadState.NetworkIdle);

                Assert.Equal(
                    expected: selectedCulture?.ToLowerInvariant(),
                    actual: await page.EvaluateAsync<string>(
                        expression: "() => window.session.culture"));

                await Assertions.Expect(
                    locator: page.Locator(
                        selector: ".component[name='CultureFlags'] "
                            + $"a[data-culture='{selectedCulture}'].active"))
                    .ToHaveCountAsync(count: 1);
            });
    }
}