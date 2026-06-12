# Art Direction Bible

Date: 2026-06-09

Status: stage 1 production target approved by user on 2026-06-09. On 2026-06-12, the Stage 3 rework review sample was fixed as the current mood and finish reference for future art-related work. This document defines the visual target for later modeling, UI, animation, VFX, lighting, and sound presentation work. It does not attach any asset to the game. Specific model, UI, animation, VFX, material, and sound results still require their own `artSample/` review before runtime integration.

Related sample:

- `artSample/art_direction_reference_board.html`
- `artSample/stage3_rework_review/index.html`
- `artSample/stage3_rework_review/unity_current_pass/` stores the current Unity comparison renders for the integrated Stage 3 rework pass.

## Core Direction

Bellerophon is a first-person 3D space cargo transport survival/horror game. The visual direction must support transport pressure, ship fragility, limited information, and fear from distance, sound, and poor visibility.

Use `docs/GAME_DESIGN.md` as the top-level source:

- Low-saturation industrial space.
- Closed interiors and narrow sightlines.
- Dark lighting with controlled readable silhouettes.
- Rough, worn surfaces.
- Limited visibility.
- Uncertain threats.
- Lethal Company is a mood and texture reference only. Do not copy its exploration/scavenging structure, quota loop, or visual identity wholesale.

## Locked Art Mood Reference

As of 2026-06-12, all new art-related samples and runtime art proposals must visually fit with `artSample/stage3_rework_review/index.html`.

This reference locks the following mood:

- Closed, cramped, practical cargo-ship interiors.
- Low-key lighting with readable silhouettes, not full black voids.
- Desaturated worn metal, scratched screen glass, dark rubber, bolts, grime, chipped paint, and used industrial surfaces.
- Controlled green/cyan display glow plus rare red/amber warning accents.
- Heavy functional props that look installed into the ship, not decorative standalone objects.
- First-person equipment that reads as worn survival gear and stays out of critical HUD/map sightlines.
- Control room CCTV surfaces follow the source layout: one large main screen, one small horizontal screen at the upper-left of the main screen, and one vertical screen to the right. Do not reinterpret this as a row of separate CCTV monitors.
- The basic stick must read as a two-handed melee weapon in first-person samples: long enough vertical silhouette, two-hand grip spacing, and an overhead/downward strike pose. Its end may have a crowbar-like hooked pry tip, but the full weapon must not collapse into a short one-handed crowbar silhouette.

Rules:

- Future ship props, equipment, UI surfaces, VFX previews, enemy presentation scenes, and lighting samples must harmonize with this Stage 3 rework review mood unless the user explicitly approves a different direction.
- Do not use the earlier primitive Stage 3 runtime implementation as the visual quality target for future work. It remains a functional placeholder/integration reference only.
- New `artSample/` files should include both isolated asset views and the applied Unity-style zone view when the asset will appear in a room, first-person camera, or gameplay screen.

## Visual Pillars

### 1. Heavy Industrial Ship

The cargo ship should feel like a working transport vessel, not a clean sci-fi cockpit.

Rules:

- User-confirmed interior finish: the ship interior should feel rough, heavily used, and utilitarian rather than luxurious or premium.
- Use practical panels, brackets, straps, repair patches, pipe runs, cable trays, and heavy fasteners.
- Keep repeated modular surfaces imperfect through decals, edge wear, stains, and damage overlays.
- Avoid glossy luxury sci-fi surfaces except limited cockpit glass and screen panels.
- Important interactables can be cleaner or better lit than the background, but should still feel installed into the ship.

### 2. Navigation Under Stress

The player must be able to navigate under low visibility without the ship becoming unreadable.

Rules:

- Each room gets one strong spatial identity:
  - Cockpit: glass, forward frame, control silhouettes.
  - Cargo hold: open central mass, straps, cargo frames.
  - Armory: turret handle, weapon racks, hard warning marks.
  - Supply room: cabinet grid, labels, compact shelving.
  - Engine room: heavy core, pipe density, heat marks.
  - Control room: screens, CCTV surface, cable clutter.
- Corridors need repeated orientation cues: floor striping, ceiling pipe direction, beacon color, and room threshold shape.
- Darkness is allowed, but dead black hiding critical exits is not.

### 3. Threat By Signal Before Contact

Threats should often be understood first through sound, lighting, screen noise, movement in the distance, or silhouette.

Rules:

- External hazards use warning light, audio stingers, and HUD/map cues before damage.
- Intruders use silhouette and movement profile before detailed texture.
- Each faction should be readable by broad form language:
  - Seed entities: organic, intrusive, asymmetrical.
  - Alien lifeforms: unfamiliar biological forms, less tool-like.
  - Cargo Freedom League: improvised, raider, stolen/retrofitted hardware.
  - Space pirates: armed, tactical, formation-capable silhouettes.

### 4. Functional UI, Not Decorative UI

UI must help repeated play and ship operation.

Rules:

- Ship-critical HUD stays compact and readable.
- Fullscreen screens should feel like operational panels, not marketing pages.
- Use restrained colors and clear state hierarchy.
- Do not hide essential settlement, repair, contract, or shop numbers behind visual flair.
- Debug-like text panels must be replaced by production UI before release candidate.

## Palette Direction

The palette is muted and industrial, with controlled warning accents.

| Role | Suggested Color | Usage |
| --- | --- | --- |
| Deep ship shadow | `#101312` | Background voids, unlit wall recesses, ceiling depth. |
| Oxidized dark metal | `#2C3532` | Main walls, corridor panels, engine casing. |
| Worn blue gray | `#52616A` | Structural panels and readable midtone surfaces. |
| Desaturated olive | `#566451` | aged paint, supply/cargo industrial surfaces. |
| Bone label | `#B7B2A2` | small labels, worn stencils, UI secondary text. |
| Hazard amber | `#D89A3D` | warnings, interactable highlights, caution strips. |
| Emergency red | `#A33B35` | critical damage, game-over escalation, lockout state. |
| Screen cyan | `#6FA7A8` | low-intensity control room/cockpit display glow. |
| Organic bruise green | `#4D6A58` | seed/organic intrusion accent, used sparingly. |

Restrictions:

- Avoid a one-hue interface. Do not let the game become only blue, only green, only orange, or only purple.
- Avoid clean neon cyberpunk saturation.
- Avoid beige/tan dominance.
- Keep warning colors rare enough that they still mean something.

## Material Bible

| Material ID | Purpose | Finish | Key Treatment | Notes |
| --- | --- | --- | --- | --- |
| `MAT_Ship_WornPaintedMetal` | primary room panels | matte/semi-matte | chipped edges, grime, uneven paint | Main production material for ship walls. |
| `MAT_Ship_ExposedDarkMetal` | beams, door frames, brackets | rough metallic | scratches, dark oil residue | Use for structural weight. |
| `MAT_Ship_RubberStrap` | cargo straps, grips, seals | matte rubber | compressed edges, dust | Critical for cargo hold identity. |
| `MAT_Ship_CockpitGlass` | cockpit front glass | low-gloss transparent | grime, hairline scratches | Must not feel premium or polished. |
| `MAT_Ship_WarningPaint` | caution strips and warnings | matte paint | worn yellow/black or amber marks | Use sparingly near hazards/interactables. |
| `MAT_Ship_ScreenGlass` | cockpit/control UI panels | subtle emissive | scanline/noise layer | Functional worn displays, not luxury glass. |
| `MAT_Ship_DamagedBurnt` | damaged/offline room overlay | charred rough surface | soot, heat stains, melted edges | Layer or variant, not separate gameplay state. |
| `MAT_Organic_Intrusion` | seed/alien residue | damp organic roughness | vein-like ridges, low wet highlights | Use sparingly to signal non-ship threat. |
| `MAT_Enemy_SeedBody` | seed entities | organic matte/wet mix | asymmetry, scars, growth patterns | Silhouette first, texture second. |
| `MAT_Enemy_RaiderMetal` | Cargo Freedom League gear | mixed scrap metal | patched plates, visible repairs | Should feel improvised. |
| `MAT_Enemy_PirateArmor` | pirate armor/boarding craft | hard surface | formation-readable armor plates | Avoid clean military sci-fi polish. |

Technical guidance:

- Prefer few reusable master materials with material instances/variants.
- Normal/roughness variation should carry detail more than high-saturation albedo.
- Use emission for gameplay signals, not decoration.
- Make damaged overlays modular so room damage states can reuse them.

## Lighting Bible

| Lighting State | Purpose | Direction |
| --- | --- | --- |
| Normal ship state | baseline navigation | Dim but readable, cool industrial ambient, local light pools at devices. |
| Damaged room state | room problem signal | Flicker, lower fill, amber/red localized warning, visible damaged material layer. |
| Low visibility corridor | tension and uncertainty | Beacon-led path, fog/limited distance, no full black exits. |
| Hazard alert state | external danger | Amber warning rhythm, console flicker, HUD/map warning color, short audio cue. |
| Intruder alert state | internal danger | Directional room warning, low pulsing light, stronger contrast near affected room. |
| Planet hub state | recovery and decision phase | Slightly warmer, calmer, more readable UI light; still industrial, not cozy. |
| Game-over state | final failure | Red/black escalation, reduced controls, strong silhouette of ship/pod event. |

Rules:

- Critical interaction surfaces must have enough contrast at target brightness.
- Darkness should hide details, not block the route.
- Do not use decorative colored lights that compete with warning states.
- Build lighting variants per room only after the modular ship kit direction is approved.

## First-Person Scale References

These are proposed production scale references for art approval. They must not be applied to gameplay collision or room layout until the ship modeling pass is approved.

| Reference | Proposed Target |
| --- | --- |
| Player eye height | 1.65 m |
| Standing body height reference | 1.8 m |
| Main door/threshold height | 2.2 m |
| Main corridor width | 2-person side-by-side movement width; art target 2.4 m to 2.8 m |
| Tight service corridor width | 2-person side-by-side squeeze movement width; art target 2.0 m to 2.2 m |
| Corridor traffic rule | 2-person rule defines width only; it does not limit front-to-back corridor traffic count |
| Room density | 3 standing players should make any room feel crowded |
| Cargo hold central clear width | enough to read cargo while still feeling busy with 3 players present |
| Console interaction height | 0.95 m to 1.15 m |
| Manual turret handle height | 1.1 m to 1.3 m |
| Supply cabinet handle height | 0.8 m to 1.4 m range |
| Standard cargo crate | 1.2 m x 1.0 m x 1.0 m |
| Large cargo frame | 2.4 m x 1.6 m x 1.4 m |

## Enemy Silhouette Direction

Enemy art should start with silhouette sheets before detailed modeling.

Priority for first silhouette approval:

1. Parvum: active playable-loop intruder, small/fast internal threat.
2. Transfer/player body: first-person scale and multiplayer future readability.
3. Space pirate boarding craft: external target with combat clarity.
4. Cargo Freedom League boarding craft: external target with faction contrast.
5. Alien lifeform external object: biological external target contrast.
6. Fuga, Longa Arma, Tergo, Urzere, Societas, Monstrum, Mimesis.
7. Alien lifeform set.
8. Cargo Freedom League set.
9. Space pirate set.

Silhouette rules:

- Parvum must read as an internal crawling/attacking threat, not a generic blob.
- Seed entities should be asymmetrical and invasive.
- Cargo Freedom League should read as patched raider hardware and improvised boarding presence.
- Space pirates should read as coordinated, armed, and formation-capable.
- Alien lifeforms should read as biological and unfamiliar, not as pirates with organic texture.

## UI Presentation Direction

UI should move from debug panels to operational screens.

Targets:

- HUD: compact, readable, persistent only where needed.
- Settlement: financial breakdown first, visual polish second.
- Maintenance: ship state and repair action clarity first.
- Shop: inventory/price/affordability clarity first.
- Contract board: type, route, difficulty, reward, risk, and requirement clarity first.
- Manual flight/turret: full-screen mode clarity with minimal text.

Rules:

- Avoid large decorative panels with low information density.
- Use icons and clear state color where it helps repeated operation.
- Keep warning and unavailable states distinct.
- Use high contrast mode and reduced camera shake settings as production constraints.

## Animation Direction

Initial animation quality target is readable timing, not cinematic polish.

Priorities:

- First-person stick swing/throw.
- First-person musket fire/reload skeleton replacement.
- Manual turret interaction feedback.
- Parvum locomotion, attack, hit, neutralized.
- Door/terminal/cabinet interaction motion if required by readability.
- Enemy hit/death timing aligned with existing damage rules.

Rules:

- Do not alter gameplay timing because an animation looks better. If timing must change, ask for approval.
- Attacks need anticipation and impact frames that match damage windows.
- First-person arms must not cover critical HUD/map information.

## Sound Direction

Sound should support uncertainty and state recognition.

Targets:

- Ship baseline hum per area.
- External hazard warning.
- Intruder direction/room warning.
- Device interaction feedback.
- Weapon and turret feedback.
- Repair/shop/contract UI confirmations.
- Game-over sequence.

Rules:

- Warning cues need cooldowns.
- Intruder, external hazard, and ship damage cues must be distinct.
- Audio settings and reduced intensity options remain part of production constraints.

## Approval Gate

Stage 1 direction approval is complete. Before any specific art-heavy runtime integration:

- User reviews the relevant `artSample/` file for that specific asset or presentation pass.
- User approves or requests changes to that sample.
- Only then should the approved result be attached to Unity scenes, prefabs, runtime assets, or UI flows.

For ship interior modeling, the next review file is `artSample/ship_interior_modeling_sample.html`.
