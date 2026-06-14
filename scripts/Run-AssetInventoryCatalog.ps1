$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $projectRoot "Logs\AssetInventoryCatalog.log"

New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot "Invoke-UnityEditorBridge.ps1") -Command "CaptureAssetInventoryCatalog" -LogPath $logPath -TimeoutSeconds 900
$bridgeExitCode = $LASTEXITCODE

$bridgeLog = ""
if (Test-Path -LiteralPath $logPath) {
  $bridgeLog = Get-Content -LiteralPath $logPath -Raw
}

if ($bridgeExitCode -eq 2 -or $bridgeLog -match "Unknown bridge command: CaptureAssetInventoryCatalog") {
  & (Join-Path $PSScriptRoot "Wait-UnityProjectReady.ps1")
  $unity = & (Join-Path $PSScriptRoot "unity-path.ps1")
  $arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "Bellerophon.Editor.Validation.AssetInventoryCatalogRenderer.Capture",
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

$log = Get-Content -LiteralPath $logPath -Raw
if ($log -notmatch "Asset inventory catalog saved:") {
  Write-Error "Asset inventory catalog success marker was not found. See $logPath"
  exit 1
}

$catalogRoot = Join-Path $projectRoot "artSample\asset_inventory_catalog_2026-06-14"
$requiredFiles = @(
  "index.html",
  "README.md",
  "APPROVAL_STATUS.json",
  "asset_catalog.csv",
  "asset_catalog.json"
)

foreach ($fileName in $requiredFiles) {
  $filePath = Join-Path $catalogRoot $fileName
  if (-not (Test-Path -LiteralPath $filePath)) {
    Write-Error "Missing asset inventory catalog file: $filePath"
    exit 1
  }
}

$thumbnailCount = 0
$thumbnailRoot = Join-Path $catalogRoot "thumbnails"
if (Test-Path -LiteralPath $thumbnailRoot) {
  $thumbnailCount = (Get-ChildItem -LiteralPath $thumbnailRoot -Recurse -File -Filter *.png | Measure-Object).Count
}

if ($thumbnailCount -lt 100) {
  Write-Error "Asset inventory catalog thumbnail count is unexpectedly low: $thumbnailCount"
  exit 1
}

if ($log -match "Scripts have compiler errors|error CS\d+|Unity editor bridge failed") {
  Write-Error "Asset inventory catalog log contains errors. See $logPath"
  exit 1
}

Write-Host "Asset inventory catalog generated: $catalogRoot"
Write-Host "Thumbnail count: $thumbnailCount"
exit 0
