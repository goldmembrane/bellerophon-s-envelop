# 몬스트룸 어두운 녹색/눈 오브젝트 샘플

- 기준 모델: 현재 Unity `CargoRunMvp`에 배치된 `Monstrum_00_Static_Review` 정리 메시 기반
- 참고 이미지: `image/monstrum(몬스트룸).png`
- 반영 의도: 씨앗/솜털/떠 있는 알갱이는 제외하고, 어두운 녹색 몸통과 노란 눈 인상만 샘플링
- 실제 씬 적용 여부: 아니오. 이 폴더의 PNG/설명/내보내기 파일만 생성

## 샘플 파일

- `index.html`: 기준 이미지와 생성 렌더를 직접 비교하는 대표 미리보기
- `renders/front.png`: 정면 검토용
- `renders/side.png`: 측면 검토용
- `renders/back.png`: 후면 검토용
- `renders/head_close.png`: 눈 위치/색 확인용 근접 샷
- `renders/reference_comparison.png`: 기준 이미지와 샘플 정면 렌더 좌우 비교
- `textures/monstrum_dark_moss_body_albedo.png`: 어두운 녹색 몸통용 절차적 표면 텍스처
- `exports/monstrum_visual_recolor_eye_sample.obj`: 검토용 OBJ 내보내기 파일
- `exports/monstrum_visual_recolor_eye_sample.mtl`: OBJ용 머티리얼 정의
- `APPROVAL_STATUS.json`: 승인 상태
- `VISUAL_ANALYSIS.md`: 기준 이미지/기획서/사용자 확인 사항 분석
- `UNITY_APPLICATION_PLAN.md`: 승인 후 Unity 적용 계획
- `RULE_COMPLIANCE_CHECKLIST.md`: `AGENTS.md` 샘플 규칙 점검표

## 적용 예정 방식

- 몸통은 어두운 녹색 계열 머티리얼과 절차적 얼룩 텍스처를 몬스트룸 렌더러에 적용
- 눈은 본체 메시를 직접 변형하지 않고, 머리 앞쪽의 어두운 얼굴면 위에 작고 날카로운 황색 눈틈을 자식 오브젝트로 추가
- 원본 FBX는 직접 수정하지 않음
- 실제 Unity 씬/프리팹 적용은 이 샘플 승인 후 별도 승인으로만 진행

## 샘플 수치

- BodyColor: (0.055, 0.145, 0.065, 1)
- BodyShadowReference: (0.035, 0.085, 0.035, 1)
- EyeGlowColor: (0.94, 0.78, 0.16, 1)
- EyeCenter: (0.002, 1.63, -0.471)
- LeftEye: (0.049, 1.63, -0.471)
- RightEye: (-0.045, 1.63, -0.471)
- EyeHeightReference: 0.01
- EyeSeparation: 0.047
