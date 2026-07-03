# 파르붐 기준 이미지 재반영 단일 메시 샘플

이 샘플은 기준 이미지의 파르붐처럼 높은 초록 점액 몸체와 몸체 앞면에서 자연스럽게 솟아난 회녹색 주둥이, 열린 입, 치아, 혀가 한 덩어리로 보이도록 다시 만든 검토용 적대 개체 샘플입니다.

## 이번 수정 기준

- 보이는 오브젝트는 `Unified_Parvum_Reference_Matched_Single_Mesh` 하나입니다.
- 원통형 주둥이 오브젝트를 앞에 붙이지 않고, 몸체 표면 자체를 앞으로 밀어 주둥이 형상을 만들었습니다.
- 주둥이와 몸체 사이의 물리적 경계선, 내부 물방울, 검은 두 줄 오브젝트는 만들지 않았습니다.
- 초록 점액 몸체에는 알베도, 거칠기, 범프, 흰 박락 마스크 텍스처를 실제 재질 노드에 연결했습니다.
- 회녹색 주둥이는 별도 오브젝트가 아니라 같은 몸통 메시의 일부 face와 컬러 블렌딩으로 이어지며, 회녹색 비늘 알베도/범프 텍스처를 적용했습니다.
- 입, 치아, 혀도 단순 단색이 아니라 각 부위용 알베도 텍스처와 표면 특성을 적용했습니다.

## 사용 텍스처

- `parvum_slime_albedo.png`: 점액 몸통 알베도 / 적용 부위: 몸통 전체 / 표현: 짙은 초록 점액의 내부 마블링과 색 변화
- `parvum_slime_roughness.png`: 점액 몸통 거칠기 / 적용 부위: 몸통 전체 / 표현: 젖은 부분과 탁한 부분의 roughness 차이
- `parvum_slime_bump.png`: 점액 몸통 범프 / 적용 부위: 몸통 전체 / 표현: 흐르는 점액 주름과 미세 요철
- `parvum_white_fleck_mask.png`: 흰 박락 마스크 / 적용 부위: 몸통 표면 / 표현: 기준 이미지의 흰색 벗겨진 얼룩
- `parvum_muzzle_scale_albedo.png`: 회녹색 주둥이 알베도 / 적용 부위: 몸통 전면 주둥이 영역 / 표현: 파충류성 회녹색 비늘 색 변화
- `parvum_muzzle_scale_bump.png`: 회녹색 주둥이 범프 / 적용 부위: 몸통 전면 주둥이 영역 / 표현: 비늘과 모공 요철
- `parvum_mouth_cavity_albedo.png`: 입 안쪽 알베도 / 적용 부위: 입 내부 / 표현: 검은 선이 아닌 젖은 어두운 구강
- `parvum_tooth_albedo.png`: 치아 알베도 / 적용 부위: 치아 / 표현: 누런 치아 얼룩과 색 변화
- `parvum_tongue_albedo.png`: 혀 알베도 / 적용 부위: 혀 / 표현: 젖은 붉은 혀 색 변화

## 사용 머티리얼

- `M_Parvum_Wet_Marbled_Green_Slime_Texture`: 몸통 단일 메시 대부분 / 연결 내용: 점액 알베도, 거칠기, 범프, 흰 박락 마스크, 컬러 속성 블렌딩
- `M_Parvum_Embedded_Grey_Green_Muzzle_Texture`: 몸통 전면 주둥이 face 영역 / 연결 내용: 회녹색 알베도, 비늘 범프, 거칠기 응답
- `M_Parvum_Dark_Muzzle_Pores`: 콧구멍 / 연결 내용: 작은 어두운 모공
- `M_Parvum_Deep_Mouth_Cavity_No_Line_Objects`: 입 내부 / 연결 내용: 검은 두 줄 오브젝트 없는 어두운 젖은 구강
- `M_Parvum_Irregular_Embedded_Teeth`: 치아 / 연결 내용: 누런 알베도와 미세 거칠기
- `M_Parvum_Mouth_Tongue_Detail`: 혀 / 연결 내용: 젖은 붉은 표면

## Unity 적용 상태

아직 Unity 씬, 프리팹, 런타임 에셋에는 연결하지 않았습니다. 사용자 승인 전까지 이 샘플은 `artSample/` 검토 산출물입니다.
