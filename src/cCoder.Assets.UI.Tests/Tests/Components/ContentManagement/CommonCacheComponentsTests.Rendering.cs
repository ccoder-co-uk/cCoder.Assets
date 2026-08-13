// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

[Collection(name: "Published Core UI")]
public sealed partial class CommonCacheComponentsTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitializeThroughParentComponent()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/CommonCache";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "CommonCacheComponents",
            action: async page =>
            {
                await page.Locator(
                    selector: ".component[name='CommonCacheComponents'] "
                        + ".k-master-row .k-hierarchy-cell")
                    .First.ClickAsync(options: null);

                await ComponentTestDriver.AssertMonacoEditorAsync(
                    page: page,
                    containerSelector: ".component[name='CommonCacheComponents'] "
                        + ".k-detail-row [name='content']",
                    language: "html");

                ILocator scriptTab = page.Locator(
                    selector: ".component[name='CommonCacheComponents'] "
                        + ".k-detail-row:visible "
                        + "button[data-editor-tab='script']");

                await scriptTab.ClickAsync();

                await ComponentTestDriver.AssertMonacoEditorAsync(
                    page: page,
                    containerSelector: ".component[name='CommonCacheComponents'] "
                        + ".k-detail-row [name='script']",
                    language: "javascript");
            });

        // Then
    }
}
