# Hands_Draw_Back 사용자 영상 직접 확인

- 원본 영상: `C:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-08-29 00-36-52.mp4`
- 길이: 6.422233초
- 해상도: 1920×1000
- 목적: 등 뒤 인출 이후 오른팔이 전방이 아니라 위쪽을 향하는 구간을 전체 영상 표본에서 직접 확인한다.
- 판정 우선순위: 1순위 영상 프레임 직접 확인, 2순위 현재 Controller·FBX 참조 상태 확인.
- 전체 접촉 시트 `video_contact_sheet.png`는 4fps로 26개 표본을 순서대로 배치했고, 확대 접촉 시트 `video_character_contact_sheet.png`는 2.5fps로 16개 표본을 배치했다.

## 직접 판정

- 확대 16개 표본에서 동일한 2.3초 동작이 반복됐다. 오른손은 머리 옆·등 뒤에서 빠져나온 뒤 명치 앞쪽으로 진행하지 않고, 오른팔꿈치와 손이 함께 머리 위 대각선 방향을 거쳐 거의 수직 위쪽까지 올라갔다.
- 두 번째와 세 번째 반복에서도 같은 위쪽 궤적이 재현되므로 일시적인 프레임 문제나 반복 경계의 한 번뿐인 현상이 아니다.
- 현재 `Hands_Draw_Back.controller`는 `Hands_Draw_Back_Mixamo.fbx`의 `mixamo.com` Take를 직접 참조한다. 따라서 영상의 위쪽 지향 동작은 Controller 연결 오류나 파생 클립 혼입이 아니라 지정 FBX 원본 Take의 실제 오른팔 궤적이다.
- 사용자 요구인 “등 뒤에서 꺼낸 뒤 오른팔을 앞으로 내미는 동작”으로 만들려면 원본 FBX는 보존하고, 원본 타이밍을 표본화한 파생 클립에서 오른팔 체인의 방향만 전방 목표로 보정한 뒤 Controller 연결을 해당 파생 클립으로 바꾸는 별도 구현이 필요하다.
