# 오스티나토 제공 피격 FBX 교체 결과

## 적용 대상

- 장면 개체: `Approved Ostinato Enemy Placement/Ostinato_05_Hit_Recoil/Ostinato_Model`
- 외부 원본: `enemies model/ostinato hitted.fbx`
- Unity 복사본: `Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_05_Hit_Recoil.fbx`
- 선택 테이크: `mixamo.com`
- 선택 구간: `0~86프레임` 전체
- 재생 설정: `60fps`, `1.433333초`, 반복 재생, 상태 속도 `1`, `Animator.applyRootMotion=False`

## 교체 결과

- 기존 자체 제작 피격 자식 개체를 삭제하고 제공 FBX 직접 인스턴스로 교체했다.
- 교체 전후 GlobalObjectId가 달라 실제 개체 교체가 확인됐다.
- 제공 원본과 Unity 복사본의 SHA-256은 모두 `AB55910AE10AA48A100C1DD433917BB68E08786B9AE34290EA9062DA9C74C6C3`이다.
- `mixamo.com` 기본 테이크의 시작·종료 프레임을 그대로 사용했다. 키, 포즈, 타이밍, 애니메이션 커브는 수정하지 않았다.
- 적용 전후 임포트 커브 지문은 `F4495E826BDF017E73D01F33E2CD351DDB185198748629D0E8A29472191E782D`로 동일하다.

## 외형 동기화

- 기준 개체는 `Ostinato_01_Static_Review`다.
- 승인 메시 `Ostinato_ApprovedUnity.fbx`를 제공 FBX의 동일한 24본 리그에 연결했다.
- 승인 머티리얼 `Chitin`, `SoftTissue`, `HookBlade`, `CompoundEye` 4종을 기준 개체와 같은 순서로 사용한다.
- 제공 FBX에 함께 들어 있던 별도 비승인 렌더러는 표시되지 않는다.
- 다른 오스티나토 8개 슬롯은 변경되지 않았다.

## 실제 재생 확인

- 독립 점검 통과 뒤 최종 캡처를 한 번 실행했다.
- Unity Editor Play Mode 실제 Animator를 정면·사선 15개 시점으로 확인했다.
- 제공 모션의 전신 움찔과 연속 자세 변화가 재생됐으며 승인 외형은 전 구간 유지됐다.
- 메시 찢어짐, 관절 분리, 개체 소실은 보이지 않았다.
- 관찰된 개체 루트 이동과 회전은 모두 `0`이다.

![Unity Play Mode 최종 접촉 시트](Ostinato_HitRecoil_RuntimeContactSheet.png)

## 기록 파일

- 적용 기록: `Ostinato_HitFbxReplacementApply.txt`
- 독립 점검: `Ostinato_HitFbxReplacementInspection.txt`
- 실제 재생 기록: `Ostinato_HitRecoil_RuntimePlayback.txt`
- 최종 접촉 시트: `Ostinato_HitRecoil_RuntimeContactSheet.png`
- 개별 프레임: `runtime_frames/`

이전에 에이전트가 제작한 피격 Blend·생성기·점검 기록은 이력으로만 보존하며 현재 피격 개체 재생에는 사용하지 않는다.
