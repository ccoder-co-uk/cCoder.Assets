// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Workflow;

public sealed partial class WorkflowManagementTests
{
    [Theory]
    [InlineData("FlowInstanceManagement")]
    [InlineData("FlowInstanceDetails")]
    public async Task Navigation_ShouldInitializeTransientComponent(
        string componentName)
    {
        // Given
        const string pagePath = "Admin/Workflows/Editor";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: componentName,
            navigate: true);

        // Then
    }
}