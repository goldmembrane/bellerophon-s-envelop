# Ship Interior Modeling Sample

Date: 2026-06-09

Status: stage 2 sample approved by user on 2026-06-09. This document defines the first ship-interior modeling sample after stage 1 art direction approval. No Unity scene, prefab, runtime material, collision, or gameplay anchor has been changed.

Related sample:

- `artSample/ship_interior_modeling_sample.html`

## Purpose

The goal is to preview the direction for replacing the 6-room graybox with modular production geometry while preserving the current playable loop.

This sample focuses on the first ship-interior slice:

- Cargo hold shell and cargo strap kit.
- Main corridor straight/angled/ramp/threshold kit.
- Cockpit helm and forward frame.
- Armory turret station.
- Room threshold language and low-visibility beacons.
- Damage-state visual layer direction.

## Non-Goals

- Do not attach these visuals to `CargoRunMvp`.
- Do not create Unity prefabs from this sample yet.
- Do not change scene geometry, colliders, nav paths, interaction anchors, smoke test assumptions, or gameplay timing.
- Do not replace runtime materials yet.
- Do not start first-person weapon, Parvum, UI skin, animation, or audio runtime integration from this sample.

## Layout Direction

The ship should preserve the existing 6-room identity:

- Cockpit: forward-focused, glass/frame silhouette, transport-start control center.
- Cargo hold: lower, wider, visually central, with strapped cargo and status panel.
- Armory: turret station and weapon storage identity.
- Supply room: compact cabinet wall identity.
- Engine room: dense heavy core and pipe identity.
- Control room: screen bank and CCTV surface identity.

The first modeling slice should establish cargo hold, corridors, cockpit, and armory before less visible rooms.

User-confirmed finish direction:

- The interior should not feel premium, sleek, or luxurious.
- The dominant impression should be a rough, heavily used cargo ship.
- Surfaces should show wear, practical repairs, grime, scratched paint, exposed structure, and utilitarian fastening.
- Cockpit and control screens can be clearer than surrounding walls, but they should still feel aged and functional rather than high-end.

## Proposed Spatial Reference

These values are review targets for art production. They do not become gameplay collision values until separately approved and implemented.

User-confirmed density rule:

- Corridors must be wide enough for up to 2 people to move at the same time.
- The 2-person rule is a width rule: 2 people can move side by side. It does not limit how many people can move front-to-back along a corridor.
- Each room should feel visually crowded when 3 people are inside.
- This means rooms should stay compact and pressure-heavy rather than becoming spacious sci-fi halls.

| Area | Proposed Visual Reference |
| --- | --- |
| Main corridor width | 2-person simultaneous movement width; art target `2.4 m to 2.8 m` |
| Tight service corridor width | still supports 2-person squeeze movement; art target `2.0 m to 2.2 m` |
| Main door/threshold height | 2.2 m |
| Room density | 3 standing players should make the room look crowded |
| Corridor movement rule | 2-person rule means side-by-side width only; front-to-back traffic count is not limited |
| Cargo hold central clear width | enough to read cargo, but not spacious; 3 players plus cargo should feel busy |
| Cargo hold height impression | taller than corridors, with visible overhead structure, but not warehouse-like |
| Ramp slope impression | readable descent into cargo hold, not steep enough to feel like a ladder |
| Cockpit helm height | 0.95 m to 1.15 m usable surface |
| Manual turret handle height | 1.1 m to 1.3 m |
| Cargo crate base unit | 1.2 m x 1.0 m x 1.0 m |
| Large cargo frame | 2.4 m x 1.6 m x 1.4 m |

## Modular Kit Preview

P0 modules for first implementation after approval:

| Module | Purpose | Notes |
| --- | --- | --- |
| `SM_Ship_Corridor_Straight_A` | baseline route piece | floor striping, wall ribs, cable tray sockets |
| `SM_Ship_Corridor_Angled_A` | production replacement for temporary curved routing | use angled/junction forms instead of preserving the current temporary curve |
| `SM_Ship_Ramp_Cargo_A` | cargo hold descent | low-visibility beacon edge markers |
| `SM_Ship_Threshold_A` | room identity boundary | room-specific color/shape cue |
| `SM_Ship_CargoHold_Wall_A` | cargo hold wall module | strap brackets and cargo frame sockets |
| `SM_Ship_CargoHold_Floor_A` | cargo hold floor module | heavy floor plates and tie-down points |
| `SM_Ship_Cockpit_Frame_A` | cockpit forward frame | dirty glass and structural silhouette |
| `SM_Ship_Armory_TurretStation_A` | turret station | first-person interaction readability |

## Visual Hierarchy

Readable order in first-person view:

1. Exit/route direction.
2. Critical interactable.
3. Current room identity.
4. Damage or hazard state.
5. Decorative industrial detail.

If decoration competes with navigation or interaction, remove decoration.

## Damage-State Layers

Each modeled room should support these visual overlays later:

- Normal: low-saturation industrial baseline.
- Warning: amber strips, small flicker, localized caution light.
- Offline: reduced panel light, darker room fill, dead screens.
- Heavily damaged: smoke/sparks later, burnt material overlays, red warning pockets.
- Total loss: catastrophic damage layer, emergency red/black read, but still clear enough for game-over presentation.

## Device Prop Scope Explanation

Approval checklist item 3, "Cockpit helm/frame and armory turret station as the first device props," means the first production prop samples would focus on the physical objects that the player sees and interacts with most often:

- Cockpit helm/frame: the worn forward cockpit frame, dirty glass, control surface, handles/buttons/screens, and the physical silhouette around the transport-start interaction point.
- Armory turret station: the manual turret grip, mount, surrounding armory support frame, local warning marks, and the physical silhouette around the turret interaction point.

This does not mean changing transport or turret gameplay rules. It also does not mean attaching these props to Unity yet. The user approved these two high-visibility device surfaces as the first device prop art samples after cargo hold/corridor direction.

## Approval Decisions

Before Unity modeling starts, these decisions are now recorded:

1. Cargo hold as the first modeled room.
2. Main corridor straight/angled/ramp/threshold kit as the first modular kit. The current curved corridor is a temporary workaround and should be removed in production.
3. Cockpit helm/frame and armory turret station as the first device props. Approved as proposed after explanation.
4. Proposed visual scale references. User clarified that 2-person corridor movement means side-by-side width only; front-to-back traffic count is not limited. Room density proceeds as proposed.
5. Damage-state layer direction. Approved as proposed.

## Next Step After Approval

The stage 2 sample is approved. The first real runtime asset pass should still be created in small slices:

1. Create `Assets/_Project/Art/Ship` and shared material folder structure.
2. Produce cargo hold and corridor kit meshes/prefabs.
3. Keep interaction anchors and existing scene flow intact.
4. Run focused smoke tests for navigation, interaction prompts, HUD/map, manual turret, and full MVP loop.
