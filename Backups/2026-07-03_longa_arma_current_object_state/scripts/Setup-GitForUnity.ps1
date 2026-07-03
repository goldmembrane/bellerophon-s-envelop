$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$originUrl = "https://github.com/goldmembrane/bellerophon.git"

Push-Location $projectRoot
try {
  if (-not (Test-Path -LiteralPath ".git")) {
    git init
  }

  $currentOrigin = git remote get-url origin 2>$null
  if ($LASTEXITCODE -ne 0) {
    git remote add origin $originUrl
  } elseif ($currentOrigin -ne $originUrl) {
    git remote set-url origin $originUrl
  }

  git lfs install --local
  git config --local core.longpaths true
  git config --local merge.unityyamlmerge.name "Unity Smart Merge"

  $unityExe = & (Join-Path $PSScriptRoot "unity-path.ps1")
  $unityEditorDir = Split-Path $unityExe
  $yamlMerge = Join-Path $unityEditorDir "Data\Tools\UnityYAMLMerge.exe"

  if (-not (Test-Path -LiteralPath $yamlMerge)) {
    throw "UnityYAMLMerge.exe를 찾지 못했습니다: $yamlMerge"
  }

  $fileSystem = New-Object -ComObject Scripting.FileSystemObject
  $shortYamlMerge = $fileSystem.GetFile($yamlMerge).ShortPath
  $yamlMergeForGit = $shortYamlMerge.Replace("\", "/")
  $driver = "$yamlMergeForGit merge -p %O %B %A %A"
  git config --local --replace-all merge.unityyamlmerge.driver $driver

  Write-Output "Git remote, LFS, long path support, and Unity Smart Merge are configured."
}
finally {
  Pop-Location
}
