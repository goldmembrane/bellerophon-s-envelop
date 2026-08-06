import argparse
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Vector


PROJECT_ROOT = Path(__file__).resolve().parents[4]
SAMPLE_DIR = PROJECT_ROOT / "artSample" / "enemies" / "ispant_draw_sword"
DIAGNOSTICS_DIR = SAMPLE_DIR / "diagnostics"
SOURCE_BLEND = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "ispant_armed"
    / "Ispant_Armed_Appearance_Sample.blend"
)
CURRENT_DRAW_FBX = (
    PROJECT_ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Enemies"
    / "Ispant"
    / "Animations"
    / "Ispant_DrawSword.fbx"
)
REFERENCE_IMAGE = PROJECT_ROOT / "image" / "išpant(이슈판트)-armed.png"
OUTPUT_BLEND = SAMPLE_DIR / "Ispant_DrawSword_ArtSample.blend"
OUTPUT_GLB = SAMPLE_DIR / "Ispant_DrawSword_ArtSample.glb"
OUTPUT_FBX = SAMPLE_DIR / "Ispant_DrawSword_ArtSample.fbx"
DIAGNOSTIC_RENDER = DIAGNOSTICS_DIR / "Ispant_DrawSword_Diagnostic.png"
DIAGNOSTIC_HANDLE_RENDER = DIAGNOSTICS_DIR / "Ispant_DrawSword_Handle_Diagnostic.png"
FULL_RENDER = SAMPLE_DIR / "Ispant_DrawSword_Full.png"
CLOSEUP_RENDER = SAMPLE_DIR / "Ispant_DrawSword_HandleCloseup.png"
SIDE_RENDER = SAMPLE_DIR / "Ispant_DrawSword_Side.png"
REPORT_PATH = SAMPLE_DIR / "Ispant_DrawSword_ArtSample_Report.json"

EXPECTED_DRAW_FBX_SHA256 = (
    "B9DEB78C6BECA61C81EE5ECD86C4763E56186B8925EED29720B4B62ED482CE42"
)
FRAME = 1
BLADE_LENGTH = 0.82
BLADE_BASE_HALF_WIDTH = 0.034
BLADE_THICKNESS = 0.012
GUARD_WIDTH = 0.19
GRIP_LENGTH = 0.17
GRIP_RADIUS = 0.026
POMMEL_LENGTH = 0.055
GRIP_CENTER_Z = -0.103
EXPECTED_OVERALL_LENGTH = 1.055


def parse_args():
    values = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--diagnostic", action="store_true")
    parser.add_argument("--final", action="store_true")
    return parser.parse_args(values)


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def connected_components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        first, second = edge.vertices
        adjacency[first].add(second)
        adjacency[second].add(first)
    pending = set(range(len(mesh.vertices)))
    result = []
    while pending:
        seed = min(pending)
        pending.remove(seed)
        stack = [seed]
        vertices = []
        while stack:
            current = stack.pop()
            vertices.append(current)
            for neighbour in adjacency[current]:
                if neighbour in pending:
                    pending.remove(neighbour)
                    stack.append(neighbour)
        result.append(set(vertices))
    return result


def load_approved_materials():
    requested_names = [
        "Ispant_Armor",
        "Ispant_Copper",
        "Ispant_Eye_Cyan",
        "Ispant_Gunmetal",
        "Ispant_Helmet",
        "Ispant_Helmet_Face",
        "Ispant_Leather",
        "Ispant_Rubber_Black",
        "Ispant_Steel",
        "Ispant_Wood",
    ]
    with bpy.data.libraries.load(str(SOURCE_BLEND), link=False) as (source, target):
        available = set(source.materials)
        missing = [name for name in requested_names if name not in available]
        if missing:
            raise RuntimeError("Approved Ispant materials are missing: {}".format(missing))
        target.materials = list(requested_names)
    return {
        name: material
        for name, material in zip(requested_names, target.materials)
    }


def normalized_material_name(name):
    value = name.replace("_Approved", "")
    if value == "Ispant_Crescent_Armor":
        return "Ispant_Steel"
    return value


def create_sword_material(name, dark_color, light_color, metallic, roughness, scale):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    for node in list(nodes):
        nodes.remove(node)
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    noise = nodes.new("ShaderNodeTexNoise")
    ramp = nodes.new("ShaderNodeValToRGB")
    bump = nodes.new("ShaderNodeBump")
    texcoord = nodes.new("ShaderNodeTexCoord")
    noise.inputs["Scale"].default_value = scale
    noise.inputs["Detail"].default_value = 5.0
    noise.inputs["Roughness"].default_value = 0.72
    ramp.color_ramp.elements[0].position = 0.25
    ramp.color_ramp.elements[0].color = (*dark_color, 1.0)
    ramp.color_ramp.elements[1].position = 0.78
    ramp.color_ramp.elements[1].color = (*light_color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    bump.inputs["Strength"].default_value = 0.22 if metallic < 0.5 else 0.12
    bump.inputs["Distance"].default_value = 0.055 if metallic < 0.5 else 0.025
    links.new(texcoord.outputs["Generated"], noise.inputs["Vector"])
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], shader.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def create_reference_sword_materials():
    return {
        "steel": create_sword_material(
            "Ispant_LongSword_WornSteel",
            (0.028, 0.034, 0.038),
            (0.13, 0.15, 0.16),
            0.92,
            0.34,
            5.5,
        ),
        "leather": create_sword_material(
            "Ispant_LongSword_BrownLeather",
            (0.006, 0.0015, 0.0006),
            (0.042, 0.010, 0.003),
            0.0,
            0.58,
            8.0,
        ),
        "engraving": create_sword_material(
            "Ispant_LongSword_DarkEngraving",
            (0.025, 0.030, 0.032),
            (0.08, 0.09, 0.10),
            0.85,
            0.42,
            11.0,
        ),
    }


def synchronize_appearance(materials):
    synchronized = 0
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        if len(obj.data.uv_layers) == 3:
            expected_uv_names = (
                "uv",
                "IspantMechanicalUV",
                "IspantHelmetFaceUV",
            )
            for layer, expected_name in zip(obj.data.uv_layers, expected_uv_names):
                layer.name = expected_name
        for slot in obj.material_slots:
            if slot.material is None:
                raise RuntimeError("Null material on {}".format(obj.name))
            target_name = normalized_material_name(slot.material.name)
            if target_name not in materials:
                raise RuntimeError(
                    "No exact approved material matches {}/{}".format(
                        obj.name,
                        slot.material.name,
                    )
                )
            slot.material = materials[target_name]
            synchronized += 1
    return synchronized


def create_blade(material):
    sections = [
        (0.018, BLADE_BASE_HALF_WIDTH, BLADE_THICKNESS * 0.5),
        (0.085, BLADE_BASE_HALF_WIDTH * 1.03, BLADE_THICKNESS * 0.5),
        (0.61, BLADE_BASE_HALF_WIDTH * 0.72, BLADE_THICKNESS * 0.42),
        (0.765, BLADE_BASE_HALF_WIDTH * 0.38, BLADE_THICKNESS * 0.30),
        (BLADE_LENGTH, 0.0015, BLADE_THICKNESS * 0.10),
    ]
    vertices = []
    for z, width, depth in sections:
        vertices.extend(
            [
                (0.0, depth, z),
                (width, 0.0, z),
                (0.0, -depth, z),
                (-width, 0.0, z),
            ]
        )
    faces = []
    for section in range(len(sections) - 1):
        start = section * 4
        following = (section + 1) * 4
        for corner in range(4):
            faces.append(
                (
                    start + corner,
                    start + (corner + 1) % 4,
                    following + (corner + 1) % 4,
                    following + corner,
                )
            )
    faces.append((3, 2, 1, 0))
    faces.append(tuple(range((len(sections) - 1) * 4, len(sections) * 4)))
    mesh = bpy.data.meshes.new("Ispant_Reference_LongSword_BladeMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Ispant_Reference_LongSword_Blade", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    bevel = obj.modifiers.new("ReferenceBladeEdgeBevel", "BEVEL")
    bevel.width = 0.0022
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth()
    obj.select_set(False)
    return obj


def create_cylinder(name, radius, depth, location, material, vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name + "Mesh"
    obj.data.materials.append(material)
    bevel = obj.modifiers.new(name + "Bevel", "BEVEL")
    bevel.width = min(radius * 0.22, depth * 0.12)
    bevel.segments = 2
    bpy.ops.object.shade_smooth()
    return obj


def create_cylinder_between(name, start, end, radius, material, vertices=12):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    obj = create_cylinder(
        name,
        radius,
        direction.length,
        (start + end) * 0.5,
        material,
        vertices,
    )
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    return obj


def create_guard(material):
    parts = []
    left_points = [
        (0.0, 0.0, 0.0),
        (-0.045, 0.0, 0.006),
        (-0.078, 0.0, -0.004),
        (-GUARD_WIDTH * 0.5, 0.0, -0.022),
    ]
    right_points = [(-x, y, z) for x, y, z in left_points]
    for side, points in (("Left", left_points), ("Right", right_points)):
        for index in range(len(points) - 1):
            parts.append(
                create_cylinder_between(
                    "Ispant_Reference_LongSword_Guard_{}_{}".format(side, index),
                    points[index],
                    points[index + 1],
                    0.009 - index * 0.0012,
                    material,
                )
            )
    bpy.ops.mesh.primitive_torus_add(
        align="WORLD",
        major_segments=20,
        minor_segments=8,
        location=(0.0, 0.0, -0.002),
        major_radius=0.031,
        minor_radius=0.007,
    )
    collar = bpy.context.object
    collar.name = "Ispant_Reference_LongSword_GuardCollar"
    collar.data.materials.append(material)
    parts.append(collar)
    return parts


def create_grip(steel, leather):
    parts = []
    grip = create_cylinder(
        "Ispant_Reference_LongSword_Grip",
        GRIP_RADIUS,
        GRIP_LENGTH,
        (0.0, 0.0, GRIP_CENTER_Z),
        leather,
        vertices=18,
    )
    parts.append(grip)
    for index in range(8):
        z = -0.028 - index * (GRIP_LENGTH - 0.025) / 7.0
        bpy.ops.mesh.primitive_torus_add(
            align="WORLD",
            major_segments=18,
            minor_segments=6,
            location=(0.0, 0.0, z),
            major_radius=GRIP_RADIUS * 1.01,
            minor_radius=0.0021,
        )
        wrap = bpy.context.object
        wrap.name = "Ispant_Reference_LongSword_GripWrap_{:02d}".format(index + 1)
        wrap.data.materials.append(leather)
        parts.append(wrap)
    parts.append(
        create_cylinder(
            "Ispant_Reference_LongSword_UpperGripCollar",
            0.031,
            0.018,
            (0.0, 0.0, -0.018),
            steel,
            vertices=16,
        )
    )
    parts.append(
        create_cylinder(
            "Ispant_Reference_LongSword_LowerGripCollar",
            0.031,
            0.018,
            (0.0, 0.0, -0.188),
            steel,
            vertices=16,
        )
    )
    bpy.ops.mesh.primitive_ico_sphere_add(
        subdivisions=2,
        radius=1.0,
        location=(0.0, 0.0, -0.224),
    )
    pommel = bpy.context.object
    pommel.name = "Ispant_Reference_LongSword_Pommel"
    pommel.scale = (0.035, 0.027, POMMEL_LENGTH * 0.5)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    pommel.data.materials.append(steel)
    bevel = pommel.modifiers.new("ReferencePommelBevel", "BEVEL")
    bevel.width = 0.003
    bevel.segments = 2
    bpy.ops.object.shade_smooth()
    parts.append(pommel)
    return parts


def create_blade_etching(material):
    parts = []
    for side in (-1.0, 1.0):
        points = []
        for index in range(17):
            ratio = index / 16.0
            z = 0.08 + ratio * 0.61
            taper = 1.0 - ratio * 0.34
            x = side * BLADE_BASE_HALF_WIDTH * taper * 0.56
            x += side * math.sin(ratio * math.pi * 8.0) * 0.0022
            points.append((x, -BLADE_THICKNESS * 0.52, z))
        curve_data = bpy.data.curves.new(
            "Ispant_Reference_LongSword_EtchingCurve",
            "CURVE",
        )
        curve_data.dimensions = "3D"
        curve_data.bevel_depth = 0.0008
        curve_data.bevel_resolution = 1
        spline = curve_data.splines.new("POLY")
        spline.points.add(len(points) - 1)
        for point, coordinate in zip(spline.points, points):
            point.co = (*coordinate, 1.0)
        obj = bpy.data.objects.new(
            "Ispant_Reference_LongSword_Etching_{}".format(
                "Left" if side < 0 else "Right"
            ),
            curve_data,
        )
        bpy.context.collection.objects.link(obj)
        obj.data.materials.append(material)
        parts.append(obj)
    return parts


def join_sword(parts):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts:
        bpy.ops.object.select_all(action="DESELECT")
        obj.hide_set(False)
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        if obj.type == "CURVE":
            bpy.ops.object.convert(target="MESH")
        for modifier in list(obj.modifiers):
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    sword = bpy.context.object
    sword.name = "Ispant_Reference_LongSword"
    sword.data.name = "Ispant_Reference_LongSword_Mesh"
    for polygon in sword.data.polygons:
        polygon.use_smooth = True
    if len(sword.data.uv_layers) == 0:
        sword.data.uv_layers.new(name="uv")
    else:
        sword.data.uv_layers[0].name = "uv"
    bpy.context.view_layer.objects.active = sword
    sword.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    sword["art_sample_role"] = "Reference-matched one-handed longsword"
    sword["grip_anchor_local"] = (0.0, 0.0, GRIP_CENTER_Z)
    return sword


def old_sword_alignment(armature, old_sword):
    components = connected_components(old_sword.data)
    if len(components) != 2:
        raise RuntimeError("Current Unity draw-sword mesh must contain blade and handle components.")
    hand = armature.pose.bones.get("mixamorig:RightHand")
    if hand is None:
        raise RuntimeError("Current Unity draw-sword right-hand bone is missing.")
    hand_center = armature.matrix_world @ ((hand.head + hand.tail) * 0.5)
    world_vertices = {
        index: old_sword.matrix_world @ old_sword.data.vertices[index].co
        for component in components
        for index in component
    }
    component_centers = [
        sum((world_vertices[index] for index in component), Vector()) / len(component)
        for component in components
    ]
    handle_index = min(
        range(len(components)),
        key=lambda index: (component_centers[index] - hand_center).length,
    )
    blade_index = 1 - handle_index
    handle_center = component_centers[handle_index]
    blade_points = np.array(
        [tuple(world_vertices[index]) for index in sorted(components[blade_index])],
        dtype=np.float64,
    )
    blade_center = blade_points.mean(axis=0)
    _, _, axes = np.linalg.svd(blade_points - blade_center)
    length_axis = Vector(axes[0])
    if length_axis.dot(Vector(blade_center) - handle_center) < 0.0:
        length_axis.negate()
    width_axis = Vector(axes[1])
    width_axis -= length_axis * width_axis.dot(length_axis)
    width_axis.normalize()
    normal_axis = length_axis.cross(width_axis).normalized()
    if normal_axis.dot(Vector((0.0, -1.0, 0.0))) < 0.0:
        normal_axis.negate()
        width_axis.negate()
    rotation = Matrix((width_axis, normal_axis, length_axis)).transposed().to_4x4()
    local_grip_center = Vector((0.0, 0.0, GRIP_CENTER_Z))
    translation = handle_center - rotation.to_3x3() @ local_grip_center
    transform = Matrix.Translation(translation) @ rotation
    return transform, hand_center, handle_center, length_axis, normal_axis


def apply_alignment(sword, armature, transform):
    sword.matrix_world = transform
    world = sword.matrix_world.copy()
    sword.parent = armature
    sword.parent_type = "BONE"
    sword.parent_bone = "mixamorig:RightHand"
    sword.matrix_world = world


def evaluated_world_vertices(obj):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        return [evaluated.matrix_world @ vertex.co for vertex in mesh.vertices]
    finally:
        evaluated.to_mesh_clear()


def combined_bounds(objects):
    points = []
    for obj in objects:
        if obj.type == "MESH" and not obj.hide_render:
            points.extend(evaluated_world_vertices(obj))
    if not points:
        raise RuntimeError("No visible mesh vertices are available for framing.")
    minimum = Vector((min(point.x for point in points), min(point.y for point in points), min(point.z for point in points)))
    maximum = Vector((max(point.x for point in points), max(point.y for point in points), max(point.z for point in points)))
    return minimum, maximum


def look_at(camera, target):
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()


def clear_review_objects():
    for obj in list(bpy.context.scene.objects):
        if obj.name.startswith("IspantArtSampleReview_"):
            bpy.data.objects.remove(obj, do_unlink=True)


def create_camera(name, location, target, ortho_scale):
    bpy.ops.object.camera_add(location=location)
    camera = bpy.context.object
    camera.name = name
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale
    camera.data.lens = 62.0
    look_at(camera, target)
    bpy.context.scene.camera = camera
    return camera


def setup_lighting(center, height, energy_scale=1.0):
    world = bpy.context.scene.world
    if world is None:
        world = bpy.data.worlds.new("IspantArtSampleReview_World")
        bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.055, 0.065, 0.08, 1.0)
    background.inputs["Strength"].default_value = 0.38 * energy_scale
    lights = [
        ((-1.6, -2.2, 2.4), 1200.0, 3.0, (1.0, 0.91, 0.80)),
        ((1.8, -0.8, 1.7), 900.0, 2.5, (0.76, 0.87, 1.0)),
        ((0.4, 1.9, 2.2), 1000.0, 2.8, (0.82, 0.90, 1.0)),
    ]
    for index, (offset, energy, size, color) in enumerate(lights):
        bpy.ops.object.light_add(type="AREA", location=Vector(center) + Vector(offset) * height)
        light = bpy.context.object
        light.name = "IspantArtSampleReview_Light_{:02d}".format(index + 1)
        light.data.energy = energy * energy_scale
        light.data.shape = "DISK"
        light.data.size = size
        light.data.color = color
        look_at(light, center)


def setup_render(path, width=900, height=900):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.render.filepath = str(path)
    scene.render.image_settings.compression = 15
    scene.render.use_file_extension = True
    scene.view_settings.look = "AgX - Medium High Contrast"


def render_full(path, visible_meshes):
    clear_review_objects()
    minimum, maximum = combined_bounds(visible_meshes)
    center = (minimum + maximum) * 0.5
    height = max(maximum.z - minimum.z, 0.8)
    setup_lighting(center, height)
    camera = create_camera(
        "IspantArtSampleReview_FrontCamera",
        center + Vector((0.0, -3.4 * height, 0.08 * height)),
        center,
        max(height * 1.42, (maximum.x - minimum.x) * 1.18, (maximum.y - minimum.y) * 1.18),
    )
    setup_render(path)
    bpy.context.scene.camera = camera
    bpy.ops.render.render(write_still=True)


def render_side(path, visible_meshes):
    clear_review_objects()
    minimum, maximum = combined_bounds(visible_meshes)
    center = (minimum + maximum) * 0.5
    height = max(maximum.z - minimum.z, 0.8)
    setup_lighting(center, height)
    camera = create_camera(
        "IspantArtSampleReview_SideCamera",
        center + Vector((3.4 * height, 0.0, 0.08 * height)),
        center,
        max(height * 1.42, (maximum.x - minimum.x) * 1.18, (maximum.y - minimum.y) * 1.18),
    )
    setup_render(path)
    bpy.context.scene.camera = camera
    bpy.ops.render.render(write_still=True)


def render_handle_closeup(path, sword, handle_center, normal_axis):
    clear_review_objects()
    hidden_states = {
        obj: obj.hide_render
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
    }
    for obj in hidden_states:
        obj.hide_render = obj != sword
    detail_center = sword.matrix_world @ Vector((0.0, 0.0, -0.045))
    setup_lighting(detail_center, 0.7, 0.20)
    view_normal = normal_axis.copy()
    if view_normal.dot(Vector((0.0, -1.0, 0.0))) < 0.0:
        view_normal.negate()
    camera = create_camera(
        "IspantArtSampleReview_HandleCamera",
        detail_center + view_normal * 1.5,
        detail_center,
        0.52,
    )
    setup_render(path)
    bpy.context.scene.camera = camera
    try:
        bpy.ops.render.render(write_still=True)
    finally:
        for obj, hidden in hidden_states.items():
            obj.hide_render = hidden


def export_sample(armature, meshes, sword):
    export_objects = [armature] + meshes + [sword]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in export_objects:
        obj.hide_set(False)
        obj.hide_render = False
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.gltf(
        filepath=str(OUTPUT_GLB),
        export_format="GLB",
        use_selection=True,
        export_animations=True,
        export_apply=False,
        export_cameras=False,
        export_lights=False,
        export_extras=True,
    )
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_FBX),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode="COPY",
        embed_textures=True,
    )


def main():
    args = parse_args()
    if not args.diagnostic and not args.final:
        raise RuntimeError("Use --diagnostic or --final.")
    SAMPLE_DIR.mkdir(parents=True, exist_ok=True)
    DIAGNOSTICS_DIR.mkdir(parents=True, exist_ok=True)
    if sha256(CURRENT_DRAW_FBX) != EXPECTED_DRAW_FBX_SHA256:
        raise RuntimeError("Current Unity Ispant draw-sword FBX hash differs.")
    if not REFERENCE_IMAGE.exists() or not SOURCE_BLEND.exists():
        raise RuntimeError("The approved Ispant reference or appearance source is missing.")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(CURRENT_DRAW_FBX))
    armature = bpy.data.objects.get("Armature")
    old_sword = bpy.data.objects.get("Ispant_DrawSword_RigidSword")
    old_sheath = bpy.data.objects.get("Ispant_DrawSword_RigidSheath")
    if armature is None or old_sword is None or old_sheath is None:
        raise RuntimeError("Current Unity Ispant draw-sword structure differs.")
    actions = list(bpy.data.actions)
    if len(actions) != 1 or tuple(round(value) for value in actions[0].frame_range) != (1, 46):
        raise RuntimeError("Current Unity Ispant draw-sword Mixamo action differs.")
    if armature.animation_data is None:
        armature.animation_data_create()
    armature.animation_data.action = actions[0]
    armature.data.pose_position = "POSE"
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 46
    bpy.context.scene.frame_set(FRAME)
    bpy.context.view_layer.update()

    materials = load_approved_materials()
    synchronized_slots = synchronize_appearance(materials)
    sword_materials = create_reference_sword_materials()
    transform, hand_center, source_handle_center, length_axis, normal_axis = old_sword_alignment(
        armature,
        old_sword,
    )
    old_sword.hide_render = True
    old_sword.hide_set(True)
    old_sheath.hide_render = True
    old_sheath.hide_set(True)

    parts = [create_blade(sword_materials["steel"])]
    parts.extend(create_guard(sword_materials["steel"]))
    parts.extend(create_grip(sword_materials["steel"], sword_materials["leather"]))
    parts.extend(create_blade_etching(sword_materials["engraving"]))
    sword = join_sword(parts)
    apply_alignment(sword, armature, transform)
    sword["reference_image"] = "image/išpant(이슈판트)-armed.png"
    sword["unseen_surface_rule"] = "Reference-front matched with symmetric thickness only"
    sword["blade_length_m"] = BLADE_LENGTH
    sword["grip_length_m"] = GRIP_LENGTH
    sword["guard_width_m"] = GUARD_WIDTH

    local_vertices = [vertex.co for vertex in sword.data.vertices]
    local_min_z = min(vertex.z for vertex in local_vertices)
    local_max_z = max(vertex.z for vertex in local_vertices)
    overall_length = local_max_z - local_min_z
    if abs(overall_length - EXPECTED_OVERALL_LENGTH) > 0.035:
        raise RuntimeError("Reference longsword overall length differs: {}".format(overall_length))
    if not {
        "Ispant_LongSword_WornSteel",
        "Ispant_LongSword_BrownLeather",
        "Ispant_LongSword_DarkEngraving",
    }.issubset(
        {material.name for material in sword.data.materials}
    ):
        raise RuntimeError("Reference longsword PBR material set differs.")
    grip_world = sword.matrix_world @ Vector((0.0, 0.0, GRIP_CENTER_Z))
    grip_source_error = (grip_world - source_handle_center).length
    if grip_source_error > 0.00002:
        raise RuntimeError("Reference longsword grip did not preserve the source handle center.")
    hand_to_grip = (grip_world - hand_center).length

    visible_meshes = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and obj not in {old_sword, old_sheath}
    ]
    export_meshes = [obj for obj in visible_meshes if obj != sword]
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND), check_existing=False)
    export_sample(armature, export_meshes, sword)

    if args.diagnostic:
        render_full(DIAGNOSTIC_RENDER, visible_meshes)
        render_handle_closeup(
            DIAGNOSTIC_HANDLE_RENDER,
            sword,
            grip_world,
            normal_axis,
        )
    if args.final:
        render_full(FULL_RENDER, visible_meshes)
        render_side(SIDE_RENDER, visible_meshes)
        render_handle_closeup(CLOSEUP_RENDER, sword, grip_world, normal_axis)

    report = {
        "result": "PASS",
        "unity_runtime_applied": False,
        "source_current_unity_draw_fbx": str(CURRENT_DRAW_FBX),
        "source_current_unity_draw_fbx_sha256": sha256(CURRENT_DRAW_FBX),
        "approved_appearance_blend": str(SOURCE_BLEND),
        "approved_appearance_blend_sha256": sha256(SOURCE_BLEND),
        "reference_image": str(REFERENCE_IMAGE),
        "reference_image_sha256": sha256(REFERENCE_IMAGE),
        "source_mixamo_frame_range": [1, 46],
        "sample_pose_frame": FRAME,
        "approved_material_slots_synchronized": synchronized_slots,
        "replaced_sample_only_objects": [
            "Ispant_DrawSword_RigidSword",
            "Ispant_DrawSword_RigidSheath",
        ],
        "new_sword_object": sword.name,
        "new_sword_parent_bone": sword.parent_bone,
        "new_sword_materials": [material.name for material in sword.data.materials],
        "new_sword_vertices": len(sword.data.vertices),
        "new_sword_triangles": sum(len(polygon.vertices) - 2 for polygon in sword.data.polygons),
        "blade_length_m": BLADE_LENGTH,
        "blade_base_width_m": BLADE_BASE_HALF_WIDTH * 2.0,
        "blade_thickness_m": BLADE_THICKNESS,
        "guard_width_m": GUARD_WIDTH,
        "grip_length_m": GRIP_LENGTH,
        "grip_diameter_m": GRIP_RADIUS * 2.0,
        "pommel_length_m": POMMEL_LENGTH,
        "overall_length_m": overall_length,
        "grip_source_alignment_error_m": grip_source_error,
        "right_hand_to_grip_center_m": hand_to_grip,
        "symmetric_unseen_thickness_only": True,
        "output_blend": str(OUTPUT_BLEND),
        "output_blend_sha256": sha256(OUTPUT_BLEND),
        "output_glb": str(OUTPUT_GLB),
        "output_glb_sha256": sha256(OUTPUT_GLB),
        "output_fbx": str(OUTPUT_FBX),
        "output_fbx_sha256": sha256(OUTPUT_FBX),
        "diagnostic_render": str(DIAGNOSTIC_RENDER) if args.diagnostic else None,
        "diagnostic_handle_render": (
            str(DIAGNOSTIC_HANDLE_RENDER) if args.diagnostic else None
        ),
        "full_render": str(FULL_RENDER) if args.final else None,
        "handle_closeup_render": str(CLOSEUP_RENDER) if args.final else None,
        "side_render": str(SIDE_RENDER) if args.final else None,
    }
    REPORT_PATH.write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print("ISPANT_DRAW_SWORD_ART_SAMPLE_RESULT=PASS")
    print("ISPANT_DRAW_SWORD_OVERALL_LENGTH={}".format(overall_length))
    print("ISPANT_DRAW_SWORD_HAND_TO_GRIP={}".format(hand_to_grip))
    print("ISPANT_DRAW_SWORD_OUTPUT={}".format(OUTPUT_BLEND))


if __name__ == "__main__":
    main()
