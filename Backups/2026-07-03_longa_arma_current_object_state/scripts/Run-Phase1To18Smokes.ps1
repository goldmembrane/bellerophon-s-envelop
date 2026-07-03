$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\Phase1To18Smokes.log"

$checks = @(
  @{ Phase = 1; Script = "Run-Phase1SessionModelsSmoke.ps1"; Name = "session models" },
  @{ Phase = 2; Script = "Run-Phase2PlayModeSmoke.ps1"; Name = "player MVP" },
  @{ Phase = 3; Script = "Run-Phase3InteractionSystemSmoke.ps1"; Name = "interaction system" },
  @{ Phase = 4; Script = "Run-Phase4CargoShipGrayboxSmoke.ps1"; Name = "cargo ship graybox" },
  @{ Phase = 5; Script = "Run-Phase5ShipStateModelsSmoke.ps1"; Name = "ship state models" },
  @{ Phase = 6; Script = "Run-Phase6RoomInteractionsSmoke.ps1"; Name = "room interactions" },
  @{ Phase = 7; Script = "Run-Phase7NewGameStartSmoke.ps1"; Name = "new game start" },
  @{ Phase = 8; Script = "Run-Phase8TransportRunSmoke.ps1"; Name = "transport run" },
  @{ Phase = 9; Script = "Run-Phase9SettlementGameOverSmoke.ps1"; Name = "settlement game over" },
  @{ Phase = 10; Script = "Run-Phase10PlanetMaintenanceSmoke.ps1"; Name = "planet maintenance" },
  @{ Phase = 11; Script = "Run-Phase11AsteroidHazardSmoke.ps1"; Name = "asteroid hazard" },
  @{ Phase = 12; Script = "Run-Phase12ManualTurretSmoke.ps1"; Name = "manual turret" },
  @{ Phase = 13; Script = "Run-Phase13IntruderFrameworkSmoke.ps1"; Name = "intruder framework" },
  @{ Phase = 14; Script = "Run-Phase14ParvumIntruderSmoke.ps1"; Name = "Parvum intruder" },
  @{ Phase = 15; Script = "Run-Phase15EquipmentLoopSmoke.ps1"; Name = "equipment loop" },
  @{ Phase = 16; Script = "Run-Phase16HudMapAtmosphereSmoke.ps1"; Name = "HUD map atmosphere" },
  @{ Phase = 17; Script = "Run-Phase17CoopFoundationSmoke.ps1"; Name = "coop foundation" },
  @{ Phase = 18; Script = "Run-Phase18MvpPlaytestLoopSmoke.ps1"; Name = "MVP playtest loop" }
)

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Set-Content -LiteralPath $logPath -Value "Phase 1 to 18 smoke validation started: $(Get-Date -Format o)" -Encoding UTF8

foreach ($check in $checks) {
  $scriptPath = Join-Path $PSScriptRoot $check.Script
  if (-not (Test-Path -LiteralPath $scriptPath)) {
    Add-Content -LiteralPath $logPath -Value "Phase $($check.Phase) FAILED: missing script $($check.Script)" -Encoding UTF8
    Write-Error "Phase $($check.Phase) script is missing: $scriptPath"
    exit 1
  }

  Write-Host "Running Phase $($check.Phase): $($check.Name)"
  Add-Content -LiteralPath $logPath -Value "Phase $($check.Phase) START: $($check.Name)" -Encoding UTF8

  $start = Get-Date
  & powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath
  $exitCode = $LASTEXITCODE
  $elapsed = (Get-Date) - $start

  if ($exitCode -ne 0) {
    Add-Content -LiteralPath $logPath -Value ("Phase {0} FAILED after {1:n1}s with exit code {2}" -f $check.Phase, $elapsed.TotalSeconds, $exitCode) -Encoding UTF8
    Write-Error "Phase $($check.Phase) failed. See $logPath"
    exit $exitCode
  }

  Add-Content -LiteralPath $logPath -Value ("Phase {0} PASSED after {1:n1}s" -f $check.Phase, $elapsed.TotalSeconds) -Encoding UTF8
}

Add-Content -LiteralPath $logPath -Value "Phase 1 to 18 smoke validation passed: $(Get-Date -Format o)" -Encoding UTF8
Write-Host "Phase 1 to 18 smoke validation passed. Log: $logPath"
exit 0
