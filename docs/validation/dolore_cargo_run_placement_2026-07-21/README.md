# 돌로레 CargoRunMvp 배치 기록

## 적용 결과

- 외부 원본 `D:/Bellerophon2/Bellerophon/enemies model/dolore.fbx`를 `Assets/_Project/Art/Enemies/Dolore/Models/Dolore.fbx`로 바이트 그대로 복사했다.
- 원본과 프로젝트 복사본의 SHA-256은 `0A8DF2A16B881B24A5FC856E2E3534A05D506049CE4C49975BB8433E71A2204E`로 일치한다.
- `CargoRunMvp` 씬에 `Approved Dolore Enemy Placement` 루트를 만들고 아래 7개 슬롯을 FBX 직접 인스턴스로 배치했다.
  1. `Dolore_01_Static_Review`
  2. `Dolore_02_Idle`
  3. `Dolore_03_Move_Quadruped`
  4. `Dolore_04_Tentacle_Stab_Attack`
  5. `Dolore_05_Execution_Pull_In`
  6. `Dolore_06_Hit_Reaction`
  7. `Dolore_07_Death`
- 이번 작업은 애니메이션 개수에 맞춘 정적 자리 준비이며 애니메이션은 적용하지 않았다.
- 각 슬롯은 FBX의 `char1` 렌더러만 표시하고 높이를 `1.8m`로 맞춰 지면에 정렬했다.

## 배치 수치

- Longa Arma Z: `-37.974`
- Tergo Z: `-45.351`
- 두 개체의 Z축 간격: `7.376999`
- Ostinato Z: `-111.744`
- Dolore 루트 위치: `(57.86535, 0, -119.121)`
- Dolore 슬롯 X축 간격: `2.444763`
- Player 시작 위치: `(65.25806, 0, -128.8872)`
- Player 전방: `(0, 0, 1)`

## 외형 확인

- 최종 캡처에서 7개 돌로레가 플레이어 정면의 한 줄 안에 모두 들어오는 것을 확인했다.
- 원본 FBX는 `output.fbm/None`을 참조하지만 실제 이미지 데이터가 제공되지 않았다. 사용자 지시에 따라 임의 아트 샘플, 대체 텍스처, 대체 머티리얼은 생성하지 않았으므로 표면은 흰색으로 표시된다.
- 최종 캡처: `Dolore_PlayerStartView.png`
- 적용·독립 점검 결과는 같은 폴더의 텍스트 보고서에 기록했다.

## 실행 제외

- `Run-HarnessValidation.ps1`, EditMode/PlayMode 테스트, 빌드, Unity 재시작, Git 작업은 실행하지 않았다.
