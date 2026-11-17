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

                var contexts = serviceDocument.RootElement
                    .GetProperty(propertyName: "value")
                    .EnumerateArray()
                    .Where(
                        predicate: item => item
                            .GetProperty(propertyName: "kind")
                            .GetString() == "Context")
                    .Select(
                        selector: item => new
                        {
                            Name = item
                                .GetProperty(propertyName: "name")
                                .GetString()!,
                            Url = item
                                .GetProperty(propertyName: "url")
                                .GetString()!,
                            SwaggerDefinition = item
                                .GetProperty(propertyName: "swaggerDef")
                                .GetString()!,
                        })
                    .OrderBy(
                        keySelector: value => value.Name,
                        comparer: StringComparer.Ordinal)
                    .ToArray();

                Assert.NotEmpty(collection: contexts);

                foreach (var context in contexts)
                {
                    ILocator card = page.Locator(
                        selector: ".metadata-context-card"
                            + $"[data-context='{context.Name}']");

                    string contextPath = $"/Api/{context.Url}";
                    string metadataPath = $"{contextPath}/$metadata";

                    IAPIResponse metadataResponse =
                        await page.Context.APIRequest.GetAsync(
                            url: new Uri(
                                baseUri: new Uri(uriString: page.Url),
                                relativeUri: metadataPath).ToString());

                    Assert.Equal(
                        expected: 1,
                        actual: await card.CountAsync());

                    Assert.Equal(
                        expected: contextPath,
                        actual: await page.Locator(
                            selector: ".metadata-context-card"
                                + $"[data-context='{context.Name}'] "
                                + ".metadata-context-link")
                            .Last
                            .GetAttributeAsync(name: "href"));

                    Assert.Equal(
                        expected: metadataPath,
                        actual: await page.Locator(
                            selector: ".metadata-context-card"
                                + $"[data-context='{context.Name}'] "
                                + ".metadata-description-link")
                            .GetAttributeAsync(name: "href"));

                    Assert.Equal(
                        expected: context.SwaggerDefinition,
                        actual: await page.Locator(
                            selector: ".metadata-context-card"
                                + $"[data-context='{context.Name}'] "
                                + ".metadata-swagger-link")
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
