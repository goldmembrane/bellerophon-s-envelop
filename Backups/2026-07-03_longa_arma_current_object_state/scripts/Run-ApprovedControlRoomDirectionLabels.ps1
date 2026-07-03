$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$applyLogPath = Join-Path $logDir "ApprovedControlRoomDirectionLabels.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $applyLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "EnsureApprovedControlRoomDirectionLabels" -LogPath $applyLogPath -TimeoutSeconds 360
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$applyLog = Get-Content -LiteralPath $applyLogPath -Raw

if ($applyLog -notmatch "Approved CR-17 control room direction labels applied\.") {
  Write-Error "Approved control room direction labels apply success marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "ExistingObjectsUntouched=True") {
  Write-Error "Approved control room direction labels log did not confirm existing objects untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "ControlRoomUntouched=True") {
  Write-Error "Approved control room direction labels log did not confirm existing control room untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "CockpitUntouched=True") {
  Write-Error "Approved control room direction labels log did not confirm cockpit untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "EngineRoomUntouched=True") {
  Write-Error "Approved control room direction labels log did not confirm engine room untouched. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "EnglishMainLabels=True") {
  Write-Error "Approved control room direction labels log did not confirm English main labels. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "VisibleTextMeshes=8") {
  Write-Error "Approved control room direction labels log did not confirm visible text meshes. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "TextFitChecked=8") {
  Write-Error "Approved control room direction labels log did not confirm text fit record count. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "TextFitsLabelPanels=True") {
  Write-Error "Approved control room direction labels log did not confirm text fits label panels. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "DirectionLabelsOverlapsEngineRoom=False") {
  Write-Error "Approved control room direction labels log did not confirm engine-room non-overlap. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "DirectionLabelsOverlapsCockpit=False") {
  Write-Error "Approved control room direction labels log did not confirm cockpit non-overlap. See $applyLogPath"
  exit 1
}

if ($applyLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed|overlap existing|Protected object") {
  Write-Error "Approved control room direction labels log contains errors. See $applyLogPath"
  exit 1
}

exit 0
