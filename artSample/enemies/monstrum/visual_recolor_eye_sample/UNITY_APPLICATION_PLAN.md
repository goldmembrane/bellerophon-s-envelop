# Unity 적용 계획

이 문서는 샘플 승인 후 별도 승인 요청에 포함할 적용 계획 초안입니다. 현재 샘플 생성 단계에서는 실제 씬을 저장하거나 런타임 에셋에 연결하지 않았습니다.

## 대상

- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`
- 루트: `Approved Monstrum Enemy Placement`
- 기준 오브젝트: `Monstrum_00_Static_Review`
- 필요 시 같은 시각 상태를 맞출 후보 슬롯: `Monstrum_02_Idle`부터 `Monstrum_09_Death`까지의 검토 슬롯

## 적용 방식

- 몸통: 몬스트룸 렌더러에 어두운 녹색 머티리얼과 `textures/monstrum_dark_moss_body_albedo.png` 계열 텍스처를 적용
- 눈: 본체 메시를 직접 수정하지 않고, 머리 앞쪽 자식 오브젝트로 작은 황색 눈틈만 추가
- 눈 충돌: 눈 오브젝트에는 Collider를 두지 않음
- 원본 FBX: 직접 수정하지 않음
- 씬 저장: 승인받은 대상 루트 외 오브젝트는 생성, 삭제, 비활성화, 이동, 이름 변경하지 않음

## 샘플 좌표

- EyeCenter: (0.002, 1.63, -0.471)
- LeftEye: (0.049, 1.63, -0.471)
- RightEye: (-0.045, 1.63, -0.471)
- EyeHeightReference: 0.01
- EyeSeparation: 0.047
