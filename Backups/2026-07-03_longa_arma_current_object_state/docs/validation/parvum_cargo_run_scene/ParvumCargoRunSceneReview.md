# 파르붐 CargoRunMvp 전용 검토

- 기준 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 승인 샘플: `artSample/enemies/parvum_physics_rig_rework_sample/`
- 기존 Harness/EditMode/PlayMode/Build 및 기존 Validate/Ensure/Smoke/Run 검증 루프: 실행하지 않음
- 검토 산출 위치: `docs/validation/parvum_cargo_run_scene/`

## 적용 요약

| 항목 | 내용 |
| --- | --- |
| 배치 수 | 6개 |
| 비교용 정적 개체 | `Parvum_00_Static` |
| 애니메이션 개체 | `Parvum_01_Idle`, `Parvum_02_Move`, `Parvum_03_Attack`, `Parvum_04_Hit`, `Parvum_05_Death` |
| 표시 메시 | `Unified_Parvum_Reference_Matched_Single_Mesh` 단일 Renderer |
| 루트 모션 | `Rigidbody + BoxCollider + ParvumPhysicsMotionDriver`, 현재 검토 배치는 루트 이동 잠금 |
| Motion Path 역할 | 목표 Transform만 애니메이션하고 실제 런타임 이동 구조는 유지하되, 현재 씬 검토 배치는 Rigidbody 이동을 잠금 |
| IK/Joint/Jiggle 역할 | 비표시 helper target, Animation Rigging marker, ConfigurableJoint, JiggleRig marker 구성 |

## 텍스처/머티리얼

| 구분 | 적용 파일 또는 머티리얼 | 용도 |
| --- | --- | --- |
| 텍스처 | `parvum_slime_albedo.png`, `parvum_slime_roughness.png`, `parvum_slime_bump.png`, `parvum_white_fleck_mask.png` | 젖은 초록 슬라임 표면, 얼룩, 거칠기, 범프 |
| 텍스처 | `parvum_muzzle_scale_albedo.png`, `parvum_muzzle_scale_bump.png` | 몸통에서 이어지는 회녹색 주둥이 질감 |
| 텍스처 | `parvum_mouth_cavity_albedo.png`, `parvum_tooth_albedo.png`, `parvum_tongue_albedo.png` | 입 내부, 치아, 혀 표면 |
| 머티리얼 | `M_Parvum_Dark_Muzzle_Pores` | 승인 샘플 FBX 슬롯에 재매핑 |
| 머티리얼 | `M_Parvum_Deep_Mouth_Cavity_No_Line_Objects` | 승인 샘플 FBX 슬롯에 재매핑 |
| 머티리얼 | `M_Parvum_Embedded_Grey_Green_Muzzle_Texture` | 승인 샘플 FBX 슬롯에 재매핑 |
| 머티리얼 | `M_Parvum_Irregular_Embedded_Teeth` | 승인 샘플 FBX 슬롯에 재매핑 |
| 머티리얼 | `M_Parvum_Mouth_Tongue_Detail` | 승인 샘플 FBX 슬롯에 재매핑 |
| 머티리얼 | `M_Parvum_Wet_Marbled_Green_Slime_Texture` | 승인 샘플 FBX 슬롯에 재매핑 |

## 애니메이션 적용 방식

| 개체 | 방식 |
| --- | --- |
| `Parvum_01_Idle` | `Idle_Pulse_Surface_Jiggle` Shape Key와 Jiggle helper target 커브 |
| `Parvum_02_Move` | 낮은 전진 출렁임 중심. `Move_Squash_Forward_Slosh` Shape Key와 작은 Motion Path target 반복 |
| `Parvum_03_Attack` | 이동보다 큰 전방 압축/도약. `Attack_Bite_Core_Kick` + `Attack_Teeth_Chomp`, 입 IK/Joint target 씹기 커브 |
| `Parvum_04_Hit` | 깨지는 변형을 줄이고 `Hit_Slow_Recoil` 중심으로 뒤로 물러난 뒤 굼뜨게 복귀 |
| `Parvum_05_Death` | `Death_Flatten_Liquid_Spread` + `Death_Liquefy_Collapse`로 아래로 녹아내리고 퍼지는 액체화 |
