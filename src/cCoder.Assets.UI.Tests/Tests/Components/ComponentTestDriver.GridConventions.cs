// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components;

internal sealed partial class ComponentTestDriver
{
    internal static async Task AssertGridConventionsAsync(
        IPage page,
        string componentName)
    {
        ILocator component = page.Locator(
            selector: $".component[name='{componentName}']");

        ILocator grids = component.Locator(
            selectorOrLocator: ".k-grid");

        int gridCount = await grids.CountAsync();

        for (int index = 0; index < gridCount; index++)
        {
            ILocator grid = grids.Nth(index: index);

            if (!await grid.IsVisibleAsync())
            {
                continue;
            }

            string[] failures = await grid.EvaluateAsync<string[]>(
                expression: "element => {"
                    + "const widget = window.jQuery(element).data('kendoGrid');"
                    + "if (!widget) return ['Kendo Grid was not initialized'];"
                    + "const failures = [];"
                    + "const columns = (widget.columns || []).filter(column => "
                    + "Boolean(column.field));"
                    + "if (columns.length && !widget.options.sortable) "
                    + "failures.push('sorting is disabled');"
                    + "if (columns.length && !widget.options.filterable) "
                    + "failures.push('filtering is disabled');"
                    + "columns.forEach(column => {"
                    + "if (column.sortable === false) failures.push(column.field "
                    + "+ ' is not sortable');"
                    + "if (column.filterable === false) failures.push(column.field "
                    + "+ ' is not filterable'); });"
                    + "if (widget.options.pageable "
                    + "&& !element.querySelector('.k-pager')) "
                    + "failures.push('paging controls are missing');"
                    + "return failures; }");

            Assert.True(
                condition: failures.Length == 0,
                userMessage: $"{componentName} grid {index + 1}: "
                    + string.Join(separator: "; ", value: failures));

            await AssertRepeatedGridExpansionAsync(
                grid: grid,
                componentName: componentName,
                gridIndex: index);

            await AssertGridSearchAsync(
                grid: grid,
                componentName: componentName,
                gridIndex: index);
        }
    }

    private static async Task AssertRepeatedGridExpansionAsync(
        ILocator grid,
        string componentName,
        int gridIndex)
    {
        ILocator expander = grid.Locator(
            selectorOrLocator: ".k-master-row:not(.k-grid-edit-row) "
                + ".k-hierarchy-cell")
            .First;

        if (await expander.CountAsync() == 0)
        {
            return;
        }

        ILocator visibleDetails = grid.Locator(
            selectorOrLocator: ".k-detail-row:visible");

        if (await visibleDetails.CountAsync() > 0)
        {
            await expander.ClickAsync();
        }

        int? expectedComponentCount = null;

        for (int iteration = 0; iteration < 3; iteration++)
        {
            await expander.ClickAsync();

            ILocator detailRow = grid.Locator(
                selectorOrLocator: ".k-detail-row:visible")
                .First;

            await detailRow.WaitForAsync(
                options: new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5_000
                });

            await grid.Page.WaitForLoadStateAsync(
                state: LoadState.NetworkIdle);

            ILocator loadingComponents = detailRow.Locator(
                selectorOrLocator: "[data-component-loading='true']");

            if (await loadingComponents.CountAsync() > 0)
            {
                await Assertions.Expect(locator: loadingComponents)
                    .ToHaveCountAsync(
                        count: 0,
                        options: new LocatorAssertionsToHaveCountOptions
                        {
                            Timeout = 10_000
                        });
            }

            float height = await detailRow.EvaluateAsync<float>(
                expression: "element => element.getBoundingClientRect().height");

            Assert.True(
                condition: height > 50,
                userMessage: $"{componentName} grid {gridIndex + 1} "
                    + $"expanded to only {height:F1}px.");

            int componentCount = await detailRow.Locator(
                selectorOrLocator: ".component")
                .CountAsync();

            expectedComponentCount ??= componentCount;

            Assert.Equal(
                expected: expectedComponentCount.Value,
                actual: componentCount);

            await expander.ClickAsync();
        }
    }

    private static async Task AssertGridSearchAsync(
        ILocator grid,
        string componentName,
        int gridIndex)
    {
        ILocator search = grid.Locator(
            selectorOrLocator: ".k-grid-search input:visible");

        if (await search.CountAsync() == 0)
        {
            return;
        }

        await search.ClickAsync();
        ILocatorAssertions searchAssertions = Assertions.Expect(
            locator: search);

        await searchAssertions.ToBeEditableAsync();
        await search.FillAsync(value: "component-search-check");

        await grid.Page.WaitForTimeoutAsync(timeout: 750);

        string value = await search.InputValueAsync();

        Assert.Equal(
            expected: "component-search-check",
            actual: value);

        bool hasSearchFilter = await grid.EvaluateAsync<bool>(
            expression: "element => {"
                + "const widget = window.jQuery(element).data('kendoGrid');"
                + "const filter = widget?.dataSource?.filter();"
                + "return Boolean(filter?.filters?.length); }");

        Assert.True(
            condition: hasSearchFilter,
            userMessage: $"{componentName} grid {gridIndex + 1} "
                + "search did not apply a data-source filter.");

        await search.FillAsync(value: string.Empty);
    }
}