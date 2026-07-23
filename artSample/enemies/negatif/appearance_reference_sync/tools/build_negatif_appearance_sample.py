import bpy
import bmesh
import hashlib
import json
import math
import shutil
import struct
from collections import Counter
from pathlib import Path

import numpy as np
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SOURCE_FBX = PROJECT_ROOT / "enemies model" / "négatif.fbx"
REFERENCE_IMAGE = PROJECT_ROOT / "image" / "négatif(네거티프).png"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / "negatif" / "appearance_reference_sync"
SOURCE_DIR = SAMPLE_ROOT / "source"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
RENDER_DIR = SAMPLE_ROOT / "renders"
BLENDER_DIR = SAMPLE_ROOT / "blender"
EXPORT_DIR = SAMPLE_ROOT / "exports"
TOOLS_DIR = SAMPLE_ROOT / "tools"

BLEND_PATH = BLENDER_DIR / "Negatif_Appearance_ReferenceSync.blend"
GLB_PATH = EXPORT_DIR / "Negatif_Appearance_ReferenceSync.glb"
SOURCE_COPY_PATH = SOURCE_DIR / "Negatif_Source_Unmodified.fbx"
REFERENCE_COPY_PATH = SOURCE_DIR / "negatif_reference.png"

TEXTURE_SIZE = 512
RENDER_WIDTH = 1408
RENDER_HEIGHT = 768

MATERIAL_SPECS = {
    "Negatif_Worn_Bronze": {
        "prefix": "negatif_worn_bronze",
        "base": (0.290, 0.250, 0.215),
        "roughness": 0.34,
        "metallic": 0.84,
        "bump_strength": 0.38,
        "seed": 3101,
        "kind": "armor",
    },
    "Negatif_Dark_Mechanism": {
        "prefix": "negatif_dark_mechanism",
        "base": (0.032, 0.036, 0.042),
        "roughness": 0.25,
        "metallic": 0.98,
        "bump_strength": 0.20,
        "seed": 3102,
        "kind": "metal",
    },
    "Negatif_Canvas_Sack": {
        "prefix": "negatif_canvas",
        "base": (0.420, 0.350, 0.255),
        "roughness": 0.92,
        "metallic": 0.02,
        "bump_strength": 0.42,
        "seed": 3103,
        "kind": "canvas",
    },
    "Negatif_Leather_Strap": {
        "prefix": "negatif_leather",
        "base": (0.130, 0.042, 0.014),
        "roughness": 0.84,
        "metallic": 0.01,
        "bump_strength": 0.45,
        "seed": 3104,
        "kind": "leather",
    },
    "Negatif_Copper_Accent": {
        "prefix": "negatif_copper_accent",
        "base": (0.315, 0.155, 0.095),
        "roughness": 0.32,
        "metallic": 0.76,
        "bump_strength": 0.13,
        "seed": 3105,
        "kind": "metal",
    },
    "Negatif_Amber_Eye": {
        "prefix": "negatif_amber_eye",
        "base": (0.95, 0.21, 0.012),
        "roughness": 0.18,
        "metallic": 0.12,
        "bump_strength": 0.05,
        "seed": 3106,
        "kind": "eye",
        "emission": (1.0, 0.16, 0.005, 5.0),
    },
}

LEG_GROUPS = {
    "backleg",
    "backleg0",
    "backleg1",
    "backleg2",
    "R_backleg",
    "R_backleg0",
    "R_backleg1",
    "R_backleg2",
    "frontleg",
    "frontleg0",
    "frontleg1",
    "frontleg2",
    "R_frontleg",
    "R_frontleg0",
    "R_frontleg1",
    "R_frontleg2",
}
TAIL_GROUPS = {"tail", "tailstart", "tail1", "tail2", "tail3"}
STRAP_CENTERS_Z = (-0.060, -0.009, 0.055)


def ensure_directories():
    for path in (SOURCE_DIR, TEXTURE_DIR, RENDER_DIR, BLENDER_DIR, EXPORT_DIR, TOOLS_DIR):
        path.mkdir(parents=True, exist_ok=True)


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def geometry_signature(mesh):
    digest = hashlib.sha256()
    for vertex in mesh.vertices:
        digest.update(struct.pack("<3d", vertex.co.x, vertex.co.y, vertex.co.z))
    for polygon in mesh.polygons:
        digest.update(struct.pack("<I", len(polygon.vertices)))
        for index in polygon.vertices:
            digest.update(struct.pack("<I", index))
    return digest.hexdigest().upper()


def local_bounds(mesh):
    coordinates = [vertex.co for vertex in mesh.vertices]
    minimum = [min(co[axis] for co in coordinates) for axis in range(3)]
    maximum = [max(co[axis] for co in coordinates) for axis in range(3)]
    return {
        "min": [round(value, 9) for value in minimum],
        "max": [round(value, 9) for value in maximum],
        "size": [round(maximum[axis] - minimum[axis], 9) for axis in range(3)],
    }


def image_texture_data(base, roughness, seed, kind):
    size = TEXTURE_SIZE
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:size, 0:size].astype(np.float32)
    u = x / float(size)
    v = y / float(size)
    fine = rng.random((size, size), dtype=np.float32) - 0.5
    broad = (
        np.sin(u * math.tau * 3.1 + np.sin(v * math.tau * 1.7)) * 0.5
        + np.sin(v * math.tau * 4.3 + 0.4) * 0.5
    )

    if kind == "armor":
        # Broad panel grooves, worn edges, and paired raised rivets give the
        # existing body surface a mechanical plate read without new geometry.
        panel_u = np.mod(u * 4.0, 1.0)
        panel_v = np.mod(v * 3.0, 1.0)
        edge_u = np.minimum(panel_u, 1.0 - panel_u)
        edge_v = np.minimum(panel_v, 1.0 - panel_v)
        panel_grooves = np.maximum(
            np.clip((0.055 - edge_u) / 0.055, 0.0, 1.0),
            np.clip((0.055 - edge_v) / 0.055, 0.0, 1.0),
        )
        rivet_a = np.exp(
            -(
                (panel_u - 0.16) ** 2
                + (panel_v - 0.18) ** 2
            )
            / 0.0030
        )
        rivet_b = np.exp(
            -(
                (panel_u - 0.84) ** 2
                + (panel_v - 0.18) ** 2
            )
            / 0.0030
        )
        rivets = np.maximum(rivet_a, rivet_b)
        brushed = np.sin((v * 43.0 + np.sin(u * 9.0) * 0.12) * math.tau) * 0.5
        scratches = (
            np.sin((u * 37.0 - v * 11.0) * math.tau) > 0.992
        ).astype(np.float32)
        oxidation = np.maximum(0.0, broad * 0.50 + fine * 0.24)
        variation = (
            fine * 0.010
            + broad * 0.035
            - panel_grooves * 0.42
            + rivets * 0.38
            - oxidation * 0.045
            + scratches * 0.010
        )
        rough = np.clip(
            roughness
            + panel_grooves * 0.24
            + oxidation * 0.16
            - rivets * 0.15
            - scratches * 0.06,
            0.16,
            0.82,
        )
        bump = np.clip(
            0.5
            - panel_grooves * 0.42
            + rivets * 0.55
            + fine * 0.006
            + scratches * 0.008,
            0.0,
            1.0,
        )
    elif kind == "canvas":
        weave_a = np.sin(u * math.tau * 118.0) * 0.5 + 0.5
        weave_b = np.sin(v * math.tau * 112.0 + 0.7) * 0.5 + 0.5
        weave = (weave_a * 0.48 + weave_b * 0.48) - 0.48
        stains = np.maximum(0.0, np.sin(u * 18.0 + np.sin(v * 9.0)) * 0.5 + broad * 0.35)
        variation = fine * 0.08 + weave * 0.12 - stains * 0.08
        rough = np.clip(roughness + fine * 0.10 + weave * 0.06, 0.68, 0.98)
        bump = np.clip(0.5 + weave * 0.50 + fine * 0.08, 0.0, 1.0)
    elif kind == "leather":
        pores = np.sin((u * 91.0 + np.sin(v * 27.0)) * math.tau) * 0.5
        scratches = (np.sin((u * 11.0 + v * 71.0) * math.tau) > 0.985).astype(np.float32)
        variation = fine * 0.07 + broad * 0.045 - scratches * 0.12
        rough = np.clip(roughness + fine * 0.08 + scratches * 0.18, 0.38, 0.86)
        bump = np.clip(0.5 + pores * 0.16 + fine * 0.08 - scratches * 0.28, 0.0, 1.0)
    elif kind == "eye":
        glow = 0.84 + 0.16 * (np.sin(u * math.tau * 3.0) * np.sin(v * math.tau * 3.0))
        variation = fine * 0.025 + glow * 0.12
        rough = np.clip(roughness + fine * 0.025, 0.12, 0.25)
        bump = np.clip(0.5 + fine * 0.025, 0.0, 1.0)
    else:
        scratch_a = (np.sin((u * 47.0 + v * 7.0) * math.tau) > 0.992).astype(np.float32)
        scratch_b = (np.sin((u * 9.0 - v * 61.0) * math.tau + 0.4) > 0.994).astype(np.float32)
        scratches = np.maximum(scratch_a, scratch_b)
        pitting = (rng.random((size, size), dtype=np.float32) > 0.975).astype(np.float32)
        oxidation = np.maximum(0.0, broad * 0.55 + fine * 0.25)
        variation = fine * 0.055 + broad * 0.045 - oxidation * 0.055 + scratches * 0.16
        rough = np.clip(roughness + oxidation * 0.14 + pitting * 0.22 - scratches * 0.12, 0.18, 0.78)
        bump = np.clip(0.5 + fine * 0.08 - pitting * 0.34 + scratches * 0.20, 0.0, 1.0)

    base_array = np.array(base, dtype=np.float32)[None, None, :]
    albedo = np.clip(base_array * (1.0 + variation[:, :, None]), 0.005, 0.98)
    return albedo, rough.astype(np.float32), bump.astype(np.float32)


def save_texture(path, rgb_or_gray, is_color):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    if rgb_or_gray.ndim == 2:
        rgb = np.repeat(rgb_or_gray[:, :, None], 3, axis=2)
    else:
        rgb = rgb_or_gray
    alpha = np.ones((rgb.shape[0], rgb.shape[1], 1), dtype=np.float32)
    rgba = np.concatenate((rgb.astype(np.float32), alpha), axis=2)
    image = bpy.data.images.new(
        name=path.stem,
        width=rgb.shape[1],
        height=rgb.shape[0],
        alpha=True,
        float_buffer=False,
    )
    image.colorspace_settings.name = "sRGB" if is_color else "Non-Color"
    image.pixels.foreach_set(rgba.reshape(-1))
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    bpy.data.images.remove(image)


def generate_textures():
    outputs = {}
    for material_name, spec in MATERIAL_SPECS.items():
        albedo, roughness, bump = image_texture_data(
            spec["base"],
            spec["roughness"],
            spec["seed"],
            spec["kind"],
        )
        prefix = spec["prefix"]
        paths = {
            "albedo": TEXTURE_DIR / f"{prefix}_albedo.png",
            "roughness": TEXTURE_DIR / f"{prefix}_roughness.png",
            "bump": TEXTURE_DIR / f"{prefix}_bump.png",
        }
        save_texture(paths["albedo"], albedo, True)
        save_texture(paths["roughness"], roughness, False)
        save_texture(paths["bump"], bump, False)
        outputs[material_name] = paths
    return outputs


def image_node(nodes, path, colorspace, label):
    image = bpy.data.images.load(str(path), check_existing=True)
    image.colorspace_settings.name = colorspace
    node = nodes.new("ShaderNodeTexImage")
    node.name = label
    node.label = label
    node.image = image
    node.interpolation = "Linear"
    return node


def create_material(name, spec, texture_paths):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    output.location = (720, 0)
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.location = (400, 0)
    shader.inputs["Metallic"].default_value = spec["metallic"]
    shader.inputs["Roughness"].default_value = spec["roughness"]
    if "IOR" in shader.inputs:
        shader.inputs["IOR"].default_value = 1.46

    albedo = image_node(nodes, texture_paths["albedo"], "sRGB", "Albedo")
    albedo.location = (-620, 180)
    roughness = image_node(nodes, texture_paths["roughness"], "Non-Color", "Roughness")
    roughness.location = (-620, -60)
    bump_texture = image_node(nodes, texture_paths["bump"], "Non-Color", "Bump")
    bump_texture.location = (-620, -310)
    bump = nodes.new("ShaderNodeBump")
    bump.location = (120, -250)
    bump.inputs["Strength"].default_value = spec["bump_strength"]
    bump.inputs["Distance"].default_value = 0.085 if spec["kind"] == "canvas" else 0.035

    links.new(albedo.outputs["Color"], shader.inputs["Base Color"])
    links.new(roughness.outputs["Color"], shader.inputs["Roughness"])
    links.new(bump_texture.outputs["Color"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])

    emission = spec.get("emission")
    if emission:
        emission_input = shader.inputs.get("Emission Color") or shader.inputs.get("Emission")
        strength_input = shader.inputs.get("Emission Strength")
        if emission_input:
            emission_input.default_value = (*emission[:3], 1.0)
        if strength_input:
            strength_input.default_value = emission[3]

    return material


def create_master_material(texture_paths):
    material = bpy.data.materials.new("Negatif_ReferenceSync_Master")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    output.location = (1550, 0)
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.location = (1260, 0)
    shader.inputs["Roughness"].default_value = 0.45
    if "IOR" in shader.inputs:
        shader.inputs["IOR"].default_value = 1.46

    masks_a = nodes.new("ShaderNodeVertexColor")
    masks_a.layer_name = "Negatif_MasksA"
    masks_a.location = (-1250, 620)
    masks_a.label = "R Canvas | G Leather | B Mechanism | A Eye"
    masks_a_split = nodes.new("ShaderNodeSeparateColor")
    masks_a_split.location = (-1020, 620)
    links.new(masks_a.outputs["Color"], masks_a_split.inputs["Color"])

    masks_b = nodes.new("ShaderNodeVertexColor")
    masks_b.layer_name = "Negatif_MasksB"
    masks_b.location = (-1250, 430)
    masks_b.label = "R Copper"
    masks_b_split = nodes.new("ShaderNodeSeparateColor")
    masks_b_split.location = (-1020, 430)
    links.new(masks_b.outputs["Color"], masks_b_split.inputs["Color"])

    masks = {
        "Negatif_Canvas_Sack": masks_a_split.outputs["Red"],
        "Negatif_Leather_Strap": masks_a_split.outputs["Green"],
        "Negatif_Dark_Mechanism": masks_a_split.outputs["Blue"],
        "Negatif_Amber_Eye": masks_a.outputs["Alpha"],
        "Negatif_Copper_Accent": masks_b_split.outputs["Red"],
    }
    # Project the armor texture coherently over the existing body bounds.
    # Other materials keep the source sample's UV projection.
    armor_coordinates = nodes.new("ShaderNodeTexCoord")
    armor_coordinates.location = (-1260, 180)
    armor_mapping = nodes.new("ShaderNodeMapping")
    armor_mapping.location = (-1050, 180)
    armor_mapping.vector_type = "POINT"
    armor_mapping.inputs["Scale"].default_value = (1.15, 1.15, 1.15)
    links.new(armor_coordinates.outputs["Generated"], armor_mapping.inputs["Vector"])
    mix_order = [
        "Negatif_Dark_Mechanism",
        "Negatif_Canvas_Sack",
        "Negatif_Leather_Strap",
        "Negatif_Copper_Accent",
        "Negatif_Amber_Eye",
    ]

    texture_nodes = {}
    for row, name in enumerate(MATERIAL_SPECS):
        texture_nodes[name] = {}
        for column, channel in enumerate(("albedo", "roughness", "bump")):
            node = image_node(
                nodes,
                texture_paths[name][channel],
                "sRGB" if channel == "albedo" else "Non-Color",
                f"{name}_{channel}",
            )
            node.location = (-780 + column * 20, 220 - row * 180)
            if name == "Negatif_Worn_Bronze":
                node.projection = "BOX"
                node.projection_blend = 0.18
                links.new(armor_mapping.outputs["Vector"], node.inputs["Vector"])
            texture_nodes[name][channel] = node

    def cascade(channel, start_y):
        current = texture_nodes["Negatif_Worn_Bronze"][channel].outputs["Color"]
        x = -360
        for index, name in enumerate(mix_order):
            mix = nodes.new("ShaderNodeMixRGB")
            mix.blend_type = "MIX"
            mix.location = (x + index * 230, start_y)
            mix.label = f"{channel}: {name}"
            links.new(masks[name], mix.inputs["Fac"])
            links.new(current, mix.inputs[1])
            links.new(texture_nodes[name][channel].outputs["Color"], mix.inputs[2])
            current = mix.outputs["Color"]
        return current

    albedo_output = cascade("albedo", 300)
    roughness_output = cascade("roughness", 20)
    bump_output = cascade("bump", -280)

    metallic_nodes = {}
    for index, name in enumerate(MATERIAL_SPECS):
        value = MATERIAL_SPECS[name]["metallic"]
        node = nodes.new("ShaderNodeRGB")
        node.location = (-430, -620 - index * 80)
        node.outputs["Color"].default_value = (value, value, value, 1.0)
        metallic_nodes[name] = node
    metallic_current = metallic_nodes["Negatif_Worn_Bronze"].outputs["Color"]
    for index, name in enumerate(mix_order):
        mix = nodes.new("ShaderNodeMixRGB")
        mix.blend_type = "MIX"
        mix.location = (-100 + index * 210, -610)
        links.new(masks[name], mix.inputs["Fac"])
        links.new(metallic_current, mix.inputs[1])
        links.new(metallic_nodes[name].outputs["Color"], mix.inputs[2])
        metallic_current = mix.outputs["Color"]

    bump = nodes.new("ShaderNodeBump")
    bump.location = (1010, -260)
    bump.inputs["Strength"].default_value = 0.42
    bump.inputs["Distance"].default_value = 0.070

    # Add a raised plateau at each leather-mask boundary. This gives the three
    # straps a readable thickness without adding or deforming model geometry.
    strap_height = nodes.new("ShaderNodeMath")
    strap_height.operation = "MULTIPLY"
    strap_height.location = (760, -420)
    strap_height.inputs[1].default_value = 0.75
    links.new(masks["Negatif_Leather_Strap"], strap_height.inputs[0])
    raised_bump = nodes.new("ShaderNodeMath")
    raised_bump.operation = "ADD"
    raised_bump.location = (990, -420)
    links.new(bump_output, raised_bump.inputs[0])
    links.new(strap_height.outputs[0], raised_bump.inputs[1])
    links.new(raised_bump.outputs[0], bump.inputs["Height"])

    eye_emission = nodes.new("ShaderNodeMixRGB")
    eye_emission.blend_type = "MIX"
    eye_emission.location = (1010, 190)
    eye_emission.inputs[1].default_value = (0.0, 0.0, 0.0, 1.0)
    eye_emission.inputs[2].default_value = (1.0, 0.085, 0.002, 1.0)
    links.new(masks["Negatif_Amber_Eye"], eye_emission.inputs["Fac"])

    links.new(albedo_output, shader.inputs["Base Color"])
    links.new(roughness_output, shader.inputs["Roughness"])
    links.new(metallic_current, shader.inputs["Metallic"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    emission_input = shader.inputs.get("Emission Color") or shader.inputs.get("Emission")
    if emission_input:
        links.new(eye_emission.outputs["Color"], emission_input)
    if shader.inputs.get("Emission Strength"):
        shader.inputs["Emission Strength"].default_value = 1.5
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def vertex_group_weights(obj, vertex_index):
    names = {group.index: group.name for group in obj.vertex_groups}
    return {
        names.get(assignment.group, str(assignment.group)): assignment.weight
        for assignment in obj.data.vertices[vertex_index].groups
    }


def polygon_group_weight(obj, polygon, group_names):
    total = 0.0
    for vertex_index in polygon.vertices:
        weights = vertex_group_weights(obj, vertex_index)
        total += sum(weights.get(name, 0.0) for name in group_names)
    return total / max(1, len(polygon.vertices))


def smoothstep(edge0, edge1, value):
    if edge0 == edge1:
        return 0.0
    value = max(0.0, min(1.0, (value - edge0) / (edge1 - edge0)))
    return value * value * (3.0 - 2.0 * value)


def smooth_band(value, center, half_width, feather):
    distance = abs(value - center)
    return 1.0 - smoothstep(half_width, half_width + feather, distance)


def apply_mechanical_armor_modeling(obj, master_material):
    """Build clean beveled armor shells and join them to the skinned body."""

    def add_prism(name, inner, outer):
        count = len(inner)
        vertices = inner + outer
        faces = [
            tuple(reversed(range(count))),
            tuple(range(count, count * 2)),
        ]
        for index in range(count):
            next_index = (index + 1) % count
            faces.append((index, next_index, next_index + count, index + count))
        mesh = bpy.data.meshes.new(f"{name}_Mesh")
        mesh.from_pydata(vertices, [], faces)
        mesh.update(calc_edges=True)
        part = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(part)
        return part

    def bevel_part(part, width):
        bpy.context.view_layer.objects.active = part
        part.select_set(True)
        modifier = part.modifiers.new("ManufacturedEdgeBevel", "BEVEL")
        modifier.width = width
        modifier.segments = 2
        modifier.limit_method = "ANGLE"
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        part.select_set(False)

    def configure_part(part, bone_name, mechanism=False, smooth=False):
        # All profiles are authored in the imported mesh's local coordinates.
        # Match its FBX transform before joining so inverse-scale conversion
        # cannot enlarge or detach the new armor assembly.
        part.matrix_world = obj.matrix_world.copy()
        part.data.materials.append(master_material)
        part.data.uv_layers.new(name="Negatif_MaterialUV")
        masks_a = part.data.color_attributes.new(
            name="Negatif_MasksA",
            type="FLOAT_COLOR",
            domain="CORNER",
        )
        masks_b = part.data.color_attributes.new(
            name="Negatif_MasksB",
            type="FLOAT_COLOR",
            domain="CORNER",
        )
        mask_a_color = (0.0, 0.0, 1.0, 0.0) if mechanism else (0.0, 0.0, 0.0, 0.0)
        for entry in masks_a.data:
            entry.color = mask_a_color
        for entry in masks_b.data:
            entry.color = (0.0, 0.0, 0.0, 1.0)
        group = part.vertex_groups.new(name=bone_name)
        group.add(list(range(len(part.data.vertices))), 1.0, "REPLACE")
        for polygon in part.data.polygons:
            polygon.use_smooth = smooth

    parts = []
    operations = []

    head_profiles = [
        (
            "Snout",
            [
                (0.021, 0.092, 0.238),
                (0.026, 0.130, 0.260),
                (0.034, 0.132, 0.207),
                (0.042, 0.104, 0.178),
            ],
        ),
        (
            "Brow",
            [
                (0.030, 0.184, 0.224),
                (0.039, 0.200, 0.208),
                (0.048, 0.200, 0.178),
                (0.043, 0.182, 0.186),
            ],
        ),
        (
            "Cheek",
            [
                (0.041, 0.102, 0.174),
                (0.047, 0.151, 0.182),
                (0.062, 0.188, 0.147),
                (0.066, 0.126, 0.126),
            ],
        ),
    ]
    chest_profile = [
        (0.084, 0.094, 0.108),
        (0.090, 0.169, 0.110),
        (0.100, 0.180, 0.078),
        (0.115, 0.145, 0.052),
        (0.105, 0.094, 0.055),
    ]
    for sign, suffix in ((-1.0, "L"), (1.0, "R")):
        for panel_name, profile in head_profiles:
            inner = [(sign * x, y, z) for x, y, z in profile]
            outer = [(sign * (x + 0.0028), y, z) for x, y, z in profile]
            part = add_prism(
                f"Negatif_Head{panel_name}Armor_{suffix}",
                inner,
                outer,
            )
            bevel_part(part, 0.0008)
            configure_part(part, "head", smooth=False)
            parts.append(part)

        inner = [(sign * x, y, z) for x, y, z in chest_profile]
        outer = [(sign * (x + 0.0032), y, z) for x, y, z in chest_profile]
        part = add_prism(f"Negatif_ChestArmor_{suffix}", inner, outer)
        bevel_part(part, 0.0009)
        configure_part(part, "chest", smooth=False)
        parts.append(part)

    top_inner = [
        (-0.024, 0.169, 0.225),
        (0.024, 0.169, 0.225),
        (0.034, 0.195, 0.190),
        (0.030, 0.190, 0.150),
        (-0.030, 0.190, 0.150),
        (-0.034, 0.195, 0.190),
    ]
    top_outer = [(x, y + 0.0045, z) for x, y, z in top_inner]
    top_plate = add_prism("Negatif_HeadTopArmor", top_inner, top_outer)
    bevel_part(top_plate, 0.0011)
    configure_part(top_plate, "head", smooth=False)
    parts.append(top_plate)

    for sign, suffix in ((-1.0, "L"), (1.0, "R")):
        for label, location, radius, bone_name in (
            ("HeadJoint", (sign * 0.061, 0.158, 0.151), 0.019, "head"),
            ("ChestJoint", (sign * 0.119, 0.137, 0.075), 0.018, "chest"),
        ):
            bpy.ops.mesh.primitive_torus_add(
                major_radius=radius,
                minor_radius=0.0036,
                major_segments=20,
                minor_segments=6,
                location=location,
                rotation=(0.0, math.radians(90.0), 0.0),
            )
            ring = bpy.context.object
            ring.name = f"Negatif_{label}_{suffix}"
            bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
            configure_part(ring, bone_name, smooth=True)
            parts.append(ring)

    for index, (y_position, z_position) in enumerate(
        ((0.195, 0.164), (0.198, 0.182), (0.190, 0.200))
    ):
        bpy.ops.mesh.primitive_cube_add(
            location=(0.0, y_position + 0.0045, z_position),
            scale=(0.021, 0.0018, 0.0032),
        )
        vent = bpy.context.object
        vent.name = f"Negatif_HeadVent_{index + 1}"
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=True)
        bevel_part(vent, 0.0007)
        configure_part(vent, "head", mechanism=True, smooth=False)
        parts.append(vent)

    vertices_before = len(obj.data.vertices)
    polygons_before = len(obj.data.polygons)
    part_names = [part.name for part in parts]
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.join()
    obj.data.materials.clear()
    obj.data.materials.append(master_material)
    for polygon in obj.data.polygons:
        polygon.material_index = 0
    obj.data.update(calc_edges=True)
    degenerate_faces = [
        polygon
        for polygon in obj.data.polygons
        if polygon.area < 1.0e-10
    ]
    degenerate_faces_removed = len(degenerate_faces)
    if degenerate_faces:
        degenerate_face_indices = {
            polygon.index
            for polygon in degenerate_faces
        }
        editable_mesh = bmesh.new()
        editable_mesh.from_mesh(obj.data)
        editable_mesh.faces.ensure_lookup_table()
        removable_faces = [
            editable_mesh.faces[index]
            for index in sorted(degenerate_face_indices)
        ]
        bmesh.ops.delete(
            editable_mesh,
            geom=removable_faces,
            context="FACES_ONLY",
        )
        editable_mesh.to_mesh(obj.data)
        editable_mesh.free()
        obj.data.update(calc_edges=True)
    bpy.context.view_layer.update()

    operations.append(
        {
            "name": "joined_beveled_armor_assembly",
            "parts": part_names,
            "vertices_added": len(obj.data.vertices) - vertices_before,
            "polygons_added": len(obj.data.polygons) - polygons_before,
            "head_side_plates": 6,
            "chest_side_plates": 2,
            "head_top_plates": 1,
            "joint_rings": 4,
            "head_vents": 3,
            "degenerate_faces_removed": degenerate_faces_removed,
        }
    )
    return operations


def count_surface_masks(obj):
    masks_a = obj.data.color_attributes["Negatif_MasksA"]
    masks_b = obj.data.color_attributes["Negatif_MasksB"]
    counts = Counter()
    for index in range(len(obj.data.loops)):
        mask_a = masks_a.data[index].color
        mask_b = masks_b.data[index].color
        canvas, leather, mechanism, eye = mask_a
        copper = mask_b[0]
        if canvas > 0.25:
            counts["Negatif_Canvas_Sack"] += 1
        if leather > 0.25:
            counts["Negatif_Leather_Strap"] += 1
        if mechanism > 0.25:
            counts["Negatif_Dark_Mechanism"] += 1
        if eye > 0.25:
            counts["Negatif_Amber_Eye"] += 1
        if copper > 0.25:
            counts["Negatif_Copper_Accent"] += 1
        if max(canvas, leather, mechanism, eye, copper) <= 0.25:
            counts["Negatif_Worn_Bronze"] += 1
    return counts


def assign_surface_masks(obj, master_material):
    obj.data.materials.clear()
    obj.data.materials.append(master_material)
    for polygon in obj.data.polygons:
        polygon.material_index = 0
        # Blender does not reproduce this FBX's imported split normals cleanly;
        # flat presentation shading keeps the low-poly mechanical planes legible
        # without changing vertices, faces, weights, or the rig.
        polygon.use_smooth = False

    for name in ("Negatif_MasksA", "Negatif_MasksB"):
        existing = obj.data.color_attributes.get(name)
        if existing:
            obj.data.color_attributes.remove(existing)
    masks_a = obj.data.color_attributes.new(
        name="Negatif_MasksA",
        type="FLOAT_COLOR",
        domain="CORNER",
    )
    masks_b = obj.data.color_attributes.new(
        name="Negatif_MasksB",
        type="FLOAT_COLOR",
        domain="CORNER",
    )

    # These four source polygons are the two compact, outward-facing eye plates
    # on each side of the head. Per-polygon corners prevent glow interpolation
    # from spreading across the much larger cheek and brow triangles.
    eye_polygon_indices = {
        polygon.index
        for polygon in obj.data.polygons
        if 0.027 < abs(polygon.center.x) < 0.036
        and 0.156 < polygon.center.y < 0.168
        and 0.198 < polygon.center.z < 0.215
        and abs(polygon.normal.x) > 0.75
    }
    if len(eye_polygon_indices) != 4:
        raise RuntimeError(
            f"Expected four compact Negatif eye polygons, found {len(eye_polygon_indices)}."
        )
    eye_loop_indices = {
        loop_index
        for polygon in obj.data.polygons
        if polygon.index in eye_polygon_indices
        for loop_index in polygon.loop_indices
    }

    counts = Counter()
    for loop in obj.data.loops:
        vertex = obj.data.vertices[loop.vertex_index]
        x, y, z = vertex.co.x, vertex.co.y, vertex.co.z
        weights = vertex_group_weights(obj, vertex.index)
        leg_weight = min(1.0, sum(weights.get(name, 0.0) for name in LEG_GROUPS))
        tail_weight = min(1.0, sum(weights.get(name, 0.0) for name in TAIL_GROUPS))
        armor_weight = min(
            1.0,
            weights.get("head", 0.0)
            + weights.get("chest", 0.0)
            + weights.get("Hips", 0.0),
        )

        sack_z = smoothstep(-0.135, -0.100, z) * (1.0 - smoothstep(0.090, 0.122, z))
        sack_y = smoothstep(0.092, 0.132, y)
        sack_side = 1.0 - smoothstep(0.140, 0.158, abs(x))
        canvas = sack_z * sack_y * sack_side * (1.0 - max(leg_weight, tail_weight))
        strap_band = max(
            smooth_band(z, center, 0.013, 0.006)
            for center in STRAP_CENTERS_Z
        )
        # The source skin assigns much of the lower cargo shell to leg groups.
        # Preserve those weights, but let the outer cargo-side ridges remain
        # leather so each strap visibly wraps below the canvas pouch.
        cargo_side_override = smoothstep(0.075, 0.090, abs(x))
        strap_surface = (
            sack_z
            * smoothstep(0.006, 0.011, y)
            * (1.0 - smoothstep(0.157, 0.168, abs(x)))
            * max(
                1.0 - max(leg_weight, tail_weight),
                cargo_side_override * (1.0 - tail_weight),
            )
        )
        leather = strap_surface * strap_band
        mechanism = max(
            leg_weight,
            tail_weight,
            (1.0 - smoothstep(0.032, 0.070, y)) * (1.0 - armor_weight),
        )
        eye = 1.0 if loop.index in eye_loop_indices else 0.0
        copper = (
            smoothstep(0.245, 0.258, z)
            * (1.0 - smoothstep(0.125, 0.145, y))
            * (1.0 - smoothstep(0.020, 0.040, abs(x)))
            * (1.0 - eye)
        )

        canvas = max(0.0, min(1.0, canvas))
        leather = max(0.0, min(1.0, leather))
        mechanism = max(0.0, min(1.0, mechanism))
        eye = max(0.0, min(1.0, eye))
        copper = max(0.0, min(1.0, copper))
        masks_a.data[loop.index].color = (canvas, leather, mechanism, eye)
        masks_b.data[loop.index].color = (copper, 0.0, 0.0, 1.0)

        if canvas > 0.25:
            counts["Negatif_Canvas_Sack"] += 1
        if leather > 0.25:
            counts["Negatif_Leather_Strap"] += 1
        if mechanism > 0.25:
            counts["Negatif_Dark_Mechanism"] += 1
        if eye > 0.25:
            counts["Negatif_Amber_Eye"] += 1
        if copper > 0.25:
            counts["Negatif_Copper_Accent"] += 1
        if max(canvas, leather, mechanism, eye, copper) <= 0.25:
            counts["Negatif_Worn_Bronze"] += 1

    missing = [name for name in MATERIAL_SPECS if counts[name] == 0]
    if missing:
        raise RuntimeError("Surface mask classification produced empty regions: " + ", ".join(missing))
    return counts


def create_uv(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.025)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.uv_layers.active.name = "Negatif_MaterialUV"


def world_bounds(objects):
    points = []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        if obj.type != "MESH":
            continue
        evaluated = obj.evaluated_get(depsgraph)
        evaluated_mesh = evaluated.to_mesh()
        try:
            for vertex in evaluated_mesh.vertices:
                points.append(evaluated.matrix_world @ vertex.co)
        finally:
            evaluated.to_mesh_clear()
    if not points:
        raise RuntimeError("No mesh bounds are available.")
    minimum = Vector((min(point.x for point in points), min(point.y for point in points), min(point.z for point in points)))
    maximum = Vector((max(point.x for point in points), max(point.y for point in points), max(point.z for point in points)))
    return minimum, maximum


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def create_area_light(name, location, energy, size, color):
    data = bpy.data.lights.new(name=name, type="AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    light = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(light)
    light.location = location
    look_at(light, (0.0, 0.8, 0.0))
    return light


def prepare_presentation(model_objects):
    root = bpy.data.objects.new("Negatif_PresentationRoot", None)
    bpy.context.scene.collection.objects.link(root)
    top_level = [obj for obj in model_objects if obj.parent is None]
    for obj in top_level:
        obj.parent = root

    root.scale = (500.0, 500.0, 500.0)
    bpy.context.view_layer.update()
    minimum, maximum = world_bounds(model_objects)
    center = (minimum + maximum) * 0.5
    # Blender is Z-up after FBX axis conversion. Center the horizontal X/Y
    # footprint and place the evaluated mesh's lowest Z point on the ground.
    root.location += Vector((-center.x, -center.y, -minimum.z))
    bpy.context.view_layer.update()
    return root


def setup_render_scene(model_objects):
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = RENDER_WIDTH
    scene.render.resolution_y = RENDER_HEIGHT
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass
    world = bpy.data.worlds.new("Negatif_Presentation_World")
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.93, 0.93, 0.91, 1.0)
    background.inputs["Strength"].default_value = 0.82
    scene.world = world

    minimum, maximum = world_bounds(model_objects)
    size = maximum - minimum
    center = (minimum + maximum) * 0.5
    target = Vector((center.x, center.y - size.y * 0.03, minimum.z + size.z * 0.45))

    ground_material = bpy.data.materials.new("Negatif_Presentation_Ground")
    ground_material.diffuse_color = (0.77, 0.76, 0.72, 1.0)
    ground_material.use_nodes = True
    ground_shader = ground_material.node_tree.nodes.get("Principled BSDF")
    ground_shader.inputs["Base Color"].default_value = (0.77, 0.76, 0.72, 1.0)
    ground_shader.inputs["Roughness"].default_value = 0.92

    bpy.ops.mesh.primitive_plane_add(size=max(18.0, size.y * 4.5), location=(0.0, 0.0, minimum.z - 0.004))
    ground = bpy.context.object
    ground.name = "Negatif_Presentation_Ground"
    ground.data.materials.append(ground_material)

    camera_data = bpy.data.cameras.new("Negatif_Review_Camera")
    camera = bpy.data.objects.new("Negatif_Review_Camera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    camera.data.lens = 58.0
    camera.data.sensor_width = 36.0
    camera.data.dof.use_dof = False
    scene.camera = camera

    create_area_light("Negatif_Key", (-5.5, 7.0, 6.5), 1500.0, 5.0, (1.0, 0.92, 0.84))
    create_area_light("Negatif_Fill", (5.0, 4.0, 3.0), 850.0, 4.0, (0.66, 0.78, 1.0))
    create_area_light("Negatif_Rim", (0.0, -6.5, 4.5), 1050.0, 3.5, (1.0, 0.53, 0.30))

    return camera, target, size


def render_view(camera, target, size, relative_location, filename, lens):
    scale = max(size.x, size.y, size.z)
    camera.location = Vector(target) + Vector(relative_location) * scale
    camera.data.lens = lens
    look_at(camera, target)
    bpy.context.scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def write_documents(
    source_hash,
    geometry_before,
    geometry_after,
    counts,
    bones,
    bounds,
):
    material_counts = {name: int(counts[name]) for name in MATERIAL_SPECS}
    texture_files = sorted(path.name for path in TEXTURE_DIR.glob("*.png"))
    render_files = [
        "01_reference_matched_three_quarter.png",
        "02_side.png",
        "03_front.png",
        "04_back_three_quarter.png",
        "05_reference_comparison.png",
        "06_material_texture_breakdown.png",
    ]
    manifest = {
        "enemy_id": "negatif",
        "sample": "appearance_reference_sync",
        "approval_status": "PENDING_USER_APPROVAL",
        "source_fbx": "enemies model/négatif.fbx",
        "source_sha256": source_hash,
        "reference_image": "image/négatif(네거티프).png",
        "modeling_policy": "원본 FBX와 샘플 복제본의 메시·리그·스킨 가중치를 유지하고, 재질 슬롯·UV·표면 마스크·절차적 텍스처만 추가했습니다.",
        "presentation": {
            "static_bind_mesh": True,
            "armature_modifier_retained": True,
            "armature_modifier_evaluated": False,
            "shading": "source mesh uses flat presentation shading to avoid broken imported split normals",
            "glb_note": "사용자 정의 표면 마스크 호환성 확인용 보조 산출물이며 Blender 파일과 렌더가 재질 검토 기준입니다.",
        },
        "geometry_validation": {
            "vertices_before": len(geometry_before["vertices"]),
            "vertices_after": len(geometry_after["vertices"]),
            "polygons_before": geometry_before["polygon_count"],
            "polygons_after": geometry_after["polygon_count"],
            "bones": len(bones),
            "bounds_before": bounds,
            "bounds_after": geometry_after["bounds"],
            "signature_before": geometry_before["signature"],
            "signature_after": geometry_after["signature"],
            "signature_match": geometry_before["signature"] == geometry_after["signature"],
            "intentional_modeling_change": False,
        },
        "surface_mask_corner_counts": material_counts,
        "outputs": {
            "blend": "blender/Negatif_Appearance_ReferenceSync.blend",
            "glb": "exports/Negatif_Appearance_ReferenceSync.glb",
            "source_copy": "source/Negatif_Source_Unmodified.fbx",
            "reference_copy": "source/negatif_reference.png",
            "textures": [f"textures/{name}" for name in texture_files],
            "renders": [f"renders/{name}" for name in render_files],
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

    geometry_validation = {
        "result": (
            "PASS"
            if (
                geometry_before["signature"] == geometry_after["signature"]
                and len(geometry_after["vertices"]) == len(geometry_before["vertices"])
                and geometry_after["polygon_count"] == geometry_before["polygon_count"]
            )
            else "FAIL"
        ),
        "source_sha256": source_hash,
        "source_copy_sha256": sha256(SOURCE_COPY_PATH),
        "vertices_before": len(geometry_before["vertices"]),
        "vertices_after": len(geometry_after["vertices"]),
        "polygons_before": geometry_before["polygon_count"],
        "polygons_after": geometry_after["polygon_count"],
        "geometry_signature_before": geometry_before["signature"],
        "geometry_signature_after": geometry_after["signature"],
        "bounds_before": geometry_before["bounds"],
        "bounds_after": geometry_after["bounds"],
        "bone_names": bones,
        "allowed_sample_data_changes": [
            "material_slots",
            "polygon_material_index",
            "UV:Negatif_MaterialUV",
            "ColorAttribute:Negatif_MasksA",
            "ColorAttribute:Negatif_MasksB",
            "armature_modifier:retained_but_disabled_for_static_presentation",
        ],
        "modeling_changed": False,
        "source_modeling_changed": False,
    }

    readme = f"""# 니게티프 외형 동기화 아트 샘플

## 목표

`image/négatif(네거티프).png`의 기계적인 색·재질·표면 특성을 현재 `négatif.fbx`에 맞춘 승인용 샘플입니다. 원본 FBX와 샘플 복제본의 메시·27본 리그·스킨 가중치는 수정하지 않았습니다.

## 기준 이미지에서 반영한 요소

- 머리·가슴·복부에 이어지는 패널 홈과 리벳 요철이 있는 회갈색 금속 판재 외장
- 판재 사이로 드러나는 검고 짙은 관절·다리·꼬리 내부 기계부
- 베이지·황갈색의 낡은 캔버스 화물 주머니
- 적재부의 윗면·옆면·아래 경계를 연속해서 감싸는 세 개의 짙은 적갈색 가죽 스트랩
- 구리색 코와 기계 포인트
- 볼이 아닌 머리 측면 위쪽의 작은 주황색 발광 눈
- 금속 긁힘·산화 얼룩, 캔버스 직조·먼지, 가죽 모공과 마모

## 원본 형상 보존

- 정점: {len(geometry_before["vertices"])}개 → {len(geometry_after["vertices"])}개
- 면: {geometry_before["polygon_count"]}개 → {geometry_after["polygon_count"]}개
- 본: {len(bones)}개
- 형상 서명 일치: `{geometry_before["signature"] == geometry_after["signature"]}`
- 허용된 샘플 변경: 통합 머티리얼, `Negatif_MaterialUV`, 표면 혼합 마스크 2개
- 정적 표시 방식: Armature Modifier와 리그는 보존하되, Unity의 애니메이션 비활성 정적 검토와 같은 바인드 메시를 보여주기 위해 샘플 렌더에서만 변형 평가를 끔

## 검토 순서

1. `renders/05_reference_comparison.png`
2. `renders/01_reference_matched_three_quarter.png`
3. `renders/02_side.png`, `03_front.png`, `04_back_three_quarter.png`
4. `renders/06_material_texture_breakdown.png`
5. 재질 검토 기준: `blender/Negatif_Appearance_ReferenceSync.blend`
6. 호환성 보조 산출물: `exports/Negatif_Appearance_ReferenceSync.glb` — 사용자 정의 표면 마스크는 뷰어에 따라 동일하게 표시되지 않을 수 있음

## Unity 반영 계획

사용자 승인 후 별도 승인 범위에서 `Approved Negatif Enemy Placement` 아래 7개 `Negatif_Model` 인스턴스에 이 샘플의 색 분포와 PBR 재질 의도를 재현합니다. 현재 Unity 씬과 에셋은 이번 작업에서 변경하지 않았습니다.
"""

    texture_analysis = """# 니게티프 기준 이미지 표면 분석

## 색 분포

- 몸체 외장: 천 적재부와 구분되는 중간 명도의 회갈색·갈동색 금속 판재
- 내부 기계부와 다리: 거의 검은 흑갈색 금속
- 주머니: 먼지와 얼룩이 밴 황갈색 캔버스
- 스트랩: 캔버스와 분명히 구분되는 짙은 적갈색 무광 가죽
- 포인트: 산화된 구리색
- 눈: 중심이 밝은 주황색 발광

## 표면 성질

- 몸통 금속은 원본 메시를 변형하지 않고 박스 투영 절차적 텍스처의 패널 홈, 리벳형 요철, 가장자리 마모와 금속성·거칠기 차이로 기계적인 표면을 표현합니다.
- 캔버스는 높은 거칠기와 가로·세로 직조 요철, 얼룩과 색 바램을 가집니다.
- 가죽은 중간 거칠기와 미세한 모공, 길게 난 긁힘을 가집니다.
- 관절과 꼬리 및 판재 사이 내부 기구는 본체 외장보다 어둡고 금속성이 강합니다.
- 가방 끈은 원본 메시에서 확인한 세 융기 중심 `-0.060 / -0.009 / 0.055`를 따라 윗면에서 측면과 아래 경계까지 연속되며, 캔버스보다 어둡고 거친 돌출 띠로 표현합니다. 원본 스킨의 하단 가방 면에 섞여 있는 다리 본 가중치는 유지하되, 외측 가방 면의 끈 마스크가 그 가중치 때문에 사라지지 않도록 재질 분류만 보정합니다.
- 눈은 볼 전체에 마스크를 보간하지 않고 머리 측면 위쪽의 기존 소형 폴리곤 두 장씩에만 적용합니다.

## 2D 기준에서 보이지 않는 부분

후면과 반대쪽은 새 장식을 추가하지 않고 같은 재질 분류를 대칭적으로 연장했습니다. 이는 기준 이미지에서 직접 보이지 않는 부분에 대한 최소 추론입니다.
"""

    approval = {
        "status": "PENDING_USER_APPROVAL",
        "approved_for_unity": False,
        "scope": "artSample only",
        "unity_scene_modified": False,
        "modeling_modified": False,
        "source_fbx_modified": False,
    }

    html = """<!doctype html>
<html lang="ko">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>니게티프 외형 동기화 아트 샘플</title>
<style>
body{margin:0;background:#151311;color:#eee8df;font-family:"Malgun Gothic",sans-serif}
main{max-width:1280px;margin:auto;padding:32px}
h1{font-size:30px}p{color:#cfc5b8;line-height:1.7}
.hero,.grid img{width:100%;border:1px solid #4b4037;background:#eee;box-shadow:0 14px 36px #0008}
.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px;margin-top:18px}
.tag{display:inline-block;padding:7px 11px;background:#423127;color:#f7c78f;border-radius:999px}
code{color:#ffbd73}@media(max-width:760px){.grid{grid-template-columns:1fr}}
</style>
</head>
<body><main>
<span class="tag">사용자 승인 대기 · Unity 미반영</span>
<h1>니게티프 외형 동기화 샘플</h1>
<p>원본 FBX와 샘플 복제본의 메시·리그는 그대로 유지했습니다. 절차적 패널 금속, 캔버스 주머니, 아래 경계까지 감싸는 가죽 스트랩, 어두운 내부 기계부와 주황 발광 눈을 재질·텍스처·요철만으로 표현했습니다.</p>
<img class="hero" src="renders/05_reference_comparison.png" alt="기준 이미지 비교">
<div class="grid">
<img src="renders/01_reference_matched_three_quarter.png" alt="기준 시점">
<img src="renders/02_side.png" alt="측면">
<img src="renders/03_front.png" alt="정면">
<img src="renders/04_back_three_quarter.png" alt="후면 사선">
<img src="renders/06_material_texture_breakdown.png" alt="재질 텍스처">
</div>
</main></body></html>
"""

    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")
    (SAMPLE_ROOT / "TEXTURE_ANALYSIS.md").write_text(texture_analysis, encoding="utf-8")
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(
        json.dumps(approval, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    (SAMPLE_ROOT / "GEOMETRY_VALIDATION.json").write_text(
        json.dumps(geometry_validation, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def capture_geometry(mesh):
    return {
        "vertices": [(vertex.co.x, vertex.co.y, vertex.co.z) for vertex in mesh.vertices],
        "polygon_count": len(mesh.polygons),
        "signature": geometry_signature(mesh),
        "bounds": local_bounds(mesh),
    }


def main():
    ensure_directories()
    if not SOURCE_FBX.exists():
        raise FileNotFoundError(SOURCE_FBX)
    if not REFERENCE_IMAGE.exists():
        raise FileNotFoundError(REFERENCE_IMAGE)

    source_hash_before = sha256(SOURCE_FBX)
    shutil.copy2(SOURCE_FBX, SOURCE_COPY_PATH)
    shutil.copy2(REFERENCE_IMAGE, REFERENCE_COPY_PATH)
    shutil.copy2(Path(__file__), TOOLS_DIR / "build_negatif_appearance_sample.py")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    model_objects = [obj for obj in bpy.context.scene.objects if obj.type in {"MESH", "ARMATURE"}]
    mesh_objects = [obj for obj in model_objects if obj.type == "MESH"]
    armatures = [obj for obj in model_objects if obj.type == "ARMATURE"]
    if len(mesh_objects) != 1 or len(armatures) != 1:
        raise RuntimeError(
            f"Expected one mesh and one armature, found {len(mesh_objects)} meshes and {len(armatures)} armatures."
        )

    mesh_object = mesh_objects[0]
    armature = armatures[0]
    mesh_object.name = "Negatif_AppearanceSync"
    armature.name = "Negatif_Armature"
    # Unity imports this review instance with animation disabled. Present the raw
    # bind mesh likewise: retain the rig and weights, but do not evaluate an FBX
    # action/armature deformation in this appearance-only sample.
    armature.data.pose_position = "REST"
    for modifier in mesh_object.modifiers:
        if modifier.type == "ARMATURE":
            modifier.show_viewport = False
            modifier.show_render = False
    bpy.context.view_layer.update()
    geometry_before = capture_geometry(mesh_object.data)
    bones = [bone.name for bone in armature.data.bones]

    texture_paths = generate_textures()
    master_material = create_master_material(texture_paths)
    counts = assign_surface_masks(mesh_object, master_material)
    create_uv(mesh_object)

    geometry_after = capture_geometry(mesh_object.data)
    if geometry_before["signature"] != geometry_after["signature"]:
        raise RuntimeError("Appearance-only generation unexpectedly changed the sample mesh.")
    if (
        len(mesh_object.data.vertices) != len(geometry_before["vertices"])
        or len(mesh_object.data.polygons) != geometry_before["polygon_count"]
    ):
        raise RuntimeError("Appearance-only generation unexpectedly changed mesh topology.")
    if geometry_before["bounds"] != geometry_after["bounds"]:
        raise RuntimeError("Appearance-only generation unexpectedly changed mesh bounds.")

    prepare_presentation(model_objects)
    camera, target, size = setup_render_scene(model_objects)
    render_view(
        camera,
        target,
        size,
        (1.36, -1.25, 0.76),
        "01_reference_matched_three_quarter.png",
        61.0,
    )
    render_view(camera, target, size, (1.62, 0.0, 0.60), "02_side.png", 64.0)
    render_view(camera, target, size, (0.0, -1.70, 0.52), "03_front.png", 62.0)
    render_view(camera, target, size, (-1.30, 1.26, 0.35), "04_back_three_quarter.png", 60.0)

    bpy.context.scene.render.filepath = ""
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    blend_backup = BLEND_PATH.with_suffix(".blend1")
    if blend_backup.exists():
        blend_backup.unlink()

    bpy.ops.object.select_all(action="DESELECT")
    for obj in model_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = mesh_object
    bpy.ops.export_scene.gltf(
        filepath=str(GLB_PATH),
        export_format="GLB",
        use_selection=True,
        export_animations=False,
        export_cameras=False,
        export_lights=False,
        export_materials="EXPORT",
        export_yup=True,
    )

    source_hash_after = sha256(SOURCE_FBX)
    if source_hash_before != source_hash_after or source_hash_before != sha256(SOURCE_COPY_PATH):
        raise RuntimeError("The original Negatif FBX or its sample copy hash changed.")

    write_documents(
        source_hash_before,
        geometry_before,
        geometry_after,
        counts,
        bones,
        geometry_before["bounds"],
    )
    print(
        "NEGATIF_APPEARANCE_SAMPLE_COMPLETE "
        f"SourceHash={source_hash_before} "
        f"Vertices={len(mesh_object.data.vertices)} "
        f"Polygons={len(mesh_object.data.polygons)} "
        f"IntentionalModelingChange={geometry_before['signature'] != geometry_after['signature']} "
        f"MaterialCounts={dict(counts)}"
    )


if __name__ == "__main__":
    main()
