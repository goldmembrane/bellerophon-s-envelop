$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\AssetDressingStep02SelectedCorridorSample.log"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "CaptureAssetDressingStep02SelectedCorridorSample" -LogPath $logPath -TimeoutSeconds 300
$bridgeExitCode = $LASTEXITCODE

$bridgeLog = ""
if (Test-Path -LiteralPath $logPath) {
  $bridgeLog = Get-Content -LiteralPath $logPath -Raw
}

if ($bridgeExitCode -eq 2 -or $bridgeLog -match "Unknown bridge command: CaptureAssetDressingStep02SelectedCorridorSample") {
  & (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
  $unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
  $arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "Bellerophon.Editor.Validation.AssetDressingStep02SelectedCorridorSampleRenderer.Capture",
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
if ($log -notmatch "Asset dressing step 02 selected corridor sample renders saved:") {
  Write-Error "Asset dressing step 02 selected corridor sample success marker was not found. See $logPath"
  exit 1
}

$sampleRoot = Join-Path $projectRoot "artSample\asset_dressing_samples\step02_corridor_floor5_wall2_dense_floorbase_unifiedwall_fullwidthfloor_2026-06-14"
$requiredFiles = @(
  "view_01_player_entry.png",
  "view_02_floor_wall_diagonal.png",
  "view_03_ceiling_and_wall_underlook.png",
  "view_04_layout_topdown.png",
  "view_05_floor_stack_detail.png",
  "README.md",
  "ASSET_MANIFEST.md",
  "APPROVAL_STATUS.json",
  "index.html"
)

foreach ($fileName in $requiredFiles) {
  $filePath = Join-Path $sampleRoot $fileName
  if (-not (Test-Path -LiteralPath $filePath)) {
    Write-Error "Missing selected corridor sample file: $filePath"
    exit 1
  }
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Asset dressing step 02 selected corridor sample log contains errors. See $logPath"
  exit 1
}

Write-Host "Selected corridor sample generated: $sampleRoot"
exit 0
