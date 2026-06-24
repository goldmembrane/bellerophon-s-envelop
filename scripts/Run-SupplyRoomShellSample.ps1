$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$logPath = Join-Path $logDir "SupplyRoomShellSample.log"
$stdoutPath = Join-Path $logDir "SupplyRoomShellSample.stdout.log"
$stderrPath = Join-Path $logDir "SupplyRoomShellSample.stderr.log"
$scriptPath = Join-Path $projectRoot "scripts\GenerateSupplyRoomShellSample.py"
$sampleRoot = Join-Path $projectRoot "artSample\supply_room_shell"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $stdoutPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $stderrPath -Force -ErrorAction SilentlyContinue

$blenderCandidates = @(
  "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 5.0\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.3\blender.exe"
)

$blenderExe = $null
foreach ($candidate in $blenderCandidates) {
  if (Test-Path -LiteralPath $candidate) {
    $blenderExe = $candidate
    break
  }
}

if ($null -eq $blenderExe) {
  $command = Get-Command blender -ErrorAction SilentlyContinue
  if ($null -ne $command) {
    $blenderExe = $command.Source
  }
}

if ($null -eq $blenderExe) {
  Write-Error "Blender executable was not found."
  exit 1
}

$process = Start-Process -FilePath $blenderExe `
  -ArgumentList @("--background", "--python", $scriptPath) `
  -RedirectStandardOutput $stdoutPath `
  -RedirectStandardError $stderrPath `
  -WindowStyle Hidden `
  -Wait `
  -PassThru

$stdout = if (Test-Path -LiteralPath $stdoutPath) { Get-Content -LiteralPath $stdoutPath -Raw } else { "" }
$stderr = if (Test-Path -LiteralPath $stderrPath) { Get-Content -LiteralPath $stderrPath -Raw } else { "" }
[System.IO.File]::WriteAllText($logPath, $stdout + "`r`n" + $stderr, [System.Text.UTF8Encoding]::new($false))

if ($process.ExitCode -ne 0) {
  Get-Content -LiteralPath $logPath -Tail 120
  exit $process.ExitCode
}

$requiredFiles = @(
  "index.html",
  "README.md",
  "ASSET_MANIFEST.json",
  "APPROVAL_STATUS.json",
  "blender\supply_room_shell.blend",
  "exports\supply_room_shell.fbx",
  "exports\supply_room_shell.glb",
  "renders\01_overview.png",
  "renders\02_floor_plan.png",
  "renders\03_supply_storage_wall.png",
  "renders\04_ejection_wall.png",
  "renders\05_corridor_entries.png",
  "renders\06_room_shell_markers.png",
  "renders\07_ejection_hazard_floor.png",
  "renders\08_corridor_direction_labels.png",
  "renders\09_cctv_corner.png",
  "renders\10_ejection_terminal_hsk_screen.png"
)

foreach ($relative in $requiredFiles) {
  $path = Join-Path $sampleRoot $relative
  if (-not (Test-Path -LiteralPath $path)) {
    Write-Error "Missing generated supply room shell sample file: $path"
    exit 1
  }
}

exit 0
