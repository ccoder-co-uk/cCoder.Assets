// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class LayoutManagementTests
{
    [Fact]
    public async Task Actions_ShouldCreateSaveAndDeleteLayout()
    {
        // Given
        const string pagePath = "Admin/AppManagement";
        string layoutName = $"AcceptanceLayout{Guid.NewGuid():N}";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "LayoutManagement",
            action: async page =>
            {
                ILocator layoutsTab = page.GetByRole(
                    role: AriaRole.Tab,
                    options: new() { Name = "Layouts", Exact = true });

                await layoutsTab.ClickAsync();

                ILocator component = page.Locator(
                    selector: ".component[name='LayoutManagement']");

                ILocator createButton = component.Locator(
                    selectorOrLocator: ".k-grid-toolbar button[name='create']");

                await createButton.ClickAsync();

                ILocator dialog = page.Locator(selector: ".k-window:visible");

                ILocator nameInput = dialog.Locator(
                    selectorOrLocator: "input[name='name']");

                await nameInput
                    .FillAsync(value: layoutName);

                ILocator confirmCreate = dialog.GetByRole(
                    role: AriaRole.Button,
                    options: new() { Name = "create", Exact = true });

                await confirmCreate.ClickAsync();

                ILocator rows = component.Locator(
                    selectorOrLocator: ".k-master-row");

                ILocator row = rows.Filter(
                    options: new() { HasText = layoutName });

                await row.WaitForAsync(
                    options: new() { State = WaitForSelectorState.Visible });

                ILocator saveButton = row
                    .GetByRole(
                        role: AriaRole.Button,
                        options: new() { Name = "save", Exact = true });

                await saveButton.ClickAsync();

                ILocator successNotification = page.Locator(
                    selector: ".k-notification-success:visible");

                await successNotification.WaitForAsync();

                ILocator deleteButton = row
                    .GetByRole(
                        role: AriaRole.Button,
                        options: new() { Name = "delete", Exact = true });

                await deleteButton.ClickAsync();

                ILocatorAssertions rowAssertions = Assertions.Expect(locator: row);

                await rowAssertions.ToHaveCountAsync(count: 0);
            });

        // Then
    }

    [Fact]
    public async Task Toolbar_ShouldSpaceMultipleActions()
    {
        // Given
        const string pagePath = "Admin/AppManagement";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "LayoutManagement",
            action: async page =>
            {
                ILocator layoutsTab = page.GetByRole(
                    role: AriaRole.Tab,
                    options: new() { Name = "Layouts", Exact = true });

                await layoutsTab.ClickAsync();

                ILocator buttons = page.Locator(
                    selector: ".component[name='LayoutManagement'] "
                        + ".k-grid-toolbar button");

                ILocator firstButton = buttons.First;

                await firstButton.WaitForAsync(
                    options: new() { State = WaitForSelectorState.Visible });

                Assert.True(
                    condition: await buttons.CountAsync() >= 2,
                    userMessage: "The Layout toolbar needs multiple actions.");

                float gap = await buttons.EvaluateAllAsync<float>(
                    expression: "buttons => buttons[1].getBoundingClientRect().left "
                        + "- buttons[0].getBoundingClientRect().right");

                Assert.True(
                    condition: gap >= 8,
                    userMessage: $"Layout toolbar action gap is only {gap:F1}px.");
            });

        // Then
    }
}