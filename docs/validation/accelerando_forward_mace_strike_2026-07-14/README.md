# Accelerando 정면 철퇴 타격 모션 검증

## 적용 대상

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 슬롯: `Approved Accelerando Enemy Placement/Accelerando_03_Antenna_Strike`
- 클립: `Assets/_Project/Art/Enemies/Accelerando/Animations/Accelerando_Antenna_Strike_Attack.anim`

## 구현 방식

- 현재 아첼레란도가 바라보는 방향을 정면 기준으로 사용했다.
- 좌우 더듬이를 먼저 정면으로 당겨 철퇴의 준비 거리를 확보했다.
- 타격 구간에서 더듬이를 짧게 반대 방향으로 스냅해 사슬의 관성과 철퇴 질량이 정면으로 넘어가게 했다.
- 공격 클립 길이와 Animator 속도는 유지했다.
- 직접 애니메이션한 본은 다음 6개뿐이다.
  - 오른쪽: `Bone_008`, `Bone_007`, `Bone_006`
  - 왼쪽: `Bone_011`, `Bone_010`, `Bone_009`
- 사슬, 철퇴, 숨김 물리 앵커에는 AnimationClip Transform 커브를 추가하지 않았다.
- 기존 `AntennaPhysicsAnchor → Rigidbody/ConfigurableJoint 사슬 → MaceHead` 연결을 유지했다.

## 물리 궤적 검증

- 왼쪽:
  - 더듬이 전후 범위: `0.756`
  - 철퇴 전후 범위: `1.313`
  - 철퇴 정면 최대 이동량: `0.577`
  - 증폭률: `1.737`
  - 철퇴와 물리 프록시 최대 분리 거리: `0`
- 오른쪽:
  - 더듬이 전후 범위: `0.790`
  - 철퇴 전후 범위: `2.217`
  - 철퇴 정면 최대 이동량: `0.978`
  - 증폭률: `2.807`
  - 철퇴와 물리 프록시 최대 분리 거리: `0`
- 결과: `PASS`
- 원본 수치 기록: `physics_validation.txt`

## 단계별 캡처

- `00_Neutral`: 중립
- `01_Windup`: 정면 준비
- `02_Release`: 더듬이 스냅
- `03_ForwardSwing`: 철퇴 정면 스윙
- `04_Recovery`: 회수
- `05_Return`: 중립 복귀

각 단계는 `_Front.png`와 `_Oblique.png` 파일로 저장했다. 정면 캡처 카메라는 현재 개체가 바라보는 방향 앞쪽에 배치했다.

Harness/EditMode/PlayMode/Build/Ensure/Smoke 검증은 실행하지 않았다.
