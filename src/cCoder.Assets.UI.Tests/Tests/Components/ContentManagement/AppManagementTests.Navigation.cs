// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class AppManagementTests
{
    [Fact]
    public async Task Navigation_ShouldInitializeTransientComponents()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: "PageManagement",
            navigate: true);

        // Then
    }
}