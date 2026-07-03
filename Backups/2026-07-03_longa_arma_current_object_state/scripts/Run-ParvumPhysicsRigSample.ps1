$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $projectRoot "Logs"
$logPath = Join-Path $logDir "ParvumPhysicsRigSample.log"
$stdoutPath = Join-Path $logDir "ParvumPhysicsRigSample.stdout.log"
$stderrPath = Join-Path $logDir "ParvumPhysicsRigSample.stderr.log"
$scriptPath = Join-Path $projectRoot "scripts\GenerateParvumPhysicsRigSample.py"
$sampleRoot = Join-Path $projectRoot "artSample\enemies\parvum_physics_rig_sample"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $stdoutPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $stderrPath -Force -ErrorAction SilentlyContinue

$blenderCandidates = @()
if ($env:BLENDER_EXE) {
  $blenderCandidates += $env:BLENDER_EXE
}

$blenderCommand = Get-Command blender -ErrorAction SilentlyContinue
if ($null -ne $blenderCommand) {
  $blenderCandidates += $blenderCommand.Source
}

$blenderCandidates += @(
  "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 5.0\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.4\blender.exe",
  "C:\Program Files\Blender Foundation\Blender 4.3\blender.exe",
  "C:\Program Files\Blender Foundation\Blender\blender.exe"
)

$blenderExe = $blenderCandidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
if ($null -eq $blenderExe) {
  Write-Error "Blender executable was not found. Set BLENDER_EXE or add blender to PATH."
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
  Get-Content -LiteralPath $logPath -Tail 140
  exit $process.ExitCode
}

$requiredFiles = @(
  "index.html",
  "README.md",
  "ASSET_MANIFEST.json",
  "APPROVAL_STATUS.json",
  "TEXTURE_ANALYSIS.md",
  "PHYSICS_RIG_NOTES.md",
  "blender\parvum_physics_rig_sample.blend",
  "exports\parvum_physics_rig_sample.fbx",
  "exports\parvum_physics_rig_sample.glb",
  "textures\parvum_slime_albedo.png",
  "textures\parvum_slime_roughness.png",
  "textures\parvum_white_fleck_mask.png",
  "textures\parvum_snout_scale_albedo.png",
  "textures\parvum_snout_scale_bump.png",
  "textures\parvum_tooth_albedo.png",
  "textures\parvum_tongue_albedo.png",
  "renders\01_front_reference_match.png",
  "renders\02_side_reference_match.png",
  "renders\03_back_reference_match.png",
  "renders\04_top_anchor_map.png",
  "renders\05_physics_proxy_overview.png",
  "renders\06_idle_pulse_pose.png",
  "renders\07_move_squash_pose.png",
  "renders\08_attack_bite_pose.png",
  "renders\09_hit_recoil_pose.png",
  "renders\10_death_flatten_pose.png"
)

foreach ($relative in $requiredFiles) {
  $path = Join-Path $sampleRoot $relative
  if (-not (Test-Path -LiteralPath $path)) {
    Write-Error "Missing generated parvum physics rig sample file: $path"
    exit 1
  }
}

exit 0
