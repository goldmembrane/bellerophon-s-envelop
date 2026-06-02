$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$requestPath = Join-Path $projectRoot "Logs\Phase4CargoShipGrayboxSmoke.request"
$activePath = Join-Path $projectRoot "Logs\Phase4CargoShipGrayboxSmoke.active"
$logPath = Join-Path $projectRoot "Logs\Phase4CargoShipGrayboxSmoke.log"
$timeoutSeconds = 60

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

$openEditorProcess = Get-OpenUnityEditorProcess
if (-not $openEditorProcess) {
  Write-Error "No open Unity editor process found for $projectRoot"
  exit 2
}

New-Item -ItemType Directory -Force -Path (Split-Path $requestPath), (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $requestPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

if (Test-Path -LiteralPath $activePath) {
  $activeItem = Get-Item -LiteralPath $activePath
  if ($activeItem.LastWriteTime -lt (Get-Date).AddMinutes(-2)) {
    Remove-Item -LiteralPath $activePath -Force -ErrorAction SilentlyContinue
  } else {
    Write-Error "A Phase 4 cargo ship graybox smoke request is already active. See $activePath"
    exit 1
  }
}

$requestId = [guid]::NewGuid().ToString("N")
Set-Content -LiteralPath $logPath -Value "Phase 4 cargo ship graybox smoke pending: $requestId" -Encoding UTF8
@(
  "id=$requestId",
  "logPath=$logPath"
) | Set-Content -LiteralPath $requestPath -Encoding UTF8

$deadline = (Get-Date).AddSeconds($timeoutSeconds)
while ((Get-Date) -lt $deadline) {
  if (Test-Path -LiteralPath $logPath) {
    $log = Get-Content -LiteralPath $logPath -Raw
    if ($log -match "Phase 4 cargo ship graybox smoke completed: $requestId") {
      if ($log -match "Result: Passed") {
        exit 0
      }

      Write-Error "Phase 4 cargo ship graybox smoke failed. See $logPath"
      exit 1
    }
  }

  Start-Sleep -Milliseconds 250
}

Write-Error "Phase 4 cargo ship graybox smoke timed out after $timeoutSeconds seconds. See $logPath"
exit 1
