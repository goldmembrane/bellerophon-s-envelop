# Longa Arma Animation-Ready Model Sample

- 기준 원본: `enemies model/longa arma.blend`
- 목적: 대기, 이동, 공격, 피격, 섭취, 사망 애니메이션을 나중에 제대로 넣을 수 있도록 모델 구조를 다시 만든 샘플입니다.
- 이번 산출물은 최종 애니메이션 클립이 아니라 애니메이션 가능한 모델링 구조입니다.

## 핵심 구조

- 단일 고밀도 원본 메시를 그대로 쓰지 않고, 몸통, 가슴, 골반, 목, 머리, 턱, 네 다리, 왼쪽 칼날 팔을 별도 파트로 재구성했습니다.
- 각 파트는 `LongaArma_AnimationReady_RigGuide`의 해당 `DEF_*` 본에 rigid 1.0 weight로 연결됩니다.
- 관절부는 `GOO_*` 점액/살점 덩어리로 겹쳐서 큰 동작에서도 찢어져 보이는 문제를 줄이도록 구성했습니다.
- 몸통, 가슴, 골반에는 대기/피격/사망용 Shape Key가 들어 있습니다.
- 사망 웅덩이는 본으로 억지 변형하지 않고 `ANIM_DEATH_puddle_target_mesh`로 전환할 수 있게 별도 타깃 메시를 포함했습니다.

## 애니메이션 가능 기준

- 대기: `PART_body_core`, `PART_chest_lift_mass`의 호흡 Shape Key로 몸통 모핑 가능
- 이동: 네 다리 체인을 각각 따로 키잉 가능
- 공격: 상체 리프트, 왼쪽 칼날 팔, 오른 앞다리 리프트/내리찍기 가능
- 피격: 머리/턱 본과 몸통 압축 Shape Key로 고개 흔들림과 recoil 가능
- 섭취: 목, 머리, 턱 파트로 뒤젖힘과 전방 peck 가능
- 사망: 몸통 flatten Shape Key와 웅덩이 타깃 메시로 녹아내림/웅덩이 전환 가능

## 산출물

- `blender/longa_arma_animation_ready_model.blend`
- `exports/longa_arma_animation_ready_model.fbx`
- `exports/longa_arma_animation_ready_model.glb`
- `renders/01_neutral_front.png`
- `renders/02_side_structure.png`
- `renders/03_move_crawl_structure_check.png`
- `renders/04_attack_lift_structure_check.png`
- `renders/05_death_puddle_structure_check.png`
- `textures/*.png`

## 확인 결과

- Blender 생성 스크립트는 종료 코드 0으로 완료됐습니다.
- `.blend` 재검사에서 필수 파트, 22개 가이드 본, 몸통/가슴/골반 Shape Key가 확인됐습니다.
- `.fbx` 파일은 다시 Blender로 import해 armature, 칼날 팔, 네 다리 upper 파트가 포함된 것을 확인했습니다.
- 원본 `REF_original_*` 고밀도 참조 메시가 `.fbx`에는 포함되지 않는 것을 확인했습니다.

## 주의

- 기존 `runtime_lowpoly` 결과는 이번 애니메이션 작업 기준에서 제외했습니다.
- 이 샘플은 Unity에 적용하지 않았습니다.
- 실제 애니메이션 클립은 이 구조가 승인된 뒤 별도 단계로 제작해야 합니다.
