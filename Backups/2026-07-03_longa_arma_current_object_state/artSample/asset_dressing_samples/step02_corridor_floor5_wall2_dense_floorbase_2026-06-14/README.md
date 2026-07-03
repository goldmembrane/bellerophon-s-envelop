# 2단계 복도 사용자 지정 에셋 조합 시안

이 시안은 사용자가 지정한 바닥/벽 조합을 실제 `CargoRunMvp` 씬에 적용하기 전 확인하기 위한 `artSample` 렌더입니다.
런타임 씬, 프리팹, 프로젝트 설정, 원본 Asset Store 파일은 수정하지 않았습니다.

## 구성

- 바닥 하부: `Floor_5_base_Plate.fbx` 반복
- 바닥 상부: `Floor Base 1 F.prefab`를 2열로 촘촘하게 반복
- 벽: `Wall 2.FBX`와 `Wall 2 Half.FBX`를 좌우에 교대로 연결
- 천장: 화물선 상부 패널처럼 보이는 `TB_2.prefab` 반복
- 보조: 이음매/입구 프레임/렌더 조명은 검토용 임시 요소입니다.

## 검토 이미지

- `view_01_player_entry.png`: 플레이어 진입 시점
- `view_02_floor_wall_diagonal.png`: 바닥과 벽 연결 대각 구도
- `view_03_ceiling_and_wall_underlook.png`: 천장과 상부 벽 구도
- `view_04_layout_topdown.png`: 천장을 숨긴 배치/동선 확인용 컷어웨이 상단 구도
- `view_05_floor_stack_detail.png`: `Floor_5_base_Plate.fbx` 위 `Floor Base 1 F.prefab` 반복 상세
