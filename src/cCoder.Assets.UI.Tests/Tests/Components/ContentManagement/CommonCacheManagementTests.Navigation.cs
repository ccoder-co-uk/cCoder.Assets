// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class CommonCacheManagementTests
{
    [Fact]
    public async Task Navigation_ShouldInitializeTransientComponents()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/CommonCache";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: "CommonCacheComponents",
            navigate: true);

        // Then
    }
}
