# 비품실 현재 상태 백업

- 생성일: 2026-06-24
- 대상 루트: `Approved Supply Room 01 Shell`
- 목적: 현재 Unity 씬의 비품실 전체 상태를 복구 기준으로 보존한다.

## 포함 파일

- `CargoRunMvp.unity`
- `ApprovedSupplyRoomShellCurrentState.cs`
- `ApprovedSupplyRoomShellCurrentState.cs.meta`
- `ApprovedSupplyRoomShellCurrentStateCapture.log`

## 생성 절차

1. `Refresh-UnityProject.ps1`로 열린 Unity 프로젝트를 갱신했다.
2. `CaptureApprovedSupplyRoomShellCurrentState`로 현재 비품실 Transform/activeSelf 상태를 캡처했다.
3. 캡처 직후의 씬과 비품실 상태 스크립트를 이 폴더에 복사했다.

## 실행하지 않은 작업

- `Ensure` 실행
- `Restore` 실행
- 비품실 전체 재생성
- 비품실 오브젝트 수정
- 다른 구역 수정
- `artSample/` 수정
- 코드 로직 수정
- Validate/Smoke/Test/Build
