# 아첼레란도 정면 철퇴 타격 균형 및 오른쪽 더듬이 변형 검증

## 대상

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 슬롯: `Approved Accelerando Enemy Placement/Accelerando_03_Antenna_Strike`
- 클립: `Assets/_Project/Art/Enemies/Accelerando/Animations/Accelerando_Antenna_Strike_Attack.anim`
- 공격 슬롯 전용 보정 메시: `Assets/_Project/Art/Enemies/Accelerando/Models/accelerando_forward_strike_deformation_fixed_body.asset`

## 수정 의도

- 전방 벡터를 증가시키는 리그 회전을 빠른 타격 구간에 배치했다.
- 준비 동작은 전방 타격 반대쪽의 55% 각도로 축소했다.
- 0.32초 준비 자세에서 0.44초 전방 타격 자세로 짧게 전환한다.
- 2.40초 루프 중 전방 자세를 유지한 뒤 1.20초 동안 느슨하게 원위치로 복귀한다.
- 사슬·철퇴에는 애니메이션 커브를 추가하지 않고 기존 Rigidbody/ConfigurableJoint 물리를 유지했다.
- 오른쪽 더듬이 정점 887개의 스킨 웨이트를 승인 모델 왼쪽 더듬이의 인접 4개 정점으로부터 거리 가중 보간했다.
- 중립 정점, 삼각형, 머티리얼, 텍스처는 변경하지 않았다.

## 전용 검증 결과

- 직접 애니메이션 바인딩: `Bone_008/007/006/011/010/009`만 존재한다.
- 사슬 및 철퇴 Transform 커브: 0개다.
- 왼쪽 전방/복귀 더듬이 속도: `12.695 / 0.431`, 비율 `29.439`다.
- 오른쪽 전방/복귀 더듬이 속도: `12.564 / 0.439`, 비율 `28.589`다.
- 왼쪽 철퇴 전방 최대 이동: `0.774`, 복귀 역방향 초과 이동: `0`이다.
- 오른쪽 철퇴 전방 최대 이동: `0.990`, 복귀 역방향 초과 이동: `0`이다.
- 철퇴 시각 메시와 물리 프록시 최대 분리 거리: 좌우 모두 `0`이다.
- 보정 메시 중립 최대 정점 차이: `0`이다.
- 스킨 웨이트 합 최대 오차: `0.00000012`다.
- 오른쪽 더듬이 1,224개 삼각형 표본에서 과신장 삼각형은 0개다.
- 전용 물리 및 변형 검증 결과: `PASS`다.

상세 측정값은 `physics_validation.txt`에 기록했다.

## 캡처

- `00_Neutral`: 중립
- `01_Windup`: 축소된 준비 동작
- `02_Release`: 빠른 전방 릴리스
- `03_ForwardDrive`: 철퇴가 정면으로 넘어가는 구간
- `04_ForwardHold`: 전방 자세 유지
- `05_LooseRecovery`: 느슨한 복귀
- `06_Return`: 중립 복귀

각 상태는 `Front`와 `Oblique` PNG로 저장했다. 캡처에서 오른쪽 더듬이가 리그 곡선을 따라 연속적으로 변형되고, 사슬과 철퇴가 물리적으로 연결된 상태로 정면 타격 후 느슨하게 복귀하는 것을 확인했다.

## 실행하지 않은 검증

- `Run-HarnessValidation.ps1`
- `Run-EditModeTests.ps1`
- `Run-PlayModeTests.ps1`
- `Build-WindowsDev.ps1`
- Ensure/Smoke 계열 명령
- Unity 에디터 재시작
- Git 커밋/푸시
