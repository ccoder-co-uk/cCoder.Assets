// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.DocumentManagement;

public sealed partial class DocumentManagementTests
{
    [Fact]
    public async Task Files_ShouldLoadForSelectedFolder()
    {
        // Given
        const string pagePath = "Admin/DocumentManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "DocumentManagement",
            action: async page =>
            {
                string fileName = $"assets-file-{Guid.NewGuid():N}.txt";

                await page.EvaluateAsync(
                    expression: "async fileName => {"
                        + "const folder = await api.add('DocumentManagement/Folder', {"
                        + "Id: crypto.randomUUID(), AppId: session.app.Id, ParentId: null, "
                        + "Name: 'Assets Files', Path: 'Assets Files', SubFolders: [], "
                        + "Files: [], Roles: [] });"
                        + "await api.add('DocumentManagement/File', {"
                        + "Id: crypto.randomUUID(), FolderId: folder.Id, Name: fileName, "
                        + "Description: 'Playwright file listing', "
                        + "Path: folder.Path + '/' + fileName, MimeType: 'text/plain', "
                        + "CreatedBy: 'AssetsAcceptanceAdmin', Size: '4', "
                        + "CreatedOn: new Date().toISOString(), Contents: [] });"
                        + "const persisted = (await api.get("
                        + "'DocumentManagement/File?$filter=FolderId eq ' + folder.Id)).value;"
                        + "if (!persisted.some(file => file.Name === fileName)) "
                        + "throw new Error('Seeded file was not returned by the API.');"
                        + "await FolderManagement.init(session.app, "
                        + "$('.component[name=FolderManagement]'), folder, false); }",
                    arg: fileName);

                ILocator row = page.Locator(
                    selector: ".component[name='FolderManagement'] "
                        + ".k-grid tbody > tr")
                    .Filter(options: new() { HasText = fileName });

                await Assertions.Expect(locator: row)
                    .ToHaveCountAsync(
                        count: 1,
                        options: new() { Timeout = 15_000 });
            });

        // Then
    }
}