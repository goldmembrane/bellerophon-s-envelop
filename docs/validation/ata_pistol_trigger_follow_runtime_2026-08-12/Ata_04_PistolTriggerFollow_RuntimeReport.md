# Ata_04 권총 방아쇠 접촉 및 오른팔 추종 실제 Play Mode 확인

## 결과

- 사용자 제공 영상 `2026-08-12 11-20-33.mp4`를 직접 프레임 단위로 확인했으며, 기존 권총이 오른손보다 화면 오른쪽 아래에 크게 떨어진 채 움직이는 문제를 재현했습니다.
- 원인은 `RightHand`에서 모델 상향축으로 `-0.80`만큼 이동시킨 고정 앵커였습니다.
- 고정 오프셋을 제거하고 기존 스킨 메시에서 `RightHand` 가중치가 `0.45` 이상인 보이는 정점들의 중심을 방아쇠 접촉 앵커로 사용했습니다.
- 앵커는 `RightHand`의 자식이므로 오른팔·오른손의 위치와 회전을 그대로 상속합니다.
- 허리 복귀 구간은 정규화 시간 `0.995–1.0`을 유지했습니다.

## 직접 시각 확인

- 실제 Unity Play Mode에서 `Ata_04_PistolAimAndFire`의 실제 Animator를 두 루프 연속 재생해 MP4로 캡처했습니다.
- 최종 영상은 `4.099262초`, `720×720`, `55`프레임이며 두 루프 `2.012364`까지 포함합니다.
- 손에 올라간 구간의 35개 연속 표본에서 권총 방아쇠 피벗과 오른손 앵커의 위치·회전 오차가 모두 `0`이었습니다.
- 원본 확대 프레임에서 권총 뒤쪽 잡는 부분이 오른손 손가락 안에 겹치고, 팔과 손목 자세가 바뀔 때 권총의 위치와 각도가 함께 바뀌는 것을 직접 확인했습니다.
- 정규화 시간 `0.994763`까지 권총이 손에 유지되고, 다음 루프 `1.030305`에서 오른쪽 허리 위치로 복귀한 것을 영상과 측정값으로 확인했습니다.
- 첫 루프와 두 번째 루프에서 동일한 접촉·추종·복귀가 반복되는 것을 확인했습니다.

## 산출물

- `actual_playmode_motion/Ata_04_PistolTriggerFollow_TwoLoops.mp4`
- `actual_playmode_motion/Ata_04_PistolTriggerFollow_Frames.csv`
- `actual_playmode_motion/Ata_04_PistolTriggerFollow_FinalContactSheet.png`
- `final_inspection/held_1.png`
- `final_inspection/held_2.png`
- `final_inspection/held_3.png`
- `final_inspection/edge_before.png`
- `final_inspection/edge_after.png`
- `final_inspection/held_loop2.png`
- `ApplyAtaPistolTriggerFollow.log`
- `CaptureActualPlayModeTwoLoops_Final.log`

## 실행하지 않은 항목

- 하네스 검증: 실행하지 않음
- `Run-HarnessValidation.ps1` 관련 실행·재실행·조사·로그·결과 산출물: 실행하지 않음
- EditMode/PlayMode 테스트 스크립트 및 Windows 빌드: 실행하지 않음
- 다른 아타 슬롯, 플레이어, 카메라, 게임플레이 시스템 수정: 실행하지 않음
- Unity 재시작, 외부 설치, Git 커밋·푸시: 실행하지 않음

## 사용자 지정 권총 손잡이 전체 메시 결합 정정

- 사용자 제공 이미지 `11111.png`를 확대하고 잔여 메시 구성요소 렌더와 직접 대조했습니다.
- 기존 `6`삼각형 파편 판정은 이미지의 전체 형태와 달라 무효화했습니다.
- 넓은 회색 금속 면과 위·아래 돌출면을 모두 포함하는 `28`삼각형 구성요소 전체를 지목 부품으로 확정해 바디에서 제거하고 권총 손잡이에 결합했습니다.
- 권총 분리 원본은 `307`삼각형이며 선형 아티팩트 제거 후 렌더 권총은 `273`삼각형입니다.
- 적용 후 바디 잔여 목록에서 `28`삼각형 구성요소가 사라졌고, 권총 기하 검사에서 전체 `273`삼각형이 단일 연결 성분으로 확인됐습니다.
- 실제 Play Mode 2회 반복 영상과 손 부분 확대 프레임에서 지목 부품이 허리에 중복 잔류하지 않고 권총과 같은 위치·각도로 오른팔을 따라 움직이는 것을 직접 확인했습니다.
- 오른손 끝 접촉과 루프 종료 시 오른쪽 허리춤 복귀 상태를 유지했습니다.
- 최종 영상은 `4.105840초`, `720×720`, `56`프레임, `2.01626`루프이며 손 유지 `37`개 표본의 최대 위치·회전 오차는 모두 `0`입니다.

### 실행하지 않은 항목

- 아트 샘플: 생성·실행하지 않음
- 하네스 검증: 실행하지 않음
- `Run-HarnessValidation.ps1` 관련 실행·재실행·조사·로그·문서·생성물 작업: 실행하지 않음
- EditMode/PlayMode 테스트 스크립트 및 Windows 빌드: 실행하지 않음
- 다른 Ata 슬롯, 플레이어, 카메라, 게임플레이 시스템 수정: 실행하지 않음
- Unity 재시작, 외부 설치, Git 커밋·푸시: 실행하지 않음

## 12:37:53 영상 기준 권총 손잡이 잔여 메시 조사 및 결합

- 제공 영상에서 오른쪽 허리의 회색 메시가 권총과 분리돼 다리 움직임에 따라 기울어지는 현상을 직접 확인했습니다.
- 바디 잔여 후보를 각각 4방향으로 격리한 결과, 붉은 천 후보와 달리 `6`삼각형·`10`정점 후보만 회색 금속 재질의 꺾인 손잡이 형상이었습니다. 이 성분의 `RightUpLeg` 가중치는 약 `97%`로, 바디에 남아 다리와 함께 기울던 원인이 확인됐습니다.
- 확인된 `6`삼각형만 기존 권총 본체에 결합했습니다. 최종 권총은 `251`삼각형 단일 강체 연결 성분이고, 바디 잔여 성분에서는 해당 조각이 제거됐습니다.
- 붉은 천 후보는 권총 손잡이가 아니므로 결합하지 않고 바디에 유지했습니다.
- 최종 실제 Play Mode 영상은 `4.043026초`, `720×720`, `57`프레임, `1.986048`루프입니다.
- 허리, 인출, 첫 회차 조준, 두 번째 회차 조준 프레임을 직접 확인해 손잡이가 권총과 같은 위치·각도로 이동하고 허리에 별도로 남지 않는 것을 확인했습니다.
- 손 유지 39개 표본의 권총-오른손 앵커 최대 위치 오차 및 최대 회전 오차는 모두 `0`이며, 애니메이션 종료 시 완성된 권총이 오른쪽 허리춤으로 복귀합니다.
- Play Mode 전환 과정의 기존 inactive Animator 경고 2건은 기록됐으나 영상과 측정 산출물은 정상 생성됐습니다.

### 최종 확인 산출물

- `source_123753_handle_investigation/source_four_phase_zoom.png`
- `source_123753_handle_investigation/residual_components/candidate_2_t6.png`
- `actual_playmode_motion/Ata_04_PistolTriggerFollow_TwoLoops.mp4`
- `actual_playmode_motion/Ata_04_PistolTriggerFollow_Frames.csv`
- `handle_attachment_final/final_waist_zoom.png`
- `handle_attachment_final/final_held_zoom.png`
- `handle_attachment_final/final_edge_before.png`
- `handle_attachment_final/final_edge_after.png`
- `handle_attachment_final/final_loop2_zoom.png`

### 실행하지 않은 항목

- 아트 샘플: 생성·실행하지 않음
- 하네스 검증: 실행하지 않음
- `Run-HarnessValidation.ps1` 관련 실행·재실행·조사·로그·문서·생성물 작업: 실행하지 않음
- `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`, `Build-WindowsDev.ps1`: 실행하지 않음
- 다른 Ata 슬롯, 플레이어, 카메라 및 게임플레이 시스템 수정: 실행하지 않음
- Unity 재시작, 외부 프로그램 설치, Git 커밋·푸시: 실행하지 않음

## 권총 전진값 0.24 및 오른손 끝 겹침 확인

- 손끝 방향 전진값을 기존 `0.12`에서 `0.24`로 두 배 늘렸습니다. 높이 `0.30`과 오른손 자식 앵커 구조는 유지해 권총이 오른팔의 위치와 회전을 그대로 추종합니다.
- 이전 배치와 변경 배치의 동일 조준 구간을 나란히 직접 확인한 결과, 권총이 손가락 진행 방향으로 눈에 띄게 더 이동했습니다.
- 첫 회차의 여러 팔 각도와 두 번째 회차를 고배율로 직접 확인했으며, 오른손 끝이 권총 뒤쪽 접촉부 안에 계속 겹치고 분리된 틈은 보이지 않았습니다.
- 최종 실제 Play Mode 2회 반복 영상은 `4.109144초`, `720×720`, `59`프레임이며 `2.019748`루프까지 기록됐습니다.
- 손에 유지되는 39개 표본의 권총-오른손 앵커 최대 위치 오차와 최대 회전 오차는 모두 `0`입니다.
- 루프 경계 직전까지 손에 유지되고 애니메이션 종료 시 오른쪽 허리춤으로 복귀하는 것을 최종 프레임에서 직접 확인했습니다.
- 렌더 권총은 본체 `245`개 삼각형이며 선형 잔여 오브젝트 제거 상태를 유지합니다.
- Play Mode 전환 과정의 기존 inactive Animator 경고 2건은 기록됐으나 영상과 측정 산출물은 정상 생성됐습니다.

### 최종 확인 산출물

- `actual_playmode_motion/Ata_04_PistolTriggerFollow_TwoLoops.mp4`
- `actual_playmode_motion/Ata_04_PistolTriggerFollow_Frames.csv`
- `forward024_final/before012_vs_forward024_hand_zoom.png`
- `forward024_final/final_held_hand_zoom.png`
- `forward024_final/final_edge_before.png`
- `forward024_final/final_edge_after.png`
- `forward024_final/final_loop2_hand_zoom.png`

### 실행하지 않은 항목

- 아트 샘플: 생성·실행하지 않음
- 하네스 검증: 실행하지 않음
- `Run-HarnessValidation.ps1` 관련 실행·재실행·조사·로그·문서·생성물 작업: 실행하지 않음
- `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`, `Build-WindowsDev.ps1`: 실행하지 않음
- 다른 Ata 슬롯, 플레이어, 카메라 및 게임플레이 시스템 수정: 실행하지 않음
- Unity 재시작, 외부 프로그램 설치, Git 커밋·푸시: 실행하지 않음

## 권총 위·앞 위치 추가 조정

- 기존 오른손 끝 접촉 기준은 유지하면서 권총 접촉 높이를 모델 위쪽 기준 `0.18`에서 `0.30`으로 올렸습니다.
- 손바닥 중심에서 오른손 끝 중심으로 향하는 손가락 방향을 계산하고, 그 방향으로 `0.12`만큼 추가 이동하도록 했습니다. 월드 고정축이 아니라 오른손 자식 앵커에 저장되므로 팔의 위치와 각도 변화에 그대로 따라갑니다.
- 이전 최종 프레임과 새 진단 프레임을 나란히 직접 확인해 권총이 화면상 위쪽과 손끝 방향으로 분명히 이동하면서 오른손 끝과 계속 겹치는 것을 확인했습니다.
- 최종 실제 Play Mode 2회 반복 영상은 `4.053726초`, `720×720`, `59`프레임이며 `1.992510`루프까지 기록됐습니다.
- 손에 유지되는 41개 측정 표본에서 권총과 오른손 앵커의 최대 위치 오차 및 최대 회전 오차가 모두 `0`이었습니다.
- 최종 조준 유지 프레임과 2회차 프레임을 직접 확인해 동일한 손끝 접촉이 유지되는 것을 확인했고, 루프 경계 직전·직후 프레임에서는 애니메이션 종료 시 권총이 오른쪽 허리춤으로 복귀하는 것을 확인했습니다.
- 권총 본체 `245`개 삼각형과 선형 오브젝트 제거 상태, 다른 Ata 슬롯 및 다른 씬 루트 불변 상태를 유지했습니다.
- Play Mode 전환 과정의 기존 inactive Animator 경고 2건은 기록됐으나 최종 영상과 측정 산출물은 정상 생성됐습니다.

### 최종 확인 산출물

- `actual_playmode_motion/Ata_04_PistolTriggerFollow_TwoLoops.mp4`
- `actual_playmode_motion/Ata_04_PistolTriggerFollow_Frames.csv`
- `lift030_forward012_final/before_vs_lift030_forward012.png`
- `lift030_forward012_final/final_held_crop.png`
- `lift030_forward012_final/final_edge_before.png`
- `lift030_forward012_final/final_edge_after.png`
- `lift030_forward012_final/final_loop2_crop.png`

### 실행하지 않은 항목

- 아트 샘플: 생성·실행하지 않음
- 하네스 검증: 실행하지 않음
- `Run-HarnessValidation.ps1` 관련 실행·재실행·조사·로그·문서·생성물 작업: 실행하지 않음
- `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`, `Build-WindowsDev.ps1`: 실행하지 않음
- 다른 Ata 슬롯, 플레이어, 카메라, 게임플레이 시스템 수정: 실행하지 않음
- Unity 재시작, 외부 설치, Git 커밋·푸시: 실행하지 않음

## 12:07:35 영상 기준 오른손 끝 접촉 조정

- 제공 영상 `2026-08-12 12-07-35.mp4`는 `4.391033초`, `1920×1000`, `149`프레임이며, 권총이 손바닥·손목 쪽에 깊게 겹쳐 오른손 끝과 접촉하지 않는 문제를 직접 확인했습니다.
- 기존 모델 상향 오프셋 `0.18`은 유지했습니다.
- 화면 방향을 임의의 월드축으로 사용하지 않고, 기존 스킨 메시에서 `RightHand` 영향이 `0.45` 이상인 정점 중 손목에서 가장 먼 상위 `25%`의 중심을 오른손 끝 접촉 기준으로 사용했습니다.
- 이 손끝 기준은 `RightHand` 자식 앵커에 저장되므로 팔·손목이 회전해도 권총 위치와 회전이 함께 추종합니다.
- 기존 손바닥 중심 결과와 좌우 비교해 권총이 손가락이 뻗은 방향으로 이동하고, 손끝이 권총 뒤쪽 손잡이·방아쇠 부위에 겹치는 것을 직접 확인했습니다.
- 서로 다른 첫 루프 세 시점과 두 번째 루프에서도 손끝 접촉이 유지되고 권총이 허공에 분리되지 않는 것을 확대 프레임으로 확인했습니다.
- 최종 실제 Play Mode 영상은 `4.089262초`, `720×720`, `59`프레임이며 `2.009977`루프를 포함합니다.
- 손 유지 구간 19개 표본에서 권총과 손끝 앵커의 최대 위치·회전 오차는 모두 `0`입니다.
- 루프 경계에서 권총이 오른쪽 허리로 복귀한 뒤 다음 루프에서 다시 인출되는 것을 직접 확인했습니다.
- 선형 메시 제거, 권총 본체 `245`개 삼각형, 다른 Ata 슬롯 및 다른 씬 루트 불변 상태를 유지했습니다.
- Play Mode 전환 과정의 기존 inactive Animator 경고 2건은 기록됐으나 캡처 및 측정 산출물은 정상 생성됐습니다.

### 최종 확인 산출물

- `actual_playmode_motion/Ata_04_PistolTriggerFollow_TwoLoops.mp4`
- `actual_playmode_motion/Ata_04_PistolTriggerFollow_Frames.csv`
- `fingertip_contact_final/before_palm_vs_after_fingertip.png`
- `fingertip_contact_final/final_held_crop.png`
- `fingertip_contact_final/final_edge_before.png`
- `fingertip_contact_final/final_edge_after.png`
- `fingertip_contact_final/final_loop2_crop.png`

### 실행하지 않은 항목

- 아트 샘플: 생성·실행하지 않음
- 하네스 검증: 실행하지 않음
- `Run-HarnessValidation.ps1` 관련 실행·재실행·조사·로그·문서·생성물 작업: 실행하지 않음
- EditMode/PlayMode 테스트 스크립트 및 Windows 빌드: 실행하지 않음
- 다른 Ata 슬롯, 플레이어, 카메라, 게임플레이 시스템 수정: 실행하지 않음
- Unity 재시작, 외부 설치, Git 커밋·푸시: 실행하지 않음

## 권총 하단부 접촉 위치 재조정

- 사용자가 기존 `0.06` 상향 조정은 화면에서 변화가 보이지 않는다고 판정했으므로 이전 높이 완료 판정은 무효 처리했습니다.
- 오른손 접촉 앵커의 모델 상향 오프셋을 `0.06`에서 `0.18`로 3배 올렸습니다.
- 실제 Play Mode 확대 화면에서 권총 전체가 이전보다 눈에 띄게 올라가고, 오른손이 권총 몸체 중앙이 아닌 아래쪽 손잡이·하단부와 겹치는 것을 확인했습니다.
- 서로 다른 팔 각도의 첫 루프 세 시점과 두 번째 루프에서 같은 하단부 접촉 관계가 유지되는 것을 직접 확인했습니다.
- 최종 Play Mode 영상은 `4.045028초`, `720×720`, `58`프레임이며 `1.987643`루프까지 포함합니다.
- 손 유지 구간 19개 표본에서 권총과 오른손 앵커의 최대 위치 오차와 회전 오차는 모두 `0`이었습니다.
- 첫 루프 종료 직전까지 권총이 손에 유지되고, 다음 루프 시작 프레임에서 오른쪽 허리로 돌아가 다시 인출되는 것을 영상과 측정값으로 확인했습니다.
- 기존 선형 메시 제거 상태와 권총 본체 `245`개 삼각형 구성은 유지했습니다.
- Play Mode 진입 시 기존 inactive Animator 경고 2건이 기록됐으나 캡처와 측정 산출물은 정상 생성됐습니다.

### 최종 확인 산출물

- `actual_playmode_motion/Ata_04_PistolTriggerFollow_TwoLoops.mp4`
- `actual_playmode_motion/Ata_04_PistolTriggerFollow_Frames.csv`
- `lift_and_line_cleanup_final/contact_lift_018_final_held_crop.png`
- `lift_and_line_cleanup_final/contact_lift_018_final_edge_before.png`
- `lift_and_line_cleanup_final/contact_lift_018_final_edge_after.png`
- `lift_and_line_cleanup_final/contact_lift_018_final_loop2_crop.png`

### 실행하지 않은 항목

- 아트 샘플: 생성·실행하지 않음
- 하네스 검증: 실행하지 않음
- `Run-HarnessValidation.ps1` 관련 실행·재실행·조사·로그·문서·생성물 작업: 실행하지 않음
- EditMode/PlayMode 테스트 스크립트 및 Windows 빌드: 실행하지 않음
- 다른 Ata 슬롯, 플레이어, 카메라, 게임플레이 시스템 수정: 실행하지 않음
- Unity 재시작, 외부 설치, Git 커밋·푸시: 실행하지 않음

## 11:40:56 영상 기준 권총 높이 및 선형 메시 제거

- 제공 영상 `2026-08-12 11-40-56.mp4`를 직접 확인해 권총 접촉부가 손에서 조금 낮고, 권총과 함께 움직이는 가느다란 선형 형상이 남은 상태를 확인했습니다.
- 오른손 접촉 앵커를 모델 위쪽으로 `0.06` 올렸습니다.
- 원본 권총 영역 `279`개 삼각형은 몸체 메시에서 그대로 제외하고, 슬리버 제거 뒤 공유 변 기준 최대 연결 성분만 권총 본체로 렌더링하도록 변경했습니다.
- 최종 권총 렌더 메시는 `245`개 삼각형이며, 분리된 `15 / 8 / 2`개 삼각형의 선형 성분은 렌더 대상에서 제거했습니다.
- 실제 Unity Play Mode에서 두 루프를 연속 재생한 최종 영상은 `4.127829초`, `720×720`, `40`프레임이고 `2.012317`루프를 포함합니다.
- 손에 머무는 구간의 권총-손 앵커 최대 거리는 `0`이었습니다.
- 최종 영상의 손 접촉부를 확대해 첫 루프와 둘째 루프 모두 권총이 올라간 위치에서 오른손과 함께 움직이고, 이전에 손 아래로 이어지던 점선형·삼각형 잔여 형상이 사라진 것을 직접 확인했습니다.
- 루프 경계 프레임에서 권총이 오른쪽 허리 위치로 돌아간 것도 직접 확인했습니다.
- 적용 로그에서 다른 Ata 슬롯과 다른 씬 루트가 변경되지 않았고 씬 저장이 완료된 것을 확인했습니다.
- Play Mode 진입 과정에서 기존 `Game object with animator is inactive`, `Can't call Animator.Update on inactive object` 경고가 기록됐으나 캡처는 완료됐고 최종 영상과 프레임 측정값이 생성됐습니다.

### 최종 산출물

- `actual_playmode_motion/Ata_04_PistolTriggerFollow_TwoLoops.mp4`
- `actual_playmode_motion/Ata_04_PistolTriggerFollow_Frames.csv`
- `lift_and_line_cleanup_final/held_clean_1.png`
- `lift_and_line_cleanup_final/held_clean_1_hand_crop.png`
- `lift_and_line_cleanup_final/held_clean_2.png`
- `lift_and_line_cleanup_final/loop_edge_clean.png`
- `lift_and_line_cleanup_final/held_clean_loop2.png`

### 실행하지 않은 항목

- 아트 샘플: 생성·실행하지 않음
- 하네스 검증: 실행하지 않음
- `Run-HarnessValidation.ps1` 관련 실행·재실행·조사·로그·문서·생성물 작업: 실행하지 않음
- EditMode/PlayMode 테스트 스크립트 및 Windows 빌드: 실행하지 않음
- 다른 Ata 슬롯, 플레이어, 카메라, 게임플레이 시스템 수정: 실행하지 않음
- Unity 재시작, 외부 설치, Git 커밋·푸시: 실행하지 않음
