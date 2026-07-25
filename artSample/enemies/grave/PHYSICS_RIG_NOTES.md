# 그라베 Unity 적용 및 리그 메모

- 이 샘플은 모델링·텍스처·머티리얼 검토용이며 애니메이션이나 물리 샘플이 아닙니다.
- 재현 모델은 원본 FBX의 24본 리그와 Armature 스키닝을 유지합니다.
- 검토용 GLB는 범용 확인을 위해 내보낸 파일이며, 일부 정점의 4개 초과 본 가중치는 glTF 규격에 따라 상위 4개로 정규화됩니다. Unity 적용 원본 후보는 Blend/FBX를 우선합니다.
- Unity 적용 단계에서는 `CargoRunMvp/Approved Grave Enemy Placement`의 7개 슬롯과 각 `Grave_Model` 하위 범위를 다시 묶음 승인받아야 합니다.
- 모델 교체 승인만으로 슬롯 위치, Player, 카메라, 다른 씬 루트, AnimationClip, Animator, AI, Collider, Rigidbody, 전투 로직을 변경하지 않습니다.
- 실제 루트 이동이 필요한 단계에서는 `Rigidbody + Collider`를 기준으로 하고, 경로·관절·IK·보조 물리의 역할을 같은 Transform에 중복시키지 않습니다.
- 이 아트 샘플 승인만으로 Unity 씬, 프리팹, 런타임 에셋을 변경하지 않습니다.
