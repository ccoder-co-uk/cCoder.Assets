// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class MetadataMenuTests
{
    [Fact]
    public Task Navigation_ShouldExposeEveryAuthenticatedContext() =>
        VerifyMetadataNavigationAsync(pagePath: "Admin/Metadata");

    private Task VerifyMetadataNavigationAsync(string pagePath) =>
        driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "MetadataMenu",
            action: async page =>
            {
                IAPIResponse serviceResponse =
                    await page.Context.APIRequest.GetAsync(
                        url: new Uri(
                            baseUri: new Uri(uriString: page.Url),
                            relativeUri: "/Api").ToString());

                Assert.True(condition: serviceResponse.Ok);

                using JsonDocument serviceDocument = JsonDocument.Parse(
                    json: await serviceResponse.TextAsync());

                string[] contexts = serviceDocument.RootElement
                    .GetProperty(propertyName: "value")
                    .EnumerateArray()
                    .Where(
                        predicate: item => item
                            .GetProperty(propertyName: "kind")
                            .GetString() == "Context")
                    .Select(
                        selector: item => item
                            .GetProperty(propertyName: "url")
                            .GetString()!)
                    .OrderBy(
                        keySelector: value => value,
                        comparer: StringComparer.Ordinal)
                    .ToArray();

                Assert.NotEmpty(collection: contexts);

                foreach (string context in contexts)
                {
                    ILocator card = page.Locator(
                        selector: ".metadata-context-card"
                            + $"[data-context='{context}']");

                    string metadataPath = $"/Api/{context}/$metadata";

                    IAPIResponse metadataResponse =
                        await page.Context.APIRequest.GetAsync(
                            url: new Uri(
                                baseUri: new Uri(uriString: page.Url),
                                relativeUri: metadataPath).ToString());

                    Assert.Equal(
                        expected: 1,
                        actual: await card.CountAsync());

                    Assert.Equal(
                        expected: $"/Api/{context}",
                        actual: await page.Locator(
                            selector: ".metadata-context-card"
                                + $"[data-context='{context}'] "
                                + ".metadata-context-link")
                            .Last
                            .GetAttributeAsync(name: "href"));

                    Assert.Equal(
                        expected: metadataPath,
                        actual: await page.Locator(
                            selector: ".metadata-context-card"
                                + $"[data-context='{context}'] "
                                + ".metadata-description-link")
                            .GetAttributeAsync(name: "href"));

                    Assert.True(
                        condition: metadataResponse.Ok,
                        userMessage: $"{metadataPath} returned "
                            + metadataResponse.Status);

                    Assert.Contains(
                        expectedSubstring: "Edmx",
                        actualString: await metadataResponse.TextAsync(),
                        comparisonType: StringComparison.Ordinal);
                }
            });
}