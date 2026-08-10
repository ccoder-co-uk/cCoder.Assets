// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Workflow;

[Collection(name: "Published Core UI")]
public sealed partial class FlowInstanceDetailsTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitializeThroughParentComponent()
    {
        // Given
        const string pagePath = "Admin/Workflows";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: "FlowInstanceDetails",
            navigate: true);

        // Then
    }
}