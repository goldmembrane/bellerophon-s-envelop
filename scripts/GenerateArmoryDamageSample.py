from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "armory_damage_state"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"


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
    mat = material(name, base, metallic=0.28, roughness=0.88)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 38
    noise.inputs["Detail"].default_value = 9
    noise.inputs["Roughness"].default_value = 0.58
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.20
    ramp.color_ramp.elements[0].color = (base[0] * 0.42, base[1] * 0.42, base[2] * 0.42, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.32, 1),
        min(base[1] * 1.32, 1),
        min(base[2] * 1.32, 1),
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
        bevel.segments = 2
        obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_cylinder_between(
    name: str,
    parent: bpy.types.Object,
    start: tuple[float, float, float],
    end: tuple[float, float, float],
    radius: float,
    mat: bpy.types.Material,
    vertices: int = 10,
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
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    segments: int = 24,
    rings: int = 12,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
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
    size: float = 0.16,
) -> bpy.types.Object:
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.004
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def add_triangle(
    name: str,
    parent: bpy.types.Object,
    points: tuple[tuple[float, float, float], tuple[float, float, float], tuple[float, float, float]],
    mat: bpy.types.Material,
) -> bpy.types.Object:
    mesh = bpy.data.meshes.new(name + " mesh")
    mesh.from_pydata(list(points), [], [(0, 1, 2)])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def curved_screen_y(x: float, width: float = 3.20, center_y: float = 0.82, depth: float = 0.30) -> float:
    normalized = min(abs(x) / (width * 0.5), 1.0)
    return center_y - depth * (normalized**1.75)


def curved_screen_point(x: float, z: float, *, front_offset: float = 0.035) -> tuple[float, float, float]:
    return (x, curved_screen_y(x) - front_offset, z)


def add_curved_surface(
    name: str,
    parent: bpy.types.Object,
    width: float,
    bottom_z: float,
    top_z: float,
    mat: bpy.types.Material,
    *,
    segments: int = 36,
    front_offset: float = 0.0,
) -> bpy.types.Object:
    verts: list[tuple[float, float, float]] = []
    for row_z in (bottom_z, top_z):
        for index in range(segments + 1):
            x = -width * 0.5 + width * index / segments
            verts.append((x, curved_screen_y(x) - front_offset, row_z))

    faces = []
    for index in range(segments):
        faces.append((index, index + 1, index + segments + 2, index + segments + 1))

    mesh = bpy.data.meshes.new(name + " mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("AR-06 curved weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_curved_frame(parent: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    width = 3.34
    bottom_z = 1.05
    top_z = 2.00
    segments = 16
    for index in range(segments):
        x0 = -width * 0.5 + width * index / segments
        x1 = -width * 0.5 + width * (index + 1) / segments
        add_cylinder_between(
            "AR-06 curved large monitor top frame segment %02d" % index,
            parent,
            curved_screen_point(x0, top_z, front_offset=0.008),
            curved_screen_point(x1, top_z, front_offset=0.008),
            0.028,
            mats["frame"],
            10,
        )
        add_cylinder_between(
            "AR-06 curved large monitor bottom frame segment %02d" % index,
            parent,
            curved_screen_point(x0, bottom_z, front_offset=0.008),
            curved_screen_point(x1, bottom_z, front_offset=0.008),
            0.028,
            mats["frame"],
            10,
        )

    left_x = -width * 0.5
    right_x = width * 0.5
    add_cylinder_between(
        "AR-06 curved large monitor left side frame",
        parent,
        curved_screen_point(left_x, bottom_z, front_offset=0.008),
        curved_screen_point(left_x, top_z, front_offset=0.008),
        0.034,
        mats["frame"],
        10,
    )
    add_cylinder_between(
        "AR-06 curved large monitor right side frame",
        parent,
        curved_screen_point(right_x, bottom_z, front_offset=0.008),
        curved_screen_point(right_x, top_z, front_offset=0.008),
        0.034,
        mats["frame"],
        10,
    )
    add_box("AR-06 large curved monitor left wall bracket", parent, (-1.82, 0.46, 1.52), (0.10, 0.20, 0.78), mats["frame"], bevel_width=0.012)
    add_box("AR-06 large curved monitor right wall bracket", parent, (1.82, 0.46, 1.52), (0.10, 0.20, 0.78), mats["frame"], bevel_width=0.012)
    add_box("AR-06 large curved monitor dead lower icon strip", parent, (-1.18, 0.47, 1.09), (0.42, 0.020, 0.034), mats["dim_strip"], bevel_width=0.004)


def add_dense_cracks_on_curved_monitor(parent: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    center_x = 0.16
    center_z = 1.50
    center = curved_screen_point(center_x, center_z, front_offset=0.070)
    add_uv_sphere(
        "AR-06 cracked glass crushed white impact cluster",
        parent,
        center,
        (0.22, 0.018, 0.15),
        mats["crushed_glass"],
        40,
        10,
    )
    add_uv_sphere(
        "AR-06 cracked glass black central puncture",
        parent,
        curved_screen_point(center_x + 0.015, center_z - 0.010, front_offset=0.084),
        (0.070, 0.010, 0.050),
        mats["pit"],
        24,
        8,
    )

    radial_cracks = [
        (-1.45, 1.88, 0.010, "far upper left"),
        (-1.62, 1.47, 0.010, "far left horizontal"),
        (-1.35, 1.16, 0.009, "far lower left"),
        (-0.92, 1.96, 0.008, "upper left rake"),
        (-0.70, 1.22, 0.008, "lower left rake"),
        (-0.26, 1.98, 0.007, "near top left"),
        (0.06, 1.05, 0.008, "near bottom drop"),
        (0.28, 2.00, 0.007, "top center"),
        (0.48, 1.03, 0.009, "bottom center"),
        (0.82, 1.94, 0.010, "upper right"),
        (1.18, 1.73, 0.010, "long upper right"),
        (1.55, 1.51, 0.011, "far right horizontal"),
        (1.28, 1.20, 0.010, "long lower right"),
        (0.74, 1.26, 0.007, "short lower right"),
        (0.55, 1.63, 0.007, "short upper right"),
        (-0.42, 1.64, 0.007, "short upper left"),
    ]
    for end_x, end_z, radius, label in radial_cracks:
        mid_x = center_x + (end_x - center_x) * 0.46
        mid_z = center_z + (end_z - center_z) * 0.46 + (0.035 if end_z > center_z else -0.026)
        mid = curved_screen_point(mid_x, mid_z, front_offset=0.068)
        end = curved_screen_point(end_x, end_z, front_offset=0.066)
        add_cylinder_between("AR-06 cracked curved monitor radial fracture %s inner" % label, parent, center, mid, radius, mats["crack"], 8)
        add_cylinder_between("AR-06 cracked curved monitor radial fracture %s outer" % label, parent, mid, end, radius * 0.74, mats["crack"], 8)

    branch_lines = [
        ((-1.05, 1.76), (-1.34, 1.62), "upper left branch A"),
        ((-0.78, 1.70), (-0.93, 1.88), "upper left branch B"),
        ((-0.68, 1.33), (-0.98, 1.24), "lower left branch A"),
        ((-0.38, 1.25), (-0.56, 1.08), "lower left branch B"),
        ((-0.10, 1.74), (-0.25, 1.93), "top center branch"),
        ((0.05, 1.28), (-0.12, 1.10), "bottom center branch"),
        ((0.43, 1.74), (0.66, 1.90), "upper right branch A"),
        ((0.65, 1.62), (0.98, 1.55), "upper right branch B"),
        ((0.64, 1.34), (0.94, 1.20), "lower right branch A"),
        ((1.03, 1.34), (1.30, 1.41), "lower right branch B"),
        ((1.18, 1.60), (1.47, 1.72), "far right splinter"),
        ((-1.23, 1.43), (-1.55, 1.36), "far left splinter"),
        ((0.20, 1.88), (0.12, 1.98), "tiny top center"),
        ((0.30, 1.13), (0.20, 1.04), "tiny bottom center"),
    ]
    for (start_x, start_z), (end_x, end_z), label in branch_lines:
        add_cylinder_between(
            "AR-06 curved monitor secondary glass crack " + label,
            parent,
            curved_screen_point(start_x, start_z, front_offset=0.064),
            curved_screen_point(end_x, end_z, front_offset=0.063),
            0.0045,
            mats["crack"],
            8,
        )

    center_splinters = [
        (-0.15, 0.10, "left white spider"),
        (-0.10, -0.10, "lower left white spider"),
        (0.12, 0.12, "upper right white spider"),
        (0.16, -0.08, "lower right white spider"),
        (0.00, 0.18, "vertical white spider"),
        (0.21, 0.02, "right white spider"),
        (-0.23, -0.02, "left horizontal white spider"),
        (0.04, -0.20, "bottom white spider"),
    ]
    for dx, dz, label in center_splinters:
        add_cylinder_between(
            "AR-06 dense white impact splinter " + label,
            parent,
            curved_screen_point(center_x - dx * 0.18, center_z - dz * 0.18, front_offset=0.076),
            curved_screen_point(center_x + dx, center_z + dz, front_offset=0.075),
            0.006,
            mats["crack_bright"],
            8,
        )

    shard_specs = [
        ((0.14, 1.51), (0.31, 1.58), (0.22, 1.69), "raised upper right shard"),
        ((0.13, 1.48), (-0.05, 1.58), (-0.14, 1.47), "raised upper left shard"),
        ((0.15, 1.47), (0.26, 1.34), (0.08, 1.29), "raised lower shard"),
        ((0.19, 1.54), (0.46, 1.49), (0.35, 1.39), "right triangular shard"),
        ((0.04, 1.55), (-0.28, 1.62), (-0.18, 1.73), "left triangular shard"),
        ((0.00, 1.40), (-0.22, 1.30), (-0.02, 1.24), "lower left shard"),
        ((0.32, 1.63), (0.50, 1.77), (0.43, 1.58), "small upper right chip"),
        ((-0.28, 1.36), (-0.46, 1.26), (-0.34, 1.48), "small left chip"),
        ((0.42, 1.28), (0.62, 1.20), (0.56, 1.38), "small lower right chip"),
    ]
    for p1, p2, p3, label in shard_specs:
        points = tuple(curved_screen_point(x, z, front_offset=0.090) for x, z in (p1, p2, p3))
        add_triangle("AR-06 raised broken curved screen glass chip " + label, parent, points, mats["shard"])


def add_curved_large_broken_monitor(parent: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_curved_surface("AR-06 large curved monitor inactive black glass", parent, 3.12, 1.08, 1.96, mats["dead_screen"], front_offset=0.030)
    add_curved_surface("AR-06 large curved monitor subtle dark reflection band", parent, 2.82, 1.28, 1.85, mats["dead_reflection"], front_offset=0.048)
    add_curved_frame(parent, mats)
    add_dense_cracks_on_curved_monitor(parent, mats)


def add_smoke(parent: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    smoke_specs = [
        ((-0.42, -0.42, 0.92), (0.18, 0.11, 0.23), "broken handle low puff"),
        ((-0.62, -0.36, 1.16), (0.24, 0.14, 0.30), "broken handle rising puff"),
        ((-0.30, -0.20, 1.42), (0.20, 0.13, 0.26), "upper gray puff"),
        ((0.22, 0.48, 1.62), (0.16, 0.08, 0.20), "cracked monitor dark puff"),
        ((0.46, 0.42, 1.72), (0.18, 0.09, 0.24), "right screen thin puff"),
    ]
    for loc, scale, label in smoke_specs:
        add_uv_sphere("AR-12 translucent smoke volume " + label, parent, loc, scale, mats["smoke"], 32, 16)

    wisps = [
        ((-0.48, -0.42, 0.78), (-0.76, -0.22, 1.30), "left broken handle smoke wisp"),
        ((-0.14, -0.36, 0.82), (-0.30, -0.10, 1.48), "center console smoke wisp"),
        ((0.18, 0.52, 1.48), (0.48, 0.42, 1.90), "monitor crack smoke wisp"),
    ]
    for start, end, label in wisps:
        add_cylinder_between("AR-12 soft smoke streak " + label, parent, start, end, 0.020, mats["smoke_line"], 12)


def build_damage_sample(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("AR-12 armory damaged turret console sample")

    add_box("AR-12 scorched deck inspection base", root, (0, 0, -0.04), (3.4, 2.3, 0.08), mats["floor"], bevel_width=0.012)
    add_box("AR-12 black blast stain under console and screen", root, (-0.08, -0.16, 0.012), (2.35, 1.36, 0.018), mats["scorch"], bevel_width=0.020)

    add_curved_large_broken_monitor(root, mats)
    add_box("AR-06 central monitor support mast", root, (0.0, 0.55, 0.64), (0.12, 0.16, 1.05), mats["charred_edge"], bevel_width=0.016)
    add_box("AR-06 curved monitor lower mounting rail", root, (0.0, 0.48, 1.00), (2.56, 0.08, 0.07), mats["frame"], bevel_width=0.012)

    console = add_empty("AR-12 damaged console body upright")
    console.parent = root
    console.location = (0.0, -0.42, 0.30)

    add_box("AR-12 upright turret console body not tilted", console, (0.0, 0.0, 0.24), (1.58, 0.88, 0.48), mats["console"], bevel_width=0.035)
    add_box("AR-12 torn left console side exposing dark interior", console, (-0.82, -0.02, 0.25), (0.08, 0.70, 0.38), mats["dark_interior"], bevel_width=0.010)
    add_box("AR-12 bent front armor lip below handle", console, (0.0, -0.48, 0.48), (1.34, 0.10, 0.11), mats["charred_edge"], (math.radians(-8), 0, 0), 0.012)
    add_box("AR-12 AR-05 handle fixed pivot socket", console, (0.0, -0.46, 0.58), (0.68, 0.12, 0.10), mats["frame"], bevel_width=0.018)

    handle = add_empty("AR-12 AR-05 handle only bent left 45 degrees", console)
    handle.location = (0.0, -0.49, 0.62)
    handle.rotation_euler = (0.0, math.radians(-45.0), 0.0)
    add_cylinder_between("AR-12 AR-05 bent left 45 lower U grip", handle, (-0.34, 0.0, 0.02), (0.34, 0.0, 0.02), 0.035, mats["handle"], 14)
    add_cylinder_between("AR-12 AR-05 bent left 45 left vertical grip", handle, (-0.34, 0.0, 0.02), (-0.34, 0.0, 0.34), 0.042, mats["handle"], 14)
    add_cylinder_between("AR-12 AR-05 bent left 45 right vertical grip", handle, (0.34, 0.0, 0.02), (0.34, 0.0, 0.28), 0.042, mats["handle"], 14)
    add_box("AR-12 AR-05 cracked thumb switch on bent left grip", handle, (-0.34, 0.0, 0.38), (0.12, 0.08, 0.04), mats["broken_switch"], bevel_width=0.010)
    add_box("AR-12 AR-05 missing right thumb switch scar", handle, (0.34, 0.0, 0.32), (0.10, 0.07, 0.025), mats["dark_interior"], bevel_width=0.006)

    add_cylinder_between("AR-12 loose cable from broken console", console, (-0.60, -0.34, 0.30), (-0.92, -0.56, 0.06), 0.018, mats["cable"], 10)
    add_cylinder_between("AR-12 second loose cable from broken console", console, (-0.36, -0.38, 0.34), (-0.12, -0.68, 0.08), 0.014, mats["cable"], 10)

    add_smoke(root, mats)
    add_text_label(
        "AR-12 sample state label",
        root,
        "AR-12 DAMAGED / OFFLINE",
        (0.0, -1.12, 0.05),
        (math.radians(90), 0, 0),
        mats["label"],
        0.17,
    )


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 56
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("ArmoryDamageWorld")
    scene.world.color = (0.012, 0.012, 0.014)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -2.3, 4.2))
    key = bpy.context.object
    key.name = "armory damage overhead inspection light"
    key.data.energy = 460
    key.data.size = 4.2

    bpy.ops.object.light_add(type="AREA", location=(-3.0, -1.5, 2.6))
    warm = bpy.context.object
    warm.name = "warm low blast fill"
    warm.data.energy = 125
    warm.data.size = 2.6
    warm.data.color = (1.0, 0.58, 0.32)

    bpy.ops.object.light_add(type="POINT", location=(0.2, -0.4, 1.2))
    ember = bpy.context.object
    ember.name = "faint ember glow from broken console"
    ember.data.energy = 55
    ember.data.color = (1.0, 0.18, 0.06)


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
    camera.name = "armory damage camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "armory_damage_state.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "armory_damage_state.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "armory_damage_state.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "AR-12",
        "title": "포탑 조종대 파손/폭파 상태",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "referenceImage": "C:/Users/gus68/OneDrive/바탕 화면/111.jpg",
        "sourceBasis": [
            "docs/ARMORY_OBJECTS.md - AR-12 포탑 조종대 파손/폭파 상태.",
            "docs/GAME_DESIGN_SOURCE.txt:135 - 무기실 내구도 0% 도달 시 포탑 조종대 폭파와 근처 유저 체력 피해.",
            "사용자 지정 정정 - 조종대 본체가 아니라 AR-05 핸들 부분만 왼쪽으로 45도 꺾인 상태.",
            "사용자 지정 정정 - 파손 화면은 조종대 소형 모니터가 아니라 AR-06 커브형 대형 모니터.",
            "사용자 제공 이미지 - 꺼진 커브형 대형 모니터 표면의 중심 충격점, 방사형 균열, 잔금, 흰 유리 파편 패턴.",
        ],
        "generatedFiles": [
            "blender/armory_damage_state.blend",
            "exports/armory_damage_state.fbx",
            "exports/armory_damage_state.glb",
            "renders/01_overview.png",
            "renders/02_monitor_crack_detail.png",
            "renders/03_left_tilt_profile.png",
            "renders/04_smoke_effect.png",
            "index.html",
            "README.md",
            "ASSET_MANIFEST.json",
            "APPROVAL_STATUS.json",
        ],
        "includedParts": [
            "기울어지지 않은 고정 포탑 조종대 본체",
            "왼쪽으로 45도 꺾인 AR-05 U자형 포탑 핸들",
            "꺼진 검은 AR-06 커브형 대형 모니터",
            "참고 이미지 기반 중심 충격점과 커브 표면을 따라 뻗는 방사형 액정 균열",
            "짧은 잔금, 중심부 흰 유리 파편, 삼각 유리 조각",
            "조종대와 대형 모니터 주변 반투명 연기와 그을림",
            "느슨한 케이블",
        ],
        "excludedParts": [
            "Unity 씬 배치",
            "실제 폭발/연기 파티클 시스템",
            "체력 피해 로직",
            "포탑 수동 모드 UI",
            "외부 선체 포탑 모델",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "AR-12",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산, UI 흐름에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# armory_damage_state

AR-12 포탑 조종대 파손/폭파 상태 승인용 Blender 샘플입니다.

## 목적

무기실 내구도 0% 상태에서 보일 포탑 조종대 손상 외형을 Unity에 반영하기 전에 검사하기 위한 샘플입니다.

## 반영 기준

- 조종대 본체는 기울어지지 않은 정상 배치 상태입니다.
- AR-05 U자형 포탑 핸들 부분만 왼쪽으로 45도 꺾인 파손 상태입니다.
- 파손된 화면은 조종대 소형 화면이 아니라 AR-06 커브형 대형 모니터입니다.
- AR-06 커브형 대형 모니터는 꺼진 검은 화면이며 파손되어 있습니다.
- 액정 깨짐은 사용자 제공 이미지처럼 중심 충격점에서 긴 방사형 균열이 뻗고, 주변에 짧은 잔금과 흰 유리 파편이 몰린 형태로 만들었습니다.
- 조종대 주변에는 반투명 연기 볼륨, 얇은 연기 줄기, 바닥 그을림을 넣었습니다.
- 실제 Unity 파티클, 폭발 로직, 피해 로직은 포함하지 않습니다.

## 포함

- `blender/armory_damage_state.blend`
- `exports/armory_damage_state.fbx`
- `exports/armory_damage_state.glb`
- `renders/*.png` 4개 구도
- `index.html`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- Unity 씬 배치
- 실제 폭발/연기 파티클 시스템
- 체력 피해 로직
- 포탑 수동 모드 UI
- 외부 선체 포탑 모델
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_overview.png", "01 AR-12 파손 상태 전체"),
        ("02_monitor_crack_detail.png", "02 AR-06 커브형 대형 모니터 액정 균열 상세"),
        ("03_left_tilt_profile.png", "03 AR-05 핸들만 왼쪽 45도 꺾인 상태"),
        ("04_smoke_effect.png", "04 연기와 그을림 효과"),
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
  <title>armory_damage_state review</title>
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
  <h1>armory_damage_state</h1>
  <p>AR-12 포탑 조종대 파손/폭파 상태 승인용 샘플입니다. 조종대 본체는 정상 배치이고 AR-05 U자형 핸들만 왼쪽으로 45도 꺾여 있습니다. 파손 화면은 조종대 소형 화면이 아니라 AR-06 커브형 대형 모니터이며, 꺼진 검은 화면 위에 중심 충격점, 방사형 균열, 짧은 잔금, 흰 유리 파편을 배치했습니다. Unity 씬 배치와 실제 파티클/피해 로직은 포함하지 않았습니다.</p>
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
        "floor": noisy_metal("AR-12 worn armory floor", (0.13, 0.14, 0.14, 1)),
        "scorch": material("AR-12 black soot stain", (0.015, 0.012, 0.010, 1), roughness=0.95),
        "console": noisy_metal("AR-12 scorched turret console metal", (0.14, 0.15, 0.15, 1)),
        "frame": noisy_metal("AR-12 broken monitor black frame", (0.025, 0.026, 0.028, 1)),
        "dead_screen": material("AR-12 inactive dead black glass", (0.006, 0.008, 0.010, 1), roughness=0.22, metallic=0.05),
        "dead_reflection": material("AR-06 dim cold reflection on dead curved glass", (0.030, 0.042, 0.052, 0.42), roughness=0.18, metallic=0.02, alpha=0.42),
        "dim_strip": material("AR-12 dead unlit status icons", (0.05, 0.055, 0.06, 1), roughness=0.65),
        "crack": material("AR-12 bright white cracked glass lines", (0.90, 0.92, 0.88, 1), roughness=0.38, emission=(0.35, 0.36, 0.33, 1), emission_strength=0.08),
        "crack_bright": material("AR-06 dense bright white impact splinters", (0.98, 1.00, 0.96, 1), roughness=0.30, emission=(0.68, 0.70, 0.64, 1), emission_strength=0.18),
        "crushed_glass": material("AR-12 crushed white glass center", (0.86, 0.88, 0.84, 1), roughness=0.42, alpha=0.78),
        "shard": material("AR-12 raised translucent glass shard", (0.70, 0.78, 0.80, 0.54), roughness=0.24, alpha=0.54),
        "pit": material("AR-12 black impact pit", (0.002, 0.002, 0.002, 1), roughness=0.80),
        "dark_interior": material("AR-12 exposed black interior", (0.01, 0.008, 0.006, 1), roughness=0.92),
        "charred_edge": noisy_metal("AR-12 charred bent armor edge", (0.06, 0.05, 0.045, 1)),
        "handle": noisy_metal("AR-12 damaged U handle metal", (0.35, 0.34, 0.30, 1)),
        "broken_switch": material("AR-12 cracked red thumb switch", (0.45, 0.035, 0.025, 1), roughness=0.65),
        "cable": material("AR-12 loose black cable rubber", (0.006, 0.006, 0.006, 1), roughness=0.70),
        "smoke": material("AR-12 translucent gray smoke", (0.35, 0.36, 0.35, 0.26), roughness=0.95, alpha=0.26),
        "smoke_line": material("AR-12 thin dark smoke wisp", (0.18, 0.18, 0.17, 0.32), roughness=0.95, alpha=0.32),
        "label": material("AR-12 pale floor label", (0.80, 0.86, 0.82, 1), roughness=0.72, emission=(0.12, 0.20, 0.18, 1), emission_strength=0.05),
    }

    build_damage_sample(mats)
    add_render_lights()

    cameras = [
        ("overview", (3.6, -3.9, 2.55), (0.0, 0.02, 1.05), 43, "01_overview.png", None),
        ("monitor_crack_detail", (0.35, -2.25, 1.72), (0.12, 0.64, 1.52), 78, "02_monitor_crack_detail.png", None),
        ("left_tilt_profile", (2.30, -1.55, 1.18), (-0.08, -0.52, 0.78), 64, "03_left_tilt_profile.png", None),
        ("smoke_effect", (-2.7, -2.8, 2.45), (-0.18, 0.00, 1.28), 48, "04_smoke_effect.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} Blender sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
