// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Common;

public sealed partial class DetailedNavTests
{
    [Fact]
    public async Task Navigation_ShouldOpenSelectedAdministrationPage()
    {
        // Given
        const string pagePath = "Admin";

        // When
        await NavigateAsync(pagePath: pagePath);

        // Then
        Assert.True(condition: true);
    }

    private Task NavigateAsync(string pagePath) =>
        driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "DetailedNav",
            action: async page =>
            {
                await page.Locator(
                    selector: ".component[name='DetailedNav'] "
                        + "a[href='/Admin/AppManagement']")
                    .First
                    .ClickAsync();

                await page.WaitForURLAsync(
                    url: url => new Uri(uriString: url).AbsolutePath
                        == "/Admin/AppManagement");

                await page.Locator(
                    selector: ".component[name='AppManagement']")
                    .WaitForAsync();
            });
}