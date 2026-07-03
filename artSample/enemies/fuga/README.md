# 푸가 모델링 샘플

이 샘플은 `image/fuga2(푸가).png`, `image/fuga2-back.png`, `image/fuga2-beside.png`를 1차 재현 기준으로 제작한 적대 개체 아트 샘플입니다. `fuga(푸가).png` 계열은 형태를 뒤집지 않는 보조 참고로만 사용했습니다.

## 재현 목표

- 젖은 녹회색 피부와 거친 돌기를 가진 두꺼비형 중앙 머리.
- 좌우로 크게 펼쳐진 조류형 날개와 겹겹이 분리된 깃.
- 특정 각도에서 실루엣만 보이지 않도록 두께를 키운 단일 기반 날개 메쉬.
- 날개 안쪽의 어두운 올리브/갈색 깃층과 바깥쪽 녹회색 깃.
- 황금색 세로 동공 눈과 얇은 물결형 입.
- 하단에 매달린 소라 껍질 또는 잎 장식 부품.
- 공중에서 떠 있는 비행 씨앗체의 실루엣.

## Unity 반영 의도

- 승인 후 `CargoRunMvp` 복도 오브젝트 하단에 비행 적대 개체로 배치하는 것을 전제로 합니다.
- 런타임 루트 이동은 `Rigidbody + Collider` 기준으로 처리하고, Motion Path는 목표/경로 편집 기준으로 사용합니다.
- 정적 비교, 대기, 이동, 공격, 피격, 사망 상태를 분리해 확인할 수 있도록 Shape Key 이름을 포함했습니다.
- 사망 모션 검토 렌더는 공중 부유 시작, 몸체 기울어짐, 날개 힘 빠짐, 바닥 낙하, 충돌/정착, 최종 정지 포즈의 6단계로 구성했습니다.
- 샘플 단계에서는 Unity 런타임 씬, 프리팹, 에셋, AI, 피격 판정, UI 흐름에 연결하지 않았습니다.

## 산출물

- `blender/fuga_sample.blend`
- `exports/fuga_sample.fbx`
- `exports/fuga_sample.glb`
- `textures/`
- `renders/`
- `index.html`
- `TEXTURE_ANALYSIS.md`
- `PHYSICS_RIG_NOTES.md`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 승인 상태

`APPROVAL_STATUS.json` 기준 `requiresUserApprovalBeforeUnity=true`, `unityApplicationAllowed=false` 상태입니다. 사용자의 명시 승인 전에는 Unity에 적용하지 않습니다.
