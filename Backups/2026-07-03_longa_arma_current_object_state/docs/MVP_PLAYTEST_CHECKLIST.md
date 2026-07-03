# MVP Playtest Checklist

## Detailed Step 21 Balance And Playtest Hardening

- MVP baseline: run `Run-Phase1To18Smokes.ps1` and confirm the start, tutorial, settlement, repair, follow-up transport, hazard, intruder, equipment, HUD, local-coop foundation, and repeatable MVP loop still pass.
- New detailed domain: run `Run-DetailedStep21BalancePlaytestHardeningSmoke.ps1` and confirm source-valued repair, towing, reward, hazard cadence, equipment price, special-route, and debt recovery pins are unchanged.
- Regression risk: run `Run-DetailedStep21FullSmokeSuite.ps1` after balance-sensitive changes to confirm detailed steps 13 through 21 remain compatible with the MVP sweep.
- Manual confirmation: record any proposed change to source-valued rewards, repair/towing costs, hazard probabilities, enemy stats, or item prices as a user-confirmed design change before implementation.
- Playtest scenario coverage: keep at least one early tutorial scenario, one mid-risk fame-gated hazard scenario, one high-risk special-contract scenario, and one failure-recovery scenario available in automated validation or the manual notes.

## Detailed Step 10 Regression Extension

- Confirm manual-flight booster reduces the active asteroid hazard duration by the source value of 10 seconds.
- Confirm engine room damage at the booster-disabled threshold blocks manual-flight booster use.
- Confirm manual flight prevents weapon-room manual turret operation and forces auto turret mode.
- Confirm weapon systems upgrades set manual turret magazine capacity through the source values `50`, `60`, `75`, and `100`.
- Confirm upgraded plasma is available only after the weapon systems tier that includes plasma, fires from manual turret right click, lasts 3 seconds, deals 50 damage every 0.1 seconds, and has a 1 minute cooldown that recovers outside manual mode.
- Confirm armory damage at `<=75%` blocks plasma/auto-aim and destroyed armory blocks weapon operation.
- Confirm control-room CCTV uses the five non-control rooms and includes Supply Room.
- Confirm control-room main screen right click progresses from main CCTV to vertical room list and then to horizontal ship layout.
- Confirm ESC closes the control-room screen and restores first-person input/cursor state.
- Confirm ESC closes the engine-room and supply-room interaction panels without leaving their HUD text on screen.
- Confirm vertical room list room buttons are clickable and number keys `1` through `6` select the same listed rooms.
- Confirm control-room internal purification is the selected-room operation from the vertical screen: selected room only, temporary seal, 30 seconds, total fire damage 500, players inside are damaged too, room durability is not damaged by purification itself, and the selected room reopens afterward.
- Confirm the control-room selected-room internal purification is not the special item `복도 정화 장치`; the special item targets all corridors and remains a separate future implementation path.
- Confirm Phase6 and Phase12 smoke tests cover these step 10 regressions.

## Detailed Step 8 Regression Extension

- Confirm shop Buy text and buttons expose step 8 representative items: Shotgun, Protective Suit, Strength Enhancer, Flashlight, and Injury Reliever.
- Confirm expanded weapon definitions match the design source first-pass values for shotgun, mini flamethrower, electric baton, and dagger.
- Confirm the Presence Detector remains locked until special-contract unlock work exists.
- Confirm Flashlight use activates a timed utility state and is shown in the equipment HUD.
- Confirm treatment supply items apply recovery deltas and consume one stored item.
- Confirm protective supply items equip from storage, lose durability, and add damage reduction on top of the basic protective suit.
- Confirm Strength Enhancer applies only to melee damage in the current first pass: stick/electric baton +40%, dagger +10, ranged weapons unchanged.
- Confirm the shop Sell tab still requires selecting a listed item before selling after step 8 item usage.
- Confirm Phase15 smoke covers step 8 item purchases, active effects, selected personal cargo sale, selected purchased item disposal, and Parvum combat with strengthened stick plus base musket.

## Detailed Step 7 Regression Extension

- Confirm player hand slots start at 3 and the pouch upgrade model expands them to 4.
- Confirm association start still grants the basic protective suit and one stick without filling the extra hand slots.
- Confirm supply storage starts at 3 slots and equipped Supply Slots upgrades expand the equipment storage capacity through the existing upgrade effect values.
- Confirm the supply storage panel shows tabs for All, Weapon, Protective, Treatment, Enhancement, and Utility.
- Confirm shop Buy text separates common products, fame-limited products, and special products.
- Confirm shop purchases can enter hand slots or supply storage according to the item storage target.
- Confirm purchased items keep durability and purchase-price metadata.
- Confirm shop Sell text separates purchased-item 1% disposal from personal cargo sale, requires selecting a listed item first, and only sells the selected item after pressing Sell Selected.
- Confirm Phase15 smoke covers musket, flashlight, treatment purchase, selected item disposal, selected personal cargo sale, and the later Parvum combat regression.

## Detailed Step 6 Regression Extension

- Confirm planet maintenance room rows show concrete six-room damage effects, not only generic risk text.
- Confirm cockpit damage disables auto pilot at the existing threshold and reduces manual flight input response at stable/critical thresholds.
- Confirm cargo hold damage reduces hold capacity, blocks personal cargo transport at `<=50%`, blocks launch at `<=25%`, and still applies cargo loss at critical/destroyed thresholds.
- Confirm engine room damage increases transport duration and disables booster/overclock at the damaged threshold.
- Confirm control room damage limits CCTV channels, disables intrusion detection/cargo warning/suppression at the configured thresholds, and increases seed intruder occurrence/damage when suppression is offline.
- Confirm armory damage disables auto aim/plasma, reduces turret capability, and destroyed armory disables manual turret operation.
- Confirm supply room damage reduces usable storage slots and displays security/equipment risk in the supply panel.
- Confirm contract board and planet maintenance launch readiness account for stored personal cargo when cargo hold damage blocks personal cargo transport.
- Confirm Phase6 smoke covers damaged CCTV shutdown, storage slot reduction, and destroyed manual turret shutdown.
- Confirm Phase10/11/12/14/15/18 smoke tests still pass after the six-room rule connections.

## Detailed Step 5 Regression Extension

- Confirm repair charge uses total missing durability across all 6 rooms at `$5` per 1%, not per-room repair rates.
- Confirm a near-total-loss ship caps normal repair at `$2995`, while all 6 rooms at 0% uses the separate `$5000` total-loss claim.
- Confirm towing costs increase by towing incident count: `$2000`, `$3000`, `$5000`, then +`$2500` per additional tow.
- Confirm planet maintenance opens a separate full-screen ship upgrade screen from `Upgrades`.
- Confirm durability upgrade purchase applies immediately without a separate equip action.
- Confirm non-durability upgrade purchase and equip remain separate actions.
- Confirm upgrade costs match the updated design source for durability, weapons, autopilot, supply slots, and internal control.
- Confirm the upgrade screen returns to planet maintenance through `Back` and does not block later shop, cargo, contract board, repair, or start-run interactions.
- Confirm Phase10 smoke covers upgrade buy/equip before the repair and contract-board flow.

## Detailed Step 3 Regression Extension

- Confirm planet maintenance no longer starts association/private contracts directly.
- Confirm planet maintenance exposes a separate contract board entry along with repair, shop, personal cargo, and upgrades.
- Confirm the contract board opens as a separate full-screen UI and returns to maintenance through its back button.
- Confirm damaged ships can view the contract board but cannot accept board contracts until repaired.
- Confirm association/private/special category buttons select categories only and never immediately accept a contract.
- Confirm a listed contract row can be selected while the board stays open.
- Confirm repaired ships add selected contracts to the pending run when pressing `Accept` while the board stays open.
- Confirm multiple accepted contracts can be queued before transport starts.
- Confirm transport starts only after pressing the separate `Start Run` button.
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
- 전체 phase 회귀는 `.\scripts\Run-Phase1To18Smokes.ps1`로 먼저 확인한다. MVP end-to-end 루프만 좁혀 볼 때는 `.\scripts\Run-Phase18MvpPlaytestLoopSmoke.ps1`를 사용한다.

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
- 후속 운송에서는 원본 확률과 유명세 해금 상태에 따라 소행성 지대, 화물 자유 연대 출몰 지역, 우주 해적 출몰 지역, 외계 생명체 출몰 구역이 운행 중 주기 체크로 발생할 수 있다.
- 유명세 조건은 세션 단위 해금으로 유지되어 한 번 해금된 위험은 유명세가 다시 내려가도 게임오버 초기화 전까지 재잠금되지 않는다.
- 은폐 블랙홀은 후반 위험 확장으로 보류되어 현재 후속 운송에서 발생하지 않는다.
- 조종대 수동 운행으로 회피에 성공하면 선박 수리비가 추가되지 않는다.
- 무기실 수동 포탑으로 외부 목표를 파괴하면 위험 결과가 중립화된다.
- 파르붐 침입자는 막대기와 머스킷 공격으로 처치할 수 있다.

## 반복 가능성

- 후속 운송 완료 후 두 번째 정산 화면이 열린다.
- 수동 회피, 포탑 중립화, 침입자 처치가 성공했다면 두 번째 정산에 예정 수리비가 남지 않는다.
- 두 번째 정비 화면에서 다음 의뢰 선택이 다시 가능하다.
- 같은 루프를 반복해도 이전 정산 화면 때문에 다음 도착 정산이 막히지 않는다.

## 자동 검증 대응

- `Run-Phase1To18Smokes.ps1`는 Phase 1부터 Phase 18까지 순서대로 실행하는 기본 회귀 검증이다.
- `MvpPlaytestLoopTests`는 시작 상태, 튜토리얼 정산/수리, 후속 위험 대응, 파르붐 처치, 로컬 협동 스냅샷, 정산 도착 게이트 회귀를 검증한다.
- `Run-Phase18MvpPlaytestLoopSmoke.ps1`는 실제 `CargoRunMvp` PlayMode에서 시작부터 두 번째 정비 준비까지 연결 흐름을 검증한다.
