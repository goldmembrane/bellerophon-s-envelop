# 푸가 텍스처/머티리얼 분석

## 1차 기준 이미지

- `image/fuga2(푸가).png`
- `image/fuga2-back.png`
- `image/fuga2-beside.png`

## 표면 분석

- 몸체: 젖은 녹회색 피부, 불규칙한 돌기, 점액성 하이라이트가 필요합니다.
- 머리와 눈 주변: 눈두덩이 두껍고 얼굴 전면이 두꺼비처럼 낮게 돌출되어야 합니다.
- 날개: 바깥쪽은 차가운 녹회색 깃, 안쪽은 어두운 올리브/갈색이 섞인 층상 깃입니다. 깃은 단일 면이 아니라 겹겹이 쌓인 판형 조각처럼 보여야 합니다.
- 눈: 황금색 세로 동공이 정면 시선을 만듭니다.
- 하단 장식: 돌 또는 소라 껍질 같은 매달린 장식이며, 새겨진 문양과 거친 roughness가 필요합니다.

## 생성 텍스처

- `fuga2_wet_green_bumpy_body_albedo.png`
- `fuga2_body_wart_bump.png`
- `fuga2_olive_feather_albedo.png`
- `fuga2_inner_brown_olive_feather_albedo.png`
- `fuga2_lower_shell_leaf_albedo.png`
- `fuga2_golden_eye_albedo.png`

## 한계와 확인 필요 사항

- 기준 이미지의 정확한 뒷면 구조와 장식 부착 방식은 보이는 이미지에서 추론했습니다.
- Unity 적용 시에는 이 샘플을 분위기 참고가 아니라 재현 대상으로 두고, 렌더 비교를 통해 추가 보정해야 합니다.
