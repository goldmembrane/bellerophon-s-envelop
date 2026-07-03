$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectVersionPath = Join-Path $projectRoot "ProjectSettings\ProjectVersion.txt"

if ($env:UNITY_EXE -and (Test-Path -LiteralPath $env:UNITY_EXE)) {
  Write-Output $env:UNITY_EXE
  exit 0
}

$versionLine = Get-Content -LiteralPath $projectVersionPath | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
if (-not $versionLine) {
  throw "ProjectSettings\ProjectVersion.txt에서 Unity 버전을 찾지 못했습니다."
}

$version = ($versionLine -replace "^m_EditorVersion:\s*", "").Trim()
$candidate = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"

if (Test-Path -LiteralPath $candidate) {
  Write-Output $candidate
  exit 0
}

throw "Unity $version 실행 파일을 찾지 못했습니다. UNITY_EXE 환경 변수로 Unity.exe 경로를 지정하세요."

