// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.DocumentManagement;

public sealed partial class DocumentManagementTests
{
    [Fact]
    public async Task DragDrop_ShouldMoveFolderAndPersistParent()
    {
        // Given
        const string pagePath = "Admin/DocumentManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "DocumentManagement",
            action: async page =>
            {
                string sourceName = $"Move Source {Guid.NewGuid():N}";
                string targetName = $"Move Target {Guid.NewGuid():N}";

                string[] ids = await page.EvaluateAsync<string[]>(
                    expression: "async names => {"
                        + "const create = name => api.add('DocumentManagement/Folder', {"
                        + "Id: crypto.randomUUID(), AppId: session.app.Id, ParentId: null, "
                        + "Name: name, Path: name, SubFolders: [], Files: [], Roles: [] });"
                        + "const source = await create(names[0]);"
                        + "const target = await create(names[1]);"
                        + "await DocumentManagement.init(session.app, "
                        + "$('.component[name=DocumentManagement]'));"
                        + "return [source.Id, target.Id]; }",
                    arg: new[] { sourceName, targetName });

                ILocator tree = page.Locator(
                    selector: ".component[name='DocumentManagement'] [name='treeRoot']");

                ILocator source = tree.Locator(
                    selectorOrLocator: $".document-tree-node-text:text-is('{sourceName}')");

                ILocator target = tree.Locator(
                    selectorOrLocator: $".document-tree-node-text:text-is('{targetName}')");

                await source.DragToAsync(
                    target: target,
                    options: new LocatorDragToOptions());

                await page.WaitForFunctionAsync(
                    expression: "async args => (await api.get("
                        + "'DocumentManagement/Folder(' + args.sourceId + ')')).ParentId "
                        + "=== args.targetId",
                    arg: new { sourceId = ids[0], targetId = ids[1] },
                    options: new() { Timeout = 10000 });

                string? parentId = await page.EvaluateAsync<string?>(
                    expression: "async id => (await api.get("
                        + "'DocumentManagement/Folder(' + id + ')')).ParentId",
                    arg: ids[0]);

                Assert.Equal(expected: ids[1], actual: parentId);
            });

        // Then
    }
}
