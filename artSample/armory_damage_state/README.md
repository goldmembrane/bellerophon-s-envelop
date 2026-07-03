# armory_damage_state

AR-12 포탑 조종대 파손/폭파 상태 승인용 Blender 샘플입니다.

## 목적

무기실 내구도 0% 상태에서 보일 포탑 조종대 손상 외형을 Unity에 반영하기 전에 검사하기 위한 샘플입니다.

## 반영 기준

- 조종대 본체는 기울어지지 않은 정상 배치 상태입니다.
- AR-05 U자형 포탑 핸들 부분만 왼쪽으로 45도 꺾인 파손 상태입니다.
- 파손된 화면은 조종대 소형 화면이 아니라 AR-06 커브형 대형 모니터입니다.
- AR-06 커브형 대형 모니터는 꺼진 검은 화면이며 파손되어 있습니다.
- 액정 깨짐은 사용자 제공 이미지처럼 중심 충격점에서 긴 방사형 균열이 뻗고, 주변에 짧은 잔금과 흰 유리 파편이 몰린 형태로 만들었습니다.
- 조종대 주변에는 반투명 연기 볼륨, 얇은 연기 줄기, 바닥 그을림을 넣었습니다.
- 실제 Unity 파티클, 폭발 로직, 피해 로직은 포함하지 않습니다.

## 포함

- `blender/armory_damage_state.blend`
- `exports/armory_damage_state.fbx`
- `exports/armory_damage_state.glb`
- `renders/*.png` 4개 구도
- `index.html`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- Unity 씬 배치
- 실제 폭발/연기 파티클 시스템
- 체력 피해 로직
- 포탑 수동 모드 UI
- 외부 선체 포탑 모델
