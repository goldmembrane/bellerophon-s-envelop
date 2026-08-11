# 아타 9개 배치 및 시작지점 정면 이동

## 적용 결과

- 지정 원본 `enemies model/attas.fbx`를 동일 해시로
  `Assets/_Project/Art/Enemies/Ata/Models/Ata.fbx`에 임포트했습니다.
- `CargoRunMvp`에 `Approved Ata Enemy Placement` 루트를 만들고 아타
  9개를 X축으로 나열했습니다.
- 아타 열 중심 X는 이슈판트 열 중심 X와 같은 `71.31155`입니다.
- 이슈판트의 실제 슬롯 X 간격 `2.444763`을 아타 9개에 그대로
  적용했습니다.
- 롱가 아르마 Z `-37.974`와 테르고 Z `-45.351`의 실제 간격
  `7.376999`를 이슈판트 Z `-170.76` 아래 방향으로 적용했습니다.
- 아타 열 중심 위치는 `(71.31155, 0, -178.137)`입니다.
- Player 시작 위치는 `(71.20419, 0, -190.1315)`로 이동했고 아타 열
  중앙을 정면으로 바라봅니다.
- 아타 원본 FBX의 기본 정면 방향과 크기·메시·머티리얼을 유지했습니다.
  애니메이션과 외형은 이번 작업에서 수정하지 않았습니다.

## 배치 개체

1. `Ata_01_Static`
2. `Ata_02_Idle`
3. `Ata_03_Move`
4. `Ata_04_PistolAimAndFire`
5. `Ata_05_Command`
6. `Ata_06_Sabotage`
7. `Ata_07_BombInstall`
8. `Ata_08_Hit`
9. `Ata_09_Death`

각 슬롯은 직접 임포트된 `Ata_Model` 하나를 하위 개체로 사용합니다.

## 직접 화면 확인

- 첫 진단 화면에서 이슈판트 열 아래에 아타 9개가 같은 X축 중심과
  간격으로 나열된 상태를 확인했습니다.
- 두 번째 진단 화면에서 실제 Player 카메라가 아타 9개 전체를 정면으로
  프레임 안에 담는 것을 확인했습니다.
- 두 진단 확인 후 최종 Player 시작 화면은 한 번만 생성해 원본
  해상도로 직접 확인했습니다.
- 이슈판트·롱가 아르마·테르고 Transform과 Player·아타 이외의 씬 루트는
  변경하지 않았습니다.

## 원본 보존

- 원본 및 프로젝트 사본 SHA-256:
  `CF7EE9DA3D4C3C00A8F26CE2F9D71FB165043C9BF6E0407CB9503DE1F51A795D`
- 원본 FBX는 수정하지 않았습니다.

## 결과 파일

- `Ata_Placement_Diagnostic_01.png`: 이슈판트–아타 배치 관계 진단
- `Ata_Placement_Diagnostic_02.png`: 실제 Player 시작 시점 진단
- `Ata_Placement_Final.png`: 최종 Player 시작 화면
- `RefreshAssets_AtaPlacement.log`
- `ApplyAtaNineSlotPlacement.log`
- `CaptureAtaNineSlotPlacementDiagnostic.log`
- `CaptureAtaNineSlotPlayerViewDiagnostic.log`
- `CaptureAtaNineSlotPlacementFinal.log`

## 실행하지 않은 항목

- 하네스 검증
- EditMode·PlayMode 테스트 및 Windows 빌드
- Ensure·Validate·Smoke 계열 명령
- 아타 애니메이션·메시·리그·머티리얼·텍스처 수정
- 이슈판트·롱가 아르마·테르고 Transform 수정
- Player 시작지점 이외의 플레이어 시스템 수정
- AI·전투·경로 탐색 구현
- Git 작업
