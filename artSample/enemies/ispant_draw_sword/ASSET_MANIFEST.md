# 이슈판트 발검 장검 아트 샘플 에셋 매니페스트

상태: `ART_SAMPLE_APPROVED_2026-08-06`

Unity 적용 상태: `APPLIED_TO_ALL_12_ISPANT_SLOTS_2026-08-06`

## 주요 결과물

| 파일 | 역할 |
|---|---|
| `appearance_reference_sync/index.html` | 기존 이슈판트 외형 샘플 형식을 따른 전체 장검 기준 비교 페이지 |
| `appearance_reference_sync/summary.html` | 같은 형식의 빠른 승인 판정용 요약 페이지 |
| `appearance_reference_sync/Ispant_DrawSword_Index_Desktop.png` | 전체 페이지 데스크톱 브라우저 렌더 확인 |
| `appearance_reference_sync/Ispant_DrawSword_Index_Narrow.png` | 전체 페이지 720px 반응형 렌더 확인 |
| `appearance_reference_sync/Ispant_DrawSword_Summary_Desktop.png` | 요약 페이지 데스크톱 브라우저 렌더 확인 |
| `appearance_reference_sync/Ispant_DrawSword_Summary_Narrow.png` | 요약 페이지 720px 반응형 렌더 확인 |
| `Ispant_DrawSword_FinalReview.png` | 기준 이미지·정면·측면·손잡이 확대 최종 검토본 |
| `Ispant_DrawSword_Full.png` | 현재 발검 자세 기준 정면 렌더 |
| `Ispant_DrawSword_Side.png` | 칼날 두께와 몸체 간섭 측면 렌더 |
| `Ispant_DrawSword_HandleCloseup.png` | 손잡이·가드·폼멜 확대 렌더 |
| `Ispant_DrawSword_ArtSample.blend` | 리그·Mixamo 액션·재질이 포함된 검사 가능한 Blender 원본 |
| `Ispant_DrawSword_ArtSample.fbx` | FBX 3D 검토본 |
| `Ispant_DrawSword_ArtSample.glb` | GLB 3D 검토본 |
| `Ispant_DrawSword_ArtSample_Report.json` | 제작 치수·부모 연결·입력과 출력 해시 기록 |
| `Ispant_DrawSword_ExportValidation.json` | BLEND·FBX·GLB 독립 재가져오기 검사 결과 |
| `scripts/build_ispant_draw_sword_art_sample.py` | 샘플·렌더·3D 파일 재현 스크립트 |
| `scripts/validate_art_sample_exports.py` | 3D 출력 구조 재검사 스크립트 |

## 입력 무결성

- Unity 발검 파생 FBX SHA-256: `B9DEB78C6BECA61C81EE5ECD86C4763E56186B8925EED29720B4B62ED482CE42`
- 승인된 정적 외형 BLEND SHA-256: `4DA80CAB606F6A7A3C76FFB216636DC0D2CF39ADBA82FC4431741977CDE1EDEF`
- 기준 이미지 SHA-256: `5B220058A614D883B9A82B6E84EB00F362587E29FDF4F641C051AD6AAABDC1DB`
- 제작 후 Unity 발검 FBX 해시 보존: `True`
- 샘플 제작 단계 Unity 수정: `False`
- 사용자 승인 후 적용 씬 수정: `Assets/_Project/Scenes/CargoRunMvp.unity`

## 장검 구조 기록

- 오브젝트: `Ispant_Reference_LongSword` 단일 메시
- 정점: `2,080`
- 삼각형: `4,092`
- 전체 치수: `0.198372m × 0.076m × 1.0715m`
- 칼날 길이·뿌리 폭·두께: `0.82m × 0.068m × 0.012m`
- 가드 폭: `0.19m`
- 손잡이 길이·지름: `0.17m × 0.052m`
- 폼멜 길이: `0.055m`
- 부모: `Armature` / `BONE` / `mixamorig:RightHand`
- 기존 손잡이 중심 정렬 오차: `0m`
- 오른손 본 원점–손잡이 중심: `0.0557747075m`
- 기준 이미지에서 보이지 않는 후면 처리: 앞면 윤곽의 좌우 대칭 두께만 적용

## 머티리얼

1. `Ispant_LongSword_WornSteel`
2. `Ispant_LongSword_BrownLeather`
3. `Ispant_LongSword_DarkEngraving`

장검 전용 절차형 PBR 재질만 새로 만들었으며, 몸체는 승인된 정적 이슈판트
샘플의 기존 재질 객체를 직접 사용합니다.

## 3D 재가져오기 검사

- BLEND: 장검 메시 1개, 2,080정점, 4,092삼각형, 재질 3개, 오른손 본 부모 연결 `PASS`
- FBX: 장검 메시 1개, 2,080정점, 4,092삼각형, 재질 3개, 오른손 본 부모 연결 `PASS`
- GLB: 장검 메시 1개, 3,597분할 정점, 4,092삼각형, 재질 3개, 오른손 본 부모 연결 `PASS`
- 전체 치수 일치: `PASS`
- 입력 FBX 해시 보존: `PASS`

출력 SHA-256:

- BLEND: `F112EFF207D2EAB5FC89AF5735103877B51F973FBAD9B5EBD8D3DBEB44770FB9`
- FBX: `FCDFE999620445DC85AD2535977FA5D9E4EE5325BF625C3C8439A020482F1392`
- GLB: `C659F47C298459169F0B69DBB77C5CA84E1E81275363455344E959CB4B40BFA0`
- 최종 4분할 검토본: `1952F107915AF4B7DA3AB33663261E53C8F00D2930A24E0DC76CD09862E6FF22`

## 승인 대상에서 제외

- `diagnostics/` 아래 진단 렌더와 기준 이미지 크롭
- 파일명에 `FAILED`가 포함된 과거 렌더
- Blender 자동 백업 파일

## HTML 확인

- `index.html`: 로컬 참조 14개, 누락 `0`, 기존 템플릿 핵심 선택자 일치 `PASS`
- `summary.html`: 로컬 참조 5개, 누락 `0`, 기존 템플릿 핵심 선택자 일치 `PASS`
- 데스크톱 1,440px 2열 비교: `PASS`
- 좁은 화면 720px 1열 전환: `PASS`
- 한글·상태 코드·이미지 잘림 및 겹침: 없음

## 승인 후 Unity 적용 기록

- 적용 슬롯: `12`
- 정적 슬롯 `1,2,5,6,7,8,9,10,11,12`: 기존 `Hips` 장착 위치 유지
- 이동 슬롯 `3`: 기존 `mixamorig:Hips` 장착 위치 유지
- 발검 슬롯 `4`: `mixamorig:RightHand` 직접 자식, 1~46프레임 반복 재생 유지
- 공통 승인 장검: 원본 `2,080`정점·`4,092`삼각형·`0.198372m × 0.076m × 1.0715m`
- 최대 부착 오차: `0m`
- 전 구간 오른손 표면 최대 거리: `0.01812077m`
- 발검 장검 최대 월드 이동량: `0.8065813m`
- 기존 장검 렌더러: `0`
- 기존 발검 칼집 렌더러: `0`
- 독립 구조 검사: `PASS`, 검사로 인한 씬 변경 `False`
- BaseColor Unity 색 공간: 승인 선형색을 sRGB 전달함수로 인코딩
- Unity 장검 재질 노출: `3×`
- 손잡이 중심–오른손 가중치 메시 중심: `0.00000006m~0.004224267m`
- 장검·오른손 최대 월드 회전: 각각 `179.3398°`
- 장검–오른손 상대 회전 오차: `0°`

원본 FBX와 승인된 Blender 샘플은 수정하지 않았습니다.
`Run-HarnessValidation.ps1`, EditMode·PlayMode 테스트와 빌드는 실행하지
않았습니다.

## 0.9m 개정 샘플 · 승인 대기

- 위치: `length_0_9m_revision/`
- 상태: `ART_SAMPLE_PENDING_USER_APPROVAL_2026-08-06`
- 전체 길이 / 칼날 길이: `0.9m / 0.6485m`
- 손잡이 `0.17m`, 가드 폭 `0.19m`, 폼멜 `0.055m`: 변경 없음
- Unity 적용: `NOT_APPLIED`
- 전체 검토 HTML: `length_0_9m_revision/appearance_reference_sync/index.html`
- 하네스 검증, EditMode·PlayMode 테스트와 빌드는 실행하지 않았습니다.
