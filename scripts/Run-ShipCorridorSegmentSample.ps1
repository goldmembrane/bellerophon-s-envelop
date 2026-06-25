$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$logPath = Join-Path $logDir "ShipCorridorSegmentSample.log"
$stdoutPath = Join-Path $logDir "ShipCorridorSegmentSample.stdout.log"
$stderrPath = Join-Path $logDir "ShipCorridorSegmentSample.stderr.log"
$scriptPath = Join-Path $projectRoot "scripts\GenerateShipCorridorSegmentSample.py"
$sampleRoot = Join-Path $projectRoot "artSample\ship_corridor_segment"

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
  "blender\ship_corridor_segment.blend",
  "exports\ship_corridor_segment.fbx",
  "exports\ship_corridor_segment.glb",
  "renders\01_horizontal_overview.png",
  "renders\02_horizontal_top.png",
  "renders\03_horizontal_shutter_front.png",
  "renders\04_horizontal_side.png",
  "renders\05_sloped_overview.png",
  "renders\06_sloped_top.png",
  "renders\07_sloped_shutter_front.png",
  "renders\08_sloped_side.png"
)

foreach ($relative in $requiredFiles) {
  $path = Join-Path $sampleRoot $relative
  if (-not (Test-Path -LiteralPath $path)) {
    Write-Error "Missing generated ship corridor segment sample file: $path"
    exit 1
  }
}

exit 0
