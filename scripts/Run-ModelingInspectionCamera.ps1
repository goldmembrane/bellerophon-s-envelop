$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$applyLogPath = Join-Path $logDir "ModelingInspectionCamera.log"
$validateLogPath = Join-Path $logDir "ModelingInspectionCameraValidate.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $applyLogPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $validateLogPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "EnableModelingInspectionFreeCamera" -LogPath $applyLogPath -TimeoutSeconds 240
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "ValidateModelingInspectionFreeCamera" -LogPath $validateLogPath -TimeoutSeconds 120
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}

$scenePath = Join-Path $projectRoot "Assets\_Project\Scenes\CargoRunMvp.unity"
if (Test-Path -LiteralPath $scenePath) {
  $sceneText = [System.IO.File]::ReadAllText($scenePath)
  $sceneText = [System.Text.RegularExpressions.Regex]::Replace($sceneText, '[ \t]+(?=\r?\n)', '')
  $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
  [System.IO.File]::WriteAllText($scenePath, $sceneText, $utf8NoBom)
}

$applyLog = Get-Content -LiteralPath $applyLogPath -Raw
$validateLog = Get-Content -LiteralPath $validateLogPath -Raw

if ($applyLog -notmatch "Modeling inspection free camera enabled\.") {
  Write-Error "Modeling inspection free camera apply marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Modeling inspection mode validation passed\.") {
  Write-Error "Modeling inspection mode validation marker was not found. See $applyLogPath"
  exit 1
}

if ($applyLog -notmatch "Modeling inspection free camera validation passed\.") {
  Write-Error "Modeling inspection free camera validation marker was not found in apply log. See $applyLogPath"
  exit 1
}

if ($validateLog -notmatch "Modeling inspection free camera validation passed\.") {
  Write-Error "Modeling inspection free camera validation marker was not found. See $validateLogPath"
  exit 1
}

if ($validateLog -notmatch "OtherEnabledCameras=0") {
  Write-Error "Other enabled cameras were not confirmed disabled. See $validateLogPath"
  exit 1
}

if ($validateLog -notmatch "ActivePlayerViewComponents=0") {
  Write-Error "Player view/input components were not confirmed disabled. See $validateLogPath"
  exit 1
}

if ($applyLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed" -or
    $validateLog -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Modeling inspection camera logs contain errors. See $applyLogPath and $validateLogPath"
  exit 1
}

exit 0
