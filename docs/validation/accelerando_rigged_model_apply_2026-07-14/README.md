# Accelerando 승인 리깅 모델 Unity 적용 검증

## 결과

- 대상 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 대상 루트: `Approved Accelerando Enemy Placement`
- 적용 수: 기존 7개 슬롯 전체
- 적용 모델: `Assets/_Project/Art/Enemies/Accelerando/Models/accelerando_rigged_attack_model_match.glb`
- 구조 검증: 통과
- 시각 비교: 승인 샘플과 동일한 형상·실루엣·좌우 12링·분리 철퇴·하단 소켓 링 제거 상태 확인
- Unity 캡처는 샘플 렌더와 조명 환경이 달라 더 어둡지만, 살점·갑각·녹슨 금속 머티리얼의 수치값은 승인 샘플 기준과 동일하다.

## 비교 파일

- 승인 샘플 정면: `Target_Accelerando_RiggedAttack_Front.png`
- Unity 정적 정면: `Unity_Accelerando_Static_Front.png`
- 승인 샘플 사선: `Target_Accelerando_RiggedAttack_Oblique.png`
- Unity 정적 사선: `Unity_Accelerando_Static_Oblique.png`
- Unity 공격 입력 포즈 정면: `Unity_Accelerando_AttackInputPose_Front.png`
- Unity 공격 입력 포즈 사선: `Unity_Accelerando_AttackInputPose_Oblique.png`
- 일곱 슬롯 전체 정면/사선: `Unity_Accelerando_AllPlacements_Front.png`, `Unity_Accelerando_AllPlacements_Oblique.png`
- 구조 검증 기록: `validation_report.txt`

## 전용 검증 요약

- 각 슬롯: `UniRigArmature`, 본 18개, 스킨 몸체 1개
- 각 슬롯: 좌우 사슬 12개씩, 좌우 분리 철퇴 1개씩
- 렌더되는 `MaceSocket_Ring`: 0개
- 공격 클립 직접 제어 본: `Bone_008/007/006/011/010/009`만 사용
- 이동·공격 슬롯 각각: 동적 Rigidbody 24개, 키네마틱 Rigidbody 2개, ConfigurableJoint 24개
- 기존 배치 루트와 일곱 슬롯은 재생성하지 않았고 슬롯 Transform을 유지했다.

기존 Harness/EditMode/PlayMode/Build 검증은 실행하지 않았다.
