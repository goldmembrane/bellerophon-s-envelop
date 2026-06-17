# engine_room_shell

ER-01 동력실 룸 쉘 승인용 Blender 샘플입니다.

## 목적

원본 기획서의 동력실 구조를 Unity에 넣기 전 검토하기 위한 Blender 모델링 샘플입니다.
아직 승인되지 않았으므로 실제 Unity 씬, 프리팹, 런타임 자산에 연결하지 않습니다.

## 반영 기준

- 우주선 내부 구역이므로 외벽은 막힌 금속 벽체입니다.
- 바닥은 전체가 메워진 연속 바닥입니다.
- 중앙 원통은 외부와 단절된 밀폐 구조입니다.
- 중앙 원통은 투명 재질이며, 안쪽의 동력 코어 내용물이 보여야 합니다.
- 위에서 내려다본 기준 조종실 입구는 1시 방향, 통제실 입구는 3시 방향, 운송창고 입구는 5시 방향입니다.
- 세 입구 외의 외벽은 막혀 있습니다.
- 5시 방향 운송창고 통로는 아래로 내려가는 경사 구조입니다.

## 포함

- `blender/engine_room_shell.blend`
- `exports/engine_room_shell.fbx`
- `exports/engine_room_shell.glb`
- `renders/*.png` 6개 구도
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- 내구도 스크린
- 오버클럭 상호작용 장치
- 손전등 충전 앵커
- 사보타주/복구 앵커
- 암전, 파괴, 오버클럭 상태 표현
- Unity 배치와 충돌 설정
