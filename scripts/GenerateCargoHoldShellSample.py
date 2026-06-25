from __future__ import annotations

import json
import math
import shutil
from datetime import date
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "cargo_hold_shell"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
CH11_DISPLAY_TEXTURE_PATH = PROJECT_ROOT / "Assets" / "Heavy Station Kit" / "BASE" / "Textures" / "Displays" / "B2_Eq2_E.png"
CH11_DISPLAY_CROP_PATH = TEXTURE_DIR / "B2_Eq2_E_bottom_right.png"

ROOM_WIDTH = 9.8
ROOM_NORTH_Y = 4.35
ROOM_SOUTH_Y = -4.35
ROOM_DEPTH = ROOM_NORTH_Y - ROOM_SOUTH_Y
ROOM_CENTER_Y = (ROOM_NORTH_Y + ROOM_SOUTH_Y) * 0.5
ROOM_HEIGHT = 3.2
FLOOR_THICKNESS = 0.18
WALL_THICKNESS = 0.34
DOOR_WIDTH = 1.55
DOOR_HEIGHT = 2.12

COCKPIT_DOOR_X = 0.0
CONTROL_DOOR_Y = 0.0
ENGINE_DOOR_Y = 0.0
SUPPLY_7_OCLOCK_X = -3.58
ARMORY_5_OCLOCK_X = 3.58


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR, TEXTURE_DIR):
        path.mkdir(parents=True, exist_ok=True)


def clean_generated_files() -> None:
    for directory in (BLENDER_DIR, RENDER_DIR, EXPORT_DIR, TEXTURE_DIR):
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
    roughness: float = 0.82,
    emission: tuple[float, float, float, float] | None = None,
    emission_strength: float = 0.0,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    set_principled_input(mat, "Base Color", color)
    set_principled_input(mat, "Metallic", metallic)
    set_principled_input(mat, "Roughness", roughness)
    if emission is not None:
        set_principled_input(mat, "Emission Color", emission)
        set_principled_input(mat, "Emission Strength", emission_strength)
    mat.diffuse_color = color
    return mat


def noisy_metal(name: str, base: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = material(name, base, metallic=0.22, roughness=0.88)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 34
    noise.inputs["Detail"].default_value = 9
    noise.inputs["Roughness"].default_value = 0.58
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.16
    ramp.color_ramp.elements[0].color = (base[0] * 0.50, base[1] * 0.50, base[2] * 0.50, 1)
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


def image_emission_material(
    name: str,
    image_path: Path,
    *,
    emission_strength: float = 0.42,
) -> bpy.types.Material:
    mat = material(
        name,
        (0.02, 0.07, 0.09, 1),
        roughness=0.36,
        emission=(0.02, 0.18, 0.22, 1),
        emission_strength=emission_strength,
    )
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    image = bpy.data.images.load(str(image_path), check_existing=True)
    texture = nodes.new(type="ShaderNodeTexImage")
    texture.image = image
    texture.extension = "CLIP"
    texture.interpolation = "Closest"
    links.new(texture.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(texture.outputs["Color"], bsdf.inputs["Emission Color"])
    return mat


def crop_ch11_display_texture() -> Path:
    source = bpy.data.images.load(str(CH11_DISPLAY_TEXTURE_PATH), check_existing=True)
    width = int(source.size[0])
    height = int(source.size[1])
    crop_width = width // 2
    crop_height = int((height // 2) * 0.44)
    start_x = width - crop_width
    start_y = 0

    source_pixels = list(source.pixels[:])
    cropped_pixels = [0.0] * (crop_width * crop_height * 4)
    for y in range(crop_height):
        source_start = ((start_y + y) * width + start_x) * 4
        target_start = y * crop_width * 4
        cropped_pixels[target_start:target_start + (crop_width * 4)] = source_pixels[source_start:source_start + (crop_width * 4)]

    cropped = bpy.data.images.new("CH-11 B2_Eq2_E bottom right display crop", crop_width, crop_height, alpha=True)
    cropped.pixels[:] = cropped_pixels
    cropped.filepath_raw = str(CH11_DISPLAY_CROP_PATH)
    cropped.file_format = "PNG"
    cropped.save()
    return CH11_DISPLAY_CROP_PATH


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


def add_yz_image_plane(
    name: str,
    parent: bpy.types.Object,
    x: float,
    y: float,
    z: float,
    width: float,
    height: float,
    mat: bpy.types.Material,
    uv_rect: tuple[float, float, float, float],
) -> bpy.types.Object:
    half_width = width * 0.5
    half_height = height * 0.5
    mesh = bpy.data.meshes.new(name + " mesh")
    mesh.from_pydata(
        [
            (x, y - half_width, z - half_height),
            (x, y + half_width, z - half_height),
            (x, y + half_width, z + half_height),
            (x, y - half_width, z + half_height),
        ],
        [],
        [(0, 3, 2, 1)],
    )
    mesh.update()

    u0, v0, u1, v1 = uv_rect
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for loop_index, uv in zip(
        mesh.polygons[0].loop_indices,
        [(u0, v0), (u0, v1), (u1, v1), (u1, v0)],
    ):
        uv_layer.data[loop_index].uv = uv

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.parent = parent
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
    size: float = 0.22,
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


def add_wall_y_with_doors(
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
        add_box(f"{name} {label} doorway left frame", root, (center - DOOR_WIDTH * 0.5, y, DOOR_HEIGHT * 0.5), (0.18, WALL_THICKNESS + 0.12, DOOR_HEIGHT), mats["door_frame"])
        add_box(f"{name} {label} doorway right frame", root, (center + DOOR_WIDTH * 0.5, y, DOOR_HEIGHT * 0.5), (0.18, WALL_THICKNESS + 0.12, DOOR_HEIGHT), mats["door_frame"])


def add_wall_x_with_door(
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


def add_wall_x_with_door_and_south_corner_gap(
    root: bpy.types.Object,
    name: str,
    x: float,
    door_y: float,
    mats: dict[str, bpy.types.Material],
) -> None:
    lower_limit = DIAGONAL_CORNER_GAP_START
    lower_depth = door_y - lower_limit - DOOR_WIDTH * 0.5
    upper_depth = ROOM_NORTH_Y - door_y - DOOR_WIDTH * 0.5
    z_mid = ROOM_HEIGHT * 0.5
    if lower_depth > 0.05:
        add_box(f"{name} lower sealed wall", root, (x, lower_limit + lower_depth * 0.5, z_mid), (WALL_THICKNESS, lower_depth, ROOM_HEIGHT), mats["wall"])
    if upper_depth > 0.05:
        add_box(f"{name} upper sealed wall", root, (x, door_y + DOOR_WIDTH * 0.5 + upper_depth * 0.5, z_mid), (WALL_THICKNESS, upper_depth, ROOM_HEIGHT), mats["wall"])
    header_height = ROOM_HEIGHT - DOOR_HEIGHT
    add_box(f"{name} doorway upper header", root, (x, door_y, DOOR_HEIGHT + header_height * 0.5), (WALL_THICKNESS, DOOR_WIDTH, header_height), mats["wall"])
    add_box(f"{name} doorway lower frame", root, (x, door_y - DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.12, 0.18, DOOR_HEIGHT), mats["door_frame"])
    add_box(f"{name} doorway upper frame", root, (x, door_y + DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.12, 0.18, DOOR_HEIGHT), mats["door_frame"])


def add_south_wall_with_corner_gaps(
    root: bpy.types.Object,
    name: str,
    mats: dict[str, bpy.types.Material],
) -> None:
    sealed_width = ROOM_WIDTH - 4.95
    add_box(
        f"{name} sealed center wall between 5 and 7 oclock entries",
        root,
        (0.0, ROOM_SOUTH_Y, ROOM_HEIGHT * 0.5),
        (sealed_width, WALL_THICKNESS, ROOM_HEIGHT),
        mats["wall"],
    )
    for x in (-sealed_width * 0.5, sealed_width * 0.5):
        add_box(
            f"{name} diagonal corner gap end frame {x:+.2f}",
            root,
            (x, ROOM_SOUTH_Y, DOOR_HEIGHT * 0.5),
            (0.18, WALL_THICKNESS + 0.12, DOOR_HEIGHT),
            mats["door_frame"],
        )


def add_floor_grid(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    for x in (-3.85, -2.55, -1.25, 1.25, 2.55, 3.85):
        add_box(f"CH-01 removable cargo hold deck plate {x:+.2f}", root, (x, ROOM_CENTER_Y, 0.115), (1.05, ROOM_DEPTH - 0.90, 0.042), mats["floor_panel"], bevel_width=0.006)
    for y in (-3.55, -2.40, -1.25, 0.0, 1.25, 2.40, 3.55):
        add_box(f"CH-01 transverse cargo deck rib {y:+.2f}", root, (0, y, 0.152), (ROOM_WIDTH - 0.82, 0.046, 0.048), mats["deck_rib"], bevel_width=0.002)


def add_corridor_stub(
    root: bpy.types.Object,
    name: str,
    center: tuple[float, float, float],
    scale: tuple[float, float, float],
    mats: dict[str, bpy.types.Material],
    side_axis: str,
    ramp_angle_y: float = 0.0,
) -> None:
    add_box(f"{name} corridor floor continuation", root, center, scale, mats["corridor_floor"], (0, ramp_angle_y, 0), 0.012)
    if side_axis == "x":
        for side in (-0.95, 0.95):
            add_box(f"{name} corridor side wall {side:+.2f}", root, (center[0] + side, center[1], 1.04), (0.22, scale[1], 2.08), mats["wall_dark"], (0, ramp_angle_y, 0), 0.012)
    else:
        for side in (-0.95, 0.95):
            add_box(f"{name} corridor side wall {side:+.2f}", root, (center[0], center[1] + side, 1.04), (scale[0], 0.22, 2.08), mats["wall_dark"], (0, ramp_angle_y, 0), 0.012)


def add_diagonal_corridor_stub(
    root: bpy.types.Object,
    name: str,
    start: tuple[float, float, float],
    direction: tuple[float, float],
    mats: dict[str, bpy.types.Material],
    marker_mat: bpy.types.Material,
    label: str,
) -> None:
    length = 2.70
    width = DOOR_WIDTH + 0.34
    dir_v = Vector((direction[0], direction[1], 0.0)).normalized()
    perp_v = Vector((-dir_v.y, dir_v.x, 0.0))
    angle = math.atan2(dir_v.y, dir_v.x)
    start_v = Vector(start)
    center = start_v + dir_v * (length * 0.5)

    add_box(
        f"{name} diagonal corridor floor continuation",
        root,
        (center.x, center.y, -0.055),
        (length, width, FLOOR_THICKNESS),
        mats["corridor_floor"],
        (0, 0, angle),
        0.012,
    )
    for side in (-1, 1):
        wall_center = center + perp_v * (width * 0.5 * side)
        add_box(
            f"{name} diagonal corridor side wall {side:+d}",
            root,
            (wall_center.x, wall_center.y, 1.04),
            (length, 0.22, 2.08),
            mats["wall_dark"],
            (0, 0, angle),
            0.012,
        )

    header_height = ROOM_HEIGHT - DOOR_HEIGHT
    for side in (-1, 1):
        post_center = start_v + perp_v * (DOOR_WIDTH * 0.5 * side)
        add_box(
            f"{name} diagonal doorway vertical frame {side:+d}",
            root,
            (post_center.x, post_center.y, DOOR_HEIGHT * 0.5),
            (0.18, 0.18, DOOR_HEIGHT),
            mats["door_frame"],
            (0, 0, angle),
            0.010,
        )
    header_center = start_v + dir_v * 0.05
    add_box(
        f"{name} diagonal doorway upper header",
        root,
        (header_center.x, header_center.y, DOOR_HEIGHT + header_height * 0.5),
        (0.22, DOOR_WIDTH + 0.16, header_height),
        mats["wall"],
        (0, 0, angle),
        0.010,
    )
    add_box(
        f"{name} diagonal colored direction threshold",
        root,
        (start_v.x + dir_v.x * 0.22, start_v.y + dir_v.y * 0.22, 0.225),
        (0.70, DOOR_WIDTH + 0.36, 0.050),
        marker_mat,
        (0, 0, angle),
        0.006,
    )
    label_center = start_v + dir_v * 0.62
    add_text_label(
        f"{name} diagonal floor label",
        root,
        label,
        (label_center.x, label_center.y, 0.255),
        (0, 0, angle),
        mats["label_text"],
        0.18,
    )


def add_direction_marker(
    root: bpy.types.Object,
    name: str,
    text: str,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    plate_rot_z: float,
    arrow_rot_z: float,
    text_rot_z: float,
    mat: bpy.types.Material,
    mats: dict[str, bpy.types.Material],
) -> None:
    def offset(base: tuple[float, float, float], right: float, forward: float, rot_z: float, z_delta: float = 0.0) -> tuple[float, float, float]:
        cos_z = math.cos(rot_z)
        sin_z = math.sin(rot_z)
        return (
            base[0] + right * cos_z - forward * sin_z,
            base[1] + right * sin_z + forward * cos_z,
            base[2] + z_delta,
        )

    plate_rot = (0, 0, plate_rot_z)
    arrow_rot = (0, 0, arrow_rot_z)
    text_rot = (0, 0, text_rot_z)
    label_back_width = min(scale[0] * 0.72, 1.34)

    add_box(f"{name} colored direction plate", root, loc, scale, mat, plate_rot, bevel_width=0.006)
    add_box(
        f"{name} dark recessed label backing",
        root,
        offset(loc, 0.0, -0.13, plate_rot_z, 0.042),
        (label_back_width, 0.18, 0.034),
        mats["marker_backing"],
        plate_rot,
        bevel_width=0.004,
    )
    add_box(
        f"{name} pale arrow stem",
        root,
        offset(loc, 0.0, 0.13, arrow_rot_z, 0.056),
        (0.085, 0.32, 0.032),
        mats["marker_arrow"],
        arrow_rot,
        bevel_width=0.003,
    )
    add_cylinder(
        f"{name} pale arrow head",
        root,
        offset(loc, 0.0, 0.36, arrow_rot_z, 0.058),
        0.18,
        0.034,
        mats["marker_arrow"],
        (0, 0, arrow_rot_z + math.radians(30)),
        3,
    )
    trim_forward = scale[1] * 0.5 - 0.035
    add_box(
        f"{name} worn front trim",
        root,
        offset(loc, 0.0, trim_forward, plate_rot_z, 0.070),
        (scale[0] * 0.92, 0.035, 0.026),
        mats["marker_wear"],
        plate_rot,
        bevel_width=0.002,
    )
    add_box(
        f"{name} worn rear trim",
        root,
        offset(loc, 0.0, -trim_forward, plate_rot_z, 0.070),
        (scale[0] * 0.92, 0.035, 0.026),
        mats["marker_wear"],
        plate_rot,
        bevel_width=0.002,
    )
    add_text_label(f"{name} raised floor label", root, text, offset(loc, 0.0, -0.13, text_rot_z, 0.088), text_rot, mats["label_text"], 0.15)


def add_cargo_container(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_box("CH-02 central cargo placement recessed deck outline", root, (0, 0, 0.212), (4.85, 3.20, 0.055), mats["cargo_zone"], bevel_width=0.010)
    add_box("CH-03 single visible cargo container main body", root, (0, 0, 0.92), (3.75, 2.12, 1.55), mats["container"], bevel_width=0.035)
    add_box("CH-03 container left reinforced side panel", root, (-1.96, 0, 0.96), (0.12, 2.20, 1.38), mats["container_dark"], bevel_width=0.012)
    add_box("CH-03 container right reinforced side panel", root, (1.96, 0, 0.96), (0.12, 2.20, 1.38), mats["container_dark"], bevel_width=0.012)
    for x in (-1.16, 0.0, 1.16):
        add_box(f"CH-03 single container vertical rib {x:+.2f}", root, (x, -1.10, 0.96), (0.08, 0.10, 1.43), mats["container_rib"], bevel_width=0.006)
        add_box(f"CH-03 single container rear vertical rib {x:+.2f}", root, (x, 1.10, 0.96), (0.08, 0.10, 1.43), mats["container_rib"], bevel_width=0.006)
    for z in (0.38, 0.94, 1.50):
        add_box(f"CH-03 single container front horizontal seam {z:+.2f}", root, (0, -1.115, z), (3.62, 0.06, 0.045), mats["container_rib"], bevel_width=0.003)
        add_box(f"CH-03 single container rear horizontal seam {z:+.2f}", root, (0, 1.115, z), (3.62, 0.06, 0.045), mats["container_rib"], bevel_width=0.003)
    for x in (-1.55, 1.55):
        add_cylinder(f"CH-03 recessed tie down cap front {x:+.2f}", root, (x, -1.17, 1.48), 0.065, 0.032, mats["container_rib"], (math.radians(90), 0, 0), 20)
        add_cylinder(f"CH-03 recessed tie down cap rear {x:+.2f}", root, (x, 1.17, 1.48), 0.065, 0.032, mats["container_rib"], (math.radians(90), 0, 0), 20)
    add_text_label("CH-03 container single-load label", root, "SINGLE CARGO LOAD", (0, -1.185, 1.12), (math.radians(90), 0, 0), mats["label_text"], 0.20)


def add_edge_walkway(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    # Visible ring-like movement area around the container without adding cargo-handling devices.
    add_box("CH-04 forward edge walkway band", root, (0, 2.62, 0.245), (6.70, 0.58, 0.075), mats["walkway"], bevel_width=0.006)
    add_box("CH-04 rear edge walkway band", root, (0, -2.62, 0.245), (6.70, 0.58, 0.075), mats["walkway"], bevel_width=0.006)
    add_box("CH-04 west edge walkway band", root, (-3.20, 0, 0.245), (0.58, 4.70, 0.075), mats["walkway"], bevel_width=0.006)
    add_box("CH-04 east edge walkway band", root, (3.20, 0, 0.245), (0.58, 4.70, 0.075), mats["walkway"], bevel_width=0.006)
    for x in (-3.45, -1.72, 1.72, 3.45):
        add_box(f"CH-04 yellow walkway width tick {x:+.2f}", root, (x, 2.18, 0.305), (0.12, 0.62, 0.045), mats["hazard"], (0, 0, math.radians(22)), 0.002)
        add_box(f"CH-04 rear yellow walkway width tick {x:+.2f}", root, (x, -2.18, 0.305), (0.12, 0.62, 0.045), mats["hazard"], (0, 0, math.radians(-22)), 0.002)


def add_status_panel(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    x = ROOM_WIDTH * 0.5 - 0.22
    y = -2.25
    add_box("CH-11 wall mounted cargo status panel body", root, (x, y, 1.62), (0.16, 1.42, 0.96), mats["panel_body"], bevel_width=0.018)
    add_box("CH-11 cargo status glowing screen", root, (x - 0.088, y, 1.68), (0.045, 1.08, 0.66), mats["screen"], bevel_width=0.006)
    add_yz_image_plane(
        "CH-11 B2_Eq2_E bottom right display surface",
        root,
        x - 0.116,
        y,
        1.68,
        1.02,
        0.58,
        mats["screen_display"],
        (1.0, 0.0, 0.0, 1.0),
    )


def add_wall_dressing(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    for x in (-4.0, -2.0, 0.0, 2.0, 4.0):
        add_box(f"CH-01 north cargo wall vertical rib {x:+.2f}", root, (x, ROOM_NORTH_Y - 0.16, 1.64), (0.10, 0.14, 2.50), mats["beam"], bevel_width=0.004)
    for x in (-2.0, 0.0, 2.0):
        add_box(f"CH-01 south cargo wall vertical rib {x:+.2f}", root, (x, ROOM_SOUTH_Y + 0.16, 1.64), (0.10, 0.14, 2.50), mats["beam"], bevel_width=0.004)
    for y in (-3.2, -1.6, 0.0, 1.6, 3.2):
        add_cylinder_between("CH-01 west utility conduit", root, (-ROOM_WIDTH * 0.5 + 0.14, y, 2.55), (-ROOM_WIDTH * 0.5 + 0.14, y + 0.75, 2.55), 0.027, mats["conduit"], 14)
        add_cylinder_between("CH-01 east utility conduit", root, (ROOM_WIDTH * 0.5 - 0.14, y, 2.55), (ROOM_WIDTH * 0.5 - 0.14, y + 0.75, 2.55), 0.027, mats["conduit"], 14)
    add_text_label("CH-04 walkway floor label", root, "EDGE WALKWAY", (0, 2.62, 0.335), (0, 0, 0), mats["label_text"], 0.20)
    add_text_label("CH-02 central cargo zone label", root, "CENTRAL CARGO ZONE", (0, -2.05, 0.335), (0, 0, 0), mats["label_text"], 0.20)


def build_cargo_hold_shell(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("CH-01 cargo hold shell Blender sample")
    add_box("CH-01 sealed cargo hold deck floor", root, (0, ROOM_CENTER_Y, 0), (ROOM_WIDTH, ROOM_DEPTH, FLOOR_THICKNESS), mats["floor"], bevel_width=0.018)
    add_wall_y_with_doors(root, "CH-05 north 12 oclock cockpit", ROOM_NORTH_Y, [("cockpit", COCKPIT_DOOR_X)], mats)
    add_wall_y_with_doors(
        root,
        "CH-09 CH-08 south end supply and armory",
        ROOM_SOUTH_Y,
        [("supply room 7 oclock", SUPPLY_7_OCLOCK_X), ("armory 5 oclock", ARMORY_5_OCLOCK_X)],
        mats,
    )
    add_wall_x_with_door(root, "CH-06 west 9 oclock engine room", -ROOM_WIDTH * 0.5, ENGINE_DOOR_Y, mats)
    add_wall_x_with_door(root, "CH-07 east 3 oclock control room", ROOM_WIDTH * 0.5, CONTROL_DOOR_Y, mats)

    add_corridor_stub(root, "CH-05 cockpit 12 oclock", (COCKPIT_DOOR_X, ROOM_NORTH_Y + 1.05, 0.0), (DOOR_WIDTH + 0.32, 2.10, FLOOR_THICKNESS), mats, "x")
    add_corridor_stub(root, "CH-07 control 3 oclock", (ROOM_WIDTH * 0.5 + 1.10, CONTROL_DOOR_Y, 0.0), (2.18, DOOR_WIDTH + 0.32, FLOOR_THICKNESS), mats, "y")
    add_corridor_stub(root, "CH-06 engine 9 oclock", (-ROOM_WIDTH * 0.5 - 1.10, ENGINE_DOOR_Y, 0.0), (2.18, DOOR_WIDTH + 0.32, FLOOR_THICKNESS), mats, "y")
    add_corridor_stub(root, "CH-08 armory 5 oclock south end", (ARMORY_5_OCLOCK_X, ROOM_SOUTH_Y - 1.05, 0.0), (DOOR_WIDTH + 0.32, 2.10, FLOOR_THICKNESS), mats, "x")
    add_corridor_stub(root, "CH-09 supply 7 oclock south end", (SUPPLY_7_OCLOCK_X, ROOM_SOUTH_Y - 1.05, 0.0), (DOOR_WIDTH + 0.32, 2.10, FLOOR_THICKNESS), mats, "x")

    add_floor_grid(root, mats)
    add_edge_walkway(root, mats)
    add_cargo_container(root, mats)
    add_status_panel(root, mats)
    add_wall_dressing(root, mats)

    marker_scale = (DOOR_WIDTH + 0.42, 0.58, 0.050)
    add_direction_marker(root, "CH-10 cockpit 12 oclock direction marker", "COCKPIT", (COCKPIT_DOOR_X, ROOM_NORTH_Y - 0.46, 0.225), marker_scale, 0, 0, 0, mats["cockpit_marker"], mats)
    add_direction_marker(root, "CH-10 control 3 oclock direction marker", "CONTROL", (ROOM_WIDTH * 0.5 - 0.46, CONTROL_DOOR_Y, 0.225), marker_scale, math.radians(90), math.radians(-90), math.radians(90), mats["control_marker"], mats)
    add_direction_marker(root, "CH-10 engine 9 oclock direction marker", "ENGINE", (-ROOM_WIDTH * 0.5 + 0.46, ENGINE_DOOR_Y, 0.225), marker_scale, math.radians(90), math.radians(90), math.radians(90), mats["engine_marker"], mats)
    add_direction_marker(root, "CH-10 armory 5 oclock direction marker", "ARMORY", (ARMORY_5_OCLOCK_X, ROOM_SOUTH_Y + 0.46, 0.225), marker_scale, 0, math.radians(180), 0, mats["armory_marker"], mats)
    add_direction_marker(root, "CH-10 supply 7 oclock direction marker", "SUPPLY", (SUPPLY_7_OCLOCK_X, ROOM_SOUTH_Y + 0.46, 0.225), marker_scale, 0, math.radians(180), 0, mats["supply_marker"], mats)


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("CargoHoldShellWorld")
    scene.world.color = (0.010, 0.011, 0.013)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, 0, 6.1))
    top = bpy.context.object
    top.name = "large cargo hold overhead inspection softbox"
    top.data.energy = 720
    top.data.size = 8.0

    bpy.ops.object.light_add(type="AREA", location=(-6.0, -2.4, 3.0))
    west = bpy.context.object
    west.name = "warm armory-side cargo hold fill"
    west.data.energy = 185
    west.data.size = 3.7
    west.data.color = (1.0, 0.73, 0.45)

    bpy.ops.object.light_add(type="AREA", location=(6.0, 2.3, 3.0))
    east = bpy.context.object
    east.name = "cool supply-side cargo hold fill"
    east.data.energy = 190
    east.data.size = 3.7
    east.data.color = (0.55, 0.76, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(ROOM_WIDTH * 0.5 - 0.55, -2.25, 1.80))
    status = bpy.context.object
    status.name = "dim cargo status panel glow"
    status.data.energy = 120
    status.data.color = (0.38, 0.82, 0.95)


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
    camera.name = "cargo hold shell camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "cargo_hold_shell.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "cargo_hold_shell.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "cargo_hold_shell.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "CH-01",
        "title": "운송창고 룸 셸",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/CARGO_HOLD_OBJECTS.md - CH-01 운송창고 룸 셸, CH-03 단일 컨테이너 화물, CH-04 가장자리 이동 영역, CH-05~CH-09 연결 지점, CH-10 연결 방향 표시.",
            "docs/CARGO_HOLD_OBJECTS.md - 사용자 확정: 중앙 화물은 여러 개 의뢰를 받아도 단일 컨테이너로 보이게 한다.",
            "docs/CARGO_HOLD_OBJECTS.md - 화물 직접 집기, 운반, 납품 상호작용은 구현하지 않는다.",
        ],
        "generatedFiles": [
            "blender/cargo_hold_shell.blend",
            "exports/cargo_hold_shell.fbx",
            "exports/cargo_hold_shell.glb",
            "renders/01_overview.png",
            "renders/02_floor_plan.png",
            "renders/03_single_container.png",
            "renders/04_edge_walkway.png",
            "renders/05_connection_points.png",
            "renders/06_status_display.png",
            "textures/B2_Eq2_E_bottom_right.png",
            "index.html",
            "README.md",
            "ASSET_MANIFEST.json",
            "APPROVAL_STATUS.json",
        ],
        "includedParts": [
            "천장을 제외한 운송창고 바닥과 벽 셸",
            "여러 의뢰를 받아도 하나로 보이는 중앙 단일 컨테이너 화물",
            "중앙 컨테이너 주변 가장자리 이동 영역",
            "12시 조종실, 3시 통제실, 9시 동력실, 5시 무기실, 7시 비품실 방향 연결 출입구와 복도 스텁",
            "각 연결 방향을 보여주는 CH-10 영어 방향 표시, 화살표, 색상 출입구 프레임",
            "CH-11 벽면 상태 패널과 B2_Eq2_E.png 오른쪽 아래 디스플레이의 실제 UI 영역 크롭 적용 화면",
            "Blender 원본 모델, FBX, GLB 범용 모델 파일",
        ],
        "excludedParts": [
            "화물 직접 집기/운반/납품 상호작용",
            "크레인, 지게차, 컨베이어, 선반, 팔레트 랙",
            "운송창고 자동 포탑",
            "개별 화물 종류별 모델",
            "Unity 씬 배치와 충돌 설정",
            "런타임 게임 로직",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "CH-01",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산, UI 흐름에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# cargo_hold_shell

CH-01 운송창고 룸 셸 승인용 Blender 샘플입니다.

## 목적

운송창고를 Unity에 연결하기 전에, 방의 기본 형태와 중앙 화물, 가장자리 이동 영역, 5개 연결 지점, CH-10 연결 방향 표시, 화물 상태 표시 지점의 위치 관계를 검사하기 위한 샘플입니다.
이 샘플은 룸 셸 중심이며 실제 화물 집기, 운반, 납품, 정산 로직, 런타임 UI는 포함하지 않습니다.

## 반영 기준

- 중앙에는 여러 의뢰를 받아도 하나로 보이는 단일 컨테이너 화물을 두었습니다.
- 중앙 컨테이너 주변에는 플레이어가 이동할 수 있는 가장자리 이동 영역을 표시했습니다.
- 위에서 내려다본 기준으로 12시에는 조종실, 3시에는 통제실, 9시에는 동력실, 5시에는 무기실, 7시에는 비품실 방향 연결 출입구와 복도 스텁을 두었습니다.
- 각 연결 지점 안쪽에는 `CH-10` 영어 방향 표시, 진행 화살표, 어두운 라벨 백킹, 마모 트림을 넣었습니다.
- CH-11 벽면 패널 화면에는 `B2_Eq2_E.png` 오른쪽 아래 디스플레이의 실제 UI 영역만 잘라 넣었습니다.
- 내부 구조 확인이 쉽도록 천장은 제외했습니다.

## 포함

- `blender/cargo_hold_shell.blend`
- `exports/cargo_hold_shell.fbx`
- `exports/cargo_hold_shell.glb`
- `renders/*.png` 6개 구도
- `textures/B2_Eq2_E_bottom_right.png`
- `index.html`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- 화물 직접 집기/운반/납품 상호작용
- 크레인, 지게차, 컨베이어, 선반, 팔레트 랙
- 운송창고 자동 포탑
- 개별 화물 종류별 모델
- Unity 씬 배치와 충돌 설정
- 런타임 게임 로직
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_overview.png", "01 전체 운송창고 룸 셸"),
        ("02_floor_plan.png", "02 상단 배치와 5방향 연결 지점"),
        ("03_single_container.png", "03 중앙 단일 컨테이너 화물"),
        ("04_edge_walkway.png", "04 중앙 컨테이너 주변 가장자리 이동 영역"),
        ("05_connection_points.png", "05 12시/3시/9시/5시/7시 연결 지점과 CH-10 방향 표시"),
        ("06_status_display.png", "06 B2 디스플레이가 적용된 CH-11 패널"),
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
  <title>cargo_hold_shell review</title>
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
  <h1>cargo_hold_shell</h1>
  <p>CH-01 운송창고 룸 셸 승인용 Blender 샘플입니다. 원본 기획서와 운송창고 오브젝트 목록 기준에 따라 중앙에는 여러 의뢰를 받아도 하나로 보이는 단일 컨테이너 화물을 배치했고, 그 주변에는 가장자리 이동 영역을 표시했습니다. 위에서 내려다본 기준으로 12시에는 조종실, 3시에는 통제실, 9시에는 동력실, 5시에는 무기실, 7시에는 비품실로 이어지는 5개 연결 지점을 넣고, 각 지점 안쪽에 CH-10 연결 방향 표시를 기존 룸 셸 샘플 안에 통합했습니다. CH-10 표시는 컬러 바닥 플레이트, 흰색 진행 화살표, 어두운 라벨 백킹, 돌출 텍스트, 마모 트림으로 구성했습니다. CH-11 상태 패널 화면에는 B2_Eq2_E.png 오른쪽 아래 디스플레이의 실제 UI 영역만 잘라 적용했습니다. 화물 직접 집기, 운반, 납품 상호작용과 크레인, 지게차, 컨베이어, 선반, 자동 포탑은 원본 기준 운송창고 고정 오브젝트가 아니므로 포함하지 않았습니다.</p>
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
    display_texture = crop_ch11_display_texture()

    mats = {
        "floor": noisy_metal("cargo hold worn sealed deck", (0.15, 0.17, 0.16, 1)),
        "floor_panel": noisy_metal("cargo hold removable dark floor panel", (0.19, 0.20, 0.19, 1)),
        "deck_rib": noisy_metal("cargo hold raised deck rib", (0.08, 0.09, 0.09, 1)),
        "wall": noisy_metal("cargo hold thick armored wall", (0.22, 0.25, 0.24, 1)),
        "wall_dark": noisy_metal("cargo hold dark corridor wall", (0.10, 0.12, 0.12, 1)),
        "door_frame": noisy_metal("cargo hold heavy doorway frame", (0.34, 0.34, 0.30, 1)),
        "corridor_floor": noisy_metal("cargo hold corridor continuation steel", (0.15, 0.18, 0.18, 1)),
        "walkway": noisy_metal("cargo hold edge walkway raised band", (0.22, 0.27, 0.25, 1)),
        "cargo_zone": material("cargo hold central cargo zone muted amber", (0.48, 0.34, 0.12, 1), metallic=0.08, roughness=0.86),
        "container": noisy_metal("cargo hold single red-brown container", (0.42, 0.17, 0.12, 1)),
        "container_dark": noisy_metal("cargo hold container dark side panel", (0.24, 0.11, 0.09, 1)),
        "container_rib": noisy_metal("cargo hold container worn rib", (0.58, 0.41, 0.32, 1)),
        "panel_body": noisy_metal("cargo hold status panel body", (0.11, 0.13, 0.13, 1)),
        "screen": material("cargo hold status screen cyan glow", (0.020, 0.075, 0.090, 1), roughness=0.34, emission=(0.02, 0.23, 0.27, 1), emission_strength=0.42),
        "screen_display": image_emission_material("CH-11 B2_Eq2_E bottom right display", display_texture, emission_strength=0.54),
        "bar_slot": material("cargo hold status bar dark slot", (0.020, 0.025, 0.026, 1), roughness=0.74),
        "health_bar": material("cargo hold health status green", (0.20, 0.70, 0.42, 1), roughness=0.45, emission=(0.05, 0.35, 0.13, 1), emission_strength=0.22),
        "loss_bar": material("cargo hold loss status red", (0.78, 0.16, 0.11, 1), roughness=0.50, emission=(0.35, 0.04, 0.02, 1), emission_strength=0.16),
        "score_bar": material("cargo hold score status blue", (0.20, 0.42, 0.74, 1), roughness=0.50, emission=(0.05, 0.13, 0.35, 1), emission_strength=0.18),
        "beam": noisy_metal("cargo hold structural rib", (0.30, 0.31, 0.28, 1)),
        "conduit": noisy_metal("cargo hold utility conduit", (0.045, 0.052, 0.052, 1)),
        "hazard": material("cargo hold muted hazard stripe", (0.86, 0.50, 0.12, 1), roughness=0.86),
        "label_text": material("cargo hold pale direction label text", (0.78, 0.88, 0.84, 1), roughness=0.70, emission=(0.16, 0.32, 0.29, 1), emission_strength=0.08),
        "marker_backing": material("CH-10 dark direction label backing", (0.030, 0.036, 0.038, 1), roughness=0.78),
        "marker_arrow": material("CH-10 worn pale direction arrow", (0.84, 0.89, 0.82, 1), roughness=0.68, emission=(0.22, 0.26, 0.22, 1), emission_strength=0.08),
        "marker_wear": material("CH-10 scraped direction marker trim", (0.70, 0.62, 0.46, 1), roughness=0.90),
        "cockpit_marker": material("cockpit direction blue marker", (0.13, 0.28, 0.58, 1), roughness=0.70, emission=(0.03, 0.09, 0.24, 1), emission_strength=0.18),
        "control_marker": material("control room direction purple marker", (0.32, 0.22, 0.56, 1), roughness=0.70, emission=(0.10, 0.05, 0.22, 1), emission_strength=0.15),
        "engine_marker": material("engine room direction green marker", (0.18, 0.42, 0.30, 1), roughness=0.72, emission=(0.04, 0.16, 0.10, 1), emission_strength=0.18),
        "armory_marker": material("armory direction red marker", (0.55, 0.18, 0.12, 1), roughness=0.72, emission=(0.16, 0.04, 0.02, 1), emission_strength=0.15),
        "supply_marker": material("supply room direction amber marker", (0.70, 0.42, 0.14, 1), roughness=0.74, emission=(0.25, 0.13, 0.03, 1), emission_strength=0.17),
    }

    build_cargo_hold_shell(mats)
    add_render_lights()

    cameras = [
        ("overview", (7.6, -7.2, 4.65), (0.0, 0.0, 1.12), 31, "01_overview.png", None),
        ("floor_plan", (0.0, 0.0, 13.2), (0.0, 0.0, 0.0), 50, "02_floor_plan.png", 13.5),
        ("single_container", (4.9, -4.9, 2.65), (0.0, 0.0, 1.05), 40, "03_single_container.png", None),
        ("edge_walkway", (0.0, -6.9, 4.05), (0.0, 0.0, 0.72), 34, "04_edge_walkway.png", None),
        ("connection_points", (0.0, 0.0, 12.4), (0.0, 0.0, 0.0), 50, "05_connection_points.png", 11.8),
        ("status_display", (2.65, -3.35, 2.35), (ROOM_WIDTH * 0.5 - 0.34, -2.25, 1.68), 72, "06_status_display.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} Blender sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
