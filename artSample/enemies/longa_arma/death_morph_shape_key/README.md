# Longa Arma Death Morph Shape Key Sample

- 목적: 롱가 아르마가 바로 사라지고 웅덩이가 켜지는 방식이 아니라, 사망 전용 단일 Morph 메시가 `몸체 -> 주저앉음 -> 액체 덩어리 -> 웅덩이`로 변형되는 샘플입니다.
- 기준 원본: `enemies model/longa arma.blend`
- 최종 웅덩이 기준: `enemies model/dead.fbx`

## 포함 파일

- `blender/longa_arma_death_morph_shape_key.blend`
- `exports/longa_arma_death_morph_shape_key.fbx`
- `exports/longa_arma_death_morph_shape_key.glb`
- `renders/01_death_basis.png`
- `renders/02_death_sag.png`
- `renders/03_death_collapse.png`
- `renders/04_death_puddle.png`
- `renders/05_death_sequence_overview.png`

## Shape Key

- `DEATH_01_body_sag`: 몸통과 머리가 아래로 처지는 1차 녹아내림입니다.
- `DEATH_02_collapse_liquid_mass`: 전체 실루엣이 바닥 가까이 찌그러지는 중간 액체 덩어리입니다.
- `DEATH_03_dead_fbx_puddle_match`: `dead.fbx` 웅덩이 풋프린트를 기준으로 납작해진 최종 형태입니다.

## 주의

- 이 샘플은 Unity 런타임에 적용하지 않았습니다.
- 원본 `longa arma.blend`와 `dead.fbx`는 직접 수정하지 않았습니다.
- 기존 롱가 아르마 메시와 `dead.fbx`는 토폴로지가 달라 직접 Shape Key로 연결할 수 없으므로, 사망 전용 동일 토폴로지 Morph 메시를 별도로 만들었습니다.
