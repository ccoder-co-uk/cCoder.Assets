// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using System.Text.Json;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Security;

public sealed partial class TenantManagementTests
{
    [Fact]
    public async Task AddApp_ShouldOfferTenantBoundCreationWithInitialPackages()
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

                await component.Locator(
                    selectorOrLocator: ".k-master-row .k-hierarchy-cell")
                    .First
                    .ClickAsync();

                ILocator detail = component.Locator(
                    selectorOrLocator: ".k-detail-row:visible")
                    .First;

                await detail.Locator(
                    selectorOrLocator: "button[data-bs-target^='#tenant-apps-']")
                    .ClickAsync();

                ILocator tenantApps = detail.Locator(
                    selectorOrLocator: ".component[name='TenantAppManagement']");

                ILocator newApp = tenantApps.Locator(
                    selectorOrLocator: "button[name='newApp']");

                await Assertions.Expect(locator: newApp)
                    .ToBeVisibleAsync();

                await newApp.ClickAsync();

                ILocator dialog = page.Locator(
                    selector: ".k-window:visible");

                await Assertions.Expect(
                    locator: dialog.Locator(
                        selectorOrLocator: "input[name='name']"))
                    .ToBeVisibleAsync();

                await Assertions.Expect(
                    locator: dialog.Locator(
                        selectorOrLocator: "input[name='domain']"))
                    .ToBeVisibleAsync();

                await Assertions.Expect(
                    locator: dialog.Locator(
                        selectorOrLocator: "[name='initialPackages']"))
                    .ToBeVisibleAsync();

                await dialog.GetByRole(
                    role: AriaRole.Button,
                    options: new() { Name = "Close" })
                    .ClickAsync();
            });

        // Then
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task AddApp_ShouldCreateTenantChildAndImportSelectedPackages(
        int packageCount)
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/Tenants";
        string domain = $"tenant-child-{packageCount}.localhost";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "TenantManagement",
            action: async page =>
            {
                ILocator component = page.Locator(
                    selector: ".component[name='TenantManagement']");

                ILocator tenantRow = component.Locator(
                    selectorOrLocator: ".k-master-row")
                    .First;

                string tenantId = await tenantRow.EvaluateAsync<string>(
                    expression: "row => window.jQuery(row)"
                        + ".closest('.k-grid').data('kendoGrid')"
                        + ".dataItem(row).Id");

                await tenantRow.Locator(
                    selectorOrLocator: ".k-hierarchy-cell")
                    .ClickAsync();

                ILocator detail = component.Locator(
                    selectorOrLocator: ".k-detail-row:visible")
                    .First;

                await detail.Locator(
                    selectorOrLocator: "button[data-bs-target^='#tenant-apps-']")
                    .ClickAsync();

                ILocator tenantApps = detail.Locator(
                    selectorOrLocator: ".component[name='TenantAppManagement']");

                await tenantApps.Locator(
                    selectorOrLocator: "button[name='newApp']")
                    .ClickAsync();

                ILocator dialog = page.Locator(
                    selector: ".k-window:visible");

                await dialog.Locator(selectorOrLocator: "input[name='name']")
                    .FillAsync(value: $"Tenant child {packageCount}");

                await dialog.Locator(selectorOrLocator: "input[name='domain']")
                    .FillAsync(value: domain);

                string[] packages = packageCount == 1
                    ? ["App/Core.json"]
                    : ["App/Core.json", "App/Mail.json"];

                await dialog.Locator(
                    selectorOrLocator: "select[name='initialPackages']")
                    .SelectOptionAsync(values: packages);

                await dialog.Locator(selectorOrLocator: "button[name='create']")
                    .ClickAsync();

                await Assertions.Expect(locator: dialog)
                    .ToBeHiddenAsync(
                        options: new() { Timeout = 20_000 });

                IJSHandle appHandle = await page.WaitForFunctionAsync(
                    expression: "async domain => {"
                        + "const result = await api.get("
                        + "`ContentManagement/App?$filter=Domain eq '${domain}'`);"
                        + "return result.value.length ? JSON.stringify(result.value[0]) : false;}",
                    arg: domain,
                    options: new() { Timeout = 20_000 });

                string appJson = await appHandle.JsonValueAsync<string>();

                using JsonDocument appDocument = JsonDocument.Parse(json: appJson);
                JsonElement app = appDocument.RootElement;

                int appId = app
                    .GetProperty(propertyName: "Id")
                    .GetInt32();

                Assert.Equal(
                    expected: tenantId,
                    actual: app
                        .GetProperty(propertyName: "TenantId")
                        .GetString());

                await page.WaitForFunctionAsync(
                    expression: "async appId => (await api.get("
                        + "`ContentManagement/Page?$filter=AppId eq ${appId}`))"
                        + ".value.length > 0",
                    arg: appId,
                    options: new() { Timeout = 30_000 });

                if (packageCount == 2)
                {
                    await page.WaitForFunctionAsync(
                        expression: "async appId => (await api.get("
                            + "`ContentManagement/Page?$filter=AppId eq ${appId} "
                            + "and Path eq 'Admin/MailManagement'`))"
                            + ".value.length === 1",
                        arg: appId,
                        options: new() { Timeout = 30_000 });
                }
            });

        // Then
    }
}