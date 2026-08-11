// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;

namespace cCoder.Assets.UI.Tests.Tests.Components.DocumentManagement;

internal static class DocumentManagementGridFixture
{
    internal static async Task ArrangeVisibleFileRowAsync(IPage page)
    {
        string fileName = $"assets-grid-{Guid.NewGuid():N}.txt";

        await page.EvaluateAsync(
            expression: "async fileName => {"
                + "const folder = await api.add('DocumentManagement/Folder', {"
                + "Id: crypto.randomUUID(), AppId: session.app.Id, ParentId: null, "
                + "Name: 'Assets Grid', Path: 'Assets Grid/' + fileName, "
                + "SubFolders: [], Files: [], Roles: [] });"
                + "await api.add('DocumentManagement/File', {"
                + "Id: crypto.randomUUID(), FolderId: folder.Id, Name: fileName, "
                + "Description: 'Playwright grid convention', "
                + "Path: folder.Path + '/' + fileName, MimeType: 'text/plain', "
                + "CreatedBy: 'AssetsAcceptanceAdmin', Size: '4', "
                + "CreatedOn: new Date().toISOString(), Contents: [] });"
                + "await FolderManagement.init(session.app, "
                + "$('.component[name=FolderManagement]'), folder, false); }",
            arg: fileName);

        await page.Locator(
            selector: ".component[name='FolderManagement'] .k-grid tbody > tr")
            .Filter(options: new() { HasText = fileName })
            .WaitForAsync(
                options: new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 15_000
                });
    }
}