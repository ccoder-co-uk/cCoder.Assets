// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class PageManagementTests
{
    [Fact]
    public async Task Editing_ShouldDockPageToolbarAboveRenderedPage()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "PageManagement",
            action: async page =>
            {
                await OpenEditorAsync(page: page);

                ILocator workspace = page.Locator(
                    selector: ".component[name='PageManagement'] [name='workspace']");

                ILocator editorFrame = workspace.Locator(
                    selectorOrLocator: "> iframe");

                LocatorBoundingBoxResult? workspaceBounds =
                    await workspace.BoundingBoxAsync();

                LocatorBoundingBoxResult? editorFrameBounds =
                    await editorFrame.BoundingBoxAsync();

                Assert.NotNull(@object: workspaceBounds);
                Assert.NotNull(@object: editorFrameBounds);

                Assert.True(
                    condition: Math.Abs(
                        value: editorFrameBounds.X - workspaceBounds.X) <= 1
                        && Math.Abs(
                            value: editorFrameBounds.Y - workspaceBounds.Y) <= 1,
                    userMessage: "The editor frame retains the workspace's top or left inset.");

                Assert.True(
                    condition: editorFrameBounds.Width >= workspaceBounds.Width - 2,
                    userMessage: "The editor frame does not span the full workspace width.");

                IFrameLocator editor = page.FrameLocator(
                    selector: ".component[name='PageManagement'] "
                        + "[name='workspace'] > iframe");

                ILocator toolbar = editor.Locator(
                    selectorOrLocator: ".pageToolbar");

                await toolbar.WaitForAsync(
                    options: new()
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10_000
                    });

                bool isDraggable = await toolbar.EvaluateAsync<bool>(
                    expression: "element => element.classList.contains('ui-draggable') "
                        + "|| Boolean(window.jQuery(element).data('ui-draggable'))");

                Assert.False(
                    condition: isDraggable,
                    userMessage: "The permanent page toolbar is still draggable.");

                await Assertions.Expect(
                        locator: toolbar.Locator(
                            selectorOrLocator: ".k-editortoolbar-dragHandle"))
                    .ToHaveCountAsync(count: 0);

                string position = await toolbar.EvaluateAsync<string>(
                    expression: "element => getComputedStyle(element).position");

                Assert.False(
                    condition: new[] { "absolute", "fixed", "sticky" }
                        .Contains(
                            value: position,
                            comparer: StringComparer.Ordinal),
                    userMessage: "The permanent page toolbar still floats over the rendered page.");

                ILocator editorBody = editor.Locator(selectorOrLocator: "body");

                LocatorBoundingBoxResult? toolbarBounds =
                    await toolbar.BoundingBoxAsync();

                LocatorBoundingBoxResult? bodyBounds =
                    await editorBody.BoundingBoxAsync();

                LocatorBoundingBoxResult? renderedPageBounds =
                    await editor.Locator(selectorOrLocator: ".site-shell")
                        .BoundingBoxAsync();

                Assert.NotNull(@object: toolbarBounds);
                Assert.NotNull(@object: bodyBounds);
                Assert.NotNull(@object: renderedPageBounds);

                Assert.True(
                    condition: toolbarBounds.Width >= bodyBounds.Width - 2,
                    userMessage: "The permanent page toolbar does not fill the editor panel header.");

                ILocator toolbarContent = toolbar.Locator(
                    selectorOrLocator: ".editorToolbarWindow");

                ILocator toolbarControls = toolbar.Locator(
                    selectorOrLocator: ".k-editor-toolbar");

                string[] contentPadding =
                    await toolbarContent.EvaluateAsync<string[]>(
                        expression: "element => { const style = getComputedStyle(element); "
                            + "return [style.paddingTop, style.paddingRight, "
                            + "style.paddingBottom, style.paddingLeft]; }");

                Assert.Equal(expected: "8px", actual: contentPadding[0]);
                Assert.Equal(expected: "8px", actual: contentPadding[1]);
                Assert.Equal(expected: "8px", actual: contentPadding[2]);
                Assert.Equal(expected: "8px", actual: contentPadding[3]);

                LocatorBoundingBoxResult? toolbarContentBounds =
                    await toolbarContent.BoundingBoxAsync();

                LocatorBoundingBoxResult? toolbarControlBounds =
                    await toolbarControls.BoundingBoxAsync();

                Assert.NotNull(@object: toolbarContentBounds);
                Assert.NotNull(@object: toolbarControlBounds);

                Assert.True(
                    condition: toolbarControlBounds.Width
                        >= toolbarContentBounds.Width - 18,
                    userMessage: "The permanent toolbar controls do not fill the panel header width.");

                Assert.True(
                    condition: toolbarControlBounds.X
                        >= toolbarContentBounds.X + 8
                        && toolbarControlBounds.Y
                        >= toolbarContentBounds.Y + 8,
                    userMessage: "The permanent toolbar controls have lost their internal padding.");

                Assert.True(
                    condition: Math.Abs(value: toolbarBounds.X - bodyBounds.X) <= 1
                        && Math.Abs(value: toolbarBounds.Y - bodyBounds.Y) <= 1,
                    userMessage: "The permanent toolbar retains an outer top or left margin.");

                Assert.True(
                    condition: toolbarBounds.Y + toolbarBounds.Height
                        <= renderedPageBounds.Y,
                    userMessage: "The permanent page toolbar overlaps the rendered page instead of pushing it down.");
            });

        // Then
    }

    [Fact]
    public async Task Editing_ShouldSizeContextualToolbarToItsControlRows()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "PageManagement",
            action: async page =>
            {
                await OpenEditorAsync(page: page);

                IFrameLocator editor = page.FrameLocator(
                    selector: ".component[name='PageManagement'] "
                        + "[name='workspace'] > iframe");

                await editor.Locator(
                        selectorOrLocator:
                            ".ccoder-editable-content[contenteditable]")
                    .First
                    .ClickAsync();

                ILocator contextualToolbar = editor.Locator(
                    selectorOrLocator:
                        ".ccoder-content-editor-toolbar:visible");

                await contextualToolbar.WaitForAsync(
                    options: new()
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10_000
                    });

                await Assertions.Expect(
                        locator: contextualToolbar.Locator(
                            selectorOrLocator: ".ccoder-toolbar-row"))
                    .ToHaveCountAsync(count: 2);

                await Assertions.Expect(
                        locator: contextualToolbar.Locator(
                            selectorOrLocator: ".ccoder-toolbar-row-break"))
                    .ToHaveCountAsync(count: 0);

                LocatorBoundingBoxResult? toolbarBounds =
                    await contextualToolbar.BoundingBoxAsync();

                ILocator rows = contextualToolbar.Locator(
                    selectorOrLocator: ".ccoder-toolbar-row");

                LocatorBoundingBoxResult? firstRowBounds =
                    await rows.First.BoundingBoxAsync();

                LocatorBoundingBoxResult? secondRowBounds =
                    await rows.Last.BoundingBoxAsync();

                Assert.NotNull(@object: toolbarBounds);
                Assert.NotNull(@object: firstRowBounds);
                Assert.NotNull(@object: secondRowBounds);

                double widestRow = Math.Max(
                    val1: firstRowBounds.Width,
                    val2: secondRowBounds.Width);

                Assert.True(
                    condition: toolbarBounds.Width <= widestRow + 20,
                    userMessage: "The contextual toolbar contains excessive empty horizontal space.");

                ILocator viewSource = contextualToolbar.Locator(
                    selectorOrLocator: "button[name='viewSource']");

                bool sourceIsInFirstRow = await rows.First.Locator(
                        selectorOrLocator: "button[name='viewSource']")
                    .CountAsync() == 1;

                Assert.True(
                    condition: sourceIsInFirstRow,
                    userMessage: "View Source is not grouped with the first row of editor controls.");

                await Assertions.Expect(locator: viewSource)
                    .ToHaveCountAsync(count: 1);

                ILocator standardButton = rows.First.Locator(
                        selectorOrLocator: "button.k-toolbar-tool")
                    .First;

                LocatorBoundingBoxResult? sourceBounds =
                    await viewSource.BoundingBoxAsync();

                LocatorBoundingBoxResult? standardButtonBounds =
                    await standardButton.BoundingBoxAsync();

                Assert.NotNull(@object: sourceBounds);
                Assert.NotNull(@object: standardButtonBounds);

                Assert.True(
                    condition: Math.Abs(
                        value: sourceBounds.Width - standardButtonBounds.Width) <= 1
                        && Math.Abs(
                            value: sourceBounds.Height - standardButtonBounds.Height) <= 1,
                    userMessage: "View Source is smaller than the other editor toolbar buttons. "
                        + $"Source={sourceBounds.Width}x{sourceBounds.Height}, "
                        + $"standard={standardButtonBounds.Width}x{standardButtonBounds.Height}.");

                double expectedHeight = firstRowBounds.Height
                    + secondRowBounds.Height
                    + 8
                    + 16;

                Assert.True(
                    condition: toolbarBounds.Height <= expectedHeight + 2,
                    userMessage: "The contextual toolbar contains excessive empty vertical space. "
                        + $"Toolbar={toolbarBounds.Height}, first row={firstRowBounds.Height}, "
                        + $"second row={secondRowBounds.Height}, expected maximum={expectedHeight + 2}.");
            });

        // Then
    }

    [Fact]
    public async Task Editing_ShouldNotLeaveEmptyToolbarWindowWhenContextualToolbarOpens()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "PageManagement",
            action: async page =>
            {
                await OpenEditorAsync(page: page);

                IFrameLocator editor = page.FrameLocator(
                    selector: ".component[name='PageManagement'] "
                        + "[name='workspace'] > iframe");

                await editor.Locator(
                        selectorOrLocator:
                            ".ccoder-editable-content[contenteditable]")
                    .First
                    .ClickAsync();

                await editor.Locator(
                        selectorOrLocator:
                            ".ccoder-content-editor-toolbar:visible")
                    .WaitForAsync(
                        options: new()
                        {
                            State = WaitForSelectorState.Visible,
                            Timeout = 10_000
                        });

                ILocator emptyToolbarWindows = editor.Locator(
                    selectorOrLocator:
                        ".k-window:visible:has(> .editorToolbarWindow:empty)");

                await Assertions.Expect(locator: emptyToolbarWindows)
                    .ToHaveCountAsync(count: 0);
            });

        // Then
    }

    [Fact]
    public async Task Editing_ShouldKeepContextualToolbarAndDragHandleTogether()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "PageManagement",
            action: async page =>
            {
                await OpenEditorAsync(page: page);

                IFrameLocator editor = page.FrameLocator(
                    selector: ".component[name='PageManagement'] "
                        + "[name='workspace'] > iframe");

                ILocator editableContent = editor.Locator(
                        selectorOrLocator:
                            ".ccoder-editable-content[contenteditable]")
                    .First;

                await editableContent.ClickAsync();

                ILocator contextualToolbar = editor.Locator(
                    selectorOrLocator:
                        ".ccoder-content-editor-toolbar:visible");

                await contextualToolbar.WaitForAsync(
                    options: new()
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10_000
                    });

                await Assertions.Expect(
                        locator: contextualToolbar.Locator(
                            selectorOrLocator: ".k-editortoolbar-dragHandle:visible"))
                    .ToHaveCountAsync(count: 1);

                await Assertions.Expect(
                        locator: editor.Locator(
                            selectorOrLocator: ".k-editortoolbar-dragHandle:visible"))
                    .ToHaveCountAsync(count: 1);

                bool isDraggable = await contextualToolbar.EvaluateAsync<bool>(
                    expression: "element => element.classList.contains('ui-draggable') "
                        + "&& Boolean(window.jQuery(element).data('ui-draggable'))");

                Assert.True(
                    condition: isDraggable,
                    userMessage: "The contextual editor toolbar handle does not drag its toolbar.");
            });

        // Then
    }

    [Fact]
    public async Task Editing_ShouldWrapContextualToolbarBeforeFirstDropdown()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "PageManagement",
            action: async page =>
            {
                await OpenEditorAsync(page: page);

                IFrameLocator editor = page.FrameLocator(
                    selector: ".component[name='PageManagement'] "
                        + "[name='workspace'] > iframe");

                await editor.Locator(
                        selectorOrLocator:
                            ".ccoder-editable-content[contenteditable]")
                    .First
                    .ClickAsync();

                ILocator contextualToolbar = editor.Locator(
                    selectorOrLocator:
                        ".ccoder-content-editor-toolbar:visible");

                ILocator lastButtonBeforeDropdown = contextualToolbar.Locator(
                    selectorOrLocator: "[data-command='createTable']");

                ILocator firstDropdown = contextualToolbar.Locator(
                    selectorOrLocator:
                        ".k-toolbar-item[data-command='formatting']");

                LocatorBoundingBoxResult? buttonBounds =
                    await lastButtonBeforeDropdown.BoundingBoxAsync();

                LocatorBoundingBoxResult? dropdownBounds =
                    await firstDropdown.BoundingBoxAsync();

                Assert.NotNull(@object: buttonBounds);
                Assert.NotNull(@object: dropdownBounds);

                Assert.True(
                    condition: dropdownBounds.Y
                        >= buttonBounds.Y + buttonBounds.Height,
                    userMessage: "The contextual editor toolbar did not wrap before its first dropdown.");
            });

        // Then
    }

    [Fact]
    public async Task Editing_ShouldLoadSelectedPageContentAndToolbars()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "PageManagement",
            action: async page =>
            {
                await OpenEditorAsync(page: page);

                ILocator frame = page.Locator(
                    selector: ".component[name='PageManagement'] "
                        + "[name='workspace'] > iframe");

                string? source = await frame.GetAttributeAsync(name: "src");

                Assert.False(
                    condition: string.IsNullOrWhiteSpace(value: source),
                    userMessage: "Selecting a page did not assign the editor frame URL.");

                Uri editorAddress = new(uriString: source);

                Assert.Equal(
                    expected: fixture.WebBaseAddress.Authority,
                    actual: editorAddress.Authority);

                IFrameLocator editor = page.FrameLocator(
                    selector: ".component[name='PageManagement'] "
                        + "[name='workspace'] > iframe");

                ILocator permanentToolbar = editor.Locator(
                    selectorOrLocator: ".pageToolbar");

                await permanentToolbar.WaitForAsync(
                    options: new()
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10_000
                    });

                await permanentToolbar.Locator(
                        selectorOrLocator: "button[name='pageSave']")
                    .WaitForAsync();

                await permanentToolbar.Locator(
                        selectorOrLocator: "input[name='cultureDropdown']")
                    .WaitForAsync(
                        options: new()
                        {
                            State = WaitForSelectorState.Attached
                        });

                await permanentToolbar.Locator(
                        selectorOrLocator: ".k-dropdownlist")
                    .WaitForAsync(
                        options: new()
                        {
                            State = WaitForSelectorState.Visible
                        });

                ILocator editableContent = editor.Locator(
                        selectorOrLocator:
                            ".ccoder-editable-content[contenteditable]")
                    .First;

                await editableContent.WaitForAsync(
                    options: new()
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10_000
                    });

                string borderStyle = await editableContent.EvaluateAsync<string>(
                    expression: "element => getComputedStyle(element).borderStyle");

                Assert.Equal(expected: "dashed", actual: borderStyle);

                await editableContent.ClickAsync();

                ILocator contextualToolbar = editor.Locator(
                    selectorOrLocator:
                        ".ccoder-content-editor-toolbar:visible");

                await Assertions.Expect(locator: contextualToolbar)
                    .ToHaveCountAsync(count: 1);

                LocatorBoundingBoxResult? contentBounds =
                    await editableContent.BoundingBoxAsync();

                LocatorBoundingBoxResult? contextualToolbarBounds =
                    await contextualToolbar.BoundingBoxAsync();

                LocatorBoundingBoxResult? permanentToolbarBounds =
                    await permanentToolbar.BoundingBoxAsync();

                Assert.NotNull(@object: contentBounds);
                Assert.NotNull(@object: contextualToolbarBounds);
                Assert.NotNull(@object: permanentToolbarBounds);

                Assert.True(
                    condition: contextualToolbarBounds.Y
                        + contextualToolbarBounds.Height <= contentBounds.Y,
                    userMessage: "The contextual editor toolbar was not positioned above its content area.");

                Assert.True(
                    condition: permanentToolbarBounds.Y < contentBounds.Y,
                    userMessage: "The permanent page toolbar was not positioned above the editor content.");
            });

        // Then
    }

    [Fact]
    public async Task Editing_ShouldSaveContentAndSwitchCulture()
    {
        // Given
        const string pagePath = "Admin/AppManagement";
        string editedContent = $"Page Management acceptance {Guid.NewGuid():N}";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "PageManagement",
            action: async page =>
            {
                await OpenEditorAsync(page: page);

                IFrameLocator editor = page.FrameLocator(
                    selector: ".component[name='PageManagement'] "
                        + "[name='workspace'] > iframe");

                ILocator editableContent = editor.Locator(
                        selectorOrLocator:
                            ".ccoder-editable-content[contenteditable]")
                    .First;

                await editableContent.WaitForAsync(
                    options: new()
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10_000
                    });

                await editableContent.EvaluateAsync(
                    expression: "(element, content) => { "
                        + "const editor = window.jQuery(element)"
                        + ".data('contentEditor').kendoEditor; "
                        + "editor.value(content); editor.trigger('change'); }",
                    arg: editedContent);

                await editor.Locator(
                        selectorOrLocator: ".pageToolbar button[name='pageSave']")
                    .ClickAsync();

                await editor.Locator(
                        selectorOrLocator: ".k-notification-success:visible")
                    .First
                    .WaitForAsync(
                        options: new()
                        {
                            State = WaitForSelectorState.Visible,
                            Timeout = 10_000
                        });

                IFrame editorFrame = page.Frames.Single(
                    predicate: frame => frame.ParentFrame is not null
                        && frame.Url.Contains(
                            value: "edit=true",
                            comparisonType: StringComparison.Ordinal));

                await editorFrame.GotoAsync(url: editorFrame.Url);

                await editorFrame.GetByText(
                        text: editedContent,
                        options: new() { Exact = true })
                    .WaitForAsync(
                        options: new()
                        {
                            State = WaitForSelectorState.Visible,
                            Timeout = 10_000
                        });

                await editorFrame.WaitForFunctionAsync(
                    expression: "() => Boolean(window.jQuery("
                        + "'[name=cultureDropdown]')"
                        + ".data('kendoDropDownList'))");

                Task cultureNavigation = editorFrame.WaitForURLAsync(
                    url: url => new Uri(uriString: url).Query.Contains(
                        value: "culture=",
                        comparisonType: StringComparison.Ordinal));

                await editor.Locator(
                        selectorOrLocator: "[name='cultureDropdown']")
                    .EvaluateAsync(
                        expression: "element => window.jQuery(element)"
                            + ".data('kendoDropDownList').trigger('change')");

                await cultureNavigation;

                Assert.Contains(
                    expectedSubstring: "culture=",
                    actualString: editorFrame.Url);
            });

        // Then
    }

    private static async Task OpenEditorAsync(IPage page)
    {
        ILocator pagesTab = page.GetByRole(
            role: AriaRole.Tab,
            options: new() { Name = "Pages", Exact = true });

        await pagesTab.ClickAsync();

        ILocator tree = page.Locator(
            selector: ".component[name='PageManagement'] .pageTree");

        await tree.Locator(selectorOrLocator: "[role='treeitem']")
            .First
            .WaitForAsync(
                options: new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 10_000
                });

        ILocator pageNode = tree.GetByText(
            text: "Welcome to your cCoder platform",
            options: new() { Exact = true });

        await pageNode.ClickAsync();

        await page.Locator(
                selector: ".component[name='PageManagement'] "
                    + "[name='workspace'] > iframe")
            .WaitForAsync(
                options: new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 10_000
                });
    }
}