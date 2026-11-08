# Generates src/KursorTheme/Assets/AppIcon.ico (multi-size, PNG-compressed).
# Run from the repository root:  powershell -ExecutionPolicy Bypass -File tools/make-icon.ps1

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root "src\KursorTheme\Assets"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$outFile = Join-Path $outDir "AppIcon.ico"

$sizes = @(16, 24, 32, 48, 64, 128, 256)

function New-RoundedPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $d = [Math]::Min($r * 2.0, [Math]::Min($w, $h))
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function Render-Icon([int]$size) {
    $bmp = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    [float]$scale = $size / 256.0

    # Background tile
    $bgRect = [System.Drawing.RectangleF]::new(6.0 * $scale, 6.0 * $scale, 244.0 * $scale, 244.0 * $scale)
    $bgPath = New-RoundedPath $bgRect.X $bgRect.Y $bgRect.Width $bgRect.Height (52.0 * $scale)
    $bgTop = [System.Drawing.Color]::FromArgb(255, 27, 32, 42)
    $bgBottom = [System.Drawing.Color]::FromArgb(255, 11, 13, 18)
    $bgBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($bgRect, $bgTop, $bgBottom, [single]45.0)
    $g.FillPath($bgBrush, $bgPath)

    $borderPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(70, 90, 210, 235), [single][Math]::Max(1.0, 1.6 * $scale))
    $g.DrawPath($borderPen, $bgPath)

    # Soft glow
    $glowPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $glowPath.AddEllipse(48.0 * $scale, 38.0 * $scale, 176.0 * $scale, 184.0 * $scale)
    $glowBrush = [System.Drawing.Drawing2D.PathGradientBrush]::new($glowPath)
    $glowBrush.CenterColor = [System.Drawing.Color]::FromArgb(90, 54, 229, 255)
    $glowBrush.SurroundColors = [System.Drawing.Color[]]@([System.Drawing.Color]::FromArgb(0, 139, 92, 246))
    $g.FillPath($glowBrush, $glowPath)

    # Cursor arrow
    $points = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(78.0 * $scale, 44.0 * $scale),
        [System.Drawing.PointF]::new(78.0 * $scale, 190.0 * $scale),
        [System.Drawing.PointF]::new(112.0 * $scale, 156.0 * $scale),
        [System.Drawing.PointF]::new(136.0 * $scale, 210.0 * $scale),
        [System.Drawing.PointF]::new(160.0 * $scale, 199.0 * $scale),
        [System.Drawing.PointF]::new(136.0 * $scale, 146.0 * $scale),
        [System.Drawing.PointF]::new(180.0 * $scale, 146.0 * $scale)
    )

    $arrow = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $arrow.AddPolygon($points)

    $start = [System.Drawing.PointF]::new(78.0 * $scale, 44.0 * $scale)
    $end = [System.Drawing.PointF]::new(180.0 * $scale, 210.0 * $scale)
    $cyan = [System.Drawing.Color]::FromArgb(255, 54, 229, 255)
    $violet = [System.Drawing.Color]::FromArgb(255, 139, 92, 246)
    $arrowBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($start, $end, $cyan, $violet)
    $g.FillPath($arrowBrush, $arrow)

    $outline = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(150, 8, 14, 20), [single][Math]::Max(1.0, 2.4 * $scale))
    $outline.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($outline, $arrow)

    $g.Dispose()
    return $bmp
}

$images = @()
foreach ($size in $sizes) {
    $bmp = Render-Icon $size
    $ms = [System.IO.MemoryStream]::new()
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += , @($size, $ms.ToArray())
    $bmp.Dispose()
    $ms.Dispose()
}

$stream = [System.IO.MemoryStream]::new()
$writer = [System.IO.BinaryWriter]::new($stream)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$images.Count)

$offset = 6 + 16 * $images.Count
foreach ($image in $images) {
    [int]$size = $image[0]
    [byte[]]$data = $image[1]
    $dimension = 0
    if ($size -lt 256) { $dimension = $size }
    $writer.Write([byte]$dimension)
    $writer.Write([byte]$dimension)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$data.Length)
    $writer.Write([UInt32]$offset)
    $offset += $data.Length
}

foreach ($image in $images) {
    $writer.Write([byte[]]$image[1])
}

$writer.Flush()
[System.IO.File]::WriteAllBytes($outFile, $stream.ToArray())
$writer.Dispose()
$stream.Dispose()

Write-Output "Wrote $outFile ($([Math]::Round((Get-Item $outFile).Length / 1024, 1)) KB)"
