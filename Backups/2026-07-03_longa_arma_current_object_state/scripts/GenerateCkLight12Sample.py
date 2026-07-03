from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "ck_light12"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR):
        path.mkdir(parents=True, exist_ok=True)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def set_principled_input(mat: bpy.types.Material, name: str, value) -> None:
    if not mat.use_nodes:
        return

    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None and name in bsdf.inputs:
        bsdf.inputs[name].default_value = value


def material(
    name: str,
    color: tuple[float, float, float, float],
    *,
    metallic: float = 0.0,
    roughness: float = 0.75,
    alpha: float | None = None,
    emission: tuple[float, float, float, float] | None = None,
    emission_strength: float = 0.0,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    set_principled_input(mat, "Base Color", color)
    set_principled_input(mat, "Metallic", metallic)
    set_principled_input(mat, "Roughness", roughness)
    if alpha is not None:
        set_principled_input(mat, "Alpha", alpha)
        mat.blend_method = "BLEND"
        mat.show_transparent_back = True
    if emission is not None:
        set_principled_input(mat, "Emission Color", emission)
        set_principled_input(mat, "Emission Strength", emission_strength)
    mat.diffuse_color = color
    return mat


def worn_metal_material(name: str, base: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = material(name, base, metallic=0.28, roughness=0.90)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 38
    noise.inputs["Detail"].default_value = 8
    noise.inputs["Roughness"].default_value = 0.62
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[0].color = (base[0] * 0.48, base[1] * 0.48, base[2] * 0.48, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.42, 1),
        min(base[1] * 1.42, 1),
        min(base[2] * 1.42, 1),
        1,
    )
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def add_empty(name: str, parent: bpy.types.Object | None = None) -> bpy.types.Object:
    empty = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(empty)
    if parent is not None:
        empty.parent = parent
    return empty


def add_box(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
    bevel_width: float = 0.014,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    obj.parent = parent
    if bevel_width > 0:
        bevel = obj.modifiers.new("hard edge bevel", "BEVEL")
        bevel.width = bevel_width
        bevel.segments = 1
        obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_cylinder(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    radius: float,
    depth: float,
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
    vertices: int = 20,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_cylinder_between(
    name: str,
    parent: bpy.types.Object,
    start: tuple[float, float, float],
    end: tuple[float, float, float],
    radius: float,
    mat: bpy.types.Material,
    vertices: int = 14,
) -> bpy.types.Object:
    start_v = Vector(start)
    end_v = Vector(end)
    direction = end_v - start_v
    midpoint = (start_v + end_v) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=direction.length, location=midpoint)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_sphere(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    radius: float,
    mat: bpy.types.Material,
    scale: tuple[float, float, float] = (1.0, 1.0, 1.0),
    segments: int = 32,
    ring_count: int = 12,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=ring_count, radius=radius, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 80
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("CK-12 dark cockpit world")
    scene.world.color = (0.010, 0.012, 0.014)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def build_context(mats: dict[str, bpy.types.Material]) -> None:
    context = add_empty("CK-12 cockpit placement context")

    add_box("context front panoramic screen", context, (0, 0.42, 1.78), (4.9, 0.05, 1.24), mats["glass"], bevel_width=0.018)
    add_box("context front screen upper frame", context, (0, 0.38, 2.45), (5.1, 0.09, 0.09), mats["frame"], bevel_width=0.01)
    add_box("context front screen lower frame", context, (0, 0.38, 1.10), (5.1, 0.09, 0.09), mats["frame"], bevel_width=0.01)
    add_box("context left cockpit wall", context, (-2.55, -0.82, 1.25), (0.08, 2.95, 2.2), mats["wall"], bevel_width=0.006)
    add_box("context right cockpit wall", context, (2.55, -0.82, 1.25), (0.08, 2.95, 2.2), mats["wall"], bevel_width=0.006)
    add_box("context rear cargo threshold", context, (0.0, -2.42, 1.18), (1.55, 0.09, 1.24), mats["rear_wall"], bevel_width=0.01)
    add_box("context ceiling reference plane", context, (0, -0.82, 2.68), (5.2, 3.65, 0.035), mats["ceiling"], bevel_width=0)
    add_box("context floor clearance", context, (0, -1.05, 0.02), (5.2, 3.7, 0.04), mats["floor"], bevel_width=0)
    add_box("context approved CK-02 console body ghost", context, (0, -0.76, 0.62), (3.8, 1.0, 0.56), mats["console_ghost"], bevel_width=0.035)
    add_box("context approved CK-02 console top ghost", context, (0, -1.0, 0.94), (3.5, 0.64, 0.09), mats["console_ghost"], bevel_width=0.025)


def add_ceiling_inspection_bar(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_box("front ceiling inspection rail armored body", root, (0, -0.22, 2.57), (4.42, 0.16, 0.12), mats["fixture"], bevel_width=0.025)
    add_box("front ceiling inspection rail rear conduit", root, (0, -0.36, 2.60), (4.62, 0.055, 0.075), mats["conduit"], bevel_width=0.012)

    for index, x in enumerate((-1.55, -0.52, 0.52, 1.55), start=1):
        add_box(f"front ceiling cool inspection diffuser {index}", root, (x, -0.305, 2.505), (0.78, 0.025, 0.045), mats["cool_light"], bevel_width=0.015)
        add_box(f"front ceiling diffuser dark lip {index}", root, (x, -0.322, 2.505), (0.88, 0.015, 0.060), mats["dark_lip"], bevel_width=0.007)
        add_sphere(f"front ceiling soft light pool {index}", root, (x, -0.76, 0.98), 0.58, mats["cool_pool"], (0.82, 0.24, 0.030), 24, 10)

    for x in (-2.05, -1.05, 0.0, 1.05, 2.05):
        add_cylinder("front ceiling rail exposed screw head", root, (x, -0.325, 2.57), 0.025, 0.014, mats["worn"], (math.radians(90), 0, 0), 16)


def add_side_service_strips(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    side_specs = (
        ("left", -2.495, -1.02, 1.55, -1.0),
        ("right", 2.495, -1.02, 1.55, 1.0),
    )
    for side, x, y, z, normal in side_specs:
        add_box(f"{side} wall inspection strip recessed backing", root, (x + normal * 0.005, y, z), (0.050, 1.20, 0.15), mats["fixture"], bevel_width=0.018)
        add_box(f"{side} wall cool white service lens", root, (x - normal * 0.020, y, z), (0.018, 0.96, 0.070), mats["side_light"], bevel_width=0.012)
        add_box(f"{side} wall lower amber inspection tick", root, (x - normal * 0.022, y - 0.54, z - 0.18), (0.014, 0.14, 0.045), mats["warm_light"], bevel_width=0.006)
        add_box(f"{side} wall upper amber inspection tick", root, (x - normal * 0.022, y + 0.54, z + 0.18), (0.014, 0.14, 0.045), mats["warm_light"], bevel_width=0.006)
        add_sphere(f"{side} wall pale inspection wash", root, (x - normal * 0.040, y, z - 0.02), 0.55, mats["side_pool"], (0.030, 0.82, 0.34), 24, 10)

    add_cylinder_between("left ceiling-to-wall lighting conduit", root, (-2.10, -0.34, 2.57), (-2.47, -0.54, 2.07), 0.018, mats["conduit"])
    add_cylinder_between("right ceiling-to-wall lighting conduit", root, (2.10, -0.34, 2.57), (2.47, -0.54, 2.07), 0.018, mats["conduit"])


def add_rear_threshold_downlights(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_box("rear threshold inspection light mounting rail", root, (0, -2.37, 2.13), (1.78, 0.13, 0.12), mats["fixture"], bevel_width=0.018)
    for index, x in enumerate((-0.58, 0.0, 0.58), start=1):
        add_cylinder(
            f"rear cargo threshold round downlight {index}",
            root,
            (x, -2.46, 2.08),
            0.085,
            0.040,
            mats["warm_light"],
            (math.radians(90), 0, 0),
            24,
        )
        add_cylinder(
            f"rear cargo threshold dark retaining ring {index}",
            root,
            (x, -2.455, 2.08),
            0.108,
            0.018,
            mats["dark_lip"],
            (math.radians(90), 0, 0),
            28,
        )
        add_sphere(f"rear threshold warm floor pool {index}", root, (x, -2.05, 0.08), 0.46, mats["warm_pool"], (0.58, 0.28, 0.025), 24, 10)


def add_console_work_lights(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_box("console front underdeck inspection strip backing", root, (0, -1.55, 0.40), (3.55, 0.055, 0.055), mats["fixture"], bevel_width=0.012)
    add_box("console front underdeck soft cyan lens", root, (0, -1.585, 0.395), (3.28, 0.018, 0.033), mats["cyan_light"], bevel_width=0.010)
    add_sphere("console front soft cyan work pool", root, (0, -1.58, 0.055), 1.06, mats["cyan_pool"], (1.55, 0.18, 0.020), 32, 10)

    for index, x in enumerate((-2.10, -0.72, 0.72, 2.10), start=1):
        add_box(f"floor toe-kick inspection marker {index}", root, (x, -1.84, 0.075), (0.42, 0.050, 0.018), mats["warm_light"], bevel_width=0.004)
        add_box(f"floor toe-kick worn metal lip {index}", root, (x, -1.81, 0.075), (0.48, 0.030, 0.024), mats["worn"], bevel_width=0.004)


def add_maintenance_lamp_pods(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    pod_specs = (
        ("left forward angled inspection pod", -2.03, 0.03, 2.33, -12),
        ("right forward angled inspection pod", 2.03, 0.03, 2.33, 12),
        ("left rear service pod", -2.15, -2.02, 2.16, 18),
        ("right rear service pod", 2.15, -2.02, 2.16, -18),
    )
    for name, x, y, z, rz in pod_specs:
        rot = (0.0, 0.0, math.radians(rz))
        add_box(name + " armored yoke", root, (x, y, z), (0.38, 0.11, 0.10), mats["fixture"], rot, bevel_width=0.018)
        add_box(name + " recessed maintenance lens", root, (x, y - 0.055, z - 0.035), (0.24, 0.022, 0.040), mats["cool_light"], rot, bevel_width=0.010)
        add_cylinder(name + " left pivot bolt", root, (x - 0.18, y - 0.058, z), 0.025, 0.014, mats["worn"], (math.radians(90), 0, 0), 16)
        add_cylinder(name + " right pivot bolt", root, (x + 0.18, y - 0.058, z), 0.025, 0.014, mats["worn"], (math.radians(90), 0, 0), 16)


def build_ck12_lighting(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("CK-12 cockpit inspection lighting sample")
    add_ceiling_inspection_bar(root, mats)
    add_side_service_strips(root, mats)
    add_rear_threshold_downlights(root, mats)
    add_console_work_lights(root, mats)
    add_maintenance_lamp_pods(root, mats)

    add_box("small spare inspection fuse cover", root, (-2.30, -1.86, 1.03), (0.22, 0.05, 0.18), mats["fixture"], bevel_width=0.010)
    add_box("small spare inspection fuse amber window", root, (-2.30, -1.895, 1.03), (0.13, 0.014, 0.06), mats["warm_light"], bevel_width=0.004)
    add_box("right wall removable light service cover", root, (2.50, -1.86, 1.02), (0.045, 0.34, 0.20), mats["fixture"], bevel_width=0.010)
    add_cylinder_between("low voltage cable along front screen frame", root, (-2.02, 0.31, 2.30), (2.02, 0.31, 2.30), 0.012, mats["conduit"], 12)


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -0.8, 2.35))
    top = bpy.context.object
    top.name = "ck12 soft combined inspection glow"
    top.data.energy = 190
    top.data.size = 4.8

    bpy.ops.object.light_add(type="POINT", location=(-2.2, -0.95, 1.45))
    left = bpy.context.object
    left.name = "ck12 left wall service glow"
    left.data.energy = 55
    left.data.color = (0.78, 0.92, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(2.2, -0.95, 1.45))
    right = bpy.context.object
    right.name = "ck12 right wall service glow"
    right.data.energy = 55
    right.data.color = (0.78, 0.92, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(0, -2.10, 1.10))
    rear = bpy.context.object
    rear.name = "ck12 rear threshold warm glow"
    rear.data.energy = 45
    rear.data.color = (1.0, 0.74, 0.45)


def add_camera(
    name: str,
    loc: tuple[float, float, float],
    target: tuple[float, float, float],
    lens: float,
    *,
    ortho_scale: float | None = None,
) -> bpy.types.Object:
    bpy.ops.object.camera_add(location=loc)
    camera = bpy.context.object
    camera.name = "ck12 camera " + name
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = lens
    if ortho_scale is not None:
        camera.data.type = "ORTHO"
        camera.data.ortho_scale = ortho_scale
    camera.data.dof.use_dof = False
    return camera


def render_camera(camera: bpy.types.Object, output_path: Path) -> None:
    bpy.context.scene.camera = camera
    bpy.context.scene.render.filepath = str(output_path)
    bpy.ops.render.render(write_still=True)


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "CK-12",
        "title": "조종실 내부 조명 / 점검등",
        "approvalState": "조건부 승인",
        "unityApplicationAllowed": True,
        "targetUnityRoot": "Approved Cockpit 12 Inspection Lighting",
        "targetScene": "Assets/_Project/Scenes/CargoRunMvp.unity",
        "integrationRule": "CK-01/CK-02/CK-04/CK-11을 교체하지 않고 별도 조명 루트로 배치한다. 왼쪽 동력실, 오른쪽 통제실, 뒤쪽 운송창고 입구에 점검등을 두고 천장등은 기존 CK-04 경고등과 겹치지 않는다.",
        "includedParts": [
            "전면 천장 냉백색 점검등 바",
            "좌우 벽면 서비스 라이트 스트립",
            "뒤쪽 운송창고 방향 문턱 다운라이트",
            "조종대 하부 작업등",
            "바닥 toe-kick 점검 마커",
            "천장 모서리 유지보수 램프 포드",
            "저전압 케이블과 마운팅 브래킷",
        ],
        "excludedParts": [
            "CK-04 비상 경고등 교체",
            "수동 운행 UI",
            "실제 게임플레이 상태 로직",
            "콜라이더",
            "사운드",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "approvalState": "조건부 승인",
        "unityApplicationAllowed": True,
        "requiresUserApprovalBeforeUnity": False,
        "conditionToReview": "왼쪽 동력실, 오른쪽 통제실, 뒤쪽 운송창고 입구마다 점검등을 배치하고 천장등은 기존 CK-04 경고등과 겹치지 않는 조건으로 승인",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# ck_light12

CK-12 조종실 내부 조명 / 점검등 샘플입니다.

## 목적

- 조종실 구조와 CK-02 조종대를 읽기 쉽게 만드는 보조 조명입니다.
- CK-04 비상 경고등을 대체하지 않습니다.
- 수동 운행 UI, 내구도 표시, 경고 상태 로직은 포함하지 않습니다.

## 구성

- 전면 천장 냉백색 점검등 바
- 좌우 벽면 서비스 라이트 스트립
- 뒤쪽 운송창고 방향 문턱 다운라이트
- 조종대 하부 작업등
- 바닥 toe-kick 점검 마커
- 천장 모서리 유지보수 램프 포드
- 저전압 케이블과 마운팅 브래킷

## Unity 반영 기준

조건부 승인 상태입니다. Unity 적용 시 `Approved Cockpit 12 Inspection Lighting` 별도 루트로 배치하고, 기존 CK-01, CK-02, CK-04, CK-11 오브젝트를 교체하지 않습니다.
왼쪽 동력실, 오른쪽 통제실, 뒤쪽 운송창고 입구마다 점검등을 배치하고, 천장등은 기존 CK-04 경고등과 겹치지 않아야 합니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    html = """<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ck_light12 review</title>
  <style>
    body{margin:0;background:#111514;color:#ece5d8;font-family:Arial,sans-serif}
    main{max-width:1280px;margin:0 auto;padding:24px}
    h1{font-size:28px;margin:0 0 8px}
    .meta{color:#cfc6b8;margin:0 0 18px;line-height:1.55}
    .grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:16px}
    figure{margin:0;border:1px solid #3c4643;background:#1b211f;border-radius:6px;padding:10px}
    img{display:block;width:100%;height:auto;background:#050807}
    figcaption{font-size:14px;color:#ddd3c3;margin-top:8px}
    a{color:#9bd5ff}
  </style>
</head>
<body>
<main>
  <h1>CK-12 조종실 내부 조명 / 점검등</h1>
  <p class="meta">조건부 승인 샘플입니다. Unity 적용 시 각 복도 입구에 점검등을 배치하고, 천장등은 기존 CK-04 경고등과 겹치지 않게 유지합니다. 기존 CK-04 경고등을 교체하지 않고, 조종실 구조를 읽기 쉽게 만드는 보조 조명 루트로만 적용합니다.</p>
  <section class="grid">
    <figure><a href="renders/01_front.png"><img src="renders/01_front.png" alt="전면"></a><figcaption>01 전면 배치</figcaption></figure>
    <figure><a href="renders/02_player.png"><img src="renders/02_player.png" alt="플레이어 시점"></a><figcaption>02 플레이어 시점</figcaption></figure>
    <figure><a href="renders/03_side.png"><img src="renders/03_side.png" alt="측면"></a><figcaption>03 측면 구조</figcaption></figure>
    <figure><a href="renders/04_top.png"><img src="renders/04_top.png" alt="상단"></a><figcaption>04 상단 배치</figcaption></figure>
    <figure><a href="renders/05_ceiling_detail.png"><img src="renders/05_ceiling_detail.png" alt="천장 세부"></a><figcaption>05 천장 점검등 세부</figcaption></figure>
    <figure><a href="renders/06_service_detail.png"><img src="renders/06_service_detail.png" alt="서비스 조명 세부"></a><figcaption>06 벽면/하부 작업등 세부</figcaption></figure>
  </section>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def export_assets() -> None:
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "ck_light12.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "ck_light12.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "ck_light12.glb"), export_format="GLB")


def main() -> None:
    ensure_dirs()
    reset_scene()
    configure_rendering()

    mats = {
        "floor": material("context muted cockpit floor", (0.070, 0.082, 0.078, 1), roughness=0.90),
        "glass": material("context front panorama glass", (0.045, 0.30, 0.28, 0.28), roughness=0.20, alpha=0.28, emission=(0.025, 0.20, 0.18, 1), emission_strength=0.12),
        "wall": material("context cockpit wall", (0.13, 0.16, 0.15, 0.50), roughness=0.88, alpha=0.50),
        "rear_wall": material("context rear cargo threshold wall", (0.12, 0.14, 0.135, 0.58), roughness=0.88, alpha=0.58),
        "ceiling": material("transparent ceiling reference", (0.060, 0.070, 0.068, 0.20), roughness=0.88, alpha=0.20),
        "frame": worn_metal_material("context black cockpit frame", (0.052, 0.060, 0.058, 1)),
        "console_ghost": material("transparent approved CK02 console ghost", (0.16, 0.20, 0.19, 0.14), roughness=0.80, alpha=0.14),
        "fixture": worn_metal_material("dark worn inspection light housing", (0.050, 0.057, 0.054, 1)),
        "conduit": worn_metal_material("black low voltage conduit", (0.025, 0.028, 0.027, 1)),
        "dark_lip": worn_metal_material("dark retaining light lip", (0.018, 0.020, 0.019, 1)),
        "worn": worn_metal_material("worn screw and bracket metal", (0.36, 0.36, 0.32, 1)),
        "cool_light": material("cool white frosted inspection lens", (0.72, 0.88, 1.0, 1), roughness=0.22, emission=(0.55, 0.78, 1.0, 1), emission_strength=1.6),
        "side_light": material("side wall cool service lens", (0.62, 0.90, 1.0, 1), roughness=0.24, emission=(0.42, 0.72, 1.0, 1), emission_strength=1.15),
        "warm_light": material("warm amber inspection lens", (1.0, 0.63, 0.25, 1), roughness=0.28, emission=(1.0, 0.46, 0.12, 1), emission_strength=0.9),
        "cyan_light": material("soft cyan under console lens", (0.25, 0.92, 0.95, 1), roughness=0.28, emission=(0.10, 0.72, 0.76, 1), emission_strength=0.72),
        "cool_pool": material("transparent cool white light footprint", (0.45, 0.72, 1.0, 0.040), roughness=0.92, alpha=0.040, emission=(0.12, 0.20, 0.32, 1), emission_strength=0.025),
        "side_pool": material("transparent side wall inspection wash", (0.38, 0.70, 0.92, 0.040), roughness=0.92, alpha=0.040, emission=(0.06, 0.13, 0.18, 1), emission_strength=0.022),
        "warm_pool": material("transparent warm downlight footprint", (1.0, 0.46, 0.16, 0.045), roughness=0.92, alpha=0.045, emission=(0.18, 0.07, 0.02, 1), emission_strength=0.020),
        "cyan_pool": material("transparent cyan work light footprint", (0.20, 0.85, 0.85, 0.045), roughness=0.92, alpha=0.045, emission=(0.025, 0.14, 0.14, 1), emission_strength=0.020),
    }

    build_context(mats)
    build_ck12_lighting(mats)
    add_render_lights()

    cameras = [
        ("front", (0.0, -5.75, 2.32), (0.0, -0.92, 1.50), 36, "01_front.png", None),
        ("player", (0.0, -3.38, 1.62), (0.0, -0.74, 1.43), 32, "02_player.png", None),
        ("side", (5.2, -1.55, 2.10), (0.0, -0.90, 1.38), 42, "03_side.png", None),
        ("top", (0.0, -0.92, 6.45), (0.0, -0.92, 0.30), 50, "04_top.png", 5.9),
        ("ceiling_detail", (-2.45, -2.45, 2.45), (-0.62, -0.42, 2.26), 44, "05_ceiling_detail.png", None),
        ("service_detail", (2.92, -2.60, 1.58), (1.24, -1.34, 0.92), 44, "06_service_detail.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
