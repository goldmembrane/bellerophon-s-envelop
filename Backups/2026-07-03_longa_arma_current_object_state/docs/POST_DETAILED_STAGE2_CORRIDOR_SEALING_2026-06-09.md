# Post-detailed Stage 2 Corridor Sealing Notes - 2026-06-09

## 2026-06-10 최신 기준

- 사용자 확인으로 `Control Room -> Armory` 물리 복도와 `Phase 16 Map Corridor - ControlRoom to Armory` HUD 맵 선은 다시 생성한다.
- `Control Room -> Armory` 복도는 통제실 남쪽의 `Cargo Hold` 출입구 바로 옆에서 시작하고, 무기고 북쪽의 `Cargo Hold` 출입구 옆으로 연결한다.
- 무기고 쪽에서는 기존 `Cargo Hold -> Armory` 램프 입구를 막지 않는 3-segment 경로를 사용하고, 중간 랜딩은 세그먼트 바닥과 같은 높이로 맞춰 턱이 생기지 않게 한다.
- 계속 금지되는 물리 복도와 HUD 맵 선은 `Control Room -> Supply Room`, `Supply Room -> Control Room`뿐이다.
- 최신 허용 복도는 10개다. 기존 9개 복도에 `Control Room -> Armory`를 추가한다.
- `Phase4CargoShipGrayboxPlayModeSmoke`는 10개 정의 복도를 모두 CharacterController로 통과해야 한다.
- 최신 통과 기준은 `Corridors=10`, `ValidatedRoutes=10`, `ArmoryCargoSegments=1`, `SupplyCargoSegments=1`, `SupplyArmorySegments=1`, `ControlArmorySegments=3`이다.

## 2026-06-09 이전 기준

- 정의되지 않은 `Control Room -> Armory`, `Control Room -> Supply Room`, `Supply Room -> Control Room` 물리 복도와 HUD 맵 선은 만들지 않는다.
- 최종 허용 복도는 9개다: Cargo Hold에서 Cockpit, Engine Room, Control Room, Armory, Supply Room으로 가는 복도, Supply Room에서 Armory로 가는 복도, Cockpit에서 Engine Room 및 Control Room으로 가는 복도, Engine Room에서 Control Room으로 가는 복도.
- 방 문 개구부가 복도보다 넓어 생기는 좌우 구멍은 threshold `Left Mouth Closure Wall` 및 `Right Mouth Closure Wall`로 막는다.
- 낮은 Cargo Hold와 높은 데크 사이 경사 복도에서 생기는 상단 검은 구멍은 `Upper Bulkhead Wall`과 4.4m 폭 `Ceiling Cap`으로 막는다.
- `Floor Lip`은 시각용으로만 유지한다. collider를 넣으면 경사 램프 끝에서 CharacterController 이동을 막는다.
- `Engine Room -> Control Room` 복도는 기존 정의된 전면 우회 복도만 유지하고, 코너 내부 벽은 CharacterController 통과를 위해 짧게 끊는다.
- 중간에 꺾이는 복도 route point는 threshold와 별개로 `Joint Seal`을 가져야 한다.
- `Joint Seal`은 조인트 천장 캡과, 열려 있어야 하는 이동 방향을 제외한 바깥쪽 closure wall들로 구성한다.
- `Engine Room -> Control Room` 조인트 1/2는 각각 최소 2개 이상의 closure wall과 하나의 ceiling cap을 가져야 한다.

## 검증 기준

- `Phase4CargoShipGrayboxPlayModeSmoke`는 9개 정의 복도를 모두 CharacterController로 통과해야 한다.
- `PostDetailedStage2ShipInteriorEditorValidation`은 모든 threshold의 mouth closure wall, upper bulkhead wall, 넓어진 ceiling cap 존재와 최소 폭/높이를 확인해야 한다.
- `Phase4CargoShipGrayboxEditorValidation`은 중간 corridor joint의 ceiling cap 크기와 closure wall 개수를 확인해야 한다.
- 최신 통과 기준은 `Corridors=9`, `ValidatedRoutes=9`, `ArmoryCargoSegments=1`, `SupplyCargoSegments=1`, `SupplyArmorySegments=1`이다.

## 2026-06-09 경사 복도 endpoint 실링 추가 기준

- 경사 복도는 Cargo Hold에서 Cockpit, Engine Room, Control Room, Armory, Supply Room으로 올라가는 5개 복도다.
- 기존 threshold seal은 복도 방향으로 회전되어 있어, 대각선 경사 복도에서는 축정렬된 방 벽 개구부와 복도 외피 사이에 삼각형 외부 노출 틈이 남을 수 있다.
- 각 경사 복도 시작/끝에는 `Sloped Endpoint Seal`을 반드시 생성한다.
- `Sloped Endpoint Seal`은 방 벽 평면에 맞춘 `Room Plane Left/Right Closure Wall`, `Room Plane Upper Bulkhead Wall`, `Room Plane Ceiling Cap`과 복도 방향으로 3.2m 깊게 들어가는 `Sleeve Left/Right Closure Wall`, `Sleeve Ceiling Cap`으로 구성한다.
- `Phase4CargoShipGrayboxEditorValidation`과 `PostDetailedStage2ShipInteriorEditorValidation`은 5개 경사 복도의 양 끝, 총 10개 endpoint에서 위 실링 오브젝트가 빠지면 실패해야 한다.
- 검증 결과: `Run-Phase4CargoShipGrayboxSmoke.ps1`, `Run-PostDetailedStage2ShipInteriorSmoke.ps1`, `Run-HarnessValidation.ps1`, `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`, `Build-WindowsDev.ps1` passed.

## 2026-06-09 경사 복도 옆 틈 전환 메쉬 추가 기준

- 사용자 재검토에서 Cargo Hold-Control의 Cargo Hold 쪽, Cargo Hold-Engine의 Engine Room 쪽, Cargo Hold-Armory 양쪽, Cargo Hold-Supply 양쪽 입구 옆 틈에 외부 공간이 보이는 문제가 확인되었다.
- 원인은 방 벽 평면 패치와 복도 방향 슬리브가 서로 다른 각도로 서 있으면서, 두 실링 사이의 옆면 전환부가 실제 메쉬로 이어져 있지 않았기 때문이다.
- 각 `Sloped Endpoint Seal`은 이제 `Left Side Wedge Fill`과 `Right Side Wedge Fill`을 가진다.
- side wedge는 방 벽 평면의 문 옆 edge와 복도 슬리브의 문 옆 edge를 직접 잇는 수직 prism mesh이며, renderer와 mesh collider를 모두 가진다.
- `Phase4CargoShipGrayboxEditorValidation`과 `PostDetailedStage2ShipInteriorEditorValidation`은 각 경사 endpoint의 side wedge mesh와 collider가 없으면 실패해야 한다.
- 검증 결과: `Run-Phase4CargoShipGrayboxSmoke.ps1`, `Run-PostDetailedStage2ShipInteriorSmoke.ps1`, `Run-HarnessValidation.ps1`, `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`, `Build-WindowsDev.ps1` passed.
