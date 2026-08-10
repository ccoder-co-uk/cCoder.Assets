// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Common;

[Collection(name: "Published Core UI")]
public sealed partial class DetailedNavTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Theory]
    [InlineData("Admin")]
    [InlineData("Admin/PlatformAdmin")]
    public async Task Rendering_ShouldInitialize(string pagePath)
    {
        // Given
        const string componentName = "DetailedNav";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: componentName);

        // Then
        Assert.True(condition: true);
    }
}