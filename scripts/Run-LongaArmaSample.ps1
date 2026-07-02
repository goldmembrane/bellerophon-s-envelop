$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$scriptPath = Join-Path $projectRoot "scripts\GenerateLongaArmaSample.py"
$sampleRoot = Join-Path $projectRoot "artSample\enemies\longa_arma"

$blenderCandidates = @()
if ($env:BLENDER_EXE) {
  $blenderCandidates += $env:BLENDER_EXE
}

$blenderCommand = Get-Command blender -ErrorAction SilentlyContinue
if ($null -ne $blenderCommand) {
  $blenderCandidates += $blenderCommand.Source
}

$blenderCandidates += @(
  "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 5.0\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.4\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.3\blender.exe",
  "C:\Program Files\Blender Foundation\Blender\blender.exe"
)

$blenderExe = $blenderCandidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
if ($null -eq $blenderExe) {
  Write-Error "Blender executable was not found. Set BLENDER_EXE or add blender to PATH."
  exit 1
}

& $blenderExe --background --python $scriptPath
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$requiredFiles = @(
  "index.html",
  "README.md",
  "TEXTURE_ANALYSIS.md",
  "ASSET_MANIFEST.json",
  "APPROVAL_STATUS.json",
  "blender\longa_arma.blend",
  "exports\longa_arma.fbx",
  "exports\longa_arma.glb",
  "textures\longa_arma_wet_green_albedo.png",
  "textures\longa_arma_wet_green_roughness.png",
  "textures\longa_arma_wet_green_bump.png",
  "textures\longa_arma_dark_blade_albedo.png",
  "textures\longa_arma_dark_blade_roughness.png",
  "textures\longa_arma_slime_albedo.png",
  "renders\front.png",
  "renders\side.png",
  "renders\back.png",
  "renders\reference_comparison.png"
)

foreach ($relative in $requiredFiles) {
  $path = Join-Path $sampleRoot $relative
  if (-not (Test-Path -LiteralPath $path)) {
    Write-Error "Missing generated Longa Arma sample file: $path"
    exit 1
  }
}

exit 0
