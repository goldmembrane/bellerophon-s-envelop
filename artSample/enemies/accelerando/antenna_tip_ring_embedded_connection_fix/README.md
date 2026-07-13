# 아첼레란도 더듬이 끝 고리 매립 연결 샘플

## 목적

측면 근접 시점에서 사슬 끝 고리가 더듬이 끝과 살짝 떨어져 보이는 문제를 줄이기 위해, 사슬 시작 고리를 더듬이 끝부분 안쪽으로 일부 밀어 넣은 검토용 샘플입니다.

## 승인 대상 파일

- `index.html`
- `exports/accelerando_antenna_tip_ring_embedded_connection_sample.glb`
- `exports/accelerando_antenna_tip_ring_embedded_connection_sample.blend`
- `renders/accelerando_antenna_tip_ring_embedded_connection_front.png`
- `renders/accelerando_antenna_tip_ring_embedded_connection_side.png`
- `renders/accelerando_antenna_tip_ring_embedded_connection_oblique.png`
- `renders/accelerando_antenna_tip_ring_embedded_connection_closeup_left.png`
- `renders/accelerando_antenna_tip_ring_embedded_connection_closeup_right.png`
- `renders/accelerando_antenna_tip_ring_embedded_connection_side_closeup_left.png`
- `renders/accelerando_antenna_tip_ring_embedded_connection_side_closeup_right.png`

## 반영 내용

- 기준은 기존 승인 샘플 `antenna_connection_color_fix`의 `.blend` 파일입니다.
- 양쪽 `AntennaTip_Ring` 위치를 기존 표면 위치보다 위쪽과 안쪽으로 옮겨 더듬이 끝 실루엣에 일부 겹치게 했습니다.
- 사슬 링크 12개를 새 매립 시작점부터 철퇴 소켓까지 다시 배열해 첫 고리만 따로 떠 보이지 않도록 했습니다.
- 더듬이 끝 표면에는 어두운 껍질색 소켓 립과 압박 패드를 추가해 금속 고리가 몸체에 파묻힌 것처럼 보이게 했습니다.
- 몸통, 껍질, 철퇴, 사슬의 기존 색과 재질 의도는 유지했습니다.

## Unity 적용 계획

샘플 승인 후 Unity 적용 단계에서는 새 GLB를 `Assets/_Project/Art/Enemies/Accelerando/Models/`에 별도 모델로 임포트하고, `CargoRunMvp`의 `Approved Accelerando Enemy Placement` 아래 Accelerando 7개 리뷰 오브젝트가 이 모델을 사용하도록 교체하는 방식이 적합합니다. 씬 적용은 별도 승인 후 진행해야 합니다.

## 적용하지 않은 항목

- Unity `Assets/`와 `CargoRunMvp.unity`에는 적용하지 않았습니다.
- 기존 승인 샘플 파일은 덮어쓰지 않았습니다.
- 런타임 프리팹, 애니메이션, 충돌, 배치 상태는 변경하지 않았습니다.
