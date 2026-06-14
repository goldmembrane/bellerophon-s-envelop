# 2단계 복도 사용자 지정 에셋 조합 시안 - 벽 메움 수정판

이 시안은 사용자가 지정한 바닥/벽 조합을 실제 `CargoRunMvp` 씬에 적용하기 전 확인하기 위한 `artSample` 렌더입니다.
이 수정판은 이전 시안에서 벽 구멍이 과하게 보였던 문제를 줄이기 위해 벽 뒤쪽에 추가 벽 에셋 백킹과 보조 패널을 촘촘히 덧댔습니다.
검게 뚫려 보이던 측면과 복도 끝단에는 연속 뒤판과 `Wall 2 Variant.prefab` 폐쇄 패널을 추가했습니다.
런타임 씬, 프리팹, 프로젝트 설정, 원본 Asset Store 파일은 수정하지 않았습니다.

## 구성

- 바닥 하부: `Floor_5_base_Plate.fbx` 반복
- 바닥 상부: `Floor Base 1 F.prefab`를 2열로 촘촘하게 반복
- 벽: `Wall 2.FBX`와 `Wall 2 Half.FBX`를 좌우에 교대로 연결
- 벽 메움: `Wall 2 Variant.prefab`, `Wall 2 Half Variant.prefab`, `Wall Pillar.prefab`, `Wall Pillar 3.prefab`를 뒤쪽 백킹과 이음매 보강으로 추가
- 끝단 메움: 출구 쪽 검은 빈 공간을 줄이기 위해 `Wall 2 Variant.prefab`를 가로 폐쇄 패널로 추가
- 천장: 화물선 상부 패널처럼 보이는 `TB_2.prefab` 반복
- 보조: 이음매/입구 프레임/렌더 조명은 검토용 임시 요소입니다.

## 검토 이미지

- `view_01_player_entry.png`: 플레이어 진입 시점
- `view_02_floor_wall_diagonal.png`: 바닥과 벽 연결 대각 구도
- `view_03_ceiling_and_wall_underlook.png`: 천장과 상부 벽 구도
- `view_04_layout_topdown.png`: 천장을 숨긴 배치/동선 확인용 컷어웨이 상단 구도
- `view_05_floor_stack_detail.png`: `Floor_5_base_Plate.fbx` 위 `Floor Base 1 F.prefab` 반복 상세
