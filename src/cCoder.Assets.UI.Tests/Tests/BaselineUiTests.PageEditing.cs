// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests;

public sealed partial class BaselineUiTests
{
    [Fact]
    public async Task PageEditing_ShouldRenderSaveAndSwitchCulture()
    {
        // Given
        IPage page = await fixture.NewPageAsync();
        string editedContent = $"Editor acceptance {Guid.NewGuid():N}";

        try
        {
            await LoginAsInitialAdministratorAsync(page: page);

            IResponse? response = await page.GotoAsync(
                url: new Uri(
                    baseUri: fixture.WebBaseAddress,
                    relativeUri: "?edit=true")
                    .ToString());

            // When
            Assert.NotNull(@object: response);
            Assert.True(condition: response.Ok);

            ILocator editorBundle = page.Locator(
                selector: "script[src$='/editor.js'], "
                    + "script[src$='/editor.min.js']");

            // Then
            await Assertions.Expect(locator: editorBundle)
                .ToHaveCountAsync(count: 1);

            ILocator pageToolbar = page.Locator(selector: ".pageToolbar");

            await pageToolbar.WaitForAsync(
                options: new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 10_000
                });

            ILocator editableContent = page.Locator(
                    selector: "[contenteditable]")
                .First;

            await editableContent.WaitForAsync(
                options: new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible
                });

            await page.Locator(selector: ".k-editor-toolbar:visible")
                .First
                .WaitForAsync(
                    options: new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible
                    });

            await page.WaitForFunctionAsync(
                expression: "() => Boolean(window.currentContentWidget?.kendoEditor) "
                    + "&& Boolean(window.jQuery('[name=cultureDropdown]')"
                    + ".data('kendoDropDownList'))");

            await page.EvaluateAsync(
                expression: "content => { "
                    + "window.currentContentWidget.kendoEditor.value(content); "
                    + "window.currentContentWidget.pageContent.Html = content; "
                    + "}",
                arg: editedContent);

            await pageToolbar.Locator(
                    selectorOrLocator: "button[name=pageSave]")
                .ClickAsync();

            await page.Locator(selector: ".k-notification-success:visible")
                .First
                .WaitForAsync(
                    options: new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10_000
                    });

            await page.ReloadAsync();

            await page.GetByText(text: editedContent, options: new() { Exact = true })
                .WaitForAsync(
                    options: new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10_000
                    });

            Task cultureNavigation = page.WaitForURLAsync(
                url: url => new Uri(uriString: url).Query.Contains(
                    value: "culture=",
                    comparisonType: StringComparison.Ordinal));

            await page.EvaluateAsync(
                expression: "() => window.jQuery('[name=cultureDropdown]')"
                    + ".data('kendoDropDownList').trigger('change')");

            await cultureNavigation;
        }
        finally
        {
            await fixture.ClosePageAsync(page: page);
        }
    }
}