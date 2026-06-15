$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\DisableCargoShipVisualModeling.log"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "DisableCargoShipVisualModeling" -LogPath $logPath -TimeoutSeconds 240
$bridgeExitCode = $LASTEXITCODE

$bridgeLog = ""
if (Test-Path -LiteralPath $logPath) {
  $bridgeLog = Get-Content -LiteralPath $logPath -Raw
}

if ($bridgeExitCode -eq 2 -or $bridgeLog -match "Unknown bridge command: DisableCargoShipVisualModeling") {
  & (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
  $unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
  $arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "Bellerophon.Editor.Validation.CargoShipVisualModelingBootstrap.DisableVisualModeling",
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

$scenePath = Join-Path $projectRoot "Assets\_Project\Scenes\CargoRunMvp.unity"
if (Test-Path -LiteralPath $scenePath) {
  $sceneText = [System.IO.File]::ReadAllText($scenePath)
  $sceneText = [System.Text.RegularExpressions.Regex]::Replace($sceneText, '[ \t]+(?=\r?\n)', '')
  $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
  [System.IO.File]::WriteAllText($scenePath, $sceneText, $utf8NoBom)
}

$log = Get-Content -LiteralPath $logPath -Raw
if ($log -notmatch "Cargo ship visual modeling disabled\.") {
  Write-Error "Cargo ship visual modeling disable success marker was not found. See $logPath"
  exit 1
}

if ($log -notmatch "Cargo ship visual modeling disabled validation passed\.") {
  Write-Error "Cargo ship visual modeling disabled validation marker was not found. See $logPath"
  exit 1
}

if ($log -notmatch "AssetStoreActiveInHierarchy=False") {
  Write-Error "Asset Store ship dressing root was not confirmed inactive. See $logPath"
  exit 1
}

if ($log -notmatch "ActiveVisualRoots=0") {
  Write-Error "One or more cargo ship visual dressing roots are still active. See $logPath"
  exit 1
}

if ($log -notmatch "EnabledGrayboxRenderers=0") {
  Write-Error "Cargo ship graybox renderers are still enabled. See $logPath"
  exit 1
}

if ($log -notmatch "EnabledGrayboxColliders=\d+") {
  Write-Error "Cargo ship gameplay colliders were not reported. See $logPath"
  exit 1
}

if ($log -notmatch "EnabledDebugInteractables=\d+") {
  Write-Error "Cargo ship interaction components were not reported. See $logPath"
  exit 1
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Cargo ship visual modeling disable log contains errors. See $logPath"
  exit 1
}

exit 0
