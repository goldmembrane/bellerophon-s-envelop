# Asset-Based Ship Dressing Plan

Date: 2026-06-13

Status: revised plan after abandoning the procedural Blender artSample reproduction loop.

## Decision

The previous Blender hard-surface reproduction workflow is no longer the active path. It failed to reach the required structural gate and kept repeating the same visual failure mode. The new direction is to use the imported Asset Store packs as the main production source, then adapt, recolor, combine, and place them so the ship feels like Bellerophon's rough industrial cargo vessel.

This does not delete the old `artSample/stage3_hardsurface_reproduction_sample/` files. They are retained as historical output only and must not be used as the approval basis for the next Unity work.

The apply-first-and-fix-later direction is now abandoned for future visual work. New room/corridor dressing changes must be previewed first under `artSample/asset_dressing_samples/`, reviewed by the user, and only then implemented in the runtime Unity scene.

## Imported Asset Sources

| Asset folder | Confirmed role | Current inventory |
| --- | --- | --- |
| `Assets/Heavy Station Kit` | Primary ship shell, corridor, wall, floor, railing, arch, light, heavy industrial structure | 198 prefabs, 259 FBX files, 48 materials, 110 PNG files, 18 TGA files |
| `Assets/Sci-Fi Styled Modular Pack` | Secondary modular corridor pieces, floor/wall variants, windows, lights, joints, stairs, glass panels | 152 prefabs, 202 FBX files, 24 materials, 23 PNG files |
| `Assets/ScifiOfficeLite` | Control/cockpit/supply room props: server rack, office chair, tables, shelves, monitors, doors, mechanical arms, ceiling lights | 46 prefabs, 33 FBX files, 28 materials, 102 PNG files |
| `Assets/GoldenFrame_Terminal_FREE` | Diegetic terminal/screen prop candidate | 1 prefab, 1 FBX file, 3 materials, 2 PNG files |

## Target Use Map

| Game area | Primary asset source | Intended result |
| --- | --- | --- |
| Main corridors | `Heavy Station Kit`, `Sci-Fi Styled Modular Pack` | Replace the current dull graybox feel with modular floors, bulkheads, wall panels, ceiling depth, railing, warning lights, and route-readable thresholds. |
| Cargo hold | `Heavy Station Kit`, `Sci-Fi Styled Modular Pack` | Keep central cargo readability, add heavy flooring, wall panels, beams, handrails, cargo frames, straps, and worn warning accents. |
| Cockpit | `Heavy Station Kit`, `ScifiOfficeLite`, `GoldenFrame_Terminal_FREE` | Build a practical forward control area with frame depth, glass/window pieces where suitable, installed terminals, chair/desk silhouettes, and low-key screen glow. |
| Control room | `ScifiOfficeLite`, `GoldenFrame_Terminal_FREE`, `Sci-Fi Styled Modular Pack` | Preserve the source CCTV layout: one large main screen, one small upper-left horizontal helper screen, and one right vertical screen. Add server rack/cable/terminal density without creating a generic monitor wall. |
| Engine room | `Heavy Station Kit`, `Sci-Fi Styled Modular Pack` | Create the heaviest industrial space: beams, pipes, dark metal panels, warning lights, floor grates, and power-core framing. |
| Supply room | `ScifiOfficeLite`, `Sci-Fi Styled Modular Pack` | Use shelves, cabinet-like forms, trays, labels, and compact clutter while keeping inventory interaction anchors clear. |
| Armory | `Heavy Station Kit`, `Sci-Fi Styled Modular Pack` | Keep turret/manual weapon identity with hard frames, hazard marks, heavy supports, and wall-mounted equipment silhouettes. |

## New Workflow

### 0. Asset Dressing Sample Approval Gate

Goal: prevent incorrect Asset Store prefab choices from being applied directly to the runtime ship.

Tasks:

- Before each area pass or visible correction, create a review sample under `artSample/asset_dressing_samples/`.
- Use a separate folder per step, for example `artSample/asset_dressing_samples/step03_cargo_hold_2026-06-13/`.
- Include at least three review angles before Unity runtime application:
  - player-eye view from the expected entry direction,
  - side or diagonal view that shows depth and wall/ceiling treatment,
  - top-down or layout view showing route clearance and interaction anchors.
- Include an asset manifest listing the exact prefab paths, intended role, scale intent, and whether each piece is structural, decorative, lighting, or masking.
- Include a Korean README explaining what will be applied to `CargoRunMvp` after approval and what will remain untouched.
- Record approval state in the sample folder. Until the user explicitly approves the sample, `unity_application_allowed` must be false.
- Do not add new unapproved art/asset dressing to runtime scenes, prefabs, runtime assets, or UI flows.

Output:

- Inspectable preview images, diagrams, HTML, or other reviewable files exist before runtime implementation.
- The user can reject or approve the exact visual direction before any new Unity scene dressing is applied.

### 1. Safety Baseline

Goal: make the runtime scene editable without losing the current playable structure.

Tasks:

- Preserve existing room anchors, interactables, camera, map markers, route points, and gameplay colliders.
- Create a single runtime dressing root in `CargoRunMvp`, for example `Asset Store Ship Dressing`.
- Place imported assets as visual dressing first. Do not replace gameplay scripts or interaction components in the first pass.
- Use simple parent objects per room so each area can be disabled or rebuilt independently.
- Keep vendor folders untouched. If a piece needs modification, create a project-owned prefab variant or wrapper instead of editing the imported source prefab directly.

Output:

- `CargoRunMvp` keeps all current gameplay anchors.
- New visual roots are organized by room and can be removed or revised without touching core gameplay.

### 2. Main Corridor, Threshold, And Corridor Cleanup Pass

Goal: make movement routes stop feeling like plain Unity placeholder geometry.

Tasks:

- Use `Heavy Station Kit` floors, wall pieces, arches, railings, and wall lights as the main corridor shell.
- Use `Sci-Fi Styled Modular Pack` corridor pieces, joints, windows, wall panels, and ceiling lights where they fit existing route bends and thresholds.
- Dress only the sides, ceiling, thresholds, and visual floor layer. Keep current passable floor/collider logic intact until traversal is verified.
- Add repeated orientation cues at every room entrance: door frame, side light, small color accent, or panel shape.
- Avoid dense clutter in corridors during the first pass.
- Keep the existing route topology, room connections, and approved corridor count unchanged.
- Clean up corridor visual defects during this pass: twisted-looking seams, protruding panels, abrupt lips, floating pieces, visibly misaligned threshold caps, and wall/floor overlaps that make the corridor look broken.
- If a protruding piece is only visual, move, rotate, scale, hide, or replace the visual piece under the new dressing root.
- If an existing visual shell piece makes the route look warped but gameplay collision is correct, prefer covering or visually masking it with the new dressing layer rather than rebuilding the corridor path.
- If a collider or walking surface actually blocks traversal, treat it as a functional bug and fix it only as far as needed to preserve the same corridor route and movement clearance.

Output:

- Corridors and room entrances are visually dressed.
- Player movement remains the same.
- Obvious corridor warping, sticking-out panels, and ugly transition defects are reduced before moving on to room interiors.

### 3. Cargo Hold Pass

Goal: turn the cargo hold into the visual center of the ship without blocking cargo gameplay.

Tasks:

- Use `Heavy Station Kit` floor plates, beams, railings, wall panels, and wall lights around the hold perimeter.
- Use `Sci-Fi Styled Modular Pack` floor/wall variants for secondary panel variation.
- Keep central cargo target readability. Cargo pieces, straps, and brackets should frame the cargo rather than hide it.
- Add high walls, ceiling depth, side rails, and industrial panels first; loose clutter comes later.
- Leave contract cargo, personal cargo, and existing cargo status interactions readable.

Output:

- Cargo hold reads as the main working bay of the ship.
- Existing cargo loop and movement path remain intact.

### 4. Cockpit Pass

Goal: make the starting interaction area look like an installed cargo-ship control position.

Tasks:

- Use `Heavy Station Kit` and `Sci-Fi Styled Modular Pack` structural pieces for forward frame, floor, side walls, ceiling, and window-like framing.
- Use `ScifiOfficeLite` chair/table/monitor-like props only where they can be made industrial, not office-showroom.
- Use `GoldenFrame_Terminal_FREE` as a terminal candidate if it reads better than the existing generated screen props.
- Keep the helm/status interaction anchor and player sightline readable.
- Do not overfill the forward view; cockpit needs a strong silhouette and usable navigation, not clutter.

Output:

- Cockpit feels functional and enclosed.
- Transport start/readiness surfaces remain obvious.

### 5. Control Room Pass

Goal: improve screen/terminal density while preserving the approved CCTV layout.

Tasks:

- Use `ScifiOfficeLite` server racks, mechanical arms, PC/monitor-like props, tables, and ceiling lights as supporting room clutter.
- Use `GoldenFrame_Terminal_FREE` for a diegetic terminal candidate.
- Use `Sci-Fi Styled Modular Pack` decorative wall panels and lights to make the screen wall feel installed.
- Preserve the CCTV rule: one large main screen, one small horizontal helper screen at upper-left, and one right vertical screen.
- Route cables/panels around the screens without turning the room into a generic multi-monitor bank.

Output:

- Control room reads as the ship's monitoring room.
- Existing CCTV/control interactions remain usable.

### 6. Engine Room Pass

Goal: make the engine room the densest and heaviest mechanical space.

Tasks:

- Use `Heavy Station Kit` beams, floor pieces, arches, ladders, railing, and wall lights.
- Use `Sci-Fi Styled Modular Pack` cylinders, wall panels, lights, and stairs as power-core framing and mechanical bulk.
- Build visual mass around the current engine-room interaction anchor, not on top of it.
- Add warning lights and heat/damage-friendly surfaces, but keep the overclock/power interaction readable.

Output:

- Engine room has clear heavy machinery identity.
- Damage/overclock feedback still has a readable focal point.

### 7. Supply Room Pass

Goal: make the supply room compact, stocked, and readable.

Tasks:

- Use `ScifiOfficeLite` shelves, trays, drawers, and cabinet-like pieces.
- Use `Sci-Fi Styled Modular Pack` wall panels and floor pieces for a less office-like base.
- Keep storage and inventory interaction points unobstructed.
- Avoid loose props that make the small room hard to navigate.

Output:

- Supply room reads as practical storage.
- Supply interaction remains easy to find.

### 8. Armory Pass

Goal: make the armory feel heavier and more dangerous without blocking turret/equipment use.

Tasks:

- Use `Heavy Station Kit` supports, railings, arches, and wall panels as hard industrial framing.
- Use `Sci-Fi Styled Modular Pack` lights, floor pieces, and wall panels for hazard-lit variation.
- Keep manual turret handle/mount area clear and visually emphasized.
- Add weapon-wall silhouettes or hard brackets only as visual dressing until later equipment-specific art exists.

Output:

- Armory reads as a weapon/control station area.
- Manual turret flow remains unchanged.

### 9. First Material And Lighting Cohesion Pass

Goal: make the different packs look like one ship after the first placement pass.

Tasks:

- Reduce overly clean or glossy imported materials with shared darker/desaturated overrides where needed.
- Favor worn metal, dark rubber, muted green/blue gray panels, amber warnings, and restrained cyan display glow.
- Remove or disable decorative lights that confuse warning states.
- Keep the first pass simple: obvious material clashes are fixed now; detailed grime and decals can wait.

Output:

- The ship no longer looks like four unrelated asset packs dropped into one scene.
- Lighting remains dark but navigable.

### 10. Verification And Revision Capture

Goal: verify the ship still plays correctly, then capture what needs visual correction.

Tasks:

- `.\scripts\Run-HarnessValidation.ps1`
- Existing ship/interactions smoke paths, especially Phase 4, Phase 6, Phase 8, Phase 12, Phase 16, and Phase 18 where relevant.
- `.\scripts\Run-EditModeTests.ps1`
- `.\scripts\Run-PlayModeTests.ps1`
- `.\scripts\Build-WindowsDev.ps1` if scene, package, or project settings are changed.
- Capture screenshots from the same room viewpoints after the placement pass.
- List visual problems as follow-up fixes: scale mismatch, clutter, material clash, blocked sightline, repeated pieces, lighting imbalance.

Output:

- Playable cargo ship with imported asset dressing applied.
- Follow-up visual fix list based on the live result, not abstract scoring.

## Immediate Next Step

Start with sample-first review in this order:

1. For the next visual change, create a sample folder under `artSample/asset_dressing_samples/`.
2. Prepare multiple preview angles and a prefab-path manifest for that step.
3. Ask the user to approve or reject the sample.
4. Only after approval, apply that exact asset direction to `CargoRunMvp`.
5. Run traversal and interaction validation.
6. Capture live screenshots and make the first visual revision list.
