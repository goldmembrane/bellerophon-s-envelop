# ck_warn04

CK-04 경고등 / 작은 물리 표시등 승인된 Blender 샘플입니다.

## 범위

- 포함: 전면 상부 얇은 적색 경고등 바, 중앙 천장 비상 경고등 본체, 천장 장착 브래킷, 보호 가드, 사이렌 그릴, 경고 표식, 배선, 마모 디테일.
- 제외: 좌우 벽면 경고등, 조종대 가장자리의 작은 경고등, 붉은 회전광 빔 모델, 자동/수동 운행 상태 패널, 운행 진행도, 구역 내구도 수치 표시, 메인 스크린 UI, 경고음 로직, 상호작용 컴포넌트.
- 회색/반투명 조종실 구조와 조종대는 배치 확인용 프록시입니다. 승인 대상은 경고등 세트입니다.

## 배치 기준

- 조종 시야를 가리지 않도록 전면 유리 중앙에는 부품을 두지 않았습니다.
- 전면 상부 프레임의 얇은 경고등 바는 유지했습니다.
- 천장 경고등은 추후 애니메이션으로 붉은 빛을 내뿜는 기준이 되도록 본체, 가드, 사이렌 그릴만 남겼습니다.

## 사용 에셋 후보

- `Assets/Sci-Fi Styled Modular Pack/Textures/projector_warning.png` (Sci-Fi Styled Modular Pack projector_warning texture)

## Unity 반영 상태

`CargoRunMvp`에 `Approved Cockpit 04 Warning` 루트로 반영했습니다.
천장 비상 경고등은 `Approved Cockpit 01 Structure` 기준 조종실 천장 한가운데에 배치했고, 스크린 위 경고등 바는 전면 스크린 바로 위에 둡니다.
적용 비교 캡처는 `unity_applied_comparison/index.html`에서 확인합니다.
콜라이더, 경고음, 자동/수동 상태 로직, 메인 스크린 UI는 이번 샘플 범위에 포함하지 않습니다.
