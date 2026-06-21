$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$applyLogPath = Join-Path $logDir "ApprovedControlRoomVerticalAuxScreens.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $applyLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "EnsureApprovedControlRoomVerticalAuxScreens" -LogPath $applyLogPath -TimeoutSeconds 360
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$applyLog = Get-Content -LiteralPath $applyLogPath -Raw

if ($applyLog -notmatch "Approved CR-08 control room vertical auxiliary screens applied\.") {
  Write-Error "Approved control room vertical auxiliary screens apply success marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "ExistingObjectsUntouched=True") {
  Write-Error "Approved control room vertical auxiliary screens log did not confirm existing objects untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "ControlRoomUntouched=True") {
  Write-Error "Approved control room vertical auxiliary screens log did not confirm existing control room untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "CockpitUntouched=True") {
  Write-Error "Approved control room vertical auxiliary screens log did not confirm cockpit untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "EngineRoomUntouched=True") {
  Write-Error "Approved control room vertical auxiliary screens log did not confirm engine room untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "VerticalAuxScreensLeftOfMainScreen=True") {
  Write-Error "Approved control room vertical auxiliary screens log did not confirm left-of-main placement. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "VerticalAuxScreensOverlapsEngineRoom=False") {
  Write-Error "Approved control room vertical auxiliary screens log did not confirm engine-room non-overlap. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "VerticalAuxScreensOverlapsCockpit=False") {
  Write-Error "Approved control room vertical auxiliary screens log did not confirm cockpit non-overlap. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "VerticalAuxScreensOverlapsCr07=False") {
  Write-Error "Approved control room vertical auxiliary screens log did not confirm CR-07 non-overlap. See $applyLogPath"
  exit 1
}

if ($applyLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed|overlap existing|Protected object|Existing CR-08 root") {
  Write-Error "Approved control room vertical auxiliary screens log contains errors. See $applyLogPath"
  exit 1
}

exit 0
