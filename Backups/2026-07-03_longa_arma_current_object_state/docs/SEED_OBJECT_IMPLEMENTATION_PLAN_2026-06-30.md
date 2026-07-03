# 씨앗체 오브젝트 구현 계획 2026-06-30

## 1. 규칙 및 현재 상태 확인

- 현재 날짜: 2026-06-30.
- 오늘 날짜 진행문서 `docs/PROGRESS_2026-06-30.md`는 아직 없다.
- 최신 진행문서는 `docs/PROGRESS_2026-06-29.md`다.
- `AGENTS.md` 기준으로 파일 읽기, 명령 실행, 파일 수정, Unity 반영, 검증은 모두 사전 묶음 승인 후 진행해야 한다.
- 적대 개체 아트/모델링/텍스처 작업은 먼저 `artSample/enemies/{enemy_id}/` 아래에 검사 가능한 샘플을 만들고, 사용자 승인 전에는 Unity 런타임 씬, 프리팹, 에셋, AI, 피격 판정, UI 흐름에 연결하지 않는다.
- 승인된 적대 개체를 Unity에 적용할 때는 승인 샘플을 분위기 참고가 아니라 재현 대상으로 취급한다.
- 적대 개체 Unity 적용 시 사용자가 다르게 지시하지 않으면 복도 오브젝트 아래쪽에 최소 5개 이상 배치하고, 정적 비교 1개체와 서로 다른 기능형 애니메이션 상태를 확인 가능하게 구성한다.
- 물리 기반 이동이 필요한 개체는 단순 `transform.position` 또는 `transform.Translate` 직접 이동을 기본 구현으로 사용하지 않고, `Rigidbody + Collider` 기준으로 처리한다.

## 2. 최근 진행 상태

- 파르붐은 `artSample/enemies/parvum_physics_rig_rework_sample/` 승인 샘플을 기준으로 Unity에 적용됐다.
- `CargoRunMvp` 씬의 `Approved Parvum Enemy Placement` 아래에 6개 파르붐이 배치됐다.
- 배치 상태는 정적 비교 1개체와 `Idle`, `Move`, `Attack`, `Hit`, `Death` 5개 기능형 애니메이션 개체다.
- 파르붐 적용 파일은 `Assets/_Project/Art/Enemies/Parvum/`, `Assets/_Project/Prefabs/Enemies/Parvum/ParvumApproved.prefab`, `Assets/_Project/Scenes/CargoRunMvp.unity`, `Assets/_Project/Runtime/Enemies/Parvum/ParvumPhysicsMotionDriver.cs` 등으로 기록되어 있다.
- 최신 기록 기준으로 `Run-HarnessValidation.ps1`, `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`, `Build-WindowsDev.ps1`, Smoke, Ensure, Validate, Build 계열 명령은 실행하지 않았다.
- 최신 기록에 `AI, 공격/피격 판정, 런타임 이동 로직 추가 구현`은 수행하지 않은 작업으로 남아 있다.

## 3. 다음 씨앗체 대상 판단

다음 씨앗체 구현 대상으로는 `푸가(Fuga)`를 계획 대상으로 잡는다.

근거:

- `docs/MVP_IMPLEMENTATION_ORDER.md`는 14단계 첫 씨앗체 MVP 구현 대상을 파르붐 1종으로 확정했다.
- 같은 문서는 이후 후보로 `푸가`, `롱가 아르마`, `테르고`, `우르제레`, `소치에타스`, `몬스트룸`, `미메시스`를 다시 확정해야 한다고 기록한다.
- `docs/GAME_DESIGN_SOURCE.txt`의 씨앗체 목록은 파르붐 다음에 푸가를 둔다.
- 사용자는 이전 확인에서 푸가의 1차 재현 기준을 `fuga2` 계열 이미지로 지정했다.
- `image/`에는 1차 기준 이미지 `fuga2(푸가).png`, `fuga2-back.png`, `fuga2-beside.png`가 준비되어 있다. `fuga(푸가).png`, `fuga-back.png`, `fuga-beside.png`는 보조 비교 참고로만 사용한다.
- 푸가는 파르붐과 같은 금속 우선 목표, 구역 내구도 피해, 공격받으면 공격자로 타겟 변경 구조를 공유한다. 따라서 파르붐 이후 공통 씨앗체 구조를 확장하기에 적합하다.
- 롱가 아르마, 테르고, 우르제레, 소치에타스, 몬스트룸, 미메시스는 각각 긴 팔 내려찍기, 후방 기습, 구역 강화 버프, 군집, 대형 충격, 플레이어 의태처럼 별도 AI/애니메이션 복잡도가 더 크므로 다음 첫 확장 대상으로는 푸가보다 위험도가 높다.

주의:

- `MVP_IMPLEMENTATION_ORDER.md`가 이후 씨앗체는 구현 전에 다시 확정한다고 기록했으므로, 이 문서는 `푸가를 다음 대상으로 제안하는 계획`이다.
- 실제 구현 착수 전에는 사용자에게 `푸가로 진행` 승인을 다시 받아야 한다.

## 4. 푸가 원본 기획 요약

원본 기획서 기준 푸가:

- 분류: 씨앗체.
- 크기: 높이 약 60cm, 가로 약 40cm, 세로 약 20cm.
- 외형: 조류의 날개를 가진 소라 형태의 비행 개체.
- 이동: 날아다니는 비행 개체, 이동속도 3.5.
- 체력: 65.
- 1순위 목표: 금속.
- 공격 방식: 상대에게 근접해 날개로 타격.
- 공격 사거리: 1.
- 데미지: 10.
- 공격 딜레이: 1초.
- 공격 대상: 화물선 전체 시설. 화물선 내부 시설은 금속으로 취급한다.
- 방치 결과: 해당 구역 내구도 감소.
- 운송 화물이 금속 재질일 경우 화물 섭취 가능. 단, 현재 화물 모델에 재질 필드가 없다면 파르붐 때처럼 임의 구현하지 않고 구역 내구도 피해를 우선한다.
- 일정 시간 이상 공격받은 구역에는 `파괴 부위`가 발생하고, 파괴 부위 방치 시 해당 구역 내구도가 지속 감소한다.
- 금속 섭취 중 공격받으면 타겟을 공격자로 변경한다.

## 5. 구현 범위 제안

### 5.1 1차 범위: 푸가 artSample 제작

목표:

- Unity 적용 전 검토 가능한 푸가 샘플을 `artSample/enemies/fuga/` 아래에 만든다.
- `fuga2(푸가).png`, `fuga2-back.png`, `fuga2-beside.png`를 1차 재현 대상으로 삼고, 단순 분위기 참고로 처리하지 않는다.
- `fuga(푸가).png` 계열은 1차 형태를 흔들지 않는 범위에서 보조 비교용으로만 둔다.

산출물:

- `artSample/enemies/fuga/blender/fuga_sample.blend`
- `artSample/enemies/fuga/exports/fuga_sample.fbx`
- `artSample/enemies/fuga/exports/fuga_sample.glb`
- `artSample/enemies/fuga/textures/`
- `artSample/enemies/fuga/renders/`
- `artSample/enemies/fuga/index.html`
- `artSample/enemies/fuga/README.md`
- `artSample/enemies/fuga/TEXTURE_ANALYSIS.md`
- `artSample/enemies/fuga/PHYSICS_RIG_NOTES.md`
- `artSample/enemies/fuga/ASSET_MANIFEST.json`
- `artSample/enemies/fuga/APPROVAL_STATUS.json`
- `artSample/enemies/fuga/tools/build_fuga_sample.py`

필수 렌더:

- `fuga2(푸가).png` 정면 기준 이미지 비교 렌더.
- `fuga2-beside.png` 측면 기준 이미지 비교 렌더.
- `fuga2-back.png` 후면 기준 이미지 비교 렌더.
- 사선 런타임 시야 렌더.
- 비행 대기 자세.
- 이동 활공 자세.
- 날개 타격 공격 자세.
- 피격 자세.
- 사망 또는 추락 자세.

모델링 방향:

- `fuga2` 계열의 실루엣, 비율, 소라 껍질 몸체, 날개 형태, 접합부, 색 분포를 우선 재현한다.
- 소라 껍질 몸체와 조류형 날개가 하나의 생물처럼 보이게 구성한다.
- 날개는 별도 부품이더라도 몸체와 접합부가 떠 보이거나 따로 미끄러져 보이면 안 된다.
- 공중 비행 개체이므로 바닥 접지보다 호버 기준 높이와 그림자/실루엣 확인을 샘플에 포함한다.
- 공격 모션은 이동 활공과 명확히 구분되도록 날개가 앞으로 감기거나 내려치는 큰 실루엣 변화를 가져야 한다.
- 피격 모션은 조각 분리나 깨짐이 아니라 짧은 균형 상실, 흔들림, 고도 저하로 표현한다.
- 사망 모션은 공중에서 힘을 잃고 바닥으로 떨어지거나 껍질/날개가 접혀 멈추는 방향으로 계획한다.

텍스처/머티리얼 방향:

- 소라 껍질은 나선형 결, 거친 표면, 마모, 색상 변화, roughness/normal detail을 포함한다.
- 날개는 깃털 또는 막질 구조가 기준 이미지에 보이는 방식에 맞춰 알베도 변화, 가장자리 마모, 요철감을 포함한다.
- 단순 단색 머티리얼, 기본 셰이더, 임시 재질만으로는 완료 처리하지 않는다.
- `index.html` 하단에 사용 텍스처와 머티리얼 목록을 별도로 표시한다.

### 5.2 2차 범위: 사용자 승인 후 Unity 적용

전제:

- `artSample/enemies/fuga/` 샘플을 사용자가 승인해야 한다.
- Unity 적용 전에는 승인된 샘플, 현재 씬 상태, 필요한 애니메이션 상태, 배치 수, 검증 범위를 다시 확인하고 별도 묶음 승인을 받아야 한다.

예상 적용 경로:

- `Assets/_Project/Art/Enemies/Fuga/Models/fuga.fbx`
- `Assets/_Project/Art/Enemies/Fuga/Models/fuga_runtime_blendshape_mesh.asset`
- `Assets/_Project/Art/Enemies/Fuga/Textures/`
- `Assets/_Project/Art/Enemies/Fuga/Materials/`
- `Assets/_Project/Art/Enemies/Fuga/Animations/`
- `Assets/_Project/Art/Enemies/Fuga/Animations/Controllers/`
- `Assets/_Project/Prefabs/Enemies/Fuga/FugaApproved.prefab`
- `Assets/_Project/Runtime/Enemies/Fuga/FugaPhysicsMotionDriver.cs`
- `Assets/_Project/Editor/FugaCargoRunScene/FugaCargoRunSceneApplyAndReview.cs`
- `Assets/_Project/Scenes/CargoRunMvp.unity`

Unity 배치:

- `CargoRunMvp` 씬에 `Approved Fuga Enemy Placement` 루트를 만든다.
- 사용자가 다르게 지시하지 않으면 복도 오브젝트 아래쪽에 최소 6개체를 배치한다.
- `Fuga_00_Static`: 정적 비교 상태.
- `Fuga_01_Idle`: 공중 호버 대기.
- `Fuga_02_Move`: 날개짓 이동 또는 활공.
- `Fuga_03_Attack`: 근접 날개 타격.
- `Fuga_04_Hit`: 피격 recoil과 고도 흔들림.
- `Fuga_05_Death`: 추락 또는 힘 빠진 정지.

애니메이션:

- 가능한 경우 Blender Shape Key 또는 Armature 기반 변형을 Unity `AnimationClip`에 실제 바인딩한다.
- Transform 커브만으로 완료 처리하지 않는다.
- 공격 클립은 이동 클립보다 큰 날개 실루엣 변화와 타격 순간을 가져야 한다.
- 검토를 위해 전진 이동이 방해되면 애니메이션을 삭제하지 않고 root motion lock, kinematic Rigidbody, Animator 설정, 전용 배치 상태로 잠근다.

물리/이동:

- 런타임 루트 이동은 `Rigidbody + Collider` 기준으로 처리한다.
- 비행 경로나 공격 접근 경로는 Motion Path 목표값으로만 사용하고, 실제 이동은 `Rigidbody.linearVelocity`, velocity 제어, 또는 `AddForce` 계열로 추종한다.
- 같은 Transform을 Motion Path, Rigidbody, AnimationClip, IK, Joint, 보조 흔들림이 동시에 직접 움직이지 않게 역할을 분리한다.

### 5.3 3차 범위: 씨앗체 런타임/게임플레이 연결

전제:

- 푸가 Unity 시각 적용이 승인되고 검토 상태가 정리된 뒤 진행한다.
- 현재 파르붐 기록처럼 AI, 공격/피격 판정, 런타임 이동 로직이 아직 남은 상태라면, 푸가 전용 구현 전에 공통 씨앗체 런타임 구조를 먼저 정리해야 한다.

구현 방향:

- 씨앗체 침입 판정은 원본 기준을 유지한다. 튜토리얼 첫 운행 제외, 이후 운행 중 2초마다 15% 확률.
- 침입은 외부에서 저지할 수 없고 내부 대응만 가능하게 한다.
- 푸가는 금속 우선 타겟을 사용한다.
- 화물 재질 필드가 없으면 금속 화물 섭취는 구현하지 않고, 파르붐과 동일하게 구역 내구도 피해와 수리비 반영을 먼저 검증한다.
- 구역 내구도 피해, 공격받으면 공격자로 타겟 변경, 처치/실패/운송 완료 시 정산 반영 범위를 공통 씨앗체 프레임워크에 맞춘다.
- 비행 개체이므로 지상 이동 NavMesh만 가정하지 않고, 복도/구역 내 호버 높이와 충돌체 높이를 별도 설정값으로 둔다.

## 6. 검증 계획

검증은 구현 단계별로 별도 승인 후 실행한다.

artSample 단계:

- Blender 생성 명령 종료 코드 확인.
- 생성된 `.blend`, `.fbx`, `.glb`, 텍스처, 렌더, 문서, 승인 상태 파일 존재 확인.
- 기준 이미지와 샘플 렌더를 나란히 비교한다.
- 텍스처/머티리얼이 단순 단색 또는 기본 셰이더 수준이 아닌지 확인한다.
- `APPROVAL_STATUS.json`은 사용자 승인 전 `requiresUserApprovalBeforeUnity=true`, `unityApplicationAllowed=false` 상태로 둔다.

Unity 적용 단계:

- `.\scripts\Refresh-UnityProject.ps1` 후 승인된 Unity 브리지 명령으로만 적용한다.
- 씬에 정적/대기/이동/공격/피격/사망 상태가 배치됐는지 확인한다.
- BlendShape 또는 Armature 바인딩이 실제 애니메이션 클립에 들어갔는지 확인한다.
- 공중 호버 높이, 충돌체, Rigidbody 설정, root motion lock 검토 상태를 확인한다.
- 콘솔 에러, 개체 사라짐, 애니메이션 중간 끊김, 루트 이동 오작동을 확인한다.

코드/게임플레이 단계:

- 순수 규칙은 EditMode 테스트로 검증한다.
- 씬/입력/물리/UI는 PlayMode 테스트 또는 smoke로 검증한다.
- 변경 범위가 씬 구성이나 프로젝트 설정까지 포함되면 검증 사다리에 따라 `Build-WindowsDev.ps1`까지 검토한다.
- 단, Harness/EditMode/PlayMode/Smoke/Build/Ensure/Validate 계열 명령은 사용자가 명령명과 대상 범위를 명시 승인한 경우에만 실행한다.

## 7. 구현 전 확인 질문

실제 구현 전에 사용자 확인이 필요한 항목:

- 다음 씨앗체를 `푸가`로 확정할지.
- 푸가 1차 재현 기준 이미지는 사용자 이전 확인에 따라 `fuga2(푸가).png`, `fuga2-back.png`, `fuga2-beside.png`로 확정한다.
- 푸가 작업을 먼저 `artSample/enemies/fuga/` 샘플 제작까지만 할지, 승인 후 Unity 적용 계획까지 한 번에 묶어 진행할지.
- 푸가 런타임 연결은 파르붐의 AI/공격/피격/런타임 이동 로직 정리 이후로 둘지, 푸가와 함께 공통 씨앗체 프레임워크를 확장할지.
- 화물 재질 필드가 아직 없다면 금속 화물 섭취를 계속 보류하고 구역 내구도 피해만 우선 검증해도 되는지.

## 8. 이번 계획문서 작성에서 실행하지 않은 항목

- Unity Refresh/Bridge 실행.
- Unity 씬, 프리팹, 런타임 에셋 수정.
- Blender 실행.
- `artSample/enemies/fuga/` 생성.
- 실제 푸가 구현.
- Harness/EditMode/PlayMode 테스트.
- Smoke, Ensure, Validate, Build.
- Git 작업.
