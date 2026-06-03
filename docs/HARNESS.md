# Harness Engineering

Bellerophon의 하네스는 AI/사람 개발자가 같은 구조, 같은 명령, 같은 완료 기준으로 작업하게 만드는 프로젝트 운영 계층이다.

## 목적

- Unity 프로젝트 구조를 일관되게 유지한다.
- 기능 변경 후 컴파일, 테스트, 빌드 검증을 반복 가능하게 만든다.
- Steam, 세이브, 업적, 입력, 렌더링 같은 외부 의존성을 격리해 테스트 가능성을 유지한다.
- 실패한 검증은 문서, 코드 구조, 테스트 중 하나로 되먹여 같은 종류의 실수를 줄인다.

## 3C 모델

### Context

에이전트와 개발자가 참고해야 하는 지식이다.

- `AGENTS.md`: 에이전트 작업 규칙
- `docs/GAME_DESIGN.md`: 게임 방향과 핵심 루프
- `docs/ARCHITECTURE.md`: 코드/씬/플랫폼 구조
- `docs/DECISIONS`: 유지할 기술 결정 기록

### Constraint

결과물을 제약하는 규칙이다.

- Unity 버전은 `6000.3.x LTS` 계열만 사용한다.
- 런타임 로직은 `Assets/_Project/Runtime` 아래 둔다.
- 에디터 전용 자동화는 `Assets/_Project/Editor` 아래 둔다.
- 테스트는 `Assets/_Project/Tests/EditMode`와 `Assets/_Project/Tests/PlayMode`로 분리한다.
- 플랫폼 기능은 런타임 인터페이스 뒤에 둔다.
- 생성물 폴더는 커밋하지 않는다.
- 기획서에 없거나 애매한 기능 요구는 에이전트가 임의로 보강하지 않는다. 구현 전에 사용자에게 의도를 확인하고 답변을 받은 뒤 작업한다.
- 구현 계획을 세운 뒤 저장해둔 원본 기획서와 비교해 계획이 원본 방향과 맞는지 확인한다.

### Convergence

작업 후 검증하고, 실패 원인을 하네스에 반영하는 반복 과정이다.

1. 구조 검증
2. EditMode 테스트
3. PlayMode 테스트
4. Windows 개발 빌드
5. 실패 원인을 문서/테스트/스크립트/아키텍처 규칙으로 반영

## 검증 명령

```powershell
.\scripts\Setup-GitForUnity.ps1
.\scripts\Bootstrap-UnityProject.ps1
.\scripts\Run-HarnessValidation.ps1
.\scripts\Run-EditModeTests.ps1
.\scripts\Run-PlayModeTests.ps1
.\scripts\Run-Phase2PlayModeSmoke.ps1
.\scripts\Run-Phase4CargoShipGrayboxSmoke.ps1
.\scripts\Run-Phase6RoomInteractionsSmoke.ps1
.\scripts\Run-Phase7NewGameStartSmoke.ps1
.\scripts\Run-Phase8TransportRunSmoke.ps1
.\scripts\Run-Phase9SettlementGameOverSmoke.ps1
.\scripts\Run-Phase10PlanetMaintenanceSmoke.ps1
.\scripts\Run-Phase11AsteroidHazardSmoke.ps1
.\scripts\Run-Phase12ManualTurretSmoke.ps1
.\scripts\Run-Phase13IntruderFrameworkSmoke.ps1
.\scripts\Run-Phase14ParvumIntruderSmoke.ps1
.\scripts\Run-Phase15EquipmentLoopSmoke.ps1
.\scripts\Run-AllChecks.ps1
.\scripts\Build-WindowsDev.ps1
```

## 열린 에디터 검증

사용자가 같은 프로젝트의 Unity 에디터를 열어 둔 상태라면 검증 명령은 그 에디터를 활용한다. 각 PowerShell 검증 스크립트는 열린 GUI 에디터를 감지하면 새 batchmode Unity를 띄우지 않고 `Assets/_Project/Editor/Validation/UnityEditorValidationBridge.cs`를 통해 검증 요청을 전달한다.

열린 에디터가 없으면 기존처럼 batchmode Unity를 실행한다. 열린 에디터 검증은 사용자의 에디터 세션에서 Test Runner와 BuildPipeline을 실행하므로 PlayMode 테스트와 빌드는 에디터 상태를 일시적으로 바꿀 수 있다.

열린 에디터 브리지에서 PlayMode 테스트를 요청할 때 에디터가 이미 Play mode이면 먼저 Edit mode 복귀를 기다린 뒤 Test Runner를 시작한다. Test Runner가 내부 오류를 보고하면 브리지는 실패 로그를 남기고 다음 요청을 받을 수 있는 상태로 복구한다.

사용자가 에디터를 직접 확인하는 중에는 전체 `Run-PlayModeTests.ps1`보다 기능별 빠른 PlayMode smoke를 먼저 사용한다. 2단계 플레이어 MVP는 `.\scripts\Run-Phase2PlayModeSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 `CargoRunMvp`를 Play 모드로 짧게 실행하고, 런타임 플레이어/HUD/MainCamera/카메라 렌더/상호작용을 확인한 뒤 다시 Edit 모드로 돌아온다.

기능별 smoke는 해당 단계까지의 씬 구성을 재생성한다. 최신 단계까지 직접 플레이로 확인해야 할 때는 하위 단계 smoke를 마지막에 실행하지 말고, 현재 구현된 가장 높은 단계의 smoke 또는 bootstrap을 마지막에 실행해 `CargoRunMvp` 씬을 최신 단계 상태로 남긴다.

4단계 화물선 Graybox는 `.\scripts\Run-Phase4CargoShipGrayboxSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 `CargoRunMvp`의 6구역 화물선 Graybox를 재생성하고, Play 모드에서 주요 방/복도/상호작용 지점/카메라 렌더/플레이어 이동을 확인한 뒤 다시 Edit 모드로 돌아온다.

6단계 방별 상호작용 1차는 `.\scripts\Run-Phase6RoomInteractionsSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 `CargoRunMvp`의 6구역 장치 상호작용을 재생성하고, Play 모드에서 조종대/동력실 스크린/통제실 스크린/무기실 포탑 핸들/비품창고/운송창고 화물 상태 장치와 통제실 CCTV A/D 전환을 확인한 뒤 다시 Edit 모드로 돌아온다.

7단계 기본 시작 세팅과 튜토리얼 의뢰는 `.\scripts\Run-Phase7NewGameStartSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 협회 계약 최소 UI를 재생성하고, Play 모드에서 예 버튼, 협회 로고 행성 시작 상태, 돈 0/기본 화물선/기본 방호복/막대기 1개, 1분짜리 튜토리얼 의뢰 단독 노출, 운송창고 중앙 화물의 세션 운송 대상 등록을 확인한 뒤 다시 Edit 모드로 돌아온다.

8단계 자동/수동 운행 루프는 `.\scripts\Run-Phase8TransportRunSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 운행 상태 HUD와 수동 운행 화면을 재생성하고, Play 모드에서 튜토리얼 의뢰 1분 운행 진행도, 조종대 수동 운행 진입, WASD 회피 마커 이동, ESC 자동 조종 복귀, 조종실 내구도 50% 이하 자동 조종 불가 상태를 확인한 뒤 다시 Edit 모드로 돌아온다.

9단계 도착 정산과 게임오버는 `.\scripts\Run-Phase9SettlementGameOverSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 정산 UI와 게임오버 컷씬 루트를 재생성하고, Play 모드에서 운행 완료 후 정산 UI 자동 표시, 첫 마이너스 정산 유예, 다음 정산 후 마이너스 확정 게임오버, 플레이어 입력 억제, 전체 화면 화물선/포드 사출 컷씬을 확인한 뒤 다시 Edit 모드로 돌아온다.

10단계 행성 정비와 다음 운송 준비는 `.\scripts\Run-Phase10PlanetMaintenanceSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 정산 후 정비 화면, 수리 버튼, 후속 의뢰 목록, 상점/개인 화물/업그레이드 진입점을 재생성하고, Play 모드에서 정산 후 정비 화면 이동, 수리비 청구와 6구역 회복, 후속 협회 의뢰 선택과 다음 운송 시작을 확인한 뒤 다시 Edit 모드로 돌아온다.

11단계 소행성 지대 위험 1차는 `.\scripts\Run-Phase11AsteroidHazardSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 소행성 위험 런타임을 최신 운송/정산/정비 흐름에 연결하고, Play 모드에서 튜토리얼 운행에는 위험이 발생하지 않는지, 후속 의뢰 운행에는 소행성 지대가 발생하는지, 자동 조종 방치와 수동 운행 회피 결과가 선박 손상/정비 비용에 반영되는지 확인한 뒤 다시 Edit 모드로 돌아온다.

12단계 수동 포탑과 외부 목표는 `.\scripts\Run-Phase12ManualTurretSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 무기실 포탑 전체 화면, 외부 목표, 좌클릭 누르고 있기 연속 발사, 0.25초 연사 딜레이, 탄창/재장전, 명중 판정을 재생성하고, Play 모드에서 튜토리얼 이후 소행성 목표를 수동 포탑으로 파괴해 위험을 중립화하는 성공 경로와 목표를 방치해 선박 손상으로 이어지는 실패 경로를 확인한 뒤 다시 Edit 모드로 돌아온다.

13단계 침입자/적대 개체 프레임워크는 `.\scripts\Run-Phase13IntruderFrameworkSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 Phase13 프레임워크 루트를 재생성하고, 에디터 검증에서 침입 시도/성공, 구역 배치, 화물 공격, 구역 점유, 플레이어 공격, 화물선 파괴 목표 유형이 순수 규칙으로 계산되는지 확인한다. 구체 침입자 외형, 공격 모션, 실제 전투 연결은 이후 단계에서 별도 검증한다.

14단계 첫 침입자 씨앗체 구현은 `.\scripts\Run-Phase14ParvumIntruderSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 Phase14 파르붐 루트를 재생성하고, Play 모드에서 튜토리얼 첫 운행에는 씨앗체가 발생하지 않는지, 후속 운행 중 2초마다 15% 판정으로 파르붐이 내부 침입자로 발생하는지, 외부 목표가 생성되지 않는지, HUD 표시와 월드 placeholder 표시/숨김이 동작하는지, 파르붐의 0.5초 공격 피해가 정산 후 정비 수리비로 남는지 확인한다.

15단계 무기류와 비품실 기본 루프는 `.\scripts\Run-Phase15EquipmentLoopSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 Phase15 장비 HUD와 상점 루트를 재생성하고, Play 모드에서 협회 기본 지급 장비, 비품창고 3칸 표시, 정비 화면 상점 Buy/Sell 골격, 머스킷 $450 구매, 막대기/머스킷으로 파르붐을 처치하는 전투 연결, 머스킷 R 재장전 골격을 확인한다.

## 테스트 정책

EditMode 테스트는 빠르고 결정적이어야 한다.

- 전투 수치 계산
- 아이템/스탯 규칙
- 세이브 데이터 직렬화
- 플랫폼 인터페이스의 Mock 구현

PlayMode 테스트는 Unity 런타임 통합을 확인한다.

- 씬 로딩
- 플레이어 스폰
- 입력/물리/충돌
- UI 흐름
- 콘솔 에러 없는 프레임 진행

## Steam 연동 정책

Steamworks SDK는 직접 게임 로직에 물리지 않는다. 다음 계층을 유지한다.

- `IPlatformServices`: 런타임이 바라보는 플랫폼 인터페이스
- 개발/테스트: Mock 또는 Null 구현
- Steam 빌드: Steam 구현

이 구조를 유지해야 Steam 없이도 대부분의 테스트를 실행할 수 있다.
