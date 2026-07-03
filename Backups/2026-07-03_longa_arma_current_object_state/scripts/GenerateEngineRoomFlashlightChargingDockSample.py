from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "engine_room_flashlight_charging_dock"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR):
        path.mkdir(parents=True, exist_ok=True)


def clean_generated_files() -> None:
    for directory in (BLENDER_DIR, RENDER_DIR, EXPORT_DIR):
        if not directory.exists():
            continue

        for item in directory.iterdir():
            if item.is_file():
                item.unlink()
            elif item.is_dir():
                shutil.rmtree(item)


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
    alpha: float = 1.0,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    set_principled_input(mat, "Base Color", color)
    set_principled_input(mat, "Metallic", metallic)
    set_principled_input(mat, "Roughness", roughness)
    if alpha < 1.0:
        set_principled_input(mat, "Alpha", alpha)
        mat.blend_method = "BLEND"
    mat.diffuse_color = (color[0], color[1], color[2], alpha)
    return mat


def noisy_metal(name: str, base: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = material(name, base, metallic=0.32, roughness=0.86)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 44
    noise.inputs["Detail"].default_value = 9
    noise.inputs["Roughness"].default_value = 0.60
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.22
    ramp.color_ramp.elements[0].color = (base[0] * 0.45, base[1] * 0.45, base[2] * 0.45, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.34, 1),
        min(base[1] * 1.34, 1),
        min(base[2] * 1.34, 1),
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
    bevel_width: float = 0.01,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    obj.parent = parent
    if bevel_width > 0:
        bevel = obj.modifiers.new("hard surface bevel", "BEVEL")
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
    vertices: int = 32,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
    bpy.ops.object.shade_smooth()
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_torus(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    major_radius: float,
    minor_radius: float,
    mat: bpy.types.Material,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_segments=48,
        minor_segments=10,
        major_radius=major_radius,
        minor_radius=minor_radius,
        location=loc,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
    bpy.ops.object.shade_smooth()
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_bolt(parent: bpy.types.Object, name: str, x: float, z: float, mat: bpy.types.Material) -> None:
    add_cylinder(name, parent, (x, -0.246, z), 0.026, 0.017, mat, (math.radians(90), 0, 0), 18)
    add_box(f"{name} slot", parent, (x, -0.257, z), (0.040, 0.006, 0.007), mat, bevel_width=0.001)


def add_flashlight(
    parent: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    center_x: float,
    y: float,
    z_bottom: float,
    *,
    name_prefix: str,
) -> None:
    body_height = 1.28
    body_radius = 0.145
    body_center_z = z_bottom + body_height * 0.5
    add_cylinder(f"{name_prefix} vertical flashlight rubberized body", parent, (center_x, y, body_center_z), body_radius, body_height, mats["flashlight_body"], vertices=40)

    add_cylinder(f"{name_prefix} flashlight knurled lower grip ring", parent, (center_x, y, z_bottom + 0.250), 0.158, 0.055, mats["flashlight_ring"], vertices=40)
    add_cylinder(f"{name_prefix} flashlight knurled upper grip ring", parent, (center_x, y, z_bottom + 0.620), 0.158, 0.055, mats["flashlight_ring"], vertices=40)
    add_cylinder(f"{name_prefix} flashlight tail contact cap", parent, (center_x, y, z_bottom - 0.035), 0.150, 0.070, mats["contact"], vertices=36)

    head_z = z_bottom + body_height + 0.130
    add_cylinder(f"{name_prefix} slightly wider flashlight head", parent, (center_x, y, head_z), 0.205, 0.260, mats["flashlight_head"], vertices=48)
    add_cylinder(f"{name_prefix} recessed front lens", parent, (center_x, y, head_z + 0.140), 0.152, 0.026, mats["lens"], vertices=40)


def add_screen_reference(parent: bpy.types.Object, mats: dict[str, bpy.types.Material], center_x: float) -> None:
    add_box("ER-09 screen right-side placement reference wall patch", parent, (center_x, 0.060, 1.130), (1.85, 0.120, 1.42), mats["wall"], bevel_width=0.008)
    add_box("ER-09 screen reference outer frame", parent, (center_x, -0.075, 1.130), (1.64, 0.105, 1.12), mats["screen_frame"], bevel_width=0.020)
    add_box("ER-09 screen reference dark display surface", parent, (center_x, -0.148, 1.130), (1.36, 0.030, 0.82), mats["screen_dark"], bevel_width=0.012)
    add_box("ER-09 screen reference lower mounting rail", parent, (center_x, -0.155, 0.430), (1.74, 0.070, 0.080), mats["rail"], bevel_width=0.007)


def add_charging_dock(
    parent: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    center_x: float,
    *,
    mode: str,
    include_screen_reference: bool,
) -> bpy.types.Object:
    root = add_empty(f"ER-15 flashlight dock {mode}", parent)
    add_box(f"{mode} dock wall placement proxy", root, (center_x, 0.065, 1.060), (1.05, 0.135, 2.26), mats["wall"], bevel_width=0.008)
    add_box(f"{mode} armored charging dock backplate", root, (center_x, -0.050, 1.060), (0.82, 0.115, 2.04), mats["backplate"], bevel_width=0.020)
    add_box(f"{mode} dark vertical flashlight size recess", root, (center_x, -0.124, 1.030), (0.430, 0.050, 1.610), mats["recess"], bevel_width=0.035)

    add_box(f"{mode} left raised cradle rail", root, (center_x - 0.265, -0.205, 1.040), (0.080, 0.105, 1.620), mats["rail"], bevel_width=0.014)
    add_box(f"{mode} right raised cradle rail", root, (center_x + 0.265, -0.205, 1.040), (0.080, 0.105, 1.620), mats["rail"], bevel_width=0.014)
    add_box(f"{mode} lower receiving cup block", root, (center_x, -0.214, 0.185), (0.540, 0.115, 0.185), mats["cup"], bevel_width=0.020)
    add_torus(f"{mode} lower rounded flashlight heel cradle", root, (center_x, -0.222, 0.295), 0.220, 0.028, mats["cup"])

    add_torus(f"{mode} upper passive retaining collar", root, (center_x, -0.233, 1.640), 0.228, 0.023, mats["clamp"])
    add_torus(f"{mode} lower passive retaining collar", root, (center_x, -0.233, 0.700), 0.228, 0.023, mats["clamp"])

    add_box(f"{mode} rear copper contact strip left", root, (center_x - 0.083, -0.258, 0.275), (0.072, 0.018, 0.225), mats["contact"], bevel_width=0.004)
    add_box(f"{mode} rear copper contact strip right", root, (center_x + 0.083, -0.258, 0.275), (0.072, 0.018, 0.225), mats["contact"], bevel_width=0.004)
    add_cylinder(f"{mode} spring loaded lower contact pin left", root, (center_x - 0.083, -0.292, 0.145), 0.024, 0.030, mats["contact"], (math.radians(90), 0, 0), 20)
    add_cylinder(f"{mode} spring loaded lower contact pin right", root, (center_x + 0.083, -0.292, 0.145), 0.024, 0.030, mats["contact"], (math.radians(90), 0, 0), 20)

    for sx in (-1, 1):
        for sz in (-1, 1):
            add_bolt(root, f"{mode} dock corner bolt", center_x + sx * 0.350, 1.060 + sz * 0.885, mats["bolt"])

    scratches = [
        (-0.270, 1.720, 0.115, 0.017, -8),
        (0.260, 1.250, 0.130, 0.016, 11),
        (0.220, 0.490, 0.100, 0.014, -14),
    ]
    for index, (x, z, sx, sz, angle) in enumerate(scratches, start=1):
        add_box(
            f"{mode} dock chipped edge {index}",
            root,
            (center_x + x, -0.271, z),
            (sx, 0.010, sz),
            mats["wear"],
            (0, 0, math.radians(angle)),
            bevel_width=0.001,
        )

    if mode == "inserted":
        add_flashlight(root, mats, center_x, -0.342, 0.360, name_prefix=mode)
    elif mode == "removed":
        add_flashlight(root, mats, center_x + 0.515, -0.405, 0.360, name_prefix=mode)

    if include_screen_reference:
        add_screen_reference(root, mats, center_x + 1.420)

    return root


def build_sample(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("ER-15 vertical flashlight charging dock sample")
    add_charging_dock(root, mats, -4.650, mode="screen left placement", include_screen_reference=True)
    add_charging_dock(root, mats, -1.400, mode="empty", include_screen_reference=False)
    add_charging_dock(root, mats, 1.400, mode="inserted", include_screen_reference=False)
    add_charging_dock(root, mats, 4.200, mode="removed", include_screen_reference=False)


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 56
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("EngineRoomFlashlightChargingDockWorld")
    scene.world.color = (0.010, 0.012, 0.011)
    scene.render.resolution_x = 1500
    scene.render.resolution_y = 950
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -5.4, 4.1))
    key = bpy.context.object
    key.name = "large soft front inspection light"
    key.data.energy = 500
    key.data.size = 6.8
    key.data.color = (0.92, 0.97, 0.92)

    bpy.ops.object.light_add(type="AREA", location=(-5.2, -2.4, 2.2))
    fill = bpy.context.object
    fill.name = "cool fill for charging dock rails"
    fill.data.energy = 135
    fill.data.size = 4.4
    fill.data.color = (0.62, 0.78, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(1.4, -1.7, 0.45))
    contact = bpy.context.object
    contact.name = "small contact inspection glint"
    contact.data.energy = 35
    contact.data.color = (1.0, 0.72, 0.35)


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
    camera.name = "engine room flashlight charging dock camera " + name
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


def export_assets() -> None:
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "engine_room_flashlight_charging_dock.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "engine_room_flashlight_charging_dock.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "engine_room_flashlight_charging_dock.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-15",
        "title": "동력실 손전등 세로 충전 홈 샘플",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt:181 - 손전등 배터리는 동력실에서 F 상호작용으로 충전 가능합니다.",
            "사용자 확인: 스크린 왼쪽에 손전등 크기에 맞는 홈을 생성합니다.",
            "사용자 확인: 손전등을 세로로 끼우고, 넣었다 빼면 바로 풀충전됩니다.",
            "사용자 확인: 상태등과 안내 표식은 넣지 않습니다.",
            "사용자 확인: 충전 중 전기 이펙트는 추후 별도 구현합니다.",
        ],
        "generatedFiles": [
            "blender/engine_room_flashlight_charging_dock.blend",
            "exports/engine_room_flashlight_charging_dock.fbx",
            "exports/engine_room_flashlight_charging_dock.glb",
            "renders/01_screen_left_placement.png",
            "renders/02_empty_vertical_slot.png",
            "renders/03_flashlight_inserted.png",
            "renders/04_flashlight_removed_after_charge.png",
            "renders/05_side_mount_depth.png",
            "renders/06_contact_detail.png",
        ],
        "includedParts": [
            "ER-09 스크린 왼쪽 벽면 배치 기준",
            "손전등을 세로로 끼우는 크기의 어두운 충전 홈",
            "좌우 레일, 하단 받침 컵, 상하 수동 고정 링",
            "손전등 하단 접점과 맞물리는 구리 접점",
            "삽입 상태와 탈착 상태 확인용 손전등 프록시",
            "얕은 벽면 장착 깊이",
        ],
        "excludedParts": [
            "상태등",
            "안내 표식",
            "충전 중 전기 이펙트",
            "실제 손전등 배터리 풀충전 로직",
            "Unity 씬, 프리팹, 런타임 자산 연결",
            "상호작용 프롬프트와 입력 처리",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-15",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산 또는 UI 흐름에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# engine_room_flashlight_charging_dock

ER-15 손전등 충전 앵커의 승인용 Blender 샘플입니다.

## 목적

동력실 ER-09 스크린 왼쪽 벽면에 붙일 손전등 충전 홈을 검토하기 위한 샘플입니다.  
손전등을 세로로 홈에 끼운 뒤 다시 빼면 바로 풀충전되는 구조를 전제로 하며, 실제 충전 로직은 포함하지 않습니다.

## 사용자 확인 사양

- 스크린 왼쪽에 배치합니다.
- 손전등 크기에 맞는 세로 홈입니다.
- 손전등을 넣었다 빼면 바로 풀충전됩니다.
- 상태등과 안내 표식은 넣지 않습니다.
- 충전 중 전기 이펙트는 추후 별도로 구현합니다.

## 포함

- 스크린 왼쪽 벽면 배치 기준
- 세로 손전등 수납 홈
- 좌우 레일, 하단 받침 컵, 상하 수동 고정 링
- 손전등 하단 접점과 맞물리는 구리 접점
- 삽입 상태와 탈착 상태 확인용 손전등 프록시
- 얕은 벽면 장착 깊이

## 제외

- 상태등
- 안내 표식
- 충전 중 전기 이펙트
- 실제 손전등 배터리 풀충전 로직
- Unity 씬, 프리팹, 런타임 자산 연결
- 상호작용 프롬프트와 입력 처리
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_screen_left_placement.png", "01 ER-09 스크린 왼쪽 벽면 배치 기준"),
        ("02_empty_vertical_slot.png", "02 손전등을 세로로 끼우는 빈 충전 홈"),
        ("03_flashlight_inserted.png", "03 손전등 삽입 상태"),
        ("04_flashlight_removed_after_charge.png", "04 손전등 탈착 상태"),
        ("05_side_mount_depth.png", "05 얕은 벽면 장착 깊이"),
        ("06_contact_detail.png", "06 하단 구리 접점 세부"),
    ]
    cards = "\n".join(
        f'    <figure><a href="renders/{name}"><img src="renders/{name}" alt="{label}"></a><figcaption>{label}</figcaption></figure>'
        for name, label in images
    )
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>engine_room_flashlight_charging_dock review</title>
  <style>
    body {{ margin: 0; background: #151817; color: #e8e1d2; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #c8c0af; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #3e453f; background: #202521; padding: 10px; }}
    img {{ width: 100%; display: block; background: #0c0f0e; }}
    figcaption {{ margin-top: 8px; color: #d9cfba; font-size: 14px; }}
    @media (max-width: 800px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>engine_room_flashlight_charging_dock</h1>
  <p>ER-15 손전등 충전 홈 샘플입니다. 스크린 왼쪽 벽면에 붙는 세로 홈, 손전등 삽입 상태, 탈착 상태, 하단 접점 구조를 분리해 볼 수 있게 구성했습니다. 상태등과 안내 표식, 충전 중 전기 이펙트는 포함하지 않았습니다. 사용자 승인 전에는 Unity 씬이나 런타임 자산에 연결하지 않습니다.</p>
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
    clean_generated_files()
    reset_scene()
    configure_rendering()

    mats = {
        "wall": noisy_metal("engine room wall behind flashlight dock", (0.17, 0.20, 0.18, 1)),
        "backplate": noisy_metal("dark armored flashlight charging dock backplate", (0.13, 0.15, 0.14, 1)),
        "recess": material("deep black flashlight sized vertical recess", (0.006, 0.007, 0.007, 1), roughness=0.88),
        "rail": noisy_metal("worn charging dock side rail", (0.24, 0.26, 0.22, 1)),
        "cup": noisy_metal("lower flashlight receiving cup", (0.19, 0.21, 0.19, 1)),
        "clamp": noisy_metal("passive flashlight retaining collar", (0.07, 0.08, 0.075, 1)),
        "contact": material("brushed copper charging contact", (0.83, 0.48, 0.20, 1), metallic=0.65, roughness=0.38),
        "bolt": noisy_metal("small dock recessed bolt heads", (0.40, 0.39, 0.32, 1)),
        "wear": material("scraped exposed dock metal", (0.68, 0.66, 0.56, 1), metallic=0.42, roughness=0.55),
        "flashlight_body": noisy_metal("matte black flashlight body", (0.025, 0.028, 0.026, 1)),
        "flashlight_head": noisy_metal("dark gunmetal flashlight head", (0.09, 0.10, 0.095, 1)),
        "flashlight_ring": noisy_metal("flashlight grip ring", (0.15, 0.16, 0.145, 1)),
        "lens": material("inactive flashlight glass lens", (0.30, 0.40, 0.45, 1), metallic=0.0, roughness=0.20, alpha=0.78),
        "screen_frame": noisy_metal("ER-09 reference screen frame", (0.20, 0.23, 0.20, 1)),
        "screen_dark": material("ER-09 reference dark screen surface", (0.006, 0.011, 0.010, 1), roughness=0.36),
    }

    build_sample(mats)
    add_render_lights()

    cameras = [
        ("screen_left_placement", (-3.950, -4.9, 1.12), (-3.400, -0.16, 1.10), 48, "01_screen_left_placement.png", 2.55),
        ("empty_vertical_slot", (-1.400, -4.4, 1.05), (-1.400, -0.16, 1.04), 58, "02_empty_vertical_slot.png", None),
        ("flashlight_inserted", (1.400, -4.4, 1.05), (1.400, -0.16, 1.04), 58, "03_flashlight_inserted.png", None),
        ("flashlight_removed_after_charge", (4.345, -4.5, 1.05), (4.360, -0.18, 1.04), 58, "04_flashlight_removed_after_charge.png", None),
        ("side_mount_depth", (2.310, -2.15, 1.10), (1.400, -0.14, 1.04), 54, "05_side_mount_depth.png", None),
        ("contact_detail", (1.400, -2.25, 0.340), (1.400, -0.24, 0.260), 88, "06_contact_detail.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
