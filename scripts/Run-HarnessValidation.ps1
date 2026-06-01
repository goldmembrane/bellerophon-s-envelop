$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
$logPath = Join-Path $projectRoot "Logs\HarnessValidation.log"

& (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null

$arguments = @(
  "-batchmode",
  "-nographics",
  "-quit",
  "-projectPath", $projectRoot,
  "-executeMethod", "Bellerophon.Editor.Validation.HarnessValidation.Run",
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
    if ($log -match "Harness validation passed\.|Scripts have compiler errors|error CS\d+|Harness validation failed|Aborting batchmode due to fatal error") {
      break
    }
  }

  Start-Sleep -Milliseconds 250
}

if ($log -notmatch "Harness validation passed\.") {
  Write-Error "Harness validation success marker was not found. See $logPath"
  exit 1
}

if ($log -match "Scripts have compiler errors|error CS\d+|Harness validation failed") {
  Write-Error "Harness validation log contains errors. See $logPath"
  exit 1
}

exit 0
