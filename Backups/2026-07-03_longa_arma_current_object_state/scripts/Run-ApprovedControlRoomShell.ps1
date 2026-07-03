$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$applyLogPath = Join-Path $logDir "ApprovedControlRoomShell.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $applyLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "EnsureApprovedControlRoomShell" -LogPath $applyLogPath -TimeoutSeconds 360
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

if ($applyLog -notmatch "Approved CR-01 control room shell applied\.") {
  Write-Error "Approved control room shell apply success marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "CockpitUntouched=True") {
  Write-Error "Approved control room shell log did not confirm cockpit untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "EngineRoomUntouched=True") {
  Write-Error "Approved control room shell log did not confirm engine room untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "ControlRoomPlacedNextToCockpit=True") {
  Write-Error "Approved control room shell log did not confirm cockpit-adjacent placement. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "ControlRoomOverlapsEngineRoom=False") {
  Write-Error "Approved control room shell log did not confirm engine-room non-overlap. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "ControlRoomOverlapsCockpit=False") {
  Write-Error "Approved control room shell log did not confirm cockpit non-overlap. See $applyLogPath"
  exit 1
}

if ($applyLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed|overlaps existing engine room|overlaps existing cockpit|Protected object") {
  Write-Error "Approved control room shell log contains errors. See $applyLogPath"
  exit 1
}

exit 0
