# Longa Arma Low-Poly From Original

- 기준 원본: `enemies model/longa arma.blend`
- 목적: 원본 `mesh_node`의 실루엣을 유지하면서 로우 폴리 정적 샘플을 만드는 것입니다.
- 이번 샘플은 리깅/애니메이션/Unity 적용이 아닙니다.

## 제작 방식

- 원본 단일 고밀도 메시를 복제했습니다.
- 복제본에 Blender Decimate를 적용해 face 수를 289086에서 12000로 줄였습니다.
- 새 UV를 생성하고, 젖은 녹색 몸체/어두운 칼날/점액 재질을 새로 적용했습니다.
- 형상 자체는 원본 감량본을 사용했고, 임의로 다리나 칼날 팔을 새로 붙이지 않았습니다.

## 검토 기준

- 네 다리와 말형 몸체가 원본처럼 보이는지 확인해야 합니다.
- 왼쪽의 긴 칼날 팔이 추가 다리처럼 보이지 않고 한쪽 앞팔의 연장으로 읽히는지 확인해야 합니다.
- 세부 조형은 원본보다 줄었지만, 큰 실루엣은 원본과 일치해야 합니다.

## 산출물

- `blender/longa_arma_lowpoly_from_original.blend`
- `exports/longa_arma_lowpoly_from_original.fbx`
- `exports/longa_arma_lowpoly_from_original.glb`
- `renders/*.png`
- `textures/*.png`
