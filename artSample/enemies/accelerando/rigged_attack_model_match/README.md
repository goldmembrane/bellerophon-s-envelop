# Accelerando 리깅 공격 모델 일치 샘플

상태: **사용자 검토 및 승인 대기**  
Unity 런타임 적용: **미적용**

## 작업 목표

- `enemies model/accelerando.glb`의 `UniRigArmature`와 18본 스킨 구조를 유지합니다.
- 현재 Unity에 배치된 Accelerando의 몸체 색, 갑각 색, 녹슨 금속 사슬·철퇴 외형을 재현합니다.
- 좌우 철퇴를 몸체와 분리해 이후 Unity에서 `Rigidbody + ConfigurableJoint` 사슬 물리에 연결할 수 있게 합니다.
- 철퇴 아래에 노출되던 옛 막대 지지물과 보이는 `MaceSocket_Ring`을 제거합니다. 물리용 끝점은 렌더되지 않는 Empty 앵커로만 남깁니다.
- 공격 동작에서 직접 제어할 범위를 좌우 더듬이 본 체인으로 한정합니다.

## 구성

- 몸체: `Accelerando_RiggedAttack_Body`
- 리그: `UniRigArmature`, `Bone_000`~`Bone_017`
- 공격 제어 본:
  - 오른쪽 더듬이: `Bone_008`, `Bone_007`, `Bone_006`
  - 왼쪽 더듬이: `Bone_011`, `Bone_010`, `Bone_009`
- 사슬: 좌우 각 12링
- 철퇴: `Accelerando_Left_MaceHead`, `Accelerando_Right_MaceHead`
- 숨김 물리 앵커:
  - `Accelerando_*_AntennaPhysicsAnchor`
  - `Accelerando_*_MacePhysicsAnchor`
- 보이는 하단 소켓 링: 0개

## 색과 재질 기준

현재 Unity 머티리얼 값을 기준으로 맞췄습니다.

- 젖은 회갈색 살점: Base Color `(0.39, 0.32, 0.27)`, Smoothness `0.72`
- 짙은 갑각: Base Color `(0.14, 0.12, 0.10)`, Smoothness `0.32`
- 녹슨 금속: Base Color `(0.30, 0.15, 0.08)`, Metallic `0.72`, Smoothness `0.46`

## 검토 파일

- 정면: `renders/accelerando_rigged_attack_front.png`
- 측면: `renders/accelerando_rigged_attack_side.png`
- 사선: `renders/accelerando_rigged_attack_oblique.png`
- 리그 표시: `renders/accelerando_rigged_attack_rig_overlay.png`
- 더듬이 포즈 정면: `renders/accelerando_rigged_attack_pose_front.png`
- 더듬이 포즈 사선: `renders/accelerando_rigged_attack_pose_oblique.png`
- 포즈 리그 표시: `renders/accelerando_rigged_attack_pose_rig_overlay.png`
- 원본 작업 파일: `exports/accelerando_rigged_attack_model_match.blend`
- 범용 검토 파일: `exports/accelerando_rigged_attack_model_match.glb`
- 수치와 해시: `asset_manifest.json`

포즈 렌더는 리깅 범위와 사슬·철퇴의 전방 흔들림 연결 계획을 확인하기 위한 기능 미리보기입니다. AnimationClip이나 가속 제어는 이 샘플에 포함하지 않았습니다.

## 검증 결과

- 내보낸 GLB 재임포트 확인: Armature 1개, 본 18개, 스킨 그룹 18개
- 가중치가 있는 몸체 정점: 전체 정점
- 좌우 체인: 각각 12링
- 분리 철퇴: 좌우 각 1개
- 보이는 `MaceSocket_Ring`: 0개
- 옛 철퇴 막대 지지물 제거: 220면
- 더듬이 밖 공격 본 잔여 웨이트 정리: 863정점
- 더듬이 포즈 이동 정점: 1,777개
- 직접 포즈한 비공격 본: 0개
- 원본 GLB와 `CargoRunMvp.unity` 해시: 작업 전후 동일

## 승인 후 Unity 적용 예정 범위

- 대상 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 대상 루트: `Approved Accelerando Enemy Placement/Accelerando_03_Antenna_Strike`
- 교체 대상: 해당 루트 아래 `Accelerando_Model`
- 유지 대상: 공격 슬롯의 기존 Animator 상태, 물리 사슬 구성, 배치 Transform
- 연결 계획: 더듬이 본 애니메이션 → 숨김 안테나 앵커 → ConfigurableJoint 사슬 → 분리 철퇴 Rigidbody

이 샘플 승인만으로 Unity 씬이나 런타임 에셋은 변경하지 않습니다. 실제 적용은 별도의 묶음 승인을 받은 뒤 진행합니다.

