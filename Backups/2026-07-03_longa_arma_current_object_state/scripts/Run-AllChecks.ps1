$ErrorActionPreference = "Stop"

$checks = @(
  "Run-HarnessValidation.ps1",
  "Run-EditModeTests.ps1",
  "Run-PlayModeTests.ps1"
)

foreach ($check in $checks) {
  $scriptPath = Join-Path $PSScriptRoot $check
  & powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath

  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }
}

exit 0
