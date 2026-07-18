# Smorzando 모델 불꽃·이펙트 결합 재제작 기록

## 사용자 확정 내용

- 모델링에 원래 존재하는 입체 불꽃을 밝은 불꽃 중심으로 사용한다.
- 심지는 검은색으로 남긴다.
- 이펙트 불꽃은 모델 불꽃에 밀착시킨다.
- 공기 흐름 반응을 표현하는 불꽃 흔들림과 주변광 깜박임은 유지한다.
- 이번 파생 Mesh·재질·VFX는 사용자 예외 승인에 따라 별도 `artSample/` 없이 Unity에 직접 적용했다.

## 원본 형상 조사

- 원본 FBX는 단일 Visible Mesh이며 불꽃과 심지가 양초 몸체에 이어져 있다.
- 전체 삼각형 연결 요소는 `2개`지만 두 번째 요소는 불꽃이 아니라 바닥의 작은 촛농 조각이다.
- 로컬 Z 높이 `0.00150137` 이상에서 중앙 돌출부가 양초 테두리와 분리된다.
- 중앙 돌출부를 높이와 중심 반경으로 다시 나눠 모델 불꽃과 심지 파생 Mesh를 만들었다.
- 최종 모델 불꽃 선택: 삼각형 `193개`, 정점 `347개`
- 최종 심지 선택: 삼각형 `129개`, 정점 `315개`
- 첫 분할에서는 불꽃색이 너무 아래까지 내려와 심지가 촛농 테두리 뒤에 가려졌으므로 채택하지 않았다.
- 최종 분할에서는 불꽃 시작 높이를 로컬 Z `0.00215`로 올려 검은 심지가 불꽃 아래에 보이게 했다.
- 연결 요소와 높이별 군집 기록: `Smorzando_ModeledFlameGeometry.txt`
- 최종 분할 확대 진단: `Smorzando_ModeledFlameSegmentationPreview.png`

## 생성 에셋

- 모델 불꽃 파생 Mesh: `Assets/_Project/Art/Enemies/Smorzando/Models/Derived/Smorzando_Installed_ModeledFlame.asset`
- 심지 파생 Mesh: `Assets/_Project/Art/Enemies/Smorzando/Models/Derived/Smorzando_Installed_ModeledWick.asset`
- 모델 불꽃 재질: `Assets/_Project/Art/Enemies/Smorzando/VFX/Materials/Smorzando_ModeledFlame_Core.mat`
- 검은 심지 재질: `Assets/_Project/Art/Enemies/Smorzando/VFX/Materials/Smorzando_ModeledFlame_Wick.mat`
- 밀착 외곽 재질: `Assets/_Project/Art/Enemies/Smorzando/VFX/Materials/Smorzando_ModeledFlame_Envelope.mat`
- 갱신된 프리팹: `Assets/_Project/Art/Enemies/Smorzando/VFX/Prefabs/Smorzando_Installed_Flame.prefab`
- 원본 FBX는 덮어쓰지 않았다.

## 최종 불꽃 구조

- `ModeledFlameCore`: 원본 모델의 입체 불꽃 형상과 노란색·주황색 발광 재질
- `ModeledWick`: 같은 모델에서 추출한 하단 형상과 검은색 비발광 재질
- `FlameEnvelope`: `ModeledFlameCore`와 같은 Mesh를 `1.08배`로 감싼 반투명 가산 Shell
- 기존 `Outer_A`, `Outer_B`, `Core_A`, `Core_B` 교차 평면은 최종 프리팹에서 제거했다.
- 불꽃 Root 피벗은 모델 형상의 불꽃 시작점 `(0, -0.000039, 0.00215)`에 맞췄다.
- 두 번째 대기 개체의 모델 중심과 심지는 호흡하는 몸체 위치를 따라가며, 첫 번째·세 번째에서는 고정된 몸체 위치를 유지한다.
- 외곽 Shell의 작은 기울기·폭·높이 변화와 Point Light 깜박임은 몸체 모핑과 분리돼 설치형 세 개체 모두에 적용된다.
- Point Light는 범위 `2m`, 기본 세기 `0.45`, 밝기 흔들림 `12%`, 그림자 없음이다.

## Unity 적용 결과

- 적용 명령: `ApplySmorzandoHybridFlame`
- 대상: `Approved Smorzando Enemy Placement` 아래 설치형 3개
- 세 설치형 모두 동일한 하이브리드 프리팹을 사용한다.
- 불꽃 위상 `0초`, `1.0667초`, `2.1333초`를 유지했다.
- 몸체 호흡·물결·상하 움직임은 `Smorzando_Installed_02`에만 적용하고 첫 번째·세 번째 몸체는 정지한다.
- 호흡·물결·상하 움직임 수치는 변경하지 않았다.
- 기존 스모르찬도와 Player Transform은 변경하지 않았다.
- 적용 보고서: `Smorzando_HybridFlameApply.txt`

## 자동 시각 확인

- 캡처 명령: `CaptureSmorzandoHybridFlameFrames`
- 최종 Unity 렌더는 한 번 실행하고 결과를 이 폴더에 보존했다.
- 주요 확대 시트에서 모델의 다각형 불꽃이 주황색 중심으로 보이고 하단의 검은 심지가 네 시점 모두 유지되는 것을 직접 확인했다.
- 외곽 Shell은 모델 불꽃 윤곽을 바로 감싸며 옆으로 떨어진 별도 평면처럼 보이지 않는다.
- 네 시점에서 외곽 Shell의 기울기와 폭이 작게 달라져 공기 흐름 반응이 유지된다.
- 세 개체 비교 화면에서 세 불꽃이 각 심지 위치에 붙어 있고 위상 차이가 유지된다.
- 실제 Player Main Camera 화면에서도 설치형 3개의 불꽃과 따뜻한 주변광이 모두 보인다.
- 후속 몸체 범위 수정은 `../smorzando_installed_idle_2026-07-17/automated_visual_capture/Smorzando_InstalledIdle_BodyScope_T000.png`과 `Smorzando_InstalledIdle_BodyScope_T080.png`에서 확인했다.
- 반복 영상: `automated_visual_capture/Smorzando_HybridFlame_Loop.mp4`
- 불꽃 확대 시트: `automated_visual_capture/Smorzando_HybridFlame_CloseKeyframeSheet.png`
- 전체 주요 프레임: `automated_visual_capture/Smorzando_HybridFlame_KeyframeSheet.png`
- 세 개체 비교: `automated_visual_capture/Smorzando_InstalledIdle_ThreePhaseRow.png`
- 실제 시작 화면: `automated_visual_capture/Smorzando_InstalledIdle_PlayerView.png`
- 캡처 중 기존 장면의 다수 조명으로 그림자 아틀라스 해상도 축소 경고가 한 건 있었으나 새 불꽃 조명은 그림자를 사용하지 않으며 적용 오류는 없었다.
- 캡처 뒤 임시 Mesh를 복원했고 Scene View 포커스와 씬 저장을 하지 않았으며 Unity 선택을 해제했다.

## 실행하지 않은 항목

- 원본 FBX를 덮어쓰지 않았다.
- 기존 호흡·물결 모션 수치를 변경하지 않았다.
- 설치형 배치·회전·크기와 Player 시작점을 변경하지 않았다.
- 좀비형 애니메이션·재질과 다른 적대 개체를 변경하지 않았다.
- 변환, AI, 체력, 피격, 사망, 자폭, 전투 로직을 구현하지 않았다.
- Unity 재시작, 하네스 검증, 범위 밖 검증·테스트·빌드, Git 작업을 실행하지 않았다.
- 현재 상태는 사용자 시각 검토 대기이며 승인 완료로 처리하지 않는다.
