# Smorzando 인간형 피격 모션 구현 기록

## 사용자 확정 내용

- `Smorzando_Person_05`를 피격 모션 검토 개체로 사용한다.
- 피격 모션은 순간적으로 뒤로 움찔한 뒤 서 있는 자세로 복귀한다.
- 피격 모션은 검토용 개체에서 반복 재생한다.
- 공격은 자폭이며 형태 변화는 이펙트로 처리하므로 별도 공격 Animation Clip은 만들지 않는다.

## 진행 상태

- 피격 Animation Clip 제작, 전용 Animator Controller 연결, 5번 개체 적용, 반복 재생 설정과 시각 확인을 완료했다.
- 정면·사선 키프레임과 반복 영상을 직접 확인했으며 시각 판정은 통과다.

## 대상 조사

- 조사 명령: `InspectSmorzandoPersonHitTarget`
- 대상: `Approved Smorzando Enemy Placement/Smorzando_Person_05/Smorzando_Person_Model`
- 적용 전 5번 개체에는 Animator가 없었다.
- 정적 모델과 동일한 `char1` 메시, 정점 `14,193개`, 본 `24개`, `Smorzando_Person_Reference` 머티리얼을 사용한다.
- 리그 루트는 `Armature/Hips`이며 골반·척추·목·머리·양팔·양다리 Transform이 분리되어 있다.
- 상세 조사 결과: `Smorzando_PersonHitTargetInspection.txt`

## 피격 모션 구조

- Animation Clip: `Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Hit.anim`
- Animator Controller: `Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Hit.controller`
- 적용·캡처 도구: `Assets/_Project/Editor/Validation/SmorzandoPersonHitApplyAndReview.cs`
- 전체 반복 주기: `0.75초`
- 최대 후방 반동 시점: `0.14초`
- `0.58초`에 서 있는 기준 자세로 복귀하고 `0.75초`까지 유지한 뒤 반복한다.
- 골반·척추·목·머리·어깨·팔·다리 17개 Transform에 Quaternion 회전 커브 68개를 적용했다.
- 척추 누적 후방 반동은 약 `32도`다.
- 모델 루트에는 애니메이션 커브가 없으며 `applyRootMotion=false`다.

## Unity 적용 결과

- 적용 명령: `ApplySmorzandoPersonHit`
- `Smorzando_Person_05` 모델에만 Hit Controller와 Animator를 연결했다.
- Clip은 `loopTime=true`, `loopBlend=true`로 설정했다.
- 최대 반동 뒤 시작 자세와 같은 서 있는 자세로 복귀한다.
- 정적 기준과 메시 구조·머티리얼이 일치한다.
- `Smorzando_Person_02` 대기, `03` 보행, `04` 돌진 Controller는 보존됐다.
- 다른 스모르찬도 Transform은 변경하지 않았다.
- 적용 보고: `Smorzando_PersonHitApply.txt`

## 전용 시각 확인

- 캡처 명령: `CaptureSmorzandoPersonHitFrames`
- `0.75초`, `20fps`, 정면·사선 각 `15프레임`을 생성했다.
- 시작, 반동 진입, 최대 반동, 감쇠, 복귀, 서 있는 자세 유지 구간을 키프레임 시트에서 확인했다.
- 사선 화면에서 머리·어깨·척추가 뒤로 빠지고 팔이 반사적으로 벌어진 뒤 원래 자세로 돌아오는 변화가 확인된다.
- 발과 모델 루트는 제자리에 유지되며 표면 찢김, 부품 이탈, 개체 소실, 머티리얼 변화가 보이지 않는다.
- 키프레임 시트: `automated_visual_capture/Smorzando_PersonHit_KeyframeSheet.png`
- 정적 비교: `automated_visual_capture/Smorzando_PersonHit_StaticVsHit_T000.png`, `Smorzando_PersonHit_StaticVsHit_T014.png`
- 반복 영상: `automated_visual_capture/Smorzando_PersonHit_Loop.mp4`
- 캡처 보고: `ReturnedToStanding=True`, `ApplyRootMotion=False`, `ModelRootTransformAnimated=False`, `MaterialReferenceMatched=True`, `GeometryMatchedStatic=True`, `VideoEncoded=True`, `SceneViewFocused=False`, `SceneSaved=False`, `SelectionCleared=True`.

## 실행하지 않은 항목

- 실제 피격 판정, 체력 감소, 물리 넉백과 AI 연결은 수행하지 않았다.
- 자폭 이펙트와 공격 로직은 변경하지 않았다.
- 별도 공격 Animation Clip은 만들지 않았다.
- 새 모델링·텍스처·머티리얼 및 별도 아트 샘플은 만들지 않았다.
- Unity 재시작과 Git 작업은 수행하지 않았다.
