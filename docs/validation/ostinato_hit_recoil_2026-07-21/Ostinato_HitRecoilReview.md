# 오스티나토 피격 모션 검토

## 적용 대상

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 개체: `Approved Ostinato Enemy Placement/Ostinato_05_Hit_Recoil/Ostinato_Model`
- 클립·상태: `Ostinato_05_Hit_Recoil`
- 재생 방식: Unity Editor Play Mode의 실제 씬 `Animator`

## 동작 구성

- 전체 반복 주기는 `0.70초`, 프레임 레이트는 `60fps`다.
- `0.183333초`에 최대 피격 자세에 도달한다.
- 몸통은 뒤로 `26.99983도` 반동하고 머리는 뒤로 `58.99811도` 반동한다.
- 머리 후방 이동은 `0.110458m`, 척추 후방 이동은 `0.036476m`다.
- `0.50초`에 기본 자세로 완전히 돌아오며 `0.70초` 반복 경계까지 기본 자세를 유지한다.
- 모델 루트의 위치·회전은 움직이지 않으며 `Animator.applyRootMotion=False`다.

## Unity 시각 판정

- 최대 반동 프레임에서 고개가 위·뒤로 크게 젖혀지고 가슴과 상체가 함께 뒤로 움찔한다.
- 머리 움직임이 몸통 움직임보다 크게 읽혀 사용자가 지정한 피격 의도가 정면과 사선에서 모두 분명하다.
- 다리와 발은 고정돼 미끄러지지 않고, 복귀 뒤 시작 자세와 같은 실루엣으로 돌아온다.
- 메시 찢어짐, 관절 역전, 칼날·손목 분리, 개체 소실은 보이지 않았다.
- 승인된 정적 메시와 `Chitin`, `SoftTissue`, `HookBlade`, `CompoundEye` 머티리얼 4종은 그대로 유지됐다.

## 결과물

- 적용 기록: `Ostinato_HitRecoilApply.txt`
- 독립 점검: `Ostinato_HitRecoilInspection.txt`
- 실제 재생 기록: `Ostinato_HitRecoil_RuntimePlayback.txt`
- 최종 정면·사선 접촉 시트: `Ostinato_HitRecoil_RuntimeContactSheet.png`
- 연속 원본 프레임: `runtime_frames/`

수치 점검과 실제 Unity 재생 시각 판정은 모두 통과했다. 최종 캡처 명령은 관련 점검 통과 후 한 번만 실행했다.
