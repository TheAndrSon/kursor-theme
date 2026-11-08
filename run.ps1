param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$exe = Join-Path $root 'src\KursorTheme\bin\Release\net8.0-windows\KursorTheme.exe'

if (-not (Test-Path $exe)) {
    & (Join-Path $root 'build.ps1')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (Test-Path $localDotnet) {
    $env:DOTNET_ROOT = Split-Path $localDotnet
    $env:PATH = "$(Split-Path $localDotnet);$env:PATH"
}

$startArgs = @{}
if ($Arguments -and $Arguments.Count -gt 0) { $startArgs.ArgumentList = $Arguments }
Start-Process -FilePath $exe @startArgs

Write-Output "Started $exe"
Write-Output "The app lives in the system tray. Hotkeys: Ctrl+Alt+E / Ctrl+Alt+S / Ctrl+Alt+O."
