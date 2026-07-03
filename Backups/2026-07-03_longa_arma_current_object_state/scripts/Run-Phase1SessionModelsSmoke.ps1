$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\Phase1SessionModelsSmoke.log"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "ValidatePhase1SessionModels" -LogPath $logPath -TimeoutSeconds 180
$bridgeExitCode = $LASTEXITCODE

if ($bridgeExitCode -eq 2) {
  Write-Error "No open Unity editor process found for $projectRoot"
  exit 2
}

if ($bridgeExitCode -ne 0) {
  exit $bridgeExitCode
}

$log = Get-Content -LiteralPath $logPath -Raw
if ($log -notmatch "Phase 1 session models editor validation passed\.") {
  Write-Error "Phase 1 session models smoke success marker was not found. See $logPath"
  exit 1
}

if ($log -notmatch "Phase 1 session model details:") {
  Write-Error "Phase 1 session models smoke details were not found. See $logPath"
  exit 1
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Phase 1 session models smoke log contains errors. See $logPath"
  exit 1
}

exit 0
