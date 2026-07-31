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

    $additionalPackageSpecifications = @(
        [pscustomobject]@{
            Directory = 'Resources'
            PackageFile = 'all-resources.json'
            PackageName = 'All Resources Common Cache'
            ItemType = 'ContentManagement/Resource'
            ExpectedCount = 314
            IncludeCulture = $true
        },
        [pscustomobject]@{
            Directory = 'Scripts'
            PackageFile = 'all-scripts.json'
            PackageName = 'All Scripts Common Cache'
            ItemType = 'ContentManagement/Script'
            ExpectedCount = 10
            IncludeCulture = $false
        }
    )

    foreach ($specification in $additionalPackageSpecifications) {
        $entityStagingRoot = Join-Path $stagingRoot $specification.Directory
        $entityFiles = Get-ChildItem -Path $sourceSnapshot -Recurse -File -Filter '*.json' |
            Where-Object { $_.Directory.Name -eq $specification.Directory } |
            Sort-Object FullName
        $entityIdentities = @{}

        foreach ($entityFile in $entityFiles) {
            $entities = @(Get-Content -Raw $entityFile.FullName | ConvertFrom-Json)

            foreach ($entity in $entities) {
                $resourceKey = if ([string]::IsNullOrWhiteSpace($entity.ResourceKey)) {
                    $entity.Key
                } else {
                    $entity.ResourceKey
                }
                $culture = if ($specification.IncludeCulture) {
                    [string]$entity.Culture
                } else {
                    ''
                }
                $identity = "$resourceKey/$($entity.Name)/$culture".ToLowerInvariant()
                $candidateIsCommonCache =
                    $entityFile.FullName -like '*\Common Cache\*'

                if ($entityIdentities.ContainsKey($identity)) {
                    $existing = $entityIdentities[$identity]

                    if ($existing.IsCommonCache -and
                        -not $candidateIsCommonCache) {
                        continue
                    }
                }

                $entityIdentities[$identity] = [pscustomobject]@{
                    Entity = $entity
                    IsCommonCache = $candidateIsCommonCache
                    Source = $entityFile.FullName
                }
            }
        }

        $entityIndex = 0

        foreach ($identity in @($entityIdentities.Keys | Sort-Object)) {
            $entry = $entityIdentities[$identity]
            $entity = $entry.Entity
            $resourceKey = if ([string]::IsNullOrWhiteSpace($entity.ResourceKey)) {
                $entity.Key
            } else {
                $entity.ResourceKey
            }
            $entity.PSObject.Properties.Remove('PackageType')
            $entity.PSObject.Properties.Remove('IncludeInSubSequentImports')
            $entityDirectory =
                Join-Path $entityStagingRoot "$resourceKey/$($specification.Directory)"
            New-Item -ItemType Directory -Path $entityDirectory -Force | Out-Null
            $entityJson = $entity | ConvertTo-Json -Depth 100
            $entityPath =
                Join-Path $entityDirectory "$($entityIndex.ToString('D4')).json"
            [System.IO.File]::WriteAllText(
                $entityPath,
                "$entityJson$([Environment]::NewLine)",
                [System.Text.UTF8Encoding]::new($false))
            $entityIndex++
        }

        if ($entityIdentities.Count -ne $specification.ExpectedCount) {
            throw "Expected $($specification.ExpectedCount) unique $($specification.Directory) records in the ccoder.co.uk snapshot but found $($entityIdentities.Count)."
        }

        $packagePath = Join-Path $packages "Common Cache/$($specification.PackageFile)"
        & $packer pack `
            -dataPath $entityStagingRoot `
            -destination $packagePath `
            -name $specification.PackageName `
            -category 'Common Cache'

        $package = Get-Content -Raw $packagePath | ConvertFrom-Json
        $packageItems = @(
            $package.Items |
                Where-Object { $_.Type -eq $specification.ItemType }
        )
        $packagedEntities = @($packageItems.Data | ConvertFrom-Json)

        if ($packageItems.Count -ne 1 -or
            $packagedEntities.Count -ne $entityIdentities.Count) {
            throw "The $($specification.PackageFile) package contains $($packagedEntities.Count) records; expected $($entityIdentities.Count)."
        }
    }

    $manifestPath = Join-Path $packages 'manifest.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $completePackageEntries = @(
        [pscustomobject]@{
            Path = 'Common Cache/all-components.json'
            ItemType = 'ContentManagement/Component'
        },
        [pscustomobject]@{
            Path = 'Common Cache/all-resources.json'
            ItemType = 'ContentManagement/Resource'
        },
        [pscustomobject]@{
            Path = 'Common Cache/all-scripts.json'
            ItemType = 'ContentManagement/Script'
        }
    )
    $completePackagePaths = @($completePackageEntries.Path)
    $manifestPackages = @(
        $manifest.Packages |
            Where-Object { $_.Path -notin $completePackagePaths }
    )

    foreach ($completePackageEntry in $completePackageEntries) {
        $packagePath = Join-Path $packages $completePackageEntry.Path
        $manifestPackages += [pscustomobject]@{
            Path = $completePackageEntry.Path
            Sha256 = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
            FirstTimeSetup = $false
            Source = 'Common Cache'
            Category = 'Common Cache'
            ItemTypes = @($completePackageEntry.ItemType)
        }
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
