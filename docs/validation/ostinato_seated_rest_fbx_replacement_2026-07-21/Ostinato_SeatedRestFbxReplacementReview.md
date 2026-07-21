# 오스티나토 제공 착석 휴식 FBX 교체 결과

## 적용 대상

- 장면 개체: `Approved Ostinato Enemy Placement/Ostinato_07_Seated_Rest/Ostinato_Model`
- 외부 원본: `enemies model/ostinato sitting.fbx`
- Unity 복사본: `Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_07_Seated_Rest.fbx`
- 선택 테이크: `mixamo.com`
- 선택 구간: `0~96프레임` 전체
- 재생 설정: `60fps`, `1.6초`, 반복 재생, 상태 속도 `1`, `Animator.applyRootMotion=False`

## 교체 결과

- `Ostinato_07_Static_Review`의 기존 정적 자식 개체를 삭제하고 제공 FBX 직접 인스턴스로 교체했다.
- 교체 전후 GlobalObjectId가 달라 실제 개체 교체가 확인됐다.
- 제공 원본과 Unity 복사본의 SHA-256은 모두 `5A8750A865B24DFB1C32134994CC0FEDB2B90BDE2CB383DB51FC52C048EDAECA`다.
- `mixamo.com` 기본 테이크의 시작·종료 프레임을 그대로 사용했다. 키, 포즈, 타이밍, 애니메이션 커브는 수정하지 않았다.
- 적용 전후 임포트 커브 지문은 `99518A569E731406F6ACCA90D5D2733988EF26A8D9A51DE61F1906CC7EB55B91`로 동일하다.

## 외형 동기화

- 기준 개체는 `Ostinato_01_Static_Review`다.
- 승인 메시 `Ostinato_ApprovedUnity.fbx`를 제공 FBX의 동일한 24본 리그에 연결했다.
- 승인 머티리얼 `Chitin`, `SoftTissue`, `HookBlade`, `CompoundEye` 4종을 기준 개체와 같은 순서로 사용한다.
- 제공 FBX의 별도 비승인 렌더러는 표시되지 않는다.
- 다른 오스티나토 8개 슬롯은 변경되지 않았다.

## 실제 재생 확인

- 독립 점검 통과 뒤 최종 캡처를 한 번 실행했다.
- Unity Editor Play Mode 실제 Animator를 정면·사선 15개 시점으로 확인했다.
- 기립 상태에서 몸을 낮춰 깊게 착석하고 양팔·칼날을 아래로 이완한 뒤 다시 일어나는 동작이 연속적으로 재생됐다.
- 승인 외형이 전 구간 유지됐으며 메시 찢어짐, 관절 분리, 개체 소실은 보이지 않았다.
- 관찰된 개체 루트 이동과 회전은 모두 `0`이다.

![Unity Play Mode 최종 접촉 시트](Ostinato_SeatedRest_RuntimeContactSheet.png)

## 기록 파일

- 대상 조사: `Ostinato_SeatedRestFbxReplacementTarget.txt`
- 적용 기록: `Ostinato_SeatedRestFbxReplacementApply.txt`
- 독립 점검: `Ostinato_SeatedRestFbxReplacementInspection.txt`
- 실제 재생 기록: `Ostinato_SeatedRest_RuntimePlayback.txt`
- 최종 접촉 시트: `Ostinato_SeatedRest_RuntimeContactSheet.png`
- 개별 프레임: `runtime_frames/`
