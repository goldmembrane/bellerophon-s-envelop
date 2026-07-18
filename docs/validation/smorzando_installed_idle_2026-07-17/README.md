# Smorzando 설치형 대기 모션 적용 기록

## 사용자 확정 내용

- 바닥에 퍼진 촛농과 중앙 양초를 모두 하나의 몸체로 취급한다.
- 바닥 촛농은 물결처럼 움직인다.
- 촛농 전체가 호흡에 맞춰 조금 위아래로 들썩인다.
- 검토용 설치형 3개의 재생 타이밍은 조금씩 다르게 한다.
- 심지의 불꽃은 항상 켜져 있고 주변을 은은하게 밝힌다.
- 이번 불꽃 VFX는 사용자 예외 승인에 따라 별도 `artSample/` 없이 Unity에 바로 적용했다.

## 원본 구조 조사

- 원본 FBX는 수정하지 않았다.
- 설치형은 `Mesh1.0` 단일 Visible Mesh이며 정점 `23666개`, 삼각형 `12666개`, 서브메시 `1개`다.
- 인스턴스의 X축 `-90도` 보정 뒤 FBX 로컬 Z축이 실제 수직축이다.
- Mesh 로컬 크기는 `(0.018474, 0.019022, 0.005362)`이고 씬의 `100배` 보정을 거쳐 기존 설치형 크기가 유지된다.
- 최상단 1.5% 정점 중심 `(0.000352, 0.000022, 0.002454)`을 심지 후보 위치로 사용했다.
- 상세 조사 결과는 `Smorzando_InstalledIdleGeometry.txt`에 저장했다.

## 호흡·물결 구현

- 런타임 구성요소: `Assets/_Project/Runtime/Enemies/Smorzando/SmorzandoInstalledIdleMotion.cs`
- 원본 Mesh를 덮어쓰지 않고 재생 또는 자동 캡처 시에만 임시 Mesh 복사본을 만든다.
- 모든 정점을 같은 배열에서 변형해 바닥 촛농과 중앙 양초가 분리된 개체처럼 움직이지 않게 했다.
- 주기: `3.2초`
- 중앙 몸체 수평 호흡: 최대 `0.8%`
- 바닥 촛농 수평 호흡: 최대 `0.4%`
- 중심에서 외곽으로 흐르는 방사형 물결 높이: 최대 `6mm`
- 전체 몸체 상하 움직임: 최대 `8mm`
- 몸체 대기 모션 대상: `Smorzando_Installed_02` 한 개체
- 정적 몸체 대상: `Smorzando_Installed_01`, `Smorzando_Installed_03`
- 몸체 모션의 위상: 두 번째 슬롯의 기존 값 `1.0667초` 유지
- 씬에 저장된 원본 Mesh 참조와 설치형의 기준 Transform은 유지된다.

## 불꽃과 주변 조명

- 이 절의 최초 교차 평면 불꽃은 후속 하이브리드 불꽃 작업에서 교체됐다.
- 현재 최종 구조와 시각 자료는 `../smorzando_hybrid_flame_2026-07-17/README.md`를 기준으로 한다.

- 불꽃 프리팹: `Assets/_Project/Art/Enemies/Smorzando/VFX/Prefabs/Smorzando_Installed_Flame.prefab`
- 불꽃 마스크: `Assets/_Project/Art/Enemies/Smorzando/VFX/Textures/Smorzando_Flame_SoftTeardrop.png`
- 불꽃은 주황색 외곽 2면과 노란색 중심 2면이 교차하는 작은 투명 가산 VFX다.
- 불꽃 위치는 최상단 정점 중심보다 실제 거리 `12mm` 위에 놓고 몸체 상하 움직임을 따라가게 했다.
- 불꽃은 몸체 모핑과 분리된 `SmorzandoInstalledFlameMotion`으로 제어한다.
- 세 불꽃은 `0초`, `1.0667초`, `2.1333초`의 서로 다른 위상에 따라 기울기·폭·높이·밝기가 작게 달라진다.
- 첫 번째·세 번째는 몸체만 정지하며 불꽃의 공기 흐름 반응과 주변광은 계속 유지된다.
- Point Light 색은 따뜻한 주황색이며 범위 `2m`, 기본 세기 `0.45`, 밝기 흔들림 `12%`다.
- 새 Point Light는 그림자를 만들지 않는다.

## Unity 적용 결과

- 적용 명령: `ApplySmorzandoInstalledIdle`
- 대상: `Approved Smorzando Enemy Placement` 아래 설치형 3개
- 몸체 대기 모션 구성요소 수: `1` (`Smorzando_Installed_02`)
- 불꽃 모션 구성요소 수: `3`
- 불꽃 프리팹 인스턴스 수: `3`
- 기존 스모르찬도 및 Player Transform 사전·사후 비교 결과: 변경 없음
- 좀비형 5개에는 모션·불꽃·조명을 추가하지 않았다.
- 적용 보고서는 `Smorzando_InstalledIdleApply.txt`에 저장했다.

## 자동 시각 확인

- 캡처 명령: `CaptureSmorzandoInstalledIdleFrames`
- Play Mode와 Scene View 선택·포커스를 사용하지 않고 임시 Mesh 복사본을 시간별로 샘플링해 렌더했다.
- `3.2초`, `10fps`, `32프레임`, `640×640` 한 주기 영상을 생성했다.
- 두 번째 대기 개체의 한 주기 프레임에서 바닥 촛농 외곽·표면 높이와 중앙 몸체 높이가 작게 달라지는 것을 직접 확인했다.
- 동일 구도의 `Smorzando_InstalledIdle_BodyScope_T000.png`와 `Smorzando_InstalledIdle_BodyScope_T080.png`을 직접 비교했다.
- 첫 번째·세 번째 몸체는 두 시점에서 같은 자세를 유지하고 두 번째 몸체만 높이와 촛농 표면이 변한다.
- 세 개체의 불꽃은 두 시점 모두 심지에 붙어 켜져 있으며 서로 다른 공기 흐름 반응을 유지한다.
- 실제 Player Main Camera 화면에서 설치형 3개의 불꽃이 모두 켜지고 주변 촛농에 따뜻한 빛이 닿는 것을 확인했다.
- 불꽃은 모든 확인 프레임에서 심지 상단을 따라가며 이탈하지 않았다.
- 현재 몸체 범위 비교 시작 프레임: `automated_visual_capture/Smorzando_InstalledIdle_BodyScope_T000.png`
- 현재 몸체 범위 비교 변화 프레임: `automated_visual_capture/Smorzando_InstalledIdle_BodyScope_T080.png`
- 두 번째 대기 개체 한 주기 원본: `automated_visual_capture/cycle_frames/`
- 실제 시작 화면: `automated_visual_capture/Smorzando_InstalledIdle_PlayerView.png`
- 캡처 중 기존 장면의 다수 조명으로 그림자 아틀라스 해상도 축소 경고가 한 건 있었으나 새 불꽃 조명은 그림자를 사용하지 않으며 적용 오류는 없었다.
- 캡처 뒤 임시 Mesh와 개체를 제거하고 씬 dirty 상태를 복원했으며 Unity 선택을 해제했다.

## 실행하지 않은 항목

- 원본 FBX를 덮어쓰지 않았다.
- 설치형 기준 배치·회전·크기와 Player 시작점을 변경하지 않았다.
- 좀비형 애니메이션·재질과 다른 적대 개체를 변경하지 않았다.
- 변환, AI, 체력, 피격, 사망, 자폭, 전투 로직을 구현하지 않았다.
- Unity 재시작, 하네스 검증, 범위 밖 검증·테스트·빌드, Git 작업을 실행하지 않았다.
- 현재 상태는 사용자 시각 검토 대기이며 승인 완료로 처리하지 않는다.
