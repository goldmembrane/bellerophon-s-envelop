# MarkerSpray 붉은 원뿔형 미스트 — Unity 적용용 아트 샘플

## 목표

- `MarkerSpray_Spray`의 분사구에서 시작하는 붉은 원뿔형 미스트를 정의한다.
- 분사 도달 거리는 약 `1m`다.
- 지속 입력 중 입자가 끊기지 않고 생성·확산·감쇠하는 연속 분사를 기준으로 한다.
- 중심부는 진한 적색, 외곽과 원거리 끝은 옅은 반투명 적색으로 감쇠한다.
- 완성된 원뿔 그림 한 장이 아니라 Unity Particle System이 반복 배치할 수 있는 독립 RGBA 텍스처로 구성한다.
- 원뿔 확산은 중심선 기준 상·하 `7°` 반각이며, 1m 끝점의 반높이는 약 `12.28cm`, 전체 높이는 약 `24.56cm`다.

## 구성

- `index.html`: 실제 시간 기반 연속 분사 미리보기, 1m 눈금, 정지/재개 조작, 정적 비교와 구현 기준
- `marker_spray_red_cone_mist_reference.png`: 어두운 배경에서 원뿔 실루엣과 밀도 분포를 확인하는 정적 승인 이미지
- `marker_spray_mist_particle.png`: 부드러운 중심 미스트용 Billboard 텍스처
- `marker_spray_droplet_particle.png`: 건조한 미세 입자용 Billboard 텍스처
- `marker_spray_red_cone_mist_texture.png`: 속도 방향으로 늘어나는 Stretched Billboard 흐름 텍스처
- `unity_particle_system_spec.json`: URP 17.3.0 호환 머티리얼·임포트·Particle System 수치 계약
- `Verify-UnityVfxTextures.ps1`: 실제 알파, 투명 여백, 가장자리 알파와 유효 픽셀을 검사하는 보조 도구
- `asset_manifest.json`: 생성 방식, 프롬프트, 승인 상태 및 수치 기준

## 승인 상태

이 폴더는 Unity 적용 가능한 원본 텍스처와 수치 계약을 갖춘 아트 샘플 단계다. Unity 씬, `MarkerSpray_Spray`, Particle System, VFX Graph, 런타임 입력에는 아직 반영하지 않았다. 사용자가 이 시안을 승인한 뒤 별도 작업으로 연결한다.

## 생성 및 검증 메모

- 정적 기준 이미지와 세 파티클 PNG는 Codex 내장 `image_gen`으로 생성했다.
- HTML의 동적 미리보기는 세 실제 PNG를 Canvas 빌보드로 반복 배치한다.
- 직접 시각 확인에서 분사 원점, 좌→우 진행 방향, 원뿔 확산, 중심/외곽 밀도차, 1m 눈금 및 연속 순환을 우선 판정한다.
- 텍스처 검사는 알파 최솟값·최댓값, 가장자리 알파, 투명 영역 비율과 유효 픽셀 경계를 확인한다.
- 수치와 파일 검사는 직접 확인 뒤의 보조 근거로만 사용한다.

## Unity 연결 시 불변 조건

- `MarkerSpray_Spray`의 실제 분사구를 발생 원점으로 삼는다.
- Unity Shape Cone의 Angle은 `7°`를 사용한다.
- 발생기는 오른손과 분사구를 따라가지만 이미 나온 입자는 `World` 공간에서 자연스럽게 이동·감쇠한다.
- 입력을 누르는 동안 세 계층을 계속 방출하고, 입력을 놓으면 새 방출만 멈춘 뒤 잔여 입자를 자연 소멸시킨다.
- URP `Universal Render Pipeline/Particles/Unlit`, Transparent Surface, Alpha Blend, `ZWrite Off`, 양면 표시를 기준으로 한다.
- 실제 구현 수치는 `unity_particle_system_spec.json`을 재현 대상으로 사용한다.
