param(
  [switch]$Restart,
  [switch]$ValidateCargoRunScene,
  [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectRootForward = $projectRoot.Replace("\", "/")
$projectVersionPath = Join-Path $projectRoot "ProjectSettings\ProjectVersion.txt"
$cargoRunScenePath = Join-Path $projectRoot "Assets\_Project\Scenes\CargoRunMvp.unity"
$lockPath = Join-Path $projectRoot "Temp\UnityLockfile"
$sceneOpenLogPath = Join-Path $projectRoot "Logs\OpenCargoRunMvpScene.log"
$openedNewEditor = $false

if (-not (Test-Path -LiteralPath $projectVersionPath)) {
  throw "ProjectSettings\ProjectVersion.txt was not found. Refusing to open Unity outside the project root: $projectRoot"
}

if (-not (Test-Path -LiteralPath $cargoRunScenePath)) {
  throw "CargoRunMvp scene was not found. Refusing to open an incomplete Unity project: $cargoRunScenePath"
}

$unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
$unityEditorDir = Split-Path -Parent $unity

function Get-ProjectUnityProcesses {
  $unityProcesses = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue
  foreach ($process in $unityProcesses) {
    $commandLine = [string]$process.CommandLine
    if ($commandLine.IndexOf($projectRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $commandLine.IndexOf($projectRootForward, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
      $process
    }
  }
}

function Get-OpenProjectEditorProcess {
  foreach ($process in Get-ProjectUnityProcesses) {
    $commandLine = [string]$process.CommandLine
    if ($commandLine.IndexOf("-batchmode", [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $commandLine.IndexOf("AssetImportWorker", [StringComparison]::OrdinalIgnoreCase) -ge 0) {
      continue
    }

    $process
    return
  }
}

if ($Restart) {
  foreach ($process in Get-ProjectUnityProcesses) {
    Stop-Process -Id $process.ProcessId -Force
  }

  Start-Sleep -Seconds 3
}

$openEditor = Get-OpenProjectEditorProcess
if ($openEditor) {
  Write-Output "Unity editor is already open for project: $projectRoot"
  Write-Output "ProcessId=$($openEditor.ProcessId)"
} else {
  $projectProcesses = @(Get-ProjectUnityProcesses)
  if ($projectProcesses.Count -eq 0 -and (Test-Path -LiteralPath $lockPath)) {
    $resolvedLockPath = (Resolve-Path -LiteralPath $lockPath).Path
    if ($resolvedLockPath.StartsWith($projectRoot, [StringComparison]::OrdinalIgnoreCase)) {
      Remove-Item -LiteralPath $resolvedLockPath -Force
    }
  }

  $arguments = @("-projectPath", $projectRoot)
  Start-Process -FilePath $unity -WorkingDirectory $unityEditorDir -ArgumentList $arguments | Out-Null
  $openedNewEditor = $true

  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    Start-Sleep -Milliseconds 500
    $openEditor = Get-OpenProjectEditorProcess
  } while (-not $openEditor -and (Get-Date) -lt $deadline)

  if (-not $openEditor) {
    throw "Unity editor did not start for project within $TimeoutSeconds seconds: $projectRoot"
  }

  Write-Output "Unity editor opened for project: $projectRoot"
  Write-Output "ProcessId=$($openEditor.ProcessId)"
}

$commandLine = [string]$openEditor.CommandLine
if ($commandLine.IndexOf("-projectPath", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
  throw "Unity editor is not using -projectPath. CommandLine=$commandLine"
}

if ($commandLine.IndexOf($projectRoot, [StringComparison]::OrdinalIgnoreCase) -lt 0 -and
    $commandLine.IndexOf($projectRootForward, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
  throw "Unity editor command line does not match project root. CommandLine=$commandLine"
}

if ($openedNewEditor -or $Restart) {
  Start-Sleep -Seconds 20
}

Remove-Item -LiteralPath $sceneOpenLogPath -Force -ErrorAction SilentlyContinue
& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "OpenCargoRunMvpScene" -LogPath $sceneOpenLogPath -TimeoutSeconds $TimeoutSeconds
$sceneOpenExitCode = $LASTEXITCODE

$sceneOpenLog = ""
if (Test-Path -LiteralPath $sceneOpenLogPath) {
  $sceneOpenLog = Get-Content -LiteralPath $sceneOpenLogPath -Raw
}

if ($sceneOpenExitCode -ne 0 -or
    $sceneOpenLog -notmatch "CargoRunMvp scene opened\." -or
    $sceneOpenLog -match "Unity editor bridge failed|Unknown bridge command: OpenCargoRunMvpScene|Scripts have compiler errors|error CS\d+") {
  throw "Unity editor opened the project, but failed to open CargoRunMvp scene. See $sceneOpenLogPath"
}

if ($ValidateCargoRunScene) {
  & (Join-Path $PSScriptRoot "Run-PostDetailedStage2ShipInteriorSmoke.ps1")
}
