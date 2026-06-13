$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\AssetStoreShipDressingStep2.log"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "ValidateAssetStoreShipDressingStep2" -LogPath $logPath -TimeoutSeconds 300
$bridgeExitCode = $LASTEXITCODE

$bridgeLog = ""
if (Test-Path -LiteralPath $logPath) {
  $bridgeLog = Get-Content -LiteralPath $logPath -Raw
}

if ($bridgeExitCode -eq 2 -or $bridgeLog -match "Unknown bridge command: ValidateAssetStoreShipDressingStep2") {
  & (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
  $unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
  $arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "Bellerophon.Editor.Validation.AssetStoreShipDressingEditorValidation.RunStep2",
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
if ($log -notmatch "Asset Store ship dressing step 2 corridor validation passed\.") {
  Write-Error "Asset Store ship dressing step 2 success marker was not found. See $logPath"
  exit 1
}

if ($log -notmatch "Asset Store ship dressing step 2 details:") {
  Write-Error "Asset Store ship dressing step 2 details were not found. See $logPath"
  exit 1
}

if ($log -notmatch "CorridorRoots=10") {
  Write-Error "Asset Store ship dressing step 2 did not confirm all corridor roots. See $logPath"
  exit 1
}

if ($log -notmatch "EnabledColliders=0") {
  Write-Error "Asset Store ship dressing step 2 must keep imported dressing colliders disabled. See $logPath"
  exit 1
}

if ($log -notmatch "ErrorMaterialRenderers=0") {
  Write-Error "Asset Store ship dressing step 2 still has magenta/error-shader renderers. See $logPath"
  exit 1
}

if ($log -notmatch "ThresholdCenterBlockers=0") {
  Write-Error "Asset Store ship dressing step 2 still has pass-through visual doorway blockers. See $logPath"
  exit 1
}

if ($log -notmatch "SolidWallBackers=\d+") {
  Write-Error "Asset Store ship dressing step 2 did not report solid wall backers. See $logPath"
  exit 1
}

if ($log -notmatch "OpaqueWallBackings=\d+") {
  Write-Error "Asset Store ship dressing step 2 did not report opaque wall backings. See $logPath"
  exit 1
}

if ($log -notmatch "EnabledLegacyCorridorRenderers=0") {
  Write-Error "Asset Store ship dressing step 2 must hide legacy graybox corridor renderers. See $logPath"
  exit 1
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Asset Store ship dressing step 2 log contains errors. See $logPath"
  exit 1
}

exit 0
