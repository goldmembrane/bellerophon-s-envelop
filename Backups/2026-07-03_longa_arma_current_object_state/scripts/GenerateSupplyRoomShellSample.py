from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "supply_room_shell"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
HSK_OPEN_CLOSE_TEXTURE = PROJECT_ROOT / "Assets" / "Heavy Station Kit" / "_common" / "Textures" / "GUI" / "HSK_Open_Close.png"

ROOM_WIDTH = 7.2
ROOM_NORTH_Y = 2.9
ROOM_SOUTH_Y = -2.9
ROOM_DEPTH = ROOM_NORTH_Y - ROOM_SOUTH_Y
ROOM_CENTER_Y = (ROOM_NORTH_Y + ROOM_SOUTH_Y) * 0.5
ROOM_HEIGHT = 3.0
FLOOR_THICKNESS = 0.16
WALL_THICKNESS = 0.32
DOOR_WIDTH = 1.22
DOOR_HEIGHT = 2.05
WEST_X = -ROOM_WIDTH * 0.5
EAST_X = ROOM_WIDTH * 0.5
ARMORY_DOOR_Y = 1.08
CARGO_DOOR_Y = -1.18


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


def image_material(
    name: str,
    image_path: Path,
    fallback_color: tuple[float, float, float, float],
    *,
    emission_strength: float = 0.18,
    use_alpha: bool = True,
) -> bpy.types.Material:
    if not image_path.exists():
        raise FileNotFoundError(f"Missing texture image: {image_path}")

    mat = material(name, fallback_color, roughness=0.42, emission=fallback_color, emission_strength=emission_strength)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    texture = nodes.new(type="ShaderNodeTexImage")
    texture.name = name + " image texture"
    texture.image = bpy.data.images.load(str(image_path), check_existing=True)
    texture.extension = "CLIP"
    texture.interpolation = "Linear"
    links.new(texture.outputs["Color"], bsdf.inputs["Base Color"])
    if "Emission Color" in bsdf.inputs:
        links.new(texture.outputs["Color"], bsdf.inputs["Emission Color"])
        set_principled_input(mat, "Emission Strength", emission_strength)
    if use_alpha and "Alpha" in texture.outputs and "Alpha" in bsdf.inputs:
        links.new(texture.outputs["Alpha"], bsdf.inputs["Alpha"])
        mat.blend_method = "BLEND"
        mat.show_transparent_back = True

    return mat


def noisy_metal(name: str, base: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = material(name, base, metallic=0.22, roughness=0.84)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 28
    noise.inputs["Detail"].default_value = 8
    noise.inputs["Roughness"].default_value = 0.55
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.20
    ramp.color_ramp.elements[0].color = (base[0] * 0.46, base[1] * 0.46, base[2] * 0.46, 1)
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
    vertices: int = 12,
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


def add_uv_sphere(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    radius: float,
    mat: bpy.types.Material,
    scale: tuple[float, float, float] = (1.0, 1.0, 1.0),
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=radius, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_textured_xz_plane(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    width: float,
    height: float,
    mat: bpy.types.Material,
    uv_min: tuple[float, float] = (0.0, 0.0),
    uv_max: tuple[float, float] = (1.0, 1.0),
) -> bpy.types.Object:
    mesh = bpy.data.meshes.new(name + " mesh")
    half_width = width * 0.5
    half_height = height * 0.5
    verts = [
        (-half_width, 0.0, -half_height),
        (-half_width, 0.0, half_height),
        (half_width, 0.0, half_height),
        (half_width, 0.0, -half_height),
    ]
    faces = [(0, 1, 2, 3)]
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    uv_layer = mesh.uv_layers.new(name="HSK screen UV")
    u_min, v_min = uv_min
    u_max, v_max = uv_max
    for loop, uv in zip(mesh.polygons[0].loop_indices, ((u_max, v_min), (u_max, v_max), (u_min, v_max), (u_min, v_min))):
        uv_layer.data[loop].uv = uv

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    obj.data.materials.append(mat)
    obj.parent = parent
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


def add_plain_wall_y(root: bpy.types.Object, name: str, y: float, mats: dict[str, bpy.types.Material]) -> None:
    add_box(name, root, (0, y, ROOM_HEIGHT * 0.5), (ROOM_WIDTH + WALL_THICKNESS, WALL_THICKNESS, ROOM_HEIGHT), mats["wall"], bevel_width=0.014)


def add_plain_wall_x(root: bpy.types.Object, name: str, x: float, mats: dict[str, bpy.types.Material]) -> None:
    add_box(name, root, (x, ROOM_CENTER_Y, ROOM_HEIGHT * 0.5), (WALL_THICKNESS, ROOM_DEPTH + WALL_THICKNESS, ROOM_HEIGHT), mats["wall"], bevel_width=0.014)


def add_double_door_wall_x(
    root: bpy.types.Object,
    name: str,
    x: float,
    door_centers: list[tuple[str, float, bpy.types.Material]],
    mats: dict[str, bpy.types.Material],
) -> None:
    intervals = sorted((center - DOOR_WIDTH * 0.5, center + DOOR_WIDTH * 0.5, label, marker) for label, center, marker in door_centers)
    cursor = ROOM_SOUTH_Y
    z_mid = ROOM_HEIGHT * 0.5
    for index, (start, end, _label, _marker) in enumerate(intervals, start=1):
        if start > cursor:
            depth = start - cursor
            add_box(f"{name} sealed wall segment {index}", root, (x, cursor + depth * 0.5, z_mid), (WALL_THICKNESS, depth, ROOM_HEIGHT), mats["wall"])
        cursor = end
    if cursor < ROOM_NORTH_Y:
        depth = ROOM_NORTH_Y - cursor
        add_box(f"{name} sealed final wall segment", root, (x, cursor + depth * 0.5, z_mid), (WALL_THICKNESS, depth, ROOM_HEIGHT), mats["wall"])

    header_height = ROOM_HEIGHT - DOOR_HEIGHT
    for label, center, marker in door_centers:
        add_box(f"{name} {label} doorway upper header", root, (x, center, DOOR_HEIGHT + header_height * 0.5), (WALL_THICKNESS, DOOR_WIDTH, header_height), mats["wall"])
        add_box(f"{name} {label} doorway lower frame", root, (x, center - DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.12, 0.16, DOOR_HEIGHT), mats["door_frame"])
        add_box(f"{name} {label} doorway upper frame", root, (x, center + DOOR_WIDTH * 0.5, DOOR_HEIGHT * 0.5), (WALL_THICKNESS + 0.12, 0.16, DOOR_HEIGHT), mats["door_frame"])
        add_box(f"{name} {label} doorway color band", root, (x + 0.012, center, 1.42), (0.060, DOOR_WIDTH + 0.22, 0.42), marker, bevel_width=0.004)


def add_floor_grid(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    for x in (-2.6, -1.3, 0.0, 1.3, 2.6):
        add_box(f"SR-01 removable supply room floor plate {x:+.1f}", root, (x, ROOM_CENTER_Y, 0.112), (1.05, ROOM_DEPTH - 0.54, 0.040), mats["floor_panel"], bevel_width=0.006)
    for y in (-2.2, -1.1, 0.0, 1.1, 2.2):
        add_box(f"SR-01 transverse deck rib {y:+.1f}", root, (0, y, 0.145), (ROOM_WIDTH - 0.58, 0.040, 0.044), mats["deck_rib"], bevel_width=0.002)


def add_supply_storage_wall(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    wall_y = ROOM_NORTH_Y - WALL_THICKNESS * 0.74
    locker_y = wall_y - 0.45
    locker_front_y = locker_y - 0.34
    add_box("SR-02 supply storage wall placement marker only", root, (0.0, wall_y - 0.030, 1.46), (5.55, 0.045, 2.38), mats["storage_marker"], bevel_width=0.006)
    add_box("SR-02 visible gap behind freestanding locker", root, (0.0, wall_y - 0.235, 0.205), (5.50, 0.18, 0.045), mats["storage_cavity"], bevel_width=0.004)
    add_box("SR-02 freestanding locker base plinth", root, (0.0, locker_y, 0.245), (5.34, 0.70, 0.20), mats["locker_frame"], bevel_width=0.010)
    add_box("SR-02 freestanding supply locker main body", root, (0.0, locker_y, 1.32), (5.18, 0.62, 2.10), mats["locker_body"], bevel_width=0.014)
    add_box("SR-02 freestanding locker top cap", root, (0.0, locker_y, 2.42), (5.32, 0.72, 0.18), mats["locker_frame"], bevel_width=0.008)
    add_box("SR-02 freestanding locker left side panel", root, (-2.66, locker_y, 1.32), (0.16, 0.72, 2.12), mats["locker_frame"], bevel_width=0.006)
    add_box("SR-02 freestanding locker right side panel", root, (2.66, locker_y, 1.32), (0.16, 0.72, 2.12), mats["locker_frame"], bevel_width=0.006)

    front_y = locker_front_y - 0.072
    add_box("SR-03 reference style outer top frame", root, (0.0, front_y, 2.31), (4.92, 0.075, 0.12), mats["locker_frame"], bevel_width=0.004)
    add_box("SR-03 reference style outer bottom frame", root, (0.0, front_y, 0.41), (4.92, 0.075, 0.12), mats["locker_frame"], bevel_width=0.004)
    add_box("SR-03 reference style outer left frame", root, (-2.46, front_y, 1.36), (0.11, 0.075, 1.90), mats["locker_frame"], bevel_width=0.004)
    add_box("SR-03 reference style outer right frame", root, (2.46, front_y, 1.36), (0.11, 0.075, 1.90), mats["locker_frame"], bevel_width=0.004)

    for side, x in (("left", -1.18), ("right", 1.18)):
        add_box(f"SR-03 closed {side} flat metal locker door", root, (x, front_y - 0.030, 1.36), (2.30, 0.070, 1.82), mats["locker_door"], bevel_width=0.006)
        add_box(f"SR-03 {side} inset door border top", root, (x, front_y - 0.071, 2.20), (2.04, 0.024, 0.030), mats["locker_shadow"], bevel_width=0.001)
        add_box(f"SR-03 {side} inset door border bottom", root, (x, front_y - 0.071, 0.52), (2.04, 0.024, 0.030), mats["locker_shadow"], bevel_width=0.001)
        add_box(f"SR-03 {side} inset door border outer", root, (x + (-1 if side == "left" else 1) * 1.04, front_y - 0.071, 1.36), (0.026, 0.024, 1.66), mats["locker_shadow"], bevel_width=0.001)
        nameplate_x = x - 0.42 if side == "left" else x + 0.42
        add_box(f"SR-03 {side} upper horizontal name plate recess", root, (nameplate_x, front_y - 0.096, 2.02), (0.52, 0.032, 0.15), mats["locker_shadow"], bevel_width=0.004)
        add_box(f"SR-03 {side} upper horizontal name plate metal rim", root, (nameplate_x, front_y - 0.116, 2.02), (0.42, 0.020, 0.075), mats["locker_frame"], bevel_width=0.006)
        lock_x = -0.23 if side == "left" else 0.23
        add_box(f"SR-03 {side} black recessed vertical pull pocket", root, (lock_x, front_y - 0.112, 1.24), (0.22, 0.040, 0.48), mats["locker_shadow"], bevel_width=0.006)
        add_box(f"SR-03 {side} raised lock plate", root, (lock_x + (0.08 if side == "left" else -0.08), front_y - 0.137, 1.28), (0.105, 0.026, 0.34), mats["locker_frame"], bevel_width=0.004)

    add_box("SR-03 closed double door central seam", root, (0.0, front_y - 0.124, 1.36), (0.030, 0.028, 1.83), mats["locker_shadow"], bevel_width=0.001)

    for side, hinge_x in (("left", -2.58), ("right", 2.58)):
        add_box(f"SR-03 {side} exposed side hinge rail", root, (hinge_x, front_y - 0.020, 1.36), (0.055, 0.080, 1.68), mats["locker_frame"], bevel_width=0.004)
        for hinge_index, hinge_z in enumerate((0.65, 1.36, 2.07), start=1):
            add_box(f"SR-03 {side} exposed hinge barrel {hinge_index}", root, (hinge_x, front_y - 0.094, hinge_z), (0.105, 0.080, 0.22), mats["locker_frame"], bevel_width=0.006)

    def add_corner_perforations(name: str, origin_x: float, origin_z: float, x_dir: int, z_dir: int) -> None:
        for row in range(5):
            for column in range(row + 1):
                add_box(
                    f"SR-03 {name} triangular perforation {row + 1}-{column + 1}",
                    root,
                    (origin_x + x_dir * column * 0.075, front_y - 0.132, origin_z + z_dir * row * 0.055),
                    (0.028, 0.018, 0.028),
                    mats["locker_shadow"],
                    bevel_width=0.010,
                )

    add_corner_perforations("upper left corner", -2.10, 2.12, 1, -1)
    add_corner_perforations("upper right corner", 2.10, 2.12, -1, -1)
    add_corner_perforations("lower left corner", -2.10, 0.60, 1, 1)
    add_corner_perforations("lower right corner", 2.10, 0.60, -1, 1)


def add_ejection_wall_marker(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    wall_y = ROOM_SOUTH_Y + WALL_THICKNESS * 0.74
    add_box("SR-05 ejection bay wall position backplate", root, (0.0, wall_y + 0.04, 1.42), (4.55, 0.16, 2.20), mats["ejection_back"], bevel_width=0.014)
    add_box("SR-05 ejection bay closed frame top", root, (0.0, wall_y + 0.17, 2.23), (3.52, 0.20, 0.28), mats["door_frame"], bevel_width=0.010)
    add_box("SR-05 ejection bay closed frame bottom", root, (0.0, wall_y + 0.17, 0.62), (3.52, 0.20, 0.28), mats["door_frame"], bevel_width=0.010)
    add_box("SR-05 ejection bay left frame", root, (-1.88, wall_y + 0.17, 1.42), (0.26, 0.20, 1.82), mats["door_frame"], bevel_width=0.010)
    add_box("SR-05 ejection bay right frame", root, (1.88, wall_y + 0.17, 1.42), (0.26, 0.20, 1.82), mats["door_frame"], bevel_width=0.010)
    add_box("SR-06 ejection bay upper closed door", root, (0.0, wall_y + 0.25, 1.78), (3.26, 0.12, 0.70), mats["ejection_door"], bevel_width=0.008)
    add_box("SR-06 ejection bay lower closed door", root, (0.0, wall_y + 0.25, 1.06), (3.26, 0.12, 0.70), mats["ejection_door"], bevel_width=0.008)
    add_box("SR-06 ejection bay center seam", root, (0.0, wall_y + 0.325, 1.42), (3.18, 0.035, 0.035), mats["hazard"], bevel_width=0.002)

    terminal_x = 2.48
    terminal_y = wall_y + 0.58
    add_box("SR-07 visible ejection terminal floor pedestal", root, (terminal_x, terminal_y - 0.10, 0.68), (0.22, 0.28, 1.12), mats["terminal_frame"], bevel_width=0.010)
    add_box("SR-07 visible ejection terminal angled support arm", root, (terminal_x, terminal_y - 0.02, 1.20), (0.18, 0.34, 0.24), mats["terminal_frame"], (math.radians(-8), 0, 0), 0.008)
    add_box("SR-07 visible ejection terminal screen housing", root, (terminal_x, terminal_y, 1.52), (0.78, 0.22, 0.76), mats["terminal_frame"], bevel_width=0.012)
    add_box("SR-07 visible ejection terminal inactive screen", root, (terminal_x, terminal_y + 0.124, 1.62), (0.58, 0.032, 0.40), mats["terminal_screen"], bevel_width=0.004)
    add_textured_xz_plane(
        "SR-07 ejection terminal HSK open close screen texture",
        root,
        (terminal_x, terminal_y + 0.146, 1.62),
        0.59,
        0.405,
        mats["terminal_hsk_screen"],
        uv_min=(0.0, 0.075),
        uv_max=(0.86, 0.925),
    )
    add_box("SR-07 visible ejection terminal warning button", root, (terminal_x, terminal_y + 0.130, 1.14), (0.32, 0.038, 0.16), mats["hazard"], bevel_width=0.004)
    add_box("SR-07 visible ejection terminal status light", root, (terminal_x + 0.28, terminal_y + 0.132, 1.94), (0.14, 0.038, 0.10), mats["terminal_screen"], bevel_width=0.004)
    add_text_label(
        "SR-07 ejection terminal label",
        root,
        "TERMINAL",
        (terminal_x, terminal_y + 0.154, 1.02),
        (math.radians(90), 0, math.radians(180)),
        mats["label_text"],
        0.085,
    )
    add_text_label(
        "SR-05 ejection bay wall label",
        root,
        "EJECTION BAY",
        (0.0, wall_y + 0.42, 2.62),
        (math.radians(90), 0, math.radians(180)),
        mats["label_text"],
        0.18,
    )


def add_ejection_hazard_floor_zone(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    zone_center_y = ROOM_SOUTH_Y + 1.40
    zone_width = 3.92
    zone_depth = 1.82
    zone_z = 0.238
    left_x = -zone_width * 0.5
    right_x = zone_width * 0.5
    near_y = zone_center_y - zone_depth * 0.5
    far_y = zone_center_y + zone_depth * 0.5

    add_box(
        "SR-08 ejection hazard steel floor plate main",
        root,
        (0.0, zone_center_y, zone_z),
        (zone_width, zone_depth, 0.055),
        mats["ejection_zone_plate"],
        bevel_width=0.006,
    )
    for x in (-1.30, 0.0, 1.30):
        add_box(
            f"SR-08 ejection hazard plate vertical seam {x:+.1f}",
            root,
            (x, zone_center_y, zone_z + 0.038),
            (0.040, zone_depth - 0.18, 0.018),
            mats["ejection_zone_seam"],
            bevel_width=0.002,
        )
    for y in (zone_center_y - 0.46, zone_center_y + 0.46):
        add_box(
            f"SR-08 ejection hazard plate cross seam {y:+.2f}",
            root,
            (0.0, y, zone_z + 0.040),
            (zone_width - 0.22, 0.040, 0.018),
            mats["ejection_zone_seam"],
            bevel_width=0.002,
        )

    add_box("SR-08 ejection hazard floor amber ejection-side trim", root, (0.0, near_y, zone_z + 0.056), (zone_width, 0.075, 0.026), mats["hazard"], bevel_width=0.003)
    add_box("SR-08 ejection hazard floor amber safe-side trim", root, (0.0, far_y, zone_z + 0.056), (zone_width, 0.075, 0.026), mats["hazard"], bevel_width=0.003)
    add_box("SR-08 ejection hazard floor amber left trim", root, (left_x, zone_center_y, zone_z + 0.056), (0.075, zone_depth, 0.026), mats["hazard"], bevel_width=0.003)
    add_box("SR-08 ejection hazard floor amber right trim", root, (right_x, zone_center_y, zone_z + 0.056), (0.075, zone_depth, 0.026), mats["hazard"], bevel_width=0.003)

    for index, x in enumerate((-1.55, -0.95, -0.35, 0.25, 0.85, 1.45), start=1):
        add_box(
            f"SR-08 ejection hazard black diagonal caution slash {index}",
            root,
            (x, far_y, zone_z + 0.078),
            (0.075, 0.56, 0.020),
            mats["hazard_dark"],
            rot=(0, 0, math.radians(34)),
            bevel_width=0.001,
        )

    for index, x in enumerate((-0.42, 0.42), start=1):
        add_box(
            f"SR-08 ejection pull direction chevron {index} left stroke",
            root,
            (x - 0.18, zone_center_y - 0.18, zone_z + 0.070),
            (0.070, 0.74, 0.022),
            mats["hazard"],
            rot=(0, 0, math.radians(28)),
            bevel_width=0.002,
        )
        add_box(
            f"SR-08 ejection pull direction chevron {index} right stroke",
            root,
            (x + 0.18, zone_center_y - 0.18, zone_z + 0.070),
            (0.070, 0.74, 0.022),
            mats["hazard"],
            rot=(0, 0, math.radians(-28)),
            bevel_width=0.002,
        )

    bolt_index = 1
    for x in (-1.68, -0.56, 0.56, 1.68):
        for y in (near_y + 0.18, zone_center_y, far_y - 0.18):
            add_box(
                f"SR-08 ejection hazard recessed floor bolt {bolt_index:02d}",
                root,
                (x, y, zone_z + 0.076),
                (0.085, 0.085, 0.018),
                mats["ejection_zone_bolt"],
                bevel_width=0.012,
            )
            bolt_index += 1

    add_text_label(
        "SR-08 ejection hazard floor stamped label",
        root,
        "EJECTION ZONE",
        (0.0, zone_center_y + 0.44, zone_z + 0.090),
        (0, 0, 0),
        mats["label_text"],
        0.16,
    )


def add_empty_wall(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    for y in (-2.1, -1.05, 0.0, 1.05, 2.1):
        add_box(f"SR-01 empty west wall vertical service rib {y:+.2f}", root, (WEST_X + 0.12, y, 1.50), (0.10, 0.12, 2.40), mats["beam"], bevel_width=0.004)
    add_text_label(
        "SR-01 empty wall label",
        root,
        "EMPTY WALL",
        (WEST_X + 0.18, 0.0, 2.60),
        (math.radians(90), 0, math.radians(90)),
        mats["label_text"],
        0.15,
    )


def add_corridor_stub(
    root: bpy.types.Object,
    name: str,
    center_y: float,
    mat: bpy.types.Material,
    mats: dict[str, bpy.types.Material],
    label: str,
) -> None:
    center_x = EAST_X + 1.02
    add_box(f"{name} corridor floor continuation", root, (center_x, center_y, 0.0), (2.08, DOOR_WIDTH + 0.32, FLOOR_THICKNESS), mats["corridor_floor"], bevel_width=0.012)
    add_box(f"{name} corridor upper side wall", root, (center_x, center_y + DOOR_WIDTH * 0.5 + 0.16, 1.02), (2.08, 0.20, 2.04), mats["wall_dark"], bevel_width=0.010)
    add_box(f"{name} corridor lower side wall", root, (center_x, center_y - DOOR_WIDTH * 0.5 - 0.16, 1.02), (2.08, 0.20, 2.04), mats["wall_dark"], bevel_width=0.010)
    add_box(f"{name} colored threshold", root, (EAST_X + 0.03, center_y, 0.210), (0.62, DOOR_WIDTH + 0.42, 0.055), mat, bevel_width=0.006)
    add_text_label(
        f"{name} floor direction label",
        root,
        label,
        (EAST_X + 0.38, center_y, 0.255),
        (0, 0, math.radians(90)),
        mats["label_text"],
        0.15,
    )


def add_corridor_direction_labels(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    labels = [
        ("armory", ARMORY_DOOR_Y, "ARMORY", mats["armory_marker"]),
        ("cargo hold", CARGO_DOOR_Y, "CARGO HOLD", mats["cargo_marker"]),
    ]
    for key, center_y, label, marker_mat in labels:
        sign_name = "SR-11 " + key
        add_box(
            f"{sign_name} direction wall sign backplate",
            root,
            (EAST_X - 0.035, center_y, 2.18),
            (0.060, 1.05, 0.42),
            mats["direction_panel"],
            bevel_width=0.010,
        )
        add_box(
            f"{sign_name} direction wall sign color strip",
            root,
            (EAST_X - 0.072, center_y - 0.43, 2.18),
            (0.026, 0.12, 0.34),
            marker_mat,
            bevel_width=0.004,
        )
        add_text_label(
            f"{sign_name} direction wall text",
            root,
            label,
            (EAST_X - 0.080, center_y + 0.05, 2.18),
            (math.radians(90), 0, math.radians(90)),
            mats["direction_text"],
            0.16,
        )

        add_box(
            f"{sign_name} floor arrow shaft",
            root,
            (EAST_X - 0.78, center_y, 0.265),
            (0.82, 0.12, 0.040),
            marker_mat,
            bevel_width=0.004,
        )
        add_box(
            f"{sign_name} floor arrow head upper stroke",
            root,
            (EAST_X - 0.36, center_y + 0.13, 0.272),
            (0.34, 0.075, 0.040),
            marker_mat,
            rot=(0, 0, math.radians(34)),
            bevel_width=0.004,
        )
        add_box(
            f"{sign_name} floor arrow head lower stroke",
            root,
            (EAST_X - 0.36, center_y - 0.13, 0.272),
            (0.34, 0.075, 0.040),
            marker_mat,
            rot=(0, 0, math.radians(-34)),
            bevel_width=0.004,
        )
        add_text_label(
            f"{sign_name} floor direction text",
            root,
            label,
            (EAST_X - 1.18, center_y, 0.292),
            (0, 0, math.radians(90)),
            mats["direction_text"],
            0.135,
        )


def add_cctv_corner(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    corner_x = WEST_X + 0.34
    corner_y = ROOM_NORTH_Y - 0.34
    wall_joint = (WEST_X + 0.30, ROOM_NORTH_Y - 0.30, 2.68)
    elbow = (-3.02, 2.30, 2.56)
    arm_end = (-2.72, 2.08, 2.42)
    yaw = math.radians(-37)
    body_center = (-2.46, 1.88, 2.34)
    forward = Vector((math.cos(yaw), math.sin(yaw), -0.08)).normalized()

    add_box(
        "SR-12 CCTV northwest west wall mounting plate",
        root,
        (WEST_X + 0.052, corner_y, 2.48),
        (0.060, 0.52, 0.56),
        mats["cctv_mount"],
        bevel_width=0.006,
    )
    add_box(
        "SR-12 CCTV northwest north wall mounting plate",
        root,
        (corner_x, ROOM_NORTH_Y - 0.052, 2.50),
        (0.56, 0.060, 0.42),
        mats["cctv_mount"],
        bevel_width=0.006,
    )
    add_box(
        "SR-12 CCTV northwest overhead junction block",
        root,
        (corner_x, corner_y, 2.76),
        (0.34, 0.34, 0.16),
        mats["cctv_mount"],
        bevel_width=0.010,
    )
    add_cylinder_between(
        "SR-12 CCTV black cable along north wall",
        root,
        (WEST_X + 0.17, ROOM_NORTH_Y - 0.055, 2.77),
        (-2.55, ROOM_NORTH_Y - 0.055, 2.77),
        0.018,
        mats["cctv_cable"],
        vertices=8,
    )
    add_cylinder_between(
        "SR-12 CCTV black cable down west wall",
        root,
        (WEST_X + 0.055, corner_y + 0.20, 2.77),
        (WEST_X + 0.055, corner_y + 0.20, 2.38),
        0.016,
        mats["cctv_cable"],
        vertices=8,
    )
    add_uv_sphere("SR-12 CCTV wall swivel ball joint", root, wall_joint, 0.095, mats["cctv_mount"])
    add_cylinder_between(
        "SR-12 CCTV short articulated arm upper segment",
        root,
        wall_joint,
        elbow,
        0.045,
        mats["cctv_mount"],
        vertices=14,
    )
    add_uv_sphere("SR-12 CCTV elbow hinge joint", root, elbow, 0.075, mats["cctv_mount"])
    add_cylinder_between(
        "SR-12 CCTV short articulated arm lower segment",
        root,
        elbow,
        arm_end,
        0.040,
        mats["cctv_mount"],
        vertices=14,
    )
    add_box(
        "SR-12 CCTV angled compact camera body",
        root,
        body_center,
        (0.48, 0.25, 0.22),
        mats["cctv_body"],
        rot=(0, 0, yaw),
        bevel_width=0.018,
    )
    add_box(
        "SR-12 CCTV protective top hood",
        root,
        (body_center[0] + 0.02, body_center[1] - 0.02, body_center[2] + 0.13),
        (0.56, 0.33, 0.060),
        mats["cctv_mount"],
        rot=(0, 0, yaw),
        bevel_width=0.010,
    )
    add_cylinder_between(
        "SR-12 CCTV rear clamp to camera body",
        root,
        arm_end,
        (body_center[0] - 0.22 * math.cos(yaw), body_center[1] - 0.22 * math.sin(yaw), body_center[2] + 0.01),
        0.055,
        mats["cctv_mount"],
        vertices=14,
    )

    lens_start = Vector(body_center) + forward * 0.23
    lens_end = lens_start + forward * 0.18
    add_cylinder_between(
        "SR-12 CCTV dark recessed lens barrel",
        root,
        tuple(lens_start),
        tuple(lens_end),
        0.105,
        mats["cctv_lens"],
        vertices=28,
    )
    glass_start = lens_end
    glass_end = lens_end + forward * 0.025
    add_cylinder_between(
        "SR-12 CCTV faint glass lens face",
        root,
        tuple(glass_start),
        tuple(glass_end),
        0.082,
        mats["cctv_glass"],
        vertices=28,
    )

    ray_origin = tuple(glass_end + forward * 0.03)
    add_cylinder_between("SR-12 CCTV viewing direction center ray", root, ray_origin, (-0.45, 0.18, 0.42), 0.014, mats["cctv_view"], vertices=8)
    add_cylinder_between("SR-12 CCTV viewing direction left edge ray", root, ray_origin, (-1.38, 0.92, 0.40), 0.010, mats["cctv_view"], vertices=8)
    add_cylinder_between("SR-12 CCTV viewing direction right edge ray", root, ray_origin, (0.22, -0.76, 0.38), 0.010, mats["cctv_view"], vertices=8)
def add_outline_markers(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_box("SR-01 north supply storage wall floor marker", root, (0.0, ROOM_NORTH_Y - 0.46, 0.205), (5.70, 0.08, 0.045), mats["storage_marker"], bevel_width=0.004)
    add_box("SR-01 south ejection wall floor marker", root, (0.0, ROOM_SOUTH_Y + 0.46, 0.205), (4.80, 0.08, 0.045), mats["ejection_marker"], bevel_width=0.004)
    add_box("SR-01 west empty wall floor marker", root, (WEST_X + 0.45, 0.0, 0.205), (0.08, 4.80, 0.045), mats["empty_marker"], bevel_width=0.004)


def build_supply_room_shell(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("SR-01 supply room shell Blender sample")

    add_box("SR-01 sealed supply room deck floor", root, (0, ROOM_CENTER_Y, 0), (ROOM_WIDTH, ROOM_DEPTH, FLOOR_THICKNESS), mats["floor"], bevel_width=0.018)
    add_plain_wall_y(root, "SR-02 north supply storage wall shell", ROOM_NORTH_Y, mats)
    add_plain_wall_y(root, "SR-05 south ejection bay wall shell", ROOM_SOUTH_Y, mats)
    add_plain_wall_x(root, "SR-01 west empty wall shell", WEST_X, mats)
    add_double_door_wall_x(
        root,
        "SR-09 SR-10 east shared corridor wall",
        EAST_X,
        [("armory", ARMORY_DOOR_Y, mats["armory_marker"]), ("cargo hold", CARGO_DOOR_Y, mats["cargo_marker"])],
        mats,
    )

    add_floor_grid(root, mats)
    add_supply_storage_wall(root, mats)
    add_ejection_wall_marker(root, mats)
    add_ejection_hazard_floor_zone(root, mats)
    add_empty_wall(root, mats)
    add_corridor_stub(root, "SR-09 armory direction", ARMORY_DOOR_Y, mats["armory_marker"], mats, "ARMORY")
    add_corridor_stub(root, "SR-10 cargo hold direction", CARGO_DOOR_Y, mats["cargo_marker"], mats, "CARGO HOLD")
    add_corridor_direction_labels(root, mats)
    add_cctv_corner(root, mats)
    add_outline_markers(root, mats)
    add_text_label(
        "SR-01 room shell title floor label",
        root,
        "SUPPLY ROOM SHELL",
        (0.0, 0.0, 0.245),
        (0, 0, 0),
        mats["label_text"],
        0.20,
    )


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 48
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("SupplyRoomShellWorld")
    scene.world.color = (0.010, 0.011, 0.013)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, 0, 5.6))
    top = bpy.context.object
    top.name = "large overhead supply room inspection softbox"
    top.data.energy = 690
    top.data.size = 6.8

    bpy.ops.object.light_add(type="AREA", location=(-4.9, 1.6, 2.7))
    storage = bpy.context.object
    storage.name = "cool storage wall fill"
    storage.data.energy = 165
    storage.data.size = 3.0
    storage.data.color = (0.55, 0.82, 1.0)

    bpy.ops.object.light_add(type="AREA", location=(4.8, -2.3, 2.6))
    ejection = bpy.context.object
    ejection.name = "warm ejection wall fill"
    ejection.data.energy = 165
    ejection.data.size = 3.2
    ejection.data.color = (1.0, 0.60, 0.34)

    bpy.ops.object.light_add(type="POINT", location=(2.2, -2.25, 1.55))
    terminal = bpy.context.object
    terminal.name = "faint ejection terminal glow"
    terminal.data.energy = 70
    terminal.data.color = (0.96, 0.22, 0.08)


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
    camera.name = "supply room shell camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "supply_room_shell.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "supply_room_shell.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "supply_room_shell.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "SR-07",
        "title": "사출대 단말기 HSK Open/Close 화면",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt - 비품실은 벽에 비품창고가 있고 맞은편 벽은 사출대가 있다.",
            "docs/GAME_DESIGN_SOURCE.txt - 위에서 내려다본 비품실은 비품창고 벽, 아무것도 없는 벽, 사출대가 있는 벽이 ㄷ자 형태다.",
            "docs/GAME_DESIGN_SOURCE.txt - 나머지 한 면에 무기실과 이어지는 복도와 운송창고로 이어지는 복도가 존재한다.",
            "docs/MVP_IMPLEMENTATION_ORDER.md - 4단계 Graybox: 비품실은 비품창고 벽, 사출대 자리, 무기실/운송창고 연결 방향을 둔다.",
            "docs/SUPPLY_ROOM_OBJECTS.md - SR-01 비품실 룸 셸.",
            "사용자 정정 - 비품창고는 벽에 붙은 패널이 아니라 벽 앞에 따로 배치된 사물함형 오브젝트다.",
            "사용자 정정 - 사출대 단말기는 샘플 렌더에서 분명히 보여야 한다.",
            "사용자 정정 - 비품창고 전면은 3개 슬롯이 아니라 좌우 2개 문으로 닫힌 사물함 형태다.",
            "사용자 참고 이미지 - 연녹색 금속 캐비닛, 중앙 이음선, 상단 명패 손잡이, 중앙 매립 손잡이, 바깥 힌지, 모서리 타공 패턴.",
            "사용자 정정 - SR-04 분류 표시부는 UI로 처리하므로 모델링 작업에서 제외한다.",
            "사용자 지시 - SR-08 사출 위험 구역은 사출대 앞 바닥에 철판을 까는 형태로 표시한다.",
            "docs/SUPPLY_ROOM_OBJECTS.md - SR-11 복도 방향 표시 레이블.",
            "사용자 지시 - SR-11 샘플 제작.",
            "docs/SUPPLY_ROOM_OBJECTS.md - SR-12 비품실 CCTV 앵커.",
            "사용자 지시 - 비품실을 위에서 내려다볼 때 왼쪽 상단 모서리에 CCTV 오브젝트를 배치한다.",
            "Assets/Heavy Station Kit/_common/Textures/GUI/HSK_Open_Close.png - 사출대 단말기 화면에 사용할 Open/Close GUI 텍스처.",
            "사용자 지시 - 사출대 단말기 화면에 HSK_Open_Close.png를 넣은 샘플 제작.",
        ],
        "generatedFiles": [
            "blender/supply_room_shell.blend",
            "exports/supply_room_shell.fbx",
            "exports/supply_room_shell.glb",
            "renders/01_overview.png",
            "renders/02_floor_plan.png",
            "renders/03_supply_storage_wall.png",
            "renders/04_ejection_wall.png",
            "renders/05_corridor_entries.png",
            "renders/06_room_shell_markers.png",
            "renders/07_ejection_hazard_floor.png",
            "renders/08_corridor_direction_labels.png",
            "renders/09_cctv_corner.png",
            "renders/10_ejection_terminal_hsk_screen.png",
            "index.html",
            "README.md",
            "ASSET_MANIFEST.json",
            "APPROVAL_STATUS.json",
        ],
        "includedParts": [
            "천장을 제외한 비품실 바닥과 ㄷ자형 벽 셸",
            "비품창고 벽 앞에 따로 배치된 사물함형 독립 오브젝트",
            "참고 이미지형 닫힌 좌우 2문 금속 사물함",
            "상단 소형 명패 손잡이, 중앙 매립 손잡이와 잠금판, 바깥쪽 힌지",
            "사물함 문 모서리의 삼각 타공 패턴",
            "맞은편 사출대 벽 위치와 닫힌 상하 도어 자리 표시",
            "렌더에서 보이도록 전면으로 당긴 사출대 오른쪽 단말기",
            "SR-07 사출대 단말기 화면의 HSK_Open_Close.png 텍스처 평면",
            "SR-08 사출대 앞 철판형 위험 구역 바닥 패널",
            "SR-08 패널 이음선, 볼트, 경고 트림, 사출 방향 체브론",
            "남은 한 면의 무기실 방향 출입구",
            "남은 한 면의 운송창고 방향 출입구",
            "SR-11 벽면 방향 표지판 백플레이트와 색상 스트립",
            "SR-11 무기실/운송창고 방향 바닥 화살표",
            "SR-11 ARMORY, CARGO HOLD 독립 텍스트 레이블",
            "SR-12 북서쪽 코너 CCTV 벽/천장 브래킷",
            "SR-12 짧은 관절 암과 힌지 조인트",
            "SR-12 소형 CCTV 본체, 보호 후드, 렌즈",
            "SR-12 감시 방향 표시 레이",
            "ARMORY, CARGO HOLD, SUPPLY STORAGE, EJECTION BAY 방향/구역 라벨",
        ],
        "excludedParts": [
            "Unity 씬 배치",
            "SR-04 분류 탭 물리 모델링",
            "SR-11 길찾기 로직 또는 UI",
            "SR-09/SR-10 출입구 구조 변경",
            "사출 판정 로직",
            "사출대 개폐 애니메이션",
            "보관 데이터 저장 로직",
            "상점 구매/판매 UI",
            "개별 무기와 장비 모델",
            "CCTV 화면 전환 로직",
            "CCTV 구매/해금 상태 로직",
            "실제 렌더 카메라와 RenderTexture",
            "SR-07 사출대 개폐 상호작용 로직",
            "Validate/Test/Build",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "SR-07",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산, UI 흐름에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# supply_room_shell

SR-07 사출대 단말기 HSK Open/Close 화면 승인용 Blender 샘플입니다.

## 목적

비품실을 Unity에 연결하기 전에, 사출대 오른쪽 단말기 화면에 `HSK_Open_Close.png` 텍스처가 들어간 상태를 검사하기 위한 샘플입니다.

## 반영 기준

- 비품창고 벽, 빈 벽, 사출대 벽이 ㄷ자 형태가 되도록 구성했습니다.
- 남은 한 면에는 무기실 방향 출입구와 운송창고 방향 출입구를 함께 배치했습니다.
- 비품창고는 벽면 패널이 아니라 벽 앞에 따로 배치된 사물함형 독립 오브젝트로 만들었습니다.
- 사물함 전면은 3개 슬롯이 아니라 참고 이미지처럼 닫힌 좌우 2문 금속 캐비닛 형태로 만들었습니다.
- 상단 소형 명패 손잡이, 중앙 매립 손잡이와 잠금판, 바깥쪽 힌지, 모서리 타공 패턴을 포함했습니다.
- 기본 3칸 보관 기준은 외형 슬롯으로 드러내지 않고 추후 UI/데이터 연결에서만 다룹니다.
- SR-04 분류 표시부는 UI로 처리하므로 물리 모델링 샘플에서는 제외했습니다.
- 사출대 벽에는 닫힌 상하 도어와 오른쪽 단말기를 넣었고, 단말기는 렌더에서 보이도록 더 앞으로 당기고 크기를 키웠습니다.
- SR-07 사출대 단말기 화면에는 `Assets/Heavy Station Kit/_common/Textures/GUI/HSK_Open_Close.png`를 텍스처 평면으로 얹었습니다.
- SR-08 사출 위험 구역은 경고선만 그린 것이 아니라 사출대 앞 바닥에 별도 철판 패널을 깔아 보이게 했습니다.
- SR-08 철판에는 패널 이음선, 가장자리 경고 트림, 볼트, 사출 방향 체브론을 넣었습니다.
- SR-11은 기존 SR-09/SR-10 출입구를 변경하지 않고, 벽면 방향 표지판과 바닥 화살표만 독립 오브젝트로 추가했습니다.
- SR-11 표기는 `ARMORY`, `CARGO HOLD` 영어 메인 레이블과 출입구 색상에 맞춘 파랑/초록 방향 표시로 구성했습니다.
- SR-12는 비품실 상단 평면 기준 왼쪽 상단, 즉 북서쪽 벽/천장 코너에 고정된 CCTV 오브젝트로 구성했습니다.
- SR-12에는 코너 브래킷, 짧은 관절 암, 소형 본체, 보호 후드, 검은 렌즈, 감시 방향 표시 레이를 포함했습니다.
- 내부 구조 확인이 쉽도록 천장은 제외했습니다.
- 실제 사출 판정, 개폐 애니메이션, 보관 UI, 보관 데이터 저장 로직, CCTV 화면 전환/구매/해금 로직, 개별 무기/장비 모델은 포함하지 않습니다.

## 포함

- `blender/supply_room_shell.blend`
- `exports/supply_room_shell.fbx`
- `exports/supply_room_shell.glb`
- `renders/*.png` 10개 구도
- `index.html`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- Unity 씬 배치
- SR-11 길찾기 로직 또는 UI
- SR-09/SR-10 출입구 구조 변경
- 사출 판정 로직
- 사출대 개폐 애니메이션
- SR-07 사출대 개폐 상호작용 로직
- 보관 데이터 저장 로직
- 상점 구매/판매 UI
- 개별 무기와 장비 모델
- CCTV 화면 전환 로직
- CCTV 구매/해금 상태 로직
- 실제 렌더 카메라와 RenderTexture
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_overview.png", "01 비품실 룸 셸 전체"),
        ("02_floor_plan.png", "02 ㄷ자형 벽 구조와 두 출입구"),
        ("03_supply_storage_wall.png", "03 참고 이미지형 닫힌 2문 금속 사물함"),
        ("04_ejection_wall.png", "04 사출대 벽과 보이는 오른쪽 단말기"),
        ("05_corridor_entries.png", "05 무기실과 운송창고 방향 출입구"),
        ("06_room_shell_markers.png", "06 룸 셸 벽 역할 마커"),
        ("07_ejection_hazard_floor.png", "07 SR-08 사출대 앞 철판형 위험 구역"),
        ("08_corridor_direction_labels.png", "08 SR-11 복도 방향 표시 레이블"),
        ("09_cctv_corner.png", "09 SR-12 좌상단 코너 CCTV 오브젝트"),
        ("10_ejection_terminal_hsk_screen.png", "10 SR-07 HSK Open/Close 단말기 화면"),
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
  <title>supply_room_shell review</title>
  <style>
    body {{ margin: 0; background: #111314; color: #e8e0d0; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #c9bfad; line-height: 1.55; }}
    h2 {{ margin: 0 0 10px; font-size: 20px; color: #f0e6d2; }}
    h3 {{ margin: 0 0 8px; font-size: 15px; color: #f0e6d2; }}
    ul {{ margin: 0; padding-left: 18px; color: #c9bfad; line-height: 1.55; }}
    li {{ margin: 2px 0; }}
    .summary {{ margin: 18px 0 20px; border: 1px solid #4b514c; background: #1a1f1f; padding: 16px; }}
    .summary p {{ margin: 0 0 14px; }}
    .summary-grid {{ display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 14px; }}
    .summary-card {{ border: 1px solid #343b38; background: #151919; padding: 12px; }}
    .badge {{ display: inline-block; margin-bottom: 10px; padding: 4px 8px; border: 1px solid #a66728; color: #ffcf91; background: #291b12; font-size: 13px; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #3f4542; background: #1c2121; padding: 10px; }}
    img {{ width: 100%; display: block; background: #0b0e0e; }}
    figcaption {{ margin-top: 8px; color: #ded3bd; font-size: 14px; }}
    @media (max-width: 900px) {{ .summary-grid, .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>supply_room_shell</h1>
  <p>SR-07 사출대 단말기 HSK Open/Close 화면 승인용 샘플입니다. 기존 비품실 구조, 사물함, 사출대, SR-08 바닥 철판, SR-11 복도 문구, SR-12 CCTV 샘플 구조는 유지하고, 사출대 오른쪽 단말기 화면 앞면에 `HSK_Open_Close.png` 텍스처만 새 샘플 대상으로 추가했습니다. Unity 씬 배치와 사출대 개폐/쿨타임/상호작용 로직은 포함하지 않았습니다.</p>
  <section class="summary">
    <span class="badge">승인 대기 샘플</span>
    <h2>SR-07 HSK Open/Close 단말기 화면 요약</h2>
    <p>이번 변경은 사출대 오른쪽 단말기 화면에 Heavy Station Kit의 `HSK_Open_Close.png` GUI 이미지를 실제 화면 텍스처처럼 보이도록 얹은 샘플입니다. 기존 단말기 외형은 유지하고 화면 앞면의 표시만 교체했습니다.</p>
    <div class="summary-grid">
      <div class="summary-card">
        <h3>포함</h3>
        <ul>
          <li>SR-07 사출대 단말기 화면 텍스처 평면</li>
          <li>`HSK_Open_Close.png` 이미지 기반 Open/Close 표시</li>
          <li>기존 단말기 프레임과 버튼 구조 유지</li>
          <li>10번 근접 렌더 추가</li>
        </ul>
      </div>
      <div class="summary-card">
        <h3>제외</h3>
        <ul>
          <li>Unity 씬, 프리팹, 런타임 자산 반영</li>
          <li>사출대 개폐 상호작용 로직</li>
          <li>사출 판정과 쿨타임 로직</li>
          <li>비품실 기존 오브젝트 수정</li>
        </ul>
      </div>
      <div class="summary-card">
        <h3>승인 후 적용 범위</h3>
        <ul>
          <li>`Approved Supply Room 01 Shell` 하위 SR-07 화면만 수정</li>
          <li>사출대 단말기 위치와 각도는 유지</li>
          <li>비품실 다른 오브젝트는 수정 제외</li>
          <li>시각 기준은 10번 샘플 렌더와의 일치도</li>
        </ul>
      </div>
    </div>
  </section>
  <section class="summary">
    <span class="badge">승인 대기 샘플</span>
    <h2>SR-12 비품실 CCTV 앵커 요약</h2>
    <p>이번 변경은 비품실을 위에서 내려다볼 때 왼쪽 상단 모서리에 고정될 CCTV의 위치와 실루엣을 검사하기 위한 샘플입니다. 벽과 천장 코너에 붙은 브래킷에서 짧은 관절 암이 내려오고, 카메라 본체는 비품실 내부를 향하도록 기울였습니다.</p>
    <div class="summary-grid">
      <div class="summary-card">
        <h3>포함</h3>
        <ul>
          <li>SR-12 북서쪽 코너 벽/천장 브래킷</li>
          <li>짧은 관절 암과 힌지 조인트</li>
          <li>소형 CCTV 본체, 보호 후드, 검은 렌즈</li>
          <li>검토용 감시 방향 표시 레이</li>
        </ul>
      </div>
      <div class="summary-card">
        <h3>제외</h3>
        <ul>
          <li>Unity 씬, 프리팹, 런타임 자산 반영</li>
          <li>통제실 CCTV 화면 전환 로직</li>
          <li>CCTV 구매/해금 상태 로직</li>
          <li>실제 렌더 카메라와 RenderTexture</li>
        </ul>
      </div>
      <div class="summary-card">
        <h3>승인 후 적용 범위</h3>
        <ul>
          <li>`Approved Supply Room 01 Shell` 하위 SR-12만 추가</li>
          <li>기존 비품실 오브젝트는 위치와 구조 유지</li>
          <li>비품실 밖 모든 씬 오브젝트는 수정 제외</li>
          <li>시각 기준은 09번 샘플 렌더와의 일치도</li>
        </ul>
      </div>
    </div>
  </section>
  <section class="summary">
    <span class="badge">승인 대기 샘플</span>
    <h2>SR-11 복도 방향 표시 레이블 요약</h2>
    <p>이번 변경은 비품실에서 무기실과 운송창고 방향을 즉시 확인할 수 있도록, 각 복도 입구 주변에 벽면 표지판과 바닥 화살표를 독립 오브젝트로 추가한 샘플입니다.</p>
    <div class="summary-grid">
      <div class="summary-card">
        <h3>포함</h3>
        <ul>
          <li>SR-11 벽면 방향 표지판 백플레이트</li>
          <li>`ARMORY`, `CARGO HOLD` 영어 메인 표기</li>
          <li>출입구 색상과 맞춘 파랑/초록 색상 스트립</li>
          <li>각 복도 입구 앞 바닥 방향 화살표</li>
        </ul>
      </div>
      <div class="summary-card">
        <h3>제외</h3>
        <ul>
          <li>Unity 씬, 프리팹, 런타임 자산 반영</li>
          <li>SR-09/SR-10 출입구 구조 변경</li>
          <li>길찾기 로직 또는 UI</li>
          <li>비품실 밖 오브젝트 수정</li>
        </ul>
      </div>
      <div class="summary-card">
        <h3>승인 후 적용 범위</h3>
        <ul>
          <li>`Approved Supply Room 01 Shell` 하위 SR-11만 추가</li>
          <li>기존 SR-09/SR-10 출입구는 위치와 구조 유지</li>
          <li>비품실의 다른 오브젝트는 수정 제외</li>
          <li>시각 기준은 08번 샘플 렌더와의 일치도</li>
        </ul>
      </div>
    </div>
  </section>
  <section class="summary">
    <span class="badge">승인 대기 샘플</span>
    <h2>SR-08 사출 위험 구역 요약</h2>
    <p>이번 변경은 사출대 앞에 서 있으면 사출될 수 있는 영역을 바닥에서 바로 읽을 수 있도록, 기존 바닥 위에 별도 철판 구역을 까는 샘플입니다. SR-04 분류 표시부는 사용자 확인에 따라 물리 모델링이 아니라 UI 작업으로 분리했습니다.</p>
    <div class="summary-grid">
      <div class="summary-card">
        <h3>포함</h3>
        <ul>
          <li>사출대 앞 SR-08 철판형 바닥 패널</li>
          <li>패널 이음선과 매립 볼트</li>
          <li>가장자리 경고 트림과 검은 사선 무늬</li>
          <li>사출대 방향을 가리키는 체브론</li>
        </ul>
      </div>
      <div class="summary-card">
        <h3>제외</h3>
        <ul>
          <li>Unity 씬, 프리팹, 런타임 자산 반영</li>
          <li>사출 판정, 개폐, 쿨타임 로직</li>
          <li>SR-04 분류 표시부 물리 모델링</li>
          <li>보관 UI와 데이터 저장 로직</li>
        </ul>
      </div>
      <div class="summary-card">
        <h3>승인 후 적용 범위</h3>
        <ul>
          <li>`Approved Supply Room 01 Shell` 하위 SR-08만 추가</li>
          <li>기존 비품실 비대상 오브젝트는 수정 제외</li>
          <li>비품실 밖 모든 씬 오브젝트는 수정 제외</li>
          <li>시각 기준은 이 샘플 렌더와의 일치도</li>
        </ul>
      </div>
    </div>
  </section>
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
        "floor": noisy_metal("SR-01 worn supply room deck", (0.15, 0.17, 0.17, 1)),
        "floor_panel": noisy_metal("SR-01 removable dark supply floor panel", (0.18, 0.19, 0.18, 1)),
        "deck_rib": noisy_metal("SR-01 raised supply deck rib", (0.08, 0.09, 0.09, 1)),
        "wall": noisy_metal("SR-01 thick supply room armored wall", (0.20, 0.23, 0.23, 1)),
        "wall_dark": noisy_metal("SR-01 dark corridor wall", (0.10, 0.12, 0.13, 1)),
        "door_frame": noisy_metal("SR-01 heavy doorway and equipment frame", (0.34, 0.34, 0.30, 1)),
        "beam": noisy_metal("SR-01 empty wall structural rib", (0.28, 0.30, 0.28, 1)),
        "corridor_floor": noisy_metal("SR-01 corridor continuation steel", (0.15, 0.18, 0.18, 1)),
        "storage_back": noisy_metal("SR-02 supply storage wall blue backplate", (0.11, 0.16, 0.18, 1)),
        "storage_slot": noisy_metal("SR-03 supply storage slot frame", (0.24, 0.27, 0.26, 1)),
        "storage_cavity": material("SR-03 dark empty storage cavity", (0.012, 0.014, 0.014, 1), roughness=0.92),
        "locker_body": noisy_metal("SR-02 reference pale green locker body", (0.48, 0.51, 0.42, 1)),
        "locker_door": noisy_metal("SR-03 reference pale green locker flat door", (0.60, 0.63, 0.52, 1)),
        "locker_frame": noisy_metal("SR-03 reference muted metal locker frame", (0.39, 0.42, 0.34, 1)),
        "locker_shadow": material("SR-03 dark inset locker handle and perforation shadow", (0.030, 0.034, 0.032, 1), roughness=0.88),
        "screen": material("SR-04 inactive supply category strip", (0.028, 0.086, 0.096, 1), roughness=0.36, emission=(0.018, 0.16, 0.18, 1), emission_strength=0.20),
        "category_marker": material("SR-04 category tab amber marker", (0.63, 0.40, 0.12, 1), roughness=0.70, emission=(0.20, 0.10, 0.025, 1), emission_strength=0.12),
        "ejection_back": noisy_metal("SR-05 ejection bay wall dark backplate", (0.16, 0.13, 0.12, 1)),
        "ejection_door": noisy_metal("SR-06 closed ejection bay door", (0.22, 0.21, 0.19, 1)),
        "terminal_frame": noisy_metal("SR-07 ejection terminal frame", (0.18, 0.18, 0.17, 1)),
        "terminal_screen": material("SR-07 inactive ejection terminal screen", (0.09, 0.018, 0.014, 1), roughness=0.44, emission=(0.20, 0.020, 0.010, 1), emission_strength=0.18),
        "terminal_hsk_screen": image_material("SR-07 HSK open close terminal screen texture", HSK_OPEN_CLOSE_TEXTURE, (0.18, 0.24, 0.22, 1), emission_strength=0.22, use_alpha=False),
        "hazard": material("SR-05 muted ejection hazard amber", (0.86, 0.50, 0.12, 1), roughness=0.82),
        "hazard_dark": material("SR-08 black warning paint", (0.018, 0.018, 0.015, 1), roughness=0.86),
        "ejection_zone_plate": noisy_metal("SR-08 scuffed ejection hazard floor steel plate", (0.19, 0.19, 0.17, 1)),
        "ejection_zone_seam": noisy_metal("SR-08 dark recessed ejection plate seam", (0.045, 0.047, 0.044, 1)),
        "ejection_zone_bolt": noisy_metal("SR-08 recessed ejection floor bolt heads", (0.08, 0.08, 0.075, 1)),
        "label_text": material("SR-01 pale floor and wall label text", (0.78, 0.88, 0.84, 1), roughness=0.70, emission=(0.16, 0.32, 0.29, 1), emission_strength=0.08),
        "direction_panel": noisy_metal("SR-11 dark corridor direction sign backplate", (0.075, 0.085, 0.080, 1)),
        "direction_text": material("SR-11 bright corridor direction label text", (0.92, 0.88, 0.66, 1), roughness=0.64, emission=(0.32, 0.26, 0.10, 1), emission_strength=0.16),
        "cctv_mount": noisy_metal("SR-12 CCTV dark corner mounting bracket", (0.13, 0.14, 0.14, 1)),
        "cctv_body": noisy_metal("SR-12 CCTV off-white compact camera housing", (0.62, 0.64, 0.58, 1)),
        "cctv_lens": material("SR-12 CCTV black recessed lens barrel", (0.010, 0.012, 0.014, 1), roughness=0.38),
        "cctv_glass": material("SR-12 CCTV faint blue glass lens", (0.05, 0.16, 0.20, 1), roughness=0.25, emission=(0.02, 0.10, 0.12, 1), emission_strength=0.10),
        "cctv_cable": material("SR-12 CCTV black wall cable", (0.018, 0.018, 0.016, 1), roughness=0.82),
        "cctv_view": material("SR-12 CCTV translucent viewing direction ray", (0.08, 0.40, 0.42, 1), roughness=0.62, emission=(0.02, 0.22, 0.22, 1), emission_strength=0.10),
        "armory_marker": material("SR-09 armory direction blue marker", (0.13, 0.28, 0.58, 1), roughness=0.70, emission=(0.03, 0.09, 0.24, 1), emission_strength=0.18),
        "cargo_marker": material("SR-10 cargo hold direction green marker", (0.18, 0.42, 0.30, 1), roughness=0.72, emission=(0.04, 0.16, 0.10, 1), emission_strength=0.18),
        "storage_marker": material("SR-02 supply wall floor cyan marker", (0.10, 0.45, 0.50, 1), roughness=0.72, emission=(0.02, 0.16, 0.18, 1), emission_strength=0.14),
        "ejection_marker": material("SR-05 ejection wall floor orange marker", (0.66, 0.25, 0.08, 1), roughness=0.76, emission=(0.18, 0.045, 0.015, 1), emission_strength=0.15),
        "empty_marker": material("SR-01 empty wall floor gray marker", (0.32, 0.34, 0.32, 1), roughness=0.80),
    }

    build_supply_room_shell(mats)
    add_render_lights()

    cameras = [
        ("overview", (6.7, -6.7, 4.05), (0.0, 0.0, 1.25), 33, "01_overview.png", None),
        ("floor_plan", (0.0, ROOM_CENTER_Y, 11.0), (0.0, ROOM_CENTER_Y, 0.0), 50, "02_floor_plan.png", 10.8),
        ("supply_storage_wall", (0.0, -1.70, 2.22), (0.0, ROOM_NORTH_Y - 1.02, 1.50), 40, "03_supply_storage_wall.png", None),
        ("ejection_wall", (1.35, 1.70, 2.10), (1.16, ROOM_SOUTH_Y + 0.54, 1.46), 38, "04_ejection_wall.png", None),
        ("corridor_entries", (6.9, -0.15, 2.40), (EAST_X, -0.05, 1.20), 36, "05_corridor_entries.png", None),
        ("room_shell_markers", (4.8, -5.7, 5.1), (0.0, 0.0, 0.9), 37, "06_room_shell_markers.png", None),
        ("ejection_hazard_floor", (4.45, 1.55, 2.95), (0.0, ROOM_SOUTH_Y + 1.42, 0.30), 42, "07_ejection_hazard_floor.png", None),
        ("corridor_direction_labels", (5.85, -0.10, 2.35), (EAST_X - 0.22, -0.05, 1.28), 36, "08_corridor_direction_labels.png", None),
        ("cctv_corner", (-0.35, -0.15, 2.72), (-2.92, 2.16, 2.45), 38, "09_cctv_corner.png", None),
        ("ejection_terminal_hsk_screen", (3.05, -0.65, 2.05), (2.48, ROOM_SOUTH_Y + 0.96, 1.58), 65, "10_ejection_terminal_hsk_screen.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} Blender sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
