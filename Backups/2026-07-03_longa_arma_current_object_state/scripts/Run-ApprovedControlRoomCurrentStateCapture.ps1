$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$captureLogPath = Join-Path $logDir "ApprovedControlRoomCurrentStateCapture.log"
$captureOutputPath = Join-Path $projectRoot "artSample\control_room_current\editor_current\control_room_current_objects.md"
$generatedSnapshotPath = Join-Path $projectRoot "Assets\_Project\Editor\Validation\ApprovedControlRoomCurrentStateSnapshot.cs"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $captureLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "CaptureApprovedControlRoomCurrentState" -LogPath $captureLogPath -TimeoutSeconds 180
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$captureLog = Get-Content -LiteralPath $captureLogPath -Raw

if ($captureLog -notmatch "Approved control room current state capture saved:") {
  Write-Error "Approved control room current state capture success marker was not found. See $captureLogPath"
  exit 1
}

if (-not (Test-Path -LiteralPath $captureOutputPath)) {
  Write-Error "Approved control room current state capture output was not found: $captureOutputPath"
  exit 1
}

if (-not (Test-Path -LiteralPath $generatedSnapshotPath)) {
  Write-Error "Approved control room current state generated snapshot was not found: $generatedSnapshotPath"
  exit 1
}

if ($captureLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Approved control room current state capture log contains errors. See $captureLogPath"
  exit 1
}

exit 0
