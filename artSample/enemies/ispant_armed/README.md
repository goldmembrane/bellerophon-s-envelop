# 이슈판트 무장 외형 아트 샘플

상태: `ART_SAMPLE_PENDING_USER_APPROVAL_2026-08-05`

이번 개정의 Unity 적용 상태: `NOT_APPLIED`

- 이번 개정은 아트 샘플 파일에만 존재하며 Unity 씬·프리팹·런타임 자산에는 연결하지 않았습니다.
- 대각선 가슴 스트랩은 유지하고 허리띠 본체·파우치·버클·측면 연결 고정부를 제거했습니다.
- 얼굴에는 사용자 제공 얼굴 기준의 연결된 검은 패턴을 전용 PBR 재질로 적용하고, 기존 좌우 눈 슬릿의 폭·두께·토폴로지는 유지한 채 각도만 조정했습니다.
- 하네스 검증, EditMode·PlayMode 테스트와 빌드는 실행하지 않았습니다.

## 목적

현재 Unity에 배치된 `Ispant_Armed.fbx`의 신체 모델링, 리그, 스킨, 어깨
머스켓과 장검 및 약 180cm 기준 배율을 유지하면서 기준 이미지의 색 배치,
텍스처, 머티리얼을 검토 가능한 샘플로 제시합니다. 사용자가 표시한 우측의
저해상도 보조 총기는 기존 승인 수정대로 제거된 상태를 유지합니다. 이번
개정에서는 허리띠만 추가로 제거하고 대각선 가슴 스트랩은 보존했으며,
기준 이미지에서 확인되는 눈 슬릿과 이마·눈 위·코·턱·측면 레일 패턴을
헬멧 전면에 재현했습니다.

기준 자료:

- `image/išpant(이슈판트)-armed.png`
- `docs/GAME_DESIGN_SOURCE.txt`의 이슈판트 정의
- `enemies model/išpant-armed.fbx`

## 적용한 외형

- 장갑판: 낡은 아이보리·백색 도장 금속의 연결 부품 43개마다 동일한
  이중 절곡 림, 팔각형 음각 중앙판, 네 개의 링형 체결구와 상·하 점검
  슬롯을 반복한 규칙적 기계 장갑
- 내부 관절: 검은 건메탈과 고무성 표면
- 얼굴 전면: 중앙 콧등 접점에서 급상승하고 눈 중앙 위에서 완만해졌다가 외측에서 다시 급상승해 장갑 외곽 끝까지 연결되는 다단 장갑 경계, 청록색 눈을
  감싸는 검은 건메탈 소켓, 채워진 짧은 콧등판, 외측·내측 볼 프레임과 닫힌 턱판을 사용자 제공 얼굴 기준에 맞춰 배치
- 대각선 가슴 스트랩: 갈색 가죽 유지
- 허리띠: 본체·전후면 파우치·버클·측면 고정부 제거
- 어깨 머스켓: 갈색 목재와 은회색 강철
- 장검: 긁힘이 있는 은회색 금속
- 소형 관절 장식: 구리색 금속
- 눈: 기존 32정점·28폴리곤과 폭·두께를 유지하고 바깥쪽 상승량만 `0.014m`, 정면 기울기 약 `14.5도`로 조정한 청록색 발광 슬릿
- 얼굴 UV: 전용 평면 UV `IspantHelmetFaceUV`로 사용자 지정 검은 패턴을
  좌우대칭으로 고정하고 마모 진폭을 낮춰 선의 식별성을 유지
- 텍스처: Base Color, Roughness, Metallic, Normal 맵으로 장갑의 규격 패널
  경계·체결구와 절제된 마모, 오염, 목재 결, 가죽 요철과 금속 긁힘을 구성
- UV: 장갑·헬멧만 부품별 정규화 UV `IspantMechanicalUV`를 사용하고,
  나머지 재질은 활성 렌더 레이어로 보존한 원본 `uv`를 사용
- 머티리얼: 장갑·헬멧의 노멀 강도 `0.66`, 도장 코팅 가중치 `0.16`으로
  절곡·체결부의 깊이와 금속 반사를 동일하게 구성

## 메시 변경 범위

원본 스킨드 메시에서 머리 위 원형 장식에 해당하는 독립 메시 섬 48정점만
제거했습니다. 같은 위치와 크기에 오른쪽으로 열린 초승달 메시를 생성하고,
전체 정점을 기존 `Head` 본에 가중치 `1.0`으로 연결했습니다.

사용자가 표시한 우측의 평행한 막대형 보조 총기는 원본 연결 표면
`57`, `79`, `92`에 해당하며, 목재 부품 2개와 강철 부품 1개를 합쳐
34정점·56폴리곤만 제거했습니다.

허리띠는 원본 연결 표면 `48`, `50`, `55`, `63`, `65`, `70`, `82`,
`84`, `85`, `90`, `91`, `94`, `96`, `98`, `99`에 해당하며
158정점·256폴리곤을 제거했습니다. 대각선 가슴 스트랩 연결 표면 `22`는
제거 집합에서 제외했습니다.

결과 본체는 2,044정점·3,596폴리곤입니다. 새 눈 슬릿은 합계
32정점·28폴리곤이며 기존 `Head` 본의 직접 자식입니다. 안구·렌즈·내부
발광 구조나 VFX는 만들지 않았습니다. GLB 본체에는 원본 UV와 기계 장갑용
UV 두 세트가 보존됩니다.

## 검토 파일

- `Ispant_Armed_Appearance_FinalReview.png`: 정면·사선·후면과 초승달 확대
- `diagnostics/Ispant_Face_Diagnostic.png`: 두 눈 슬릿의 헬멧 표면 부착 확대
- `diagnostics/Ispant_Face_Reference_SideBySide.png`: 사용자 제공 얼굴 기준과 현재 패턴 병렬 비교
- `appearance_reference_sync/index.html`: Kursa 샘플 형식을 따른 전체 외형 비교 페이지
- `appearance_reference_sync/summary.html`: 빠른 승인 판정용 요약 페이지
- `Ispant_Reference_Comparison.html`: 기존 단일 비교 페이지(호환용 유지)
- `Ispant_Armed_Appearance_Sample.blend`: 텍스처가 패킹된 Blender 원본
- `Ispant_Armed_Appearance_Sample.glb`: 범용 3D 검토 파일
- `ASSET_MANIFEST.md`: 구성 파일과 구조 검사 기록
- `textures/`: 기존 24개와 얼굴 전용 4개를 합친 PBR 텍스처 28개
- `scripts/build_ispant_art_sample.py`: 샘플 재현 스크립트

`diagnostics/`의 경계·구도·얼굴 이미지는 제작 과정 확인 자료이며 최종 승인
대상은 `Ispant_Armed_Appearance_FinalReview.png`와 HTML 비교 페이지입니다.

## 재현 명령

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' `
  --background --factory-startup `
  --python 'artSample\enemies\ispant_armed\scripts\build_ispant_art_sample.py' `
  -- --final
```

## 승인 후 Unity 적용 경계

이번 허리띠·눈 개정은 사용자가 샘플을 승인하고 Unity 적용 대상과 명령을
별도로 지정한 경우에만 반영합니다. 현재 Unity 배치, Transform, 씬·프리팹,
런타임 자산은 이번 작업에서 변경하지 않았습니다.
