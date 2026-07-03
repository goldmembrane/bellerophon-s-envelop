param(
  [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$lockPath = Join-Path $projectRoot "Temp\UnityLockfile"
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)

while ((Get-Date) -lt $deadline) {
  $unityProcesses = Get-Process Unity -ErrorAction SilentlyContinue
  if (-not $unityProcesses -and -not (Test-Path -LiteralPath $lockPath)) {
    return
  }

  Start-Sleep -Milliseconds 500
}

throw "Unity project is still locked after $TimeoutSeconds seconds. Close Unity or wait for the previous batch command to finish."
