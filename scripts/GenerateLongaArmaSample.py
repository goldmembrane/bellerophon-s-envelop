from __future__ import annotations

import json
import math
import random
import shutil
from datetime import datetime
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "longa_arma"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
REFERENCE_FRONT = PROJECT_ROOT / "image" / "longa arma(롱가 아르마).png"
REFERENCE_BACK = PROJECT_ROOT / "image" / "longa arma-back.png"
REFERENCE_SIDE = PROJECT_ROOT / "image" / "longa arma-beside.png"

MODEL_HEIGHT_M = 0.80
MODEL_WIDTH_M = 0.70
MODEL_DEPTH_M = 1.50

random.seed(7031)
MODEL_OBJECTS: list[bpy.types.Object] = []


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR, TEXTURE_DIR):
        path.mkdir(parents=True, exist_ok=True)


def clean_generated_files() -> None:
    for directory in (BLENDER_DIR, RENDER_DIR, EXPORT_DIR, TEXTURE_DIR):
        if not directory.exists():
            continue
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
    bsdf = mat.node_tree.nodes.get("Principled BSDF") if mat.use_nodes else None
    if bsdf is not None and name in bsdf.inputs:
        bsdf.inputs[name].default_value = value


def save_texture(path: Path, width: int, height: int, pixel_fn) -> None:
    image = bpy.data.images.new(path.stem, width=width, height=height, alpha=True)
    pixels = [0.0] * (width * height * 4)
    for y in range(height):
        v = y / max(1, height - 1)
        for x in range(width):
            u = x / max(1, width - 1)
            r, g, b, a = pixel_fn(u, v)
            index = (y * width + x) * 4
            pixels[index : index + 4] = [
                max(0.0, min(1.0, r)),
                max(0.0, min(1.0, g)),
                max(0.0, min(1.0, b)),
                max(0.0, min(1.0, a)),
            ]
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    bpy.data.images.remove(image)


def wave_noise(u: float, v: float, seed: float) -> float:
    value = math.sin(u * (22.0 + seed) + v * 18.0)
    value += 0.55 * math.sin((u - v) * (43.0 - seed) + math.sin(u * 9.0) * 2.0)
    value += 0.35 * math.sin((u + v) * (61.0 + seed * 0.3))
    return 0.5 + 0.5 * value / 1.9


def create_textures() -> dict[str, Path]:
    width = 768
    height = 768
    body_albedo = TEXTURE_DIR / "longa_arma_wet_green_albedo.png"
    body_roughness = TEXTURE_DIR / "longa_arma_wet_green_roughness.png"
    body_bump = TEXTURE_DIR / "longa_arma_wet_green_bump.png"
    blade_albedo = TEXTURE_DIR / "longa_arma_dark_blade_albedo.png"
    blade_roughness = TEXTURE_DIR / "longa_arma_dark_blade_roughness.png"
    slime_albedo = TEXTURE_DIR / "longa_arma_slime_albedo.png"

    def body_color(u: float, v: float) -> tuple[float, float, float, float]:
        n1 = wave_noise(u * 1.9, v * 2.6, 3.1)
        n2 = wave_noise(u * 7.2 + 0.1, v * 5.1, 6.4)
        pore = wave_noise(u * 18.0, v * 16.0, 1.3)
        vein = max(0.0, math.sin((u * 5.6 + v * 13.0) * math.pi) ** 10)
        dark_fissure = max(0.0, math.sin((u * 11.0 - v * 9.5) * math.pi) ** 18)
        r = 0.020 + 0.060 * n1 + 0.018 * pore + 0.045 * vein - 0.018 * dark_fissure
        g = 0.075 + 0.210 * n2 + 0.032 * pore + 0.090 * vein - 0.050 * dark_fissure
        b = 0.050 + 0.100 * n1 + 0.020 * pore + 0.030 * vein - 0.030 * dark_fissure
        r = max(0.006, min(0.180, r))
        g = max(0.030, min(0.360, g))
        b = max(0.020, min(0.240, b))
        return (r, g, b, 1.0)

    def body_rough(u: float, v: float) -> tuple[float, float, float, float]:
        n = wave_noise(u * 3.0, v * 3.0, 8.3)
        pore = wave_noise(u * 16.0, v * 14.0, 2.4)
        value = 0.58 + 0.28 * n + 0.10 * pore
        return (value, value, value, 1.0)

    def body_bump_px(u: float, v: float) -> tuple[float, float, float, float]:
        n = wave_noise(u * 3.2, v * 3.0, 11.2)
        fine = wave_noise(u * 18.0, v * 16.0, 1.7)
        striation = max(0.0, math.sin((u * 21.0 + v * 8.0) * math.pi) ** 4)
        value = 0.22 + 0.36 * n + 0.30 * fine + 0.20 * striation
        value = max(0.0, min(1.0, value))
        return (value, value, value, 1.0)

    def blade_color(u: float, v: float) -> tuple[float, float, float, float]:
        scratches = max(0.0, math.sin((u * 38.0 + v * 7.0) * math.pi) ** 10)
        edge = 0.25 if v > 0.82 or v < 0.08 else 0.0
        base = 0.025 + 0.055 * wave_noise(u, v, 5.8)
        return (base + scratches * 0.22 + edge, base + scratches * 0.20 + edge, base + scratches * 0.18 + edge, 1.0)

    def blade_rough(u: float, v: float) -> tuple[float, float, float, float]:
        n = wave_noise(u, v, 2.9)
        value = 0.24 + 0.28 * n
        return (value, value, value, 1.0)

    def unused_slime_compat_color(u: float, v: float) -> tuple[float, float, float, float]:
        return (0.0, 0.0, 0.0, 0.0)

    save_texture(body_albedo, width, height, body_color)
    save_texture(body_roughness, width, height, body_rough)
    save_texture(body_bump, width, height, body_bump_px)
    save_texture(blade_albedo, 512, 512, blade_color)
    save_texture(blade_roughness, 512, 512, blade_rough)
    save_texture(slime_albedo, 512, 512, unused_slime_compat_color)

    return {
        "body_albedo": body_albedo,
        "body_roughness": body_roughness,
        "body_bump": body_bump,
        "blade_albedo": blade_albedo,
        "blade_roughness": blade_roughness,
    }


def material_from_texture(
    name: str,
    image_path: Path,
    *,
    roughness: float,
    metallic: float = 0.0,
    alpha: float | None = None,
    bump: Path | None = None,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    image = bpy.data.images.load(str(image_path))
    tex = nodes.new(type="ShaderNodeTexImage")
    tex.image = image
    if bsdf is not None:
        links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        set_principled_input(mat, "Metallic", metallic)
        set_principled_input(mat, "Roughness", roughness)
        if alpha is not None:
            set_principled_input(mat, "Alpha", alpha)
            mat.blend_method = "BLEND"
            mat.show_transparent_back = True
        if bump is not None:
            bump_image = bpy.data.images.load(str(bump))
            bump_tex = nodes.new(type="ShaderNodeTexImage")
            bump_tex.image = bump_image
            bump_tex.image.colorspace_settings.name = "Non-Color"
            bump_node = nodes.new(type="ShaderNodeBump")
            bump_node.inputs["Strength"].default_value = 0.320
            bump_node.inputs["Distance"].default_value = 0.115
            links.new(bump_tex.outputs["Color"], bump_node.inputs["Height"])
            links.new(bump_node.outputs["Normal"], bsdf.inputs["Normal"])
    mat.diffuse_color = (0.10, 0.35, 0.20, alpha if alpha is not None else 1.0)
    return mat


def create_wet_body_material(name: str) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is not None:
        noise = nodes.new(type="ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 16.0
        noise.inputs["Detail"].default_value = 16.0
        noise.inputs["Roughness"].default_value = 0.72

        ramp = nodes.new(type="ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.18
        ramp.color_ramp.elements[0].color = (0.004, 0.018, 0.014, 1.0)
        ramp.color_ramp.elements[1].position = 1.00
        ramp.color_ramp.elements[1].color = (0.075, 0.205, 0.135, 1.0)
        mid = ramp.color_ramp.elements.new(0.50)
        mid.color = (0.020, 0.076, 0.052, 1.0)
        dark_mid = ramp.color_ramp.elements.new(0.34)
        dark_mid.color = (0.010, 0.044, 0.032, 1.0)

        bump_noise = nodes.new(type="ShaderNodeTexNoise")
        bump_noise.inputs["Scale"].default_value = 96.0
        bump_noise.inputs["Detail"].default_value = 16.0
        bump_noise.inputs["Roughness"].default_value = 0.78

        bump_node = nodes.new(type="ShaderNodeBump")
        bump_node.inputs["Strength"].default_value = 0.340
        bump_node.inputs["Distance"].default_value = 0.120

        links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
        links.new(bump_noise.outputs["Fac"], bump_node.inputs["Height"])
        links.new(bump_node.outputs["Normal"], bsdf.inputs["Normal"])

        set_principled_input(mat, "Metallic", 0.0)
        set_principled_input(mat, "Roughness", 0.84)
    mat.diffuse_color = (0.016, 0.070, 0.048, 1.0)
    return mat


def simple_material(
    name: str,
    color: tuple[float, float, float, float],
    *,
    roughness: float = 0.45,
    metallic: float = 0.0,
    alpha: float | None = None,
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
    mat.diffuse_color = color
    return mat


def register(obj: bpy.types.Object) -> bpy.types.Object:
    MODEL_OBJECTS.append(obj)
    return obj


def apply_smooth(obj: bpy.types.Object) -> None:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try:
        bpy.ops.object.shade_smooth()
    except Exception:
        pass
    obj.select_set(False)


def apply_rough_skin_displacement(
    obj: bpy.types.Object,
    *,
    strength: float,
    scale: float,
    contrast: float = 4.0,
) -> None:
    if strength <= 0.0:
        return
    tex = bpy.data.textures.new(obj.name + "_coarse_skin_noise", type="VORONOI")
    tex.noise_scale = scale
    tex.intensity = 0.36
    tex.contrast = contrast
    disp = obj.modifiers.new("coarse uneven monster skin", "DISPLACE")
    disp.strength = strength
    disp.texture = tex


def create_ellipsoid(
    name: str,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    *,
    segments: int = 48,
    rings: int = 24,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    apply_smooth(obj)
    register(obj)
    return obj


def create_section_mesh(
    name: str,
    sections: list[tuple[float, float, float, float, float]],
    mat: bpy.types.Material,
    *,
    radial_segments: int = 40,
    ripple: float = 0.020,
) -> bpy.types.Object:
    vertices: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []
    for section_index, (y, z_center, radius_x, radius_z, x_offset) in enumerate(sections):
        section_phase = y * 7.1 + section_index * 0.37
        for radial_index in range(radial_segments):
            theta = (math.tau * radial_index) / radial_segments
            organic = 1.0
            organic += ripple * math.sin(theta * 3.0 + section_phase)
            organic += ripple * 0.55 * math.sin(theta * 7.0 - section_phase * 1.7)
            x = x_offset + math.cos(theta) * radius_x * organic
            z = z_center + math.sin(theta) * radius_z * organic
            vertices.append((x, y, z))

    for section_index in range(len(sections) - 1):
        current = section_index * radial_segments
        next_ring = (section_index + 1) * radial_segments
        for radial_index in range(radial_segments):
            a = current + radial_index
            b = current + ((radial_index + 1) % radial_segments)
            c = next_ring + ((radial_index + 1) % radial_segments)
            d = next_ring + radial_index
            faces.append((a, b, c, d))

    faces.append(tuple(reversed(range(radial_segments))))
    last_start = (len(sections) - 1) * radial_segments
    faces.append(tuple(range(last_start, last_start + radial_segments)))

    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    subdiv = obj.modifiers.new("soft organic subdivision", "SUBSURF")
    subdiv.levels = 1
    subdiv.render_levels = 1
    obj.modifiers.new("weighted organic normals", "WEIGHTED_NORMAL")
    register(obj)
    return obj


def create_asymmetric_section_mesh(
    name: str,
    sections: list[tuple[float, float, float, float, float, float]],
    mat: bpy.types.Material,
    *,
    radial_segments: int = 56,
    ripple: float = 0.018,
    subdiv_levels: int = 1,
    rough_strength: float = 0.028,
    rough_scale: float = 0.105,
) -> bpy.types.Object:
    vertices: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []
    for section_index, (y, z_center, radius_x, top_z, bottom_z, x_offset) in enumerate(sections):
        phase = section_index * 0.73 + y * 5.9
        for radial_index in range(radial_segments):
            theta = (math.tau * radial_index) / radial_segments
            sin_t = math.sin(theta)
            cos_t = math.cos(theta)
            vertical_radius = top_z if sin_t >= 0.0 else bottom_z
            side_taper = 0.86 + 0.14 * (1.0 - abs(sin_t))
            organic = 1.0
            organic += ripple * math.sin(theta * 4.0 + phase)
            organic += ripple * 0.50 * math.sin(theta * 9.0 - phase * 0.8)
            x = x_offset + cos_t * radius_x * side_taper * organic
            z = z_center + sin_t * vertical_radius * organic
            vertices.append((x, y, z))

    for section_index in range(len(sections) - 1):
        current = section_index * radial_segments
        next_ring = (section_index + 1) * radial_segments
        for radial_index in range(radial_segments):
            a = current + radial_index
            b = current + ((radial_index + 1) % radial_segments)
            c = next_ring + ((radial_index + 1) % radial_segments)
            d = next_ring + radial_index
            faces.append((a, b, c, d))

    faces.append(tuple(reversed(range(radial_segments))))
    last_start = (len(sections) - 1) * radial_segments
    faces.append(tuple(range(last_start, last_start + radial_segments)))

    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    if subdiv_levels > 0:
        subdiv = obj.modifiers.new("reference organic subdivision", "SUBSURF")
        subdiv.levels = subdiv_levels
        subdiv.render_levels = subdiv_levels
    apply_rough_skin_displacement(obj, strength=rough_strength, scale=rough_scale, contrast=4.8)
    obj.modifiers.new("reference weighted normals", "WEIGHTED_NORMAL")
    register(obj)
    return obj


def create_organic_limb_mesh(
    name: str,
    points: list[Vector],
    radii: list[tuple[float, float]],
    mat: bpy.types.Material,
    *,
    radial_segments: int = 24,
    ripple: float = 0.018,
    rough_strength: float = 0.017,
    rough_scale: float = 0.095,
) -> bpy.types.Object:
    if len(points) != len(radii):
        raise ValueError("points and radii must have the same length")

    vertices: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []
    up_ref = Vector((0.0, 0.0, 1.0))
    for point_index, point in enumerate(points):
        if point_index == 0:
            tangent = points[1] - point
        elif point_index == len(points) - 1:
            tangent = point - points[point_index - 1]
        else:
            tangent = points[point_index + 1] - points[point_index - 1]
        if tangent.length < 0.0001:
            tangent = Vector((0.0, 1.0, 0.0))
        forward = tangent.normalized()
        right = forward.cross(up_ref)
        if right.length < 0.0001:
            right = Vector((1.0, 0.0, 0.0))
        right.normalize()
        up = right.cross(forward)
        up.normalize()
        radius_x, radius_z = radii[point_index]
        for radial_index in range(radial_segments):
            theta = (math.tau * radial_index) / radial_segments
            organic = 1.0
            organic += ripple * math.sin(theta * 4.0 + point_index * 0.83)
            organic += ripple * 0.45 * math.sin(theta * 9.0 - point_index * 0.41)
            vertex = point + right * (math.cos(theta) * radius_x * organic) + up * (math.sin(theta) * radius_z * organic)
            vertices.append((vertex.x, vertex.y, vertex.z))

    for point_index in range(len(points) - 1):
        current = point_index * radial_segments
        next_ring = (point_index + 1) * radial_segments
        for radial_index in range(radial_segments):
            a = current + radial_index
            b = current + ((radial_index + 1) % radial_segments)
            c = next_ring + ((radial_index + 1) % radial_segments)
            d = next_ring + radial_index
            faces.append((a, b, c, d))

    faces.append(tuple(reversed(range(radial_segments))))
    last_start = (len(points) - 1) * radial_segments
    faces.append(tuple(range(last_start, last_start + radial_segments)))

    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    subdiv = obj.modifiers.new("soft sinew subdivision", "SUBSURF")
    subdiv.levels = 1
    subdiv.render_levels = 1
    apply_rough_skin_displacement(obj, strength=rough_strength, scale=rough_scale, contrast=4.5)
    obj.modifiers.new("weighted limb normals", "WEIGHTED_NORMAL")
    register(obj)
    return obj


def create_cone_between(
    name: str,
    start: Vector,
    end: Vector,
    radius_start: float,
    radius_end: float,
    mat: bpy.types.Material,
    *,
    vertices: int = 24,
) -> bpy.types.Object:
    direction = end - start
    length = direction.length
    midpoint = start + direction * 0.5
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius_start,
        radius2=radius_end,
        depth=length,
        location=midpoint,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(mat)
    apply_smooth(obj)
    register(obj)
    return obj


def create_curve(name: str, points: list[Vector], mat: bpy.types.Material, bevel: float) -> bpy.types.Object:
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 2
    curve.bevel_depth = bevel
    curve.bevel_resolution = 3
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, vector in zip(spline.points, points):
        point.co = (vector.x, vector.y, vector.z, 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    register(obj)
    return obj


def create_crescent_blade(mat: bpy.types.Material) -> bpy.types.Object:
    thickness = 0.090
    blade_x = -1.030
    outer = [
        (-0.750, 0.225),
        (-0.940, 0.130),
        (-1.185, 0.060),
        (-1.455, 0.100),
        (-1.645, 0.315),
        (-1.530, 0.505),
        (-1.265, 0.450),
        (-0.975, 0.330),
        (-0.750, 0.255),
    ]
    inner = [
        (-0.825, 0.205),
        (-1.030, 0.185),
        (-1.260, 0.185),
        (-1.465, 0.245),
        (-1.530, 0.330),
        (-1.440, 0.385),
        (-1.205, 0.330),
        (-0.955, 0.245),
        (-0.825, 0.225),
    ]
    vertices = []
    for x_offset in (-thickness * 0.5, thickness * 0.5):
        for y, z in outer:
            vertices.append((blade_x + x_offset, y, z))
        for y, z in inner:
            vertices.append((blade_x + x_offset, y, z))
    faces = []
    count = len(outer)
    back_offset = count * 2
    for i in range(count - 1):
        j = i + 1
        faces.append((i, j, count + j, count + i))
        faces.append((back_offset + i, back_offset + count + i, back_offset + count + j, back_offset + j))
        faces.append((i, back_offset + i, back_offset + j, j))
        faces.append((count + i, count + j, back_offset + count + j, back_offset + count + i))
    faces.append((0, count, back_offset + count, back_offset))
    faces.append((count - 1, back_offset + count - 1, back_offset + count * 2 - 1, count * 2 - 1))
    mesh = bpy.data.meshes.new("Longa_Arma_Local_Left_Crescent_Blade_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Longa_Arma_Local_Left_Black_Crescent_Blade", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("worn bright bevel", "BEVEL")
    bevel.width = 0.015
    bevel.segments = 2
    obj.modifiers.new("weighted blade normals", "WEIGHTED_NORMAL")
    register(obj)
    return obj


def create_reference_blade(
    blade_mat: bpy.types.Material,
    edge_mat: bpy.types.Material,
) -> bpy.types.Object:
    thickness = 0.130
    blade_x = -0.760
    outer = [
        (-1.315, 0.105),
        (-1.470, 0.070),
        (-1.650, 0.055),
        (-1.840, 0.055),
        (-2.000, 0.095),
        (-2.105, 0.175),
        (-2.170, 0.320),
        (-2.195, 0.485),
        (-2.175, 0.635),
    ]
    inner = [
        (-1.325, 0.205),
        (-1.485, 0.230),
        (-1.670, 0.235),
        (-1.870, 0.220),
        (-2.030, 0.240),
        (-2.125, 0.305),
        (-2.185, 0.425),
        (-2.220, 0.565),
        (-2.205, 0.730),
    ]
    vertices: list[tuple[float, float, float]] = []
    for x_offset in (-thickness * 0.5, thickness * 0.5):
        for y, z in outer:
            vertices.append((blade_x + x_offset, y, z))
        for y, z in inner:
            vertices.append((blade_x + x_offset, y, z))
    faces: list[tuple[int, ...]] = []
    outer_count = len(outer)
    inner_count = len(inner)
    side_count = outer_count + inner_count
    back = side_count
    for i in range(outer_count - 1):
        faces.append((i, i + 1, back + i + 1, back + i))
    for i in range(inner_count - 1):
        a = outer_count + i
        faces.append((a, back + a, back + a + 1, a + 1))
    strip_count = min(outer_count, inner_count)
    for i in range(strip_count - 1):
        faces.append((i, i + 1, outer_count + i + 1, outer_count + i))
        faces.append((back + i, back + outer_count + i, back + outer_count + i + 1, back + i + 1))
    faces.append((0, outer_count, back + outer_count, back))
    faces.append((outer_count - 1, back + outer_count - 1, back + outer_count + inner_count - 1, outer_count + inner_count - 1))

    mesh = bpy.data.meshes.new("Longa_Arma_Reference_Crescent_Blade_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Longa_Arma_Local_Left_Reference_Black_Crescent_Blade", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(blade_mat)
    bevel = obj.modifiers.new("subtle worn thickness bevel", "BEVEL")
    bevel.width = 0.018
    bevel.segments = 2
    subdiv = obj.modifiers.new("smooth crescent surface", "SUBSURF")
    subdiv.levels = 1
    subdiv.render_levels = 1
    obj.modifiers.new("weighted blade normals", "WEIGHTED_NORMAL")
    register(obj)

    create_curve(
        "Longa_Arma_Blade_Silver_Outer_Worn_Edge",
        [Vector((blade_x - 0.060, y, z + 0.006)) for y, z in outer[3:]],
        edge_mat,
        0.006,
    )
    create_curve(
        "Longa_Arma_Blade_Inner_Cut_Edge",
        [Vector((blade_x - 0.063, y, z + 0.004)) for y, z in inner[5:]],
        edge_mat,
        0.0035,
    )
    return obj


def create_extruded_profile_mesh(
    name: str,
    points_yz: list[tuple[float, float]],
    half_width: float,
    mat: bpy.types.Material,
    *,
    x_center: float = 0.0,
    bevel_width: float = 0.006,
) -> bpy.types.Object:
    vertices: list[tuple[float, float, float]] = []
    for x in (x_center - half_width, x_center + half_width):
        for y, z in points_yz:
            vertices.append((x, y, z))

    count = len(points_yz)
    faces: list[tuple[int, ...]] = [tuple(range(count)), tuple(range(count, count * 2))]
    for index in range(count):
        next_index = (index + 1) % count
        faces.append((index, next_index, count + next_index, count + index))

    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    bevel = obj.modifiers.new("soft raised silhouette bevel", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    obj.modifiers.new("profile weighted normals", "WEIGHTED_NORMAL")
    register(obj)
    return obj


def add_clawed_foot(
    name: str,
    foot: Vector,
    forward: Vector,
    side_axis: Vector,
    mat: bpy.types.Material,
    *,
    toe_count: int,
    toe_spread: float,
) -> None:
    create_ellipsoid(name + "_Narrow_Knuckled_Foot", tuple(foot), (0.060, 0.044, 0.024), mat, segments=24, rings=8)
    forward_dir = forward.normalized()
    side_dir = side_axis.normalized()
    for toe_index in range(toe_count):
        offset = (toe_index - (toe_count - 1) * 0.5) * toe_spread
        start = foot + side_dir * (offset * 0.35) + Vector((0.0, 0.0, 0.006))
        end = foot + forward_dir * 0.120 + side_dir * offset + Vector((0.0, 0.0, -0.006))
        create_organic_limb_mesh(
            f"{name}_Long_Toe_{toe_index + 1}",
            [start, (start + end) * 0.5 + Vector((0.0, 0.0, 0.006)), end],
            [(0.011, 0.009), (0.009, 0.007), (0.0045, 0.0045)],
            mat,
            radial_segments=10,
            ripple=0.006,
        )


def add_puddle(name: str, loc: tuple[float, float, float], scale: tuple[float, float, float], mat: bpy.types.Material) -> None:
    obj = create_ellipsoid(name, loc, scale, mat, segments=32, rings=12)
    obj.name = name


def add_slime_drip(
    name: str,
    x: float,
    y: float,
    z_top: float,
    length: float,
    mat: bpy.types.Material,
    *,
    radius: float = 0.010,
) -> None:
    top = Vector((x, y, z_top))
    bottom = Vector((x + random.uniform(-0.015, 0.015), y + random.uniform(-0.010, 0.010), z_top - length))
    create_cone_between(name + "_strand", top, bottom, radius, radius * 0.52, mat, vertices=10)
    create_ellipsoid(name + "_drop", (bottom.x, bottom.y, bottom.z - radius * 1.6), (radius * 1.45, radius * 1.45, radius * 2.2), mat, segments=16, rings=8)


def add_reference_rough_skin_detail(dark: bpy.types.Material) -> None:
    muscle_lines = [
        ("Neck_Left_Fold", [(-0.055, -0.70, 0.700), (-0.080, -0.88, 0.780), (-0.068, -1.08, 0.770), (-0.048, -1.25, 0.675)], dark, 0.006),
        ("Neck_Right_Fold", [(0.055, -0.70, 0.700), (0.080, -0.88, 0.780), (0.068, -1.08, 0.770), (0.048, -1.25, 0.675)], dark, 0.006),
        ("Back_Spine_Dark_Groove", [(0.000, 0.82, 0.675), (0.000, 0.45, 0.705), (0.000, -0.05, 0.710), (0.000, -0.48, 0.700)], dark, 0.005),
        ("Left_Flank_Sinew_Long", [(-0.205, 0.70, 0.585), (-0.255, 0.42, 0.535), (-0.230, 0.08, 0.520), (-0.185, -0.35, 0.565)], dark, 0.005),
        ("Right_Flank_Sinew_Long", [(0.205, 0.70, 0.585), (0.255, 0.42, 0.535), (0.230, 0.08, 0.520), (0.185, -0.35, 0.565)], dark, 0.005),
        ("Left_Flank_Upper_Wave", [(-0.185, 0.50, 0.660), (-0.230, 0.30, 0.620), (-0.210, 0.03, 0.620), (-0.145, -0.24, 0.650)], dark, 0.004),
        ("Right_Flank_Upper_Wave", [(0.185, 0.50, 0.660), (0.230, 0.30, 0.620), (0.210, 0.03, 0.620), (0.145, -0.24, 0.650)], dark, 0.004),
        ("Left_Rib_Rough_Groove_A", [(-0.240, 0.28, 0.600), (-0.265, 0.18, 0.545), (-0.250, 0.05, 0.505)], dark, 0.004),
        ("Right_Rib_Rough_Groove_A", [(0.240, 0.28, 0.600), (0.265, 0.18, 0.545), (0.250, 0.05, 0.505)], dark, 0.004),
        ("Left_Rib_Rough_Groove_B", [(-0.235, -0.02, 0.595), (-0.255, -0.12, 0.540), (-0.225, -0.28, 0.500)], dark, 0.004),
        ("Right_Rib_Rough_Groove_B", [(0.235, -0.02, 0.595), (0.255, -0.12, 0.540), (0.225, -0.28, 0.500)], dark, 0.004),
        ("Left_Hind_Thigh_Groove", [(-0.210, 0.62, 0.510), (-0.280, 0.72, 0.395), (-0.300, 0.84, 0.260)], dark, 0.006),
        ("Right_Hind_Thigh_Groove", [(0.210, 0.62, 0.510), (0.280, 0.72, 0.395), (0.300, 0.84, 0.260)], dark, 0.006),
        ("Left_Haunch_Deep_Wave", [(-0.240, 0.78, 0.610), (-0.290, 0.70, 0.525), (-0.300, 0.60, 0.445)], dark, 0.005),
        ("Right_Haunch_Deep_Wave", [(0.240, 0.78, 0.610), (0.290, 0.70, 0.525), (0.300, 0.60, 0.445)], dark, 0.005),
        ("Weapon_Arm_Dark_Tendon_A", [(-0.315, -0.58, 0.505), (-0.465, -0.85, 0.340), (-0.585, -1.18, 0.175)], dark, 0.006),
        ("Weapon_Arm_Dark_Tendon_B", [(-0.235, -0.58, 0.525), (-0.380, -0.92, 0.365), (-0.545, -1.34, 0.125)], dark, 0.004),
        ("Weapon_Arm_Outer_Fold", [(-0.370, -0.620, 0.505), (-0.505, -0.900, 0.330), (-0.650, -1.270, 0.130)], dark, 0.005),
        ("Head_Cheek_Groove_Left", [(-0.052, -0.985, 0.800), (-0.078, -1.105, 0.720), (-0.060, -1.230, 0.625)], dark, 0.004),
        ("Head_Cheek_Groove_Right", [(0.052, -0.985, 0.800), (0.078, -1.105, 0.720), (0.060, -1.230, 0.625)], dark, 0.004),
        ("Visible_Side_Withers_Fold", [(0.035, -0.565, 0.745), (0.045, -0.420, 0.665), (0.040, -0.270, 0.610), (0.030, -0.085, 0.585)], dark, 0.006),
        ("Visible_Side_Rib_Fold_01", [(0.035, -0.200, 0.665), (0.052, -0.065, 0.605), (0.040, 0.105, 0.555)], dark, 0.005),
        ("Visible_Side_Rib_Fold_02", [(0.038, 0.080, 0.680), (0.058, 0.230, 0.615), (0.050, 0.410, 0.548)], dark, 0.005),
        ("Visible_Side_Haunch_Fold_01", [(0.042, 0.470, 0.680), (0.068, 0.600, 0.600), (0.050, 0.765, 0.520)], dark, 0.006),
        ("Visible_Side_Haunch_Fold_02", [(0.045, 0.640, 0.625), (0.075, 0.760, 0.510), (0.060, 0.885, 0.410)], dark, 0.005),
        ("Visible_Side_Neck_Melt_Fold_01", [(0.034, -0.880, 0.840), (0.048, -0.760, 0.775), (0.040, -0.640, 0.705)], dark, 0.005),
        ("Visible_Side_Neck_Melt_Fold_02", [(0.032, -1.085, 0.705), (0.052, -0.980, 0.640), (0.043, -0.850, 0.590)], dark, 0.004),
    ]
    for name, points, mat, bevel in muscle_lines:
        create_curve(f"Longa_Arma_Reference_{name}", [Vector(point) for point in points], mat, bevel)

    # These are dark recessed skin folds, not slime/drip/puddle effects.


def build_reference_matched_model(materials: dict[str, bpy.types.Material]) -> None:
    body = materials["body"]
    blade = materials["blade"]
    dark = materials["dark"]
    eye = materials["eye"]
    ridge = materials.get("ridge", dark)
    blade_edge = materials.get("blade_edge", blade)

    create_asymmetric_section_mesh(
        "Longa_Arma_Reference_Fused_Body_Neck_Head",
        [
            (-1.395, 0.535, 0.040, 0.042, 0.036, 0.000),
            (-1.270, 0.585, 0.062, 0.066, 0.056, 0.000),
            (-1.125, 0.700, 0.082, 0.096, 0.082, 0.000),
            (-0.950, 0.825, 0.100, 0.120, 0.092, 0.000),
            (-0.775, 0.810, 0.092, 0.096, 0.078, 0.000),
            (-0.615, 0.700, 0.122, 0.122, 0.092, 0.000),
            (-0.450, 0.600, 0.150, 0.150, 0.120, 0.000),
            (-0.190, 0.565, 0.205, 0.168, 0.168, 0.000),
            (0.165, 0.565, 0.235, 0.180, 0.178, 0.000),
            (0.515, 0.565, 0.260, 0.196, 0.190, 0.000),
            (0.790, 0.535, 0.225, 0.178, 0.160, 0.000),
            (0.995, 0.465, 0.120, 0.112, 0.088, 0.000),
        ],
        body,
        radial_segments=76,
        ripple=0.040,
        subdiv_levels=1,
        rough_strength=0.034,
        rough_scale=0.095,
    )
    create_ellipsoid("Longa_Arma_Reference_Muzzle_Dark_Slit", (0.0, -1.330, 0.565), (0.048, 0.012, 0.016), dark, segments=24, rings=8)

    for side, suffix in [(-1.0, "Left"), (1.0, "Right")]:
        create_organic_limb_mesh(
            f"Longa_Arma_Reference_{suffix}_Tall_Ear",
            [
                Vector((side * 0.060, -0.965, 0.915)),
                Vector((side * 0.078, -0.940, 1.010)),
                Vector((side * 0.066, -0.965, 1.110)),
            ],
            [(0.030, 0.025), (0.022, 0.018), (0.005, 0.005)],
            body,
            radial_segments=14,
            ripple=0.004,
        )
        create_ellipsoid(
            f"Longa_Arma_Reference_{suffix}_Small_Wet_Eye",
            (side * 0.080, -1.105, 0.735),
            (0.014, 0.010, 0.020),
            eye,
            segments=18,
            rings=8,
        )

    create_organic_limb_mesh(
        "Longa_Arma_Reference_Local_Left_Massive_Weapon_Arm",
        [
            Vector((-0.085, -0.350, 0.545)),
            Vector((-0.250, -0.540, 0.455)),
            Vector((-0.430, -0.800, 0.300)),
            Vector((-0.610, -1.075, 0.170)),
            Vector((-0.720, -1.270, 0.110)),
            Vector((-0.770, -1.420, 0.102)),
        ],
        [(0.170, 0.114), (0.150, 0.096), (0.125, 0.078), (0.098, 0.058), (0.088, 0.045), (0.078, 0.038)],
        body,
        radial_segments=42,
        ripple=0.034,
        rough_strength=0.024,
        rough_scale=0.085,
    )
    create_organic_limb_mesh(
        "Longa_Arma_Reference_Local_Left_Flesh_To_Blade_Growth",
        [
            Vector((-0.705, -1.220, 0.128)),
            Vector((-0.760, -1.340, 0.102)),
            Vector((-0.795, -1.490, 0.108)),
            Vector((-0.805, -1.640, 0.162)),
        ],
        [(0.100, 0.052), (0.138, 0.044), (0.172, 0.034), (0.205, 0.026)],
        body,
        radial_segments=36,
        ripple=0.026,
        rough_strength=0.020,
        rough_scale=0.080,
    )
    create_organic_limb_mesh(
        "Longa_Arma_Reference_Local_Left_Calcified_Blade_Base",
        [
            Vector((-0.790, -1.315, 0.108)),
            Vector((-0.805, -1.455, 0.102)),
            Vector((-0.810, -1.610, 0.145)),
            Vector((-0.802, -1.745, 0.235)),
        ],
        [(0.122, 0.026), (0.180, 0.024), (0.220, 0.020), (0.165, 0.015)],
        blade,
        radial_segments=34,
        ripple=0.014,
        rough_strength=0.009,
        rough_scale=0.090,
    )
    create_organic_limb_mesh(
        "Longa_Arma_Reference_Local_Left_Calcified_Underside_Transition",
        [
            Vector((-0.745, -1.040, 0.150)),
            Vector((-0.790, -1.190, 0.112)),
            Vector((-0.815, -1.365, 0.095)),
            Vector((-0.820, -1.555, 0.118)),
        ],
        [(0.072, 0.018), (0.118, 0.017), (0.164, 0.014), (0.190, 0.012)],
        blade,
        radial_segments=28,
        ripple=0.010,
        rough_strength=0.007,
        rough_scale=0.075,
    )
    create_extruded_profile_mesh(
        "Longa_Arma_Reference_Local_Left_Flesh_Web_Into_Blade",
        [
            (-1.255, 0.082),
            (-1.420, 0.058),
            (-1.610, 0.080),
            (-1.770, 0.160),
            (-1.720, 0.245),
            (-1.520, 0.220),
            (-1.335, 0.155),
        ],
        0.085,
        body,
        x_center=-0.780,
        bevel_width=0.016,
    )
    create_extruded_profile_mesh(
        "Longa_Arma_Reference_Local_Left_Blackened_Blade_Root",
        [
            (-1.315, 0.090),
            (-1.500, 0.066),
            (-1.695, 0.095),
            (-1.805, 0.170),
            (-1.650, 0.230),
            (-1.450, 0.195),
            (-1.315, 0.135),
        ],
        0.078,
        blade,
        x_center=-0.762,
        bevel_width=0.012,
    )
    create_reference_blade(blade, blade_edge)

    leg_specs = [
        (
            "Right_Front",
            [Vector((0.105, -0.335, 0.475)), Vector((0.190, -0.485, 0.300)), Vector((0.198, -0.600, 0.098)), Vector((0.260, -0.695, 0.034))],
            [(0.104, 0.074), (0.068, 0.052), (0.040, 0.032), (0.028, 0.022)],
            Vector((0.0, -1.0, 0.0)),
            4,
            0.034,
        ),
        (
            "Left_Rear",
            [Vector((-0.125, 0.455, 0.475)), Vector((-0.260, 0.690, 0.300)), Vector((-0.330, 0.885, 0.110)), Vector((-0.410, 1.015, 0.034))],
            [(0.112, 0.080), (0.072, 0.054), (0.044, 0.034), (0.028, 0.022)],
            Vector((0.0, 1.0, 0.0)),
            3,
            0.033,
        ),
        (
            "Right_Rear",
            [Vector((0.130, 0.455, 0.475)), Vector((0.310, 0.720, 0.310)), Vector((0.440, 0.905, 0.110)), Vector((0.525, 1.045, 0.034))],
            [(0.118, 0.084), (0.076, 0.056), (0.044, 0.034), (0.028, 0.022)],
            Vector((0.0, 1.0, 0.0)),
            3,
            0.033,
        ),
    ]
    for suffix, points, radii, forward, toe_count, toe_spread in leg_specs:
        create_organic_limb_mesh(
            f"Longa_Arma_Reference_{suffix}_Sinewy_Leg",
            points,
            radii,
            body,
            radial_segments=28,
            ripple=0.014,
        )
        add_clawed_foot(
            f"Longa_Arma_Reference_{suffix}",
            points[-1],
            forward,
            Vector((1.0, 0.0, 0.0)),
            body,
            toe_count=toe_count,
            toe_spread=toe_spread,
        )

    create_organic_limb_mesh(
        "Longa_Arma_Reference_Drooping_Tail",
        [Vector((0.0, 0.900, 0.495)), Vector((0.0, 1.000, 0.360)), Vector((0.0, 1.020, 0.230))],
        [(0.045, 0.035), (0.026, 0.022), (0.010, 0.010)],
        body,
        radial_segments=16,
        ripple=0.008,
    )

    # Do not add separate surface slime, dripping liquid, drops, or puddle objects.


def build_model(materials: dict[str, bpy.types.Material]) -> None:
    build_reference_matched_model(materials)
    return

    body = materials["body"]
    blade = materials["blade"]
    slime = materials["slime"]
    dark = materials["dark"]
    eye = materials["eye"]
    ridge = materials.get("ridge", slime)
    blade_edge = materials.get("blade_edge", blade)

    body_sections = [
        (0.92, 0.46, 0.105, 0.115, 0.000),
        (0.76, 0.53, 0.215, 0.195, 0.000),
        (0.50, 0.55, 0.238, 0.180, 0.000),
        (0.18, 0.55, 0.220, 0.148, 0.000),
        (-0.15, 0.56, 0.200, 0.152, 0.000),
        (-0.43, 0.59, 0.180, 0.176, 0.000),
        (-0.64, 0.56, 0.125, 0.150, 0.000),
    ]
    create_section_mesh("Longa_Arma_Lean_Continuous_Ribcage_And_Rump", body_sections, body, radial_segments=56, ripple=0.024)

    neck_head_sections = [
        (-0.55, 0.66, 0.088, 0.104, 0.000),
        (-0.72, 0.78, 0.078, 0.112, 0.000),
        (-0.91, 0.86, 0.073, 0.112, 0.000),
        (-1.10, 0.82, 0.080, 0.122, 0.000),
        (-1.26, 0.72, 0.068, 0.105, 0.000),
        (-1.40, 0.63, 0.046, 0.064, 0.000),
    ]
    create_section_mesh("Longa_Arma_Seamless_Long_Neck_And_Horse_Head", neck_head_sections, body, radial_segments=44, ripple=0.018)
    create_ellipsoid("Longa_Arma_Dark_Drooping_Mouth_Slit", (0.0, -1.455, 0.615), (0.048, 0.008, 0.016), dark, segments=24, rings=8)

    for side, suffix in [(-1.0, "Left"), (1.0, "Right")]:
        create_organic_limb_mesh(
            f"Longa_Arma_{suffix}_Thin_Ear_Horn",
            [
                Vector((side * 0.058, -1.045, 0.930)),
                Vector((side * 0.078, -1.030, 1.020)),
                Vector((side * 0.070, -1.035, 1.105)),
            ],
            [(0.033, 0.025), (0.025, 0.020), (0.006, 0.006)],
            body,
            radial_segments=14,
            ripple=0.006,
        )
        create_ellipsoid(
            f"Longa_Arma_{suffix}_Wet_Black_Eye",
            (side * 0.080, -1.225, 0.790),
            (0.016, 0.010, 0.024),
            eye,
            segments=16,
            rings=8,
        )

    create_curve(
        "Longa_Arma_Dark_Spine_Ridge_From_Rump_To_Neck",
        [
            Vector((0.000, 0.78, 0.675)),
            Vector((0.000, 0.32, 0.695)),
            Vector((0.000, -0.30, 0.720)),
            Vector((0.000, -0.82, 0.905)),
        ],
        ridge,
        0.006,
    )
    for side, suffix in [(-1.0, "Left"), (1.0, "Right")]:
        create_curve(
            f"Longa_Arma_{suffix}_Flank_Recess_Muscle_Line",
            [
                Vector((side * 0.205, 0.64, 0.590)),
                Vector((side * 0.255, 0.30, 0.535)),
                Vector((side * 0.220, -0.12, 0.510)),
                Vector((side * 0.170, -0.48, 0.585)),
            ],
            ridge,
            0.005,
        )

    # Model local left is negative X. This long arm is shaped as an organic weapon limb.
    left_shoulder = Vector((-0.220, -0.500, 0.555))
    left_elbow = Vector((-0.455, -0.690, 0.355))
    left_mid = Vector((-0.725, -0.790, 0.205))
    left_wrist = Vector((-0.985, -0.740, 0.145))
    create_organic_limb_mesh(
        "Longa_Arma_Local_Left_Lean_Overgrown_Weapon_Arm",
        [left_shoulder, left_elbow, left_mid, left_wrist],
        [(0.090, 0.074), (0.074, 0.062), (0.055, 0.043), (0.060, 0.034)],
        body,
        radial_segments=30,
        ripple=0.020,
    )
    create_ellipsoid("Longa_Arma_Local_Left_Fused_Shoulder_Muscle", tuple(left_shoulder), (0.090, 0.078, 0.084), body)
    create_organic_limb_mesh(
        "Longa_Arma_Local_Left_Black_Blade_Grown_Sheath",
        [
            left_wrist,
            Vector((-1.025, -0.760, 0.170)),
            Vector((-1.035, -0.835, 0.195)),
        ],
        [(0.060, 0.032), (0.082, 0.030), (0.114, 0.025)],
        body,
        radial_segments=26,
        ripple=0.010,
    )
    create_crescent_blade(blade)
    create_curve(
        "Longa_Arma_Blade_Bright_Worn_Outer_Edge",
        [
            Vector((-1.088, -0.800, 0.220)),
            Vector((-1.088, -1.120, 0.360)),
            Vector((-1.088, -1.470, 0.480)),
            Vector((-1.088, -1.625, 0.330)),
        ],
        blade_edge,
        0.006,
    )
    for tendon_index, z in enumerate([0.135, 0.168, 0.205], start=1):
        create_curve(
            f"Longa_Arma_Local_Left_Green_Tendon_Into_Blade_{tendon_index}",
            [
                Vector((-0.890, -0.735, z - 0.015)),
                Vector((-1.020, -0.795, z)),
                Vector((-1.075, -0.970, z + 0.010)),
            ],
            slime,
            0.010,
        )

    limb_specs = [
        (
            "Right_Front",
            [Vector((0.205, -0.455, 0.510)), Vector((0.220, -0.515, 0.300)), Vector((0.205, -0.620, 0.090)), Vector((0.250, -0.690, 0.040))],
            [(0.060, 0.050), (0.048, 0.043), (0.032, 0.028), (0.024, 0.022)],
            Vector((0.0, -1.0, 0.0)),
            4,
            0.035,
        ),
        (
            "Left_Rear",
            [Vector((-0.205, 0.520, 0.500)), Vector((-0.245, 0.655, 0.300)), Vector((-0.310, 0.825, 0.110)), Vector((-0.360, 0.935, 0.042))],
            [(0.067, 0.056), (0.054, 0.046), (0.036, 0.030), (0.026, 0.022)],
            Vector((0.0, 1.0, 0.0)),
            3,
            0.033,
        ),
        (
            "Right_Rear",
            [Vector((0.215, 0.520, 0.500)), Vector((0.315, 0.680, 0.310)), Vector((0.390, 0.845, 0.120)), Vector((0.460, 0.970, 0.042))],
            [(0.070, 0.058), (0.054, 0.046), (0.036, 0.030), (0.026, 0.022)],
            Vector((0.0, 1.0, 0.0)),
            3,
            0.033,
        ),
    ]
    for suffix, points, radii, foot_forward, toe_count, toe_spread in limb_specs:
        create_organic_limb_mesh(f"Longa_Arma_{suffix}_Lean_Sinewy_Leg", points, radii, body, radial_segments=24, ripple=0.016)
        add_clawed_foot(
            f"Longa_Arma_{suffix}",
            points[-1],
            foot_forward,
            Vector((1.0, 0.0, 0.0)),
            body,
            toe_count=toe_count,
            toe_spread=toe_spread,
        )

    create_organic_limb_mesh(
        "Longa_Arma_Thin_Drooping_Tail_Appendage",
        [Vector((0.0, 0.900, 0.500)), Vector((0.0, 1.015, 0.365)), Vector((0.0, 1.030, 0.245))],
        [(0.043, 0.036), (0.028, 0.024), (0.010, 0.010)],
        body,
        radial_segments=16,
        ripple=0.010,
    )

    for i in range(22):
        side = random.choice([-1.0, 1.0])
        y0 = random.uniform(-0.52, 0.72)
        x0 = side * random.uniform(0.105, 0.245)
        z0 = random.uniform(0.500, 0.690)
        points = [
            Vector((x0, y0, z0)),
            Vector((x0 * 0.93 + side * 0.018, y0 + 0.070, z0 - 0.030)),
            Vector((x0 * 0.76 + side * 0.010, y0 + 0.170, z0 - 0.055)),
        ]
        create_curve(f"Longa_Arma_Continuous_Wet_Surface_Streak_{i + 1:02d}", points, slime, random.uniform(0.0035, 0.0070))

    drip_points = [
        (-0.16, -0.05, 0.375, 0.20), (0.15, -0.18, 0.365, 0.17), (0.04, 0.38, 0.370, 0.24),
        (0.0, -1.41, 0.585, 0.15), (-0.07, -1.05, 0.735, 0.13), (-0.46, -0.66, 0.310, 0.16),
        (-0.72, -0.79, 0.205, 0.15), (-1.08, -0.84, 0.160, 0.11), (-1.30, -1.02, 0.165, 0.12),
        (-0.02, 0.86, 0.245, 0.18), (0.36, 0.91, 0.230, 0.13),
    ]
    for i, (x, y, z, length) in enumerate(drip_points, start=1):
        add_slime_drip(f"Longa_Arma_Hanging_Green_Drip_{i:02d}", x, y, z, length, slime, radius=random.uniform(0.006, 0.012))

    for i, (x, y, sx, sy) in enumerate(
        [
            (-0.34, 0.96, 0.090, 0.045),
            (0.46, 1.00, 0.085, 0.045),
            (0.25, -0.72, 0.070, 0.040),
            (-1.22, -1.03, 0.145, 0.050),
            (0.0, -1.45, 0.070, 0.036),
        ],
        start=1,
    ):
        add_puddle(f"Longa_Arma_Green_Slime_Puddle_{i:02d}", (x, y, 0.008), (sx, sy, 0.010), slime)


def setup_lighting() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -3.6, 3.5))
    key = bpy.context.object
    key.name = "Softbox_Key_Light"
    key.data.energy = 560
    key.data.size = 5.2
    bpy.ops.object.light_add(type="AREA", location=(-2.8, 1.6, 2.2))
    fill = bpy.context.object
    fill.name = "Cool_Left_Fill_Light"
    fill.data.energy = 85
    fill.data.size = 3.0
    bpy.ops.object.light_add(type="AREA", location=(2.6, 1.4, 2.0))
    rim = bpy.context.object
    rim.name = "Blade_Rim_Light"
    rim.data.energy = 120
    rim.data.size = 2.8


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def set_camera(location: tuple[float, float, float], target: tuple[float, float, float], ortho: float) -> bpy.types.Object:
    camera_data = bpy.data.cameras.new("Longa_Arma_Render_Camera")
    camera = bpy.data.objects.new("Longa_Arma_Render_Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = location
    look_at(camera, Vector(target))
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho
    bpy.context.scene.camera = camera
    return camera


def setup_render() -> None:
    scene = bpy.context.scene
    for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            continue
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 900
    scene.eevee.taa_render_samples = 64 if hasattr(scene, "eevee") else 16
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("Longa_Arma_Dark_Review_World")
    scene.world.color = (0.030, 0.036, 0.033)
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    if background is not None:
        background.inputs["Color"].default_value = (0.030, 0.036, 0.033, 1.0)
        background.inputs["Strength"].default_value = 0.72
    try:
        scene.view_settings.view_transform = "Standard"
        scene.view_settings.look = "None"
        scene.view_settings.exposure = -0.05
        scene.view_settings.gamma = 1.0
    except Exception:
        pass


def render_view(name: str, location: tuple[float, float, float], target: tuple[float, float, float], ortho: float) -> None:
    camera = set_camera(location, target, ortho)
    bpy.context.scene.render.filepath = str(RENDER_DIR / name)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)


def select_model_objects() -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in MODEL_OBJECTS:
        if obj.name in bpy.data.objects:
            obj.select_set(True)
    if MODEL_OBJECTS:
        bpy.context.view_layer.objects.active = MODEL_OBJECTS[0]


def export_model() -> None:
    select_model_objects()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "longa_arma.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "longa_arma.fbx"), use_selection=True, apply_scale_options="FBX_SCALE_ALL")
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "longa_arma.glb"), export_format="GLB", use_selection=True)


def create_image_material(name: str, image_path: Path) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    tex = nodes.new(type="ShaderNodeTexImage")
    image = bpy.data.images.load(str(image_path))
    tex.image = image
    if bsdf is not None:
        links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        set_principled_input(mat, "Roughness", 0.52)
    return mat


def add_image_plane(name: str, image_path: Path, center: tuple[float, float, float], width: float) -> None:
    image = bpy.data.images.load(str(image_path))
    aspect = image.size[1] / image.size[0]
    height = width * aspect
    mesh = bpy.data.meshes.new(name + "_Mesh")
    half_w = width * 0.5
    half_h = height * 0.5
    verts = [(-half_w, 0, -half_h), (half_w, 0, -half_h), (half_w, 0, half_h), (-half_w, 0, half_h)]
    faces = [(0, 1, 2, 3)]
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = center
    obj.data.materials.append(create_image_material(name + "_Mat", image_path))
    uv = obj.data.uv_layers.new(name="UVMap")
    coords = [(0, 0), (1, 0), (1, 1), (0, 1)]
    for loop_index, loop in enumerate(obj.data.loops):
        uv.data[loop_index].uv = coords[loop.vertex_index]


def render_reference_comparison() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    setup_render()
    width = 2.6
    y = 0.0
    columns = [(-2.9, "front", REFERENCE_FRONT, RENDER_DIR / "front.png"), (0.0, "side", REFERENCE_SIDE, RENDER_DIR / "side.png"), (2.9, "back", REFERENCE_BACK, RENDER_DIR / "back.png")]
    for x, label, reference, sample in columns:
        add_image_plane(f"Reference_{label}", reference, (x, y, 1.05), width)
        add_image_plane(f"Sample_{label}", sample, (x, y, -1.05), width)
    bpy.ops.object.light_add(type="AREA", location=(0, -3, 4))
    light = bpy.context.object
    light.data.energy = 250
    light.data.size = 6.0
    camera = set_camera((0, -6.0, 0.0), (0, 0, 0), 6.6)
    bpy.context.scene.render.resolution_x = 1800
    bpy.context.scene.render.resolution_y = 1200
    bpy.context.scene.render.filepath = str(RENDER_DIR / "reference_comparison.png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)


def write_docs() -> None:
    created_at = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    readme = f"""# 롱가 아르마 승인용 아트 샘플

- 생성 시각: {created_at}
- 상태: 사용자 승인 전 샘플
- 기준 이미지:
  - `image/longa arma(롱가 아르마).png`
  - `image/longa arma-back.png`
  - `image/longa arma-beside.png`

## 원본 기획 기준

- 높이 약 80cm, 가로 약 70cm, 세로 약 150cm 비율을 기준으로 제작했습니다.
- 한쪽 팔이 비정상적으로 거대한 긴 팔 개체이며, 긴 무기 팔은 모델 로컬 기준 왼팔입니다.
- 공격 의도는 상체를 들어올린 뒤 긴 왼팔 칼날로 내려찍는 형태입니다.
- 내려찍기 이후 바닥 쓸림은 공격 판정이 없고, Unity 적용 단계에서는 내려찍기 순간에만 데미지 30 판정을 연결해야 합니다.

## 샘플에서 드러낸 요소

- 말 또는 대형 사족 짐승형 긴 몸체
- 모델 로컬 왼쪽으로 길게 늘어진 비정상 팔
- 왼팔 끝의 검은 초승달형 칼날
- 어두운 녹색/회녹색/청록색이 섞인 젖은 유기질 표면
- 몸체, 주둥이, 배 아래, 긴 왼팔, 칼날에서 흘러내리는 점액 줄기와 방울
- 발 주변과 칼날 아래에 남는 점액 웅덩이

## 검토 파일

- 정면 렌더: `renders/front.png`
- 측면 렌더: `renders/side.png`
- 후면 렌더: `renders/back.png`
- 기준 이미지/샘플 비교 렌더: `renders/reference_comparison.png`
- Blender 원본: `blender/longa_arma.blend`
- 내보내기: `exports/longa_arma.fbx`, `exports/longa_arma.glb`

## Unity 적용 전제

- 이 샘플은 아직 Unity 런타임 씬, 프리팹, AI, 피격 판정에 연결되지 않았습니다.
- 사용자 승인 후 `Assets/_Project/Art/Enemies/LongaArma/`와 `LongaArmaApproved.prefab`로 재현 적용하는 것이 다음 단계입니다.
- Unity 적용 시 정적, 대기, 이동, 공격, 피격, 사망, 섭취 상태를 분리해 확인할 계획입니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    texture_analysis = """# 롱가 아르마 텍스처/머티리얼 분석

## 몸체

- 기준 이미지는 단색 녹색이 아니라 어두운 녹색, 회녹색, 청록색이 젖은 표면 위에서 섞여 있습니다.
- 이를 반영하기 위해 `longa_arma_wet_green_albedo.png`는 마블링 노이즈, 흐르는 줄무늬, 밝은 점액 자국을 섞어 생성했습니다.
- `longa_arma_wet_green_bump.png`는 울퉁불퉁한 근섬유/점액 표면을 위한 높이 정보로 사용합니다.
- `longa_arma_wet_green_roughness.png`는 젖은 표면답게 낮은 roughness 중심으로 만들었습니다.

## 칼날

- 기준 이미지의 칼날은 검정에 가까운 어두운 재질이며, 날 끝과 가장자리에서 밝은 마모/반사가 보입니다.
- `longa_arma_dark_blade_albedo.png`에는 어두운 흑회색, 긁힘, 가장자리 반사선을 넣었습니다.
- 칼날은 점액처럼 녹는 몸체와 다르게 단단한 무기 재질로 남는 계획입니다.

## 점액

- `longa_arma_slime_albedo.png`는 반투명 녹색 점액 줄기, 방울, 웅덩이에 사용합니다.
- 점액은 몸체 표면, 주둥이, 배 아래, 긴 왼팔, 칼날 하단에서 흘러내리도록 배치했습니다.
"""
    (SAMPLE_ROOT / "TEXTURE_ANALYSIS.md").write_text(texture_analysis, encoding="utf-8")

    manifest = {
        "enemyId": "longa_arma",
        "createdAt": created_at,
        "status": "pending_user_approval",
        "sourceReferences": [
            "docs/GAME_DESIGN_SOURCE.txt",
            "docs/LONGA_ARMA_IMPLEMENTATION_PLAN_2026-07-01.md",
            "image/longa arma(롱가 아르마).png",
            "image/longa arma-back.png",
            "image/longa arma-beside.png",
        ],
        "modelScaleMeters": {"height": MODEL_HEIGHT_M, "width": MODEL_WIDTH_M, "depth": MODEL_DEPTH_M},
        "localLeftWeaponArm": True,
        "attackDamagePlan": {"downslamDamage": 30, "scrapeDamage": 0},
        "futureUnityPaths": [
            "Assets/_Project/Art/Enemies/LongaArma/",
            "Assets/_Project/Prefabs/Enemies/LongaArma/LongaArmaApproved.prefab",
            "docs/validation/longa_arma_cargo_run_scene/",
        ],
        "files": [
            "README.md",
            "TEXTURE_ANALYSIS.md",
            "APPROVAL_STATUS.json",
            "ASSET_MANIFEST.json",
            "index.html",
            "blender/longa_arma.blend",
            "exports/longa_arma.fbx",
            "exports/longa_arma.glb",
            "textures/longa_arma_wet_green_albedo.png",
            "textures/longa_arma_wet_green_roughness.png",
            "textures/longa_arma_wet_green_bump.png",
            "textures/longa_arma_dark_blade_albedo.png",
            "textures/longa_arma_dark_blade_roughness.png",
            "textures/longa_arma_slime_albedo.png",
            "renders/front.png",
            "renders/side.png",
            "renders/back.png",
            "renders/reference_comparison.png",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "enemyId": "longa_arma",
        "status": "pending_user_approval",
        "approved": False,
        "createdAt": created_at,
        "note": "Unity 적용 전 사용자 검토가 필요한 승인용 artSample입니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    html = """<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <title>롱가 아르마 승인용 샘플</title>
  <style>
    body { margin: 0; font-family: "Malgun Gothic", Arial, sans-serif; background: #111513; color: #e9efe6; }
    main { max-width: 1180px; margin: 0 auto; padding: 28px; }
    h1, h2 { font-weight: 650; }
    figure { margin: 18px 0; border: 1px solid #314038; background: #1b221e; padding: 12px; }
    img { display: block; width: 100%; height: auto; background: #fff; }
    code { color: #b7e2a7; }
    .grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 14px; }
  </style>
</head>
<body>
<main>
  <h1>롱가 아르마 승인용 샘플</h1>
  <p>Unity 적용 전 검토용 샘플입니다. 기준 이미지와 유사한 비대칭 긴 왼팔, 검은 초승달형 칼날, 젖은 녹색 점액 표면을 확인해 주세요.</p>
  <figure>
    <figcaption>기준 이미지 / 샘플 비교</figcaption>
    <img src="renders/reference_comparison.png" alt="reference comparison">
  </figure>
  <div class="grid">
    <figure><figcaption>정면</figcaption><img src="renders/front.png" alt="front"></figure>
    <figure><figcaption>측면</figcaption><img src="renders/side.png" alt="side"></figure>
    <figure><figcaption>후면</figcaption><img src="renders/back.png" alt="back"></figure>
  </div>
  <h2>산출물</h2>
  <p><code>blender/longa_arma.blend</code>, <code>exports/longa_arma.fbx</code>, <code>exports/longa_arma.glb</code>, <code>textures/</code></p>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")

    readme_ko = f"""# 롱가 아르마 승인용 아트 샘플

- 생성 시각: {created_at}
- 상태: 사용자 승인 전 샘플
- 기준 이미지:
  - `image/longa arma(롱가 아르마).png`
  - `image/longa arma-back.png`
  - `image/longa arma-beside.png`

## 반영 기준

- 기준 이미지처럼 긴 목과 긴 몸체를 가진 말/사냥개형 실루엣을 목표로 했습니다.
- 사용자가 확정한 대로 긴 무기 팔은 모델 로컬 기준 왼쪽입니다.
- 기존 샘플보다 몸통 폭과 목 단면을 줄여 더 날렵하게 재제작했습니다.
- 몸체는 큰 구체 조합 대신 길이 방향 단면 메쉬를 사용했고, 목/머리와 다리는 유기적 튜브 메쉬로 다시 만들었습니다.
- 로컬 왼팔 끝의 반달형 칼은 단순 판 부착이 아니라 팔에서 칼날로 변형되는 연결부, 녹색 힘줄, 칼날 외곽 하이라이트를 포함하도록 수정했습니다.
- 몸체 머티리얼은 이미지 텍스처만 반복하는 방식에서 벗어나 절차적 젖은 표면 머티리얼을 사용해 부품 사이 텍스처 단절감을 줄였습니다.

## 검사 파일

- 정면 렌더: `renders/front.png`
- 측면 렌더: `renders/side.png`
- 후면 렌더: `renders/back.png`
- 기준 이미지/샘플 비교 렌더: `renders/reference_comparison.png`
- Blender 원본: `blender/longa_arma.blend`
- 내보내기: `exports/longa_arma.fbx`, `exports/longa_arma.glb`

## Unity 적용 전제

- 이 샘플은 아직 Unity 씬, 프리팹, 런타임 에셋, AI, 피격 판정에 연결하지 않았습니다.
- 사용자 승인 후 `Assets/_Project/Art/Enemies/LongaArma/` 및 `LongaArmaApproved.prefab` 쪽으로 재현 적용하는 것이 다음 단계입니다.
- 전투 판정 계획은 내려찍기 데미지 30, 쓸림 데미지 없음으로 유지합니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme_ko, encoding="utf-8")

    texture_analysis_ko = """# 롱가 아르마 텍스처/머티리얼 분석

## 몸체

- 기준 이미지는 단색 초록이 아니라 어두운 녹색, 청록, 검은 습윤 표면이 섞인 점액질 피부입니다.
- 이번 샘플은 `longa_arma_wet_green_albedo.png`, `longa_arma_wet_green_bump.png`, `longa_arma_wet_green_roughness.png`를 생성하며, 렌더 머티리얼은 절차적 노이즈와 범프를 함께 사용합니다.
- 이전 샘플에서 보였던 부품별 줄무늬 단절을 줄이기 위해 메인 몸체를 연속 단면 메쉬로 바꾸고, 표면 색 변화는 반복 텍스처보다 절차적 젖은 표면 위주로 처리했습니다.

## 칼날

- 칼날은 검정에 가까운 어두운 금속/각질 재질이며, 외곽에 밝은 마모선이 있는 반달형 무기입니다.
- 이번 샘플에서는 칼날을 속이 빈 초승달형 스트립 메쉬로 바꾸고, 팔 끝의 녹색 연결부와 힘줄이 칼날 뿌리로 이어지도록 배치했습니다.

## 점액

- 점액은 몸체, 목, 턱, 배, 긴 왼팔, 칼날 하단에서 아래로 흐르며 바닥에 작은 웅덩이를 만듭니다.
- Unity 적용 단계에서는 이 점액 의도를 머티리얼, 보조 메쉬, 사망 시 녹아내림 연출로 이어갈 수 있습니다.
"""
    (SAMPLE_ROOT / "TEXTURE_ANALYSIS.md").write_text(texture_analysis_ko, encoding="utf-8")

    manifest_ko = {
        "enemyId": "longa_arma",
        "createdAt": created_at,
        "status": "pending_user_approval",
        "sourceReferences": [
            "docs/GAME_DESIGN_SOURCE.txt",
            "docs/LONGA_ARMA_IMPLEMENTATION_PLAN_2026-07-01.md",
            "image/longa arma(롱가 아르마).png",
            "image/longa arma-back.png",
            "image/longa arma-beside.png",
        ],
        "revisionNotes": [
            "Body proportions were narrowed to reduce the earlier chunky silhouette.",
            "Main body and limbs now use organic procedural meshes with subdivision smoothing.",
            "The local-left crescent blade now has a crescent strip silhouette and green tendon transition.",
            "Body material uses procedural wet surface shading to reduce visible texture breaks.",
        ],
        "modelScaleMeters": {"height": MODEL_HEIGHT_M, "width": MODEL_WIDTH_M, "depth": MODEL_DEPTH_M},
        "localLeftWeaponArm": True,
        "attackDamagePlan": {"downslamDamage": 30, "scrapeDamage": 0},
        "futureUnityPaths": [
            "Assets/_Project/Art/Enemies/LongaArma/",
            "Assets/_Project/Prefabs/Enemies/LongaArma/LongaArmaApproved.prefab",
            "docs/validation/longa_arma_cargo_run_scene/",
        ],
        "files": [
            "README.md",
            "TEXTURE_ANALYSIS.md",
            "APPROVAL_STATUS.json",
            "ASSET_MANIFEST.json",
            "index.html",
            "blender/longa_arma.blend",
            "exports/longa_arma.fbx",
            "exports/longa_arma.glb",
            "textures/longa_arma_wet_green_albedo.png",
            "textures/longa_arma_wet_green_roughness.png",
            "textures/longa_arma_wet_green_bump.png",
            "textures/longa_arma_dark_blade_albedo.png",
            "textures/longa_arma_dark_blade_roughness.png",
            "textures/longa_arma_slime_albedo.png",
            "renders/front.png",
            "renders/side.png",
            "renders/back.png",
            "renders/reference_comparison.png",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest_ko, ensure_ascii=False, indent=2), encoding="utf-8")

    approval_ko = {
        "enemyId": "longa_arma",
        "status": "pending_user_approval",
        "approved": False,
        "createdAt": created_at,
        "note": "Unity 적용 전 사용자 검토와 승인이 필요한 artSample 샘플입니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval_ko, ensure_ascii=False, indent=2), encoding="utf-8")

    html_ko = """<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <title>롱가 아르마 승인용 샘플</title>
  <style>
    body { margin: 0; font-family: "Malgun Gothic", Arial, sans-serif; background: #0b0f0d; color: #e8eee9; }
    main { max-width: 1180px; margin: 0 auto; padding: 28px; }
    h1, h2 { font-weight: 650; }
    figure { margin: 18px 0; border: 1px solid #25342c; background: #111915; padding: 12px; }
    img { display: block; width: 100%; height: auto; background: #0b0f0d; object-fit: contain; }
    code { color: #91d39d; }
    .grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 14px; }
  </style>
</head>
<body>
<main>
  <h1>롱가 아르마 승인용 샘플</h1>
  <p>기준 이미지와 비교해 날렵한 몸체, 로컬 왼쪽의 긴 무기 팔, 자연스럽게 변형된 반달형 칼 연결부, 젖은 녹색 점액 표면을 확인하기 위한 샘플입니다.</p>
  <figure>
    <figcaption>기준 이미지 / 샘플 비교</figcaption>
    <img src="renders/reference_comparison.png" alt="reference comparison">
  </figure>
  <div class="grid">
    <figure><figcaption>정면</figcaption><img src="renders/front.png" alt="front"></figure>
    <figure><figcaption>측면</figcaption><img src="renders/side.png" alt="side"></figure>
    <figure><figcaption>후면</figcaption><img src="renders/back.png" alt="back"></figure>
  </div>
  <h2>산출물</h2>
  <p><code>blender/longa_arma.blend</code>, <code>exports/longa_arma.fbx</code>, <code>exports/longa_arma.glb</code>, <code>textures/</code></p>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html_ko, encoding="utf-8")


def write_docs_fuga_style() -> None:
    created_at = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    readme = f"""# 롱가 아르마 모델링 샘플

- 생성 시각: {created_at}
- 상태: 사용자 승인 전 샘플
- 기준 이미지:
  - `image/longa arma(롱가 아르마).png`
  - `image/longa arma-beside.png`
  - `image/longa arma-back.png`

## 반영 내용

- 푸가 샘플처럼 기준 이미지와 생성 렌더를 정면/측면/후면으로 직접 비교할 수 있게 `index.html`을 구성했습니다.
- 표면 점액 표현용 별도 오브젝트, 액체 줄기, 방울, 웅덩이 오브젝트는 생성하지 않습니다.
- 몸체와 사지는 절차형 거친 녹색 피부 머티리얼, 강한 bump, coarse displacement로 매끈한 표면감을 줄였습니다.
- 몸체, 어깨, 엉덩이, 사지 반경을 키워 이전보다 덩치 큰 괴물 체형으로 조정했습니다.
- 목과 머리는 몸통과 하나의 연속 메쉬로 이어지게 구성했습니다.
- 로컬 기준 왼쪽 긴 팔은 몸통 안쪽에서 자라 나와, 낮은 반달형 칼날과 위로 솟은 칼끝으로 경화되는 구조로 조정했습니다.

## 검토 파일

- 정면 렌더: `renders/front.png`
- 측면 렌더: `renders/side.png`
- 후면 렌더: `renders/back.png`
- 기준 이미지/샘플 비교 렌더: `renders/reference_comparison.png`
- Blender 원본: `blender/longa_arma.blend`
- 내보내기: `exports/longa_arma.fbx`, `exports/longa_arma.glb`
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    texture_analysis = """# 롱가 아르마 텍스처/머티리얼 분석

## 몸체

- 몸체는 별도 점액 오브젝트 없이 절차형 거친 녹색 피부 머티리얼, 강한 bump, coarse displacement로 처리합니다.
- 기준 이미지의 어두운 녹색, 청록, 검은 습윤 질감 분석용으로 `longa_arma_wet_green_albedo.png`, `longa_arma_wet_green_bump.png`, `longa_arma_wet_green_roughness.png`를 생성합니다.
- 표면 위에 따로 얹히는 녹색 점액선, 방울, 줄기, 웅덩이 모델링은 제외했습니다.

## 칼날

- 칼날은 갈고리형 고리가 아니라 낮게 깔린 흑회색 반달형 무기이며, 끝점만 위로 솟게 잡았습니다.
- 팔 끝에서는 살 조직이 넓어지고 밑면이 검게 경화되며 칼날 뿌리로 이어지는 형태를 목표로 합니다.
- `longa_arma_dark_blade_albedo.png`, `longa_arma_dark_blade_roughness.png`를 사용합니다.
"""
    (SAMPLE_ROOT / "TEXTURE_ANALYSIS.md").write_text(texture_analysis, encoding="utf-8")

    manifest = {
        "enemyId": "longa_arma",
        "createdAt": created_at,
        "status": "pending_user_approval",
        "sourceReferences": [
            "docs/GAME_DESIGN_SOURCE.txt",
            "docs/LONGA_ARMA_IMPLEMENTATION_PLAN_2026-07-01.md",
            "image/longa arma(롱가 아르마).png",
            "image/longa arma-beside.png",
            "image/longa arma-back.png",
        ],
        "revisionNotes": [
            "Fuga-style review HTML with direct reference/render comparisons.",
            "Surface slime representation objects are not generated.",
            "Coarse procedural body material and displacement reduce the previous smooth surface look.",
            "Body, neck, and head are fused into one continuous section mesh.",
            "Body, shoulder, haunch, limb, foot, and toe radii were enlarged for a heavier monster silhouette.",
            "The local-left blade profile is no longer a curled hook; only the blade tip rises upward.",
            "The local-left weapon arm transitions into the blade through fleshy growth, blackened underside, webbing, and calcified root geometry.",
        ],
        "modelScaleMeters": {"height": MODEL_HEIGHT_M, "width": MODEL_WIDTH_M, "depth": MODEL_DEPTH_M},
        "localLeftWeaponArm": True,
        "attackDamagePlan": {"downslamDamage": 30, "scrapeDamage": 0},
        "files": [
            "README.md",
            "TEXTURE_ANALYSIS.md",
            "APPROVAL_STATUS.json",
            "ASSET_MANIFEST.json",
            "index.html",
            "blender/longa_arma.blend",
            "exports/longa_arma.fbx",
            "exports/longa_arma.glb",
            "textures/longa_arma_wet_green_albedo.png",
            "textures/longa_arma_wet_green_roughness.png",
            "textures/longa_arma_wet_green_bump.png",
            "textures/longa_arma_dark_blade_albedo.png",
            "textures/longa_arma_dark_blade_roughness.png",
            "renders/front.png",
            "renders/side.png",
            "renders/back.png",
            "renders/reference_comparison.png",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "enemyId": "longa_arma",
        "status": "pending_user_approval",
        "approved": False,
        "createdAt": created_at,
        "note": "Unity 적용 전 사용자 검토와 승인이 필요한 artSample 샘플입니다. 표면 점액 표현 오브젝트는 제외했습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    html = """<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <title>롱가 아르마 모델링 샘플</title>
  <style>
    body { margin: 0; font-family: Arial, "Malgun Gothic", sans-serif; background: #18201a; color: #edf2e6; }
    main { max-width: 1180px; margin: 0 auto; padding: 28px; }
    h1, h2, h3 { margin: 0 0 14px; }
    section { margin: 30px 0; }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 16px; }
    .comparison { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-bottom: 22px; background: #222b24; border: 1px solid #435041; padding: 14px; }
    .comparison h3 { grid-column: 1 / -1; }
    figure { margin: 0; background: #111711; border: 1px solid #364236; padding: 10px; }
    img { width: 100%; height: auto; display: block; object-fit: contain; }
    figcaption { margin-top: 8px; font-size: 13px; color: #cdd7c5; word-break: break-all; }
    p, li { line-height: 1.55; }
    code { color: #d7e6c8; }
    @media (max-width: 760px) { .comparison { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
<main>
  <h1>롱가 아르마 모델링 샘플</h1>
  <p>기준 이미지를 직접 비교하기 위한 승인 전 샘플입니다. 몸체는 더 큰 괴물 체형으로 키웠고, 왼팔 칼날은 낮은 반달형과 위로 솟은 칼끝으로 다시 잡았습니다. 표면 점액 표현 오브젝트, 액체 줄기, 방울, 웅덩이는 생성하지 않았습니다.</p>

  <section>
    <h2>기준 이미지와 생성 렌더 비교</h2>
    <article class="comparison">
      <h3>정면</h3>
      <figure><img src="../../../image/longa arma(롱가 아르마).png" alt="정면 기준 이미지"><figcaption>기준 이미지: ../../../image/longa arma(롱가 아르마).png</figcaption></figure>
      <figure><img src="renders/front.png" alt="정면 생성 렌더"><figcaption>생성 렌더: renders/front.png</figcaption></figure>
    </article>
    <article class="comparison">
      <h3>측면</h3>
      <figure><img src="../../../image/longa arma-beside.png" alt="측면 기준 이미지"><figcaption>기준 이미지: ../../../image/longa arma-beside.png</figcaption></figure>
      <figure><img src="renders/side.png" alt="측면 생성 렌더"><figcaption>생성 렌더: renders/side.png</figcaption></figure>
    </article>
    <article class="comparison">
      <h3>후면</h3>
      <figure><img src="../../../image/longa arma-back.png" alt="후면 기준 이미지"><figcaption>기준 이미지: ../../../image/longa arma-back.png</figcaption></figure>
      <figure><img src="renders/back.png" alt="후면 생성 렌더"><figcaption>생성 렌더: renders/back.png</figcaption></figure>
    </article>
  </section>

  <section>
    <h2>생성 렌더</h2>
    <div class="grid">
      <figure><img src="renders/front.png" alt="front"><figcaption>front.png</figcaption></figure>
      <figure><img src="renders/side.png" alt="side"><figcaption>side.png</figcaption></figure>
      <figure><img src="renders/back.png" alt="back"><figcaption>back.png</figcaption></figure>
      <figure><img src="renders/reference_comparison.png" alt="reference comparison"><figcaption>reference_comparison.png</figcaption></figure>
    </div>
  </section>

  <section>
    <h2>사용 텍스처</h2>
    <div class="grid">
      <figure><img src="textures/longa_arma_wet_green_albedo.png" alt="body albedo"><figcaption>longa_arma_wet_green_albedo.png</figcaption></figure>
      <figure><img src="textures/longa_arma_wet_green_bump.png" alt="body bump"><figcaption>longa_arma_wet_green_bump.png</figcaption></figure>
      <figure><img src="textures/longa_arma_wet_green_roughness.png" alt="body roughness"><figcaption>longa_arma_wet_green_roughness.png</figcaption></figure>
      <figure><img src="textures/longa_arma_dark_blade_albedo.png" alt="blade albedo"><figcaption>longa_arma_dark_blade_albedo.png</figcaption></figure>
      <figure><img src="textures/longa_arma_dark_blade_roughness.png" alt="blade roughness"><figcaption>longa_arma_dark_blade_roughness.png</figcaption></figure>
    </div>
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
    texture_paths = create_textures()
    materials = {
        "body": create_wet_body_material("M_LongaArma_Coarse_Procedural_Green_Body"),
        "blade": material_from_texture(
            "M_LongaArma_Dark_Crescent_Blade",
            texture_paths["blade_albedo"],
            roughness=0.52,
            metallic=0.18,
        ),
        "dark": simple_material("M_LongaArma_Dark_Mouth_Recess", (0.015, 0.018, 0.014, 1.0), roughness=0.62),
        "eye": simple_material("M_LongaArma_Wet_Black_Eye", (0.012, 0.018, 0.014, 1.0), roughness=0.12),
        "ridge": simple_material("M_LongaArma_Dark_Recessed_Muscle_Grooves", (0.006, 0.028, 0.022, 1.0), roughness=0.86),
        "blade_edge": simple_material("M_LongaArma_Worn_Bright_Blade_Edge", (0.54, 0.56, 0.52, 1.0), roughness=0.22, metallic=0.45),
    }
    build_model(materials)
    setup_lighting()
    setup_render()
    render_view("front.png", (-0.85, -4.0, 0.62), (-0.28, -0.42, 0.50), 3.05)
    render_view("side.png", (4.0, -0.20, 0.58), (-0.24, -0.72, 0.48), 4.05)
    render_view("back.png", (0.0, 4.0, 0.58), (0.0, -0.04, 0.46), 2.90)
    export_model()
    render_reference_comparison()
    write_docs_fuga_style()
    print("Longa Arma art sample generated at", SAMPLE_ROOT)


if __name__ == "__main__":
    main()
