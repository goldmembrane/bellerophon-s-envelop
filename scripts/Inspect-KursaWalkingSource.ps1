[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$scriptPath = Join-Path $PSScriptRoot "InspectKursaWalkingSource.py"
$blenderCandidates = @()
if ($env:BLENDER_EXE) {
  $blenderCandidates += $env:BLENDER_EXE
}
$blenderCandidates += @(
  "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 5.0\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe"
)
$blenderExe = $blenderCandidates |
  Where-Object { Test-Path -LiteralPath $_ } |
  Select-Object -First 1
if (-not $blenderExe) {
  throw "Blender executable was not found."
}

& $blenderExe --background --python-exit-code 1 --python $scriptPath
if ($LASTEXITCODE -ne 0) {
  throw "Kursa walking source inspection failed with exit code $LASTEXITCODE."
}
