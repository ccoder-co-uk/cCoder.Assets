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

            await AssertVisibleGridRowAsync(
                grid: grid,
                componentName: componentName,
                gridIndex: index);

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

    private static async Task AssertVisibleGridRowAsync(
        ILocator grid,
        string componentName,
        int gridIndex)
    {
        string[] failures = await grid.EvaluateAsync<string[]>(
            expression: "element => {"
                + "const widget = window.jQuery(element).data('kendoGrid');"
                + "if (!widget) return ['Kendo Grid was not initialized'];"
                + "const rows = [...element.querySelectorAll("
                + "'tbody > tr:not(.k-detail-row):not(.k-grouping-row):not(.k-grid-norecords)')]"
                + ".filter(row => row.getClientRects().length > 0 "
                + "&& row.closest('.k-grid') === element);"
                + "if (!rows.length) return ['no visible data row was rendered'];"
                + "const row = rows[0];"
                + "const dataItem = widget.dataItem(row);"
                + "if (!dataItem) return ['the first visible row has no data item'];"
                + "const cells = [...row.children].filter(cell => "
                + "cell.matches('td:not(.k-hierarchy-cell)') "
                + "&& cell.getClientRects().length > 0);"
                + "const visibleColumns = (widget.columns || []).filter(column => "
                + "!column.hidden);"
                + "const groups = widget.dataSource.group() || [];"
                + "const groupedFields = groups.map(group => group.field);"
                + "const failures = [];"
                + "if (cells.length < visibleColumns.length) failures.push("
                + "`the first row has ${cells.length} visible cells for "
                + "${visibleColumns.length} visible columns`);"
                + "visibleColumns.forEach((column, columnIndex) => {"
                + "if (!column.field) return;"
                + "const cell = cells[columnIndex];"
                + "if (!cell) { failures.push(`${column.field} has no cell`); return; }"
                + "const text = (cell.textContent || '').trim();"
                + "const value = dataItem.get ? dataItem.get(column.field) "
                + ": dataItem[column.field];"
                + "const hasVisualValue = Boolean(cell.querySelector("
                + "'input, img, svg, .k-icon, .k-svg-icon'));"
                + "if (groupedFields.includes(column.field)) {"
                + "const groupText = [...element.querySelectorAll("
                + "'.k-grouping-row, .k-table-group-row')]"
                + ".filter(group => group.getClientRects().length > 0)"
                + ".map(group => group.textContent || '').join(' ');"
                + "if (value !== null && value !== undefined "
                + "&& !groupText.includes(String(value))) failures.push("
                + "`${column.field} is absent from its group header`);"
                + "return; }"
                + "if (value !== null && value !== undefined && value !== '' "
                + "&& !text && !hasVisualValue) failures.push("
                + "`${column.field} has data but renders empty`);"
                + "});"
                + "const rendered = row.innerHTML;"
                + "if (/\\[\\[(?:Missing|Unresolved)|\\[(?:component|resource|script|style|meta)\\[/i.test(rendered)) "
                + "failures.push('the first row contains an unresolved placeholder');"
                + "if (/\\bundefined\\b|\\[object Object\\]/i.test(row.textContent || '')) "
                + "failures.push('the first row contains an invalid display value');"
                + "const actions = [...row.querySelectorAll("
                + "'button, a')].filter(action => "
                + "action.getClientRects().length > 0);"
                + "actions.forEach(action => {"
                + "const name = (action.textContent || action.getAttribute('aria-label') "
                + "|| action.getAttribute('title') || '').trim();"
                + "if (!name) failures.push('a row action has no accessible name');"
                + "});"
                + "return failures; }");

        Assert.True(
            condition: failures.Length == 0,
            userMessage: $"{componentName} grid {gridIndex + 1} first row: "
                + string.Join(separator: "; ", value: failures));
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

            string[] childFailures = await detailRow.EvaluateAsync<string[]>(
                expression: "element => {"
                    + "const failures = [];"
                    + "const rendered = element.cloneNode(true);"
                    + "rendered.querySelectorAll("
                    + "'code, pre, script, style, textarea, .monaco-editor')"
                    + ".forEach(source => source.remove());"
                    + "const html = rendered.innerHTML;"
                    + "if (/\\[\\[(?:Missing|Unresolved)|\\[(?:component|resource|script|style|meta)\\[/i.test(html)) "
                    + "failures.push('contains an unresolved placeholder');"
                    + "if (element.querySelector('[data-component-error], .component-error')) "
                    + "failures.push('contains a component error');"
                    + "const actions = [...element.querySelectorAll('button, a')]"
                    + ".filter(action => action.getClientRects().length > 0);"
                    + "actions.forEach(action => {"
                    + "const name = (action.textContent || action.getAttribute('aria-label') "
                    + "|| action.getAttribute('title') || '').trim();"
                    + "if (!name) failures.push('contains an unnamed child action');"
                    + "});"
                    + "return failures; }");

            Assert.True(
                condition: childFailures.Length == 0,
                userMessage: $"{componentName} grid {gridIndex + 1} "
                    + "expanded child: "
                    + string.Join(separator: "; ", value: childFailures));

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