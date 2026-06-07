# PlayMode Test Runner Stability

## Step 10 Open Editor Recheck (2026-06-07)

- Unity editor was open for `D:\Bellerophon2\Bellerophon` with Unity `6000.3.16f1`.
- Step 10 feature smokes passed:
  - `.\scripts\Run-Phase6RoomInteractionsSmoke.ps1`
  - `.\scripts\Run-Phase11AsteroidHazardSmoke.ps1`
  - `.\scripts\Run-Phase12ManualTurretSmoke.ps1`
  - `.\scripts\Run-Phase16HudMapAtmosphereSmoke.ps1`
  - `.\scripts\Run-Phase18MvpPlaytestLoopSmoke.ps1`
- `.\scripts\Run-EditModeTests.ps1` passed with `177/177`.
- `.\scripts\Build-WindowsDev.ps1` passed.
- Open-editor `.\scripts\Run-PlayModeTests.ps1` still failed with `PlayModeTests did not return a Unity Test Runner completion callback within 120 seconds.`
- No `TestResults\playmode-results.xml` was produced in that open-editor Test Runner path.
- The editor remained in Play mode after the timeout. Restarting the Unity editor cleared the stuck state, and `.\scripts\Run-Phase16HudMapAtmosphereSmoke.ps1` passed afterward.

## Step 9 Open Editor Recheck (2026-06-07)

- Unity editor was launched for `D:\Bellerophon2\Bellerophon` with Unity `6000.3.16f1`.
- Open-editor feature smoke tests passed:
  - `.\scripts\Run-Phase15EquipmentLoopSmoke.ps1`
  - `.\scripts\Run-Phase16HudMapAtmosphereSmoke.ps1`
  - `.\scripts\Run-Phase18MvpPlaytestLoopSmoke.ps1`
- Full open-editor phase smoke recheck passed for all currently available phase smoke scripts through Phase18: Phase2, Phase4, Phase6, Phase7, Phase8, Phase9, Phase10, Phase11, Phase12, Phase13, Phase14, Phase15, Phase16, Phase17, and Phase18.
- Phase1, Phase3, and Phase5 do not currently have dedicated smoke scripts.
- Open-editor `.\scripts\Run-PlayModeTests.ps1` still failed twice with `PlayModeTests did not return a Unity Test Runner completion callback within 120 seconds.`
- A later open-editor retry after the Phase18 smoke recheck failed with the same callback timeout and produced no `TestResults\playmode-results.xml`.
- No PlayMode result XML was produced in the open-editor Test Runner path.
- `.\scripts\Build-WindowsDev.ps1` passed afterward through the open-editor bridge, so the stale Test Runner request did not block later bridge commands.

## Step 8 Recheck (2026-06-06)

- During detailed step 8 validation, feature-specific smoke tests completed, but the open-editor `UnityEditorValidationBridge` stopped picking up later bridge requests after a Test Runner pending state.
- Observed pending-only logs:
  - `Logs\PlayModeTests.log`
  - `Logs\Build-WindowsDev.log`
  - `Logs\HarnessValidation.log` during the final rerun attempt
- `Run-Phase15EquipmentLoopSmoke.ps1` and `Run-Phase18MvpPlaytestLoopSmoke.ps1` still passed because they use their own PlayMode smoke polling path.
- `UnityEditorValidationBridge` was updated so recovered PlayMode results are checked before the `isRunning` guard, and stale EditMode/PlayMode Test Runner requests fail out after 120 seconds instead of blocking future bridge requests.
- The already-open editor did not reload that patched bridge during this run. Restart or script-domain reload the Unity editor before re-running full bridge-based validation or Windows build.

## Step 3 Recheck (2026-06-05)

- During detailed step 3 validation, the open-editor PlayMode Test Runner entered PlayMode and loaded the generated init scene but did not return the completion callback.
- The stale bridge request left `Logs\PlayModeTests.log` at `pending` and did not produce `TestResults\playmode-results.xml`.
- Restarting only the Unity editor for this project cleared the stuck Test Runner state.
- Re-run result after restart: `.\scripts\Run-PlayModeTests.ps1` passed and produced `TestResults\playmode-results.xml` with `7/7` passed.
- Keep this as a stability watch item if a future PlayMode run remains pending without an active request file.

## Step 1 Result (2026-06-05)

- Status: completed and passed.
- `UnityEditorValidationBridge` now keeps Test Runner callback request data through PlayMode domain reload by using a serialized `ScriptableObject` callback.
- `Run-PlayModeTests.ps1` open-editor bridge timeout is 180 seconds.
- Open editor verification passed: `.\scripts\Run-PlayModeTests.ps1` produced `TestResults\playmode-results.xml` with `7/7` passed.
- The runner also wrote completion evidence to `Logs\PlayModeTests.log` instead of leaving a pending request.
- Baseline regression checks also passed: harness validation, EditMode tests, Phase18 MVP playtest loop smoke, and Windows dev build.

이 문서는 상세 구현 1단계에서 `Run-PlayModeTests.ps1` 안정화 상태를 추적하기 위한 작업 메모다.

## 기준

- `.\scripts\Run-PlayModeTests.ps1`는 결과 XML을 `TestResults\playmode-results.xml`에 생성해야 한다.
- 열린 Unity 에디터가 있을 때도 `UnityEditorValidationBridge`를 통해 완료 로그를 `Logs\PlayModeTests.log`에 남겨야 한다.
- PlayMode 진입 중 도메인 리로드가 발생해도 Test Runner 완료 콜백이 결과 저장까지 이어져야 한다.
- 실패 시에는 pending 로그만 남기지 않고 오류 로그 또는 누락된 결과 XML을 명확히 보고해야 한다.

## 1단계 조치

- `UnityEditorValidationBridge`의 Test Runner 콜백을 도메인 리로드 후에도 요청 정보를 복구할 수 있는 `ScriptableObject` 기반 콜백으로 변경한다.
- PlayMode 테스트 브리지 대기 시간을 90초에서 180초로 늘려 첫 컴파일, 씬 로딩, PlayMode 전환 지연을 흡수한다.
- `Run-PlayModeTests.ps1` 통과 여부를 1단계 완료 조건에 포함한다.

## 추적 상태

- 상태: 구현 중
- 마지막 확인: 1단계 검증에서 갱신한다.
- 재발 시 확인할 파일:
  - `Logs\PlayModeTests.log`
  - `TestResults\playmode-results.xml`
  - Unity Editor log
