# 쿠르사 정면 정렬 승인용 샘플

상태: `USER_REVIEW_REQUIRED` / `NOT_APPLIED_TO_UNITY`

이 폴더는 쿠르사 몸통·양팔·얼굴 정면 정렬안을 Unity에 반영하기 전에 직접 확인하기 위한 `artSample`입니다. 현재 Unity의 FBX, 프리팹, 애니메이션 및 배치 개체에는 적용하지 않았습니다.

## 기준과 작업 내용

- 원본 기준 이미지: `image/KUŠkursa(쿠르사).png`
- 현재 런타임 원본: `Assets/_Project/Art/Enemies/Kursa/ApprovedAppearance/Models/Kursa_Appearance_RuntimeProjection.fbx`
- 외형·머티리얼 기준: `artSample/enemies/kursa/appearance_reference_sync/blender/Kursa_Appearance_ReferenceSync.blend`
- 몸통은 복구된 현재 상태의 정면 편차가 작아 큰 회전을 추가하지 않았습니다.
- 방패 없는 오른팔은 본 이름만으로 어깨를 추정하지 않고, 현재 메시의 실제 오른쪽 어깨 캡 아래 관절에서 시작해 몸 옆으로 자연스럽게 내려오도록 정렬했습니다.
- 방패를 든 왼팔은 실제 왼쪽 어깨에서 팔꿈치까지 다시 연결하되, 손과 방패의 월드 위치·회전을 유지했습니다.
- 원본 런타임 메시의 정점 `2109개`, 면 `3913개`, 토폴로지와 스킨 가중치를 그대로 유지했습니다. 전면 면 삭제, 정점 이동, 대체 얼굴 메시 생성은 사용하지 않습니다.
- 기존 메시를 단계별로 직접 렌더해 비교한 결과 `+22°` 시각 보정에서 양눈 중점 아래로 콧등과 턱이 가장 곧게 들어왔습니다. 최종 `Head` 회전 보정은 약 `-29.3302°`이며 고개 전체의 좌우 이동은 적용하지 않았습니다.
- 양눈은 기존 얼굴 재질 투영만 사용합니다. 투영 중심 높이는 기존 두 눈의 평균 높이로 맞추고, 곡면 노출 차이를 상쇄하기 위해 왼쪽 크기 `3.45`, 오른쪽 크기 `3.95`를 사용했습니다. 메시의 눈구멍이나 얼굴 형상은 이동하지 않았습니다.
- 정면 금속판의 한쪽 반사광이 이목을 가리지 않도록 기존 검토 조명의 Key/Fill 세기만 균형 있게 조정했습니다. 좌우 25도 렌더에서도 눈이 얼굴에서 이탈하거나 별도 오브젝트처럼 분리되지 않는지 직접 확인합니다.

## 직접 확인할 렌더

- `renders/13_reference_current_candidate.png`: 원본 기준, 현재 정적 자세, 정면 정렬 후보의 전체 비교
- `renders/14_upper_face_comparison.png`: 몸통·양팔과 얼굴 확대 비교
- `renders/15_candidate_yaw_review.png`: 방패 표시/숨김 상태의 좌우 25도 사선 확인
- `renders/09_candidate_landmarks_front.png`: 실제 어깨·팔꿈치·손목 기준점 확인
- `renders/10_candidate_upper_front_no_shield.png`: 방패에 가려진 왼팔 연결 확인

확인 항목은 다음과 같습니다.

1. 방패 없는 팔이 실제 오른쪽 어깨 캡 아래에서 끊김 없이 이어지는지
2. 방패 팔이 실제 왼쪽 어깨에서 연결되고 손잡이 그립을 유지하는지
3. 원본 얼굴 메시에서 양눈 투영의 높이와 화면 크기가 맞고 미간·콧등·턱이 한 중앙축에 놓이는지
4. 좌우 25도에서 얼굴 형상이 찌그러지거나 팔이 몸통을 관통하지 않는지
5. 전체 실루엣이 원본 기준의 정면 방패병 자세와 일치하는지

## 산출물

- 편집 가능한 샘플: `blender/Kursa_FrontAlignment_Sample.blend`
- 분석·변경 기록: `KURSA_FRONT_ALIGNMENT_SAMPLE.json`
- 재현 스크립트: `tools/build_kursa_front_alignment_sample.py`
- 비교판 생성 스크립트: `tools/build_review_boards.py`

Unity 반영은 이 샘플에 대한 사용자 확인 이후 별도 승인 범위를 받아 진행합니다.
