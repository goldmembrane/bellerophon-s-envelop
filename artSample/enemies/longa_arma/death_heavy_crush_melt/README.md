# Longa Arma 사망 Heavy Crush Melt 샘플

- 생성 시각: 2026-07-04 15:44:37 KST
- 목적: `dead.fbx`를 사용하지 않고 기존 Longa Arma 런타임 메시가 무거워져 주저앉고, 압착되고, 바닥으로 퍼지는 사망 연출을 검토합니다.
- 기준 모델: `artSample/enemies/longa_arma/runtime_lowpoly/blender/longa_arma_runtime_lowpoly.blend`
- 원본 참고: `enemies model/longa arma.blend`
- Unity 적용 상태: 적용하지 않음

## 검토 방식

- `index.html`에서 프레임 타임라인을 확인합니다.
- `frames/`에는 1~96프레임 중 주요 12개 프레임이 있습니다.
- `renders/06_sequence_overview.png`는 시작, 처짐, 붕괴, 최종 퍼짐 상태를 한 장에 비교합니다.

## 모션 구성

- 1프레임: 기존 Longa Arma 자세와 동일한 시작 상태입니다.
- 16프레임: 몸 전체가 갑자기 무거워진 것처럼 아래로 처집니다.
- 42프레임: 몸통, 머리, 다리, 칼날 팔이 바닥 쪽으로 압착됩니다.
- 76프레임: 기존 메시의 정체성을 유지한 채 바닥으로 넓게 퍼집니다.
- 96프레임: 새 웅덩이 모델로 교체하지 않고 같은 메시가 바닥으로 넓게 퍼진 최종 상태입니다.

## 포함 파일

- `blender/longa_arma_death_heavy_crush_melt.blend`
- `exports/longa_arma_death_heavy_crush_melt.fbx`
- `exports/longa_arma_death_heavy_crush_melt.glb`
- `renders/*.png`
- `frames/*.png`
- `ASSET_MANIFEST.json`
- `DEATH_HEAVY_CRUSH_MELT_STATUS_2026-07-04.md`
- `index.html`

## 주의

- 이 샘플은 Unity 씬, 프리팹, 런타임 에셋에 연결하지 않았습니다.
- `dead.fbx`는 사용하지 않았습니다.
- 기존 Longa Arma 메시에서 생성한 동일 토폴로지 Shape Key 변형만 사용했습니다.
