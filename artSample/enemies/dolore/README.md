# 돌로레 원본 모델 재질 동기화 아트 샘플

## 상태

- 사용자가 승인한 `artSample`입니다.
- 승인 FBX의 UV와 3개 재질 슬롯을 Unity 프로젝트의 돌로레 에셋과 `CargoRunMvp` 씬의 7개 개체에 적용했습니다.
- 원본 `enemies model/dolore.fbx`는 수정하지 않았습니다.

## 기준과 제작 방향

- 기준 이미지는 `image/dolore(돌로레).png`, `image/dolore-attack.png`입니다.
- 현재 Unity 배치에 사용된 FBX의 `char1` 메시와 27본 리그를 그대로 사용했습니다.
- 원본 스킨 메시의 `2,223정점`, `4,139폴리곤`, 9개 연결 성분, 오브젝트 변환과 27본을 보존했습니다.
- 별도 장식, 액자, 표면 융기, 촉수 메시를 생성하지 않았습니다.
- 기준 이미지 동기화 범위는 UV, 재질 슬롯, 몸통·액자·초상 텍스처와 Smooth Shading뿐입니다.
- 원본과 활성 Blend의 정점 위치 및 폴리곤 토폴로지 SHA-256이 일치합니다.

## 재질과 텍스처

- `Dolore_Wet_Deep_Teal_Tissue`: 어두운 청록 생체 조직, 비취색 융기와 젖은 광택
- `Dolore_Oxidized_Brass_Frame`: 검은 황동 바탕, 녹청 산화와 마모 반응
- `Dolore_Faded_Portrait`: 기준 이미지에서 분리한 흐릿한 남성 초상

## 검토 파일

- 정적 기준 비교: `renders/06_reference_comparison_static.png`
- 기준 시점: `renders/01_reference_matched_three_quarter.png`
- 정면: `renders/02_front.png`
- 측면: `renders/03_side.png`
- 후면: `renders/04_back.png`
- 재질 확대: `renders/05_material_closeup.png`
- Blender 원본: `blender/Dolore_CurrentModel_ReferenceSync.blend`
- 리깅 보존 FBX: `exports/Dolore_CurrentModel_ReferenceSync.fbx`
- 정적 검토 GLB: `exports/Dolore_CurrentModel_ReferenceSync.glb`

## 내보내기와 Unity 적용 상태

- FBX는 원본 메시 1개와 27본 리그, 갱신된 재질을 포함합니다.
- GLB는 추가 오브젝트 없이 원본 메시 1개만 담은 정적 재질 검토용입니다.
- 공격 촉수는 원본 모델에 존재하지 않으므로 이번 색상 동기화 샘플에서 새로 만들지 않았습니다.
- Unity 적용본은 승인 FBX의 직접 인스턴스이며 형상·토폴로지·27본 리그와 기존 슬롯 배치를 보존합니다.
- Unity 렌더 정점은 UV와 재질 경계 분리로 5,173개이며, 원본 제어점 2,223개와 4,139폴리곤은 변경되지 않았습니다.

## 재생성 및 점검

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' --background --factory-startup --python 'artSample\enemies\dolore\tools\build_dolore_art_sample.py'
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' --background --factory-startup --python 'artSample\enemies\dolore\tools\inspect_dolore_art_sample.py'
```

- 최신 독립 점검 결과는 `SAMPLE_INSPECTION.txt`와 `SAMPLE_INSPECTION.json`에 있습니다.
- `Run-HarnessValidation.ps1`, EditMode/PlayMode 테스트, 빌드, Unity 재시작과 Git 작업은 실행하지 않았습니다.
