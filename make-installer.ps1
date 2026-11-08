param(
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$dist = Join-Path $root 'dist'
$publishDir = Join-Path $root 'publish'
$exe = Join-Path $publishDir 'KursorTheme.exe'
$icon = Join-Path $root 'src\KursorTheme\Assets\AppIcon.ico'

if (-not $SkipPublish) {
    & (Join-Path $root 'publish.ps1')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not (Test-Path $exe)) { throw "Publish output not found: $exe" }
New-Item -ItemType Directory -Force -Path $dist | Out-Null

# Portable single executable
Copy-Item $exe (Join-Path $dist 'KursorTheme.exe') -Force

# Portable archive
$stage = Join-Path $env:TEMP 'kursor-theme-portable'
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Copy-Item $exe (Join-Path $stage 'KursorTheme.exe')
Copy-Item (Join-Path $root 'README.md') (Join-Path $stage 'README.md')
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath (Join-Path $dist 'KursorTheme-portable.zip') -Force
Remove-Item -Recurse -Force $stage

# NSIS (portable toolchain, no installation required)
$nsisRoot = Join-Path $root 'tools\nsis'
$makensis = Get-ChildItem -Path $nsisRoot -Recurse -Filter makensis.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -notmatch '\\Bin$' } |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $makensis) {
    Write-Output "Downloading NSIS..."
    $zip = Join-Path $env:TEMP 'nsis-3.10.zip'
    curl.exe -sSL --fail --max-time 180 "https://downloads.sourceforge.net/project/nsis/NSIS%203/3.10/nsis-3.10.zip" -o $zip
    if ($LASTEXITCODE -ne 0) { throw "NSIS download failed" }
    New-Item -ItemType Directory -Force -Path $nsisRoot | Out-Null
    Expand-Archive -Path $zip -DestinationPath $nsisRoot -Force
    $makensis = Get-ChildItem -Path $nsisRoot -Recurse -Filter makensis.exe |
        Where-Object { $_.DirectoryName -notmatch '\\Bin$' } |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not $makensis) { throw "makensis.exe not found" }

$outFile = Join-Path $dist 'KursorThemeSetup.exe'
Push-Location $root
try {
    & $makensis /V2 `
        "/DAPP_EXE=$exe" `
        "/DOUT_FILE=$outFile" `
        "/DAPP_ICON=$icon" `
        (Join-Path $root 'installer\KursorTheme.nsi')
    if ($LASTEXITCODE -ne 0) { throw "NSIS compilation failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}

$portableSize = [Math]::Round((Get-Item (Join-Path $dist 'KursorTheme.exe')).Length / 1MB, 1)
$zipSize = [Math]::Round((Get-Item (Join-Path $dist 'KursorTheme-portable.zip')).Length / 1MB, 1)
$setupSize = [Math]::Round((Get-Item $outFile).Length / 1MB, 1)

Write-Output "Portable:  dist\KursorTheme.exe ($portableSize MB)"
Write-Output "Archive:   dist\KursorTheme-portable.zip ($zipSize MB)"
Write-Output "Installer: dist\KursorThemeSetup.exe ($setupSize MB)"
