// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Security;

[Collection(name: "Published Core UI")]
public sealed partial class TenantManagementTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitialize()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/Tenants";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "TenantManagement",
            action: page => ComponentTestDriver.AssertKendoWidgetAsync(
                page: page,
                selector: ".component[name='TenantManagement'] "
                    + "[name='tenantsGrid']",
                widgetName: "kendoGrid"));

        // Then
    }
}