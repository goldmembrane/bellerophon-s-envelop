# 이슈판트 5번 Slash 상체 + Running 하체 합성 직접 대조

## 대상과 공급 원본

- Unity 대상: `Approved Ispant Enemy Placement/Ispant_05_RunningOneHandedSwordAttack/Ispant_New_Direct_Model`
- Slash 공급 원본: `enemies model/išpant-new slash.fbx`
- Slash SHA-256: `AB3346A9FA93A0FC6045D5155E60BBB65A095460A444E124D236B987F899FCDE`
- Slash 선택 Take: `mixamo.com`, 1–91프레임
- Running 공급 원본: `enemies model/išpant-new running.fbx`
- Running SHA-256: `A8471D0B2F1DF84D589A7BE3D54A171DF05E056915FD358FBEE0F74B5E1D77CB`
- Running 선택 Take: `mixamo.com`, 1–43프레임
- 두 원본과 현재 직접 모델은 Generic 24본 이름·부모 계층이 정확히 일치한다.

## 적용 순서와 범위

1. Animator Base Layer `Slash Full Body`에 Slash 전체 Transform 곡선을 적용했다.
2. Override Layer `Running Hips And Legs`에 Running의 `Hips`, 좌우 `UpLeg`, `Leg`, `Foot`, `ToeBase` 곡선만 적용했다.
3. Running Layer에는 상체 곡선이 0개이며 `AvatarMask`도 같은 하체 경로만 활성화한다.
4. `Hips` 로컬 X/Y 전진축만 첫 값으로 고정해 슬롯 안에서 제자리 반복하도록 했다. `Hips` 회전·로컬 Z 수직 바운스와 양다리 동작은 원본을 유지했다.
5. Animator의 Root Motion은 비활성화했다.

## 수치 검사

- Slash 원본과 Base 클립의 최대 곡선 오차: `0`
- Running 원본과 하체 클립의 최대 곡선 오차: `0`
- 최종 합성 상체와 Slash 원본의 로컬 위치 최대 오차: `0m`
- 최종 합성 상체와 Slash 원본의 로컬 각도 최대 오차: `0°`
- 최종 합성 하체와 Running 원본의 로컬 위치 최대 오차: `0m`
- 최종 합성 하체와 Running 원본의 로컬 각도 최대 오차: `0°`
- Running 상체 곡선 수: `0`
- BakeMesh 정점: 전부 유한
- 현재 본체 메시: 8,755정점·9,798삼각형
- 메시·본 웨이트·바인드포즈·머티리얼은 변경하지 않았다.

## 반복 재생 확인

- Unity Edit Mode에서 실제 5번 개체를 합성 상태로 반복 재생했다.
- 확인 시간 동안 Slash 4주기, Running 하체 8주기가 완료됐다.
- 재생 중 두 클립은 각자 원래 길이와 속도로 반복됐다.
- 중지 뒤 모든 Transform과 씬 상태를 복구했다.

## 원본 움직임 직접 대조

- 최종 비교 이미지: `captures/Ispant_05_Source_Composite_Comparison.png`
- 이미지 구성:
  - 위 행: Slash 공급 원본 `mixamo.com`
  - 가운데 행: Running 공급 원본 `mixamo.com`
  - 아래 행: 최종 Slash + Running 합성
  - 열: 시작, 25%, 50%, 75%, 종료
- 세 행은 현재 직접 모델 복제본, 같은 카메라 방향·배율·조명에서 촬영했다. 공급 FBX의 애니메이션 곡선을 현재 모델에 직접 샘플해 메시나 카메라 차이 없이 움직임만 비교했다.
- 직접 확인 결과:
  - 아래 행의 척추·어깨·팔·손·머리는 위 행 Slash의 준비·베기·회수 방향과 시점별 자세가 같다.
  - 아래 행의 골반·허벅지·무릎·발은 가운데 행 Running의 좌우 교대·착지·무릎 굽힘과 시점별 자세가 같다.
  - 상·하체 경계에서 순간적으로 꺾이거나 분리된 모습이 없다.
  - 15개 패널 전체에서 늘어진 판, 찢어진 메시, 부유 조각, 비정상 신체 관통이 보이지 않는다.
- 최종 시각 판정: `PASS`

## 범위 보존

- 다른 이슈판트 11개 슬롯 변경: 없음
- `Approved Ispant Enemy Placement` 밖 씬 루트 변경: 없음
- 장검·머스켓 배치 또는 추종 방식 변경: 없음
- 최종 씬 SHA-256: `5A550CFD50E3EE74CB8E7DBF272181E0044173979D5A9AA2AA8A2F29E6834B21`
- 캡처 중 기존 씬 조명에 의한 그림자 아틀라스 해상도 축소 경고가 기록됐지만 컴파일 오류·예외·실패 없이 비교 이미지 생성과 씬 비변경 확인을 마쳤다.

