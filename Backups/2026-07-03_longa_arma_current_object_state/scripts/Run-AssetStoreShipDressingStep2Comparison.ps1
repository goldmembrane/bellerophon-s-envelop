$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\AssetStoreShipDressingStep2Comparison.log"
$sampleRoot = Join-Path $projectRoot "artSample\asset_dressing_samples\step02_corridor_floor5_wall2_dense_floorbase_unifiedwall_fullwidthfloor_2026-06-14\unity_applied_comparison"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "CaptureAssetStoreShipDressingStep2Comparison" -LogPath $logPath -TimeoutSeconds 300
$bridgeExitCode = $LASTEXITCODE

$bridgeLog = ""
if (Test-Path -LiteralPath $logPath) {
  $bridgeLog = Get-Content -LiteralPath $logPath -Raw
}

if ($bridgeExitCode -eq 2 -or $bridgeLog -match "Unknown bridge command: CaptureAssetStoreShipDressingStep2Comparison") {
  & (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
  $unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
  $arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "Bellerophon.Editor.Validation.AssetStoreShipDressingEditorValidation.CaptureApprovedStep2Comparison",
    "-logFile", $logPath
  )

  $process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
  if ($process.ExitCode -ne 0) {
    exit $process.ExitCode
  }
}
elseif ($bridgeExitCode -ne 0) {
  exit $bridgeExitCode
}

$log = Get-Content -LiteralPath $logPath -Raw
if ($log -notmatch "Asset Store ship dressing step 2 Unity comparison snapshots saved:") {
  Write-Error "Asset Store ship dressing step 2 comparison success marker was not found. See $logPath"
  exit 1
}

foreach ($fileName in @(
  "index.html",
  "unity_view_01_player_entry.png",
  "unity_view_02_floor_wall_diagonal.png",
  "unity_view_03_ceiling_and_wall_underlook.png",
  "unity_view_04_layout_topdown.png",
  "unity_view_05_floor_stack_detail.png",
  "unity_view_06_cargo_hold_engine_slope.png",
  "unity_view_07_cargo_hold_armory_slope.png",
  "unity_view_08_control_armory_dense_floor_wall.png"
)) {
  $path = Join-Path $sampleRoot $fileName
  if (-not (Test-Path -LiteralPath $path)) {
    Write-Error "Missing comparison output: $path"
    exit 1
  }
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Asset Store ship dressing step 2 comparison log contains errors. See $logPath"
  exit 1
}

exit 0
