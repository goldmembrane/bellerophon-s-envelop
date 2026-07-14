# Accelerando 공격 더듬이 양쪽 스킨 보정 검증

## 적용 범위

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 대상: `Approved Accelerando Enemy Placement/Accelerando_03_Antenna_Strike`
- 변경 부위: `Accelerando_RiggedAttack_Body`의 공격 전용 보정 메시
- 공격 클립 각도: 기존 `40/28/14`도 유지
- 직접 애니메이션 본: `Bone_008/007/006`, `Bone_011/010/009` 유지
- 사슬·철퇴 Transform 커브: 0개 유지

## 수정 내용

- 승인 원본 스킨 웨이트를 기준으로 공격 전용 메시를 다시 생성했다.
- 리그 Left 고정 평행 체인의 모든 영향 정점을 좌우 좌표와 무관하게 찾아 다음처럼 병합했다.
  - `Bone_017 → Bone_011`
  - `Bone_016 → Bone_010`
  - `Bone_015 → Bone_009`
- 리그 Right는 기존 승인 결과와 같은 방식으로 평행 체인 웨이트를 공격 체인에 합친 좌우 반사 보정을 유지했다.
- 몸체 접합부의 공통 본 웨이트는 유지했다.
- 변형 검증과 근접 캡처를 좌우 양쪽으로 확장했다.

## 적용 결과

- Left 보정 정점: 1,578개
- Left 참조 정점: 1,612개
- Right 보정 정점: 1,141개
- 중립 정점 위치 변경: 0
- 최대 웨이트 합 오차: `0.00000012`
- 정점 수: 7,216개 유지
- 원본 형상, 삼각형, UV, bind pose, 머티리얼과 텍스처는 변경하지 않았다.

## 양쪽 변형 검증

| 항목 | Right | Left |
|---|---:|---:|
| 가중 정점 | 1,141 | 1,631 |
| 측정 삼각형 | 1,560 | 2,216 |
| 고정 평행 체인 잔여 영향 정점 | 0 | 0 |
| 릴리스에서 거의 고정된 정점 | 0 | 0 |
| 붕괴 삼각형 | 0 | 0 |
| 과신장 삼각형 | 0 | 0 |
| 최소 면적비 | 0.076420 | 0.431048 |
| 최대 면적비 | 3.025966 | 2.077638 |

결과: `PASS`

## 공격 및 물리 검증

- 공격 더듬이 전방 속도: Left `18.871`, Right `18.423`
- 반복 검증의 철퇴 전방 이동 범위: Left `1.548`, Right `2.045`
- 반복 검증의 철퇴 전방 최대 오프셋: Left `0.908`, Right `1.362`
- 물리 프록시 최대 분리: Left `0`, Right `0`
- 철퇴 물리 측정치는 시뮬레이션 초기 상태에 따라 실행 간 일부 차이가 있었지만, 공격 본 속도·방향과 프록시 결합은 유지됐고 두 실행 모두 전용 검증을 통과했다.

## 육안 확인

- 준비, 릴리스, 전방 구동, 전방 유지의 좌우 근접 정면·사선 캡처를 확인했다.
- 기존 영상에서 화면 왼쪽 더듬이가 길고 얇은 띠처럼 늘어나던 자세에 대응하는 릴리스·전방 구동 구간에서 더듬이 폭과 곡률이 연속적으로 유지됐다.
- 반대쪽 더듬이에서도 새 고정점, 메시 찢어짐 또는 과신장을 확인하지 못했다.
- 전체 공격 캡처에서 사슬과 철퇴가 더듬이 움직임을 계속 물리적으로 추종했다.

## 실행 명령

- `Refresh-UnityProject.ps1`
- `InspectApprovedAccelerandoAttackAntennaSkinConstraints`
- `ApplyApprovedAccelerandoForwardMaceStrikeMotion`
- `ValidateApprovedAccelerandoForwardMaceStrikeMotion`
- `CaptureApprovedAccelerandoForwardMaceStrikeMotion`
- 재현성 확인을 위한 `ValidateApprovedAccelerandoForwardMaceStrikeMotion` 반복 실행

## 실행하지 않은 항목

- Harness/EditMode/PlayMode/Build/Ensure/Smoke
- Unity 에디터 재시작
- 다른 아첼레란도 슬롯 수정
- 사슬·철퇴 물리 설정 또는 런타임 물리 코드 수정
- Git 커밋·푸시

