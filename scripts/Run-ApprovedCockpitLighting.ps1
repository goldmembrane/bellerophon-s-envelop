$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$applyLogPath = Join-Path $logDir "ApprovedCockpitLighting.log"
$captureLogPath = Join-Path $logDir "ApprovedCockpitLightingComparison.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $applyLogPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $captureLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "EnsureApprovedCockpitLighting" -LogPath $applyLogPath -TimeoutSeconds 240
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "CaptureApprovedCockpitLightingComparison" -LogPath $captureLogPath -TimeoutSeconds 240
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
$captureLog = Get-Content -LiteralPath $captureLogPath -Raw

if ($applyLog -notmatch "Approved cockpit 12 inspection lighting applied\.") {
  Write-Error "Approved cockpit 12 apply success marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Approved cockpit 12 inspection lighting validation passed\.") {
  Write-Error "Approved cockpit 12 validation marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "EditorReviewApplied=True") {
  Write-Error "Approved cockpit 12 editor review marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "RuntimeLights=3") {
  Write-Error "Approved cockpit 12 runtime light count marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "WarningOverlap=False") {
  Write-Error "Approved cockpit 12 warning-overlap marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Approved cockpit 04 warning validation passed\.") {
  Write-Error "Approved cockpit 04 warning validation marker was not found. See $applyLogPath"
  exit 1
}

if ($captureLog -notmatch "Approved cockpit 12 inspection lighting Unity comparison snapshots saved:") {
  Write-Error "Approved cockpit 12 comparison capture marker was not found. See $captureLogPath"
  exit 1
}

if ($applyLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed" -or
    $captureLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Approved cockpit 12 logs contain errors. See $applyLogPath and $captureLogPath"
  exit 1
}

exit 0
