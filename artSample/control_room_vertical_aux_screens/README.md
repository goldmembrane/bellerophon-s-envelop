# control_room_vertical_aux_screens

CR-08 통제실 세로형 보조 스크린 묶음 승인용 Blender 샘플입니다.

## 목적

원본 기획서에는 통제실에 대형 스크린, 가로형 스크린, 세로형 스크린 여러 개가 필요하다고 되어 있습니다. 이 샘플은 CR-06 대형 메인 스크린과 CR-07 가로형 보조 스크린을 기준으로, CR-08 세로형 보조 스크린 묶음이 어느 위치와 비율로 붙을지 확인하기 위한 승인용 샘플입니다.

## 배치 기준

- CR-08은 CR-06 대형 메인 스크린의 왼쪽 보조 베이에 붙는 폭을 대폭 줄이고 높이를 확실히 늘린 3개 세로 패널 묶음 제안안입니다.
- CR-07은 오른쪽 상단 컨텍스트 프레임으로만 표시해서 CR-08과 충돌하지 않는지 확인할 수 있게 했습니다.
- 세로 패널 개수는 원본에 확정 수량이 없으므로 이번 샘플에서는 3개로 제안했습니다.
- 패널 안의 `ZONE`, `CCTV`, `LOCK` 표시는 실제 UI가 아니라 기능 방향을 암시하는 더미 화면입니다.
- Unity 반영은 하지 않았습니다.

## 포함

- `blender/control_room_vertical_aux_screens.blend`
- `exports/control_room_vertical_aux_screens.fbx`
- `exports/control_room_vertical_aux_screens.glb`
- `renders/*.png` 5개 구도
- `index.html`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- 실제 구역 상태 UI
- 실제 CCTV 영상 피드
- 실제 복도 폐쇄 로직
- 상호작용 로직
- Unity 씬 반영
