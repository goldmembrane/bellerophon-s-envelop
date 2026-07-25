# 오스티나토 Blender 3D 아트 샘플

## 상태

- 승인 상태: 사용자 검토 대기
- Unity 런타임 적용: 하지 않음
- 기준 모델: `Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx`
- 기준 이미지: `image/ostinato(오스티나토).png`, `image/ostinato-beside.png`, `image/ostinato-back.png`

## 검토 방법

1. `index.html`을 열어 정면·측면·후면 기준 이미지와 Blender 렌더를 비교합니다.
2. `blender/Ostinato_CurrentModel_TexturedSample.blend`에서 실제 UV, 재질 노드와 연결된 텍스처를 확인합니다.
3. 범용 검토는 `exports/Ostinato_CurrentModel_TexturedSample.glb`를 사용합니다.

## 제작 범위

- 현재 FBX 메시 형상과 24본 리그는 유지했습니다.
- 원본 `uv` 레이어는 보존하고 샘플 전용 `OstinatoSampleUV`를 추가했습니다.
- 갑각, 연부 조직, 가위날, 복안 4개 PBR 재질을 구성했습니다.
- 각 재질은 Base Color, Roughness, Metallic, Normal 텍스처를 실제로 연결합니다.
- 몸통 갑각은 UV 절단에 의해 판 경계가 깨지지 않도록 3D 좌표 기반 판상 홈과 이미지 기반 미세 키틴 표면을 결합했습니다.
- 넓은 적갈색 몸통 띠를 제거하고, 적갈색 연부 조직은 관절과 목 연결부에 집중했습니다.
- 복부 정면은 전신 무작위 홈을 차단하고 원본처럼 중앙 아래로 모이는 좌우 대칭 V자 겹갑각 4~5단으로 구성했습니다.
- 보이지 않는 부위는 후면 기준 이미지의 색상·재질 범위 안에서만 보완했으며 새 형상은 만들지 않았습니다.

## 재생성

Blender 5.1.2 기준:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' --background --python 'artSample/enemies/ostinato/tools/build_ostinato_blender_sample.py'
```

Auto-Rig Pro 사용자 확장은 Blender 종료 시 정리 오류를 출력할 수 있으나, 스크립트의 종료 코드와 생성 결과에는 영향을 주지 않았습니다.
