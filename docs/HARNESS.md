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
.\scripts\Run-AllChecks.ps1
.\scripts\Build-WindowsDev.ps1
```

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
