// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests;

public sealed partial class BaselineUiTests
{
    [Fact]
    public async Task FirstTimeSetup_ShouldExposeWorkflowMetadataRequiredByComponents()
    {
        // Given
        IPage page = await fixture.NewPageAsync();
        await LoginAsInitialAdministratorAsync(page: page);

        // When
        IAPIResponse response = await page.Context.APIRequest.GetAsync(
            url: new Uri(
                baseUri: fixture.WebBaseAddress,
                relativeUri: "/Api/GetMetadata")
                .ToString());

        // Then
        Assert.True(condition: response.Ok);

        string metadata = await response.TextAsync();

        Assert.Contains(
            expectedSubstring: "\"ServerTypeName\":\"FlowDefinition\"",
            actualString: metadata,
            comparisonType: StringComparison.Ordinal);

        await fixture.ClosePageAsync(page: page);
    }
}