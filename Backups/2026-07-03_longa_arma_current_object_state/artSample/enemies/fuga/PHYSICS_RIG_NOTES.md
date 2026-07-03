# 푸가 물리/애니메이션 적용 계획

- 푸가는 비행 씨앗체이므로 적용 단계의 루트 이동은 `Rigidbody + Collider` 기준으로 처리합니다.
- Motion Path는 실제 Transform 직접 이동 도구가 아니라 호버 위치, 접근 경로, 공격 목표를 편집하는 기준으로만 사용합니다.
- 실제 이동은 `Rigidbody.linearVelocity`, velocity 제어, 또는 `AddForce` 계열로 추종합니다.
- 같은 Transform을 Motion Path, Rigidbody, AnimationClip, IK, Joint, 보조 흔들림이 동시에 직접 움직이지 않게 역할을 분리합니다.
- 샘플에는 Unity 적용 검토를 위한 Shape Key 이름을 포함했습니다.
  - `Idle_Hover_Breathing_Surface_Pulse`
  - `Move_Wingbeat_Forward_Glide`
  - `Attack_Wing_Slap_Front_Lunge`
  - `Hit_Recoil_Altitude_Drop`
  - `Death_Folded_Wings_Fall`
- 사망 모션은 다음 흐름으로 Unity 적용 단계에서 구성합니다.
  1. 공중 부유 상태에서 시작합니다.
  2. 몸체가 한쪽으로 기울어집니다.
  3. 날개가 접히거나 힘이 빠집니다.
  4. `Rigidbody + Collider` 기준으로 바닥 쪽으로 낙하합니다.
  5. 바닥에 기울어진 자세로 충돌/정착합니다.
  6. 최종적으로 움직임이 줄어든 사망 포즈를 유지합니다.
- 샘플 렌더 `10_death_01_hover_start.png`부터 `15_death_06_final_still_pose.png`까지는 위 흐름을 Unity 구현 전 검토하기 위한 정적 시퀀스입니다.
- Unity 적용 시 정적 비교 1개체와 대기, 이동, 공격, 피격, 사망 상태를 분리해 확인 가능하게 배치합니다.
