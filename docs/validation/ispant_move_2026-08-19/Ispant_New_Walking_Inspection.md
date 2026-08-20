# 이슈판트 신규 Mixamo 이동 모션 검사

## 적용 대상

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 슬롯: `Approved Ispant Enemy Placement/Ispant_03_Move`
- 기존 모델: `Ispant_New_Direct_Model`
- 사용자 원본: `enemies model/išpant-new walking.fbx`
- Unity 복사본: `Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_Walking_Source.fbx`
- 적용 클립: `Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_Walking_InPlace.anim`
- 컨트롤러: `Assets/_Project/Art/Enemies/Ispant/Controllers/Ispant_New_Walking.controller`

## 원본 및 호환성

- 사용자 원본과 Unity 복사본은 모두 7,740,064바이트이며 SHA-256은 `7132D83B27CD5C0C11D6D7F014F3138473312D5BC7645623C2EB86A6788B1C5A`로 일치한다.
- FBX의 두 Take 중 사용자가 지정한 Mixamo Take는 `mixamo.com` 하나이며 범위는 0–61프레임, 60fps, 길이는 1.016667초다.
- 다른 Take `Armature|Armature|Armature|Armature|walking_man|baselayer`는 임포트 대상에서 제외했다.
- 원본과 현재 직접 모델의 24본 이름·부모 계층은 정확히 일치한다. 두 모델 모두 Generic 리그이므로 Avatar 생성이나 수동 본 매핑 없이 동일 경로의 Mixamo 본 커브를 직접 연결했다.
- 현재 직접 모델 FBX SHA-256은 작업 전후 `5CE54F6117AF08F141BC18A0E46C823AD07877D815DA2906D59CA2967A4974FF`로 동일하다.

## 적용 방식

- Mixamo Take의 자세·본 회전·발 동작·골반 수직 바운스를 유지했다.
- 원본 골반 위치 커브는 로컬 X `-0.04292509..-0.007372902`, Y `-1.829834..0.01192618`, Z `0.8341644..0.9490986`였다.
- 슬롯 위치를 벗어나는 약 1.84m 전진을 막기 위해 locomotion 축인 로컬 X/Y만 첫 값으로 고정했다. 수직 축 Z는 변경하지 않았다.
- 별도 장검 메시에는 장면 전용 컴포넌트를 추가하지 않았다. 동일 Mixamo 골반 Transform에서 계산한 위치·회전 커브를 60fps로 적용 클립에 베이크해 골반을 따라가도록 했다.
- Animator는 현재 직접 모델 루트에 추가했으며 `Apply Root Motion=False`, `Always Animate`, 단일 루프 상태로 설정했다.
- 모델·메시·본·웨이트·머티리얼·텍스처·슬롯 Transform과 다른 11개 이슈판트 슬롯은 변경하지 않았다.

## 검사 결과

- 적용 결과: `PASS`
- 클립 길이: `1.016667초`
- 프레임률: `60fps`
- 반복: `True`
- 골반 수평 이동 범위: `0m`
- 골반 수직 이동 범위: `0.1025192m`
- 양발 최대 이동: `0.7628192m`
- 장검 최대 골반 상대 위치 오차: `0.000612372m`
- 장검 최대 골반 상대 각도 오차: `0°`
- 렌더러: 2개(`char1`, `Ispant_Approved_LongSword_10K`)
- 직접 FBX 씬 참조: 12개 유지
- 다른 이슈판트 슬롯 변경: 없음
- 이슈판트 배치 밖 씬 루트 변경: 없음
- 최근 컴파일 오류: 0개
- Unity AnimationMode 라이브 검토: 4주기 반복 후 정지 및 Transform 복구 `PASS`
- 최종 씬 SHA-256: `125A10C775EAA23E9B459B37B4F02F5D2A35A77B5DF3765AA669E199348ED142`

첫 적용 시 모델 프리팹 내부 장검 자식에 `ParentConstraint`를 추가할 수 없어 씬을 저장하지 않고 중단했다. 해당 미저장 변경을 기존 씬 상태로 되돌린 뒤 장검 추종을 클립 커브 방식으로 교체했으며, 최종 씬에는 `ParentConstraint`가 없다.

## 실행하지 않은 항목

- `Run-HarnessValidation.ps1` 하네스 검증 및 관련 실행·재실행·조사·로그·문서·생성물 작업
- 수동 재리깅, 수동 본 매핑, Avatar 생성
- 원본 FBX 재저장·변환
- 모델·메시·본·웨이트·UV·머티리얼·텍스처 수정
- 무기 메시 분리 또는 교체
- 다른 이슈판트 슬롯과 배치 밖 씬 루트 수정
- AI·내비게이션·전투·물리 이동 구현
- Git 작업
