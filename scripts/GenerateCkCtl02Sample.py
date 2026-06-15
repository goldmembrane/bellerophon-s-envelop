from __future__ import annotations

import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SCRIPT_ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
LOW_BODY_VARIANT = "--low-body" in SCRIPT_ARGS
SAMPLE_NAME = "ck_ctl02_low" if LOW_BODY_VARIANT else "ck_ctl02"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"

SMP_MODEL_DIR = PROJECT_ROOT / "Assets" / "Sci-Fi Styled Modular Pack" / "Models"
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
    mat = material(name, base, metallic=0.25, roughness=0.86)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 28
    noise.inputs["Detail"].default_value = 9
    noise.inputs["Roughness"].default_value = 0.58
    color_ramp = nodes.new(type="ShaderNodeValToRGB")
    color_ramp.color_ramp.elements[0].position = 0.24
    color_ramp.color_ramp.elements[0].color = (base[0] * 0.55, base[1] * 0.55, base[2] * 0.55, 1)
    color_ramp.color_ramp.elements[1].position = 1.0
    color_ramp.color_ramp.elements[1].color = (min(base[0] * 1.35, 1), min(base[1] * 1.35, 1), min(base[2] * 1.35, 1), 1)
    links.new(noise.outputs["Fac"], color_ramp.inputs["Fac"])
    links.new(color_ramp.outputs["Color"], bsdf.inputs["Base Color"])
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
    bevel_width: float = 0.025,
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
    vertices: int = 24,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_torus(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    major_radius: float,
    minor_radius: float,
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
    major_segments: int = 96,
    minor_segments: int = 12,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_segments=major_segments,
        minor_segments=minor_segments,
        major_radius=major_radius,
        minor_radius=minor_radius,
        location=loc,
        rotation=rot,
    )
    obj = bpy.context.object
    obj.name = name
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
    segments: int = 18,
    ring_count: int = 9,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=ring_count, radius=radius, location=loc)
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
    vertices: int = 18,
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


def add_text(
    name: str,
    parent: bpy.types.Object,
    text: str,
    loc: tuple[float, float, float],
    size: float,
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (math.radians(72), 0.0, 0.0),
) -> bpy.types.Object:
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.003
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


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


def override_materials(root: bpy.types.Object, mat: bpy.types.Material) -> None:
    for child in root.children_recursive:
        if child.type == "MESH":
            child.data.materials.clear()
            child.data.materials.append(mat)


def import_asset(
    path: Path,
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    target_size: tuple[float, float, float],
    mat: bpy.types.Material,
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
        if source > 0.0001 and target > 0.0001:
            scale_values.append(target / source)
    uniform_scale = min(scale_values) if scale_values else 1.0

    root.location = loc
    root.rotation_euler = rot
    root.scale = (uniform_scale, uniform_scale, uniform_scale)
    override_materials(root, mat)
    return root


def build_context(mats: dict[str, bpy.types.Material]) -> bpy.types.Object:
    root = add_empty("CK-02 placement context - cockpit front only")

    add_box("cockpit floor footprint proxy", root, (0, 0.15, -0.06), (8.8, 5.0, 0.12), mats["floor"], bevel_width=0.01)
    add_box("front broad window screen proxy", root, (0, 2.95, 1.85), (8.9, 0.08, 2.18), mats["glass"], bevel_width=0.02)
    add_box("front lower sill proxy", root, (0, 2.88, 0.64), (9.2, 0.18, 0.28), mats["frame"], bevel_width=0.02)
    add_box("front upper frame proxy", root, (0, 2.88, 3.03), (9.2, 0.18, 0.26), mats["frame"], bevel_width=0.02)
    add_box("left cockpit wall proxy", root, (-4.55, 0.25, 1.45), (0.18, 4.9, 2.9), mats["wall"], bevel_width=0.015)
    add_box("right cockpit wall proxy", root, (4.55, 0.25, 1.45), (0.18, 4.9, 2.9), mats["wall"], bevel_width=0.015)
    add_box("player clearance marker", root, (0, -1.85, 0.015), (3.4, 0.12, 0.03), mats["amber"], bevel_width=0.005)
    add_box("console front anchor marker", root, (0, 1.44, 0.02), (4.8, 0.08, 0.04), mats["cyan"], bevel_width=0.004)
    return root


def add_edge_wear(parent: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    chip_specs = [
        (-2.15, 1.10, 1.28, 0.42),
        (-1.15, 1.07, 1.30, 0.30),
        (1.42, 1.08, 1.29, 0.36),
        (2.25, 1.05, 1.27, 0.28),
        (-2.65, 0.03, 0.75, 0.22),
        (2.67, 0.02, 0.76, 0.24),
        (0.0, -0.36, 1.04, 0.34),
    ]
    for index, (x, y, z, width) in enumerate(chip_specs, start=1):
        add_box(
            f"paint worn bright edge chip {index}",
            parent,
            (x, y, z),
            (width, 0.018, 0.025),
            mats["wear"],
            rot=(math.radians(-12), 0.0, 0.0),
            bevel_width=0.004,
        )


def build_console(mats: dict[str, bpy.types.Material]) -> dict[str, str]:
    root = add_empty("CK-02 main cockpit console sample")
    used_assets: dict[str, str] = {}
    body_drop = 0.24 if LOW_BODY_VARIANT else 0.0
    body_height_cut = 0.30 if LOW_BODY_VARIANT else 0.0
    body_base_z = 0.42 - body_drop * 0.60
    body_base_height = 0.82 - body_height_cut
    deck_z = 0.96 - body_drop
    coaming_z = 1.12 - body_drop
    hand_rest_z = 0.88 - body_drop
    kick_plate_z = 0.34 - body_drop * 0.75
    kick_plate_height = 0.42 - body_height_cut * 0.65
    cheek_z = 0.72 - body_drop * 0.75
    cheek_height = 0.76 - body_height_cut * 0.55
    panel_drop = body_drop * 0.75

    # Main body: low, broad, and set back from the front glass so the window remains readable.
    add_box("single-piece heavy lower console base", root, (0, 0.45, body_base_z), (4.95, 1.36, body_base_height), mats["body"], bevel_width=0.06)
    add_box("sloped main control deck", root, (0, 0.62, deck_z), (4.72, 1.08, 0.20 if LOW_BODY_VARIANT else 0.24), mats["body"], rot=(math.radians(-14), 0.0, 0.0), bevel_width=0.045)
    add_box("raised rear instrument coaming", root, (0, 1.15, coaming_z), (4.35, 0.32, 0.27 if LOW_BODY_VARIANT else 0.34), mats["frame"], rot=(math.radians(-7), 0.0, 0.0), bevel_width=0.04)
    add_box("recessed black hand rest strip", root, (0, -0.05, hand_rest_z), (4.35, 0.22, 0.10 if LOW_BODY_VARIANT else 0.12), mats["rubber"], rot=(math.radians(-14), 0.0, 0.0), bevel_width=0.025)
    add_box("front armored lower kick plate", root, (0, -0.24, kick_plate_z), (4.65, 0.16, kick_plate_height), mats["dark"], bevel_width=0.025)
    add_box("left angled side cheek", root, (-2.66, 0.46, cheek_z), (0.28, 1.08, cheek_height), mats["frame"], rot=(0.0, 0.0, math.radians(-5)), bevel_width=0.035)
    add_box("right angled side cheek", root, (2.66, 0.46, cheek_z), (0.28, 1.08, cheek_height), mats["frame"], rot=(0.0, 0.0, math.radians(5)), bevel_width=0.035)

    # CK-01 already provides the broad central screen. CK-02 is only the physical control desk.
    add_box("central blank armored control plate", root, (0, 0.77, 1.13 - panel_drop), (1.42, 0.06, 0.36 if LOW_BODY_VARIANT else 0.44), mats["panel"], rot=(math.radians(68), 0.0, 0.0), bevel_width=0.025)
    add_box("left mechanical switch panel", root, (-1.56, 0.66, 1.05 - panel_drop), (1.04, 0.055, 0.34 if LOW_BODY_VARIANT else 0.42), mats["panel"], rot=(math.radians(64), 0.0, math.radians(-7)), bevel_width=0.025)
    add_box("right mechanical switch panel", root, (1.56, 0.66, 1.05 - panel_drop), (1.04, 0.055, 0.34 if LOW_BODY_VARIANT else 0.42), mats["panel"], rot=(math.radians(64), 0.0, math.radians(7)), bevel_width=0.025)
    add_box("left caution label strip", root, (-1.56, 0.54, 1.21 - panel_drop), (0.88, 0.024, 0.055), mats["amber"], rot=(math.radians(64), 0.0, math.radians(-7)), bevel_width=0.006)
    add_box("right caution label strip", root, (1.56, 0.54, 1.21 - panel_drop), (0.88, 0.024, 0.055), mats["amber"], rot=(math.radians(64), 0.0, math.radians(7)), bevel_width=0.006)

    for side, base_x, z_rot in (("left", -1.56, -7), ("right", 1.56, 7)):
        for row, z in enumerate((1.04 - panel_drop, 1.13 - panel_drop), start=1):
            for col, x_offset in enumerate((-0.30, -0.10, 0.10, 0.30), start=1):
                add_box(
                    f"{side} toggle switch {row}-{col}",
                    root,
                    (base_x + x_offset, 0.45, z),
                    (0.045, 0.035, 0.12),
                    mats["dark"],
                    rot=(math.radians(54), 0.0, math.radians(z_rot)),
                    bevel_width=0.006,
                )
        for col, x_offset in enumerate((-0.34, -0.17, 0.0, 0.17, 0.34), start=1):
            led_mat = mats["green_led"] if col % 2 else mats["red"]
            add_cylinder(
                f"{side} small indicator light {col}",
                root,
                (base_x + x_offset, 0.43, 0.91 - panel_drop),
                0.028,
                0.014,
                led_mat,
                rot=(math.radians(64), 0.0, math.radians(z_rot)),
                vertices=16,
            )

    for index, x in enumerate((-0.48, -0.24, 0.0, 0.24, 0.48), start=1):
        add_cylinder(
            f"central physical gauge bezel {index}",
            root,
            (x, 0.52, 1.09 - panel_drop),
            0.085,
            0.018,
            mats["dark"],
            rot=(math.radians(68), 0.0, 0.0),
            vertices=24,
        )
        add_cylinder(
            f"central analog gauge face {index}",
            root,
            (x, 0.505, 1.09 - panel_drop),
            0.060,
            0.012,
            mats["label"],
            rot=(math.radians(68), 0.0, 0.0),
            vertices=24,
        )

    # Manual flight controls: a ship-like helm wheel plus a forward push lever on the right.
    helm_center = (0.0, -0.02, 1.84)
    add_box("helm reinforced mounting foot", root, (0.0, -0.02, 1.06 - panel_drop), (0.52, 0.28, 0.13 if LOW_BODY_VARIANT else 0.16), mats["frame"], bevel_width=0.025)
    add_cylinder_between("helm angled support strut", root, (0.0, 0.02, 1.14 - panel_drop), helm_center, 0.055, mats["dark"], vertices=20)
    add_cylinder("helm bearing housing", root, helm_center, 0.16, 0.12, mats["frame"], rot=(math.radians(90), 0.0, 0.0), vertices=28)
    add_cylinder("helm worn brass hub cap", root, (0.0, -0.095, helm_center[2]), 0.105, 0.035, mats["brass"], rot=(math.radians(90), 0.0, 0.0), vertices=28)
    add_torus("large ship helm wheel ring", root, helm_center, 0.47, 0.035, mats["brass"], rot=(math.radians(90), 0.0, 0.0), major_segments=112, minor_segments=12)

    for index in range(8):
        angle = (math.tau / 8.0) * index + math.radians(22.5)
        inner = (
            helm_center[0] + math.cos(angle) * 0.14,
            helm_center[1],
            helm_center[2] + math.sin(angle) * 0.14,
        )
        outer = (
            helm_center[0] + math.cos(angle) * 0.47,
            helm_center[1],
            helm_center[2] + math.sin(angle) * 0.47,
        )
        knob = (
            helm_center[0] + math.cos(angle) * 0.56,
            helm_center[1],
            helm_center[2] + math.sin(angle) * 0.56,
        )
        add_cylinder_between(f"helm radial spoke {index + 1}", root, inner, outer, 0.021, mats["brass"], vertices=12)
        add_sphere(f"helm outer hand knob {index + 1}", root, knob, 0.055, mats["brass"], segments=16, ring_count=8)

    lever_track_loc = (1.82, 0.12, 1.08 - panel_drop)
    add_box("right forward lever recessed slot", root, lever_track_loc, (0.28, 1.18, 0.085), mats["dark"], rot=(math.radians(-14), 0.0, 0.0), bevel_width=0.018)
    add_box("right forward lever left rail", root, (1.66, 0.12, 1.12 - panel_drop), (0.035, 1.06, 0.08), mats["wear"], rot=(math.radians(-14), 0.0, 0.0), bevel_width=0.006)
    add_box("right forward lever right rail", root, (1.98, 0.12, 1.12 - panel_drop), (0.035, 1.06, 0.08), mats["wear"], rot=(math.radians(-14), 0.0, 0.0), bevel_width=0.006)
    for index, y in enumerate((-0.33, -0.08, 0.17, 0.42, 0.67), start=1):
        add_box(
            f"right lever travel notch {index}",
            root,
            (2.13, y, 1.13 - panel_drop),
            (0.15, 0.018, 0.055),
            mats["label"],
            rot=(math.radians(-14), 0.0, 0.0),
            bevel_width=0.004,
        )

    lever_start = (1.82, -0.28, 1.16 - panel_drop)
    lever_end = (1.82, 0.48, 1.58 - panel_drop * 0.35)
    add_cylinder("right forward lever pivot drum", root, lever_start, 0.12, 0.34, mats["frame"], rot=(0.0, math.radians(90), 0.0), vertices=24)
    add_cylinder_between("right forward push lever shaft", root, lever_start, lever_end, 0.045, mats["dark"], vertices=18)
    add_cylinder("right forward lever horizontal grip", root, lever_end, 0.08, 0.48, mats["rubber"], rot=(0.0, math.radians(90), 0.0), vertices=22)
    add_box("right lever forward stop block", root, (1.82, 0.78, 1.16 - panel_drop), (0.42, 0.10, 0.12), mats["red"], rot=(math.radians(-14), 0.0, 0.0), bevel_width=0.014)

    # Interaction anchor marker for later Unity work.
    add_box("F interaction anchor plate", root, (0.0, -0.47, 0.80 - panel_drop), (0.42, 0.07, 0.15 if LOW_BODY_VARIANT else 0.18), mats["amber"], rot=(math.radians(-6), 0.0, 0.0), bevel_width=0.018)
    add_text("F interaction letter marker", root, "F", (0.0, -0.515, 0.84 - panel_drop), 0.15, mats["text"], rot=(math.radians(84), 0.0, 0.0))

    # Mounting feet make the object feel like a heavy cargo-ship console rather than a floating desk.
    for index, x in enumerate((-1.95, -0.65, 0.65, 1.95), start=1):
        add_box(f"floor bolted console foot {index}", root, (x, -0.10, 0.10), (0.42, 0.36, 0.20), mats["dark"], bevel_width=0.02)
        add_cylinder(f"foot bolt pair {index} A", root, (x - 0.12, -0.21, 0.225), 0.025, 0.014, mats["wear"], vertices=12)
        add_cylinder(f"foot bolt pair {index} B", root, (x + 0.12, -0.21, 0.225), 0.025, 0.014, mats["wear"], vertices=12)

    add_edge_wear(root, mats)

    # Optional pilot seat is ghosted scale context, not part of CK-02.
    seat = import_asset(
        SMP_MODEL_DIR / "pilot_seat.fbx",
        "context asset pilot_seat scale reference only",
        root,
        (0.0, -1.45, 0.44),
        (1.05, 1.05, 1.05),
        mats["seat_context"],
        rot=(0.0, 0.0, 0.0),
    )
    if seat is not None:
        used_assets["scale context seat"] = str(SMP_MODEL_DIR / "pilot_seat.fbx")

    return used_assets


def set_context_render_visible(visible: bool) -> None:
    context = bpy.data.objects.get("CK-02 placement context - cockpit front only")
    if context is None:
        return

    context.hide_render = not visible
    for child in context.children_recursive:
        child.hide_render = not visible


def add_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -2.8, 5.8))
    key = bpy.context.object
    key.name = "large cockpit console key light"
    key.data.energy = 560
    key.data.size = 6.5

    bpy.ops.object.light_add(type="POINT", location=(-2.7, 1.7, 2.3))
    left = bpy.context.object
    left.name = "green console spill light"
    left.data.energy = 95
    left.data.color = (0.3, 0.9, 0.58)

    bpy.ops.object.light_add(type="POINT", location=(2.9, -0.9, 1.7))
    warm = bpy.context.object
    warm.name = "warm edge inspection light"
    warm.data.energy = 72
    warm.data.color = (1.0, 0.72, 0.42)


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


def render_camera(camera: bpy.types.Object, output_name: str, *, show_context: bool = True) -> None:
    set_context_render_visible(show_context)
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.filepath = str(RENDER_DIR / output_name)
    bpy.ops.render.render(write_still=True)


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
    scene.world = bpy.data.worlds.new("CK02World")
    scene.world.color = (0.018, 0.021, 0.022)
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0
    scene.view_settings.gamma = 1


def export_assets() -> None:
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / (SAMPLE_NAME + ".blend")))
    set_context_render_visible(True)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / (SAMPLE_NAME + ".glb")), export_format="GLB")
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / (SAMPLE_NAME + ".fbx")), use_selection=False)


def write_docs(used_assets: dict[str, str]) -> None:
    approval_state = "승인"
    unity_allowed = True
    variant_note = (
        "수정안: Unity 배치 위치는 유지하고 조종대 본체의 세로 높이와 전면 하단 두께를 이전 수정안보다 조금 더 낮춘 버전입니다. "
        "타륜 중심 높이는 유지했습니다."
        if LOW_BODY_VARIANT
        else "승인 완료된 샘플입니다."
    )
    asset_manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "CK-02",
        "scope": "조종실 메인 조종대 낮은 본체 수정안" if LOW_BODY_VARIANT else "조종실 메인 조종대 샘플",
        "sourceBasis": [
            "docs/COCKPIT_OBJECTS.md: CK-02 메인 조종대",
            "docs/GAME_DESIGN_SOURCE.txt:115 조종실 전면 유리와 수리 장치",
            "docs/GAME_DESIGN_SOURCE.txt:116 유리창 앞 조종대와 F 상호작용",
            "docs/MVP_IMPLEMENTATION_ORDER.md:98 전면 유리, 조종대, 연결 방향 표시",
            "docs/MVP_IMPLEMENTATION_ORDER.md:138 조종대 F 상호작용으로 수동 운행 모드 진입",
        ],
        "usedAssetCandidates": used_assets,
        "unityApplicationAllowed": unity_allowed,
        "approvalState": approval_state,
        "screenPolicy": "CK-01 front panoramic screen is the main cockpit screen. CK-02 does not include a central monitor.",
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(
        json.dumps(asset_manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    approval = {
        "sample": SAMPLE_NAME,
        "approvalState": approval_state,
        "unityApplicationAllowed": unity_allowed,
        "requiresUserApprovalBeforeUnity": LOW_BODY_VARIANT,
        "included": [
            "메인 조종대 본체",
            "좌우 보조 콘솔",
            "물리 스위치와 아날로그 게이지",
            "타륜형 조작 핸들",
            "오른쪽 전진 레버",
            "F 상호작용 앵커 시각 표시",
            "전면 창문 앞 배치 기준",
        ],
        "excluded": [
            "중앙/전면 대형 화면",
            "수동 운행 UI 런타임 로직",
            "실제 F 상호작용 컴포넌트",
            "조종실 복도 연결",
            "조종대 파손 상태",
        ],
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(
        json.dumps(approval, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    assets_md = "\n".join(f"- `{path}` ({label})" for label, path in used_assets.items())
    if not assets_md:
        assets_md = "- Blender 절차 모델만 사용했습니다."

    readme = f"""# {SAMPLE_NAME}

{variant_note}

CK-02 메인 조종대 승인용 Blender 샘플입니다.

## 범위

- 포함: 조종대 본체, 좌우 보조 콘솔, 물리 스위치와 아날로그 게이지, 타륜형 조작 핸들, 오른쪽 전진 레버, F 상호작용 앵커 표시, 전면 창문 앞 배치 기준.
- 제외: 중앙/전면 대형 화면, 수동 운행 UI 런타임 로직, 실제 상호작용 컴포넌트, 복도 연결, 파손 상태.
- 전면 창문과 조종실 벽은 배치 확인용 프록시입니다. CK-02 승인 대상은 조종대입니다.
- 중앙 화면 역할은 이미 승인된 CK-01 전면 스크린이 담당하므로 CK-02에는 별도 중앙 모니터를 넣지 않았습니다.
- 화물선을 직접 운행하는 느낌을 강화하기 위해 조종대 상단 중앙에 타륜형 핸들을 두고, 조종대 오른쪽에는 앞으로 밀 수 있는 막대형 전진 레버를 배치했습니다.

## 배치 기준

- 조종대는 전면 유리창 바로 앞이 아니라 약간 뒤로 물려 배치합니다.
- 플레이어가 조종대 뒤쪽에서 접근할 수 있도록 바닥에 여유 공간을 남깁니다.
- F 상호작용 앵커는 조종대 뒤쪽 중앙 하단에 표시했습니다.

## 사용 에셋 후보

{assets_md}

## Unity 반영 방식

Unity 반영 시에는 `Approved Cockpit 01 Structure` 내부, `Approved Cockpit 01 Window` 앞쪽 넓은 면 기준으로 배치하고, 기존 튜토리얼/운행 로직은 건드리지 않은 채 시각 모델부터 붙입니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_front.png", "전면 창문과 조종대 배치"),
        ("02_player.png", "플레이어 접근 방향"),
        ("03_side.png", "측면 높이와 깊이"),
        ("04_top.png", "상단 배치 기준"),
        ("05_detail.png", "타륜과 전진 레버 상세"),
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
    code {{ color: #9bdad1; }}
    @media (max-width: 820px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>{SAMPLE_NAME}</h1>
  <p>CK-02 메인 조종대 {'낮은 본체 승인 샘플' if LOW_BODY_VARIANT else '승인용 Blender 샘플'}입니다. 현재 승인된 기준 샘플입니다.</p>
  <p>전면 창문과 벽은 배치 확인용 프록시이며, 승인 대상은 화면이 없는 조종대 본체, 타륜형 조작 핸들, 오른쪽 전진 레버입니다.</p>
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
        "wall": material("context muted cockpit wall", (0.24, 0.31, 0.32, 0.52), roughness=0.78, alpha=0.52),
        "floor": material("context dark floor", (0.10, 0.12, 0.11, 1), roughness=0.86),
        "glass": material("context front green glass", (0.08, 0.42, 0.36, 0.30), roughness=0.18, alpha=0.30, emission=(0.05, 0.28, 0.22, 1), emission_strength=0.25),
        "frame": worn_metal_material("context window dark frame", (0.08, 0.095, 0.09, 1)),
        "body": worn_metal_material("worn dark console body", (0.12, 0.15, 0.145, 1)),
        "frame": worn_metal_material("dark ribbed console frame", (0.065, 0.075, 0.072, 1)),
        "dark": material("matte black rubberized metal", (0.015, 0.016, 0.015, 1), roughness=0.94),
        "rubber": material("aged black rubber", (0.006, 0.006, 0.005, 1), roughness=0.96),
        "panel": worn_metal_material("scratched dark mechanical panel", (0.07, 0.082, 0.078, 1)),
        "label": material("aged ivory gauge face", (0.78, 0.73, 0.62, 1), roughness=0.68),
        "green_led": material("small green indicator lamp", (0.10, 0.85, 0.46, 1), roughness=0.38, emission=(0.06, 0.62, 0.30, 1), emission_strength=0.35),
        "brass": worn_metal_material("worn dull brass helm metal", (0.56, 0.42, 0.20, 1)),
        "amber": material("amber interaction marker", (0.95, 0.62, 0.16, 1), roughness=0.55, emission=(0.95, 0.48, 0.08, 1), emission_strength=0.45),
        "cyan": material("cyan placement marker", (0.18, 0.78, 0.86, 1), roughness=0.5, emission=(0.08, 0.55, 0.65, 1), emission_strength=0.35),
        "red": material("red thumb switch", (0.85, 0.06, 0.035, 1), roughness=0.48, emission=(0.65, 0.02, 0.01, 1), emission_strength=0.22),
        "text": material("white interaction text", (0.92, 0.88, 0.72, 1), roughness=0.5, emission=(0.8, 0.68, 0.42, 1), emission_strength=0.25),
        "wear": material("exposed scraped steel", (0.66, 0.64, 0.57, 1), metallic=0.35, roughness=0.62),
        "seat_context": material("transparent seat scale context", (0.16, 0.18, 0.18, 0.34), roughness=0.8, alpha=0.34),
    }

    build_context(mats)
    used_assets = build_console(mats)
    add_lights()

    cameras = [
        ("front", (0.0, -5.8, 2.42), (0.0, 0.82, 1.66), 36, "01_front.png", None, True),
        ("player", (0.0, -3.35, 1.64), (0.0, 0.62, 1.46), 32, "02_player.png", None, True),
        ("side", (5.5, -1.5, 2.0), (0.1, 0.55, 1.0), 42, "03_side.png", None, True),
        ("top", (0.0, 0.25, 7.4), (0.0, 0.25, 0.0), 50, "04_top.png", 6.2, True),
        ("detail", (-1.72, -2.28, 2.20), (0.52, 0.08, 1.68), 48, "05_detail.png", None, False),
    ]
    for name, loc, target, lens, output, ortho_scale, show_context in cameras:
        render_camera(add_camera("cam_" + name, loc, target, lens, ortho_scale), output, show_context=show_context)

    set_context_render_visible(True)
    export_assets()
    write_docs(used_assets)
    print(SAMPLE_NAME + " sample generated: " + str(SAMPLE_ROOT))


if __name__ == "__main__":
    main()
