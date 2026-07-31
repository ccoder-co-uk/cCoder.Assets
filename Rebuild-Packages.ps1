param(
    [switch] $NoTests
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$project = Join-Path $repoRoot 'src/cCoder.Packer/cCoder.Packer.csproj'
$configuration = 'Release'
$packer = Join-Path $repoRoot "src/cCoder.Packer/bin/$configuration/net10.0/cCoder.Packer.exe"
$baselineData = Join-Path $repoRoot 'Data/Default App'
$sourceSnapshot = Join-Path $repoRoot 'Data/ccoder.co.uk'
$packages = Join-Path $repoRoot 'Packages'
$allComponentsPackage = Join-Path $packages 'Common Cache/all-components.json'
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "ccoder-assets-all-components-$PID"

dotnet build $project --configuration $configuration

if (-not $NoTests) {
    dotnet test (Join-Path $repoRoot 'src/cCoder.Packer.Tests/cCoder.Packer.Tests.csproj') `
        --configuration $configuration
}

& $packer pack `
    -dataPath $baselineData `
    -packagesPath $packages

try {
    New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

    $componentFiles = Get-ChildItem -Path $sourceSnapshot -Recurse -File -Filter '*.json' |
        Where-Object { $_.Directory.Name -eq 'Components' }
    $componentIdentities = @{}

    foreach ($componentFile in $componentFiles) {
        $component = Get-Content -Raw $componentFile.FullName | ConvertFrom-Json
        $resourceKey = if ([string]::IsNullOrWhiteSpace($component.ResourceKey)) {
            $component.Key
        } else {
            $component.ResourceKey
        }
        $identity = "$resourceKey/$($component.Name)".ToLowerInvariant()

        if ($componentIdentities.ContainsKey($identity)) {
            throw "Duplicate component identity '$identity' found in '$($componentFile.FullName)' and '$($componentIdentities[$identity])'."
        }

        $componentIdentities[$identity] = $componentFile.FullName
        $componentDirectory = Join-Path $stagingRoot "$resourceKey/Components"
        New-Item -ItemType Directory -Path $componentDirectory -Force | Out-Null
        Copy-Item -LiteralPath $componentFile.FullName -Destination (Join-Path $componentDirectory $componentFile.Name)
    }

    if ($componentIdentities.Count -ne 111) {
        throw "Expected 111 unique components in the ccoder.co.uk snapshot but found $($componentIdentities.Count)."
    }

    & $packer pack `
        -dataPath $stagingRoot `
        -destination $allComponentsPackage `
        -name 'All Components Common Cache' `
        -category 'Common Cache'

    $allComponents = Get-Content -Raw $allComponentsPackage | ConvertFrom-Json
    $componentPackageItems = @(
        $allComponents.Items |
            Where-Object { $_.Type -eq 'ContentManagement/Component' }
    )
    $packagedComponents = @($componentPackageItems.Data | ConvertFrom-Json)

    if ($componentPackageItems.Count -ne 1 -or
        $packagedComponents.Count -ne $componentIdentities.Count) {
        throw "The all-components package contains $($packagedComponents.Count) components; expected $($componentIdentities.Count)."
    }

    $manifestPath = Join-Path $packages 'manifest.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $relativePackagePath = 'Common Cache/all-components.json'
    $packageHash = (Get-FileHash -LiteralPath $allComponentsPackage -Algorithm SHA256).Hash.ToLowerInvariant()
    $manifestPackages = @(
        $manifest.Packages |
            Where-Object { $_.Path -ne $relativePackagePath }
    )
    $manifestPackages += [pscustomobject]@{
        Path = $relativePackagePath
        Sha256 = $packageHash
        FirstTimeSetup = $false
        Source = 'Common Cache'
        Category = 'Common Cache'
        ItemTypes = @('ContentManagement/Component')
    }
    $manifest.Packages = @($manifestPackages | Sort-Object Path)
    $manifestJson = $manifest | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText(
        $manifestPath,
        "$manifestJson$([Environment]::NewLine)",
        [System.Text.UTF8Encoding]::new($false))

    & $packer report `
        -dataPath (Join-Path $repoRoot 'Data') `
        -packagesPath $packages
}
finally {
    $resolvedStagingRoot = [System.IO.Path]::GetFullPath($stagingRoot)
    $resolvedTempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())

    if ($resolvedStagingRoot.StartsWith($resolvedTempRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedStagingRoot)) {
        Remove-Item -LiteralPath $resolvedStagingRoot -Recurse -Force
    }
}

Write-Host 'Packages and asset-usage report rebuilt successfully.'
