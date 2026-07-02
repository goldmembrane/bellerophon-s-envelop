# 롱가 아르마 모델링 샘플

- 생성 시각: 2026-07-02 14:33:28
- 상태: 사용자 승인 전 샘플
- 기준 이미지:
  - `image/longa arma(롱가 아르마).png`
  - `image/longa arma-beside.png`
  - `image/longa arma-back.png`

## 반영 내용

- 푸가 샘플처럼 기준 이미지와 생성 렌더를 정면/측면/후면으로 직접 비교할 수 있게 `index.html`을 구성했습니다.
- 표면 주름과 흘러내림은 같은 메쉬 내부 지오메트리로만 넣고, 점액 줄기/방울/웅덩이용 별도 오브젝트는 생성하지 않습니다.
- 머리, 몸통, 다리, 발가락, 귀, 로컬 왼팔, 칼날은 모두 하나의 연속 필드에서 융합한 뒤 `Longa_Arma_Continuous_Single_Surface_Mesh` 하나의 오브젝트와 하나의 메쉬 데이터로 변환했습니다.
- 칼날은 별도 오브젝트나 별도 판 부품이 아니라 같은 연속 표면 안에서 로컬 왼팔 끝이 검게 경화되고 납작해져 반달형 칼날로 변형되는 형태로 구성했습니다.
- 몸체와 사지는 절차형 거친 녹색 피부 머티리얼과 bump로 매끈한 표면감을 줄였습니다.
- 몸체, 어깨, 엉덩이, 사지 반경을 조정해 기준 이미지의 말/사냥개형 괴물 체형에 더 가깝게 다시 잡았습니다.

## 검토 파일

- 정면 렌더: `renders/front.png`
- 측면 렌더: `renders/side.png`
- 후면 렌더: `renders/back.png`
- 기준 이미지/샘플 비교 렌더: `renders/reference_comparison.png`
- Blender 원본: `blender/longa_arma.blend`
- 내보내기: `exports/longa_arma.fbx`, `exports/longa_arma.glb`
