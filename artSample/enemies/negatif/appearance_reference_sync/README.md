# 니게티프 외형 동기화 아트 샘플

## 목표

`image/négatif(네거티프).png`의 기계적인 색·재질·표면 특성을 현재 `négatif.fbx`에 맞춘 승인용 샘플입니다. 원본 FBX와 샘플 복제본의 메시·27본 리그·스킨 가중치는 수정하지 않았습니다.

## 기준 이미지에서 반영한 요소

- 머리·가슴·복부에 이어지는 패널 홈과 리벳 요철이 있는 회갈색 금속 판재 외장
- 판재 사이로 드러나는 검고 짙은 관절·다리·꼬리 내부 기계부
- 베이지·황갈색의 낡은 캔버스 화물 주머니
- 적재부의 윗면·옆면·아래 경계를 연속해서 감싸는 세 개의 짙은 적갈색 가죽 스트랩
- 구리색 코와 기계 포인트
- 볼이 아닌 머리 측면 위쪽의 작은 주황색 발광 눈
- 금속 긁힘·산화 얼룩, 캔버스 직조·먼지, 가죽 모공과 마모

## 원본 형상 보존

- 정점: 3218개 → 3218개
- 면: 6330개 → 6330개
- 본: 27개
- 형상 서명 일치: `True`
- 허용된 샘플 변경: 통합 머티리얼, `Negatif_MaterialUV`, 표면 혼합 마스크 2개
- 정적 표시 방식: Armature Modifier와 리그는 보존하되, Unity의 애니메이션 비활성 정적 검토와 같은 바인드 메시를 보여주기 위해 샘플 렌더에서만 변형 평가를 끔

## 검토 순서

1. `renders/05_reference_comparison.png`
2. `renders/01_reference_matched_three_quarter.png`
3. `renders/02_side.png`, `03_front.png`, `04_back_three_quarter.png`
4. `renders/06_material_texture_breakdown.png`
5. 재질 검토 기준: `blender/Negatif_Appearance_ReferenceSync.blend`
6. 호환성 보조 산출물: `exports/Negatif_Appearance_ReferenceSync.glb` — 사용자 정의 표면 마스크는 뷰어에 따라 동일하게 표시되지 않을 수 있음

## Unity 반영 계획

사용자가 이 샘플을 승인했으며, 승인된 별도 작업 범위에서 `Approved Negatif Enemy Placement` 아래 7개 `Negatif_Model` 인스턴스의 외형 동기화 기준으로 사용합니다.
