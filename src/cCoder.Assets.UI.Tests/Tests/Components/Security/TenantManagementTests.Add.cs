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
    public async Task Add_ShouldInsertEditableTenantRow()
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

                ILocator rows = component.Locator(
                    selectorOrLocator: ".k-grid tbody > tr");

                int originalCount = await rows.CountAsync();

                await component.Locator(
                    selectorOrLocator: "button[name='add']")
                    .ClickAsync();

                await Assertions.Expect(locator: rows)
                    .ToHaveCountAsync(count: originalCount + 1);

                await Assertions.Expect(locator: rows.First)
                    .ToHaveAttributeAsync(
                        name: "data-uid",
                        value: new Regex(pattern: ".+"));
            });

        // Then
    }
}