from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[4]
SAMPLE_ROOT = ROOT / "artSample/enemies/ostinato"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
BLEND_DIR = SAMPLE_ROOT / "blender"

TEXTURE_SIZE = 1024
MATERIAL_KINDS = ("chitin", "soft_tissue", "hook_blade", "compound_eye")
MATERIAL_LABELS = {
    "chitin": "Ostinato_Chitin",
    "soft_tissue": "Ostinato_SoftTissue",
    "hook_blade": "Ostinato_HookBlade",
    "compound_eye": "Ostinato_CompoundEye",
}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def clamp01(value):
    return np.clip(value, 0.0, 1.0)


def smoothstep(edge0, edge1, value):
    x = clamp01((value - edge0) / max(edge1 - edge0, 1e-6))
    return x * x * (3.0 - 2.0 * x)


def layered_field(u, v, phase=0.0):
    broad = 0.5 + 0.5 * np.sin(u * 13.7 + np.sin(v * 8.3 + phase) * 2.1 + phase)
    medium = 0.5 + 0.5 * np.sin(u * 37.0 - v * 29.0 + np.sin(u * 9.0) * 3.0 + phase * 1.7)
    fine = 0.5 + 0.5 * np.sin(u * 113.0 + v * 97.0 + np.sin(v * 31.0) * 2.4 + phase * 2.9)
    return clamp01(broad * 0.46 + medium * 0.34 + fine * 0.20)


def blend_colors(a, b, factor):
    return a[None, None, :] * (1.0 - factor[..., None]) + b[None, None, :] * factor[..., None]


def generate_texture_fields(kind: str):
    axis = np.linspace(0.0, 1.0, TEXTURE_SIZE, endpoint=False, dtype=np.float32)
    u, v = np.meshgrid(axis, axis)
    field = layered_field(u, v, phase={"chitin": 0.2, "soft_tissue": 1.4, "hook_blade": 2.8, "compound_eye": 4.1}[kind])

    if kind == "chitin":
        dark_olive = np.array([0.105, 0.120, 0.038], dtype=np.float32)
        shell_olive = np.array([0.275, 0.300, 0.092], dtype=np.float32)
        shell_highlight = np.array([0.43, 0.37, 0.13], dtype=np.float32)
        oxidized_rust = np.array([0.31, 0.075, 0.026], dtype=np.float32)

        micro_mottle = layered_field(u * 4.7 + 0.17, v * 4.3 - 0.09, 1.3)
        speckle_field = layered_field(u * 7.2 - 0.31, v * 6.8 + 0.27, 3.8)
        rust_freckles = smoothstep(0.89, 0.975, speckle_field)
        polished_wear = smoothstep(0.76, 0.97, micro_mottle)
        pin_pits = smoothstep(
            0.92,
            0.995,
            0.5 + 0.5 * np.sin(u * 283.0 + np.sin(v * 41.0) * 2.0) * np.sin(v * 239.0 + u * 17.0),
        )

        base = blend_colors(dark_olive, shell_olive, 0.34 + micro_mottle * 0.46)
        base = base * (1.0 - polished_wear[..., None] * 0.12) + shell_highlight[None, None, :] * polished_wear[..., None] * 0.12
        base = base * (1.0 - rust_freckles[..., None] * 0.46) + oxidized_rust[None, None, :] * rust_freckles[..., None] * 0.46
        base *= 1.0 - pin_pits[..., None] * 0.16

        height = micro_mottle * 0.20 + polished_wear * 0.05 - pin_pits * 0.08
        roughness = clamp01(0.45 + (1.0 - micro_mottle) * 0.16 + rust_freckles * 0.12 + pin_pits * 0.04)
        metallic = np.zeros_like(field)
    elif kind == "soft_tissue":
        dark = np.array([0.045, 0.012, 0.008], dtype=np.float32)
        red_brown = np.array([0.19, 0.045, 0.024], dtype=np.float32)
        warm = np.array([0.32, 0.095, 0.042], dtype=np.float32)
        tissue_field = layered_field(u * 2.7, v * 3.0, 2.6)
        fibers = 0.5 + 0.5 * np.sin(v * 124.0 + np.sin(u * 31.0) * 3.4)
        pits = smoothstep(0.84, 0.98, layered_field(u * 4.1, v * 3.7, 3.6))
        base = blend_colors(dark, red_brown, smoothstep(0.14, 0.82, tissue_field))
        base = base * (1.0 - fibers[..., None] * 0.12) + warm[None, None, :] * fibers[..., None] * 0.12
        base *= 1.0 - pits[..., None] * 0.48
        height = tissue_field * 0.18 + fibers * 0.28 - pits * 0.36
        roughness = clamp01(0.56 + pits * 0.22 - fibers * 0.07)
        metallic = np.zeros_like(field)
    elif kind == "hook_blade":
        dark = np.array([0.055, 0.075, 0.075], dtype=np.float32)
        steel = np.array([0.37, 0.46, 0.47], dtype=np.float32)
        edge = np.array([0.78, 0.82, 0.78], dtype=np.float32)
        oxide = np.array([0.20, 0.085, 0.045], dtype=np.float32)
        vertical = smoothstep(0.02, 0.98, u)
        base = blend_colors(dark, steel, 0.26 + field * 0.54)
        edge_mask = smoothstep(0.78, 0.98, vertical)
        base = base * (1.0 - edge_mask[..., None] * 0.70) + edge[None, None, :] * edge_mask[..., None] * 0.70
        oxide_mask = smoothstep(0.77, 0.94, layered_field(u * 0.84, v * 1.22, 5.1)) * (1.0 - edge_mask)
        base = base * (1.0 - oxide_mask[..., None] * 0.68) + oxide[None, None, :] * oxide_mask[..., None] * 0.68
        scratches = smoothstep(0.92, 0.995, 0.5 + 0.5 * np.sin(v * 257.0 + u * 17.0))
        base += scratches[..., None] * 0.16
        height = field * 0.14 - scratches * 0.23 - oxide_mask * 0.08
        roughness = clamp01(0.20 + oxide_mask * 0.44 + scratches * 0.12)
        metallic = clamp01(0.86 - oxide_mask * 0.66)
    else:
        dark_green = np.array([0.035, 0.075, 0.052], dtype=np.float32)
        lens_green = np.array([0.16, 0.33, 0.19], dtype=np.float32)
        red = np.array([0.58, 0.075, 0.045], dtype=np.float32)
        glint = np.array([0.95, 0.72, 0.48], dtype=np.float32)
        hex_field = 0.5 + 0.5 * np.sin(u * 146.0) * np.sin(v * 126.0 + np.sin(u * 73.0))
        radial = np.sqrt((u - 0.54) ** 2 + (v - 0.48) ** 2)
        red_mask = 1.0 - smoothstep(0.07, 0.32, radial)
        glint_mask = 1.0 - smoothstep(0.015, 0.075, np.sqrt((u - 0.37) ** 2 + (v - 0.30) ** 2))
        base = blend_colors(dark_green, lens_green, 0.35 + hex_field * 0.48)
        base = base * (1.0 - red_mask[..., None] * 0.82) + red[None, None, :] * red_mask[..., None] * 0.82
        base = base * (1.0 - glint_mask[..., None]) + glint[None, None, :] * glint_mask[..., None]
        height = hex_field * 0.22 + glint_mask * 0.08
        roughness = clamp01(0.16 + (1.0 - hex_field) * 0.10)
        metallic = np.full_like(field, 0.04)

    dy, dx = np.gradient(height.astype(np.float32))
    strength = {"chitin": 13.0, "soft_tissue": 9.0, "hook_blade": 4.0, "compound_eye": 5.0}[kind]
    normal = np.dstack((-dx * strength, -dy * strength, np.ones_like(height)))
    normal /= np.maximum(np.linalg.norm(normal, axis=2, keepdims=True), 1e-6)
    normal = normal * 0.5 + 0.5
    return clamp01(base), clamp01(roughness), clamp01(metallic), clamp01(normal)


def save_image(path: Path, data: np.ndarray, is_data: bool):
    if data.ndim == 2:
        data = np.dstack((data, data, data))
    rgba = np.dstack((data, np.ones(data.shape[:2], dtype=np.float32)))
    rgba = np.flipud(rgba).astype(np.float32)
    image = bpy.data.images.new(path.stem, width=data.shape[1], height=data.shape[0], alpha=True, float_buffer=False)
    image.colorspace_settings.name = "Non-Color" if is_data else "sRGB"
    image.pixels.foreach_set(rgba.ravel())
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    bpy.data.images.remove(image)


def create_textures():
    paths = {}
    for kind in MATERIAL_KINDS:
        base, roughness, metallic, normal = generate_texture_fields(kind)
        prefix = MATERIAL_LABELS[kind]
        outputs = {
            "base_color": (TEXTURE_DIR / f"{prefix}_BaseColor.png", base, False),
            "roughness": (TEXTURE_DIR / f"{prefix}_Roughness.png", roughness, True),
            "metallic": (TEXTURE_DIR / f"{prefix}_Metallic.png", metallic, True),
            "normal": (TEXTURE_DIR / f"{prefix}_Normal.png", normal, True),
        }
        paths[kind] = {}
        for channel, (path, data, is_data) in outputs.items():
            save_image(path, data, is_data)
            paths[kind][channel] = path
    return paths


def image_node(nodes, image_path: Path, is_data: bool, location):
    node = nodes.new("ShaderNodeTexImage")
    node.image = bpy.data.images.load(str(image_path), check_existing=True)
    node.image.colorspace_settings.name = "Non-Color" if is_data else "sRGB"
    node.extension = "REPEAT"
    node.interpolation = "Linear"
    node.location = location
    return node


def create_material(kind: str, texture_paths):
    material = bpy.data.materials.new(MATERIAL_LABELS[kind])
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    output.location = (700, 0)
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.location = (430, 0)
    shader.inputs["IOR"].default_value = 1.52 if kind == "chitin" else 1.46
    if "Specular IOR Level" in shader.inputs:
        shader.inputs["Specular IOR Level"].default_value = {"chitin": 0.30, "soft_tissue": 0.34, "hook_blade": 0.48, "compound_eye": 0.72}[kind]
    if "Coat Weight" in shader.inputs:
        shader.inputs["Coat Weight"].default_value = 0.10 if kind == "chitin" else 0.0
    if "Coat Roughness" in shader.inputs:
        shader.inputs["Coat Roughness"].default_value = 0.46 if kind == "chitin" else 0.0
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])

    texcoord = nodes.new("ShaderNodeTexCoord")
    texcoord.location = (-850, 0)
    mapping = nodes.new("ShaderNodeMapping")
    mapping.location = (-650, 0)
    tile = {"chitin": 1.0, "soft_tissue": 1.25, "hook_blade": 1.0, "compound_eye": 1.0}[kind]
    mapping.inputs["Scale"].default_value = (tile, tile, tile)
    links.new(texcoord.outputs["UV"], mapping.inputs["Vector"])

    base = image_node(nodes, texture_paths["base_color"], False, (-390, 240))
    roughness = image_node(nodes, texture_paths["roughness"], True, (-390, 70))
    metallic = image_node(nodes, texture_paths["metallic"], True, (-390, -100))
    normal_texture = image_node(nodes, texture_paths["normal"], True, (-390, -280))
    for node in (base, roughness, metallic, normal_texture):
        links.new(mapping.outputs["Vector"], node.inputs["Vector"])
    links.new(metallic.outputs["Color"], shader.inputs["Metallic"])
    normal = nodes.new("ShaderNodeNormalMap")
    normal.location = (170, -250)
    normal.inputs["Strength"].default_value = {"chitin": 0.70, "soft_tissue": 0.68, "hook_blade": 0.38, "compound_eye": 0.45}[kind]
    links.new(normal_texture.outputs["Color"], normal.inputs["Color"])

    if kind == "chitin":
        generated_mapping = nodes.new("ShaderNodeMapping")
        generated_mapping.location = (-650, 520)
        generated_mapping.inputs["Scale"].default_value = (1.15, 1.15, 1.15)
        links.new(texcoord.outputs["Generated"], generated_mapping.inputs["Vector"])

        shell_noise = nodes.new("ShaderNodeTexNoise")
        shell_noise.location = (-390, 650)
        shell_noise.noise_dimensions = "3D"
        shell_noise.inputs["Scale"].default_value = 3.8
        shell_noise.inputs["Detail"].default_value = 5.0
        shell_noise.inputs["Roughness"].default_value = 0.68
        shell_noise.inputs["Distortion"].default_value = 0.16
        links.new(generated_mapping.outputs["Vector"], shell_noise.inputs["Vector"])

        shell_palette = nodes.new("ShaderNodeValToRGB")
        shell_palette.location = (-110, 650)
        ramp = shell_palette.color_ramp
        ramp.interpolation = "EASE"
        ramp.elements[0].position = 0.18
        ramp.elements[0].color = (0.012, 0.010, 0.002, 1.0)
        ramp.elements[1].position = 0.78
        ramp.elements[1].color = (0.095, 0.078, 0.012, 1.0)
        rust_element = ramp.elements.new(0.34)
        rust_element.color = (0.055, 0.009, 0.002, 1.0)
        olive_element = ramp.elements.new(0.50)
        olive_element.color = (0.026, 0.044, 0.005, 1.0)
        links.new(shell_noise.outputs["Fac"], shell_palette.inputs["Fac"])

        texture_mix = nodes.new("ShaderNodeMixRGB")
        texture_mix.location = (120, 420)
        texture_mix.blend_type = "MIX"
        texture_mix.inputs[0].default_value = 0.46
        links.new(base.outputs["Color"], texture_mix.inputs[1])
        links.new(shell_palette.outputs["Color"], texture_mix.inputs[2])

        plates = nodes.new("ShaderNodeTexVoronoi")
        plates.location = (-390, 900)
        plates.voronoi_dimensions = "3D"
        plates.feature = "DISTANCE_TO_EDGE"
        plates.distance = "EUCLIDEAN"
        plates.inputs["Scale"].default_value = 3.7
        links.new(generated_mapping.outputs["Vector"], plates.inputs["Vector"])

        seam_mask = nodes.new("ShaderNodeValToRGB")
        seam_mask.location = (-110, 900)
        seam_mask.color_ramp.interpolation = "EASE"
        seam_mask.color_ramp.elements[0].position = 0.014
        seam_mask.color_ramp.elements[0].color = (1.0, 1.0, 1.0, 1.0)
        seam_mask.color_ramp.elements[1].position = 0.064
        seam_mask.color_ramp.elements[1].color = (0.0, 0.0, 0.0, 1.0)
        links.new(plates.outputs["Distance"], seam_mask.inputs["Fac"])

        # The reference abdomen uses stacked bilateral plates that converge into
        # a downward center point. Replace the anatomy-agnostic Voronoi seams in
        # the front abdomen with a localized chevron segmentation mask.
        generated_xyz = nodes.new("ShaderNodeSeparateXYZ")
        generated_xyz.location = (-860, 1160)
        links.new(texcoord.outputs["Generated"], generated_xyz.inputs["Vector"])

        centered_x = nodes.new("ShaderNodeMath")
        centered_x.operation = "SUBTRACT"
        centered_x.location = (-650, 1220)
        centered_x.inputs[1].default_value = 0.5
        links.new(generated_xyz.outputs["X"], centered_x.inputs[0])

        abdomen_abs_x = nodes.new("ShaderNodeMath")
        abdomen_abs_x.operation = "ABSOLUTE"
        abdomen_abs_x.location = (-470, 1220)
        links.new(centered_x.outputs[0], abdomen_abs_x.inputs[0])

        abdomen_x_mask = nodes.new("ShaderNodeValToRGB")
        abdomen_x_mask.location = (-280, 1280)
        abdomen_x_mask.color_ramp.interpolation = "EASE"
        abdomen_x_mask.color_ramp.elements[0].position = 0.19
        abdomen_x_mask.color_ramp.elements[0].color = (1.0, 1.0, 1.0, 1.0)
        abdomen_x_mask.color_ramp.elements[1].position = 0.31
        abdomen_x_mask.color_ramp.elements[1].color = (0.0, 0.0, 0.0, 1.0)
        links.new(abdomen_abs_x.outputs[0], abdomen_x_mask.inputs["Fac"])

        abdomen_z_mask = nodes.new("ShaderNodeValToRGB")
        abdomen_z_mask.location = (-280, 1120)
        abdomen_z_mask.color_ramp.interpolation = "EASE"
        abdomen_z_mask.color_ramp.elements[0].position = 0.39
        abdomen_z_mask.color_ramp.elements[0].color = (0.0, 0.0, 0.0, 1.0)
        abdomen_z_mask.color_ramp.elements[1].position = 0.82
        abdomen_z_mask.color_ramp.elements[1].color = (0.0, 0.0, 0.0, 1.0)
        lower_plate = abdomen_z_mask.color_ramp.elements.new(0.445)
        lower_plate.color = (1.0, 1.0, 1.0, 1.0)
        upper_plate = abdomen_z_mask.color_ramp.elements.new(0.77)
        upper_plate.color = (1.0, 1.0, 1.0, 1.0)
        links.new(generated_xyz.outputs["Y"], abdomen_z_mask.inputs["Fac"])

        abdomen_front_mask = nodes.new("ShaderNodeValToRGB")
        abdomen_front_mask.location = (-280, 960)
        abdomen_front_mask.color_ramp.interpolation = "EASE"
        abdomen_front_mask.color_ramp.elements[0].position = 0.38
        abdomen_front_mask.color_ramp.elements[0].color = (0.0, 0.0, 0.0, 1.0)
        abdomen_front_mask.color_ramp.elements[1].position = 0.62
        abdomen_front_mask.color_ramp.elements[1].color = (1.0, 1.0, 1.0, 1.0)
        links.new(generated_xyz.outputs["Z"], abdomen_front_mask.inputs["Fac"])

        abdomen_xz_mask = nodes.new("ShaderNodeMath")
        abdomen_xz_mask.operation = "MULTIPLY"
        abdomen_xz_mask.location = (-60, 1190)
        links.new(abdomen_x_mask.outputs["Color"], abdomen_xz_mask.inputs[0])
        links.new(abdomen_z_mask.outputs["Color"], abdomen_xz_mask.inputs[1])

        abdomen_mask = nodes.new("ShaderNodeMath")
        abdomen_mask.operation = "MULTIPLY"
        abdomen_mask.location = (120, 1190)
        links.new(abdomen_xz_mask.outputs[0], abdomen_mask.inputs[0])
        links.new(abdomen_front_mask.outputs["Color"], abdomen_mask.inputs[1])

        chevron_slope = nodes.new("ShaderNodeMath")
        chevron_slope.operation = "MULTIPLY"
        chevron_slope.location = (-280, 1440)
        chevron_slope.inputs[1].default_value = 0.82
        links.new(abdomen_abs_x.outputs[0], chevron_slope.inputs[0])

        chevron_coordinate = nodes.new("ShaderNodeMath")
        chevron_coordinate.operation = "SUBTRACT"
        chevron_coordinate.location = (-60, 1440)
        links.new(generated_xyz.outputs["Y"], chevron_coordinate.inputs[0])
        links.new(chevron_slope.outputs[0], chevron_coordinate.inputs[1])

        chevron_repeat = nodes.new("ShaderNodeMath")
        chevron_repeat.operation = "MULTIPLY"
        chevron_repeat.location = (120, 1440)
        chevron_repeat.inputs[1].default_value = 17.0
        links.new(chevron_coordinate.outputs[0], chevron_repeat.inputs[0])

        chevron_fraction = nodes.new("ShaderNodeMath")
        chevron_fraction.operation = "FRACT"
        chevron_fraction.location = (300, 1440)
        links.new(chevron_repeat.outputs[0], chevron_fraction.inputs[0])

        chevron_lines = nodes.new("ShaderNodeValToRGB")
        chevron_lines.location = (480, 1440)
        chevron_lines.color_ramp.interpolation = "EASE"
        chevron_lines.color_ramp.elements[0].position = 0.0
        chevron_lines.color_ramp.elements[0].color = (1.0, 1.0, 1.0, 1.0)
        chevron_lines.color_ramp.elements[1].position = 1.0
        chevron_lines.color_ramp.elements[1].color = (1.0, 1.0, 1.0, 1.0)
        line_falloff_in = chevron_lines.color_ramp.elements.new(0.14)
        line_falloff_in.color = (0.0, 0.0, 0.0, 1.0)
        line_falloff_out = chevron_lines.color_ramp.elements.new(0.86)
        line_falloff_out.color = (0.0, 0.0, 0.0, 1.0)
        links.new(chevron_fraction.outputs[0], chevron_lines.inputs["Fac"])

        abdomen_chevrons = nodes.new("ShaderNodeMath")
        abdomen_chevrons.operation = "MULTIPLY"
        abdomen_chevrons.location = (680, 1360)
        links.new(chevron_lines.outputs["Color"], abdomen_chevrons.inputs[0])
        links.new(abdomen_mask.outputs[0], abdomen_chevrons.inputs[1])

        inverse_abdomen = nodes.new("ShaderNodeMath")
        inverse_abdomen.operation = "SUBTRACT"
        inverse_abdomen.location = (300, 1180)
        inverse_abdomen.inputs[0].default_value = 1.0
        links.new(abdomen_mask.outputs[0], inverse_abdomen.inputs[1])

        exterior_seams = nodes.new("ShaderNodeMath")
        exterior_seams.operation = "MULTIPLY"
        exterior_seams.location = (500, 1180)
        links.new(seam_mask.outputs["Color"], exterior_seams.inputs[0])
        links.new(inverse_abdomen.outputs[0], exterior_seams.inputs[1])

        final_seam_mask = nodes.new("ShaderNodeMath")
        final_seam_mask.operation = "MAXIMUM"
        final_seam_mask.location = (860, 1260)
        links.new(exterior_seams.outputs[0], final_seam_mask.inputs[0])
        links.new(abdomen_chevrons.outputs[0], final_seam_mask.inputs[1])

        chevron_height = nodes.new("ShaderNodeMath")
        chevron_height.operation = "SUBTRACT"
        chevron_height.location = (680, 1510)
        chevron_height.inputs[0].default_value = 1.0
        links.new(chevron_lines.outputs["Color"], chevron_height.inputs[1])

        plate_height = nodes.new("ShaderNodeMixRGB")
        plate_height.location = (880, 1510)
        plate_height.blend_type = "MIX"
        links.new(abdomen_mask.outputs[0], plate_height.inputs[0])
        links.new(plates.outputs["Distance"], plate_height.inputs[1])
        links.new(chevron_height.outputs[0], plate_height.inputs[2])

        abdomen_plate_tone = nodes.new("ShaderNodeValToRGB")
        abdomen_plate_tone.location = (680, 1650)
        abdomen_plate_tone.color_ramp.interpolation = "EASE"
        abdomen_plate_tone.color_ramp.elements[0].position = 0.0
        abdomen_plate_tone.color_ramp.elements[0].color = (0.010, 0.005, 0.001, 1.0)
        abdomen_plate_tone.color_ramp.elements[1].position = 1.0
        abdomen_plate_tone.color_ramp.elements[1].color = (0.010, 0.005, 0.001, 1.0)
        lower_rust = abdomen_plate_tone.color_ramp.elements.new(0.17)
        lower_rust.color = (0.048, 0.012, 0.0025, 1.0)
        plate_center = abdomen_plate_tone.color_ramp.elements.new(0.50)
        plate_center.color = (0.072, 0.086, 0.011, 1.0)
        upper_rust = abdomen_plate_tone.color_ramp.elements.new(0.83)
        upper_rust.color = (0.048, 0.012, 0.0025, 1.0)
        links.new(chevron_fraction.outputs[0], abdomen_plate_tone.inputs["Fac"])

        abdomen_tone_factor = nodes.new("ShaderNodeMath")
        abdomen_tone_factor.operation = "MULTIPLY"
        abdomen_tone_factor.location = (880, 1650)
        abdomen_tone_factor.inputs[1].default_value = 0.84
        links.new(abdomen_mask.outputs[0], abdomen_tone_factor.inputs[0])

        abdomen_surface = nodes.new("ShaderNodeMixRGB")
        abdomen_surface.location = (1060, 1580)
        abdomen_surface.blend_type = "MIX"
        links.new(abdomen_tone_factor.outputs[0], abdomen_surface.inputs[0])
        links.new(texture_mix.outputs["Color"], abdomen_surface.inputs[1])
        links.new(abdomen_plate_tone.outputs["Color"], abdomen_surface.inputs[2])

        seam_color = nodes.new("ShaderNodeMixRGB")
        seam_color.location = (350, 420)
        seam_color.blend_type = "MIX"
        seam_color.inputs[2].default_value = (0.010, 0.0025, 0.001, 1.0)
        links.new(final_seam_mask.outputs[0], seam_color.inputs[0])
        links.new(abdomen_surface.outputs["Color"], seam_color.inputs[1])
        links.new(seam_color.outputs["Color"], shader.inputs["Base Color"])

        seam_roughness = nodes.new("ShaderNodeMixRGB")
        seam_roughness.location = (120, 150)
        seam_roughness.blend_type = "MIX"
        seam_roughness.inputs[2].default_value = (0.72, 0.72, 0.72, 1.0)
        links.new(final_seam_mask.outputs[0], seam_roughness.inputs[0])
        links.new(roughness.outputs["Color"], seam_roughness.inputs[1])
        links.new(seam_roughness.outputs["Color"], shader.inputs["Roughness"])

        plate_bump = nodes.new("ShaderNodeBump")
        plate_bump.location = (370, -170)
        plate_bump.inputs["Strength"].default_value = 0.28
        plate_bump.inputs["Distance"].default_value = 0.10
        links.new(plate_height.outputs["Color"], plate_bump.inputs["Height"])
        links.new(normal.outputs["Normal"], plate_bump.inputs["Normal"])
        links.new(plate_bump.outputs["Normal"], shader.inputs["Normal"])
    else:
        links.new(base.outputs["Color"], shader.inputs["Base Color"])
        links.new(roughness.outputs["Color"], shader.inputs["Roughness"])
        links.new(normal.outputs["Normal"], shader.inputs["Normal"])
    return material


def polygon_group_scores(mesh_object, polygon):
    scores = {}
    divisor = max(len(polygon.vertices), 1)
    for vertex_index in polygon.vertices:
        for membership in mesh_object.data.vertices[vertex_index].groups:
            name = mesh_object.vertex_groups[membership.group].name
            scores[name] = scores.get(name, 0.0) + membership.weight / divisor
    return scores


def assign_material_regions(mesh_object, materials):
    mesh = mesh_object.data
    mesh.materials.clear()
    for kind in MATERIAL_KINDS:
        mesh.materials.append(materials[kind])

    counts = {kind: 0 for kind in MATERIAL_KINDS}
    index_by_kind = {kind: index for index, kind in enumerate(MATERIAL_KINDS)}
    for polygon in mesh.polygons:
        scores = polygon_group_scores(mesh_object, polygon)
        centroid = mesh_object.matrix_world @ polygon.center
        arm_score = sum(scores.get(name, 0.0) for name in ("LeftArm", "RightArm", "LeftForeArm", "RightForeArm", "LeftHand", "RightHand"))
        forearm_score = sum(scores.get(name, 0.0) for name in ("LeftForeArm", "RightForeArm"))
        head_score = scores.get("Head", 0.0)

        is_eye = head_score > 0.42 and centroid.y < -0.22 and centroid.z > 1.24 and abs(centroid.x) > 0.055
        is_blade = forearm_score > 0.34 and abs(centroid.x) > 0.43 and (centroid.y < -0.14 or centroid.z < 0.82)

        limb_joint = (
            arm_score > 0.42 and abs(centroid.x) < 0.50 and 0.76 < centroid.z < 1.03
        ) or (
            sum(scores.get(name, 0.0) for name in ("LeftUpLeg", "RightUpLeg", "LeftLeg", "RightLeg")) > 0.44
            and 0.30 < centroid.z < 0.48
        )
        neck_tissue = head_score > 0.30 and 1.08 < centroid.z < 1.18 and centroid.y < 0.02

        if is_eye:
            kind = "compound_eye"
        elif is_blade:
            kind = "hook_blade"
        elif limb_joint or neck_tissue:
            kind = "soft_tissue"
        else:
            kind = "chitin"
        polygon.material_index = index_by_kind[kind]
        counts[kind] += 1
    return counts


def rebuild_sample_uv(mesh_object):
    bpy.context.view_layer.objects.active = mesh_object
    mesh_object.select_set(True)
    sample_uv = mesh_object.data.uv_layers.get("OstinatoSampleUV")
    if sample_uv is None:
        sample_uv = mesh_object.data.uv_layers.new(name="OstinatoSampleUV")
    sample_uv.active = True
    sample_uv.active_render = True
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(58.0), island_margin=0.025, area_weight=0.20)
    bpy.ops.object.mode_set(mode="OBJECT")


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def create_studio(mesh_object):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1408
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    if scene.world is None:
        scene.world = bpy.data.worlds.new("Ostinato_Review_World")
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.46, 0.45, 0.41, 1.0)
    background.inputs["Strength"].default_value = 0.34
    scene.view_settings.look = "AgX - Medium Low Contrast"

    bpy.ops.mesh.primitive_plane_add(size=12.0, location=(0.0, 0.0, -0.070))
    floor = bpy.context.object
    floor.name = "Review_Ground"
    floor_material = bpy.data.materials.new("Review_Ground_Material")
    floor_material.diffuse_color = (0.68, 0.66, 0.59, 1.0)
    floor.data.materials.append(floor_material)

    bpy.ops.object.camera_add(location=(0.0, -4.0, 0.70))
    camera = bpy.context.object
    camera.name = "Review_Camera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 1.78
    camera.data.lens = 58
    scene.camera = camera

    lights = [
        ((-2.3, -3.0, 3.3), 460.0, 4.0, (1.0, 0.88, 0.74)),
        ((2.7, -1.0, 2.0), 250.0, 3.0, (0.74, 0.82, 1.0)),
        ((0.4, 2.8, 3.0), 320.0, 3.5, (0.86, 0.92, 1.0)),
    ]
    for index, (location, energy, size, color) in enumerate(lights, 1):
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.name = f"Review_Light_{index:02d}"
        light.data.energy = energy
        light.data.shape = "DISK"
        light.data.size = size
        light.data.color = color
        look_at(light, (0.0, 0.0, 0.75))
    return camera, floor


def evaluated_world_points(mesh_object):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh_object.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh()
    points = [evaluated.matrix_world @ vertex.co for vertex in evaluated_mesh.vertices]
    evaluated.to_mesh_clear()
    return points


def fit_orthographic_camera(camera, mesh_object, margin=1.12):
    inverse = camera.matrix_world.inverted()
    camera_points = [inverse @ point for point in evaluated_world_points(mesh_object)]
    width = max(point.x for point in camera_points) - min(point.x for point in camera_points)
    height = max(point.y for point in camera_points) - min(point.y for point in camera_points)
    aspect = bpy.context.scene.render.resolution_x / bpy.context.scene.render.resolution_y
    camera.data.ortho_scale = max(height, width / aspect) * margin


def render_view(scene, camera, mesh_object, filename, location, target=None, ortho_scale=None):
    points = evaluated_world_points(mesh_object)
    if target is None:
        target = tuple(
            (min(point[index] for point in points) + max(point[index] for point in points)) * 0.5
            for index in range(3)
        )
    camera.location = location
    look_at(camera, target)
    bpy.context.view_layer.update()
    if ortho_scale is None:
        fit_orthographic_camera(camera, mesh_object)
    else:
        camera.data.ortho_scale = ortho_scale
    scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def export_sample(mesh_object, armature_object, floor):
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    mesh_object.select_set(True)
    armature_object.select_set(True)
    bpy.context.view_layer.objects.active = mesh_object

    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_DIR / "Ostinato_CurrentModel_TexturedSample.fbx"),
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="RELATIVE",
        use_armature_deform_only=True,
    )
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_DIR / "Ostinato_CurrentModel_TexturedSample.glb"),
        export_format="GLB",
        use_selection=True,
        export_texcoords=True,
        export_normals=True,
        export_materials="EXPORT",
        export_animations=False,
    )


def write_manifest(mesh_object, armature_object, material_counts, texture_paths):
    source_hash = sha256(SOURCE_FBX)
    texture_files = []
    for kind in MATERIAL_KINDS:
        texture_files.extend(str(path.relative_to(SAMPLE_ROOT)).replace("\\", "/") for path in texture_paths[kind].values())
    manifest = {
        "enemy_id": "ostinato",
        "sample_root": "artSample/enemies/ostinato",
        "review_entry": "index.html",
        "source_model": "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx",
        "source_model_sha256": source_hash,
        "geometry_changed": False,
        "rig_changed": False,
        "source_structure": {
            "mesh": mesh_object.name,
            "vertices_blender": len(mesh_object.data.vertices),
            "polygons": len(mesh_object.data.polygons),
            "uv_layers": [layer.name for layer in mesh_object.data.uv_layers],
            "sample_uv_rebuilt": True,
            "armature": armature_object.name,
            "bones": len(armature_object.data.bones),
        },
        "material_polygon_counts": material_counts,
        "blender": ["blender/Ostinato_CurrentModel_TexturedSample.blend"],
        "exports": [
            "exports/Ostinato_CurrentModel_TexturedSample.fbx",
            "exports/Ostinato_CurrentModel_TexturedSample.glb",
        ],
        "renders": [
            "renders/01_front_blender_reference_material.png",
            "renders/02_side_blender_reference_material.png",
            "renders/03_back_blender_reference_material.png",
            "renders/04_three_quarter_blender_reference_material.png",
            "renders/05_head_blade_closeup.png",
            "renders/06_abdomen_closeup.png",
        ],
        "textures": texture_files,
        "tools": [
            "tools/build_ostinato_blender_sample.py",
            "tools/inspect_ostinato_fbx.py",
        ],
        "unity_runtime_applied": False,
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    approval = {
        "enemy_id": "ostinato",
        "sample_type": "blender_current_model_uv_pbr_texture",
        "status": "pending_user_review",
        "geometry_changed": False,
        "rig_changed": False,
        "unity_runtime_applied": False,
        "source_model": manifest["source_model"],
        "source_model_sha256": source_hash,
        "reference_images": [
            "image/ostinato(오스티나토).png",
            "image/ostinato-beside.png",
            "image/ostinato-back.png",
        ],
        "review_entry": "artSample/enemies/ostinato/index.html",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    for directory in (TEXTURE_DIR, RENDER_DIR, EXPORT_DIR, BLEND_DIR):
        directory.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX), use_anim=False)
    mesh_object = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    armature_object = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
    mesh_object.name = "Ostinato_CurrentModel_TexturedSample"
    armature_object.name = "Ostinato_CurrentModel_Armature"

    rebuild_sample_uv(mesh_object)
    texture_paths = create_textures()
    materials = {kind: create_material(kind, texture_paths[kind]) for kind in MATERIAL_KINDS}
    material_counts = assign_material_regions(mesh_object, materials)

    mesh_object["sample_source_model"] = "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx"
    mesh_object["sample_geometry_changed"] = False
    mesh_object["sample_texture_target"] = "image/ostinato front, side, back"

    camera, floor = create_studio(mesh_object)
    scene = bpy.context.scene
    scene.frame_set(0)
    review_target = (0.0, 0.0, 0.66)
    render_view(scene, camera, mesh_object, "01_front_blender_reference_material.png", (0.0, -4.0, 0.70), review_target, 3.10)
    render_view(scene, camera, mesh_object, "02_side_blender_reference_material.png", (-4.0, 0.0, 0.70), review_target, 3.10)
    render_view(scene, camera, mesh_object, "03_back_blender_reference_material.png", (0.0, 4.0, 0.70), review_target, 3.10)
    render_view(scene, camera, mesh_object, "04_three_quarter_blender_reference_material.png", (-3.0, -3.0, 1.05), review_target, 3.10)
    render_view(scene, camera, mesh_object, "05_head_blade_closeup.png", (-2.5, -3.0, 1.55), (0.0, -0.06, 1.03), 1.18)
    render_view(scene, camera, mesh_object, "06_abdomen_closeup.png", (0.0, -3.0, 0.78), (0.0, -0.08, 0.78), 0.92)

    for image in bpy.data.images:
        if image.source == "FILE" and Path(bpy.path.abspath(image.filepath)).is_relative_to(TEXTURE_DIR):
            image.pack()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_DIR / "Ostinato_CurrentModel_TexturedSample.blend"))
    export_sample(mesh_object, armature_object, floor)
    write_manifest(mesh_object, armature_object, material_counts, texture_paths)

    print(f"SourceSha256={sha256(SOURCE_FBX)}")
    print(f"MeshVertices={len(mesh_object.data.vertices)}")
    print(f"MeshPolygons={len(mesh_object.data.polygons)}")
    print(f"UvLayers={[layer.name for layer in mesh_object.data.uv_layers]}")
    print(f"ArmatureBones={len(armature_object.data.bones)}")
    print(f"MaterialPolygonCounts={material_counts}")
    print("GeometryChanged=False")
    print("RigChanged=False")
    print("UnityRuntimeApplied=False")


if __name__ == "__main__":
    main()
