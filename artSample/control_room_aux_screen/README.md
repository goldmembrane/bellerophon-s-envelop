# control_room_aux_screen

CR-07 통제실 가로형 보조 스크린 승인용 Blender 샘플입니다.

## 목적

CR-06 대형 메인 스크린보다 조금 더 위에 붙는 CR-07 가로형 보조 스크린의 위치, 비율, 장착 구조, 디스플레이 에셋 적용 상태를 확인하기 위한 샘플입니다.
실제 구역 상태 UI, CCTV 영상, 상호작용 로직은 포함하지 않았습니다.

## 반영 기준

- CR-06 대형 메인 스크린을 기준 패널로 함께 보여줍니다.
- CR-07은 CR-06 표시 면을 침범하지 않고, 메인 스크린보다 조금 더 위쪽 오른쪽 벽면에 붙는 얇은 가로형 보조 화면입니다.
- 디스플레이에는 `Assets/Heavy Station Kit/_common/Textures/GUI/C2_ElC2Disp.png`를 화면 면 전체에 꽉 차게 넣었습니다.
- 벽면 장착 패드, 방진 가스켓, 좌측 브래킷, 우측 케이블 소켓, 상부 케이블 레이스웨이로 실제 Unity 배치 시 부품 경계를 드러냈습니다.
- 이 샘플은 승인 전 검토용이며 Unity 씬에는 적용하지 않았습니다.

## 포함

- `blender/control_room_aux_screen.blend`
- `exports/control_room_aux_screen.fbx`
- `exports/control_room_aux_screen.glb`
- `renders/*.png` 5개 구도
- `index.html`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- 실제 구역 상태 UI
- CCTV 영상 피드
- 상호작용 로직
- Unity 씬 배치
- CR-08 세로형 보조 스크린 묶음
