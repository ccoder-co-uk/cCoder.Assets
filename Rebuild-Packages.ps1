param(
    [switch] $NoTests
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$project = Join-Path $repoRoot 'src/cCoder.Packer/cCoder.Packer.csproj'
$configuration = 'Release'
$packer = Join-Path $repoRoot "src/cCoder.Packer/bin/$configuration/net10.0/cCoder.Packer.exe"
$baselineData = Join-Path $repoRoot 'Data/Default App'
$packages = Join-Path $repoRoot 'Packages'

dotnet build $project --configuration $configuration

if (-not $NoTests) {
    dotnet test (Join-Path $repoRoot 'src/cCoder.Packer.Tests/cCoder.Packer.Tests.csproj') `
        --configuration $configuration
}

& $packer pack `
    -dataPath $baselineData `
    -packagesPath $packages

& $packer report `
    -dataPath (Join-Path $repoRoot 'Data') `
    -packagesPath $packages

Write-Host 'Packages and asset-usage report rebuilt successfully.'
