# Accelerando 더듬이 구동 철퇴 후속 운동 분석

## 분석 대상

- 영상: `C:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-07-14 21-27-19.mp4`
- Unity 대상: `Approved Accelerando Enemy Placement/Accelerando_03_Antenna_Strike`

## 영상에서 확인한 원인

- 기존 공격 모드는 더듬이의 전방 위치가 유지되는 동안 모든 동적 고리와 철퇴에 별도 전방 가속을 계속 적용했다.
- 철퇴용 배율이 고리보다 크게 설정돼, 철퇴가 사슬 장력에 끌려오기보다 스스로 전방으로 추진되는 것처럼 보였다.
- 기존 전용 검증도 역사상 가장 큰 전방 도달 거리만 강제해 이 직접 추진을 억제하지 못했다.

## 적용한 수정

- 공격 모드의 고리·철퇴 직접 spring, damper, inertia, 전방 도달 가속을 모두 0으로 설정했다.
- 더듬이가 첫 키네마틱 고리를 움직이고, 이후 `ConfigurableJoint`와 고리 관성, 철퇴 질량을 통해서만 힘이 전달되게 했다.
- 12개 고리의 선형 연결 잠금과 구간 길이 투영은 유지해 반동 중에도 시각적 단절이 생기지 않게 했다.
- 공격용 선형/각 감쇠만 질량감 있게 조정했으며 더듬이 공격 커브, 모델, 메시 웨이트, 머티리얼은 변경하지 않았다.
- 전용 검증에 다음 조건을 추가했다.
  - 공격 모드 직접 follower 힘 0
  - 철퇴 반응이 더듬이 릴리스 시작 뒤 발생
  - 더듬이가 거의 정지한 구간에도 철퇴 관성이 남음
  - 철퇴 전방 이동이 더듬이 입력을 증폭
  - 12개 고리 연결 길이 유지

## 최종 반복 검증

- 두 차례 연속 전용 검증 모두 통과했다.
- 직접 힘 전달: `AntennaKinematicAnchorToConnectedJointsOnly`
- 직접 follower spring/damper/inertia: `0`
- 철퇴 릴리스 반응 지연:
  - 반복 A: Left `0.091s`, Right `0.569s`
  - 반복 B: Left `0.224s`, Right `0.780s`
- 더듬이가 거의 정지한 동안 남은 철퇴 속도:
  - 반복 A: Left `28.346`, Right `45.398`
  - 반복 B: Left `28.685`, Right `45.056`
- 철퇴 전방 이동 범위:
  - 반복 A: Left `2.257`, Right `2.597`
  - 반복 B: Left `2.134`, Right `2.240`
- 최대 사슬 구간 늘어남: 양쪽 반복 모두 `0.000004`
- 철퇴 시각 메시와 물리 프록시 최대 분리: 양쪽 반복 모두 `0`
- 좌우 더듬이 대칭, 양쪽 메시 붕괴/과신장, 사슬·철퇴 Transform 커브 없음 검증도 통과했다.

## 실행하지 않은 검증

- `Run-HarnessValidation.ps1`
- `Run-EditModeTests.ps1`
- `Run-PlayModeTests.ps1`
- `Build-WindowsDev.ps1`
- Ensure/Smoke
- Unity 에디터 재시작

