# 리벨리온 애니메이션용 리그 연결 보정

## 목적

승인 외형 생성 과정에서 다리 본 `Bone_017`에 잘못 연결된 추가 디테일을
원본 리그의 몸통·무장 브랜치로 바로잡아, 다리 애니메이션 중 몸통과 무장이
다리를 따라 변형되지 않게 합니다.

## 확정한 원본 리그

- 몸통·무장 브랜치: `Bone_008 > Bone_007 > Bone_006`
- 네 다리 브랜치 루트: `Bone_013`, `Bone_018`, `Bone_023`, `Bone_028`
- 각 다리: 루트부터 끝 본까지 5개 본
- 전체 본 수: 29개

전체 계층과 판정 근거는 `analysis/ORIGINAL_RIG_MAP.md`에 기록했습니다.

## 보정 내용

- 수납부 메시 정점 51개: `Bone_017` 가중치 제거 후 `Bone_008`에 귀속
- 패널·환기구·고정구·스캔 렌즈: `Bone_008`에 연결
- 총구 허브와 7개 총열: `Bone_007`에 연결
- Blender 검토 전용 스캔 옵틱: `Bone_008`에 연결

원본 본 계층, 메시 형상, 머티리얼, 텍스처와 정지 자세는 변경하지 않았습니다.

## 결과

- 보정 GLB: `exports/Rebellion_RigAttachmentCorrection.glb`
- 보정 Blender 파일: `blender/Rebellion_RigAttachmentCorrection.blend`
- 자동화 스크립트: `scripts/build_rig_attachment_correction.py`
- 상세 보고서: `RIG_ATTACHMENT_CORRECTION.json`
- 승인 상태: `APPROVAL_STATUS.json`
- 최종 GLB SHA-256:
  `2FCDD1322554251B2E4461946E98B97A83CF1CD9B53225E0ED1442742C29400C`

왕복 검사에서 29개 본, 보정된 디테일 부모, 수납부 영역의 `Bone_017` 영향
0개, 형상 시그니처 일치를 확인했습니다.
