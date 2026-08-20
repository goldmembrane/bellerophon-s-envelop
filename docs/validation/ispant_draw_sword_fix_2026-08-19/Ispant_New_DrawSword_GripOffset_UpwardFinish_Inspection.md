# 이슈판트 발검 칼자루 외향 이동 및 칼날 상향 전환 최종 검사

## 적용 대상

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 대상: `Approved Ispant Enemy Placement/Ispant_04_DrawSword/Ispant_New_Direct_Model`
- Mixamo 원본: `Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_DrawSword_Source.fbx`
- 직접 모델: `Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx`
- 이 문서는 이전 `Ispant_New_DrawSword_BladeOpposite_Inspection.md`의 고정 칼날 방향 결과를 대체한다.

## 최종 적용 방식

- 칼자루 기준점을 오른손 손바닥 기준에서 칼날 반대쪽으로 `0.1m` 이동했다.
- 요청값 `0.1m`를 우선 적용하되, 손과 칼자루 형상의 도달 범위에서 `5mm` 접촉 여유를 제외한 한도를 넘으면 자동 축소하도록 구성했다. 현재 모델은 자동 축소 없이 `0.1m`가 적용됐다.
- 칼날은 발검 시작 시 기존 승인 방향인 `RightHand → RightForeArm`을 사용한다.
- 발검 전 구간에서 `SmoothStep` 진행률과 구면 보간을 사용해 칼날 방향을 자연스럽게 변경하고, 마지막 프레임에서 모델의 위쪽 축과 일치시킨다.
- 장검은 스키닝·메시 변형 없이 강체 `MeshRenderer`를 유지하며, Animator 갱신 뒤 `LateUpdate`에서 현재 오른팔·오른손 Transform을 따라 위치와 회전을 매 프레임 다시 계산한다.
- Mixamo 애니메이션은 `1.5초`, `60fps`, 정방향 전용이다. 마지막 프레임 다음에는 역재생 없이 첫 프레임으로 즉시 돌아간다.

## 검사 결과

- 결과: `PASS`
- 검사 프레임: 시작과 끝을 포함한 `91프레임`
- 적용 칼자루 외향 거리: `0.1m`
- 칼자루 거리 최대 오차: `0.000121459m`(약 `0.121mm`)
- 손·칼자루 최소 접촉 여유: `0.1295469m`
- 시작 칼날과 모델 위쪽 축의 각도: `58.58714°`
- 종료 칼날과 모델 위쪽 축의 각도: `0°`
- 프레임당 칼날 최대 각도 변화: `5.321118°`
- 칼날 방향이 변한 프레임: `90/90`
- 장검 최대 이동량: `1.511637m`
- 오른손 최대 이동량: `0.9560029m`
- 마지막 자세에서 첫 자세로 즉시 복귀하는 위치 차이: `0.7732029m`
- 마지막 자세에서 첫 자세로 즉시 복귀하는 각도 차이: `68.51866°`
- 역방향 프레임: 없음
- 장검 메시: `20,409` 정점, `19,950` 삼각형, 스키닝·블렌드셰이프 없음
- Unity AnimationMode 실시간 검토: `7회` 반복 후 정상 중지
- 재생 중지 후 Transform·씬 상태 복원: `PASS`
- 컨트롤러 씬 참조: `1개`
- 실시간 검 추종 컴포넌트 씬 참조: `1개`
- 다른 11개 이슈판트 슬롯 변경: 없음
- 이슈판트 배치 밖 씬 루트 변경: 없음
- 검사 중 씬 변경: 없음
- 새 캡처 생성: 없음

## 최종 무결성

- 씬 SHA-256: `58F4CD42F749B9F8577A6BF0082B101FEE87330D4902C9FFE35E9C729FD7DCAC`
- 직접 모델 FBX SHA-256: `5CE54F6117AF08F141BC18A0E46C823AD07877D815DA2906D59CA2967A4974FF`
- Mixamo 원본 FBX SHA-256: `EFF460E3201EFF5749A13705898B019C68036F25A7FEEFC9B18F7503FCEF1F81`
- 반복 클립 SHA-256: `23A4BAFDECB558F44312A30CCE61DD082EAE8B3321FF4EEFEA21AC4EBBDEB507`
- 컨트롤러 SHA-256: `83294D67E337CDA397000C59867939749DC1B405E8CB13580DF9947E1E3A97E3`
- 실시간 추종 코드 SHA-256: `7146FB004006D1BCDE36725166C8A85150677F2F1A715181E5209FFFEB607A75`
- 적용·검사 도구 SHA-256: `C67CC64A945B9585BDDDC416CDCFAEEBBDBB439D42772E1D7E75372469152241`
- Unity 브리지 SHA-256: `45F28CC6B2FE1371C16B018C5D175FD78F00CEBB81F09D8E7244B9AB6E332E74`
- 임시 롤백 표식: `0개`

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
