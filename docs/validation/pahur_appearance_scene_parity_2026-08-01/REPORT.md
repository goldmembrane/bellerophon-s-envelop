# 파후르 승인 외형 실제 씬 동기화

## 원인

- 승인 Blender 렌더는 밝은 중성 월드광과 warm key, cool fill, cool rim의
  3점 면광원을 사용했다.
- 기존 Unity 승인 캡처는 저장되지 않는 복제본에 `_PreviewUnlit=1`을
  적용했으므로 실제 씬 PBR 결과를 검증하지 못했다.
- 실제 씬은 매우 어두운 환경광을 사용해 고금속성 파후르 재질의 반사와
  적갈색 명암이 소실됐다. `_PreviewUnlit=1`로 바꾸면 밝아지는 대신
  법선·거칠기·금속성에 따른 명암이 사라져 단조로워졌다.
- 정지 모션 이전 검토 이미지에도 같은 어두움이 확인되어, 문제는 남은
  정지 모션 잔재가 아니라 최초 Unity 외형 적용·판정 방식이었다.

## 수정

- `Bellerophon/Pahur/ApprovedAppearance` 셰이더에 승인 Blender 조명을
  기준으로 한 중성 간접광과 warm key, cool fill, cool rim의 물리 기반
  재질 응답을 추가했다.
- 실제 씬 PBR 결과가 더 밝으면 기존 결과를 유지하고, 씬이 어두울 때만
  승인 재질 응답이 최소 밝기와 반사 형태를 보존하도록 했다.
- 승인 머티리얼 20개에 같은 값을 적용하고 `_PreviewUnlit=0`을 고정했다.
- 텍스처, 색 배치, 메시, FBX, 포즈, 애니메이션, VFX와 씬 조명은
  변경하지 않았다.

## 확인 결과

- 파후르 승인 머티리얼: 20개
- 파후르 배치 하위 전체 렌더러: 18개
- 임시 캡처 조명: 0개
- 실제 씬 캡처의 `_PreviewUnlit`: 0
- 씬 저장 또는 씬 조명 변경: 없음
- 실제 씬 캡처:
  `Pahur_ActualScene_ApprovedParity.png`
- 하네스 검증, EditMode/PlayMode 테스트, Windows 빌드는 실행하지 않았다.
