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
$pegasusScenePath = Join-Path $projectRoot "Assets\_Project\Scenes\Pegasus.unity"
$lockPath = Join-Path $projectRoot "Temp\UnityLockfile"
$sceneOpenLogPath = Join-Path $projectRoot "Logs\OpenPegasusScene.log"
$openedNewEditor = $false
$expectedCargoRunSceneLength = 17784840
$expectedCargoRunSceneHash = "8BA694A01DA24953AD6EA73814D0D9FFCA9D07738F498AA3C967309D1E1E3D52"
$requiredCargoRunSceneMarkers = @(
  "value: Lightsaber_Off_Idle",
  "value: Lightsaber_DiagonalSlash",
  "value: Lightsaber_Grip_OneHand",
  "value: Lightsaber_ThrustMode_Enter",
  "value: Lightsaber_ThrustMode_Exit",
  "value: Detector_Attached_Static",
  "value: PresenceDetector_Attached",
  "value: ElectricMine_Idle",
  "value: ElectricMine_Activate",
  "value: ElectricMine_Armed_Idle",
  "value: Stun_Twist",
  "value: Exhausted_Idle",
  "value: Exhausted_Walk_Forward",
  "value: Fatigue_HeadShake",
  "value: Confused_Walk_Forward",
  "value: Knockback_Reaction",
  "value: PostureBreak"
)
$requiredPegasusSceneMarkers = @(
  "m_Name: Approved Engine Room 01 Shell",
  "m_Name: Approved Cockpit 01 Structure",
  "m_Name: Approved Cargo Hold 01 Shell",
  "m_Name: Approved Ship Corridor Segments"
)

function Assert-CurrentPegasusScene {
  $resolvedScenePath = (Resolve-Path -LiteralPath $pegasusScenePath).Path
  $expectedScenePath = [IO.Path]::GetFullPath((Join-Path $projectRoot "Assets\_Project\Scenes\Pegasus.unity"))
  if (-not $resolvedScenePath.Equals($expectedScenePath, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Pegasus resolved outside the current scene path. Expected=$expectedScenePath; Actual=$resolvedScenePath"
  }

  $sceneText = [IO.File]::ReadAllText($resolvedScenePath, [Text.Encoding]::UTF8)
  foreach ($marker in $requiredPegasusSceneMarkers) {
    if ($sceneText.IndexOf($marker, [StringComparison]::Ordinal) -lt 0) {
      throw "Pegasus is missing a current-layout marker: $marker"
    }
  }
}

function Assert-CurrentCargoRunScene {
  $resolvedScenePath = (Resolve-Path -LiteralPath $cargoRunScenePath).Path
  $expectedScenePath = [IO.Path]::GetFullPath((Join-Path $projectRoot "Assets\_Project\Scenes\CargoRunMvp.unity"))
  if (-not $resolvedScenePath.Equals($expectedScenePath, [StringComparison]::OrdinalIgnoreCase)) {
    throw "CargoRunMvp resolved outside the protected current scene path. Expected=$expectedScenePath; Actual=$resolvedScenePath"
  }

  $sceneInfo = Get-Item -LiteralPath $resolvedScenePath
  if ($sceneInfo.Length -ne $expectedCargoRunSceneLength) {
    throw "CargoRunMvp is not the protected current scene. Length mismatch. Expected=$expectedCargoRunSceneLength; Actual=$($sceneInfo.Length)"
  }

  $actualHash = (Get-FileHash -LiteralPath $resolvedScenePath -Algorithm SHA256).Hash
  if (-not $actualHash.Equals($expectedCargoRunSceneHash, [StringComparison]::OrdinalIgnoreCase)) {
    throw "CargoRunMvp is not the protected current scene. SHA-256 mismatch. Expected=$expectedCargoRunSceneHash; Actual=$actualHash"
  }

  $sceneText = [IO.File]::ReadAllText($resolvedScenePath, [Text.Encoding]::UTF8)
  foreach ($marker in $requiredCargoRunSceneMarkers) {
    if ($sceneText.IndexOf($marker, [StringComparison]::Ordinal) -lt 0) {
      throw "CargoRunMvp is missing a protected current-work marker: $marker"
    }
  }
}

if (-not (Test-Path -LiteralPath $projectVersionPath)) {
  throw "ProjectSettings\ProjectVersion.txt was not found. Refusing to open Unity outside the project root: $projectRoot"
}

if (-not (Test-Path -LiteralPath $pegasusScenePath)) {
  throw "Pegasus scene was not found. Refusing to open an incomplete Unity project: $pegasusScenePath"
}

Assert-CurrentPegasusScene
if ($ValidateCargoRunScene) {
  if (-not (Test-Path -LiteralPath $cargoRunScenePath)) {
    throw "CargoRunMvp scene was not found. Refusing requested CargoRunMvp validation: $cargoRunScenePath"
  }

  Assert-CurrentCargoRunScene
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
& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "OpenPegasusScene" -LogPath $sceneOpenLogPath -TimeoutSeconds $TimeoutSeconds
$sceneOpenExitCode = $LASTEXITCODE

$sceneOpenLog = ""
if (Test-Path -LiteralPath $sceneOpenLogPath) {
  $sceneOpenLog = Get-Content -LiteralPath $sceneOpenLogPath -Raw
}

if ($sceneOpenExitCode -ne 0 -or
    $sceneOpenLog -notmatch "Pegasus scene opened\." -or
    $sceneOpenLog -match "Unity editor bridge failed|Unknown bridge command: OpenPegasusScene|Scripts have compiler errors|error CS\d+") {
  throw "Unity editor opened the project, but failed to open Pegasus scene. See $sceneOpenLogPath"
}

Assert-CurrentPegasusScene
if ($ValidateCargoRunScene) {
  Assert-CurrentCargoRunScene
}
