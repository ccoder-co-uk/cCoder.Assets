// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.ContentManagement;

public sealed partial class ComponentManagementTests
{
    [Fact]
    public async Task Actions_ShouldCreateSaveAndDeleteComponent()
    {
        // Given
        const string pagePath = "Admin/AppManagement";
        string componentName = $"AcceptanceComponent{Guid.NewGuid():N}";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "ComponentManagement",
            action: async page =>
            {
                ILocator componentsTab = page.GetByRole(
                    role: AriaRole.Tab,
                    options: new() { Name = "Components", Exact = true });

                await componentsTab.ClickAsync();

                ILocator component = page.Locator(
                    selector: ".component[name='ComponentManagement']");

                ILocator createButton = component.Locator(
                    selectorOrLocator: ".k-grid-toolbar button[name='create']");

                await createButton.ClickAsync();

                ILocator dialog = page.Locator(selector: ".k-window:visible");
                ILocator inputs = dialog.Locator(selectorOrLocator: "input");

                ILocator nameInput = inputs.Nth(index: 0);
                ILocator keyInput = inputs.Nth(index: 1);
                ILocator resourceKeyInput = inputs.Nth(index: 2);

                await nameInput.FillAsync(value: componentName);
                await keyInput.FillAsync(value: "Acceptance");
                await resourceKeyInput.FillAsync(value: "ContentManagement");

                ILocator confirmCreate = dialog.GetByRole(
                    role: AriaRole.Button,
                    options: new() { Name = "confirm", Exact = true });

                await confirmCreate.ClickAsync();

                ILocator rows = component.Locator(
                    selectorOrLocator: ".k-master-row");

                ILocator row = rows.Filter(
                    options: new() { HasText = componentName });

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

                ILocator confirmation = page.Locator(selector: ".k-window:visible");

                ILocator confirmDelete = confirmation.GetByRole(
                    role: AriaRole.Button,
                    options: new() { Name = "confirm", Exact = true });

                await confirmDelete.ClickAsync();

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
            componentName: "ComponentManagement",
            action: async page =>
            {
                ILocator componentsTab = page.GetByRole(
                    role: AriaRole.Tab,
                    options: new() { Name = "Components", Exact = true });

                await componentsTab.ClickAsync();

                ILocator buttons = page.Locator(
                    selector: ".component[name='ComponentManagement'] "
                        + ".k-grid-toolbar button");

                ILocator firstButton = buttons.First;

                await firstButton.WaitForAsync(
                    options: new() { State = WaitForSelectorState.Visible });

                Assert.True(
                    condition: await buttons.CountAsync() >= 2,
                    userMessage: "The Component toolbar needs multiple actions.");

                float gap = await buttons.EvaluateAllAsync<float>(
                    expression: "buttons => buttons[1].getBoundingClientRect().left "
                        + "- buttons[0].getBoundingClientRect().right");

                Assert.True(
                    condition: gap >= 8,
                    userMessage: $"Component toolbar action gap is only {gap:F1}px.");
            });

        // Then
    }
}