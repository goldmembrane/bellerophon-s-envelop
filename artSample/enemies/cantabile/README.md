# 칸타빌레 현재 모델 색상 샘플

## 상태

- 승인 상태: 사용자 검토 대기
- Unity 런타임 씬, 프리팹, 에셋, AI, 피격 판정, UI 흐름에는 연결하지 않았습니다.
- 기준 모델: `Assets/_Project/Art/Enemies/Cantabile/Models/cantabille.glb`
- 기준 이미지: `image/cantabile(칸타빌레).png`, `image/cantabile-beside.png`

## 검토 방법

- `index.html`을 열어 기준 이미지와 생성 렌더를 비교합니다.
- `exports/cantabile_current_model_colored_sample.glb`는 현재 GLB 메시를 유지한 색상 검토용 3D 파일입니다.
- `renders/03_reference_side_by_side_overview.png`는 정면/측면 기준 이미지를 생성 렌더와 나란히 배치한 비교 이미지입니다.

## 현재 모델 제약

- 원본 GLB는 UV와 텍스처가 없는 단일 메시입니다.
- 샘플은 현재 모델 기준을 유지하기 위해 생성 UV, 정점색, 절차적 텍스처를 함께 사용했습니다.
- 실제 Unity 적용 시에는 정점색/텍스처 대응 머티리얼 또는 검토용 GLB에서 파생한 메시 에셋 적용이 필요합니다.
