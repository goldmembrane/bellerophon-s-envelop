# 애니메이션 규칙

이 문서는 Unity 애니메이션, 적대 개체 상태, 물리 기반 모션과 보조 물리 도구에 적용한다. 작업 시작 전 루트 `AGENTS.md`와 관련 디자인·모델링 규칙을 먼저 따른다.

## 애니메이션 승인과 샘플 예외

- 애니메이션 작업은 `artSample/` 샘플 제작이 면제된다. Unity 구현 전에 GIF, MP4, HTML, 별도 애니메이션 샘플 파일을 요구하지 않는다.
- 대신 정확한 Unity 대상, 클립·상태, 개체 범위, 명령, 검증 범위를 명시한 별도 묶음 승인을 받은 뒤 `AnimationClip`, `Animator`, 리깅, BlendShape, 물리 또는 승인된 기능형 애니메이션 방식으로 Unity에서 구현·검토한다.
- 이 예외는 애니메이션에만 적용된다. 신규·변경 모델링, 텍스처링, 머티리얼, VFX, UI, 사운드에는 `artSample/` 승인이 필요하다.
- 사용자가 애니메이션 샘플 제작을 직접 요구해 `artSample/`을 만들면 해당 샘플에 요약 HTML도 포함한다.

## 적대 개체 배치와 상태

- 사용자가 배치 범위를 명시적으로 다르게 지시하지 않는 한 복도 오브젝트 아래쪽에 적대 개체를 최소 5개 이상 배치한다.
- 배치한 개체 중 1개체는 기준 정적 상태 또는 비교용 상태로 둘 수 있다.
- 나머지 개체에는 각각 서로 다른 필요 애니메이션을 적용한다. 예: 대기, 이동, 공격, 피격, 사망.
- Unity 적용 전에 현재 승인 샘플, 현재 씬 상태, 필요한 애니메이션 상태, 배치 수, 검증 범위를 다시 확인하고 묶음 승인을 받는다.
- 승인된 샘플은 분위기 참고가 아니라 재현 대상이다. Unity 모델, 텍스처, 머티리얼, 실루엣, 부품 연결, 표면 질감을 가능한 한 가깝게 맞춘다.
- 슬라임, 액체형, 살덩이형처럼 한 덩어리로 보여야 하는 개체는 보이는 몸체를 여러 독립 오브젝트가 따로 노는 방식으로 구성하지 않는다. 가능한 한 단일 visible mesh, Shape Key/BlendShape, material slot, vertex color, weight 기반 변형으로 연결된 몸체처럼 보이게 한다.
- 보조 오브젝트가 필요해도 결과는 하나의 개체처럼 움직여야 한다. 코, 입술, 치아, 혀, 촉수, 표면 덩어리는 메인 몸체의 변형과 시간상 연결되어야 하며 독립적으로 떠 있거나 미끄러져 보이면 완료가 아니다.
- 공격 모션은 이동 모션보다 큰 실루엣 변화와 명확한 공격 의도를 가져야 한다. 물기·베기·찌르기 계열은 입술, 코·주둥이, 치아, 몸체 변형이 함께 반응하고 씹기 또는 타격 순간이 커브상 분리되어 보여야 한다.
- 피격 모션은 오브젝트가 깨지거나 분리되어 보이면 안 된다. 피격 방향으로 약간 물러나고 행동이 굼떠지는 recoil/slowdown이 보여야 한다.
- 사망 모션은 사망 연출 의도에 맞는 최종 형태 변화를 보여야 한다. 액체형은 바닥으로 녹아 퍼지고, 필요하면 입·눈·부속부가 사라지는 변화까지 포함한다.
- 사용자가 애니메이션 검토를 위해 전진 이동을 멈추라고 요구하면 애니메이션을 삭제하지 말고 root motion lock, kinematic Rigidbody, Animator 설정, 전용 배치 상태로 검토 가능하게 한다.

## 물리 기반 모션

- 물리 기반 모션이 필요한 모델, 적대 개체, 플레이어, 오브젝트는 단순 `transform.position` 또는 `transform.Translate` 직접 이동을 기본 구현으로 쓰지 않는다.
- 런타임 루트 이동은 `Rigidbody`와 `Collider`를 기준으로 하고, 물리 이동 처리는 `FixedUpdate`에서 한다.
- Motion Path Animation Editor는 Transform 직접 이동 도구가 아니라 경로·궤적·목표점 편집 도구로 쓴다. 실제 이동은 Motion Path 목표값을 `Rigidbody.linearVelocity`, velocity 제어, `AddForce`로 추종하게 구성한다.
- Blender 모델에 Unity 애니메이션을 부여할 때는 가능한 한 `Rigidbody + Collider + Motion Path target + Animation Rigging IK + ConfigurableJoint + Jiggle Physics` 구조를 우선 검토한다.
- `Animation Rigging`은 손, 발, 머리, 시선, 촉수 끝점, 무기·도구 잡기, 접지 보정처럼 IK·constraint가 필요한 부위에 쓴다.
- `ConfigurableJoint`는 active ragdoll, 물리 추종 관절, 흔들리는 부속물, 충돌 반응을 물리로 처리할 부위에 쓴다.
- `Jiggle Physics` 또는 동등한 보조 물리 도구는 슬라임, 액체형 몸체, 살덩이, 장비, 촉수, 표면 흔들림 같은 secondary motion에 쓴다.
- 같은 Transform을 Motion Path, Rigidbody, IK, Joint, Jiggle, AnimationClip이 동시에 직접 움직이지 않게 역할을 분리한다. `Motion Path=목표`, `Rigidbody=루트 이동`, `Joint=물리 관절`, `IK=끝점 보정`, `Jiggle=보조 흔들림`이다.
- 코드로 `.anim` 커브를 직접 생성하는 방식은 임시 검증용 또는 단순 보조 모션에 한정한다. 승인용·실제 적용용 모션은 Blender 모델 구조와 Unity 물리·IK·Joint·Jiggle 조합을 우선한다.
- Asset Store 유료 도구는 프로젝트에 실제 임포트된 뒤에만 기준으로 삼는다. 에이전트는 Unity 계정 구매, 다운로드, 라이선스 인증을 대신하지 않는다.

## 애니메이션 검토와 기록

- Blender Shape Key 또는 Unity BlendShape가 있는 모델은 `AnimationClip`에서 `blendShape.*` 커브가 실제로 바인딩됐는지 확인한다. Transform 커브만 있는 상태를 완료로 보고하지 않는다.
- 검증은 현재 씬과 작업 상태에 맞춘 전용 계획을 먼저 세운다. 검증 산출물은 `artSample/`이 아니라 `docs/validation/` 같은 문서·검증 경로에 둔다.
- 적용 완료 전 콘솔 에러, 애니메이션 중간 끊김, 개체 사라짐, 루트 이동 오작동, BlendShape 커브 바인딩, 정적·대기·이동·공격·피격·사망 상태 배치 여부를 확인한다.
- 작업 후 현재 날짜 `docs/PROGRESS_YYYY-MM-DD.md`에 적용 모델, 애니메이션 상태, 사용한 Unity 반영 명령, 실행하지 않은 검증, 남은 확인 사항을 기록한다.
