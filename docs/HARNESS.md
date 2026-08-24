# Harness Engineering

## Unity Editor Launch

- Open the interactive Unity editor with `.\scripts\Open-UnityProject.ps1`.
- When code or asset changes need to be picked up by an already-open editor, run `.\scripts\Refresh-UnityProject.ps1` before considering an editor restart.
- Use `.\scripts\Open-UnityProject.ps1 -Restart -ValidateCargoRunScene` only as a last resort when refresh/bridge retry fails, the editor is opened on the wrong project, or a leftover lock/import worker keeps the project unusable.
- Do not open `.unity` scene files directly and do not hand-compose ad hoc `Start-Process Unity.exe ...` commands for the interactive editor.
- The launch script validates the project root, Unity version path, `CargoRunMvp` scene path, `-projectPath` command line, and then sends `OpenCargoRunMvpScene` through the editor bridge so the active scene is not Unity's default `Untitled` scene.
- The optional `-ValidateCargoRunScene` switch runs the CargoRun scene validation smoke after opening the scene.

## Full Phase 1-18 Smoke Sweep

- `Run-Phase1To18Smokes.ps1` is the default phase smoke sweep when validating MVP phase coverage.
- It runs Phase 1 through Phase 18 in numeric order and stops on the first failing phase.
- Phase 1, Phase 3, and Phase 5 use focused editor/model smoke validations; Phase 2, Phase 4, and Phase 6 through Phase 18 use the existing phase smoke scripts.
- After fixing a phase regression, rerun the full sweep unless the current task is only diagnosing a single failing phase.

## Detailed Step 10 Phase6/Phase12 Smoke

- `Run-Phase6RoomInteractionsSmoke.ps1` now covers the detailed step 10 control-room extension.
- The Phase6 smoke verifies right-click screen progression from main CCTV to the vertical room list, the actual room-button UI click path for selected-room internal purification, player damage inside the selected room, no room durability damage from purification itself, room reopening after the 30 second operation, ESC closing the control-room interaction screen, and ESC closing the engine-room and supply-room interaction panels.
- This control-room internal purification check is intentionally scoped to the clicked room only and must not be treated as the special item `복도 정화 장치`, which targets all corridors.
- `Run-Phase12ManualTurretSmoke.ps1` now covers the detailed step 10 armory/cockpit extension.
- The Phase12 smoke verifies upgraded weapon magazine/plasma behavior, plasma target neutralization, manual-flight booster reduction for the active asteroid hazard, and the rule that manual flight forces weapon-room auto turret mode.

## Detailed Step 8 Bridge Recovery

- `UnityEditorValidationBridge` now attempts recovered PlayMode result completion before honoring the in-memory `isRunning` guard.
- If an EditMode or PlayMode Test Runner request does not return a completion callback within 120 seconds, the bridge writes a failure log, clears Test Runner callback state, and releases later bridge requests instead of staying permanently blocked.
- If the already-open Unity editor has a stale bridge domain from before this fix, refresh or reload the editor before re-running bridge-based validation or Windows builds. Prefer `.\scripts\Refresh-UnityProject.ps1`; restart only when refresh cannot recover the editor.

## Art Validation Harness

- Generic harness, EditMode tests, PlayMode tests, and builds do not prove that art work matches the approved `artSample/` direction.
- For art-heavy runtime integration, use a focused art smoke or editor validation that checks the approved sample scope explicitly: required objects exist, sample-only review objects are absent, unpurchased or inactive equipment is hidden, generated materials use supported shaders, and first-person items do not block the camera/HUD.
- The focused art validation log must expose art-specific markers instead of only a generic pass marker. Example: `SampleOnlyLooseProps=0`, `CargoStraps=2`, `DeviceSurfaces=7`, `ArtSampleMatch=True`, or another task-specific count.
- When the art issue is visual shape, silhouette, placement, lighting, or screen readability, run a screenshot/render review path or capture a comparable preview before claiming completion. Structural tests alone are insufficient.
- Before creating an `artSample/` file, define how the approved sample would be translated into Unity: target scene or prefab, runtime root, anchor/interactable, camera viewpoint, scale, collision boundary, state-driven visibility, and which sample-only elements must stay out of live placement.
- `artSample/` files must explain review intent in Korean by default. Use English only for file names, code identifiers, proper names, and unavoidable asset labels.
- If an existing `artSample/` image is the modeling or texturing target, the validation path must treat it as a reproduction target. Break it down into silhouette, proportions, major forms, individual parts, surface material, wear pattern, lighting, and camera angle, then compare a Blender/DCC render against the reference before runtime integration.
- For 2D-only references, validate the matched camera render first. Any backside, interior, scale, or mechanical detail that is not visible in the image must come from `docs/GAME_DESIGN_SOURCE.txt` or be explicitly documented as inference.
- Art validation must not pass solely on renderer counts, object existence, FBX/GLB export existence, or material assignment. Those checks may support the process, but a side-by-side visual comparison remains required for shape, texture, and placement claims.

## Detailed Step 8 Phase15 Smoke

- `Run-Phase15EquipmentLoopSmoke.ps1` now covers the detailed step 8 item first pass in addition to the original Phase15 equipment loop.
- The smoke verifies representative purchases and active effects for flashlight utility, treatment recovery, protective gear damage reduction, and strength-enhanced melee damage.
- It also verifies the shop Sell tab still sells only the selected listed item after step 8 item usage, then resolves Parvum with strengthened stick damage and base musket damage.
- After a passing run, the smoke re-applies Phase16 HUD/map/atmosphere assets so `CargoRunMvp` does not remain saved at the lower Phase15-only scene state.

## Detailed Phase 1 PlayMode Stability

- `Run-PlayModeTests.ps1` must now create `TestResults\playmode-results.xml` as a required validation path.
- When a Unity editor is already open, `UnityEditorValidationBridge` must still save PlayMode Test Runner results through the open editor path.
- The PlayMode Test Runner bridge uses a `ScriptableObject` callback so request data survives domain reload during PlayMode entry.
- Stability status is tracked in `docs/PLAYMODE_TEST_RUNNER_STABILITY.md`.

## Detailed Step 21 Balance And Playtest Hardening

- `Run-DetailedStep21BalancePlaytestHardeningSmoke.ps1` validates the step 21 balance/playtest guardrails through the open editor bridge when possible, with the same batchmode fallback pattern as earlier detailed-step smokes.
- The smoke pins source-valued economy, repair, towing, hazard cadence, equipment price, special-route, and debt-recovery values. It is a guardrail against accidental tuning drift, not an approval path for changing source-valued balance.
- `Run-DetailedStep21FullSmokeSuite.ps1` runs the MVP phase sweep plus detailed step 13 through 21 smoke scripts in order and stops on the first failure.
- After changing balance-sensitive rules, run the focused step 21 smoke first, then the full detailed smoke suite when the change could affect prior detailed domains.

Bellerophon의 하네스는 AI/사람 개발자가 같은 구조, 같은 명령, 같은 완료 기준으로 작업하게 만드는 프로젝트 운영 계층이다.

## Codex 세션 규칙 하네스

- 프로젝트 설정은 `.codex/config.toml`의 `[features] hooks = true`와 `.codex/hooks.json`을 사용한다. 프로젝트 훅은 Codex가 저장소를 신뢰한 경우에만 로드되며, 훅 변경 뒤에는 새 세션의 `/hooks` 화면에서 내용을 검토하고 다시 신뢰해야 한다.
- `.codex/rule-sources.json`이 규칙 원본을 등록한다. 필수 원본은 `AGENTS.md`, `docs/DESIGN_RULES.md`, `docs/MODELING_RULES.md`, `docs/ANIMATION_RULES.md`이며, 저장소의 중첩 `AGENTS.md`·`AGENTS.override.md`, `docs/**/*_RULES.md`, `docs/agent-rules/**/*.md`, `docs/**/RULES.md`, `.codex/rules/**/*.md`도 동적으로 발견한다. `Backups`, Unity 생성물, `.codex/state`, 샘플 아트 경로는 활성 규칙 검색에서 제외한다.
- 중첩 `AGENTS.md`·`AGENTS.override.md` 전문도 세션에 제공하되 해당 디렉터리 트리에만 적용하고 작업 대상에 더 가까운 문서를 우선한다.
- `docs/HARNESS.md`는 하네스 설명과 운영 계약이며 규칙 전문 주입 대상은 아니다. 대신 `.codex/hooks.json`, `.codex/config.toml`, `.codex/rule-sources.json`, `.codex/hooks/**`와 함께 보호 매니페스트에 포함한다.
- `Invoke-RuleHook.ps1`은 Windows PowerShell 5.1이 BOM 없는 UTF-8 훅 파일을 ANSI로 오해하지 않도록 실제 훅 소스를 UTF-8로 읽어 실행하는 ASCII 전용 진입점이다. `.codex/hooks.json`의 모든 명령 훅은 이 진입점을 사용한다.
- `Initialize-RuleSession.ps1`은 `SessionStart`의 `startup`, `resume`, `clear`, `compact`와 `SubagentStart`에서 활성 규칙 전문을 주입하고 규칙·하네스 SHA-256 매니페스트를 세션 ID별 `.codex/state/`에 기록한다. 신뢰 검토 시점 등의 이유로 시작 훅이 건너뛰어져 매니페스트가 없으면 다음 `UserPromptSubmit`, `PreToolUse`, `PostToolUse` 또는 `Stop`에서 현재 세션 매니페스트를 복구한다.
- `Check-RuleViolations.ps1`과 `Capture-RuleApproval.ps1`은 어시스턴트의 여섯 섹션 승인 요청을 바로 다음의 100자 이하 `진행해`·`승인할게` 응답과 연결한다. 파일·명령 경계는 승인 요청에서 백틱으로 감싼 항목만 기계 범위로 사용하되, 빈 줄 위치나 설명용 불릿의 백틱 누락은 승인 실패 사유로 삼지 않는다.
- 승인된 실행 명령에 `*` 또는 `?`가 없으면 정규화된 전체 명령이 정확히 같을 때만 허용한다. 명시된 `*`와 `?`는 각각 임의 길이 문자열과 한 문자의 와일드카드로 처리하되 전체 명령에 앵커를 적용하고 나머지 정규식 문자는 이스케이프한다. 단독 `*`와 실행 파일 토큰 안의 와일드카드는 승인 요청 단계와 실행 단계에서 모두 거부한다.
- PowerShell에서 `Get-Content -LiteralPath file1,file2`처럼 쉼표로 나열한 경로는 각각 독립된 경로로 분리해 승인된 읽기·수정 범위와 비교한다.
- `.unity` 경로가 명령에 포함됐다는 이유만으로 Unity 실행으로 판정하지 않는다. 실제 `Unity.exe` 실행, Unity 또는 씬을 여는 `Start-Process`, 씬 파일 직접 호출만 전용 프로젝트 실행 스크립트 규칙으로 차단하며 `git diff`, `git add` 같은 파일 관리 명령은 승인 범위 안에서 허용한다.
- 모델링·애니메이션 수정 승인 요청의 기존 여섯 섹션은 유지하되 `검증 범위` 안에 `모호성 확인: 없음` 또는 `모호성 확인: 사용자 답변으로 해소됨`과 `검증 우선순위: 1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증`을 적어야 한다. 규칙·훅 자체만 고치는 유지보수 승인은 이 도메인 게이트에서 제외한다.
- 모호성 표기가 없거나 해소되지 않았으면 `Check-RuleViolations.ps1`이 `[RULE_HOOK_CLARIFY]`로 승인 요청을 차단하고 사용자에게 먼저 질문하게 한다. 직접 확인 우선순위가 누락되거나 역전되면 `[RULE_HOOK_DIRECT_CHECK_FIRST]`로 차단한다.
- 활성 승인 토큰에는 모델링·애니메이션 수정 여부와 두 게이트 통과 상태를 함께 기록한다. 해당 작업의 완료 응답은 직접 확인 근거를 먼저 제시해야 하며 수치·로그·스크립트 근거가 앞서거나 직접 확인 근거가 없으면 Stop에서 차단한다.
- `Guard-RuleCompliance.ps1`은 매처 `*`의 `PreToolUse` 및 `PermissionRequest`에서 매니페스트, 현재 턴 승인, 파일·명령 범위, 검증 명령 선제 금지를 검사한다. 불일치하면 실행 전에 거부하고 실패 사유를 기록하지만 반복 횟수로 세션을 중단하지 않는다.
- `Audit-RuleToolResult.ps1`은 매처 `*`의 `PostToolUse`에서 보호 파일 변경을 다시 검사한다. 승인되지 않은 변경은 후속 진행을 차단한다. 승인된 하네스 유지보수 변경은 매니페스트를 갱신하고 `restart-required` 상태를 남겨, 유지보수 범위 밖 다음 작업이 새 세션 재신뢰 전 실행되지 않게 한다.
- `Check-RuleViolations.ps1`은 `Stop`과 `SubagentStop`에서 승인 요청의 여섯 섹션 순서와 식별 가능한 백틱 범위, 한국어 최종 응답, 선제 금지 명령명을 정적으로 검사한다. 일반 최종 응답은 첫 Stop에서 `decision: block`과 내부 교정 프롬프트로 한 차례 더 자체 점검하며, 교정 연속 턴은 기존 승인 토큰의 범위만 유지한다. 반복 실패는 다시 교정할 수 있으며 세션을 중단 상태로 고정하지 않는다.
- 호스티드 도구는 공식 Codex 사양상 `PreToolUse`와 `PostToolUse` 경로를 통과하지 않는다. 따라서 실행 전 완전 차단은 보장할 수 없지만, 모든 일반 최종 응답에 대한 Stop 자체 점검으로 도구 없는 응답과 호스티드 도구 사용 결과도 교정 대상으로 포함한다. Stop은 이미 발생한 외부 부작용을 되돌릴 수 없으며, 이런 경우 에이전트가 사실을 보고하고 승인 범위를 넓히지 않도록 한다.
- 완료·중단 알림 스크립트의 정확한 단독 호출만 승인 토큰 없이 허용한다.

- UserPromptSubmit 처리기는 승인 저장과 규칙 컨텍스트 주입만 수행하며, 내부 오류가 나더라도 사용자 프롬프트에는 차단 결정을 반환하지 않는다. 오류는 경고와 추가 컨텍스트로 보고하고 이후 로컬 도구 경계에서만 차단한다.
- 승인 요청 검사는 읽기·수정·실행 섹션마다 `없음`·`- 없음`이거나 최소 하나의 백틱 경로 또는 명령이 있는지 확인한다. 설명용 불릿은 백틱이 없어도 허용한다. 수정 범위가 있으면 apply_patch 같은 실제 수정 도구도 실행 목록에 있어야 하며, 승인 뒤에는 그 도구와 대상 경로를 하나의 승인 토큰으로 저장한다.
- 훅 검증이 실패하면 해당 도구나 응답을 차단하고 사용자에게 즉시 보고하게 한다. 에이전트는 추가 승인을 요청하지 않고 기존 승인 범위 안에서 보완·재검증한 뒤 작업을 계속한다. 정정에 승인 범위 밖 파일·명령·대상·검증이 필요할 때만 기존 승인 규칙에 따라 추가 승인을 요청한다. 같은 사유가 반복되어도 세션 작업 상태를 중단으로 고정하지 않는다.
- 사용자가 규칙 문서 자체의 추가 또는 수정을 명시하고 승인 요청의 수정 범위에도 그 규칙 문서가 포함된 경우, 해당 규칙 문서 변경에 대한 매니페스트 검증은 생략한다. 승인 토큰과 다른 파일·명령의 범위 검사는 계속 유지한다.
- 공식 Codex 훅 사양에서 호스티드 도구는 로컬 PreToolUse 및 PostToolUse 경로를 통과하지 않으므로 완전한 사전 차단 대상이 아니다. UserPromptSubmit은 항상 통과시키고, 로컬 도구는 PreToolUse에서 차단하며, Stop은 응답을 한 번 자체 점검하게 하는 경계로 사용한다.

## 목적

- Unity 프로젝트 구조를 일관되게 유지한다.
- 기능 변경 후 컴파일, 테스트, 빌드 검증을 반복 가능하게 만든다.
- Steam, 세이브, 업적, 입력, 렌더링 같은 외부 의존성을 격리해 테스트 가능성을 유지한다.
- 실패한 검증은 문서, 코드 구조, 테스트 중 하나로 되먹여 같은 종류의 실수를 줄인다.

## 3C 모델

### Context

에이전트와 개발자가 참고해야 하는 지식이다.

- `AGENTS.md`: 에이전트 작업 규칙
- `docs/GAME_DESIGN.md`: 게임 방향과 핵심 루프
- `docs/ARCHITECTURE.md`: 코드/씬/플랫폼 구조
- `docs/DECISIONS`: 유지할 기술 결정 기록

### Constraint

결과물을 제약하는 규칙이다.

- Unity 버전은 `6000.3.x LTS` 계열만 사용한다.
- 런타임 로직은 `Assets/_Project/Runtime` 아래 둔다.
- 에디터 전용 자동화는 `Assets/_Project/Editor` 아래 둔다.
- 테스트는 `Assets/_Project/Tests/EditMode`와 `Assets/_Project/Tests/PlayMode`로 분리한다.
- 플랫폼 기능은 런타임 인터페이스 뒤에 둔다.
- 생성물 폴더는 커밋하지 않는다.
- 기획서에 없거나 애매한 기능 요구는 에이전트가 임의로 보강하지 않는다. 구현 전에 사용자에게 의도를 확인하고 답변을 받은 뒤 작업한다.
- 구현 계획을 세운 뒤 저장해둔 원본 기획서와 비교해 계획이 원본 방향과 맞는지 확인한다.
- 모델링, UI, 애니메이션, 머티리얼, VFX, 사운드처럼 아트와 연관이 깊은 작업은 실제 게임에 붙이기 전에 저장소 루트의 `artSample/`에 사용자가 검사할 수 있는 샘플 파일을 만들고, 사용자 승인 후 실제 씬/프리팹/런타임 자산/UI 흐름에 연결한다.

#### 중간 작업 경과 보고 절대 규칙

- 작업 시작, 원인 확인, 첫 수정 완료, 검증 결과 확인 시점마다 현재 상태를 즉시 사용자에게 보고한다.
- 새 결과가 없더라도 진행 중에는 5분마다 현재 단계, 지연 원인, 다음 행동을 보고하며 5분을 넘는 무응답 상태로 작업을 계속하지 않는다.
- 장기 실행 명령은 60초 이내 간격으로 상태를 확인하고, 실행 중이면 명령과 경과 시간 및 대기 사유를 보고한다.
- 같은 검증 실패가 두 번 연속 반복되면 추가 조정 전에 핵심 수치, 반복 원인 판단, 다음 수정 방향을 먼저 보고한다.
- 최종 캡처는 관련 검증 통과 후 한 번만 실행하며, 명시적으로 승인된 반복 캡처만 예외로 한다.
- 연결이나 스트리밍 문제로 경과 보고 전달 실패가 의심되면 새 변경을 누적하지 않고 마지막 완료 단계와 실행 상태를 먼저 확인해 보고한다.

### Convergence

작업 후 검증하고, 실패 원인을 하네스에 반영하는 반복 과정이다.

1. 구조 검증
2. EditMode 테스트
3. PlayMode 테스트
4. Windows 개발 빌드
5. 실패 원인을 문서/테스트/스크립트/아키텍처 규칙으로 반영

## 검증 명령

```powershell
.\scripts\Setup-GitForUnity.ps1
.\scripts\Bootstrap-UnityProject.ps1
.\scripts\Refresh-UnityProject.ps1
.\scripts\Run-HarnessValidation.ps1
.\scripts\Run-EditModeTests.ps1
.\scripts\Run-PlayModeTests.ps1
.\scripts\Run-Phase1To18Smokes.ps1
.\scripts\Run-Phase1SessionModelsSmoke.ps1
.\scripts\Run-Phase2PlayModeSmoke.ps1
.\scripts\Run-Phase3InteractionSystemSmoke.ps1
.\scripts\Run-Phase4CargoShipGrayboxSmoke.ps1
.\scripts\Run-Phase5ShipStateModelsSmoke.ps1
.\scripts\Run-Phase6RoomInteractionsSmoke.ps1
.\scripts\Run-Phase7NewGameStartSmoke.ps1
.\scripts\Run-Phase8TransportRunSmoke.ps1
.\scripts\Run-Phase9SettlementGameOverSmoke.ps1
.\scripts\Run-Phase10PlanetMaintenanceSmoke.ps1
.\scripts\Run-Phase11AsteroidHazardSmoke.ps1
.\scripts\Run-Phase12ManualTurretSmoke.ps1
.\scripts\Run-Phase13IntruderFrameworkSmoke.ps1
.\scripts\Run-Phase14ParvumIntruderSmoke.ps1
.\scripts\Run-Phase15EquipmentLoopSmoke.ps1
.\scripts\Run-Phase16HudMapAtmosphereSmoke.ps1
.\scripts\Run-Phase17CoopFoundationSmoke.ps1
.\scripts\Run-Phase18MvpPlaytestLoopSmoke.ps1
.\scripts\Run-PostDetailedStage3GameplayPropsSmoke.ps1
.\scripts\Run-PostDetailedStage3GameplayPropsArtValidation.ps1
.\scripts\Run-DetailedStep21BalancePlaytestHardeningSmoke.ps1
.\scripts\Run-DetailedStep21FullSmokeSuite.ps1
.\scripts\Run-AllChecks.ps1
.\scripts\Build-WindowsDev.ps1
```

## 열린 에디터 검증

사용자가 같은 프로젝트의 Unity 에디터를 열어 둔 상태라면 검증 명령은 그 에디터를 활용한다. 각 PowerShell 검증 스크립트는 열린 GUI 에디터를 감지하면 새 batchmode Unity를 띄우지 않고 `Assets/_Project/Editor/Validation/UnityEditorValidationBridge.cs`를 통해 검증 요청을 전달한다.

열린 에디터가 없으면 기존처럼 batchmode Unity를 실행한다. 열린 에디터 검증은 사용자의 에디터 세션에서 Test Runner와 BuildPipeline을 실행하므로 PlayMode 테스트와 빌드는 에디터 상태를 일시적으로 바꿀 수 있다.

열린 에디터 브리지에서 PlayMode 테스트를 요청할 때 에디터가 이미 Play mode이면 먼저 Edit mode 복귀를 기다린 뒤 Test Runner를 시작한다. Test Runner가 내부 오류를 보고하면 브리지는 실패 로그를 남기고 다음 요청을 받을 수 있는 상태로 복구한다.

PlayMode Test Runner는 Play mode 진입 중 도메인 리로드가 발생하므로, 브리지는 active 요청 파일을 남기고 도메인 리로드 후 콜백 재등록을 시도한다. 그래도 Unity Test Runner가 완료 콜백을 돌려주지 않으면 단계별 PlayMode smoke를 우선 검증 경로로 사용한다.

사용자가 에디터를 직접 확인하는 중에는 전체 `Run-PlayModeTests.ps1`보다 기능별 빠른 PlayMode smoke를 먼저 사용한다. 2단계 플레이어 MVP는 `.\scripts\Run-Phase2PlayModeSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 `CargoRunMvp`를 Play 모드로 짧게 실행하고, 런타임 플레이어/HUD/MainCamera/카메라 렌더/상호작용을 확인한 뒤 다시 Edit 모드로 돌아온다.

기능별 smoke는 해당 단계까지의 씬 구성을 재생성한다. 최신 단계까지 직접 플레이로 확인해야 할 때는 하위 단계 smoke를 마지막에 실행하지 말고, 현재 구현된 가장 높은 단계의 smoke 또는 bootstrap을 마지막에 실행해 `CargoRunMvp` 씬을 최신 단계 상태로 남긴다.

4단계 화물선 Graybox는 `.\scripts\Run-Phase4CargoShipGrayboxSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 `CargoRunMvp`의 6구역 화물선 Graybox를 재생성하고, Play 모드에서 주요 방/복도/상호작용 지점/카메라 렌더/플레이어 이동을 확인한 뒤 다시 Edit 모드로 돌아온다.

6단계 방별 상호작용 1차는 `.\scripts\Run-Phase6RoomInteractionsSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 `CargoRunMvp`의 6구역 장치 상호작용을 재생성하고, Play 모드에서 조종대/동력실 스크린/통제실 스크린/무기실 포탑 핸들/비품창고/운송창고 화물 상태 장치와 통제실 CCTV A/D 전환을 확인한 뒤 다시 Edit 모드로 돌아온다.

7단계 기본 시작 세팅과 튜토리얼 의뢰는 `.\scripts\Run-Phase7NewGameStartSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 협회 계약 최소 UI를 재생성하고, Play 모드에서 예 버튼, 협회 로고 행성 시작 상태, 돈 0/기본 화물선/기본 방호복/막대기 1개, 1분짜리 튜토리얼 의뢰 단독 노출, 운송창고 중앙 화물의 세션 운송 대상 등록을 확인한 뒤 다시 Edit 모드로 돌아온다.

8단계 자동/수동 운행 루프는 `.\scripts\Run-Phase8TransportRunSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 운행 상태 HUD와 수동 운행 화면을 재생성하고, Play 모드에서 튜토리얼 의뢰 1분 운행 진행도, 조종대 수동 운행 진입, WASD 회피 마커 이동, ESC 자동 조종 복귀, 조종실 내구도 50% 이하 자동 조종 불가 상태를 확인한 뒤 다시 Edit 모드로 돌아온다.

9단계 도착 정산과 게임오버는 `.\scripts\Run-Phase9SettlementGameOverSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 정산 UI와 게임오버 컷씬 루트를 재생성하고, Play 모드에서 운행 완료 후 정산 UI 자동 표시, 첫 마이너스 정산 유예, 다음 정산 후 마이너스 확정 게임오버, 플레이어 입력 억제, 전체 화면 화물선/포드 사출 컷씬을 확인한 뒤 다시 Edit 모드로 돌아온다.

10단계 행성 정비와 다음 운송 준비는 `.\scripts\Run-Phase10PlanetMaintenanceSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 정산 후 정비 화면, 수리 버튼, 후속 의뢰 목록, 상점/개인 화물/업그레이드 진입점을 재생성하고, Play 모드에서 정산 후 정비 화면 이동, 수리비 청구와 6구역 회복, 후속 협회 의뢰 선택과 다음 운송 시작을 확인한 뒤 다시 Edit 모드로 돌아온다.

11단계 외부 위험 구간 확장은 `.\scripts\Run-Phase11AsteroidHazardSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 외부 위험 런타임을 최신 운송/정산/정비 흐름에 연결하고, Play 모드에서 튜토리얼 운행에는 위험이 발생하지 않는지, 검증용 소행성 小/大, 화물 자유 연대 출몰 지역, 우주 해적 출몰 지역, 외계 생명체 출몰 구역 결과가 각각 선박 손상, 침입 이벤트, 포격 이벤트 중 원본에 맞는 결과로 이어지는지 확인한 뒤 다시 Edit 모드로 돌아온다. 은폐 블랙홀은 후반 위험 확장으로 보류되어 이 smoke에서 시작하지 않는다.

12단계 수동 포탑과 외부 목표는 `.\scripts\Run-Phase12ManualTurretSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 무기실 포탑 전체 화면, 외부 목표, 좌클릭 누르고 있기 연속 발사, 0.25초 연사 딜레이, 탄창/재장전, 명중 판정을 재생성하고, Play 모드에서 튜토리얼 이후 소행성 목표를 수동 포탑으로 파괴해 위험을 중립화하는 성공 경로와 목표를 방치해 선박 손상으로 이어지는 실패 경로를 확인한 뒤 다시 Edit 모드로 돌아온다.

13단계 침입자/적대 개체 프레임워크는 `.\scripts\Run-Phase13IntruderFrameworkSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 Phase13 프레임워크 루트를 재생성하고, 에디터 검증에서 침입 시도/성공, 구역 배치, 화물 공격, 구역 점유, 플레이어 공격, 화물선 파괴 목표 유형이 순수 규칙으로 계산되는지 확인한다. 구체 침입자 외형, 공격 모션, 실제 전투 연결은 이후 단계에서 별도 검증한다.

14단계 첫 침입자 씨앗체 구현은 `.\scripts\Run-Phase14ParvumIntruderSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 Phase14 파르붐 루트를 재생성하고, Play 모드에서 튜토리얼 첫 운행에는 씨앗체가 발생하지 않는지, 후속 운행 중 2초마다 15% 판정으로 파르붐이 내부 침입자로 발생하는지, 외부 목표가 생성되지 않는지, HUD 표시와 월드 placeholder 표시/숨김이 동작하는지, 파르붐의 0.5초 공격 피해가 정산 후 정비 수리비로 남는지 확인한다.

15단계 무기류와 비품실 기본 루프 및 상세 7단계 장비/상점 확장은 `.\scripts\Run-Phase15EquipmentLoopSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 Phase15 장비 HUD와 상점 루트를 재생성하고, Play 모드에서 협회 기본 지급 장비, 손 슬롯 기본 3칸, 비품창고 3칸, 정비 화면 상점 Buy/Sell, 머스킷/손전등/치료 아이템 구매와 보관, Sell 목록 선택 후 구매품 1% 처분/개인 화물 판매, 막대기/머스킷 파르붐 전투 연결, 머스킷 R 재장전 골격을 확인한다.

16단계 HUD, 맵, 분위기 1차는 `.\scripts\Run-Phase16HudMapAtmosphereSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 Phase16 HUD, 화물선 내부 맵, 분위기 조명/fog, 사운드 훅을 재생성하고, Play 모드에서 체력/보호막 표시, 현재 구역 맵 갱신, 기본 중앙 조준선 숨김, 머스킷 정밀 조준 레티클 토글, 우클릭 보조 모드 토글을 확인한다.

17단계 협동 플레이 기반은 `.\scripts\Run-Phase17CoopFoundationSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 실제 네트워크 패키지나 Steam 로비를 사용하지 않고, 로컬 권한 세션으로 2명의 플레이어 포즈/상호작용 상태, 장치 소유권, CCTV 상태, 운송 세션 상태, 선박 구역 피해가 같은 스냅샷으로 공유되는지 확인한다.

18단계 반복 가능한 플레이테스트 루프는 `.\scripts\Run-Phase18MvpPlaytestLoopSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 `CargoRunMvp`를 Play 모드로 실행하고, 협회 계약 시작, 튜토리얼 운송 완료, 정산/수리, 후속 협회 의뢰, 수동 회피, 수동 포탑 중립화, 파르붐 침입자 처치, 두 번째 정산과 다음 정비 준비까지 한 번에 확인한다. Phase16 HUD/맵/분위기와 Phase17 로컬 협동 스냅샷 경계도 같은 smoke 안에서 회귀 검증한다.

19단계 저장, 설정, 플랫폼 경계는 `.\scripts\Run-DetailedStep19SaveSettingsPlatformSmoke.ps1`로 검증한다. 이 스크립트는 저장 프로필에서 튜토리얼 스킵 가능 여부와 `$1100` 스킵 보상이 복구되는지, 설정 저장값과 저장 파일 버전 마이그레이션이 동작하는지, Steam SDK 없이 Null 업적/클라우드/통계 경계가 동작하는지 확인한다.

20단계 행성 허브, 프레젠테이션, 사운드 placeholder는 `.\scripts\Run-DetailedStep20PresentationSmoke.ps1`로 검증한다. 이 스크립트는 settlement 이후 행성 체류 허브가 정비 화면보다 먼저 연결되는지, 행성 허브의 시설 진입점과 지도 마커가 구성되는지, 선내 방별 프레젠테이션 placeholder 오브젝트와 `ShipSignalAudioHooks` cue hook이 유지되는지 확인한다.

Stage 3 gameplay props/equipment art work must finish with an art validation after the normal validation ladder. First regenerate or update the scene with `.\scripts\Run-PostDetailedStage3GameplayPropsSmoke.ps1`. Then run the normal checks such as `Run-HarnessValidation.ps1`, `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`, and `Build-WindowsDev.ps1` when the scene changed. Only after those pass, run `.\scripts\Run-PostDetailedStage3GameplayPropsArtValidation.ps1`. Completion may be reported only when that final art validation also passes and confirms art-specific markers such as `SampleOnlyLooseProps=0`, `CargoStraps=2`, `DeviceSurfaces=7`, and `ArtSampleMatch=True`.

## 테스트 정책

EditMode 테스트는 빠르고 결정적이어야 한다.

- 전투 수치 계산
- 아이템/스탯 규칙
- 세이브 데이터 직렬화
- 플랫폼 인터페이스의 Mock 구현

PlayMode 테스트는 Unity 런타임 통합을 확인한다.

- 씬 로딩
- 플레이어 스폰
- 입력/물리/충돌
- UI 흐름
- 콘솔 에러 없는 프레임 진행

## Steam 연동 정책

Steamworks SDK는 직접 게임 로직에 물리지 않는다. 다음 계층을 유지한다.

- `IPlatformServices`: 런타임이 바라보는 플랫폼 인터페이스
- `IPlatformAchievementServices`, `IPlatformCloudSaveServices`, `IPlatformStatsServices`: 업적, 클라우드 저장, 통계 세부 인터페이스
- 개발/테스트: Mock 또는 Null 구현
- Steam 빌드: Steam 구현

이 구조를 유지해야 Steam 없이도 대부분의 테스트를 실행할 수 있다.
