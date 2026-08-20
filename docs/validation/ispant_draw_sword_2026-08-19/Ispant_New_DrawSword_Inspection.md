# 이슈판트 신규 Mixamo 발검 모션 검사

## 적용 대상

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 슬롯: `Approved Ispant Enemy Placement/Ispant_04_DrawSword`
- 현재 모델: `Ispant_New_Direct_Model`
- 사용자 원본: `enemies model/išpant-new draw sword.fbx`
- Unity 복사본: `Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_DrawSword_Source.fbx`
- 반복 클립: `Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_DrawSword_Loop.anim`
- 컨트롤러: `Assets/_Project/Art/Enemies/Ispant/Controllers/Ispant_New_DrawSword.controller`

## 원본 및 리그 호환성

- 사용자 원본과 Unity 복사본은 모두 7,776,608바이트이며 SHA-256은 `EFF460E3201EFF5749A13705898B019C68036F25A7FEEFC9B18F7503FCEF1F81`로 일치한다.
- 사용한 Mixamo Take는 `mixamo.com` 하나이며 범위는 0–90프레임, 60fps, 길이는 1.5초다.
- 다른 Action `Armature|Armature|Armature|Armature|walking_man|baselayer`는 적용 대상에서 제외했다.
- 원본과 현재 직접 모델은 동일한 이름·부모 계층의 Generic 24본 리그다. Avatar 생성이나 수동 본 매핑 없이 같은 본 경로의 Mixamo 커브를 직접 사용했다.
- 현재 직접 모델 FBX SHA-256은 작업 전후 `5CE54F6117AF08F141BC18A0E46C823AD07877D815DA2906D59CA2967A4974FF`로 동일하다.

## 반복 및 장검 연결 방식

- 원본 Mixamo Take의 시작·끝은 오른손 위치 차이 `0.6591913m`, 오른손 각도 차이 `177.066°`, 골반 위치 차이 `0.2041335m`, 골반 각도 차이 `38.57386°`로 직접 반복 시 큰 끊김이 발생하는 상태였다.
- 공급된 Mixamo 자세를 유지하면서 반복 이음새를 맞추기 위해 원본 90프레임을 순방향으로 재생한 뒤 같은 프레임을 역순으로 재생하는 3초 루프를 만들었다.
- 현재 장검 `Ispant_Approved_LongSword_10K`의 손잡이 영역 중심과 현재 신체 메시의 `RightHand` 가중 정점 중심을 기준으로 칼자루가 오른손 손바닥에 놓이도록 계산했다.
- 기존 승인 발검 장검 마운트의 방향을 기준으로 장검 로컬 축을 맞췄으며, 매 프레임 `RightHand` 회전에 종속되는 위치·회전 Transform 커브를 60fps로 베이크했다.
- 장검은 별도 `MeshRenderer`로 유지했다. `SkinnedMeshRenderer`, 본 웨이트, 블렌드셰이프 또는 제약 컴포넌트를 장검에 추가하지 않았다.
- Animator는 현재 모델 루트에 연결했고 `Apply Root Motion=False`, `Always Animate`, 단일 반복 상태로 설정했다.
- 원본 FBX, 모델, 메시, 본, 웨이트, UV, 머티리얼, 텍스처와 다른 11개 이슈판트 슬롯은 변경하지 않았다.

## 검사 결과

- 적용 결과: `PASS`
- 반복 클립 길이: `3초`
- 프레임률: `60fps`
- 반복: `True`
- Root Motion: `False`
- 전 181프레임 최대 칼자루–오른손 손바닥 거리: `0.000015263m`(약 `0.015mm`)
- 전 181프레임 손 대비 장검 상대 각도 최대 편차: `0°`
- 장검 최대 이동량: `1.562799m`
- 오른손 최대 이동량: `0.9560029m`
- 루프 시작·끝 위치 차이: `0m`
- 루프 시작·끝 각도 차이: `0°`
- 장검: 20,409정점, 19,950삼각형, 비스키닝 메시 유지
- 다른 이슈판트 슬롯 변경: 없음
- 이슈판트 배치 밖 씬 루트 변경: 없음
- Unity 현재 스크립트 컴파일 실패 플래그: 없음(검사·재생 명령의 컴파일 게이트 통과)
- Unity AnimationMode 라이브 검토: 6주기 반복 후 정지, Transform 및 씬 상태 복구 `PASS`
- 최종 씬 SHA-256: `E07192ED87DD9225E262C72847CE869496B1537BE3AF9B190D7B70156B234FB5`

## 생성 에셋 식별자

- 원본 FBX 복사본 GUID: `5ae9f9ca11668c34798a365203f84fbe`
- 반복 클립 GUID: `608576b0e47b5994e935f927ae8124a4`, SHA-256 `3AF073617C412E18A9B3F698C62E3914BC6E6121537EF6F747699FEBBA49953D`
- Animator Controller GUID: `4a8d770eaccb99b4e975db29b687c40e`, SHA-256 `9BD5A748BEBECAFCC57968868E8F8E841A3AB19DEA2278DE0B5E04FFD4F7C88F`

## 실행하지 않은 항목

- `Run-HarnessValidation.ps1` 하네스 검증 및 관련 실행·재실행·조사·로그·문서·생성물 작업
- 수동 재리깅·수동 본 매핑·Avatar 생성
- 원본 FBX 재저장·변환
- 모델·메시·본·웨이트·UV·머티리얼·텍스처 수정
- 장검 메시 분리·교체 또는 장검 스키닝
- 다른 이슈판트 슬롯과 배치 밖 씬 루트 수정
- AI·내비게이션·전투·물리 이동 구현
- 캡처 생성
- Git 작업
