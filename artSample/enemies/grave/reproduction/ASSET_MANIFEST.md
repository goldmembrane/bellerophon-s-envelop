# 그라베 재현 샘플 에셋 매니페스트

## 출처

- `source/grave_reference.png`: `image/grave(그라베).png`의 변경 없는 검토용 복사본
- `source/grave_base.fbx`: `enemies model/grave.fbx`의 변경 없는 작업용 복사본

## 모델

- `grave_reproduction.blend`: Blender 5.1.2 작업 원본, 네 텍스처 패킹
- `grave_reproduction.fbx`: 수정된 휴식 형상, 원본 리그와 스키닝, 두 머티리얼 슬롯, `uv`와 `GraveReferenceUV`

## 텍스처와 머티리얼

- `textures/grave_front_albedo.png`: 기준 이미지의 턱시도 선화와 회색 직물 전면 알베도
- `textures/grave_textile_albedo.png`: 기준 이미지에서 추론한 후면·옆면 회색 직물 알베도
- `textures/grave_fabric_normal.png`: 미세 섬유 요철 노멀
- `textures/grave_fabric_roughness.png`: 무광 직물 거칠기
- `Grave_Suit_Front_Mat`: 전면 턱시도 투영용 머티리얼
- `Grave_Textile_BackSide_Mat`: 후면·옆면용 직물 머티리얼

## 검토 산출물

- `review/grave_reproduction_front.png`: 최종 정면 렌더
- `review/grave_reproduction_front_rgba.png`: 흰 배경 합성 전 최종 렌더
- `review/grave_reference_comparison.png`: 기준 이미지/최종 샘플 나란히 비교
- `review/fbx_validation.txt`: FBX 재임포트 구조·치수 검증
- `review/visual_validation.txt`: 정면 이미지 정량 비교
- `review/work_preview.png`, `review/work_preview_rgba.png`: 최종 캡처 전 검증용 미리보기

## 재현 및 검증 스크립트

- `prepare_grave_textures.py`: 절차적 직물·재질 텍스처 생성
- `build_grave_reproduction.py`: 모델 보정, UV/머티리얼 구성, Blend/FBX 내보내기
- `composite_grave_preview.py`: 검증용 투명 렌더를 흰 배경으로 합성
- `validate_grave_reproduction_fbx.py`: 빈 Blender 세션 재임포트 검증
- `validate_grave_visual.py`: 기준 이미지와 작업 미리보기의 정량 비교
- `render_grave_reproduction_final.py`: 검증 통과 뒤 최종 렌더 1회 실행
- `prepare_grave_review.py`: 최종 흰 배경 렌더와 비교본 생성

## 현재 적용 상태

- 승인용 `artSample/` 산출물만 생성됨
- Unity 씬·프리팹·런타임 에셋 미적용
- 기존 그라베 7개 슬롯과 애니메이션·AI·물리·전투 로직 미변경

