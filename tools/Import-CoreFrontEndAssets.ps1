[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $FrontEndAssetsPath,

    [string[]] $Snapshots = @('Default App', 'ccoder.co.uk')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$assetsRoot = [IO.Path]::GetFullPath($FrontEndAssetsPath)

if (-not (Test-Path -LiteralPath $assetsRoot -PathType Container)) {
    throw "Front-end assets path '$assetsRoot' does not exist."
}

function Get-AssetName([string] $relativePath) {
    $name = $relativePath `
        -replace '\\', '/' `
        -replace '\.(min\.)?(js|css)$', '' `
        -replace '^bootstrap/lib/', '' `
        -replace '^bootstrap/css/', 'Bootstrap/' `
        -replace '^dependencies/', 'Dependency/' `
        -replace '^css/', 'Source/'

    return (($name -split '/') | ForEach-Object {
        (($_ -split '[._-]') | ForEach-Object {
            if ($_.Length -gt 0) {
                $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1)
            }
        }) -join ''
    }) -join '.'
}

function Get-ResourceKey([string] $relativePath) {
    if ($relativePath -match '^bootstrap/lib/workflow/') {
        return 'Workflow'
    }

    if ($relativePath -match '^bootstrap/lib/(Monaco|Core/(editor|contentEditor|pageToolbar))') {
        return 'ContentManagement'
    }

    return 'Common'
}

function Get-SourceTimestamp([string] $path) {
    $sourceRepository = Resolve-Path -LiteralPath (Join-Path $assetsRoot '..\..')
    $relativePath = [IO.Path]::GetRelativePath($sourceRepository, $path).Replace('\', '/')
    $timestamp = git -C $sourceRepository log -1 --format=%cI -- $relativePath

    if ([string]::IsNullOrWhiteSpace($timestamp)) {
        throw "Could not determine the committed timestamp for '$relativePath'."
    }

    return [DateTimeOffset]::Parse($timestamp).ToUniversalTime().ToString('O')
}

$sourceFiles = Get-ChildItem -LiteralPath $assetsRoot -Recurse -File |
    Where-Object { $_.Extension -in @('.js', '.css') } |
    Sort-Object FullName

foreach ($sourceFile in $sourceFiles) {
    $relativePath = $sourceFile.FullName.Substring($assetsRoot.Length + 1).Replace('\', '/')
    $assetType = if ($sourceFile.Extension -eq '.js') { 'Script' } else { 'Style' }
    $assetDirectory = "${assetType}s"
    $assetName = Get-AssetName -relativePath $relativePath

    if ($relativePath -eq 'bootstrap/css/site.css') {
        $assetName = 'Baseline'
    }

    $resourceKey = Get-ResourceKey -relativePath $relativePath
    $timestamp = Get-SourceTimestamp -path $sourceFile.FullName
    $asset = [ordered]@{
        Name = $assetName
        Content = [IO.File]::ReadAllText($sourceFile.FullName)
        CreatedOn = $timestamp
        LastUpdated = $timestamp
        Key = $resourceKey
        PackageType = "ContentManagement/$assetType"
        IncludeInSubSequentImports = $true
    }

    $json = $asset | ConvertTo-Json -Depth 10

    foreach ($snapshot in $Snapshots) {
        $targetDirectory = Join-Path $repoRoot "Data/$snapshot/Common Cache/$resourceKey/$assetDirectory"
        [IO.Directory]::CreateDirectory($targetDirectory) | Out-Null
        $targetPath = Join-Path $targetDirectory "$assetName.json"
        [IO.File]::WriteAllText(
            $targetPath,
            "$json$([Environment]::NewLine)",
            [Text.UTF8Encoding]::new($false))
    }
}

[pscustomobject]@{
    Source = $assetsRoot
    SourceFiles = $sourceFiles.Count
    Snapshots = $Snapshots
}
