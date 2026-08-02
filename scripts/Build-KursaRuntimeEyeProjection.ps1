[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $PSScriptRoot "BuildKursaRuntimeEyeProjection.py"
$approvedBlend = Join-Path $projectRoot "artSample\enemies\kursa\appearance_reference_sync\blender\Kursa_Appearance_ReferenceSync.blend"

$blenderCandidates = @()
if ($env:BLENDER_EXE) {
  $blenderCandidates += $env:BLENDER_EXE
}
$blenderCandidates += @(
  "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 5.0\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.4\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.3\blender.exe",
  "C:\Program Files\Blender Foundation\Blender\blender.exe"
)
$blenderExe = $blenderCandidates |
  Where-Object { Test-Path -LiteralPath $_ } |
  Select-Object -First 1
if (-not $blenderExe) {
  throw "Blender executable was not found. Set BLENDER_EXE or install a supported Blender version."
}
if (-not (Test-Path -LiteralPath $approvedBlend)) {
  throw "Approved Kursa Blend was not found: $approvedBlend"
}

& $blenderExe --background $approvedBlend --python-exit-code 1 --python $scriptPath
if ($LASTEXITCODE -ne 0) {
  throw "Kursa runtime eye projection export failed with exit code $LASTEXITCODE."
}
