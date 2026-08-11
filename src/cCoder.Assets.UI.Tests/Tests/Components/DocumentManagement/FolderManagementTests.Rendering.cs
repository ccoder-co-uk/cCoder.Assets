// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.DocumentManagement;

[Collection(name: "Published Core UI")]
public sealed partial class FolderManagementTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitializeThroughParentComponent()
    {
        // Given
        const string pagePath = "Admin/DocumentManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "FolderManagement",
            action: async page =>
            {
                await DocumentManagementGridFixture.ArrangeVisibleFileRowAsync(
                    page: page);
            });

        // Then
    }
}