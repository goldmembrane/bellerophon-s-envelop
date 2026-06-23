# 전체 오브젝트 복구 백업

- 생성 시각: 2026-06-23 23:28
- 목적: 현재 CargoRunMvp 씬의 전체 오브젝트 상태와 비품실 복구용 스크립트 상태를 함께 보존한다.
- 기준 상태: git 기준 조종실/동력실/통제실/무기실 복원 후, 비품실 루트 생성 및 2026-06-23 21:07 계열 CurrentState 스냅샷을 이름 기준으로 적용한 상태.

## 포함 파일

- `CargoRunMvp.unity`
- `ApprovedSupplyRoomShellCurrentState.cs`
- `ApprovedArmoryShellCurrentState.cs`
- `ApprovedArmoryShellBootstrap.cs`
- `UnityEditorValidationBridge.cs`

## 주의

- 이 백업 생성 과정에서는 Unity 실행, Unity 브리지, Ensure, Restore, 테스트, 검증, 빌드를 실행하지 않았다.
- 복구 시에는 사용자 승인 후 대상 파일을 명시적으로 지정해서 복사해야 한다.
