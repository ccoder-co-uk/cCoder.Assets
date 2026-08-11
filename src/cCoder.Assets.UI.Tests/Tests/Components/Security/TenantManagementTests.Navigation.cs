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

                ILocator tenantAppManagement = appsPane.Locator(
                    selectorOrLocator: ".component[name='TenantAppManagement']");

                ILocator appExpander = tenantAppManagement.Locator(
                    selectorOrLocator: ".k-master-row .k-hierarchy-cell")
                    .First;

                await appExpander.WaitForAsync(
                    options: new() { State = WaitForSelectorState.Visible });

                await appExpander.ClickAsync();

                ILocator appDetail = tenantAppManagement.Locator(
                    selectorOrLocator: ".k-detail-row:visible")
                    .First;

                ILocator appManagement = appDetail.Locator(
                    selectorOrLocator: ".component[name='AppManagement']");

                await Assertions.Expect(locator: appManagement)
                    .ToHaveCountAsync(count: 1);

                Dictionary<string, string> managersByTab = new()
                {
                    ["Pages"] = "PageManagement",
                    ["Theming"] = "AppTheming",
                    ["Cultures"] = "CultureManagement",
                    ["Layouts"] = "LayoutManagement",
                    ["Templates"] = "TemplateManagement",
                    ["Components"] = "ComponentManagement",
                    ["Resources"] = "ResourceManagement",
                    ["roles"] = "RoleManagement"
                };

                foreach ((string tabName, string managerName) in managersByTab)
                {
                    ILocator managerTab = appManagement.GetByRole(
                        role: AriaRole.Tab,
                        options: new() { Name = tabName, Exact = true });

                    await managerTab.ClickAsync();

                    await Assertions.Expect(locator: managerTab)
                        .ToHaveClassAsync(
                            expected: new Regex(pattern: "(^|\\s)active(\\s|$)"));

                    ILocator manager = appManagement.Locator(
                        selectorOrLocator: $".component[name='{managerName}']");

                    await Assertions.Expect(locator: manager)
                        .ToHaveCountAsync(count: 1);

                    try
                    {
                        await page.WaitForFunctionAsync(
                            expression: "element => window.jQuery(element)"
                                + ".data('managerInitialised') === true",
                            arg: await manager.ElementHandleAsync(),
                            options: new() { Timeout = 15_000 });
                    }
                    catch (TimeoutException exception)
                    {
                        throw new TimeoutException(
                            message: $"Nested {managerName} did not initialize from the {tabName} tab.",
                            innerException: exception);
                    }
                }

                await appExpander.ClickAsync();
                await appExpander.ClickAsync();

                await Assertions.Expect(
                    locator: tenantAppManagement.Locator(
                        selectorOrLocator: ".k-detail-row:visible "
                            + ".component[name='AppManagement']"))
                    .ToHaveCountAsync(count: 1);
            });

        // Then
    }
}