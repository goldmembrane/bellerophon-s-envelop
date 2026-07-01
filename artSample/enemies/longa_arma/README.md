# 롱가 아르마 모델링 샘플

- 생성 시각: 2026-07-02 00:08:44
- 상태: 사용자 승인 전 샘플
- 기준 이미지:
  - `image/longa arma(롱가 아르마).png`
  - `image/longa arma-beside.png`
  - `image/longa arma-back.png`

## 반영 내용

- 푸가 샘플처럼 기준 이미지와 생성 렌더를 정면/측면/후면으로 직접 비교할 수 있게 `index.html`을 구성했습니다.
- 표면 점액 표현용 별도 오브젝트, 액체 줄기, 방울, 웅덩이 오브젝트는 생성하지 않습니다.
- 몸체와 사지는 절차형 거친 녹색 피부 머티리얼, 강한 bump, coarse displacement로 매끈한 표면감을 줄였습니다.
- 몸체, 어깨, 엉덩이, 사지 반경을 키워 이전보다 덩치 큰 괴물 체형으로 조정했습니다.
- 목과 머리는 몸통과 하나의 연속 메쉬로 이어지게 구성했습니다.
- 로컬 기준 왼쪽 긴 팔은 몸통 안쪽에서 자라 나와, 낮은 반달형 칼날과 위로 솟은 칼끝으로 경화되는 구조로 조정했습니다.

## 검토 파일

- 정면 렌더: `renders/front.png`
- 측면 렌더: `renders/side.png`
- 후면 렌더: `renders/back.png`
- 기준 이미지/샘플 비교 렌더: `renders/reference_comparison.png`
- Blender 원본: `blender/longa_arma.blend`
- 내보내기: `exports/longa_arma.fbx`, `exports/longa_arma.glb`
