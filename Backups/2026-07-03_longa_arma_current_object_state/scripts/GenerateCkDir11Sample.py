from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "ck_dir11"
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
    mat = material(name, base, metallic=0.28, roughness=0.88)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 30
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


def add_text(
    name: str,
    parent: bpy.types.Object,
    text: str,
    loc: tuple[float, float, float],
    size: float,
    mat: bpy.types.Material,
    rot: tuple[float, float, float],
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


def add_sign(
    root: bpy.types.Object,
    name: str,
    label: str,
    arrow: str,
    loc: tuple[float, float, float],
    rot: tuple[float, float, float],
    width: float,
    mats: dict[str, bpy.types.Material],
    accent: bpy.types.Material,
) -> None:
    add_box(name + " rear mounting rail", root, (loc[0], loc[1] + 0.012, loc[2]), (width + 0.18, 0.045, 0.34), mats["mount"], rot, 0.018)
    add_box(name + " black armored frame", root, loc, (width, 0.045, 0.28), mats["frame"], rot, 0.022)
    add_box(name + " luminous label face", root, (loc[0], loc[1] - 0.03, loc[2]), (width - 0.12, 0.018, 0.19), accent, rot, 0.012)

    text_rot = (math.radians(90), 0.0, rot[2])
    add_text(name + " arrow text", root, arrow, (loc[0] - width * 0.34, loc[1] - 0.047, loc[2] + 0.004), 0.13, mats["text_dark"], text_rot)
    add_text(name + " room label text", root, label, (loc[0] + width * 0.08, loc[1] - 0.047, loc[2] + 0.004), 0.075, mats["text_dark"], text_rot)

    add_cylinder(name + " left bolt", root, (loc[0] - width * 0.43, loc[1] - 0.045, loc[2] + 0.12), 0.025, 0.014, mats["worn"], (math.radians(90), 0, 0), 16)
    add_cylinder(name + " right bolt", root, (loc[0] + width * 0.43, loc[1] - 0.045, loc[2] + 0.12), 0.025, 0.014, mats["worn"], (math.radians(90), 0, 0), 16)
    add_cylinder(name + " lower left bolt", root, (loc[0] - width * 0.43, loc[1] - 0.045, loc[2] - 0.12), 0.025, 0.014, mats["worn"], (math.radians(90), 0, 0), 16)
    add_cylinder(name + " lower right bolt", root, (loc[0] + width * 0.43, loc[1] - 0.045, loc[2] - 0.12), 0.025, 0.014, mats["worn"], (math.radians(90), 0, 0), 16)


def build_context(mats: dict[str, bpy.types.Material]) -> None:
    context = add_empty("CK-11 cockpit placement context")

    add_box("front panoramic screen proxy", context, (0, 0.42, 1.78), (4.9, 0.05, 1.24), mats["glass"], bevel_width=0.018)
    add_box("front screen frame top", context, (0, 0.38, 2.45), (5.1, 0.09, 0.09), mats["frame"], bevel_width=0.01)
    add_box("front screen frame bottom", context, (0, 0.38, 1.10), (5.1, 0.09, 0.09), mats["frame"], bevel_width=0.01)
    add_box("left cockpit wall proxy", context, (-2.55, -0.82, 1.25), (0.08, 2.95, 2.2), mats["wall"], bevel_width=0.006)
    add_box("right cockpit wall proxy", context, (2.55, -0.82, 1.25), (0.08, 2.95, 2.2), mats["wall"], bevel_width=0.006)
    add_box("rear cargo threshold proxy", context, (0.0, -2.42, 1.18), (1.55, 0.09, 1.24), mats["rear_wall"], bevel_width=0.01)
    add_box("player floor clearance proxy", context, (0, -1.05, 0.02), (5.2, 3.7, 0.04), mats["floor"], bevel_width=0)
    add_box("approved console body ghost", context, (0, -0.76, 0.62), (3.8, 1.0, 0.56), mats["console_ghost"], bevel_width=0.035)
    add_box("approved console top ghost", context, (0, -1.0, 0.94), (3.5, 0.64, 0.09), mats["console_ghost"], bevel_width=0.025)


def build_direction_set(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("CK-11 cockpit direction labels sample")

    add_sign(
        root,
        "left engine room direction sign",
        "ENGINE",
        "<",
        (-1.75, -0.34, 1.76),
        (0.0, 0.0, 0.0),
        1.18,
        mats,
        mats["engine"],
    )
    add_sign(
        root,
        "right control room direction sign",
        "CONTROL",
        ">",
        (1.75, -0.34, 1.76),
        (0.0, 0.0, 0.0),
        1.26,
        mats,
        mats["control"],
    )
    add_sign(
        root,
        "rear cargo hold direction sign",
        "CARGO HOLD",
        "v",
        (0.0, -2.34, 1.72),
        (0.0, 0.0, 0.0),
        1.46,
        mats,
        mats["cargo"],
    )

    add_box("left wall vertical engine arrow stripe", root, (-2.48, -0.56, 1.30), (0.026, 0.03, 0.74), mats["engine"], bevel_width=0.004)
    add_box("left wall engine stripe arrow head top", root, (-2.48, -0.56, 1.72), (0.026, 0.03, 0.16), mats["engine"], rot=(0, 0, math.radians(34)), bevel_width=0.004)
    add_box("right wall vertical control arrow stripe", root, (2.48, -0.56, 1.30), (0.026, 0.03, 0.74), mats["control"], bevel_width=0.004)
    add_box("right wall control stripe arrow head top", root, (2.48, -0.56, 1.72), (0.026, 0.03, 0.16), mats["control"], rot=(0, 0, math.radians(-34)), bevel_width=0.004)

    add_box("rear floor cargo arrow shaft", root, (0.0, -1.92, 0.055), (0.18, 0.72, 0.02), mats["cargo_dim"], bevel_width=0.003)
    add_box("rear floor cargo arrow head left", root, (-0.10, -2.30, 0.06), (0.16, 0.26, 0.02), mats["cargo_dim"], rot=(0, 0, math.radians(28)), bevel_width=0.003)
    add_box("rear floor cargo arrow head right", root, (0.10, -2.30, 0.06), (0.16, 0.26, 0.02), mats["cargo_dim"], rot=(0, 0, math.radians(-28)), bevel_width=0.003)

    add_box("ceiling route label rail", root, (0.0, -1.15, 2.42), (3.8, 0.08, 0.08), mats["mount"], bevel_width=0.012)
    add_text("ceiling route hint text", root, "L: ENGINE     R: CONTROL     REAR: CARGO", (0.0, -1.22, 2.35), 0.075, mats["text_glow"], (math.radians(75), 0, 0))


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
    world = bpy.data.worlds.new("ck_dir11_world")
    world.color = (0.012, 0.015, 0.016)
    scene.world = world
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = -0.05
    scene.view_settings.gamma = 1


def add_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -3.8, 4.2))
    key = bpy.context.object
    key.name = "ck11 direction sample large softbox"
    key.data.energy = 430
    key.data.size = 5.4

    bpy.ops.object.light_add(type="POINT", location=(-1.7, -0.9, 1.85))
    left = bpy.context.object
    left.name = "engine sign green spill"
    left.data.energy = 36
    left.data.color = (0.18, 0.84, 0.58)

    bpy.ops.object.light_add(type="POINT", location=(1.7, -0.9, 1.85))
    right = bpy.context.object
    right.name = "control sign amber spill"
    right.data.energy = 34
    right.data.color = (1.0, 0.58, 0.22)

    bpy.ops.object.light_add(type="POINT", location=(0.0, -2.2, 1.75))
    rear = bpy.context.object
    rear.name = "cargo sign blue spill"
    rear.data.energy = 30
    rear.data.color = (0.25, 0.58, 1.0)


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
        "objectId": "CK-11",
        "scope": "Cockpit physical direction labels and room route signs sample.",
        "sourceBasis": [
            "docs/COCKPIT_OBJECTS.md: CK-11 방향 표시 / 방 라벨",
            "docs/GAME_DESIGN_SOURCE.txt:115 조종실 왼쪽은 동력실, 오른쪽은 통제실, 뒤쪽은 운송창고 방향",
            "docs/MVP_IMPLEMENTATION_ORDER.md:98 조종실은 동력실/통제실/운송창고 연결 방향을 알아볼 수 있게 만든다",
        ],
        "included": [
            "왼쪽 동력실 방향 표식",
            "오른쪽 통제실 방향 표식",
            "뒤쪽 운송창고 방향 표식",
            "벽면 보조 화살표",
            "후방 바닥 화살표",
            "천장 루트 라벨 레일",
        ],
        "excluded": [
            "복도 본체 모델링",
            "문/출입구 기능",
            "상호작용 로직",
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

    readme = """# ck_dir11

CK-11 방향 표시 / 방 라벨 샘플입니다.

## 범위

- 포함: 왼쪽 동력실, 오른쪽 통제실, 뒤쪽 운송창고 방향을 알리는 물리 표식과 보조 화살표.
- 제외: 복도 본체 모델링, 문/출입구 기능, 상호작용 로직, Unity 런타임 연결.

## 기획 기준

- 원본 기획서 기준으로 조종실 왼쪽은 동력실, 오른쪽은 통제실, 뒤쪽은 운송창고 방향입니다.
- 실제 복도는 아직 연결하지 않고, 조종실 내부에서 방향만 알아볼 수 있는 표식으로 제작했습니다.

## Unity 반영 방식

승인되면 조종실 내부 벽면과 후방 바닥/천장 쪽에 표식만 배치합니다. 복도 연결 구조나 문 기능은 포함하지 않습니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_front.png", "정면: 좌우 방향 표식과 전면 조종실 맥락"),
        ("02_player.png", "플레이어 시점: 조종대 뒤에서 보는 방향 표식"),
        ("03_rear.png", "후방: 운송창고 방향 표식"),
        ("04_top.png", "상단: 좌/우/후방 표식 배치"),
        ("05_detail.png", "상세: 표식 프레임과 라벨"),
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
  <p>CK-11 방향 표시 / 방 라벨 샘플입니다. 아직 Unity 씬에는 적용하지 않았습니다.</p>
  <p>조종실 내부에서 왼쪽 동력실, 오른쪽 통제실, 뒤쪽 운송창고 방향을 알아볼 수 있는 물리 표식 시안입니다.</p>
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
        "glass": material("context front green glass", (0.08, 0.42, 0.36, 0.30), roughness=0.18, alpha=0.30, emission=(0.05, 0.28, 0.22, 1), emission_strength=0.18),
        "wall": material("context side wall muted", (0.17, 0.21, 0.20, 0.55), roughness=0.88, alpha=0.55),
        "rear_wall": material("context rear cargo threshold muted", (0.16, 0.18, 0.18, 0.62), roughness=0.88, alpha=0.62),
        "floor": material("context dark floor", (0.10, 0.12, 0.11, 1), roughness=0.86),
        "console_ghost": material("approved console ghost", (0.11, 0.14, 0.13, 0.34), roughness=0.86, alpha=0.34),
        "frame": worn_metal_material("ck11 worn black sign frame", (0.045, 0.052, 0.05, 1)),
        "mount": worn_metal_material("ck11 dark mounting rail", (0.06, 0.068, 0.064, 1)),
        "engine": material("ck11 engine green sign face", (0.16, 0.80, 0.52, 1), roughness=0.48, emission=(0.06, 0.52, 0.30, 1), emission_strength=0.55),
        "control": material("ck11 control amber sign face", (0.95, 0.55, 0.18, 1), roughness=0.52, emission=(0.62, 0.30, 0.06, 1), emission_strength=0.45),
        "cargo": material("ck11 cargo blue sign face", (0.22, 0.56, 0.95, 1), roughness=0.50, emission=(0.08, 0.32, 0.68, 1), emission_strength=0.52),
        "cargo_dim": material("ck11 dim blue floor arrow", (0.08, 0.28, 0.52, 1), roughness=0.72, emission=(0.02, 0.12, 0.28, 1), emission_strength=0.20),
        "text_dark": material("ck11 dark sign text", (0.0, 0.01, 0.008, 1), roughness=0.55),
        "text_glow": material("ck11 pale route rail text", (0.75, 0.96, 0.78, 1), roughness=0.5, emission=(0.44, 0.75, 0.48, 1), emission_strength=0.35),
        "worn": material("ck11 exposed bolt metal", (0.64, 0.62, 0.54, 1), metallic=0.35, roughness=0.62),
    }

    build_context(mats)
    build_direction_set(mats)
    add_lights()

    cameras = [
        ("front", (0.0, -5.3, 1.75), (0.0, -0.62, 1.58), 38, "01_front.png", None),
        ("player", (0.0, -4.15, 1.82), (0.0, -0.55, 1.55), 26, "02_player.png", None),
        ("rear", (0.0, -4.35, 1.55), (0.0, -2.23, 1.45), 42, "03_rear.png", None),
        ("top", (0.0, -1.05, 5.2), (0.0, -0.95, 0.85), 35, "04_top.png", 5.4),
        ("detail", (1.45, -2.25, 1.82), (1.75, -0.39, 1.74), 56, "05_detail.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera("ck11 camera " + name, loc, target, lens, ortho_scale)
        render_camera(camera, output)

    export_assets()
    write_docs()


if __name__ == "__main__":
    main()
