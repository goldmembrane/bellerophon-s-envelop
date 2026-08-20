# 이슈판트 발검 칼날 시각적 위쪽 수정 최종 검사

## 적용 대상과 원인

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 대상: `Approved Ispant Enemy Placement/Ispant_04_DrawSword/Ispant_New_Direct_Model`
- 기존 종료 기준 `animatedModel.up`이 현재 임포트 모델의 화면상 아래쪽을 가리키고 있었다.
- 종료 칼날 목표를 정확한 반대축인 `-animatedModel.up`으로 변경했다.
- 이 문서는 이전 `Ispant_New_DrawSword_GripOffset_UpwardFinish_Inspection.md`의 종료 방향 결과를 대체한다.

## 직접 시각 검사

- 수치 검사를 통과한 뒤 승인된 대상의 임시 복제본만 격리 렌더링했다. 원본 씬 오브젝트는 캡처에 사용하지 않았고 렌더 뒤 임시 카메라·조명·복제본을 폐기했다.
- `01_Start_Oblique.png`: 시작 시 칼날이 캐릭터 하체 옆 아래쪽에 있다.
- `02_Quarter_Oblique.png`: 발검 초반 칼날이 팔과 몸 앞을 통과하며 일부 가려지는 구간이다.
- `03_Middle_Oblique.png`: 중간 시점에 칼날이 대각선 위쪽으로 올라간다.
- `04_ThreeQuarter_Oblique.png`: 75% 시점에 칼끝이 화면 위쪽을 향하며 수직에 가까워진다.
- `05_End_Oblique.png`: 종료 시 칼끝이 캐릭터 머리 방향인 화면 위쪽을 향한다.
- `06_End_Grip_Close.png`: 종료 손잡이 근접 화면에서 칼자루가 오른손 손바닥·손가락 범위 안에 있으며 허공으로 분리된 틈이 보이지 않는다.
- `07_End_Side.png`: 종료 측면 화면에서도 칼끝이 화면 위쪽을 향한다.
- 전환 중 칼날이 다시 아래로 꺾이거나 갑자기 반전되는 장면은 보이지 않았다.
- 판정: `PASS`

## 수치 및 실시간 검사

- 애니메이션: `1.5초`, `60fps`, 정방향 전용, 마지막 뒤 첫 프레임으로 즉시 복귀
- 검사 프레임: 시작과 끝을 포함한 `91프레임`
- 시작 칼날과 시각적 위쪽 축의 각도: `121.4129°`
- 종료 칼날과 시각적 위쪽 축의 각도: `0°`
- 칼날 방향이 변한 프레임: `90/90`
- 프레임당 최대 각도 변화: `5.971924°`
- 적용 칼자루 외향 거리: `0.1m`
- 칼자루 거리 최대 오차: `0.000123993m`(약 `0.124mm`)
- 손·칼자루 최소 접촉 여유: `0.1295443m`
- 장검 최대 이동량: `1.220308m`
- 오른손 최대 이동량: `0.9560029m`
- 마지막 뒤 첫 자세로 즉시 복귀하는 위치 차이: `0.7322837m`
- 마지막 뒤 첫 자세로 즉시 복귀하는 각도 차이: `165.5526°`
- 역방향 프레임: 없음
- 장검 메시: `20,409` 정점, `19,950` 삼각형, 스키닝·블렌드셰이프 없음
- Unity AnimationMode 실시간 검토: `7회` 반복 후 정상 중지
- 중지 후 Transform·씬 상태 복원: `PASS`
- 캡처 중 그림자 아틀라스 해상도 자동 축소 안내가 있었으나 애니메이션·씬 오류는 없었다.

## 격리 및 무결성

- 컨트롤러 씬 참조: `1개`
- 실시간 검 추종 컴포넌트 씬 참조: `1개`
- 다른 11개 이슈판트 슬롯 변경: 없음
- 이슈판트 배치 밖 씬 루트 변경: 없음
- 직접 모델 및 장검 메시 변경: 없음
- 임시 롤백 표식: `0개`
- 씬 SHA-256: `58F4CD42F749B9F8577A6BF0082B101FEE87330D4902C9FFE35E9C729FD7DCAC`
- 직접 모델 FBX SHA-256: `5CE54F6117AF08F141BC18A0E46C823AD07877D815DA2906D59CA2967A4974FF`
- Mixamo 원본 FBX SHA-256: `EFF460E3201EFF5749A13705898B019C68036F25A7FEEFC9B18F7503FCEF1F81`
- 반복 클립 SHA-256: `23A4BAFDECB558F44312A30CCE61DD082EAE8B3321FF4EEFEA21AC4EBBDEB507`
- 컨트롤러 SHA-256: `C7220652A3F478426FDFDF2ED1B210073EB7F38AF142BEE1806A6568462FA31C`
- 실시간 추종 코드 SHA-256: `C69E5672230567BC53B226BCE1E079E7FBC363975B80054F091104F2C37446F4`
- 적용·검사·캡처 도구 SHA-256: `87C4E7D8A5A3344B9DE296823F265632A648AD86C433E44898A846D8830F5C09`
- Unity 브리지 SHA-256: `5EB5B5588E686783FE9C001472FE6CFEE19DDD5D54D0B477356233E4353039AB`

## 시각 검사 이미지 SHA-256

- `01_Start_Oblique.png`: `81CADD8E1FF83ED68607FA60E2FAEB34247EEB6D00842C721D9CC9240ED19501`
- `02_Quarter_Oblique.png`: `AAB8287C499528B59FCCED259AC0390DC84E75B5AC39BA1BA00968B0F9F34ECC`
- `03_Middle_Oblique.png`: `E45578B90856446C44C171D0D983852A39DD9C571729606093A6646AAE2CEDAC`
- `04_ThreeQuarter_Oblique.png`: `13F1775BF92020891F08F1F7C1B6FC86AE319BDE159894A9E1E6145194BCC903`
- `05_End_Oblique.png`: `EAF88AC253F484AE45BCA174A621AD153843B60E0C862FD301F339D588DB3714`
- `06_End_Grip_Close.png`: `13079959DCE93CDE103BAE38E13D64B54C313CA43B826E2F38C0BB216AFE4EF2`
- `07_End_Side.png`: `97C739D5C78E07059D512372F9290457DBC1819560BB68DC819ED6E09BA92D47`

## 실행하지 않은 항목

- `Run-HarnessValidation.ps1` 하네스 검증 및 관련 실행·재실행·조사·로그·문서·생성물 작업
- 역방향 애니메이션 생성 또는 재생
- 원본 FBX 수정
- 장검 메시 변형·스키닝·분리·교체
- 다른 이슈판트 슬롯 및 배치 밖 씬 루트 수정
- 전체 화면 또는 다른 프로그램이 포함된 데스크톱 캡처
- Git 작업
