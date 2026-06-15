$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$applyLogPath = Join-Path $logDir "ApprovedCockpitDirection.log"
$captureLogPath = Join-Path $logDir "ApprovedCockpitDirectionComparison.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $applyLogPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $captureLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "EnsureApprovedCockpitDirection" -LogPath $applyLogPath -TimeoutSeconds 360
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "CaptureApprovedCockpitDirectionComparison" -LogPath $captureLogPath -TimeoutSeconds 360
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

if ($applyLog -notmatch "Approved cockpit 11 direction applied\.") {
  Write-Error "Approved cockpit direction apply success marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Approved cockpit 11 direction validation passed\.") {
  Write-Error "Approved cockpit direction validation marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Approved cockpit 01 structure validation passed\.") {
  Write-Error "Approved cockpit structure validation marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Approved cockpit 01 window validation passed\.") {
  Write-Error "Approved cockpit window validation marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Approved cockpit 02 console validation passed\.") {
  Write-Error "Approved cockpit console validation marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Approved cockpit 04 warning validation passed\.") {
  Write-Error "Approved cockpit warning validation marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Cargo ship visual modeling disabled validation passed\.") {
  Write-Error "Legacy cargo ship visual modeling was not confirmed disabled. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Modeling inspection mode validation passed\.") {
  Write-Error "Tutorial/modeling inspection mode validation marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Modeling inspection free camera validation passed\.") {
  Write-Error "Modeling inspection free camera validation marker was not found. See $applyLogPath"
  exit 1
}

if ($captureLog -notmatch "Approved cockpit 11 direction Unity comparison snapshots saved:") {
  Write-Error "Approved cockpit direction comparison capture marker was not found. See $captureLogPath"
  exit 1
}

if ($applyLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed" -or
    $captureLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Approved cockpit direction logs contain errors. See $applyLogPath and $captureLogPath"
  exit 1
}

exit 0
