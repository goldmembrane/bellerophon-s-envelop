import hashlib
import json
import math
import shutil
import struct
import sys
from array import array
from collections import Counter
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[4]
SAMPLE_DIR = ROOT / "artSample/enemies/resistance"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Resistance/Models/Resistance.fbx"
REFERENCE_IMAGE = ROOT / "image/résistance(레지스탕스).png"
RENDER_DIR = SAMPLE_DIR / "renders"
GEOMETRY_DIR = SAMPLE_DIR / "geometry"
TEXTURE_DIR = SAMPLE_DIR / "textures"
EXPORT_DIR = SAMPLE_DIR / "exports"

NEUTRAL_RENDER = RENDER_DIR / "00_neutral_current_model.png"
SOURCE_SIGNATURE = GEOMETRY_DIR / "source_geometry_signature.json"
INVARIANCE_REPORT = GEOMETRY_DIR / "geometry_invariance_report.json"
OUTPUT_BLEND = EXPORT_DIR / "resistance_current_model_material_sample.blend"
UNCHANGED_FBX = EXPORT_DIR / "Resistance_source_geometry_unchanged.fbx"

TEXTURE_SIZE = 1024
TEXTURE_PATHS = {
    "silver": TEXTURE_DIR / "resistance_worn_silver_albedo.png",
    "dark": TEXTURE_DIR / "resistance_dark_mechanics_albedo.png",
    "cyan": TEXTURE_DIR / "resistance_cyan_emission_albedo.png",
    "bronze": TEXTURE_DIR / "resistance_bronze_accents_albedo.png",
    "olive": TEXTURE_DIR / "resistance_bandana_olive_albedo.png",
    "roughness": TEXTURE_DIR / "resistance_surface_roughness.png",
    "bump": TEXTURE_DIR / "resistance_surface_micro_bump.png",
}


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_source():
    if not SOURCE_FBX.is_file():
        raise FileNotFoundError(SOURCE_FBX)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    mesh = bpy.data.objects.get("char1")
    armature = bpy.data.objects.get("Armature")
    if mesh is None or mesh.type != "MESH":
        raise RuntimeError("Resistance mesh char1 is missing.")
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("Resistance Armature is missing.")
    return mesh, armature


def float_bytes(values):
    output = bytearray()
    for value in values:
        output.extend(struct.pack("<d", float(value)))
    return output


def geometry_signature(mesh, armature):
    digest = hashlib.sha256()
    for vertex in mesh.data.vertices:
        digest.update(float_bytes(vertex.co))
    for edge in mesh.data.edges:
        digest.update(struct.pack("<II", *edge.vertices))
    for polygon in mesh.data.polygons:
        digest.update(struct.pack("<I", len(polygon.vertices)))
        for index in polygon.vertices:
            digest.update(struct.pack("<I", index))
    bone_names = [bone.name for bone in armature.data.bones]
    for name in bone_names:
        digest.update(name.encode("utf-8"))
        digest.update(b"\0")
    world_points = [mesh.matrix_world @ vertex.co for vertex in mesh.data.vertices]
    bounds_min = [min(point[axis] for point in world_points) for axis in range(3)]
    bounds_max = [max(point[axis] for point in world_points) for axis in range(3)]
    return {
        "source_fbx": SOURCE_FBX.relative_to(ROOT).as_posix(),
        "source_sha256": sha256(SOURCE_FBX),
        "mesh_object": mesh.name,
        "armature_object": armature.name,
        "vertex_count": len(mesh.data.vertices),
        "edge_count": len(mesh.data.edges),
        "polygon_count": len(mesh.data.polygons),
        "loop_count": len(mesh.data.loops),
        "bone_count": len(bone_names),
        "bone_names": bone_names,
        "uv_layers": [layer.name for layer in mesh.data.uv_layers],
        "shape_key_count": (
            len(mesh.data.shape_keys.key_blocks)
            if mesh.data.shape_keys is not None
            else 0
        ),
        "bounds_min": bounds_min,
        "bounds_max": bounds_max,
        "bounds_size": [
            bounds_max[axis] - bounds_min[axis] for axis in range(3)
        ],
        "geometry_sha256": digest.hexdigest().upper(),
    }


def write_json(path, payload):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def make_principled_material(name, base_color, metallic, roughness):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (*base_color, 1.0)
    principled.inputs["Metallic"].default_value = metallic
    principled.inputs["Roughness"].default_value = roughness
    return material


def noise01(x, y, seed):
    value = math.sin(
        x * 12.9898 + y * 78.233 + seed * 37.719
    ) * 43758.5453
    return value - math.floor(value)


def clamp01(value):
    return max(0.0, min(1.0, value))


def material_color(kind, u, v):
    fine = noise01(u * 41.0, v * 43.0, 1.0)
    broad = noise01(u * 9.0, v * 11.0, 2.0)
    scratch = 1.0 if (
        abs(math.sin((u * 19.0 + v * 71.0) * math.pi)) > 0.992
        and broad > 0.58
    ) else 0.0
    edge_grime = (
        clamp01((0.08 - min(u, 1.0 - u)) / 0.08) +
        clamp01((0.08 - min(v, 1.0 - v)) / 0.08)
    ) * 0.5
    if kind == "silver":
        base = [0.44, 0.47, 0.47]
        wear = (fine - 0.5) * 0.035 - edge_grime * 0.030
        color = [channel + wear for channel in base]
        if scratch > 0.0:
            color = [channel + 0.045 for channel in color]
        if broad < 0.018:
            color = [
                color[0] * 0.76,
                color[1] * 0.73,
                color[2] * 0.69,
            ]
    elif kind == "dark":
        base = [0.035, 0.050, 0.065]
        wear = (fine - 0.5) * 0.065 + broad * 0.030
        color = [
            base[0] + wear * 0.55,
            base[1] + wear * 0.75,
            base[2] + wear,
        ]
        if scratch > 0.0:
            color = [channel + 0.10 for channel in color]
    elif kind == "cyan":
        pulse = 0.82 + 0.18 * math.sin((u * 3.0 + v * 5.0) * math.pi)
        color = [0.01 * pulse, 0.58 * pulse, 0.82 * pulse]
    elif kind == "bronze":
        base = [0.24, 0.14, 0.065]
        wear = (fine - 0.5) * 0.075 - edge_grime * 0.050
        color = [
            base[0] + wear,
            base[1] + wear * 0.72,
            base[2] + wear * 0.42,
        ]
        if scratch > 0.0:
            color = [channel + 0.07 for channel in color]
    elif kind == "olive":
        weave = 0.5 + 0.5 * math.sin(u * 88.0 + v * 21.0)
        base = [0.16, 0.25, 0.10]
        color = [
            base[0] + (fine - 0.5) * 0.05 + weave * 0.018,
            base[1] + (fine - 0.5) * 0.07 + weave * 0.025,
            base[2] + (fine - 0.5) * 0.035,
        ]
    else:
        raise ValueError(kind)
    return tuple(clamp01(channel) for channel in color)


def create_texture_image(name, path, pixel_function, non_color=False):
    path.parent.mkdir(parents=True, exist_ok=True)
    image = bpy.data.images.new(
        name,
        width=TEXTURE_SIZE,
        height=TEXTURE_SIZE,
        alpha=False,
        float_buffer=False,
    )
    pixels = array("f")
    for y in range(TEXTURE_SIZE):
        v = y / max(1, TEXTURE_SIZE - 1)
        for x in range(TEXTURE_SIZE):
            u = x / max(1, TEXTURE_SIZE - 1)
            red, green, blue = pixel_function(u, v)
            pixels.extend((red, green, blue, 1.0))
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    if non_color:
        image.colorspace_settings.name = "Non-Color"
    image.save()
    return image


def create_textures():
    textures = {}
    for kind in ("silver", "dark", "cyan", "bronze", "olive"):
        textures[kind] = create_texture_image(
            "Resistance_" + kind.title() + "_Albedo",
            TEXTURE_PATHS[kind],
            lambda u, v, key=kind: material_color(key, u, v),
        )

    def roughness_pixel(u, v):
        fine = noise01(u * 33.0, v * 37.0, 7.0)
        broad = noise01(u * 7.0, v * 9.0, 8.0)
        value = clamp01(0.52 + fine * 0.26 + broad * 0.16)
        return value, value, value

    def bump_pixel(u, v):
        fine = noise01(u * 83.0, v * 79.0, 12.0)
        scratch = 0.18 if (
            abs(math.sin((u * 23.0 + v * 89.0) * math.pi)) > 0.992
        ) else 0.0
        value = clamp01(0.50 + (fine - 0.5) * 0.22 - scratch)
        return value, value, value

    textures["roughness"] = create_texture_image(
        "Resistance_Surface_Roughness",
        TEXTURE_PATHS["roughness"],
        roughness_pixel,
        non_color=True,
    )
    textures["bump"] = create_texture_image(
        "Resistance_Surface_Micro_Bump",
        TEXTURE_PATHS["bump"],
        bump_pixel,
        non_color=True,
    )
    return textures


def principled_input(principled, *names):
    for name in names:
        if name in principled.inputs:
            return principled.inputs[name]
    raise KeyError("Missing Principled input: " + ", ".join(names))


def math_socket(nodes, links, operation, left, right=None):
    node = nodes.new("ShaderNodeMath")
    node.operation = operation
    if hasattr(left, "bl_idname"):
        links.new(left, node.inputs[0])
    else:
        node.inputs[0].default_value = left
    if right is not None:
        if hasattr(right, "bl_idname"):
            links.new(right, node.inputs[1])
        else:
            node.inputs[1].default_value = right
    return node.outputs[0]


def range_mask(nodes, links, value, minimum, maximum):
    above = math_socket(
        nodes,
        links,
        "GREATER_THAN",
        value,
        minimum,
    )
    below = math_socket(
        nodes,
        links,
        "LESS_THAN",
        value,
        maximum,
    )
    return math_socket(nodes, links, "MULTIPLY", above, below)


def combine_masks(nodes, links, masks):
    combined = masks[0]
    for mask in masks[1:]:
        combined = math_socket(nodes, links, "MAXIMUM", combined, mask)
    return combined


def rectangle_mask(
    nodes,
    links,
    x_socket,
    z_socket,
    center_x,
    center_z,
    half_width,
    half_height,
    slope=0.0,
):
    z_offset = math_socket(
        nodes,
        links,
        "SUBTRACT",
        z_socket,
        center_z,
    )
    tilted_x = math_socket(
        nodes,
        links,
        "ADD",
        x_socket,
        math_socket(nodes, links, "MULTIPLY", z_offset, slope),
    )
    x_mask = range_mask(
        nodes,
        links,
        tilted_x,
        center_x - half_width,
        center_x + half_width,
    )
    z_mask = range_mask(
        nodes,
        links,
        z_socket,
        center_z - half_height,
        center_z + half_height,
    )
    return math_socket(nodes, links, "MULTIPLY", x_mask, z_mask)


def ellipse_mask(
    nodes,
    links,
    x_socket,
    z_socket,
    center_x,
    center_z,
    radius_x,
    radius_z,
):
    x_offset = math_socket(
        nodes,
        links,
        "DIVIDE",
        math_socket(nodes, links, "SUBTRACT", x_socket, center_x),
        radius_x,
    )
    z_offset = math_socket(
        nodes,
        links,
        "DIVIDE",
        math_socket(nodes, links, "SUBTRACT", z_socket, center_z),
        radius_z,
    )
    distance_squared = math_socket(
        nodes,
        links,
        "ADD",
        math_socket(nodes, links, "MULTIPLY", x_offset, x_offset),
        math_socket(nodes, links, "MULTIPLY", z_offset, z_offset),
    )
    return math_socket(nodes, links, "LESS_THAN", distance_squared, 1.0)


def reference_panel_projection(nodes, links, base_color):
    geometry = nodes.new("ShaderNodeNewGeometry")
    position = nodes.new("ShaderNodeSeparateXYZ")
    normal = nodes.new("ShaderNodeSeparateXYZ")
    links.new(geometry.outputs["Position"], position.inputs["Vector"])
    links.new(geometry.outputs["Normal"], normal.inputs["Vector"])
    abs_x = math_socket(
        nodes,
        links,
        "ABSOLUTE",
        position.outputs["X"],
    )
    front = math_socket(
        nodes,
        links,
        "LESS_THAN",
        normal.outputs["Y"],
        -0.16,
    )
    panel_facing = math_socket(
        nodes,
        links,
        "LESS_THAN",
        normal.outputs["Y"],
        0.10,
    )
    frame_masks = []
    core_masks = []

    def add_panel(frame_mask, *cyan_masks, visibility=None):
        active_visibility = front if visibility is None else visibility
        frame_masks.append(
            math_socket(
                nodes,
                links,
                "MULTIPLY",
                frame_mask,
                active_visibility,
            )
        )
        for cyan_mask in cyan_masks:
            core_masks.append(
                math_socket(
                    nodes,
                    links,
                    "MULTIPLY",
                    cyan_mask,
                    active_visibility,
                )
            )

    # 기준 이미지에서 보이는 어깨 원형 상태 인셋입니다.
    add_panel(
        ellipse_mask(
            nodes,
            links,
            position.outputs["X"],
            position.outputs["Z"],
            -0.235,
            1.425,
            0.042,
            0.040,
        ),
        ellipse_mask(
            nodes,
            links,
            position.outputs["X"],
            position.outputs["Z"],
            -0.235,
            1.425,
            0.024,
            0.022,
        ),
        visibility=panel_facing,
    )

    # 흉갑 아래쪽의 짧은 청록 표시입니다.
    add_panel(
        rectangle_mask(
            nodes,
            links,
            abs_x,
            position.outputs["Z"],
            0.115,
            1.292,
            0.040,
            0.020,
        ),
        rectangle_mask(
            nodes,
            links,
            abs_x,
            position.outputs["Z"],
            0.115,
            1.292,
            0.030,
            0.010,
        ),
    )

    # 복부 중앙 장갑의 단계형 표시 네 줄입니다.
    for abdomen_z in (1.205, 1.170, 1.135, 1.100):
        add_panel(
            rectangle_mask(
                nodes,
                links,
                position.outputs["X"],
                position.outputs["Z"],
                0.0,
                abdomen_z,
                0.031,
                0.012,
            ),
            rectangle_mask(
                nodes,
                links,
                position.outputs["X"],
                position.outputs["Z"],
                0.0,
                abdomen_z,
                0.021,
                0.005,
            ),
        )

    # 양쪽 전완은 팔 범위와 카메라 쪽 표면 깊이를 함께 제한합니다.
    forearm_frame = math_socket(
        nodes,
        links,
        "MULTIPLY",
        rectangle_mask(
            nodes,
            links,
            abs_x,
            position.outputs["Z"],
            0.325,
            1.070,
            0.075,
            0.080,
        ),
        range_mask(
            nodes,
            links,
            position.outputs["Y"],
            -0.18,
            0.02,
        ),
    )
    forearm_core = math_socket(
        nodes,
        links,
        "MULTIPLY",
        rectangle_mask(
            nodes,
            links,
            abs_x,
            position.outputs["Z"],
            0.325,
            1.070,
            0.065,
            0.055,
        ),
        range_mask(
            nodes,
            links,
            position.outputs["Y"],
            -0.18,
            -0.08,
        ),
    )
    near_forearm_side = range_mask(
        nodes,
        links,
        position.outputs["X"],
        -0.40,
        -0.25,
    )
    near_forearm_frame = math_socket(
        nodes,
        links,
        "MULTIPLY",
        near_forearm_side,
        math_socket(
            nodes,
            links,
            "MULTIPLY",
            range_mask(
                nodes,
                links,
                position.outputs["Z"],
                0.99,
                1.15,
            ),
            range_mask(
                nodes,
                links,
                position.outputs["Y"],
                0.02,
                0.16,
            ),
        ),
    )
    near_forearm_core = math_socket(
        nodes,
        links,
        "MULTIPLY",
        near_forearm_side,
        math_socket(
            nodes,
            links,
            "MULTIPLY",
            range_mask(
                nodes,
                links,
                position.outputs["Z"],
                1.015,
                1.125,
            ),
            range_mask(
                nodes,
                links,
                position.outputs["Y"],
                0.08,
                0.16,
            ),
        ),
    )
    add_panel(
        combine_masks(nodes, links, [forearm_frame, near_forearm_frame]),
        combine_masks(nodes, links, [forearm_core, near_forearm_core]),
        visibility=1.0,
    )

    # 허벅지는 검은 세로 패널 안에 짧은 청록 표시 세 줄만 둡니다.
    thigh_frame = rectangle_mask(
        nodes,
        links,
        abs_x,
        position.outputs["Z"],
        0.170,
        0.765,
        0.034,
        0.092,
        slope=0.10,
    )
    thigh_segments = [
        rectangle_mask(
            nodes,
            links,
            abs_x,
            position.outputs["Z"],
            0.170,
            segment_z,
            0.019,
            0.006,
            slope=0.10,
        )
        for segment_z in (0.720, 0.765, 0.810)
    ]
    add_panel(thigh_frame, *thigh_segments)

    # 정강이 장갑의 긴 세로 청록 인셋입니다.
    add_panel(
        rectangle_mask(
            nodes,
            links,
            abs_x,
            position.outputs["Z"],
            0.205,
            0.410,
            0.026,
            0.105,
        ),
        rectangle_mask(
            nodes,
            links,
            abs_x,
            position.outputs["Z"],
            0.205,
            0.410,
            0.011,
            0.078,
        ),
    )
    frame_mask = combine_masks(nodes, links, frame_masks)
    core_mask = combine_masks(nodes, links, core_masks)

    frame_mix = nodes.new("ShaderNodeMixRGB")
    frame_mix.blend_type = "MIX"
    links.new(frame_mask, frame_mix.inputs["Fac"])
    links.new(base_color, frame_mix.inputs[1])
    frame_mix.inputs[2].default_value = (0.008, 0.014, 0.020, 1.0)

    cyan_mix = nodes.new("ShaderNodeMixRGB")
    cyan_mix.blend_type = "MIX"
    links.new(core_mask, cyan_mix.inputs["Fac"])
    links.new(frame_mix.outputs["Color"], cyan_mix.inputs[1])
    cyan_mix.inputs[2].default_value = (0.005, 0.55, 0.80, 1.0)
    return cyan_mix.outputs["Color"], core_mask


def make_textured_material(
    name,
    albedo,
    roughness,
    bump,
    metallic,
    bump_strength,
    emission_strength=0.0,
    reference_projection=False,
):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    principled = nodes.get("Principled BSDF")

    albedo_node = nodes.new("ShaderNodeTexImage")
    albedo_node.name = name + "_Albedo"
    albedo_node.image = albedo
    albedo_node.interpolation = "Linear"
    base_color = albedo_node.outputs["Color"]
    projection_mask = None
    if reference_projection:
        base_color, projection_mask = reference_panel_projection(
            nodes,
            links,
            base_color,
        )
    links.new(base_color, principled_input(principled, "Base Color"))

    roughness_node = nodes.new("ShaderNodeTexImage")
    roughness_node.name = name + "_Roughness"
    roughness_node.image = roughness
    roughness_node.interpolation = "Linear"
    links.new(
        roughness_node.outputs["Color"],
        principled_input(principled, "Roughness"),
    )

    bump_texture = nodes.new("ShaderNodeTexImage")
    bump_texture.name = name + "_MicroBump"
    bump_texture.image = bump
    bump_texture.interpolation = "Linear"
    bump_node = nodes.new("ShaderNodeBump")
    bump_node.inputs["Strength"].default_value = bump_strength
    bump_node.inputs["Distance"].default_value = 0.055
    links.new(bump_texture.outputs["Color"], bump_node.inputs["Height"])
    links.new(
        bump_node.outputs["Normal"],
        principled_input(principled, "Normal"),
    )

    principled_input(principled, "Metallic").default_value = metallic
    if projection_mask is not None:
        projected_emission = nodes.new("ShaderNodeMixRGB")
        links.new(projection_mask, projected_emission.inputs["Fac"])
        projected_emission.inputs[1].default_value = (0.0, 0.0, 0.0, 1.0)
        projected_emission.inputs[2].default_value = (
            0.005,
            0.55,
            0.80,
            1.0,
        )
        links.new(
            projected_emission.outputs["Color"],
            principled_input(principled, "Emission Color", "Emission"),
        )
        principled_input(
            principled,
            "Emission Strength",
        ).default_value = 0.65
    if emission_strength > 0.0:
        links.new(
            albedo_node.outputs["Color"],
            principled_input(principled, "Emission Color", "Emission"),
        )
        principled_input(
            principled,
            "Emission Strength",
        ).default_value = emission_strength
    return material


def create_materials(textures):
    return {
        "silver": make_textured_material(
            "M_Resistance_Worn_Silver",
            textures["silver"],
            textures["roughness"],
            textures["bump"],
            metallic=0.38,
            bump_strength=0.12,
            reference_projection=True,
        ),
        "dark": make_textured_material(
            "M_Resistance_Dark_Mechanics",
            textures["dark"],
            textures["roughness"],
            textures["bump"],
            metallic=0.48,
            bump_strength=0.12,
            reference_projection=True,
        ),
        "cyan": make_textured_material(
            "M_Resistance_Cyan_Emission",
            textures["cyan"],
            textures["roughness"],
            textures["bump"],
            metallic=0.32,
            bump_strength=0.04,
            emission_strength=1.45,
        ),
        "bronze": make_textured_material(
            "M_Resistance_Bronze_Accents",
            textures["bronze"],
            textures["roughness"],
            textures["bump"],
            metallic=0.58,
            bump_strength=0.16,
            reference_projection=True,
        ),
        "olive": make_textured_material(
            "M_Resistance_Bandana_Olive",
            textures["olive"],
            textures["roughness"],
            textures["bump"],
            metallic=0.0,
            bump_strength=0.24,
        ),
    }


def dominant_bone_names(mesh):
    group_names = {group.index: group.name for group in mesh.vertex_groups}
    result = {}
    for vertex in mesh.data.vertices:
        if not vertex.groups:
            result[vertex.index] = ""
            continue
        dominant = max(vertex.groups, key=lambda group: group.weight)
        result[vertex.index] = group_names.get(dominant.group, "")
    return result


def polygon_bone(polygon, vertex_bones):
    names = [
        vertex_bones[index]
        for index in polygon.vertices
        if vertex_bones.get(index)
    ]
    return Counter(names).most_common(1)[0][0] if names else ""


def polygon_world_area(mesh, polygon):
    points = [
        mesh.matrix_world @ mesh.data.vertices[index].co
        for index in polygon.vertices
    ]
    if len(points) < 3:
        return 0.0
    area = 0.0
    for index in range(1, len(points) - 1):
        area += (
            (points[index] - points[0])
            .cross(points[index + 1] - points[0])
            .length
            * 0.5
        )
    return area


def classify_polygon(mesh, polygon, vertex_bones):
    center = mesh.matrix_world @ polygon.center
    normal = (mesh.matrix_world.to_3x3() @ polygon.normal).normalized()
    bone = polygon_bone(polygon, vertex_bones)
    x, y, z = center
    frontness = -normal.y
    world_area = polygon_world_area(mesh, polygon)

    is_bandana = (
        (1.695 <= z <= 1.735)
        or (z > 1.54 and y > 0.085 and abs(x) > 0.095)
    )
    if bone in {"Head", "neck"} and is_bandana:
        return "olive"

    side = 1.0 if x >= 0.0 else -1.0
    is_torso_recess = frontness > 0.30 and (
        (1.03 <= z <= 1.23 and abs(x) <= 0.13)
        or (1.24 <= z <= 1.45 and abs(x) <= 0.032)
        or (1.205 <= z <= 1.245 and abs(x) <= 0.23)
    )
    is_inner_limb = (
        bone in {
            "LeftArm",
            "RightArm",
            "LeftForeArm",
            "RightForeArm",
            "LeftUpLeg",
            "RightUpLeg",
            "LeftLeg",
            "RightLeg",
        }
        and normal.x * side < -0.68
        and frontness < 0.28
    )
    is_joint_band = (
        (0.55 <= z <= 0.61 and 0.15 <= abs(x) <= 0.29)
        or (0.91 <= z <= 0.97 and abs(x) <= 0.25)
        or (1.13 <= z <= 1.19 and abs(x) >= 0.27)
        or (0.15 <= z <= 0.20 and 0.18 <= abs(x) <= 0.31)
        or (0.96 <= z <= 1.00 and abs(x) >= 0.31)
        or (1.45 <= z <= 1.56 and abs(x) <= 0.13)
    )
    is_dark_bone = bone in {
        "neck",
        "LeftHand",
        "RightHand",
        "LeftFoot",
        "RightFoot",
        "LeftToeBase",
        "RightToeBase",
    }
    is_bronze_band = world_area <= 0.00033 and (
        (
            bone in {
                "LeftShoulder",
                "RightShoulder",
                "LeftArm",
                "RightArm",
            }
            and 1.37 <= z <= 1.47
            and abs(x) >= 0.22
            and (frontness > 0.18 or abs(normal.x) > 0.38)
        )
        or (
            1.13 <= z <= 1.20
            and abs(x) >= 0.27
            and frontness > 0.10
        )
        or (
            0.91 <= z <= 0.99
            and 0.12 <= abs(x) <= 0.28
            and frontness > 0.05
        )
        or (
            0.55 <= z <= 0.62
            and 0.15 <= abs(x) <= 0.29
            and frontness > 0.05
        )
        or (
            0.15 <= z <= 0.20
            and 0.18 <= abs(x) <= 0.31
            and abs(normal.x) > 0.30
        )
        or (
            bone in {"Spine01", "Spine"}
            and 1.41 <= z <= 1.46
            and 0.07 <= abs(x) <= 0.24
            and frontness > 0.28
        )
        or (
            bone in {"Hips", "Spine02"}
            and 0.94 <= z <= 1.02
            and abs(x) <= 0.085
            and frontness > 0.20
        )
    )
    if is_bronze_band:
        return "bronze"
    if is_dark_bone or is_torso_recess or is_inner_limb or is_joint_band:
        return "dark"
    return "silver"


def assign_material_zones(mesh, materials):
    mesh.data.materials.clear()
    ordered = ["silver", "dark", "cyan", "bronze", "olive"]
    for key in ordered:
        mesh.data.materials.append(materials[key])
    indices = {key: index for index, key in enumerate(ordered)}
    vertex_bones = dominant_bone_names(mesh)
    counts = Counter({key: 0 for key in ordered})
    for polygon in mesh.data.polygons:
        key = classify_polygon(mesh, polygon, vertex_bones)
        polygon.material_index = indices[key]
        counts[key] += 1
    return dict(counts)


def assign_neutral_material(mesh):
    mesh.data.materials.clear()
    material = make_principled_material(
        "M_Resistance_Neutral_Geometry_Check",
        (0.42, 0.46, 0.52),
        0.62,
        0.44,
    )
    mesh.data.materials.append(material)
    for polygon in mesh.data.polygons:
        polygon.material_index = 0


def world_bounds(mesh):
    points = [mesh.matrix_world @ vertex.co for vertex in mesh.data.vertices]
    minimum = Vector(
        tuple(min(point[axis] for point in points) for axis in range(3))
    )
    maximum = Vector(
        tuple(max(point[axis] for point in points) for axis in range(3))
    )
    return minimum, maximum


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_area_light(name, location, energy, size, color):
    data = bpy.data.lights.new(name, type="AREA")
    data.energy = energy
    data.shape = "RECTANGLE"
    data.size = size
    data.size_y = size * 0.55
    data.color = color
    light = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(light)
    light.location = location
    return light


def setup_studio(mesh):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "AgX - Medium High Contrast"
    if scene.world is None:
        scene.world = bpy.data.worlds.new("Resistance_Studio_World")
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.82, 0.84, 0.87, 1.0)
    background.inputs["Strength"].default_value = 0.62

    minimum, maximum = world_bounds(mesh)
    center = (minimum + maximum) * 0.5
    height = maximum.z - minimum.z

    camera_data = bpy.data.cameras.new("ResistanceReviewCamera")
    camera = bpy.data.objects.new("ResistanceReviewCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = (
        center.x,
        minimum.y - max(3.4, height * 2.15),
        minimum.z + height * 0.54,
    )
    camera_data.lens = 58.0
    look_at(camera, (center.x, center.y, minimum.z + height * 0.52))
    scene.camera = camera

    key = add_area_light(
        "Resistance_Key",
        (center.x - 2.2, minimum.y - 2.4, maximum.z + 1.8),
        620.0,
        4.0,
        (1.0, 0.96, 0.90),
    )
    look_at(key, center)
    fill = add_area_light(
        "Resistance_Fill",
        (center.x + 2.4, minimum.y - 0.6, minimum.z + height * 0.78),
        340.0,
        3.4,
        (0.66, 0.82, 1.0),
    )
    look_at(fill, center)
    rim = add_area_light(
        "Resistance_Rim",
        (center.x, maximum.y + 2.0, maximum.z + 0.8),
        460.0,
        3.0,
        (0.58, 0.80, 1.0),
    )
    look_at(rim, center)

    floor_material = make_principled_material(
        "M_Resistance_Studio_Floor",
        (0.72, 0.75, 0.79),
        0.0,
        0.72,
    )
    bpy.ops.mesh.primitive_plane_add(
        size=12.0,
        location=(center.x, center.y, minimum.z - 0.002),
    )
    floor = bpy.context.object
    floor.name = "Resistance_Studio_Floor"
    floor.data.materials.append(floor_material)
    return {
        "minimum": minimum,
        "maximum": maximum,
        "center": center,
        "height": height,
        "camera": camera,
    }


def position_camera(studio, yaw_degrees, zoom=1.0):
    angle = math.radians(yaw_degrees)
    center = studio["center"]
    minimum = studio["minimum"]
    height = studio["height"]
    distance = max(3.4, height * 2.15) / zoom
    horizontal = Vector((math.sin(angle), -math.cos(angle), 0.0))
    camera = studio["camera"]
    camera.location = (
        center.x + horizontal.x * distance,
        center.y + horizontal.y * distance,
        minimum.z + height * 0.54,
    )
    look_at(
        camera,
        (center.x, center.y, minimum.z + height * 0.52),
    )


def render(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def run_neutral():
    clear_scene()
    mesh, armature = import_source()
    signature_before = geometry_signature(mesh, armature)
    assign_neutral_material(mesh)
    signature_after = geometry_signature(mesh, armature)
    if signature_before != signature_after:
        raise RuntimeError(
            "Neutral material assignment changed Resistance geometry."
        )
    write_json(SOURCE_SIGNATURE, signature_before)
    setup_studio(mesh)
    render(NEUTRAL_RENDER)
    print("Resistance neutral render:", NEUTRAL_RENDER)
    print("Resistance geometry signature:", SOURCE_SIGNATURE)


def run_final():
    clear_scene()
    mesh, armature = import_source()
    signature_before = geometry_signature(mesh, armature)
    textures = create_textures()
    materials = create_materials(textures)
    material_face_counts = assign_material_zones(mesh, materials)
    signature_after_materials = geometry_signature(mesh, armature)
    if signature_before != signature_after_materials:
        raise RuntimeError(
            "Resistance material assignment changed source geometry."
        )

    studio = setup_studio(mesh)
    renders = [
        ("01_front_resistance_reference_match.png", -18.0, 1.0),
        ("02_side_resistance_current_model_material.png", 90.0, 1.0),
        ("04_three_quarter_current_model_material.png", 34.0, 1.0),
        ("05_close_current_model_color_application.png", -18.0, 1.23),
        ("07_back_current_model_material.png", 180.0, 1.0),
    ]
    for file_name, yaw, zoom in renders:
        position_camera(studio, yaw, zoom)
        render(RENDER_DIR / file_name)

    signature_before_save = geometry_signature(mesh, armature)
    if signature_before != signature_before_save:
        raise RuntimeError(
            "Resistance rendering changed source geometry."
        )

    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    shutil.copy2(SOURCE_FBX, UNCHANGED_FBX)
    if sha256(SOURCE_FBX) != sha256(UNCHANGED_FBX):
        raise RuntimeError("The unchanged Resistance FBX copy hash differs.")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND))
    signature_after_save = geometry_signature(mesh, armature)
    if signature_before != signature_after_save:
        raise RuntimeError(
            "Saving the Resistance sample blend changed source geometry."
        )

    report = {
        "result": "PASS",
        "source": signature_before,
        "after_material_assignment": signature_after_materials,
        "before_blend_save": signature_before_save,
        "after_blend_save": signature_after_save,
        "source_fbx_sha256": sha256(SOURCE_FBX),
        "unchanged_fbx_sha256": sha256(UNCHANGED_FBX),
        "sample_blend_sha256": sha256(OUTPUT_BLEND),
        "material_face_counts": material_face_counts,
        "geometry_changed": False,
        "allowed_changes": [
            "material_slots",
            "polygon_material_indices",
            "material_node_masks",
            "texture_images",
            "studio_lights",
            "review_camera",
            "review_floor",
        ],
        "prohibited_changes_confirmed_absent": [
            "vertex_changes",
            "edge_changes",
            "polygon_changes",
            "bone_changes",
            "uv_changes",
            "shape_keys",
            "source_mesh_modifiers",
            "model_part_additions",
            "displacement",
            "geometry_nodes",
        ],
        "unity_runtime_applied": False,
    }
    write_json(INVARIANCE_REPORT, report)
    print("Resistance final renders:", RENDER_DIR)
    print("Resistance textures:", TEXTURE_DIR)
    print("Resistance sample blend:", OUTPUT_BLEND)
    print("Resistance invariance report:", INVARIANCE_REPORT)


def run_verify_saved():
    mesh = bpy.data.objects.get("char1")
    armature = bpy.data.objects.get("Armature")
    if mesh is None or mesh.type != "MESH":
        raise RuntimeError("Saved Resistance mesh char1 is missing.")
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("Saved Resistance Armature is missing.")

    saved_model_meshes = sorted(
        obj.name
        for obj in bpy.data.objects
        if obj.type == "MESH" and obj.name != "Resistance_Studio_Floor"
    )
    if saved_model_meshes != ["char1"]:
        raise RuntimeError(
            "Saved Resistance sample contains added model mesh objects: "
            + ", ".join(saved_model_meshes)
        )
    if any(modifier.type == "NODES" for modifier in mesh.modifiers):
        raise RuntimeError(
            "Saved Resistance mesh contains a Geometry Nodes modifier."
        )

    report = json.loads(INVARIANCE_REPORT.read_text(encoding="utf-8"))
    saved_signature = geometry_signature(mesh, armature)
    if saved_signature != report["source"]:
        raise RuntimeError(
            "Reopened Resistance sample blend differs from source geometry."
        )
    report["reopened_saved_blend"] = saved_signature
    report["saved_blend_reopen_verified"] = True
    report["saved_model_mesh_objects"] = saved_model_meshes
    report["saved_model_part_additions"] = False
    report["saved_geometry_nodes"] = False
    write_json(INVARIANCE_REPORT, report)
    print("Resistance saved blend reopen geometry: PASS")
    print("Resistance geometry SHA-256:", saved_signature["geometry_sha256"])


def main():
    args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    mode = args[0] if args else "neutral"
    if mode == "neutral":
        run_neutral()
    elif mode == "final":
        run_final()
    elif mode == "verify-saved":
        run_verify_saved()
    else:
        raise ValueError("Unsupported mode: " + mode)


if __name__ == "__main__":
    main()
