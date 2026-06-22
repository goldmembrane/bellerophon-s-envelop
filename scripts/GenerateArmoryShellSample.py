from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "armory_shell"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"

ROOM_WIDTH = 8.4
ROOM_NORTH_Y = 3.55
ROOM_SOUTH_Y = -4.75
ROOM_DEPTH = ROOM_NORTH_Y - ROOM_SOUTH_Y
ROOM_CENTER_Y = (ROOM_NORTH_Y + ROOM_SOUTH_Y) * 0.5
ROOM_HEIGHT = 3.05
FLOOR_THICKNESS = 0.18
WALL_THICKNESS = 0.34
DOOR_WIDTH = 1.55
DOOR_HEIGHT = 2.08
CONTROL_DOOR_X = 0.0
SUPPLY_DOOR_Y = -0.35
CARGO_DOOR_Y = -2.10


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR):
        path.mkdir(parents=True, exist_ok=True)


def clean_generated_files() -> None:
    for directory in (BLENDER_DIR, RENDER_DIR, EXPORT_DIR):
        if directory.exists():
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
    roughness: float = 0.8,
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


def noisy_metal(name: str, base: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = material(name, base, metallic=0.24, roughness=0.86)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 30
    noise.inputs["Detail"].default_value = 8
    noise.inputs["Roughness"].default_value = 0.56
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[0].color = (base[0] * 0.48, base[1] * 0.48, base[2] * 0.48, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.36, 1),
        min(base[1] * 1.36, 1),
        min(base[2] * 1.36, 1),
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
    bevel_width: float = 0.016,
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
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_torus(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
    major_radius: float = 0.22,
    minor_radius: float = 0.025,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_segments=48,
        minor_segments=8,
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


def add_text_label(
    name: str,
    parent: bpy.types.Object,
    text: str,
    loc: tuple[float, float, float],
    rot: tuple[float, float, float],
    mat: bpy.types.Material,
    size: float = 0.20,
) -> bpy.types.Object:
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.006
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def add_wall_with_door_y(
    root: bpy.types.Object,
    name: str,
    y: float,
    door_x: float,
    mats: dict[str, bpy.types.Material],
) -> None:
    left_width = door_x + ROOM_WIDTH * 0.5 - DOOR_WIDTH * 0.5
    right_width = ROOM_WIDTH * 0.5 - door_x - DOOR_WIDTH * 0.5
    z_mid = ROOM_HEIGHT * 0.5
    if left_width > 0.05:
        add_box(f"{name} left sealed wall", root, (-ROOM_WIDTH * 0.5 + left_width * 0.5, y, z_mid), (left_width, WALL_THICKNESS, ROOM_HEIGHT), mats["wall"])
    if right_width > 0.05:
        add_box(f"{name} right sealed wall", root, (door_x + DOOR_WIDTH * 0.5 + right_width * 0.5, y, z_mid), (right_width, WALL_THICKNESS, ROOM_HEIGHT), mats["wall"])
    header_height = ROOM_HEIGHT - DOOR_HEIGHT
    add_box(f"{name} doorway upper header", root, (door_x, y, DOOR_HEIGHT + header_height * 0.5), (DOOR_WIDTH, WALL_THICKNESS, header_height), mats["wall"])
    add_box(f"{name} doorway left frame", root, (door_x - DOOR_WIDTH * 0.5, y, DOOR_HEIGHT * 0.5), (0.18, WALL_THICKNESS + 0.12, DOOR_HEIGHT), mats["door_frame"])
    add_box(f"{name} doorway right frame", root, (door_x + DOOR_WIDTH * 0.5, y, DOOR_HEIGHT * 0.5), (0.18, WALL_THICKNESS + 0.12, DOOR_HEIGHT), mats["door_frame"])


def add_wall_with_door_x(
    root: bpy.types.Object,
    name: str,
    x: float,
    door_y: float,
    mats: dict[str, bpy.types.Material],
) -> None:
    lower_depth = door_y - ROOM_SOUTH_Y - DOOR_WIDTH * 0.5
    upper_depth = ROOM_NORTH_Y - door_y - DOOR_WIDTH * 0.5
    z_mid = ROOM_HEIGHT * 0.5
    if lower_depth > 0.05:
        add_box(f"{name} lower sealed wall", root, (x, ROOM_SOUTH_Y + lower_depth * 0.5, z_mid), (WALL_THICKNESS, lower_depth, ROOM_HEIGHT), mats["wall"])
    if upper_depth > 0.05:
        add_box(f"{name} upper sealed wall", root, (x, door_y + DOOR_WIDTH * 0.5 + upper_depth * 0.5, z_mid), (WALL_THICKNESS, upper_depth, ROOM_HEIGHT), mats["wall"])
    header_height = ROOM_HEIGHT - DOOR_HEIGHT
    add_box(f"{name} doorway upper header", root, (x, door_y, DOOR_HEIGHT + header_height * 0.5), (WALL_THICKNESS, DOOR_WIDTH, header_height), mats["wall"])
    add_box(f"{name} doorway lower frame", root, (x, door_y - DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.12, 0.18, DOOR_HEIGHT), mats["door_frame"])
    add_box(f"{name} doorway upper frame", root, (x, door_y + DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.12, 0.18, DOOR_HEIGHT), mats["door_frame"])


def add_plain_wall_x(root: bpy.types.Object, name: str, x: float, mats: dict[str, bpy.types.Material]) -> None:
    add_box(name, root, (x, ROOM_CENTER_Y, ROOM_HEIGHT * 0.5), (WALL_THICKNESS, ROOM_DEPTH + WALL_THICKNESS, ROOM_HEIGHT), mats["wall"])


def add_double_door_wall_x(
    root: bpy.types.Object,
    name: str,
    x: float,
    door_centers: list[tuple[str, float]],
    mats: dict[str, bpy.types.Material],
) -> None:
    intervals = sorted((center - DOOR_WIDTH * 0.5, center + DOOR_WIDTH * 0.5, label) for label, center in door_centers)
    cursor = ROOM_SOUTH_Y
    z_mid = ROOM_HEIGHT * 0.5
    for index, (start, end, _label) in enumerate(intervals, start=1):
        if start > cursor:
            depth = start - cursor
            add_box(f"{name} sealed wall segment {index}", root, (x, cursor + depth * 0.5, z_mid), (WALL_THICKNESS, depth, ROOM_HEIGHT), mats["wall"])
        cursor = end
    if cursor < ROOM_NORTH_Y:
        depth = ROOM_NORTH_Y - cursor
        add_box(f"{name} sealed final wall segment", root, (x, cursor + depth * 0.5, z_mid), (WALL_THICKNESS, depth, ROOM_HEIGHT), mats["wall"])

    header_height = ROOM_HEIGHT - DOOR_HEIGHT
    for label, center in door_centers:
        add_box(f"{name} {label} doorway upper header", root, (x, center, DOOR_HEIGHT + header_height * 0.5), (WALL_THICKNESS, DOOR_WIDTH, header_height), mats["wall"])
        add_box(f"{name} {label} doorway lower frame", root, (x, center - DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.12, 0.18, DOOR_HEIGHT), mats["door_frame"])
        add_box(f"{name} {label} doorway upper frame", root, (x, center + DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.12, 0.18, DOOR_HEIGHT), mats["door_frame"])


def add_floor_grid(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    for x in (-3.25, -1.95, -0.65, 0.65, 1.95, 3.25):
        add_box(f"armory removable floor plate {x:+.2f}", root, (x, ROOM_CENTER_Y, 0.115), (1.02, ROOM_DEPTH - 0.82, 0.042), mats["floor_panel"], (0, 0, 0), 0.006)
    for y in (-4.05, -3.05, -2.05, -1.05, -0.05, 0.95, 1.95, 2.95):
        add_box(f"armory transverse deck rib {y:+.2f}", root, (0, y, 0.150), (ROOM_WIDTH - 0.74, 0.038, 0.046), mats["deck_rib"], (0, 0, 0), 0.002)


def add_curved_screen_placeholder(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    segments = 32
    half_width = 3.42
    z_bottom = 0.88
    z_top = 2.64
    wall_y = ROOM_NORTH_Y - WALL_THICKNESS * 0.95
    curve_depth = 1.18
    verts: list[tuple[float, float, float]] = []

    def curve_point(u: float, z: float) -> tuple[float, float, float]:
        x = half_width * u
        # Lower Y is toward the room interior. This makes the center recessed and both ends wrap forward.
        y = wall_y - curve_depth * (u * u)
        return (x, y, z)

    for z in (z_bottom, z_top):
        for i in range(segments + 1):
            u = -1.0 + 2.0 * i / segments
            verts.append(curve_point(u, z))

    faces = []
    row = segments + 1
    for i in range(segments):
        faces.append((i, i + 1, row + i + 1, row + i))

    mesh = bpy.data.meshes.new("armory curved screen placeholder mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("AR-06 placeholder curved forward view screen surface", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats["screen"])
    obj.parent = root
    solidify = obj.modifiers.new("thin curved screen body", "SOLIDIFY")
    solidify.thickness = 0.035
    obj.modifiers.new("screen weighted normals", "WEIGHTED_NORMAL")

    for rail_name, z in (("upper", z_top + 0.10), ("lower", z_bottom - 0.10)):
        for i in range(segments):
            start_u = -1.0 + 2.0 * i / segments
            end_u = -1.0 + 2.0 * (i + 1) / segments
            add_cylinder_between(
                f"AR-06 visibly curved screen {rail_name} rail {i + 1:02d}",
                root,
                curve_point(start_u, z),
                curve_point(end_u, z),
                0.038,
                mats["door_frame"],
                12,
            )

    for index, u in enumerate((-1.0, -0.72, -0.48, -0.24, 0.0, 0.24, 0.48, 0.72, 1.0), start=1):
        radius = 0.052 if abs(u) == 1.0 else 0.026
        add_cylinder_between(
            f"AR-06 curved screen vertical mullion {index:02d}",
            root,
            curve_point(u, z_bottom - 0.07),
            curve_point(u, z_top + 0.07),
            radius,
            mats["door_frame"],
            12,
        )

    for u, side_name in ((-1.0, "left"), (1.0, "right")):
        x, y, _ = curve_point(u, 0.0)
        add_box(
            f"AR-06 {side_name} curved screen side return plate",
            root,
            (x, y - 0.24, (z_bottom + z_top) * 0.5),
            (0.20, 0.52, z_top - z_bottom + 0.42),
            mats["door_frame"],
            (0, 0, 0),
            0.010,
        )

    for u in (-0.76, -0.38, 0.0, 0.38, 0.76):
        x, y, _ = curve_point(u, 0.0)
        add_box(
            f"AR-06 curved screen floor arc marker {u:+.2f}",
            root,
            (x, y - 0.34, 0.215),
            (0.34, 0.052, 0.038),
            mats["screen"],
            (0, 0, 0),
            0.004,
        )

    add_text_label(
        "AR-06 screen placeholder floor label",
        root,
        "CONCAVE FORWARD VIEW",
        (0, 2.08, 0.245),
        (0, 0, 0),
        mats["label_text"],
        0.20,
    )


def add_central_pillar_context(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    # AR-01 shell sample includes low-detail placeholders so the room proportions and walking path are readable.
    add_cylinder("AR-02 placeholder central turret support pillar", root, (0, -0.28, 1.12), 0.54, 2.24, mats["pillar"], vertices=48)
    add_cylinder("AR-04 placeholder top operating platform", root, (0, -0.28, 2.26), 1.18, 0.20, mats["platform"], vertices=64)
    add_cylinder("AR-04 placeholder platform safety rim", root, (0, -0.28, 2.42), 1.24, 0.10, mats["rim"], vertices=64)

    step_count = 12
    for i in range(step_count):
        t = i / (step_count - 1)
        y = -3.25 + t * 1.79
        z = 0.22 + t * 2.06
        width = 1.42 - t * 0.18
        add_box(f"AR-03 placeholder rear stair tread {i + 1:02d}", root, (0, y, z), (width, 0.28, 0.16), mats["stair"], (0, 0, 0), 0.008)
    for x in (-0.86, 0.86):
        add_cylinder_between(
            "AR-03 placeholder stair side rail",
            root,
            (x, -3.32, 0.46),
            (x, -1.40, 2.46),
            0.030,
            mats["rail"],
            12,
        )

    add_box("AR-05 placeholder turret handle console base", root, (0, 0.38, 2.48), (0.72, 0.42, 0.22), mats["console"], (0, 0, 0), 0.010)
    add_cylinder_between("AR-05 placeholder handle support column", root, (0, 0.56, 2.52), (0, 0.78, 2.80), 0.045, mats["rail"], 14)
    add_torus(
        "AR-05 placeholder two-hand turret wheel",
        root,
        (0, 0.82, 2.82),
        mats["handle"],
        (math.radians(90), 0, 0),
        0.24,
        0.026,
    )
    add_cylinder_between("AR-05 placeholder horizontal grip bar", root, (-0.29, 0.82, 2.82), (0.29, 0.82, 2.82), 0.020, mats["handle"], 10)
    add_cylinder_between("AR-05 placeholder vertical grip bar", root, (0, 0.82, 2.54), (0, 0.82, 3.10), 0.018, mats["handle"], 10)


def add_corridor_stub(
    root: bpy.types.Object,
    name: str,
    center: tuple[float, float, float],
    scale: tuple[float, float, float],
    mats: dict[str, bpy.types.Material],
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
    side_axis: str = "x",
) -> None:
    add_box(f"{name} corridor floor continuation", root, center, scale, mats["corridor_floor"], rot, 0.012)
    if side_axis == "x":
        for side in (-0.92, 0.92):
            add_box(f"{name} corridor side wall {side:+.2f}", root, (center[0] + side, center[1], 1.02), (0.20, scale[1], 2.04), mats["wall_dark"], rot, 0.012)
    else:
        for side in (-0.92, 0.92):
            add_box(f"{name} corridor side wall {side:+.2f}", root, (center[0], center[1] + side, 1.02), (scale[0], 0.20, 2.04), mats["wall_dark"], rot, 0.012)


def add_entry_highlights(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    entries = [
        ("CONTROL ROOM", "control room south", (CONTROL_DOOR_X, ROOM_SOUTH_Y - 0.02, 1.42), (1.48, 0.07, 0.42), mats["control_marker"], 0.0),
        ("SUPPLY ROOM", "supply room east", (ROOM_WIDTH * 0.5 + 0.02, SUPPLY_DOOR_Y, 1.42), (0.07, 1.36, 0.42), mats["supply_marker"], math.radians(90)),
        ("CARGO HOLD", "cargo hold east", (ROOM_WIDTH * 0.5 + 0.02, CARGO_DOOR_Y, 1.42), (0.07, 1.36, 0.42), mats["cargo_marker"], math.radians(90)),
    ]
    for text, key, loc, scale, mat, text_angle in entries:
        add_box(f"{key} color direction plate", root, loc, scale, mat, (0, 0, 0), 0.006)
        add_text_label(
            f"{key} floor direction text",
            root,
            text,
            (loc[0], loc[1], 0.255),
            (0, 0, text_angle),
            mats["label_text"],
            0.18,
        )

    add_box("control doorway colored threshold", root, (CONTROL_DOOR_X, ROOM_SOUTH_Y - 0.02, 0.210), (DOOR_WIDTH + 0.48, 0.62, 0.055), mats["control_marker"], (0, 0, 0), 0.006)
    add_box("supply doorway colored threshold", root, (ROOM_WIDTH * 0.5 + 0.02, SUPPLY_DOOR_Y, 0.210), (0.62, DOOR_WIDTH + 0.42, 0.055), mats["supply_marker"], (0, 0, 0), 0.006)
    add_box("cargo doorway colored threshold", root, (ROOM_WIDTH * 0.5 + 0.02, CARGO_DOOR_Y, 0.210), (0.62, DOOR_WIDTH + 0.42, 0.055), mats["cargo_marker"], (0, 0, 0), 0.006)


def add_wall_dressing(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    for x in (-3.45, -2.30, -1.15, 1.15, 2.30, 3.45):
        add_box(f"north weapon screen wall vertical rib {x:+.2f}", root, (x, ROOM_NORTH_Y - 0.16, 1.56), (0.10, 0.14, 2.34), mats["beam"], (0, 0, 0), 0.004)
    for y in (-3.52, -2.08, 1.08, 2.48):
        add_cylinder_between("west wall armory service conduit", root, (-ROOM_WIDTH * 0.5 + 0.14, y, 2.45), (-ROOM_WIDTH * 0.5 + 0.14, y + 0.74, 2.45), 0.027, mats["conduit"], 14)
        add_cylinder_between("east wall armory service conduit", root, (ROOM_WIDTH * 0.5 - 0.14, y, 2.45), (ROOM_WIDTH * 0.5 - 0.14, y + 0.74, 2.45), 0.027, mats["conduit"], 14)
    for x in (-2.6, -1.3, 0.0, 1.3, 2.6):
        add_box(f"south ramp hazard stripe {x:+.1f}", root, (x, ROOM_SOUTH_Y + 0.30, 0.205), (0.16, 0.82, 0.035), mats["hazard"], (0, 0, math.radians(18)), 0.002)


def build_armory_shell(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("AR-01 armory shell Blender sample")

    add_box("AR-01 sealed armory deck floor", root, (0, ROOM_CENTER_Y, 0), (ROOM_WIDTH, ROOM_DEPTH, FLOOR_THICKNESS), mats["floor"], (0, 0, 0), 0.018)
    add_box("AR-01 solid forward curved-screen wall", root, (0, ROOM_NORTH_Y, ROOM_HEIGHT * 0.5), (ROOM_WIDTH + WALL_THICKNESS, WALL_THICKNESS, ROOM_HEIGHT), mats["wall"], (0, 0, 0), 0.016)
    add_wall_with_door_y(root, "AR-08 south control room", ROOM_SOUTH_Y, CONTROL_DOOR_X, mats)
    add_plain_wall_x(root, "AR-01 west sealed armory wall", -ROOM_WIDTH * 0.5, mats)
    add_double_door_wall_x(
        root,
        "AR-07 AR-09 east adjacent supply and cargo",
        ROOM_WIDTH * 0.5,
        [("supply room", SUPPLY_DOOR_Y), ("cargo hold ramp", CARGO_DOOR_Y)],
        mats,
    )

    add_corridor_stub(root, "AR-08 control room south", (CONTROL_DOOR_X, ROOM_SOUTH_Y - 1.08, 0.0), (DOOR_WIDTH + 0.32, 2.12, FLOOR_THICKNESS), mats, (0, 0, 0), "x")
    add_corridor_stub(root, "AR-07 supply room east", (ROOM_WIDTH * 0.5 + 1.08, SUPPLY_DOOR_Y, 0.0), (2.12, DOOR_WIDTH + 0.32, FLOOR_THICKNESS), mats, (0, 0, 0), "y")
    add_box("AR-09 cargo hold descending ramp floor", root, (ROOM_WIDTH * 0.5 + 1.33, CARGO_DOOR_Y, -0.16), (2.64, DOOR_WIDTH + 0.52, FLOOR_THICKNESS), mats["ramp_floor"], (0, math.radians(7), 0), 0.012)
    for side in (-1.04, 1.04):
        add_box("AR-09 cargo ramp side wall", root, (ROOM_WIDTH * 0.5 + 1.32, CARGO_DOOR_Y + side, 0.72), (2.52, 0.22, 1.78), mats["wall_dark"], (0, math.radians(7), 0), 0.012)

    add_floor_grid(root, mats)
    add_curved_screen_placeholder(root, mats)
    add_central_pillar_context(root, mats)
    add_entry_highlights(root, mats)
    add_wall_dressing(root, mats)


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 44
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("ArmoryShellWorld")
    scene.world.color = (0.010, 0.011, 0.013)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -0.6, 5.9))
    top = bpy.context.object
    top.name = "large overhead armory inspection softbox"
    top.data.energy = 650
    top.data.size = 7.0

    bpy.ops.object.light_add(type="AREA", location=(-5.4, -2.4, 2.8))
    west = bpy.context.object
    west.name = "warm supply corridor fill"
    west.data.energy = 170
    west.data.size = 3.4
    west.data.color = (1.0, 0.70, 0.42)

    bpy.ops.object.light_add(type="AREA", location=(5.4, -2.4, 2.8))
    east = bpy.context.object
    east.name = "cool control corridor fill"
    east.data.energy = 175
    east.data.size = 3.4
    east.data.color = (0.56, 0.76, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(0, 2.30, 1.72))
    screen = bpy.context.object
    screen.name = "dim forward screen glow"
    screen.data.energy = 125
    screen.data.color = (0.36, 0.84, 0.94)


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
    camera.name = "armory shell camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "armory_shell.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "armory_shell.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "armory_shell.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "AR-01",
        "title": "무기실 룸 셸",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt:130 - 무기실 중앙 기둥, 기둥 뒤편 계단, 기둥 위 포탑 핸들, 기둥 앞 가로 커브형 대형 스크린.",
            "docs/GAME_DESIGN_SOURCE.txt:131 - 스크린 벽 기준 뒤쪽 통제실 복도, 오른쪽 비품실 복도, 비품실 복도 바로 옆 운송창고 경사 복도.",
            "docs/ARMORY_OBJECTS.md - AR-01 무기실 룸 셸: 바닥, 벽, 스크린 벽, 출입구 3개, 중앙 기둥 주변 이동 공간.",
        ],
        "generatedFiles": [
            "blender/armory_shell.blend",
            "exports/armory_shell.fbx",
            "exports/armory_shell.glb",
            "renders/01_overview.png",
            "renders/02_floor_plan.png",
            "renders/03_forward_screen_wall.png",
            "renders/04_central_pillar_context.png",
            "renders/05_side_entries.png",
            "renders/06_cargo_ramp.png",
            "index.html",
            "README.md",
            "ASSET_MANIFEST.json",
            "APPROVAL_STATUS.json",
        ],
        "includedParts": [
            "천장을 제외한 무기실 바닥과 벽 셸",
            "중앙이 뒤로 들어가고 양 끝이 앞으로 나온 오목형 정면 가로 커브 스크린 자리 표시",
            "중앙 기둥, 후면 계단, 상부 발판, 포탑 핸들의 낮은 밀도 위치 관계 표시",
            "스크린 벽 기준 뒤쪽 통제실 방향 출입구와 복도 스텁",
            "스크린 벽 기준 오른쪽 비품실 방향 출입구와 복도 스텁",
            "비품실 복도 바로 옆 오른쪽 운송창고 방향 하강 경사 복도",
            "영어 메인 방향 표시와 색상 출입구 프레임",
        ],
        "excludedParts": [
            "실제 포탑 조준/발사 로직",
            "포탑 수동 모드 UI",
            "외부 선체 포탑 모델",
            "소행성, 침입선, 외계 생명체 외부 목표",
            "개인 휴대 무기 진열 랙과 탄약 보관함",
            "Unity 씬 배치",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "AR-01",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산, UI 흐름에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# armory_shell

AR-01 무기실 룸 셸 승인용 Blender 샘플입니다.

## 목적

무기실을 Unity에 연결하기 전에, 방의 기본 형태와 주요 오브젝트 위치 관계를 검사하기 위한 샘플입니다.
이 샘플은 룸 셸 중심이며 실제 포탑 조준/발사 로직, 수동 포탑 UI, 외부 목표 모델은 포함하지 않습니다.

## 반영 기준

- 스크린 벽을 정면으로 보는 기준에서 뒤쪽에는 통제실 방향 출입구를 두었습니다.
- 스크린 벽을 정면으로 보는 기준에서 오른쪽에는 비품실 방향 출입구를 두었습니다.
- 비품실 방향 출입구 바로 옆에는 운송창고 방향 출입구와 아래로 내려가는 경사 복도 스텁을 두었습니다.
- 정면 벽에는 화물선 정면을 보여줄 가로 커브형 대형 스크린 자리를 표시했습니다. 방 안에서 볼 때 중앙은 뒤로 들어가고 양 끝은 앞으로 나온 오목형 커브이며, 곡면 레일과 세로 분할선을 함께 넣었습니다.
- 중앙에는 기둥, 기둥 뒤편 계단, 상부 조작 발판, 포탑 핸들 위치 관계를 낮은 밀도 자리 표시로 넣었습니다.
- 개인 휴대 무기 진열 랙과 탄약 보관함은 원본 기준 무기실 고정 오브젝트가 아니므로 넣지 않았습니다.
- 내부 구조 확인이 쉽도록 천장은 제외했습니다.

## 포함

- `blender/armory_shell.blend`
- `exports/armory_shell.fbx`
- `exports/armory_shell.glb`
- `renders/*.png` 6개 구도
- `index.html`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- 실제 포탑 조준/발사 로직
- 포탑 수동 모드 UI
- 외부 선체 포탑 모델
- 소행성, 침입선, 외계 생명체 외부 목표
- 개인 휴대 무기 진열 랙과 탄약 보관함
- Unity 씬 배치와 충돌 설정
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_overview.png", "01 전체 무기실 룸 셸"),
        ("02_floor_plan.png", "02 상단 배치와 3방향 출입구"),
        ("03_forward_screen_wall.png", "03 곡률 강조 정면 가로 커브형 스크린 벽"),
        ("04_central_pillar_context.png", "04 중앙 기둥과 포탑 핸들 위치 관계"),
        ("05_side_entries.png", "05 오른쪽 비품실과 운송창고 인접 출입구"),
        ("06_cargo_ramp.png", "06 오른쪽 운송창고 방향 하강 경사 복도"),
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
  <title>armory_shell review</title>
  <style>
    body {{ margin: 0; background: #111314; color: #e8e0d0; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #c9bfad; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #3f4542; background: #1c2121; padding: 10px; }}
    img {{ width: 100%; display: block; background: #0b0e0e; }}
    figcaption {{ margin-top: 8px; color: #ded3bd; font-size: 14px; }}
    @media (max-width: 800px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>armory_shell</h1>
  <p>AR-01 무기실 룸 셸 승인용 샘플입니다. 수정된 기획서 기준에 따라 스크린 벽 기준 뒤쪽에는 통제실 복도, 오른쪽에는 비품실 복도, 비품실 복도 바로 옆에는 운송창고 경사 복도를 배치했습니다. 정면에는 방 안에서 볼 때 중앙은 뒤로 들어가고 양 끝은 앞으로 나온 오목형 가로 커브 스크린을 표시했고, 중앙 기둥과 후면 계단, 상부 포탑 핸들은 위치 관계 확인용 낮은 밀도 자리 표시로 넣었습니다. 개인 휴대 무기 진열 랙은 원본 기준 무기실 고정 오브젝트가 아니므로 포함하지 않았습니다.</p>
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
        "floor": noisy_metal("armory worn sealed deck", (0.15, 0.17, 0.17, 1)),
        "floor_panel": noisy_metal("armory removable dark floor panel", (0.19, 0.20, 0.19, 1)),
        "deck_rib": noisy_metal("armory raised deck rib", (0.08, 0.09, 0.09, 1)),
        "wall": noisy_metal("armory thick armored wall", (0.21, 0.24, 0.24, 1)),
        "wall_dark": noisy_metal("armory dark corridor wall", (0.10, 0.12, 0.13, 1)),
        "door_frame": noisy_metal("armory heavy doorway frame", (0.34, 0.34, 0.30, 1)),
        "corridor_floor": noisy_metal("armory corridor continuation steel", (0.15, 0.18, 0.18, 1)),
        "ramp_floor": noisy_metal("armory cargo descending ramp steel", (0.17, 0.19, 0.18, 1)),
        "screen": material("inactive cyan curved forward view screen", (0.030, 0.090, 0.105, 1), roughness=0.32, emission=(0.02, 0.22, 0.26, 1), emission_strength=0.34),
        "pillar": noisy_metal("armory central support pillar gunmetal", (0.23, 0.25, 0.24, 1)),
        "platform": noisy_metal("armory top operator platform", (0.18, 0.20, 0.20, 1)),
        "rim": noisy_metal("armory top platform safety rim", (0.36, 0.35, 0.30, 1)),
        "stair": noisy_metal("armory rear stair treads", (0.29, 0.30, 0.27, 1)),
        "rail": noisy_metal("armory stair rail and supports", (0.42, 0.41, 0.35, 1)),
        "console": noisy_metal("armory turret handle console", (0.16, 0.18, 0.18, 1)),
        "handle": noisy_metal("armory turret handle worn grip", (0.48, 0.46, 0.38, 1)),
        "beam": noisy_metal("armory wall structural rib", (0.30, 0.31, 0.28, 1)),
        "conduit": noisy_metal("armory wall utility conduit", (0.045, 0.052, 0.052, 1)),
        "label_text": material("armory pale direction label text", (0.78, 0.88, 0.84, 1), roughness=0.70, emission=(0.16, 0.32, 0.29, 1), emission_strength=0.08),
        "supply_marker": material("supply room direction amber marker", (0.70, 0.42, 0.14, 1), roughness=0.74, emission=(0.25, 0.13, 0.03, 1), emission_strength=0.17),
        "control_marker": material("control room direction blue marker", (0.13, 0.28, 0.58, 1), roughness=0.70, emission=(0.03, 0.09, 0.24, 1), emission_strength=0.18),
        "cargo_marker": material("cargo hold direction green marker", (0.18, 0.42, 0.30, 1), roughness=0.72, emission=(0.04, 0.16, 0.10, 1), emission_strength=0.18),
        "hazard": material("armory muted cargo ramp hazard stripe", (0.86, 0.50, 0.12, 1), roughness=0.86),
    }

    build_armory_shell(mats)
    add_render_lights()

    cameras = [
        ("overview", (7.2, -7.7, 4.45), (0.0, -0.55, 1.26), 32, "01_overview.png", None),
        ("floor_plan", (0.0, ROOM_CENTER_Y, 12.0), (0.0, ROOM_CENTER_Y, 0.0), 50, "02_floor_plan.png", 13.6),
        ("forward_screen_wall", (2.75, -2.65, 2.92), (0.0, 2.72, 1.56), 26, "03_forward_screen_wall.png", None),
        ("central_pillar_context", (4.8, -4.1, 2.95), (0.0, -0.35, 1.78), 35, "04_central_pillar_context.png", None),
        ("side_entries", (ROOM_WIDTH * 0.5 + 0.72, (SUPPLY_DOOR_Y + CARGO_DOOR_Y) * 0.5, 11.0), (ROOM_WIDTH * 0.5 + 0.72, (SUPPLY_DOOR_Y + CARGO_DOOR_Y) * 0.5, 0.0), 50, "05_side_entries.png", 5.2),
        ("cargo_ramp", (8.4, CARGO_DOOR_Y, 2.12), (ROOM_WIDTH * 0.5, CARGO_DOOR_Y, 0.54), 34, "06_cargo_ramp.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} Blender sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
