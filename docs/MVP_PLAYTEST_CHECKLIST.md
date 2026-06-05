# MVP Playtest Checklist

## Detailed Step 3 Regression Extension

- Confirm planet maintenance no longer starts association/private contracts directly.
- Confirm planet maintenance exposes a separate contract board entry along with repair, shop, personal cargo, and upgrades.
- Confirm the contract board opens as a separate full-screen UI and returns to maintenance through its back button.
- Confirm damaged ships can view the contract board but cannot accept board contracts until repaired.
- Confirm association/private/special category buttons select categories only and never immediately accept a contract.
- Confirm a listed contract row can be selected while the board stays open.
- Confirm repaired ships start the association follow-up contract only after pressing the separate `Accept` button.
- Confirm association, private, and special categories are displayed separately on the contract board.
- Confirm low fame hides private contracts and shows the one-time association revival contract.
- Confirm a clean revival contract completion resets fame to zero and marks the revival contract as used.
- Confirm negative fame does not create negative displayed reputation pay.
- Confirm Phase10/11/12/14/15/18 smoke tests still start follow-up transport through the contract board.

## Detailed Step 2 Regression Extension

- Confirm the first tutorial contract still starts as an association intro contract with 60 second duration and `$1000` fixed reward.
- Confirm post-tutorial maintenance still offers the current UI-compatible association/private pair.
- Confirm detailed contract rules expose at least five association contracts for association members and two for non-members.
- Confirm contract reward traces cargo value, route distance, and fame/association fame as separate inputs.
- Confirm route distance is treated as a simple route value, not a coordinate distance system.

## Detailed Implementation Regression Extension

- 상세 구현 단계가 추가될 때마다 이 체크리스트에는 `MVP baseline`, `new detailed domain`, `regression risk`, `manual confirmation` 항목을 구분해서 추가한다.
- MVP baseline은 18단계 반복 루프가 계속 통과하는지 확인한다.
- new detailed domain은 해당 단계에서 새로 채운 행성, 항로, 계약, 화물, 방, 장비, 적대 세력, 위험 구간 규칙을 확인한다.
- regression risk는 기존 정산, 정비, 운송, 침입자, HUD, 로컬 협동 상태가 깨지지 않았는지 확인한다.
- manual confirmation은 자동화로 잡기 어려운 UI 텍스트 겹침, 카메라, 플레이 감각, 행성 화면 흐름을 기록한다.

이 체크리스트는 18단계 기준으로, 자동 smoke가 통과한 뒤 사람이 직접 확인할 최소 반복 루프를 정리한다.

## 준비

- Unity `6000.3.16f1` 에디터에서 `CargoRunMvp` 씬을 연다.
- 실행 전 `.\scripts\Run-HarnessValidation.ps1`와 `.\scripts\Run-EditModeTests.ps1`가 통과했는지 확인한다.
- 빠른 자동 루프는 `.\scripts\Run-Phase18MvpPlaytestLoopSmoke.ps1`로 먼저 확인한다.

## 시작과 튜토리얼

- 새 게임 시작 시 협회 계약 UI가 보인다.
- 협회 계약 수락 후 기본 방호복과 막대기 1개가 지급된다.
- 튜토리얼 의뢰는 60초 운송 하나만 선택 가능하다.
- 튜토리얼 운송 중 소행성 지대와 씨앗체 침입자는 발생하지 않는다.
- HUD에는 체력, 보호막, 현재 구역 맵이 표시되고 기본 중앙 조준선은 보이지 않는다.

## 첫 도착과 정비

- 운송 완료 시 정산 화면이 자동으로 열린다.
- 정산 화면에서 수익, 비용, 최종 잔액, 예정 수리비가 항목별로 보인다.
- 정산 화면이 열려 있는 동안 커서가 사용 가능하고 1인칭 조작은 방해되지 않는다.
- 정비 화면으로 이동하면 6구역 내구도와 후속 의뢰 목록이 표시된다.
- 수리비가 남아 있으면 다음 의뢰 버튼은 비활성화된다.
- 수리 버튼을 누르면 수리비가 차감되고 손상 구역이 100%로 회복된다.

## 후속 운송

- 수리 후 협회 후속 의뢰와 개인 의뢰 버튼이 활성화된다.
- 협회 후속 의뢰를 시작하면 운송 상태로 돌아가고 정비 화면은 닫힌다.
- 후속 운송에서는 소행성 지대가 발생한다.
- 조종대 수동 운행으로 회피에 성공하면 선박 수리비가 추가되지 않는다.
- 무기실 수동 포탑으로 외부 목표를 파괴하면 위험 결과가 중립화된다.
- 파르붐 침입자는 막대기와 머스킷 공격으로 처치할 수 있다.

## 반복 가능성

- 후속 운송 완료 후 두 번째 정산 화면이 열린다.
- 수동 회피, 포탑 중립화, 침입자 처치가 성공했다면 두 번째 정산에 예정 수리비가 남지 않는다.
- 두 번째 정비 화면에서 다음 의뢰 선택이 다시 가능하다.
- 같은 루프를 반복해도 이전 정산 화면 때문에 다음 도착 정산이 막히지 않는다.

## 자동 검증 대응

- `MvpPlaytestLoopTests`는 시작 상태, 튜토리얼 정산/수리, 후속 위험 대응, 파르붐 처치, 로컬 협동 스냅샷, 정산 도착 게이트 회귀를 검증한다.
- `Run-Phase18MvpPlaytestLoopSmoke.ps1`는 실제 `CargoRunMvp` PlayMode에서 시작부터 두 번째 정비 준비까지 연결 흐름을 검증한다.
