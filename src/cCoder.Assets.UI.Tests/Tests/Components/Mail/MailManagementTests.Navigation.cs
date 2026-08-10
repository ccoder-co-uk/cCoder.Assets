// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Mail;

public sealed partial class MailManagementTests
{
    public static IEnumerable<object[]> Tabs =>
    [
        ["Queue", "#mail-queue"],
        ["Sent", "#mail-history"],
        ["Senders", "#mail-senders"],
        ["Receivers", "#mail-receivers"]
    ];

    [Theory]
    [MemberData(memberName: nameof(Tabs))]
    public async Task Navigation_ShouldActivateRequestedTab(
        string tabName,
        string paneSelector)
    {
        // Given
        const string pagePath = "Admin/MailManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "MailManagement",
            action: async page =>
            {
                ILocator component = page.Locator(
                    selector: ".component[name='MailManagement']");

                ILocator tab = component.Locator(
                    selectorOrLocator: $"button[data-bs-target='{paneSelector}']");

                await Assertions.Expect(locator: tab)
                    .ToContainTextAsync(expected: tabName);

                await tab.ClickAsync();

                await Assertions.Expect(
                    locator: component.Locator(
                        selectorOrLocator: paneSelector))
                    .ToHaveClassAsync(
                        expected: new Regex(pattern: "(^|\\s)active(\\s|$)"));
            });

        // Then
    }
}