# 이슈판트 승인 장검 12개 슬롯 적용 결과

상태: `PASS`

- 적용 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 적용 슬롯: `12`
- 정적 슬롯 `1,2,5,6,7,8,9,10,11,12`: 기존 `Hips` 장착 위치 유지
- 이동 슬롯 `3`: 기존 `mixamorig:Hips` 장착 위치 유지
- 발검 슬롯 `4`: `mixamorig:RightHand` 직접 자식
- 승인 장검: `2,080`정점, `4,092`삼각형
- 발검 검사 프레임: `1~46`, 반복 재생 `True`
- 최대 부착 오차: `0m`
- 오른손 표면 최대 거리: `0.01812077m`
- 최대 추종 이동량: `0.8065813m`
- 장검·오른손 최대 월드 회전: 각각 `179.3398°`
- 장검–오른손 상대 회전 오차: `0°`
- 손잡이 중심–오른손 가중치 메시 중심: `0.00000006m~0.004224267m`
- 승인 BaseColor sRGB 전달함수 교정: 적용
- Unity 장검 재질 노출: `3×`
- 기존 장검 렌더러: `0`
- 기존 발검 칼집 렌더러: `0`
- 독립 검사에 의한 씬 변경: `False`

구조 상세는 `Ispant_ApprovedLongSword_Inspection.txt`에 기록했습니다.
노출 보정 전에 생성된 어두운 이미지는
`discarded/Ispant_ApprovedLongSword_PreExposure_Dark_Rejected.png`로 분리했습니다.
승인된 1회 캡처 범위를 넘는 추가 캡처는 실행하지 않았습니다.

`Run-HarnessValidation.ps1`, EditMode·PlayMode 테스트와 빌드는 실행하지
않았습니다.
