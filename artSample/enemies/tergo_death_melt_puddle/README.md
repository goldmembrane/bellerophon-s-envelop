# Tergo 사망 MeltPuddle BlendShape 샘플

## 목적

이 샘플은 `Tergo_12_Death`에 사용할 수 있는 전용 사망 MeltPuddle 모델 후보입니다. 기준은 `LongaArma_06_Death_MeltPuddle`과 같은 방식입니다. 즉, 본이나 Transform을 억지로 눌러 웅덩이처럼 보이게 하는 방식이 아니라, 사망 전용 모델의 BlendShape를 애니메이션 커브로 구동하는 방식입니다.

이 버전은 `tergo_dying.fbx` 사망 애니메이션의 마지막 누운 포즈를 Blender에서 평가한 뒤, 그 누운 포즈를 BlendShape Basis로 사용합니다. 서 있는 기본 포즈에서 녹이는 샘플이 아닙니다.

## 제작 기준

- 원본 모델 기준: `Assets/_Project/Art/Enemies/Tergo/Models/tergo_dying.fbx`
- Basis 기준: `tergo_dying.fbx` 사망 애니메이션 마지막 프레임의 누운 포즈
- 샘플 위치: `artSample/enemies/tergo_death_melt_puddle/`
- 몸체 색감: 기존 Tergo 녹색 몸체 기준의 젖은 유기체 질감
- 눈과 광원: 이 샘플에서는 임의로 새로 만들지 않음. Unity 적용 단계에서 기존처럼 `Tergo_00_Static_Review` 기준 눈/광원 복사 방식을 유지한다.
- 사망 후 형태: 이미 누운 몸체가 아래로 주저앉고, 압착되고, 마지막에 머리/몸통/팔/다리 실루엣이 남지 않는 얇은 웅덩이가 되는 구조

## 포함 BlendShape

- `DEATH_TERGO_01_weight_sag`
  - 쓰러진 몸체가 무게 때문에 아래로 처지는 단계
- `DEATH_TERGO_02_crush_collapse`
  - 몸통과 팔다리가 바닥 근처로 압착되는 단계
- `DEATH_TERGO_03_melt_spread`
  - 최종 웅덩이처럼 얇고 넓게 퍼지며 몸체 부위 실루엣이 사라지는 단계

## 검토 파일

- `renders/tergo_death_melt_puddle_overview.png`
  - 사망 마지막 누운 포즈, 처짐, 압착, 최종 웅덩이 4단계 비교 렌더
- `renders/tergo_death_melt_puddle_side_height.png`
  - 최종 단계가 바닥에 낮게 고이는지 확인하기 위한 측면 렌더
- `renders/tergo_death_melt_puddle_final_puddle.png`
  - 최종 웅덩이 형태 집중 렌더
- `exports/tergo_death_melt_puddle_blendshape.fbx`
  - Unity 적용 후보 BlendShape FBX
- `exports/tergo_death_melt_puddle_preview.glb`
  - 4단계 비교용 GLB
- `blender/tergo_death_melt_puddle.blend`
  - Blender 원본 작업 파일

## Unity 적용 계획

사용자가 이 샘플을 승인하면 별도 승인 후 다음 단계로 진행한다.

1. `exports/tergo_death_melt_puddle_blendshape.fbx`를 Tergo 런타임 아트 경로로 임포트한다.
2. `Tergo_12_Death`의 기존 쓰러지는 원본 모션 구간은 유지한다.
3. 원본 사망 클립 종료 이후에만 이 사망 마지막 포즈 기반 모델을 보이게 하고 BlendShape 커브를 추가한다.
4. BlendShape 커브는 `DEATH_TERGO_01_weight_sag`, `DEATH_TERGO_02_crush_collapse`, `DEATH_TERGO_03_melt_spread` 순서로 구동한다.
5. 몸통 머티리얼, 눈 상대 위치, 눈 형태, 광원은 기존 0번 기준 동기화 방식을 유지한다.

## 승인 상태

- 현재 상태: 사용자 검토 대기
- 실제 Unity 씬, 프리팹, 런타임 에셋에는 아직 연결하지 않음
