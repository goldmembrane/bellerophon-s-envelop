# AGENTS.md 샘플 규칙 점검표

- `artSample/enemies/monstrum/` 아래 생성: 충족
- 사용자 승인 전 Unity 런타임 씬/프리팹/에셋 연결 금지: 충족, 샘플 생성용 임시 오브젝트만 사용
- `docs/GAME_DESIGN_SOURCE.txt`, 사용자 확인 사항, `image/` 기준 이미지 참고: 충족
- 기준 이미지의 외형/색/재질/질감 분석 문서화: `VISUAL_ANALYSIS.md`에 기록
- 텍스처와 머티리얼 포함: `textures/monstrum_dark_moss_body_albedo.png`, `MATERIAL_SETTINGS.txt`, `exports/*.mtl` 포함
- 단순 단색 머티리얼 금지: 절차적 어두운 녹색 얼룩 텍스처를 적용
- 정적 렌더 포함: 정면, 3/4, 근접 PNG 포함
- 기준 이미지 대비 side-by-side 비교 포함: `renders/reference_comparison.png` 포함
- 검토용 원본/내보내기 파일 포함: `exports/monstrum_visual_recolor_eye_sample.obj`, `exports/monstrum_visual_recolor_eye_sample.mtl` 포함
- README, 승인 상태 JSON, 에셋 매니페스트 JSON, `index.html` 미리보기 포함: 충족
- 애니메이션 샘플 필수 아님: 이번 작업은 비애니메이션 시각 샘플이며 애니메이션은 생성하지 않음
