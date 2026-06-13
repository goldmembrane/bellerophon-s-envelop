$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\AssetDressingStep02SteelPlateSample.log"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "CaptureAssetDressingStep02SteelPlateSample" -LogPath $logPath -TimeoutSeconds 300
$bridgeExitCode = $LASTEXITCODE

$bridgeLog = ""
if (Test-Path -LiteralPath $logPath) {
  $bridgeLog = Get-Content -LiteralPath $logPath -Raw
}

if ($bridgeExitCode -eq 2 -or $bridgeLog -match "Unknown bridge command: CaptureAssetDressingStep02SteelPlateSample") {
  & (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
  $unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
  $arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "Bellerophon.Editor.Validation.AssetDressingStep02SampleRenderer.CaptureSteelPlate",
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
if ($log -notmatch "Asset dressing step 02 steel plate sample renders saved:") {
  Write-Error "Asset dressing step 02 steel plate sample success marker was not found. See $logPath"
  exit 1
}

$sampleRoot = Join-Path $projectRoot "artSample\asset_dressing_samples\step02_corridors_thresholds_steel_plate_2026-06-14"
$requiredImages = @(
  "view_01_player_entry.png",
  "view_02_threshold_diagonal.png",
  "view_03_layout_topdown.png",
  "view_04_module_stack.png"
)

foreach ($imageName in $requiredImages) {
  $imagePath = Join-Path $sampleRoot $imageName
  if (-not (Test-Path -LiteralPath $imagePath)) {
    Write-Error "Missing step 02 steel plate sample image: $imagePath"
    exit 1
  }
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Asset dressing step 02 steel plate sample log contains errors. See $logPath"
  exit 1
}

exit 0
