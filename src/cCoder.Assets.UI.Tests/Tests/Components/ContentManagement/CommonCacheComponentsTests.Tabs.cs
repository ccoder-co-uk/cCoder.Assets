// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class CommonCacheComponentsTests
{
    [Fact]
    public async Task Tabs_ShouldShowOneComponentEditorAtATime()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/CommonCacheManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "CommonCacheComponents",
            action: async page =>
            {
                ILocator component = page.Locator(
                    selector: ".component[name='CommonCacheComponents']");

                await component
                    .Locator(selectorOrLocator: ".k-master-row .k-hierarchy-cell")
                    .First
                    .ClickAsync();

                ILocator detail = component.Locator(
                    selectorOrLocator: ".k-detail-row:visible")
                    .First;

                await detail.WaitForAsync(
                    options: new() { State = WaitForSelectorState.Visible });

                ILocator tabs = detail.Locator(
                    selectorOrLocator: "[name='componentEditor'] "
                        + "button[data-bs-toggle='tab']");

                Assert.Equal(expected: 2, actual: await tabs.CountAsync());

                Assert.Equal(
                    expected: 2,
                    actual: await tabs.Locator(
                        selectorOrLocator: ".k-icon:visible")
                        .CountAsync());

                Assert.Equal(
                    expected: 1,
                    actual: await detail.Locator(
                        selectorOrLocator: ".editor-tab-pane:visible")
                        .CountAsync());

                await AssertActiveEditorIsUsableAsync(
                    detail: detail,
                    paneName: "content");

                await tabs
                    .Nth(index: 1)
                    .ClickAsync();

                Assert.Equal(
                    expected: 1,
                    actual: await detail.Locator(
                        selectorOrLocator: ".editor-tab-pane:visible")
                        .CountAsync());

                await AssertActiveEditorIsUsableAsync(
                    detail: detail,
                    paneName: "script");

                await ComponentTestDriver.AssertMonacoEditorAsync(
                    page: page,
                    containerSelector: ".component[name='CommonCacheComponents'] "
                        + ".k-detail-row:visible [data-editor-pane='script']",
                    language: "javascript");

                await tabs
                    .Nth(index: 0)
                    .ClickAsync();

                await AssertActiveEditorIsUsableAsync(
                    detail: detail,
                    paneName: "content");
            });

        // Then
    }

    private static async Task AssertActiveEditorIsUsableAsync(
        ILocator detail,
        string paneName)
    {
        ILocator editor = detail.Locator(
            selectorOrLocator: $"[data-editor-pane='{paneName}'] .monaco-editor");

        await editor.WaitForAsync(
            options: new() { State = WaitForSelectorState.Visible });

        float width = await editor.EvaluateAsync<float>(
            expression: "element => element.getBoundingClientRect().width");

        float height = await editor.EvaluateAsync<float>(
            expression: "element => element.getBoundingClientRect().height");

        Assert.True(
            condition: width >= 300 && height >= 250,
            userMessage: $"The active {paneName} editor is only "
                + $"{width:F1}x{height:F1}px.");
    }
}