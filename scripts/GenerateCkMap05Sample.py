from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "ck_map05"
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
    roughness: float = 0.72,
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
    mat = material(name, base, metallic=0.25, roughness=0.88)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 32
    noise.inputs["Detail"].default_value = 8
    noise.inputs["Roughness"].default_value = 0.58
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.22
    ramp.color_ramp.elements[0].color = (base[0] * 0.55, base[1] * 0.55, base[2] * 0.55, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.35, 1),
        min(base[1] * 1.35, 1),
        min(base[2] * 1.35, 1),
        1,
    )
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def add_empty(name: str) -> bpy.types.Object:
    empty = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(empty)
    return empty


def add_box(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
    bevel_width: float = 0.012,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    obj.parent = parent
    if bevel_width > 0:
        bevel = obj.modifiers.new("edge bevel", "BEVEL")
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
    vertices: int = 24,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_text(
    name: str,
    parent: bpy.types.Object,
    text: str,
    loc: tuple[float, float, float],
    size: float,
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (math.radians(90), 0.0, 0.0),
    align: str = "CENTER",
) -> bpy.types.Object:
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.body = text
    obj.data.align_x = align
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.001
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def add_line(
    name: str,
    parent: bpy.types.Object,
    start: tuple[float, float, float],
    end: tuple[float, float, float],
    thickness: float,
    mat: bpy.types.Material,
) -> bpy.types.Object:
    sx, sy, sz = start
    ex, ey, ez = end
    mx = (sx + ex) * 0.5
    my = (sy + ey) * 0.5
    mz = (sz + ez) * 0.5
    dx = ex - sx
    dz = ez - sz
    length = math.sqrt(dx * dx + dz * dz)
    angle = math.atan2(dz, dx)
    return add_box(name, parent, (mx, my, mz), (length, thickness, thickness), mat, (0.0, 0.0, angle), 0.004)


def build_context(mats: dict[str, bpy.types.Material]) -> None:
    context = add_empty("CK-05 cockpit placement context")

    add_box("front panoramic screen proxy", context, (0, 0.52, 1.82), (4.7, 0.04, 1.25), mats["glass"], bevel_width=0.018)
    add_box("front screen upper frame proxy", context, (0, 0.49, 2.48), (4.95, 0.07, 0.08), mats["frame"], bevel_width=0.008)
    add_box("front screen lower frame proxy", context, (0, 0.49, 1.16), (4.95, 0.07, 0.08), mats["frame"], bevel_width=0.008)
    add_box("front screen left frame proxy", context, (-2.49, 0.49, 1.82), (0.08, 0.07, 1.35), mats["frame"], bevel_width=0.008)
    add_box("front screen right frame proxy", context, (2.49, 0.49, 1.82), (0.08, 0.07, 1.35), mats["frame"], bevel_width=0.008)

    add_box("approved console body ghost", context, (0, -0.78, 0.62), (3.9, 1.0, 0.58), mats["console_ghost"], bevel_width=0.035)
    add_box("approved console top ghost", context, (0, -1.02, 0.96), (3.6, 0.64, 0.10), mats["console_ghost"], bevel_width=0.025)
    add_box("player clearance floor proxy", context, (0, -1.45, 0.02), (4.8, 3.2, 0.04), mats["floor"], bevel_width=0)


def add_room_block(
    root: bpy.types.Object,
    name: str,
    label: str,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    text_mat: bpy.types.Material,
) -> None:
    add_box(name + " durability color plate", root, loc, scale, mat, bevel_width=0.018)
    add_box(name + " black inset rim", root, (loc[0], loc[1] - 0.006, loc[2]), (scale[0] + 0.04, 0.01, scale[2] + 0.04), text_mat, bevel_width=0.012)
    add_box(name + " durability color plate front", root, (loc[0], loc[1] - 0.014, loc[2]), scale, mat, bevel_width=0.014)
    add_text(name + " label", root, label, (loc[0], loc[1] - 0.044, loc[2] + 0.002), 0.055, text_mat)


def build_map_panel(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("CK-05 ship schematic durability panel sample")

    add_box("left lower console mount bracket", root, (-1.25, -1.02, 0.78), (0.22, 0.18, 0.72), mats["mount"], bevel_width=0.018)
    add_box("angled rear support arm", root, (-1.18, -1.07, 1.08), (0.14, 0.18, 0.62), mats["mount"], rot=(0, math.radians(0), math.radians(-16)), bevel_width=0.012)

    add_box("armored map panel back plate", root, (-1.15, -1.22, 1.13), (1.55, 0.16, 1.18), mats["body"], bevel_width=0.04)
    add_box("armored map panel raised frame", root, (-1.15, -1.315, 1.13), (1.42, 0.08, 1.04), mats["frame"], bevel_width=0.028)
    add_box("recessed green glass map screen", root, (-1.15, -1.365, 1.13), (1.28, 0.028, 0.88), mats["screen"], bevel_width=0.018)

    # Schematic wiring lines. The map follows the source ship topology, not the full corridor model.
    line_y = -1.39
    add_line("map line cockpit cargo", root, (-1.15, line_y, 1.44), (-1.15, line_y, 1.16), 0.018, mats["line"])
    add_line("map line cargo engine", root, (-1.15, line_y, 1.16), (-1.58, line_y, 1.27), 0.018, mats["line"])
    add_line("map line cargo control", root, (-1.15, line_y, 1.16), (-0.72, line_y, 1.27), 0.018, mats["line"])
    add_line("map line cargo supply", root, (-1.15, line_y, 1.16), (-1.54, line_y, 0.90), 0.018, mats["line"])
    add_line("map line cargo armory", root, (-1.15, line_y, 1.16), (-0.76, line_y, 0.90), 0.018, mats["line"])
    add_line("map line engine control", root, (-1.58, line_y, 1.27), (-0.72, line_y, 1.27), 0.012, mats["line_dim"])
    add_line("map line supply armory", root, (-1.54, line_y, 0.90), (-0.76, line_y, 0.90), 0.012, mats["line_dim"])

    add_room_block(root, "cockpit room", "COCK", (-1.15, -1.415, 1.54), (0.36, 0.028, 0.14), mats["white"], mats["text_dark"])
    add_room_block(root, "cargo room", "CARGO", (-1.15, -1.415, 1.16), (0.42, 0.028, 0.18), mats["yellow"], mats["text_dark"])
    add_room_block(root, "engine room", "ENG", (-1.58, -1.415, 1.28), (0.32, 0.028, 0.14), mats["orange"], mats["text_dark"])
    add_room_block(root, "control room", "CTRL", (-0.72, -1.415, 1.28), (0.32, 0.028, 0.14), mats["vermilion"], mats["text_dark"])
    add_room_block(root, "supply room", "SUP", (-1.54, -1.415, 0.84), (0.32, 0.028, 0.14), mats["red"], mats["text_light"])
    add_room_block(root, "armory room", "ARM", (-0.76, -1.415, 0.84), (0.32, 0.028, 0.14), mats["black"], mats["text_light"])

    add_text("panel title ship schematic", root, "SHIP  STATUS", (-1.15, -1.435, 1.76), 0.06, mats["text_glow"])
    add_text("panel subtitle durability", root, "ZONE DURABILITY", (-1.15, -1.435, 0.55), 0.045, mats["text_glow"])

    legend_x = -1.67
    legend_z = 0.45
    legend = [
        ("100", mats["white"]),
        ("94", mats["yellow"]),
        ("79", mats["orange"]),
        ("49", mats["vermilion"]),
        ("19", mats["red"]),
        ("0", mats["black"]),
    ]
    for index, (label, mat) in enumerate(legend):
        x = legend_x + index * 0.205
        add_box("durability legend swatch " + label, root, (x, -1.42, legend_z), (0.12, 0.026, 0.06), mat, bevel_width=0.006)
        add_text("durability legend text " + label, root, label, (x, -1.445, legend_z - 0.08), 0.033, mats["text_glow"])

    add_box("small hardware screw top left", root, (-1.82, -1.43, 1.64), (0.05, 0.018, 0.05), mats["worn"], bevel_width=0.01)
    add_box("small hardware screw top right", root, (-0.48, -1.43, 1.64), (0.05, 0.018, 0.05), mats["worn"], bevel_width=0.01)
    add_box("small hardware screw bottom left", root, (-1.82, -1.43, 0.61), (0.05, 0.018, 0.05), mats["worn"], bevel_width=0.01)
    add_box("small hardware screw bottom right", root, (-0.48, -1.43, 0.61), (0.05, 0.018, 0.05), mats["worn"], bevel_width=0.01)

    add_cylinder("left cable socket", root, (-1.9, -1.25, 0.72), 0.055, 0.08, mats["rubber"], rot=(math.radians(90), 0, 0))
    add_cylinder("right cable socket", root, (-0.40, -1.25, 0.72), 0.055, 0.08, mats["rubber"], rot=(math.radians(90), 0, 0))
    add_line("left dangling cable", root, (-1.9, -1.25, 0.72), (-1.74, -1.25, 0.42), 0.025, mats["rubber"])
    add_line("right dangling cable", root, (-0.40, -1.25, 0.72), (-0.58, -1.25, 0.42), 0.025, mats["rubber"])


def configure_rendering() -> None:
    scene = bpy.context.scene
    for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            continue

    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False
    world = bpy.data.worlds.new("ck_map05_world")
    world.color = (0.012, 0.015, 0.016)
    scene.world = world
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = -0.05
    scene.view_settings.gamma = 1.0


def add_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -4.2, 4.4))
    key = bpy.context.object
    key.name = "ck map panel large softbox"
    key.data.energy = 420
    key.data.size = 5.2

    bpy.ops.object.light_add(type="POINT", location=(-1.2, -1.8, 1.35))
    screen = bpy.context.object
    screen.name = "ck map panel green screen spill"
    screen.data.energy = 32
    screen.data.color = (0.20, 0.86, 0.62)

    bpy.ops.object.light_add(type="POINT", location=(1.5, -1.0, 2.0))
    warm = bpy.context.object
    warm.name = "ck map panel warm edge fill"
    warm.data.energy = 55
    warm.data.color = (1.0, 0.74, 0.42)


def look_at(obj: bpy.types.Object, target: tuple[float, float, float]) -> None:
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_camera(
    name: str,
    loc: tuple[float, float, float],
    target: tuple[float, float, float],
    lens: float,
    orthographic_scale: float | None = None,
) -> bpy.types.Object:
    camera_data = bpy.data.cameras.new(name)
    camera = bpy.data.objects.new(name, camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = loc
    if orthographic_scale is None:
        camera_data.lens = lens
    else:
        camera_data.type = "ORTHO"
        camera_data.ortho_scale = orthographic_scale
    camera_data.clip_end = 100
    look_at(camera, target)
    return camera


def render_camera(camera: bpy.types.Object, output_name: str) -> None:
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.filepath = str(RENDER_DIR / output_name)
    bpy.ops.render.render(write_still=True)


def export_assets() -> None:
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / f"{SAMPLE_NAME}.blend"))
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / f"{SAMPLE_NAME}.glb"), export_format="GLB")
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / f"{SAMPLE_NAME}.fbx"), use_selection=False)


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "CK-05",
        "scope": "Cockpit physical ship schematic and zone durability display panel sample.",
        "sourceBasis": [
            "docs/COCKPIT_OBJECTS.md: CK-05 화물선 전개도 / 내구도 표시 패널",
            "docs/GAME_DESIGN_SOURCE.txt:117 수동 운행 화면 왼쪽 하단 전개도와 구역 내구도 색상",
            "docs/GAME_DESIGN_SOURCE.txt:113-115 화물선 6구역과 조종실 연결 구조",
        ],
        "included": [
            "조종대 왼쪽 하단에 붙는 물리 디스플레이 패널",
            "6구역 전개도",
            "구역 연결선",
            "내구도 색상 범례",
            "마운트 브래킷, 케이블, 나사 디테일",
        ],
        "excluded": [
            "수동 운행 UI 전체 화면",
            "자동/수동 조종 상태 표시",
            "복구/수리 장치",
            "실제 내구도 로직",
            "Unity 런타임 연결",
        ],
        "unityApplicationAllowed": False,
        "approvalState": "미승인",
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    approval = {
        "sample": SAMPLE_NAME,
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(
        json.dumps(approval, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    readme = """# ck_map05

CK-05 화물선 전개도 / 구역 내구도 표시 패널 샘플입니다.

## 범위

- 포함: 조종대 왼쪽 하단에 붙는 물리 디스플레이 패널, 6구역 전개도, 구역 연결선, 내구도 색상 범례, 마운트 브래킷, 케이블, 나사 디테일.
- 제외: 수동 운행 UI 전체 화면, 자동/수동 조종 상태 표시, 복구/수리 장치, 실제 내구도 로직, Unity 런타임 연결.

## 기획 기준

- 원본 기획서의 수동 운행 화면 왼쪽 하단 전개도와 구역 내구도 색상 규칙을 조종실 내부에서 확인 가능한 물리 패널로 옮긴 시안입니다.
- 전개도는 조종실, 운송창고, 무기실, 비품실, 동력실, 통제실 6구역을 보여줍니다.
- 색상 범례는 흰색, 노란색, 주황색, 다홍색, 빨간색, 검은색 단계로 표현했습니다.

## Unity 반영 방식

승인되면 조종실 조종대 왼쪽 하단 또는 왼쪽 보조 콘솔 면에 배치합니다. 이 샘플은 모델링 시안이며, 실제 구역 내구도 UI/로직은 별도 런타임 흐름에서 연결합니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_front.png", "정면: 조종대 왼쪽 하단 패널 배치"),
        ("02_player.png", "플레이어 시점: 조종대 옆에서 보이는 전개도"),
        ("03_detail.png", "상세: 6구역 전개도와 내구도 색상"),
        ("04_top.png", "상단: 조종대와 패널 위치 관계"),
        ("05_side.png", "측면: 패널 두께와 마운트 브래킷"),
    ]
    cards = "\n".join(
        f'<figure><a href="renders/{name}"><img src="renders/{name}" alt="{label}"></a><figcaption>{label}</figcaption></figure>'
        for name, label in images
    )
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{SAMPLE_NAME}</title>
  <style>
    body {{ margin: 0; background: #101414; color: #eee9dc; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #cdc5b8; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #39413f; background: #1b2220; padding: 10px; }}
    img {{ width: 100%; display: block; background: #060807; }}
    figcaption {{ margin-top: 8px; color: #ddd4c6; font-size: 14px; }}
    @media (max-width: 820px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>{SAMPLE_NAME}</h1>
  <p>CK-05 화물선 전개도 / 구역 내구도 표시 패널 샘플입니다. 아직 Unity 씬에는 적용하지 않았습니다.</p>
  <p>수동 운행 UI 전체가 아니라, 조종실 내부에서 구역 상태를 확인할 수 있는 물리 패널 형태의 모델링 시안입니다.</p>
  <section class="grid">
    {cards}
  </section>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def main() -> None:
    ensure_dirs()
    reset_scene()
    configure_rendering()

    mats = {
        "glass": material("context front green glass", (0.08, 0.42, 0.36, 0.30), roughness=0.18, alpha=0.30, emission=(0.05, 0.28, 0.22, 1), emission_strength=0.22),
        "frame": worn_metal_material("context dark frame", (0.08, 0.095, 0.09, 1)),
        "console_ghost": material("approved console ghost", (0.11, 0.14, 0.13, 0.36), roughness=0.86, alpha=0.36),
        "floor": material("context dark floor", (0.10, 0.12, 0.11, 1), roughness=0.86),
        "body": worn_metal_material("ck05 worn dark armored body", (0.075, 0.088, 0.083, 1)),
        "frame": worn_metal_material("ck05 heavy black display frame", (0.035, 0.041, 0.039, 1)),
        "mount": worn_metal_material("ck05 black metal mount", (0.05, 0.056, 0.052, 1)),
        "screen": material("ck05 smoky green glass display", (0.025, 0.18, 0.15, 0.92), roughness=0.32, alpha=0.92, emission=(0.01, 0.20, 0.15, 1), emission_strength=0.25),
        "line": material("ck05 bright schematic line", (0.16, 0.82, 0.54, 1), roughness=0.48, emission=(0.06, 0.58, 0.32, 1), emission_strength=0.45),
        "line_dim": material("ck05 dim schematic line", (0.06, 0.34, 0.28, 1), roughness=0.55, emission=(0.02, 0.20, 0.17, 1), emission_strength=0.25),
        "white": material("durability white 100", (0.74, 0.82, 0.78, 1), roughness=0.56),
        "yellow": material("durability yellow 94", (0.82, 0.72, 0.18, 1), roughness=0.62),
        "orange": material("durability orange 79", (0.84, 0.44, 0.10, 1), roughness=0.62),
        "vermilion": material("durability vermilion 49", (0.82, 0.18, 0.08, 1), roughness=0.58),
        "red": material("durability red 19", (0.72, 0.02, 0.03, 1), roughness=0.55, emission=(0.34, 0.0, 0.005, 1), emission_strength=0.22),
        "black": material("durability black 0", (0.003, 0.003, 0.003, 1), roughness=0.9, emission=(0.03, 0.0, 0.0, 1), emission_strength=0.18),
        "text_glow": material("ck05 pale green label text", (0.76, 1.0, 0.78, 1), roughness=0.5, emission=(0.46, 0.9, 0.56, 1), emission_strength=0.55),
        "text_dark": material("ck05 dark room label text", (0.0, 0.008, 0.006, 1), roughness=0.55),
        "text_light": material("ck05 light room label text", (0.88, 0.93, 0.86, 1), roughness=0.55, emission=(0.48, 0.58, 0.44, 1), emission_strength=0.25),
        "worn": material("ck05 exposed worn screw metal", (0.62, 0.61, 0.55, 1), metallic=0.35, roughness=0.62),
        "rubber": material("ck05 aged black cable rubber", (0.005, 0.005, 0.005, 1), roughness=0.96),
    }

    build_context(mats)
    build_map_panel(mats)
    add_lights()

    cameras = [
        ("front", (0.0, -5.2, 1.55), (-0.92, -1.18, 1.18), 42, "01_front.png", None),
        ("player", (-0.55, -3.25, 1.45), (-1.14, -1.28, 1.12), 36, "02_player.png", None),
        ("detail", (-1.15, -2.55, 1.12), (-1.15, -1.40, 1.10), 62, "03_detail.png", 1.62),
        ("top", (0.0, -1.10, 5.1), (-0.65, -1.08, 0.80), 35, "04_top.png", 4.1),
        ("side", (2.7, -2.3, 1.45), (-1.12, -1.22, 1.1), 48, "05_side.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera("ck05 camera " + name, loc, target, lens, ortho_scale)
        render_camera(camera, output)

    export_assets()
    write_docs()


if __name__ == "__main__":
    main()
