# Ata 04 권총 사격 연속 모션 결과

## 3초 사격 유지 및 원본 발사 주기

- 원본 기획서 `docs/GAME_DESIGN_SOURCE.txt:247`의 아타 권총 연사 딜레이 `1.5초`를 적용했다.
- 사격 원본 클립의 한 주기를 `1.5초`로 맞추고 두 주기를 반복해 `PistolShooting` 상태를 총 `3초`로 구성했다.
- `PistolAimAndFire → PistolShooting`의 `0.5초` 중간 동작은 유지했다. 중간 동작에는 사격 원본의 마지막 자세 구간을 사용하고, 전환 완료 후 사격 상태의 정규화 시간 `1 → 3`을 재생하므로 사격 유지시간은 별도로 정확히 `3초`다.
- `PistolShooting → PistolAimAndFire` 복귀 전환은 기존 `0.05초`를 유지했다.
- 실제 Play Mode 첫 사격 상태는 전환 완료 약 `2.50초`부터 종료 약 `5.50초`, 두 번째 사격 상태는 약 `8.00초`부터 약 `11.00초`까지 유지됐다.
- 총구 섬광은 사격 상태당 2회, 두 전체 반복에서 총 4회 발생했다.
- 같은 사격 상태 안 섬광 간격은 첫 반복 `1.490142초`, 두 번째 반복 `1.542185초`로 20fps 캡처 오차 범위에서 기획값 `1.5초`와 일치한다.
- 직접 확인 결과, 양손 사격 자세와 반동이 두 번 반복된 뒤에만 권총이 오른쪽 허리로 복귀한다.
- 두 전체 반복의 사격 유지 구간에서 권총-오른손 앵커 최대 위치·회전 오차는 모두 `0`이다.
- 전체 흐름 접촉 시트: `actual_playmode_motion/Ata_04_Shooting3s_ContactSheet.png`
- 네 발 총구 이펙트 확대: `actual_playmode_motion/Ata_04_Shooting3s_MuzzleEvents.png`
- Unity 적용 로그: `ApplyShootingDuration3sInterval1_5s.log`
- 최종 실제 Play Mode 로그: `CaptureShootingDuration3sInterval1_5s_Final.log`

## 기존 모션 → 사격 모션 0.5초 중간 동작

- 사용자 지시에 따라 `PistolAimAndFire → PistolShooting` 고정 시간 전환을 기존 `0.2초`에서 `0.5초`로 변경했다.
- 별도의 세 번째 클립을 만들지 않고 두 원본 자세를 Animator가 0.5초 동안 블렌드하는 중간 동작으로 구성했다.
- `PistolShooting → PistolAimAndFire` 복귀 전환은 기존 `0.05초`를 유지했다.
- 실제 Play Mode 두 번째 주기에서 조준→사격 전환 시작은 약 `5.168초`, 종료는 약 `5.668초`로 계산되어 실제 전환 길이가 약 `0.500초`임을 확인했다.
- 최종 캡처에서 조준→사격 전환 진행률 `0.059039`, `0.209397`, `0.356366`, `0.504853`, `0.649247`, `0.793921`, `0.938670`의 중간 자세가 연속적으로 기록됐다.
- 직접 확인 결과 오른팔과 상체가 기존 마지막 자세에서 양손 사격 자세로 단계적으로 이어지며 순간적인 자세 점프가 없다.
- 전환 중 권총-오른손 앵커의 위치 오차와 회전 오차는 모두 `0`이다.
- 0.5초 블렌드에서는 사격 클립의 발사 시점이 전환 도중 도달하므로 총구 섬광 드라이버가 다음 사격 상태의 시간도 읽도록 보정했다. 섬광의 정규화 시간 범위는 변경하지 않았다.
- 두 주기 전체의 손 유지 구간 최대 위치·회전 오차는 모두 `0`이며, 총구 섬광은 주기당 1회씩 총 2회 유지됐다.
- 최종 캡처는 `720×720`, `20fps`, `92`프레임이다.
- 전환 접촉 시트: `actual_playmode_motion/Ata_04_AimToShootingTransition050_ContactSheet.png`
- 상체 확대: `actual_playmode_motion/Ata_04_AimToShootingTransition050_UpperBody.png`
- Unity 적용 로그: `ApplyAimToShootingTransition050.log`
- 첫 캡처에서 다음 상태의 섬광 시간을 읽지 않아 섬광이 0회였으며, 원인 보정 후 최종 캡처에서 2회로 확인했다.
- 최종 실제 Play Mode 로그: `CaptureAimToShootingTransition050_Final.log`

## 적용 결과

- 대상: `CargoRunMvp/Approved Ata Enemy Placement/Ata_04_PistolAimAndFire`
- 순서: `PistolAimAndFire` 2.000000초 → 0.500000초 전환 → `PistolShooting` 3.000000초 → 처음
- 사격 원본: `enemies model/attas shooting.fbx`
- Unity 소스 사본: `Assets/_Project/Art/Enemies/Ata/Animations/Sources/Ata_Shooting.fbx`
- 조준 클립은 개별 반복을 끄고, 사격 원본은 1.5초 주기로 두 번 반복한 뒤 상태를 종료한다.
- 권총은 첫 모션 말미에 오른손에 유지되고 사격 모션 동안 오른팔을 그대로 추종한다.
- 사격 모션 종료 시 권총을 오른쪽 허리 앵커로 반환한 뒤 첫 상태로 전환한다.

## 총구 이펙트

- 새 아트 샘플이나 새 VFX 디자인은 만들지 않았다.
- 기존 `Rebellion_Forward_Burst_Flash` 메시와 머티리얼을 재사용했다.
- 사격 클립의 가장 큰 반동 시점인 정규화 시간 `0.285714`부터 `0.354286`까지만 표시한다.
- 실제 Play Mode 2회 연속 재생에서 총구 섬광 이벤트는 총 4회이며 각 3초 사격 상태마다 2회 발생했다.

## 실제 Play Mode 직접 확인

- 실제 `Animator`를 연속 재생하여 2개 전체 순서를 캡처했다.
- 상태 기록: `PistolAimAndFire → PistolShooting → PistolAimAndFire → PistolShooting → PistolAimAndFire`.
- 캡처 프레임: 92프레임, 20fps.
- 권총 손 추종: 실제 손 유지 구간의 피벗 거리 `0`, 회전 오차 `0도`.
- 원 캡처 로그의 최대 거리 `0.474415`, 각도 `26.40564`는 정규화 시간이 `1.031713`인 최종 허리 복귀 프레임을 반복 시간 `0.031713`으로 잘못 분류한 캡처 지표 오류였다. CSV 원자료와 영상을 대조했으며 실제 손 유지 구간의 이탈이 아니다. 캡처 코드의 지표 계산은 비반복 상태에 맞게 `Clamp01`로 수정했다.
- 직접 확인 결과, 기존 모션이 끝나기 전에 권총이 먼저 허리로 돌아가지 않으며 사격 모션이 끝날 때 허리로 복귀한다.
- 직접 확인 결과, 총구 섬광은 권총과 함께 오른팔 움직임을 추종하며 정면 총구 위치에서 발생한다.

## 산출물

- 실제 연속 영상: `actual_playmode_motion/Ata_04_PistolShootingSequence_TwoLoops.mp4`
- 프레임 지표: `actual_playmode_motion/Ata_04_PistolShootingSequence_Frames.csv`
- 전체 구간 접촉 시트: `actual_playmode_motion/Ata_04_PistolShootingSequence_ContactSheet.png`
- 총구 섬광 확대: `actual_playmode_motion/Ata_04_PistolShootingSequence_MuzzleFlash_Zoom.png`
- Unity 컴파일 확인: `RefreshAssets_Final.log`, `RefreshAssets_PostMetricFix.log`
- 최종 캡처 로그: `CaptureActualPlayModeTwoSequences_Final.log`

## 실행하지 않은 항목

- 하네스 검증 및 `Run-HarnessValidation.ps1`
- `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`
- `Build-WindowsDev.ps1` 및 기타 빌드
- 다른 Ata 슬롯, 플레이어, 카메라, 게임플레이 수정
- Unity 재시작, 외부 설치, Git 커밋·푸시
- 신규 아트 샘플 및 신규 VFX 디자인
- 별도의 세 번째 애니메이션 클립 제작

## 권총 총신 및 총구 이펙트 정면 교정

> 아래 기록은 중간 교정 이력입니다. 최종 상태는 다음 절의 `2026-08-12 최종 총구 방향 및 원본 반동 복구`가 우선합니다.

- 기존 구현은 권총 로컬 `-Y`를 총신으로, 최소 Y 끝점을 총구로 사용했다. 메시 형상을 직접 확인한 결과 이는 손잡이 방향과 손잡이 끝이었다.
- 실제 권총 총신은 로컬 `+Z`이며 총구는 최대 Z 끝이다. 권총 정렬과 이펙트 위치·회전 기준을 모두 실제 총신축으로 변경했다.
- 사격 자세 정규화 시간 `0.32`에서 실제 총신 `+Z`를 몸통→오른손 조준축에 정렬했다.
- 총구 이펙트의 로컬 `+Z`도 권총 총신 `+Z`를 그대로 따르도록 구성했다.
- 실제 Play Mode 2회 반복, 157프레임을 직접 확인했다. 총구 섬광 이벤트는 4회이며 발광 6프레임에서 이펙트축-총신축 최대 오차는 `0도`다.
- 발광 중 총신-몸통→손 조준축 최대 오차는 `6.448288도`, 권총-오른손 앵커 최대 위치·회전 오차는 모두 `0`이다.
- 직접 확인 결과 권총 총신은 아타의 조준 정면으로 뻗고 이펙트는 손잡이 끝이 아닌 실제 총구 끝에서 같은 방향으로 발생한다.
- 실제 영상: `actual_playmode_motion/Ata_04_PistolShootingSequence_TwoLoops.mp4`
- 프레임 지표: `actual_playmode_motion/Ata_04_PistolShootingSequence_Frames.csv`
- 발광 프레임 확인: `actual_playmode_motion/direction_review/Ata_Pistol_ActualBarrel_FlashFrames.png`
- 하네스 검증, EditMode/PlayMode 테스트 스크립트, Windows 빌드는 실행하지 않았다.

## 2026-08-12 최종 총구 방향 및 원본 반동 복구

> 아래 기록의 `-Y` 총구축 판정과 완료 판정은 사용자 영상 `2026-08-12 15-50-14.mp4` 확인 후 철회했다. 최종 상태는 다음 절의 `아타 시선 기준 최종 재교정`이 우선한다.

- 사용자 영상 `2026-08-12 15-11-50.mp4`를 직접 확인해 권총의 긴 형상이 수직 아래로 꺾이고 사격 반동이 권총 회전에 전달되지 않는 문제를 재현했다.
- 메시와 실제 화면을 다시 대조한 결과, 직전 중간 기록의 로컬 `+Z` 총신 판정은 화면에 보이는 권총의 긴 축과 일치하지 않았다. 최종 구현은 보이는 긴 축인 로컬 `-Y` 끝을 총구로 사용한다.
- 사격 원본의 정규화 시간 `0.32`에서 권총 로컬 `-Y`를 활성 `Model Cam`의 화면 오른쪽 정면 방향에 맞췄다. 총구 섬광의 축과 위치도 같은 로컬 `-Y` 총구 끝에 맞췄다.
- 기존 오른손 앵커만 따라가던 구성에서는 실제 사격 상태 권총 회전 변화가 `0도`였다. 오른손 접촉 위치는 유지하고 사격 중 회전만 원본 `RightArm/RightForeArm` 애니메이션에서 상속받는 `Ata_Pistol_ShootingRecoilRotationAnchor`를 추가했다.
- 실제 Play Mode 두 반복 영상은 `720×720`, `20fps`, `142`프레임으로 생성했다. 총구 섬광은 총 4회 발생했고 이펙트축-총구축 오차는 `0도`였다.
- 발광 프레임에서 총구와 화면 정면 방향의 최대 오차는 `3.376784도`, 사격 반동 구간의 프레임 간 권총 최대 회전 변화는 `11.58177도`였다.
- 손 접촉 위치와 사격 회전 앵커에 대한 최대 피벗 위치·회전 오차는 모두 `0`이었다.
- 실제 영상과 발사 전후 확대 프레임을 직접 확인해 권총이 아래로 수직 낙하하지 않고 화면 오른쪽 정면을 향하며, 발사 순간 원본 팔 반동을 따라 각도가 변하는 것을 확인했다.
- 최종 실제 영상: `actual_playmode_motion/Ata_04_PistolShootingSequence_TwoLoops.mp4`
- 전체 접촉 시트: `actual_playmode_motion/Ata_04_PistolShootingSequence_ContactSheet.png`
- 반동 확대: `actual_playmode_motion/Ata_04_PistolShootingSequence_RecoilCloseup.png`
- 프레임 지표: `actual_playmode_motion/Ata_04_PistolShootingSequence_Frames.csv`
- 적용 로그: `Logs/AtaPistolForwardRecoilApply.log`
- 최종 실제 캡처 로그: `Logs/AtaPistolForwardRecoilActualCapture2.log`

### 실행하지 않은 항목

- 하네스 검증 및 `Run-HarnessValidation.ps1`
- `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`
- `Build-WindowsDev.ps1` 및 기타 빌드
- 다른 Ata 슬롯, 플레이어, 게임플레이 수정
- Unity 재시작, 신규 아트 샘플, 외부 설치, Git 커밋·푸시

## 2026-08-12 권총 롤 및 오른손 끝 접촉 최종 교정

- 입력 영상 `2026-08-12 16-12-39.mp4`에서 총구는 시선을 향하지만 권총의 롤이 약 90도 돌아 옆으로 누운 문제를 직접 확인했다.
- 단일 축 `FromToRotation`을 제거하고 `Quaternion.LookRotation(Head.right, ProjectOnPlane(Model.up, Head.right))`으로 총구축과 슬라이드 위쪽 축을 함께 고정했다.
- Unity 적용 로그: `ApplyAtaPistolUprightRollFix.log`
- 적용 결과: `AfterAngle=0`, `UprightAngleAfter=0`, `SceneSaved=True`
- 실제 Play Mode 두 반복: `153`프레임, 총구 섬광 `4회`
- 권총–오른손 접촉 위치·회전 최대 오차: `0`, `0도`
- 발광 중 총구–아타 시선 최대 각도: `4.744375도`
- 이펙트축–총구축 최대 각도: `0도`
- 사격 프레임 간 최대 권총 회전 변화: `11.11125도`
- 직접 확인 결과 슬라이드는 위, 손잡이는 아래로 서며, 방아쇠 뒤쪽이 오른손 끝과 겹친 상태로 반동을 따라 움직인다.
- 실제 영상: `actual_playmode_motion/Ata_04_PistolShootingSequence_TwoLoops.mp4`
- 손 접촉·반동 확대: `actual_playmode_motion/upright_front_review/Ata_04_UprightPistol_RecoilAndHandContact.png`
- 발광 프레임: `actual_playmode_motion/upright_front_review/Ata_04_UprightPistol_FlashFrames.png`
- 실제 캡처 로그: `CaptureActualPlayModeTwoSequences_UprightFrontReview.log`

### 실행하지 않은 항목

- 하네스 검증 및 `Run-HarnessValidation.ps1`
- `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`
- `Build-WindowsDev.ps1` 및 기타 빌드
- 다른 Ata 슬롯, 플레이어, 게임플레이 수정
- Unity 재시작, 신규 아트 샘플, 외부 설치, Git 커밋·푸시

## 아타 시선 기준 최종 재교정

- 정면 기준을 카메라가 아닌 사격 애니메이션 중 아타의 얼굴·눈이 향하는 방향으로 고정했다.
- 아타 리그의 해부학적 시선축은 `Armature/Hips/Spine02/Spine01/Spine/neck/Head`의 로컬 `right` 축이다.
- 사용자 영상에서 세로로 서 있던 형상은 권총 손잡이축 로컬 `-Y`를 총신으로 잘못 정렬한 결과였다.
- 권총의 실제 총신축인 로컬 `+Z`를 `Head.right`에 정렬하고, 총구 위치도 메시 최대 Z 끝으로 변경했다. 총구 섬광의 로컬 회전은 총신 `+Z`와 동일하게 설정했다.
- 실제 Play Mode 두 반복을 아타의 시선축이 화면 수평으로 보이는 측면에서 캡처하고, 전체 영상·사격 확대·발광 6프레임을 직접 확인했다.
- 육안 확인 결과 권총 본체가 얼굴 시선과 같은 방향으로 수평으로 뻗으며, 이전 사용자 영상처럼 손 앞에서 세로로 서 있지 않는다.
- 발광 프레임에서 섬광은 권총 총구 끝에 붙어 얼굴 시선과 같은 방향으로 발생한다.
- 사격 반동은 유지되며 프레임 간 최대 권총 회전 변화는 `11.04623도`다. 손 접촉 위치 및 회전 앵커 오차는 모두 `0`이다.
- 최종 영상: `actual_playmode_motion/Ata_04_PistolShootingSequence_TwoLoops.mp4`
- 시선 측면 전체 접촉 시트: `actual_playmode_motion/Ata_04_HeadGaze_SideFront_ContactSheet.png`
- 시선 측면 사격 확대: `actual_playmode_motion/Ata_04_HeadGaze_SideFront_ShootingCloseup.png`
- 최종 발광 프레임: `actual_playmode_motion/Ata_04_HeadGaze_FinalFlashFrames.png`
- 사용자 영상 분석: `user_capture_155014_analysis/user_capture_contact_sheet.png`, `user_capture_155014_analysis/hands_sequence_large.png`

### 실행하지 않은 항목

- 하네스 검증 및 `Run-HarnessValidation.ps1`
- `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`
- `Build-WindowsDev.ps1` 및 기타 빌드
- 다른 Ata 슬롯, 플레이어, 게임플레이 수정
- Unity 재시작, 신규 아트 샘플, 외부 설치, Git 커밋·푸시
