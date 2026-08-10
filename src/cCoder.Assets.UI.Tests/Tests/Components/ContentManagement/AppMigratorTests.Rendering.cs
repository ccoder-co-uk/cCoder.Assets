// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

[Collection(name: "Published Core UI")]
public sealed partial class AppMigratorTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitializeThroughParentComponent()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: "AppMigrator",
            navigate: true);

        // Then
    }
}