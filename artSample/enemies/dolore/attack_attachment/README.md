# 돌로레 공격 촉수 부착 아트 샘플

## 상태

- 사용자 검토 대기 상태입니다.
- Unity와 돌로레 3번·4번 모션 개체에는 아직 적용하지 않았습니다.
- 애니메이션은 제작하지 않았습니다.

## 제작 기준

- 배치 기준: `image/dolore-attack.png`
- 촉수 단독 외형 기준: `image/dolore attack model.png`
- 공격 부품 원본: `enemies model/dolore attack.glb`
- 돌로레 기준 모델: 기존 승인 `artSample/enemies/dolore/blender/Dolore_CurrentModel_ReferenceSync.blend`

원본 기획서의 `초상 속 남성의 가슴이 열리고, 끝이 거대한 못인 촉수가 뻗어나온다`는 설명에 맞춰 촉수 뿌리를 초상 패널 중앙 가슴 위치에 배치했습니다. 첫 번째 뿌리 본은 액자 면의 정면 법선 방향으로 돌출되고, 두 번째 본부터 제공 GLB의 기존 곡선으로 복귀해 아래로 휘어진 뒤 오른쪽 위로 올라갑니다.

## 보존 범위

- 기존 돌로레: `2,223정점·4,139면·27본`과 기존 재질을 변경하지 않았습니다.
- 공격 촉수: `13,059정점·4,417면·13본`을 변경하지 않았습니다.
- 출구 각도는 `Bone_000`만 액자 정면으로 조정하고 `Bone_012`부터 기존 곡선을 유지했습니다.
- 수정된 출구 포즈를 샘플 리그의 기준 포즈로 고정해 Blender와 GLB가 같은 각도를 표시합니다.
- 공격 원본 GLB의 SHA-256은 `56C400903A1B977024DFE7999F6D8AD0A19F6E0634E40816CCC18BCC04EF20A0`입니다.
- 원본 공격 GLB에는 재질과 UV가 없어, 샘플 복제본에만 연속 UV와 기존 돌로레 살결 텍스처 기반 `Dolore_Attack_Flesh` 재질을 연결했습니다.
- 원본 공격 GLB에 동봉된 기본 Cube·Camera·Light·Icosphere는 실제 촉수 부품이 아니므로 샘플 내보내기에서 제외했습니다.

## 검토 파일

- 기준 비교: `renders/05_reference_comparison.png`
- 정면: `renders/01_front_attached.png`
- 사선: `renders/02_three_quarter_attached.png`
- 측면 결합: `renders/03_side_attachment.png`
- 결합부 확대: `renders/04_attachment_closeup.png`
- Blender 원본: `blender/Dolore_AttackAttachment_Sample.blend`
- 검토용 GLB: `exports/Dolore_AttackAttachment_Sample.glb`
- 브라우저 요약: `index.html`

## 촉수 리깅 확인

- 원본 GLB, Blender 샘플, 내보낸 GLB 모두 Armature modifier와 13본 단일 연결 사슬을 유지합니다.
- 촉수의 13,059개 정점은 모두 유효한 스킨 웨이트를 가지며 정점별 총합은 `1.0`입니다.
- 뿌리를 고정해도 `Bone_012`부터 `Bone_003`까지 10개 본이 연속적으로 메시를 변형하므로 촉수 굽힘·찌르기·당기기 몸통 모션에 사용할 수 있습니다.
- 말단 `Bone_002`와 `Bone_001`은 직접 웨이트가 없어 못 끝을 두 본으로 별도 미세 관절 제어하는 기능은 제한됩니다. 현재 요구된 촉수 몸통 움직임에는 재리깅이 필요하지 않습니다.
- 상세 수치: `RIG_INSPECTION.txt`, `RIG_INSPECTION.json`

## 승인 후 적용 의도

- 돌로레 3번 촉수 찌르기 공격과 4번 처형 끌어오기 모션에서 동일한 `Dolore_Attack_Attachment`와 13본 촉수 리그를 사용합니다.
- 실제 애니메이션과 Unity 적용은 별도 승인 작업으로 진행합니다.

## 재생성 및 점검

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' --background --factory-startup --python 'artSample\enemies\dolore\attack_attachment\tools\build_attack_attachment_sample.py'
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' --background --factory-startup --python 'artSample\enemies\dolore\attack_attachment\tools\inspect_attack_attachment_sample.py'
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' --background --factory-startup --python 'artSample\enemies\dolore\attack_attachment\tools\inspect_tentacle_rig.py'
```

독립 점검 결과는 `SAMPLE_INSPECTION.txt`와 `SAMPLE_INSPECTION.json`에 있습니다.
