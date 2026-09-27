# 통제실 칸막이·출입구 및 동력실 공중 간판 — 2026-09-27

## 직접 확인과 결과

- 통제실 칸막이 앞뒤 동일 구도와 상단 근접 시점에서 수정 전후를 확인했다. 기존 칸막이 위의 공간이 사라지고 천장에 연결됐으며 문은 열린 통로를 유지한다. `ControlBefore.png`/`ControlRaised45.png`, `ControlRearBefore.png`/`ControlRearRaised45.png`, `CeilingJoinFront.png`/`CeilingJoinRear.png`가 근거다.
- 기존 출입구는 중앙 보행으로 통과는 가능했으나 문 아래에서 상단 충돌이 기록됐다. 30cm 보정 후에도 접촉이 남아 15cm 추가 보완했다. 최종 출입구 상단은 기존보다 45cm 높으며, 바닥·천장은 이동하지 않았다.
- 최종 실행모드에서 중앙·좌우 0.6m의 양방향 6경로와 대각선 양방향 2경로를 기존 수송자로 보행했다. 숙이기/점프/자유비행 없이 통과하는 화면을 직접 확인했다. 모든 최종 보행 기록은 도착했고 상단·측면 접촉이 없다. 벽 자체를 양쪽에서 밀었을 때에는 벽 앞에서 멈췄다.
- 실제 이동은 가상 키보드를 기존 Input System → Motor → CharacterController에 공급했다. 시작점 배치 외에 이동 도중 위치를 강제로 넘기지 않았고 플레이어 크기·속도는 유지했다. 물리 키보드를 손으로 조작한 검증은 아니다.
- 동력실의 세 방향에서 지지대 없이 공중에 떠 있는 판형 간판 3개를 직접 특정했다. 동일한 앞/옆 구도의 `EngineEastBefore/After`, `EngineNorthBefore/After`, `EngineSouthBefore/After`에서 해당 판이 제거되고 주변 벽·입구·중앙 설비가 유지됐음을 확인했다.

## 변경 범위

- `Approved Control Room 01 Shell/Internal Partition - individually editable`: 좌우 칸막이 2개, 가운데 문 위 벽 1개, 문틀 기둥 2개의 높이·중심만 변경. 기존 MeshCollider가 같은 Transform을 따라가므로 충돌을 해제하지 않았다. 벽 상단은 기존 천장 하부에 약 1cm 겹쳐 연결한다.
- 삭제: `Approved Engine Room 01 Shell/Labels - individually editable/` 아래 `ER-01 1시 Cockpit wall label plate`, `ER-01 3시 Control wall label plate`, `ER-01 5시 Cargo wall label plate`.
- 신규 모델·머티리얼·전용 메시 에셋 불필요. 공유 원본, 다른 방·복도, 바닥, 천장, Player, 정상 스크린·설비·표지 변경 없음.
- 삭제한 간판은 `Before.unity` 복구본에 보존돼 복원 가능하다. 다른 사용자 변경을 덮어쓰거나 전체 씬을 재생성하지 않았다.

## 검증 기록

- `Route_*_Before`: 최초 중앙 보행. `Route_*_After`/`Diagonal`: 30cm 중간 보정, 최종 통과 근거로 사용하지 않음.
- `Route_*_Raised45`/`Diagonal45`: 최종 8개 보행 경로. `Route_*_Closed`: 문 개폐 시험이 아니라 **칸막이 벽 양면 차단** 확인이며, 4초 시간 종료는 의도한 벽 차단이다.
- `Changes.txt`는 최초 30cm 보정과 삭제 목록. 최종 45cm 형상은 `Survey.txt`/`After.unity` 기준.
- 최종 실행모드 종료, Dirty=False, Compiling=False, Console Entries=0. 에디터 복사본 `After.unity`와 저장된 Pegasus 전체 텍스트 일치.
- 최종 캡처 `Final.png` 1회. 중간 비교 캡처와 반복 검증은 승인 범위에서 수행했다.
- Unity 재시작, 자동 테스트·빌드, Git 커밋·푸시를 실행하지 않았다.
