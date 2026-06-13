# 사용 에셋과 배치 의도

아래 목록은 이번 철판 덮개 시안에서 사용한 주요 에셋과 배치 의도입니다.

| 역할 | 에셋 경로 | 배치 의도 |
| --- | --- | --- |
| 직선 복도 기본 뼈대 | `Assets/Sci-Fi Styled Modular Pack/Prefabs/Corridors/Corridor_I.prefab` | 복도 전체 형태와 통과 축을 유지합니다. |
| 꺾임 후보 기본 뼈대 | `Assets/Sci-Fi Styled Modular Pack/Prefabs/Corridors/Corridor_I.prefab` | 직선 복도 프리팹을 직각으로 맞물려 꺾임 후보를 확인합니다. |
| 내부 철판 덮개 | `Assets/Heavy Station Kit/BASE/Prefabs/Floors/Floor_5_base_Plate.prefab` | 바닥, 좌우 벽, 천장 안쪽을 덮는 핵심 부품입니다. 이번 시안의 주재료입니다. |
| 기본 바닥 보강판 | `Assets/Heavy Station Kit/BASE/Prefabs/Floors/Floor Base 3.prefab` | 철판 덮개 아래쪽에 기본 바닥 구조를 받쳐 줍니다. |
| 입구 좌우 기둥 | `Assets/Heavy Station Kit/BASE/Prefabs/Walls/1 Wall/W1_D0.prefab` | 입구가 막힌 벽처럼 보이지 않도록 좌우 기둥만 세웁니다. |
| 입구 상단 보강재 | `Assets/Heavy Station Kit/BASE/Prefabs/Walls/2 Walls/W2_D0.prefab` | 입구 위쪽 프레임을 잡되 중앙 통로는 열어 둡니다. |
| 벽면 조명 | `Assets/Heavy Station Kit/BASE/Prefabs/Walls/Wall Lights/Wall Lights On.prefab` | 철판으로 어두워진 내부에서 벽면 기준점을 만듭니다. |
| 낮은 난간 | `Assets/Heavy Station Kit/BASE/Prefabs/Floors Fill/_Handrails/St_Base2_Railing.prefab` | 복도 하단에 반복 기준선을 만들되 통로 중앙은 막지 않습니다. |
| 천장 조명 | `Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_celing_1.prefab` | 천장 철판 사이에 조명 기준을 둡니다. |
| 꺾임 바닥 연결부 | `Assets/Sci-Fi Styled Modular Pack/Prefabs/Joints/Joint_X_6.prefab` | 꺾임 후보의 바닥 연결 형태를 확인합니다. |

## 이번 시안에서 제외한 것

- 실제 게임 씬 배치 적용
- 통과 판정, 충돌체, 상호작용 앵커 연결
- 문 작동, 문턱 애니메이션, 게임플레이 상태 전환
- 이전 낡은 시안의 녹, 그을음, 과한 얼룩 표현

## 승인 후 적용 메모

승인되면 실제 화물선 복도에는 먼저 내부 철판 덮개를 적용하고, 이후 사용자 확인을 거쳐 낡음, 먼지, 긁힘 같은 표면 변형을 별도 단계로 추가합니다.
