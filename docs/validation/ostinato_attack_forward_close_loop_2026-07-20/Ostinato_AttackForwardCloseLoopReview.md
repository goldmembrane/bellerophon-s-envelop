# 오스티나토 공격 모션 개체 교체 및 전방 합장 구간 반복

## 적용 결과

- `Approved Ostinato Enemy Placement/Ostinato_04_Scissor_Attack`의 기존 공격 모션 자식을 삭제하고 `Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack.fbx`를 새 직접 인스턴스로 배치했다.
- Unity 프로젝트의 공격 FBX와 사용자 제공 `enemies model/ostinato attack.fbx`의 SHA-256은 모두 `0430EEE08D7D2AFBC645ECD3DFDA7B37C1DBF7BB4A6A34AF761E9AFAD1708260`으로 일치한다.
- 표시 메시를 승인된 정적 오스티나토 `Ostinato_ApprovedUnity.fbx`의 메시로 동기화하고 `Chitin`, `SoftTissue`, `HookBlade`, `CompoundEye` 머티리얼 4종을 같은 순서로 연결했다.
- 공격 원본 커브는 수정하지 않고 별도 클립 `Ostinato_04_Scissor_Attack_ForwardCloseLoop.anim`에 원본 `0~93프레임`만 복제했다.
- 반복 클립은 `60fps`, `1.55초`, `Loop Time=True`, Controller 재생 속도 `1`이며 `93프레임` 다음에 `0프레임`으로 돌아간다.
- 슬롯 Transform과 sibling index `3`, 나머지 오스티나토 슬롯 8개는 변경하지 않았다.

## 검사 결과

- 적용 전후 공격 자식의 GlobalObjectId가 달라 기존 개체 삭제와 새 개체 생성을 확인했다.
- FBX 직접 인스턴스, 승인 정적 메시, 승인 머티리얼 4종, Controller 연결, Root Motion 비활성 상태가 모두 통과했다.
- 원본과 반복 클립의 Float Curve Binding은 60개이며 Object Curve Binding은 0개다.
- `0~93프레임` 전 구간을 0.25프레임 간격으로 비교한 최대 값 오차는 `0`이다.
- 원본 FBX와 Unity 임포트 FBX 해시가 일치하며 원본 커브 지문은 적용 전후 유지됐다.
- 별도 칼날·손목 보정 제어 개체, 모델 편집, 본 편집은 없다.

## 최종 캡처

- Unity Edit Mode `AnimationMode`에서 `0, 15, 30, 53, 70, 84, 85, 90, 93, 0`프레임을 정면과 3/4 시점으로 정확 샘플링했다.
- 반복 경계는 `93→0`이며 수치 검사 통과 후 최종 접촉 시트를 1회 생성했다.
- 결과 이미지: `Ostinato_AttackForwardCloseLoopContactSheet.png`

## 실행하지 않은 항목

- 하네스 검증 및 관련 실행·재실행·조사·로그·문서·생성물 작업
- EditMode/PlayMode 테스트 및 빌드
- Unity 재시작
- 원본 FBX, 모델링, 본, 손목, 칼날, 원본 애니메이션 커브 수정
- 다른 오스티나토 슬롯 수정 및 기존 파생 애셋 삭제
- Git 작업
