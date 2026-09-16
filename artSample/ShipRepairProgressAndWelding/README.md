# ShipRepair 진행 바 · 용접 이펙트 아트 샘플

## 산출물

- `ProgressBars_50Percent_ArtSample.png`: `ShipRepair`, `SabotageRepair` 머리 위 50% 진행 바
- `ShipRepair_WeldingVFX_ArtSample.png`: `ShipRepair` 앞 작업점 용접 이펙트 전체 배치
- `ShipRepair_WeldingVFX_Comparison.png`: Unity 원본과 용접 샘플 나란한 비교
- `ShipRepair_WeldingVFX_Detail.png`: 용접 작업점 확대
- `WeldingVFX_TransparentSource.png`: 합성에 사용한 투명 용접 VFX 소스
- `UnityReference_Paired.png`, `UnityReference_ShipRepair.png`: 현재 Unity 모델 기준 이미지
- `index.html`: 사용자 검토용 요약 페이지

## 제작 방식

- 진행 바: `ffmpeg` 픽셀 합성. 내부 트랙 206px 중 103px를 채워 50%를 고정했습니다.
- 용접 이펙트: 내장 `image_gen` 도구로 투명 VFX 레이어를 만든 뒤 `ffmpeg`로 현재 Unity 캡처에 합성했습니다.
- Unity 적용: 아직 수행하지 않았습니다.

## 최종 이미지 생성 프롬프트

```text
Use case: stylized-concept
Asset type: transparent Unity game welding VFX art-sample sprite
Primary request: Create only one compact active welding effect element with a tiny intensely bright blue-white arc core near the upper center, a thin cyan electrical corona, and a controlled fan of small orange-gold sparks traveling mostly downward and slightly outward. Include a very faint short gray-blue smoke wisp close to the arc.
Style/medium: polished real-time sci-fi game particle-effect concept, crisp additive glow, practical Unity VFX reference.
Composition/framing: centered effect with generous transparent padding; compact core; sparks contained within the canvas and weighted downward.
Background: genuinely transparent with preserved alpha.
Constraints: VFX only; no person, astronaut, hands, tool, metal panel, scenery, progress bar, text, labels, logo, watermark, fireball, explosion, or large smoke cloud.
```

## 승인 후 적용 기준

- 진행 바는 각 개체 머리 중심 위의 월드 공간 앵커를 추종합니다.
- 수리 진행 중 표시하고 완료·취소·상호작용 종료 시 숨깁니다.
- 용접 이펙트는 `ShipRepair` 앞의 작업점을 추종하며 수리 진행 중 연속 재생합니다.
- 샘플의 청백색 아크, 청색 외곽광, 주황색 하강 스파크 비율을 재현합니다.
