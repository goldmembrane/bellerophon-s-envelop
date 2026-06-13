# 에셋 배치 시안 샘플 기준

이 폴더는 Asset Store 에셋을 `CargoRunMvp` 런타임 씬에 적용하기 전에 검토할 시안을 저장하는 곳입니다.

## 승인 흐름

1. 각 단계마다 별도 폴더를 만든다.
   - 예: `step03_cargo_hold_2026-06-13`
   - 예: `step04_cockpit_2026-06-13`
2. 해당 폴더에 여러 구도 시안을 저장한다.
   - 플레이어 시점 구도
   - 대각선/측면 구도
   - 탑다운 또는 배치도 구도
3. 같은 폴더에 `ASSET_MANIFEST.md`를 만든다.
   - 사용할 프리팹 경로
   - 사용 목적
   - 배치 위치
   - 스케일 의도
   - 콜라이더 사용 여부
4. 같은 폴더에 `APPROVAL_STATUS.json`을 만든다.
   - 승인 전에는 `unity_application_allowed`가 `false`여야 한다.
5. 사용자 승인 후에만 `CargoRunMvp` 씬, 프리팹, 런타임 자산에 반영한다.

## 기본 산출물

- `README.md`: 시안 설명, Unity 반영 범위, 건드리지 않을 게임플레이 요소
- `ASSET_MANIFEST.md`: 정확한 프리팹 경로와 역할
- `APPROVAL_STATUS.json`: 승인 상태
- `view_01_player_entry.png`: 플레이어 진입 시점
- `view_02_diagonal.png`: 대각선 또는 측면 시점
- `view_03_layout.png`: 탑다운 또는 배치도

## 원칙

- 시안은 독립적인 장식 그림이 아니라 실제 Unity 반영 위치와 축척을 전제로 만든다.
- 사용자가 승인하지 않은 시안은 런타임 씬에 적용하지 않는다.
- 승인된 시안은 분위기 참고가 아니라 Unity 적용 기준이다.
- 에셋 원본 폴더는 수정하지 않는다.
