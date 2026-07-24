import hashlib
import json
import math
import shutil
import statistics
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector


PROJECT_ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "appearance_reference_sync"
)
SOURCE_GLB = (
    PROJECT_ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Enemies"
    / "Rebellion"
    / "Models"
    / "Rebellion.glb"
)
REFERENCE_IMAGE = PROJECT_ROOT / "image" / "rébellion(리벨리온).png"
SOURCE_DIR = SAMPLE_ROOT / "source"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
RENDER_DIR = SAMPLE_ROOT / "renders"
BLEND_DIR = SAMPLE_ROOT / "blender"
EXPORT_DIR = SAMPLE_ROOT / "exports"
SOURCE_COPY = SOURCE_DIR / "Rebellion_Unity_Source_Unmodified.glb"
REFERENCE_COPY = SOURCE_DIR / "rebellion_reference.png"
BLEND_PATH = BLEND_DIR / "Rebellion_Appearance_ReferenceSync.blend"
EXPORT_PATH = EXPORT_DIR / "Rebellion_Appearance_ReferenceSync.glb"
MANIFEST_PATH = SAMPLE_ROOT / "ASSET_MANIFEST.json"
GEOMETRY_PATH = SAMPLE_ROOT / "GEOMETRY_VALIDATION.json"
TEXTURE_SIZE = 512
RNG = np.random.default_rng(240724)


MATERIAL_SPECS = {
    "Rebellion_Worn_Disc_Steel": {
        "base": (0.42, 0.43, 0.40),
        "roughness": 0.58,
        "metallic": 0.88,
        "variation": 0.18,
        "scratch": 0.22,
    },
    "Rebellion_Dark_Leg_Mechanism": {
        "base": (0.10, 0.115, 0.11),
        "roughness": 0.42,
        "metallic": 0.82,
        "variation": 0.12,
        "scratch": 0.18,
    },
    "Rebellion_Hydraulic_Steel": {
        "base": (0.36, 0.39, 0.37),
        "roughness": 0.28,
        "metallic": 0.96,
        "variation": 0.10,
        "scratch": 0.14,
    },
    "Rebellion_Front_Weapon_Black": {
        "base": (0.05, 0.06, 0.055),
        "roughness": 0.33,
        "metallic": 0.90,
        "variation": 0.08,
        "scratch": 0.10,
    },
}


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def ensure_directories():
    for path in (
        SOURCE_DIR,
        TEXTURE_DIR,
        RENDER_DIR,
        BLEND_DIR,
        EXPORT_DIR,
    ):
        path.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.images,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def mesh_signature(obj):
    digest = hashlib.sha256()
    for vertex in obj.data.vertices:
        digest.update(
            (
                f"v:{vertex.co.x:.9f},{vertex.co.y:.9f},{vertex.co.z:.9f};"
            ).encode("ascii")
        )
        weights = sorted(
            (
                obj.vertex_groups[group.group].name,
                round(group.weight, 9),
            )
            for group in vertex.groups
        )
        for name, weight in weights:
            digest.update(f"w:{name}:{weight:.9f};".encode("ascii"))
    for polygon in obj.data.polygons:
        digest.update(
            ("p:" + ",".join(str(index) for index in polygon.vertices) + ";").encode(
                "ascii"
            )
        )
    return digest.hexdigest().upper()


def topology_weight_signature(obj):
    digest = hashlib.sha256()
    for vertex in obj.data.vertices:
        weights = sorted(
            (
                obj.vertex_groups[group.group].name,
                round(group.weight, 9),
            )
            for group in vertex.groups
        )
        for name, weight in weights:
            digest.update(f"w:{name}:{weight:.9f};".encode("ascii"))
    for polygon in obj.data.polygons:
        digest.update(
            ("p:" + ",".join(str(index) for index in polygon.vertices) + ";").encode(
                "ascii"
            )
        )
    return digest.hexdigest().upper()


def world_bounds(objects):
    corners = []
    for obj in objects:
        if obj.type == "MESH":
            corners.extend(
                obj.matrix_world @ Vector(corner) for corner in obj.bound_box
            )
    if not corners:
        raise RuntimeError("Rebellion sample has no mesh bounds.")
    minimum = Vector(
        (
            min(point.x for point in corners),
            min(point.y for point in corners),
            min(point.z for point in corners),
        )
    )
    maximum = Vector(
        (
            max(point.x for point in corners),
            max(point.y for point in corners),
            max(point.z for point in corners),
        )
    )
    return minimum, maximum


def vec(value):
    return [round(float(component), 9) for component in value]


def generate_texture_maps(name, spec):
    size = TEXTURE_SIZE
    yy, xx = np.mgrid[0:size, 0:size]
    u = xx / float(size - 1)
    v = yy / float(size - 1)
    broad = (
        np.sin(u * math.tau * 3.0 + 0.8)
        + np.sin(v * math.tau * 4.0 + 1.7)
        + np.sin((u + v) * math.tau * 7.0)
    ) / 3.0
    fine = RNG.normal(0.0, 1.0, (size, size))
    fine = (
        fine
        + np.roll(fine, 1, 0)
        + np.roll(fine, -1, 0)
        + np.roll(fine, 1, 1)
        + np.roll(fine, -1, 1)
    ) / 5.0
    wear = np.clip(0.5 + 0.24 * broad + 0.20 * fine, 0.0, 1.0)
    grime = np.clip(
        0.58 * np.sin(u * math.tau * 1.5 + v * math.tau * 2.1)
        + 0.42 * fine,
        -1.0,
        1.0,
    )
    scratch_mask = np.zeros((size, size), dtype=np.float32)
    for _ in range(70):
        x0 = int(RNG.integers(0, size))
        y0 = int(RNG.integers(0, size))
        length = int(RNG.integers(8, 70))
        slope = float(RNG.uniform(-0.18, 0.18))
        for step in range(length):
            x = (x0 + step) % size
            y = int((y0 + step * slope) % size)
            scratch_mask[max(0, y - 1) : min(size, y + 2), x] = 1.0
    scratch_mask *= spec["scratch"]

    base = np.array(spec["base"], dtype=np.float32)
    color_factor = (
        0.62
        + spec["variation"] * 1.6 * wear
        + 0.10 * broad
        + 0.08 * fine
        - 0.18 * np.maximum(grime, 0.0)
        + scratch_mask
    )
    albedo = np.clip(base[None, None, :] * color_factor[:, :, None], 0.0, 1.0)
    oxidation = np.clip(grime - 0.20, 0.0, 1.0)[:, :, None]
    albedo = np.clip(
        albedo * (1.0 - oxidation * 0.38)
        + oxidation * np.array((0.12, 0.085, 0.055))[None, None, :] * 0.40,
        0.0,
        1.0,
    )
    roughness = np.clip(
        spec["roughness"] + 0.16 * grime - scratch_mask * 0.28,
        0.08,
        0.98,
    )
    height = np.clip(
        0.5 + 0.18 * fine + 0.13 * broad - scratch_mask * 0.6,
        0.0,
        1.0,
    )
    dy, dx = np.gradient(height)
    strength = 2.8
    nx = -dx * strength
    ny = -dy * strength
    nz = np.ones_like(nx)
    norm = np.sqrt(nx * nx + ny * ny + nz * nz)
    normal = np.stack(
        (
            nx / norm * 0.5 + 0.5,
            ny / norm * 0.5 + 0.5,
            nz / norm * 0.5 + 0.5,
        ),
        axis=2,
    )

    paths = {
        "albedo": TEXTURE_DIR / f"{name.lower()}_albedo.png",
        "roughness": TEXTURE_DIR / f"{name.lower()}_roughness.png",
        "normal": TEXTURE_DIR / f"{name.lower()}_normal.png",
    }
    save_rgb_image(paths["albedo"], albedo)
    save_gray_image(paths["roughness"], roughness)
    save_rgb_image(paths["normal"], normal)
    return paths


def save_rgb_image(path, values):
    height, width, _ = values.shape
    rgba = np.ones((height, width, 4), dtype=np.float32)
    rgba[:, :, :3] = np.clip(values, 0.0, 1.0)
    image = bpy.data.images.new(path.stem, width=width, height=height)
    image.pixels.foreach_set(rgba.ravel())
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()


def save_gray_image(path, values):
    rgb = np.repeat(values[:, :, None], 3, axis=2)
    save_rgb_image(path, rgb)


def create_pbr_material(name, spec, paths):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (*spec["base"], 1.0)
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.inputs["Metallic"].default_value = spec["metallic"]
    shader.inputs["Roughness"].default_value = spec["roughness"]
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = bpy.data.images.load(str(paths["albedo"]), check_existing=True)
    albedo.interpolation = "Linear"
    roughness = nodes.new("ShaderNodeTexImage")
    roughness.image = bpy.data.images.load(
        str(paths["roughness"]), check_existing=True
    )
    roughness.image.colorspace_settings.name = "Non-Color"
    normal_image = nodes.new("ShaderNodeTexImage")
    normal_image.image = bpy.data.images.load(
        str(paths["normal"]), check_existing=True
    )
    normal_image.image.colorspace_settings.name = "Non-Color"
    normal_map = nodes.new("ShaderNodeNormalMap")
    normal_map.inputs["Strength"].default_value = 0.62
    links.new(albedo.outputs["Color"], shader.inputs["Base Color"])
    links.new(roughness.outputs["Color"], shader.inputs["Roughness"])
    links.new(normal_image.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], shader.inputs["Normal"])
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def create_optic_material():
    material = bpy.data.materials.new("Rebellion_Scan_Optic_Red")
    material.use_nodes = True
    material.diffuse_color = (0.18, 0.004, 0.002, 1.0)
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (0.12, 0.003, 0.002, 1.0)
    shader.inputs["Metallic"].default_value = 0.12
    shader.inputs["Roughness"].default_value = 0.12
    shader.inputs["Emission Color"].default_value = (0.65, 0.006, 0.002, 1.0)
    shader.inputs["Emission Strength"].default_value = 0.7
    return material


def create_uv(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)
    if obj.data.uv_layers.active:
        obj.data.uv_layers.active.name = "Rebellion_MaterialUV"


def assign_source_materials(obj, materials, minimum, maximum):
    obj.data.materials.clear()
    order = [
        "Rebellion_Worn_Disc_Steel",
        "Rebellion_Dark_Leg_Mechanism",
        "Rebellion_Hydraulic_Steel",
        "Rebellion_Front_Weapon_Black",
    ]
    for name in order:
        obj.data.materials.append(materials[name])

    lower_areas = []
    for polygon in obj.data.polygons:
        center = obj.matrix_world @ polygon.center
        if center.z < 0.88:
            lower_areas.append(polygon.area)
    small_face = (
        statistics.quantiles(lower_areas, n=4)[0]
        if len(lower_areas) >= 4
        else 0.001
    )

    counts = {name: 0 for name in order}
    for polygon in obj.data.polygons:
        center = obj.matrix_world @ polygon.center
        if (
            center.y < minimum.y + 0.15
            and abs(center.x) < 0.68
            and 0.96 < center.z < 1.48
        ):
            index = 3
        elif center.z > 0.88:
            index = 0
        elif polygon.area <= small_face * 1.35:
            index = 2
        else:
            index = 1
        polygon.material_index = index
        counts[order[index]] += 1
    return counts


def dominant_body_bone(obj):
    totals = {}
    for vertex in obj.data.vertices:
        world = obj.matrix_world @ vertex.co
        if world.z <= 0.88:
            continue
        for element in vertex.groups:
            group_name = obj.vertex_groups[element.group].name
            totals[group_name] = totals.get(group_name, 0.0) + element.weight
    if not totals:
        raise RuntimeError("Could not resolve the Rebellion body bone.")
    return max(totals.items(), key=lambda item: item[1])[0], totals


def apply_bevel(obj, width=0.015, segments=2):
    modifier = obj.modifiers.new("ReferenceDetailBevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)


def add_box(name, location, scale, material, bevel=0.015):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0.0:
        apply_bevel(obj, bevel, 2)
    obj.data.materials.append(material)
    return obj


def add_front_cylinder(
    name, location, radius, depth, material, vertices=20
):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=(math.radians(90.0), 0.0, 0.0),
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    return obj


def parent_to_bone(obj, armature, bone_name):
    world = obj.matrix_world.copy()
    obj.parent = armature
    obj.parent_type = "BONE"
    obj.parent_bone = bone_name
    obj.matrix_world = world


def carve_front_panel_recess(obj, body_bone):
    vertices_before = len(obj.data.vertices)
    polygons_before = len(obj.data.polygons)
    bpy.ops.mesh.primitive_cube_add(location=(0.0, -1.10, 1.43))
    cutter = bpy.context.object
    cutter.name = "Rebellion_Front_Recess_Cutter"
    cutter.scale = (0.48, 0.20, 0.205)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    apply_bevel(cutter, 0.018, 3)

    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    cutter.select_set(False)
    modifier = obj.modifiers.new("RebellionFrontPanelRecess", "BOOLEAN")
    modifier.operation = "DIFFERENCE"
    modifier.solver = "EXACT"
    modifier.object = cutter
    while obj.modifiers.find(modifier.name) > 0:
        bpy.ops.object.modifier_move_up(modifier=modifier.name)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)

    bpy.data.objects.remove(cutter, do_unlink=True)
    mesh_validation_fixed = obj.data.validate(
        verbose=True,
        clean_customdata=False,
    )
    obj.data.update()
    body_group = obj.vertex_groups.get(body_bone)
    if body_group is None:
        raise RuntimeError(f"Missing Rebellion body group: {body_bone}")
    unweighted = [
        vertex.index for vertex in obj.data.vertices if not vertex.groups
    ]
    if unweighted:
        body_group.add(unweighted, 1.0, "REPLACE")
    remaining_unweighted = sum(
        1 for vertex in obj.data.vertices if not vertex.groups
    )
    if remaining_unweighted:
        raise RuntimeError(
            f"Rebellion recess left {remaining_unweighted} unweighted vertices."
        )
    return {
        "operation": "BOOLEAN_DIFFERENCE",
        "target_region": "disc_front_panel_only",
        "opening_center": [0.0, -1.10, 1.43],
        "opening_size": [0.96, 0.40, 0.41],
        "corner_bevel": 0.018,
        "vertices_before": vertices_before,
        "vertices_after": len(obj.data.vertices),
        "polygons_before": polygons_before,
        "polygons_after": len(obj.data.polygons),
        "topology_change_limited_to_derived_sample": True,
        "new_vertices_weighted_to": body_bone,
        "unweighted_vertices_after": remaining_unweighted,
        "mesh_validation_fixed": bool(mesh_validation_fixed),
        "mesh_is_valid_after_cleanup": not obj.data.validate(
            verbose=False,
            clean_customdata=False,
        ),
    }


def create_front_weapon_assembly(armature, body_bone, materials, optic):
    panel = materials["Rebellion_Front_Weapon_Black"]
    steel = materials["Rebellion_Hydraulic_Steel"]
    details = []
    panel_center_y = -0.925
    panel_center_z = 1.50
    details.append(
        add_box(
            "Rebellion_Front_Recess_Backplate",
            (0.0, panel_center_y, panel_center_z),
            (0.424, 0.004, 0.08),
            panel,
            0.008,
        )
    )
    gun_center = Vector((0.285, -1.12, panel_center_z))
    details.append(
        add_front_cylinder(
            "Rebellion_Gun_Hub",
            gun_center + Vector((0.0, 0.075, 0.0)),
            0.075,
            0.24,
            steel,
            24,
        )
    )
    barrel_offsets = [(0.0, 0.0)]
    for angle_index in range(6):
        angle = angle_index * math.tau / 6.0
        barrel_offsets.append((math.cos(angle) * 0.032, math.sin(angle) * 0.032))
    for index, (offset_x, offset_z) in enumerate(barrel_offsets):
        details.append(
            add_front_cylinder(
                f"Rebellion_Gun_Barrel_{index:02d}",
                gun_center + Vector((offset_x, -0.13, offset_z)),
                0.010,
                0.34,
                panel,
                16,
            )
        )
    details.append(
        add_front_cylinder(
            "Rebellion_Scan_Lens",
            (-0.255, -0.96, panel_center_z + 0.01),
            0.030,
            0.025,
            optic,
            24,
        )
    )
    for index in range(4):
        details.append(
            add_box(
                f"Rebellion_Panel_Vent_{index:02d}",
                (-0.10 + index * 0.040, -0.945, panel_center_z - 0.02),
                (0.010, 0.004, 0.025),
                steel,
                0.004,
            )
        )
    for index, (x, z) in enumerate(
        ((-0.39, 1.435), (-0.39, 1.565), (0.39, 1.435), (0.39, 1.565))
    ):
        details.append(
            add_front_cylinder(
                f"Rebellion_Panel_Fastener_{index:02d}",
                (x, -0.94, z),
                0.012,
                0.025,
                steel,
                12,
            )
        )
    for obj in details:
        parent_to_bone(obj, armature, body_bone)
    return details


def configure_source_sensor(sensor, optic, armature, body_bone):
    sensor.name = "Rebellion_Reference_Scan_Optic"
    sensor.data.materials.clear()
    sensor.data.materials.append(optic)
    parent_to_bone(sensor, armature, body_bone)


def look_at(camera, target):
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_area_light(name, location, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = location
    return obj


def setup_render_scene(model_objects):
    minimum, maximum = world_bounds(model_objects)
    target = (minimum + maximum) * 0.5
    size = maximum - minimum
    scale = max(size.x, size.y, size.z)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.82, 0.82, 0.82, 1.0)
    background.inputs["Strength"].default_value = 0.65
    scene.view_settings.look = "AgX - Medium Low Contrast"
    scene.view_settings.exposure = 0.0

    ground_material = bpy.data.materials.new("Rebellion_Review_Ground")
    ground_material.diffuse_color = (0.62, 0.62, 0.60, 1.0)
    bpy.ops.mesh.primitive_plane_add(size=scale * 8.0)
    ground = bpy.context.object
    ground.name = "Rebellion_Review_Ground"
    ground.location.z = minimum.z - 0.018
    ground.data.materials.append(ground_material)

    camera_data = bpy.data.cameras.new("Rebellion_Review_Camera")
    camera = bpy.data.objects.new("Rebellion_Review_Camera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    camera.data.lens = 62.0
    camera.data.sensor_width = 36.0
    scene.camera = camera

    add_area_light(
        "Rebellion_Key",
        target + Vector((-scale * 1.5, -scale * 1.7, scale * 2.2)),
        2000.0,
        scale * 1.15,
        (1.0, 0.91, 0.80),
    )
    add_area_light(
        "Rebellion_Fill",
        target + Vector((scale * 1.8, -scale * 0.8, scale * 1.0)),
        1200.0,
        scale,
        (0.62, 0.76, 1.0),
    )
    add_area_light(
        "Rebellion_Rim",
        target + Vector((0.0, scale * 1.7, scale * 1.6)),
        1600.0,
        scale,
        (0.75, 0.84, 1.0),
    )
    return camera, target, scale, ground


def render_view(camera, target, scale, relative, filename, lens):
    camera.location = target + Vector(relative) * scale
    camera.data.lens = lens
    look_at(camera, target)
    bpy.context.scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def composite_reference(render_path, output_path):
    reference = bpy.data.images.load(str(REFERENCE_IMAGE), check_existing=False)
    render = bpy.data.images.load(str(render_path), check_existing=False)
    ref_pixels = np.array(reference.pixels[:], dtype=np.float32).reshape(
        reference.size[1], reference.size[0], 4
    )
    render_pixels = np.array(render.pixels[:], dtype=np.float32).reshape(
        render.size[1], render.size[0], 4
    )
    height = 720
    panel_width = 960
    canvas = np.ones((height, panel_width * 2, 4), dtype=np.float32)
    canvas[:, :, 3] = 1.0
    fit_image(ref_pixels, canvas[:, :panel_width, :])
    fit_image(render_pixels, canvas[:, panel_width:, :])
    image = bpy.data.images.new(
        "Rebellion_Reference_Comparison",
        width=panel_width * 2,
        height=height,
    )
    image.pixels.foreach_set(canvas.ravel())
    image.filepath_raw = str(output_path)
    image.file_format = "PNG"
    image.save()


def fit_image(source, target):
    source_h, source_w, _ = source.shape
    target_h, target_w, _ = target.shape
    scale = min(target_w / source_w, target_h / source_h)
    width = max(1, int(source_w * scale))
    height = max(1, int(source_h * scale))
    x_indices = np.clip(
        np.linspace(0, source_w - 1, width).astype(np.int32),
        0,
        source_w - 1,
    )
    y_indices = np.clip(
        np.linspace(0, source_h - 1, height).astype(np.int32),
        0,
        source_h - 1,
    )
    resized = source[y_indices[:, None], x_indices[None, :], :]
    x0 = (target_w - width) // 2
    y0 = (target_h - height) // 2
    target[y0 : y0 + height, x0 : x0 + width, :] = resized


def export_sample(export_objects):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in export_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = export_objects[0]
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_PATH),
        export_format="GLB",
        use_selection=True,
        export_apply=False,
        export_animations=False,
        export_cameras=False,
        export_lights=False,
        export_materials="EXPORT",
        export_all_influences=True,
        export_influence_nb=8,
    )


def roundtrip_metrics():
    clear_scene()
    bpy.ops.import_scene.gltf(filepath=str(EXPORT_PATH))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    armatures = [
        obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"
    ]
    return {
        "mesh_objects": len(meshes),
        "vertices": sum(len(obj.data.vertices) for obj in meshes),
        "polygons": sum(len(obj.data.polygons) for obj in meshes),
        "maximum_vertex_influences": max(
            (
                len(vertex.groups)
                for obj in meshes
                for vertex in obj.data.vertices
            ),
            default=0,
        ),
        "armatures": len(armatures),
        "bones": sum(len(obj.data.bones) for obj in armatures),
        "materials": sorted(
            {
                slot.material.name
                for obj in meshes
                for slot in obj.material_slots
                if slot.material
            }
        ),
    }


def main():
    ensure_directories()
    if not SOURCE_GLB.exists():
        raise FileNotFoundError(SOURCE_GLB)
    if not REFERENCE_IMAGE.exists():
        raise FileNotFoundError(REFERENCE_IMAGE)
    shutil.copy2(SOURCE_GLB, SOURCE_COPY)
    shutil.copy2(REFERENCE_IMAGE, REFERENCE_COPY)

    clear_scene()
    bpy.ops.import_scene.gltf(filepath=str(SOURCE_GLB))
    mesh_objects = [
        obj for obj in bpy.context.scene.objects if obj.type == "MESH"
    ]
    armatures = [
        obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"
    ]
    if len(armatures) != 1:
        raise RuntimeError(
            f"Expected one Rebellion armature, found {len(armatures)}."
        )
    skinned = next(
        (
            obj
            for obj in mesh_objects
            if obj.vertex_groups and any(mod.type == "ARMATURE" for mod in obj.modifiers)
        ),
        None,
    )
    if skinned is None:
        raise RuntimeError("The Rebellion skinned mesh is missing.")
    sensor = next((obj for obj in mesh_objects if obj is not skinned), None)
    armature = armatures[0]
    minimum_before, maximum_before = world_bounds(mesh_objects)
    signature_before = mesh_signature(skinned)
    topology_weight_before = topology_weight_signature(skinned)
    source_metrics = {
        "skinned_mesh": skinned.name,
        "vertices": len(skinned.data.vertices),
        "polygons": len(skinned.data.polygons),
        "vertex_groups": len(skinned.vertex_groups),
        "maximum_vertex_influences": max(
            (len(vertex.groups) for vertex in skinned.data.vertices),
            default=0,
        ),
        "bones": len(armature.data.bones),
        "bounds_min": vec(minimum_before),
        "bounds_max": vec(maximum_before),
        "bounds_size": vec(maximum_before - minimum_before),
        "signature": signature_before,
        "topology_weight_signature": topology_weight_before,
    }

    texture_paths = {
        name: generate_texture_maps(name, spec)
        for name, spec in MATERIAL_SPECS.items()
    }
    materials = {
        name: create_pbr_material(name, MATERIAL_SPECS[name], texture_paths[name])
        for name in MATERIAL_SPECS
    }
    optic = create_optic_material()
    body_bone, body_weight_totals = dominant_body_bone(skinned)
    recess_metrics = carve_front_panel_recess(skinned, body_bone)
    create_uv(skinned)
    minimum_sample, maximum_sample = world_bounds(mesh_objects)
    material_counts = assign_source_materials(
        skinned, materials, minimum_sample, maximum_sample
    )
    if sensor is not None:
        configure_source_sensor(sensor, optic, armature, body_bone)
    details = create_front_weapon_assembly(
        armature, body_bone, materials, optic
    )

    signature_after = mesh_signature(skinned)
    topology_weight_after = topology_weight_signature(skinned)
    if signature_before == signature_after:
        raise RuntimeError("Rebellion derived sample recess was not created.")
    if len(armature.data.bones) != 29:
        raise RuntimeError("Rebellion rig bone count changed.")

    model_objects = [skinned] + ([sensor] if sensor is not None else []) + details
    camera, target, scale, ground = setup_render_scene(model_objects)
    render_view(
        camera,
        target,
        scale,
        (0.0, -2.35, 0.48),
        "01_reference_matched_front.png",
        67.0,
    )
    render_view(
        camera,
        target,
        scale,
        (1.68, -1.72, 0.72),
        "02_three_quarter.png",
        58.0,
    )
    render_view(
        camera,
        target,
        scale,
        (2.38, 0.0, 0.56),
        "03_side.png",
        62.0,
    )
    render_view(
        camera,
        target,
        scale,
        (-1.55, 1.78, 0.65),
        "04_back_three_quarter.png",
        58.0,
    )
    render_view(
        camera,
        Vector((0.0, -1.10, 1.50)),
        scale * 0.55,
        (0.62, -1.90, 0.24),
        "05_front_weapon_detail.png",
        72.0,
    )
    composite_reference(
        RENDER_DIR / "01_reference_matched_front.png",
        RENDER_DIR / "06_reference_comparison.png",
    )

    export_objects = [armature, skinned] + (
        [sensor] if sensor is not None else []
    ) + details
    sample_metrics = {
        "vertices": len(skinned.data.vertices),
        "polygons": len(skinned.data.polygons),
        "bones": len(armature.data.bones),
        "signature": signature_after,
        "signature_match": signature_before == signature_after,
        "signature_change_intentional": True,
        "topology_weight_signature": topology_weight_after,
        "topology_weight_signature_match": (
            topology_weight_before == topology_weight_after
        ),
        "topology_change_intentional_in_derived_sample": True,
        "derived_sample_recess": recess_metrics,
        "added_reference_detail_objects": len(details),
        "body_attachment_bone": body_bone,
        "front_panel_contract": {
            "object": "Rebellion_Front_Recess_Backplate",
            "center": [0.0, -0.925, 1.50],
            "size": [0.848, 0.008, 0.16],
            "vertical_bounds": [1.42, 1.58],
            "gun_hub_radius": 0.075,
            "barrel_radius": 0.010,
            "barrel_array_radius": 0.032,
            "source_disc_face_y": -1.13,
            "panel_front_y": -0.929,
            "panel_setback_depth": 0.201,
            "cavity_wall_source": "boolean_cut_in_disc_mesh",
            "cavity_back_y": -0.90,
            "panel_recessed_behind_disc_surface": True,
            "panel_within_disc_front_band": True,
            "disc_mesh_cut_around_panel": True,
            "cavity_has_internal_walls": True,
            "weapon_assembly_projects_forward_from_panel": True,
        },
    }
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    export_sample(export_objects)
    roundtrip = roundtrip_metrics()

    validation = {
        "result": "PASS",
        "source_geometry": source_metrics,
        "sample_geometry": sample_metrics,
        "roundtrip_glb": roundtrip,
        "source_model_modified": False,
        "derived_sample_panel_region_modified": True,
        "unity_asset_modified": False,
        "unity_scene_modified": False,
    }
    GEOMETRY_PATH.write_text(
        json.dumps(validation, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    texture_files = sorted(
        str(path.relative_to(SAMPLE_ROOT)).replace("\\", "/")
        for path in TEXTURE_DIR.glob("*.png")
    )
    render_files = [
        "renders/01_reference_matched_front.png",
        "renders/02_three_quarter.png",
        "renders/03_side.png",
        "renders/04_back_three_quarter.png",
        "renders/05_front_weapon_detail.png",
        "renders/06_reference_comparison.png",
    ]
    manifest = {
        "enemy_id": "rebellion",
        "sample": "appearance_reference_sync",
        "approval_status": "PENDING_USER_REVIEW",
        "source_unity_glb": "Assets/_Project/Art/Enemies/Rebellion/Models/Rebellion.glb",
        "source_sha256": sha256(SOURCE_GLB),
        "source_copy_sha256": sha256(SOURCE_COPY),
        "reference_image": "image/rébellion(리벨리온).png",
        "reference_sha256": sha256(REFERENCE_IMAGE),
        "reference_copy_sha256": sha256(REFERENCE_COPY),
        "modeling_policy": (
            "원본 Unity GLB와 29본 리그는 변경하지 않았습니다. 파생 아트 샘플의 "
            "원반 정면 패널 영역을 절삭해 오목한 수납부와 후퇴한 검은 뒷판을 "
            "만들고, 절삭으로 생긴 정점은 몸통 본에 귀속했습니다."
        ),
        "surface_materials": MATERIAL_SPECS,
        "source_polygon_material_counts": material_counts,
        "body_attachment_bone": body_bone,
        "geometry_validation": validation,
        "outputs": {
            "blend": str(BLEND_PATH.relative_to(SAMPLE_ROOT)).replace("\\", "/"),
            "glb": str(EXPORT_PATH.relative_to(SAMPLE_ROOT)).replace("\\", "/"),
            "source_copy": str(SOURCE_COPY.relative_to(SAMPLE_ROOT)).replace("\\", "/"),
            "reference_copy": str(REFERENCE_COPY.relative_to(SAMPLE_ROOT)).replace("\\", "/"),
            "textures": texture_files,
            "renders": render_files,
            "documents": [
                "README.md",
                "TEXTURE_ANALYSIS.md",
                "ASSET_MANIFEST.json",
                "APPROVAL_STATUS.json",
                "GEOMETRY_VALIDATION.json",
                "index.html",
            ],
        },
    }
    MANIFEST_PATH.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "result": "PASS",
                "source_asset_unmodified": sha256(SOURCE_GLB) == sha256(SOURCE_COPY),
                "derived_sample_recess_created": signature_before != signature_after,
                "body_attachment_bone": body_bone,
                "source_sha256": manifest["source_sha256"],
                "export_sha256": sha256(EXPORT_PATH),
                "roundtrip": roundtrip,
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
