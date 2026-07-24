param(
    [Parameter(Mandatory = $true)]
    [string] $DataPath,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'

$resolvedDataPath = [System.IO.Path]::GetFullPath($DataPath)
$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)

if (-not (Test-Path -LiteralPath $resolvedDataPath -PathType Container)) {
    throw "Data path '$resolvedDataPath' does not exist."
}

if ($resolvedOutputPath.StartsWith(
    $resolvedDataPath + [System.IO.Path]::DirectorySeparatorChar,
    [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'The output path must not be inside the source Data directory.'
}

if (Test-Path -LiteralPath $resolvedOutputPath) {
    Remove-Item -LiteralPath $resolvedOutputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $resolvedOutputPath | Out-Null

$domainAliases = @{
    'CMS' = 'ContentManagement'
    'DMS' = 'DocumentManagement'
    'Scheduling' = 'Workflow'
    'SSO' = 'AppSecurity'
    'Account' = 'AppSecurity'
    'RolePrivManagement' = 'AppSecurity'
}

$nonDomainOperations = @(
    'GetMetadata',
    'Getmetadata',
    'RefreshCache'
)

$assetTypes = @(
    'Apps',
    'Calendars',
    'Components',
    'FlowDefinitions',
    'FolderRoles',
    'Layouts',
    'PageRoles',
    'Pages',
    'Resources',
    'Roles',
    'Scripts',
    'Templates'
)

$apiPattern = [regex]::new(
    '(?i)(?:/Api/|api\.(?:get|post|put|delete|patch|add|update|destroy)\s*\(\s*["''`]|endpoint\s*:\s*["''`]|model\.(?:get|save|remove)\w*\s*\(\s*["''`]|\[meta\[)(?<domain>[A-Za-z][A-Za-z0-9]+)')

$componentTagPattern = [regex]::new(
    '(?i)\[component\[(?<name>[^\]]+)\]\]|data-component\s*=\s*["''](?<attribute>[^"'']+)["'']')

$scriptTagPattern = [regex]::new(
    '(?i)\[script\[(?<name>[^\]]+)\]\]')

$resourceTagPattern = [regex]::new(
    '(?i)\[resource[^\[]*\[(?<name>[^\]]+)\]\]')

$loadComponentPattern = [regex]::new(
    '(?is)loadComponent\s*\((?<arguments>.*?)\)')

function Get-NormalisedDomain {
    param([string] $Domain)

    if ([string]::IsNullOrWhiteSpace($Domain) -or
        $nonDomainOperations -contains $Domain) {
        return $null
    }

    if ($domainAliases.ContainsKey($Domain)) {
        return $domainAliases[$Domain]
    }

    return $Domain
}

function Get-JsonText {
    param([object] $Value)

    $parts = [System.Collections.Generic.List[string]]::new()

    foreach ($property in $Value.PSObject.Properties) {
        if ($property.Value -is [string]) {
            $parts.Add($property.Value)
        }
    }

    $parts.Add(($Value | ConvertTo-Json -Depth 100 -Compress))

    return [string]::Join("`n", $parts)
}

function Get-TaggedNames {
    param(
        [string] $Text,
        [regex] $Pattern
    )

    $names = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)

    foreach ($match in $Pattern.Matches($Text)) {
        foreach ($groupName in @('name', 'attribute')) {
            $value = $match.Groups[$groupName].Value

            if (-not [string]::IsNullOrWhiteSpace($value)) {
                $null = $names.Add($value)
            }
        }
    }

    return @(
        $names |
            ForEach-Object { [string] $_ }
    )
}

function Get-ComponentNames {
    param(
        [string] $Text,
        [System.Collections.Generic.HashSet[string]] $KnownNames
    )

    $names = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)

    foreach ($name in Get-TaggedNames $Text $componentTagPattern) {
        $null = $names.Add($name)
    }

    foreach ($call in $loadComponentPattern.Matches($Text)) {
        foreach ($quoted in [regex]::Matches(
            $call.Groups['arguments'].Value,
            '["''`](?<value>[^"''`]+)["''`]')) {
            $candidate = $quoted.Groups['value'].Value

            if ($KnownNames.Contains($candidate)) {
                $null = $names.Add($candidate)
            }
        }
    }

    return @(
        $names |
            ForEach-Object { [string] $_ }
    )
}

function Get-TargetKey {
    param(
        [object] $Component,
        [string] $Text
    )

    $domainCounts = @{}

    foreach ($match in $apiPattern.Matches($Text)) {
        $domain = Get-NormalisedDomain $match.Groups['domain'].Value

        if ($null -eq $domain) {
            continue
        }

        if ($domainCounts.ContainsKey($domain)) {
            $domainCounts[$domain]++
        }
        else {
            $domainCounts[$domain] = 1
        }
    }

    if ($domainCounts.Count -eq 0) {
        return 'Default'
    }

    $currentKey = Get-NormalisedDomain ([string] $Component.ResourceKey)

    if ($null -ne $currentKey -and $domainCounts.ContainsKey($currentKey)) {
        return $currentKey
    }

    if ($domainCounts.Count -eq 1) {
        return [string] @($domainCounts.Keys)[0]
    }

    $rankedDomains = @(
        $domainCounts.GetEnumerator() |
            Sort-Object Value -Descending
    )

    if ($rankedDomains[0].Value -gt $rankedDomains[1].Value) {
        return [string] $rankedDomains[0].Key
    }

    return 'Default'
}

function Get-Source {
    param([string] $File)

    $relativePath = [System.IO.Path]::GetRelativePath(
        $resolvedDataPath,
        $File)

    return $relativePath.Split(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.StringSplitOptions]::RemoveEmptyEntries)[0]
}

function Get-SafeFileName {
    param([string] $Value)

    $invalidCharacters = [System.IO.Path]::GetInvalidFileNameChars()
    $characters = foreach ($character in $Value.ToCharArray()) {
        if ($invalidCharacters -contains $character -or
            $character -in @('/', '\')) {
            '_'
        }
        else {
            $character
        }
    }

    return -join $characters
}

function Write-Json {
    param(
        [string] $Path,
        [object] $Value
    )

    $parent = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $parent -Force | Out-Null

    $json = $Value | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText(
        $Path,
        $json,
        [System.Text.UTF8Encoding]::new($false))
}

function Set-Property {
    param(
        [object] $Value,
        [string] $Name,
        [object] $PropertyValue
    )

    if ($null -eq $Value.PSObject.Properties[$Name]) {
        $Value | Add-Member `
            -MemberType NoteProperty `
            -Name $Name `
            -Value $PropertyValue
    }
    else {
        $Value.$Name = $PropertyValue
    }
}

function Write-Asset {
    param(
        [string] $Source,
        [string] $Key,
        [string] $Type,
        [string] $Name,
        [object] $Value
    )

    $path = Join-Path $resolvedOutputPath $Source
    $path = Join-Path $path $Key
    $path = Join-Path $path $Type
    $path = Join-Path $path ((Get-SafeFileName $Name) + '.json')

    if (Test-Path -LiteralPath $path) {
        $existing = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
        $existingJson = $existing | ConvertTo-Json -Depth 100 -Compress
        $incomingJson = $Value | ConvertTo-Json -Depth 100 -Compress

        if ($existingJson -ne $incomingJson) {
            if ($Type -in @(
                'Components',
                'Layouts',
                'Pages',
                'Scripts',
                'Templates'
            )) {
                throw "Normalisation collision at '$path'."
            }

            $bytes = [System.Text.Encoding]::UTF8.GetBytes($incomingJson)
            $hash = [System.Convert]::ToHexString(
                [System.Security.Cryptography.SHA256]::HashData($bytes)
            ).Substring(0, 12)

            $path = Join-Path `
                (Split-Path -Parent $path) `
                ((Get-SafeFileName $Name) + "-$hash.json")
        }
        else {
            return
        }
    }

    Write-Json $path $Value
}

$assets = [System.Collections.Generic.List[object]]::new()

foreach ($file in Get-ChildItem -LiteralPath $resolvedDataPath -Recurse -Filter '*.json') {
    $relativePath = [System.IO.Path]::GetRelativePath(
        $resolvedDataPath,
        $file.FullName)

    $segments = $relativePath.Split(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.StringSplitOptions]::RemoveEmptyEntries)

    if ($segments.Length -lt 4) {
        continue
    }

    $value = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json

    foreach ($item in @($value)) {
        $keySegment = $segments[1]
        $typeSegment = $segments[2]

        if ($assetTypes -contains $segments[1] -and
            $assetTypes -notcontains $segments[2]) {
            $keySegment = $segments[2]
            $typeSegment = $segments[1]
        }

        $assets.Add([pscustomobject]@{
            Source = $segments[0]
            OriginalKey = $keySegment
            Type = $typeSegment
            File = $file.FullName
            Value = $item
            Text = Get-JsonText $item
            TargetKey = $null
        })
    }
}

$components = @($assets | Where-Object Type -eq 'Components')
$componentNames = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)

foreach ($component in $components) {
    $null = $componentNames.Add([string] $component.Value.Name)
    $component.TargetKey = Get-TargetKey $component.Value $component.Text
}

function Resolve-Component {
    param(
        [string] $Source,
        [string] $Name
    )

    $local = @(
        $components | Where-Object {
            $_.Source -eq $Source -and $_.Value.Name -eq $Name
        })

    if ($local.Count -gt 0) {
        return $local[0]
    }

    if ($Source -ne 'Common Cache') {
        $common = @(
            $components | Where-Object {
                $_.Source -eq 'Common Cache' -and $_.Value.Name -eq $Name
            })

        if ($common.Count -gt 0) {
            return $common[0]
        }
    }

    return $null
}

function Get-DependencyKeys {
    param(
        [object] $Asset,
        [System.Collections.Generic.HashSet[string]] $Visited
    )

    $keys = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)

    foreach ($name in Get-ComponentNames $Asset.Text $componentNames) {
        $component = Resolve-Component $Asset.Source $name

        if ($null -eq $component) {
            continue
        }

        $null = $keys.Add($component.TargetKey)
        $identity = "$($component.Source)/$($component.Value.Name)"

        if ($Visited.Add($identity)) {
            foreach ($nestedKey in Get-DependencyKeys $component $Visited) {
                $null = $keys.Add($nestedKey)
            }
        }
    }

    return @(
        $keys |
            ForEach-Object { [string] $_ }
    )
}

foreach ($asset in $assets | Where-Object Type -in @('Pages', 'Layouts')) {
    $keys = @(
        Get-DependencyKeys $asset (
            [System.Collections.Generic.HashSet[string]]::new(
                [System.StringComparer]::OrdinalIgnoreCase))
    )

    $asset.TargetKey = if ($keys.Count -eq 1) {
        $keys[0]
    }
    else {
        'Default'
    }
}

$scriptConsumers = @{}

foreach ($asset in $assets) {
    $ownerKey = if ($asset.Type -eq 'Components') {
        $asset.TargetKey
    }
    elseif ($asset.Type -in @('Pages', 'Layouts')) {
        $asset.TargetKey
    }
    else {
        $null
    }

    if ($null -eq $ownerKey) {
        continue
    }

    foreach ($scriptName in Get-TaggedNames $asset.Text $scriptTagPattern) {
        $identity = "$($asset.Source)|$scriptName"

        if (-not $scriptConsumers.ContainsKey($identity)) {
            $scriptConsumers[$identity] =
                [System.Collections.Generic.HashSet[string]]::new(
                    [System.StringComparer]::OrdinalIgnoreCase)
        }

        $null = $scriptConsumers[$identity].Add($ownerKey)
    }
}

foreach ($script in $assets | Where-Object Type -eq 'Scripts') {
    $identity = "$($script.Source)|$($script.Value.Name)"
    $keys = @()

    if ($scriptConsumers.ContainsKey($identity)) {
        $keys = @(
            $scriptConsumers[$identity] |
                ForEach-Object { [string] $_ }
        )
    }

    $script.TargetKey = if ($keys.Count -eq 1) {
        $keys[0]
    }
    else {
        'Default'
    }
}

$resourceConsumers = @{}

foreach ($asset in $assets | Where-Object {
    $_.Type -in @('Components', 'Scripts', 'Pages', 'Layouts')
}) {
    $ownerKey = $asset.TargetKey

    if ([string]::IsNullOrWhiteSpace($ownerKey)) {
        $ownerKey = 'Default'
    }

    foreach ($resourceName in Get-TaggedNames $asset.Text $resourceTagPattern) {
        $identity = "$($asset.Source)|$resourceName"

        if (-not $resourceConsumers.ContainsKey($identity)) {
            $resourceConsumers[$identity] =
                [System.Collections.Generic.HashSet[string]]::new(
                    [System.StringComparer]::OrdinalIgnoreCase)
        }

        $null = $resourceConsumers[$identity].Add($ownerKey)
    }
}

foreach ($resource in $assets | Where-Object Type -eq 'Resources') {
    $identity = "$($resource.Source)|$($resource.Value.Name)"
    $keys = @()

    if ($resourceConsumers.ContainsKey($identity)) {
        $keys = @(
            $resourceConsumers[$identity] |
                ForEach-Object { [string] $_ }
        )
    }

    $resource.TargetKey = if ($keys.Count -eq 1) {
        $keys[0]
    }
    else {
        'Default'
    }
}

foreach ($asset in $assets) {
    if ([string]::IsNullOrWhiteSpace($asset.TargetKey)) {
        $asset.TargetKey = 'Default'
    }

    if ($null -eq $asset.Value.PSObject.Properties[
        'IncludeInSubSequentImports']) {
        Set-Property `
            $asset.Value `
            'IncludeInSubSequentImports' `
            $true
    }

    switch ($asset.Type) {
        'Components' {
            Set-Property $asset.Value 'Key' $asset.TargetKey
            Set-Property $asset.Value 'ResourceKey' $asset.TargetKey
        }
        'Pages' {
            Set-Property $asset.Value 'ResourceKey' $asset.TargetKey
        }
        'Scripts' {
            Set-Property $asset.Value 'Key' $asset.TargetKey
        }
        'Resources' {
            Set-Property $asset.Value 'Key' $asset.TargetKey
        }
    }
}

foreach ($asset in $assets | Where-Object Type -ne 'Resources') {
    $name = if ($asset.Type -eq 'Pages' -and
        -not [string]::IsNullOrWhiteSpace([string] $asset.Value.Path)) {
        ([string] $asset.Value.Path).Replace('/', '_')
    }
    elseif ($asset.Type -in @(
        'Components',
        'Layouts',
        'Scripts',
        'Templates'
    ) -and -not [string]::IsNullOrWhiteSpace([string] $asset.Value.Name)) {
        [string] $asset.Value.Name
    }
    else {
        [System.IO.Path]::GetFileNameWithoutExtension($asset.File)
    }

    Write-Asset `
        -Source $asset.Source `
        -Key $asset.TargetKey `
        -Type $asset.Type `
        -Name $name `
        -Value $asset.Value
}

$resourceGroups = $assets |
    Where-Object Type -eq 'Resources' |
    Group-Object Source, TargetKey, { [string] $_.Value.Culture }

foreach ($group in $resourceGroups) {
    $first = $group.Group[0]
    $culture = [string] $first.Value.Culture
    $fileName = if ([string]::IsNullOrWhiteSpace($culture)) {
        'Default'
    }
    else {
        $culture
    }

    $path = Join-Path $resolvedOutputPath $first.Source
    $path = Join-Path $path $first.TargetKey
    $path = Join-Path $path 'Resources'
    $path = Join-Path $path ((Get-SafeFileName $fileName) + '.json')

    Write-Json $path @(
        $group.Group |
            Sort-Object { [string] $_.Value.Name } |
            ForEach-Object Value
    )
}

$summary = $assets |
    Group-Object Type, TargetKey |
    Sort-Object Name |
    ForEach-Object {
        [pscustomobject]@{
            Count = $_.Count
            TypeAndKey = $_.Name
        }
    }

$summary | Format-Table -AutoSize
