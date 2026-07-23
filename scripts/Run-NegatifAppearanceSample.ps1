$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$generator = Join-Path $projectRoot "scripts\GenerateNegatifAppearanceSample.py"
$composer = Join-Path $projectRoot "scripts\ComposeNegatifAppearanceBoard.py"
$sampleRoot = Join-Path $projectRoot "artSample\enemies\negatif\appearance_reference_sync"
$sourceName = "n$([char]0x00E9)gatif.fbx"
$sourcePath = Join-Path $projectRoot (Join-Path "enemies model" $sourceName)

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
  "C:\Program Files\Blender Foundation\Blender 4.4\blender.exe"
)
$blenderExe = $blenderCandidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
if ($null -eq $blenderExe) {
  throw "Blender executable was not found."
}

$sourceHashBefore = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
& $blenderExe --factory-startup --background --python-exit-code 1 --python $generator
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}
$primaryRender = Join-Path $sampleRoot "renders\01_reference_matched_three_quarter.png"
if (-not (Test-Path -LiteralPath $primaryRender)) {
  throw "Blender did not complete the Negatif sample render."
}

python $composer
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$requiredFiles = @(
  "index.html",
  "README.md",
  "TEXTURE_ANALYSIS.md",
  "ASSET_MANIFEST.json",
  "APPROVAL_STATUS.json",
  "GEOMETRY_VALIDATION.json",
  "blender\Negatif_Appearance_ReferenceSync.blend",
  "exports\Negatif_Appearance_ReferenceSync.glb",
  "source\Negatif_Source_Unmodified.fbx",
  "source\negatif_reference.png",
  "renders\01_reference_matched_three_quarter.png",
  "renders\02_side.png",
  "renders\03_front.png",
  "renders\04_back_three_quarter.png",
  "renders\05_reference_comparison.png",
  "renders\06_material_texture_breakdown.png"
)
foreach ($relative in $requiredFiles) {
  $path = Join-Path $sampleRoot $relative
  if (-not (Test-Path -LiteralPath $path)) {
    throw "Missing generated Negatif sample file: $path"
  }
}

$geometryValidation = Get-Content -LiteralPath (Join-Path $sampleRoot "GEOMETRY_VALIDATION.json") -Raw -Encoding UTF8 | ConvertFrom-Json
if ($geometryValidation.result -ne "PASS" -or $geometryValidation.modeling_changed) {
  throw "Negatif geometry preservation validation did not pass."
}
$sourceHashAfter = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$sourceCopyHash = (Get-FileHash -LiteralPath (Join-Path $sampleRoot "source\Negatif_Source_Unmodified.fbx") -Algorithm SHA256).Hash
if ($sourceHashBefore -ne $sourceHashAfter -or $sourceHashBefore -ne $sourceCopyHash) {
  throw "Negatif source FBX hash changed or the sample source copy differs."
}

Write-Output "Negatif appearance sample generated."
Write-Output "SourceHash=$sourceHashAfter"
Write-Output "GeometryResult=$($geometryValidation.result)"
Write-Output "SampleRoot=$sampleRoot"
