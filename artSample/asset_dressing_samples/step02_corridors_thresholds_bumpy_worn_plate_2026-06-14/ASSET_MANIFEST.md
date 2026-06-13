# 사용 에셋과 변형 목록

이번 시안은 현재 가져온 에셋 중 요철이 있는 철판 질감을 중심으로 구성했습니다.

| 역할 | 에셋 경로 | 사용 방식 |
| --- | --- | --- |
| 직선 복도 기본 뼈대 | `Assets/Sci-Fi Styled Modular Pack/Prefabs/Corridors/Corridor_I.prefab` | 복도 전체 길이와 통과 구조를 유지합니다. |
| 요철 철판 원본 프리팹 | `Assets/Heavy Station Kit/_common/Prefabs/plate.prefab` | 단면 평면이라 직접 반복 배치는 제외하고, 같은 질감 텍스처를 두께 있는 패널에 적용했습니다. |
| 요철 철판 색상 | `Assets/Heavy Station Kit/_common/Textures/plate_D.png` | 육각형 철판 무늬의 기본 표면으로 사용합니다. |
| 요철 철판 노멀 | `Assets/Heavy Station Kit/_common/Textures/plate_N.png` | 오돌토돌한 표면 요철을 표현합니다. |
| 요철 철판 반응 | `Assets/Heavy Station Kit/_common/Textures/plate_S.png` | 금속 표면 반응 보조 텍스처로 사용합니다. |
| 기본 바닥 보강판 | `Assets/Heavy Station Kit/BASE/Prefabs/Floors/Floor Base 3.prefab` | 철판 아래쪽 기본 구조로 남깁니다. |
| 입구 좌우 기둥 | `Assets/Heavy Station Kit/BASE/Prefabs/Walls/1 Wall/W1_D0.prefab` | 입구 프레임을 만들되 중앙 통로는 열어 둡니다. |
| 입구 상단 보강재 | `Assets/Heavy Station Kit/BASE/Prefabs/Walls/2 Walls/W2_D0.prefab` | 입구 상단 프레임으로 사용합니다. |
| 벽면 조명 | `Assets/Heavy Station Kit/BASE/Prefabs/Walls/Wall Lights/Wall Lights On.prefab` | 어두운 내부에서 기준 조명 역할을 합니다. |
| 낮은 난간 | `Assets/Heavy Station Kit/BASE/Prefabs/Floors Fill/_Handrails/St_Base2_Railing.prefab` | 하단 반복 기준선으로 사용합니다. |
| 천장 조명 | `Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_celing_1.prefab` | 천장 철판 사이에 조명 기준을 둡니다. |

## 변형 내용

- 원본 재질과 단면 평면 프리팹을 그대로 쓰지 않고 검토용 금속 재질과 두께 있는 패널을 새로 만들어 분홍색 오류 재질과 정면 시야 차단을 피했습니다.
- 철판 표면에 녹, 기름때, 그을음, 긁힘, 벗겨진 모서리를 추가했습니다.
- 철판 사이의 어두운 이음매를 두껍게 보이도록 조정했습니다.

## 이번 시안에서 제외한 것

- 실제 게임 씬 적용
- 충돌체와 통과 판정 설정
- 문 작동 또는 상호작용 연결
- 최종 머티리얼 에셋 생성
