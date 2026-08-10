// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

[Collection(name: "Published Core UI")]
public sealed partial class CommonCacheEndpointTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitialize()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/CommonCacheEndpoint";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: "CommonCacheEndpoint");

        // Then
    }
}