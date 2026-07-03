$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
$resultsPath = Join-Path $projectRoot "TestResults\editmode-results.xml"
$logPath = Join-Path $projectRoot "Logs\EditModeTests.log"

New-Item -ItemType Directory -Force -Path (Split-Path $resultsPath), (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $resultsPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "EditModeTests" -LogPath $logPath -ResultsPath $resultsPath -TimeoutSeconds 300
$bridgeExitCode = $LASTEXITCODE

if ($bridgeExitCode -eq 0) {
  $log = Get-Content -LiteralPath $logPath -Raw
  if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
    Write-Error "EditMode test log contains errors. See $logPath"
    exit 1
  }

  if (-not (Test-Path -LiteralPath $resultsPath)) {
    Write-Error "EditMode test result file was not created. See $logPath"
    exit 1
  }

  $results = [xml](Get-Content -LiteralPath $resultsPath -Raw)
  $testRun = $results.'test-run'
  if ($testRun.result -ne "Passed" -or [int]$testRun.failed -ne 0) {
    Write-Error "EditMode tests failed. See $resultsPath"
    exit 1
  }

  exit 0
}

if ($bridgeExitCode -ne 2) {
  exit $bridgeExitCode
}

& (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")

$arguments = @(
  "-batchmode",
  "-nographics",
  "-projectPath", $projectRoot,
  "-runTests",
  "-testPlatform", "EditMode",
  "-testResults", $resultsPath,
  "-logFile", $logPath
)

$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
$unityExitCode = $process.ExitCode
if ($unityExitCode -ne 0) {
  exit $unityExitCode
}

for ($attempt = 0; $attempt -lt 40; $attempt++) {
  if (Test-Path -LiteralPath $resultsPath) {
    break
  }

  Start-Sleep -Milliseconds 250
}

if (-not (Test-Path -LiteralPath $resultsPath)) {
  Write-Error "EditMode test result file was not created. See $logPath"
  exit 1
}

$log = Get-Content -LiteralPath $logPath -Raw
if ($log -match "Scripts have compiler errors|error CS\d+") {
  Write-Error "EditMode test log contains compiler errors. See $logPath"
  exit 1
}

$results = [xml](Get-Content -LiteralPath $resultsPath -Raw)
$testRun = $results.'test-run'
if ($testRun.result -ne "Passed" -or [int]$testRun.failed -ne 0) {
  Write-Error "EditMode tests failed. See $resultsPath"
  exit 1
}

exit 0
