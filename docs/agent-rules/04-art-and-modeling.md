# 아트, 에셋 및 모델링 규칙

## 아트/에셋 승인 규칙

- 모델링, UI, 애니메이션, 머티리얼, VFX, 사운드처럼 아트와 연관이 깊은 작업은 실제 게임 씬, 프리팹, 런타임 자산, UI 흐름에 붙이기 전에 먼저 저장소 루트의 `artSample/`에 사용자가 볼 수 있는 샘플 파일로 저장한다.
- 샘플 파일은 사용자가 검사할 수 있는 형식이어야 한다. 예: PNG/JPG/WebP 이미지, MP4/GIF 영상, HTML 미리보기, FBX/GLB 같은 범용 3D 파일, 또는 독립적으로 확인 가능한 Unity 샘플 씬/프리팹과 설명 문서.
- `artSample/`을 만들 때는 독립적인 그림이나 장식 시안으로만 만들지 않는다. 승인 후 Unity에서 어느 씬, 프리팹, 런타임 루트, 상호작용 앵커, 카메라 시점, 충돌 기준에 어떻게 반영될지 먼저 정리하고, 그 반영 방식에 맞는 축척, 위치, 부품 경계, 상태 전환, 표시/비표시 조건을 샘플에 드러낸다.
- `artSample/`의 설명 문구는 한국어를 기본으로 작성한다. 영어는 파일명, 코드 식별자, 고유 명사, 장비명처럼 필요한 경우에만 사용하고, 어설픈 혼합 표현으로 설명을 흐리지 않는다.
- 사용자가 `artSample/`의 샘플을 검사하고 승인한 뒤에만 해당 아트/UI/애니메이션 결과물을 실제 게임에 연결한다.
- 승인 전에는 해당 결과물을 실제 게임 씬, 프리팹, 런타임 자산, UI 흐름에 붙이지 않는다. 단, `artSample/` 생성을 위한 임시 파일과 미리보기 파일은 허용한다.

## 아트 샘플 요약 HTML 필수 규칙 (2026-08-18)

- 새 `artSample/` 샘플을 만들 때는 사용자가 결과를 한 페이지에서 확인할 수 있는 요약 또는 비교 HTML을 같은 샘플 폴더에 반드시 함께 만든다.
- HTML 형식은 해당 작업과 가장 가까운 기존 비교 페이지를 따른다. 이슈판트 관련 작업은 `artSample/enemies/ispant_armed/Ispant_Reference_Comparison.html`의 한국어 설명, 비교 이미지, 핵심 변경 사항, 승인 및 Unity 적용 상태 구성을 기준으로 한다.
- HTML에는 검토 이미지나 미디어를 상대 경로로 직접 표시하고, 주요 샘플 산출물로 이동할 수 있는 상대 링크를 제공한다. 원본 또는 목표, 수행 범위, 확인 수치나 검증 결과, 추론·생성 여부, Unity 적용 여부를 명확히 적는다.
- 애니메이션은 아래 `Art Validation Rule Override (2026-06-12)`에 따라 별도 `artSample/` 제작 자체가 면제된다. 다만 사용자가 애니메이션 샘플 제작을 직접 요구해 실제로 `artSample/`을 만들면 그 샘플에도 요약 HTML을 포함한다.

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
