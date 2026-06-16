from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "engine_room_shell"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
COMPARISON_DIR = SAMPLE_ROOT / "unity_applied_comparison"

OUTER_RADIUS = 4.4
INNER_RADIUS = 1.48
FLOOR_THICKNESS = 0.16
WALL_HEIGHT = 2.55
WALL_THICKNESS = 0.34
DOOR_OPENING_HEIGHT = 1.92
DOOR_OPENING_HALF_DEGREES = 10.0


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR, COMPARISON_DIR):
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
        mat.show_transparent_back = True
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
    noise.inputs["Scale"].default_value = 32
    noise.inputs["Detail"].default_value = 8
    noise.inputs["Roughness"].default_value = 0.58
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.20
    ramp.color_ramp.elements[0].color = (base[0] * 0.45, base[1] * 0.45, base[2] * 0.45, 1)
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
    bevel_width: float = 0.015,
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
    ring_count: int = 16,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=ring_count, radius=radius, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def arc_points(radius: float, start_deg: float, end_deg: float, segments: int) -> list[tuple[float, float]]:
    points = []
    for i in range(segments + 1):
        t = i / segments
        angle = math.radians(start_deg + (end_deg - start_deg) * t)
        points.append((math.cos(angle) * radius, math.sin(angle) * radius))
    return points


def add_annular_sector(
    name: str,
    parent: bpy.types.Object,
    inner_radius: float,
    outer_radius: float,
    start_deg: float,
    end_deg: float,
    z: float,
    thickness: float,
    mat: bpy.types.Material,
    segments: int = 36,
) -> bpy.types.Object:
    outer = arc_points(outer_radius, start_deg, end_deg, segments)
    inner = arc_points(inner_radius, start_deg, end_deg, segments)
    verts: list[tuple[float, float, float]] = []
    for x, y in outer:
        verts.append((x, y, z + thickness * 0.5))
    for x, y in inner:
        verts.append((x, y, z + thickness * 0.5))
    for x, y in outer:
        verts.append((x, y, z - thickness * 0.5))
    for x, y in inner:
        verts.append((x, y, z - thickness * 0.5))

    n = segments + 1
    faces = []
    for i in range(segments):
        faces.append((i, i + 1, n + i + 1, n + i))
        faces.append((2 * n + i + 1, 2 * n + i, 3 * n + i, 3 * n + i + 1))
        faces.append((i + 1, 2 * n + i + 1, 3 * n + i + 1, n + i + 1))
        faces.append((2 * n + i, i, n + i, 3 * n + i))
    faces.append((0, n, 3 * n, 2 * n))
    faces.append((segments, 2 * n + segments, 3 * n + segments, n + segments))

    mesh = bpy.data.meshes.new(name + " mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.parent = parent
    bevel = obj.modifiers.new("sector edge bevel", "BEVEL")
    bevel.width = 0.018
    bevel.segments = 1
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_arc_wall(
    name: str,
    parent: bpy.types.Object,
    radius: float,
    start_deg: float,
    end_deg: float,
    z_base: float,
    height: float,
    thickness: float,
    mat: bpy.types.Material,
    segments: int = 32,
) -> bpy.types.Object:
    return add_annular_sector(
        name,
        parent,
        radius - thickness * 0.5,
        radius + thickness * 0.5,
        start_deg,
        end_deg,
        z_base + height * 0.5,
        height,
        mat,
        segments,
    )


def add_cylindrical_shell(
    name: str,
    parent: bpy.types.Object,
    inner_radius: float,
    outer_radius: float,
    start_deg: float,
    end_deg: float,
    z_base: float,
    height: float,
    mat: bpy.types.Material,
    segments: int = 64,
) -> bpy.types.Object:
    verts: list[tuple[float, float, float]] = []
    z_bottom = z_base
    z_top = z_base + height
    for radius, z in (
        (outer_radius, z_bottom),
        (outer_radius, z_top),
        (inner_radius, z_bottom),
        (inner_radius, z_top),
    ):
        for i in range(segments + 1):
            t = i / segments
            angle = math.radians(start_deg + (end_deg - start_deg) * t)
            verts.append((math.cos(angle) * radius, math.sin(angle) * radius, z))

    faces = []
    n = segments + 1
    for i in range(segments):
        j = i + 1
        faces.append((i, j, n + j, n + i))
        faces.append((2 * n + j, 2 * n + i, 3 * n + i, 3 * n + j))
        faces.append((n + i, n + j, 3 * n + j, 3 * n + i))
        faces.append((j, i, 2 * n + i, 2 * n + j))
    faces.append((0, n, 3 * n, 2 * n))
    faces.append((segments, 2 * n + segments, 3 * n + segments, n + segments))

    mesh = bpy.data.meshes.new(name + " mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    for polygon in mesh.polygons:
        polygon.use_smooth = True

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("single-piece weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_corridor_opened_cylindrical_shell(
    name: str,
    parent: bpy.types.Object,
    inner_radius: float,
    outer_radius: float,
    z_base: float,
    height: float,
    mat: bpy.types.Material,
) -> None:
    # Openings align to corridor axes: control/east, cockpit/north, cargo/south.
    # Only the lower doorway height is cut; the upper wall remains sealed.
    door_half = DOOR_OPENING_HALF_DEGREES
    solid_ranges = (
        (-180.0, -90.0 - door_half),
        (-90.0 + door_half, -door_half),
        (door_half, 90.0 - door_half),
        (90.0 + door_half, 180.0),
    )
    lower_height = min(DOOR_OPENING_HEIGHT, height)
    for index, (start_deg, end_deg) in enumerate(solid_ranges, start=1):
        arc_degrees = abs(end_deg - start_deg)
        segment_count = max(18, int(256 * arc_degrees / 360))
        add_cylindrical_shell(
            f"{name} lower sealed section {index}",
            parent,
            inner_radius,
            outer_radius,
            start_deg,
            end_deg,
            z_base,
            lower_height,
            mat,
            segment_count,
        )
    upper_height = max(height - lower_height, 0.0)
    if upper_height > 0.01:
        add_cylindrical_shell(
            f"{name} continuous upper doorway header",
            parent,
            inner_radius,
            outer_radius,
            -180.0,
            180.0,
            z_base + lower_height,
            upper_height,
            mat,
            256,
        )


def angle_to_xy(radius: float, degree: float, z: float) -> tuple[float, float, float]:
    a = math.radians(degree)
    return (math.cos(a) * radius, math.sin(a) * radius, z)


def radial_orientation(degree: float) -> float:
    return math.radians(degree - 90.0)


def add_direction_label(
    name: str,
    parent: bpy.types.Object,
    text: str,
    loc: tuple[float, float, float],
    face: str,
    mats: dict[str, bpy.types.Material],
) -> None:
    if face in ("north", "south"):
        plate_scale = (1.16, 0.045, 0.34)
        plate_rot = (0.0, 0.0, 0.0)
        if face == "north":
            text_loc = (loc[0], loc[1] + 0.032, loc[2] - 0.02)
            text_rot = (math.radians(90), 0.0, 0.0)
        else:
            text_loc = (loc[0], loc[1] - 0.032, loc[2] - 0.02)
            text_rot = (math.radians(90), 0.0, math.radians(180))
    else:
        plate_scale = (0.045, 1.16, 0.34)
        plate_rot = (0.0, 0.0, 0.0)
        if face == "east":
            text_loc = (loc[0] + 0.032, loc[1], loc[2] - 0.02)
            text_rot = (math.radians(90), 0.0, math.radians(-90))
        else:
            text_loc = (loc[0] - 0.032, loc[1], loc[2] - 0.02)
            text_rot = (math.radians(90), 0.0, math.radians(90))

    add_box(name + " wall label plate", parent, loc, plate_scale, mats["label_plate"], plate_rot, 0.010)
    bpy.ops.object.text_add(location=text_loc, rotation=text_rot)
    label = bpy.context.object
    label.name = name + " text"
    mirrored_text = {
        "-> 조종실": "실종조 <-",
        "-> 통제실": "실제통 <-",
        "-> 운송창고": "고창송운 <-",
    }
    label.data.body = mirrored_text.get(text, text)
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = 0.155
    label.data.extrude = 0.006
    label.data.materials.append(mats["label_text"])
    label.parent = parent


def add_floor_grating(
    root: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    start_deg: float,
    end_deg: float,
    count: int,
) -> None:
    for i in range(count):
        t = i / max(count - 1, 1)
        degree = start_deg + (end_deg - start_deg) * t
        radius = (INNER_RADIUS + OUTER_RADIUS) * 0.5
        loc = angle_to_xy(radius, degree, 0.035)
        add_box(
            f"radial deck rib {degree:.0f}",
            root,
            loc,
            (0.040, OUTER_RADIUS - INNER_RADIUS - 0.45, 0.035),
            mats["deck_rib"],
            (0.0, 0.0, radial_orientation(degree)),
            0.002,
        )


def add_doorway_side_seals(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    seal_height = DOOR_OPENING_HEIGHT * 0.5
    seal_z = DOOR_OPENING_HEIGHT * 0.5
    seal_depth = 1.04
    seal_offset = 0.88
    radial_center = OUTER_RADIUS + 0.48

    for side_x in (-seal_offset, seal_offset):
        add_box(
            "cockpit doorway sealed side return wall",
            root,
            (side_x, radial_center, seal_z),
            (0.24, seal_depth, seal_height * 2.0),
            mats["outer_wall"],
            bevel_width=0.012,
        )
        add_box(
            "cargo doorway sealed side return wall",
            root,
            (side_x, -radial_center, seal_z),
            (0.24, seal_depth, seal_height * 2.0),
            mats["outer_wall"],
            bevel_width=0.012,
        )

    for side_y in (-seal_offset, seal_offset):
        add_box(
            "control doorway sealed side return wall",
            root,
            (radial_center, side_y, seal_z),
            (seal_depth, 0.24, seal_height * 2.0),
            mats["outer_wall"],
            bevel_width=0.012,
        )


def build_engine_room_shell(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("ER-01 engine room shell Blender sample")

    # The engine room is inside a spaceship. Floor and walls are sealed; only corridor mouths are open.
    add_cylinder("sealed full circular floor deck", root, (0, 0, 0.0), OUTER_RADIUS, FLOOR_THICKNESS, mats["floor"], vertices=128)
    add_annular_sector("raised circular walking route panel", root, 1.38, OUTER_RADIUS - 0.34, -178, 178, 0.105, 0.045, mats["floor_panel"], 96)
    add_annular_sector("central sealed cylinder base gasket", root, 0.98, 1.34, -178, 178, 0.155, 0.075, mats["rim"], 96)

    add_corridor_opened_cylindrical_shell(
        "corridor-opened sealed cylindrical outer hull wall",
        root,
        OUTER_RADIUS - 0.02,
        OUTER_RADIUS + WALL_THICKNESS,
        0.0,
        WALL_HEIGHT,
        mats["outer_wall"],
    )
    add_corridor_opened_cylindrical_shell(
        "corridor-opened smooth interior pressure wall liner",
        root,
        OUTER_RADIUS - 0.22,
        OUTER_RADIUS - 0.04,
        0.04,
        WALL_HEIGHT - 0.12,
        mats["wall_liner"],
    )

    add_annular_sector("upper inspection rim around sealed power cylinder", root, 0.98, 1.34, -178, 178, 2.52, 0.12, mats["rim"], 96)
    add_annular_sector("solid outer upper maintenance rim", root, OUTER_RADIUS - 0.18, OUTER_RADIUS + 0.30, -178, 178, WALL_HEIGHT + 0.07, 0.14, mats["rim"], 18)

    # Corridor mouths and walls.
    add_box("cockpit corridor floor stub outside sealed wall", root, (0, OUTER_RADIUS + 1.62, 0.0), (1.55, 1.50, FLOOR_THICKNESS), mats["corridor_floor"], bevel_width=0.015)
    add_box("cockpit corridor left wall outside sealed wall", root, (-0.96, OUTER_RADIUS + 1.62, WALL_HEIGHT * 0.5), (0.26, 1.50, WALL_HEIGHT), mats["outer_wall"], bevel_width=0.014)
    add_box("cockpit corridor right wall outside sealed wall", root, (0.96, OUTER_RADIUS + 1.62, WALL_HEIGHT * 0.5), (0.26, 1.50, WALL_HEIGHT), mats["outer_wall"], bevel_width=0.014)

    add_box("control corridor floor stub outside sealed wall", root, (OUTER_RADIUS + 1.62, 0, 0.0), (1.50, 1.55, FLOOR_THICKNESS), mats["corridor_floor"], bevel_width=0.015)
    add_box("control corridor upper wall outside sealed wall", root, (OUTER_RADIUS + 1.62, 0.96, WALL_HEIGHT * 0.5), (1.50, 0.26, WALL_HEIGHT), mats["outer_wall"], bevel_width=0.014)
    add_box("control corridor lower wall outside sealed wall", root, (OUTER_RADIUS + 1.62, -0.96, WALL_HEIGHT * 0.5), (1.50, 0.26, WALL_HEIGHT), mats["outer_wall"], bevel_width=0.014)

    add_box("cargo descending ramp slab outside sealed wall", root, (0, -OUTER_RADIUS - 1.70, -0.20), (1.78, 1.70, 0.18), mats["ramp_floor"], (math.radians(-7), 0, 0), bevel_width=0.012)
    add_box("cargo ramp left wall outside sealed wall", root, (-1.10, -OUTER_RADIUS - 1.70, WALL_HEIGHT * 0.45 - 0.12), (0.26, 1.70, WALL_HEIGHT * 0.92), mats["outer_wall"], (math.radians(-7), 0, 0), 0.014)
    add_box("cargo ramp right wall outside sealed wall", root, (1.10, -OUTER_RADIUS - 1.70, WALL_HEIGHT * 0.45 - 0.12), (0.26, 1.70, WALL_HEIGHT * 0.92), mats["outer_wall"], (math.radians(-7), 0, 0), 0.014)
    add_doorway_side_seals(root, mats)

    # Sealed transparent central cylinder. It is isolated from the room and shows the inner power contents.
    add_cylinder("sealed transparent cylindrical power chamber glass", root, (0, 0, 1.34), 1.06, 2.28, mats["glass"], vertices=96)
    add_cylinder("sealed cylinder lower metal cap", root, (0, 0, 0.30), 1.12, 0.18, mats["rim"], vertices=96)
    add_cylinder("sealed cylinder upper metal cap", root, (0, 0, 2.46), 1.12, 0.18, mats["rim"], vertices=96)
    add_cylinder("visible inner power core column", root, (0, 0, 1.34), 0.20, 1.70, mats["core_glow"], vertices=48)
    add_sphere("visible suspended power plasma", root, (0, 0, 1.34), 0.44, mats["core_plasma"], (1.0, 1.0, 1.28), 48, 18)
    for index, degree in enumerate((0, 60, 120, 180, 240, 300), start=1):
        start = angle_to_xy(0.62, degree, 0.60)
        end = angle_to_xy(0.62, degree, 2.06)
        add_cylinder_between(f"visible insulated inner coil support {index}", root, start, end, 0.018, mats["core_metal"], 12)

    add_floor_grating(root, mats, -160, -110, 4)
    add_floor_grating(root, mats, -64, -24, 4)
    add_floor_grating(root, mats, 24, 64, 4)
    add_floor_grating(root, mats, 112, 160, 5)

    # Ramp and threshold markings.
    for offset in (-0.54, -0.18, 0.18, 0.54):
        add_box("cargo ramp amber hazard stripe", root, (offset, -OUTER_RADIUS - 0.24, 0.025), (0.12, 0.82, 0.032), mats["hazard"], (0, 0, math.radians(0)), 0.002)
    for degree in (-148, -128, -52, -32, 32, 52, 128, 148):
        loc = angle_to_xy(OUTER_RADIUS + 0.03, degree, 0.28)
        add_cylinder("outer wall exposed structural bolt", root, loc, 0.045, 0.036, mats["bolt"], (math.radians(90), 0, math.radians(degree)), 16)

    add_direction_label("cockpit direction", root, "-> 조종실", (0.0, OUTER_RADIUS + 0.09, 1.42), "north", mats)
    add_direction_label("control direction", root, "-> 통제실", (OUTER_RADIUS + 0.09, 0.0, 1.42), "east", mats)
    add_direction_label("cargo direction", root, "-> 운송창고", (0.0, -OUTER_RADIUS - 0.08, 1.30), "south", mats)

    # Cables and pipes are shell dressing only; no engine machinery is included.
    for y in (-0.45, 0.45):
        add_cylinder_between("ceiling conduit across shell opening", root, (-OUTER_RADIUS + 0.45, y, WALL_HEIGHT + 0.25), (OUTER_RADIUS - 0.45, y, WALL_HEIGHT + 0.25), 0.026, mats["conduit"], 14)
    add_cylinder_between("cargo ramp side utility pipe left", root, (-1.24, -OUTER_RADIUS - 2.30, 0.72), (-1.24, -OUTER_RADIUS - 0.28, 0.98), 0.030, mats["conduit"], 14)
    add_cylinder_between("cargo ramp side utility pipe right", root, (1.24, -OUTER_RADIUS - 2.30, 0.72), (1.24, -OUTER_RADIUS - 0.28, 0.98), 0.030, mats["conduit"], 14)


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 40
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("EngineRoomShellWorld")
    scene.world.color = (0.010, 0.012, 0.011)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, 0, 6.8))
    top = bpy.context.object
    top.name = "large overhead room inspection softbox"
    top.data.energy = 520
    top.data.size = 7.5

    bpy.ops.object.light_add(type="AREA", location=(-4.2, 5.2, 3.3))
    cool = bpy.context.object
    cool.name = "cool corridor entry fill"
    cool.data.energy = 180
    cool.data.size = 4.5
    cool.data.color = (0.70, 0.90, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(0, -5.7, 1.6))
    warm = bpy.context.object
    warm.name = "warm cargo ramp low light"
    warm.data.energy = 120
    warm.data.color = (1.0, 0.62, 0.32)


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
    camera.name = "engine room shell camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "engine_room_shell.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "engine_room_shell.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "engine_room_shell.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-01",
        "title": "동력실 룸 쉘",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt:122 - 동력실은 중앙 원통형 동력기계, 도넛형 구조, 위/아래/옆 복도 연결, 통로 옆면 구역 표시, 운송창고 방향 경사 복도를 가진다.",
            "docs/ENGINE_ROOM_OBJECTS.md - ER-01 동력실 룸 쉘, ER-03 중앙 빈 공간, ER-04~ER-08 연결 통로/방향 표시 기준.",
        ],
        "generatedFiles": [
            "blender/engine_room_shell.blend",
            "exports/engine_room_shell.fbx",
            "exports/engine_room_shell.glb",
            "renders/01_top.png",
            "renders/02_cockpit_entry.png",
            "renders/03_control_entry.png",
            "renders/04_cargo_ramp.png",
            "renders/05_inner_ring.png",
        "renders/06_sealed_wall.png",
        ],
        "includedParts": [
            "도넛형 링 바닥",
            "두꺼운 외벽과 중앙 빈 공간 내벽",
            "조종실 방향 통로 입구",
            "통제실 방향 통로 입구",
            "운송창고 방향 하강 경사 통로",
            "벽면 부착 방향 라벨",
            "중앙 동력기계 예약 풋프린트",
        ],
        "excludedParts": [
            "중앙 원통형 동력기계 본체",
            "내구도 스크린",
            "오버클럭 상호작용 장치",
            "손전등 충전 앵커",
            "사보타주/복구 앵커",
            "암전/파괴/오버클럭 상태 표현",
            "Unity 씬 배치",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-01",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전 Unity 씬, 프리팹, 런타임 자산에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# engine_room_shell

ER-01 동력실 룸 쉘 승인용 Blender 샘플입니다.

## 목적

원본 기획서의 동력실 구조를 Unity에 넣기 전 검토하기 위한 Blender 모델링 샘플입니다.
아직 승인되지 않았으므로 실제 Unity 씬, 프리팹, 런타임 자산에 연결하지 않습니다.

## 원본 반영 내용

- 동력실은 위에서 보면 가운데가 비어있는 도넛 형태입니다.
- 중앙에는 원통형 동력기계가 들어가야 하므로, 샘플에서는 중앙을 빈 축과 예약 풋프린트로 남겼습니다.
- 조종실, 통제실, 운송창고 방향으로 이어지는 통로 입구를 만들었습니다.
- 운송창고 방향 통로는 아래로 내려가는 경사 구조로 분리했습니다.
- 통로 옆면에 이어지는 구역 표시가 필요하므로 벽면 라벨을 붙였습니다.

## 포함

- `blender/engine_room_shell.blend`
- `exports/engine_room_shell.fbx`
- `exports/engine_room_shell.glb`
- `renders/*.png` 6개 구도
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- 중앙 원통형 동력기계 본체
- 내구도 스크린
- 오버클럭 상호작용 장치
- 손전등 충전 앵커
- 사보타주/복구 앵커
- 암전, 파괴, 오버클럭 상태 표현
- Unity 배치와 충돌 설정
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_top.png", "01 상단 구조"),
        ("02_cockpit_entry.png", "02 조종실 방향 입구"),
        ("03_control_entry.png", "03 통제실 방향 입구"),
        ("04_cargo_ramp.png", "04 운송창고 방향 경사"),
        ("05_inner_ring.png", "05 내부 링 보행 공간"),
        ("06_cutaway.png", "06 사선 절개 구조"),
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
  <title>engine_room_shell review</title>
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
  <h1>engine_room_shell</h1>
  <p>ER-01 동력실 룸 쉘 승인용 Blender 샘플입니다. 도넛형 룸 구조, 중앙 빈 공간, 조종실/통제실/운송창고 방향 통로, 운송창고 방향 하강 경사를 확인하기 위한 모델링 렌더입니다. 중앙 동력기계와 스크린 등 세부 오브젝트는 포함하지 않았습니다.</p>
  <section class="grid">
{cards}
  </section>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-01",
        "title": "동력실 룸 쉘",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt:122 - 동력실은 중앙 원통형 동력기계, 도넛형 구조, 위/아래/옆 복도 연결, 통로 옆면 구역 표시, 운송창고 방향 경사 복도를 가진다.",
            "사용자 확인 - 우주선 내부이므로 외벽과 바닥은 막힌 구조여야 하며, 중앙 원통은 외부와 단절된 투명 밀폐 구조로 내부 내용물이 보여야 한다.",
        ],
        "generatedFiles": [
            "blender/engine_room_shell.blend",
            "exports/engine_room_shell.fbx",
            "exports/engine_room_shell.glb",
            "renders/01_top.png",
            "renders/02_cockpit_entry.png",
            "renders/03_control_entry.png",
            "renders/04_cargo_ramp.png",
            "renders/05_inner_ring.png",
            "renders/06_sealed_wall.png",
        ],
        "includedParts": [
            "완전히 메워진 원형 바닥",
            "통로 입구를 제외하고 막힌 금속 외벽",
            "조종실 방향 통로 입구",
            "통제실 방향 통로 입구",
            "운송창고 방향 하강 경사 통로",
            "벽면 부착 방향 라벨",
            "외부와 단절된 투명 밀폐 원통",
            "투명 원통 안에 보이는 동력 코어 내용물",
        ],
        "excludedParts": [
            "내구도 스크린",
            "오버클럭 상호작용 장치",
            "손전등 충전 앵커",
            "사보타주/복구 앵커",
            "암전/파괴/오버클럭 상태 표현",
            "Unity 씬 배치",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-01",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전 Unity 씬, 프리팹, 런타임 자산에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# engine_room_shell

ER-01 동력실 룸 쉘 승인용 Blender 샘플입니다.

## 목적

원본 기획서의 동력실 구조를 Unity에 넣기 전 검토하기 위한 Blender 모델링 샘플입니다.
아직 승인되지 않았으므로 실제 Unity 씬, 프리팹, 런타임 자산에 연결하지 않습니다.

## 반영 기준

- 우주선 내부 구역이므로 외벽은 막힌 금속 벽체입니다.
- 바닥은 전체가 메워진 연속 바닥입니다.
- 중앙 원통은 외부와 단절된 밀폐 구조입니다.
- 중앙 원통은 투명 재질이며, 안쪽의 동력 코어 내용물이 보여야 합니다.
- 조종실, 통제실, 운송창고 방향 통로 입구만 열려 있습니다.
- 운송창고 방향 통로는 아래로 내려가는 경사 구조입니다.

## 포함

- `blender/engine_room_shell.blend`
- `exports/engine_room_shell.fbx`
- `exports/engine_room_shell.glb`
- `renders/*.png` 6개 구도
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- 내구도 스크린
- 오버클럭 상호작용 장치
- 손전등 충전 앵커
- 사보타주/복구 앵커
- 암전, 파괴, 오버클럭 상태 표현
- Unity 배치와 충돌 설정
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_top.png", "01 상단 구조"),
        ("02_cockpit_entry.png", "02 조종실 방향 입구"),
        ("03_control_entry.png", "03 통제실 방향 입구"),
        ("04_cargo_ramp.png", "04 운송창고 방향 경사"),
        ("05_inner_ring.png", "05 내부 링 보행 공간"),
        ("06_sealed_wall.png", "06 밀폐 외벽 사선 구조"),
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
  <title>engine_room_shell review</title>
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
  <h1>engine_room_shell</h1>
  <p>ER-01 동력실 룸 쉘 승인용 Blender 샘플입니다. 외벽과 바닥은 막힌 우주선 내부 구조이며, 중앙에는 외부와 단절된 투명 밀폐 원통과 내부 동력 코어가 보이도록 배치했습니다. 조종실, 통제실, 운송창고 방향 통로만 열려 있습니다.</p>
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
        "floor": noisy_metal("dark worn annular deck", (0.17, 0.20, 0.18, 1)),
        "floor_panel": noisy_metal("sealed circular floor panel variation", (0.13, 0.16, 0.15, 1)),
        "corridor_floor": noisy_metal("corridor floor worn steel", (0.15, 0.18, 0.17, 1)),
        "ramp_floor": noisy_metal("cargo descending ramp deck", (0.18, 0.20, 0.19, 1)),
        "outer_wall": noisy_metal("thick outer hull wall", (0.22, 0.27, 0.24, 1)),
        "wall_liner": noisy_metal("continuous sealed interior wall liner", (0.20, 0.25, 0.23, 1)),
        "inner_wall": noisy_metal("inner shaft safety wall", (0.14, 0.17, 0.16, 1)),
        "rim": noisy_metal("worn upper inspection rim", (0.30, 0.32, 0.28, 1)),
        "deck_rib": noisy_metal("raised deck rib metal", (0.095, 0.105, 0.10, 1)),
        "conduit": noisy_metal("dark utility conduit", (0.045, 0.050, 0.047, 1)),
        "bolt": noisy_metal("exposed wall bolt", (0.34, 0.34, 0.30, 1)),
        "hazard": material("muted amber ramp hazard paint", (0.86, 0.50, 0.12, 1), roughness=0.86),
        "void": material("black empty central depth", (0.004, 0.005, 0.005, 1), roughness=0.95),
        "reserved": material("transparent power machine reserved footprint", (0.18, 0.62, 0.58, 0.26), roughness=0.45, alpha=0.26, emission=(0.06, 0.28, 0.25, 1), emission_strength=0.12),
        "glass": material("sealed transparent power cylinder glass", (0.38, 0.86, 0.92, 0.28), roughness=0.12, alpha=0.28, emission=(0.02, 0.12, 0.14, 1), emission_strength=0.08),
        "core_glow": material("visible blue white inner power core", (0.42, 0.95, 1.0, 1), roughness=0.18, emission=(0.18, 0.86, 1.0, 1), emission_strength=1.9),
        "core_plasma": material("visible contained plasma volume", (0.16, 0.72, 0.90, 0.48), roughness=0.32, alpha=0.48, emission=(0.07, 0.48, 0.72, 1), emission_strength=0.95),
        "core_metal": noisy_metal("sealed inner coil support metal", (0.42, 0.43, 0.38, 1)),
        "label_plate": noisy_metal("black direction label plate", (0.030, 0.034, 0.032, 1)),
        "label_text": material("painted direction label text", (0.75, 0.86, 0.82, 1), roughness=0.72, emission=(0.20, 0.34, 0.30, 1), emission_strength=0.08),
    }

    build_engine_room_shell(mats)
    add_render_lights()

    cameras = [
        ("top", (0.0, 0.0, 12.5), (0.0, 0.0, 0.0), 50, "01_top.png", 10.8),
        ("cockpit_entry", (0.0, 8.9, 2.35), (0.0, 0.35, 1.05), 32, "02_cockpit_entry.png", None),
        ("control_entry", (8.9, 0.0, 2.35), (0.35, 0.0, 1.05), 32, "03_control_entry.png", None),
        ("cargo_ramp", (0.0, -9.3, 2.15), (0.0, -1.25, 0.72), 34, "04_cargo_ramp.png", None),
        ("inner_ring", (-2.65, 2.85, 1.72), (0.0, 0.0, 1.10), 32, "05_inner_ring.png", None),
        ("sealed_wall", (-1.45, -2.15, 1.55), (-4.05, -0.35, 1.28), 38, "06_sealed_wall.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} Blender sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
