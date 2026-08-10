// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Workflow;

public sealed partial class WorkflowAdminTests
{
    [Fact]
    public async Task Navigation_ShouldInitializeTransientComponents()
    {
        // Given
        const string pagePath = "Admin/Workflows";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: "WorkflowScheduling",
            navigate: true);

        // Then
    }
}