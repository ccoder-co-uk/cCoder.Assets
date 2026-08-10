// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Logging;

[Collection(name: "Published Core UI")]
public sealed partial class FullLogStreamTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitialize()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/FullLogStream";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "FullLogStream",
            action: async page =>
            {
                await Assertions.Expect(
                    locator: page.Locator(
                        selector: ".component[name='FullLogStream'] "
                            + "[name='logConsole'] > .message"))
                    .Not.ToHaveCountAsync(count: 0);
            });

        // Then
    }
}