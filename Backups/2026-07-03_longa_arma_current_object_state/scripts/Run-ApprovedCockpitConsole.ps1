$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$applyLogPath = Join-Path $logDir "ApprovedCockpitConsole.log"
$captureLogPath = Join-Path $logDir "ApprovedCockpitConsoleComparison.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $applyLogPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $captureLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "EnsureApprovedCockpitConsole" -LogPath $applyLogPath -TimeoutSeconds 240
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "CaptureApprovedCockpitConsoleComparison" -LogPath $captureLogPath -TimeoutSeconds 240
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

if ($applyLog -notmatch "Approved cockpit 02 console applied\.") {
  Write-Error "Approved cockpit console apply success marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Approved cockpit 02 console validation passed\.") {
  Write-Error "Approved cockpit console validation marker was not found. See $applyLogPath"
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

if ($captureLog -notmatch "Approved cockpit 02 console Unity comparison snapshots saved:") {
  Write-Error "Approved cockpit console comparison capture marker was not found. See $captureLogPath"
  exit 1
}

if ($applyLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed" -or
    $captureLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Approved cockpit console logs contain errors. See $applyLogPath and $captureLogPath"
  exit 1
}

exit 0
