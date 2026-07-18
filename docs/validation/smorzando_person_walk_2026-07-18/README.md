# Smorzando 인간형 보행 모델 교체 및 외형 동기화 기록

## 사용자 확정 내용

- 원본 `enemies model/smorzando walking.fbx`를 인간형 보행 모델로 사용한다.
- 정적 비교는 `Smorzando_Person_01`, 대기는 `Smorzando_Person_02`, 보행은 `Smorzando_Person_03`으로 분리한다.
- 보행 모델의 몸통색·텍스처·머티리얼 표현을 정적 인간형과 동기화한다.

## 진행 상태

- Unity 모델 교체·외형 동기화·전용 시각 확인을 완료했으며 시각 판정은 통과다.

## 원본과 Unity 에셋

- 사용자 지정 원본: `enemies model/smorzando walking.fbx`
- Unity 복사본: `Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person_Walking.fbx`
- 원본·Unity 복사본 SHA-256: `F431F8B755E51EA98155C2A2A7D5E5C67D8ACF680D4BBFA084257E811183BB8D`
- 기존 정적 FBX SHA-256: `CDE712400B52965CF0BE0E3D32FB5B59D9349C8C24F777CE7EEFA97233C75BF9`
- 기존 정적 FBX는 덮어쓰거나 수정하지 않았다.

## 구조 조사

- 구조 조사 명령: `InspectSmorzandoPersonWalkingSource`
- 걷기 FBX와 정적 FBX는 모두 `char1` 단일 `SkinnedMeshRenderer`를 사용한다.
- 두 모델은 정점 `14,193개`, 서브메시 `1개`, 본 `24개`, Mesh Bounds가 정확히 일치한다.
- 걷기 FBX에는 길이 `4.1초`, `60fps`, Transform 커브 `250개`인 걷기 클립 한 개가 있다.
- 모델 루트를 이동시키는 루트 모션 커브는 `0개`다.
- 상세 조사 결과: `Smorzando_PersonWalkSourceInspection.txt`

## 보행 적용 구조

- 적용·캡처 도구: `Assets/_Project/Editor/Validation/SmorzandoPersonWalkApplyAndReview.cs`
- Animator Controller: `Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Walk.controller`
- 대상: `Approved Smorzando Enemy Placement/Smorzando_Person_03/Smorzando_Person_Model`
- 정적 비교: `Smorzando_Person_01`
- 기존 대기 모션: `Smorzando_Person_02`에 그대로 유지
- 원본 클립 이름을 Unity에서 `Smorzando_Person_Walk`로 정리하고 `loopTime=1`, `loopBlend=1`로 설정했다.
- Animator의 `applyRootMotion`은 `false`다. 실제 전진 이동은 이번 범위에 포함하지 않고 이후 Rigidbody 기반 이동과 분리한다.

## 외형 동기화

- 걷기 FBX가 정적 FBX와 같은 Mesh·정점·본·서브메시 구조인지 적용 전에 확인했다.
- `Smorzando_Person_01`이 사용하는 `Smorzando_Person_Reference` 머티리얼 에셋을 걷기 모델의 동일한 머티리얼 슬롯에 직접 연결했다.
- 새 텍스처나 머티리얼을 만들지 않았으며 몸통색, 갈색 촛농 광택, 어두운 표면 음영은 정적 모델과 동일한 에셋에서 나온다.
- 적용 보고에는 `MaterialReferenceMatched=True`, `GeometryMatchedStatic=True`가 기록됐다.

## Unity 적용 결과

- 적용 명령: `ApplySmorzandoPersonWalk`
- `Smorzando_Person_03` 하위의 기존 정적 모델 인스턴스만 걷기 FBX 인스턴스로 교체했다.
- 슬롯 루트의 위치·회전·크기와 다른 스모르찬도 Transform은 변경하지 않았다.
- 걷기 모델의 로컬 위치·회전·크기는 교체 전 모델과 동일하게 유지했다.
- `Smorzando_Person_01`, `02`, `04`, `05`와 설치형·불꽃·변환 모션은 변경하지 않았다.
- 첫 적용은 Animator 생성 전 Unity 가짜 null 참조로 저장 전에 중단됐다.
- 두 번째 적용은 모델 교체 저장 뒤 보존 대상 Animator 검증의 같은 가짜 null 참조로 중단됐다. 저장된 교체 상태는 정상이었다.
- Unity 방식의 명시적 null 판정으로 검증을 수정한 뒤 최종 적용이 정상 완료됐다.
- 최종 적용 보고: `Smorzando_PersonWalkApply.txt`

## 전용 시각 확인

- 캡처 명령: `CaptureSmorzandoPersonWalkFrames`
- `4.1초`, `10fps`, 정면·사선 각 `41프레임`을 생성했다.
- `0.0`, `1.0`, `2.0`, `3.0`, `4.0초` 키프레임에서 양팔과 양다리가 교대로 움직이는 보행 자세를 확인했다.
- 몸통과 흘러내리는 촛농 부위가 리그를 따라 함께 움직이며 표면 찢김, 부품 이탈, 개체 소실이 없다.
- `0.0초`와 `4.0초` 자세는 같은 보행 위상으로 수렴하며 Unity의 loop blend가 활성화돼 있다.
- 모델 루트 Transform은 전 프레임에서 고정돼 있고 Animator 루트 모션도 비활성화돼 있다.
- 정적 비교 화면에서 걷기 모델의 몸통색·광택·어두운 촛농 음영이 정적 모델과 일치한다.
- 키프레임 시트: `automated_visual_capture/Smorzando_PersonWalk_KeyframeSheet.png`
- 정적 비교: `automated_visual_capture/Smorzando_PersonWalk_StaticVsWalk_T000.png`, `Smorzando_PersonWalk_StaticVsWalk_T103.png`
- 반복 영상: `automated_visual_capture/Smorzando_PersonWalk_Loop.mp4`
- 캡처 보고: `MaterialReferenceMatched=True`, `GeometryMatchedStatic=True`, `ApplyRootMotion=False`, `ModelRootTransformAnimated=False`, `VideoEncoded=True`, `SceneViewFocused=False`, `SceneSaved=False`, `SelectionCleared=True`.

## 확인된 경고와 실행하지 않은 항목

- 캡처 중 기존 장면의 다수 광원 때문에 그림자 아틀라스 해상도 축소 경고가 한 건 기록됐다. 전용 캡처 광원은 그림자를 사용하지 않으며 모델·애니메이션 적용 오류는 없다.
- 실제 전진 이동, Rigidbody·Collider, AI, 조우·돌진·자폭, 피격·사망을 연결하지 않았다.
- 새 모델링·텍스처·머티리얼을 만들지 않았다.
- Unity 재시작과 Git 작업은 실행하지 않았다.
