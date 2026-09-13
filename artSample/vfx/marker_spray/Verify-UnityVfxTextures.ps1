[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$sampleRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$textureNames = @(
    'marker_spray_mist_particle.png',
    'marker_spray_droplet_particle.png',
    'marker_spray_red_cone_mist_texture.png'
)

$results = foreach ($textureName in $textureNames) {
    $texturePath = Join-Path $sampleRoot $textureName
    if (-not (Test-Path -LiteralPath $texturePath)) {
        throw "Missing texture: $texturePath"
    }

    $bitmap = [System.Drawing.Bitmap]::new($texturePath)
    try {
        $sampleStep = [Math]::Max(1, [int][Math]::Floor([Math]::Min($bitmap.Width, $bitmap.Height) / 160))
        $alphaMin = 255
        $alphaMax = 0
        $transparentSamples = 0
        $visibleSamples = 0
        $sampleCount = 0
        $borderAlphaMax = 0
        $visibleMinX = $bitmap.Width
        $visibleMinY = $bitmap.Height
        $visibleMaxX = -1
        $visibleMaxY = -1

        for ($y = 0; $y -lt $bitmap.Height; $y += $sampleStep) {
            for ($x = 0; $x -lt $bitmap.Width; $x += $sampleStep) {
                $alpha = $bitmap.GetPixel($x, $y).A
                $sampleCount++
                if ($alpha -lt $alphaMin) { $alphaMin = $alpha }
                if ($alpha -gt $alphaMax) { $alphaMax = $alpha }
                if ($alpha -le 5) { $transparentSamples++ }
                if ($alpha -ge 12) {
                    $visibleSamples++
                    if ($x -lt $visibleMinX) { $visibleMinX = $x }
                    if ($x -gt $visibleMaxX) { $visibleMaxX = $x }
                    if ($y -lt $visibleMinY) { $visibleMinY = $y }
                    if ($y -gt $visibleMaxY) { $visibleMaxY = $y }
                }
            }
        }

        for ($x = 0; $x -lt $bitmap.Width; $x += $sampleStep) {
            $topAlpha = $bitmap.GetPixel($x, 0).A
            $bottomAlpha = $bitmap.GetPixel($x, $bitmap.Height - 1).A
            if ($topAlpha -gt $borderAlphaMax) { $borderAlphaMax = $topAlpha }
            if ($bottomAlpha -gt $borderAlphaMax) { $borderAlphaMax = $bottomAlpha }
        }
        for ($y = 0; $y -lt $bitmap.Height; $y += $sampleStep) {
            $leftAlpha = $bitmap.GetPixel(0, $y).A
            $rightAlpha = $bitmap.GetPixel($bitmap.Width - 1, $y).A
            if ($leftAlpha -gt $borderAlphaMax) { $borderAlphaMax = $leftAlpha }
            if ($rightAlpha -gt $borderAlphaMax) { $borderAlphaMax = $rightAlpha }
        }

        $transparentRatio = if ($sampleCount -gt 0) { $transparentSamples / $sampleCount } else { 0 }
        $visibleRatio = if ($sampleCount -gt 0) { $visibleSamples / $sampleCount } else { 0 }
        $hasAlphaFormat = $bitmap.PixelFormat.ToString() -match 'Alpha|Argb|PArgb'
        $passes = $hasAlphaFormat -and
            $alphaMin -eq 0 -and
            $alphaMax -ge 64 -and
            $borderAlphaMax -le 8 -and
            $transparentRatio -ge 0.20 -and
            $visibleRatio -ge 0.002 -and
            $visibleMaxX -ge 0

        [pscustomobject]@{
            Texture = $textureName
            Dimensions = "$($bitmap.Width)x$($bitmap.Height)"
            PixelFormat = $bitmap.PixelFormat.ToString()
            AlphaMin = $alphaMin
            AlphaMax = $alphaMax
            BorderAlphaMax = $borderAlphaMax
            TransparentPercent = [Math]::Round($transparentRatio * 100, 2)
            VisiblePercent = [Math]::Round($visibleRatio * 100, 2)
            VisibleBounds = "$visibleMinX,$visibleMinY-$visibleMaxX,$visibleMaxY"
            Pass = $passes
        }
    }
    finally {
        $bitmap.Dispose()
    }
}

$results | Format-Table -AutoSize
if ($results.Pass -contains $false) {
    throw 'One or more VFX textures failed the Unity-ready alpha gate.'
}
