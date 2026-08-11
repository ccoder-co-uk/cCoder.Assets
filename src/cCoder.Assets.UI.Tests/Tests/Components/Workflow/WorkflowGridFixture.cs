// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Playwright;

namespace cCoder.Assets.UI.Tests.Tests.Components.Workflow;

internal static class WorkflowGridFixture
{
    internal static async Task ArrangeVisibleFlowAsync(
        IPage page,
        string componentName)
    {
        ILocator grid = page.Locator(
            selector: $".component[name='{componentName}'] .k-grid:visible, "
                + $".component[name='{componentName}'].k-grid:visible")
            .First;

        await grid.EvaluateAsync(
            expression: "async element => {"
                + "const grid = window.jQuery(element).data('kendoGrid');"
                + "if (!grid) throw new Error(`${componentName} grid was not initialized`);"
                + "await api.add('Workflow/FlowDefinition', { "
                + "AppId: session.app.Id, Name: 'Acceptance workflow', "
                + "Description: 'Visible workflow row', "
                + "DefinitionJson: JSON.stringify({ Name: 'Acceptance workflow', "
                + "RequiredRoles: '', Links: [], Activities: [{ "
                + "'$type': 'cCoder.Workflow.Activities.Start, cCoder.Workflow.Activities', "
                + "AuthToken: null, Data: null, Ref: 'Start', State: 0 }] }), "
                + "ReportingComponentName: null, "
                + "InstanceReportingComponentName: null });"
                + "await grid.dataSource.read(); }");

        await grid.Locator(
            selectorOrLocator: "tbody > tr:not(.k-grid-norecords)")
            .First
            .WaitForAsync();
    }
}