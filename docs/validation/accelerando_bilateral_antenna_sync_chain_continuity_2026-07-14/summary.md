# 아첼레란도 좌우 더듬이 대칭 및 사슬 연속성 검증

## 대상

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 루트: `Approved Accelerando Enemy Placement/Accelerando_03_Antenna_Strike`
- 클립: `Assets/_Project/Art/Enemies/Accelerando/Animations/Accelerando_Antenna_Strike_Attack.anim`
- 기준 더듬이: 리그 `Left` (`Bone_011/010/009`)

## 적용 내용

- 왼쪽 공격 커브는 유지하고 오른쪽 `Bone_008/007/006`을 왼쪽 관절 위치와 끝 방향의 좌우 대칭 자세로 생성했다.
- 오른쪽은 Euler 짐벌 구간을 피하도록 Quaternion 회전 커브를 사용했다.
- 공격 슬롯의 기존 12개 사슬 링크를 모두 활성화하고, 중립 상태에서 아래로 처진 연결 곡선에 등간격 배치했다.
- 중립 사슬 곡선 길이는 양쪽 `1.368073`, 링크 중심 간격은 `0.124370`이다.
- 공격 전용 ConfigurableJoint의 선형 축을 잠그고 각 회전은 자유롭게 유지했다.
- Unity 조인트 솔버 이후 Rigidbody 중심 간 원래 길이를 다시 투영해 방사 방향 분리를 제거하고 접선 방향 흔들림은 유지했다.
- 철퇴와 링크 시각 메시를 최신 Rigidbody 위치·회전에서 직접 동기화했다.
- 더듬이가 정면으로 전진해 있는 구간에만 사슬과 철퇴에 전방 물리 가속을 적용하고 복귀 구간에는 해제했다.

## 수치 검증

- 좌우 더듬이 최대 대칭 위치 오차: `0.035242 / 0.065000`
- 좌우 더듬이 최대 구간 각도 차이: `0 / 6도`
- 왼쪽 사슬 최대 구간 늘어남: `0.000004 / 0.008000`
- 오른쪽 사슬 최대 구간 늘어남: `0.000004 / 0.008000`
- 좌우 철퇴 시각 메시와 물리 프록시 최대 분리: `0 / 0`
- 왼쪽 철퇴 전방 이동 범위/증폭: `2.274 / 3.143배`
- 오른쪽 철퇴 전방 이동 범위/증폭: `2.231 / 3.326배`
- 더듬이 전방/복귀 속도:
  - Left: `18.871 / 0.613`
  - Right: `16.241 / 0.505`
- 양쪽 더듬이 변형 검사: 붕괴 삼각형 `0`, 과신장 삼각형 `0`, 중립 정점 변화 `0`
- 최종 결과: `PASS`

## 시각 검토

- 중립, 준비, 릴리스, 전방 구동, 전방 유지, 느슨한 복귀, 중립 복귀의 정면·사선 캡처를 생성했다.
- 준비·릴리스·전방 구동·전방 유지·느슨한 복귀에서 좌우 더듬이 및 사슬 근접 캡처를 생성했다.
- 중립·전방 구동·느슨한 복귀 캡처에서 더듬이 끝부터 철퇴까지 고리가 이어지고, 이전 영상처럼 멀리 떨어진 단독 링크가 없는 것을 확인했다.
- 주요 파일:
  - `Accelerando_ForwardMaceStrike_03_ForwardDrive_Front.png`
  - `Accelerando_LeftChainContinuity_03_ForwardDrive_Front.png`
  - `Accelerando_RightChainContinuity_03_ForwardDrive_Front.png`
  - `Accelerando_LeftChainContinuity_05_LooseRecovery_Front.png`
  - `Accelerando_RightChainContinuity_05_LooseRecovery_Front.png`

## 실행한 Unity 명령

- `Refresh-UnityProject.ps1`
- `ApplyApprovedAccelerandoForwardMaceStrikeMotion`
- `InspectApprovedAccelerandoAttackAntennaSkinConstraints`
- `ValidateApprovedAccelerandoForwardMaceStrikeMotion`
- `CaptureApprovedAccelerandoForwardMaceStrikeMotion`

## 실행하지 않은 항목

- Unity 에디터 재시작
- Harness 검증
- EditMode/PlayMode 테스트
- Build/Smoke/Ensure
- Git 작업

