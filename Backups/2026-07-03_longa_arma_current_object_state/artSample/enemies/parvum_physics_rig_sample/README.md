# parvum_physics_rig_sample

파르붐 적대 개체의 물리 보조 모션 적용을 전제로 한 모델링 보강 샘플입니다.

## 목적

- 기준 이미지에 더 가까운 낮고 넓은 반투명 점액 실루엣을 잡습니다.
- 입/주둥이/윗입술이 몸체에서 떠 보이지 않도록 앞쪽 점액 브리지, 아래쪽 목 연결 점액, 측면 점액 덮개를 포함합니다.
- 기준 이미지처럼 이빨 길이를 늘리되 뿌리는 잇몸 안에 묻히도록 유지합니다.
- 혀는 유지하고, 입 안에 붉은 두 줄로 보이던 위/아래 잇몸선은 제거합니다.
- Unity 적용 시 Motion Path, Jiggle Physics, Animation Rigging, Joint 보조 모션을 나눠 걸 수 있도록 중심 몸체, 외피, 좌우/후방 덩어리, 입 루트를 구분합니다.
- 몸체 전체가 덩어리째만 움직이지 않도록 Shape Key와 제어 본/프록시 기준점을 포함합니다.

## 포함 파일

- Blender 원본: `blender/parvum_physics_rig_sample.blend`
- 범용 확인 파일: `exports/parvum_physics_rig_sample.fbx`, `exports/parvum_physics_rig_sample.glb`
- 기준 이미지 비교 렌더: `renders/01_front_reference_match.png`, `renders/02_side_reference_match.png`, `renders/03_back_reference_match.png`
- 물리 구조 렌더: `renders/04_top_anchor_map.png`, `renders/05_physics_proxy_overview.png`
- 상태 포즈 렌더: `renders/06_idle_pulse_pose.png`부터 `renders/10_death_flatten_pose.png`
- 분석 문서: `TEXTURE_ANALYSIS.md`, `PHYSICS_RIG_NOTES.md`

## 승인 상태

현재 상태는 `승인`입니다.

## Unity 적용 조건

- Unity 적용 시 여러 덩어리, 프록시, 보조체 구조는 사용할 수 있습니다.
- 최종 Unity 시각 결과에서는 덩어리끼리의 경계선, 겹침선, 틈, 재질 경계가 보이면 안 됩니다.
- 경계 비노출을 위해 공통 머티리얼/쉐이더, 투명도/깊이 정렬, 외피 오버레이, 겹침 배치, 메시 병합 또는 스킨/쉐이프키 보강을 사용합니다.
- 내부 오브젝트 존재 확인만으로 완료 처리하지 않고 승인 샘플 렌더와 시각 비교해야 합니다.
