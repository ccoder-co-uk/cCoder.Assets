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

                Assert.Equal(
                    expected: "API Metadata",
                    actual: await page
                        .Locator(
                            ".component[name=MetadataMenu] "
                                + "> .docs-page-header > h1")
                        .TextContentAsync());

                Assert.Equal(
                    expected: "Browse the API contexts available to this "
                        + "portal and open their service and metadata documents.",
                    actual: await page
                        .Locator(
                            ".component[name=MetadataMenu] "
                                + "> .docs-page-header > p")
                        .TextContentAsync());

                foreach (var context in contexts)
                {
                    ILocator card = page.Locator(
                        selector: ".docs-nav-card"
                            + $"[data-context='{context.Name}']");

                    string contextPath = $"/Api/{context.Url}";
                    string metadataPath = $"{contextPath}/$metadata";
                    string swaggerUiPath = "/swagger/index.html"
                        + $"?urls.primaryName={Uri.EscapeDataString($"{context.Name} API")}";

                    IAPIResponse contextResponse =
                        await page.Context.APIRequest.GetAsync(
                            url: new Uri(
                                baseUri: new Uri(uriString: page.Url),
                                relativeUri: contextPath).ToString());

                    IAPIResponse metadataResponse =
                        await page.Context.APIRequest.GetAsync(
                            url: new Uri(
                                baseUri: new Uri(uriString: page.Url),
                                relativeUri: metadataPath).ToString());

                    IAPIResponse swaggerResponse =
                        await page.Context.APIRequest.GetAsync(
                            url: new Uri(
                                baseUri: new Uri(uriString: page.Url),
                                relativeUri: context.SwaggerDefinition).ToString());

                    IAPIResponse swaggerUiResponse =
                        await page.Context.APIRequest.GetAsync(
                            url: new Uri(
                                baseUri: new Uri(uriString: page.Url),
                                relativeUri: swaggerUiPath).ToString());

                    Assert.Equal(
                        expected: 1,
                        actual: await card.CountAsync());

                    Assert.Equal(
                        expected: contextPath,
                        actual: await page.Locator(
                            selector: ".docs-nav-card"
                                + $"[data-context='{context.Name}'] "
                                + ".metadata-context-link")
                            .Last
                            .GetAttributeAsync(name: "href"));

                    Assert.Equal(
                        expected: metadataPath,
                        actual: await page.Locator(
                            selector: ".docs-nav-card"
                                + $"[data-context='{context.Name}'] "
                                + ".metadata-description-link")
                            .GetAttributeAsync(name: "href"));

                    Assert.Equal(
                        expected: swaggerUiPath,
                        actual: await page.Locator(
                            selector: ".docs-nav-card"
                                + $"[data-context='{context.Name}'] "
                                + ".metadata-swagger-link")
                            .GetAttributeAsync(name: "href"));

                    Assert.Equal(
                        expected: ["Context Root", "OData", "Swagger"],
                        actual: await card
                            .Locator(".docs-nav-command-name")
                            .AllTextContentsAsync());

                    Assert.True(
                        condition: contextResponse.Ok,
                        userMessage: $"{contextPath} returned "
                            + contextResponse.Status);

                    Assert.True(
                        condition: metadataResponse.Ok,
                        userMessage: $"{metadataPath} returned "
                            + metadataResponse.Status);

                    Assert.Contains(
                        expectedSubstring: "Edmx",
                        actualString: await metadataResponse.TextAsync(),
                        comparisonType: StringComparison.Ordinal);

                    Assert.True(
                        condition: swaggerResponse.Ok,
                        userMessage: $"{context.SwaggerDefinition} returned "
                            + swaggerResponse.Status);

                    Assert.Contains(
                        expectedSubstring: "openapi",
                        actualString: await swaggerResponse.TextAsync(),
                        comparisonType: StringComparison.OrdinalIgnoreCase);

                    Assert.True(
                        condition: swaggerUiResponse.Ok,
                        userMessage: $"{swaggerUiPath} returned "
                            + swaggerUiResponse.Status);

                    Assert.Contains(
                        expectedSubstring: "swagger-ui",
                        actualString: await swaggerUiResponse.TextAsync(),
                        comparisonType: StringComparison.OrdinalIgnoreCase);
                }
            });
}
