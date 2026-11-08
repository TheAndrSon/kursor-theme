param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }

if (Test-Path $localDotnet) {
    $env:DOTNET_ROOT = Split-Path $localDotnet
    $env:PATH = "$(Split-Path $localDotnet);$env:PATH"
}

& $dotnet build (Join-Path $root 'src\KursorTheme\KursorTheme.csproj') -c $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
