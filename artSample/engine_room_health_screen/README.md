# engine_room_health_screen

ER-09 동력기계 내구도 스크린의 승인용 Blender 샘플입니다.

## 목적

동력실 옆면에 붙는 물리 스크린 하우징에 기존 에셋의 컴퓨터 화면 텍스처를 넣어 확인하기 위한 샘플입니다.  
내구도 색상, 수치, 게이지, 경고 문구 같은 세부 표시 정보는 모델링하지 않고 런타임 UI 구현 대상으로 남겼습니다.
메인 스크린 하나만 배치했을 때 벽면이 허전해 보이는 문제를 줄이기 위해, 별도 기능을 갖지 않는 장식용 보조 스크린 2개를 함께 배치했습니다.

## 에셋 기준

- 주 후보: `Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/big_screen.prefab`
- 화면 텍스처: `Assets/Heavy Station Kit/BASE/Textures/Displays/B2_Eq41_E.png`
- 보조 후보: `console_screen.prefab`, `computer_station.prefab`, `decorative_wall_4_computer.prefab`
- 승인 후 Unity 적용 시에는 실제 프리팹 또는 그에 맞춘 편집 가능한 부품 구조로 옮깁니다.

## 포함

- 벽면 부착형 스크린 프레임
- `B2_Eq41_E.png`의 반복 화면 중 좌상단 단일 화면 타일이 꽉 차게 들어간 디스플레이 면
- 기능 없는 장식용 보조 스크린 2개
- 런타임 UI 정렬용 코너 탭
- 후면 서비스 플레이트, 볼트, 힌지, 케이블 소켓
- ER-10 오버클럭 장치 연결을 위한 하단 예비 커버
- 동력실 옆면 벽 배치 기준 프록시

## 제외

- 내구도 색상 상태 UI
- 내구도 수치, 게이지, 경고 문구, 파형 표시
- 스크린 파괴 상태
- 실제 오버클럭 상호작용 장치
- Unity 씬 배치와 충돌 설정
- 보조 스크린의 별도 기능 또는 런타임 UI 의미
