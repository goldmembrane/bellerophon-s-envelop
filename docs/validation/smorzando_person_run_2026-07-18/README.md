# Smorzando 인간형 돌진 모델 교체 및 외형 동기화 기록

## 사용자 확정 내용

- 원본 `enemies model/smorzando running.fbx`를 인간형 돌진 모델로 사용한다.
- 현재 배치된 돌진 검토 개체는 `Smorzando_Person_04`로 확정한다.
- 돌진 모델의 몸통색·텍스처·머티리얼 표현을 정적 인간형 `Smorzando_Person_01`과 동기화한다.
- 이번 모션 개체는 실제 전진 이동 없이 제자리 달리기 애니메이션만 담당한다.

## 진행 상태

- Unity 모델 교체, 외형 동기화, 반복 클립과 전용 Animator Controller 연결, 시각 확인을 완료했다.
- 정적 비교와 달리기 키프레임을 직접 확인했으며 시각 판정은 통과다.

## 원본과 Unity 에셋

- 사용자 지정 원본: `enemies model/smorzando running.fbx`
- Unity 복사본: `Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person_Running.fbx`
- 원본·Unity 복사본 SHA-256: `3BC3C7BEC57170DF4A66CFDD94EC4E7D9B54F25FBF596B294EFDFB565B79044A`
- 기존 정적 FBX SHA-256: `CDE712400B52965CF0BE0E3D32FB5B59D9349C8C24F777CE7EEFA97233C75BF9`
- 기존 정적 FBX는 덮어쓰거나 수정하지 않았다.

## 구조 조사

- 구조 조사 명령: `InspectSmorzandoPersonRunningSource`
- 달리기 FBX와 정적 FBX는 모두 `char1` 단일 `SkinnedMeshRenderer`를 사용한다.
- 두 모델은 정점 `14,193개`, 서브메시 `1개`, 본 `24개`, Mesh Bounds가 정확히 일치한다.
- 달리기 FBX에는 길이 `0.633333초`, `60fps`, Transform 커브 `250개`인 Generic 클립 한 개가 있다.
- 모델 루트를 이동시키는 루트 모션 커브는 `0개`다.
- 상세 조사 결과: `Smorzando_PersonRunSourceInspection.txt`

## 돌진 모션 적용 구조

- 적용·캡처 도구: `Assets/_Project/Editor/Validation/SmorzandoPersonRunApplyAndReview.cs`
- Animator Controller: `Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Run.controller`
- 대상: `Approved Smorzando Enemy Placement/Smorzando_Person_04/Smorzando_Person_Model`
- 정적 비교: `Smorzando_Person_01`
- 기존 대기·보행: `Smorzando_Person_02`, `Smorzando_Person_03`에 그대로 유지
- 원본 클립 이름을 Unity에서 `Smorzando_Person_Run`으로 정리하고 반복 재생과 루트 회전·높이·XZ 위치 잠금을 설정했다.
- Animator의 `applyRootMotion`은 `false`다. 실제 돌진 이동은 애니메이션과 분리된 이후 물리 이동 계층의 책임으로 남겼다.

## 외형 동기화

- 달리기 FBX가 정적 FBX와 같은 Mesh·정점·본·서브메시 구조인지 적용 전에 확인했다.
- `Smorzando_Person_01`이 사용하는 `Smorzando_Person_Reference` 머티리얼 에셋을 달리기 모델의 동일 슬롯에 직접 연결했다.
- 새 텍스처나 머티리얼은 만들지 않았으며 몸통색, 갈색 촛농 광택, 어두운 표면 음영은 정적 모델과 동일한 에셋에서 나온다.
- 적용 보고에는 `MaterialReferenceMatched=True`, `GeometryMatchedStatic=True`가 기록됐다.

## Unity 적용 결과

- 적용 명령: `ApplySmorzandoPersonRun`
- `Smorzando_Person_04` 하위의 기존 정적 모델 인스턴스만 달리기 FBX 인스턴스로 교체했다.
- 슬롯 루트와 교체 전 모델의 로컬 위치·회전·크기는 유지했다.
- `Smorzando_Person_02` 대기 Animator Controller와 `Smorzando_Person_03` 보행 Animator Controller는 변경되지 않았다.
- 다른 스모르찬도 Transform도 변경하지 않았다.
- 적용 보고: `Smorzando_PersonRunApply.txt`

## 전용 시각 확인

- 캡처 명령: `CaptureSmorzandoPersonRunFrames`
- `0.633333초`, `10fps`, 정면·사선 각 `6프레임`과 정적 비교 화면을 생성했다.
- 정면·사선 키프레임에서 몸을 앞으로 낮춘 상태로 팔과 다리가 교대하는 돌진 달리기 자세를 확인했다.
- 메시 표면은 리그를 따라 함께 움직이며 표면 찢김, 부품 이탈, 개체 소실이 보이지 않았다.
- 정적 비교 화면에서 몸통색·광택·어두운 촛농 음영이 정적 모델과 일치한다.
- 모델 루트 Transform은 캡처 전 프레임에서 제자리에 고정했고 Animator 루트 모션도 비활성화했다.
- 키프레임 시트: `automated_visual_capture/Smorzando_PersonRun_KeyframeSheet.png`
- 정적 비교: `automated_visual_capture/Smorzando_PersonRun_StaticVsRun_T000.png`, `Smorzando_PersonRun_StaticVsRun_T025.png`
- 반복 영상: `automated_visual_capture/Smorzando_PersonRun_Loop.mp4`
- 캡처 보고: `MaterialReferenceMatched=True`, `GeometryMatchedStatic=True`, `ApplyRootMotion=False`, `ModelRootTransformHeldInPlace=True`, `VideoEncoded=True`, `SceneViewFocused=False`, `SceneSaved=False`, `SelectionCleared=True`.

## 확인된 경고와 실행하지 않은 항목

- 캡처 중 기존 장면의 다수 광원 때문에 그림자 아틀라스 해상도 축소 경고가 한 건 기록됐다. 전용 캡처 광원은 그림자를 사용하지 않으며 모델·애니메이션 적용 오류는 없다.
- 실제 전진 이동, Rigidbody·Collider, AI 돌진 판정, 충돌·피격·사망 연결은 수행하지 않았다.
- 별도 아트 샘플, 새 모델링·텍스처·머티리얼은 만들지 않았다.
- Unity 재시작, 검증·테스트·빌드 스크립트, Git 작업은 실행하지 않았다.
