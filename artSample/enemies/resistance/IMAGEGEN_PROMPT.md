# 레지스탕스 보조 색·재질 시안

## 실행 방식

- `imagegen` 기본 내장 도구 모드
- 편집 대상: `renders/00_neutral_current_model.png`
- 색·재질 참조: `image/résistance(레지스탕스).png`
- 출력: `renders/00_imagegen_material_guide.png`

## 최종 프롬프트

```text
Use case: precise-object-edit
Asset type: game character material and color guide for a 3D art sample
Input images: Image 1 is the edit target and exact current Resistance model render; Image 2 is the color, material, and surface-wear reference only.
Primary request: Repaint only Image 1's existing surfaces so their colors and material qualities match Image 2 as closely as the unchanged model permits.
Materials/textures: worn off-white and silver metal on broad outer body surfaces; dark charcoal mechanical material at joints, waist, neck, inner limbs and recess-like existing forms; restrained bronze-brown accents; bright cyan-blue luminous-looking color accents painted only onto suitable existing narrow surface regions; olive green on the existing headband and ribbon-tail geometry; subtle chipped paint, grime, edge wear, roughness variation, and metal response.
Composition/framing: preserve Image 1 exactly.
Constraints: Change only color, material appearance, and surface texture. Preserve Image 1's exact silhouette, mesh shape, proportions, pose, anatomy, limb positions, head shape, ribbon geometry, camera, framing, studio background, and lighting direction. Do not add, remove, reshape, thicken, carve, or move any geometry. Do not invent armor plates, seams, panels, sockets, cables, face features, weapons, accessories, or raised details. Any panel-like impression must be flat color/material separation on the existing surface only. No text, logo, watermark, or extra objects.
Avoid: geometry redesign, facial redesign, new mechanical components, weapons, pose changes, camera changes, background changes.
```

## 사용 제한

- 이 이미지는 팔레트·재질 대비 참고용입니다.
- 2D로 추가된 판금 인상과 기계 세부는 최종 3D 샘플에 반영하지 않았습니다.
