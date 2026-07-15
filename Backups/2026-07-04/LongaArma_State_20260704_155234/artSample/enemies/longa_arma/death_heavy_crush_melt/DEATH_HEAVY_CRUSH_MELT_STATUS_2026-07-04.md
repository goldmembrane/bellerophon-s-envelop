# Longa Arma Death Heavy Crush Melt Status - 2026-07-04

## 결과

- 샘플 폴더: `artSample/enemies/longa_arma/death_heavy_crush_melt/`
- 생성 시각: 2026-07-04 15:44:37 KST
- 기준 메시: 기존 `runtime_lowpoly` Longa Arma 메시의 평가 결과
- `dead.fbx` 사용 여부: 사용하지 않음
- Unity 적용 여부: 적용하지 않음

## 생성된 변형

- `DEATH_HEAVY_01_weight_sag`
- `DEATH_HEAVY_02_crush_collapse`
- `DEATH_HEAVY_03_melt_spread`

## 검토 포인트

- 첫 프레임이 기존 Longa Arma로 보이는지 확인해야 합니다.
- 중간 프레임에서 새 사망 모델로 바뀐 듯 보이면 반려 대상입니다.
- 최종 프레임은 완전한 별도 웅덩이가 아니라 기존 몸체가 눌려 바닥으로 퍼진 형태입니다.
- 승인 전까지 Unity 사망 모션 개체에는 연결하지 않습니다.

## 실행하지 않은 항목

- Unity Refresh 또는 Bridge 명령
- `ApplyLongaArmaDeathMeltPuddle`
- Harness/EditMode/PlayMode/Build/Smoke/Validate
- Git 커밋/푸시
- `dead.fbx` 사용 또는 수정
