# Tergo 눈 추가 샘플

## 목적

기존 `Assets/_Project/Art/Enemies/Tergo/Models/tergo.fbx` 본체는 유지하고, 머리 전면에 작은 주황 발광 눈만 추가한 승인용 샘플입니다. 아직 Unity 씬, 프리팹, 런타임 에셋에는 적용하지 않았습니다.

## 반영 방식

- 원본 Tergo FBX를 그대로 임포트했습니다.
- 머리 전면 상단에 작은 주황 발광 렌즈 2개를 배치했습니다.
- 각 눈에는 어두운 젖은 소켓, 얕은 돌출 테두리, 밝은 중심광, 검토용 작은 포인트 라이트를 추가했습니다.
- 몸체 메시, Armature, 드릴 팔, 스케일, 방향, 리깅 구조는 수정하지 않았습니다.
- 정면 검토용 고해상도 렌더와 측면 검토용 고해상도 렌더를 별도로 생성했습니다.

## 검토 파일

- `index.html`
- `renders/tergo_eyes_front.png`
- `renders/tergo_eyes_three_quarter.png`
- `renders/tergo_eyes_side.png`
- `renders/tergo_eyes_closeup.png`
- `renders/tergo_eyes_front_large.png`
- `renders/tergo_eyes_side_large.png`
- `blender/tergo_eyes_added.blend`
- `exports/tergo_eyes_added.fbx`
- `exports/tergo_eyes_added.glb`

## 기준

- 원본 기획서: `docs/GAME_DESIGN_SOURCE.txt`
- 애니메이션 계획 문서: `docs/enemies/TERGO_ANIMATION_PLAN.md`
- 기준 이미지: `image/tergo(테르고).png`, `image/tergo-beside.png`, `image/tergo-back.png`

## 위치 기록

- 원본 bounds min: `[-0.3009, -0.1976, 0.0133]`
- 원본 bounds max: `[0.3278, 0.4257, 1.6783]`
- 원본 dimensions: `[0.6287, 0.6232, 1.665]`
- 눈 중심 X: `-0.0`
- 눈 중심 Z: `1.5584`
- 머리 전면 기준 Y: `-0.1186`
- 왼쪽 눈 surface/socket/lens Y: `-0.1186` / `-0.1486` / `-0.1735`
- 오른쪽 눈 surface/socket/lens Y: `-0.1186` / `-0.1386` / `-0.1585`
- 눈 간격: `0.0733`

## 승인 전 제한

사용자 승인 전에는 이 샘플을 Unity 씬, 프리팹, 런타임 모델, AI, 충돌, 피격 판정, 애니메이션에 연결하지 않습니다.
