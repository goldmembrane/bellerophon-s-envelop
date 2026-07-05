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
- 여기는 한국이기 때문에 기본적으로 인코딩으로 한글이 깨질 수 있다. 문서를 읽을 때는 한글부터 온전히 변환한 뒤에 내용을 파악한다.
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
- Animation work is exempt from required `artSample/` sample production. Do not require GIF, MP4, HTML, or separate animation sample files before Unity implementation. Animation work must instead receive a separate bundled approval for the exact Unity target, clip/state, object scope, commands, and validation range, then be implemented and reviewed in Unity using `AnimationClip`, `Animator`, rigging, BlendShape, physics, or the approved functional animation method.
- This animation exception applies only to animation. New or changed modeling, texturing, materials, VFX, UI, sound, or other non-animation art outputs still require inspectable `artSample/` approval before Unity runtime implementation unless the user explicitly approves a narrower rule update.
- Approved `artSample/` outputs are not mood references. When implementing them in Unity, the goal is to reproduce the approved `artSample/` sample as closely and exactly as possible.
- During Unity implementation, repeatedly compare the Unity result against the approved `artSample/` sample and iterate until the visual sync rate is acceptable.
- Do not replace visual sync with renderer-count, object-presence, or other internal validation checks. Internal validation may support the process, but user-approved `artSample/` visual matching is the quality gate.
- When an existing `artSample/` image is the target for modeling or texturing, treat it as a reproduction target rather than a creative prompt. Break the image into silhouette, proportions, major forms, individual parts, surface material, wear pattern, lighting, and camera angle, then model and texture those elements in Blender or the appropriate DCC tool.
- For approval samples made from a 2D reference, match the reference camera render first. Unseen backsides, interiors, exact dimensions, or mechanical details must be derived from `docs/GAME_DESIGN_SOURCE.txt` or explicitly marked as inference; do not invent visible design changes without user confirmation.
- Art/modeling/texturing completion requires side-by-side visual comparison against the target `artSample/` render or image. Do not report completion only because assets, renderers, object counts, FBX files, or materials exist.
- Texturing must include the material qualities needed by the reference, such as albedo variation, chipped paint, dirt, roughness/metalness response, and normal/bump detail. Superficial line scratches or flat colors are not enough when the reference shows rough worn surfaces.

## 모델링 교체 및 Unity 적용 범위 규칙 - 절대 규칙 (2026-07-03)

- 모델링 교체 작업은 승인된 모델 파일, 내보내기 파일, 텍스처, 머티리얼, 프리팹, 또는 사용자가 명시한 Unity 루트 오브젝트에만 한정한다.
- 사용자가 씬 전체 작업을 명시적으로 승인하지 않은 경우, 모델링 교체를 이유로 Unity 씬 전체를 열거나 저장하거나 덮어쓰지 않는다.
- Unity 씬 반영이 필요한 경우에는 대상 씬 경로, 대상 루트 오브젝트 이름, 교체할 하위 오브젝트, 유지해야 할 기존 오브젝트, 삭제 또는 비활성화할 오브젝트를 승인 요청에 구체적으로 적어야 한다.
- `Player`, `Hud`, `EventSystem`, 조명, 카메라, Phase 루트, graybox, 방 상호작용 루트처럼 모델 교체 대상이 아닌 기존 씬 루트는 사용자가 이름으로 명시하지 않는 한 읽기, 복사, 삭제, 비활성화, 저장 대상에 포함하지 않는다.
- 승인된 `artSample/` 모델을 Unity에 적용할 때도 샘플 승인과 런타임 씬 적용은 별도 작업으로 취급한다. 샘플 승인만으로 씬, 프리팹, 런타임 에셋을 변경하지 않는다.
- 모델 교체 스크립트나 Unity 브리지 명령을 작성할 때는 씬 전체를 여는 패턴을 기본값으로 삼지 않는다. 먼저 모델 임포트, 프리팹 교체, 지정 루트 하위 교체처럼 더 좁은 범위의 적용 방법을 검토한다.
- 기존 씬을 열어야만 하는 경우에는 `OpenSceneMode.Single` 사용 여부와 저장 여부를 승인 요청에 명시해야 하며, 승인받은 루트 외의 씬 오브젝트가 생성, 삭제, 복원, 이동, 이름 변경, 활성 상태 변경되지 않아야 한다.

## 적대 개체 샘플 제작 규칙 - 절대 규칙 (2026-06-28)

- 이 섹션은 적대 개체 샘플 제작에 적용되는 절대 규칙이며, 적대 개체 샘플을 완료로 보고하기 전에 반드시 충족해야 한다.
- 적대 개체 샘플은 먼저 `artSample/enemies/{enemy_id}/` 아래에 생성해야 하며, 사용자의 명시 승인 전에는 Unity 런타임 씬, 프리팹, 에셋, AI, 피격 판정, UI 흐름에 연결하지 않는다.
- 적대 개체 샘플은 `docs/GAME_DESIGN_SOURCE.txt`, 사용자 확인 사항, `image/` 폴더의 기준 이미지를 참고해 제작해야 한다.
- 적대 개체 샘플은 기준 이미지의 외형, 실루엣, 비율, 주요 부위, 색 분포, 재질, 질감, 표면 특성을 최대한 맞춰야 한다.
- 적대 개체 샘플에는 텍스처 적용과 머티리얼 적용을 반드시 포함해야 한다. 텍스처나 머티리얼이 없으면 직접 만든 절차적/생성 텍스처와 머티리얼이라도 제작해 적용해야 한다.
- 텍스처 또는 머티리얼 제작이 어렵거나 불완전할 경우, 기준 이미지를 분석해 색 분포, 표면 패턴, 거칠기, 광택, 투명도, 금속성/비금속성, 오염/손상, 요철감, 젖음/점액/분말/섬유/피부 같은 표면 성질을 정리하고 그 분석을 제작에 반영해야 한다.
- 단순 단색 머티리얼, 기본 셰이더, 임시 재질만 적용한 상태는 적대 개체 샘플 완료로 보지 않는다.
- 적대 개체 샘플은 사용자가 검사할 수 있는 정적 렌더와 검토에 필요한 원본/내보내기 파일을 포함해야 한다. 예: Blender, FBX, GLB, README, 승인 상태, 에셋 매니페스트, HTML 미리보기 파일.
- 애니메이션 GIF 또는 애니메이션 샘플은 적대 개체 샘플의 필수 조건이 아니다. 애니메이션은 Unity 적용 단계에서 사용자가 별도 승인한 범위 안에서 기능형 애니메이션 작업으로 다룬다.
- 승인된 적대 개체를 Unity에 구현할 때는 사용자가 Unity 적용 범위를 명시적으로 제한하지 않는 한, 승인된 `artSample/enemies/{enemy_id}/`의 시각 모델, 텍스처, 머티리얼 의도를 가능한 한 가깝게 재현해야 한다.

## 적대 개체 Unity 적용 규칙 - 절대 규칙 (2026-06-28)

- 이 섹션은 승인된 `artSample/enemies/{enemy_id}/` 적대 개체를 Unity 씬, 프리팹, 런타임 에셋에 적용할 때 적용되는 절대 규칙이다.
- 적대 개체를 Unity에 적용할 때는 사용자가 배치 범위를 명시적으로 다르게 지시하지 않는 한, 복도 오브젝트 아래쪽에 해당 적대 개체를 최소 5개 이상 배치해야 한다.
- 배치된 적대 개체 중 1개체는 기준 정적 상태 또는 비교용 상태로 둘 수 있다.
- 1개체를 제외한 나머지 적대 개체에는 각각 서로 다른 필요 애니메이션을 적용해야 한다.
- 필요 애니메이션은 해당 적대 개체의 Unity 적용 범위에서 요구되는 기능형 애니메이션을 의미한다. 예: 대기, 이동, 공격, 피격, 사망.
- 적대 개체 샘플 단계에서 애니메이션 GIF가 필수 조건이 아니더라도, Unity 적용 단계에서 이 규칙이 승인 범위에 포함되면 Unity `AnimationClip`, `Animator`, 기존 클립, 또는 승인된 기능형 애니메이션 방식으로 각 개체의 서로 다른 애니메이션 상태를 확인 가능하게 구성해야 한다.

## 물리 기반 모션 및 애니메이션 도구 규칙 - 절대 규칙 (2026-06-28)

- 물리 기반 모션이 필요한 모델, 적대 개체, 플레이어, 오브젝트는 단순 `transform.position` 또는 `transform.Translate` 직접 이동을 기본 구현으로 사용하지 않는다.
- 런타임 루트 이동은 `Rigidbody`와 `Collider`를 기준으로 하고, 물리 이동 처리는 `FixedUpdate`에서 수행한다.
- Motion Path Animation Editor는 실제 Transform 직접 이동 도구가 아니라 경로, 궤적, 목표점 편집 도구로 사용한다. 런타임 실제 이동은 Motion Path 목표값을 `Rigidbody.linearVelocity`, velocity 제어, 또는 `AddForce`로 추종하게 구성한다.
- Blender로 생성된 모델링에 Unity 애니메이션을 부여할 때는 가능한 한 `Rigidbody + Collider + Motion Path target + Animation Rigging IK + ConfigurableJoint + Jiggle Physics` 구조를 우선 검토한다.
- `Animation Rigging`은 손, 발, 머리, 시선, 촉수 끝점, 무기/도구 잡기, 접지 보정처럼 IK/constraint가 필요한 부위에 사용한다.
- `ConfigurableJoint`는 active ragdoll, 물리 추종 관절, 흔들리는 부속물, 충돌 반응을 물리로 처리해야 하는 부위에 사용한다.
- `Jiggle Physics` 또는 동등한 보조 물리 도구는 슬라임, 액체형 몸체, 살덩이, 장비, 촉수, 표면 흔들림 같은 secondary motion에 사용한다.
- 같은 Transform을 Motion Path, Rigidbody, IK, Joint, Jiggle이 동시에 직접 움직이지 않게 한다. 역할은 `Motion Path=목표`, `Rigidbody=루트 이동`, `Joint=물리 관절`, `IK=끝점 보정`, `Jiggle=보조 흔들림`으로 분리한다.
- 코드로 `.anim` 커브를 직접 생성하는 방식은 임시 검증용 또는 단순 보조 모션에 한정한다. 승인용/실제 적용용 모션은 Blender 모델 구조와 Unity 물리/IK/Joint/Jiggle 조합을 우선한다.
- Asset Store 유료 도구는 프로젝트에 실제 임포트된 뒤에만 해당 도구를 기준으로 작업한다. 에이전트는 Unity 계정 구매, 다운로드, 라이선스 인증을 대신하지 않는다.

## 승인 형식 규칙 - 절대 규칙 (2026-06-29)

- 에이전트가 파일 읽기, 명령 실행, 파일 수정, Unity 반영, 검증, 문서 갱신, Git 커밋/푸시처럼 작업 상태를 바꾸거나 확인하는 행동을 수행해야 하면 먼저 아래 형식의 승인 요청을 한국어로 출력하고 사용자의 명시 승인을 받아야 한다.
- 승인 요청은 반드시 `작업 승인 요청` 제목으로 시작하고, 아래 항목을 이 순서로 포함해야 한다.
  - `작업 목표`
  - `읽을 파일/범위`
  - `수정할 파일/범위`
  - `실행할 명령`
  - `검증 범위`
  - `실행하지 않을 항목`
- 항목에 해당 내용이 없으면 `없음`이라고 명시한다. 생략하지 않는다.
- 사용자가 `진행해`, `승인`, `좋아`, `계속 진행`처럼 직전 승인 요청 범위를 명확히 승인한 경우에만 해당 묶음 범위 안에서 작업한다.
- 승인받은 뒤에도 읽을 파일, 실행할 명령, 수정할 파일, 검증 범위, Unity 반영 범위, Git 대상이 바뀌면 즉시 중단하고 새 승인 요청을 출력한다.
- 승인 요청 없이 실행한 작업은 규칙 위반으로 취급하고, 사용자가 승인한 기존 작업 상태로 되돌리는 것을 우선한다.

## 적대 개체 Unity 애니메이션 적용 절차 - 절대 규칙 (2026-06-29)

- 이 섹션은 승인된 적대 개체 `artSample/enemies/{enemy_id}/`를 Unity 씬, 프리팹, 런타임 에셋에 적용하고 기능형 애니메이션을 구성할 때 적용한다.
- Unity 적용 전에는 현재 승인된 샘플, 현재 씬 상태, 필요한 애니메이션 상태, 배치 수, 검증 범위를 다시 확인하고 승인 형식 규칙에 따라 묶음 승인을 받아야 한다.
- 승인된 샘플은 분위기 참고가 아니라 재현 대상이다. Unity 모델, 텍스처, 머티리얼, 실루엣, 부품 연결, 표면 질감은 승인된 샘플과 가능한 한 가깝게 맞춘다.
- 슬라임, 액체형, 살덩이형처럼 한 덩어리로 보여야 하는 적대 개체는 보이는 몸체를 여러 독립 오브젝트가 따로 노는 방식으로 구성하지 않는다. 가능한 한 단일 visible mesh, Shape Key/BlendShape, material slot, vertex color, weight 기반 변형으로 연결된 몸체처럼 보이게 한다.
- 여러 보조 오브젝트가 필요하더라도 보이는 결과는 하나의 개체처럼 움직여야 한다. 코, 입술, 치아, 혀, 촉수, 표면 덩어리 같은 부위는 메인 몸체의 변형과 시간상 연결되어야 하며 독립적으로 떠 있거나 따로 미끄러져 보이면 완료로 보지 않는다.
- 기능형 애니메이션은 정적 비교 상태 1개체와 대기, 이동, 공격, 피격, 사망 같은 필요 상태를 분리해 확인 가능하게 구성한다. 사용자가 다르게 지시하지 않으면 적대 개체 Unity 적용 규칙의 최소 5개 이상 배치 기준을 유지한다.
- 공격 모션은 이동 모션보다 큰 실루엣 변화와 명확한 공격 의도를 가져야 한다. 물기/베기/찌르기 계열 공격은 입술, 코/주둥이, 치아, 몸체 변형이 함께 반응해야 하며, 씹기 또는 타격 순간이 커브상 분리되어 보여야 한다.
- 피격 모션은 오브젝트가 깨지거나 분리되어 보이면 안 된다. 피격 방향으로 약간 물러나고 행동이 굼떠지는 recoil/slowdown이 보이게 한다.
- 사망 모션은 해당 적대 개체의 사망 연출 의도에 맞는 최종 형태 변화가 보여야 한다. 액체형 개체는 바닥으로 녹아 퍼지고, 필요하면 입/눈/부속부가 사라지는 변화까지 포함한다.
- 루트 이동이나 물리 기반 이동이 필요한 경우 실제 런타임 이동은 `Rigidbody + Collider` 기준으로 처리하고, Motion Path는 목표/경로 편집 기준으로 사용한다. 같은 Transform을 Motion Path, Rigidbody, IK, Joint, Jiggle, AnimationClip이 동시에 직접 움직이지 않게 역할을 분리한다.
- Blender Shape Key/Unity BlendShape가 있는 모델은 Unity `AnimationClip`에서 `blendShape.*` 커브가 실제로 바인딩됐는지 확인한다. Transform 커브만 있는 상태를 실제 적용 애니메이션 완료로 보고하지 않는다.
- `Animation Rigging`, `ConfigurableJoint`, `Jiggle Physics` 또는 동등한 보조 물리 도구가 필요한 부위는 모델 구조와 Unity 적용 단계에서 우선 검토한다. 단, 프로젝트에 실제 임포트되지 않은 유료 도구는 기준으로 삼지 않는다.
- 사용자가 애니메이션 검토를 위해 전진 이동을 멈추라고 요구하면 애니메이션 자체를 삭제하지 말고 검토용 root motion lock, kinematic Rigidbody, Animator 설정, 전용 배치 상태로 확인 가능하게 한다.
- 기존 Harness/EditMode/PlayMode/Build 검증은 사용자가 명령명과 대상 범위를 명시적으로 승인한 경우에만 실행한다. 적대 개체 Unity 애니메이션 검증은 현재 씬과 현재 작업 상태에 맞춘 전용 검증 계획을 먼저 세워야 하며, 검증 산출물은 `artSample/`이 아니라 `docs/validation/` 같은 문서/검증 경로에 둔다.
- 적용 완료 보고 전에는 콘솔 에러, 애니메이션 중간 끊김, 개체 사라짐, 루트 이동 오작동, BlendShape 커브 바인딩, 정적/대기/이동/공격/피격/사망 상태 배치 여부를 현재 씬 기준으로 확인한다.
- 작업 후에는 현재 날짜 `docs/PROGRESS_YYYY-MM-DD.md`에 적용한 모델, 애니메이션 상태, 사용한 Unity 반영 명령, 실행하지 않은 검증, 남은 확인 사항을 기록한다.
