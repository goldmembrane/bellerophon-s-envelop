# 상세 구현 이후 제작 로드맵

작성일: 2026-06-09

이 로드맵은 `docs/DETAILED_IMPLEMENTATION_PLAN.md` 22단계 이후부터 적용한다. 상세 구현 계획은 플레이 가능하고 검증 가능한 시스템 기반을 만들었지만, 이것이 출시 준비 완료를 뜻하지는 않는다. 다음 작업은 제작 완성 단계다. 아트, 모델링, 애니메이션, 사운드, UX 폴리싱, 콘텐츠 패스, 플레이테스트 하드닝, 최적화, 패키징을 진행하고, 네트워크 작업은 비네트워크 게임이 완성된 뒤에 별도로 진행한다.

## 현재 기준선

- 핵심 싱글 플레이 MVP 루프가 존재한다: 시작, 의뢰, 운송, 위험, 수동 운행, 수동 포탑, 침입자, 정산, 수리, 상점, 후속 운행, 저장/설정/플랫폼 경계.
- 행성/항로/의뢰, 적대 세력, 특수 의뢰, 장비, 경제 고정값, 로컬 협동 상태 경계에 대한 상세 데이터가 존재한다.
- 현재 비주얼은 대부분 graybox, placeholder 머티리얼, 생성된 씬 오브젝트, 텍스트 중심 UI 패널이다.
- 적대 개체 시각 레퍼런스는 `image/` 아래에 있으며 `docs/ENEMY_REFERENCES.md`에 정리되어 있다.
- 전용 런타임 아트/오디오 폴더는 아직 확립되지 않았다. 현재 `Assets/_Project/Art`와 `Assets/_Project/Audio` 디렉터리는 없다.
- 네트워크 구현은 비네트워크 게임이 완성될 때까지 보류한다.

## 범위 설명

이 로드맵은 에셋 제작 계획만이 아니다. 두 범주의 작업을 포함한다.

- 에셋/표현 작업: 모델링, 머티리얼, 애니메이션, UI 표현, VFX, 사운드, 조명, Steam 상점용 미디어.
- 상세 구현 이후 남은 비네트워크 완성 작업: placeholder hook 교체, 승인된 아이템/적 표현과 행동 연결 마무리, 콘텐츠 매트릭스 채우기, UX 개선, 플레이테스트 기반 밸런스 하드닝, 진행 버그 수정, 최적화, 패키징, 비네트워크 출시 후보 준비.

이 로드맵은 상세 구현을 제한 없는 기능 추가 단계로 다시 여는 문서가 아니다. 원본에서 애매한 메커니즘, 새 시스템, 밸런스 변경, 실제 음성/흉내 기능, 네트워크 기능은 구현 전에 명시적인 사용자 확인이 필요하다.

## 제작 규칙

- 22단계 완료를 출시 준비 완료로 취급하지 않는다.
- 이 로드맵 중에는 Steam 로비, 실제 네트워크 패키지, 온라인 동기화 작업을 시작하지 않는다.
- Steam, Cloud, Achievement, Stats, 향후 Multiplayer는 플랫폼 인터페이스 뒤에 둔다.
- 원본 수치가 있는 보상, 비용, 적 스탯, 아이템 가격, 확률은 사용자 확인 없이 변경하지 않는다.
- 화물 직접 집기/운반/납품 상호작용은 구현하지 않는다. 화물은 운송 대상, 상태, 정산 객체다.
- 새 동작이 필요하면 런타임 게임 로직은 가능한 한 `MonoBehaviour` 밖에서 테스트 가능하게 유지한다.
- 모든 제작 패스는 위험도에 맞는 검증 경로를 가진다: 기존 smoke, 새 focused smoke, screenshot review, PlayMode test, build, 또는 manual checklist.

## 목표 마일스톤

### 0. 제작 기준선 감사

목표: placeholder를 교체하기 전에 현재 상태를 고정한다.

작업:

- 현재 플레이 가능 루프를 기준 빌드로 캡처한다.
- placeholder inventory를 만든다: 선박 방, 복도, 소품, UI 표면, 적, 오디오 hook, VFX hook, 누락된 최종 에셋.
- 런타임 에셋 폴더를 확립한다:
  - `Assets/_Project/Art/Ship`
  - `Assets/_Project/Art/Props`
  - `Assets/_Project/Art/Characters`
  - `Assets/_Project/Art/Enemies`
  - `Assets/_Project/Art/VFX`
  - `Assets/_Project/Audio/Ambience`
  - `Assets/_Project/Audio/SFX`
  - `Assets/_Project/Audio/UI`
- 모델, 머티리얼, 프리팹, 애니메이션, 오디오 클립 명명 규칙을 정한다.
- Windows 우선 출시 기준 성능 예산을 정한다: 목표 FPS, 텍스처 크기, 메시 삼각형 범위, 조명 수, 그림자 규칙, 오디오 voice 한도.

산출물:

- 에셋 inventory 문서.
- 폴더 및 명명 규칙.
- 기준 screenshot과 짧은 capture 목록.
- 아트/오디오 제작용 검증 체크리스트 갱신.

완료 기준:

- 기존 harness와 smoke 테스트가 계속 통과한다.
- placeholder inventory가 출시 후보 전에 교체해야 할 항목을 명확히 표시한다.

### 1. 아트 디렉션과 에셋 바이블

목표: 이후 모든 모델링과 사운드 결정의 기준을 일관되게 만든다.

작업:

- `docs/GAME_DESIGN.md` 기준 최종 시각 기둥을 정의한다: 저채도, 산업적 폐쇄 공간, 어두운 조명, 거친 표면, 제한 시야, 불확실한 위협.
- material bible을 만든다:
  - 낡은 도장 금속
  - 어두운 노출 금속
  - 화물 스트랩과 고무
  - 조종석 유리
  - 경고 페인트
  - 콘솔 유리/화면
  - 손상/그을린 방 표면
  - 유기체 침입 재질
- lighting bible을 만든다:
  - 정상 선박 상태
  - 손상된 방 상태
  - 저시야 복도 상태
  - 위험 경보 상태
  - 행성 허브 상태
- `docs/ENEMY_REFERENCES.md`와 `image/`를 사용해 적 실루엣 시트를 만든다.
- 1인칭 스케일 기준을 정한다: 문 높이, 복도 폭, 콘솔 높이, 포탑 손잡이 높이, 화물 상자 크기, 플레이어 눈높이.

산출물:

- `docs/ART_DIRECTION_BIBLE.md`
- `docs/ASSET_PRODUCTION_LIST.md`
- 머티리얼/조명 reference board.

완료 기준:

- 본격 모델링 전에 사용자가 시각 목표를 승인한다.
- 선박과 적 실루엣 우선순위가 확정된다.

2026-06-09 실행 상태:

- 1단계 산출물을 준비했다:
  - `docs/ART_DIRECTION_BIBLE.md`
  - `docs/ASSET_PRODUCTION_LIST.md`
  - `artSample/art_direction_reference_board.html`
- reference board는 검토용 샘플일 뿐이며 Unity 씬, 프리팹, 런타임 자산, UI 흐름에 연결되지 않았다.
- 사용자가 2026-06-09에 1단계 방향을 검토하고 승인했다.
- 2단계 샘플 작업을 진행할 수 있다. 단, 특정 모델, UI, 애니메이션, VFX, 머티리얼, 사운드를 실제 런타임에 붙이려면 각각 `artSample/` 검토가 필요하다.

### 2. 선박 내부 모델링 패스

목표: 현재 6구역 graybox를 플레이 경로를 유지하는 modular production geometry로 교체한다.

작업 순서:

1. 6구역 공간 기준을 확정한다:
   - 조종실
   - 화물칸
   - 무기실
   - 비품창고
   - 동력실
   - 통제실
   - production용 복도 교체안과 램프
   - 사용자 확정 밀도 규칙: 복도는 최대 2명이 동시에 이동할 수 있는 폭을 지원하고, 각 방은 3명이 들어가면 꽉 차 보이게 한다.
   - 사용자 보충 확인: 복도 2명 규칙은 좌우 폭 기준이며, 앞뒤로 이동하는 인원 수를 제한하지 않는다.
   - 사용자 확정 마감 규칙: 선박 내부는 고급스럽기보다 투박하고, 많이 사용했으며, 실용적인 느낌이 주가 되어야 한다.
   - 사용자 확정 복도 규칙: 현재 곡선 복도는 임시방편이므로 production에서는 제거한다.
   - 사용자 확정 폐쇄형 내부 규칙: production 선박 내부는 현재처럼 벽 없는 열린 상태가 아니며, 방과 복도에 물리적 벽, 문틀/문턱, 천장 구조를 세워 닫힌 화물선 내부로 읽히게 한다.
2. modular kit을 만든다:
   - 바닥 조각
   - 벽 조각
   - 천장 조각
   - 코너 조각
   - 복도 segment
   - 램프 segment
   - 문/문턱 frame
   - 케이블 tray와 pipe
   - hazard light와 beacon
3. graybox 방 shell을 한 번에 하나씩 교체한다.
4. collision은 단순하고 명확하게 유지한다.
5. 현재 interaction anchor와 smoke test 가정을 보존한다.
6. damage-state visual layer를 추가한다:
   - normal
   - warning
   - offline
   - heavily damaged
   - total loss

우선순위:

1. 화물칸과 메인 복도: navigation readability를 정의하기 때문이다.
2. 조종실: 운송이 시작되는 공간이며 첫인상에 영향을 준다.
3. 무기실: 수동 포탑과 장비 루프가 의존한다.
4. 통제실: CCTV/state UI에 읽기 쉬운 패널이 필요하다.
5. 동력실: damage/overclock feedback이 의존한다.
6. 비품창고: inventory/supply loop는 기능적으로 존재하지만 시각적으로는 placeholder 상태다.

산출물:

- modular ship kit prefab.
- 방 prefab 또는 씬 교체 계획.
- collision과 navigation pass.
- room-state material variant.

완료 기준:

- 기존 Phase 4/6/8/10/12/16/18 smoke 경로가 계속 통과한다.
- 플레이어가 복도나 interaction zone에 끼지 않는다.
- 모든 핵심 장치가 1인칭 거리에서 읽힌다.
- 모든 방과 복도는 바닥 표식만이 아니라 벽, 문틀/문턱, 천장 구조로 구획된다.
- 빌드 성능이 합의된 목표 안에 남는다.

2026-06-09 샘플 상태:

- 2단계 선박 내부 모델링 샘플을 준비했다:
  - `docs/SHIP_INTERIOR_MODELING_SAMPLE.md`
  - `artSample/ship_interior_modeling_sample.html`
- 샘플 범위는 화물칸, 메인 복도 straight/angled/ramp/threshold kit, 조종석 전면 frame, 무기실 포탑 station, 시각 scale 제안, damage-state layer다.
- 샘플은 Unity 씬, 프리팹, 런타임 자산, 머티리얼, collision, UI 흐름에 연결되지 않았다.
- 사용자가 2026-06-09에 2단계 샘플 checklist를 검토하고 승인했다. 여기에는 cockpit helm/frame과 armory turret station을 첫 device prop art sample로 진행하는 결정이 포함된다.
- 런타임 선박 모델링은 다음 구현 작업에서 진행할 수 있다. 다만 작은 단위로 나누고 navigation, interaction anchor, smoke test를 기준으로 검증해야 한다.

2026-06-09 적용 상태:

- 2단계 선박 내부 모델링 패스를 실제 `CargoRunMvp` 생성 경로에 적용했다.
- 6개 방은 벽, 문틀/문턱, 천장 구조를 가진 닫힌 production shell로 교체했다.
- 복도 폭은 승인된 2인 동시 이동 기준에 맞춰 `2.6m`로 설정했다.
- Cargo Hold to Armory 임시 곡선 복도는 3-segment angled/ramp route로 교체했다.
- 화물칸 고정 프레임/스트랩, cockpit forward frame/helm 표면, armory turret station support frame을 실제 씬 생성에 포함했다.
- 새 production material은 `Assets/_Project/Art/Ship/Materials` 아래에 생성했다.
- 검증은 `Run-PostDetailedStage2ShipInteriorSmoke.ps1`, harness, EditMode, PlayMode, Windows dev build까지 통과했다.
- 2단계 구현은 완료 상태이며, 다음 단계 진행 전 사용자 씬 검토가 필요하다.

2026-06-09 검토 피드백 수정:

- 사용자가 플레이 중 확인한 통제실-무기고/통제실-화물칸 복도 겹침, 비품실-화물칸 통행 불가, 방-복도 접합부 낙하 위험을 수정했다.
- `Control Room -> Armory`는 `Control Room -> Cargo Hold`와 분리된 4-segment perimeter route로 변경했다.
- `Cargo Hold -> Supply Room`은 별도 3-segment ramp route로 재배치했다.
- 모든 corridor route point에 landing plate를 추가해 방과 복도 접합부가 끊기지 않게 했다.
- 전용 검증은 `SupplyCargoSegments=3`, `ControlArmorySegments=4`, route separation, 주요 landing/door frame 존재까지 확인한다.

### 3. 게임플레이 소품과 장비 모델링 패스

목표: 가장 자주 보이는 소품과 장비 placeholder를 교체한다.

작업:

- 선박 장치:
  - 조종석 helm
  - 동력실 power screen
  - 통제실 main screen
  - 무기실 turret handle
  - 비품창고 storage cabinet
  - 화물칸 status panel
- 화물과 선박 소품:
  - 의뢰 화물 container
  - 개인 화물 container
  - 화물 strap과 bracket
  - 경고 label
  - repair panel
  - damaged panel
  - game-over escape pod / discarded pod visual
- 플레이어 장비:
  - stick first-person view
  - musket first-person view
  - 기본 protective suit readout
  - light blade
  - electric mine
  - corridor purifier
  - presence detector
- 상점/수리/의뢰 UI 표면:
  - diegetic terminal shell
  - button/indicator mesh
  - 읽기 쉬운 screen backing panel

우선순위:

1. 1인칭 장비와 무기실 소품.
2. 조종석/통제실/동력실 screen.
3. 화물칸 화물 소품.
4. 특수 의뢰 장비.
5. 상점/수리/의뢰 terminal shell.

산출물:

- prop prefab library.
- first-person equipment prefab.
- normal/damaged 상태용 material variant.

완료 기준:

- slot switching, shop purchase, combat interaction 테스트가 계속 유효하다.
- 1인칭 장비 에셋이 중앙 시야나 HUD를 가리지 않는다.
- 핵심 interactable이 텍스트에만 의존하지 않고 시각적으로 구분된다.

### 4. 적과 캐릭터 모델링 패스

목표: 적 placeholder를 읽기 쉬운 production model과 기본 animation으로 교체한다.

원본:

- `docs/ENEMY_REFERENCES.md`와 `image/`를 필수 시각 reference 목록으로 사용한다.

모델링 우선순위:

1. 플레이어/운송자 body reference와 first-person arms.
2. Parvum: 현재 플레이 루프에서 활성화된 침입자이기 때문이다.
3. 세력별 대표 external target 1종:
   - alien lifeform external intrusion object
   - Cargo Freedom League boarding craft
   - space pirate boarding craft
4. 나머지 seed entities:
   - Fuga
   - Longa Arma
   - Tergo
   - Urzere
   - Societas
   - Monstrum
   - Mimesis
5. Alien lifeforms:
   - Cantabile
   - Con Spirito
   - Accelerando
   - Grave
   - Smorzando
   - Ostinato
   - Dolore
6. Cargo Freedom League:
   - Negatif
   - Rebellion
   - Resistance
   - Revolution
7. Space pirates:
   - Pahur
   - Kurus
   - Istante
   - Ata

애니메이션 우선순위:

- idle
- locomotion
- attack anticipation
- attack impact
- hit reaction
- death/neutralized
- 필요 시 special behavior pose
- first-person weapon use animations

구현 정책:

- 첫 패스는 simple rig와 짧은 loop를 사용할 수 있다.
- 각 적은 상세 texture보다 먼저 읽기 쉬운 silhouette가 필요하다.
- 모델에서 떠오른다는 이유로 새 적 mechanic을 추가하지 않는다.
- 공격 timing은 기존 gameplay rule과 맞아야 하며, 변경이 필요하면 별도 승인을 받는다.

산출물:

- enemy prefab set.
- character/first-person arm prefab set.
- basic animation controller set.
- enemy visual checklist 갱신.

완료 기준:

- Parvum과 최소 1개 external target이 HUD text 없이도 구분된다.
- attack animation timing이 hit/damage timing과 맞는다.
- 기존 intruder 및 turret smoke test가 계속 통과한다.

### 5. 조명, VFX, 분위기 패스

목표: 현재 시스템을 기능 prototype이 아니라 horror/survival transport game처럼 느껴지게 만든다.

작업:

- 조명:
  - 방별 low-key lighting
  - emergency red/yellow alert
  - 손상된 방 flicker pattern
  - 저시야 복도 처리
  - 행성 허브와 선박 내부 lighting 분리
- VFX:
  - external hazard warning
  - manual turret muzzle flash와 hit result
  - asteroid/external target destruction
  - intruder hit/neutralized feedback
  - ship damage sparks/smoke
  - repair/maintenance completion feedback
  - cargo damage/loss feedback
- 카메라와 post-processing:
  - subtle damage shake
  - low visibility fog
  - readability를 위한 contrast/brightness guardrail

산출물:

- lighting profile.
- VFX prefab set.
- camera/post-processing profile.

완료 기준:

- threat state가 text를 읽기 전에 sound/light/VFX로 먼저 읽힌다.
- 어두운 방에서도 player navigation이 가능하다.
- 최악의 선박 상태에서도 성능 예산을 지킨다.

### 6. 사운드와 음악 제작 패스

목표: `ShipSignalAudioHooks` placeholder를 실제 사용 가능한 audio로 교체한다.

작업:

- ambience:
  - normal ship hum
  - cockpit ambience
  - cargo hold ambience
  - engine room ambience
  - control room ambience
  - damaged ship layer
  - planet hub ambience
- SFX:
  - footstep surface
  - device open/close
  - UI hover/confirm/back
  - turret fire/reload/hit
  - musket fire/reload/hit
  - stick hit/throw
  - intruder movement/attack/hit/death
  - external hazard warning
  - repair/shop/contract confirmation
  - game-over sequence
- mix:
  - distance와 occlusion rule
  - alert priority
  - 반복 warning cooldown
  - volume setting integration

산출물:

- audio folder structure와 clip import setting.
- audio event map.
- runtime audio routing과 mixer plan.

완료 기준:

- ship damage, external danger, intruder cue가 들리고 서로 구분된다.
- audio setting save/load가 계속 동작한다.
- 반복 warning이 spam되거나 critical cue를 덮지 않는다.

### 7. UI/UX와 다이제틱 표현 패스

목표: debug처럼 보이는 text panel을 읽기 쉬운 production UI로 교체한다.

작업:

- HUD:
  - health/protection
  - ship room map
  - transport progress
  - hazard state
  - intruder state
  - equipment slots
  - 필요한 경우 wallet/repair alert
- fullscreen mode:
  - manual flight
  - manual turret
  - settlement
  - maintenance
  - planet hub
  - shop
  - contract board
  - cargo depot
  - settings
- interaction prompt:
  - 간결한 device label
  - icon-assisted prompt
  - disabled-state reason
  - ESC/cursor behavior consistency
- accessibility:
  - high contrast mode coverage
  - reduced shake coverage
  - text size/readability pass
  - 필요 시 input remap plan

산출물:

- UI style guide.
- reworked HUD와 screen prefab.
- core UI navigation용 PlayMode smoke check.

완료 기준:

- 현재 플레이 루프를 developer/debug text를 읽지 않고 완료할 수 있다.
- ESC와 cursor behavior가 room panel, fullscreen mode, planet screen 전반에서 일관된다.
- 목표 해상도에서 UI text가 겹치지 않는다.

### 8. 콘텐츠 완성 패스

목표: 구현된 시스템을 완성된 비네트워크 게임으로 만든다.

작업:

- 의뢰 다양화:
  - association route set
  - private route set
  - special contract route set
  - high-risk route set
- 행성 다양화:
  - planet trait
  - visit record
  - hazard unlock progression
  - 승인된 경우 repair/shop/cargo depot 차이
- 적 encounter 배치:
  - early-game encounter mix
  - mid-game encounter mix
  - high-risk special encounter mix
  - faction-specific pacing
- 장비 progression:
  - unlock timing
  - shop availability
  - special equipment usefulness
  - inventory friction
- event pacing:
  - 첫 hazard 전 downtime
  - stacked hazard guardrail
  - recovery window
  - game-over escalation

산출물:

- content matrix.
- encounter pacing matrix.
- progression checklist.
- updated manual playtest scenario.

완료 기준:

- early, mid, high-risk playtest scenario를 개발자 개입 없이 플레이할 수 있다.
- 돈을 벌고, 수리하고, 장비를 사고, 더 어려운 의뢰를 수락할 명확한 이유가 생긴다.
- 원본 수치가 있는 balance는 승인 없이 변경하지 않는다.

### 9. 밸런스, QA, 회귀 하드닝

목표: 비네트워크 게임을 release candidate 작업에 들어갈 수 있을 만큼 안정화한다.

작업:

- 구조화된 playtest pass를 실행한다:
  - 첫 30분
  - 첫 debt recovery
  - 반복 mid-game hauling
  - high-risk special contract
  - total-loss recovery
  - game-over route
- 버그를 severity로 triage한다:
  - blocker: crash, soft lock, save corruption, impossible progression
  - major: wrong settlement, broken interaction, unreadable critical UI
  - minor: visual/audio polish, small layout issue
- 반복되는 regression에는 자동화 coverage를 추가한다.
- known-issues list는 완료 작업과 별도로 유지한다.

산출물:

- QA checklist.
- bug triage board/document.
- updated smoke suite.
- release-candidate test plan.

완료 기준:

- Harness, EditMode, PlayMode, focused smoke, Windows dev build가 통과한다.
- 알려진 blocker 또는 major progression bug가 남아 있지 않다.
- save/load가 반복 플레이 session을 견딘다.

### 10. 성능, 패키징, Steam 비네트워크 준비

목표: 비네트워크 Windows release candidate를 준비한다.

작업:

- 성능:
  - 최악의 선박 내부 상황 profile
  - 최악의 enemy encounter 상황 profile
  - UI-heavy planet hub/shop/contract screen profile
  - 조명, 그림자, 머티리얼, mesh count, audio voice 최적화
- 빌드:
  - development/release build profile 또는 script 분리
  - build version stamping
  - save compatibility check
  - crash/log collection policy
- Steam 비네트워크:
  - store capsule/key art 목록
  - screenshot/trailer capture plan
  - 기존 interface 뒤 achievement/stat 이름 준비
  - save policy 안정화 이후 cloud save 구현
  - Steam lobby 또는 online transport는 아직 하지 않음

산출물:

- Windows release-candidate build checklist.
- performance report.
- Steam store asset checklist.
- save/cloud/achievement integration plan.

완료 기준:

- Windows release candidate를 반복 가능하게 빌드할 수 있다.
- release candidate가 전체 verification ladder를 통과한다.
- Steam platform work가 core gameplay logic에 새지 않는다.

### 11. 네트워크 작업 보류 목록

목표: 네트워크 작업을 보류하되 잊지 않게 한다.

정책:

- 네트워크 작업은 비네트워크 게임이 완성된 뒤 시작한다.
- `docs/MULTIPLAYER_DESIGN_GATE.md`를 시작 설계로 사용한다.
- Steam-specific online transport 전에 DTO, fake transport, multi-instance local verification부터 시작한다.
- Steam lobby를 canonical gameplay state로 만들지 않는다.

## 권장 즉시 다음 작업

0단계와 1단계부터 시작한다:

1. asset inventory와 production folder policy를 만든다.
2. art direction과 asset bible을 만든다.
3. 첫 modeling slice를 고른다: cargo hold, cockpit, main corridor, Parvum, first-person arms, stick, musket, manual turret.

이 순서는 이미 플레이 가능한 루프에 다음 작업을 고정하고, 핵심 경험에서 보이지 않는 에셋에 시간을 쓰는 일을 줄인다.
# 2026-06-09 2단계 검토 피드백 최종 반영

- 실제 PlayMode 이동 검증을 추가한 뒤 `Supply Room -> Cargo Hold` 통행 불가 원인을 다시 수정했다.
- 최종 `Cargo Hold -> Supply Room`은 비품실에서 남쪽 하부 램프로 내려간 뒤 화물창고로 연결되는 2-segment route다.
- `Supply Room -> Armory` 출입구는 비품실 북쪽으로 분리해 비품실-화물창고 램프를 막지 않게 했다.
- `Control Room -> Armory` perimeter route는 더 남쪽 외곽으로 이동해 cargo-supply 하부 램프와 겹치지 않게 했다.
- 경사 복도 벽은 시각 벽과 물리 가드 콜라이더를 분리했고, cargo-supply 하부 램프는 좁은 코너 통행을 막지 않도록 별도 side guard collider를 두지 않는다.
- 전용 검증은 최종적으로 `ArmoryCargoSegments=4`, `SupplyCargoSegments=2`, `ControlArmorySegments=4`와 실제 `CharacterController` traversal을 확인한다.

# 2026-06-09 2단계 복도 구조 재수정 최종 기준

- 위의 `Control Room -> Armory` perimeter route 기록은 폐기한다. 통제실-무기고 직접 복도는 원본 연결 정의에 없으므로 생성하지 않는다.
- 금지 복도 오브젝트는 `Corridor - Control Room to Armory`, `Corridor - Control Room to Supply Room`, `Corridor - Supply Room to Control Room`이다.
- 금지 HUD 맵 선은 `Phase 16 Map Corridor - ControlRoom to Armory`, `Phase 16 Map Corridor - ControlRoom to SupplyRoom`, `Phase 16 Map Corridor - SupplyRoom to ControlRoom`이다.
- 최종 2단계 ship interior 연결은 9개 복도만 허용한다. 통제실에서 무기고로 가려면 통제실-화물칸, 화물칸-무기고처럼 이미 정의된 물리 복도를 이용해야 한다.
- 화물칸-무기고와 화물칸-비품실은 각각 2-segment 짧은 램프 경로이며, 비품실-무기고는 승인된 직접 연결 1개만 유지한다.
- 복도 floor와 landing에는 collider가 있어야 한다. 검증은 구멍, 추락 지점, 금지 복도 생성을 실패로 처리한다.
- 최신 검증 기준은 `ArmoryCargoSegments=2`, `SupplyCargoSegments=2`, `SupplyArmorySegments=1`, `Corridors=9`이다.

# 2026-06-09 복도 바닥/벽 재구성 최종 기준

- 위의 2-segment ramp 기준은 폐기한다. 화물칸-무기고와 화물칸-비품실은 중간 꺾임 없는 1-segment 직선 ramp로 생성한다.
- 복도 생성 순서는 바닥 연속성 우선이다. ramp floor collider가 실제 통행 발판이고, endpoint landing은 턱을 만들지 않도록 collider 없는 시각 덮개로만 둔다.
- 벽, 천장, threshold frame은 ramp pitch를 따라 기울이면 안 된다. 바닥만 경사를 따르고, 벽은 수직 yaw-only 패널로 세운다.
- 엔진룸-통제실 복도처럼 중간 꺾임이 필요한 연결은 route point와 실제 방 wall opening을 반드시 같은 위치로 맞춘다.
- 최신 검증 기준은 `ArmoryCargoSegments=1`, `SupplyCargoSegments=1`, `SupplyArmorySegments=1`, `Corridors=9`, `ValidatedRoutes=9`이다.

# 2026-06-09 복도 접합부 밀봉 기준

- 방 wall opening은 문 통과 높이까지만 비우고, 문 위쪽은 `Door Header Wall` bulkhead로 막아야 한다.
- 복도 시작/끝 threshold에는 visual-only floor lip, left/right reveal wall, ceiling cap을 둬 방-복도 접합부의 측면/상단 구멍을 막는다.
- 복도 ceiling bottom은 corridor wall top과 맞닿아야 하며, 벽과 천장 사이에 떠 있는 gap은 검증 실패로 처리한다.
- 이 밀봉 오브젝트는 시각용일 수 있지만, 실제 통행을 막는 collider를 추가해서는 안 된다.
