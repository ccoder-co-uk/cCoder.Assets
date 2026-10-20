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
            });

        // Then
    }
}