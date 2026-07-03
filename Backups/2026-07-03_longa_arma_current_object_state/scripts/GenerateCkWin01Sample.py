from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "ck_win01"
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
TEXTURE_DIR = SAMPLE_ROOT / "textures"

SMP_MODEL_DIR = PROJECT_ROOT / "Assets" / "Sci-Fi Styled Modular Pack" / "Models"
HSK_MESH_DIR = PROJECT_ROOT / "Assets" / "Heavy Station Kit" / "BASE" / "Meshes"
SCREEN_TEX_PATH = PROJECT_ROOT / "Assets" / "_Project" / "Art" / "Props" / "Stage3Rework" / "Textures" / "HD_Stage3_GreenCrtScreen_Albedo.png"


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR, TEXTURE_DIR):
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
    roughness: float = 0.78,
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
        mat.use_screen_refraction = True
        mat.show_transparent_back = True
    if emission is not None:
        set_principled_input(mat, "Emission Color", emission)
        set_principled_input(mat, "Emission Strength", emission_strength)
    mat.diffuse_color = color
    return mat


def textured_emissive_material(
    name: str,
    texture_path: Path,
    fallback_color: tuple[float, float, float, float],
    *,
    emission_strength: float = 1.0,
) -> bpy.types.Material:
    mat = material(
        name,
        fallback_color,
        roughness=0.35,
        emission=fallback_color,
        emission_strength=emission_strength,
    )
    if not texture_path.exists():
        return mat

    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    image = bpy.data.images.load(str(texture_path))
    texture_node = nodes.new(type="ShaderNodeTexImage")
    texture_node.image = image
    mat.node_tree.links.new(texture_node.outputs["Color"], bsdf.inputs["Base Color"])
    if "Emission Color" in bsdf.inputs:
        mat.node_tree.links.new(texture_node.outputs["Color"], bsdf.inputs["Emission Color"])
    return mat


def add_box(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
    bevel_width: float = 0.018,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    obj.parent = parent

    if bevel_width > 0.0:
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
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_empty(name: str) -> bpy.types.Object:
    empty = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(empty)
    return empty


def override_materials(root: bpy.types.Object, mat: bpy.types.Material) -> None:
    for child in root.children_recursive:
        if child.type != "MESH":
            continue
        child.data.materials.clear()
        child.data.materials.append(mat)


def combined_bounds(objects: list[bpy.types.Object]) -> tuple[Vector, Vector] | None:
    mesh_objects = [obj for obj in objects if obj.type == "MESH"]
    if not mesh_objects:
        return None

    min_v = Vector((math.inf, math.inf, math.inf))
    max_v = Vector((-math.inf, -math.inf, -math.inf))
    for obj in mesh_objects:
        for corner in obj.bound_box:
            world_corner = obj.matrix_world @ Vector(corner)
            min_v.x = min(min_v.x, world_corner.x)
            min_v.y = min(min_v.y, world_corner.y)
            min_v.z = min(min_v.z, world_corner.z)
            max_v.x = max(max_v.x, world_corner.x)
            max_v.y = max(max_v.y, world_corner.y)
            max_v.z = max(max_v.z, world_corner.z)
    return min_v, max_v


def import_asset(
    path: Path,
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    target_size: tuple[float, float, float],
    mat: bpy.types.Material | None,
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> bpy.types.Object | None:
    if not path.exists():
        return None

    before = set(bpy.data.objects)
    try:
        bpy.ops.import_scene.fbx(filepath=str(path))
    except Exception:
        return None

    imported = [obj for obj in bpy.data.objects if obj not in before]
    mesh_imports = [obj for obj in imported if obj.type == "MESH"]
    if not mesh_imports:
        for obj in imported:
            bpy.data.objects.remove(obj, do_unlink=True)
        return None

    root = add_empty(name)
    root.parent = parent

    bounds = combined_bounds(mesh_imports)
    if bounds is None:
        return None

    min_v, max_v = bounds
    center = (min_v + max_v) * 0.5
    size = max_v - min_v
    for obj in mesh_imports:
        obj.parent = root
        obj.location -= center
        obj.name = f"{name} mesh"

    scale_values = []
    for source, target in zip((size.x, size.y, size.z), target_size):
        scale_values.append(1.0 if source <= 0.0001 else target / source)
    root.scale = tuple(scale_values)
    root.rotation_euler = rot
    root.location = loc
    if mat is not None:
        override_materials(root, mat)
    return root


def assign_screen_asset_materials(
    root: bpy.types.Object,
    frame_mat: bpy.types.Material,
    screen_mat: bpy.types.Material,
) -> None:
    for child in root.children_recursive:
        if child.type != "MESH":
            continue

        for index, slot in enumerate(child.material_slots):
            source_name = slot.material.name.lower() if slot.material else ""
            slot.material = screen_mat if "screen" in source_name else frame_mat


def add_frame_label_marker(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    mat: bpy.types.Material,
) -> None:
    add_box(name + " plate", parent, loc, (0.52, 0.035, 0.12), mat, bevel_width=0.012)
    add_box(name + " notch", parent, (loc[0], loc[1] - 0.018, loc[2] + 0.085), (0.32, 0.035, 0.035), mat, bevel_width=0.006)


def build_window_sample() -> dict[str, str]:
    metal = material("dark worn frame metal", (0.095, 0.105, 0.105, 1.0), metallic=0.55, roughness=0.67)
    trim = material("edge rubbed metal", (0.45, 0.43, 0.37, 1.0), metallic=0.65, roughness=0.54)
    rubber = material("black sealing rubber", (0.018, 0.019, 0.017, 1.0), roughness=0.93)
    glass = material("thin smoked blue glass", (0.11, 0.30, 0.34, 0.18), roughness=0.14, alpha=0.18)
    screen_body = material("dark screen asset frame", (0.035, 0.041, 0.039, 1.0), metallic=0.45, roughness=0.62)
    screen_surface = textured_emissive_material(
        "green crt internal screen",
        SCREEN_TEX_PATH,
        (0.05, 0.72, 0.45, 1.0),
        emission_strength=1.65,
    )
    glow = material(
        "cold frame light",
        (0.36, 0.72, 0.84, 1.0),
        roughness=0.25,
        emission=(0.36, 0.72, 0.84, 1.0),
        emission_strength=1.25,
    )
    warning = material("small warning paint", (0.86, 0.58, 0.12, 1.0), roughness=0.72)
    ghost = material("approved shell context ghost", (0.18, 0.20, 0.19, 0.28), roughness=0.85, alpha=0.28)

    root = add_empty("ck_win01 - cockpit front window sample")
    context = add_empty("placement context only - not part of window module")
    context.parent = root
    module = add_empty("front glass and frame module")
    module.parent = root

    # Placement context matching the approved cockpit shell scale.
    add_box("floor edge context", context, (0.0, -0.42, 0.02), (10.3, 0.12, 0.04), ghost, bevel_width=0.0)
    add_box("left wall return context", context, (-5.12, -0.05, 1.55), (0.14, 0.74, 3.1), ghost, bevel_width=0.0)
    add_box("right wall return context", context, (5.12, -0.05, 1.55), (0.14, 0.74, 3.1), ghost, bevel_width=0.0)
    add_box("ceiling edge context", context, (0.0, -0.08, 3.22), (10.3, 0.18, 0.18), ghost, bevel_width=0.0)
    add_box("top view main footprint marker", context, (0.0, -0.03, 0.075), (9.95, 0.22, 0.05), warning, bevel_width=0.0)
    add_box(
        "top view left angled footprint marker",
        context,
        (-4.15, 0.15, 0.08),
        (1.5, 0.16, 0.05),
        glow,
        rot=(0.0, 0.0, math.radians(9)),
        bevel_width=0.0,
    )
    add_box(
        "top view right angled footprint marker",
        context,
        (4.15, 0.15, 0.08),
        (1.5, 0.16, 0.05),
        glow,
        rot=(0.0, 0.0, math.radians(-9)),
        bevel_width=0.0,
    )

    # Single forward aperture. No internal mullions so the pilot view stays open.
    pane_z = 1.62
    add_box("single panoramic glass pane", module, (0.0, -0.015, pane_z), (9.05, 0.045, 2.2), glass, bevel_width=0.012)

    # Structural perimeter.
    add_box("bottom crash sill", module, (0.0, -0.02, 0.45), (9.8, 0.34, 0.34), metal, bevel_width=0.035)
    add_box("top armored lintel", module, (0.0, -0.02, 2.78), (9.8, 0.36, 0.36), metal, bevel_width=0.035)
    add_box("left outer post", module, (-4.95, -0.02, 1.62), (0.36, 0.38, 2.64), metal, bevel_width=0.035)
    add_box("right outer post", module, (4.95, -0.02, 1.62), (0.36, 0.38, 2.64), metal, bevel_width=0.035)
    add_box("left angled cheek post", module, (-4.62, 0.06, 1.62), (0.24, 0.32, 2.38), metal, rot=(0.0, 0.0, math.radians(7)), bevel_width=0.03)
    add_box("right angled cheek post", module, (4.62, 0.06, 1.62), (0.24, 0.32, 2.38), metal, rot=(0.0, 0.0, math.radians(-7)), bevel_width=0.03)

    for x in (-3.92, 3.92):
        add_box(f"outer upper clamp block {x:+.2f}", module, (x, -0.24, 2.49), (0.86, 0.24, 0.16), trim, bevel_width=0.02)
        add_box(f"outer lower clamp block {x:+.2f}", module, (x, -0.24, 0.74), (0.86, 0.24, 0.13), trim, bevel_width=0.018)

    # Gaskets and front service seams.
    add_box("black upper continuous gasket", module, (0.0, -0.055, 2.58), (8.9, 0.075, 0.055), rubber, bevel_width=0.006)
    add_box("black lower continuous gasket", module, (0.0, -0.055, 0.66), (8.9, 0.075, 0.055), rubber, bevel_width=0.006)
    add_box("black left side gasket", module, (-4.55, -0.055, 1.62), (0.055, 0.075, 2.03), rubber, bevel_width=0.006)
    add_box("black right side gasket", module, (4.55, -0.055, 1.62), (0.055, 0.075, 2.03), rubber, bevel_width=0.006)

    add_box("top continuous light slot", module, (0.0, -0.31, 2.99), (5.2, 0.05, 0.08), glow, bevel_width=0.012)
    add_box("left amber inspection tag", module, (-4.42, -0.31, 0.78), (0.28, 0.05, 0.18), warning, bevel_width=0.008)
    add_box("right amber inspection tag", module, (4.42, -0.31, 0.78), (0.28, 0.05, 0.18), warning, bevel_width=0.008)

    # Small chips and worn edges, separated from the base frame so they remain visible in renders.
    for index, (x, z) in enumerate(
        [(-4.75, 2.32), (-3.0, 0.54), (-1.2, 2.85), (0.92, 0.50), (2.85, 2.62), (4.62, 1.05)],
        start=1,
    ):
        add_frame_label_marker(f"worn edge patch {index}", module, (x, -0.36, z), trim)

    # Asset-backed pieces, scaled as modular inserts. The procedural frame remains the source of truth.
    used_assets: dict[str, str] = {}
    screen_asset_path = SMP_MODEL_DIR / "big_screen.fbx"
    screen_asset = import_asset(
        screen_asset_path,
        "asset SMP panoramic internal screen",
        module,
        (0.0, 0.085, 1.62),
        (8.75, 0.10, 2.18),
        None,
    )
    if screen_asset is not None:
        assign_screen_asset_materials(screen_asset, screen_body, screen_surface)
        used_assets["Sci-Fi Styled Modular Pack big_screen"] = str(screen_asset_path.relative_to(PROJECT_ROOT)).replace("\\", "/")
    else:
        add_box("fallback panoramic crt screen", module, (0.0, 0.085, 1.62), (8.55, 0.045, 2.04), screen_surface, bevel_width=0.018)

    if SCREEN_TEX_PATH.exists():
        used_assets["Stage3 green CRT screen texture"] = str(SCREEN_TEX_PATH.relative_to(PROJECT_ROOT)).replace("\\", "/")

    candidates = [
        (
            SMP_MODEL_DIR / "light_celing_1.fbx",
            "asset SMP top light insert",
            (0.0, -0.42, 3.08),
            (3.8, 0.08, 0.18),
            glow,
            "Sci-Fi Styled Modular Pack light_celing_1",
        ),
        (
            HSK_MESH_DIR / "Partitions" / "Part_G2.fbx",
            "asset HSK left reinforcement inset",
            (-4.9, -0.42, 1.66),
            (0.38, 0.18, 1.95),
            metal,
            "Heavy Station Kit Part_G2",
        ),
        (
            HSK_MESH_DIR / "Partitions" / "Part_G2.fbx",
            "asset HSK right reinforcement inset",
            (4.9, -0.42, 1.66),
            (0.38, 0.18, 1.95),
            metal,
            "Heavy Station Kit Part_G2",
        ),
    ]
    for path, name, loc, target, mat, label in candidates:
        asset = import_asset(path, name, module, loc, target, mat)
        if asset is not None:
            used_assets[label] = str(path.relative_to(PROJECT_ROOT)).replace("\\", "/")

    # A few cylindrical bolts make scale and assembly direction readable.
    for x in (-4.72, -3.92, -1.85, 1.85, 3.92, 4.72):
        add_cylinder("top bolt", module, (x, -0.39, 2.81), 0.055, 0.035, trim, rot=(math.radians(90), 0.0, 0.0))
        add_cylinder("bottom bolt", module, (x, -0.39, 0.45), 0.052, 0.035, trim, rot=(math.radians(90), 0.0, 0.0))

    return used_assets


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


def set_footprint_marker_render_visible(visible: bool) -> None:
    for obj in bpy.data.objects:
        if "footprint marker" in obj.name:
            obj.hide_render = not visible


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
    world = bpy.data.worlds.new("ck_win01_world")
    world.color = (0.014, 0.016, 0.018)
    scene.world = world
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = -0.05
    scene.view_settings.gamma = 1.0


def add_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -4.0, 5.6))
    key = bpy.context.object
    key.name = "large front inspection light"
    key.data.energy = 520
    key.data.size = 6.5

    bpy.ops.object.light_add(type="POINT", location=(-3.7, -1.5, 1.3))
    left = bpy.context.object
    left.name = "left glass edge sparkle"
    left.data.energy = 65
    left.data.color = (0.45, 0.7, 0.86)

    bpy.ops.object.light_add(type="POINT", location=(3.7, -1.5, 1.3))
    right = bpy.context.object
    right.name = "right glass edge sparkle"
    right.data.energy = 65
    right.data.color = (0.45, 0.7, 0.86)


def export_assets() -> None:
    bpy.context.preferences.filepaths.save_version = 0
    blend_path = BLENDER_DIR / "ck_win01.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "ck_win01.glb"), export_format="GLB")
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "ck_win01.fbx"), use_selection=False)


def write_docs_old(used_assets: dict[str, str]) -> None:
    asset_manifest = {
        "sample": "ck_win01",
        "scope": "조종실 전면 유리창과 프레임 승인용 샘플",
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt: 조종실 앞은 유리로 되어 있어 밖을 볼 수 있는 형태",
            "docs/GAME_DESIGN_SOURCE.txt: 유리창 앞에 조종대 존재. 이번 샘플은 조종대 제외",
            "approved artSample/cockpit_01: 전면 개구부 폭과 전체 조종실 구조",
        ],
        "usedAssetCandidates": used_assets,
        "unityApplicationAllowed": False,
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(
        json.dumps(asset_manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    approval = {
        "sample": "ck_win01",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "scope": "조종실 전면 유리창, 금속 프레임, 세로 멀리언, 상하 보강 프레임, 상부 라이트 슬롯",
        "excluded": ["조종대", "콘솔", "복도 연결", "수동 운행 UI"],
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(
        json.dumps(approval, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    assets_md = "\n".join(f"- `{path}` ({label})" for label, path in used_assets.items())
    if not assets_md:
        assets_md = "- Blender 절차 모델만 사용했습니다. 에셋 import 실패 시 절차 모델로 대체됩니다."

    readme = f"""# ck_win01

조종실 내부 오브젝트 1번, 전면 유리창과 프레임 승인용 샘플입니다.

## 범위

- 포함: 전면 유리판, 두꺼운 외곽 금속 프레임, 세로 멀리언, 상하 보강 프레임, 상부 라이트 슬롯, 고무 가스켓, 작은 마모 패치.
- 제외: 조종대, 조종석 콘솔, 수동 운행 UI, 복도 연결.
- 회색 반투명 구조물은 승인된 조종실 구조에 붙는 위치를 보여주는 배치 기준선이며 실제 창문 부품이 아닙니다.

## 기획 근거

- 원본 기획서 기준 조종실 앞은 유리로 되어 있어 밖을 볼 수 있는 형태입니다.
- 원본 기획서 기준 조종대는 유리창 앞에 존재하지만, 이번 단계에서는 조종대 제작을 제외했습니다.
- 승인된 `cockpit_01` 구조의 전면 개구부에 맞는 폭과 높이를 기준으로 잡았습니다.

## 사용한 에셋 후보

{assets_md}

## 승인 후 Unity 반영 방식

승인되면 이 샘플을 `Approved Cockpit 01 Structure`의 전면 개구부 안쪽에 별도 루트로 배치합니다.
콜라이더는 추가하지 않고 기존 검사용 자유 카메라와 기존 조종실 구조를 유지한 채 시각 모델만 붙입니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_front.png", "정면: 전체 유리창과 프레임"),
        ("02_inside.png", "실내 시점: 조종실 안에서 보는 전면 유리"),
        ("03_diag.png", "대각: 두께와 측면 경사"),
        ("04_top.png", "상단: 전면 개구부 배치 기준"),
        ("05_detail.png", "상세: 프레임, 가스켓, 마모"),
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
  <title>ck_win01</title>
  <style>
    body {{ margin: 0; background: #111514; color: #ece5d8; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #cfc6b8; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #3c4643; background: #1c2220; padding: 10px; }}
    img {{ width: 100%; display: block; background: #050807; }}
    figcaption {{ margin-top: 8px; color: #ddd3c3; font-size: 14px; }}
    code {{ color: #9dd7d3; }}
    @media (max-width: 820px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>ck_win01</h1>
  <p>조종실 전면 유리창과 프레임 승인용 Blender 샘플입니다. 실제 Unity 씬에는 아직 적용하지 않았습니다.</p>
  <p>조종대, 콘솔, 복도 연결은 제외했습니다. 회색 반투명 구조는 전면 개구부에 붙는 위치 기준만 보여줍니다.</p>
  <section class="grid">
    {cards}
  </section>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def write_docs(used_assets: dict[str, str]) -> None:
    asset_manifest = {
        "sample": "ck_win01",
        "scope": "조종실 전면 단일 유리창과 내부 파노라마 화면 승인용 샘플",
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt: 조종실 앞은 유리로 되어 있어 밖을 볼 수 있는 형태",
            "사용자 수정 지시: 5분할 창은 조종 시야를 방해하므로 제거",
            "사용자 수정 지시: 창 안쪽을 내부 화면으로 꽉 차게 구성",
            "approved artSample/cockpit_01: 전면 개구부 폭과 전체 조종실 구조",
        ],
        "usedAssetCandidates": used_assets,
        "unityApplicationAllowed": False,
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(
        json.dumps(asset_manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    approval = {
        "sample": "ck_win01",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "scope": "조종실 전면 단일 유리, 외곽 금속 프레임, 내부 파노라마 화면, 상하 보강 프레임, 상단 라이트 슬롯",
        "excluded": ["조종대", "콘솔", "복도 연결", "수동 운행 UI 로직"],
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(
        json.dumps(approval, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    assets_md = "\n".join(f"- `{path}` ({label})" for label, path in used_assets.items())
    if not assets_md:
        assets_md = "- Blender 절차 모델만 사용했습니다. 에셋 import 실패 시 절차 모델로 대체됩니다."

    readme = f"""# ck_win01

조종실 내부 오브젝트 1번, 전면 단일 유리창과 내부 파노라마 화면 승인용 샘플입니다.

## 범위

- 포함: 넓은 단일 전면 유리, 두꺼운 외곽 금속 프레임, 내부 파노라마 화면, 상하 보강 프레임, 상단 라이트 슬롯, 고무 가스켓, 작은 마모 패치.
- 제외: 조종대, 조종석 콘솔, 수동 운행 UI 로직, 복도 연결.
- 5분할 유리판과 중앙 세로 멀리언은 제거했습니다. 조종 시야 중앙을 가리는 구조물을 두지 않는 방향입니다.
- 회색 반투명 구조물은 승인된 조종실 구조에 붙는 위치를 보여주는 배치 기준선이며 실제 창문 부품이 아닙니다.

## 기획 및 수정 근거

- 원본 기획서 기준 조종실 앞은 유리로 되어 있어 밖을 볼 수 있는 형태입니다.
- 사용자 수정 지시에 따라 5분할 창은 제거하고, 창 안쪽 대부분을 내부 화면이 채우도록 조정했습니다.
- 승인된 `cockpit_01` 구조의 전면 개구부에 맞는 폭과 높이를 기준으로 잡았습니다.

## 사용한 에셋 후보

{assets_md}

## 승인 후 Unity 반영 방식

승인되면 이 샘플을 `Approved Cockpit 01 Structure`의 전면 개구부 안쪽에 별도 루트로 배치합니다.
콜라이더와 조종 로직은 추가하지 않고, 기존 검사용 자유 카메라와 비활성화된 튜토리얼 상태를 유지한 채 시각 모델만 붙입니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_front.png", "정면: 단일 유리와 내부 화면"),
        ("02_inside.png", "실내 시점: 조종 시야를 막지 않는 화면 구성"),
        ("03_diag.png", "대각: 유리, 화면, 외곽 프레임 두께"),
        ("04_top.png", "상단: 전면 개구부 배치 기준"),
        ("05_detail.png", "상세: 외곽 프레임, 가스켓, 마모"),
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
  <title>ck_win01</title>
  <style>
    body {{ margin: 0; background: #111514; color: #ece5d8; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #cfc6b8; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #3c4643; background: #1c2220; padding: 10px; }}
    img {{ width: 100%; display: block; background: #050807; }}
    figcaption {{ margin-top: 8px; color: #ddd3c3; font-size: 14px; }}
    code {{ color: #9dd7d3; }}
    @media (max-width: 820px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>ck_win01</h1>
  <p>조종실 전면 단일 유리창과 내부 파노라마 화면 승인용 Blender 샘플입니다. 실제 Unity 씬에는 아직 적용하지 않았습니다.</p>
  <p>5분할 창과 중앙 멀리언을 제거했고, 조종대, 콘솔, 복도 연결은 제외했습니다. 회색 반투명 구조는 전면 개구부에 붙는 배치 기준만 보여줍니다.</p>
  <section class="grid">
    {cards}
  </section>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def copy_preview_textures() -> None:
    candidates = [
        PROJECT_ROOT / "Assets" / "Sci-Fi Styled Modular Pack" / "Textures" / "window" / "window_big_albedo.png",
        PROJECT_ROOT / "Assets" / "ScifiOfficeLite" / "Meshes" / "Textures" / "Environment" / "Wall texture" / "Wall set 2" / "Wall_Multiset_2_Normal.png",
        SCREEN_TEX_PATH,
    ]
    for path in candidates:
        if path.exists():
            shutil.copy2(path, TEXTURE_DIR / path.name)


def main() -> None:
    ensure_dirs()
    reset_scene()
    configure_rendering()
    used_assets = build_window_sample()
    add_lights()

    cameras = [
        ("front", (0.0, -11.0, 1.85), (0.0, 0.0, 1.6), 34, "01_front.png", None),
        ("inside", (0.0, -5.6, 1.65), (0.0, 0.05, 1.62), 30, "02_inside.png", None),
        ("diag", (6.8, -5.3, 3.55), (0.0, -0.02, 1.55), 38, "03_diag.png", None),
        ("top", (0.0, -0.1, 8.8), (0.0, -0.1, 0.0), 40, "04_top.png", 7.2),
        ("detail", (-5.25, -2.35, 1.12), (-4.35, -0.25, 0.82), 54, "05_detail.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        set_footprint_marker_render_visible(output == "04_top.png")
        render_camera(add_camera("cam_" + name, loc, target, lens, ortho_scale), output)
    set_footprint_marker_render_visible(True)

    copy_preview_textures()
    export_assets()
    write_docs(used_assets)
    print("ck_win01 sample generated: " + str(SAMPLE_ROOT))


if __name__ == "__main__":
    main()
