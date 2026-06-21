from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "control_room_shell"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"

ROOM_WIDTH = 8.8
ROOM_NORTH_Y = 3.4
ROOM_SOUTH_Y = -5.35
ROOM_DEPTH = ROOM_NORTH_Y - ROOM_SOUTH_Y
ROOM_CENTER_Y = (ROOM_NORTH_Y + ROOM_SOUTH_Y) * 0.5
ROOM_HEIGHT = 3.2
FLOOR_THICKNESS = 0.18
WALL_THICKNESS = 0.34
DOOR_WIDTH = 1.55
DOOR_HEIGHT = 2.12


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
    noise.inputs["Scale"].default_value = 28
    noise.inputs["Detail"].default_value = 9
    noise.inputs["Roughness"].default_value = 0.56
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[0].color = (base[0] * 0.55, base[1] * 0.55, base[2] * 0.55, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.38, 1),
        min(base[1] * 1.38, 1),
        min(base[2] * 1.38, 1),
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
    bevel_width: float = 0.018,
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


def add_cylinder_between(
    name: str,
    parent: bpy.types.Object,
    start: tuple[float, float, float],
    end: tuple[float, float, float],
    radius: float,
    mat: bpy.types.Material,
    vertices: int = 16,
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
    size: float = 0.16,
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
    add_box(f"{name} doorway left frame", root, (door_x - DOOR_WIDTH * 0.5, y, DOOR_HEIGHT * 0.5), (0.18, WALL_THICKNESS + 0.10, DOOR_HEIGHT), mats["door_frame"])
    add_box(f"{name} doorway right frame", root, (door_x + DOOR_WIDTH * 0.5, y, DOOR_HEIGHT * 0.5), (0.18, WALL_THICKNESS + 0.10, DOOR_HEIGHT), mats["door_frame"])


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
    add_box(f"{name} doorway lower frame", root, (x, door_y - DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.10, 0.18, DOOR_HEIGHT), mats["door_frame"])
    add_box(f"{name} doorway upper frame", root, (x, door_y + DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.10, 0.18, DOOR_HEIGHT), mats["door_frame"])


def add_double_door_wall_x(root: bpy.types.Object, x: float, mats: dict[str, bpy.types.Material]) -> None:
    door_centers = [1.45, -1.45]
    intervals = sorted((center - DOOR_WIDTH * 0.5, center + DOOR_WIDTH * 0.5) for center in door_centers)
    cursor = -ROOM_DEPTH * 0.5
    z_mid = ROOM_HEIGHT * 0.5
    for index, (start, end) in enumerate(intervals, start=1):
        if start > cursor:
            depth = start - cursor
            add_box(f"east wall sealed segment {index}", root, (x, cursor + depth * 0.5, z_mid), (WALL_THICKNESS, depth, ROOM_HEIGHT), mats["wall"])
        cursor = end
    if cursor < ROOM_NORTH_Y:
        depth = ROOM_NORTH_Y - cursor
        add_box("east wall sealed final segment", root, (x, cursor + depth * 0.5, z_mid), (WALL_THICKNESS, depth, ROOM_HEIGHT), mats["wall"])

    header_height = ROOM_HEIGHT - DOOR_HEIGHT
    for label, center in (("engine room", 1.45), ("weapon room", -1.45)):
        add_box(f"east {label} doorway upper header", root, (x, center, DOOR_HEIGHT + header_height * 0.5), (WALL_THICKNESS, DOOR_WIDTH, header_height), mats["wall"])
        add_box(f"east {label} doorway lower frame", root, (x, center - DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.10, 0.18, DOOR_HEIGHT), mats["door_frame"])
        add_box(f"east {label} doorway upper frame", root, (x, center + DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.10, 0.18, DOOR_HEIGHT), mats["door_frame"])


def add_corridor_stub(
    root: bpy.types.Object,
    name: str,
    center: tuple[float, float, float],
    scale: tuple[float, float, float],
    mats: dict[str, bpy.types.Material],
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> None:
    add_box(f"{name} corridor floor continuation", root, center, scale, mats["corridor_floor"], rot, 0.012)
    if abs(rot[2]) < 0.01:
        # Stub extends on Y, side walls are placed on X.
        for side in (-0.86, 0.86):
            add_box(
                f"{name} corridor side wall {side:+.1f}",
                root,
                (center[0] + side, center[1], 1.05),
                (0.20, scale[1], 2.1),
                mats["wall_dark"],
                rot,
                0.012,
            )
    else:
        # Stub extends on X, side walls are placed on Y.
        for side in (-0.86, 0.86):
            add_box(
                f"{name} corridor side wall {side:+.1f}",
                root,
                (center[0], center[1] + side, 1.05),
                (scale[0], 0.20, 2.1),
                mats["wall_dark"],
                rot,
                0.012,
            )


def add_wall_panel_grid(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    # Blank shell-only screen mounting wall. Functional screens are CR-06 and later.
    north_y = ROOM_NORTH_Y - WALL_THICKNESS * 0.55
    add_box("blank future main screen recessed wall bay", root, (0, north_y - 0.035, 1.82), (4.7, 0.08, 1.18), mats["blank_panel"], (0, 0, 0), 0.012)
    add_box("main screen bay upper structural lintel", root, (0, north_y - 0.075, 2.52), (5.05, 0.14, 0.18), mats["door_frame"], (0, 0, 0), 0.010)
    add_box("main screen bay lower service sill", root, (0, north_y - 0.075, 1.10), (5.05, 0.14, 0.18), mats["door_frame"], (0, 0, 0), 0.010)
    for x in (-2.75, 2.75):
        add_box("side blank vertical monitor recess", root, (x, north_y - 0.032, 1.72), (0.72, 0.075, 1.45), mats["blank_panel"], (0, 0, 0), 0.010)

    for x in (-3.4, -1.7, 0.0, 1.7, 3.4):
        add_box(f"floor access plate {x:+.1f}", root, (x, -0.15, 0.105), (1.25, 1.72, 0.045), mats["floor_panel"], (0, 0, 0), 0.006)
    for y in (-4.60, -3.70, -2.80, -1.90, -1.00, -0.10, 0.85, 1.70, 2.55):
        add_box(f"control room deck rib {y:+.2f}", root, (0, y, 0.145), (ROOM_WIDTH - 0.75, 0.035, 0.045), mats["deck_rib"], (0, 0, 0), 0.002)


def add_ceiling_and_services(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    z = ROOM_HEIGHT + 0.06
    add_box("sealed rectangular ceiling plate", root, (0, ROOM_CENTER_Y, z), (ROOM_WIDTH + WALL_THICKNESS, ROOM_DEPTH + WALL_THICKNESS, 0.16), mats["ceiling"], (0, 0, 0), 0.012)
    for x in (-3.2, -1.6, 0.0, 1.6, 3.2):
        add_box(f"ceiling longitudinal beam {x:+.1f}", root, (x, ROOM_CENTER_Y, ROOM_HEIGHT - 0.08), (0.16, ROOM_DEPTH - 0.45, 0.18), mats["beam"], (0, 0, 0), 0.006)
    for y in (-2.35, 0.0, 2.35):
        add_box(f"ceiling cross beam {y:+.1f}", root, (0, y, ROOM_HEIGHT - 0.13), (ROOM_WIDTH - 0.5, 0.13, 0.14), mats["beam"], (0, 0, 0), 0.006)
    for y in (-1.15, 1.15):
        add_cylinder_between("overhead cable tray rail", root, (-3.65, y, ROOM_HEIGHT - 0.32), (3.65, y, ROOM_HEIGHT - 0.32), 0.035, mats["conduit"], 14)


def add_direction_markers(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    labels = [
        ("운송창고", mats["cargo_marker"], (-0.86, ROOM_SOUTH_Y - 0.03, 1.50), 0.0, (1.40, 0.07, 0.44), (math.radians(90), 0, 0)),
        ("무기실", mats["weapon_marker"], (0.86, ROOM_SOUTH_Y - 0.03, 1.50), 0.0, (1.40, 0.07, 0.44), (math.radians(90), 0, 0)),
        ("동력실", mats["engine_marker"], (-ROOM_WIDTH * 0.5 - 0.03, -3.45, 1.55), 0.0, (0.07, 1.46, 0.44), (math.radians(90), 0, math.radians(-90))),
        ("조종실 40도", mats["cockpit_marker"], (-ROOM_WIDTH * 0.5 - 0.58, -1.30, 1.55), 40.0, (1.58, 0.07, 0.44), (math.radians(90), 0, math.radians(40))),
    ]
    for text, marker_mat, loc, rot_z, plate_scale, text_rot in labels:
        add_box(f"{text.lower()} large direction color plate", root, loc, plate_scale, marker_mat, (0, 0, math.radians(rot_z)), 0.006)
        add_text_label(f"{text.lower()} large direction text", root, text, loc, text_rot, mats["label_text"], 0.195)


def add_entry_highlights(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    def portal(
        key: str,
        korean: str,
        center: tuple[float, float],
        angle_degrees: float,
        mat: bpy.types.Material,
        *,
        angled_banner: bool = False,
    ) -> None:
        rot = (0, 0, math.radians(angle_degrees))
        threshold_xy = local_xy(center, angle_degrees, 0.12, 0.0)
        add_box(f"{key} colored doorway threshold", root, (threshold_xy[0], threshold_xy[1], 0.215), (0.72, DOOR_WIDTH + 0.58, 0.060), mat, rot, 0.006)
        add_box(f"{key} colored doorway upper banner", root, (center[0], center[1], DOOR_HEIGHT + 0.26), (0.16, DOOR_WIDTH + 0.78, 0.34), mat, rot, 0.008)
        for side in (-DOOR_WIDTH * 0.5 - 0.16, DOOR_WIDTH * 0.5 + 0.16):
            jamb_xy = local_xy(center, angle_degrees, 0.0, side)
            add_box(f"{key} colored doorway jamb {side:+.2f}", root, (jamb_xy[0], jamb_xy[1], 1.03), (0.16, 0.14, 1.78), mat, rot, 0.006)
        guide_xy = local_xy(center, angle_degrees, -1.02, 0.0)
        add_box(f"{key} floor guide stripe", root, (guide_xy[0], guide_xy[1], 0.190), (1.52, 0.22, 0.045), mat, rot, 0.004)
        text_xy = local_xy(center, angle_degrees, -1.32, 0.0)
        text_rot_z = angle_degrees if angled_banner else angle_degrees + 90.0
        add_text_label(
            f"{key} floor guide label",
            root,
            korean,
            (text_xy[0], text_xy[1], 0.255),
            (0.0, 0.0, math.radians(text_rot_z)),
            mats["label_text"],
            0.165,
        )

    portal("cockpit angled", "조종실 40도", (-ROOM_WIDTH * 0.5 - 0.58, -1.30), 140.0, mats["cockpit_marker"], angled_banner=True)
    portal("engine room left", "동력실", (-ROOM_WIDTH * 0.5 - 0.06, -3.45), 180.0, mats["engine_marker"])
    portal("cargo south", "운송창고", (-0.86, ROOM_SOUTH_Y - 0.06), -90.0, mats["cargo_marker"])
    portal("weapon south", "무기실", (0.86, ROOM_SOUTH_Y - 0.06), -90.0, mats["weapon_marker"])

    add_box("left side cockpit engine separation wall pier", root, (-ROOM_WIDTH * 0.5 + 0.02, -2.38, 1.35), (0.20, 0.48, 2.70), mats["door_frame"], (0, 0, 0), 0.010)
    add_box("south cargo weapon shared divider", root, (0.0, ROOM_SOUTH_Y + 0.02, 1.30), (0.18, 0.28, 2.60), mats["door_frame"], (0, 0, 0), 0.010)


def add_plain_wall_x(root: bpy.types.Object, name: str, x: float, mats: dict[str, bpy.types.Material]) -> None:
    add_box(name, root, (x, ROOM_CENTER_Y, ROOM_HEIGHT * 0.5), (WALL_THICKNESS, ROOM_DEPTH + WALL_THICKNESS, ROOM_HEIGHT), mats["wall"])


def add_double_door_wall_y(
    root: bpy.types.Object,
    name: str,
    y: float,
    door_centers: list[tuple[str, float]],
    mats: dict[str, bpy.types.Material],
) -> None:
    intervals = sorted((center - DOOR_WIDTH * 0.5, center + DOOR_WIDTH * 0.5, label) for label, center in door_centers)
    cursor = -ROOM_WIDTH * 0.5
    z_mid = ROOM_HEIGHT * 0.5
    for index, (start, end, _label) in enumerate(intervals, start=1):
        if start > cursor:
            width = start - cursor
            add_box(f"{name} sealed wall segment {index}", root, (cursor + width * 0.5, y, z_mid), (width, WALL_THICKNESS, ROOM_HEIGHT), mats["wall"])
        cursor = end
    if cursor < ROOM_WIDTH * 0.5:
        width = ROOM_WIDTH * 0.5 - cursor
        add_box(f"{name} sealed final wall segment", root, (cursor + width * 0.5, y, z_mid), (width, WALL_THICKNESS, ROOM_HEIGHT), mats["wall"])

    header_height = ROOM_HEIGHT - DOOR_HEIGHT
    for label, center in door_centers:
        add_box(f"{name} {label} doorway upper header", root, (center, y, DOOR_HEIGHT + header_height * 0.5), (DOOR_WIDTH, WALL_THICKNESS, header_height), mats["wall"])
        add_box(f"{name} {label} doorway left frame", root, (center - DOOR_WIDTH * 0.5, y, DOOR_HEIGHT * 0.5), (0.18, WALL_THICKNESS + 0.10, DOOR_HEIGHT), mats["door_frame"])
        add_box(f"{name} {label} doorway right frame", root, (center + DOOR_WIDTH * 0.5, y, DOOR_HEIGHT * 0.5), (0.18, WALL_THICKNESS + 0.10, DOOR_HEIGHT), mats["door_frame"])


def add_west_door_wall(root: bpy.types.Object, x: float, mats: dict[str, bpy.types.Material]) -> None:
    # The cockpit and engine-room corridors are both on the left side, but separated.
    door_specs = [
        ("cockpit angled", -1.30),
        ("engine room", -3.45),
    ]
    intervals = sorted((center - DOOR_WIDTH * 0.5, center + DOOR_WIDTH * 0.5, label) for label, center in door_specs)
    cursor = ROOM_SOUTH_Y
    z_mid = ROOM_HEIGHT * 0.5
    for index, (start, end, _label) in enumerate(intervals, start=1):
        if start > cursor:
            depth = start - cursor
            add_box(f"west wall separated sealed segment {index}", root, (x, cursor + depth * 0.5, z_mid), (WALL_THICKNESS, depth, ROOM_HEIGHT), mats["wall"])
        cursor = end
    if cursor < ROOM_NORTH_Y:
        depth = ROOM_NORTH_Y - cursor
        add_box("west wall separated sealed final segment", root, (x, cursor + depth * 0.5, z_mid), (WALL_THICKNESS, depth, ROOM_HEIGHT), mats["wall"])

    header_height = ROOM_HEIGHT - DOOR_HEIGHT
    for label, center in door_specs:
        add_box(f"west {label} doorway upper header", root, (x, center, DOOR_HEIGHT + header_height * 0.5), (WALL_THICKNESS, DOOR_WIDTH, header_height), mats["wall"])
        add_box(f"west {label} doorway lower frame", root, (x, center - DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.10, 0.18, DOOR_HEIGHT), mats["door_frame"])
        add_box(f"west {label} doorway upper frame", root, (x, center + DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.10, 0.18, DOOR_HEIGHT), mats["door_frame"])


def add_internal_partition(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    partition_y = 0.56
    partition_height = 2.55
    door_width = 1.36
    door_height = 2.02
    left_width = ROOM_WIDTH * 0.5 - door_width * 0.5
    right_width = ROOM_WIDTH * 0.5 - door_width * 0.5
    add_box("internal partition left wall between entry and screen side", root, (-ROOM_WIDTH * 0.25 - door_width * 0.25, partition_y, partition_height * 0.5), (left_width, 0.18, partition_height), mats["partition"])
    add_box("internal partition right wall between entry and screen side", root, (ROOM_WIDTH * 0.25 + door_width * 0.25, partition_y, partition_height * 0.5), (right_width, 0.18, partition_height), mats["partition"])
    add_box("internal partition doorway header", root, (0, partition_y, door_height + (partition_height - door_height) * 0.5), (door_width, 0.22, partition_height - door_height), mats["partition"])
    add_box("internal partition doorway left jamb", root, (-door_width * 0.5, partition_y, door_height * 0.5), (0.14, 0.26, door_height), mats["door_frame"])
    add_box("internal partition doorway right jamb", root, (door_width * 0.5, partition_y, door_height * 0.5), (0.14, 0.26, door_height), mats["door_frame"])
    add_text_label(
        "internal partition passage label",
        root,
        "스크린 구역 출입문",
        (0, partition_y - 0.135, 1.54),
        (math.radians(90), 0, 0),
        mats["label_text"],
        0.14,
    )


def local_xy(center: tuple[float, float], angle_degrees: float, forward: float, side: float) -> tuple[float, float]:
    angle = math.radians(angle_degrees)
    direction = Vector((math.cos(angle), math.sin(angle), 0.0))
    tangent = Vector((-math.sin(angle), math.cos(angle), 0.0))
    point = Vector((center[0], center[1], 0.0)) + direction * forward + tangent * side
    return (point.x, point.y)


def add_oriented_corridor(
    root: bpy.types.Object,
    name: str,
    center: tuple[float, float],
    angle_degrees: float,
    length: float,
    width: float,
    mats: dict[str, bpy.types.Material],
) -> None:
    rot = (0.0, 0.0, math.radians(angle_degrees))
    floor_center_xy = local_xy(center, angle_degrees, length * 0.5, 0.0)
    add_box(f"{name} corridor floor continuation", root, (floor_center_xy[0], floor_center_xy[1], 0.0), (length, width, FLOOR_THICKNESS), mats["corridor_floor"], rot, 0.012)
    for side in (-width * 0.5, width * 0.5):
        wall_xy = local_xy(center, angle_degrees, length * 0.5, side)
        add_box(f"{name} corridor side wall {side:+.2f}", root, (wall_xy[0], wall_xy[1], 1.05), (length, 0.20, 2.1), mats["wall_dark"], rot, 0.012)


def build_control_room_shell(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("CR-01 control room shell Blender sample")

    add_box("sealed control room deck floor", root, (0, ROOM_CENTER_Y, 0), (ROOM_WIDTH, ROOM_DEPTH, FLOOR_THICKNESS), mats["floor"], (0, 0, 0), 0.018)
    add_box("north solid future screen wall shell", root, (0, ROOM_NORTH_Y, ROOM_HEIGHT * 0.5), (ROOM_WIDTH + WALL_THICKNESS, WALL_THICKNESS, ROOM_HEIGHT), mats["wall"])
    add_double_door_wall_y(
        root,
        "south attached cargo and weapon",
        ROOM_SOUTH_Y,
        [("cargo bay", -0.86), ("weapon room", 0.86)],
        mats,
    )
    add_west_door_wall(root, -ROOM_WIDTH * 0.5, mats)
    add_plain_wall_x(root, "east solid control room wall with no corridor", ROOM_WIDTH * 0.5, mats)

    add_oriented_corridor(root, "cockpit 40 degree outside only", (-ROOM_WIDTH * 0.5 - 0.64, -1.30), 140.0, 2.15, DOOR_WIDTH + 0.36, mats)
    add_oriented_corridor(root, "engine room left separated", (-ROOM_WIDTH * 0.5 - 0.06, -3.45), 180.0, 2.05, DOOR_WIDTH + 0.42, mats)
    add_oriented_corridor(root, "cargo bay south attached", (-0.86, ROOM_SOUTH_Y - 0.06), -90.0, 2.00, DOOR_WIDTH + 0.28, mats)
    add_oriented_corridor(root, "weapon room south attached", (0.86, ROOM_SOUTH_Y - 0.06), -90.0, 2.00, DOOR_WIDTH + 0.28, mats)
    add_internal_partition(root, mats)

    add_wall_panel_grid(root, mats)
    add_direction_markers(root, mats)
    add_entry_highlights(root, mats)

    # Repeated shell bolts and wear strips make the sample read as a usable sci-fi control room without adding runtime UI.
    for x in (-3.85, -2.55, -1.25, 1.25, 2.55, 3.85):
        add_box(f"north wall armored rib {x:+.1f}", root, (x, ROOM_DEPTH * 0.5 - 0.19, 1.62), (0.10, 0.12, 2.35), mats["beam"], (0, 0, 0), 0.004)
    for y in (-2.6, -0.35, 2.6):
        add_cylinder_between("side wall utility conduit west", root, (-ROOM_WIDTH * 0.5 + 0.13, y, 2.62), (-ROOM_WIDTH * 0.5 + 0.13, y + 0.84, 2.62), 0.026, mats["conduit"], 14)
        add_cylinder_between("side wall utility conduit east", root, (ROOM_WIDTH * 0.5 - 0.13, y, 2.62), (ROOM_WIDTH * 0.5 - 0.13, y + 0.84, 2.62), 0.026, mats["conduit"], 14)


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("ControlRoomShellWorld")
    scene.world.color = (0.010, 0.012, 0.013)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, 0, 5.8))
    top = bpy.context.object
    top.name = "large overhead control room inspection softbox"
    top.data.energy = 620
    top.data.size = 7.0

    bpy.ops.object.light_add(type="AREA", location=(-5.2, -4.0, 2.9))
    cool = bpy.context.object
    cool.name = "cool cockpit corridor fill"
    cool.data.energy = 185
    cool.data.size = 3.5
    cool.data.color = (0.62, 0.82, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(3.8, 3.1, 2.0))
    amber = bpy.context.object
    amber.name = "low amber doorway status glow"
    amber.data.energy = 95
    amber.data.color = (1.0, 0.58, 0.30)


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
    camera.name = "control room shell camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "control_room_shell.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "control_room_shell.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "control_room_shell.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "CR-01",
        "title": "통제실 룸 셸",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/CONTROL_ROOM_OBJECTS.md - CR-01 통제실 룸 셸: 벽, 바닥, 천장, 출입구가 있는 독립 구역.",
            "사용자 확인 구조 - 통제실 내부에는 스크린 쪽과 입구 쪽을 나누는 문 달린 가벽이 있다.",
            "사용자 확인 구조 - 조종실과 동력실 복도는 위에서 내려다본 기준 왼쪽에 몰려 있고 서로 조금 떨어져 있다.",
            "사용자 확인 구조 - 조종실 쪽 문은 40도 정도 기울어진 형태다.",
            "사용자 확인 구조 - 운송창고와 무기실 복도는 위에서 내려다본 기준 6시 방향에 바로 옆에 붙어 있다.",
            "docs/MVP_IMPLEMENTATION_ORDER.md - 6구역 Graybox에서 각 구역과 복도 연결 방향을 플레이 중 확인 가능해야 한다.",
        ],
        "generatedFiles": [
            "blender/control_room_shell.blend",
            "exports/control_room_shell.fbx",
            "exports/control_room_shell.glb",
            "renders/01_overview.png",
            "renders/02_floor_plan.png",
            "renders/03_main_wall_shell.png",
            "renders/04_cargo_entry.png",
            "renders/05_side_entries.png",
            "renders/06_ceiling_and_wall_panels.png",
            "index.html",
            "README.md",
            "ASSET_MANIFEST.json",
            "APPROVAL_STATUS.json",
        ],
        "includedParts": [
            "천장을 제외한 통제실 바닥과 벽 셸",
            "가벽 위치를 유지한 상태에서 6시 방향으로 확장한 입구 영역",
            "스크린 쪽과 입구 쪽을 나누는 내부 가벽과 출입문",
            "왼쪽에 분리 배치된 조종실 40도 출입구와 복도",
            "왼쪽에 분리 배치된 동력실 출입구와 복도",
            "6시 방향에 붙어 있는 운송창고 출입구와 복도",
            "6시 방향에 붙어 있는 무기실 출입구와 복도",
            "향후 메인 스크린을 넣기 위한 비기능성 빈 벽면 베이",
            "바닥 점검 패널과 벽면 구조 리브",
            "조종실, 동력실, 무기실 방향을 구분하는 색상 포털 프레임과 바닥 가이드",
        ],
        "excludedParts": [
            "실제 대형 메인 스크린 UI",
            "구역 상태 표시 화면",
            "CCTV 피드 화면과 전환 로직",
            "복도 폐쇄 상태 로직",
            "침입 감지 시스템 로직",
            "화물 손상 경고 시스템 로직",
            "Unity 씬 배치",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "CR-01",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산, UI 흐름에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# control_room_shell

CR-01 통제실 룸 셸 승인용 Blender 샘플입니다.

## 목적

통제실 작업을 Unity에 연결하기 전에, 통제실의 기본 방 형태와 출입구 방향을 검사하기 위한 샘플입니다.
이 샘플은 룸 셸만 다루며 실제 스크린 UI, CCTV 로직, 구역 상태 로직은 포함하지 않습니다.

## 반영 기준

- 벽과 바닥이 있는 통제실 구역이며, 내부 확인이 쉽도록 천장은 제외했습니다.
- 통제실 내부에 스크린 쪽과 입구 쪽을 나누는 가벽을 두고, 가벽에는 출입 가능한 문을 열어 두었습니다.
- 가벽 위치는 유지하고, 조종실과 동력실 복도가 모두 들어갈 수 있도록 6시 방향 아래쪽 입구 영역을 넓혔습니다.
- 조종실과 동력실 방향 출입구는 위에서 내려다본 기준 왼쪽에 몰아 배치했으며 서로 조금 떨어져 있습니다.
- 조종실 쪽 문은 40도 정도 기울어진 형태로 만들었습니다.
- 운송창고와 무기실 방향 출입구는 위에서 내려다본 기준 6시 방향에 바로 옆에 붙여 배치했습니다.
- 각 출입구 밖에는 짧은 복도 스텁을 두어 Unity 배치 시 연결 방향을 확인할 수 있게 했습니다.
- 각 방향 출입구는 색상 포털 프레임, 큰 방향 표시판, 바닥 가이드로 더 분리해서 보이도록 했습니다.
- 북쪽 벽에는 향후 CR-06 대형 메인 스크린을 넣을 수 있는 비기능성 빈 벽면 베이를 두었습니다.
- 바닥 점검 패널과 벽면 구조 리브로 관제실 분위기를 드러냈습니다.

## 포함

- `blender/control_room_shell.blend`
- `exports/control_room_shell.fbx`
- `exports/control_room_shell.glb`
- `renders/*.png` 6개 구도
- `index.html`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- 실제 대형 메인 스크린 UI
- 구역 상태 표시 화면
- CCTV 피드 화면과 전환 로직
- 복도 폐쇄, 침입 감지, 화물 손상 경고 로직
- Unity 씬 배치와 충돌 설정
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_overview.png", "01 전체 통제실 룸 셸"),
        ("02_floor_plan.png", "02 상단 배치와 출입구 방향"),
        ("03_main_wall_shell.png", "03 비기능성 메인 스크린 벽면 베이"),
        ("04_cargo_entry.png", "04 이송창고 방향 출입구"),
        ("05_side_entries.png", "05 왼쪽 조종실 40도 문과 동력실 문"),
        ("06_ceiling_and_wall_panels.png", "06 열린 상부와 벽면 패널"),
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
  <title>control_room_shell review</title>
  <style>
    body {{ margin: 0; background: #111414; color: #e9e2d4; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #c9c0ad; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #3d4544; background: #1b2020; padding: 10px; }}
    img {{ width: 100%; display: block; background: #0b0e0e; }}
    figcaption {{ margin-top: 8px; color: #ded2bc; font-size: 14px; }}
    @media (max-width: 800px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>control_room_shell</h1>
  <p>CR-01 통제실 룸 셸 승인용 샘플입니다. 내부 확인이 쉽도록 천장은 제외했고, 스크린 쪽과 입구 쪽을 나누는 문 달린 가벽을 추가했습니다. 가벽 위치는 유지한 채 통제실을 6시 방향으로 넓혀 아래쪽 입구 영역을 크게 확보했습니다. 위에서 내려다본 기준 왼쪽에는 서로 조금 떨어진 조종실 40도 문과 동력실 문을 배치했고, 6시 방향에는 운송창고와 무기실 복도를 바로 옆에 붙여 배치했습니다. 실제 스크린 UI, CCTV, 복도 폐쇄, 침입 감지, 화물 손상 경고 기능은 아직 포함하지 않았습니다.</p>
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
        "floor": noisy_metal("control room worn sealed deck", (0.14, 0.17, 0.17, 1)),
        "floor_panel": noisy_metal("control room removable floor panel", (0.19, 0.22, 0.21, 1)),
        "deck_rib": noisy_metal("control room raised deck rib", (0.07, 0.09, 0.09, 1)),
        "corridor_floor": noisy_metal("control room corridor continuation steel", (0.16, 0.19, 0.19, 1)),
        "wall": noisy_metal("control room thick armored wall", (0.20, 0.25, 0.25, 1)),
        "wall_dark": noisy_metal("control room dark corridor wall", (0.11, 0.14, 0.15, 1)),
        "door_frame": noisy_metal("control room heavy doorway frame", (0.32, 0.34, 0.31, 1)),
        "partition": noisy_metal("control room internal partition wall", (0.16, 0.20, 0.20, 1)),
        "blank_panel": material("inactive blank future screen glass", (0.025, 0.055, 0.060, 1), roughness=0.35, emission=(0.01, 0.06, 0.07, 1), emission_strength=0.18),
        "ceiling": noisy_metal("control room sealed ceiling plate", (0.13, 0.16, 0.16, 1)),
        "beam": noisy_metal("control room ceiling structural beam", (0.27, 0.30, 0.28, 1)),
        "conduit": noisy_metal("control room cable tray conduit", (0.045, 0.055, 0.055, 1)),
        "label_plate": noisy_metal("control room black direction label plate", (0.025, 0.030, 0.030, 1)),
        "label_text": material("control room pale label text", (0.78, 0.88, 0.84, 1), roughness=0.70, emission=(0.18, 0.35, 0.30, 1), emission_strength=0.10),
        "cargo_marker": material("cargo bay direction muted green marker", (0.18, 0.42, 0.30, 1), roughness=0.72, emission=(0.04, 0.18, 0.10, 1), emission_strength=0.18),
        "cockpit_marker": material("cockpit direction blue marker", (0.12, 0.28, 0.58, 1), roughness=0.70, emission=(0.03, 0.10, 0.28, 1), emission_strength=0.20),
        "engine_marker": material("engine room direction amber marker", (0.72, 0.42, 0.12, 1), roughness=0.74, emission=(0.26, 0.12, 0.02, 1), emission_strength=0.20),
        "weapon_marker": material("weapon room direction red marker", (0.58, 0.14, 0.13, 1), roughness=0.75, emission=(0.22, 0.03, 0.025, 1), emission_strength=0.20),
    }

    build_control_room_shell(mats)
    add_render_lights()

    cameras = [
        ("overview", (7.9, -8.8, 4.5), (0.0, -0.9, 1.25), 32, "01_overview.png", None),
        ("floor_plan", (0.0, ROOM_CENTER_Y, 12.0), (0.0, ROOM_CENTER_Y, 0.0), 50, "02_floor_plan.png", 12.2),
        ("main_wall_shell", (0.0, -6.8, 2.20), (0.0, 3.0, 1.55), 31, "03_main_wall_shell.png", None),
        ("cargo_entry", (0.0, -10.0, 2.15), (0.0, -3.6, 1.05), 32, "04_cargo_entry.png", None),
        ("side_entries", (-8.5, -4.35, 2.55), (-3.75, -2.25, 1.22), 33, "05_side_entries.png", None),
        ("ceiling_and_wall_panels", (-6.4, 4.8, 3.05), (0.0, 0.9, 2.12), 36, "06_ceiling_and_wall_panels.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} Blender sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
