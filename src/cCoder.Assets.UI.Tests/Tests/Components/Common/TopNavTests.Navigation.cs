// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Common;

public sealed partial class TopNavTests
{
    [Fact]
    public async Task Navigation_ShouldHonorShowOnMenusAndExposeValidLinks()
    {
        // Given
        const string componentName = "TopNav";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: string.Empty,
            componentName: componentName,
            action: async page =>
            {
                string[] visiblePaths = await page.EvaluateAsync<string[]>(
                    expression: "async () => { const result = await api.get("
                        + "'ContentManagement/Page?$filter=AppId eq ' "
                        + "+ session.app.Id + ' and ParentId eq null'"
                        + "); return (result.value || []).filter(page => "
                        + "page.ShowOnMenus === true).map(page => '/' "
                        + "+ (page.Path || '')); }");

                string[] renderedPaths = await page.Locator(
                    selector: ".component[name='TopNav'] "
                        + "> nav > .container-fluid > .navbar-collapse "
                        + "> ul[name='menu'] > li > a[href]")
                    .EvaluateAllAsync<string[]>(
                        expression: "links => links.map(link => "
                            + "new URL(link.href).pathname)");

                // Then
                Assert.Equal(
                    expected: visiblePaths.Order(),
                    actual: renderedPaths.Order());

                Assert.All(
                    collection: renderedPaths,
                    action: path => Assert.StartsWith(
                        expectedStartString: "/",
                        actualString: path));
            });
    }
}