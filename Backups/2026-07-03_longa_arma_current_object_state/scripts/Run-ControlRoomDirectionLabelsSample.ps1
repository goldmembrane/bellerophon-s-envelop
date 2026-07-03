$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$scriptPath = Join-Path $projectRoot "scripts\GenerateControlRoomDirectionLabelsSample.py"

if (-not (Test-Path $scriptPath)) {
    throw "Generator script not found: $scriptPath"
}

$blenderCandidates = @()
if ($env:BLENDER_EXE) {
    $blenderCandidates += $env:BLENDER_EXE
}

$blenderCommand = Get-Command blender -ErrorAction SilentlyContinue
if ($blenderCommand) {
    $blenderCandidates += $blenderCommand.Source
}

$blenderCandidates += @(
    "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe",
    "C:\Program Files\Blender Foundation\Blender 5.0\blender.exe",
    "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe",
    "C:\Program Files\Blender Foundation\Blender 4.4\blender.exe",
    "C:\Program Files\Blender Foundation\Blender 4.3\blender.exe",
    "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe",
    "C:\Program Files\Blender Foundation\Blender\blender.exe"
)

$blenderPath = $blenderCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $blenderPath) {
    throw "Blender executable was not found. Set BLENDER_EXE or add blender to PATH."
}

Push-Location $projectRoot
try {
    & $blenderPath --background --python $scriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "Blender sample generation failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

$sampleRoot = Join-Path $projectRoot "artSample\control_room_direction_labels"
$requiredOutputs = @(
    "index.html",
    "README.md",
    "ASSET_MANIFEST.json",
    "APPROVAL_STATUS.json",
    "renders\01_context_overview.png",
    "renders\02_left_corridor_labels.png",
    "renders\03_south_adjacent_labels.png",
    "renders\04_panel_closeup.png",
    "renders\05_topdown_layout.png",
    "exports\control_room_direction_labels.fbx",
    "exports\control_room_direction_labels.glb",
    "blender\control_room_direction_labels.blend"
)

foreach ($relativePath in $requiredOutputs) {
    $path = Join-Path $sampleRoot $relativePath
    if (-not (Test-Path $path)) {
        throw "Expected sample output was not generated: $path"
    }
}

Write-Host "CR-17 control room direction labels sample generated: $sampleRoot"
