# Con Spirito 돌진 다리 모션 재비교 및 수정 계획

## 대상

- 현재 캡처: `C:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-07-12 14-05-37.mp4`
- 참고 영상: `C:/Users/gus68/Downloads/agent_generate_video - Pure side-profile tracking shot of a golden retriever runnin….mp4`
- 이전 분석: `docs/validation/con_spirito/charge_video_comparison_20260712/ANALYSIS.md`
- 현재 구현 코드: `Assets/_Project/Editor/Validation/ConSpiritoCargoRunSceneApplyAndReview.cs`

## 생성 산출물

- `video_metadata_and_frames.json`
- `con_spirito_140537_full_overview_contact_sheet.png`
- `golden_reference_full_overview_contact_sheet.png`
- `con_spirito_140537_cycle_focus_contact_sheet.png`
- `golden_reference_cycle_focus_contact_sheet.png`
- `con_spirito_140537_vs_golden_reference_cycle_aligned.png`
- `con_spirito_140537_cycle_subject_crop_sheet.png`
- `golden_reference_cycle_subject_crop_sheet.png`
- `con_spirito_140537_vs_golden_reference_cycle_cropped_aligned.png`
- 영상별 1초 cycle 프레임 및 crop 프레임

## 영상 기준 관찰

### 참고 영상

- 다리 실루엣은 항상 몸통 아래와 지면 사이에서 움직인다.
- 앞다리는 낮게 앞으로 뻗고, 어깨 아래에서 접지한 뒤 몸 아래를 지나 뒤로 빠진다.
- 뒷다리는 엉덩이 아래로 회수된 뒤 뒤쪽으로 길게 펴지며 추진한다.
- 좌우 다리는 대각선 쌍으로 교대하며, 한쪽 쌍이 지지할 때 반대 쌍은 회수 또는 reach 단계에 있다.
- `contact`, `push`, `tuck`, `reach`, `suspension`은 서로 섞여도 발 위치가 지면 기준을 벗어나지 않는다.
- 몸통 bob은 작고 다리 접지와 동기화되어 있으며, 머리/목은 상대적으로 안정적이다.

### 현재 콘 스피리토 캡처

- `1.20s`, `1.65s`, `1.73s`, `2.12s` 근처 프레임에서 다리 또는 다리와 연결된 스킨이 몸 위로 큰 삼각형/막 형태로 솟는다.
- 이 형태는 달리기의 reach/tuck이 아니라, 본 이동을 따라 스킨이 과도하게 끌려 올라간 리깅/스키닝 파손으로 보인다.
- 정상에 가까운 프레임에서도 다리 끝이 지면에 낮게 잠겨 있는 느낌이 약하고, 여러 다리가 몸 아래에 뭉쳐 보인다.
- 캡처에는 다른 Con Spirito 검토 슬롯이 뒤에 줄지어 있어 실루엣 판독을 방해한다. 그러나 큰 삼각형 변형은 배경 겹침만으로 설명되지 않고, 전경 개체 자체의 변형 문제다.
- 참고 영상처럼 `앞다리 낮은 reach -> contact -> support`, `뒷다리 회수 -> load -> 긴 push`가 분리되어 읽히지 않는다.

## 현재 구현과 문제의 연결

- 현재 돌진 루프는 `ChargeRunLoopDurationSeconds = 1.00f`다.
- 현재 다리 위상은 대각선 쌍에 가깝게 재배치되어 있다.
  - `frontleg = 0.02`
  - `R_backleg = 0.94`
  - `R_frontleg = 0.52`
  - `backleg = 0.44`
- 그러나 현재 구현은 다리 root bone에 큰 위치 오프셋을 직접 넣는다.
  - `ChargeRunLegLiftMeters = 0.550f`
  - `ChargeRunLegStrideMeters = 0.750f`
  - `localPosition.y`와 `localPosition.z` 커브가 다리 root bone에 직접 바인딩됨
- 이 값은 이전 dog walk 계열의 `0.055m` 수준과 비교하면 약 10배 이상 크다.
- 현재 문제는 위상만 틀린 상태가 아니라, deform bone을 컨트롤러처럼 크게 이동시키면서 스킨 웨이트가 같이 끌려가는 상태다.
- 따라서 `contact/push/tuck/reach/suspension` 타이밍을 더 다듬어도 root 위치 과구동을 유지하면 같은 파손이 반복될 가능성이 높다.

## 원인 판단

1. 다리 root bone의 직접 위치 이동이 과하다.
   - 특히 `localPosition.y` lift와 `localPosition.z` stride가 본래 bind pose에서 너무 멀리 벗어난다.
   - 다리와 몸통 연결부 버텍스가 다리 root 이동을 따라 끌려 올라가며 큰 막처럼 보인다.

2. 현재 리그는 큰 보폭 질주용 컨트롤 구조가 아니다.
   - foot IK target, pole target, control bone, deform bone 분리가 없다.
   - deform bone 자체를 큰 stride/lift 제어점처럼 쓰고 있다.

3. 발 접지 기준이 없다.
   - 참고 영상은 발이 지면에 잠깐 고정되는 stance 구간이 보인다.
   - 현재 구현은 본이 큰 타원 궤도로 움직이며 지면 접지보다 스킨 변형이 먼저 보인다.

4. 캡처 환경도 판독을 어렵게 만든다.
   - 여러 Con Spirito 슬롯이 뒤에 겹쳐 있어 정상 프레임에서도 다리 단계가 흐려진다.
   - 다음 검증은 `ConSpirito_03_Charge` 단일 개체 또는 배경 실루엣 없는 crop으로 해야 한다.

## 수정 계획

### 1단계: 파손 원인 차단

- 돌진 다리 root의 `localPosition.y/z` 대형 오프셋을 즉시 제거하거나 안전권으로 낮춘다.
- 1차 목표값:
  - `ChargeRunLegLiftMeters`: `0.550f`에서 `0.045f-0.090f` 범위로 축소
  - `ChargeRunLegStrideMeters`: `0.750f`에서 `0.050f-0.120f` 범위로 축소
- `SetLocalPositionOffsetCurve`로 root bone을 크게 움직이는 방식은 최종 돌진 모션의 핵심 수단으로 쓰지 않는다.
- 우선 메시 파손이 사라지는지 확인한 뒤에만 보폭을 다시 키운다.

### 2단계: 회전 기반 안전 루프 복구

- 루프 길이 `1.00s`와 현재 대각선 쌍 위상은 임시로 유지한다.
- 다리 root 위치 이동 대신 upper/lower/toe 회전량으로 읽히는 작은 보폭을 만든다.
- 앞다리:
  - 낮은 forward reach
  - 짧은 contact
  - 몸 아래 support
  - 과하지 않은 tuck
- 뒷다리:
  - hip 아래 회수
  - 짧은 load
  - 뒤쪽 push
  - 빠른 tuck
- 이 단계의 성공 기준은 참고 영상과 완전히 같아지는 것이 아니라, 큰 삼각형 스킨 파손과 몸 위로 솟는 다리 실루엣을 없애는 것이다.

### 3단계: 검토 카메라/대상 정리

- 다음 캡처는 `ConSpirito_03_Charge` 단일 개체만 보이게 하거나, 최소한 배경의 다른 Con Spirito 슬롯을 가리는 전용 검토 캡처로 만든다.
- 비교 산출물은 다음 기준으로 생성한다.
  - 전체 1초 loop 12프레임
  - 측면 crop 12프레임
  - 참고 영상 crop과 병렬 sheet
- 확인 항목:
  - 메시 파손 없음
  - 몸 위로 솟는 다리/스킨 없음
  - 앞다리 reach가 낮게 보임
  - 뒷다리 push가 뒤로 보임
  - 다리 쌍 교대가 읽힘

### 4단계: 보폭 확장은 리깅 수정 후 재시도

- 참고 영상 수준의 긴 보폭을 원하면 리깅 수정이 필요하다.
- 필요한 구조:
  - deform bone과 control bone 역할 분리
  - foot IK target 4개 또는 visible leg 수에 맞춘 타겟
  - pole target 또는 무릎/팔꿈치 방향 제어
  - 몸통/다리 연결부 weight 재조정
  - 필요 시 Animation Rigging 제약으로 발 접지 보정
- 이 구조를 갖추기 전에는 deform leg root의 큰 `localPosition` 이동을 다시 넣지 않는다.

### 5단계: 최종 질주 구조 재구성

- 리깅/IK 기반으로 다시 구성할 때의 목표 단계:
  - `contact`: 발이 지면 근처에 낮게 머물고 몸통이 살짝 내려감
  - `push`: 뒷다리 타겟이 뒤로 길게 빠지며 몸통 forward lean과 동기화
  - `tuck`: 발 타겟이 몸 아래로 올라오되 스킨이 몸 위로 솟지 않음
  - `reach`: 앞다리 타겟이 낮고 길게 전방으로 이동
  - `suspension`: 네 다리 모두 잠깐 지면에서 떨어지되 몸통과 스킨이 깨지지 않음
- 이 단계에서만 `0.12m` 이상의 체감 보폭 확장을 검토한다.

## 적용 우선순위

1. 현재 대형 `localPosition.y/z` 다리 root 오프셋 제거 또는 대폭 축소.
2. 회전 기반 안전 돌진 루프로 파손 제거.
3. 단일 돌진 개체 검토 캡처로 재검증.
4. 파손이 없어지면 작은 `localPosition` 보조값만 재도입.
5. 긴 보폭이 여전히 필요하면 리깅/IK/weight 수정 작업을 별도 승인 범위로 진행.

## 이번 분석에서 실행하지 않은 항목

- Unity 실행/리프레시/브리지 명령
- `.anim` 생성 또는 수정
- 리깅, 스키닝, 모델, 프리팹, 씬 수정
- Harness/EditMode/PlayMode/Build 검증
- Git 커밋/푸시
