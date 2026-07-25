# Grave 공격 모션 21:40:18 영상 복구 기록

## 목표

- 사용자 지정 영상 `Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-07-16 21-40-18.mp4`의 Grave 오른팔 낫 공격 모션을 별도 작업 클립으로 복구한다.
- 미승인 상태인 본 공격 클립은 덮어쓰지 않는다.
- Unity 적용 대상은 `Approved Grave Enemy Placement/Grave_03_Attack_RightArm_GiantSweep/Grave_Model` 하나로 한정한다.

## 영상 및 복구 기준 정정

- 영상: H.264, `1920x1000`, `6.935233초`, 233프레임.
- 기존 분석 프레임과 접촉 시트는 `docs/validation/grave_scythe_blade_accelerated_attack_2026-07-16/user_capture_214018_*`를 재사용했다.
- 첫 복구에서 사용한 위팔 `(-0.58,-0.64,0.50)`, 아래팔 `(-0.65,-0.55,0.52)`는 영상 속 값이 아니라 영상 촬영 뒤 `UntwistedTorsoClearSlash` 작업에서 팔을 직선에 가깝게 만든 후속 수정값이었다. 이 첫 복구는 잘못된 기준이므로 폐기했다.
- 영상은 `ApplyContinuousBodyFrontSlash_Final` 적용 직후이자 `UntwistedTorsoClearSlash` 적용 전 상태다.
- 영상 당시의 굽힌 커튼콜 방향을 다시 사용했다.
  - 커튼콜 위팔: `(-0.45,-0.86,0.24)`
  - 커튼콜 아래팔: `(-0.82,0.18,0.54)`
- 시간 기준:
  - 옆 전개 완료: `1.20초`
  - 연속 베기: `1.28~1.58초`
  - 커튼콜 자세 유지: `1.58~2.35초`
  - 중간 복귀: `2.65초`
  - 시작 자세 복귀: `3.00초`
- 2026-07-16 후속 작업에서 `2.65초`와 `3.00초`를 커튼콜 자세로 고정해 삭제됐던 복귀 구간을 작업 클립에만 복원했다.

## 최종 적용 결과

- 작업 클립: `Assets/_Project/Art/Enemies/Grave/Animations/Grave_Attack_CurtainCall_Sweep_Working.anim`
- 작업 컨트롤러: `Assets/_Project/Art/Enemies/Grave/Controllers/Grave_Attack_CurtainCall_Sweep_Working.controller`
- 실행 명령: `ApplyRestoredGraveAttackFromUserVideo214018`
- 최종 실행 로그: `ApplyRestoredGraveAttackFromUserVideo214018_ReferenceMotion.log`
- 브리지 결과: `Duration=3`, `Loop=True`, `LiveAnimatorBound=True`, `PrimaryClipUntouched=True`.
- 작업 컨트롤러 GUID는 씬에서 1회만 참조되며 지정 공격 슬롯의 모델 Animator에 연결됐다.
- 작업 컨트롤러는 작업 클립 GUID를 1회만 참조한다.

## 원본 보존

- 본 클립: `Assets/_Project/Art/Enemies/Grave/Animations/Grave_Attack_CurtainCall_Sweep.anim`
- 작업 전후 SHA-256: `2D2F03B7ACA5728E4931BA8E3B9047B9CC000AB254B2FF57EEC011636EF45925`
- 작업 클립 SHA-256: `B056F97932E4CC017E8C713C4E5FF63AD915A77EC20BA67DD43A63E93BCC15E3`
- 기존 본 컨트롤러와 낫 메시를 재생성하거나 덮어쓰지 않았다.

## 실제 Unity 화면 확인

- Unity Hierarchy 검색으로 `Grave_03_Attack_RightArm_GiantSweep`를 직접 선택했다.
- Play Mode 진입 뒤 `Scene` 탭에서 지정 슬롯에 다시 포커스하고 실제 화면을 8초 녹화했다.
- 실제 녹화: `current_reference_motion_focused_final.mp4`
- 0.1초 간격 접촉 시트: `current_final_00_02.jpg`, `current_final_02_04.jpg`, `current_final_04_06.jpg`, `current_final_06_08.jpg`
- 기준 영상 접촉 시트: `reference_214018_00_02.jpg`, `reference_214018_02_04.jpg`, `reference_214018_04_06.jpg`, `reference_214018_06_0694.jpg`
- 실제 화면에서 `옆 전개 → 빠른 몸통 안쪽 베기 → 굽힌 커튼콜 유지 → 기본 자세 복귀`가 3초 주기로 반복되는 것을 직접 확인했다.
- 판정은 스크립트 수치가 아니라 실제 Play Mode Scene View 녹화 프레임을 기준으로 했다.
- Play Mode는 녹화 뒤 종료했다.
- 사용자가 Unity에서 모션을 직접 확인하기 전까지 본 클립 교체나 승인 완료로 처리하지 않는다.

## 커튼콜 끝 오른팔 소폭 하강

- 사용자 확인에 따라 전체 공격 끝이 아니라 커튼콜 자세의 끝부분만 수정했다.
- 기존 커튼콜 자세는 `2.15초`까지 유지하고, `2.15~2.35초`에 `RightArm`을 모델 기준 아래쪽으로 `5도` 내렸다.
- `RightForeArm`, `RightHand`, 낫은 기존 굽힘과 형태를 유지한 채 자식 체인으로 함께 내려간다.
- 낮아진 `2.35초` 자세에서 기존 `2.65초` 중간 복귀와 `3.00초` 시작 자세로 자연스럽게 이어지게 했다.
- 최종 적용 로그: `ApplyRestoredGraveAttackFromUserVideo214018_CurtainEndingLower5deg.log`
- 실제 Unity 화면 녹화: `current_curtain_ending_lower5deg_focused.mp4`
- 전체 0.1초 간격 시트: `current_curtain_ending_lower5deg_00_02.jpg`, `current_curtain_ending_lower5deg_02_04.jpg`, `current_curtain_ending_lower5deg_04_06.jpg`, `current_curtain_ending_lower5deg_06_08.jpg`
- 커튼콜 확대 0.05초 간격 시트: `current_curtain_ending_lower5deg_detail_cycle1.jpg`, `current_curtain_ending_lower5deg_detail_cycle2.jpg`
- 두 주기에서 기존 자세 유지 후 오른팔 체인만 소폭 하강하고, 급격한 꺾임이나 위쪽 역회전 없이 복귀하는 것을 직접 확인했다.
- 화면 확인 뒤 Play Mode를 종료했고 본 공격 클립은 변경하지 않았다.

## 이후 검증 방식 변경

- Unity 창에서 대상을 직접 선택·포커스하고 수동 재생을 따라가는 방식 대신, 작업 클립을 고정 시점에 샘플링해 실제 렌더 PNG를 만드는 자동 시각 캡처를 사용한다.
- 명령: `CaptureGraveAttackCurtainEndingFrames`
- 출력 폴더: `automated_visual_capture/`
- 시점: `1.58`, `2.15`, `2.25`, `2.35`, `2.65`, `3.00초`
- 접촉 시트: `automated_visual_capture/Grave_Attack_CurtainEnding_ContactSheet.png`
- 시각 결과: `2.15 → 2.25 → 2.35초` 오른팔 소폭 하강과 `2.35 → 2.65 → 3.00초` 복귀 연결이 실제 렌더 픽셀에서 연속적으로 보인다.
- 캡처는 Scene View 포커스, Play Mode, 씬 저장을 사용하지 않으며 종료 시 개체 선택을 남기지 않는다.
