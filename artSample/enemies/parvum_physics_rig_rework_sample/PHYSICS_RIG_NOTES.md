# 파르붐 물리 기반 모션 제작 메모

## 모델링 구조

- 보이는 모델은 `Unified_Parvum_Reference_Matched_Single_Mesh` 하나입니다.
- 몸체, 주둥이 표면, 입 안쪽, 치아, 혀 디테일은 모두 같은 메시 데이터 안에 포함됩니다.
- 별도의 원통형 주둥이, 내부 물방울, 검은 선, 독립 점액 덩어리 오브젝트는 사용하지 않았습니다.

## Shape Key

- `Idle_Pulse_Surface_Jiggle`: 한 덩어리 점액 표면의 약한 맥동입니다.
- `Move_Squash_Forward_Slosh`: 몸 전체가 낮아지고 앞쪽으로 쏠리는 이동 상태입니다.
- `Attack_Bite_Core_Kick`: 입 주변과 앞면 몸체가 함께 전진하는 공격 상태입니다.
- `Hit_Recoil_Side_Wave`: 충격이 한 덩어리 몸체 전체로 전달되는 피격 상태입니다.
- `Death_Flatten_Liquid_Spread`: 몸이 낮아지고 옆으로 퍼지는 사망 상태입니다.

## Unity 실제 적용 방식

- 루트 이동은 `Rigidbody + Collider` 기준이어야 합니다.
- Motion Path는 목표 경로나 목표점 편집용으로만 사용하고, 실제 이동은 Rigidbody velocity 또는 force로 추종해야 합니다.
- Jiggle Physics는 표면 보조 흔들림에 사용합니다.
- ConfigurableJoint는 입 주변 질량이 몸체에서 분리되어 보이지 않게 제한 추종할 때만 사용합니다.
