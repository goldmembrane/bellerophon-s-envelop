# Bellerophon Agent Harness

## 작업 범위 통제 및 승인 규칙

- 에이전트는 응답하기 전에 현재 적용 중인 규칙을 먼저 확인하고, 응답과 도구 실행이 그 규칙을 위반하지 않는지 점검해야 한다. 이 규칙은 절대 규칙으로 취급한다.
- 에이전트는 해당 작업을 수행하기 전에 해당 작업에 적용되는 규칙이 있는지 먼저 파악해야 한다. 이 규칙은 절대 규칙으로 취급한다.
- 에이전트가 규칙을 어겼을 때는 해당 규칙 위반 작업을 파기하고, 사용자가 승인한 기존 작업 상태로 되돌려야 한다.
- 이 규칙은 현재 세션에만 한정하지 않고 모든 후속 작업에 적용한다.
- 에이전트는 사용자가 명시한 단일 작업 범위만 수행한다.
- 에이전트는 작업 과정에서 필요한 파일 읽기, 명령 실행, 파일 수정, Unity 반영, 검증, 문서 갱신 등이 예상되면 이를 일일이 쪼개서 반복 승인받지 않고, 먼저 전체 과정을 파악해 필요한 작업 목록을 묶어서 한국어로 제시하고 사용자 검토와 명시 승인을 받아야 한다.
- 에이전트는 승인받은 묶음 범위 안에서만 진행해야 하며, 승인된 묶음 범위 밖의 작업이 필요해지면 즉시 중단하고 추가 묶음 승인 전까지 진행하지 않는다.
- 파일 읽기, 명령 실행, Unity 브리지/검증/스모크 실행, 파일 수정 전에는 읽을 파일, 실행할 명령, 수정할 파일 범위를 먼저 한국어로 제시하고 사용자의 명시 승인을 받아야 한다.
- 사용자가 특정 명령 또는 특정 파일 수정을 직접 지시한 경우에도, 그 지시 범위 밖의 파일, 명령, 검증은 추가 승인 없이는 수행하지 않는다.
- 작업 중 승인된 범위 밖 행동이 필요해지면 즉시 중단하고 사용자 승인 전까지 진행하지 않는다.
- Unity 복원, 재생성, Ensure, Validate, Smoke, Test, Build 계열 명령은 사용자가 명령명이나 대상 범위를 명시적으로 승인한 경우에만 실행한다.
- 사용자가 직접 편집한 내용을 스크립트에 반영하는 작업에서는 해당 편집값 반영 외의 수정, 정리, 리팩터링, 검증 확장, 재생성을 하지 않는다.
- 사용자가 Unity에서 편집한 후 스크립트 반영을 요청한 경우, 편집값을 스크립트에 반영한 뒤 해당 상태를 최종 복구 시점으로 저장해야 한다.

## Unity 에디터 실행 규칙

- Unity 에디터를 열거나 재시작할 때는 직접 `Unity.exe`, `.unity` 씬 파일, 또는 임의의 `Start-Process` 명령을 사용하지 않는다.
- 에디터 실행은 `.\scripts\Open-UnityProject.ps1`를 사용한다.
- 코드/에셋 변경을 열린 Unity 에디터에 반영해야 할 때는 재시작보다 `.\scripts\Refresh-UnityProject.ps1`를 먼저 사용한다.
- stale 컴파일, 잘못 열린 에디터, 남은 `Temp\UnityLockfile`, AssetImportWorker 잔여 프로세스가 의심되어도 먼저 리프레시와 열린 에디터 브리지 재시도를 시도한다. `.\scripts\Open-UnityProject.ps1 -Restart -ValidateCargoRunScene`는 리프레시가 실패하거나 에디터가 잘못된 프로젝트를 열고 있는 경우의 최후 수단으로만 사용한다.
- 이 스크립트는 `ProjectSettings\ProjectVersion.txt`와 `Assets\_Project\Scenes\CargoRunMvp.unity` 존재를 확인하고, `-projectPath D:\Bellerophon2\Bellerophon`로 열린 실제 프로젝트 에디터만 정상으로 인정한다.
- 에디터가 기본 `Untitled` 씬만 보여주는 상태는 정상 실행으로 보지 않는다. `Open-UnityProject.ps1`는 열린 에디터 브리지에 `OpenCargoRunMvpScene` 명령을 보내 `CargoRunMvp`가 활성 씬이 되도록 해야 한다.

## 이어서 작업할 때

- 이어서 작업을 시작하기 전 현재 날짜를 먼저 확인한다.
- 현재 날짜에 해당하는 `docs/PROGRESS_YYYY-MM-DD.md`가 있으면 그 문서를 먼저 읽고, 현재 구현 상태와 다음 단계 범위를 확인한다.
- 현재 날짜 진행문서가 없으면 `docs/`의 최신 `PROGRESS_YYYY-MM-DD.md`를 확인하고, 필요한 경우 사용자에게 새 날짜 진행문서 생성 여부를 확인한다.
- 고정된 과거 날짜 진행문서만 기준으로 이어서 작업하지 않는다.
- 진행상황 문서의 내용이 `docs/GAME_DESIGN_SOURCE.txt` 및 `docs/MVP_IMPLEMENTATION_ORDER.md`와 충돌하면 원본 기획서와 사용자 확인 사항을 우선하고, 애매한 부분은 구현 전에 사용자에게 질문한다.

이 저장소는 Unity 6.3 LTS 기반 Steam 출시용 게임 프로젝트다. 에이전트는 기능 구현보다 먼저 프로젝트의 구조적 일관성과 반복 가능한 검증 루프를 유지해야 한다.

## 기본 규칙

- 대화는 한국어로 한다.
- 모든 대화는 항상 존댓말로 한다.
- 코드 식별자, 네임스페이스, 파일명은 영어를 사용한다.
- 애매모호하거나 없었던 사항은 멋대로 판단하여 구현하지 말고 사용자에게 정확한 설명을 요청하고 답변을 받은 뒤에 작업한다.
- 사용자가 직접 편집한 내용을 스크립트에 반영하는 작업에서는 요청된 반영 범위만 수행하고, 해당 작업과 무관한 수정, 정리, 리팩터링, 검증 확장 같은 추가 작업을 임의로 하지 않는다.
- 계획을 세우고 나서 저장해둔 원본 기획서와 계획이 맞게 구성되었는지 비교한다.
- 새 변수를 추가하기 전에는 기존 코드, 설정, 데이터 정의에 같은 용도로 쓰이는 변수가 있는지 먼저 확인한다. 새 변수가 필요하면 그 변수가 어디에서 어떤 역할을 하는지 작업 메모, 관련 문서, 또는 코드 근처의 최소 주석 중 적절한 위치에 기록한다.
- Unity 버전은 `6000.3.x LTS` 계열로 고정한다. 현재 기준 버전은 `6000.3.16f1`이다.
- `Library`, `Temp`, `Logs`, `UserSettings`, `Builds`, `TestResults`는 생성물로 취급하고 직접 편집하지 않는다.
- 작업 목표 달성에 파일 삭제나 생성물 정리가 필요하면 에이전트가 대상 경로와 작업 범위를 스스로 확인한 뒤 사용자에게 삭제 승인 요청이나 삭제 예고를 하지 않고 바로 진행한다. 단, 작업 범위 밖의 사용자 변경, 저장소 이력 조작, `git reset --hard` 같은 복구 불가능한 작업은 명시 요청 없이는 수행하지 않는다.
- 런타임 게임 로직은 가능한 한 `MonoBehaviour`에서 분리해 EditMode 테스트가 가능하게 만든다.
- Steam, 저장소, 업적, 클라우드 같은 플랫폼 기능은 인터페이스 뒤에 둔다. 게임 로직이 Steamworks SDK를 직접 참조하지 않게 한다.
- 새 기능은 최소 하나 이상의 검증 경로를 가져야 한다. 순수 로직은 EditMode 테스트, 씬/입력/물리/UI는 PlayMode 테스트를 우선한다.

## 아트/에셋 승인 규칙

- 모델링, UI, 애니메이션, 머티리얼, VFX, 사운드처럼 아트와 연관이 깊은 작업은 실제 게임 씬, 프리팹, 런타임 자산, UI 흐름에 붙이기 전에 먼저 저장소 루트의 `artSample/`에 사용자가 볼 수 있는 샘플 파일로 저장한다.
- 샘플 파일은 사용자가 검사할 수 있는 형식이어야 한다. 예: PNG/JPG/WebP 이미지, MP4/GIF 영상, HTML 미리보기, FBX/GLB 같은 범용 3D 파일, 또는 독립적으로 확인 가능한 Unity 샘플 씬/프리팹과 설명 문서.
- `artSample/`을 만들 때는 독립적인 그림이나 장식 시안으로만 만들지 않는다. 승인 후 Unity에서 어느 씬, 프리팹, 런타임 루트, 상호작용 앵커, 카메라 시점, 충돌 기준에 어떻게 반영될지 먼저 정리하고, 그 반영 방식에 맞는 축척, 위치, 부품 경계, 상태 전환, 표시/비표시 조건을 샘플에 드러낸다.
- `artSample/`의 설명 문구는 한국어를 기본으로 작성한다. 영어는 파일명, 코드 식별자, 고유 명사, 장비명처럼 필요한 경우에만 사용하고, 어설픈 혼합 표현으로 설명을 흐리지 않는다.
- 사용자가 `artSample/`의 샘플을 검사하고 승인한 뒤에만 해당 아트/UI/애니메이션 결과물을 실제 게임에 연결한다.
- 승인 전에는 해당 결과물을 실제 게임 씬, 프리팹, 런타임 자산, UI 흐름에 붙이지 않는다. 단, `artSample/` 생성을 위한 임시 파일과 미리보기 파일은 허용한다.

## 검증 사다리

변경 범위에 따라 아래 명령을 낮은 단계부터 실행한다.

1. `.\scripts\Run-HarnessValidation.ps1`
2. `.\scripts\Run-EditModeTests.ps1`
3. `.\scripts\Run-PlayModeTests.ps1`
4. `.\scripts\Build-WindowsDev.ps1`

빌드, 씬 구성, 패키지, 프로젝트 설정을 바꿨다면 `Build-WindowsDev.ps1`까지 확인한다.

## 완료 기준

- 컴파일 에러가 없어야 한다.
- 관련 테스트가 통과해야 한다.
- Unity 콘솔 에러를 새로 만들지 않아야 한다.
- 하네스 규칙을 바꿨다면 `docs/HARNESS.md`와 관련 스크립트를 같이 갱신한다.

## Art Validation Rule Override (2026-06-12)

- This section has highest priority within this file and overrides earlier art/asset approval rules when they conflict.
- For art, modeling, and texturing work, first save inspectable samples under `artSample/` and receive explicit user approval before implementing them in Unity runtime scenes, prefabs, assets, or UI flows.
- Approved `artSample/` outputs are not mood references. When implementing them in Unity, the goal is to reproduce the approved `artSample/` sample as closely and exactly as possible.
- During Unity implementation, repeatedly compare the Unity result against the approved `artSample/` sample and iterate until the visual sync rate is acceptable.
- Do not replace visual sync with renderer-count, object-presence, or other internal validation checks. Internal validation may support the process, but user-approved `artSample/` visual matching is the quality gate.
- When an existing `artSample/` image is the target for modeling or texturing, treat it as a reproduction target rather than a creative prompt. Break the image into silhouette, proportions, major forms, individual parts, surface material, wear pattern, lighting, and camera angle, then model and texture those elements in Blender or the appropriate DCC tool.
- For approval samples made from a 2D reference, match the reference camera render first. Unseen backsides, interiors, exact dimensions, or mechanical details must be derived from `docs/GAME_DESIGN_SOURCE.txt` or explicitly marked as inference; do not invent visible design changes without user confirmation.
- Art/modeling/texturing completion requires side-by-side visual comparison against the target `artSample/` render or image. Do not report completion only because assets, renderers, object counts, FBX files, or materials exist.
- Texturing must include the material qualities needed by the reference, such as albedo variation, chipped paint, dirt, roughness/metalness response, and normal/bump detail. Superficial line scratches or flat colors are not enough when the reference shows rough worn surfaces.
