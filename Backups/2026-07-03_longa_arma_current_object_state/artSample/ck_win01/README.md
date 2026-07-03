# ck_win01

조종실 내부 오브젝트 1번, 전면 단일 유리창과 내부 파노라마 화면 승인용 샘플입니다.

## 범위

- 포함: 넓은 단일 전면 유리, 두꺼운 외곽 금속 프레임, 내부 파노라마 화면, 상하 보강 프레임, 상단 라이트 슬롯, 고무 가스켓, 작은 마모 패치.
- 제외: 조종대, 조종석 콘솔, 수동 운행 UI 로직, 복도 연결.
- 5분할 유리판과 중앙 세로 멀리언은 제거했습니다. 조종 시야 중앙을 가리는 구조물을 두지 않는 방향입니다.
- 회색 반투명 구조물은 승인된 조종실 구조에 붙는 위치를 보여주는 배치 기준선이며 실제 창문 부품이 아닙니다.

## 기획 및 수정 근거

- 원본 기획서 기준 조종실 앞은 유리로 되어 있어 밖을 볼 수 있는 형태입니다.
- 사용자 수정 지시에 따라 5분할 창은 제거하고, 창 안쪽 대부분을 내부 화면이 채우도록 조정했습니다.
- 승인된 `cockpit_01` 구조의 전면 개구부에 맞는 폭과 높이를 기준으로 잡았습니다.

## 사용한 에셋 후보

- `Assets/Sci-Fi Styled Modular Pack/Models/big_screen.fbx` (Sci-Fi Styled Modular Pack big_screen)
- `Assets/_Project/Art/Props/Stage3Rework/Textures/HD_Stage3_GreenCrtScreen_Albedo.png` (Stage3 green CRT screen texture)
- `Assets/Sci-Fi Styled Modular Pack/Models/light_celing_1.fbx` (Sci-Fi Styled Modular Pack light_celing_1)
- `Assets/Heavy Station Kit/BASE/Meshes/Partitions/Part_G2.fbx` (Heavy Station Kit Part_G2)

## 승인 후 Unity 반영 방식

승인되면 이 샘플을 `Approved Cockpit 01 Structure`의 전면 개구부 안쪽에 별도 루트로 배치합니다.
콜라이더와 조종 로직은 추가하지 않고, 기존 검사용 자유 카메라와 비활성화된 튜토리얼 상태를 유지한 채 시각 모델만 붙입니다.
