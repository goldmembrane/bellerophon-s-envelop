# Fuga Unity Application Review

- Created at: 2026-06-30 21:58:34
- Scene: `Assets/_Project/Scenes/CargoRunMvp.unity`
- Placement root: `Approved Fuga Enemy Placement`
- Placed count: 7
- Placement rule: Fuga is placed below Approved Parvum Enemy Placement on negative Z, with the review camera on the current visual front of the rotated Fuga.
- Scene scale X/Y/Z: 0.25 / 0.25 / 0.25
- Requested uniform scale: 0.25
- Facing yaw: 180 degrees
- X placement spacing: 2.45m, measured minimum X gap: 0.466m, required minimum X clearance: 0.35m
- Camera position X/Y/Z: 57.865 / 1.795 / -36.199
- Fuga visual front direction X/Y/Z: 0 / 0 / -1
- Camera front distance: 5.25m
- Design reference H/W/D: 0.6m / 0.4m / 0.2m
- Static bounds X/Y/Z: 1.492m / 0.566m / 0.883m

- Corridor/Parvum root Z gap: 7.377m
- Desired Fuga/Parvum root Z gap: 7.377m
- Actual Fuga/Parvum root Z gap: 7.377m, minimum root clearance: 0.3m

## Animation States

- `Fuga_00_Static`: approved sample static comparison
- `Fuga_01_Idle`: slow continuous wingbeat
- `Fuga_02_Move`: faster and larger wingbeat with Motion Path target pulse
- `Fuga_03_Attack`: leans back, lunges, strikes with wingtip
- `Fuga_04_Hit`: recoils, squashes, wings droop, then recovers
- `Fuga_05_Death`: stops flapping, falls, folds broad wings near floor
- `Fuga_06_Consume`: opens mouth, leans forward, bites food/part
