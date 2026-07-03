from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "ck_warn04"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
TEXTURE_DIR = SAMPLE_ROOT / "textures"

SMP_MODEL_DIR = PROJECT_ROOT / "Assets" / "Sci-Fi Styled Modular Pack" / "Models"
HSK_LIGHT_FBX = PROJECT_ROOT / "Assets" / "Heavy Station Kit" / "BASE" / "Meshes" / "Walls" / "Wall Lights" / "Walls Light.fbx"
WARNING_TEX = PROJECT_ROOT / "Assets" / "Sci-Fi Styled Modular Pack" / "Textures" / "projector_warning.png"


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
    roughness: float = 0.75,
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
    mat = material(name, base, metallic=0.35, roughness=0.88)
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
    ramp.color_ramp.elements[0].position = 0.22
    ramp.color_ramp.elements[0].color = (base[0] * 0.55, base[1] * 0.55, base[2] * 0.55, 1)
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


def textured_warning_material(name: str, fallback: bpy.types.Material) -> bpy.types.Material:
    mat = material(
        name,
        (0.9, 0.22, 0.08, 1),
        roughness=0.5,
        emission=(0.9, 0.16, 0.05, 1),
        emission_strength=0.45,
    )
    if not WARNING_TEX.exists():
        return fallback

    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    image = bpy.data.images.load(str(WARNING_TEX))
    tex = nodes.new(type="ShaderNodeTexImage")
    tex.image = image
    links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    if "Emission Color" in bsdf.inputs:
        links.new(tex.outputs["Color"], bsdf.inputs["Emission Color"])
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


def add_torus(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    major_radius: float,
    minor_radius: float,
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
    major_segments: int = 36,
    minor_segments: int = 8,
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

    root = add_empty(name, parent)
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
    context = add_empty("placement context - cockpit front and console are not CK-04")
    add_box("cockpit floor footprint proxy", context, (0, 0.15, -0.06), (8.8, 5.0, 0.12), mats["floor"], bevel_width=0.01)
    add_box("front broad screen proxy CK-01", context, (0, 2.95, 1.85), (8.9, 0.08, 2.18), mats["glass"], bevel_width=0.02)
    add_box("front lower sill proxy", context, (0, 2.88, 0.64), (9.2, 0.18, 0.28), mats["frame"], bevel_width=0.02)
    add_box("front upper frame proxy", context, (0, 2.88, 3.03), (9.2, 0.18, 0.26), mats["frame"], bevel_width=0.02)
    add_box("left cockpit wall proxy", context, (-4.55, 0.25, 1.45), (0.18, 4.9, 2.9), mats["wall"], bevel_width=0.015)
    add_box("right cockpit wall proxy", context, (4.55, 0.25, 1.45), (0.18, 4.9, 2.9), mats["wall"], bevel_width=0.015)
    add_box("cockpit ceiling proxy", context, (0, 0.25, 3.36), (8.8, 4.9, 0.08), mats["ceiling"], bevel_width=0.01)
    add_box("approved low console placement ghost", context, (0, 0.42, 0.48), (4.95, 1.36, 0.55), mats["console_ghost"], bevel_width=0.04)
    add_box("console sloped deck ghost", context, (0, 0.62, 0.78), (4.72, 1.08, 0.18), mats["console_ghost"], rot=(math.radians(-14), 0, 0), bevel_width=0.035)
    return context


def add_lens_stack(
    parent: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    side: str,
    x: float,
    z: float,
) -> None:
    prefix = f"{side} side three-lens warning stack"
    add_box(prefix + " armored wall backplate", parent, (x, 2.76, z), (0.42, 0.16, 0.98), mats["body"], bevel_width=0.025)
    add_box(prefix + " black recessed face", parent, (x, 2.665, z), (0.32, 0.055, 0.82), mats["rubber"], bevel_width=0.018)
    add_box(prefix + " lower wire junction", parent, (x, 2.61, z - 0.57), (0.26, 0.08, 0.14), mats["dark"], bevel_width=0.012)

    colors = [
        ("red upper alarm lens", mats["red_lens"], z + 0.27, 1.25),
        ("amber middle caution lens", mats["amber_lens"], z, 0.95),
        ("red lower alarm lens", mats["red_dim"], z - 0.27, 0.45),
    ]
    for label, lens_mat, lens_z, energy in colors:
        add_cylinder(prefix + " " + label, parent, (x, 2.62, lens_z), 0.105, 0.035, lens_mat, rot=(math.radians(90), 0, 0), vertices=28)
        add_torus(prefix + " " + label + " steel guard ring", parent, (x, 2.595, lens_z), 0.112, 0.009, mats["worn"], rot=(math.radians(90), 0, 0))
        for y_offset in (-0.023, 0.023):
            add_cylinder_between(
                prefix + " " + label + f" cross guard {y_offset:+.2f}",
                parent,
                (x - 0.12, 2.565 + y_offset, lens_z),
                (x + 0.12, 2.565 + y_offset, lens_z),
                0.006,
                mats["worn"],
                vertices=8,
            )
        bpy.ops.object.light_add(type="POINT", location=(x, 2.50, lens_z))
        light = bpy.context.object
        light.name = prefix + " " + label + " local glow"
        light.data.energy = energy
        light.data.color = (1.0, 0.12, 0.04) if "red" in label else (1.0, 0.55, 0.12)


def add_rotary_beacon(
    parent: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    name: str,
    loc: tuple[float, float, float],
) -> None:
    x, y, z = loc
    add_cylinder(name + " squat metal pedestal", parent, (x, y, z), 0.14, 0.08, mats["body"], vertices=28)
    add_cylinder(name + " black rubber seal", parent, (x, y, z + 0.055), 0.16, 0.03, mats["rubber"], vertices=28)
    add_sphere(name + " red glass half dome", parent, (x, y, z + 0.12), 0.15, mats["red_lens"], scale=(1.0, 1.0, 0.55))
    add_torus(name + " lower cage ring", parent, (x, y, z + 0.11), 0.16, 0.008, mats["worn"], rot=(0, 0, 0), major_segments=42)
    add_torus(name + " upper cage ring", parent, (x, y, z + 0.20), 0.11, 0.007, mats["worn"], rot=(0, 0, 0), major_segments=42)
    for angle in (0, math.pi / 2, math.pi, math.pi * 1.5):
        add_cylinder_between(
            name + f" vertical cage rib {angle:.2f}",
            parent,
            (x + math.cos(angle) * 0.15, y + math.sin(angle) * 0.15, z + 0.08),
            (x + math.cos(angle) * 0.10, y + math.sin(angle) * 0.10, z + 0.22),
            0.006,
            mats["worn"],
            vertices=8,
        )
    bpy.ops.object.light_add(type="POINT", location=(x, y - 0.05, z + 0.19))
    light = bpy.context.object
    light.name = name + " red beacon glow"
    light.data.energy = 1.2
    light.data.color = (1.0, 0.08, 0.04)


def add_ceiling_rotary_beacon(
    parent: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    name: str,
    loc: tuple[float, float, float],
) -> None:
    x, y, z = loc
    add_box(name + " rectangular ceiling mount plate", parent, (x, y, z + 0.16), (0.92, 0.62, 0.085), mats["body"], bevel_width=0.02)
    for index, stripe_x in enumerate((-0.33, -0.18, 0.18, 0.33), start=1):
        add_box(
            name + f" yellow hazard stripe on mount {index}",
            parent,
            (x + stripe_x, y - 0.316, z + 0.18),
            (0.10, 0.014, 0.055),
            mats["hazard_yellow"],
            rot=(0.0, 0.0, math.radians(-18)),
            bevel_width=0.002,
        )
    add_cylinder(name + " circular swivel base", parent, (x, y, z + 0.09), 0.24, 0.07, mats["body"], vertices=32)
    add_cylinder(name + " black ceiling rubber seal", parent, (x, y, z + 0.035), 0.26, 0.035, mats["rubber"], vertices=32)
    add_sphere(name + " hanging red glass dome", parent, (x, y, z - 0.075), 0.24, mats["red_lens"], scale=(1.0, 1.0, 0.52), segments=32, ring_count=14)
    add_cylinder(name + " inner lamp spindle for future animation", parent, (x, y, z - 0.055), 0.035, 0.46, mats["worn"], rot=(0.0, math.radians(90), math.radians(22)), vertices=14)
    add_torus(name + " lower protective cage ring", parent, (x, y, z - 0.055), 0.265, 0.009, mats["worn"], major_segments=48, minor_segments=8)
    add_torus(name + " upper protective cage ring", parent, (x, y, z + 0.045), 0.245, 0.008, mats["worn"], major_segments=48, minor_segments=8)
    for angle in (math.radians(25), math.radians(115), math.radians(205), math.radians(295)):
        add_cylinder_between(
            name + f" cage rib {angle:.2f}",
            parent,
            (x + math.cos(angle) * 0.25, y + math.sin(angle) * 0.25, z - 0.09),
            (x + math.cos(angle) * 0.22, y + math.sin(angle) * 0.22, z + 0.06),
            0.007,
            mats["worn"],
            vertices=8,
        )

    add_cylinder(name + " front alarm sounder grille plate", parent, (x, y - 0.342, z + 0.045), 0.145, 0.035, mats["dark"], rot=(math.radians(90), 0.0, 0.0), vertices=32)
    for index, slot_z in enumerate((-0.055, -0.028, 0.0, 0.028, 0.055), start=1):
        add_box(
            name + f" alarm sounder grille slot {index}",
            parent,
            (x, y - 0.365, z + 0.045 + slot_z),
            (0.20 - abs(slot_z) * 1.2, 0.014, 0.008),
            mats["worn"],
            bevel_width=0.002,
        )
    add_box(name + " small warning decal under mount", parent, (x, y - 0.334, z + 0.245), (0.22, 0.012, 0.10), mats["warning_decal"], bevel_width=0.002)
    add_cylinder_between(name + " ceiling power cable", parent, (x + 0.30, y + 0.04, z + 0.13), (x + 0.92, y + 0.04, z + 0.13), 0.014, mats["rubber"], vertices=12)

    bpy.ops.object.light_add(type="POINT", location=(x, y - 0.05, z - 0.04))
    light = bpy.context.object
    light.name = name + " emergency red glow"
    light.data.energy = 4.6
    light.data.color = (1.0, 0.05, 0.02)


def build_warning_set(mats: dict[str, bpy.types.Material]) -> dict[str, str]:
    root = add_empty("CK-04 warning lights sample - approval target")
    used_assets: dict[str, str] = {}

    add_box("upper frame narrow red alarm bar housing", root, (0, 2.67, 3.22), (2.7, 0.13, 0.22), mats["body"], bevel_width=0.025)
    add_box("upper frame red alarm bar glass", root, (0, 2.58, 3.22), (2.42, 0.035, 0.095), mats["red_lens"], bevel_width=0.018)
    for x in (-1.05, -0.35, 0.35, 1.05):
        add_box("upper alarm bar internal dark baffle", root, (x, 2.55, 3.22), (0.045, 0.025, 0.12), mats["dark"], bevel_width=0.004)
    for index, x in enumerate((-1.50, 1.50), start=1):
        add_box("upper alarm bar hazard tab " + str(index), root, (x, 2.535, 3.08), (0.24, 0.018, 0.12), mats["hazard_yellow"], rot=(0.0, 0.0, math.radians(-18)), bevel_width=0.004)
    bpy.ops.object.light_add(type="AREA", location=(0, 2.35, 3.22))
    bar_light = bpy.context.object
    bar_light.name = "upper frame red alarm bar soft glow"
    bar_light.data.energy = 12
    bar_light.data.size = 2.4
    bar_light.data.color = (1.0, 0.09, 0.035)

    add_ceiling_rotary_beacon(root, mats, "central ceiling emergency rotary beacon", (0.0, -0.48, 3.12))

    if WARNING_TEX.exists():
        used_assets["Sci-Fi Styled Modular Pack projector_warning texture"] = str(WARNING_TEX.relative_to(PROJECT_ROOT)).replace("\\", "/")

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


def set_context_render_visible(visible: bool) -> None:
    context = bpy.data.objects.get("placement context - cockpit front and console are not CK-04")
    if context is None:
        return

    context.hide_render = not visible
    for child in context.children_recursive:
        child.hide_render = not visible


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
    world = bpy.data.worlds.new("ck_warn04_world")
    world.color = (0.012, 0.014, 0.015)
    scene.world = world
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = -0.05
    scene.view_settings.gamma = 1.0


def add_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -3.6, 5.4))
    key = bpy.context.object
    key.name = "large cockpit warning sample key light"
    key.data.energy = 430
    key.data.size = 6.2

    bpy.ops.object.light_add(type="POINT", location=(0.0, -0.65, 3.05))
    ceiling = bpy.context.object
    ceiling.name = "ceiling alarm red standby glow"
    ceiling.data.energy = 28
    ceiling.data.color = (1.0, 0.08, 0.03)

    bpy.ops.object.light_add(type="POINT", location=(0.0, 2.28, 3.2))
    bar = bpy.context.object
    bar.name = "front alarm bar red standby glow"
    bar.data.energy = 20
    bar.data.color = (1.0, 0.08, 0.03)


def export_assets() -> None:
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / f"{SAMPLE_NAME}.blend"))
    set_context_render_visible(True)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / f"{SAMPLE_NAME}.glb"), export_format="GLB")
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / f"{SAMPLE_NAME}.fbx"), use_selection=False)


def copy_preview_textures() -> None:
    if WARNING_TEX.exists():
        shutil.copy2(WARNING_TEX, TEXTURE_DIR / WARNING_TEX.name)


def write_docs(used_assets: dict[str, str]) -> None:
    asset_manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "CK-04",
        "scope": "조종실 경고등 및 작은 물리 표시등 승인용 샘플",
        "sourceBasis": [
            "docs/COCKPIT_OBJECTS.md: CK-04 경고등 / 작은 물리 표시등",
            "docs/GAME_DESIGN_SOURCE.txt:57 조종실 내구도 단계별 위험 상태",
            "docs/GAME_DESIGN_SOURCE.txt:120 수동 운행 중 침입을 소리로 감지 가능",
            "docs/GAME_DESIGN_SOURCE.txt:121 조종실 내구도 0% 시 조종대 폭파",
            "사용자 확인: 자동/수동 상태, 진행도, 내구도 정보는 CK-01 메인 스크린에 표시하고 별도 모델링하지 않음",
        ],
        "inferenceNote": "경고등 자체는 원본의 상태/위험 표현을 보조하기 위한 시각 추론 항목입니다. 좌우 벽면 경고등과 빛줄기 모델은 사용자 피드백에 따라 제외했습니다.",
        "usedAssetCandidates": used_assets,
        "unityApplicationAllowed": False,
        "approvalState": "미승인",
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(
        json.dumps(asset_manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    approval = {
        "sample": SAMPLE_NAME,
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "included": [
            "전면 상부 프레임의 얇은 적색 경고등 바",
            "조종실 중앙 천장 비상 경고등 본체",
            "천장 장착 브래킷, 보호 가드, 사이렌 그릴",
            "경고 표식, 배선, 마모 디테일",
            "CK-01/CK-02와의 배치 확인용 프록시",
        ],
        "excluded": [
            "좌우 벽면 경고등",
            "조종대 가장자리의 작은 경고등",
            "붉은 회전광 빔 모델",
            "자동/수동 운행 상태 패널",
            "운행 진행도 표시",
            "구역 내구도 수치 표시",
            "메인 스크린 UI",
            "경고음/사이렌 오디오 로직",
            "상호작용 컴포넌트",
            "Unity 런타임 씬 배치",
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

CK-04 경고등 / 작은 물리 표시등 승인용 Blender 샘플입니다.

## 범위

- 포함: 전면 상부 얇은 적색 경고등 바, 중앙 천장 비상 경고등 본체, 천장 장착 브래킷, 보호 가드, 사이렌 그릴, 경고 표식, 배선, 마모 디테일.
- 제외: 좌우 벽면 경고등, 조종대 가장자리의 작은 경고등, 붉은 회전광 빔 모델, 자동/수동 운행 상태 패널, 운행 진행도, 구역 내구도 수치 표시, 메인 스크린 UI, 경고음 로직, 상호작용 컴포넌트.
- 회색/반투명 조종실 구조와 조종대는 배치 확인용 프록시입니다. 승인 대상은 경고등 세트입니다.

## 배치 기준

- 조종 시야를 가리지 않도록 전면 유리 중앙에는 부품을 두지 않았습니다.
- 전면 상부 프레임의 얇은 경고등 바는 유지했습니다.
- 천장 경고등은 추후 애니메이션으로 붉은 빛을 내뿜는 기준이 되도록 본체, 가드, 사이렌 그릴만 남겼습니다.

## 사용 에셋 후보

{assets_md}

## Unity 반영 방식

승인되면 `Approved Cockpit 01 Structure` 내부의 전면 프레임과 `Approved Cockpit 02 Console` 주변에 시각 모델만 배치합니다.
콜라이더, 경고음, 자동/수동 상태 로직, 메인 스크린 UI는 이번 샘플 범위에 포함하지 않습니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_front.png", "정면: 스크린 위 경고등 바와 천장 경고등"),
        ("02_player.png", "플레이어 시점: 천장 경고등 거리감"),
        ("03_side.png", "측면: 천장 장착 깊이와 브래킷"),
        ("04_top.png", "상단: CK-01/CK-02 대비 설치 위치"),
        ("05_detail.png", "상세: 스크린 위 경고등 바와 경고 표식"),
        ("06_ceiling.png", "상세: 천장 경고등 본체와 사이렌 그릴"),
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
  <p>CK-04 경고등 / 작은 물리 표시등 승인용 Blender 샘플입니다. 아직 Unity 런타임 씬에는 적용하지 않았습니다.</p>
  <p>좌우 벽면 경고등과 붉은 회전광 빔 모델은 제외했습니다. 이번 샘플은 스크린 위 경고등 바와 천장 비상 경고등 본체만 다룹니다.</p>
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
        "wall": material("context muted cockpit wall", (0.24, 0.31, 0.32, 0.42), roughness=0.78, alpha=0.42),
        "floor": material("context dark floor", (0.10, 0.12, 0.11, 1), roughness=0.86),
        "glass": material("context front screen glass", (0.08, 0.42, 0.36, 0.28), roughness=0.18, alpha=0.28, emission=(0.04, 0.24, 0.20, 1), emission_strength=0.18),
        "frame": material("context window frame", (0.08, 0.095, 0.09, 0.55), roughness=0.82, alpha=0.55),
        "ceiling": material("context dark cockpit ceiling", (0.075, 0.085, 0.08, 0.46), roughness=0.86, alpha=0.46),
        "console_ghost": material("approved console ghost", (0.11, 0.14, 0.13, 0.35), roughness=0.86, alpha=0.35),
        "body": worn_metal_material("worn warning light dark body", (0.07, 0.082, 0.078, 1)),
        "dark": material("matte black inset metal", (0.012, 0.013, 0.012, 1), roughness=0.94),
        "rubber": material("aged black rubber cable", (0.006, 0.006, 0.005, 1), roughness=0.96),
        "worn": material("exposed scraped steel", (0.66, 0.64, 0.57, 1), metallic=0.35, roughness=0.62),
        "hazard_yellow": material("aged hazard yellow paint", (0.92, 0.66, 0.11, 1), roughness=0.76),
        "red_lens": material("hot red warning lens", (1.0, 0.08, 0.035, 0.82), roughness=0.28, alpha=0.82, emission=(1.0, 0.06, 0.02, 1), emission_strength=1.35),
        "red_dim": material("dim red warning lens", (0.52, 0.02, 0.018, 0.78), roughness=0.42, alpha=0.78, emission=(0.55, 0.015, 0.01, 1), emission_strength=0.42),
        "amber_lens": material("amber caution lens", (1.0, 0.55, 0.10, 0.84), roughness=0.32, alpha=0.84, emission=(1.0, 0.45, 0.06, 1), emission_strength=0.95),
    }
    mats["warning_decal"] = textured_warning_material("small projected warning decal", mats["red_lens"])

    build_context(mats)
    used_assets = build_warning_set(mats)
    add_lights()

    cameras = [
        ("front", (0.0, -5.9, 2.36), (0.0, 2.18, 1.95), 35, "01_front.png", None, True),
        ("player", (0.0, -3.05, 1.58), (0.0, 1.22, 2.54), 30, "02_player.png", None, True),
        ("side", (4.8, -1.35, 3.0), (0.0, -0.48, 3.12), 42, "03_side.png", None, True),
        ("top", (0.0, 0.25, 7.8), (0.0, 0.95, 0.0), 50, "04_top.png", 6.6, True),
        ("detail", (-1.7, 0.15, 3.30), (0.0, 2.57, 3.19), 58, "05_detail.png", None, True),
        ("ceiling", (-1.35, -2.75, 3.10), (0.0, -0.48, 3.08), 54, "06_ceiling.png", None, True),
    ]
    for name, loc, target, lens, output, ortho_scale, show_context in cameras:
        render_camera(add_camera("cam_" + name, loc, target, lens, ortho_scale), output, show_context=show_context)

    set_context_render_visible(True)
    copy_preview_textures()
    export_assets()
    write_docs(used_assets)
    print(SAMPLE_NAME + " sample generated: " + str(SAMPLE_ROOT))


if __name__ == "__main__":
    main()
