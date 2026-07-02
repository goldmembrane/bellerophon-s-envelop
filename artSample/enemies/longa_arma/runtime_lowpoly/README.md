# Longa Arma 런타임 저폴리 샘플

- 생성 시각: 2026-07-02 23:31:44
- 목적: 기존 Longa Arma 고밀도 모델의 말형 머리, 네 다리 짐승 실루엣, 왼팔 칼날, 젖은 녹청색 몸체, 어두운 칼날 인상을 유지하면서 런타임 다수 배치에 맞게 메시 밀도를 낮춘 검토용 샘플입니다.
- 원본 외부 모델 `enemies model/longa arma.blend`는 수정하지 않았습니다.
- Unity 런타임 씬, 프리팹, AI, 히트박스에는 아직 연결하지 않았습니다.

## 폴리곤 수

- 원본 Unity 적용 사본: 144,545 vertices / 289,086 polygons
- 저폴리 샘플: 6,002 vertices / 12,000 triangles
- 삼각형 감소율: 95.8%

## 검토 파일

- 정면 렌더: `renders/front.png`
- 측면 렌더: `renders/side.png`
- 후면 렌더: `renders/back.png`
- 3분기 렌더: `renders/three_quarter.png`
- 와이어프레임 밀도 렌더: `renders/wireframe_density.png`
- Blender 파일: `blender/longa_arma_runtime_lowpoly.blend`
- 내보내기 파일: `exports/longa_arma_runtime_lowpoly.fbx`, `exports/longa_arma_runtime_lowpoly.glb`
- 생성 스크립트: `build_runtime_lowpoly_sample.py`

## 애니메이션 변형 타깃

- 단일 표시 메쉬에 런타임 검토용 Armature와 Shape Key를 포함했습니다.
- 이동/공격은 짐승형 동작에 맞게 Armature 본을 기준으로 구동합니다.
- 포함된 본: `LongaRoot`, `LongaSpine`, `LongaChest`, `LongaHead`, `LongaBladeArm`, `LongaBladeArmForearm`, `LongaBladeArmTip`, `LongaFrontRightLeg`, `LongaFrontRightLowerLeg`, `LongaFrontRightFoot`, `LongaFrontLeftLeg`, `LongaFrontLeftLowerLeg`, `LongaFrontLeftFoot`, `LongaRearRightLeg`, `LongaRearRightLowerLeg`, `LongaRearRightFoot`, `LongaRearLeftLeg`, `LongaRearLeftLowerLeg`, `LongaRearLeftFoot`
- 포함된 Shape Key: `Idle_Breath_BodySway`, `Move_LimpingBladeArm_Drag`, `Move_Crawl_AlternateStep`, `Move_FrontRight_LegReach`, `Move_FrontRight_LegPush`, `Move_FrontLeft_LegReach`, `Move_RearRight_LegReach`, `Move_RearLeft_LegReach`, `Move_BladeArm_SlowDrag`, `Attack_LeftBlade_SlamWindup`, `Attack_FrontLeg_SlamDrag`, `Attack_UpperBody_Rise`, `Attack_Forelimbs_ForwardSlam`, `Attack_GroundDrag_Pullback`, `Hit_HeadBack_Flinch`, `Hit_HeadSide_Shake`, `Consume_HeadBack_Windup`, `Consume_HeadForward_BiteSlam`, `Consume_Peck_Impact`, `Death_Melt_FlatLiquidSpread`, `Death_Puddle_Final`
- Shape Key는 대기 호흡, 피격/섭취 보조 변형, 액체화 같은 비관절 변형에 사용합니다.

## 주의

- 이 샘플은 런타임 최적화용 외형 검토 샘플입니다.
- 다리 기어감과 공격 내리찍기는 저폴리 단일 SkinnedMesh의 본 애니메이션으로 처리합니다. 실제 전투 이동, 피격 판정, 섭취 판정은 Unity 게임플레이 로직에서 별도로 연결해야 합니다.
