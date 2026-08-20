# 이슈판트 발검 모션 정방향 반복 및 장검 방향 수정 검사

> 이 문서의 `RightForeArm → RightHand` 칼날 방향은 사용자 제공 11:55:33 영상 검토 후 반대 방향으로 수정됐다. 최종 상태는 `Ispant_New_DrawSword_BladeOpposite_Inspection.md`가 대체한다.

## 사용자 지적과 영상 확인

- 확인 영상: `C:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-08-19 11-39-18.mp4`
- 영상 길이: `5.7239초`, 해상도 `1920x1000`
- 확인용 프레임: `UserCapture_ContactSheet.png`, `UserCapture_DrawSword_Crop.png`
- 기존 결과는 1.5초 정방향 뒤 같은 자세를 역순으로 되돌리는 3초 루프였다.
- 기존 장검 축은 오른손에 칼자루를 맞췄지만 칼날이 몸통과 등 쪽으로 뻗어 신체 안쪽을 가로질렀다.
- 원인은 사용자 지시 없이 역방향 루프를 만든 것과 기존 발검 마운트의 장검 축을 현재 메시 축에 잘못 대응한 것이다.

## 수정 대상

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 슬롯: `Approved Ispant Enemy Placement/Ispant_04_DrawSword`
- 모델: `Ispant_New_Direct_Model`
- 원본 Take: `Ispant_New_DrawSword_Source.fbx/mixamo.com`
- 반복 클립: `Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_DrawSword_Loop.anim`
- 컨트롤러: `Assets/_Project/Art/Enemies/Ispant/Controllers/Ispant_New_DrawSword.controller`
- 실시간 추종 컴포넌트: `Assets/_Project/Runtime/Enemies/Ispant/IspantRigidSwordFollower.cs`

## 정방향 반복 방식

- 반복 클립은 원본 Mixamo Take와 같은 `1.5초`, `60fps`, `0–90프레임`이다.
- 원본의 모든 Transform 커브를 같은 시간에 그대로 복사했으며 역순 프레임과 장검 Transform 커브를 추가하지 않았다.
- 마지막 프레임 다음에는 첫 프레임으로 즉시 돌아간다. 마지막·첫 자세 차이는 장검 위치 `0.47708m`, 각도 `177.0693°`이며 이 차이를 보간하거나 역재생하지 않는다.
- Root Motion은 비활성화하고 Animator는 단일 반복 상태로 유지했다.

## 장검 실시간 강체 추종 방식

- `IspantRigidSwordFollower`는 `Ispant_04_DrawSword` 슬롯에 하나만 연결했다.
- Animator가 오른팔 본을 갱신한 뒤 `LateUpdate`에서 현재 `RightForeArm → RightHand` 방향을 매 프레임 다시 계산한다.
- 장검 메시의 칼날 방향인 로컬 `-X` 축을 오른팔 바깥 방향에 맞추고, 손목의 현재 회전으로 장검 롤 각도를 계산한다.
- 현재 오른손 가중 정점 중심을 손바닥 기준점으로 저장하고 장검 손잡이 중심을 같은 위치에 맞춘다.
- 장검 위치와 각도는 독립 애니메이션 커브가 아니라 현재 오른팔·오른손 Transform에서 직접 계산된다.
- 장검은 20,409정점·19,950삼각형의 기존 `MeshRenderer`이며 스키닝, 본 웨이트, 블렌드셰이프 또는 메시 변형을 추가하지 않았다.
- 새 직렬화 변수는 오른팔·오른손·장검 참조, 오른손 손바닥 로컬 기준점, 장검 칼자루 로컬 기준점, 장검 칼날·롤 로컬 축이며 모두 이 슬롯의 실시간 강체 추종에만 사용한다.

## 검사 결과

- 결과: `PASS`
- 원본과 반복 클립 길이·프레임률·전 91프레임 커브 일치: `PASS`
- 역방향 프레임: 없음
- 장검 Transform 애니메이션 커브: 없음
- 최대 칼자루–오른손 손바닥 거리: `0.000170872m`(약 `0.171mm`)
- 최대 칼날–오른팔 바깥 방향 각도: `0.01978234°`
- 최소 칼날 바깥 방향 내적: `0.9999999`
- 장검 최대 이동량: `1.383964m`
- 오른손 최대 이동량: `0.9560029m`
- 정방향 종료 후 즉시 복귀 위치 차이: `0.47708m`
- 정방향 종료 후 즉시 복귀 각도 차이: `177.0693°`
- Unity AnimationMode 라이브 검토: 9주기 반복 후 정지 및 Transform·씬 상태 복원 `PASS`
- 다른 이슈판트 슬롯 변경: 없음
- 이슈판트 배치 밖 씬 루트 변경: 없음
- 컨트롤러 씬 참조: 1개
- 실시간 추종 컴포넌트 씬 참조: 1개
- Unity 현재 스크립트 컴파일 실패 플래그: 없음(적용·검사·재생 명령의 컴파일 게이트 통과)

첫 컴파일에서는 추종 컴포넌트가 런타임 어셈블리 밖에 있어 Editor 어셈블리가 형식을 찾지 못했다. 씬 적용 전에 중단됐으며, 컴포넌트를 기존 `Bellerophon.Runtime` 어셈블리의 `Runtime/Enemies/Ispant` 경로로 옮긴 뒤 컴파일과 적용을 통과했다.

## 최종 무결성

- 사용자 원본과 Unity 복사본 SHA-256: `EFF460E3201EFF5749A13705898B019C68036F25A7FEEFC9B18F7503FCEF1F81`
- 현재 직접 모델 FBX SHA-256: `5CE54F6117AF08F141BC18A0E46C823AD07877D815DA2906D59CA2967A4974FF`
- 반복 클립 SHA-256: `23A4BAFDECB558F44312A30CCE61DD082EAE8B3321FF4EEFEA21AC4EBBDEB507`
- 컨트롤러 SHA-256: `EFDE62D17A6516E8B3591AACD6F8D36343AE574C254B0D5EB5366DA438D52A8C`
- 실시간 추종 코드 SHA-256: `0F94337C25C9CBF9E1A568F9DBDECEA0FF69397278BD89E8A041C46B7D531634`
- 최종 씬 SHA-256: `729EAAB81F4416425F521F188E79FBD6023DD035B3090D6DBB82F6E3CCBAC400`

## 실행하지 않은 항목

- `Run-HarnessValidation.ps1` 하네스 검증 및 관련 실행·재실행·조사·로그·문서·생성물 작업
- 역방향 애니메이션 생성 또는 재생
- 수동 재리깅·수동 본 매핑·Avatar 생성
- 원본 FBX 수정
- 장검 메시 변형·스키닝·분리·교체
- 다른 이슈판트 슬롯 및 배치 밖 씬 루트 수정
- AI·내비게이션·전투·물리 이동 구현
- 새 Unity 영상 또는 이미지 캡처 생성
- Git 작업
