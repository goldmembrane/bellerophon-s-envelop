[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$sampleRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$requiredRenders = @(
    'renders/01_volumetric_multiview.png',
    'renders/02_first_person_gameplay.png'
)

foreach ($relativePath in $requiredRenders) {
    $fullPath = Join-Path $sampleRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Required 3D VFX approval render is missing: $fullPath"
    }

    $image = [System.Drawing.Image]::FromFile($fullPath)
    try {
        if ($image.Width -lt 1280 -or $image.Height -lt 720) {
            throw "3D VFX approval render is below review resolution: $fullPath"
        }

        Write-Output (
            $fullPath +
            ' (' +
            $image.Width.ToString() +
            'x' +
            $image.Height.ToString() +
            ')')
    }
    finally {
        $image.Dispose()
    }
}
