# Longa Arma Runtime Lowpoly Rigging Status - 2026-07-03

## Result

- Rig: `LongaArma_ARP_Detailed_Rig`
- Deform bones: 29
- Control bones: 17
- IK constraints: 5
- Shape Key drivers: 21
- Animation actions: `LongaArma_Static_Review, LongaArma_Idle, LongaArma_Move_Crawl, LongaArma_Attack_SlamDrag, LongaArma_Hit_Recoil, LongaArma_Consume_Peck, LongaArma_Death_MeltPuddle, LongaArma_RigPose_QuadrupedNeutral, LongaArma_RigPose_UpperBodyLift, LongaArma_RigPose_AttackSlamContact, LongaArma_RigPose_DeathPuddleCheck`

## Possible

- Created a replacement detailed Blender armature for the current runtime_lowpoly model.
- Separated deform bones and animator control bones with DEF_/CTRL_ naming.
- Added four independent leg IK targets and pole targets for quadruped crawl posing.
- Added left blade-arm IK target and pole target for lift, slam, and floor-drag posing.
- Added chest, pelvis, head, mouth, spine-lift, and body-morph controls needed by the requested motion set.
- Connected existing Shape Keys to CTRL_body_morph custom properties for body breathing, attack morph, consume peck, hit recoil, and death melt checks.
- Generated final static, idle, move, attack, hit, consume, and death Blender Actions on the detailed rig.
- Generated animation preview renders and rig-only review renders.
- Registered final animation and rig-pose actions with Auto-Rig Pro export flags and Game Engine Export scene properties where available.
- Exported rigged FBX and GLB review files.
- Applied the rigged FBX and all seven final clips to the Unity CargoRunMvp review placement through the approved refresh/bridge flow.

## Not Possible / Not Completed

- Auto-Rig Pro append_arp preset creation could not be executed in background mode because the add-on requires a 3D View UI overlay context.
- Auto-Rig Pro UI-dependent layer/color/custom-shape/export operators were skipped in the final background run after a native Blender crash during direct operator use.
- Auto-Rig Pro Smart marker automatic rigging was not used; this non-humanoid asymmetric quadruped needs a custom control layout.
- Death puddle visual quality is still provisional and may require shape-key sculpt cleanup after Unity review.
- Legacy generated `.anim` and controller files from the previous pass remain on disk, but the current scene placement uses the new final clip/controller names.
- Harness, EditMode, PlayMode, Build, and Git commands were not run.

## Auto-Rig Pro Operator Log

- FAIL: `ui_dependent_arp_operators` - `Skipped in background mode after append_arp and UI/icon-dependent ARP operators proved unsafe without a 3D View context.`
