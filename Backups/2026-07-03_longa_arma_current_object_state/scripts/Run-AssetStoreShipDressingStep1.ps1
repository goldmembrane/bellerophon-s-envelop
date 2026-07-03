$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\AssetStoreShipDressingStep1.log"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "ValidateAssetStoreShipDressingStep1" -LogPath $logPath -TimeoutSeconds 300
$bridgeExitCode = $LASTEXITCODE

$bridgeLog = ""
if (Test-Path -LiteralPath $logPath) {
  $bridgeLog = Get-Content -LiteralPath $logPath -Raw
}

if ($bridgeExitCode -eq 2 -or $bridgeLog -match "Unknown bridge command: ValidateAssetStoreShipDressingStep1") {
  & (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
  $unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
  $arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "Bellerophon.Editor.Validation.AssetStoreShipDressingEditorValidation.Run",
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
if ($log -notmatch "Asset Store ship dressing step 1 validation passed\.") {
  Write-Error "Asset Store ship dressing step 1 success marker was not found. See $logPath"
  exit 1
}

if ($log -notmatch "Asset Store ship dressing step 1 details:") {
  Write-Error "Asset Store ship dressing step 1 details were not found. See $logPath"
  exit 1
}

if ($log -notmatch "TopRoots=10" -or $log -notmatch "CorridorRoots=10" -or $log -notmatch "ImportedPacks=4") {
  Write-Error "Asset Store ship dressing step 1 did not confirm the expected root or imported-pack counts. See $logPath"
  exit 1
}

if ($log -notmatch "EnabledColliders=0") {
  Write-Error "Asset Store ship dressing roots must not introduce enabled colliders. See $logPath"
  exit 1
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Asset Store ship dressing step 1 log contains errors. See $logPath"
  exit 1
}

exit 0
