# 이슈판트 발검 장검 외형 아트 샘플

상태: `ART_SAMPLE_APPROVED_2026-08-06`

Unity 적용 상태: `APPLIED_TO_ALL_12_ISPANT_SLOTS_2026-08-06`

- 현재 Unity 발검 개체의 몸체·리그·Mixamo 동작과 승인된 정적 이슈판트 외형을 기준으로 만든 검토용 3D 샘플입니다.
- `image/išpant(이슈판트)-armed.png`에서 확인되는 길고 좁은 은회색 칼날, 가장자리 마모 표현과 짙은 갈색 손잡이 계열을 반영했습니다.
- 사용자 승인 뒤 이 장검 메시와 재질을 Unity의 이슈판트 12개 슬롯에 적용했습니다. 10개 정적 슬롯과 이동 슬롯은 각 개체의 기존 장착 위치를 유지하고, 발검 슬롯만 `mixamorig:RightHand`를 직접 따라가도록 적용했습니다.
- `Run-HarnessValidation.ps1`, EditMode·PlayMode 테스트와 빌드는 실행하지 않았습니다.

## 목적

현재 Unity에 배치된 `Ispant_04_DrawSword`의 자세와 외형을 유지한 상태에서,
기준 이미지의 장검 비례와 재질을 식별 가능한 손잡이까지 갖춘 3D 아트
샘플로 제시합니다. 이 샘플은 사용자 승인을 받았으며 Unity 적용본의 외형
원본입니다.

기준 자료:

- `image/išpant(이슈판트)-armed.png`
- `Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_DrawSword.fbx`
- `artSample/enemies/ispant_armed/Ispant_Armed_Appearance_Sample.blend`
- `docs/GAME_DESIGN_SOURCE.txt`의 이슈판트 장검 정의

## 장검 외형

- 전체 길이: `1.0715m`
- 칼날: 길이 `0.82m`, 뿌리 폭 `0.068m`, 두께 `0.012m`
- 가드: 폭 `0.19m`, 중앙 금속 칼라와 좌우 대칭의 짧은 곡선형 가드
- 손잡이: 길이 `0.17m`, 지름 `0.052m`, 짙은 갈색 가죽과 반복 감기 표현
- 폼멜: 길이 `0.055m`, 마모된 은회색 금속
- 재질: `Ispant_LongSword_WornSteel`, `Ispant_LongSword_BrownLeather`, `Ispant_LongSword_DarkEngraving`
- 기준 이미지에서 직접 확인할 수 없는 후면 깊이는 앞면 윤곽의 대칭 두께만 사용했고 추가 장식은 만들지 않았습니다.

장검은 `Ispant_Reference_LongSword`라는 단일 메시이며 2,080정점,
4,092삼각형입니다. `mixamorig:RightHand` 본의 직접 자식으로 연결했고,
기존 발검 장검 손잡이 중심과 새 손잡이 중심의 정렬 오차는 `0m`입니다.
오른손 본 원점에서 손잡이 중심까지의 거리는 `0.0557747075m`입니다.

## 외형 동기화 범위

- 발검 몸체의 38개 머티리얼 슬롯을 승인된 정적 이슈판트 샘플의 동일 이름 머티리얼 객체와 동기화했습니다.
- 본체 UV 이름을 승인 외형의 `uv`, `IspantMechanicalUV`, `IspantHelmetFaceUV`로 복구했습니다.
- 몸체·초승달·눈·머스켓은 현재 발검 개체의 형상과 리그를 유지했습니다.
- 기존 `Ispant_DrawSword_RigidSword`와 `Ispant_DrawSword_RigidSheath`는 샘플에서만 비표시 처리했습니다.
- Mixamo 액션의 1~46프레임은 3D 검토 파일에 보존했습니다. 정적 렌더는 1프레임 기준입니다.

## 검토 파일

- `appearance_reference_sync/index.html`: 기존 이슈판트 외형 샘플 형식을 따른 전체 기준 비교·구조·적용 경계 페이지
- `appearance_reference_sync/summary.html`: 같은 형식의 빠른 승인 판정용 요약 페이지
- `appearance_reference_sync/Ispant_DrawSword_Index_Desktop.png`: 전체 페이지 데스크톱 렌더 확인
- `appearance_reference_sync/Ispant_DrawSword_Index_Narrow.png`: 전체 페이지 720px 1열 전환 확인
- `appearance_reference_sync/Ispant_DrawSword_Summary_Desktop.png`: 요약 페이지 데스크톱 렌더 확인
- `appearance_reference_sync/Ispant_DrawSword_Summary_Narrow.png`: 요약 페이지 720px 1열 전환 확인
- `Ispant_DrawSword_FinalReview.png`: 기준 이미지, 정면, 측면, 손잡이 확대 4분할 최종 검토본
- `Ispant_DrawSword_Full.png`: 발검 자세 정면
- `Ispant_DrawSword_Side.png`: 측면 두께와 몸체 간섭 확인
- `Ispant_DrawSword_HandleCloseup.png`: 손잡이·가드·폼멜 확대
- `Ispant_DrawSword_ArtSample.blend`: 리그·애니메이션·재질을 확인할 수 있는 Blender 원본
- `Ispant_DrawSword_ArtSample.fbx`: FBX 검토본
- `Ispant_DrawSword_ArtSample.glb`: 범용 3D 검토본
- `Ispant_DrawSword_ArtSample_Report.json`: 제작 구조와 기준 해시
- `Ispant_DrawSword_ExportValidation.json`: BLEND·FBX·GLB 재가져오기 검사 기록
- `ASSET_MANIFEST.md`: 결과물과 구조 검사 요약

`diagnostics/`와 파일명에 `FAILED`가 포함된 이미지는 제작 과정 확인 자료이며
최종 승인 대상은 아닙니다.

두 HTML의 로컬 링크와 이미지 경로는 누락 없이 확인했고, 데스크톱과 720px
화면에서 한글·이미지·비교 카드가 겹치거나 잘리지 않는 것을 확인했습니다.

## 재현 명령

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' `
  --background --factory-startup `
  --python 'artSample\enemies\ispant_draw_sword\scripts\build_ispant_draw_sword_art_sample.py' `
  -- --final

& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' `
  --background --factory-startup `
  --python 'artSample\enemies\ispant_draw_sword\scripts\validate_art_sample_exports.py'
```

## Unity 적용 결과

- 적용 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 적용 슬롯: 이슈판트 12개 전체
- 10개 정적 슬롯: 기존 `Hips` 장착 위치 유지
- `Ispant_03_Move`: 기존 `mixamorig:Hips` 장착 위치 유지
- `Ispant_04_DrawSword`: `mixamorig:RightHand` 직접 자식으로 연결해 오른팔 동작 추종
- 발검 1~46프레임 최대 부착 오차: `0m`
- 발검 전 구간 오른손 표면 최대 거리: `0.01812077m`
- 기존 장검 렌더러와 발검 칼집 렌더러 잔존 수: 각각 `0`
- 구조 검사: `PASS`
- 노출 보정 전 어두운 캡처는 `docs/validation/ispant_approved_longsword_2026-08-06/discarded/`로 분리했으며 추가 캡처는 실행하지 않았습니다.

Unity용 메시 세 개는 승인 메시의 2,080정점·4,092삼각형·동일 치수와 재질을
그대로 유지하면서 각 기존 장착 좌표계만 보존한 파생본입니다. 원본 발검 FBX와
승인된 Blender 샘플은 수정하지 않았습니다.

### Unity 가시성·발검 손잡이 개정

- 승인 BaseColor의 선형 값을 정확한 sRGB 전달함수로 인코딩해 Unity가 원래 선형색으로 복원하도록 수정했습니다.
- 현재 CargoRun 조명에서 마모 강철이 검게 잠기지 않도록 승인 색상 관계를 유지한 `3×` Unity 재질 노출을 적용했습니다.
- 발검 손잡이 중심은 1프레임의 오른손 가중치 메시 정점 중심에 정렬했습니다.
- 1~46프레임 손잡이 중심–손바닥 중심 거리는 `0.00000006m~0.004224267m`입니다.
- 장검과 오른손의 최대 월드 회전 변화는 각각 `179.3398°`, 상대 회전 오차는 `0°`입니다.

## 0.9m 칼날 길이 개정 샘플 · 승인 대기

- 사용자 지정에 따라 전체 길이 `0.9m`, 칼날 `0.6485m`로 줄인 별도 아트 샘플을 `length_0_9m_revision/`에 만들었습니다.
- 기존 승인 장검의 손잡이·가드·폼멜은 변경하지 않았고 Unity에는 아직 적용하지 않았습니다.
- 승인 검토: `length_0_9m_revision/appearance_reference_sync/index.html`
- 빠른 요약: `length_0_9m_revision/appearance_reference_sync/summary.html`
- 하네스 검증, EditMode·PlayMode 테스트와 빌드는 실행하지 않았습니다.
