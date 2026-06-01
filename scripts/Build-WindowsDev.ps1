$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
$outputPath = Join-Path $projectRoot "Builds\WindowsDev\Bellerophon.exe"
$logPath = Join-Path $projectRoot "Logs\Build-WindowsDev.log"

& (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
New-Item -ItemType Directory -Force -Path (Split-Path $outputPath), (Split-Path $logPath) | Out-Null

$arguments = @(
  "-batchmode",
  "-nographics",
  "-quit",
  "-projectPath", $projectRoot,
  "-executeMethod", "Bellerophon.Editor.Build.BuildCli.BuildWindows64",
  "-developmentBuild",
  "-buildOutputPath", $outputPath,
  "-logFile", $logPath
)

$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
$unityExitCode = $process.ExitCode
if ($unityExitCode -ne 0) {
  exit $unityExitCode
}

$log = ""
for ($attempt = 0; $attempt -lt 40; $attempt++) {
  if (Test-Path -LiteralPath $logPath) {
    $log = Get-Content -LiteralPath $logPath -Raw
    if ((Test-Path -LiteralPath $outputPath) -or $log -match "Scripts have compiler errors|error CS\d+|Build failed with result|Aborting batchmode due to fatal error") {
      break
    }
  }

  Start-Sleep -Milliseconds 250
}

if ($log -match "Scripts have compiler errors|error CS\d+|Build failed with result|Aborting batchmode due to fatal error") {
  Write-Error "Windows dev build log contains errors. See $logPath"
  exit 1
}

if ($log -notmatch "Build Finished, Result: Success") {
  Write-Error "Windows dev build success marker was not found. See $logPath"
  exit 1
}

if (-not (Test-Path -LiteralPath $outputPath)) {
  Write-Error "Windows dev build output was not created at $outputPath. See $logPath"
  exit 1
}

exit 0
