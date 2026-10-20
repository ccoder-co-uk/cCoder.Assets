// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components.Workflow;

[Collection(name: "Published Core UI")]
public sealed partial class FlowEditorTests(PublishedCoreFixture fixture)
{
    private readonly ComponentTestDriver driver = new(fixture: fixture);

    [Fact]
    public async Task Rendering_ShouldInitialize()
    {
        // Given
        const string pagePath = "Admin/Workflows";

        // When
        await driver.AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: "FlowEditor",
            action: async page =>
            {
                string flowId = await page.EvaluateAsync<string>(
                    expression: "async () => { const name = 'UI acceptance flow'; "
                        + "const flow = await api.add('Workflow/FlowDefinition', { "
                        + "Name: name, AppId: session.app.Id, Description: name, "
                        + "ReportingComponentName: null, "
                        + "InstanceReportingComponentName: null, "
                        + "DefinitionJson: JSON.stringify({ Name: name, "
                        + "RequiredRoles: '', Links: [], Activities: [] }) }); "
                        + "return flow.Id; }");

                await page.GotoAsync(
                    url: new Uri(
                        baseUri: fixture.WebBaseAddress,
                        relativeUri: "Admin/WorkflowDesigner?id="
                            + Uri.EscapeDataString(stringToEscape: flowId))
                        .ToString());

                await page.Locator(selector: ".component[name='FlowEditor']")
                    .WaitForAsync(
                        options: new LocatorWaitForOptions
                        {
                            State = WaitForSelectorState.Attached
                        });

                await page.WaitForFunctionAsync(
                    expression: "() => Boolean(window.editor)");

                await page.SetViewportSizeAsync(
                    width: 1280,
                    height: 800);

                ILocator editorHost = page.Locator(
                    selector: ".component[name='FlowEditor']");
                await Assertions.Expect(locator: editorHost)
                    .ToBeInViewportAsync();

                float? editorHeight = (await editorHost.BoundingBoxAsync())?.Height;
                Assert.True(
                    condition: editorHeight >= 500,
                    userMessage: $"The workflow editor was only {editorHeight}px high.");

                bool menuIconsMatch = await page.EvaluateAsync<bool>(
                    expression: "() => [...document.querySelectorAll('.categorytypes')]"
                        + ".every(group => { const header = group.querySelector(':scope > .header .k-icon'); "
                        + "const expected = [...header.classList].find(c => c.startsWith('k-i-')); "
                        + "return expected && [...group.querySelectorAll(':scope > .subtypes > li .k-icon')]"
                        + ".every(icon => icon.classList.contains(expected)); })");
                Assert.True(
                    condition: menuIconsMatch,
                    userMessage: "Workflow activity menu items did not inherit their category icon.");

                await page.EvaluateAsync(
                    expression: "() => { const meta = editor.stepTypes.find(t => !t.Name.includes('`')); "
                        + "editor.flow.addActivity(meta, { x: 100, y: 100 }); "
                        + "const activity = editor.flow.Activities[0]; "
                        + "activity.edit.call({ parent: activity }); }");
                ILocator activityForm = page.Locator(
                    selector: "form[name='activityEditorForm']");
                await activityForm.WaitForAsync();
                await Assertions.Expect(
                    locator: activityForm.Locator(".form-control").First)
                    .ToBeVisibleAsync();
                await Assertions.Expect(
                    locator: activityForm.Locator(".btn.btn-primary"))
                    .ToBeVisibleAsync();
                await page.Locator(selector: ".k-window .k-window-titlebar-action")
                    .Last
                    .ClickAsync();

                await page.EvaluateAsync(
                    expression: "() => { const source = editor.flow.Activities[0]; "
                        + "const meta = editor.stepTypes.find(t => !t.Name.includes('`') && t.ServerType !== source.meta.ServerType); "
                        + "editor.flow.addActivity(meta, { x: 350, y: 150 }); "
                        + "const destination = editor.flow.Activities.at(-1); "
                        + "new Link({ Source: source.model.Ref, Destination: destination.model.Ref, Expression: '' }, editor.flow).mouseup(); }");
                await page.Locator(selector: ".expression-builder .monaco-editor")
                    .WaitForAsync(
                        options: new LocatorWaitForOptions
                        {
                            State = WaitForSelectorState.Visible
                        });

                await page.EvaluateAsync(
                    expression: "id => api.destroy("
                        + "'Workflow/FlowDefinition(' + id + ')')",
                    arg: flowId);
            });

        // Then
    }
}
