// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.DocumentManagement;

public sealed partial class DocumentManagementTests
{
    [Fact]
    public async Task Navigation_ShouldInitializeFolderManagement()
    {
        // Given
        const string pagePath = "Admin/DocumentManagement";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: "FolderManagement",
            navigate: true);

        // Then
    }
}