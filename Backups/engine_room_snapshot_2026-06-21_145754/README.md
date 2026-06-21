# 동력실 복구 스냅샷

생성일: 2026-06-21 14:57:54

이 스냅샷은 통제실 작업으로 넘어가기 전, 현재 디스크에 저장된 동력실 관련 상태를 복구할 수 있도록 보존한 파일 사본입니다.

## 포함 범위

- `Assets/_Project/Scenes/CargoRunMvp.unity`
- `Assets/_Project/Editor/Validation/ApprovedEngineRoomHealthScreenBootstrap.cs`
- `Assets/_Project/Editor/Validation/UnityEditorValidationBridge.cs`
- `Assets/_Project/Art/Ship/EngineRoom/`
- `docs/ENGINE_ROOM_OBJECTS.md`
- `docs/PROGRESS_2026-06-21.md`
- `artSample/engine_room_*`
- `scripts/*EngineRoom*`

## 복구 기준

- 이 스냅샷은 Unity 에디터의 미저장 변경이 아니라, 생성 시점에 디스크에 저장되어 있던 파일 상태를 기준으로 한다.
- 동력실 오브젝트의 위치, 크기, 회전, 머티리얼, 계층, 런타임 연결은 스냅샷 생성 과정에서 수정하지 않았다.
- Unity 브리지, Refresh, Ensure, Validate, Smoke, Test, Build는 실행하지 않았다.
- git commit, branch, reset, checkout, tag는 실행하지 않았다.

## 복구 대상

복구가 필요하면 이 폴더 안의 파일을 동일한 저장소 상대 경로로 되돌리는 것을 기준으로 한다. 복구 전에는 당시 작업 범위를 다시 승인받아야 한다.

## 참고

- 생성 시점 `git status --short`에서 동력실 관련 씬과 머티리얼 파일들이 수정 상태로 확인되어 `Assets/_Project/Art/Ship/EngineRoom/` 전체를 포함했다.
- 이 스냅샷은 통제실 작업 전 동력실 상태를 보존하기 위한 목적이며, 통제실 오브젝트 문서 자체의 복구 지점은 아니다.
