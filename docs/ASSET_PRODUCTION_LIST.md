# Asset Production List

Date: 2026-06-09

Status: stage 1 direction approved; stage 2 ship interior sample approved; stage 3 gameplay props/equipment sample was previously approved and integrated, but the 2026-06-12 rework review sample now supersedes the old Stage 3 visual direction for future art. The 2026-06-12 `stage3_rework_review` pass has been converted into Blender/FBX/HD texture assets and integrated into `CargoRunMvp` for the seven approved image groups. This list organizes the post-detailed-implementation asset work. It is not an implementation checklist for attaching assets to the game. Any art-heavy result must first be reviewed through `artSample/` and approved before runtime integration.

## Folder Policy

Proposed runtime asset folders:

| Folder | Purpose |
| --- | --- |
| `Assets/_Project/Art/Ship` | ship room modules, corridors, doors, damage overlays |
| `Assets/_Project/Art/Props` | cargo, devices, terminals, repair panels, shop props |
| `Assets/_Project/Art/Characters` | player/transfer body, first-person arms, shared humanoid rigs |
| `Assets/_Project/Art/Enemies` | enemy models, enemy materials, enemy animation prefabs |
| `Assets/_Project/Art/VFX` | muzzle flashes, hazard effects, damage effects, repair effects |
| `Assets/_Project/Art/UI` | UI sprites, icons, screen skins, diegetic screen materials |
| `Assets/_Project/Art/Materials` | shared master materials and material variants |
| `Assets/_Project/Audio/Ambience` | ship, planet, room, and event ambience |
| `Assets/_Project/Audio/SFX` | gameplay and device sound effects |
| `Assets/_Project/Audio/UI` | UI confirm/back/hover/error sounds |

Do not create or attach runtime assets in these folders until the relevant `artSample/` review is approved.

Before drawing or building an `artSample/` preview, write down how the approved sample would become Unity content: target scene or prefab, runtime root, existing anchor or interactable, camera viewpoint, scale, collision boundary, state-driven visibility, and which review-only pieces must remain outside live placement. The sample should make those runtime assumptions visible enough that approval can be translated into implementation without guessing.

## Naming Policy

Use stable ASCII asset names for runtime files.

| Prefix | Asset Type | Example |
| --- | --- | --- |
| `SM_` | static mesh | `SM_Ship_Corridor_Straight_A` |
| `SK_` | skinned mesh | `SK_Enemy_Parvum_A` |
| `P_` | prefab | `P_Ship_Cockpit_Helm` |
| `M_` | master material | `M_Ship_WornPaintedMetal` |
| `MI_` | material instance/variant | `MI_Ship_WornPaintedMetal_CargoHold` |
| `TX_` | texture | `TX_Ship_WornPaintedMetal_N` |
| `AN_` | animation clip | `AN_Parvum_Attack_A` |
| `AC_` | animation controller | `AC_Parvum` |
| `VFX_` | visual effect prefab | `VFX_Turret_MuzzleFlash_A` |
| `SFX_` | sound effect | `SFX_Turret_Fire_A` |
| `AMB_` | ambience loop | `AMB_Ship_EngineRoom_Loop_A` |
| `UI_` | UI sprite/icon | `UI_Icon_Repair_A` |
| `SCN_` | sample scene | `SCN_ArtSample_ShipLighting_A` |

Display names can remain Korean in UI text. Runtime file names should stay ASCII.

## Approval States

| State | Meaning |
| --- | --- |
| `Planned` | listed but not sampled |
| `Sample Required` | must produce `artSample/` preview before runtime work |
| `Sample Ready` | preview exists and awaits user inspection |
| `Approved For Runtime` | user approved the sample |
| `Runtime Integrated` | attached to actual scene/prefab/runtime asset |
| `Deferred` | intentionally postponed |

Current status: stage 1 direction is approved, stage 2 ship interior P0 samples are approved/integrated, and the 2026-06-12 Stage 3 rework is runtime integrated for the seven approved `artSample/stage3_rework_review` image groups. The current Unity comparison captures are stored under `artSample/stage3_rework_review/unity_current_pass/`; they are the active runtime comparison target, while the approved review PNGs and `index.html` remain the visual mood reference for future replacements.

2026-06-12 stage 3 rework mood lock:

- `artSample/stage3_rework_review/index.html` is the current visual mood reference for future art-related work.
- Future assets should match its closed, worn, low-key industrial cargo-ship tone, with desaturated metal, dark rubber, scratched screens, rare red/amber warnings, and practical installed forms.
- New art samples should include both part close-ups and the applied Unity-style room or first-person view when relevant.
- Existing primitive Stage 3 generated Unity objects should not be extended as the target look. The 2026-06-12 runtime pass replaces the visible Stage 3 presentation with Blender-authored FBX meshes, HD texture materials, room dressings, darker atmosphere, and hidden legacy graybox renderers where they blocked the approved art direction.
- Do not extend the earlier primitive Stage 3 runtime look as a visual quality target.
- Control room CCTV art must use the source layout: one large main screen, a small horizontal screen at the large screen's upper-left, and one vertical screen on the right. Avoid side-by-side multi-monitor CCTV banks.
- Stick art must read as a two-handed basic melee weapon with enough vertical length and two-hand grip spacing. The end can use a crowbar-like hooked pry tip, but the whole silhouette must not become a short one-handed crowbar.

2026-06-12 stage 3 runtime rework integration:

- `scripts/GenerateStage3ReworkBlenderAssets.py` and `Stage3BlenderReviewAssetBuilder` produce and import `Stage3Rework_All.blend`, per-image FBX files, a mesh library, and HD texture PNGs under `Assets/_Project/Art/Props/Stage3Rework`.
- `PostDetailedStage3GameplayPropsBootstrap` now builds the seven approved image groups into `CargoRunMvp`: cockpit helm/status surfaces, control-room CCTV wall terminal, engine-room power terminal, supply-room storage cabinet wall, cargo-hold crates/terminal dressing, armory turret grip/mount, and first-person equipment/cargo corridor view.
- `Stage 3 Art Sample Room Dressings` adds per-zone floor/ceiling/wall panels, cables, pipes, rails, hazard strips, CRT/screen glows, crates, shelves, and installed prop silhouettes so the live scene compares against the approved `stage3_rework_review` board instead of the previous primitive look.
- The old side-by-side control monitor bank and visible graybox/interactable placeholder renderers are hidden where they block the art pass, while colliders and gameplay components remain intact.
- The first-person stick now uses a longer hooked continuous mesh, two-hand grip wraps, metal collars, gloved-hand silhouettes, and a camera-safe placement that keeps HUD/map visibility.
- `PostDetailedStage3GameplayPropsEditorValidation` validates the Blender source, FBX mesh use, HD textures, material mix, room dressing coverage, hidden legacy placeholders, control-room source layout, and hooked two-handed stick shape against `artSample/stage3_rework_review/index.html`.
- Verification passed through Stage 3 smoke, Stage 3 art validation, harness, EditMode, PlayMode, Windows dev build, and `git diff --check`.

2026-06-10 stage 3 sample update:

- `artSample/gameplay_props_equipment_sample.html` was approved by the user.
- This sample covers first-person equipment placement, armory turret grip direction, ship device surfaces, cargo props, special equipment silhouettes, the corridor purifier maintenance-mounted icon direction, and material variants.
- Runtime integration is generated by `PostDetailedStage3GameplayPropsBootstrap` and verified by `Run-PostDetailedStage3GameplayPropsSmoke.ps1`.

2026-06-09 approval updates:

- Stage 1 art direction sample was reviewed and approved by the user.
- Stage 2 ship interior modeling sample was reviewed and approved by the user.
- User-confirmed ship interior finish: rough, heavily used, utilitarian cargo ship surfaces are preferred over luxurious or premium sci-fi surfaces.
- User-confirmed corridor rule: the current curved corridor is temporary and should be removed in production.
- User clarification: 2-person corridor movement is a side-by-side width standard only and does not limit front-to-back traffic.
- The stage 2 sample approves the ship interior direction, but runtime integration still needs to be done as a separate implementation task with validation.

## First Production Slice

The recommended first modeling slice after user approval:

| Asset | Why First | Required Sample |
| --- | --- | --- |
| Cargo hold shell and cargo strap kit | most repeated navigation and cargo-state area | material/lighting blockout board |
| Main corridor straight/angled/ramp kit | route readability and ship identity | modular kit preview |
| Cockpit helm and forward frame | first impression and transport start | cockpit screen/frame preview |
| Armory turret handle | manual turret mode anchor | prop close-up preview |
| First-person arms | all held-equipment animation depends on it | first-person pose preview |
| Stick | default weapon and early combat | first-person weapon preview |
| Musket | shop purchase and ranged combat | first-person weapon preview |
| Parvum | current active intruder | silhouette and turntable preview |

## Ship Interior Assets

| Priority | Asset | Notes | Approval State |
| --- | --- | --- | --- |
| P0 | Cargo hold shell | Preserve existing navigation and central cargo readability; rough used cargo-ship finish. | Approved For Runtime |
| P0 | Main corridor straight segment | Base modular route piece with worn utilitarian panels. | Approved For Runtime |
| P0 | Main corridor angled/junction segment | Production replacement for the temporary curved route; avoid sleek premium sci-fi finish. | Approved For Runtime |
| P0 | Ramp/threshold segment | Preserve cargo hold lower-position identity. | Approved For Runtime |
| P0 | Cockpit helm and forward glass frame | First transport interaction. | Approved For Runtime |
| P0 | Armory turret station | Manual turret interaction anchor. | Approved For Runtime |
| P1 | Control room single large screen set | CCTV/control readability; source layout is one large screen, upper-left horizontal helper screen, and right vertical screen. | Runtime Integrated |
| P1 | Engine room core | Overclock/damage feedback area. | Sample Required |
| P1 | Supply cabinet wall | Inventory/supply interaction anchor. | Sample Required |
| P1 | Room doors/threshold frames | Orientation and room identity. | Sample Required |
| P1 | Low-visibility corridor beacons | Navigation under stress. | Sample Required |
| P2 | Ceiling pipe/cable tray kit | Atmosphere detail. | Planned |
| P2 | Floor grate variants | Wear/detail pass. | Planned |
| P2 | Wall panel variants | Modular repetition control. | Planned |
| P2 | Damage overlay meshes | Room damage states. | Planned |
| P2 | Total-loss visual layer | Game-over and catastrophic damage. | Planned |

## Ship Props And Devices

| Priority | Asset | Notes | Approval State |
| --- | --- | --- | --- |
| P0 | Contract cargo container | Core transport target visual. | Approved And Integrated |
| P0 | Cargo strap/bracket set | Cargo hold identity. | Approved And Integrated |
| P0 | Cockpit status screens | Transport progress/readiness surface. | Approved And Integrated |
| P0 | Manual turret grip and mount | Armory interaction identity. | Approved And Integrated |
| P1 | Control room CCTV terminal | CCTV target display; single large screen rework integrated on 2026-06-12. | Runtime Integrated |
| P1 | Engine room power terminal | Overclock/damage state display. | Approved And Integrated |
| P1 | Supply room storage cabinet | Inventory storage surface. | Approved And Integrated |
| P1 | Cargo hold status panel | Cargo state readout. | Approved And Integrated |
| P1 | Repair panel kit | Damaged room interaction/readability. | Sample Approved; Runtime Placement Deferred |
| P2 | Warning label/stencil pack | Repeated detail. | Approved And Integrated |
| P2 | Loose industrial clutter | Must not block navigation. | Planned |
| P2 | Escape/discarded pod visual | Game-over sequence. | Sample Approved; Runtime Placement Deferred |

## Player And Equipment Assets

| Priority | Asset | Notes | Approval State |
| --- | --- | --- | --- |
| P0 | First-person arms | Required for weapon animation and scale. | Sample Required |
| P0 | Stick first-person model | Default weapon; hooked two-handed first-person rework integrated on 2026-06-12. | Runtime Integrated |
| P0 | Musket first-person model | Early shop weapon. | Approved And Integrated |
| P1 | Basic protective suit model/readout | Player identity and future multiplayer readability. | Approved And Integrated |
| P1 | Presence detector | Special contract equipment. | Sample Approved; Runtime Placement Deferred |
| P1 | Light blade | Special contract equipment. | Sample Approved; Runtime Placement Deferred |
| P1 | Electric mine | Special contract equipment. | Sample Approved; Runtime Placement Deferred |
| P1 | Corridor purifier | Special contract equipment. | Sample Approved; Runtime Placement Deferred |
| P2 | Treatment item variants | Item UI/held presentation. | Planned |
| P2 | Enhancement item variants | Item UI/held presentation. | Planned |

## Enemy And Character Assets

Use `docs/ENEMY_REFERENCES.md` and `image/` before any modeling.

| Priority | Asset | Role | Approval State |
| --- | --- | --- | --- |
| P0 | Parvum | active playable-loop intruder | Sample Required |
| P0 | Transfer/player body | scale and future coop readability | Sample Required |
| P0 | Generic alien external target | external biological threat readability | Sample Required |
| P0 | Cargo Freedom League boarding craft | raider external target readability | Sample Required |
| P0 | Space pirate boarding craft | pirate external target readability | Sample Required |
| P1 | Fuga | seed entity | Planned |
| P1 | Longa Arma | seed entity | Planned |
| P1 | Tergo | seed entity posture threat | Planned |
| P1 | Urzere | seed entity | Planned |
| P1 | Societas | seed entity | Planned |
| P1 | Monstrum | seed entity | Planned |
| P1 | Mimesis | seed entity placeholder until voice/mimicry scope is approved | Planned |
| P2 | Cantabile | alien lifeform | Planned |
| P2 | Con Spirito | alien lifeform | Planned |
| P2 | Accelerando | alien lifeform | Planned |
| P2 | Grave | alien lifeform | Planned |
| P2 | Smorzando | alien lifeform | Planned |
| P2 | Ostinato | alien lifeform | Planned |
| P2 | Dolore | alien lifeform | Planned |
| P2 | Negatif | Cargo Freedom League | Planned |
| P2 | Rebellion | Cargo Freedom League | Planned |
| P2 | Resistance | Cargo Freedom League | Planned |
| P2 | Revolution | Cargo Freedom League | Planned |
| P2 | Pahur | space pirate | Planned |
| P2 | Kurus | space pirate | Planned |
| P2 | Istante | space pirate | Planned |
| P2 | Ata | space pirate | Planned |

## Animation Assets

| Priority | Animation | Applies To | Approval State |
| --- | --- | --- | --- |
| P0 | first-person idle | arms/equipment | Sample Required |
| P0 | stick swing | stick | Sample Required |
| P0 | stick throw pose | stick | Sample Required |
| P0 | musket fire | musket | Sample Required |
| P0 | musket reload | musket | Sample Required |
| P0 | Parvum idle/move/attack/hit/death | Parvum | Sample Required |
| P1 | manual turret operate feedback | turret station | Sample Required |
| P1 | device interaction press/open | terminals/cabinets | Planned |
| P1 | external target hit/destroyed | external targets | Planned |
| P2 | faction-specific attack loops | later enemies | Planned |

Animation timing must match gameplay rules unless the user approves a timing change.

## VFX Assets

| Priority | VFX | Notes | Approval State |
| --- | --- | --- | --- |
| P0 | manual turret muzzle flash | gameplay feedback | Sample Required |
| P0 | turret hit spark/impact | target feedback | Sample Required |
| P0 | external target destruction | hazard neutralized feedback | Sample Required |
| P0 | room damage sparks/smoke | ship damage readability | Sample Required |
| P1 | intruder hit feedback | combat readability | Planned |
| P1 | repair completion effect | maintenance feedback | Planned |
| P1 | hazard warning screen/noise | alert presentation | Planned |
| P2 | organic intrusion residue | environment state | Planned |

## UI Assets

| Priority | UI Asset | Notes | Approval State |
| --- | --- | --- | --- |
| P0 | HUD icon set | health/protection/map/hazard/intruder/equipment | Sample Required |
| P0 | ship map production skin | preserve existing map function | Sample Required |
| P0 | interaction prompt style | concise device labels | Sample Required |
| P0 | settlement screen style | financial clarity first | Sample Required |
| P1 | maintenance screen style | repair clarity first | Sample Required |
| P1 | shop screen style | inventory/price/affordability clarity | Sample Required |
| P1 | contract board style | route/reward/risk clarity | Sample Required |
| P1 | manual flight screen style | full-screen mode clarity | Sample Required |
| P1 | manual turret screen style | target/ammo/reload clarity | Sample Required |
| P2 | settings screen style | accessibility/readability | Planned |

UI samples must be reviewed in `artSample/` before replacing runtime UI.

## Audio Assets

| Priority | Audio | Notes | Approval State |
| --- | --- | --- | --- |
| P0 | ship baseline ambience | core atmosphere | Sample Required |
| P0 | external hazard warning | must differ from intruder warning | Sample Required |
| P0 | intruder warning | internal danger signal | Sample Required |
| P0 | ship damage warning | room/ship state signal | Sample Required |
| P0 | turret fire/reload/hit | manual turret feedback | Sample Required |
| P1 | musket fire/reload/hit | equipment feedback | Sample Required |
| P1 | stick hit/throw | equipment feedback | Planned |
| P1 | UI confirm/back/error | repeated operation | Planned |
| P1 | repair/shop/contract confirmations | planet hub feedback | Planned |
| P2 | room-specific ambience loops | room identity | Planned |
| P2 | game-over sequence audio | failure presentation | Planned |

## Material And Lighting Review Package

Stage 1 sample review file:

- `artSample/art_direction_reference_board.html`

It covers:

- visual pillars
- palette swatches
- material targets
- lighting states
- enemy silhouette priority
- first production slice

User approval of this board is required before moving to ship interior modeling or runtime asset integration.

## Immediate Next Approval Request

Before milestone 2 starts, ask the user to approve or revise:

1. Visual pillars and palette direction.
2. Material and lighting targets.
3. First-person scale reference proposal.
4. Enemy silhouette priority.
5. First production slice.
