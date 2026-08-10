// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class AppManagementTests
{
    [Fact]
    public async Task Navigation_ShouldInitializeTransientComponents()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        Dictionary<string, string> managersByTab = new()
        {
            ["Pages"] = "PageManagement",
            ["Theming"] = "AppTheming",
            ["Cultures"] = "CultureManagement",
            ["Layouts"] = "LayoutManagement",
            ["Templates"] = "TemplateManagement",
            ["Components"] = "ComponentManagement",
            ["Resources"] = "ResourceManagement",
            ["roles"] = "RoleManagement"
        };

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "PageManagement",
            action: async page =>
            {
                foreach ((string tabName, string managerName) in managersByTab)
                {
                    await page.GetByRole(
                        role: AriaRole.Tab,
                        options: new() { Name = tabName, Exact = true })
                        .ClickAsync();

                    try
                    {
                        await page.WaitForFunctionAsync(
                            expression: "managerName => Boolean(window.jQuery) "
                                + "&& window.jQuery(`.component[name='${managerName}']`)"
                                + ".first().data('managerInitialised') === true",
                            arg: managerName,
                            options: new() { Timeout = 15_000 });
                    }
                    catch (TimeoutException exception)
                    {
                        throw new TimeoutException(
                            message: $"{managerName} did not initialize from the {tabName} tab.",
                            innerException: exception);
                    }
                }
            });

        // Then
    }
}