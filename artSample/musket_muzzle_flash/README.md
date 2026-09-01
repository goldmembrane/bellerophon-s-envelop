# 머스켓 3D 총구 발사 VFX 샘플

## 목적

`Musket_HipFire`와 `Musket_Aim_Fire`의 실제 발사 순간에 사용할 체적형 3D 총구 VFX의 승인 기준과 Unity 적용 결과를 함께 기록한 샘플입니다.

## 시각 기준

- 0~0.06초 동안 황백색 핵과 황금색 외곽을 가진 깊이 있는 짧은 발광 연소체가 표시됩니다.
- 섬광 직후 자체 음영과 깊이가 있는 짙은 회백색 흑색화약 연기 입자군이 총구 전방과 위쪽 약 15도로 퍼집니다.
- 연기는 생성 직후 총구 로컬 3D 공간에서 전방·상방으로 퍼지고 약 0.68초 안에 사라집니다.
- 섬광·내부 가스·연기는 단일 효과 평면이나 고정 화염 메시가 아니라 서로 다른 위치·크기·회전의 소프트 파티클이 겹치는 군집이며, 작은 불티만 3D 메시를 사용합니다.
- 현재 Unity 리뷰 머스켓 길이 1.12m 기준 최대 섬광은 약 0.45m, 연기 외곽은 약 0.65m입니다.
- HipFire에는 조준선을 추가하지 않습니다.
- AimFire는 기존 조준 UI를 유지하고 같은 총구 앵커에서 VFX만 표시합니다.

## 원본 기획서 대조

- 머스켓은 양손 원거리 무기입니다.
- 좌클릭 시 화면 중앙 방향으로 발사합니다.
- 일반 발사에는 조준선이 없고 정밀 조준 모드에만 조준선이 있습니다.
- 원본 기획서의 머스켓 길이 0.8m와 현재 Unity 리뷰 모델 1.12m가 다르므로 효과 크기는 총 길이 비율로 설계했습니다.

## 포함

- `renders/01_volumetric_multiview.png`
- `renders/02_first_person_gameplay.png`
- `../../docs/validation/musket_muzzle_flash_vfx_2026-09-01/final.png`
- `index.html`
- `APPROVAL_STATUS.json`
- `Generate-MuzzleFlashSample.ps1`

## 생성·추론 공개

- 두 PNG는 OpenAI 내장 이미지 생성 도구로 제작한 3D 콘셉트 렌더입니다.
- 실제 Unity 씬, 실제 플레이어 모델, 실제 머스켓 FBX를 렌더한 결과가 아닙니다.
- 승인된 Unity 구현은 128×128 공유 불꽃 아틀라스의 네 가지 소프트 마스크를 황백색 섬광과 금빛 내부 가스 파티클에 사용하고, 별도의 128×128 공유 연기 아틀라스를 3D 공간에 분포시키는 방식으로 재현했습니다.
- `Generate-MuzzleFlashSample.ps1`은 선택된 두 최종 PNG가 검토 해상도로 존재하는지 확인하며, 기존 평면 시안을 다시 생성하지 않습니다.

## 승인 및 Unity 적용 결과

- 승인 상태: `승인됨 · Unity 적용 완료`
- 프리팹·메시·머티리얼: `Assets/_Project/Art/VFX/MusketMuzzleFlash/`
- 런타임 제어: `Assets/_Project/Scripts/VFX/`
- 적용 상태: `Musket_HipFireLoop`, `Musket_Aim_FireLoop`의 정규화 시간 0.08
- 앵커: 현재 머스켓 총구 구멍의 실제 끝점, stock→muzzle 로컬 전방축. 임의 전방 오프셋 없이 총구 표면 간격 0m
- 구성: Flash 3 + Hot Gas 6 + Soft Smoke 14 + Embers 12의 설정 상한 35개, 실제 관찰 최대 34개
- 최적화: 128×128 공유 불꽃·연기 아틀라스 각 하나와 공유 머티리얼 3개, 재사용형 비루프 파티클 4계통, 발사별 생성·삭제 없음, 레이마칭 없음, 파티클·라이트 그림자와 모션 벡터 없음
- 동시 제한: 연기 효과 6개, 총구 라이트 4개
- 실제 결과: `../../docs/validation/musket_muzzle_flash_vfx_2026-09-01/final.png`

## 현재 제외

- 실제 발사 판정 또는 피해 로직 변경
- 사운드와 카메라 흔들림
- 사용자 지정 범위 밖의 자동 테스트와 Windows 빌드
