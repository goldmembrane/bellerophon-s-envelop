$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$applyLogPath = Join-Path $logDir "ApprovedControlRoomAuxScreen.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $applyLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "EnsureApprovedControlRoomAuxScreen" -LogPath $applyLogPath -TimeoutSeconds 360
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

if ($applyLog -notmatch "Approved CR-07 control room auxiliary screen applied\.") {
  Write-Error "Approved control room auxiliary screen apply success marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "ControlRoomUntouched=True") {
  Write-Error "Approved control room auxiliary screen log did not confirm existing control room untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "CockpitUntouched=True") {
  Write-Error "Approved control room auxiliary screen log did not confirm cockpit untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "EngineRoomUntouched=True") {
  Write-Error "Approved control room auxiliary screen log did not confirm engine room untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "AuxScreenAboveMainScreen=True") {
  Write-Error "Approved control room auxiliary screen log did not confirm above-main-screen placement. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "DisplayTextureApplied=True") {
  Write-Error "Approved control room auxiliary screen log did not confirm C2 display texture application. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "AuxScreenOverlapsEngineRoom=False") {
  Write-Error "Approved control room auxiliary screen log did not confirm engine-room non-overlap. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "AuxScreenOverlapsCockpit=False") {
  Write-Error "Approved control room auxiliary screen log did not confirm cockpit non-overlap. See $applyLogPath"
  exit 1
}

if ($applyLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed|overlaps existing engine room|overlaps existing cockpit|Protected object") {
  Write-Error "Approved control room auxiliary screen log contains errors. See $applyLogPath"
  exit 1
}

exit 0
