// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Tests;

public sealed partial class AdminInformationArchitectureAssetTests
{
    [Fact]
    public void CommonCacheManagementTreeShouldBeAppHostedOnly()
    {
        // Given
        string defaultApp = Path.Combine(
            path1: FindDataDirectory(),
            path2: "Default App");

        string appComponents = Path.Combine(
            path1: defaultApp,
            path2: "App",
            path3: "ContentManagement",
            path4: "Components");

        string commonCacheComponents = Path.Combine(
            path1: defaultApp,
            path2: "Common Cache",
            path3: "ContentManagement",
            path4: "Components");

        string[] componentNames =
        [
            "CommonCacheManagement",
            "CommonCacheComponents",
            "CommonCacheResources",
            "CommonCacheScripts",
        ];

        // When
        var ownership = componentNames.Select(selector: componentName => new
        {
            Name = componentName,
            IsAppHosted = File.Exists(
                path: Path.Combine(
                    path1: appComponents,
                    path2: $"{componentName}.json")),
            IsCommonCached = File.Exists(
                path: Path.Combine(
                    path1: commonCacheComponents,
                    path2: $"{componentName}.json")),
        });

        // Then
        foreach (var component in ownership)
        {
            Assert.True(
                condition: component.IsAppHosted,
                userMessage: $"{component.Name} must be app-hosted.");

            Assert.False(
                condition: component.IsCommonCached,
                userMessage: $"{component.Name} must not live in Common Cache.");
        }
    }
}