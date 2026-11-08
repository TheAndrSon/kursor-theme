param(
    [string]$Tag = "v0.5.0",
    [string]$NotesFile = "docs/release-notes-0.5.0.md",
    [string]$Repo = "TheAndrSon/kursor-theme"
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$notesPath = Join-Path $root $NotesFile
if (-not (Test-Path $notesPath)) { throw "Release notes not found: $notesPath" }

$credText = "protocol=https`nhost=github.com`n`n" | git credential fill
$cred = @{}
foreach ($line in $credText) {
    if ($line -match '^([^=]+)=(.*)$') { $cred[$matches[1]] = $matches[2] }
}
$token = $cred['password']
if (-not $token) { throw "No GitHub token found in the credential manager." }

$headers = @{
    Authorization = "Bearer $token"
    'User-Agent' = 'KursorTheme'
    'Accept' = 'application/vnd.github+json'
}

$releases = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases" -Headers $headers
$release = $releases | Where-Object { $_.tag_name -eq $Tag } | Select-Object -First 1

if (-not $release) {
    $body = [System.IO.File]::ReadAllText($notesPath, [System.Text.Encoding]::UTF8)
    $payload = @{
        tag_name = $Tag
        name = "Kursor Theme $($Tag.TrimStart('v'))"
        body = $body
        draft = $false
        prerelease = $false
    } | ConvertTo-Json -Depth 5

    $payload | ConvertFrom-Json | Out-Null
    $payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($payload)

    $release = Invoke-RestMethod -Method Post `
        -Uri "https://api.github.com/repos/$Repo/releases" `
        -Headers $headers -Body $payloadBytes -ContentType 'application/json; charset=utf-8'
    Write-Output "Created release: $($release.html_url)"
}
else {
    Write-Output "Release already exists: $($release.html_url)"
}

$existing = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/$($release.id)/assets" -Headers $headers
$existingNames = @($existing | ForEach-Object { $_.name })

$files = @('KursorThemeSetup.exe', 'KursorTheme-portable.zip')
foreach ($file in $files) {
    if ($existingNames -contains $file) {
        Write-Output "Asset already uploaded: $file"
        continue
    }

    $path = Join-Path $root "dist\$file"
    if (-not (Test-Path $path)) { throw "Asset not found: $path" }

    $sizeMb = [Math]::Round((Get-Item $path).Length / 1MB, 1)
    Write-Output "Uploading $file ($sizeMb MB)..."

    $response = curl.exe -sS -X POST `
        -H "Authorization: Bearer $token" `
        -H "User-Agent: KursorTheme" `
        -H "Content-Type: application/octet-stream" `
        --data-binary "@$path" `
        "https://uploads.github.com/repos/$Repo/releases/$($release.id)/assets?name=$file"

    if ($LASTEXITCODE -ne 0) { throw "Upload failed for ${file}: $response" }

    $asset = $response | ConvertFrom-Json
    Write-Output ("  -> {0} ({1} MB)" -f $asset.name, [Math]::Round($asset.size / 1MB, 1))
}

$assets = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/$($release.id)/assets" -Headers $headers
Write-Output "Release assets:"
$assets | ForEach-Object { Write-Output ("  {0}  {1} MB" -f $_.name, [Math]::Round($_.size / 1MB, 1)) }
