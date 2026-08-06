# 이슈판트 발검 장검 0.9m 개정 아트 샘플

상태: `ART_SAMPLE_PENDING_USER_APPROVAL_2026-08-06`

Unity 적용 상태: `NOT_APPLIED`

- 기존 승인 장검 `Ispant_DrawSword_ArtSample.blend`를 원본으로 사용했습니다.
- 전체 길이는 `1.0715m`에서 `0.9m`로 줄였고, 칼날만 `0.82m`에서 `0.6485m`로 줄였습니다.
- 손잡이 `0.17m`, 가드 폭 `0.19m`, 폼멜 `0.055m`와 손잡이 형상·UV·머티리얼 배치는 변경하지 않았습니다.
- 칼날은 기존 연결 성분 3개의 길이축 좌표만 동일 비율 `0.7908536585365854`로 축소했습니다. 재메시·UV 재투영·새 장식 생성은 하지 않았습니다.
- 이 폴더의 샘플은 사용자 승인 전 검토본이며 Unity 씬과 12개 이슈판트 슬롯에는 적용하지 않았습니다.

## 승인 검토

- [최종 전후 비교 이미지](Ispant_DrawSword_0_9m_FinalReview.png)
- [전체 검토 HTML](appearance_reference_sync/index.html)
- [빠른 요약 HTML](appearance_reference_sync/summary.html)
- [Blender 원본](Ispant_DrawSword_0_9m_ArtSample.blend)
- [FBX 검토본](Ispant_DrawSword_0_9m_ArtSample.fbx)
- [GLB 검토본](Ispant_DrawSword_0_9m_ArtSample.glb)

## 구조 보존 결과

- 장검 메시: `2,080`정점, `4,092`삼각형
- 치수: `0.198372m × 0.076m × 0.9m`
- 부모: `Armature / BONE / mixamorig:RightHand`
- 손잡이 영역 형상·UV·머티리얼 서명: 수정 전후 동일 `06AD25964A6E973EC10E336AA5BDE8F04A8562180DF66EA4094F4E51FD7FEB87`
- 칼날 영역 토폴로지·UV·머티리얼 서명: 수정 전후 동일 `54BF28D5975A324BADCC3E44EFF6600C242D27771B8B757F0BFE46FA4211CA4F`
- BLEND·FBX·GLB 독립 재가져오기 구조 검사: `PASS`

## 승인 이후 예정 범위

이 샘플이 승인되면 별도 작업 승인 범위에서 Unity의 이슈판트 12개 장검에 같은
0.9m 외형을 적용합니다. 장착 위치는 유지하고 발검 개체만 오른손 추종을 유지하며,
기존 발검이 끝난 뒤 손 위치를 유지한 채 칼끝이 위를 향하도록 `0.8초` 동안
부드럽게 회전하는 `20프레임(25fps)` 구간을 추가합니다. 이 항목은 현재 샘플에는
구현하지 않았습니다.

## 실행하지 않은 항목

- Unity 씬·프리팹·런타임 자산 적용
- 발검 애니메이션 0.8초 회전 구간 구현
- Unity 종료·재시작
- `Run-HarnessValidation.ps1` 하네스 검증
- `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`, `Build-WindowsDev.ps1`
- Git 작업
