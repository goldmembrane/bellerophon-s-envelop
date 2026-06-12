# Stage 3 Rework Review Sample

작성일: 2026-06-12

이 폴더는 현재 `CargoRunMvp`에 붙어 있는 Stage 3 게임플레이 소품과 1인칭 장비를 전면 재검수하기 위한 아트 샘플입니다.

샘플은 승인 전 검토용입니다. 현재 Unity 씬, 프리팹, 런타임 자산, UI 흐름에는 새로 연결하지 않았습니다.

## 2026-06-12 수정 반영

- `02_control_room_cctv_terminal_review.png`
  - 원본 기획서 기준에 맞춰 통제실 CCTV를 모니터 여러 개가 나란히 달린 형태가 아니라, 벽면의 대형 스크린 1개 중심 구조로 수정했습니다.
  - 대형 스크린 왼쪽 상단에는 가로형 보조 스크린, 오른쪽에는 세로형 스크린이 붙는 구조로 정리했습니다.
  - CCTV 구역 전환은 대형 스크린 앞에서 A/D 버튼으로 넘기는 방식이 읽히도록 했습니다.
- `07_first_person_equipment_review.png`
  - 막대기가 짧은 한손 도구처럼 보이던 문제를 수정했습니다.
  - 60cm 양손 근접무기로 보이도록 상하 길이와 손잡이 간격을 유지했습니다.
  - 끝부분은 빠루처럼 굽은 갈고리형 프라이 팁이 읽히도록 다시 수정했습니다.
  - 1인칭 적용 장면에서 두 손으로 잡고 위에서 아래로 내려찍을 수 있는 자세가 보이도록 했습니다.

## 검수 파일

- `index.html`: 전체 검수 갤러리
- `01_cockpit_helm_and_status_review.png`: 조종실 조타 장치와 상태 화면
- `02_control_room_cctv_terminal_review.png`: 통제실 단일 대형 CCTV 스크린 구성
- `03_engine_room_power_terminal_review.png`: 동력실 전력 단말
- `04_supply_room_storage_cabinet_review.png`: 비품창고 보관장
- `05_cargo_hold_props_and_terminal_review.png`: 운송창고 상태 패널, 화물 컨테이너, 경고 라벨, 단말
- `06_armory_turret_grip_mount_review.png`: 무기실 포탑 손잡이 마운트
- `07_first_person_equipment_review.png`: 1인칭 막대기, 머스켓, 방호복 손목 표시 장치

## 검수 방식

- 각 이미지는 왼쪽에 부품별 클로즈업, 오른쪽에 해당 구역 적용 모습을 함께 보여줍니다.
- 실제 적용 시에는 기존 상호작용 앵커, 충돌 기준, 카메라 시야, HUD/맵 안전 영역을 보존해야 합니다.
- 화물은 운송 대상과 상태 오브젝트로만 취급하며, 직접 집기/이반/소모품 상호작용은 만들지 않습니다.
- 승인 전에는 이 샘플을 실제 게임 씬, 프리팹, 런타임 자산, UI 흐름에 연결하지 않습니다.
