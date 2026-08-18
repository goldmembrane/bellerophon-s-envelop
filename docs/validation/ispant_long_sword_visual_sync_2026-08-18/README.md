# 이슈판트 승인 장검 Unity 시각 동기화

상태: `PASS — 직접 육안 대조`

## 판정 자료

- `Current_Unity_Before.png`: 전용 장검 셰이더 적용 전 Unity 특정 창 캡처. 장검이 거의 검게 표시된다.
- `Current_Unity_Final.png`: 장검 전용 PBR 셰이더 적용 후 동일 Unity 창 캡처. 은회색 금속 면과 어두운 장식 문양이 구분된다.
- `Approved_Before_Current_Visual_Comparison.png`: 승인 배치 리뷰, 사용자 제보 이미지, 최종 Unity 직접 캡처의 3열 육안 비교.

## 직접 확인 결과

- 승인 기준 `artSample/enemies/ispant/long_sword_10k/Ispant_LongSword_10K_Review.png`와 `Ispant_LongSword_10K_UnityPlacement_Review.png`를 실제 이미지로 열어 확인했다.
- 사용자 제보 이미지의 거의 검은 검신은 승인본과 불일치했다.
- 최종 Unity 캡처에서는 승인본과 같은 은회색 계열 금속 반응, 밝은 검신 면, 어두운 장식 문양을 눈으로 확인했다.
- 승인 메시·UV·Base Color·Metallic·Normal·Roughness는 교체하거나 생성하지 않았다. 텍스처 픽셀과 채널도 변경하지 않았다.
- Unity 창은 다른 앱의 내용이 섞이지 않도록 특정 Unity HWND만 대상으로 캡처했다.

## 실행하지 않은 항목

- `Run-HarnessValidation.ps1` 하네스 검증 및 관련 실행·재실행·조사·로그·문서·생성물 작업
- `Run-EditModeTests.ps1`, `Run-PlayModeTests.ps1`, `Build-WindowsDev.ps1` 및 관련 작업
- 애니메이션 연결·수정, 씬 저장, 이슈판트 배치 밖 수정
- Git 작업
