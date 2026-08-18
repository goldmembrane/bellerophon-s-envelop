# 이슈판트 발 고정 호흡형 대기 모션

상태: `PASS — Unity 직접 반복 재생 및 육안 확인`

## 적용 대상

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 대상: `Approved Ispant Enemy Placement/Ispant_02_Idle`
- 애니메이션 대상 본: `Ispant_New_Direct_Model/Armature/Hips`
- 접지 보정: `Ispant_New_GroundedBreathing_Rig`의 좌우 Two Bone IK
- 신규 클립: `Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_02_Idle_VerticalLoop_New.anim`
- 신규 Controller: `Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_02_Idle_VerticalLoop_New.controller`

## 새로 만든 모션

- 기존 `Ispant_02_Idle.anim`과 `Ispant_Idle.fbx`는 연결·복사·변형하지 않았다.
- 기존 대기 모션에서 가져온 클립·키·본 커브는 없고, 사용자 승인값인 2초 주기와 총 1.5cm 범위만 유지했다.
- `Hips`의 현재 자세를 기준으로 기준 → 7.5mm 위 → 기준 → 7.5mm 아래 → 기준의 다섯 키를 새로 작성했다.
- FBX 상위 Transform 축과 스케일을 고려해 월드 수직 7.5mm를 `Hips` 로컬 XYZ 성분으로 변환했다. 결과적으로 몸통은 수직으로만 움직이고 수평 드리프트는 없다.
- 양발 목표 위치와 회전은 슬롯 기준 고정점이며, `LeftUpLeg/LeftLeg/LeftFoot`와 `RightUpLeg/RightLeg/RightFoot`에 각각 Two Bone IK를 적용했다.
- 발은 고정점을 유지하고 골반 높이 변화에 따라 양 무릎이 굽힘·펴짐으로 반응한다.
- `LoopTime=True`, `ApplyRootMotion=False`, `AnimatorCullingMode=AlwaysAnimate`다.

## 직접 확인

- `Ispant_Idle_LiveLoop_VisualReview.png`는 실제 Unity AnimationMode와 RigBuilder 그래프를 함께 평가하며 0.5초 간격으로 캡처한 5패널 전신 육안 검토 이미지다.
- `Ispant_GroundedBreathing_KneeFoot_VisualReview.png`는 같은 캡처의 하체 확대본이다.
- 두 이미지를 직접 보며 발끝·발바닥이 같은 바닥 격자 위치를 유지하고, 골반·상체가 미세하게 오르내리며 양 무릎 각도가 위상별로 달라지는 것을 확인했다.
- 반복 종료 로그 기준 12주기를 완료했고 첫·마지막 자세가 이어졌다.
- 미리보기 종료 뒤 AnimationMode, RigBuilder 수동 그래프, Scene View 기즈모 설정과 편집 자세를 원래 상태로 복구했다.

## 보조 수치

- 몸통 수직 이동: `0.0149999857m`
- 양발 최대 위치 오차: 검사 `0.0000040954m`, 최종 라이브 종료 재검사 `0.0000021458m`
- 왼쪽 무릎 최대 회전 변화: 검사 `0.898739159°`, 최종 `0.899609566°`
- 오른쪽 무릎 최대 회전 변화: 검사 `2.11479831°`, 최종 `2.11516857°`

## 로그

- `Ispant_GroundedBreathing_Apply.log`
- `Ispant_GroundedBreathing_Inspection.log`
- `Ispant_GroundedBreathing_LiveReview_Start_03.log`
- `Ispant_GroundedBreathing_LiveReview_Stop_03.log`

## 실행하지 않은 항목

- `Run-HarnessValidation.ps1` 하네스 검증 및 관련 실행·재실행·조사·로그·문서·생성물 작업
- `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`, `Build-WindowsDev.ps1` 및 관련 작업
- 기존 대기 AnimationClip 삽입·복사·변형
- 발의 바닥 기준 위치 이동과 루트 모션
- 다른 이슈판트 슬롯, 모델·텍스처·머티리얼·장검, 이슈판트 배치 밖 씬 루트 수정
- Git 작업
