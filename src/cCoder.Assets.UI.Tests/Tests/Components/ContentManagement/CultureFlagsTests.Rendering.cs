// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

[Collection(name: "Published Core UI")]
public sealed partial class CultureFlagsTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitialize()
    {
        // Given
        const string componentName = "CultureFlags";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: string.Empty,
            componentName: componentName,
            action: async page =>
            {
                ILocator flags = page.Locator(
                    selector: ".component[name='CultureFlags'] "
                        + "[name='flags']");

                await flags.WaitForAsync(
                    options: new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Attached
                    });

                // Then
                Assert.True(
                    condition: await page.EvaluateAsync<bool>(
                        expression: "() => Boolean(window.CultureFlags) "
                            + "&& typeof CultureFlags.init === 'function' "
                            + "&& typeof CultureFlags.setCulture === 'function'"));
            });
    }
}