# Smorzando 인간형 반복 사망 모션 구현 기록

## 사용자 확정 내용

- 별도 사망 모델이 없으므로 인간형 정적 모델을 복사한다.
- 복사본은 피격 모션 개체의 X축 양의 방향 오른쪽에 배치한다.
- 사망 모션은 뒤로 넘어져 바닥에 눕는 동작으로 구현한다.
- 검토용 사망 모션 개체에서는 Animation Clip을 반복 재생한다.

## 진행 상태

- 정적 모델 복제, 6번 개체 배치, 사망 Animation Clip 제작, Animator Controller 연결, 반복 재생 설정과 시각 확인을 완료했다.
- 정면·사선 키프레임과 반복 영상을 직접 확인했으며 시각 판정은 통과다.

## 복제와 배치

- 복제 원본: `Approved Smorzando Enemy Placement/Smorzando_Person_01`
- 신규 대상: `Approved Smorzando Enemy Placement/Smorzando_Person_06`
- 기준 피격 개체: `Smorzando_Person_05`
- `Smorzando_Person_04 → 05`의 X 간격 `2.444763`을 그대로 사용했다.
- 6번 개체의 로컬 위치는 `(19.34551, 0, 0)`이다.
- 정적 모델의 로컬 위치 `(0, 0.313106, 0)`, Y 회전 `180도`, 크기 `(1,1,1)`을 유지했다.
- `char1` 메시, 정점 `14,193개`, 본 `24개`, `Smorzando_Person_Reference` 머티리얼이 복제 원본과 일치한다.

## 사망 모션 구조

- Animation Clip: `Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Death.anim`
- Animator Controller: `Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Death.controller`
- 적용·캡처 도구: `Assets/_Project/Editor/Validation/SmorzandoPersonDeathApplyAndReview.cs`
- 전체 반복 주기: `1.8초`
- 바닥 착지 시점: `1.05초`
- 최종 누운 자세 확인 시점: `1.79초`
- 골반·척추·목·머리·어깨·팔·다리 17개 Transform에 회전·Hips 위치 커브 71개를 적용했다.
- Armature는 최종 약 `88도` 뒤로 회전한다.
- 최종 높이는 서 있는 높이의 `0.663156`, 깊이는 `1.398674`다.
- 최종 메시와 바닥 사이 간격은 `0.010025`다.
- 모델 루트에는 애니메이션 커브가 없으며 `applyRootMotion=false`다.
- `loopTime=true`, `loopBlend=false`로 설정해 누운 자세를 유지한 뒤 반복 경계에서 서 있는 시작 자세로 초기화되어 다시 넘어간다.

## Unity 적용 결과

- 조사 명령: `InspectSmorzandoPersonDeathTarget`
- 적용 명령: `ApplySmorzandoPersonDeath`
- 6번 개체와 Death Controller만 새로 추가했다.
- `Smorzando_Person_01~05`의 Transform과 대기·보행·돌진·피격 Controller는 보존됐다.
- 정적 원본 FBX, 공유 메시와 머티리얼은 수정하지 않았다.
- 적용 보고: `Smorzando_PersonDeathApply.txt`

## 바닥 정렬 측정 보정

- 최초 검사는 본 자세 변경 뒤에도 갱신되지 않은 캐시된 `Renderer.bounds`를 읽어 높이·깊이를 모두 `1`로 잘못 측정했다.
- 현재 포즈의 `SkinnedMeshRenderer.BakeMesh`를 직접 계산하도록 변경해 실제 누운 메시 Bounds를 측정했다.
- 반복 Clip의 정확한 끝 시간 `1.8초`는 Unity가 `0초`로 래핑하므로 최종 자세 판정은 반복 경계 직전 `1.79초`를 사용한다.
- Hips 위치와 메시 월드 높이의 실제 응답률을 시험 커브 재임포트 후 Bake하여 바닥 간격을 `0.010025`로 맞췄다.

## 전용 시각 확인

- 캡처 명령: `CaptureSmorzandoPersonDeathFrames`
- `1.8초`, `15fps`, 정면·사선 각 `27프레임`을 생성했다.
- 시작, 기울기 시작, 후방 낙하, 착지, 누운 자세 유지 구간을 키프레임 시트에서 확인했다.
- 정면·사선 화면에서 엉덩이와 등이 낮아지며 뒤로 넘어지고 전신이 바닥에 눕는 흐름이 확인된다.
- 마지막 세 키프레임은 같은 누운 자세를 유지하며 최종 자세 각도 차이는 `0도`다.
- 표면 찢김, 부품 이탈, 개체 소실, 머티리얼 변화와 바닥 관통이 보이지 않는다.
- 키프레임 시트: `automated_visual_capture/Smorzando_PersonDeath_KeyframeSheet.png`
- 정적 비교: `automated_visual_capture/Smorzando_PersonDeath_StaticVsDeath_T000.png`, `Smorzando_PersonDeath_StaticVsDeath_T179.png`
- 반복 영상: `automated_visual_capture/Smorzando_PersonDeath_Loop.mp4`
- 캡처 보고: `FinalPoseHeld=True`, `LoopResetToStanding=True`, `FinalGroundGap=0.010025`, `ApplyRootMotion=False`, `ModelRootTransformAnimated=False`, `MaterialReferenceMatched=True`, `GeometryMatchedStatic=True`, `VideoEncoded=True`, `SceneViewFocused=False`, `SceneSaved=False`, `SelectionCleared=True`.

## 실행하지 않은 항목

- 실제 체력·사망 판정, AI 상태, 시체 제거는 연결하지 않았다.
- Rigidbody 낙하, Collider, 물리 래그돌은 적용하지 않았다.
- 사망 VFX·사운드와 새 모델링·텍스처·머티리얼은 만들지 않았다.
- 별도 아트 샘플, Unity 재시작과 Git 작업은 수행하지 않았다.
