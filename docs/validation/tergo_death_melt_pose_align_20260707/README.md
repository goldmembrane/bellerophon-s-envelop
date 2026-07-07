# Tergo 12번 Death MeltPuddle 자세 정렬 검증

- 검증 날짜: 2026-07-07 KST
- 대상: `Approved Tergo Enemy Placement/Tergo_12_Death`
- 사용자 확인 영상:
  - `c:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-07-07 12-52-07.mp4`

## 영상 프레임 확인

- `frames/contact_sheet_full.jpg`
  - 제공 영상 전체 구간을 24프레임으로 추출한 접촉시트.
- `frames/contact_sheet_transition_crop.jpg`
  - 3.30초부터 4.34초까지 MeltPuddle 전환 구간을 확대한 접촉시트.
- 확인 내용:
  - 기존 영상에서는 3.7초 이후 MeltPuddle 샘플이 기존 쓰러진 자세와 다른 방향으로 나타나고, 3.78초 이후 회전하며 웅덩이로 전환됐다.

## 수정 후 Unity 샘플 확인

- `unity_samples/contact_sheet_unity_samples.jpg`
  - Unity 에디터에서 `Tergo_Death_Dying_Fbx.anim`을 직접 샘플링해 렌더링한 접촉시트.
- 샘플 시점:
  - `01_before_melt_4_382s.png`
  - `02_melt_start_4_403s.png`
  - `03_mid_melt_5_128s.png`
  - `04_spread_5_853s.png`
  - `05_hold_6_203s.png`
- 확인 내용:
  - `01_before_melt`와 `02_melt_start`의 누운 방향이 같은 방향으로 이어진다.
  - `02_melt_start`부터 `05_hold`까지 MeltPuddle 루트 회전이 발생하지 않는다.
  - 최종 웅덩이는 바닥 정렬 상태를 유지한다.

## 로그 확인값

- 적용 로그: `Logs/TergoDeathMeltNoRotateApply_20260707.log`
- 검증 로그: `Logs/TergoDeathMeltNoRotateValidate_20260707.log`
- 주요 값:
  - `PuddleStartHorizontalAngleDelta=0`
  - `PuddleRootRotationDelta=0`
  - `PuddleStartGroundDelta=0`
  - `PuddleStartCenterHorizontalDelta=0`
  - `PuddleStartHeightDelta=0.004706`
  - `BaseFallMaxPositionDelta=0`
  - `BaseFallMaxRotationDelta=0`
