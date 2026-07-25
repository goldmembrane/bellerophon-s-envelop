# Con Spirito 색상/재질 샘플

## 목적

`image/con spirito(콘 스피리토).png` 기준 이미지를 보고, 현재 Unity에 배치된 원본 Con Spirito 모델(`con_spirito_original.fbx`)의 형태를 유지한 상태에서 색상과 재질 의도를 입힌 승인용 샘플입니다.

## 적용 기준

- 형태, 메시, 리깅은 수정하지 않았습니다.
- 현재 모델의 단일 메시를 위치 기준으로 재질 슬롯만 나누었습니다.
- 기준 이미지의 핵심 색 분포인 붉은 털, 어두운 하부 다리, 적갈색 발굽/코 계열, 어두운 꼬리 음영을 반영했습니다.
- 털 표면은 절차 생성 알베도 텍스처와 약한 범프 텍스처로 표현했습니다.

## 주요 파일

- 검토 페이지: `index.html`
- 비교 렌더: `renders/04_reference_side_by_side_overview.png`
- 모델 렌더: `renders/01_side_reference_color_application.png`
- 텍스처 분해: `renders/05_texture_material_breakdown.png`
- 내보내기 파일: `exports/con_spirito_current_model_colored_sample.glb`
- Blender 원본: `blender/con_spirito_current_model_colored_sample.blend`
- 제작 스크립트: `tools/build_con_spirito_sample.py`

## Unity 반영 계획

사용자가 이 샘플을 승인하면 별도 승인 범위에서 현재 Unity 씬의 Con Spirito 모델에 동일한 재질 의도를 적용합니다. 승인 전에는 Unity 씬, 프리팹, 런타임 에셋을 변경하지 않습니다.
