# Ostinato 모델 배치 기록

## 사용자 확정 범위

- `enemies model/ostinato.fbx`를 오스티나토 모델로 사용한다.
- 첫 개체는 스모르찬도 설치형 정적 개체의 `-Z` 방향에 둔다.
- 첫 개체의 Z 간격은 롱가 아르마–테르고 간격과 동일하게 한다.
- 오스티나토 사이 X 간격은 스모르찬도 인간형 1·2번의 X 간격과 동일하게 한다.
- 첫 개체부터 `+X` 방향으로 총 9개를 배치한다.
- 플레이어 시작 지점은 행 중앙에서 9개 전체의 정면을 바라보게 한다.

## 확정 애니메이션 목록

1. 정적 모델링
2. 일반 대기 모션
3. 일반 보행 모션
4. 일반 가위 절단 공격
5. 피격 모션
6. 포효 및 폭주 전환 모션
7. 착석 휴식 모션
8. 기립 모션
9. 사망 모션

이번 작업에서는 목록과 검토 개체 수만 확정했으며 애니메이션은 아직 연결하지 않았다.

## 원본과 Unity 에셋

- 원본: `enemies model/ostinato.fbx`
- Unity 복사본: `Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx`
- 원본·복사본 SHA-256: `35F85E29015DE71416F5A8DD76A86424451CCF89B1C1130AC7B690E6D8B1E533`
- FBX는 `char1` 단일 `SkinnedMeshRenderer`, 정점 `3,728개`, 서브메시 `1개`를 사용한다.
- 길이 `1.033333초`, `60fps` 보행 클립 한 개가 포함되어 있지만 이번 정적 배치에는 연결하지 않았다.

## 배치 결과

- 장면: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 루트: `Approved Ostinato Enemy Placement`
- 슬롯: `Ostinato_01_Static_Review`부터 `Ostinato_09_Static_Review`
- 모델 하위 이름: `Ostinato_Model`
- 롱가 아르마–테르고 Z 중심 간격: `7.376999`
- 스모르찬도 인간형 1·2번 X 간격: `2.444763`
- 첫 위치: `(57.86535, 0, -111.744)`
- 마지막 위치: `(77.42345, 0, -111.744)`
- 행 Bounds 중심: `(67.63515, 0.880595, -111.9881)`
- 행 Bounds 크기: `(21.32887, 1.76119, 1.49379)`
- FBX 원본 정면은 로컬 `+Z`다. 새 행이 기존 적대 개체 행의 끝에 있으므로 각 슬롯을 Y축 `180°` 회전해 정면을 월드 `-Z`로 향하게 했다.
- SkinnedMeshRenderer 바인드 포즈의 최초 최저점은 슬롯 바닥보다 `0.288056` 아래였다. 슬롯 좌표는 유지하고 각 `Ostinato_Model` 하위만 같은 수치만큼 올려 실제 렌더러 최저점을 바닥에 맞췄다.

## 플레이어 시작 지점

- 플레이어 위치: `(67.63515, 0, -126.1252)`
- 플레이어 회전: `(0, 0, 0)`
- 플레이어 전방: 월드 `+Z`
- 행 전방 거리: `13.39013`
- 카메라 수직 FOV `60°`, 화면비 `16:9`, 행 Bounds 폭을 기준으로 9개 전체가 들어오는 거리를 계산했다.
- X 간격 축소에 맞춰 플레이어를 기존 위치보다 약 `19.22`만큼 행 앞으로 옮기고 새 행 중앙에 다시 맞췄다.
- 보정 전에는 모델 앞면을 월드 `+Z`로 두어 플레이어와 오스티나토 사이에 기존 적대 개체들이 놓였다. 시각 확인 후 오스티나토 정면과 플레이어 위치를 행 외곽 `-Z` 쪽으로 바꿨다.

## 적용 및 시각 확인

- 조사 명령: `InspectOstinatoPlacementTarget`
- 접지 조사 명령: `InspectOstinatoAppliedGrounding`
- 적용 명령: `ApplyOstinatoPlacement`
- 최종 캡처 명령: `CaptureOstinatoPlacementFrames`
- 적용 기록에서 `PlacementCount=9`, `PositiveXOrder=True`, `UniformXSpacing=True`, `UniformGroundAlignment=True`, `OtherSceneRootsChanged=False`, `PlayerChildLocalTransformsChanged=False`를 확인했다.
- 최종 플레이어 시작 화면에서 가까워진 행의 중앙 정렬과 정면 방향을 확인했고, 격리 정면 행 화면에서 9개가 인간형 간격으로 겹침 없이 나열된 것을 직접 확인했다.
- 플레이어 시작 화면: `automated_visual_capture/Ostinato_PlayerStart_View.png`
- 격리 정면 행: `automated_visual_capture/Ostinato_Row_Front.png`
- 제공 FBX의 `Lit` 머티리얼은 현재 어두운 장면에서 어둡게 보인다. 이번 작업은 원본 모델 배치 범위이므로 머티리얼·텍스처는 변경하지 않았다.
- 캡처 중 기존 장면의 다수 광원으로 그림자 아틀라스 해상도 축소 경고가 기록됐지만 배치 오류는 없었다.

## 실행하지 않은 항목

- 모델링, 텍스처, 머티리얼, 애니메이션, Animator, AI, 물리, 충돌, 공격은 수정하거나 연결하지 않았다.
- 다른 적대 개체 배치와 승인 범위 밖 장면 루트는 변경하지 않았다.
- Unity 재시작, 별도 테스트·빌드, Git 작업은 실행하지 않았다.
