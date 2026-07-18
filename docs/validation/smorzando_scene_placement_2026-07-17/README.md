# Smorzando CargoRunMvp 모델 배치 기록

## 사용자 지정 배치

- 기준 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 새 루트: `Approved Smorzando Enemy Placement`
- 첫 설치형은 Grave 루트의 X/Y를 유지하고, Z축 음의 방향으로 Longa Arma–Tergo 간격만큼 이동했다.
- 설치형 3개를 첫 위치부터 X축 양의 방향으로 배치했다.
- 세 번째 설치형 오른쪽부터 좀비형 5개를 같은 방향으로 이어 배치했다.
- 최종 개체 수는 설치형 3개와 좀비형 5개, 총 8개다.

## 원본과 Unity 에셋

- 설치형 원본: `enemies model/smorzando.fbx`
- 설치형 Unity 에셋: `Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Installed.fbx`
- 설치형 SHA-256: `4FE8F0D8F303A6D32D2F9737C36069B37E1FFF5F432F89D0957C69D0E41351DC`
- 좀비형 원본: `enemies model/smorzando person.fbx`
- 좀비형 Unity 에셋: `Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person.fbx`
- 좀비형 SHA-256: `CDE712400B52965CF0BE0E3D32FB5B59D9349C8C24F777CE7EEFA97233C75BF9`
- 두 원본과 Unity 복사본의 해시는 각각 일치한다.

## 임포트 단위와 축 보정

- 설치형 원본은 Unity에서 `0.018474 × 0.019022 × 0.005362m`로 들어와 좀비형에 비해 100분의 1 크기였다.
- 설치형 씬 인스턴스에만 `100배` 균일 스케일과 X축 `-90도` 보정을 적용했다.
- 최종 설치형 bounds는 `1.847359 × 0.536204 × 1.902153m`다.
- 좀비형은 원본 임포트 상태를 유지했으며 bounds는 `1.944767 × 2.495209 × 2.57404m`다.
- 최초 무보정 캡처는 설치형이 점처럼 보여 채택하지 않았다.
- X축 `+90도` 중간 캡처는 중앙 초가 아래로 향해 채택하지 않았다.
- 최종 `-90도` 결과에서 촛농 면이 바닥에 놓이고 중앙 초가 위로 향하는 것을 확인했다.
- FBX 파일 자체의 형상, 리그, 머티리얼, 텍스처 데이터는 수정하지 않았다.

## 최종 배치 수치

- 루트 위치: `(57.86535, 0, -104.367)`
- Longa Arma–Tergo Z 간격: `7.376999m`
- Grave–Smorzando Z 간격: `7.376999m`
- 인접 개체 최소 X bounds 간격: `0.5m`
- 렌더러 수: `8`
- 누락 머티리얼 수: `0`
- 적용 명령: `ApplySmorzandoScenePlacement`

## 자동 시각 확인

- 캡처 명령: `CaptureSmorzandoScenePlacementFrames`
- Unity Scene View 선택·포커스나 Play Mode 재생 없이 정면·사선·상단을 자동 렌더했다.
- 정면에서 설치형 3개 뒤에 좀비형 5개가 겹치지 않고 이어지는 것을 확인했다.
- 사선에서 설치형의 촛농 면이 바닥에 놓이고 중앙 초가 위로 솟은 방향을 확인했다.
- 상단에서 설치형 3개의 넓은 윤곽과 좀비형 5개의 분리된 간격을 확인했다.
- 접촉 시트: `automated_visual_capture/Smorzando_Placement_ContactSheet.png`
- 원본 캡처: `Smorzando_Placement_Front.png`, `Smorzando_Placement_ThreeQuarter.png`, `Smorzando_Placement_Top.png`
- 캡처 매니페스트: `automated_visual_capture/Smorzando_Placement_CaptureManifest.txt`
- 자동 캡처 후 Unity 선택과 Scene View 포커스를 남기지 않았다.

## 게임 실행 시작 위치

- Player 시작 위치를 스모르찬도 8개 전체 bounds의 정면으로 이동했다.
- 최종 Player 위치: `(66.77488, 0, -117.163)`
- 최종 실행 카메라 위치: `(66.77488, 1.62, -117.163)`
- 카메라 높이 `1.62m`와 Player 프리팹 내부의 카메라 로컬 Transform은 유지했다.
- 시선 대상: 스모르찬도 전체 bounds 중심 `(66.32657, 1.247604, -104.2744)`
- 대상까지 거리: `12.89647m`
- 실행 카메라 수직 FOV는 `60도`, 수평 FOV는 `91.493도`다.
- 전체 배치 폭과 카메라 FOV를 기준으로 설치형 3개와 좀비형 5개가 모두 화면에 들어오는 거리를 계산했다.
- 최초 이름 기반 카메라 검색은 실행 카메라가 Player 프리팹 내부에 있어 적용 전에 중단됐으며 씬을 변경하지 않았다.
- 최종 구현은 Player 자식 카메라 중 활성 `MainCamera` 태그를 우선 선택한다.
- 적용 명령: `MoveSmorzandoPlayerStartToFront`
- 실제 Main Camera 캡처 명령: `CaptureSmorzandoPlayerStartView`
- 실제 시작 화면에서 스모르찬도 8개가 가장 가까운 중앙 행에 정면으로 들어오는 것을 확인했다.
- 기존 적대 개체 검토 행은 이동하거나 숨기지 않았으므로 스모르찬도 뒤쪽 배경에 그대로 보인다.
- 시작 화면: `player_start_view/Smorzando_PlayerStart_MainCamera.png`
- 시작 화면 매니페스트: `player_start_view/Smorzando_PlayerStart_CaptureManifest.txt`
- 캡처는 Play Mode와 Scene View 포커스를 사용하지 않고 저장된 실행 Main Camera로 렌더했다.
- 시작점 적용 및 캡처 뒤 Unity 선택과 Scene View 포커스를 남기지 않았다.

## 실행하지 않은 항목

- 스모르찬도 애니메이션과 설치형→좀비형 변환 기능을 구현하지 않았다.
- AI, 체력, 피격, 자폭, 전투 로직을 연결하지 않았다.
- 기존 적대 개체 배치를 변경하지 않았다.
- Unity 재시작, 하네스 검증, 범위 밖 검증·테스트·빌드, Git 작업을 실행하지 않았다.
- 현재 상태는 사용자 시각 검토 대기이며 승인 완료로 간주하지 않는다.
