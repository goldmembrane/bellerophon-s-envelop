# Shotgun Muzzle Flash V2 아트 샘플

## 목적

기존 총구 이펙트가 실제 게임 화면에서 너무 얌전하게 보인다는 사용자 피드백을 반영한 강화안입니다. 실사 스타일과 짧은 산탄총 발사 특성은 유지하면서 순간적인 압력감과 시인성을 높였습니다.

## 시각 구성

- 왼쪽 중앙을 총구 결합 원점으로 사용합니다.
- 백색 고온 중심부에서 오른쪽으로 폭발이 진행됩니다.
- 위·아래로 갈라지는 불규칙한 황색·호박색 화염을 강조했습니다.
- 전방으로 뻗는 주황색 불티와 화약 입자를 추가했습니다.
- 길게 이어지는 화염방사기 형태가 아니라 짧고 넓은 회백색 화약 연기 충격파로 마무리합니다.
- 총기, 손, 인물, 환경, 문자, 로고와 워터마크는 포함하지 않습니다.

## Unity 적용 결과

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 대상: `PlayerAnimationLayout/Shotgun_Fire`
- 앵커: 샷건 총구 중심
- 카메라: 1인칭 게임플레이 시점
- 표시 조건: 3초 `Shotgun_Fire` 시작 후 0.15초의 단발 발사 이벤트
- 구현: 화면을 덮던 승인 이미지용 4장 교차 카드를 제거하고, 실제 머스켓과 같은 FlashCore·HotGas·Smoke·Embers 소프트 파티클 4계층을 3차원 원뿔 안에 분산
- 확산: Flash 9개·고온 가스 14개·연기 18개·불티 18개로 구성하며, 머스켓보다 넓은 원뿔과 빠른 전방 속도로 산탄총 압력감을 표현
- 전개: 총구에서 65% 크기로 즉시 점화한 뒤 `0.025초` 안에 전개하고, 화염·가스 뒤에 연기가 최대 `0.82초`까지 감쇠
- 최적화: 재사용 Renderer 4개·Material 3개·ParticleSystem 4개와 최대 59개 입자, 범위 `1.8m`·지속 `0.055초`의 동시 개수 제한 무그림자 광원 1개, 모션 벡터·발사별 생성 없음
- 직접 비교: 승인 V2 원본, 실제 `Musket_Aim_Fire` 품질 기준, Shotgun 근접 정면·측면·사선 및 전신을 같은 시트에서 확인
- 직접 확인: `Assets/_Project/Art/VFX/ShotgunMuzzleFlash/Shotgun_Fire_MuzzleFlash_V2_FinalReview.png`
- 성능 기록: `APPLICATION_METRICS.json`

## 생성 정보

- 생성 방식: Codex 내장 `imagegen` 기본 편집 모드
- 편집 기준: 기존 승인 샘플과 Unity 적용 확인 시트
- 생성·추론 여부: AI 이미지 편집 결과
- Unity 적용 여부: 사용자 승인 후 적용 완료
- 원본 PNG는 변경하지 않았고 Unity 복사본의 SHA-256도 승인본과 동일합니다.
- 불투명 체크 프레젠테이션 배경은 런타임용 파생 텍스처에서만 제거했습니다.
- 승인 PNG와 Unity 복사본은 보존했지만, 사용자가 지적한 평면감을 없애기 위해 런타임의 지배적인 텍스처 카드는 사용하지 않습니다. 승인 시안의 색·넓은 분출 의도는 머스켓식 소프트 파티클 레이어로 재현합니다.

## 산출물

- `renders/01_realistic_shotgun_muzzle_flash_v2.png`
- `index.html`
- `APPROVAL_STATUS.json`
- `APPLICATION_METRICS.json`
