// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Workflow;

[Collection(name: "Published Core UI")]
public sealed partial class WorkflowAdminTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitialize()
    {
        // Given
        const string pagePath = "Admin/Workflows";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "WorkflowAdmin",
            action: async page =>
            {
                await WorkflowGridFixture.ArrangeVisibleFlowAsync(
                    page: page,
                    componentName: "WorkflowAdmin");
            });

        // Then
    }
}