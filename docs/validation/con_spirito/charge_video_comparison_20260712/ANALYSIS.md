# Con Spirito 돌진 모션 영상 비교 분석

## 대상 영상

- 콘 스피리토 캡처:
  - `C:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-07-12 13-40-12.mp4`
  - `1920x1000`, `34.33fps`, 메타데이터 길이 `4.24s`, 실제 반복 가능 프레임 `129`, 실제 샘플 길이 약 `3.758s`
- 기준 영상:
  - `C:/Users/gus68/Downloads/agent_generate_video - Pure side-profile tracking shot of a golden retriever runninˇ.mp4`
  - `1280x720`, `24fps`, 길이 약 `8.08s`, 실제 반복 가능 프레임 `194`

## 생성한 비교 산출물

- `con_spirito_capture_full_overview_contact_sheet.png`
- `golden_reference_full_overview_contact_sheet.png`
- `con_spirito_capture_cycle_focus_contact_sheet.png`
- `golden_reference_cycle_focus_contact_sheet.png`
- `con_spirito_vs_golden_reference_cycle_aligned.png`
- `con_spirito_capture_cycle_gameview_crop_sheet.png`
- `con_spirito_capture_cycle_subject_crop_sheet.png`
- `golden_reference_cycle_large_sheet.png`
- `video_metadata_and_frames.json`

## 기준 영상의 핵심 움직임

- 기준 영상은 측면 실루엣에서 다리 단계가 명확히 읽힌다.
- 한쪽 앞다리가 낮고 길게 앞으로 뻗은 뒤 어깨 아래에서 접지하고, 접지 후 몸 아래를 지나 뒤로 밀린다.
- 뒷다리는 엉덩이 아래로 접혀 들어온 뒤 뒤로 길게 펴지면서 추진한다.
- 좌우 다리는 완전히 동시에 움직이지 않고, 대각선 쌍이 교대로 지지와 회수를 맡는다.
- 몸통은 접지와 추진 순간에 작게 내려가고, 회수/공중 전환에서 올라간다.
- 머리와 목은 몸통보다 훨씬 안정적이고, 보폭에 맞춘 작은 상하 흔들림만 보인다.

## 현재 콘 스피리토 캡처의 차이

- 캡처에는 돌진 개체 뒤로 다른 콘 스피리토 실루엣이 겹쳐 보여 전경 돌진 포즈가 흐려진다.
- 전경 개체 기준으로도 앞다리와 뒷다리의 단계가 기준 영상처럼 분리되어 읽히지 않는다.
- 앞다리 두 개가 같은 방향으로 묶여 보이는 프레임이 많고, 대각선 쌍 교대가 약하다.
- `contact -> push -> tuck -> reach -> suspension`이 순서대로 보이기보다, 여러 다리가 몸 아래에 동시에 모이거나 동시에 앞으로 뻗는 인상이 강하다.
- 기준 영상의 뒷다리 push는 엉덩이 뒤로 길게 펴지는 형태인데, 콘 스피리토는 뒷다리 추진 길이가 짧고 몸 아래에 머문다.
- 기준 영상의 앞다리 reach는 낮고 길게 앞으로 나가며 접지 직전까지 바닥 근처를 유지하지만, 콘 스피리토는 발끝이 바닥 기준 없이 떠 있거나 몸 안쪽으로 뭉쳐 보인다.
- 몸통 bob은 존재하지만 다리 접지 순간과 충분히 맞물려 보이지 않는다.
- 머리/목은 대체로 전방 자세를 유지하지만, 전경과 배경 실루엣이 겹쳐 실제 흔들림 판독이 어렵다.
- 하복부 큰 늘어짐이나 메시 분리는 이번 캡처에서는 주요 문제로 보이지 않지만, 일부 프레임에서 배 아래 회색 음영과 다리 겹침이 접지 판독을 방해한다.

## 현재 구현 구조에서 보이는 원인

- 현재 돌진 루프는 `ChargeRunLoopDurationSeconds = 1.00f`다.
- `AddScaledSourceWalkCurves`로 기존 walk clip을 시간 축척해 깔고, 이후 `AddChargeRunRootCurves`, `AddChargeRunLegChainCurves`, `AddChargeForwardPoseCurves`로 일부 본을 덮어쓴다.
- 현재 다리 위상은 다음 구조다.
  - `backleg = 0.00`
  - `R_backleg = 0.18`
  - `frontleg = 0.56`
  - `R_frontleg = 0.74`
- 이 구조는 뒷다리 그룹이 먼저 움직이고 앞다리 그룹이 뒤따르는 방식이며, 기준 영상에서 보이는 대각선 쌍 교대가 충분히 드러나지 않는다.
- `contact/push/tuck/reach/suspension` pulse는 앞다리와 뒷다리에 같은 중심값을 공유하고, 계수만 달리 적용한다.
- 앞다리와 뒷다리는 실제 달리기에서 같은 단계 이름이라도 형태가 다르다. 앞다리의 핵심은 낮은 forward reach와 어깨 아래 contact이고, 뒷다리의 핵심은 hip 아래 회수와 뒤쪽 push다.
- 현재 `ChargeRunLegLiftMeters`, `ChargeRunLegStrideMeters`, `ChargeRunLegLateralMeters` 상수는 존재하지만, 돌진 다리 루프에서는 주로 회전 커브만 적용되고 발 위치를 지면 기준으로 잠그는 위치 커브가 없다.
- 그래서 다리가 관절 회전으로만 흔들리며, 실제 발이 땅을 딛고 몸을 밀어내는 인상이 약하다.

## 수정 계획

1. 검토 환경을 먼저 정리한다.
   - 다음 Unity 검증에서는 `ConSpirito_03_Charge` 전경 개체만 보이게 캡처하거나, 최소한 배경 실루엣이 겹치지 않는 측면 카메라를 사용한다.
   - 검토용으로 지면 기준선 또는 그림자를 유지해 발 접지 높이를 판독할 수 있게 한다.

2. 루프 길이는 우선 `1.00s`를 유지하고 구조를 먼저 고친다.
   - 이번 기준 영상의 비교 구간도 1초 안에서 주기가 읽히므로, 바로 속도를 바꾸기보다 다리 구조를 먼저 고친다.
   - 구조 수정 후에도 느리거나 빠르게 보이면 `0.84s-1.00s` 범위에서 마지막에 압축한다.

3. 현재 “뒷다리 그룹 먼저, 앞다리 그룹 나중” 위상을 대각선 쌍 교대로 바꾼다.
   - 목표 쌍 A: `frontleg` + `R_backleg`
   - 목표 쌍 B: `R_frontleg` + `backleg`
   - 쌍 B는 쌍 A보다 약 `0.50` 루프 늦게 둔다.
   - 같은 쌍 안에서도 완전 동시가 아니라 `0.04-0.08` 정도의 작은 지연을 둔다.

4. 앞다리와 뒷다리 pulse를 분리한다.
   - 앞다리 전용 단계:
     - 낮은 forward reach
     - 어깨 아래 contact
     - 몸 아래를 지나가는 support/push back
     - 짧고 높은 tuck
   - 뒷다리 전용 단계:
     - hip 아래 forward recovery
     - 짧은 contact/load
     - 뒤로 길게 펴지는 push
     - 빠른 tuck
   - `SmoothChargeRunPulse` 중심값 하나를 공유하는 구조에서 벗어나, 앞다리와 뒷다리에 별도 phase profile을 둔다.

5. 다리 루트에 위치 오프셋을 추가한다.
   - 기존 회전 커브만으로는 접지와 추진이 읽히기 어렵다.
   - 돌진 다리 루트에 `localPosition.y`와 `localPosition.z` 오프셋을 추가해 swing 중에는 발이 올라가고, stance 중에는 발이 낮고 뒤로 고정된 것처럼 보이게 한다.
   - 이미 있는 `ChargeRunLegLiftMeters`, `ChargeRunLegStrideMeters`, `ChargeRunLegLateralMeters`를 우선 재사용한다.

6. 접지 구간을 더 분명히 만든다.
   - contact 순간에는 발끝/하위 관절 접힘을 줄이고, 지면 높이에 가까운 낮은 자세를 유지한다.
   - push 순간에는 뒷다리 root swing과 lower joint extension을 키워 엉덩이 뒤쪽으로 긴 추진선을 만든다.
   - tuck과 reach가 동시에 커지는 프레임을 줄여, 다리가 몸 아래에서 뭉치는 구간을 줄인다.

7. 몸통 bob, pitch, roll을 다리 접지 쌍에 맞춘다.
   - 현재 root bob 키는 독립적으로 배치되어 있어 다리 접지와 충분히 맞지 않는다.
   - 대각선 쌍 A와 B의 contact/load 순간에 몸통이 약간 내려가고, tuck/reach 전환에서 올라가도록 다시 맞춘다.
   - roll은 다리 접지 쪽으로 작게만 적용해 하복부 늘어짐을 다시 만들지 않게 한다.

8. 머리/목은 안정화한다.
   - 기준 영상처럼 머리와 목은 forward lean을 유지하고, 몸통 bob보다 작은 반응만 준다.
   - 머리/목의 과도한 상하 이동보다 chest/hips의 리듬이 먼저 읽히게 한다.

9. 적용 후 검증 기준을 바꾼다.
   - 단순 정적 PNG만 보지 말고, 같은 1초 구간의 `12프레임 contact sheet`를 기준 영상과 나란히 놓고 비교한다.
   - 확인 항목은 대각선 쌍 교대, front reach, hind push, stance foot low hold, tuck separation, body bob sync, 메시 분리/하복부 늘어짐 여부로 둔다.

## 다음 구현 우선순위

1. 검토 캡처에서 돌진 개체를 분리하거나 side camera crop을 고정한다.
2. 다리 위상을 대각선 쌍 기준으로 재배치한다.
3. 앞다리/뒷다리 pulse profile을 분리한다.
4. 다리 root `localPosition.y/z` stride/lift 커브를 추가한다.
5. root bob/pitch/roll을 새 접지 타이밍에 다시 맞춘다.
6. Unity에 적용 후 동일한 비교 contact sheet를 재생성해 기준 영상과 다시 비교한다.
