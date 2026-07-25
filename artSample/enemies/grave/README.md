# 그라베 기준 이미지 재현 아트 샘플

## 상태

- 승인 상태: 사용자 검토 대기
- Unity 런타임 씬, 프리팹, 에셋, AI, 피격 판정, UI 흐름에는 연결하지 않았습니다.
- 기준 모델: `enemies model/grave.fbx`
- 기준 이미지: `image/grave(그라베).png`

## 검토 방법

- `index.html`을 열어 기준 이미지와 생성 렌더, 추가 시점, 텍스처, 검증 결과를 확인합니다.
- `renders/03_reference_side_by_side_overview.png`는 정면 기준 이미지와 생성 렌더의 병렬 비교본입니다.
- `review/grave_art_sample_page.png`는 로컬 브라우저에서 전체 검토 페이지를 확인한 최종 캡처입니다.
- `exports/grave_reference_reproduction.glb`는 범용 3D 검토 파일입니다.
- 원본 리그 가중치를 온전히 유지하는 작업 파일은 `reproduction/grave_reproduction.blend`와 `reproduction/grave_reproduction.fbx`입니다.

## 기준 자료 제약

- 기준 이미지는 정면 한 장뿐입니다.
- 측면·후면에는 새 양복 선화를 만들지 않고 보이는 회색 직물 표면만 연장했습니다.
- 손과 발 각도는 원본 FBX의 비대칭 휴식 자세를 유지합니다.

## 검증

- FBX 빈 장면 재임포트와 텍스처 연결 검증을 통과했습니다.
- 정면 실루엣 IoU `0.799839`, 폭 오차 `2.424%`, 높이 오차 `1.153%`, 중심 오차 `1px`입니다.
- 자세한 제작·검증 기록은 `reproduction/README.md`와 `reproduction/review/`에서 확인할 수 있습니다.
