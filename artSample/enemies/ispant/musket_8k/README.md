# 이슈판트 머스켓 8K Unity 배치 텍스처 일치 샘플

## 결과

- `Ispant_Musket_8K_Textured.blend`: 원본 분할 메시, Unity 머스켓 UV 원본, 8K 최종 메시와 Unity 직접 원본 머티리얼 구성을 포함한 제작 파일
- `Ispant_Musket_8K_Textured.fbx`: 7,992정점·16,008면·UV 레이어 1개의 최종 머스켓
- `Ispant_Musket_8K_Review.png`: Unity 배치 전신의 실제 머스켓 텍스처와 최종 정면·사선을 한 장에 놓은 비교 렌더
- `Ispant_Musket_8K_UV_BeforeAfter.png`: 동일한 최종 8K 메시를 같은 방향·배율·카메라로 고정하고 이전 PCA UV와 현재 ICP 보정 UV를 위아래로 비교한 진단 렌더
- `Ispant_Musket_8K_Comparison.html`: 수치·출처·산출물을 한 화면에서 확인하는 승인용 요약 HTML
- `build_musket_8k.py`: 추출·감량·ICP UV 보정·Unity 머티리얼 구성·내보내기·재가져오기를 반복하는 제작 스크립트
- `render_uv_diagnostic.py`: 최종 FBX를 변경하지 않고 동일 메시 기반 UV 전·후 진단 구도만 재현하는 렌더 스크립트

## 원본과 식별

- 분할 원본: `C:/Users/gus68/Downloads/išpant-segment.glb`
- 분할 원본 SHA-256: `EAEB45D54E510A5CABFDAF9C36A26606A04518D8F812E1C1B8B5B84C645A0EF0`
- UV 원본: `D:/Bellerophon2/Bellerophon/enemies model/išpant-new.fbx`
- UV 원본 SHA-256: `7B682C49CDB2F4A563B736857E544DD7FCCD7213A5C375DEB92F56CE3A2E51B7`
- 분할 GLB의 `mesh_0`이 개머리판·방아쇠/격발부·총열 끝까지 이어진 머스켓 전체임을 분할 접촉 시트와 원본 FBX의 후면 무기 영역 대조로 확인했다.
- 검, 신체, 갑옷 조각은 머스켓 대상에서 제외했다.
- GLB의 `Color` 속성은 분할 표시색이며 실제 텍스처가 아니므로 사용하지 않았다.

## 정점 감량

- 원본: 95,624정점·191,272면·연결 성분 1개
- Decimate 비율: `0.0837`
- 결과: 7,992정점·16,008면·연결 성분 1개
- `0.0840`은 8,021정점으로 제한을 넘으므로, 시험한 값 중 8,000 이하에 가장 가까운 `0.0837`을 사용했다.
- 분할 원본의 형상만 감량했으며 검이나 다른 부위를 추가하거나 새 면으로 보완하지 않았다.

## UV와 텍스처

Unity 씬의 `Approved Ispant Enemy Placement`를 확인한 결과, 12개 배치 모두 `Ispant_New_Direct_Source.fbx`의 직접 인스턴스다. 인스턴스 오버라이드는 Transform과 이름뿐이고 렌더러 머티리얼 오버라이드, 추가·제거 컴포넌트는 없다. Unity 임포터는 FBX의 `texture_0`을 `Ispant_New_Direct_Source_BaseColor.png`에 명시적으로 연결한다.

사용자가 지정한 `išpant-new.fbx`와 Unity 직접 FBX의 SHA-256은 동일하다. 이 FBX에서 머스켓 전체에 해당하는 UV 섬 47개·541면을 분리했다. 두 모델의 정점 번호와 토폴로지가 달라 주성분 축과 경계 크기를 정규화한 뒤 15회 ICP 보정하고, Blender `Data Transfer / Nearest Face Interpolated` 방식으로 Unity 머스켓 표면의 UV를 전사했다. 새 UV를 임의로 펼치거나 색을 생성하지 않았다.

- 정합 축: `(1, 1, -1)` — 8개 축 부호 조합 중 양방향 평균 표면 오차가 가장 낮은 값
- 보정 전 평균/95백분위 표면 오차: `0.043044` / `0.131041`
- 보정 후 평균/95백분위 표면 오차: `0.035161` / `0.111005`
- UV 범위: `(0.011375, 0.001359)`–`(0.999921, 0.995176)`
- UV 심을 포함한 정점별 UV 좌표 합계: 8,062개
- Base Color SHA-256: `7DE6705FB7BD60E2D347023EFE51E96598845D611631F40D01C64EACC5249570`
- Metallic SHA-256: `674812FCDE6B2879D15E40BDCE0BDC1BB152C75D7B74AC3371B2C96BE478920D`
- Normal SHA-256: `11F5A8254E2FA46BF5F7EC49426F1BAD8F49CA254264EFE9FA15A73731E50C07`
- Roughness SHA-256: `45B468DCDC7E5624A0D74ED639F586759B682BB67DED3A0666C1104889689432`

네 PNG는 사용자가 제공한 파일을 가공 없이 보존했으며 위 해시가 원본과 일치한다. 다만 실제 Unity 배치 FBX의 머티리얼 구성과 일치시키기 위해 최종 머티리얼에는 Base Color 한 장만 연결했다. Metallic·Normal·Roughness PNG는 원본 보존용이며 최종 머티리얼에는 연결하지 않았다.

- 최종 머티리얼: `Ispant_Musket_8K_UnityDirect`
- 연결 이미지 노드: Base Color 1개
- Metallic: `1.0`
- Roughness: `1.0`
- Normal·Metallic·Roughness 이미지 연결: 0개

## 내보내기 확인

- 최종 FBX SHA-256: `EBF18A6F220CFCDCBD7CBF4B42F306C0BA5D552B53922FF5B2F9D7FF41F03F9A`
- FBX 재가져오기: 7,992정점·16,008면·UV 레이어 1개·연결 성분 1개·머티리얼 1개·이미지 노드 1개
- 재가져온 머티리얼도 Metallic `1.0`, Roughness `1.0`으로 일치한다.
- 최종 비교 렌더에서 Unity 배치 전신의 개머리판·격발부·총열 텍스처와 8K 결과를 같은 조명에서 확인했다.
- UV 진단 렌더는 개머리판·격발부·총열/총구 구간을 표시하고, 두 행에서 색·조명·형상·방향·배율·카메라를 고정해 UV 차이만 판별할 수 있게 구성했다.
- 제작 명령은 모든 내부 검사를 통과해 `ISPANT_MUSKET_8K_PASS`를 출력했다. 이후 Blender가 `0.000298 MB` 미해제 메모리 경고 때문에 종료 코드 1을 반환했으나 저장·FBX 재가져오기·렌더 결과에는 영향을 주지 않았다.

## 적용 상태

이 결과는 사용자 검토용 `artSample`이다. Unity 에셋·씬·프리팹에는 적용하지 않았고 리그와 애니메이션도 연결하지 않았다. `Run-HarnessValidation.ps1` 하네스 검증, EditMode·PlayMode 테스트, Windows 빌드, Git 작업은 실행하지 않았다.
