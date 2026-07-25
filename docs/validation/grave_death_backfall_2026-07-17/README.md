# Grave 뒤로 쓰러지는 사망 모션 작업 기록

## 사용자 지정 연출

- 발을 고정한 채 상체부터 뒤로 곧게 쓰러진다.
- `1.0초`에 등이 바닥에 닿고 `1.3초`까지 자세가 정착한다.
- 마지막에는 등이 바닥에 완전히 닿고, 팔은 몸통 옆, 다리는 곧고 평행한 정 자세를 유지한다.
- 바닥 충돌 반동은 없다.
- 공격 종료 후의 사망 모션이므로 오른팔 낫 형상이 남지 않는다.

## 구현

- 대상 슬롯: `Approved Grave Enemy Placement/Grave_05_Death/Grave_Model`
- 작업 클립: `Assets/_Project/Art/Enemies/Grave/Animations/Grave_Death_BackFall_Working.anim`
- 작업 컨트롤러: `Assets/_Project/Art/Enemies/Grave/Controllers/Grave_Death_BackFall_Working.controller`
- 적용 명령: `ApplyGraveDeathBackFallWorking`
- 길이: `1.3초`
- 키 시점: `0.0`, `0.2`, `0.5`, `0.8`, `1.0`, `1.3초`
- `Hips` 전신 회전과 위치 보정으로 발의 수평 위치를 기준 삼아 뒤로 쓰러지게 했다.
- 각 시점의 Bake Mesh 최저점을 바닥 높이에 맞춰 지면 관통을 막았다.
- 낙하 중 양팔과 양다리가 정 자세 방향으로 펴지며, `1.0초`와 `1.3초`는 동일한 최종 키로 반동 없이 고정된다.
- 사망 클립은 비루프이며, 낫 BlendShape가 있는 경우에도 전 구간 가중치를 `0`으로 유지한다.

## 자동 시각 검증

- 캡처 명령: `CaptureGraveDeathBackFallFrames`
- Unity Scene View 선택·포커스와 Play Mode 수동 재생을 사용하지 않았다.
- 측면과 사선 두 방향에서 동일한 카메라 구도로 여섯 시점을 자동 렌더했다.
- 캡처에만 임시 바닥 면을 추가해 발 고정, 뒤로 넘어지는 방향, 등과 바닥의 접촉을 실제 픽셀로 비교했다.
- 첫 렌더에서는 기본 리그의 굽은 팔다리가 최종 자세에 남아 결과를 채택하지 않았다.
- 팔다리를 정 자세 방향으로 펴는 곡선을 추가한 뒤 다시 적용·캡처했다.
- 최종 측면 시트에서 발을 기준으로 상체가 뒤로 기울고, `1.0초`에 등이 바닥선에 닿는다.
- 최종 사선 시트에서 정면 무늬가 위를 향하므로 뒤로 누운 방향이며, 팔은 몸통 양옆, 다리는 곧고 평행하다.
- `1.0초`와 `1.3초` 최종 프레임이 동일해 반동이 없고 낫 형상도 보이지 않는다.
- 측면 시트: `automated_visual_capture/Grave_Death_Side_ContactSheet.png`
- 사선 시트: `automated_visual_capture/Grave_Death_ThreeQuarter_ContactSheet.png`
- 현재 상태는 자동 시각 검증 통과이며 사용자 시각 승인 완료로 간주하지 않는다.

## 검토 개체 전용 반복 재생

- 사망 애니메이션 클립의 `Loop Time`은 켜지 않고 비루프 상태를 유지했다.
- `Grave_05_Death/Grave_Model`에 연결된 작업 컨트롤러의 `DeathBackFall` 상태에만 `ReviewLoop` 자기 전이를 추가했다.
- 자기 전이는 정규화 종료 시점 `1.0`, 전이 시간 `0초`, 오프셋 `0`으로 설정해 `1.3초` 종료 직후 첫 자세로 즉시 돌아간다.
- 적용 명령: `ApplyGraveDeathReviewLoop`
- 실제 컨트롤러 자동 캡처 명령: `CaptureGraveDeathReviewLoopFrames`
- 캡처는 클립을 시간 나머지로 직접 샘플링하지 않고, 검토 개체의 Animator와 작업 컨트롤러를 `120fps` 간격으로 실제 진행시켰다.
- 캡처 시점은 `0.00`, `0.80`, `1.29`, `1.31`, `2.10`, `2.59초`이며 첫 주기의 종료·재시작과 두 번째 주기의 같은 낙하·종료 자세를 포함한다.
- Animator 정규화 시간은 각각 `0.000`, `0.615`, `0.992`, `0.008`, `0.615`, `0.992`로 기록돼 `1.29~1.31초` 사이에 새 주기로 전환된다.
- 접촉 시트 위 행은 첫 주기, 아래 행은 두 번째 주기다. 두 행 모두 `서기 → 뒤로 낙하 → 등을 바닥에 붙인 자세`가 동일하게 나타난다.
- 반복 캡처 시트: `automated_visual_capture/review_loop/Grave_Death_ReviewLoop_ContactSheet.png`
- 반복 캡처 매니페스트: `automated_visual_capture/review_loop/Grave_Death_ReviewLoop_CaptureManifest.txt`
- 자동 캡처 뒤 Unity 선택과 Scene View 포커스는 남기지 않았다.

## 보존 및 실행하지 않은 항목

- 사망 컨트롤러는 씬에서 `Grave_05_Death/Grave_Model` Animator 한 곳에만 참조된다.
- 사망 컨트롤러는 사망 작업 클립 한 곳만 참조한다.
- 사망 작업 클립 SHA-256: `ADB7BE792AC3263E3171861B8BCB84D6DA0FB149F081E199CC58EDAC8F922BF0`
- 사망 작업 컨트롤러 SHA-256: `DA3D0E39E46D03E6ACEB4490CF46E47501C4680D7A39C368A7EE97915AF596A8`
- 공격 작업 클립 SHA-256: `B056F97932E4CC017E8C713C4E5FF63AD915A77EC20BA67DD43A63E93BCC15E3`
- 본 공격 클립 SHA-256: `2D2F03B7ACA5728E4931BA8E3B9047B9CC000AB254B2FF57EEC011636EF45925`
- 기존 공격·피격·걷기·대기 모션, 모델, 메시, 머티리얼, 텍스처, 전투 로직은 수정하지 않았다.
- Unity 재시작, 하네스 검증, 범위 밖 검증·테스트·빌드, Git 작업은 실행하지 않았다.
