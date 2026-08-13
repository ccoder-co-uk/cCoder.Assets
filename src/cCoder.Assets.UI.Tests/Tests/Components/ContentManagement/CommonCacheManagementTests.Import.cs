// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class CommonCacheManagementTests
{
    [Fact]
    public async Task Import_ShouldPostCommonObjectsThroughCanonicalEndpoint()
    {
        // Given
        const string pagePath = "Admin/PlatformAdmin/CommonCacheManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "CommonCacheManagement",
            action: async page =>
            {
                JsonElement request = await page.EvaluateAsync<JsonElement>(
                    expression: """
                        async () => {
                            const originalGet = window.api.get;
                            const apiPrototype = Object.getPrototypeOf(
                                window.api);
                            const originalLogin = apiPrototype.login;
                            const originalPost = apiPrototype.post;

                            try {
                                window.api.get = async () => ({
                                    value: [{
                                        Id: 17,
                                        Type: "ContentManagement/Component",
                                        Name: "AcceptanceComponent",
                                        Json: "{}"
                                    }]
                                });

                                let request;

                                apiPrototype.login = async function() { };
                                apiPrototype.post = async function(
                                    path,
                                    payload) {
                                    request = { path, payload };
                                };

                                const typeGrid = {
                                    select: () => [{
                                        Type: "ContentManagement/Component"
                                    }]
                                };

                                const app = {
                                    Config: {
                                        Deployment: {
                                            Targets: [{
                                                EnvironmentName: "Acceptance",
                                                Api: window.location.origin
                                                    + "/Api/"
                                            }]
                                        }
                                    }
                                };

                                await window.CommonCacheManagement.doMigration(
                                    typeGrid,
                                    "Acceptance",
                                    { User: "acceptance", Pass: "acceptance" },
                                    app);

                                return request;
                            } finally {
                                window.api.get = originalGet;
                                apiPrototype.login = originalLogin;
                                apiPrototype.post = originalPost;
                            }
                        }
                        """);

                // Then
                Assert.Equal(
                    expected: "ContentManagement/CommonObject",
                    actual: request.GetProperty(propertyName: "path")
                        .GetString());

                JsonElement payload = request.GetProperty(
                    propertyName: "payload");

                Assert.Equal(
                    expected: JsonValueKind.Array,
                    actual: payload.ValueKind);

                Assert.Single(
                    collection: payload.EnumerateArray());
            });
    }
}