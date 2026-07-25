# 오스티나토 내려찍기 칼날 보정 검토

## 사용자 영상에서 확인한 원인

- 최종 검토 영상: `C:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-07-20 15-33-56.mp4`
- `0.333초` 부근에서 칼날 표면이 흰 파편처럼 여러 갈래로 찢어지고, `0.667초` 이후에는 칼날이 길게 늘어나 몸통을 가로질렀다.
- 원인은 스킨된 칼날 버텍스에 프레임별 BlendShape 역보정값을 넣고 정확 프레임 사이에서 서로 다른 델타를 보간한 구조였다. 개별 정수 프레임의 수치가 강체에 가까워도 중간 시간에는 버텍스 혼합으로 형태가 변할 수 있었다.
- 분석 이미지는 `user_capture_153356_contact_sheet.png`, `user_capture_153356_0333ms.png`, `user_capture_153356_0667ms.png`, `user_capture_153356_1000ms.png`, `user_capture_153356_1333ms.png`에 보존했다.

## 적용 결과

- 기존 칼날 BlendShape 보정 방식을 폐기했다.
- 몸체 SkinnedMesh에서 양쪽 주 칼날 표시 삼각형을 제외하고, 승인 모델의 원본 칼날 버텍스를 그대로 복제한 좌우 정적 Mesh 에셋을 생성했다.
- 좌우 칼날은 각각 `MeshFilter + MeshRenderer` 강체 개체이며 `SkinnedMeshRenderer`, 본 가중치, BlendShape를 사용하지 않는다.
- 칼날 루트는 `LeftHand`와 `RightHand` 아래의 실제 열린 경계 중심에 고정했다. 위치와 스케일 커브 없이 쿼터니언 회전 커브 4개씩만 적용한다.
- 왼쪽 칼날은 오른쪽, 오른쪽 칼날은 왼쪽 성분을 유지하면서 양쪽 모두 몸통 앞쪽으로 벌어져 몸체를 관통하지 않는다.
- `62~77프레임`에서 회전하고 `78~93프레임`에서 목표 각도를 유지하며 `94~99프레임`에서 원본 자세로 복귀한다.
- 기존 신체·손·전완 애니메이션 커브와 원본 FBX는 수정하지 않았다.

## 독립 점검 수치

- `WristInteriorPivotCaps=0`
- `AddedConnectorGeometry=0`
- `MaxBodyVertexDeviation=0`
- `MaxRigidBladeShapeDeviation=0.000002`
- `MaxRigidBladeEdgeLengthError=0.000001`
- `MaxRigidBladeLocalPositionDeviation=0`
- `MaxRigidBladeLocalScaleDeviation=0`
- `MaxHoldBladeHorizontalAngleDegrees=0.076968`
- `MinLeftBladeAnatomicalRightAlignment=0.672272`
- `MinRightBladeAnatomicalLeftAlignment=0.672353`
- `MinBladeFrontClearanceFromTorso=0.059232`
- `MaxExistingHandRotationDeviationDegrees=0`
- `MaxExistingForeArmRotationDeviationDegrees=0`
- `CorrectionRotationCurveCount=8`
- `BladeBlendShapeCount=0`
- `BladeSkinWeightCount=0`
- `BladeTransformPositionCurves=0`
- `BladeTransformScaleCurves=0`
- `SourceFbxModified=False`

세부 적용 수치는 `Ostinato_AttackDownstrikeBladeRotationApply.txt`, 독립 점검 수치는 `Ostinato_AttackDownstrikeBladeRotationInspection.txt`에 기록했다.

## 시각 확인

- 비교 순서: 원본 정면 | 교정 정면 | 원본 측면 | 교정 측면 | 원본 3/4 | 교정 3/4
- 정확 프레임: `50, 61, 62, 67, 72, 77, 78, 83, 93, 94, 99`
- 전체 비교: `Ostinato_AttackDownstrikeBladeRotationComparison.png`
- 개별 프레임: `exact_frames/`
- 최종 캡처에서 사용자 영상의 흰색 파편형 찢어짐, 길이 늘어남, 비강체 굽힘이 나타나지 않았다. 칼날은 모든 시점에서 동일한 외형을 유지하고 손목 루트와 함께 움직이며 수평 완료 구간에는 몸통 앞쪽에 배치된다.

## 실행하지 않은 항목

- 하네스 검증 및 관련 실행·재실행·조사·로그·문서·생성물 작업
- EditMode 테스트
- PlayMode 테스트
- 빌드
- Unity 재시작
- Git 작업
