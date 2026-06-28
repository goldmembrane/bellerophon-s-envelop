# Physics-Based Motion Tooling Plan

작성일: 2026-06-28

## 목적

Unity 기반 물리 모션은 오브젝트를 단순히 `transform.position`이나 `transform.Translate`로 이동시키는 방식이 아니라, `Rigidbody`, `Collider`, `Joint`, IK, 보조 물리 모션을 역할별로 분리해 구성한다.

이 계획은 리썰 컴퍼니처럼 관성, 충돌, 미끄러짐, 흔들림, 부속물 반응이 느껴지는 모션을 만들기 위한 프로젝트 공통 기준이다.

## 설치 도구 상태

| 도구 | 상태 | 용도 |
| --- | --- | --- |
| Unity Animation Rigging (`com.unity.animation.rigging`) | `Packages/manifest.json`, `Packages/packages-lock.json`에 반영 | IK, constraint, 시선/팔다리/촉수 끝점 보정 |
| Jiggle Physics (`com.gator-dragon-games.jigglephysics`) | `Packages/manifest.json`, `Packages/packages-lock.json`에 OpenUPM scoped registry와 함께 반영 | 슬라임, 몸체 표면, 부속물의 secondary motion |
| Unity Physics / ConfigurableJoint | Unity 내장 기능 | Rigidbody 이동, Collider 충돌, active ragdoll, 물리 관절 |
| Motion Path Animation Editor | `Assets/ScriptBoy/MotionPathAnimationEditor`에 임포트 완료 | 경로/궤적 편집. 런타임 직접 이동이 아니라 물리 목표값 생성에 사용 |

## 구조 분리 원칙

물리 기반 모션에서 각 도구는 같은 Transform을 동시에 직접 움직이지 않는다. 역할은 아래처럼 분리한다.

```text
Motion Path = 목표 위치/경로/돌진 궤적 편집
Rigidbody = 실제 루트 이동과 충돌
Joint = 관절식 물리 반응과 active ragdoll 추종
IK = 손발/머리/촉수 끝점 보정
Jiggle = 표면, 살덩이, 장비, 슬라임 부속물의 보조 흔들림
```

## 런타임 이동 기준

- 루트 이동은 `Rigidbody`와 `Collider`를 기준으로 한다.
- 물리 이동은 `FixedUpdate`에서 처리한다.
- `transform.position`, `transform.Translate`로 런타임 물리 이동을 직접 처리하지 않는다.
- 일반 이동은 `Rigidbody.linearVelocity` 또는 감쇠가 포함된 velocity 제어를 우선한다.
- 미끄러짐, 둔한 관성, 밀림 느낌이 필요한 경우 `AddForce`를 사용한다.
- 회전 튐이 생기면 `Rigidbody.constraints`로 불필요한 축을 고정한다.

## Motion Path Animation Editor 사용 원칙

- Motion Path는 순찰 경로, 접근 경로, 돌진 궤적, 연출용 경로를 편집하는 도구로 사용한다.
- Motion Path가 런타임 Transform을 직접 이동시키게 하지 않는다.
- Motion Path에서 얻은 위치나 방향은 목표값으로만 사용하고, 실제 이동은 `Rigidbody`가 따라가게 한다.
- 경로 추종은 `targetPoint -> desiredVelocity/desiredForce -> Rigidbody` 흐름으로 구현한다.

## Animation Rigging 사용 원칙

- Blender에서 생성된 모델링에 뼈대가 있는 경우 Unity 적용 단계에서 `Animation Rigging`을 통해 IK/constraint를 구성한다.
- 손, 발, 머리, 시선, 촉수 끝점, 무기 손잡이, 바닥 접지 보정에 사용한다.
- IK 타겟 자체도 필요하면 Rigidbody/Joint의 결과를 따라가게 하며, 루트 이동과 충돌을 직접 대체하지 않는다.

## Joint 사용 원칙

- `ConfigurableJoint`는 active ragdoll, 물리 추종 관절, 흔들리는 부속물에 사용한다.
- Joint drive는 목표 자세/위치를 힘으로 따라가는 용도로 사용한다.
- 루트 이동은 Rigidbody가 담당하고, Joint는 하위 물리 부품의 반응을 담당한다.

## Jiggle Physics 사용 원칙

- 슬라임, 액체형 몸체, 천천히 흔들리는 부속물, 장비 흔들림에 사용한다.
- 파르붐 같은 연체형 개체는 IK보다 Jiggle/Joint/Physics 비중을 높인다.
- Jiggle은 보조 모션으로 사용하고, 충돌 판정이나 루트 이동을 대체하지 않는다.

## 모델링/애니메이션 제작 규칙

- Blender로 생성된 모델링은 Unity 적용 단계에서 물리 기반 모션 구조를 받을 수 있도록 루트, 몸체, 관절/부속물, 표면 흔들림 대상이 분리되어야 한다.
- 승인용/실제 적용용 모션은 `Rigidbody + Collider + Motion Path target + IK + Joint + Jiggle` 구조를 우선한다.
- 코드로 `.anim` 커브를 직접 찍는 방식은 임시 검증용 또는 아주 단순한 보조 모션에 한정한다.
- 사용자가 승인한 아트 샘플을 Unity에 적용할 때도 시각 재현을 유지하되, 모션 구조는 위 분리 원칙을 따라야 한다.

## 다음 적용 순서

1. Unity가 `Animation Rigging`과 `Jiggle Physics` 패키지를 정상 해석하는지 확인한다.
2. Motion Path Animation Editor의 문서와 데모를 확인해 프로젝트 적용 경로를 정리한다.
3. 물리 모션 공통 프리팹 구조를 설계한다.
4. 파르붐 같은 연체형 개체에는 `Rigidbody + Jiggle + Joint` 중심 구조를 먼저 시험한다.
5. 인간형/사지형 개체에는 `Rigidbody + Animation Rigging + ConfigurableJoint` 구조를 시험한다.
