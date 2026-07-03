# Longa Arma Runtime Lowpoly Rigging Sample

- 생성 시각: 2026-07-03 13:34 KST
- 기준 모델: `blender/longa_arma_runtime_lowpoly.blend`
- 원본 참조 모델 `enemies model/longa arma.blend`는 수정하지 않았습니다.
- 이번 산출물은 승인된 `runtime_lowpoly` 모델을 기준으로 한 리깅, 최종 모션 액션, Unity 교체 적용 결과입니다.

## 현재 상태

- 리그: `LongaArma_ARP_Detailed_Rig`
- 변형 본: 29개
- 컨트롤 본: 17개
- IK 제약: 5개
  - 사족 보행용 4개 다리 IK
  - 왼쪽 칼날 팔 IK
- Shape Key 드라이버: 21개
- 메시 Armature Modifier: `LongaArma_ARP_DetailedRig_Armature`

## 최종 모션 액션

- `LongaArma_Static_Review`
- `LongaArma_Idle`
- `LongaArma_Move_Crawl`
- `LongaArma_Attack_SlamDrag`
- `LongaArma_Hit_Recoil`
- `LongaArma_Consume_Peck`
- `LongaArma_Death_MeltPuddle`

## 주요 산출물

- 리깅 스크립트: `rig_longa_arma_runtime_lowpoly.py`
- Blender 파일: `blender/longa_arma_runtime_lowpoly.blend`
- 리깅 FBX: `exports/longa_arma_runtime_lowpoly_rigged.fbx`
- 리깅 GLB: `exports/longa_arma_runtime_lowpoly_rigged.glb`
- 리깅 보고서: `rigging_report_2026-07-03.json`
- 리깅 상태 문서: `RIGGING_STATUS_2026-07-03.md`

## 검토 렌더

- `renders/animation_static_review.png`
- `renders/animation_idle_body_morph.png`
- `renders/animation_move_crawl.png`
- `renders/animation_attack_slamdrag_contact.png`
- `renders/animation_hit_recoil.png`
- `renders/animation_consume_peck.png`
- `renders/animation_death_meltpuddle.png`
- `renders/rig_overview_quadruped_neutral.png`
- `renders/rig_upper_body_lift_test.png`
- `renders/rig_attack_slam_contact_test.png`
- `renders/rig_death_puddle_morph_test.png`

## 가능했던 것

- 기존 단순 리그/액션 산출물을 폐기하고 같은 `runtime_lowpoly` 모델 안에 세부 리그를 다시 구성했습니다.
- `DEF_` 변형 본과 `CTRL_` 애니메이션 컨트롤 본을 분리했습니다.
- 이동 모션을 위해 4개 다리의 IK 목표와 pole 목표를 각각 분리했습니다.
- 공격 모션을 위해 상체, 머리, 오른 앞다리, 왼쪽 칼날 팔 컨트롤을 분리했습니다.
- 대기, 피격, 섭취, 사망 보조 변형을 위해 기존 Shape Key를 `CTRL_body_morph` 커스텀 속성 드라이버로 연결했습니다.
- Blender 안에 정적, 대기, 이동, 공격, 피격, 섭취, 사망 액션을 생성했습니다.
- Auto-Rig Pro가 제공하는 export 플래그와 Game Engine Export 씬 속성을 가능한 범위에서 등록했습니다.
- Unity `CargoRunMvp` 씬에 7개 검토 개체를 새 모션 이름으로 다시 배치했습니다.

## 진행하지 못했거나 아직 부족한 것

- Auto-Rig Pro `append_arp` 프리셋 생성은 백그라운드 실행에서 3D View UI overlay context가 없어 실행하지 못했습니다.
- ARP layer, color, custom shape, export UI operator는 직접 호출 중 Blender 네이티브 크래시가 발생해 최종 실행에서는 제외했습니다.
- ARP Smart marker 자동 리깅은 Longa Arma가 비인간형 비대칭 사족 구조라 사용하지 않았습니다.
- 사망 웅덩이 모션은 기능적으로 바닥으로 녹는 형태를 만들었지만, 시각 품질은 아직 임시 수준입니다.
- 기존 구버전 `.anim`/controller 파일은 삭제하지 않았습니다. 현재 씬 배치는 새 `Move_Crawl`, `Attack_SlamDrag`, `Consume_Peck`, `Death_MeltPuddle` 자산을 사용합니다.
- Harness, EditMode, PlayMode, Build, Git 작업은 승인 범위 밖이라 실행하지 않았습니다.

## Unity 확인 산출물

- `docs/validation/longa_arma_cargo_run_scene/longa_arma_unity_model_cam.png`
- `docs/validation/longa_arma_cargo_run_scene/longa_arma_approved_front_vs_unity.png`
- 적용 로그:
  - `Logs/LongaArmaApply_20260703_fullmotion.log`
  - `Logs/LongaArmaInspect_20260703_fullmotion.log`
  - `Logs/LongaArmaCapture_20260703_fullmotion.log`
