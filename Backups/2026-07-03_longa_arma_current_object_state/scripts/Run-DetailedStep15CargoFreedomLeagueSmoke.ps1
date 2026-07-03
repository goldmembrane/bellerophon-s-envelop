$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\DetailedStep15CargoFreedomLeagueSmoke.log"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "ValidateDetailedStep15CargoFreedomLeague" -LogPath $logPath -TimeoutSeconds 180
$bridgeExitCode = $LASTEXITCODE

if ($bridgeExitCode -eq 2) {
  Write-Error "No open Unity editor process found for $projectRoot"
  exit 2
}

if ($bridgeExitCode -ne 0) {
  exit $bridgeExitCode
}

$log = Get-Content -LiteralPath $logPath -Raw
if ($log -notmatch "Detailed step 15 Cargo Freedom League editor validation passed\.") {
  Write-Error "Detailed step 15 Cargo Freedom League smoke success marker was not found. See $logPath"
  exit 1
}

if ($log -notmatch "Detailed step 15 Cargo Freedom League validation details:") {
  Write-Error "Detailed step 15 Cargo Freedom League smoke details were not found. See $logPath"
  exit 1
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Detailed step 15 Cargo Freedom League smoke log contains errors. See $logPath"
  exit 1
}

exit 0
