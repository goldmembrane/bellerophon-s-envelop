# Tergo 12번 Death MeltPuddle 높이 정렬 검증

- 검증 날짜: 2026-07-07 KST
- 대상: `Approved Tergo Enemy Placement/Tergo_12_Death`
- 사용자 확인 영상:
  - `c:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-07-07 13-09-08.mp4`

## 영상 프레임 확인

- `frames/contact_sheet_height_transition_crop.jpg`
  - 제공 영상의 3.20초부터 4.56초까지 전환 구간을 같은 화면 크롭으로 추출한 접촉시트.
- 확인 내용:
  - MeltPuddle 시작 자세가 기존 쓰러진 자세보다 화면상 높이가 맞지 않아 보였다.
  - 이번 작업에서는 각도와 회전은 유지하고 높낮이만 보정 대상으로 삼았다.

## Unity 샘플 확인

- `unity_samples/contact_sheet_unity_height_before_fixed_camera.jpg`
  - 수정 전 Unity 샘플을 같은 고정 카메라로 렌더링한 접촉시트.
- `unity_samples/overlay_before_blue_melt_green_before_height_fix.png`
  - 수정 전 `01_before_melt`와 `02_melt_start`를 겹친 이미지. 파란색은 기존 쓰러진 자세, 녹색은 MeltPuddle 시작 자세다.
- `unity_samples/contact_sheet_unity_height_after_fixed_camera.jpg`
  - 수정 후 Unity 샘플을 같은 고정 카메라로 렌더링한 접촉시트.
- `unity_samples/overlay_before_blue_melt_green_after_height_fix.png`
  - 수정 후 겹친 이미지. MeltPuddle 시작 자세를 아래로 낮춰 기존 쓰러진 자세와 더 자연스럽게 이어지게 했다.
- `unity_samples/contact_sheet_unity_start_pose_height_after_fixed_camera.jpg`
  - 사용자 추가 설명에 따라 최종 퍼들 보정은 유지하고, 녹아내리기 시작 프레임의 누운 자세 높이만 다시 맞춘 뒤의 고정 카메라 접촉시트.
- `unity_samples/overlay_before_red_melt_cyan_start_pose_height_after.png`
  - 추가 수정 후 `01_before_melt`와 `02_melt_start`를 겹친 이미지. 빨간색은 쓰러지는 애니메이션 끝 자세, 청록색은 녹아내리는 애니메이션 시작 자세다.
- `unity_samples/overlay_before_red_melt_cyan_start_pose_height_after_crop2x.png`
  - 같은 오버레이에서 누운 자세 영역만 확대한 확인 이미지.
- `unity_samples/outline_before_red_melt_cyan_start_pose_height_after_crop2x.png`
  - 채워진 실루엣 대신 외곽선만 겹쳐서 높낮이를 확인한 이미지.

## 수정 기준

- 각도, 회전, 방향 선택, 스케일, BlendShape 형태 전환은 변경하지 않았다.
- `ApprovedDeathMeltHeightVisualYOffset`은 MeltPuddle 샘플의 시각 높이를 맞추기 위한 월드 Y축 보정값이다.
- `ApprovedDeathMeltStartPoseHeightYOffset`은 최종 퍼들 높이 보정을 유지한 상태에서 녹아내리기 시작 전 누운 자세만 쓰러지는 애니메이션 끝 높이에 맞추기 위한 시작 키 전용 보정값이다.
- 위치 커브에만 보정을 적용했으며 회전/스케일 커브 값은 이전 상태를 유지했다.

## 로그 확인값

- 적용 로그: `Logs/TergoDeathMeltHeightOnlyApply_20260707.log`
- 검증 로그: `Logs/TergoDeathMeltHeightOnlyValidate_20260707.log`
- 캡처 로그: `Logs/TergoDeathMeltHeightOnlyCapture_20260707.log`
- 주요 확인:
  - MeltPuddle 루트 회전 변화는 다시 생기지 않았다.
  - 기존 쓰러짐 구간은 변경되지 않았다.
  - Unity 샘플 5장을 같은 카메라 기준으로 캡처해 직접 확인했다.

## 추가 시작 자세 높이 보정

- 적용 로그: `Logs/TergoDeathMeltStartPoseHeightApply_20260707.log`
- 검증 로그: `Logs/TergoDeathMeltStartPoseHeightValidate_20260707.log`
- 캡처 로그: `Logs/TergoDeathMeltStartPoseHeightCapture_20260707.log`
- 주요 확인:
  - `PuddleStartGroundDelta=0`
  - `PuddleStartCenterHorizontalDelta=0`
  - `PuddleStartHorizontalAngleDelta=0`
  - `PuddleRootRotationDelta=0`
  - `PuddleGroundDelta=0.035`
  - `BaseFallMaxPositionDelta=0`
  - `BaseFallMaxRotationDelta=0`
  - `ExistingDeathMotionTouchedBeforeEnd=False`
  - `FrameCount=5`
- 실행하지 않은 항목:
  - 회전, 각도, 방향 선택, 스케일 커브 수정
  - 최종 퍼들 BlendShape 형태 변경
  - 기존 쓰러지는 FBX 모션 변경
  - Harness/EditMode/PlayMode/Build 검증
