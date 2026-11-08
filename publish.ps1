$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }

if (Test-Path $localDotnet) {
    $env:DOTNET_ROOT = Split-Path $localDotnet
    $env:PATH = "$(Split-Path $localDotnet);$env:PATH"
}

$output = Join-Path $root 'publish'
$project = Join-Path $root 'src\KursorTheme\KursorTheme.csproj'

& $dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $output

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$size = [Math]::Round((Get-Item (Join-Path $output 'KursorTheme.exe')).Length / 1MB, 1)
Write-Output "Published $output\KursorTheme.exe ($size MB, self-contained, no .NET install required)"
