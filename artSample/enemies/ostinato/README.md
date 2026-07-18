# 오스티나토 현재 모델 원화 직접 전사 샘플

- 검토 시작점: `index.html`
- 기준 이미지: `image/ostinato(오스티나토).png`, `image/ostinato-beside.png`, `image/ostinato-back.png`
- 대상 모델: `Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx`
- 대상 모델 SHA-256: `35F85E29015DE71416F5A8DD76A86424451CCF89B1C1130AC7B690E6D8B1E533`
- 상태: 사용자 검토 대기, Unity 런타임 미적용
- 요약 페이지 렌더 확인: `renders/07_index_page_preview.png`

## 고정 범위

- 현재 FBX의 메시, 정점, 실루엣, 비율, 리깅, 애니메이션을 변경하지 않는다.
- 비슷한 절차 색을 만들지 않고 세 기준 이미지의 실제 픽셀에서 갑각, 적갈 조직, 가위날, 복안과 마모성 표면을 직접 전사한다.
- 원화와 현재 FBX의 형상·자세 차이는 유지하며, 텍스처·머티리얼로 표현할 수 있는 표면 외형만 재현한다.
- 현재 모델은 `char1` 단일 SkinnedMeshRenderer, 정점 3,728개, 서브메시 1개다.
- 샘플 렌더는 기존 모델의 정면·측면·후면 캡처 실루엣을 그대로 사용했다.

## 승인 후 Unity 적용 방향

- 기존 `Ostinato.fbx`를 유지한다.
- FBX의 실제 UV·정점 구조를 확인한 뒤 갑각, 연부, 가위날, 복안을 구분하는 마스크를 제작한다.
- 알베도, 노멀, 거칠기와 부위별 금속성·반사값을 승인된 직접 전사 시안에 맞춘다.
- 현재 배치된 9개와 애니메이션 구조는 건드리지 않고 렌더러의 시각 에셋만 동기화한다.

## 재생성

```powershell
python artSample/enemies/ostinato/tools/build_ostinato_material_sample.py
```
