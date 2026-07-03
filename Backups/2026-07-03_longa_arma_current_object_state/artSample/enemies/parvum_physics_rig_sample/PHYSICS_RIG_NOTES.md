# 파르붐 물리 보조 모션용 모델링 보강 계획 반영

## Blender 샘플에 포함한 구조

- `Body_Core low translucent mound with shape keys`: 중심 점액 덩어리입니다. 튕김, 전진 눌림, 공격 반동, 피격 파동, 사망 납작화용 Shape Key를 포함합니다.
- `Outer_Gel_Skin transparent layered surface`: 투명 외피입니다. 중심 몸체와 같은 Shape Key 이름을 포함해 액체 표면이 따로 출렁일 수 있게 했습니다.
- `Left_Lobe`, `Right_Lobe`, `Rear_Mass`: Jiggle Physics 또는 Configurable Joint 보조체로 분리하기 위한 덩어리 경계입니다.
- `Mouth_Root`, `Upper_Jaw`, `Lower_Jaw`, `Tongue_Tip`: 공격/피격 시 과하게 떠오르지 않도록 제한 조인트를 걸 수 있는 입 부분 제어 기준입니다.
- `parvum physics control armature`: Blender에서 확인 가능한 제어 본입니다. Unity 적용 시 Animation Rigging, Motion Path, Joint/Jiggle 보조 모션의 기준점으로 사용할 수 있습니다.
- `Proxy_*`: Unity Rigidbody/Collider/Jiggle 보조체 분리를 위한 시각 프록시입니다. 실제 Unity 적용 전 승인 검토용 표시이며 런타임 연결은 아직 하지 않았습니다.

## 상태 포즈

- `06_idle_pulse_pose.png`: 정지 상태의 미세한 액체 호흡/출렁임.
- `07_move_squash_pose.png`: 이동 상태의 전방 눌림과 후방 지연.
- `08_attack_bite_pose.png`: 입은 전방으로 제한된 범위만 움직이고 중심 몸체가 함께 튕기는 공격 포즈.
- `09_hit_recoil_pose.png`: 입이 공중으로 뜨지 않도록 입 움직임은 줄이고 몸체 측면 파동을 강조한 피격 포즈.
- `10_death_flatten_pose.png`: 점액이 바닥으로 퍼지는 사망 포즈.

## Unity 적용 전제

- 이 샘플은 사용자 승인된 `artSample/` 결과물입니다.
- 승인 후에는 Motion Path Animation Editor로 루트 이동/공격 경로를 잡고, Jiggle Physics와 Animation Rigging/Joint 보조체로 몸체와 입의 지연 모션을 나눠 적용하는 구조가 적합합니다.
- Unity 적용 시 여러 덩어리, 프록시, 보조체 구조는 사용할 수 있습니다.
- 최종 Unity 시각 결과에서는 덩어리끼리의 경계선, 겹침선, 틈, 재질 경계가 보이면 안 됩니다.
- 경계 비노출을 위해 공통 머티리얼/쉐이더, 투명도/깊이 정렬, 외피 오버레이, 겹침 배치, 메시 병합 또는 스킨/쉐이프키 보강을 사용합니다.
- 내부 오브젝트 존재 확인만으로 완료 처리하지 않고 승인 샘플 렌더와 시각 비교해야 합니다.
