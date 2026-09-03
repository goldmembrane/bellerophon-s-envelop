param(
  [Parameter(Mandatory = $true)]
  [string]$Command,

  [Parameter(Mandatory = $true)]
  [string]$LogPath,

  [string]$ResultsPath = "",
  [string]$OutputPath = "",
  [switch]$DevelopmentBuild,
  [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$requestPath = Join-Path $projectRoot "Logs\UnityEditorBridge.request"
$activeRequestPath = Join-Path $projectRoot "Logs\UnityEditorBridge.active"

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
  exit 2
}

New-Item -ItemType Directory -Force -Path (Split-Path $LogPath), (Split-Path $requestPath) | Out-Null

$requestId = [guid]::NewGuid().ToString("N")
Set-Content -LiteralPath $LogPath -Value "Unity editor bridge request pending: $requestId" -Encoding UTF8

@(
  "id=$requestId",
  "command=$Command",
  "logPath=$LogPath",
  "resultsPath=$ResultsPath",
  "outputPath=$OutputPath",
  "developmentBuild=$($DevelopmentBuild.IsPresent)"
) | Set-Content -LiteralPath $requestPath -Encoding UTF8

function Clear-OwnActiveRequest {
  if (-not (Test-Path -LiteralPath $activeRequestPath)) {
    return
  }

  $activeRequest = Get-Content -LiteralPath $activeRequestPath -Raw
  if ($activeRequest -match "id=$requestId") {
    Remove-Item -LiteralPath $activeRequestPath -Force -ErrorAction SilentlyContinue
  }
}

$log = ""
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
  if (Test-Path -LiteralPath $LogPath) {
    $log = Get-Content -LiteralPath $LogPath -Raw
    if ($log -match "Unity editor bridge request completed: $requestId") {
      exit 0
    }
  }

  Start-Sleep -Milliseconds 50
}

Clear-OwnActiveRequest
Write-Error "Open Unity editor did not complete $Command request $requestId. See $LogPath"
exit 1
