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
                        string state = await page.EvaluateAsync<string>(
                            expression: "({ tabName, managerName }) => {"
                                + "const tab = Array.from(document.querySelectorAll("
                                + "`.component[name='AppManagement'] "
                                + "button[data-bs-toggle='tab']`))"
                                + ".find(item => item.textContent.trim() === tabName);"
                                + "const paneName = window.jQuery?.data(tab, "
                                + "'appManagementPane');"
                                + "const pane = document.querySelector("
                                + "`.component[name='AppManagement'] "
                                + ".tab-pane[name='${paneName}']`);"
                                + "const component = pane?.querySelector("
                                + "`.component[name='${managerName}']`);"
                                + "const events = tab && window.jQuery?._data(tab, "
                                + "'events');"
                                + "return JSON.stringify({"
                                + "hasJQuery: Boolean(window.jQuery),"
                                + "hasManager: Boolean(window[managerName]),"
                                + "tabTarget: tab?.getAttribute('data-bs-target'),"
                                + "paneName,"
                                + "paneFound: Boolean(pane),"
                                + "componentFound: Boolean(component),"
                                + "managerLoading: component "
                                + "? window.jQuery(component).data('managerLoading') "
                                + ": null,"
                                + "managerInitialised: component "
                                + "? window.jQuery(component).data("
                                + "'managerInitialised') : null,"
                                + "eventNames: events ? Object.keys(events) : []"
                                + "});"
                                + "}",
                            arg: new { tabName, managerName });

                        throw new TimeoutException(
                            message: $"{managerName} did not initialize from "
                                + $"the {tabName} tab. State={state}",
                            innerException: exception);
                    }
                }
            });

        // Then
    }
}