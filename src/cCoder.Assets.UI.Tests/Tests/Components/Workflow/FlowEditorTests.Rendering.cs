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

                await page.EvaluateAsync(
                    expression: "id => api.destroy("
                        + "'Workflow/FlowDefinition(' + id + ')')",
                    arg: flowId);
            });

        // Then
    }
}