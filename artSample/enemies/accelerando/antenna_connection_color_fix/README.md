# 아첼레란도 더듬이 연결 및 채색 샘플

## 목적

`enemies model/accelerando.glb`의 막대형 연결부를 사슬로 치환하고, `image/` 폴더의 아첼레란도 기준 이미지처럼 더듬이 끝과 철퇴가 체인으로 이어지도록 검토용 채색 샘플을 제작했습니다.

## 승인 대상 파일

- `index.html`
- `exports/accelerando_connected_colored_sample.glb`
- `exports/accelerando_connected_colored_sample.blend`
- `renders/accelerando_connected_colored_contact_sheet.png`
- `renders/accelerando_connected_colored_front.png`
- `renders/accelerando_connected_colored_side.png`
- `renders/accelerando_connected_colored_oblique.png`

## 반영 내용

- 원본 GLB의 주 모델 메시를 기반으로 샘플을 제작했습니다.
- 원본 메시의 막대형 연결부를 제거했습니다.
- 더듬이 끝 캡에서 철퇴 상단까지 녹슨 금속 사슬 링크가 직접 이어지도록 배치했습니다.
- 사슬 양 끝에는 연결 링을 두어 기준 이미지처럼 철퇴가 체인에 매달린 구조로 보이게 했습니다.
- 기존 판 형태의 전시용 바닥부는 샘플 메시에서 제거했습니다.
- 몸통은 젖은 회갈색 살점, 등껍질과 더듬이 상부는 어두운 낡은 껍질, 철퇴와 체인은 녹슨 어두운 금속으로 구분했습니다.

## 적용하지 않은 항목

- 원본 `enemies model/accelerando.glb`는 덮어쓰지 않았습니다.
- Unity `Assets/`와 `CargoRunMvp.unity`에는 적용하지 않았습니다.
- 런타임 프리팹, 애니메이션, 충돌, 배치 상태는 변경하지 않았습니다.

## 남은 확인 사항

- 기준 이미지는 고해상도 점액 표면과 금속 마모가 강하지만, 현재 샘플은 원본 저폴리 메시를 유지한 상태라 표면 디테일은 절차적 머티리얼 수준입니다.
- Unity 적용은 이 샘플을 사용자가 승인한 뒤 별도 승인 범위로 진행해야 합니다.
