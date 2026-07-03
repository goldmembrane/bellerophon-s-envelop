# Fuga Unity Application Review

- Created at: 2026-07-01 16:42:36
- Scene: `Assets/_Project/Scenes/CargoRunMvp.unity`
- Placement root: `Approved Fuga Enemy Placement`
- Placed count: 7
- Placement rule: Fuga is placed below Approved Parvum Enemy Placement on negative Z, with the review camera on the current visual front of the rotated Fuga.
- Scene scale X/Y/Z: 0.25 / 0.25 / 0.25
- Requested uniform scale: 0.25
- Facing yaw: 180 degrees
- X placement spacing: 2.45m, measured minimum X gap: 0.647m, required minimum X clearance: 0.35m
- Camera position X/Y/Z: 57.461 / 1.407 / -36.052
- Fuga visual front direction X/Y/Z: -0.078 / 0 / -0.997
- Camera front distance: 5.25m
- Player start position X/Y/Z: 57.531 / 0 / -35.154
- Player start front distance: 4.35m, facing dot: 1
- Design reference H/W/D: 0.6m / 0.4m / 0.2m
- Static bounds X/Y/Z: 1.482m / 0.556m / 0.885m

- Approved broad wing panel thickness: 0.14 sample units
- Death motion rule: Fuga_05_Death uses a looping review death sequence: sharp Animator tilt/wing fold plus faster FugaPhysicsMotionDriver Rigidbody.linearVelocity fall, immediate Rigidbody freeze, final still hold, and reset.

- Corridor/Parvum root Z gap: 7.377m
- Desired Fuga/Parvum root Z gap: 7.377m
- Actual Fuga/Parvum root Z gap: 7.377m, minimum root clearance: 0.3m

## Animation States

- `Fuga_00_Static`: approved sample static comparison
- `Fuga_01_Idle`: review playback driver loops vertical up/down wing flap beside the body
- `Fuga_02_Move`: review playback driver loops faster vertical up/down wing flap with Motion Path target pulse
- `Fuga_03_Attack`: keeps both wing roots attached, lifts the wings apart, then swats the front with both wingtips
- `Fuga_04_Hit`: visibly recoils, squashes, wings droop, then recovers
- `Fuga_05_Death`: looping review death sequence: hover, sharp tilt, fast Rigidbody fall, hard stop, final still hold, reset
- `Fuga_06_Consume`: keeps the lower jaw connected, opens the mouth, leans forward, and closes
