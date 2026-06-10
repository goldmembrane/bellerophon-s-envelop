$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\PostDetailedStage3GameplayPropsSmoke.log"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "ValidatePostDetailedStage3GameplayProps" -LogPath $logPath -TimeoutSeconds 300
$bridgeExitCode = $LASTEXITCODE

$bridgeLog = ""
if (Test-Path -LiteralPath $logPath) {
  $bridgeLog = Get-Content -LiteralPath $logPath -Raw
}

if ($bridgeExitCode -eq 2 -or $bridgeLog -match "Unknown bridge command: ValidatePostDetailedStage3GameplayProps") {
  & (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
  $unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
  $arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "Bellerophon.Editor.Validation.PostDetailedStage3GameplayPropsEditorValidation.Run",
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
if ($log -notmatch "Post-detailed stage 3 gameplay props editor validation passed\.") {
  Write-Error "Post-detailed stage 3 gameplay props smoke success marker was not found. See $logPath"
  exit 1
}

if ($log -notmatch "Post-detailed stage 3 gameplay props details:") {
  Write-Error "Post-detailed stage 3 gameplay props smoke details were not found. See $logPath"
  exit 1
}

if ($log -notmatch "SampleOnlyLooseProps=0") {
  Write-Error "Post-detailed stage 3 art validation did not confirm that sample-only loose props are absent. See $logPath"
  exit 1
}

if ($log -notmatch "CargoStraps=2" -or $log -notmatch "DeviceSurfaces=7") {
  Write-Error "Post-detailed stage 3 art validation did not confirm the approved cargo/device surface counts. See $logPath"
  exit 1
}

if ($log -notmatch "ArtSampleMatch=True") {
  Write-Error "Post-detailed stage 3 art validation did not confirm alignment with the approved artSample file. See $logPath"
  exit 1
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Post-detailed stage 3 gameplay props smoke log contains errors. See $logPath"
  exit 1
}

exit 0
