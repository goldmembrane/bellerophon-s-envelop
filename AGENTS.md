# Bellerophon Agent Harness

## Unity 에디터 실행 규칙

- Unity 에디터를 열거나 재시작할 때는 직접 `Unity.exe`, `.unity` 씬 파일, 또는 임의의 `Start-Process` 명령을 사용하지 않는다.
- 에디터 실행은 `.\scripts\Open-UnityProject.ps1`를 사용한다.
- 코드/에셋 변경을 열린 Unity 에디터에 반영해야 할 때는 재시작보다 `.\scripts\Refresh-UnityProject.ps1`를 먼저 사용한다.
- stale 컴파일, 잘못 열린 에디터, 남은 `Temp\UnityLockfile`, AssetImportWorker 잔여 프로세스가 의심되어도 먼저 리프레시와 열린 에디터 브리지 재시도를 시도한다. `.\scripts\Open-UnityProject.ps1 -Restart -ValidateCargoRunScene`는 리프레시가 실패하거나 에디터가 잘못된 프로젝트를 열고 있는 경우의 최후 수단으로만 사용한다.
- 이 스크립트는 `ProjectSettings\ProjectVersion.txt`와 `Assets\_Project\Scenes\CargoRunMvp.unity` 존재를 확인하고, `-projectPath D:\Bellerophon2\Bellerophon`로 열린 실제 프로젝트 에디터만 정상으로 인정한다.
- 에디터가 기본 `Untitled` 씬만 보여주는 상태는 정상 실행으로 보지 않는다. `Open-UnityProject.ps1`는 열린 에디터 브리지에 `OpenCargoRunMvpScene` 명령을 보내 `CargoRunMvp`가 활성 씬이 되도록 해야 한다.

## 이어서 작업할 때

- 이어서 작업을 시작하기 전 `docs/PROGRESS_2026-06-02.md`를 먼저 읽고, 현재 구현 상태와 다음 단계 범위를 확인한다.
- 진행상황 문서의 내용이 `docs/GAME_DESIGN_SOURCE.txt` 및 `docs/MVP_IMPLEMENTATION_ORDER.md`와 충돌하면 원본 기획서와 사용자 확인 사항을 우선하고, 애매한 부분은 구현 전에 사용자에게 질문한다.

이 저장소는 Unity 6.3 LTS 기반 Steam 출시용 게임 프로젝트다. 에이전트는 기능 구현보다 먼저 프로젝트의 구조적 일관성과 반복 가능한 검증 루프를 유지해야 한다.

## 기본 규칙

- 대화는 한국어로 한다.
- 모든 대화는 항상 존댓말로 한다.
- 코드 식별자, 네임스페이스, 파일명은 영어를 사용한다.
- 애매모호하거나 없었던 사항은 멋대로 판단하여 구현하지 말고 사용자에게 정확한 설명을 요청하고 답변을 받은 뒤에 작업한다.
- 계획을 세우고 나서 저장해둔 원본 기획서와 계획이 맞게 구성되었는지 비교한다.
- 새 변수를 추가하기 전에는 기존 코드, 설정, 데이터 정의에 같은 용도로 쓰이는 변수가 있는지 먼저 확인한다. 새 변수가 필요하면 그 변수가 어디에서 어떤 역할을 하는지 작업 메모, 관련 문서, 또는 코드 근처의 최소 주석 중 적절한 위치에 기록한다.
- Unity 버전은 `6000.3.x LTS` 계열로 고정한다. 현재 기준 버전은 `6000.3.16f1`이다.
- `Library`, `Temp`, `Logs`, `UserSettings`, `Builds`, `TestResults`는 생성물로 취급하고 직접 편집하지 않는다.
- 작업 목표 달성에 파일 삭제나 생성물 정리가 필요하면 에이전트가 대상 경로와 작업 범위를 스스로 확인한 뒤 사용자에게 삭제 승인 요청이나 삭제 예고를 하지 않고 바로 진행한다. 단, 작업 범위 밖의 사용자 변경, 저장소 이력 조작, `git reset --hard` 같은 복구 불가능한 작업은 명시 요청 없이는 수행하지 않는다.
- 런타임 게임 로직은 가능한 한 `MonoBehaviour`에서 분리해 EditMode 테스트가 가능하게 만든다.
- Steam, 저장소, 업적, 클라우드 같은 플랫폼 기능은 인터페이스 뒤에 둔다. 게임 로직이 Steamworks SDK를 직접 참조하지 않게 한다.
- 새 기능은 최소 하나 이상의 검증 경로를 가져야 한다. 순수 로직은 EditMode 테스트, 씬/입력/물리/UI는 PlayMode 테스트를 우선한다.

## 아트/에셋 승인 규칙

- 모델링, UI, 애니메이션, 머티리얼, VFX, 사운드처럼 아트와 연관이 깊은 작업은 실제 게임 씬, 프리팹, 런타임 자산, UI 흐름에 붙이기 전에 먼저 저장소 루트의 `artSample/`에 사용자가 볼 수 있는 샘플 파일로 저장한다.
- 샘플 파일은 사용자가 검사할 수 있는 형식이어야 한다. 예: PNG/JPG/WebP 이미지, MP4/GIF 영상, HTML 미리보기, FBX/GLB 같은 범용 3D 파일, 또는 독립적으로 확인 가능한 Unity 샘플 씬/프리팹과 설명 문서.
- `artSample/`의 설명 문구는 한국어를 기본으로 작성한다. 영어는 파일명, 코드 식별자, 고유 명사, 장비명처럼 필요한 경우에만 사용하고, 어설픈 혼합 표현으로 설명을 흐리지 않는다.
- 사용자가 `artSample/`의 샘플을 검사하고 승인한 뒤에만 해당 아트/UI/애니메이션 결과물을 실제 게임에 연결한다.
- 승인 전에는 해당 결과물을 실제 게임 씬, 프리팹, 런타임 자산, UI 흐름에 붙이지 않는다. 단, `artSample/` 생성을 위한 임시 파일과 미리보기 파일은 허용한다.

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
