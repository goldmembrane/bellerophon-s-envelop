# 오스티나토 사망 FBX 교체 결과

## 적용 대상

- 장면 개체: `Approved Ostinato Enemy Placement/Ostinato_09_Death/Ostinato_Model`
- 외부 원본: `enemies model/ostinato death.fbx`
- Unity 복사본: `Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_09_Death.fbx`
- 선택 테이크: `mixamo.com`
- 선택 구간: Unity 기준 `0~181프레임` 전체
- 재생 설정: `60fps`, `3.016667초`, 반복 재생, 상태 속도 `1`, `Animator.applyRootMotion=False`

## 교체 결과

- `Ostinato_09_Static_Review`의 기존 정적 자식 개체를 삭제하고 제공된 사망 FBX의 직접 인스턴스로 교체했다.
- 교체 전후 GlobalObjectId가 달라 실제 개체 교체가 확인됐다.
- 외부 원본과 Unity 복사본의 SHA-256은 모두 `28476FC7621DF2139CB21C377A73BD75E516468AFC8CD03504E8E5D2F4F90AB3`이다.
- FBX가 가진 두 기본 테이크 중 사용자가 지정한 `mixamo.com`을 정확히 하나 선택했고, 해당 테이크의 시작·종료 프레임을 그대로 사용했다.
- 키, 타이밍, 포즈, 애니메이션 커브는 수정하지 않았다. 적용 전후 커브 지문은 `581E255E5905DAA33069E76C9468EA005DFF9ADF516CBF181C5EEC3420F13FAA`로 동일하다.

## 외형 동기화

- 기준 개체는 `Ostinato_01_Static_Review`이다.
- 승인 메시 `Ostinato_ApprovedUnity.fbx`를 사망 FBX의 동일한 24본 리그에 연결했다.
- 승인 머티리얼 `Chitin`, `SoftTissue`, `HookBlade`, `CompoundEye` 4종을 기준 개체와 같은 순서로 사용했다.
- 사망 FBX의 별도 비승인 렌더러는 표시하지 않는다.
- 다른 오스티나토 8개 슬롯은 변경되지 않았다.

## 실제 재생 확인

- 입력 점검과 저장 후 독립 점검을 통과한 다음 최종 캡처를 한 번 실행했다.
- Unity Editor Play Mode의 실제 Animator를 정면·사선 15개 시점으로 확인했다.
- 웅크린 자세에서 일어서고 뒤로 쓰러져 누운 자세까지 이어지는 사망 동작이 두 시점에서 정상적으로 보였다.
- 승인 외형은 전 구간 유지됐으며 메시 찢어짐, 관절 분리, 개체 소실은 보이지 않았다.
- 요청된 반복 재생 설정에 따라 종료 뒤 시작 자세로 되돌아간다.
- 관찰된 개체 루트 이동과 회전은 모두 `0`이다.

![Unity Play Mode 최종 접촉 시트](Ostinato_Death_RuntimeContactSheet.png)

## 기록 파일

- 대상 조사: `Ostinato_DeathFbxReplacementTarget.txt`
- 적용 기록: `Ostinato_DeathFbxReplacementApply.txt`
- 독립 점검: `Ostinato_DeathFbxReplacementInspection.txt`
- 실제 재생 기록: `Ostinato_Death_RuntimePlayback.txt`
- 최종 접촉 시트: `Ostinato_Death_RuntimeContactSheet.png`
- 개별 프레임: `runtime_frames/`
