# 전체 구역 현재 상태 백업

- 생성일: 2026-06-24
- 목적: 현재 씬의 주요 구역 오브젝트 상태를 날짜가 포함된 복구 백업으로 보존한다.
- 기준 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`

## 실행한 캡처

- `CaptureApprovedArmoryShellCurrentState`
- `CaptureApprovedSupplyRoomShellCurrentState`
- `CaptureApprovedControlRoomCurrentState`
- `CaptureApprovedControlRoomShellCurrentObjects`
- `CaptureApprovedEngineRoomShellCurrentObjects`
- `CaptureApprovedEngineRoomHealthScreenCurrentObjects`

## 포함 범위

- `Assets/_Project/Scenes/CargoRunMvp.unity`
- `Assets/_Project/Editor/Validation/*CurrentState*.cs`
- `Assets/_Project/Editor/Validation/*CurrentObjects*.cs`
- `Assets/_Project/Editor/Validation/*CurrentSnapshot*.cs`
- 관련 캡처 로그
- `artSample/**/editor_current/` 현재 오브젝트 기록

## 참고

- 첫 무기실 캡처 요청은 비품실 캡처와 동시에 실행되어 시간 제한에 걸렸고, 이후 같은 명령을 순차 실행해 성공했다.
- 이 백업 생성 중에는 `Ensure`, `Restore`, 오브젝트 재생성, Validate/Smoke/Test/Build를 실행하지 않았다.
