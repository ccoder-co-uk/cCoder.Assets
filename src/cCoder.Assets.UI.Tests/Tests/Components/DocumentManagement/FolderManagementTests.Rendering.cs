// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.DocumentManagement;

[Collection(name: "Published Core UI")]
public sealed partial class FolderManagementTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitializeThroughParentComponent()
    {
        // Given
        const string pagePath = "Admin/DocumentManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "FolderManagement",
            action: async page =>
            {
                await DocumentManagementGridFixture.ArrangeVisibleFileRowAsync(
                    page: page);
            });

        // Then
    }

    [Fact]
    public async Task ManageFolder_ShouldOpenRolesAndEventsComponents()
    {
        // Given
        const string pagePath = "Admin/DocumentManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "FolderManagement",
            action: async page =>
            {
                await page.WaitForFunctionAsync(
                    expression: "() => { const button = document.querySelector(\"button[name='manageFolder']\"); return !!window.jQuery?._data(button, 'events')?.click?.length; }");

                await page
                    .Locator(selector: "button[name='manageFolder']")
                    .ClickAsync();

                ILocator tabs = page.Locator(
                    selector: "[name='manageFolderTabs']:visible");

                await tabs.WaitForAsync();

                await tabs
                    .GetByText(
                        text: "Roles",
                        options: new() { Exact = true })
                    .ClickAsync();

                await page
                    .Locator(selector: ".component[name='FolderRoles']:visible")
                    .WaitForAsync();

                await tabs
                    .GetByText(
                        text: "Events",
                        options: new() { Exact = true })
                    .ClickAsync();

                ILocator events = page.Locator(
                    selector: "[name='folderEventsHost']:visible");

                await events.WaitForAsync();

                await events
                    .Locator(selectorOrLocator: ".k-grid")
                    .WaitForAsync();
            });

        // Then
    }
}
