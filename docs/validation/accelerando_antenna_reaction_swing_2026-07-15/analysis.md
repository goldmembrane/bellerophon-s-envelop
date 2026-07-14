# Accelerando 더듬이 반동 기반 철퇴 흔들림 수정

## 대상

- Unity 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 대상 루트: `Approved Accelerando Enemy Placement/Accelerando_03_Antenna_Strike`
- 공격 클립: `Assets/_Project/Art/Enemies/Accelerando/Animations/Accelerando_Antenna_Strike_Attack.anim`
- 상태: `AntennaStrikeAttack`

## 원인

- 이전 더듬이 커브는 `0.44초`에 전방 자세로 전환한 뒤 `1.20초`까지 거의 같은 방향을 유지했다.
- 철퇴는 조인트 물리로 움직였지만, 더듬이가 전진한 직후 역방향으로 흔들리는 입력이 없어서 철퇴 움직임이 더듬이 반동에 의해 발생한 것으로 보이지 않았다.

## 수정

- 기존 최대 공격 각도 배율 `1.10`과 준비 자세 범위는 유지했다.
- 공격 입력을 다음 단계로 분리했다.
  - `0.32초`: 준비
  - `0.44초`: 전방 릴리스
  - `0.50초`: 기존 최대 각도의 전방 정점
  - `0.62초`: 더듬이 급반동
  - `0.78초`: 재전진
  - `0.96초`: 잔진동
  - `1.20초`: 반동 안정화
- 오른쪽 더듬이는 왼쪽 커브를 기존 방식대로 좌우 대칭 변환했다.
- 사슬과 철퇴에는 Transform 애니메이션 커브를 추가하지 않았다.
- 기존 `더듬이 물리 앵커 → 첫 고리 → ConfigurableJoint 12링크 → 철퇴 Rigidbody` 전달 구조와 직접 follower 힘 0 설정을 유지했다.

## 전용 검증

- 검증은 더듬이가 뒤로 반동하는 동안 철퇴가 관성으로 계속 전진하는 반대 방향 표본을 좌우 각각 최소 2개 요구한다.
- 첫 검증:
  - Left: 더듬이 역반동 속도 `4.261`, 반대 방향 표본 `5`, 반동 중 철퇴 전진 속도 `14.341`.
  - Right: 더듬이 역반동 속도 `4.715`, 반대 방향 표본 `4`, 반동 중 철퇴 전진 속도 `12.824`.
- 반복 검증:
  - Left: 더듬이 역반동 속도 `4.261`, 반대 방향 표본 `5`, 반동 중 철퇴 전진 속도 `13.554`.
  - Right: 더듬이 역반동 속도 `4.715`, 반대 방향 표본 `3`, 반동 중 철퇴 전진 속도 `13.155`.
- 두 번 모두:
  - 직접 힘 전달 `AntennaKinematicAnchorToConnectedJointsOnly`.
  - 사슬 최대 구간 늘어남 Left/Right `0.000004`.
  - 철퇴 시각 메시와 물리 프록시 분리 `0`.
  - 좌우 더듬이 대칭 검증 통과.
  - 양쪽 더듬이 메시 붕괴·과신장 `0`.
  - 사슬·철퇴 Transform 애니메이션 커브 `0`.

## 최종 캡처

- 전용 검증 통과 후 `CaptureApprovedAccelerandoForwardMaceStrikeMotion`을 한 번 실행했다.
- 전방 정점, 더듬이 급반동과 철퇴 후속 전진, 재전진, 잔진동, 회복 단계를 정면·사선 및 좌우 근접 이미지로 저장했다.

## 실행하지 않은 항목

- Unity 에디터 재시작
- Harness/EditMode/PlayMode/Build/Ensure/Smoke
- 모델링·텍스처·머티리얼·메시 웨이트 변경
- 다른 아첼레란도 슬롯 변경
- Git 작업

