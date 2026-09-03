# Shotgun Muzzle Flash Unity Asset

- 승인 원본: `artSample/shotgun_muzzle_flash_v2/renders/01_realistic_shotgun_muzzle_flash_v2.png`
- Unity 원본 복사본: `02_realistic_shotgun_muzzle_flash_v2.png`
- 보존된 런타임 투명 텍스처: `ShotgunMuzzleFlashTransparent.png` (현재 지배적 카드 렌더에는 사용하지 않음)
- 구현 방식: 실제 머스켓과 같은 FlashCore·HotGas·Smoke 소프트 빌보드와 Mesh Ember를 3차원 원뿔에 분산한 4계층 재사용 파티클. 단일 지배 카드 없음
- 대상: `CargoRunMvp/PlayerAnimationLayout/Shotgun_Fire`
- 확산: Flash 9·HotGas 14·Smoke 18·Embers 18, 최대 59개 입자. 머스켓보다 넓은 샷건용 원뿔 분산
- 발사 시점: 3초 `Shotgun_Fire` 시작 후 0.15초
- 총구 원점에서 `0.025초` 동안 전개하며 연기는 최대 `0.82초`까지 감쇠
- 렌더 구성: Renderer 4개, Material 3개, ParticleSystem 4개, 범위 `1.8m`·지속 `0.055초`의 제한형 무그림자 Light 1개
- 최적화: 최대 59개 입자, 동시 연기·광원 개수 제한, 그림자·모션 벡터·발사별 오브젝트 생성 없음
- 최종 직접 확인 시트: `Shotgun_Fire_MuzzleFlash_V2_FinalReview.png`
