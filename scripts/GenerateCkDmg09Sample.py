from __future__ import annotations

import json
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "ck_dmg09"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
CK02_LOW_SOURCE_BLEND = PROJECT_ROOT / "artSample" / "ck_ctl02_low" / "blender" / "ck_ctl02_low.blend"
CK02_LOW_SOURCE_FBX = PROJECT_ROOT / "artSample" / "ck_ctl02_low" / "exports" / "ck_ctl02_low.fbx"
CK02_LOW_SOURCE_GLB = PROJECT_ROOT / "artSample" / "ck_ctl02_low" / "exports" / "ck_ctl02_low.glb"


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


def worn_metal_material(name: str, base: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = material(name, base, metallic=0.28, roughness=0.88)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 38
    noise.inputs["Detail"].default_value = 10
    noise.inputs["Roughness"].default_value = 0.62
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.22
    ramp.color_ramp.elements[0].color = (base[0] * 0.46, base[1] * 0.46, base[2] * 0.46, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.48, 1),
        min(base[1] * 1.48, 1),
        min(base[2] * 1.48, 1),
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


def add_sphere(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    radius: float,
    mat: bpy.types.Material,
    scale: tuple[float, float, float] = (1.0, 1.0, 1.0),
    segments: int = 24,
    ring_count: int = 12,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=ring_count, radius=radius, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
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


def add_ring_segment(
    name: str,
    parent: bpy.types.Object,
    center: tuple[float, float, float],
    radius: float,
    start_deg: float,
    end_deg: float,
    tube_radius: float,
    mat: bpy.types.Material,
    steps: int = 10,
) -> None:
    cx, cy, cz = center
    prev = None
    for i in range(steps + 1):
        t = i / steps
        angle = math.radians(start_deg + ((end_deg - start_deg) * t))
        current = (cx + math.cos(angle) * radius, cy, cz + math.sin(angle) * radius)
        if prev is not None:
            add_cylinder_between(f"{name} segment {i:02d}", parent, prev, current, tube_radius, mat, vertices=14)
        prev = current


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
    world = bpy.data.worlds.new("ck_dmg09_world")
    world.color = (0.014, 0.015, 0.014)
    scene.world = world
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = -0.05
    scene.view_settings.gamma = 1


def build_context(mats: dict[str, bpy.types.Material]) -> None:
    context = add_empty("CK-09 cockpit placement context")
    add_box("context cockpit floor", context, (0.0, -0.05, -0.035), (4.8, 4.0, 0.07), mats["floor"], bevel_width=0)
    add_box("context forward panorama screen proxy", context, (0.0, 1.50, 1.66), (4.2, 0.06, 1.55), mats["glass"], bevel_width=0.01)
    add_box("context forward lower frame", context, (0.0, 1.46, 0.72), (4.36, 0.13, 0.18), mats["frame"], bevel_width=0.018)
    add_box("context left cockpit wall proxy", context, (-2.44, -0.12, 1.20), (0.08, 3.45, 2.42), mats["wall"], bevel_width=0)
    add_box("context right cockpit wall proxy", context, (2.44, -0.12, 1.20), (0.08, 3.45, 2.42), mats["wall"], bevel_width=0)
    add_box("context normal console footprint ghost", context, (0.0, -0.56, 0.50), (2.42, 1.16, 0.42), mats["ghost"], rot=(math.radians(-6), 0, 0), bevel_width=0.02)
    add_box("context normal top plane ghost", context, (0.0, -0.20, 0.90), (2.10, 0.92, 0.12), mats["ghost"], rot=(math.radians(-17), 0, 0), bevel_width=0.015)


def build_destroyed_console(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("CK-09 Cockpit Destroyed Console State")

    add_box("destroyed console lower shell left twisted", root, (-0.64, -0.52, 0.46), (1.36, 1.08, 0.42), mats["burnt_metal"], rot=(math.radians(-4), 0, math.radians(3)), bevel_width=0.025)
    add_box("destroyed console lower shell right blown open", root, (0.64, -0.50, 0.43), (1.25, 1.04, 0.34), mats["burnt_metal"], rot=(math.radians(-8), math.radians(0), math.radians(-6)), bevel_width=0.025)
    add_box("ruptured black interior cavity", root, (0.12, -0.35, 0.72), (1.28, 0.72, 0.30), mats["charcoal"], rot=(math.radians(-16), 0, 0), bevel_width=0.01)
    add_box("front armor lip cracked left", root, (-0.73, -1.10, 0.63), (1.08, 0.12, 0.22), mats["exposed"], rot=(math.radians(-5), 0, math.radians(-8)), bevel_width=0.012)
    add_box("front armor lip cracked right displaced", root, (0.82, -1.05, 0.58), (0.92, 0.12, 0.18), mats["burnt_metal"], rot=(math.radians(-14), 0, math.radians(13)), bevel_width=0.012)

    add_box("left top panel folded upward", root, (-0.80, -0.18, 0.93), (0.95, 0.48, 0.08), mats["panel"], rot=(math.radians(20), math.radians(-7), math.radians(-12)), bevel_width=0.014)
    add_box("right top panel collapsed inward", root, (0.76, -0.16, 0.82), (0.92, 0.42, 0.08), mats["panel"], rot=(math.radians(-30), math.radians(2), math.radians(18)), bevel_width=0.014)
    add_box("center torn access plate", root, (0.06, -0.58, 0.98), (0.74, 0.35, 0.065), mats["exposed"], rot=(math.radians(12), math.radians(9), math.radians(3)), bevel_width=0.008)
    add_box("black scorch bloom on top shell", root, (0.16, -0.52, 1.025), (1.10, 0.66, 0.018), mats["scorch"], rot=(math.radians(12), math.radians(0), math.radians(3)), bevel_width=0)

    add_ring_segment("broken helm ring left arc", root, (0.0, -0.78, 1.44), 0.42, 126, 242, 0.027, mats["burnt_brass"], steps=8)
    add_ring_segment("broken helm ring upper right arc", root, (0.04, -0.77, 1.46), 0.40, -12, 66, 0.025, mats["burnt_brass"], steps=6)
    add_cylinder_between("helm snapped spoke left", root, (0.0, -0.78, 1.44), (-0.32, -0.78, 1.58), 0.024, mats["burnt_brass"])
    add_cylinder_between("helm snapped spoke lower", root, (0.0, -0.78, 1.44), (0.04, -0.78, 1.08), 0.022, mats["burnt_brass"])
    add_cylinder("helm broken hub", root, (0.0, -0.78, 1.44), 0.105, 0.11, mats["exposed"], rot=(math.radians(90), 0, 0), vertices=24)
    add_cylinder_between("detached helm fragment on floor", root, (-0.86, -1.42, 0.09), (-0.50, -1.23, 0.12), 0.026, mats["burnt_brass"])

    add_cylinder_between("bent right throttle lower link", root, (0.92, -0.44, 0.94), (1.14, -0.58, 1.26), 0.038, mats["dark_rubber"], vertices=18)
    add_cylinder_between("bent right throttle upper link", root, (1.14, -0.58, 1.26), (1.02, -0.72, 1.43), 0.032, mats["dark_rubber"], vertices=18)
    add_sphere("scorched throttle knob", root, (1.02, -0.72, 1.45), 0.105, mats["red_glow"], scale=(1.0, 1.0, 0.82))

    wire_points = [
        ((-0.38, -0.52, 0.84), (-0.58, -0.86, 0.70), "wire red exposed 1", mats["wire_red"]),
        ((-0.22, -0.44, 0.82), (-0.04, -0.93, 0.67), "wire amber exposed 2", mats["wire_amber"]),
        ((0.18, -0.42, 0.82), (0.38, -0.82, 0.62), "wire cyan exposed 3", mats["wire_cyan"]),
        ((0.42, -0.38, 0.82), (0.70, -0.72, 0.65), "wire black exposed 4", mats["dark_rubber"]),
    ]
    for start, end, name, mat in wire_points:
        add_cylinder_between(name, root, start, end, 0.014, mat, vertices=10)
    add_sphere("small live spark red", root, (-0.58, -0.86, 0.70), 0.035, mats["spark_red"], scale=(1.0, 1.0, 0.55), segments=16, ring_count=8)
    add_sphere("small live spark amber", root, (-0.04, -0.93, 0.67), 0.026, mats["spark_amber"], scale=(1.0, 1.0, 0.55), segments=16, ring_count=8)
    add_sphere("small live spark cyan", root, (0.38, -0.82, 0.62), 0.024, mats["spark_cyan"], scale=(1.0, 1.0, 0.55), segments=16, ring_count=8)

    for index, (x, y, z, sx, sy, sz, rz) in enumerate(
        [
            (-1.15, -1.45, 0.065, 0.38, 0.12, 0.06, 18),
            (-0.64, -1.62, 0.058, 0.24, 0.16, 0.045, -22),
            (0.32, -1.42, 0.062, 0.34, 0.11, 0.055, 9),
            (0.96, -1.38, 0.068, 0.28, 0.14, 0.055, 28),
            (1.20, -0.86, 0.070, 0.18, 0.20, 0.045, -18),
            (-1.26, -0.74, 0.060, 0.18, 0.12, 0.055, 42),
        ],
        start=1,
    ):
        add_box(f"floor debris plate {index}", root, (x, y, z), (sx, sy, sz), mats["debris"], rot=(0, 0, math.radians(rz)), bevel_width=0.006)

    rng = random.Random(9309)
    for index in range(18):
        x = rng.uniform(-1.35, 1.35)
        y = rng.uniform(-1.55, -0.15)
        z = rng.uniform(0.045, 0.09)
        size = rng.uniform(0.035, 0.095)
        add_box(
            f"small shrapnel chip {index + 1:02d}",
            root,
            (x, y, z),
            (size * rng.uniform(1.2, 2.2), size * rng.uniform(0.45, 1.2), size * 0.30),
            mats["shrapnel"],
            rot=(rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), rng.uniform(0, math.tau)),
            bevel_width=0.003,
        )

    add_sphere("floor blast scorch oval", root, (0.08, -1.05, 0.005), 0.85, mats["scorch"], scale=(1.55, 0.82, 0.016), segments=32, ring_count=8)
    add_sphere("front shell soot halo", root, (0.12, -1.14, 0.69), 0.50, mats["soot"], scale=(1.8, 0.12, 0.55), segments=32, ring_count=8)
    add_sphere("faint smoke wisp one", root, (-0.28, -0.54, 1.34), 0.13, mats["smoke"], scale=(0.60, 0.32, 1.10), segments=20, ring_count=10)
    add_sphere("faint smoke wisp two", root, (0.30, -0.62, 1.22), 0.11, mats["smoke"], scale=(0.45, 0.28, 0.92), segments=20, ring_count=10)


def assign_material(obj: bpy.types.Object, mat: bpy.types.Material) -> None:
    if obj.type != "MESH":
        return

    obj.data.materials.clear()
    obj.data.materials.append(mat)


def import_ck02_low_source(mats: dict[str, bpy.types.Material]) -> list[bpy.types.Object]:
    if not CK02_LOW_SOURCE_BLEND.exists():
        raise FileNotFoundError(f"CK-02 low source blend is missing: {CK02_LOW_SOURCE_BLEND}")

    with bpy.data.libraries.load(str(CK02_LOW_SOURCE_BLEND), link=False) as (data_from, data_to):
        data_to.objects = list(data_from.objects)

    imported = [obj for obj in data_to.objects if obj is not None]
    for obj in imported:
        try:
            bpy.context.collection.objects.link(obj)
        except RuntimeError:
            pass
    bpy.context.view_layer.update()

    root = add_empty("CK-09 destroyed state derived from ck_ctl02_low")
    for obj in list(imported):
        try:
            lower = obj.name.lower()
            obj_type = obj.type
        except ReferenceError:
            continue
        if (
            obj_type in {"CAMERA", "LIGHT"}
            or lower.startswith("cam_")
            or lower.startswith("ck-02 placement context")
            or "scale reference only" in lower
            or "cockpit floor footprint proxy" in lower
            or "front broad window screen proxy" in lower
            or "front lower sill proxy" in lower
            or "front upper frame proxy" in lower
            or "cockpit wall proxy" in lower
            or "player clearance marker" in lower
            or "console front anchor marker" in lower
        ):
            remove_object_tree(obj)

    live_imported = []
    for obj in imported:
        try:
            if obj.name in bpy.data.objects:
                live_imported.append(obj)
        except ReferenceError:
            pass
    imported = live_imported
    for obj in imported:
        if obj.parent is None:
            obj.parent = root
    bpy.context.view_layer.update()

    for obj in imported:
        lower = obj.name.lower()
        if obj.type != "MESH":
            continue

        if any(
            key in lower
            for key in (
                "single-piece heavy lower console base",
                "sloped main control deck",
                "raised rear instrument coaming",
                "recessed black hand rest strip",
                "front armored lower kick plate",
                "angled side cheek",
                "floor bolted console foot",
            )
        ):
            assign_material(obj, mats["burnt_metal"])
        elif any(key in lower for key in ("control plate", "mechanical switch panel", "lever recessed slot")):
            assign_material(obj, mats["scorched_panel"])
        elif any(key in lower for key in ("caution label", "indicator light", "analog gauge face", "interaction")):
            assign_material(obj, mats["dead_indicator"])
        elif "right forward lever" in lower or "helm" in lower:
            assign_material(obj, mats["burnt_brass"] if "helm" in lower else mats["charcoal"])

    remove_source_parts(imported, ("f interaction letter marker",))
    remove_source_parts(imported, ("central analog gauge face 2", "central analog gauge face 4", "left toggle switch 2-2", "right toggle switch 1-3"))
    return imported


def remove_object_tree(obj: bpy.types.Object) -> None:
    for child in list(obj.children):
        remove_object_tree(child)
    if obj.name in bpy.data.objects:
        bpy.data.objects.remove(obj, do_unlink=True)


def remove_source_parts(imported: list[bpy.types.Object], name_parts: tuple[str, ...]) -> None:
    for obj in list(imported):
        try:
            lower = obj.name.lower()
        except ReferenceError:
            continue
        if any(part in lower for part in name_parts):
            bpy.data.objects.remove(obj, do_unlink=True)


def source_objects_with(*name_parts: str) -> list[bpy.types.Object]:
    return [
        obj
        for obj in bpy.data.objects
        if any(part in obj.name.lower() for part in name_parts)
    ]


def remove_source_objects_with(*name_parts: str) -> None:
    for obj in source_objects_with(*name_parts):
        remove_object_tree(obj)


def angle_in_ranges(angle: float, ranges: tuple[tuple[float, float], ...]) -> bool:
    normalized = angle % 360.0
    for start, end in ranges:
        start %= 360.0
        end %= 360.0
        if start <= end:
            if start <= normalized <= end:
                return True
        elif normalized >= start or normalized <= end:
            return True
    return False


def cut_mesh_by_world_angle(
    obj: bpy.types.Object,
    center: tuple[float, float, float],
    ranges_deg: tuple[tuple[float, float], ...],
) -> None:
    if obj.type != "MESH":
        return

    obj.data = obj.data.copy()
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    center_v = Vector(center)
    delete_faces = []
    for face in bm.faces:
        world = obj.matrix_world @ face.calc_center_median()
        angle = math.degrees(math.atan2(world.z - center_v.z, world.x - center_v.x))
        if angle_in_ranges(angle, ranges_deg):
            delete_faces.append(face)

    if delete_faces:
        bmesh.ops.delete(bm, geom=delete_faces, context="FACES")
        isolated = [vert for vert in bm.verts if not vert.link_edges]
        if isolated:
            bmesh.ops.delete(bm, geom=isolated, context="VERTS")
        bm.to_mesh(obj.data)
        obj.data.update()
    bm.free()


def detach_keep_world(objects: list[bpy.types.Object]) -> None:
    bpy.context.view_layer.update()
    for obj in objects:
        if obj.name not in bpy.data.objects:
            continue
        matrix = obj.matrix_world.copy()
        obj.parent = None
        obj.matrix_world = matrix
    bpy.context.view_layer.update()


def transform_objects_around_pivot(
    objects: list[bpy.types.Object],
    pivot: tuple[float, float, float],
    rotation: tuple[float, float, float],
    translation: tuple[float, float, float],
) -> None:
    pivot_v = Vector(pivot)
    matrix = (
        Matrix.Translation(Vector(translation))
        @ Matrix.Translation(pivot_v)
        @ Euler(rotation, "XYZ").to_matrix().to_4x4()
        @ Matrix.Translation(-pivot_v)
    )
    for obj in objects:
        if obj.name in bpy.data.objects:
            obj.matrix_world = matrix @ obj.matrix_world


def damage_original_helm(mats: dict[str, bpy.types.Material], overlay_root: bpy.types.Object) -> None:
    original_center = (0.0, -0.02, 1.84)
    remove_source_objects_with(
        "helm angled support strut",
        "helm radial spoke 2",
        "helm radial spoke 6",
        "helm radial spoke 7",
        "helm outer hand knob 2",
        "helm outer hand knob 6",
        "helm outer hand knob 7",
    )

    helm_objects = source_objects_with(
        "large ship helm wheel ring",
        "helm radial spoke",
        "helm outer hand knob",
        "helm bearing housing",
        "helm worn brass hub cap",
    )
    detach_keep_world(helm_objects)
    for ring in [obj for obj in helm_objects if "large ship helm wheel ring" in obj.name.lower()]:
        cut_mesh_by_world_angle(ring, original_center, ((54.0, 102.0), (236.0, 312.0)))
        assign_material(ring, mats["burnt_brass"])

    for obj in helm_objects:
        assign_material(obj, mats["burnt_brass"] if "helm bearing housing" not in obj.name.lower() else mats["charcoal"])

    transform_objects_around_pivot(
        helm_objects,
        original_center,
        (math.radians(68.0), math.radians(-10.0), math.radians(-26.0)),
        (-0.72, -0.86, -0.86),
    )

    add_cylinder_between(
        "ck02 original helm support snapped lower stump",
        overlay_root,
        (0.0, 0.02, 0.96),
        (0.18, -0.16, 1.02),
        0.055,
        mats["charcoal"],
        vertices=18,
    )
    add_cylinder_between(
        "ck02 fallen original helm support torn end",
        overlay_root,
        (-0.56, -1.04, 0.82),
        (-0.88, -1.18, 0.84),
        0.043,
        mats["exposed"],
        vertices=14,
    )


def damage_original_lever(mats: dict[str, bpy.types.Material]) -> None:
    lever_objects = source_objects_with(
        "right forward lever pivot drum",
        "right forward push lever shaft",
        "right forward lever horizontal grip",
    )
    detach_keep_world(lever_objects)
    for obj in lever_objects:
        lower = obj.name.lower()
        assign_material(obj, mats["red_glow"] if "horizontal grip" in lower else mats["charcoal"])

    transform_objects_around_pivot(
        lever_objects,
        (1.82, -0.28, 0.98),
        (math.radians(-52.0), math.radians(0.0), math.radians(12.0)),
        (0.12, -0.07, -0.09),
    )


def add_damage_to_ck02_low(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("CK-09 damage overlay on ck_ctl02_low")
    damage_original_helm(mats, root)
    damage_original_lever(mats)

    add_box("ck02 source sloped deck scorch layer", root, (0.02, 0.58, 0.74), (3.35, 0.76, 0.028), mats["scorch"], rot=(math.radians(-14), 0, math.radians(1.5)), bevel_width=0)
    add_box("ck02 source central rupture opening", root, (0.08, 0.64, 0.93), (1.16, 0.36, 0.055), mats["charcoal"], rot=(math.radians(67), 0, math.radians(3)), bevel_width=0.01)
    add_box("ck02 source lifted left switch panel", root, (-1.42, 0.59, 0.92), (1.00, 0.055, 0.28), mats["exposed"], rot=(math.radians(42), math.radians(-11), math.radians(-15)), bevel_width=0.012)
    add_box("ck02 source collapsed right switch panel", root, (1.48, 0.55, 0.80), (0.98, 0.055, 0.27), mats["scorched_panel"], rot=(math.radians(82), math.radians(8), math.radians(16)), bevel_width=0.012)
    add_box("ck02 source torn central gauge plate", root, (0.02, 0.54, 1.00), (1.04, 0.050, 0.25), mats["exposed"], rot=(math.radians(70), math.radians(7), math.radians(-4)), bevel_width=0.008)
    add_box("ck02 source front kick plate blast scar", root, (-0.22, -0.34, 0.18), (1.55, 0.025, 0.28), mats["scorch"], bevel_width=0)
    add_box("ck02 source right cheek torn bright edge", root, (2.58, 0.31, 0.53), (0.045, 0.76, 0.26), mats["exposed"], rot=(0, 0, math.radians(9)), bevel_width=0.006)

    exposed_wire_points = [
        ((-0.38, 0.37, 0.86), (-0.70, 0.02, 0.61), "ck02 exposed red wire", mats["wire_red"]),
        ((-0.10, 0.40, 0.88), (0.00, -0.10, 0.58), "ck02 exposed amber wire", mats["wire_amber"]),
        ((0.22, 0.40, 0.84), (0.55, 0.03, 0.62), "ck02 exposed cyan wire", mats["wire_cyan"]),
        ((0.45, 0.42, 0.82), (0.85, 0.14, 0.59), "ck02 exposed black wire", mats["dark_rubber"]),
    ]
    for start, end, name, mat in exposed_wire_points:
        add_cylinder_between(name, root, start, end, 0.014, mat, vertices=10)
    add_sphere("ck02 red wire spark", root, (-0.70, 0.02, 0.61), 0.032, mats["spark_red"], scale=(1, 1, 0.55), segments=16, ring_count=8)
    add_sphere("ck02 amber wire spark", root, (0.00, -0.10, 0.58), 0.025, mats["spark_amber"], scale=(1, 1, 0.55), segments=16, ring_count=8)
    add_sphere("ck02 cyan wire spark", root, (0.55, 0.03, 0.62), 0.023, mats["spark_cyan"], scale=(1, 1, 0.55), segments=16, ring_count=8)

    rng = random.Random(9309)
    for index in range(22):
        x = rng.uniform(-2.15, 2.15)
        y = rng.uniform(-1.55, 0.10)
        z = rng.uniform(0.045, 0.10)
        size = rng.uniform(0.035, 0.11)
        add_box(
            f"ck02 floor shrapnel chip {index + 1:02d}",
            root,
            (x, y, z),
            (size * rng.uniform(1.1, 2.35), size * rng.uniform(0.45, 1.1), size * 0.30),
            mats["shrapnel"],
            rot=(rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), rng.uniform(0, math.tau)),
            bevel_width=0.003,
        )
    for index, (x, y, sx, sy, rz) in enumerate(
        [
            (-1.55, -1.34, 0.50, 0.15, 14),
            (-0.62, -1.48, 0.32, 0.18, -25),
            (0.58, -1.24, 0.42, 0.14, 11),
            (1.44, -0.92, 0.32, 0.18, 31),
            (1.82, -0.18, 0.22, 0.20, -16),
        ],
        start=1,
    ):
        add_box(f"ck02 larger fallen console plate {index}", root, (x, y, 0.070), (sx, sy, 0.055), mats["debris"], rot=(0, 0, math.radians(rz)), bevel_width=0.006)

    add_sphere("ck02 floor blast scorch oval", root, (0.03, -0.72, 0.006), 0.90, mats["scorch"], scale=(1.95, 1.00, 0.016), segments=32, ring_count=8)
    add_sphere("ck02 faint smoke over ruptured deck", root, (-0.12, 0.42, 1.26), 0.14, mats["smoke"], scale=(0.75, 0.42, 1.15), segments=20, ring_count=10)
    add_sphere("ck02 faint smoke near right lever", root, (1.30, 0.24, 1.16), 0.12, mats["smoke"], scale=(0.58, 0.38, 0.96), segments=20, ring_count=10)


def add_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -3.5, 4.3))
    key = bpy.context.object
    key.name = "ck09 destroyed console large softbox"
    key.data.energy = 480
    key.data.size = 5.2

    bpy.ops.object.light_add(type="POINT", location=(-0.5, -0.85, 1.05))
    red = bpy.context.object
    red.name = "ck09 exposed wire red spill"
    red.data.energy = 46
    red.data.color = (1.0, 0.12, 0.06)

    bpy.ops.object.light_add(type="POINT", location=(0.40, -0.8, 0.95))
    cyan = bpy.context.object
    cyan.name = "ck09 exposed wire cyan spill"
    cyan.data.energy = 20
    cyan.data.color = (0.1, 0.8, 0.9)


def set_condition_ghost_visible(visible: bool) -> None:
    for obj in bpy.data.objects:
        if "ghost" in obj.name.lower():
            obj.hide_render = not visible


def render_camera(camera: bpy.types.Object, output_name: str, *, show_condition_ghost: bool = False) -> None:
    set_condition_ghost_visible(show_condition_ghost)
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
        "objectId": "CK-09",
        "scope": "조종대 파손 / 폭파 상태 부품 샘플",
        "sourceBasis": [
            "docs/COCKPIT_OBJECTS.md: CK-09 조종대 파손 / 폭파 상태 부품",
            "artSample/ck_ctl02_low/blender/ck_ctl02_low.blend: 승인된 CK-02 낮은 본체 조종대 원본 오브젝트",
            "docs/GAME_DESIGN_SOURCE.txt:121 조종실 내구도 0% 시 조종대 폭파 및 주변 유저 피해",
            "docs/PROGRESS_2026-06-16.md: 조종실 오브젝트는 artSample 승인 후 Unity 적용",
        ],
        "baseSample": "ck_ctl02_low",
        "baseAsset": str(CK02_LOW_SOURCE_BLEND),
        "conditionalVisibility": {
            "futureUnityRule": "ShipRoomId.Cockpit durability <= 0 일 때만 표시",
            "normalState": "CK-02 메인 조종대가 표시되고 CK-09는 숨김",
            "destroyedState": "CK-02 메인 조종대는 숨기고 CK-09 파손 상태를 표시",
            "notTriggeredBy": [
                "조종실 내구도 50% 이하 자동 조종 불가",
                "아타 자동 조종 사보타주",
                "부분 손상 상태",
                "수동 운행 UI 진입",
            ],
        },
        "included": [
            "ck_ctl02_low 원본 조종대 형상",
            "폭파로 갈라진 조종대 외피",
            "접힌 상단 패널",
            "부서진 타륜 조작 핸들",
            "휘어진 오른쪽 전진 레버",
            "노출 배선과 작은 스파크",
            "바닥 파편과 그을림",
            "연기 위치 확인용 투명 위습",
        ],
        "excluded": [
            "Unity 런타임 연결",
            "실제 피해 판정",
            "실제 파티클/VFX",
            "사운드",
            "게임플레이 콜라이더",
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
        "conditionToReview": "조종실 내구도 0% 시 기존 CK-02 조종대를 숨기고 이 CK-09 파손 상태를 표시하는 용도",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(
        json.dumps(approval, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    readme = """# ck_dmg09

CK-09 조종대 파손 / 폭파 상태 부품 샘플입니다.

이 샘플은 새 조종대를 새로 만든 것이 아니라 승인된 `ck_ctl02_low` Blender 파일의 원본 오브젝트를 불러온 뒤, 그 원본 타륜과 오른쪽 전진 레버를 파손 상태로 변형한 버전입니다.

## 조건

- Unity 반영은 아직 하지 않았습니다.
- 승인 후 Unity에 적용할 때도 상시 배치물이 아니라 조건부 상태입니다.
- 조건은 `조종실 내구도 0%`입니다.
- 정상 상태에서는 기존 CK-02 메인 조종대를 표시하고, CK-09는 숨깁니다.
- 파괴 상태에서는 CK-02 메인 조종대를 숨기고, CK-09 파손 상태를 표시합니다.
- 조종실 내구도 50% 이하 자동 조종 불가, 아타 자동 조종 사보타주, 부분 손상 상태, 수동 운행 UI 진입만으로는 CK-09가 나타나지 않습니다.

## 포함

- `ck_ctl02_low` 원본 조종대 형상
- 폭파로 갈라진 조종대 외피
- 접힌 상단 패널
- 부서진 타륜 조작 핸들
- 휘어진 오른쪽 전진 레버
- 노출 배선과 작은 스파크
- 바닥 파편과 그을림
- 연기 위치 확인용 투명 위습

## 제외

- Unity 런타임 연결
- 실제 피해 판정
- 실제 파티클/VFX
- 사운드
- 게임플레이 콜라이더

## 배치 기준

- CK-02 메인 조종대와 같은 자리에서 교체 표시되는 파생 상태입니다.
- 전면 파노라마 화면과 조종실 벽은 배치 확인용 프록시입니다.
- 바닥 파편은 플레이어 통행을 막지 않는 시각물로만 취급합니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_front.png", "전면: 조종대 폭파 상태 전체"),
        ("02_player.png", "플레이어 접근 방향: 통행을 막지 않는 파손 상태"),
        ("03_side.png", "측면: 조종대 높이와 전면 화면 거리"),
        ("04_top.png", "상단: 파편과 그을림 범위"),
        ("05_detail.png", "상세: 부서진 타륜, 휘어진 레버, 노출 배선"),
        ("06_condition_swap.png", "조건부 교체 확인: 정상 조종대 위치의 파손 상태"),
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
    .notice {{ border-left: 4px solid #b64c2c; padding: 10px 14px; background: #211a17; margin: 16px 0 20px; }}
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
  <p>CK-09 조종대 파손 / 폭파 상태 부품 샘플입니다. 아직 Unity 씬에는 적용하지 않았습니다.</p>
  <p>이 샘플은 승인된 <code>ck_ctl02_low</code> Blender 파일의 원본 오브젝트를 불러온 뒤, 원본 타륜과 오른쪽 전진 레버를 파손 상태로 변형한 버전입니다.</p>
  <div class="notice">조건: 승인 후 Unity에 붙일 경우 <code>조종실 내구도 0%</code>일 때만 기존 CK-02 조종대를 숨기고 이 파손 상태를 표시합니다.</div>
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
        "floor": material("context dark cockpit floor", (0.09, 0.105, 0.10, 1), roughness=0.88),
        "glass": material("context front panorama glass", (0.06, 0.36, 0.32, 0.30), roughness=0.20, alpha=0.30, emission=(0.04, 0.24, 0.20, 1), emission_strength=0.18),
        "wall": material("context muted cockpit wall", (0.15, 0.18, 0.17, 0.52), roughness=0.88, alpha=0.52),
        "frame": worn_metal_material("context black window frame", (0.055, 0.063, 0.060, 1)),
        "ghost": material("transparent normal CK02 footprint", (0.18, 0.23, 0.22, 0.14), roughness=0.8, alpha=0.14),
        "burnt_metal": worn_metal_material("burnt dark console metal", (0.045, 0.046, 0.040, 1)),
        "panel": worn_metal_material("folded dark console panel", (0.10, 0.115, 0.105, 1)),
        "scorched_panel": worn_metal_material("scorched original ck02 panel", (0.060, 0.066, 0.058, 1)),
        "dead_indicator": material("dead dim indicator glass", (0.18, 0.12, 0.08, 1), roughness=0.72),
        "charcoal": material("deep black ruptured cavity", (0.003, 0.003, 0.002, 1), roughness=0.96),
        "exposed": material("exposed torn steel edge", (0.58, 0.56, 0.50, 1), metallic=0.38, roughness=0.64),
        "burnt_brass": worn_metal_material("burnt brass helm fragments", (0.25, 0.18, 0.10, 1)),
        "dark_rubber": material("charred black rubber", (0.006, 0.006, 0.005, 1), roughness=0.97),
        "red_glow": material("damaged red throttle glow", (0.85, 0.07, 0.025, 1), roughness=0.5, emission=(0.70, 0.035, 0.012, 1), emission_strength=0.52),
        "wire_red": material("exposed red wire", (0.75, 0.035, 0.018, 1), roughness=0.48, emission=(0.38, 0.018, 0.006, 1), emission_strength=0.20),
        "wire_amber": material("exposed amber wire", (0.95, 0.48, 0.08, 1), roughness=0.48, emission=(0.50, 0.20, 0.02, 1), emission_strength=0.22),
        "wire_cyan": material("exposed cyan wire", (0.10, 0.68, 0.78, 1), roughness=0.48, emission=(0.04, 0.36, 0.46, 1), emission_strength=0.24),
        "spark_red": material("small red spark", (1.0, 0.10, 0.04, 1), roughness=0.3, emission=(1.0, 0.04, 0.015, 1), emission_strength=1.1),
        "spark_amber": material("small amber spark", (1.0, 0.72, 0.10, 1), roughness=0.3, emission=(1.0, 0.45, 0.05, 1), emission_strength=1.0),
        "spark_cyan": material("small cyan spark", (0.30, 0.92, 1.0, 1), roughness=0.3, emission=(0.08, 0.75, 0.9, 1), emission_strength=0.85),
        "debris": worn_metal_material("burnt loose debris plates", (0.07, 0.075, 0.065, 1)),
        "shrapnel": material("small dark shrapnel", (0.035, 0.037, 0.032, 1), metallic=0.25, roughness=0.82),
        "scorch": material("transparent black scorch mark", (0.0, 0.0, 0.0, 0.54), roughness=0.98, alpha=0.54),
        "soot": material("soft soot on front shell", (0.0, 0.0, 0.0, 0.38), roughness=0.98, alpha=0.38),
        "smoke": material("transparent smoke placement wisp", (0.15, 0.15, 0.14, 0.12), roughness=0.98, alpha=0.12),
    }

    build_context(mats)
    import_ck02_low_source(mats)
    add_damage_to_ck02_low(mats)
    add_lights()

    cameras = [
        ("front", (0.0, -5.8, 2.42), (0.0, 0.82, 1.66), 36, "01_front.png", None),
        ("player", (0.0, -3.35, 1.64), (0.0, 0.62, 1.46), 32, "02_player.png", None),
        ("side", (5.5, -1.5, 2.0), (0.1, 0.55, 1.0), 42, "03_side.png", None),
        ("top", (0.0, 0.25, 7.4), (0.0, 0.25, 0.0), 50, "04_top.png", 6.2),
        ("detail", (-2.35, -2.35, 1.52), (-0.68, -0.92, 0.96), 42, "05_detail.png", None),
        ("condition", (3.1, -3.7, 2.38), (0.0, 0.50, 1.05), 44, "06_condition_swap.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        render_camera(
            add_camera("ck09 camera " + name, loc, target, lens, ortho_scale),
            output,
            show_condition_ghost=name == "condition",
        )

    export_assets()
    write_docs()
    print(SAMPLE_NAME + " sample generated: " + str(SAMPLE_ROOT))


if __name__ == "__main__":
    main()
