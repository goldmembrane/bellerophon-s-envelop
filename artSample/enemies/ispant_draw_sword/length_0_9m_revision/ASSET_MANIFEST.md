# 이슈판트 발검 장검 0.9m 개정 에셋 매니페스트

상태: `ART_SAMPLE_PENDING_USER_APPROVAL_2026-08-06`

Unity 적용 상태: `NOT_APPLIED`

## 주요 결과물

| 파일 | 역할 | SHA-256 |
|---|---|---|
| `Ispant_DrawSword_0_9m_ArtSample.blend` | 검사 가능한 Blender 원본 | `52E995ED3B121C5363E53FFA8BB832D7BF9FF4795560711AA7D12105C9CABA3D` |
| `Ispant_DrawSword_0_9m_ArtSample.fbx` | FBX 검토본 | `4058A90B57C8ABA7BCAF185B9BF5D1D1C47C2F0E0991A43BE5178E071D543208` |
| `Ispant_DrawSword_0_9m_ArtSample.glb` | GLB 검토본 | `7A3B8F91C5F9FCAFDBB78493C89755458F478130BC717FD73EE9B0281AF6AF85` |
| `Ispant_DrawSword_0_9m_FinalReview.png` | 1.0715m 승인본과 0.9m 개정본 최종 비교 | `51D701716C9A037C7AB3B3F387921BD1408A20AF590DC948FD230C950D61A2BB` |
| `Ispant_DrawSword_0_9m_Full.png` | 0.9m 개정본 전체 렌더 | `44D9B8DE4BBCAC4A96F9FE87C2338FC9B6DBEF772D2ABE5BF010D6D01452D31D` |
| `Ispant_DrawSword_0_9m_ArtSample_Report.json` | 원본 보존·칼날 축소·손잡이 서명 기록 | — |
| `Ispant_DrawSword_0_9m_ExportValidation.json` | BLEND·FBX·GLB 재가져오기 결과 | — |
| `appearance_reference_sync/index.html` | 전체 승인 검토 페이지 | — |
| `appearance_reference_sync/summary.html` | 빠른 승인 판정 페이지 | — |

## 원본과 개정 경계

- 원본 승인 BLEND: `../Ispant_DrawSword_ArtSample.blend`
- 원본 승인 BLEND SHA-256: `F112EFF207D2EAB5FC89AF5735103877B51F973FBAD9B5EBD8D3DBEB44770FB9`
- 원본 전체 길이 / 칼날 길이: `1.0715m / 0.82m`
- 개정 전체 길이 / 칼날 길이: `0.9m / 0.6485m`
- 보존 치수: 손잡이 `0.17m`, 가드 폭 `0.19m`, 폼멜 `0.055m`
- 변경 대상: 칼날 연결 성분 3개의 로컬 길이축 좌표만
- 변경하지 않은 대상: 손잡이·가드·폼멜의 형상, UV, 머티리얼 배치와 장검 부모 연결

## 독립 재가져오기 검사

| 형식 | 정점 | 삼각형 | 치수 | 재질 | 오른손 본 부모 |
|---|---:|---:|---|---|---|
| BLEND | 2,080 | 4,092 | `0.198372 × 0.076 × 0.9m` | 3개 | `PASS` |
| FBX | 2,080 | 4,092 | `0.198372 × 0.076 × 0.9m` | 3개 | `PASS` |
| GLB | 3,597 분할 정점 | 4,092 | `0.198372 × 0.076 × 0.9m` | 3개 | `PASS` |

구조 검사 결과는 `PASS`이며 사용자 승인과 Unity 적용을 의미하지 않습니다.
Blender 자동 백업 파일과 `diagnostics/`는 승인 대상에서 제외합니다.

## 실행하지 않은 항목

- Unity 12개 슬롯 반영과 발검 20프레임 회전 구현
- `Run-HarnessValidation.ps1` 하네스 검증
- EditMode·PlayMode 테스트와 Windows 빌드
- Unity 종료·재시작
- Git 작업
