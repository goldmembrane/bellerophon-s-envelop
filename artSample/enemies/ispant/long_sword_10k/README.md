# 이슈판트 장검 10K PBR 검토 샘플

## 결과

- `Ispant_LongSword_10K_Textured.blend`: 원본 세그먼트, UV 원본 장검, 최종 10K 장검을 포함한 제작 파일
- `Ispant_LongSword_10K_Textured.fbx`: 9,975정점·19,950면의 텍스처 적용 장검
- `Ispant_LongSword_10K_Review.png`: 원본 47,052정점 실루엣, 최종 정면, PBR 사선 비교
- `Ispant_LongSword_10K_Comparison.html`: 수치·텍스처·산출물을 한 페이지에서 확인하는 요약 HTML
- `build_long_sword_10k.py`: 추출·감량·UV 전사·재질 구성·내보내기·재가져오기를 반복하는 제작 스크립트
- `Ispant_LongSword_10K_UnityPlacement.blend`: 현재 장검 제거·다리 복구 본체와 승인 장검의 Unity 배치 구성
- `Ispant_LongSword_10K_UnityPlacement.fbx`: Unity에 실제 적용한 몸체·Armature·승인 장검 통합 FBX
- `Ispant_LongSword_10K_UnityPlacement_Review.png`: 기존 장검 위치와 승인 장검 왼쪽 허리 배치 비교
- `deploy_approved_long_sword.py`: 기존 장검 표면 정합, 배치 샘플 생성, 검증 및 Unity 적용 재현 스크립트

## 원본과 판별

- 분할 원본: `C:/Users/gus68/Downloads/išpant-segment.glb`
- 분할 원본 SHA-256: `EAEB45D54E510A5CABFDAF9C36A26606A04518D8F812E1C1B8B5B84C645A0EF0`
- UV 원본: `D:/Bellerophon2/Bellerophon/enemies model/išpant-new.fbx`
- UV 원본 SHA-256: `7B682C49CDB2F4A563B736857E544DD7FCCD7213A5C375DEB92F56CE3A2E51B7`
- 장검 대상은 분할 GLB의 `mesh_10`이며 검신·가드·손잡이·폼멜을 하나의 연결 요소로 포함한다.
- 분할 GLB에는 이미지 텍스처와 UV가 없고, `Color` 속성은 실제 재질이 아닌 단일 세그먼트 표시색이었다.

## 형상 감량

- 원본: 47,052정점·94,104면·연결 요소 1개
- Decimate 비율: `0.212`
- 결과: 9,975정점·19,950면·연결 요소 1개
- 검신·가드·손잡이·폼멜을 삭제하거나 새 형상으로 대체하지 않았다.
- GLB 루트의 원래 월드 변환을 메시 정점에 먼저 베이크해 원본 경계 크기 `(0.075550, 0.023307, 0.007621)`를 보존했다.

## UV와 텍스처

사용자가 UV 원본으로 지정한 `išpant-new.fbx`에서 이미 판별된 장검 전체 29개 UV 섬·242면을 사용했다. 두 장검은 정점 번호와 토폴로지가 다르므로, 각 형상을 주성분 축과 경계 크기로 정규화한 뒤 Blender `Data Transfer / Nearest Face Interpolated` 방식으로 원본 UV를 표면 전사했다. 색이나 무늬를 생성하지 않았으며 제공된 네 텍스처를 그대로 복사해 연결했다.

- UV 범위: `(0.007336, 0.027650)`–`(0.986122, 0.979067)`
- UV 전사 후 분할 정점: 0개
- Base Color SHA-256: `7DE6705FB7BD60E2D347023EFE51E96598845D611631F40D01C64EACC5249570`
- Metallic SHA-256: `674812FCDE6B2879D15E40BDCE0BDC1BB152C75D7B74AC3371B2C96BE478920D`
- Normal SHA-256: `11F5A8254E2FA46BF5F7EC49426F1BAD8F49CA254264EFE9FA15A73731E50C07`
- Roughness SHA-256: `45B468DCDC7E5624A0D74ED639F586759B682BB67DED3A0666C1104889689432`

Base Color는 현재 Unity 이슈판트 직접 원본 텍스처와 SHA-256이 동일하다. Metallic·Normal·Roughness도 사용자 제공 PNG를 가공 없이 복사했다.

## 독립 확인

- 최종 FBX SHA-256: `2A6ECA3E5CD74C2E40B03BA5B2D4FD51E2B1E73B7BA2431B7E9D4BE5C6579252`
- FBX 재가져오기: 9,975정점·19,950면·UV 레이어 1개
- 재가져온 형상도 연결 요소 1개이며 장검 전체 실루엣을 유지한다.
- Blender 제작 명령은 모든 검사를 통과해 `ISPANT_LONG_SWORD_10K_PASS`를 출력했다. 종료 시 Blender 내부의 149바이트 미해제 메모리 경고가 있었으나 산출물 생성·저장·재가져오기 결과에는 영향을 주지 않았다.

## 기존 왼쪽 허리 위치 배치

- 원본 Ispant에서 분리했던 기존 장검 29개 UV 섬·242면을 위치 참조로 사용했다.
- 승인 장검과 기존 장검의 전체 표면을 네 회전 후보와 30회 최근접 표면 정합으로 비교했다. 장검 길이 수축을 막기 위해 배율은 주축 길이 비율 `12.345491599084`로 고정하고 회전·이동만 보정했다.
- 최종 양방향 평균 표면 오차는 기존 장검 길이의 `0.023435707`이며, 배치 경계 중심은 기존 장검 경계 중심과 같다.
- 장검은 `Ispant_Approved_LongSword_10K` 별도 메시로 Armature 루트에 부모 연결했다. 버텍스 그룹과 Armature modifier는 없고 애니메이션도 연결하지 않았다.
- 통합 FBX 재가져오기 결과는 몸체 4,895정점·9,798면, 장검 9,975정점·19,950면, Armature 24본이다.
- 최종 재교체 FBX의 재가져오기 최대 좌표 오차는 몸체 `0.000000238`, 장검 `0.000000179`이며 토폴로지·면 방향·UV·몸체 웨이트·머티리얼 구조가 내보내기 전과 일치한다.
- 배치 검토 이미지에는 렌더용 원본 객체가 한 번 더 표시돼 원본이 왼쪽·중앙에 중복됐다. 비교 HTML은 가운데 기존 원본과 오른쪽 승인 장검 결과 구간만 잘라 표시하며, 모델·FBX 배치에는 이 렌더 구성 오류가 포함되지 않는다.

## 적용 상태

사용자 승인에 따라 현재 Unity FBX의 기존 장검 객체를 제거하고 `Ispant_LongSword_10K_Textured.fbx`에서 승인 장검을 새로 임포트해 `Ispant_LongSword_10K_UnityPlacement.fbx`와 Unity 직접 원본 `Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx`에 적용했다. 두 FBX의 SHA-256은 `5CE54F6117AF08F141BC18A0E46C823AD07877D815DA2906D59CA2967A4974FF`로 같다.

Unity가 FBX 내부 PBR 머티리얼을 텍스처 없이 표시하던 상태를 교정하기 위해 승인 PNG 4종을 `Models/Textures/Ispant_LongSword_10K/`에 해시 그대로 복사했다. `Ispant_LongSword_10K_PBR.mat`은 장검 전용 `Bellerophon/Ispant/LongSwordApprovedPBR` 셰이더로 Base Color·Metallic·Normal·Roughness를 각각 직접 참조하며, FBX 내부 머티리얼 `Ispant_LongSword_10K_PBR`에 외부 재매핑됐다. Base Color는 sRGB, Metallic·Roughness는 선형, Normal은 노멀맵이고 Normal Strength는 승인 Blender 머티리얼과 같은 `1`이다.

기존 공용 Ispant 셰이더 연결은 PBR 맵이 있어도 현재 Unity 씬에 중립 반사 환경이 없어 장검을 거의 검게 표시했다. 승인 Blender 검토본의 중립 스튜디오 조명 반응을 장검 금속 영역에만 보완하는 전용 셰이더로 분리했으며, 메시·UV·4개 텍스처의 픽셀·채널은 변경하지 않았다. 사용자 제보 이미지, 승인 배치 리뷰, Unity 특정 창 직접 캡처를 나란히 직접 확인해 검은 검신이 은회색 금속과 어두운 장식 문양으로 복구된 것을 판정했다. 육안 비교 자료는 `docs/validation/ispant_long_sword_visual_sync_2026-08-18/`에 있다.

기존 GUID를 참조하는 `CargoRunMvp.unity`의 Ispant 배치 12개와 씬 SHA-256은 유지됐다. 애니메이션은 연결하지 않았다. 하네스 검증, EditMode·PlayMode 테스트, Windows 빌드, Git 작업은 실행하지 않았다.
