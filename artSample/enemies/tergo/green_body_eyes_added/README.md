# Tergo 녹색 반투명 몸통 + 주황색 눈 샘플

## 목적

`enemies model/tergo.fbx` 원본 Tergo 모델에 주황색 발광 눈을 추가하고, 눈 색을 제외한 얼굴, 몸통, 팔, 다리, 눈 주변 보조 메쉬를 어두운 녹색 계열의 반투명 재질로 통일한 승인용 샘플입니다. 아직 Unity 씬, 프리팹, 런타임 에셋에는 적용하지 않았습니다.

## 반영 방식

- 원본 `enemies model/tergo.fbx`를 그대로 임포트했습니다.
- 기존 눈 샘플의 위치 값을 재사용해 주황색 발광 눈을 유지했습니다.
- 눈 렌즈와 눈 코어를 제외한 모든 시각 메쉬에는 `Tergo_Green_Translucent_Body` 재질을 적용했습니다.
- 반투명 느낌을 유지하기 위해 `blend_method=BLEND`, Alpha `0.58`, 투명 셰이더 혼합, 내부 녹색 노이즈를 사용했습니다.
- 이번 버전은 이전 녹색 샘플보다 색 램프와 발광 강도를 낮춰 조금 더 어두운 녹색으로 조정했습니다.
- 눈 보조 라인과 아이라이트는 얼굴에 까맣게 묻어나지 않도록 샘플 안에서만 약하게 낮췄습니다.
- 몸체 메쉬, Armature, 스케일, 방향, 리깅 구조는 수정하지 않았습니다.

## 검토 파일

- `index.html`
- `renders/tergo_green_eyes_front.png`
- `renders/tergo_green_eyes_three_quarter.png`
- `renders/tergo_green_eyes_side.png`
- `renders/tergo_green_eyes_closeup.png`
- `renders/tergo_green_eyes_front_large.png`
- `renders/tergo_green_eyes_side_large.png`
- `blender/tergo_green_body_eyes_added.blend`
- `exports/tergo_green_body_eyes_added.fbx`
- `exports/tergo_green_body_eyes_added.glb`

## 재질 기록

- 녹색 머티리얼: `['Tergo_Green_Translucent_Body']`
- 눈 제외 전체 반투명 재질 통일: `True`
- 재질 방식: `translucent wet green`
- 몸체 Alpha: `0.58`
- 몸체 Diffuse RGBA: `[0.04, 0.22, 0.12, 0.58]`
- 몸체 발광 강도: `0.68`
- 통일 재질: `Tergo_Green_Translucent_Body`
- 눈 보조 조명 에너지: `0.05`
- 눈 렌즈 색: 기존 주황색 발광 유지
- 눈 중심 Z: `1.5584`
- 눈 간격: `0.0733`

## 승인 전 제한

사용자 승인 전에는 이 샘플을 Unity 씬, 프리팹, 런타임 모델, AI, 충돌, 공격 판정, 애니메이션에 연결하지 않습니다.
