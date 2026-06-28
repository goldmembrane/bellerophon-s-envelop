# Enemy Modeling Plan 2026-06-28

## 목적

원본 기획서(`docs/GAME_DESIGN_SOURCE.txt`)와 `image/` 폴더의 레퍼런스 이미지를 기준으로 적대 개체 모델링 대상, 우선순위, 산출물, 승인 절차를 정리한다.

이 문서는 실제 모델 생성 문서가 아니라 작업 계획 문서다. Blender 모델, `artSample/` 샘플, Unity 씬/프리팹/런타임 자산은 이 문서 작성 단계에서 생성하지 않는다.

## 적용 규칙

- 적대 개체 모델링은 아트/모델링 작업이므로 먼저 `artSample/`에 사용자가 확인 가능한 샘플을 만든다.
- 승인 전에는 Unity 씬, 프리팹, 런타임 자산, UI 흐름에 연결하지 않는다.
- `image/`의 기존 이미지는 창작 프롬프트가 아니라 재현 기준이다.
- 모델링 시 각 이미지를 실루엣, 비율, 주요 형태, 부품 경계, 표면 재질, 손상/오염, 카메라 각도로 분해한다.
- 정면만 있는 이미지는 후면/측면을 임의로 확정하지 않는다. 필요한 부분은 원본 기획서 기준으로 추론하고, 문서에 `추론`으로 표시한다.
- 각 개체는 승인용 렌더, Blender 원본, FBX, GLB, 설명 문서, 승인 상태 파일을 갖는다.
- Unity 적용 전에는 승인된 `artSample/` 렌더와 Unity 결과를 시각적으로 비교한다.

## 현재 기획 기준

적대 개체는 원본 기획서 기준으로 네 그룹이다.

- 씨앗체: 파르붐, 푸가, 롱가 아르마, 테르고, 우르제레, 소치에타스, 몬스트룸, 미메시스
- 외계 생명체: 칸타빌레, 콘 스피리토, 아첼레란도, 그라베, 스모르찬도, 오스티나토, 돌로레
- 화물 자유 연대: 니게티프, 리벨리온, 레지스탕스, 레볼루션
- 해적: 파후르, 쿠르사스, 이슈판트, 아타

`MVP_IMPLEMENTATION_ORDER.md` 기준으로 첫 침입자는 씨앗체이며, 첫 MVP 구현 대상은 파르붐 1종으로 확정되어 있다. 따라서 모델링도 파르붐부터 시작한다.

## 이미지 참조 인벤토리

### 씨앗체

| 대상 | 참조 이미지 | 비고 |
| --- | --- | --- |
| 파르붐 | `image/parvum(파르붐).png`, `image/parvum-back.png`, `image/parvum-beside.png` | 정면/후면/측면 존재 |
| 푸가 | `image/fuga2(푸가).png`, `image/fuga2-back.png`, `image/fuga2-beside.png` | 사용자 확인 기준 참조 세트 |
| 푸가 이전 참조 | `image/fuga(푸가).png`, `image/fuga-back.png`, `image/fuga-beside.png` | 최종 기준이 아니므로 보조 참고로만 사용 |
| 롱가 아르마 | `image/longa arma(롱가 아르마).png`, `image/longa arma-back.png`, `image/longa arma-beside.png` | 정면/후면/측면 존재 |
| 테르고 | `image/tergo(테르고).png`, `image/tergo-back.png`, `image/tergo-beside.png` | 정면/후면/측면 존재 |
| 우르제레 | `image/urgere(우르제레).png`, `image/urgere-move.png` | 기본/이동 상태 존재 |
| 소치에타스 | `image/societas(소시에타스).png`, `image/societas-eating.png` | 기본/섭취 상태 존재 |
| 몬스트룸 | `image/monstrum(몬스트룸).png`, `image/monstrum-back.png`, `image/monstrum-beside.png` | 정면/후면/측면 존재 |
| 미메시스 | `image/mimesis(미메시스).png`, `image/mimesis-beside.png` | 유저 의태형, `transfer` 플레이어 외형 기준과 함께 사용 |

### 외계 생명체

| 대상 | 참조 이미지 | 비고 |
| --- | --- | --- |
| 칸타빌레 | `image/cantabile(칸타빌레).png`, `image/cantabile-beside.png` | 정면/측면 존재 |
| 콘 스피리토 | `image/con spirito(콘 스피리토).png` | 정면/측면 추가 추론 필요 |
| 아첼레란도 | `image/accelerando(아첼레란도).png`, `image/accelerando-beside.png` | 정면/측면 존재 |
| 그라베 | `image/grave(그라베).png` | 정면 중심, 후면/측면 추론 필요 |
| 스모르찬도 | `image/smorzando(스모르찬도).png`, `image/smorzando-person.png` | 설치형/인간형 전환 상태 존재 |
| 오스티나토 | `image/ostinato(오스티나토).png`, `image/ostinato-back.png`, `image/ostinato-beside.png` | 정면/후면/측면 존재 |
| 돌로레 | `image/dolore(돌로레).png`, `image/dolore-attack.png` | 기본/공격 상태 존재 |

### 화물 자유 연대

| 대상 | 참조 이미지 | 비고 |
| --- | --- | --- |
| 니게티프 | `image/négatif(네거티프).png` | 파일명 표기는 네거티프, 기획서 표기는 니게티프 |
| 리벨리온 | `image/rébellion(리벨리온).png` | 원판형 4족 기계 |
| 레지스탕스 | `image/résistance(레지스탕스).png` | 인간형 AI 로봇 |
| 레볼루션 | `image/révolution(레볼루션).png`, `image/révolution-attack.png` | 기본/기관총 전개 상태 존재 |

### 해적

| 대상 | 참조 이미지 | 비고 |
| --- | --- | --- |
| 파후르 | `image/pāḫḫur(파후르).png` | 화염방사기 장비 |
| 쿠르사스 | `image/KUŠkursa(쿠르사).png` | 방패 장비, 사용자 확인 최종 표기는 쿠르사스 |
| 이슈판트 | `image/išpant(이슈판트).png`, `image/išpant-armed.png` | 기본/무장 상태 존재 |
| 아타 | `image/atta(아타).png` | 지휘관, 3방향 참고 이미지 구성 |

### 플레이어 외형 참조

| 파일 | 현재 판단 |
| --- | --- |
| `image/transfer(운송자).png`, `image/transfer-back.png`, `image/transfer-left.png`, `image/transfer-right.png` | 사용자 확인 결과 플레이어 외형 이미지다. 적대 개체로 만들지 않고, 미메시스의 의태 기준과 플레이어 스케일 기준으로만 사용한다. |

## 모델링 대상 목록

### 1. 파르붐

- 그룹: 씨앗체
- 원본 크기: 높이 40cm, 가로 약 35cm, 세로 약 40cm
- 역할: 금속을 우선 목표로 삼는 소형 근접 섭취형 개체
- 모델 핵심: 녹색 반투명 액체 덩어리, 큰 입, 날카로운 이빨, 젖은 점액 표면
- 참조: `parvum(파르붐).png`, `parvum-back.png`, `parvum-beside.png`
- 우선순위: 1순위, MVP 첫 침입자
- 샘플 상태: 가장 먼저 `artSample/enemies/parvum/` 생성

### 2. 푸가

- 그룹: 씨앗체
- 원본 크기: 높이 60cm, 가로 약 40cm, 세로 20cm
- 역할: 날아다니는 비행 개체, 날개 타격
- 모델 핵심: 소라형 몸체, 조류 날개, 눈/입, 녹색 유기질 껍질
- 참조: `fuga(푸가).png`, `fuga-back.png`, `fuga-beside.png`
- 기준 참조: 사용자 확인에 따라 `fuga2` 이미지 세트를 기준으로 한다.

### 3. 롱가 아르마

- 그룹: 씨앗체
- 원본 크기: 높이 80cm, 가로 약 70cm, 세로 약 150cm
- 역할: 비정상적으로 긴 왼팔로 내려찍는 장거리 근접 개체
- 모델 핵심: 4족 짐승 실루엣, 말형 머리, 비대칭 거대 칼팔, 점액 흐름
- 참조: `longa arma(롱가 아르마).png`, `longa arma-back.png`, `longa arma-beside.png`

### 4. 테르고

- 그룹: 씨앗체
- 원본 크기: 높이 약 150cm, 가로 약 70cm, 세로 약 30cm
- 역할: 유저 등 뒤를 노리는 인간형 드릴팔 개체
- 모델 핵심: 인간형 골격, 양팔 드릴, 녹색 점액 피부, 발밑 점액 고임
- 참조: `tergo(테르고).png`, `tergo-back.png`, `tergo-beside.png`

### 5. 우르제레

- 그룹: 씨앗체
- 원본 크기: 높이 약 100cm, 가로 약 60cm, 세로 약 60cm
- 역할: 구역 중앙에 고정되어 씨앗체를 강화하는 박스형 지원 개체
- 모델 핵심: 박스 몸통, 상단 원통 기둥, 이동 시 드러나는 바퀴, 점액 흐름
- 참조: `urgere(우르제레).png`, `urgere-move.png`

### 6. 소치에타스

- 그룹: 씨앗체
- 원본 크기: 높이 약 30cm, 가로 약 80cm, 세로 약 70cm
- 역할: 작은 개체 군집, 커다란 입 형태로 물어뜯음
- 모델 핵심: 작은 덩어리들의 군집, 금빛 씨앗 반점, 집합 입 형태, 스멀스멀 이동
- 참조: `societas(소시에타스).png`, `societas-eating.png`

### 7. 몬스트룸

- 그룹: 씨앗체
- 원본 크기: 높이 약 250cm, 가로 약 200cm, 세로 약 300cm
- 역할: 대형 괴수, 망치형 양손 내려찍기
- 모델 핵심: 온몸 씨앗/솜털 질감, 거대 발, 둔기형 양손, 대형 이족 실루엣
- 참조: `monstrum(몬스트룸).png`, `monstrum-back.png`, `monstrum-beside.png`

### 8. 미메시스

- 그룹: 씨앗체
- 원본 크기: 유저 기본 외형과 동일
- 역할: 유저 겉모습으로 의태, 입에서 빨대를 꺼내 씨앗 주입
- 모델 핵심: 기본 우주복 실루엣, 내부 녹색 유기체, 길게 뻗은 빨대형 주입 기관
- 참조: `mimesis(미메시스).png`, `mimesis-beside.png`
- 플레이어 외형 기준: 사용자 확인에 따라 `transfer(운송자)` 이미지 세트를 유저 기본 수트 기준으로 사용한다.

### 9. 칸타빌레

- 그룹: 외계 생명체
- 원본 크기: 높이 약 50cm, 가로 약 35cm, 세로 약 35cm
- 역할: 음파로 정지 상태이상을 부여하는 비행 개체
- 모델 핵심: 나방 몸체, 나비형 푸른 날개, 더듬이, 작은 집게 다리
- 참조: `cantabile(칸타빌레).png`, `cantabile-beside.png`

### 10. 콘 스피리토

- 그룹: 외계 생명체
- 원본 크기: 높이 약 90cm, 가로 약 70cm, 세로 100cm
- 역할: 무작위 돌진 개체
- 모델 핵심: 말 몸통, 개 머리, 5발 보행 기준, 붉은 털/근육 질감
- 참조: `con spirito(콘 스피리토).png`
- 확인 필요: 다섯 번째 다리 위치는 정면 이미지와 기획서 기준으로 추론해야 한다.

### 11. 아첼레란도

- 그룹: 외계 생명체
- 원본 크기: 높이 약 80cm, 가로 약 60cm, 세로 120cm
- 역할: 조우 시간이 길수록 빨라지는 달팽이형 개체
- 모델 핵심: 달팽이 몸통, 철퇴형 더듬이, 사슬/금속 구체, 점액 하체
- 참조: `accelerando(아첼레란도).png`, `accelerando-beside.png`

### 12. 그라베

- 그룹: 외계 생명체
- 원본 크기: 높이 약 160cm, 가로 약 100cm, 세로 약 50cm
- 역할: 느린 중대형 개체, 긴 팔 변형 공격
- 모델 핵심: 세로형 막대 몸통, 양복 무늬, 짧은 다리, 긴 팔, 반달형 절삭 팔
- 참조: `grave(그라베).png`
- 확인 필요: 공격 시 변형 팔 상태 이미지는 별도 샘플에서 추론 또는 추가 확인 필요

### 13. 스모르찬도

- 그룹: 외계 생명체
- 원본 크기: 설치 상태 높이 약 50cm, 가로 약 200cm, 세로 약 300cm
- 역할: 설치형 촛농 지대, 불이 꺼지면 자폭형 인간형으로 변환
- 모델 핵심: 녹은 촛농 바닥, 중앙 초와 불꽃, 인간형 전환체
- 참조: `smorzando(스모르찬도).png`, `smorzando-person.png`
- 산출물: 설치형과 인간형 전환체를 같은 샘플 안에서 상태별로 분리 렌더

### 14. 오스티나토

- 그룹: 외계 생명체
- 원본 크기: 높이 약 100cm, 가로 약 70cm, 세로 약 30cm
- 역할: 양팔 칼로 대상을 가위처럼 절단하는 개체
- 모델 핵심: 인간형 곤충 갑각, 안쪽 날이 달린 양팔 칼, 폭주/휴식 상태 고려
- 참조: `ostinato(오스티나토).png`, `ostinato-back.png`, `ostinato-beside.png`

### 15. 돌로레

- 그룹: 외계 생명체
- 원본 크기: 높이 약 180cm, 가로 약 140cm, 세로 약 300cm
- 역할: 액자형 대형 개체, 촉수/못으로 찌르고 끌어감
- 모델 핵심: 흐릿한 초상화 액자, 뒷면 4족 보행 팔다리, 식물성/점액성 프레임, 붉은 촉수 공격 상태
- 참조: `dolore(돌로레).png`, `dolore-attack.png`

### 16. 니게티프

- 그룹: 화물 자유 연대
- 원본 크기: 높이 약 50cm, 가로 40cm, 세로 40cm
- 역할: 화물 내구도를 훔쳐 주머니에 저장하는 소형 기계
- 모델 핵심: 쥐형 4족 기계, 천 주머니, 금속 발톱, 꼬리 케이블
- 참조: `négatif(네거티프).png`
- 표기: 문서에서는 기획서 표기 `니게티프`를 우선하고, 파일명만 `네거티프`로 병기

### 17. 리벨리온

- 그룹: 화물 자유 연대
- 원본 크기: 높이 약 70cm, 가로 100cm, 세로 80cm
- 역할: 화물 보호막 설치 후 원판형 사격 모드 전환
- 모델 핵심: 납작한 원판 몸통, 거미 다리 4개, 전면 스캔/총구 모듈, 공격 모드 다리 상승
- 참조: `rébellion(리벨리온).png`

### 18. 레지스탕스

- 그룹: 화물 자유 연대
- 원본 크기: 높이 약 150cm, 가로 90cm, 세로 60cm
- 역할: 비품실 무기 탈취, 인간형 주먹 공격
- 모델 핵심: 흰색/회색 인간형 AI 로봇, 녹색 머리띠, 푸른 발광 패널
- 참조: `résistance(레지스탕스).png`

### 19. 레볼루션

- 그룹: 화물 자유 연대
- 원본 크기: 높이 약 200cm, 가로 120cm, 세로 약 100cm
- 역할: 화물선 폭파 목표, 양팔 기관총 전환
- 모델 핵심: 원형 몸통, 두꺼운 로봇 다리, 변환 팔, 기관총 전개 상태
- 참조: `révolution(레볼루션).png`, `révolution-attack.png`

### 20. 파후르

- 그룹: 해적
- 원본 크기: 높이 약 150cm, 가로 80cm, 세로 40cm
- 역할: 미니 화염방사기 사용
- 모델 핵심: 두건 안드로이드, 이마 불 마크, 등쪽 연료 탱크, 화염방사기
- 참조: `pāḫḫur(파후르).png`

### 21. 쿠르사스

- 그룹: 해적
- 원본 크기: 높이 약 155cm, 가로 75cm, 세로 40cm
- 역할: 제압방패 방어 태세와 밀치기
- 모델 핵심: 두건 안드로이드, 흐르는 물 마크, 큰 제압방패, 방어 자세 실루엣
- 참조: `KUŠkursa(쿠르사).png`

### 22. 이슈판트

- 그룹: 해적
- 원본 크기: 높이 약 180cm, 가로 75cm, 세로 40cm
- 역할: 머스켓/장검 사용하는 중대형 기계 병사
- 모델 핵심: 초승달 장식 투구, 백색 중장갑, 머스켓, 장검 장착 기준
- 참조: `išpant(이슈판트).png`, `išpant-armed.png`

### 23. 아타

- 그룹: 해적
- 원본 크기: 높이 약 185cm, 가로 75cm, 세로 40cm
- 역할: 해적 지휘관, 구역 사보타주와 병력 명령
- 모델 핵심: 안대, 붉은 그리스 복장, 금속 안드로이드 골격, 권총/지휘관 장식
- 참조: `atta(아타).png`

## 우선순위

### 1차: MVP 첫 침입자

1. 파르붐

목표는 MVP 첫 침입자 외형을 확정하는 것이다. `artSample/enemies/parvum/`에 단독 샘플을 만들고, 정면/측면/후면/상단/공격 포즈 렌더를 승인받는다.

### 2차: 씨앗체 기본군

2. 푸가
3. 롱가 아르마
4. 테르고
5. 우르제레
6. 소치에타스
7. 몬스트룸
8. 미메시스

씨앗체는 금속 선호, 녹색 점액/씨앗 질감, 화물선 내부 침입이라는 공통성이 있으므로 먼저 같은 재질 체계를 만든다.

### 3차: 외계 생명체

9. 칸타빌레
10. 콘 스피리토
11. 아첼레란도
12. 그라베
13. 스모르찬도
14. 오스티나토
15. 돌로레

외계 생명체는 개체마다 재질과 실루엣이 다르므로, 공통 재질보다 상태/공격 포즈 기준을 먼저 확정한다.

### 4차: 화물 자유 연대

16. 니게티프
17. 리벨리온
18. 레지스탕스
19. 레볼루션

기계 계열은 모듈식 하드서피스 파이프라인을 사용한다. 같은 그룹의 흰색/회색 계열, 푸른 발광, 낡은 금속/케이블 기준을 공유한다.

### 5차: 해적

20. 파후르
21. 쿠르사스
22. 이슈판트
23. 아타

해적은 명령 체계와 장비 차이가 중요하므로 본체, 무장, 장비 상태를 분리한다.

## 공통 산출물 규격

각 적대 개체 샘플은 다음 구조를 사용한다.

```text
artSample/enemies/{enemy_id}/
  README.md
  APPROVAL_STATUS.json
  ASSET_MANIFEST.json
  index.html
  blender/{enemy_id}.blend
  exports/{enemy_id}.fbx
  exports/{enemy_id}.glb
  renders/01_front.png
  renders/02_side.png
  renders/03_back.png
  renders/04_top.png
  renders/05_behavior_pose.png
  renders/06_scale_check.png
```

필요한 경우 상태별 렌더를 추가한다.

- 공격 상태: `attack_pose`
- 이동 상태: `move_pose`
- 전환 상태: `alternate_form`
- 장비 전개 상태: `armed_pose`
- 설치/인간형 전환 상태: `installed_form`, `person_form`

## 작업 단계

1. 대상 1종 선택
2. 원본 기획서 문장과 이미지 파일 매핑 확정
3. 이미지 분해 메모 작성
4. Blender 모델링
5. 재질/텍스처 제작
6. 정면/측면/후면/상단/행동 포즈 렌더 생성
7. `artSample/` 승인 문서 작성
8. 사용자 검토와 승인
9. 승인 후 Unity용 FBX/GLB 임포트
10. Unity 프리팹 배치 샘플 생성
11. 승인 렌더와 Unity 화면 side-by-side 비교
12. visual sync가 충분할 때 런타임 연결 진행

## 모델링 기준

### 스케일

- 원본 기획서의 높이/가로/세로 값을 기준으로 Blender 단위 1m = Unity 1m로 작업한다.
- 사람이 기준인 개체는 기본 플레이어/운송자 키를 1.7m~1.8m 범위로 두고, 사용자 확인에 따라 `transfer(운송자)` 이미지를 플레이어 외형 기준으로 사용한다.
- 스케일 렌더에는 1m 기준 막대와 플레이어 실루엣을 함께 배치한다.

### 재질

- 씨앗체: 반투명 녹색 점액, 젖은 반사, 금빛 씨앗 반점, 내부 흐름, 일부 불투명 생체 조직
- 외계 생명체: 개체별 고유 재질 우선. 날개, 털, 촛농, 액자, 갑각, 금속 사슬 등 개별 소재를 분리
- 화물 자유 연대: 흰색/회색 기계, 낡은 금속, 천 주머니, 푸른 발광 패널
- 해적: 검은색/회색 고품질 기계, 두건/문양, 무장, 붉은 천 또는 금속 장식

### 리깅 전제

- MVP 샘플 단계에서는 정적 모델과 핵심 포즈를 우선한다.
- Unity 적용 직전 리깅이 필요한 개체는 관절 구조를 모델링 단계에서 분리한다.
- 액체/점액형 개체는 본 리깅보다 blend shape 또는 셰이더 변형이 적합한지 별도 판단한다.

## 사용자 확인 반영

- `fuga2` 세트는 사용자 확인에 따라 푸가 최종 기준 이미지로 확정됐다.
- `transfer(운송자)` 이미지는 사용자 확인에 따라 플레이어 외형 이미지로 확정됐다. 독립 적대 개체로 만들지 않는다.
- `니게티프`는 사용자 확인에 따라 최종 표기로 확정됐다. 파일명 `négatif(네거티프)`는 레퍼런스 파일명으로만 병기한다.
- `쿠르사스`는 사용자 확인에 따라 최종 표기로 확정됐다. 파일명 `KUŠkursa(쿠르사).png`는 레퍼런스 파일명으로만 병기한다.
- 미메시스는 유저 외형 복제 개체이므로, 최종 플레이어 기본 수트가 확정되기 전에는 완성형 Unity 프리팹을 고정하지 않는다.
- 침입선 모델은 원본 기획서에 별도 크기와 문양이 있지만, 현재 요청 범위가 적대 개체 본체 모델링이므로 이번 목록에서는 별도 후속 작업으로 둔다.

## 첫 작업 제안

첫 모델링 작업은 `파르붐`으로 시작한다.

이유:

- `MVP_IMPLEMENTATION_ORDER.md`에서 14단계 첫 침입자 MVP 구현 대상이 파르붐으로 확정되어 있다.
- 원본 기획서의 크기, 역할, 공격 방식이 명확하다.
- `image/`에 정면, 후면, 측면 레퍼런스가 모두 있다.
- 녹색 점액/금속 섭취/입 구조는 이후 씨앗체 공통 재질과 제작 기준이 된다.

파르붐 첫 샘플 산출물:

- `artSample/enemies/parvum/renders/01_front.png`
- `artSample/enemies/parvum/renders/02_side.png`
- `artSample/enemies/parvum/renders/03_back.png`
- `artSample/enemies/parvum/renders/04_top.png`
- `artSample/enemies/parvum/renders/05_bite_pose.png`
- `artSample/enemies/parvum/renders/06_scale_check.png`
- `artSample/enemies/parvum/blender/parvum.blend`
- `artSample/enemies/parvum/exports/parvum.fbx`
- `artSample/enemies/parvum/exports/parvum.glb`
- `artSample/enemies/parvum/index.html`
- `artSample/enemies/parvum/README.md`
- `artSample/enemies/parvum/APPROVAL_STATUS.json`
- `artSample/enemies/parvum/ASSET_MANIFEST.json`
