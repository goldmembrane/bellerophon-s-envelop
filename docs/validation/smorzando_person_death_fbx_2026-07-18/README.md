# Smorzando 인간형 사망 FBX 교체 및 외형 동기화 기록

## 사용자 확정 내용

- 원본 `enemies model/smorzando death.fbx`를 인간형 사망 모델로 사용한다.
- 현재 배치된 사망 검토 개체 `Smorzando_Person_06`의 모델을 해당 FBX로 교체한다.
- 사망 모델의 몸통색과 머티리얼 표현을 정적 인간형 `Smorzando_Person_01`과 동기화한다.
- 내장 사망 애니메이션은 반복 재생한다.

## 진행 상태

- Unity 모델 교체, 외형 동기화, 반복 클립과 기존 사망 Animator Controller 연결, 시각 확인을 완료했다.
- 6번 슬롯과 1~5번 개체는 유지하고 `Smorzando_Person_06/Smorzando_Person_Model` 하위만 교체했다.
- 이전에 만든 절차형 `Smorzando_Person_Death.anim`은 삭제하지 않고 보존했으며, 현재 컨트롤러에서는 분리했다.

## 원본과 Unity 에셋

- 사용자 지정 원본: `enemies model/smorzando death.fbx`
- Unity 복사본: `Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person_Death.fbx`
- 원본·Unity 복사본 SHA-256: `79C2C8FCBF7B498FDA20FF300E2BC6A95CE18E6C7292C9334CFCAB63DED42D05`
- 기존 정적 FBX SHA-256: `CDE712400B52965CF0BE0E3D32FB5B59D9349C8C24F777CE7EEFA97233C75BF9`
- 기존 정적 FBX와 절차형 사망 `.anim`은 수정하거나 삭제하지 않았다.

## 구조 조사

- 구조 조사 명령: `InspectSmorzandoPersonDeathFbxSource`
- 사망 FBX와 정적 FBX는 모두 `char1` 단일 `SkinnedMeshRenderer`, 정점 `14,193개`, 서브메시 `1개`, 본 `24개`를 사용한다.
- 사망 FBX에는 길이 `9.333334초`, `60fps`, Transform 커브 `250개`인 Generic 클립 한 개가 있다.
- 모델 루트를 직접 움직이는 루트 모션 결합은 `0개`다.
- 원본 클립 이름은 `Armature|Armature|Armature|Armature|Strangled_and_Fall_Forward|baselayer`다.
- 상세 조사 결과: `Smorzando_PersonDeathFbxSourceInspection.txt`

## 사망 모션 적용 구조

- 적용·캡처 도구: `Assets/_Project/Editor/Validation/SmorzandoPersonDeathApplyAndReview.cs`
- 원본 조사 도구: `Assets/_Project/Editor/Validation/SmorzandoPersonDeathFbxApplyAndReview.cs`
- Animator Controller: `Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Death.controller`
- 대상: `Approved Smorzando Enemy Placement/Smorzando_Person_06/Smorzando_Person_Model`
- 정적 외형 기준: `Smorzando_Person_01`
- 내장 클립 이름을 Unity에서 `Smorzando_Person_Death_Fbx`로 정리하고 `loopTime=true`, `loopBlend=false`로 설정했다.
- 루트 회전·높이·XZ 위치 잠금을 설정하고 Animator의 `applyRootMotion`은 `false`로 유지했다.
- 컨트롤러 `Death` 상태는 FBX 서브 에셋 GUID `9aa75a6489418194ba61b67a6f36995e`를 참조하며, 절차형 `.anim` GUID `eaf36b37558ba1b479b548559ee09691`은 참조하지 않는다.

## 외형 동기화

- 적용 전에 사망 FBX가 정적 모델과 동일한 렌더러 수·메시 이름·정점·서브메시·본 구조인지 확인했다.
- `Smorzando_Person_01`의 `Smorzando_Person_Reference` 머티리얼 에셋을 사망 모델의 동일 슬롯에 직접 연결했다.
- 새 텍스처나 머티리얼은 만들지 않았다.
- 적용 보고에는 `MaterialReferenceMatched=True`, `GeometryStructureMatchedStatic=True`가 기록됐다.

## Unity 적용 결과

- 적용 명령: `ApplySmorzandoPersonDeathFbx`
- 6번 슬롯의 로컬 위치 `(19.34551, 0, 0)`과 기존 모델의 로컬 위치·회전·크기를 유지했다.
- `Smorzando_Person_02` 대기, `03` 보행, `04` 돌진, `05` 피격 Animator Controller는 변경하지 않았다.
- 다른 스모르찬도 Transform도 변경하지 않았다.
- 적용 보고: `Smorzando_PersonDeathFbxApply.txt`

## 전용 시각 확인

- 캡처 명령: `CaptureSmorzandoPersonDeathFbxFrames`
- `9.333334초`, `5fps`, 정면·사선 각 `47프레임`, 정적 비교 화면과 반복 영상을 생성했다.
- 직접 확인한 키프레임에서는 서 있는 자세에서 몸을 숙이고 앞으로 무너져 바닥에 눕는 원본 FBX 동작이 이어진다.
- 몸통의 짙은 갈색, 촛농 광택과 어두운 표면 음영은 정적 모델과 일치하며, 메시 찢김·부품 이탈·개체 소실은 보이지 않는다.
- 모델 루트 Transform은 캡처 전 프레임에서 고정했고 Animator 루트 모션도 비활성화했다.
- 원본 FBX의 마지막 자세는 캡처 바닥 기준 최저점이 `-0.154976`이며 일부가 바닥 안쪽으로 들어간다. 원본 애니메이션 보존을 위해 별도 자세 보정은 적용하지 않았다.
- 키프레임 시트: `automated_visual_capture/Smorzando_PersonDeath_KeyframeSheet.png`
- 정적 비교: `automated_visual_capture/Smorzando_PersonDeath_StaticVsDeath_T000.png`, `automated_visual_capture/Smorzando_PersonDeath_StaticVsDeath_Final.png`
- 반복 영상: `automated_visual_capture/Smorzando_PersonDeath_Loop.mp4`
- 캡처 보고: `ClipLoop=True`, `ApplyRootMotion=False`, `ModelRootTransformAnimated=False`, `VideoEncoded=True`, `SceneViewFocused=False`, `SceneSaved=False`, `SelectionCleared=True`.

## 실행하지 않은 항목

- 실제 사망 판정, Rigidbody·Collider, 래그돌, AI, VFX, 사운드는 연결하지 않았다.
- 별도 아트 샘플, 새 모델링·텍스처·머티리얼은 만들지 않았다.
- Unity 재시작, 검증·테스트·빌드 스크립트, Git 작업은 실행하지 않았다.
