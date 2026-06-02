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
.\scripts\Run-AllChecks.ps1
.\scripts\Build-WindowsDev.ps1
```

## 열린 에디터 검증

사용자가 같은 프로젝트의 Unity 에디터를 열어 둔 상태라면 검증 명령은 그 에디터를 활용한다. 각 PowerShell 검증 스크립트는 열린 GUI 에디터를 감지하면 새 batchmode Unity를 띄우지 않고 `Assets/_Project/Editor/Validation/UnityEditorValidationBridge.cs`를 통해 검증 요청을 전달한다.

열린 에디터가 없으면 기존처럼 batchmode Unity를 실행한다. 열린 에디터 검증은 사용자의 에디터 세션에서 Test Runner와 BuildPipeline을 실행하므로 PlayMode 테스트와 빌드는 에디터 상태를 일시적으로 바꿀 수 있다.

사용자가 에디터를 직접 확인하는 중에는 전체 `Run-PlayModeTests.ps1`보다 기능별 빠른 PlayMode smoke를 먼저 사용한다. 2단계 플레이어 MVP는 `.\scripts\Run-Phase2PlayModeSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 `CargoRunMvp`를 Play 모드로 짧게 실행하고, 런타임 플레이어/HUD/MainCamera/카메라 렌더/상호작용을 확인한 뒤 다시 Edit 모드로 돌아온다.

4단계 화물선 Graybox는 `.\scripts\Run-Phase4CargoShipGrayboxSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 `CargoRunMvp`의 6구역 화물선 Graybox를 재생성하고, Play 모드에서 주요 방/복도/상호작용 지점/카메라 렌더/플레이어 이동을 확인한 뒤 다시 Edit 모드로 돌아온다.

6단계 방별 상호작용 1차는 `.\scripts\Run-Phase6RoomInteractionsSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 `CargoRunMvp`의 6구역 장치 상호작용을 재생성하고, Play 모드에서 조종대/동력실 스크린/통제실 스크린/무기실 포탑 핸들/비품창고/운송창고 화물 상태 장치와 통제실 CCTV A/D 전환을 확인한 뒤 다시 Edit 모드로 돌아온다.

7단계 기본 시작 세팅과 튜토리얼 의뢰는 `.\scripts\Run-Phase7NewGameStartSmoke.ps1`로 검증한다. 이 스크립트는 열린 에디터에서 협회 계약 최소 UI를 재생성하고, Play 모드에서 예 버튼, 협회 로고 행성 시작 상태, 돈 0/기본 화물선/기본 방호복/막대기 1개, 1분짜리 튜토리얼 의뢰 단독 노출, 운송창고 중앙 화물의 세션 운송 대상 등록을 확인한 뒤 다시 Edit 모드로 돌아온다.

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
