# 이슈판트 무장 아트 샘플 에셋 매니페스트

상태: `ART_SAMPLE_PENDING_USER_APPROVAL_2026-08-05`

이번 개정의 Unity 적용 상태: `NOT_APPLIED`

## 주요 결과물

| 파일 | 역할 |
|---|---|
| `Ispant_Armed_Appearance_FinalReview.png` | 허리띠 제거·스트랩 보존·눈 슬릿이 반영된 정면·사선·후면·초승달 확대 최종 검토 이미지 |
| `diagnostics/Ispant_Face_Diagnostic.png` | 좌우 두 눈 슬릿의 헬멧 표면 부착 확대 진단 |
| `diagnostics/Ispant_Face_Reference_SideBySide.png` | 사용자 제공 얼굴 기준과 수정된 검은 PBR 패턴의 병렬 비교 |
| `Ispant_Armed_Appearance_Sample.blend` | 리그·스킨·머티리얼·패킹 텍스처가 포함된 원본 |
| `Ispant_Armed_Appearance_Sample.glb` | 범용 3D 검토 파일 |
| `appearance_reference_sync/index.html` | Kursa 샘플 형식을 따른 전체 외형 비교·적용 기준·텍스처 페이지 |
| `appearance_reference_sync/summary.html` | 빠른 승인 판정용 비교·변경·보존 요약 페이지 |
| `Ispant_Reference_Comparison.html` | 기존 단일 비교 페이지(호환용 유지) |
| `scripts/build_ispant_art_sample.py` | 샘플과 검토 이미지를 재현하는 Blender 스크립트 |

## 구조 기록

- 원본 본체 메시: 2,284정점, 4,004폴리곤
- 원형 장식 제거: 48정점, 96폴리곤
- 사용자 지정 우측 보조 총기 제거: 연결 표면 `57`, `79`, `92`, 34정점, 56폴리곤
- 허리띠 제거: 연결 표면 `48`, `50`, `55`, `63`, `65`, `70`, `82`, `84`, `85`, `90`, `91`, `94`, `96`, `98`, `99`, 158정점, 256폴리곤
- 대각선 가슴 스트랩 보존: 연결 표면 `22`
- 샘플 본체 메시: 2,044정점, 3,596폴리곤
- 새 초승달: 148정점, 146폴리곤
- 새 좌우 눈 슬릿: 합계 32정점, 28폴리곤, `Head` 본 직접 자식, 바깥쪽 상승량 `0.014m`, 정면 기울기 약 `14.5도`
- 제거 집합 외 본체 정점 보존: `True` (재현 스크립트가 지정 연결 표면만 삭제)
- 본 이름과 부모 계층 일치: `True`
- 기계 장갑 연결 부품 43개의 전용 UV 범위 일치 (`0.04`–`0.96`): `True`
- 장갑·헬멧 노멀 강도 `0.66`, 코팅 가중치 `0.16` 일치: `True`
- GLB 본체의 UV 세트 2개 보존: `True`
- 초승달 `Head` 가중치 1.0: `True`
- 허리띠 제거 집합에 가슴 스트랩 `22` 미포함 보호 검사: `True`
- 얼굴 전용 재질 폴리곤: 112개
- 얼굴 전용 UV `IspantHelmetFaceUV` 및 원본 `uv` 보존: `True`
- 얼굴 패턴 좌우 대칭 생성: `True` (8비트 PNG 최대 차이 `1/255` 이내)

## 머티리얼 슬롯

1. `Ispant_Armor`
2. `Ispant_Helmet`
3. `Ispant_Helmet_Face`
4. `Ispant_Gunmetal`
5. `Ispant_Leather`
6. `Ispant_Wood`
7. `Ispant_Steel`
8. `Ispant_Copper`
9. `Ispant_Rubber_Black`

별도 눈 슬릿 메시에는 `Ispant_Eye_Cyan`을 사용합니다.

## 텍스처

`textures/`에는 아래 일곱 재질군별 Base Color, Roughness, Metallic, Normal
맵이 각각 하나씩 있으며 총 28개의 PNG가 있습니다.

- `armor_ivory`
- `helmet_face`
- `gunmetal`
- `leather_brown`
- `musket_wood`
- `steel_silver`
- `copper_accent`

`armor_ivory`에는 기준 이미지의 규칙적인 조립식 기계 장갑 인상을
재현하기 위해 동일 폭의 이중 절곡 림, 팔각형 음각 중앙판, 네 개의 링형
체결구와 상·하 점검 슬롯을 Base Color·Roughness·Metallic·Normal에 함께
반영했습니다. 흰 페인트, 어두운 노출 금속과 깊은 홈의 PBR 반응을 분리하고
표면 마모만 약하게 불규칙하게 구성했습니다.
`Ispant_Armor`와 `Ispant_Helmet`은 부품별 정규화 UV
`IspantMechanicalUV`를 사용합니다. 얼굴 전용 `Ispant_Helmet_Face`는
평면 UV `IspantHelmetFaceUV`로 중앙 콧등 접점에서 급상승하고 눈 중앙 위에서 완만해졌다가 외측에서 다시 급상승해 장갑 외곽 끝까지 연결되는 다단 눈 위 프레임, 검은 눈 소켓, 채워진 짧은 콧등판,
외측·내측 볼 프레임과 닫힌 턱판을 좌우대칭으로 고정하고 마모 진폭을 낮춥니다. 원본 `uv`와 얼굴 이외 재질군의 텍스처 내용은
변경하지 않았습니다.

## 승인 대상에서 제외

- `diagnostics/Ispant_CurrentModel_Clay.png`
- `diagnostics/Ispant_ComponentMap.png`
- `diagnostics/Ispant_Appearance_Diagnostic_01.png`
- `diagnostics/Ispant_Face_Diagnostic.png`
- `diagnostics/Ispant_WaistBoundary_Check.png`
- `diagnostics/Ispant_WaistBoundary_ExtendedCheck.png`
- 파일명에 `FAILED`가 포함된 구도 실패 이미지

Unity 씬, 프리팹, 런타임 자산과 원본 FBX는 이번 샘플 제작에서 변경하지
않았습니다. 하네스 검증, EditMode·PlayMode 테스트와 빌드도 실행하지
않았습니다.
