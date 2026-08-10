// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Common;

[Collection(name: "Published Core UI")]
public sealed partial class TopNavTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitialize()
    {
        // Given
        const string componentName = "TopNav";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: string.Empty,
            componentName: componentName,
            action: async page =>
            {
                ILocator links = page.Locator(
                    selector: ".component[name='TopNav'] "
                        + "ul[name='menu'] a[href]");

                await links.First.WaitForAsync();

                // Then
                Assert.True(condition: await links.CountAsync() > 0);
            });
    }
}