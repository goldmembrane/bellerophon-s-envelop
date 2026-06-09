$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\DetailedStep21FullSmokeSuite.log"

$checks = @(
  @{ Step = "Phase 1-18"; Script = "Run-Phase1To18Smokes.ps1"; Name = "MVP phase smoke sweep" },
  @{ Step = "Detailed 13"; Script = "Run-DetailedStep13SeedEntitySmoke.ps1"; Name = "seed entity" },
  @{ Step = "Detailed 14"; Script = "Run-DetailedStep14AlienLifeformSmoke.ps1"; Name = "alien lifeform" },
  @{ Step = "Detailed 15"; Script = "Run-DetailedStep15CargoFreedomLeagueSmoke.ps1"; Name = "Cargo Freedom League" },
  @{ Step = "Detailed 16"; Script = "Run-DetailedStep16SpacePirateSmoke.ps1"; Name = "space pirate" },
  @{ Step = "Detailed 17"; Script = "Run-DetailedStep17SpecialContractsSmoke.ps1"; Name = "special contracts" },
  @{ Step = "Detailed 18"; Script = "Run-DetailedStep18PlanetUxSmoke.ps1"; Name = "planet UX" },
  @{ Step = "Detailed 19"; Script = "Run-DetailedStep19SaveSettingsPlatformSmoke.ps1"; Name = "save/settings/platform" },
  @{ Step = "Detailed 20"; Script = "Run-DetailedStep20PresentationSmoke.ps1"; Name = "presentation" },
  @{ Step = "Detailed 21"; Script = "Run-DetailedStep21BalancePlaytestHardeningSmoke.ps1"; Name = "balance and playtest hardening" }
)

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Set-Content -LiteralPath $logPath -Value "Detailed step 21 full smoke suite started: $(Get-Date -Format o)" -Encoding UTF8

foreach ($check in $checks) {
  $scriptPath = Join-Path $PSScriptRoot $check.Script
  if (-not (Test-Path -LiteralPath $scriptPath)) {
    Add-Content -LiteralPath $logPath -Value "$($check.Step) FAILED: missing script $($check.Script)" -Encoding UTF8
    Write-Error "$($check.Step) script is missing: $scriptPath"
    exit 1
  }

  Write-Host "Running $($check.Step): $($check.Name)"
  Add-Content -LiteralPath $logPath -Value "$($check.Step) START: $($check.Name)" -Encoding UTF8

  $start = Get-Date
  & powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath
  $exitCode = $LASTEXITCODE
  $elapsed = (Get-Date) - $start

  if ($exitCode -ne 0) {
    Add-Content -LiteralPath $logPath -Value ("{0} FAILED after {1:n1}s with exit code {2}" -f $check.Step, $elapsed.TotalSeconds, $exitCode) -Encoding UTF8
    Write-Error "$($check.Step) failed. See $logPath"
    exit $exitCode
  }

  Add-Content -LiteralPath $logPath -Value ("{0} PASSED after {1:n1}s" -f $check.Step, $elapsed.TotalSeconds) -Encoding UTF8
}

Add-Content -LiteralPath $logPath -Value "Detailed step 21 full smoke suite passed: $(Get-Date -Format o)" -Encoding UTF8
Write-Host "Detailed step 21 full smoke suite passed. Log: $logPath"
exit 0
