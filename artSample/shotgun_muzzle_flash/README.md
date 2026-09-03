# Shotgun Muzzle Flash — 실사형 아트 샘플

상태: **사용자 승인 대기 / Unity 미적용**

`Shotgun_Fire`의 총구에 부착할 발사 이펙트의 시각 방향 샘플입니다. 실제 산탄총의 짧고 넓은 무연화약 발사광을 기준으로, 백색 고온 중심부·옅은 황색과 호박색 화염·소량의 불꽃·회백색 화약 가스를 한 장의 투명 PNG에 분리했습니다.

## 의도한 향후 적용 위치

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 대상: `PlayerAnimationLayout/Shotgun_Fire`
- 앵커: 샷건 총열의 실제 총구 중심, 효과의 왼쪽 원점이 총열 진행 방향을 향하도록 배치
- 카메라: 1인칭 게임플레이 카메라
- 상태: 발사 입력이 유효한 단발 순간에만 표시
- 예상 타이밍: 고온 화염 0.00–0.06초, 잔류 가스·연기 0.06–0.35초
- 가시성: 어두운 산업형 선내에서도 중심 화염은 즉시 읽히고, 연기는 시야를 장시간 가리지 않도록 제한

## 파일

- `renders/01_realistic_shotgun_muzzle_flash.png`: 투명 배경 실사형 시각 원본
- `index.html`: 단일 페이지 승인용 프레젠테이션
- `APPROVAL_STATUS.json`: 승인 및 Unity 적용 상태

## 생성 고지

이미지는 Codex 내장 이미지 생성 기능의 기본 생성 모드로 제작한 신규 AI 생성 샘플입니다. 사용자께서 지정하신 “실사형”과 프로젝트의 1인칭 산탄총 사용 맥락을 반영했으며, Unity 파티클·머티리얼·프리팹 값은 아직 만들거나 추론해 적용하지 않았습니다.

최종 생성 프롬프트는 `APPROVAL_STATUS.json`의 `generationPrompt`에 원문 그대로 기록했습니다.
