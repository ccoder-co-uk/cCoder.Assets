// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Security;

public sealed partial class TenantManagementTests
{
    [Fact]
    public async Task Navigation_ShouldSwitchExpandedTenantTabs()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/Tenants";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "TenantManagement",
            action: async page =>
            {
                ILocator component = page.Locator(
                    selector: ".component[name='TenantManagement']");

                ILocator expander = component.Locator(
                    selectorOrLocator: ".k-master-row .k-hierarchy-cell")
                    .First;

                await expander.WaitForAsync(
                    options: new() { State = WaitForSelectorState.Visible });

                await expander.ClickAsync();

                ILocator detail = component.Locator(
                    selectorOrLocator: ".k-detail-row:visible")
                    .First;

                await Assertions.Expect(
                    locator: detail.Locator(
                        selectorOrLocator: ".component[name='SSORoleManagement']"))
                    .ToHaveCountAsync(count: 1);

                ILocator appsTab = detail.Locator(
                    selectorOrLocator: "button[data-bs-target^='#tenant-apps-']");

                await appsTab.ClickAsync();

                await Assertions.Expect(locator: appsTab)
                    .ToHaveClassAsync(
                        expected: new Regex(pattern: "(^|\\s)active(\\s|$)"));

                ILocator appsPane = detail.Locator(
                    selectorOrLocator: ".tab-pane[id^='tenant-apps-']");

                await Assertions.Expect(locator: appsPane)
                    .ToHaveClassAsync(
                        expected: new Regex(pattern: "(^|\\s)active(\\s|$)"));

                await Assertions.Expect(
                    locator: appsPane.Locator(
                        selectorOrLocator: ".component[name='TenantAppManagement']"))
                    .ToHaveCountAsync(count: 1);
            });

        // Then
    }
}