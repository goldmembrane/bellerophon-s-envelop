# Smorzando 레퍼런스 색상 적용 기록

## 작업 범위

- 설치형 레퍼런스: `image/smorzando(스모르찬도).png`
- 좀비형 레퍼런스: `image/smorzando-person.png`
- 적용 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 적용 대상: `Approved Smorzando Enemy Placement` 아래 설치형 3개와 좀비형 5개
- 별도 `artSample`은 만들지 않고 사용자가 지정한 두 레퍼런스와 최종 FBX에 바로 적용했다.

## 생성 에셋

- 설치형 재질: `Assets/_Project/Art/Enemies/Smorzando/Materials/Smorzando_Installed_Reference.mat`
- 좀비형 재질: `Assets/_Project/Art/Enemies/Smorzando/Materials/Smorzando_Person_Reference.mat`
- 좀비형 왁스 알베도: `Assets/_Project/Art/Enemies/Smorzando/Textures/Smorzando_Person_Wax_Albedo.png`
- 좀비형 알베도 SHA-256: `6B6F0DC56AFE4EE44081B5DB7557532D242E34A6FA386788D0E10EDE7CB9E98A`

## 레퍼런스 색상 반영

- 두 레퍼런스에서 갈색 계열 픽셀을 추출하고 명도 하위·중간·상위 3단계 팔레트를 만들었다.
- 설치형 팔레트는 Shadow `(0.401749, 0.163838, 0.088013)`, Mid `(0.560551, 0.355148, 0.278949)`, Highlight `(0.701258, 0.505114, 0.425196)`다.
- 좀비형 팔레트는 Shadow `(0.280487, 0.070735, 0.036783)`, Mid `(0.456686, 0.192496, 0.127717)`, Highlight `(0.688538, 0.422469, 0.332957)`다.
- 설치형은 적갈색 기본색과 높은 왁스 광택을 적용했다.
- 좀비형은 레퍼런스 팔레트로 만든 반복형 왁스 알베도와 광택을 적용했다.
- 장면의 강한 적색 조명에서 분홍색으로 뜨는 현상을 줄이기 위해 조명 반응 기본색을 낮추고, 레퍼런스 갈색을 약한 발광색으로 보존했다.

## 원본 모델 구조에 따른 한계

- 설치형 Mesh는 UV0가 `0개`이고 렌더러와 서브메시가 각각 `1개`여서 표면 위치별 텍스처 채색을 할 수 없다. 따라서 설치형은 단일 갈색 재질과 형상·광택으로 표현했다.
- 좀비형은 UV0가 `14193개` 있어 알베도를 적용했지만 렌더러와 서브메시는 각각 `1개`다.
- 현재 원본 구조에서는 설치형의 불꽃·심지와 좀비형의 심지·눈·입을 별도 재질색으로 정확히 분리할 수 없다. 이를 분리하려면 UV 또는 서브메시·재질 슬롯을 추가하는 모델 구조 변경이 필요하며 이번 승인 범위에서는 실행하지 않았다.

## 자동 시각 확인

- `ApplySmorzandoReferenceColors`로 설치형 렌더러 3개와 좀비형 렌더러 5개에 각각 전용 재질을 적용했다.
- 적용 전후 루트 및 하위 Transform 스냅샷을 비교해 위치·회전·크기 변경이 없음을 확인했다.
- `CaptureSmorzandoReferenceColorFrames`로 개별 정면, 전체 행, 저장된 실제 Player Main Camera 화면을 생성했다.
- 실제 시작 화면에서 설치형 3개가 적갈색, 좀비형 5개가 짙은 갈색으로 같은 계열에 들어오며 이전 회색 재질이 남지 않은 것을 확인했다.
- 개별 중립 캡처는 임시 URP 조명이 바닥까지 어둡게 렌더되어 색상 판정의 주 근거로 사용하지 않았고, 실제 Player Main Camera 캡처를 주 근거로 사용했다.
- 캡처 중 다수 장면 조명으로 그림자 아틀라스 해상도 축소 경고가 두 건 있었으나 재질 적용 오류는 없었다.
- 적용 보고서: `Smorzando_ReferenceColorApply.txt`
- UV·재질 조사: `Smorzando_MaterialUvState.txt`
- 실제 시작 화면: `automated_visual_capture/Smorzando_ReferenceColor_PlayerView.png`
- 전체 행: `automated_visual_capture/Smorzando_ReferenceColor_Row.png`
- 레퍼런스 대조 시트: `automated_visual_capture/Smorzando_ReferenceVsUnity_ContactSheet.png`
- 자동 캡처는 Play Mode와 Scene View 선택·포커스를 사용하지 않았으며 종료 시 Unity 선택을 해제했다.

## 실행하지 않은 항목

- 원본 레퍼런스 이미지와 FBX의 형상·UV·리그·애니메이션을 수정하지 않았다.
- 스모르찬도 배치, 위치·회전·크기와 Player 시작점을 변경하지 않았다.
- 애니메이션, AI, 체력, 피격, 사망, 변환, 자폭, 전투 로직을 구현하지 않았다.
- 다른 적대 개체와 재질을 변경하지 않았다.
- Unity 재시작, 하네스 검증, 범위 밖 검증·테스트·빌드, Git 작업을 실행하지 않았다.
- 현재 상태는 사용자 시각 검토 대기이며 승인 완료로 처리하지 않는다.
