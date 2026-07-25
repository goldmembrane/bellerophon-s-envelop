# 리벨리온 앞면 사각형 애니메이션 아티팩트 제거

## 확인한 현상

사용자 제공 영상에서 `Rebellion_01_Move`가 재생될 때 원반 무장 앞면의 일부가
큰 사각 판처럼 위·아래로 뒤집혀 나오는 현상을 확인했습니다.

## 원인

앞면 수납부를 Boolean으로 절삭할 때 생성된 정점 중 30개가 몸통 본이 아니라
다리 본의 가중치를 상속받았습니다. 이동 모션에서 `Bone_014`, `Bone_009`
등이 회전하면 이 수납부 벽면 정점도 함께 움직여 사각 오브젝트처럼 보였습니다.

## 보정

- 다리 가중치를 가진 앞면 수납부 정점 30개만 `Bone_008`에 고정했습니다.
- 메시 정점이나 폴리곤은 삭제하지 않았습니다.
- 나머지 정점의 가중치, 29개 본 계층, 머티리얼, 텍스처와 배치 Transform은
  변경하지 않았습니다.

## 결과

- 보정 Blender 파일:
  `blender/Rebellion_FrontArtifactRemoved.blend`
- 보정 GLB:
  `exports/Rebellion_FrontArtifactRemoved.glb`
- 상세 보고서:
  `FRONT_ARTIFACT_REMOVAL.json`
- Unity 적용 GLB SHA-256:
  `712FE23B96B773204F2F1A56588F00B9CF5AEA81D6E9A60CA830FD3FEC89E24A`

Blender 왕복 검사와 Unity 임포트 메시 검사에서 앞면 수납부의 다리 본 영향
정점이 0개임을 확인했습니다. 실제 Animator 1초 루프의 10개 시점에서도
사각형이 더 이상 나타나지 않았습니다.
