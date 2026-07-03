# 통제실 오브젝트 목록

작성일: 2026-06-21

이 문서는 원본 기획서 `docs/GAME_DESIGN_SOURCE.txt`와 구현 순서 문서 `docs/MVP_IMPLEMENTATION_ORDER.md`를 기준으로 통제실 모델링에 필요한 구조와 오브젝트를 정리한 작업 목록입니다. 실제 Unity 씬, 프리팹, 런타임 자산에 반영하기 전에는 필요한 항목별로 `artSample/`에서 검사 가능한 샘플을 만들고 사용자 승인을 받아야 합니다.

## 범위

- 포함: 통제실 내부 구조, 통제실에 필요한 스크린과 표시 장치, CCTV/구역 상태/침입 통제/복도 폐쇄 관련 시각 오브젝트, 상호작용 앵커.
- 제외: 조종실, 동력실, 무기실, 비품실, 이송창고 자체 오브젝트의 모델링, 실제 게임 로직 구현, Unity 배치, 테스트 실행.
- 기준: 원본에 명시된 항목은 확정으로 기록하고, 원본 기능에는 필요하지만 구체 외형이 명시되지 않은 항목은 확인 필요로 분리한다.

## 원본 근거 요약

- 화물선은 조종실, 이송창고, 무기실, 비품실, 동력실, 통제실 6개 구역과 이들을 잇는 복도로 구성된다.
- 모든 구역은 이송창고와 연결되며, 통제실은 동력실 및 무기실과도 복도 연결이 있다. 조종실과 통제실 사이의 복도 연결도 원본 구조에 포함된다.
- 통제실에는 대형 스크린, 가로형 스크린, 세로형 스크린 여러 개가 있는 구조가 필요하다.
- 통제실 스크린은 구역 개수, 구역명, 구역 내구도 색상 표시를 제공해야 한다.
- 통제실 CCTV는 조종실, 이송창고, 동력실, 무기실 순서 전환을 1차 대응 대상으로 한다.
- 통제실 손상 단계에 따라 복도 폐쇄, CCTV 사용 불가, 침입 감지 시스템 비활성화, 화물 손상 경고 시스템 비활성화가 발생한다.

## 확정 오브젝트 목록

| ID | 오브젝트 | 원본 근거 | 모델링/배치 메모 | 상태 |
| --- | --- | --- | --- | --- |
| CR-01 | 통제실 룸 셸 | 6구역 중 통제실이 존재한다. | 천장은 제외하고 바닥, 벽, 내부 가벽, 가벽 문, 방향별 출입구와 복도 스텁을 가진 독립 구역으로 Unity에 반영했다. 현재 Unity 편집 상태는 복구 스크립트에 캡처 반영했다. | Unity 반영 완료 / 현재 편집 상태 복구 반영 완료 |
| CR-02 | 이송창고 방향 출입구 | 모든 구역은 이송창고와 연결된다. | 통제실 6시 방향에서 무기실 복도와 바로 붙어 있는 이송창고 방향 출입구와 복도 스텁을 CR-01 셸에 포함해 Unity에 반영했다. | Unity 기본 구조 반영 완료 |
| CR-03 | 조종실 방향 출입구 | 조종실과 통제실 사이 복도 연결이 존재한다. | 위에서 내려다본 기준 왼쪽 영역에 40도 기울어진 조종실 방향 출입구와 외부 복도 스텁을 CR-01 셸에 포함해 Unity에 반영했다. | Unity 기본 구조 반영 완료 |
| CR-04 | 동력실 방향 출입구 | 동력실과 통제실 사이 복도 연결이 존재한다. | 조종실 복도와 떨어진 왼쪽 아래 영역에 동력실 방향 출입구와 복도 스텁을 CR-01 셸에 포함해 Unity에 반영했다. 동력실 오브젝트는 수정하지 않는 전제로 관리한다. | Unity 기본 구조 반영 완료 |
| CR-05 | 무기실 방향 출입구 | 통제실과 무기실 사이 복도 연결이 존재한다. | 통제실 6시 방향에서 이송창고 복도와 바로 붙어 있는 무기실 방향 출입구와 복도 스텁을 CR-01 셸에 포함해 Unity에 반영했다. | Unity 기본 구조 반영 완료 |
| CR-06 | 대형 메인 스크린 | 원본에서 통제실 대형 스크린을 요구한다. | CR-01 셸의 전면 벽에 빈 대형 메인 스크린 베이와 주변 구조물을 Unity에 반영했다. 실제 구역 상태/CCTV/경고 UI는 아직 연결하지 않았다. | Unity 기본 구조 반영 완료 / 실제 UI 미구현 |
| CR-07 | 가로형 보조 스크린 | 원본에서 가로형 스크린을 요구한다. | 대형 메인 스크린보다 조금 더 위쪽의 오른쪽 바깥 영역에 붙는 보조 표시 장치다. 장착 위치, 프레임, 케이블, `C2_ElC2Disp.png` 디스플레이 적용 상태를 Unity에 반영했다. | Unity 반영 완료 |
| CR-08 | 세로형 보조 스크린 묶음 | 원본에서 세로형 스크린 여러 개를 요구한다. | 승인된 `artSample/control_room_vertical_aux_screens/` 샘플을 기준으로 CR-06 왼쪽 보조 베이에 붙는 폭을 대폭 줄이고 높이를 확실히 늘린 3개 세로 패널 묶음을 별도 루트 `Approved Control Room 08 Vertical Aux Screens`로 Unity에 반영했다. 실제 UI 내용은 아직 더미 표시다. | Unity 반영 완료 / 실제 UI 미구현 |
| CR-09 | 구역 상태 표시 화면 | 통제실 스크린은 구역 개수, 구역명, 구역 내구도 색상 표시를 제공한다. | 조종실, 이송창고, 무기실, 비품실, 동력실, 통제실 6구역 상태를 색상 단계로 보여주는 화면이다. 사용자 확인에 따라 현재 모델링 작업에서는 스킵하고 UI 작업으로 분리한다. | 모델링 스킵 / UI 작업 대기 |
| CR-10 | CCTV 피드 화면 | 통제실 CCTV는 조종실, 이송창고, 동력실, 무기실 순서 전환을 우선 대상으로 한다. | 화면 안에 카메라 피드 프레임 또는 모니터 묶음으로 표현한다. 실제 영상 처리 로직은 별도 구현 대상이며, 현재 모델링 작업에서는 스킵한다. | 모델링 스킵 / UI 작업 대기 |
| CR-11 | CCTV 전환 조작부 | CCTV 순서 전환 기능을 통제실에서 사용한다. | 스크린/통제 UI 조작 흐름에 포함되는 항목으로 보고 현재 모델링 작업에서는 스킵한다. | 모델링 스킵 / UI 작업 대기 |
| CR-12 | 복도 폐쇄 상태 표시 | 통제실 손상에 따라 복도 폐쇄 비율이 달라진다. | 폐쇄 구역을 보여주는 미니맵, 문 잠금 표시, 경고 램프 중 하나로 표현한다. 실제 복도 폐쇄 로직과 UI 작업으로 분리한다. | 모델링 스킵 / UI 작업 대기 |
| CR-13 | 침입 감지 시스템 표시 | 통제실 손상 50% 이하에서 침입 감지 시스템이 비활성화된다. | 시스템 on/off를 보여주는 보안 패널 또는 경고 표시다. 현재 모델링 작업에서는 스킵하고 UI 작업으로 분리한다. | 모델링 스킵 / UI 작업 대기 |
| CR-14 | 화물 손상 경고 표시 | 통제실 손상 25% 이하에서 화물 손상 경고 알림 시스템이 비활성화된다. | 화물 손상 경고가 정상/고장 상태로 바뀌는 표시 장치다. 현재 모델링 작업에서는 스킵하고 UI 작업으로 분리한다. | 모델링 스킵 / UI 작업 대기 |
| CR-15 | 통제실 손상 상태 시각 요소 | 통제실 내구도 단계에 따라 CCTV, 침입 감지, 경고 시스템이 망가진다. | 깨진 화면, 꺼진 모니터, 빨간 경고등, 잠금 해제 표시 등 단계별 상태 표현이다. 현재 모델링 작업에서는 스킵하고 UI 작업으로 분리한다. | 모델링 스킵 / UI 작업 대기 |
| CR-16 | 상호작용 앵커 | 통제실 스크린과 CCTV는 플레이어 상호작용 대상이다. | 사용자 확인 기준 조작 위치는 스크린 앞 지점이다. 독립 모델링 대상이 아니라 상호작용 기준점으로 관리한다. | 모델링 스킵 / 조작 위치 확인 완료 |
| CR-17 | 복도 방향 표시 레이블 | 각 방과 복도 연결 방향을 플레이 중 확인할 수 있어야 한다. | 승인된 `artSample/control_room_direction_labels/` 샘플을 기준으로 COCKPIT, ENGINE ROOM, CARGO HOLD, ARMORY 영어 메인 표기와 한국어 보조 표기를 가진 벽면 레이블/바닥 화살표를 별도 씬 루트 `Approved Control Room 17 Direction Labels`로 Unity에 반영했다. 글자 누락 문제는 폰트 텍스처가 있는 TextMesh 머티리얼로 교체했고, 긴 영어 표기가 라벨 패널 안에 들어가도록 패널 폭 기준 텍스트 맞춤 검사를 추가했다. 기존 조종실, 동력실, 통제실 오브젝트는 수정하지 않는 조건으로 적용했다. | Unity 반영 완료 / 글자 표시 및 라벨 맞춤 수정 완료 |

## 구현 진행상황 2026-06-21

- CR-01은 승인된 `artSample/control_room_shell/` 샘플을 기준으로 `Approved Control Room 01 Shell` 루트에 Unity 반영했다.
- CR-01 현재 Unity 편집 상태는 `artSample/control_room_shell/editor_current/cr01_current_objects.md`에 캡처했고, `ApprovedControlRoomShellBootstrap.cs`의 복구 블록에 반영했다.
- CR-02, CR-03, CR-04, CR-05는 CR-01 셸 내부의 방향별 출입구, 방향 표시, 외부 복도 스텁으로 기본 구조까지 Unity에 반영했다. 실제 방 이동/복도 폐쇄 게임 로직은 아직 연결하지 않았다.
- CR-06은 통제실 전면의 빈 대형 메인 스크린 베이와 주변 구조물까지만 Unity에 반영했다. 실제 상태 화면, CCTV 화면, 경고 UI는 아직 미구현이다.
- CR-07은 승인된 `artSample/control_room_aux_screen/` 샘플을 기준으로 별도 루트 `Approved Control Room 07 Aux Screen`에 Unity 반영했다. `C2_ElC2Disp.png` 디스플레이 적용 상태까지 반영했다.
- CR-08은 승인된 `artSample/control_room_vertical_aux_screens/` 샘플을 기준으로 씬 루트의 별도 오브젝트 `Approved Control Room 08 Vertical Aux Screens`에 Unity 반영했다. 기존 CR-01, CR-07, 동력실, 조종실 오브젝트는 수정하지 않는 조건으로 적용했다.
- CR-09~CR-15는 사용자 확인에 따라 현재 모델링 작업에서 스킵하고 이후 UI 작업으로 분리한다.
- CR-16은 스크린 앞 조작 지점인 상호작용 앵커로 확인됐으며, 현재 모델링 작업 대상에서 제외한다.
- CR-17은 승인된 `artSample/control_room_direction_labels/` 샘플을 기준으로 씬 루트의 별도 오브젝트 `Approved Control Room 17 Direction Labels`에 Unity 반영했다. 기존 CR-01, CR-07, CR-08, 동력실, 조종실 오브젝트는 수정하지 않는 조건으로 적용했다.

## 상태별 표현 목록

| 상태 | 필요한 표현 | 관련 ID |
| --- | --- | --- |
| 정상 | 모든 주요 스크린 점등, CCTV 사용 가능, 구역 내구도 표시 정상, 복도 폐쇄 없음 | CR-06, CR-09, CR-10, CR-12 |
| 통제실 내구도 75% 이하 | 복도 20% 폐쇄 상태를 표시 | CR-12 |
| 통제실 내구도 50% 이하 | 복도 50% 폐쇄, 5구역 중 2구역 CCTV 사용 불가, 침입 감지 시스템 비활성화 | CR-10, CR-12, CR-13 |
| 통제실 내구도 25% 이하 | 복도 90% 폐쇄, 5구역 CCTV 사용 불가, 화물 손상 경고 시스템 비활성화 | CR-10, CR-12, CR-14 |
| 통제실 내구도 0% | 폐쇄 구역 전체 개방, 외부/내부 침입 통제 시스템 비활성화 | CR-12, CR-13, CR-15 |

## 모델링 우선순위

1. CR-09~CR-15: 현재 모델링 작업에서는 스킵하고 이후 UI 작업 범위에서 다룬다.
2. CR-16: 독립 모델링이 아니라 스크린 앞 상호작용 기준점으로 관리한다.

## 샘플 제작 전 확인 필요 항목

- 세로형 스크린의 정확한 개수와 배치 방식.
- 대형 메인 스크린이 구역 상태 화면인지, CCTV 메인 피드인지, 별도 통합 화면인지.
- CCTV 전환 조작부를 물리 콘솔로 만들지, 스크린 자체 UI로만 처리할지.
- 통제실의 전체 분위기를 군용 관제실, 산업용 설비실, 낡은 우주선 터미널 중 어느 쪽으로 확정할지.

## Unity 반영 전 주의사항

- 이 문서는 목록과 구현 진행상황 정리 문서다. 현재 CR-01~CR-08, CR-17 항목은 Unity에 반영됐으며, 나머지 항목은 표의 상태를 기준으로 별도 범위 승인 후 진행한다.
- 통제실 모델링과 UI 성격이 강한 스크린 작업은 먼저 `artSample/`에 검사 가능한 샘플로 만들어야 한다.
- 샘플 승인 전에는 `CargoRunMvp` 씬, 런타임 프리팹, UI 흐름, 상호작용 로직에 연결하지 않는다.
- 승인된 샘플을 Unity에 반영할 때는 오브젝트 존재 여부가 아니라 승인 샘플과의 시각적 일치도를 기준으로 확인한다.
- 통제실 작업은 기존 동력실 ER-01~ER-20 배치와 조종실 오브젝트를 임의로 수정하지 않는 별도 범위로 진행한다.

## 사용자 확인 구조 변경 2026-06-21

- 사용자는 통제실을 원본 기획서의 기본 연결 구조에서 일부 변형한다고 확인했다.
- CR-01 통제실 룸 셸에는 스크린 쪽과 입구 쪽을 나누는 내부 가벽이 있어야 한다.
- 내부 가벽에는 출입 가능한 문이 달려 있어야 한다.
- 통제실을 위에서 내려다본 기준으로 조종실과 동력실 방향 복도는 왼쪽에 몰려 있어야 한다.
- 조종실 쪽 문은 약 45도 정도 기울어진 형태여야 한다.
- 조종실 복도와 동력실 복도는 서로 조금 떨어진 구조여야 한다.
- 통제실을 위에서 내려다본 기준으로 6시 방향에는 운송창고로 가는 복도와 무기실로 가는 복도가 바로 옆에 붙어 있어야 한다.
- 운송창고 복도와 무기실 복도는 서로 붙어 있는 구조여야 한다.
- 이후 사용자는 가벽 위치를 옮기는 것이 아니라, 통제실 전체 면적을 6시 방향으로 늘려 가벽 아래쪽 입구 영역을 더 넓히라고 정정했다.
- 확장된 아래쪽 입구 영역 안에 조종실 복도와 동력실 복도가 모두 들어가도록 CR-01 샘플을 조정한다.
- 이 구조는 CR-01 샘플부터 반영하며, 실제 Unity 반영은 샘플 승인 후 별도 승인 범위에서 진행한다.

## Unity 반영 기록 2026-06-21

- 사용자가 승인한 `artSample/control_room_shell/` CR-01 샘플을 `CargoRunMvp` 씬에 반영했다.
- Unity 루트 이름은 `Approved Control Room 01 Shell`이다.
- 반영 범위는 CR-01 통제실 룸 셸, 내부 가벽/문, 방향별 출입구/복도 스텁, 색상 방향 표시, 빈 메인 스크린 벽면 베이, 바닥 패널/리브로 제한했다.
- 동력실 오브젝트와 조종실 오브젝트는 보호 대상으로 처리하고, 적용 로그에서 `EngineRoomUntouched=True`, `CockpitUntouched=True`를 확인했다.
- 배치 로그에서 `ControlRoomPlacedNextToCockpit=True`, `ControlRoomOverlapsEngineRoom=False`, `ControlRoomOverlapsCockpit=False`를 확인했다.
- 이번 반영은 샘플 승인된 CR-01 쉘 구현이며, CR-06 이후의 실제 메인 스크린 UI, CCTV 전환, 구역 상태 로직, 복도 폐쇄 로직은 아직 구현하지 않았다.

## CR-07 샘플 제작 기록 2026-06-21

- 사용자는 CR-01 작업 중 CR-02, CR-03, CR-04, CR-05, CR-06을 함께 진행했다고 확인하고, 대형 메인 스크린 오른쪽 상단에 넣을 CR-07 샘플 제작을 요청했다.
- 샘플 위치:
  - `artSample/control_room_aux_screen/`
- 생성 스크립트:
  - `scripts/GenerateControlRoomAuxScreenSample.py`
  - `scripts/Run-ControlRoomAuxScreenSample.ps1`
- 샘플 구성:
  - CR-06 대형 메인 스크린을 위치 기준용 배경으로 둔다.
  - CR-07 가로형 보조 스크린은 CR-06보다 조금 더 위쪽의 오른쪽 바깥 영역에 붙어 보이도록 배치한다.
  - 장갑 프레임, 벽면 장착 패드, 진동 방지 가스켓, 케이블 소켓, 서비스 래치, `C2_ElC2Disp.png`가 꽉 찬 화면 표면을 포함한다.
  - 실제 구역 상태 UI, CCTV 피드, 상호작용 로직, Unity 배치는 포함하지 않는다.
- 생성 산출물:
  - `artSample/control_room_aux_screen/index.html`
  - `artSample/control_room_aux_screen/README.md`
  - `artSample/control_room_aux_screen/ASSET_MANIFEST.json`
  - `artSample/control_room_aux_screen/APPROVAL_STATUS.json`
  - `artSample/control_room_aux_screen/blender/control_room_aux_screen.blend`
  - `artSample/control_room_aux_screen/exports/control_room_aux_screen.fbx`
  - `artSample/control_room_aux_screen/exports/control_room_aux_screen.glb`
  - `artSample/control_room_aux_screen/renders/01_context_overview.png`
  - `artSample/control_room_aux_screen/renders/02_front_alignment.png`
  - `artSample/control_room_aux_screen/renders/03_aux_screen_closeup.png`
  - `artSample/control_room_aux_screen/renders/04_side_mount_depth.png`
  - `artSample/control_room_aux_screen/renders/05_top_right_relation.png`
- Unity 반영은 아직 하지 않았고, 사용자 승인 전까지 `CargoRunMvp` 씬, 프리팹, 런타임 UI 흐름에 연결하지 않는다.

## CR-07 샘플 재배치 기록 2026-06-21

- 사용자가 CR-07이 메인 스크린을 가리지 않게 다시 배치하라고 요청했다.
- `scripts/GenerateControlRoomAuxScreenSample.py`에서 CR-07 중심 위치를 오른쪽으로 이동해, 장착 패드와 좌측 브래킷까지 CR-06 표시 면 바깥에 놓이도록 조정했다.
- 오른쪽 벽면 배치에 맞춰 배경 벽 패널, 케이블 레이스웨이, 서비스 심, 렌더 카메라 구도를 함께 조정했다.
- `artSample/control_room_aux_screen/` 산출물을 재생성했고, 대표 렌더에서 CR-06 대형 메인 스크린 표시 면을 침범하지 않는 것을 확인했다.
- Unity 반영은 하지 않았다.

## CR-07 상단 배치 및 디스플레이 에셋 적용 기록 2026-06-21

- 사용자가 가로 보조 스크린을 메인 스크린보다 조금 더 위에 자리 잡도록 위치를 수정하고, 디스플레이에는 `C2_ElC2Disp.png` 에셋을 꽉 차게 넣으라고 요청했다.
- 사용한 에셋:
  - `Assets/Heavy Station Kit/_common/Textures/GUI/C2_ElC2Disp.png`
- `scripts/GenerateControlRoomAuxScreenSample.py`에서 CR-07 중심 높이를 CR-06 대형 메인 스크린 상단보다 위로 올렸다.
- 기존 임시 구획선 대신 `C2_ElC2Disp.png`를 이미지 텍스처 머티리얼로 로드하고, CR-07 디스플레이 면 전체에 UV 0~1로 매핑했다.
- 올라간 배치에 맞춰 배경 벽 패널, 케이블 레이스웨이, 렌더 카메라 구도를 조정했다.
- `artSample/control_room_aux_screen/` 산출물을 재생성했고, 대표 렌더에서 CR-07이 메인 스크린보다 위쪽에 있으며 디스플레이 에셋이 화면 전체에 들어간 것을 확인했다.
- Unity 반영은 하지 않았다.

## CR-07 Unity 반영 기록 2026-06-21

- 사용자가 `artSample/control_room_aux_screen/` 샘플을 승인하고 Unity에 그대로 구현하라고 요청했다.
- 사용자는 기존 동력실, 조종실뿐 아니라 기존 통제실 오브젝트도 건드리지 말아야 한다고 추가 조건을 확인했다.
- Unity 루트 이름은 `Approved Control Room 07 Aux Screen`이다.
- 반영 방식:
  - CR-07은 기존 `Approved Control Room 01 Shell`의 자식으로 붙이지 않고 씬 루트의 별도 오브젝트로 생성했다.
  - 기존 CR-01 메인 스크린 기준 오브젝트의 렌더러 bounds를 읽어 위치만 계산하고, CR-01 자체 transform, 활성 상태, renderer/material 참조는 변경하지 않았다.
  - `C2_ElC2Disp.png`는 `Assets/Heavy Station Kit/_common/Textures/GUI/C2_ElC2Disp.png`를 참조하는 CR-07 전용 머티리얼로 적용했다.
- 수정/추가 파일:
  - `Assets/_Project/Editor/Validation/ApprovedControlRoomAuxScreenBootstrap.cs`
  - `Assets/_Project/Editor/Validation/UnityEditorValidationBridge.cs`
  - `scripts/Run-ApprovedControlRoomAuxScreen.ps1`
  - `Assets/_Project/Art/Ship/ControlRoom/M_Cr07_*.mat`
  - `Assets/_Project/Scenes/CargoRunMvp.unity`
- 실행 명령:
  - `.\scripts\Refresh-UnityProject.ps1`
  - `.\scripts\Run-ApprovedControlRoomAuxScreen.ps1`
- 실행 결과:
  - CR-07 적용 성공.
  - 로그 기준 루트 bounds 중심은 `16.34,3.05,22.22`이다.
  - 로그 기준 렌더러 수는 `20`개다.
  - 로그에서 `ControlRoomUntouched=True`, `CockpitUntouched=True`, `EngineRoomUntouched=True`를 확인했다.
  - 로그에서 `AuxScreenAboveMainScreen=True`, `DisplayTextureApplied=True`, `AuxScreenOverlapsEngineRoom=False`, `AuxScreenOverlapsCockpit=False`를 확인했다.
- 실행하지 않은 작업:
  - HarnessValidation, EditModeTests, PlayModeTests, WindowsDevBuild
  - 기존 통제실 오브젝트 수정
  - 동력실 오브젝트 수정
  - 조종실 오브젝트 수정
  - CR-08 이후 오브젝트 구현
  - 실제 CCTV, 구역 상태, 상호작용 로직 연결
