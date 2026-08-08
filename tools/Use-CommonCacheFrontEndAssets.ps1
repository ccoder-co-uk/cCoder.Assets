[CmdletBinding()]
param(
    [string[]] $Snapshots = @('Default App', 'ccoder.co.uk')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

$styles = @(
    'Dependency.Bootstrap.Bootstrap',
    'Dependency.Kendo.KendoV20242514Bootstrap',
    'Baseline'
)

$baselineScripts = @(
    'Dependency.Jquery.Jquery370',
    'Dependency.Jquery.JqueryValidate',
    'Dependency.Jquery.JqueryUi',
    'Dependency.Kendo.KendoAllV20242514',
    'Dependency.Kendo.KendoUiLicense',
    'Dependency.Bootstrap.BootstrapBundle',
    'Dependency.Other.Signalr',
    'Widgets.Widget',
    'Core.Api',
    'Core.Util',
    'Core.Model'
)

$fullPageScripts = @(
    $baselineScripts
    'Monaco.MonacoEditor',
    'Monaco.JavaScriptMonacoEditor',
    'Monaco.HTMLMonacoEditor',
    'Monaco.CSharpMonacoEditor',
    'Core.Drawing',
    'Widgets.Dialog',
    'Widgets.BootstrapDialog',
    'Widgets.BootstrapTabs',
    'Widgets.Chart',
    'Widgets.PieChart',
    'Widgets.ConfirmDialog',
    'Widgets.ConsoleDialog',
    'Widgets.ExportDialog',
    'Widgets.Detail',
    'Widgets.EditorDialog',
    'Widgets.Grid',
    'Widgets.ContextMenuWidget',
    'Widgets.FileDropContainerWidget',
    'Widgets.Picker',
    'Widgets.ReadOnlyDetailView',
    'Widgets.Tree',
    'Widgets.DataTreeView',
    'Widgets.OdataTree',
    'Widgets.Workspace',
    'Widgets.WritableDetailView',
    'Workflow.Close',
    'Workflow.Handle',
    'Workflow.Link',
    'Workflow.Action',
    'Workflow.Activity',
    'Workflow.Connector',
    'Workflow.Flow',
    'Workflow.Workflowdesigner'
)

$styleElement = (@('<style nonce="[request[nonce]]">') +
    @($styles | ForEach-Object { "[style[$_]]" }) +
    @('</style>')) -join [Environment]::NewLine

$updated = 0

foreach ($snapshot in $Snapshots) {
    $appRoot = Join-Path $repoRoot "Data/$snapshot/App"

    Get-ChildItem -LiteralPath $appRoot -Recurse -File -Filter '*.json' |
        Where-Object { $_.Directory.Name -eq 'Layouts' } |
        ForEach-Object {
            $layout = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
            $originalHeader = [string] $layout.HeaderHtml
            $originalHtml = [string] $layout.Html
            $usesFullScripts = $layout.Name -in @('Default', 'FullPage')
            $scripts = if ($usesFullScripts) {
                $fullPageScripts
            } else {
                $baselineScripts
            }

            $scriptElement = (@('<script nonce="[request[nonce]]">') +
                @($scripts | ForEach-Object { "[script[$_]]" }) +
                @('</script>')) -join [Environment]::NewLine

            $layout.HeaderHtml = $originalHeader -replace
                '<link\s+rel="stylesheet"\s+media="screen"\s+href="/everything\.min\.css"\s*/>',
                [Text.RegularExpressions.MatchEvaluator] { $styleElement }

            $layout.HeaderHtml = $layout.HeaderHtml -replace
                '<style nonce="\[request\[nonce\]\]">\s*System\.Object\[\]\s*</style>',
                [Text.RegularExpressions.MatchEvaluator] { $styleElement }

            $layout.Html = $originalHtml -replace
                '<script\s+src="/everything\.min\.js"\s+crossorigin="anonymous"></script>',
                [Text.RegularExpressions.MatchEvaluator] { $scriptElement }

            $layout.Html = $layout.Html -replace
                '<script nonce="\[request\[nonce\]\]">\s*System\.Object\[\]\s*</script>',
                [Text.RegularExpressions.MatchEvaluator] { $scriptElement }

            $layout.Html = $layout.Html -replace
                '(?s)<script nonce="\[request\[nonce\]\]">\s*\[script\[Dependency\.Jquery\.Jquery370\]\].*?</script>',
                [Text.RegularExpressions.MatchEvaluator] { $scriptElement }

            if (-not $layout.Html.Contains('[script[KendoCultures]]')) {
                $layout.Html = $layout.Html.Replace(
                    '[script[DefaultResourcing]]',
                    "[script[DefaultResourcing]]$([Environment]::NewLine)    [script[KendoCultures]]")
            }

            if (-not $layout.Html.Contains("kendo.setDefaults('iconType', 'svg');")) {
                $layout.Html = $layout.Html.Replace(
                    '    initContent();',
                    "    initContent();$([Environment]::NewLine)    kendo.setDefaults('iconType', 'svg');")
            }

            if ($layout.HeaderHtml -ne $originalHeader -or $layout.Html -ne $originalHtml) {
                $json = $layout | ConvertTo-Json -Depth 100
                [IO.File]::WriteAllText(
                    $_.FullName,
                    "$json$([Environment]::NewLine)",
                    [Text.UTF8Encoding]::new($false))
                $updated++
            }
        }
}

[pscustomobject]@{
    UpdatedLayouts = $updated
    StyleModules = $styles.Count
    BaselineScriptModules = $baselineScripts.Count
    FullPageScriptModules = $fullPageScripts.Count
}
