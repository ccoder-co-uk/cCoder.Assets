// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.AppSecurity;

public sealed partial class RoleManagementTests
{
    [Fact]
    public async Task Expansion_ShouldInitializeUsersAndPrivilegesOnce()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "RoleManagement",
            action: async page =>
            {
                await page.GetByRole(
                    role: AriaRole.Tab,
                    options: new() { Name = "roles", Exact = true })
                    .ClickAsync();

                ILocator component = page.Locator(
                    selector: ".component[name='RoleManagement']");

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
                        selectorOrLocator: ".component[name='RoleUserManagement']"))
                    .ToHaveCountAsync(count: 1);

                await Assertions.Expect(
                    locator: detail.Locator(
                        selectorOrLocator: ".component[name='RolePrivManagement']"))
                    .ToHaveCountAsync(count: 1);

                await expander.ClickAsync();
                await expander.ClickAsync();

                await Assertions.Expect(
                    locator: component.Locator(
                        selectorOrLocator: ".k-detail-row:visible .component"
                    ))
                    .ToHaveCountAsync(count: 2);

                await expander.ClickAsync();
            });

        // Then
    }
}