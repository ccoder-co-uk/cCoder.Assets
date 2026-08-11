// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class LayoutManagementTests
{
    [Fact]
    public async Task Expansion_ShouldRenderEditorsAtUsableHeight()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "LayoutManagement",
            action: async page =>
            {
                await page.GetByRole(
                    role: AriaRole.Tab,
                    options: new() { Name = "Layouts", Exact = true })
                    .ClickAsync();

                ILocator component = page.Locator(
                    selector: ".component[name='LayoutManagement']");

                ILocator expander = component.Locator(
                    selectorOrLocator: ".k-master-row .k-hierarchy-cell")
                    .First;

                await expander.WaitForAsync(
                    options: new() { State = WaitForSelectorState.Visible });

                await expander.ClickAsync();

                ILocator detail = component.Locator(
                    selectorOrLocator: ".k-detail-row:visible")
                    .First;

                await detail.WaitForAsync(
                    options: new() { State = WaitForSelectorState.Visible });

                float height = await detail.EvaluateAsync<float>(
                    expression: "element => element.getBoundingClientRect().height");

                Assert.True(
                    condition: height >= 300,
                    userMessage: $"Layout expansion is only {height:F1}px high.");

                await ComponentTestDriver.AssertMonacoEditorAsync(
                    page: page,
                    containerSelector: ".component[name='LayoutManagement'] "
                        + ".k-detail-row:visible",
                    language: "html");
            });

        // Then
    }
}