// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Workflow;

[Collection(name: "Published Core UI")]
public sealed partial class WorkflowListTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitializeThroughParentComponent()
    {
        // Given
        const string pagePath = "Admin/Workflows";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "WorkflowList",
            action: async page =>
            {
                await page.SetViewportSizeAsync(
                    width: 1280,
                    height: 800);

                await WorkflowGridFixture.ArrangeVisibleFlowAsync(
                    page: page,
                    componentName: "WorkflowList");

                ILocator component = page.Locator(
                    selector: ".component[name='WorkflowList']:visible");

                ILocator grid = component.Locator(
                    selectorOrLocator: ".k-grid")
                    .First;

                await grid.Locator(
                    selectorOrLocator: ".k-grid-content")
                    .WaitForAsync();

                foreach (string action in new[] { "save", "run", "delete" })
                {
                    await Assertions.Expect(
                        locator: grid.Locator(
                            selectorOrLocator: $".k-master-row:first-child button[name='{action}']"))
                        .ToBeVisibleAsync();
                }

                ILocator expander = grid.Locator(
                    selectorOrLocator: ".k-master-row:first-child .k-hierarchy-cell");

                await expander.ClickAsync();

                await Assertions.Expect(
                    locator: grid.Locator(
                        selectorOrLocator: ".k-detail-row:visible .component[name='FlowInstanceManagement']"))
                    .ToHaveCountAsync(count: 1);

                ILocator instanceGrid = grid.Locator(
                    selectorOrLocator: ".k-detail-row:visible .component[name='FlowInstanceManagement'] .k-grid")
                    .First;

                await instanceGrid.Locator(
                    selectorOrLocator: ".k-grid-content")
                    .WaitForAsync();

                foreach (ILocator actionButton in await grid.Locator(
                    selectorOrLocator: ".k-master-row:first-child button")
                    .AllAsync())
                {
                    await Assertions.Expect(locator: actionButton)
                        .ToBeInViewportAsync();
                }

                await Assertions.Expect(
                    locator: instanceGrid.Locator(
                        selectorOrLocator: ".k-master-row:first-child button[name='delete']"))
                    .ToBeInViewportAsync();

                bool instanceGridFits = await instanceGrid.EvaluateAsync<bool>(
                    expression: "element => element.scrollWidth <= element.clientWidth + 2");

                Assert.True(
                    condition: instanceGridFits,
                    userMessage: "The expanded instance grid must fit within its visible component.");
            });

        // Then
    }
}
