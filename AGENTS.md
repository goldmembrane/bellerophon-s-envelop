# Bellerophon Agent Harness

이 저장소는 Unity 6.3 LTS 기반 Steam 출시용 게임 프로젝트다. 에이전트는 기능 구현보다 먼저 프로젝트의 구조적 일관성과 반복 가능한 검증 루프를 유지해야 한다.

## 기본 규칙

- 대화는 한국어로 한다.
- 코드 식별자, 네임스페이스, 파일명은 영어를 사용한다.
- 새 변수를 추가하기 전에는 기존 코드, 설정, 데이터 정의에 같은 용도로 쓰이는 변수가 있는지 먼저 확인한다. 새 변수가 필요하면 그 변수가 어디에서 어떤 역할을 하는지 작업 메모, 관련 문서, 또는 코드 근처의 최소 주석 중 적절한 위치에 기록한다.
- Unity 버전은 `6000.3.x LTS` 계열로 고정한다. 현재 기준 버전은 `6000.3.16f1`이다.
- `Library`, `Temp`, `Logs`, `UserSettings`, `Builds`, `TestResults`는 생성물로 취급하고 직접 편집하지 않는다.
- 런타임 게임 로직은 가능한 한 `MonoBehaviour`에서 분리해 EditMode 테스트가 가능하게 만든다.
- Steam, 저장소, 업적, 클라우드 같은 플랫폼 기능은 인터페이스 뒤에 둔다. 게임 로직이 Steamworks SDK를 직접 참조하지 않게 한다.
- 새 기능은 최소 하나 이상의 검증 경로를 가져야 한다. 순수 로직은 EditMode 테스트, 씬/입력/물리/UI는 PlayMode 테스트를 우선한다.

## 검증 사다리

변경 범위에 따라 아래 명령을 낮은 단계부터 실행한다.

1. `.\scripts\Run-HarnessValidation.ps1`
2. `.\scripts\Run-EditModeTests.ps1`
3. `.\scripts\Run-PlayModeTests.ps1`
4. `.\scripts\Build-WindowsDev.ps1`

빌드, 씬 구성, 패키지, 프로젝트 설정을 바꿨다면 `Build-WindowsDev.ps1`까지 확인한다.

## 완료 기준

- 컴파일 에러가 없어야 한다.
- 관련 테스트가 통과해야 한다.
- Unity 콘솔 에러를 새로 만들지 않아야 한다.
- 하네스 규칙을 바꿨다면 `docs/HARNESS.md`와 관련 스크립트를 같이 갱신한다.
