# Smorzando 인간형 대기 모션 적용 기록

## 사용자 확정 내용

- 인간형 대기 상태에서 몸 전체가 호흡하듯 조금씩 모핑한다.
- 정적 비교 개체는 `Smorzando_Person_01`, 대기 모션 대상은 `Smorzando_Person_02`다.
- 이번 작업은 애니메이션 작업이므로 별도 `artSample/` 없이 Unity에 적용한다.

## 진행 상태

- Unity 적용과 전용 시각 확인을 완료했으며 시각 판정은 통과다.

## 구현 구조

- 런타임 구성요소: `Assets/_Project/Runtime/Enemies/Smorzando/SmorzandoPersonIdleMotion.cs`
- Unity 적용·캡처 도구: `Assets/_Project/Editor/Validation/SmorzandoPersonIdleApplyAndReview.cs`
- 적용 대상: `Approved Smorzando Enemy Placement/Smorzando_Person_02/Smorzando_Person_Model`
- 정적 비교 대상: `Approved Smorzando Enemy Placement/Smorzando_Person_01`
- 원본 `SkinnedMeshRenderer`의 현재 포즈를 실행 중 임시 Mesh로 Bake하고, 원본 FBX나 공유 Mesh를 덮어쓰지 않은 채 동일한 `14,193`개 정점을 변형한다.
- 발바닥 하단은 고정하고, 머리·몸통·팔·다리와 흘러내리는 표면에는 하나의 연속된 호흡 위상을 적용한다.
- 분리된 표면에서 법선 재계산으로 어두운 이음새가 생기지 않도록 Bake된 authored normal을 유지한다.

## 대기 모션 수치

- 한 주기: `3.4초`
- 몸 전체 수평 팽창·수축: 최대 `1.4%`
- 발바닥 기준 세로 신장·수축: 최대 `0.7%`
- 표면 2차 맥동: 최대 `0.3%`
- 발 고정 전환 범위: 전체 높이 하단 `18%`
- 루트 Transform 이동·회전·스케일 애니메이션: 없음

## Unity 적용 결과

- 적용 명령: `ApplySmorzandoPersonIdle`
- 대기 모션 구성요소 수: `1`
- 적용 개체: `Smorzando_Person_02`
- 다른 인간형 대기 모션 구성요소 수: `0`
- 원본 FBX 수정: 없음
- 다른 스모르찬도 Transform 변경: 없음
- 적용 뒤 Unity 선택을 해제했다.
- 상세 적용 보고: `Smorzando_PersonIdleApply.txt`

## 전용 시각 확인

- 캡처 명령: `CaptureSmorzandoPersonIdleFrames`
- `3.4초`, `10fps`, 정면·사선 각 `34프레임`을 생성했다.
- `0.0초` 기준 자세에서 `0.8초` 들숨 자세로 갈 때 머리·몸통·팔·다리 전체가 함께 미세하게 넓어지고 높아진다.
- `1.7초`에는 기준 자세를 지나고 `2.5초` 날숨 자세에서는 전체 실루엣이 함께 줄어든다.
- 발바닥은 전 구간에서 같은 바닥 위치를 유지하며 루트 이동은 없다.
- 정면·사선 키프레임에서 표면 구멍, 부품 분리, 재질 이탈, 개체 사라짐이 보이지 않는다.
- `3.3초` 마지막 프레임은 `0.0초` 시작 자세로 자연스럽게 수렴하며 반복 경계의 시각적 팝이 없다.
- `Smorzando_Person_01` 정적 비교와 `Smorzando_Person_02`의 기준·들숨 자세를 같은 화면에서 확인했다.
- 반복 영상 `automated_visual_capture/Smorzando_PersonIdle_Loop.mp4`를 정상 인코딩했다.
- 키프레임 시트: `automated_visual_capture/Smorzando_PersonIdle_KeyframeSheet.png`
- 정적 비교: `automated_visual_capture/Smorzando_PersonIdle_StaticVsIdle_T000.png`, `Smorzando_PersonIdle_StaticVsIdle_T085.png`
- 캡처 보고: `FootGrounded=True`, `RootTransformAnimated=False`, `VideoEncoded=True`, `SceneViewFocused=False`, `SceneSaved=False`, `SelectionCleared=True`.

## 확인된 경고와 실행하지 않은 항목

- 캡처 중 기존 장면의 다수 광원 때문에 그림자 아틀라스 해상도 축소 경고가 한 건 기록됐다. 전용 캡처 광원은 그림자를 사용하지 않으며 모션 적용 오류는 없었다.
- 인간형 이동·돌진·자폭·피격·사망, AI와 전투 로직은 구현하지 않았다.
- 설치형 대기·불꽃·변환 모션과 다른 인간형은 변경하지 않았다.
- 원본 FBX, 텍스처, 머티리얼은 수정하지 않았다.
- Unity 재시작과 Git 작업은 실행하지 않았다.
