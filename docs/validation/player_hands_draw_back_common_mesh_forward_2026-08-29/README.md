# Hands_Draw_Back 공용 메시와 전방 인출 직접 검토

## 검토 순서

1. `common_mesh_direct_review_contact_sheet.png`
   - 1~3행: 상태 전용 메시 적용 전 기준의 전신 정면·측면·오른쪽 가슴 확대
   - 4~6행: 공용 플레이어 메시로 통일한 `Hands_Draw_Back`의 같은 구도
   - 7~9행: `Hands_Empty_Idle` 공용 메시 기준의 같은 구도
   - 각 행은 왼쪽부터 동일한 12개 정규화 위상이다.
2. `forward_direct_review_contact_sheet.png`
   - 1~5행: 원본 Take의 전신 정면·측면, 오른팔 확대 정면·측면, 오른쪽 가슴 확대 정면
   - 6~10행: 전방 인출 수정본의 같은 12위상·같은 구도
3. `final.png`
   - 두 직접 검토와 보조 지표가 통과한 뒤 `forward_direct_review_contact_sheet.png`를 한 번만 최종 확정한 이미지다.

## 1순위 직접 확인 결과

- 공용 메시 통일 뒤 부품 누락, 오른쪽 가슴의 새 돌출, 겨드랑이 찢김, 표면 꺼짐과 머티리얼 차이는 보이지 않았다.
- 원본은 등 뒤 인출 뒤 오른팔과 손이 머리 위로 향했다. 수정본은 팔꿈치가 몸 오른쪽 바깥으로 먼저 나오고 손이 명치 앞쪽으로 진행했다.
- 수정본 전 구간에서 몸통·얼굴 교차, 팔꿈치 역꺾임, 손목 직각 꺾임, 프레임 사이 튐, 가슴·겨드랑이 변형과 반복 경계 불연속은 보이지 않았다.

## 2순위 보조 확인 결과

- 특징점: 인출 시작 `43`, 바깥 경로 중간 `54`, 최대 도달 `66`프레임
- 최대 도달: 정면 오차 `0°`, 손–명치 높이 차이 약 `0.000002623m`, 팔꿈치 `29.998163°`, 손바닥 왼쪽 목표 오차 `0°`
- 반복: `69프레임×2루프`, 루트 변위 `0m`, 런타임 적용 오차 최대 약 `0.000000147m / 0°`
- 보존: 오른팔 3본 회전 외 자세 차이 `0`, 원본 FBX·공용 플레이어 FBX·상태 전용 메시·Stow Controller 해시 불변
- 상태: `Loop=True`, `ApplyRootMotion=False`, 공용 메시 구성 일치, 상태 전용 메시 및 BlendShape 곡선 무참조

세부 값은 `common_mesh_apply_metrics.json`, `common_mesh_review_metrics.json`, `forward_apply_metrics.json`, `forward_review_metrics.json`에 기록했다.
