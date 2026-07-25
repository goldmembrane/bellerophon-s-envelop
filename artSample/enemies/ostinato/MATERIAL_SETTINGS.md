# 오스티나토 Blender 머티리얼 설정

## 공통

- 셰이더: Blender Principled BSDF
- UV: 원본 `uv` 보존, 샘플 렌더와 내보내기에는 `OstinatoSampleUV` 사용
- 텍스처 크기: 재질별 `1024 x 1024`
- 채널: Base Color, Roughness, Metallic, Normal

## 재질 역할

| 재질 | 주요 역할 | Metallic | Roughness 범위 | Normal 강도 |
| --- | --- | ---: | ---: | ---: |
| `Ostinato_Chitin` | 올리브 갑각, 3D 판상 홈, 산화와 미세 패임 | 0 | 0.45~0.77 | 0.70 + 판상 Bump 0.28 |
| `Ostinato_SoftTissue` | 적갈 관절 조직과 섬유 주름 | 0 | 0.49~0.78 | 0.68 |
| `Ostinato_HookBlade` | 은회색 가위날과 산화 금속 | 0.20~0.86 | 0.20~0.76 | 0.38 |
| `Ostinato_CompoundEye` | 녹적색 복안 렌즈 | 0.04 | 0.16~0.26 | 0.45 |

## 갑각 합성 방식

- `Ostinato_Chitin_*` 이미지 맵은 미세 키틴 얼룩, 작은 적갈색 산화 반점, 거칠기와 표면 요철을 담당합니다.
- Blender의 Generated 3D 좌표를 사용하는 Noise/Voronoi 노드는 몸 전체에 연속되는 큰 갑각판과 좁은 홈을 담당합니다.
- 정면 복부 마스크는 전신 Voronoi 홈을 차단하고, 약 4~5단의 좌우 대칭 V자 분절과 겹침 Bump를 대신 적용합니다.
- 갑각은 비금속이며 Specular IOR Level `0.30`, Coat Weight `0.10`, Coat Roughness `0.46`으로 과도한 왁스 광택을 억제했습니다.

## Unity 적용 전제

이 문서는 샘플 설정만 설명합니다. 사용자 승인 전에는 Unity 씬, 프리팹 또는 현재 배치된 9개 오스티나토에 연결하지 않습니다. Unity 적용 시에는 승인된 Blender 렌더와 실제 Unity 렌더를 별도로 비교해야 합니다.
