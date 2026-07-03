$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$applyLogPath = Join-Path $logDir "ApprovedEngineRoomHealthScreen.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $applyLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "EnsureApprovedEngineRoomHealthScreen" -LogPath $applyLogPath -TimeoutSeconds 360
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$scenePath = Join-Path $projectRoot "Assets\_Project\Scenes\CargoRunMvp.unity"
if (Test-Path -LiteralPath $scenePath) {
  $sceneText = [System.IO.File]::ReadAllText($scenePath)
  $sceneText = [System.Text.RegularExpressions.Regex]::Replace($sceneText, '[ \t]+(?=\r?\n)', '')
  $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
  [System.IO.File]::WriteAllText($scenePath, $sceneText, $utf8NoBom)
}

$applyLog = Get-Content -LiteralPath $applyLogPath -Raw

if ($applyLog -notmatch "Approved ER-09 engine room health screen applied\.") {
  Write-Error "Approved ER-09 health screen apply success marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "PlacementClock=") {
  Write-Error "Approved ER-09 health screen log did not confirm placement marker. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "EngineRoomRootFound=True") {
  Write-Error "Approved ER-09 health screen log did not confirm engine room root discovery. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "UnityComparisonSaved=True") {
  Write-Error "Approved ER-09 health screen log did not confirm Unity comparison capture. See $applyLogPath"
  exit 1
}

if ($applyLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Approved ER-09 health screen log contains errors. See $applyLogPath"
  exit 1
}

exit 0
