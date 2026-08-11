// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.DocumentManagement;

[Collection(name: "Published Core UI")]
public sealed partial class DocumentManagementTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitialize()
    {
        // Given
        const string pagePath = "Admin/DocumentManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "DocumentManagement",
            action: async page =>
            {
                await ComponentTestDriver.AssertKendoWidgetAsync(
                    page: page,
                    selector: ".component[name='DocumentManagement'] "
                        + "[name='splitter']",
                    widgetName: "kendoSplitter");

                await ComponentTestDriver.AssertKendoWidgetAsync(
                    page: page,
                    selector: ".component[name='DocumentManagement'] "
                        + "[name='treeRoot']",
                    widgetName: "kendoTreeView");

                string[] missingTransientComponents =
                    await page.EvaluateAsync<string[]>(
                        expression: "async names => { const missing = []; "
                            + "for (const name of names) { "
                            + "const probe = document.createElement('div'); "
                            + "document.body.appendChild(probe); "
                            + "const loaded = await loadComponent(probe, name); "
                            + "if (!loaded || typeof loaded.init !== 'function') "
                            + "missing.push(name); probe.remove(); } return missing; }",
                        arg: new[]
                        {
                            "DMSFormatting",
                            "FileActions",
                            "FolderActions",
                            "UploadActions"
                        });

                Assert.True(
                    condition: missingTransientComponents.Length == 0,
                    userMessage: "Document Management did not initialize: "
                        + string.Join(
                            separator: ", ",
                            value: missingTransientComponents)
                        + ".");

                await DocumentManagementGridFixture.ArrangeVisibleFileRowAsync(
                    page: page);
            });

        // Then
    }
}