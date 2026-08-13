// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

[Collection(name: "Published Core UI")]
public sealed partial class CommonCacheScriptsTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitializeThroughParentComponent()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/CommonCacheManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "CommonCacheScripts",
            action: async page =>
            {
                await page.GetByRole(
                    role: Microsoft.Playwright.AriaRole.Tab,
                    options: new() { Name = "scripts" })
                    .ClickAsync(options: null);

                await page.Locator(
                    selector: ".component[name='CommonCacheScripts'] "
                        + ".k-master-row .k-hierarchy-cell")
                    .First.ClickAsync(options: null);

                await ComponentTestDriver.AssertMonacoEditorAsync(
                    page: page,
                    containerSelector: ".component[name='CommonCacheScripts'] "
                        + ".k-detail-row [name='script']",
                    language: "javascript");
            });

        // Then
    }
}