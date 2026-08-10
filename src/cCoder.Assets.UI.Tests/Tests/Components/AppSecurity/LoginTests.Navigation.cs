// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.AppSecurity;

public sealed partial class LoginTests
{
    [Fact]
    public async Task Navigation_ShouldInitializeRegister()
    {
        // Given
        const string pagePath = "Login";

        // When
        await driver.AssertComponentRendersAsync(
            pagePath: pagePath,
            componentName: "Register",
            navigate: true);

        // Then
    }
}