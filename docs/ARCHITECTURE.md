# Architecture

이 문서는 초기 구조 기준이다. 1인칭 우주 화물 운송 협동 생존/공포 게임의 디자인 방향은 `docs/GAME_DESIGN.md`에 두고, 실제 게임 시스템이 생길 때 이 구조를 구체화한다.

## 폴더 구조

```text
Assets/_Project/
  Runtime/
    Core/
    Platform/
  Editor/
    Build/
    Validation/
  Tests/
    EditMode/
    PlayMode/
  Scenes/
  Prefabs/
```

## Assembly 경계

- `Bellerophon.Runtime`: 게임 런타임 코드
- `Bellerophon.Editor`: 에디터 전용 빌드/검증 자동화
- `Bellerophon.EditModeTests`: 순수 로직 및 에디터 테스트
- `Bellerophon.PlayModeTests`: Unity 런타임 통합 테스트

## 의존성 방향

```text
Tests -> Runtime
Editor -> Runtime
Runtime -> UnityEngine
Runtime -> Platform interfaces
Steam implementation -> Platform interfaces
```

런타임 핵심 로직은 `UnityEditor`를 참조하지 않는다. Steam 구현은 별도 어셈블리로 분리할 예정이다.

## 초기 원칙

- 게임 규칙은 테스트 가능한 C# 클래스로 먼저 만든다.
- `MonoBehaviour`는 Unity 씬 연결과 수명주기 어댑터 역할에 집중한다.
- 씬 이름, 빌드 대상, 테스트 대상은 스크립트와 문서에 명시한다.
- 출시 후보 빌드는 개발 빌드와 분리된 Build Profile 또는 빌드 스크립트로 관리한다.
