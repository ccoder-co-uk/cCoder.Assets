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
            expression: "element => {"
                + "const grid = window.jQuery(element).data('kendoGrid');"
                + "if (!grid) throw new Error(`${componentName} grid was not initialized`);"
                + "const now = new Date();"
                + "grid.dataSource.filter({});"
                + "grid.dataSource.data([{ Id: crypto.randomUUID(), "
                + "AppId: session.app.Id, Name: 'Acceptance workflow', "
                + "Description: 'Visible workflow row', CreatedBy: session.user.Id, "
                + "CreatedOn: now, LastUpdatedBy: session.user.Id, LastUpdated: now, "
                + "DefinitionJson: JSON.stringify({ Name: 'Acceptance workflow', "
                + "RequiredRoles: '', Activities: [], Links: [] }), "
                + "ReportingComponentName: null, "
                + "InstanceReportingComponentName: null, "
                + "type: 'Workflow/FlowDefinition' }]);"
                + "grid.refresh(); }");
    }
}