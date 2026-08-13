// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class CommonCacheScriptsTests
{
    [Fact]
    public async Task Expansion_ShouldProvideUsableEditorHeight()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/CommonCache";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "CommonCacheScripts",
            action: async page =>
            {
                await page.GetByRole(
                    role: AriaRole.Tab,
                    options: new() { Name = "Scripts", Exact = true })
                    .ClickAsync();

                ILocator component = page.Locator(
                    selector: ".component[name='CommonCacheScripts']");

                await component
                    .Locator(selectorOrLocator: ".k-master-row .k-hierarchy-cell")
                    .First
                    .ClickAsync();

                ILocator detail = component.Locator(
                    selectorOrLocator: ".k-detail-row:visible")
                    .First;

                await detail.WaitForAsync(
                    options: new() { State = WaitForSelectorState.Visible });

                ILocator editor = detail.Locator(
                    selectorOrLocator: "[name='script'] .monaco-editor");

                await editor.WaitForAsync(
                    options: new() { State = WaitForSelectorState.Visible });

                float height = await editor.EvaluateAsync<float>(
                    expression: "element => element.getBoundingClientRect().height");

                Assert.True(
                    condition: height >= 250,
                    userMessage: $"The expanded script editor is only "
                        + $"{height:F1}px high.");
            });

        // Then
    }
}
