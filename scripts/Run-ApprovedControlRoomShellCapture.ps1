$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$captureLogPath = Join-Path $logDir "ApprovedControlRoomShellCapture.log"
$captureOutputPath = Join-Path $projectRoot "artSample\control_room_shell\editor_current\cr01_current_objects.md"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $captureLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "CaptureApprovedControlRoomShellCurrentObjects" -LogPath $captureLogPath -TimeoutSeconds 180
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$captureLog = Get-Content -LiteralPath $captureLogPath -Raw

if ($captureLog -notmatch "Approved CR-01 current object capture saved:") {
  Write-Error "Approved control room shell capture success marker was not found. See $captureLogPath"
  exit 1
}

if (-not (Test-Path -LiteralPath $captureOutputPath)) {
  Write-Error "Approved control room shell capture output was not found: $captureOutputPath"
  exit 1
}

if ($captureLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Approved control room shell capture log contains errors. See $captureLogPath"
  exit 1
}

exit 0
