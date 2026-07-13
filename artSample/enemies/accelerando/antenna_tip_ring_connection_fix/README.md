# 아첼레란도 더듬이 끝 고리 연결 보정 샘플

## 목적

기존 승인 샘플을 가까이서 볼 때 더듬이 끝과 사슬 시작부가 떠 보이는 문제를 줄이기 위해, 더듬이 끝에 장착형 금속 고리와 짧은 연결 핀을 추가했습니다.

## 승인 대상 파일

- `index.html`
- `exports/accelerando_antenna_tip_ring_connection_sample.glb`
- `exports/accelerando_antenna_tip_ring_connection_sample.blend`
- `renders/accelerando_antenna_tip_ring_connection_front.png`
- `renders/accelerando_antenna_tip_ring_connection_side.png`
- `renders/accelerando_antenna_tip_ring_connection_oblique.png`
- `renders/accelerando_antenna_tip_ring_connection_closeup_left.png`
- `renders/accelerando_antenna_tip_ring_connection_closeup_right.png`
- `renders/accelerando_antenna_tip_ring_connection_side_closeup_left.png`
- `renders/accelerando_antenna_tip_ring_connection_side_closeup_right.png`

## 반영 내용

- 기준은 기존 승인 샘플 `antenna_connection_color_fix`의 `.blend` 파일입니다.
- 양쪽 더듬이 끝에 금속 장착 고리, 칼라, 연결 핀, 리벳을 추가했습니다.
- `AntennaTip_Ring`, 장착 고리, 첫 사슬 링크의 중심을 겹치게 맞춰 측면 근접 시점에서도 분리되어 보이지 않도록 보강했습니다.
- 몸통, 껍질, 철퇴, 사슬의 기존 색과 재질 의도는 유지했습니다.

## Unity 적용 계획

샘플 승인 후 Unity 적용 단계에서는 `Assets/_Project/Art/Enemies/Accelerando/Models/`에 새 GLB를 별도 모델로 임포트하고, `CargoRunMvp`의 `Approved Accelerando Enemy Placement` 아래 Accelerando 7개 리뷰 오브젝트가 이 모델을 사용하도록 교체하는 방식이 적합합니다. 씬 적용은 별도 승인 후 진행해야 합니다.

## 적용하지 않은 항목

- Unity `Assets/`와 `CargoRunMvp.unity`에는 적용하지 않았습니다.
- 기존 승인 샘플 파일은 덮어쓰지 않았습니다.
- 런타임 프리팹, 애니메이션, 충돌, 배치 상태는 변경하지 않았습니다.
