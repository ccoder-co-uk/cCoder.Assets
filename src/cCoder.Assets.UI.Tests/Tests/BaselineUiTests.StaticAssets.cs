// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests;

public sealed partial class BaselineUiTests
{
    [Fact]
    public async Task FirstTimeSetup_ShouldReferenceCacheableStaticBundles()
    {
        // Given
        IPage page = await fixture.NewPageAsync();

        try
        {
            // When
            IAPIResponse response = await page.Context.APIRequest.GetAsync(
                url: fixture.WebBaseAddress.ToString());

            string content = await response.TextAsync();

            IAPIResponse frameworkResponse = await page.Context.APIRequest.GetAsync(
                url: new Uri(
                    baseUri: fixture.WebBaseAddress,
                    relativeUri: "framework.min.js")
                    .ToString());

            // Then
            Assert.True(condition: response.Ok);

            Assert.Contains(
                expectedSubstring: "src=\"/framework.min.js\"",
                actualString: content);

            Assert.Contains(
                expectedSubstring: "<link rel=\"stylesheet\" href=\"/everything.min.css\"",
                actualString: content);

            Assert.DoesNotContain(
                expectedSubstring: "/editor.min.js",
                actualString: content);

            Assert.DoesNotContain(
                expectedSubstring: "/workflow.min.js",
                actualString: content);

            Assert.DoesNotContain(
                expectedSubstring: "/code-editor.min.js",
                actualString: content);

            Assert.True(condition: frameworkResponse.Ok);

            Assert.Contains(
                expectedSubstring: "public",
                actualString: frameworkResponse.Headers["cache-control"]);

            Assert.Contains(
                expectedSubstring: "max-age=86400",
                actualString: frameworkResponse.Headers["cache-control"]);

            Assert.True(
                condition: content.Length < 500_000,
                userMessage: $"Rendered homepage contained {content.Length:N0} characters.");
        }
        finally
        {
            await fixture.ClosePageAsync(page: page);
        }
    }
}
