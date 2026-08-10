// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Security;

[Collection(name: "Published Core UI")]
public sealed partial class PasswordResetTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitialize()
    {
        // Given
        const string pagePath = "ResetPassword";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: "PasswordReset");

        // Then
    }
}