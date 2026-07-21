# 돌로레 외형 동기화 아트 샘플 점검

- 원본: `enemies model/dolore.fbx`
- 원본 SHA-256: `0A8DF2A16B881B24A5FC856E2E3534A05D506049CE4C49975BB8433E71A2204E`
- 아트 샘플: `artSample/enemies/dolore/`
- 독립 점검: `PASS`

## 확인 결과

- Blender 원본: `1.4×3.0×1.8m`, 27본, 33개 UV 메시, 3개 재질
- 리깅 FBX: `1.4×2.989468×1.78994m`, 27본, 33개 UV 메시, 3개 재질
- 정적 검토 GLB: `1.4×2.98495×1.789991m`, 33개 UV 메시, 3개 재질
- Blender 내부 원본 스킨 메시: 2,223정점, 4,139폴리곤, 9개 연결 성분
- 공격 촉수 미리보기: 3개 별도 오브젝트, 리깅되지 않음, 정적 내보내기에서 제외
- Unity 적용: 수행하지 않음

## 시각 대조

- `artSample/enemies/dolore/renders/06_reference_comparison_static.png`
- `artSample/enemies/dolore/renders/07_reference_comparison_attack.png`

보호된 하네스 검증, EditMode/PlayMode 테스트, 빌드, Unity 재시작과 Git 작업은 실행하지 않았다.
