# 오스티나토 공격 FBX 직접 인스턴스 적용 결과

## 씬 교체 결과

- 대상: `Approved Ostinato Enemy Placement/Ostinato_04_Scissor_Attack`
- 삭제한 대상: 슬롯 아래의 기존 공격 모델 인스턴스 1개
- 새 프리팹 원본: `Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack.fbx`
- 사용자 원본: `enemies model/ostinato attack.fbx`
- 원본 및 Unity FBX SHA-256: `0430EEE08D7D2AFBC645ECD3DFDA7B37C1DBF7BB4A6A34AF761E9AFAD1708260`

현재 씬 개체는 정적 승인 FBX의 인스턴스가 아니라 사용자 공격 FBX를 임포트한 프리팹의 직접 인스턴스다. 기존 슬롯의 로컬 Transform과 sibling index `3`은 유지했다.

## 정적 외형 동기화

- 표시 메시: `Ostinato_ApprovedUnity.fbx`의 정적 승인 메시
- 표시 메시 외형 지문: `BD8D446D63E2479B647BC23470F85B350630718AF753B1C21859EE34DA7330F0`
- 정적 승인 메시 외형 지문: `BD8D446D63E2479B647BC23470F85B350630718AF753B1C21859EE34DA7330F0`
- 본 연결: 정적 승인 메시의 본 순서와 루트 본을 공격 FBX 리그의 동일 이름 본에 재매핑
- 머티리얼: `Chitin | SoftTissue | HookBlade | CompoundEye`

따라서 애니메이션과 개체 원본은 공격 FBX가 담당하고, 화면에 보이는 메시·재질은 정적 승인 오스티나토 외형과 일치한다.

## 애니메이션 상태

- 재생 테이크: `mixamo.com`
- 범위: Unity `0~196프레임`
- 길이·프레임률: `3.266667초`, `60fps`
- 반복 재생: `Loop Time=True`
- Controller 속도: `2`
- 기본 실효 반복 주기: 약 `1.633334초` (`3.266667초 ÷ 2`)
- 추가 속도 프로필: 없음. 전체 구간 `2배속` 고정
- 커브 바인딩: `60개`
- 현재 임포트 커브 지문: `B463F3A2CB5A022CBC2A0034D19BCF0A5CAEDB5C2BE3C0587B56F7D88BAB78FC`
- 모션 보정 표식: `Bellerophon.OstinatoForwardSlashMotion.v4`
- 모션 보정: FBX 임포트 후처리로 원본 `53~93프레임`의 양쪽 상완·전완 Euler 회전 12개 바인딩만 재구성. 원본 바깥 베기 `101~115프레임`의 정규화된 관절 각속도 진행률을 같은 총 회전 호에 적용
- 폐합 베기 속도: 양손 평균 시작·최대 `3.437368`, 평균 `2.155606`, 최소 `0.812071`, 초기/평균 비율 `1.594618`
- 비대상 커브 지문: 적용 전후 `FA28500CB635103B310340574C0D20FEB8B9A662A01F7D14CDC9F56C1726C289`로 동일
- Animator root motion 덮어쓰기: 없음

이전 수평 칼날 보정 클립과 `Ostinato_04_Scissor_Attack_BladeWristRig.asset`, `Ostinato_04_Scissor_Attack_RigidBladeRig.asset`은 현재 개체에서 사용하지 않는다. 씬에는 `LeftBladeControl`, `RightBladeControl`, `LeftBladeRigidRoot`, `RightBladeRigidRoot`도 없다. 관련 보정 에셋 파일은 승인 범위에 따라 삭제하지 않았다.

## 직접 확인

Unity Play Mode에서 41개 연속 프레임을 정면과 3/4 시점으로 확인했다. 바깥 베기의 가속·감속 진행률을 적용한 폐합 베기와 나머지 공격 전개·회수 중 정적 승인 외형과 머티리얼이 유지됐으며 칼날과 손목의 분리, 메시 늘어짐, 비정상 스케일 변형, 개체 소실은 보이지 않았다. 관찰된 모델 루트 이동과 회전은 모두 `0`이다.

![공격 FBX 직접 인스턴스 연속 비교](Ostinato_AttackAppearanceComparison.png)

## 실행하지 않은 항목

- 기존 칼날 보정 `.anim`·파생 메시 에셋 파일 삭제
- `53~93프레임` 밖 공격 움직임·커브·포즈·타이밍 수정
- 다른 오스티나토 슬롯 수정
- AI·피해·히트박스·물리·게임플레이 수정
- 하네스 검증 및 관련 실행·재실행·조사·로그·문서·생성물 작업
- EditMode/PlayMode 테스트
- 빌드
- Unity 재시작
- Git 작업
