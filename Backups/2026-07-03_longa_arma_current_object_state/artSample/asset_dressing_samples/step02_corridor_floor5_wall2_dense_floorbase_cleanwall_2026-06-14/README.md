# 2단계 복도 사용자 지정 에셋 조합 시안 - 벽 무겹침 보강판

이 시안은 사용자가 지정한 바닥/벽 조합을 실제 `CargoRunMvp` 씬에 적용하기 전 확인하기 위한 `artSample` 렌더입니다.
이 수정판은 벽 패널이 서로 겹쳐 보이던 문제를 없애기 위해 모듈당 벽 패널 한 세트만 배치하고, 패널 사이 빈 구간에는 기둥형 틈새 에셋을 끼워 넣었습니다.
벽의 구멍 뒤쪽에는 얇은 숨은 뒤판만 두어 외부 배경이 관통해 보이지 않게 했고, 눈에 보이는 벽 표면은 겹쳐 쌓지 않았습니다.
런타임 씬, 프리팹, 프로젝트 설정, 원본 Asset Store 파일은 수정하지 않았습니다.

## 구성

- 바닥 하부: `Floor_5_base_Plate.fbx` 반복
- 바닥 상부: `Floor Base 1 F.prefab`를 2열로 촘촘하게 반복
- 벽: `Wall 2.FBX`와 `Wall 2 Half.FBX`를 같은 구간에 겹치지 않도록 모듈 단위로 연결
- 벽 틈새: `Wall Pillar.prefab`, `Wall Pillar 3.prefab`를 각 벽 모듈 사이에 기둥형 이음매로 배치
- 구멍 방지: 벽 뒤쪽에 얇은 숨은 뒤판을 배치하되, 표면 벽 패널과 같은 면에 겹쳐 놓지 않음
- 끝단 메움: 출구 쪽 검은 빈 공간을 줄이기 위해 `Wall 2 Variant.prefab`를 간격을 둔 폐쇄 패널로 배치하고 사이를 기둥으로 메움
- 천장: 화물선 상부 패널처럼 보이는 `TB_2.prefab` 반복
- 보조: 이음매/입구 프레임/렌더 조명은 검토용 임시 요소입니다.

## 검토 이미지

- `view_01_player_entry.png`: 플레이어 진입 시점
- `view_02_floor_wall_diagonal.png`: 바닥과 벽 연결 대각 구도
- `view_03_ceiling_and_wall_underlook.png`: 천장과 상부 벽 구도
- `view_04_layout_topdown.png`: 천장을 숨긴 배치/동선 확인용 컷어웨이 상단 구도
- `view_05_floor_stack_detail.png`: `Floor_5_base_Plate.fbx` 위 `Floor Base 1 F.prefab` 반복 상세
