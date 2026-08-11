// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class PageManagementTests
{
    [Fact]
    public async Task Tree_ShouldRenderVisibleIconsAndPersistDragDrop()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "PageManagement",
            action: async page =>
            {
                string sourceName = $"Move Page {Guid.NewGuid():N}";
                string targetName = $"Target Page {Guid.NewGuid():N}";

                int[] ids = await page.EvaluateAsync<int[]>(
                    expression: "async names => {"
                        + "const create = name => api.add('ContentManagement/Page', {"
                        + "Id: 0, ParentId: null, AppId: session.app.Id, Order: 99, "
                        + "ShowOnMenus: false, Name: name, Path: name.replaceAll(' ', '-'), "
                        + "ResourceKey: 'ContentManagement', Layout: 'Default', "
                        + "PageInfo: [{ CultureId: '', Title: name, Description: '', "
                        + "Keywords: '' }], Contents: [{ CultureId: '', Name: 'body', "
                        + "Html: '<p>Playwright page</p>' }], Pages: [], Roles: [] });"
                        + "const source = await create(names[0]);"
                        + "const target = await create(names[1]);"
                        + "await PageManagement.init(session.app, "
                        + "$('.component[name=PageManagement]'));"
                        + "return [source.Id, target.Id]; }",
                    arg: new[] { sourceName, targetName });

                ILocator pagesTab = page.GetByRole(
                    role: AriaRole.Tab,
                    options: new() { Name = "Pages", Exact = true });

                await pagesTab.ClickAsync();

                ILocator tree = page.Locator(
                    selector: ".component[name='PageManagement'] .pageTree");

                ILocator nodes = tree.Locator(
                    selectorOrLocator: "[role='treeitem']");

                Assert.True(
                    condition: await nodes.CountAsync() > 1,
                    userMessage: "Page Management needs at least two real nodes for tree testing.");

                ILocator icons = tree.Locator(selectorOrLocator: ".page-tree-icon");

                Assert.Equal(
                    expected: await nodes.CountAsync(),
                    actual: await icons.CountAsync());

                bool iconsVisible = await icons.EvaluateAllAsync<bool>(
                    expression: "icons => icons.every(icon => {"
                        + "const box = icon.getBoundingClientRect();"
                        + "const style = getComputedStyle(icon);"
                        + "return box.width > 0 && box.height > 0 "
                        + "&& style.visibility !== 'hidden'; })");

                Assert.True(
                    condition: iconsVisible,
                    userMessage: "One or more CMS tree icons have no visible rendering.");

                ILocator source = tree.GetByText(
                    text: sourceName,
                    options: new() { Exact = true });

                ILocator target = tree.GetByText(
                    text: targetName,
                    options: new() { Exact = true });

                await source.DragToAsync(target: target);

                await page.WaitForLoadStateAsync(state: LoadState.NetworkIdle);

                int? parentId = await page.EvaluateAsync<int?>(
                    expression: "async id => (await api.get("
                        + "'ContentManagement/Page(' + id + ')')).ParentId",
                    arg: ids[0]);

                Assert.Equal(expected: ids[1], actual: parentId);
            });

        // Then
    }
}