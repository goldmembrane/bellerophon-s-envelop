from __future__ import annotations

import importlib.util
import json
from pathlib import Path

import bpy
from mathutils import Vector


REPO_ROOT = Path(__file__).resolve().parents[5]
SAMPLE_ROOT = REPO_ROOT / "artSample/enemies/tergo/green_body_eyes_added"
SOURCE_FBX = REPO_ROOT / "enemies model/tergo.fbx"
EYES_BUILDER_PATH = REPO_ROOT / "artSample/enemies/tergo/eyes_added/tools/build_tergo_eyes_added.py"
BLEND_PATH = SAMPLE_ROOT / "blender/tergo_green_body_eyes_added.blend"
FBX_PATH = SAMPLE_ROOT / "exports/tergo_green_body_eyes_added.fbx"
GLB_PATH = SAMPLE_ROOT / "exports/tergo_green_body_eyes_added.glb"
RENDER_DIR = SAMPLE_ROOT / "renders"


def load_eyes_builder():
    spec = importlib.util.spec_from_file_location("tergo_eyes_added_builder", EYES_BUILDER_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Cannot load eye builder: {EYES_BUILDER_PATH}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.SAMPLE_ROOT = SAMPLE_ROOT
    module.SOURCE_FBX = SOURCE_FBX
    module.BLEND_PATH = BLEND_PATH
    module.FBX_PATH = FBX_PATH
    module.GLB_PATH = GLB_PATH
    module.RENDER_DIR = RENDER_DIR
    return module


eyes = load_eyes_builder()


def ensure_dirs() -> None:
    for path in (BLEND_PATH.parent, FBX_PATH.parent, GLB_PATH.parent, RENDER_DIR):
        path.mkdir(parents=True, exist_ok=True)


def set_bsdf_input(bsdf: bpy.types.Node, names: tuple[str, ...], value) -> None:
    for name in names:
        if name in bsdf.inputs:
            bsdf.inputs[name].default_value = value
            return


def make_wet_green_material(
    name: str,
    dark: tuple[float, float, float, float],
    mid: tuple[float, float, float, float],
    light: tuple[float, float, float, float],
    roughness: float,
    noise_scale: float,
    bump_strength: float,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if not bsdf:
        return mat

    noise = nodes.new("ShaderNodeTexNoise")
    noise.name = name + "_ColorNoise"
    noise.inputs["Scale"].default_value = noise_scale
    noise.inputs["Detail"].default_value = 10.0
    noise.inputs["Roughness"].default_value = 0.62

    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.name = name + "_GreenRamp"
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[0].color = dark
    ramp.color_ramp.elements[1].position = 0.82
    ramp.color_ramp.elements[1].color = light
    mid_element = ramp.color_ramp.elements.new(0.52)
    mid_element.color = mid

    bump = nodes.new("ShaderNodeBump")
    bump.name = name + "_FineWetBump"
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.035

    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    set_bsdf_input(bsdf, ("Roughness",), roughness)
    set_bsdf_input(bsdf, ("Metallic",), 0.0)
    set_bsdf_input(bsdf, ("Specular IOR Level", "Specular"), 0.64)
    return mat


def add_soft_green_emission(mat: bpy.types.Material, color: tuple[float, float, float, float], strength: float) -> None:
    bsdf = mat.node_tree.nodes.get("Principled BSDF") if mat.use_nodes else None
    if not bsdf:
        return
    set_bsdf_input(bsdf, ("Emission Color",), color)
    set_bsdf_input(bsdf, ("Emission Strength",), strength)


def make_translucent_green_body_material() -> bpy.types.Material:
    mat = bpy.data.materials.new("Tergo_Green_Translucent_Body")
    mat.use_nodes = True
    mat.diffuse_color = (0.040, 0.22, 0.12, 0.58)
    mat.blend_method = "BLEND"
    mat.show_transparent_back = False
    try:
        mat.use_screen_refraction = True
    except AttributeError:
        pass

    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    transparent = nodes.new("ShaderNodeBsdfTransparent")
    emission = nodes.new("ShaderNodeEmission")
    mix = nodes.new("ShaderNodeMixShader")

    noise = nodes.new("ShaderNodeTexNoise")
    noise.name = "Tergo_Translucent_Green_Internal_Mottle"
    noise.inputs["Scale"].default_value = 13.5
    noise.inputs["Detail"].default_value = 9.0
    noise.inputs["Roughness"].default_value = 0.54

    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.name = "Tergo_Translucent_Green_DepthRamp"
    ramp.color_ramp.elements[0].position = 0.12
    ramp.color_ramp.elements[0].color = (0.014, 0.090, 0.055, 1.0)
    ramp.color_ramp.elements[1].position = 0.92
    ramp.color_ramp.elements[1].color = (0.090, 0.330, 0.190, 1.0)
    mid = ramp.color_ramp.elements.new(0.52)
    mid.color = (0.030, 0.200, 0.115, 1.0)

    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], emission.inputs["Color"])
    emission.inputs["Strength"].default_value = 0.68
    mix.inputs["Fac"].default_value = 0.58
    links.new(transparent.outputs["BSDF"], mix.inputs[1])
    links.new(emission.outputs["Emission"], mix.inputs[2])
    links.new(mix.outputs["Shader"], output.inputs["Surface"])
    return mat


def apply_green_body_materials(mesh_objects: list[bpy.types.Object]) -> dict[str, object]:
    unified_mat = make_translucent_green_body_material()

    assignment_counts: dict[str, int] = {}
    for obj in mesh_objects:
        if obj.name.startswith("Review_"):
            continue
        obj.data.materials.clear()
        obj.data.materials.append(unified_mat)
        for poly in obj.data.polygons:
            poly.material_index = 0
        assignment_counts[obj.name] = {
            "translucentGreen": len(obj.data.polygons),
        }

    return {
        "materials": [
            unified_mat.name,
        ],
        "unifiedExceptEyeColor": True,
        "bodyMaterialStyle": "translucent wet green",
        "bodyAlpha": 0.58,
        "bodyColorRevision": "darker translucent green v2",
        "bodyDiffuseColor": [0.040, 0.22, 0.12, 0.58],
        "bodyEmissionStrength": 0.68,
        "unifiedMaterial": unified_mat.name,
        "assignmentCounts": assignment_counts,
    }


def unify_added_non_eye_parts(material_name: str) -> dict[str, object]:
    mat = bpy.data.materials.get(material_name)
    if mat is None:
        raise RuntimeError(f"Unified material not found: {material_name}")

    changed: list[str] = []
    preserved: list[str] = []
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH" or not obj.name.startswith("Tergo_"):
            continue
        if "Glowing_Eye_Lens" in obj.name or "Eye_Hot_Core" in obj.name:
            preserved.append(obj.name)
            continue
        obj.data.materials.clear()
        obj.data.materials.append(mat)
        for poly in obj.data.polygons:
            poly.material_index = 0
        changed.append(obj.name)

    return {
        "unifiedMaterial": material_name,
        "changedNonEyeObjects": changed,
        "preservedEyeColorObjects": preserved,
    }


def tone_down_eye_lights() -> dict[str, object]:
    changed: list[str] = []
    for obj in bpy.context.scene.objects:
        if obj.type == "LIGHT" and obj.name.startswith("Tergo_") and "Eye_Amber_Point_Light" in obj.name:
            obj.data.energy = 0.05
            obj.data.color = (1.0, 0.62, 0.24)
            obj.data.shadow_soft_size = 0.055
            changed.append(obj.name)
    return {
        "eyePointLightEnergy": 0.05,
        "eyePointLightColor": [1.0, 0.62, 0.24],
        "adjustedLights": changed,
    }


def render_samples(bounds_min: Vector, bounds_max: Vector) -> list[str]:
    center = (bounds_min + bounds_max) * 0.5
    dims = bounds_max - bounds_min
    full_target = Vector((center.x, center.y, bounds_min.z + dims.z * 0.52))
    head_target = Vector((center.x, bounds_min.y, bounds_min.z + dims.z * 0.925))
    renders = [
        ("Tergo_Green_Front_Camera", (center.x, bounds_min.y - dims.z * 2.20, bounds_min.z + dims.z * 0.52), full_target, dims.z * 1.64, "tergo_green_eyes_front.png"),
        ("Tergo_Green_ThreeQuarter_Camera", (center.x + dims.x * 1.45, bounds_min.y - dims.z * 1.95, bounds_min.z + dims.z * 0.56), full_target, dims.z * 1.64, "tergo_green_eyes_three_quarter.png"),
        ("Tergo_Green_Side_Camera", (center.x + dims.z * 2.15, center.y, bounds_min.z + dims.z * 0.52), full_target, dims.z * 1.64, "tergo_green_eyes_side.png"),
        ("Tergo_Green_Closeup_Camera", (center.x, bounds_min.y - dims.z * 0.72, bounds_min.z + dims.z * 0.925), head_target, dims.z * 0.36, "tergo_green_eyes_closeup.png"),
        ("Tergo_Green_Front_Large_Camera", (center.x, bounds_min.y - dims.z * 0.78, bounds_min.z + dims.z * 0.925), head_target, dims.z * 0.44, "tergo_green_eyes_front_large.png"),
        ("Tergo_Green_Side_Large_Camera", (center.x + dims.z * 0.80, center.y, bounds_min.z + dims.z * 0.925), head_target, dims.z * 0.48, "tergo_green_eyes_side_large.png"),
    ]
    output_paths: list[str] = []
    for name, location, target, ortho_scale, file_name in renders:
        output = RENDER_DIR / file_name
        eyes.render_camera(name, location, target, ortho_scale, output)
        output_paths.append(str(output.relative_to(REPO_ROOT)).replace("\\", "/"))
    return output_paths


def write_readme(report: dict[str, object]) -> None:
    placement = report["eyePlacement"]
    body = report["greenBodyMaterials"]
    readme = f"""# Tergo 녹색 몸통 + 눈 샘플

## 목적

`enemies model/tergo.fbx` 원본 Tergo 모델에 주황 발광 눈을 추가하고, 눈 색을 제외한 얼굴, 몸통, 팔, 다리, 눈 주변 보조 메시를 녹색 계열 반투명 재질로 칠한 승인용 샘플입니다. 아직 Unity 씬, 프리팹, 런타임 에셋에는 적용하지 않았습니다.

## 반영 방식

- 원본 `enemies model/tergo.fbx`를 그대로 임포트했습니다.
- 기존 눈 샘플의 위치 값을 재사용해 주황 발광 눈 색을 유지했습니다.
- 눈 렌즈와 눈 코어를 제외한 모든 시각 메시에는 `Tergo_Green_Translucent_Body` 재질을 적용했습니다.
- Longa Arma의 반투명 부위와 같은 방향으로 `blend_method=BLEND`, `Alpha`, 저광택 젖은 표면, 내부 녹색 노이즈를 사용했습니다.
- 눈 보조 포인트 라이트는 얼굴을 노랗게 물들이지 않도록 이 샘플 안에서만 약하게 낮췄습니다.
- 몸체 메시, Armature, 스케일, 방향, 리깅 구조는 수정하지 않았습니다.

## 검토 파일

- `index.html`
- `renders/tergo_green_eyes_front.png`
- `renders/tergo_green_eyes_three_quarter.png`
- `renders/tergo_green_eyes_side.png`
- `renders/tergo_green_eyes_closeup.png`
- `renders/tergo_green_eyes_front_large.png`
- `renders/tergo_green_eyes_side_large.png`
- `blender/tergo_green_body_eyes_added.blend`
- `exports/tergo_green_body_eyes_added.fbx`
- `exports/tergo_green_body_eyes_added.glb`

## 재질 기록

- 녹색 머티리얼: `{body["materials"]}`
- 눈 제외 전체 반투명 재질 통일: `{body["unifiedExceptEyeColor"]}`
- 재질 방식: `{body["bodyMaterialStyle"]}`
- 몸체 Alpha: `{body["bodyAlpha"]}`
- 통일 재질: `{body["unifiedMaterial"]}`
- 눈 보조 조명 에너지: `{report["eyeLightAdjustment"]["eyePointLightEnergy"]}`
- 눈 렌즈 색: 기존 주황 발광 유지
- 눈 중심 Z: `{placement["eyeZ"]}`
- 눈 간격: `{placement["eyeSpacing"]}`

## 승인 전 제한

사용자 승인 전에는 이 샘플을 Unity 씬, 프리팹, 런타임 모델, AI, 충돌, 피격 판정, 애니메이션에 연결하지 않습니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")


def write_index(report: dict[str, object]) -> None:
    placement = report["eyePlacement"]
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Tergo 녹색 몸통 + 눈 샘플</title>
  <style>
    :root {{
      color-scheme: dark;
      --bg: #101512;
      --panel: #172019;
      --line: #38513e;
      --text: #edf4ed;
      --muted: #b8c8ba;
      --accent: #f4b647;
      --green: #79a752;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font-family: "Malgun Gothic", "Apple SD Gothic Neo", sans-serif;
    }}
    main {{
      max-width: 1560px;
      margin: 0 auto;
      padding: 28px;
    }}
    h1, h2, h3 {{ margin: 0; letter-spacing: 0; }}
    h1 {{ font-size: 28px; line-height: 1.25; }}
    h2 {{ font-size: 20px; margin-bottom: 12px; }}
    h3 {{ font-size: 15px; color: var(--green); margin-bottom: 8px; }}
    p, li {{ color: var(--muted); line-height: 1.58; }}
    a {{ color: var(--accent); }}
    code {{ color: #e9f4e8; }}
    section {{ margin-top: 32px; }}
    .summary {{
      display: grid;
      grid-template-columns: minmax(0, 1.2fr) minmax(340px, 0.8fr);
      gap: 18px;
      align-items: start;
    }}
    .note, figure, .asset-list {{
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 6px;
    }}
    .note {{ padding: 16px; }}
    .grid {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(310px, 1fr));
      gap: 14px;
    }}
    .large-grid {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(520px, 1fr));
      gap: 16px;
    }}
    figure {{ margin: 0; padding: 10px; }}
    figure img {{
      width: 100%;
      display: block;
      background: #0b0f0d;
      border: 1px solid #26352b;
      border-radius: 4px;
    }}
    .large-grid figure img {{
      min-height: 420px;
      object-fit: contain;
    }}
    figcaption {{
      margin-top: 8px;
      color: var(--muted);
      font-size: 13px;
      word-break: break-all;
    }}
    .asset-list {{ padding: 14px 16px; }}
    .asset-list ul {{ margin: 10px 0 0; padding-left: 18px; }}
    .pill-row {{
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-top: 12px;
    }}
    .pill {{
      border: 1px solid #4c664b;
      background: #202b21;
      color: #d8e8d7;
      padding: 6px 8px;
      font-size: 12px;
      border-radius: 4px;
    }}
    @media (max-width: 780px) {{
      main {{ padding: 18px; }}
      .summary, .large-grid {{ grid-template-columns: 1fr; }}
      .large-grid figure img {{ min-height: 0; }}
    }}
  </style>
</head>
<body>
<main>
  <h1>Tergo 녹색 몸통 + 눈 샘플</h1>
  <div class="summary">
    <div>
      <p><code>enemies model/tergo.fbx</code> 원본 모델에 주황 발광 눈을 추가하고, 눈 색을 제외한 얼굴, 몸통, 팔, 다리, 눈 주변 보조 메시를 녹색 계열 반투명 재질로 칠한 승인용 샘플입니다. Unity 씬, 프리팹, 런타임 에셋에는 아직 반영하지 않았습니다.</p>
      <div class="pill-row">
        <span class="pill">눈 색 유지</span>
        <span class="pill">녹색 반투명 몸체</span>
        <span class="pill">Unity 미적용</span>
        <span class="pill">artSample 검토용</span>
      </div>
    </div>
    <div class="note">
      <strong>제한 범위</strong>
      <p>몸체 메시, Armature, 스케일, 방향, 애니메이션, AI, 충돌, 피격 판정은 수정하지 않았습니다.</p>
    </div>
  </div>

  <section>
    <h2>기준 이미지와 확대 렌더 비교</h2>
    <div class="large-grid">
      <figure>
        <img src="../../../../image/tergo(테르고).png" alt="테르고 정면 기준 이미지">
        <figcaption>정면 기준: image/tergo(테르고).png</figcaption>
      </figure>
      <figure>
        <img src="renders/tergo_green_eyes_front_large.png" alt="녹색 몸통 테르고 정면 확대 렌더">
        <figcaption>생성 정면 확대: renders/tergo_green_eyes_front_large.png</figcaption>
      </figure>
      <figure>
        <img src="../../../../image/tergo-beside.png" alt="테르고 측면 기준 이미지">
        <figcaption>측면 기준: image/tergo-beside.png</figcaption>
      </figure>
      <figure>
        <img src="renders/tergo_green_eyes_side_large.png" alt="녹색 몸통 테르고 측면 확대 렌더">
        <figcaption>생성 측면 확대: renders/tergo_green_eyes_side_large.png</figcaption>
      </figure>
    </div>
  </section>

  <section>
    <h2>생성 렌더</h2>
    <div class="grid">
      <figure>
        <img src="renders/tergo_green_eyes_front.png" alt="녹색 몸통 테르고 정면 렌더">
        <figcaption>renders/tergo_green_eyes_front.png</figcaption>
      </figure>
      <figure>
        <img src="renders/tergo_green_eyes_three_quarter.png" alt="녹색 몸통 테르고 3/4 렌더">
        <figcaption>renders/tergo_green_eyes_three_quarter.png</figcaption>
      </figure>
      <figure>
        <img src="renders/tergo_green_eyes_side.png" alt="녹색 몸통 테르고 측면 렌더">
        <figcaption>renders/tergo_green_eyes_side.png</figcaption>
      </figure>
      <figure>
        <img src="renders/tergo_green_eyes_closeup.png" alt="녹색 몸통 테르고 머리 클로즈업">
        <figcaption>renders/tergo_green_eyes_closeup.png</figcaption>
      </figure>
    </div>
  </section>

  <section>
    <h2>산출물</h2>
    <div class="asset-list">
      <h3>검토 및 원본 파일</h3>
      <ul>
        <li><a href="blender/tergo_green_body_eyes_added.blend">blender/tergo_green_body_eyes_added.blend</a></li>
        <li><a href="exports/tergo_green_body_eyes_added.fbx">exports/tergo_green_body_eyes_added.fbx</a></li>
        <li><a href="exports/tergo_green_body_eyes_added.glb">exports/tergo_green_body_eyes_added.glb</a></li>
        <li><a href="README.md">README.md</a></li>
        <li><a href="ASSET_MANIFEST.json">ASSET_MANIFEST.json</a></li>
        <li><a href="tools/build_tergo_green_body_eyes_added.py">tools/build_tergo_green_body_eyes_added.py</a></li>
      </ul>
    </div>
  </section>

  <section>
    <h2>위치 기록</h2>
    <div class="asset-list">
      <ul>
        <li>눈 중심 Z: <code>{placement["eyeZ"]}</code></li>
        <li>눈 간격: <code>{placement["eyeSpacing"]}</code></li>
        <li>왼쪽 눈 lens Y: <code>{placement["leftFrontY"]}</code></li>
        <li>오른쪽 눈 lens Y: <code>{placement["rightFrontY"]}</code></li>
      </ul>
    </div>
  </section>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def write_readme_korean(report: dict[str, object]) -> None:
    placement = report["eyePlacement"]
    body = report["greenBodyMaterials"]
    readme = f"""# Tergo 녹색 반투명 몸통 + 주황색 눈 샘플

## 목적

`enemies model/tergo.fbx` 원본 Tergo 모델에 주황색 발광 눈을 추가하고, 눈 색을 제외한 얼굴, 몸통, 팔, 다리, 눈 주변 보조 메쉬를 어두운 녹색 계열의 반투명 재질로 통일한 승인용 샘플입니다. 아직 Unity 씬, 프리팹, 런타임 에셋에는 적용하지 않았습니다.

## 반영 방식

- 원본 `enemies model/tergo.fbx`를 그대로 임포트했습니다.
- 기존 눈 샘플의 위치 값을 재사용해 주황색 발광 눈을 유지했습니다.
- 눈 렌즈와 눈 코어를 제외한 모든 시각 메쉬에는 `Tergo_Green_Translucent_Body` 재질을 적용했습니다.
- 반투명 느낌을 유지하기 위해 `blend_method=BLEND`, Alpha `0.58`, 투명 셰이더 혼합, 내부 녹색 노이즈를 사용했습니다.
- 이번 버전은 이전 녹색 샘플보다 색 램프와 발광 강도를 낮춰 조금 더 어두운 녹색으로 조정했습니다.
- 눈 보조 라인과 아이라이트는 얼굴에 까맣게 묻어나지 않도록 샘플 안에서만 약하게 낮췄습니다.
- 몸체 메쉬, Armature, 스케일, 방향, 리깅 구조는 수정하지 않았습니다.

## 검토 파일

- `index.html`
- `renders/tergo_green_eyes_front.png`
- `renders/tergo_green_eyes_three_quarter.png`
- `renders/tergo_green_eyes_side.png`
- `renders/tergo_green_eyes_closeup.png`
- `renders/tergo_green_eyes_front_large.png`
- `renders/tergo_green_eyes_side_large.png`
- `blender/tergo_green_body_eyes_added.blend`
- `exports/tergo_green_body_eyes_added.fbx`
- `exports/tergo_green_body_eyes_added.glb`

## 재질 기록

- 녹색 머티리얼: `{body["materials"]}`
- 눈 제외 전체 반투명 재질 통일: `{body["unifiedExceptEyeColor"]}`
- 재질 방식: `{body["bodyMaterialStyle"]}`
- 몸체 Alpha: `{body["bodyAlpha"]}`
- 몸체 Diffuse RGBA: `{body["bodyDiffuseColor"]}`
- 몸체 발광 강도: `{body["bodyEmissionStrength"]}`
- 통일 재질: `{body["unifiedMaterial"]}`
- 눈 보조 조명 에너지: `{report["eyeLightAdjustment"]["eyePointLightEnergy"]}`
- 눈 렌즈 색: 기존 주황색 발광 유지
- 눈 중심 Z: `{placement["eyeZ"]}`
- 눈 간격: `{placement["eyeSpacing"]}`

## 승인 전 제한

사용자 승인 전에는 이 샘플을 Unity 씬, 프리팹, 런타임 모델, AI, 충돌, 공격 판정, 애니메이션에 연결하지 않습니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")


def write_index_korean(report: dict[str, object]) -> None:
    placement = report["eyePlacement"]
    body = report["greenBodyMaterials"]
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Tergo 녹색 반투명 몸통 + 주황색 눈 샘플</title>
  <style>
    :root {{
      color-scheme: dark;
      --bg: #101512;
      --panel: #172019;
      --line: #38513e;
      --text: #edf4ed;
      --muted: #b8c8ba;
      --accent: #f4b647;
      --green: #79a752;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font-family: "Malgun Gothic", "Apple SD Gothic Neo", sans-serif;
    }}
    main {{
      max-width: 1560px;
      margin: 0 auto;
      padding: 28px;
    }}
    h1, h2, h3 {{ margin: 0; letter-spacing: 0; }}
    h1 {{ font-size: 28px; line-height: 1.25; }}
    h2 {{ font-size: 20px; margin-bottom: 12px; }}
    h3 {{ font-size: 15px; color: var(--green); margin-bottom: 8px; }}
    p, li {{ color: var(--muted); line-height: 1.58; }}
    a {{ color: var(--accent); }}
    code {{ color: #e9f4e8; }}
    section {{ margin-top: 32px; }}
    .summary {{
      display: grid;
      grid-template-columns: minmax(0, 1.2fr) minmax(340px, 0.8fr);
      gap: 18px;
      align-items: start;
    }}
    .note, figure, .asset-list {{
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 6px;
    }}
    .note {{ padding: 16px; }}
    .grid {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(310px, 1fr));
      gap: 14px;
    }}
    .large-grid {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(520px, 1fr));
      gap: 16px;
    }}
    figure {{ margin: 0; padding: 10px; }}
    figure img {{
      width: 100%;
      display: block;
      background: #0b0f0d;
      border: 1px solid #26352b;
      border-radius: 4px;
    }}
    .large-grid figure img {{
      min-height: 420px;
      object-fit: contain;
    }}
    figcaption {{
      margin-top: 8px;
      color: var(--muted);
      font-size: 13px;
      word-break: break-all;
    }}
    .asset-list {{ padding: 14px 16px; }}
    .asset-list ul {{ margin: 10px 0 0; padding-left: 18px; }}
    .pill-row {{
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-top: 12px;
    }}
    .pill {{
      border: 1px solid #4c664b;
      background: #202b21;
      color: #d8e8d7;
      padding: 6px 8px;
      font-size: 12px;
      border-radius: 4px;
    }}
    @media (max-width: 780px) {{
      main {{ padding: 18px; }}
      .summary, .large-grid {{ grid-template-columns: 1fr; }}
      .large-grid figure img {{ min-height: 0; }}
    }}
  </style>
</head>
<body>
<main>
  <h1>Tergo 녹색 반투명 몸통 + 주황색 눈 샘플</h1>
  <div class="summary">
    <div>
      <p><code>enemies model/tergo.fbx</code> 원본 모델에 주황색 발광 눈을 추가하고, 눈을 제외한 얼굴과 몸통 전체를 어두운 녹색 계열의 반투명 재질로 통일한 승인용 샘플입니다. Unity 씬, 프리팹, 런타임 에셋에는 아직 반영하지 않았습니다.</p>
      <div class="pill-row">
        <span class="pill">주황색 눈 유지</span>
        <span class="pill">어두운 녹색 반투명 몸체</span>
        <span class="pill">Unity 미적용</span>
        <span class="pill">artSample 검토용</span>
      </div>
    </div>
    <div class="note">
      <strong>제한 범위</strong>
      <p>몸체 메쉬, Armature, 스케일, 방향, 애니메이션, AI, 충돌, 공격 판정은 수정하지 않았습니다.</p>
    </div>
  </div>

  <section>
    <h2>기준 이미지와 정밀 렌더 비교</h2>
    <div class="large-grid">
      <figure>
        <img src="../../../../image/tergo(테르고).png" alt="테르고 정면 기준 이미지">
        <figcaption>정면 기준: image/tergo(테르고).png</figcaption>
      </figure>
      <figure>
        <img src="renders/tergo_green_eyes_front_large.png" alt="녹색 반투명 테르고 정면 정밀 렌더">
        <figcaption>생성 정면 정밀: renders/tergo_green_eyes_front_large.png</figcaption>
      </figure>
      <figure>
        <img src="../../../../image/tergo-beside.png" alt="테르고 측면 기준 이미지">
        <figcaption>측면 기준: image/tergo-beside.png</figcaption>
      </figure>
      <figure>
        <img src="renders/tergo_green_eyes_side_large.png" alt="녹색 반투명 테르고 측면 정밀 렌더">
        <figcaption>생성 측면 정밀: renders/tergo_green_eyes_side_large.png</figcaption>
      </figure>
    </div>
  </section>

  <section>
    <h2>생성 렌더</h2>
    <div class="grid">
      <figure>
        <img src="renders/tergo_green_eyes_front.png" alt="녹색 반투명 테르고 정면 렌더">
        <figcaption>renders/tergo_green_eyes_front.png</figcaption>
      </figure>
      <figure>
        <img src="renders/tergo_green_eyes_three_quarter.png" alt="녹색 반투명 테르고 3/4 렌더">
        <figcaption>renders/tergo_green_eyes_three_quarter.png</figcaption>
      </figure>
      <figure>
        <img src="renders/tergo_green_eyes_side.png" alt="녹색 반투명 테르고 측면 렌더">
        <figcaption>renders/tergo_green_eyes_side.png</figcaption>
      </figure>
      <figure>
        <img src="renders/tergo_green_eyes_closeup.png" alt="녹색 반투명 테르고 머리 클로즈업">
        <figcaption>renders/tergo_green_eyes_closeup.png</figcaption>
      </figure>
    </div>
  </section>

  <section>
    <h2>산출물</h2>
    <div class="asset-list">
      <h3>검토 및 원본 파일</h3>
      <ul>
        <li><a href="blender/tergo_green_body_eyes_added.blend">blender/tergo_green_body_eyes_added.blend</a></li>
        <li><a href="exports/tergo_green_body_eyes_added.fbx">exports/tergo_green_body_eyes_added.fbx</a></li>
        <li><a href="exports/tergo_green_body_eyes_added.glb">exports/tergo_green_body_eyes_added.glb</a></li>
        <li><a href="README.md">README.md</a></li>
        <li><a href="ASSET_MANIFEST.json">ASSET_MANIFEST.json</a></li>
        <li><a href="tools/build_tergo_green_body_eyes_added.py">tools/build_tergo_green_body_eyes_added.py</a></li>
      </ul>
    </div>
  </section>

  <section>
    <h2>위치 및 재질 기록</h2>
    <div class="asset-list">
      <ul>
        <li>눈 중심 Z: <code>{placement["eyeZ"]}</code></li>
        <li>눈 간격: <code>{placement["eyeSpacing"]}</code></li>
        <li>왼쪽 눈 lens Y: <code>{placement["leftFrontY"]}</code></li>
        <li>오른쪽 눈 lens Y: <code>{placement["rightFrontY"]}</code></li>
        <li>몸체 Alpha: <code>{body["bodyAlpha"]}</code></li>
        <li>몸체 Diffuse RGBA: <code>{body["bodyDiffuseColor"]}</code></li>
        <li>몸체 발광 강도: <code>{body["bodyEmissionStrength"]}</code></li>
      </ul>
    </div>
  </section>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def export_assets() -> None:
    eyes.export_assets()


def main() -> None:
    ensure_dirs()
    eyes.clear_scene()
    eyes.import_tergo()

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not mesh_objects:
        raise RuntimeError("No mesh objects were imported from Tergo FBX.")

    material_report = apply_green_body_materials(mesh_objects)
    source_min, source_max = eyes.mesh_bounds(mesh_objects)
    head_front_y = eyes.estimate_head_front_y(mesh_objects, source_min, source_max)
    eye_report = eyes.add_eye_pair(source_min, source_max, mesh_objects, head_front_y)
    non_eye_unification_report = unify_added_non_eye_parts(material_report["unifiedMaterial"])
    eye_light_report = tone_down_eye_lights()
    eyes.setup_scene(source_min, source_max)
    render_paths = render_samples(source_min, source_max)
    export_assets()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    dims = source_max - source_min
    report = {
        "sample": "Tergo green body with amber eyes",
        "createdForApproval": True,
        "unityRuntimeApplied": False,
        "sourceFbx": str(SOURCE_FBX.relative_to(REPO_ROOT)).replace("\\", "/"),
        "sourceBoundsMin": [round(v, 4) for v in source_min],
        "sourceBoundsMax": [round(v, 4) for v in source_max],
        "sourceDimensions": [round(v, 4) for v in dims],
        "eyePlacement": eye_report,
        "eyeColorPreserved": True,
        "eyeLightAdjustment": eye_light_report,
        "greenBodyMaterials": material_report,
        "nonEyeUnification": non_eye_unification_report,
        "changed": [
            "Translucent wet green material assigned to face, body, arms, legs, and non-lens eye support meshes",
            "Amber eye lens and hot core color preserved from the approved eye sample",
            "Source FBX changed to enemies model/tergo.fbx",
            "Body green ramp and emission strength darkened while preserving alpha 0.58",
        ],
        "notChanged": [
            "Tergo body mesh vertices",
            "Tergo armature",
            "Tergo body scale",
            "Tergo drill arm geometry",
            "Unity scene and prefabs",
            "Animations and runtime logic",
        ],
        "outputs": {
            "blend": str(BLEND_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "fbx": str(FBX_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "glb": str(GLB_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "renders": render_paths,
        },
    }
    write_readme_korean(report)
    write_index_korean(report)
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print("Tergo green body with amber eyes sample generated.")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
