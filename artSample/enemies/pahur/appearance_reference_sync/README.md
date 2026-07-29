# 파후르 현재 모델 외형 동기화 샘플

## 상태

- 승인 상태: `PENDING_USER_REVIEW`
- Unity 적용 승인: `false`
- Unity 씬, 프리팹, 런타임 에셋, AI, 애니메이션, VFX에는 연결하지 않았습니다.
- 기준 모델: `Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx`
- 기준 이미지: `image/pāḫḫur(파후르).png`

## 검토 방법

- `index.html`에서 기준 이미지와 정면·사선·측면·후면 생성 렌더를 비교합니다.
- `summary.html`은 승인 판단에 필요한 반영 내용, 보존 범위, 한계를 같은 HTML 형식으로 요약합니다.
- `renders/03_reference_side_by_side_overview.png`는 기준 이미지와 현재 FBX 재질 샘플을 나란히 보여줍니다.
- `blender/Pahur_Appearance_ReferenceSync.blend`와 `exports/Pahur_Appearance_ReferenceSync.fbx`가 구조 보존 기준입니다.
- `exports/Pahur_Appearance_ReferenceSync.glb`는 범용 시각 검토용입니다.

## 보존 범위

- 원본 메시의 정점, 에지, 면, 루프, UV, 본, 본 가중치와 내장 이동 애니메이션을 수정하지 않았습니다.
- 새 메시, 장갑판, 무기 부품, 후면 기계 구조, VFX를 생성하지 않았습니다.
- 기존 폴리곤에 머티리얼 슬롯과 재질 인덱스만 배정했습니다.

## Unity 적용 기준

- 사용자 승인 후 별도 Unity 적용 승인을 받습니다.
- 원본 FBX 임포트 설정·리그·애니메이션을 유지합니다.
- Pahur 전용 텍스처와 머티리얼만 런타임 자산으로 옮겨 현재 Renderer에 연결합니다.
- `Approved Pahur Enemy Placement` 아래 10개 인스턴스의 외형을 동일하게 맞추되 모델·메시·애니메이션은 변경하지 않습니다.
