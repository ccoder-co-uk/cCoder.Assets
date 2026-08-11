// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Security;

public sealed partial class TenantManagementTests
{
    [Fact]
    public async Task Load_ShouldRenderPersistedTenants()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/Tenants";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "TenantManagement",
            action: async page =>
            {
                string tenantName = $"Assets Tenant {Guid.NewGuid():N}";

                await page.EvaluateAsync(
                    expression: "async name => {"
                        + "await api.add('Security/Tenant', {"
                        + "Id: crypto.randomUUID(), Name: name, "
                        + "Description: 'Playwright tenant load', "
                        + "CreatedBy: 'AssetsAcceptanceAdmin', "
                        + "LastUpdatedBy: 'AssetsAcceptanceAdmin', "
                        + "CreatedOn: new Date().toISOString(), "
                        + "LastUpdated: new Date().toISOString(), Roles: [], "
                        + "UserEvents: [], Analysis: [] });"
                        + "await TenantManagement.init(session.app, "
                        + "$('.component[name=TenantManagement]')); }",
                    arg: tenantName);

                ILocator row = page.Locator(
                    selector: ".component[name='TenantManagement'] "
                        + ".k-grid tbody > tr")
                    .Filter(new() { HasText = tenantName });

                await Assertions.Expect(locator: row)
                    .ToHaveCountAsync(
                        count: 1,
                        options: new() { Timeout = 15_000 });
            });

        // Then
    }
}