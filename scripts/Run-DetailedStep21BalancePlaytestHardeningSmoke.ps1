$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\DetailedStep21BalancePlaytestHardeningSmoke.log"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

function Get-OpenUnityEditorProcess {
  $unityProcesses = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue
  foreach ($process in $unityProcesses) {
    $commandLine = [string]$process.CommandLine
    if ($commandLine.IndexOf($projectRoot, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
      continue
    }

    if ($commandLine.IndexOf("-batchmode", [StringComparison]::OrdinalIgnoreCase) -ge 0) {
      continue
    }

    return $process
  }

  return $null
}

function Invoke-Step21Bridge {
  & (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "ValidateDetailedStep21BalancePlaytestHardening" -LogPath $logPath -TimeoutSeconds 240
  return $LASTEXITCODE
}

$bridgeExitCode = Invoke-Step21Bridge

$bridgeLog = ""
if (Test-Path -LiteralPath $logPath) {
  $bridgeLog = Get-Content -LiteralPath $logPath -Raw
}

if ($bridgeLog -match "Unknown bridge command: ValidateDetailedStep21BalancePlaytestHardening" -and (Get-OpenUnityEditorProcess)) {
  & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Run-DetailedStep20PresentationSmoke.ps1")
  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }

  Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
  $bridgeExitCode = Invoke-Step21Bridge
  $bridgeLog = ""
  if (Test-Path -LiteralPath $logPath) {
    $bridgeLog = Get-Content -LiteralPath $logPath -Raw
  }
}

if ($bridgeLog -match "Unknown bridge command: ValidateDetailedStep21BalancePlaytestHardening" -and (Get-OpenUnityEditorProcess)) {
  Write-Error "Open Unity editor still has a stale validation bridge after refresh. Wait for Unity script compilation/reload, then rerun this smoke."
  exit 1
}

if ($bridgeExitCode -eq 2 -or $bridgeLog -match "Unknown bridge command: ValidateDetailedStep21BalancePlaytestHardening") {
  & (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
  $unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
  $arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "Bellerophon.Editor.Validation.DetailedStep21BalancePlaytestHardeningEditorValidation.Run",
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
if ($log -notmatch "Detailed step 21 balance playtest hardening editor validation passed\.") {
  Write-Error "Detailed step 21 balance playtest hardening smoke success marker was not found. See $logPath"
  exit 1
}

if ($log -notmatch "Detailed step 21 balance playtest hardening validation details:") {
  Write-Error "Detailed step 21 balance playtest hardening smoke details were not found. See $logPath"
  exit 1
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Detailed step 21 balance playtest hardening smoke log contains errors. See $logPath"
  exit 1
}

exit 0
