from __future__ import annotations

import json
import math
import random
import shutil
from datetime import date
from pathlib import Path

import bpy
from mathutils import Euler, Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "parvum"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
LEGACY_GIF_DIR = SAMPLE_ROOT / "animations"
LEGACY_FRAME_DIR = SAMPLE_ROOT / "animation_frames"

BODY_CENTER = Vector((0.0, 0.02, 0.19))
BODY_RADII = Vector((0.255, 0.300, 0.215))


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
    for obsolete in (LEGACY_GIF_DIR, LEGACY_FRAME_DIR):
        if obsolete.exists():
            shutil.rmtree(obsolete)


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
    roughness: float = 0.5,
    alpha: float | None = None,
    emission: tuple[float, float, float, float] | None = None,
    emission_strength: float = 0.0,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    set_principled_input(mat, "Base Color", color)
    set_principled_input(mat, "Metallic", metallic)
    set_principled_input(mat, "Roughness", roughness)
    set_principled_input(mat, "Alpha", alpha if alpha is not None else color[3])
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf and "Transmission Weight" in bsdf.inputs:
        set_principled_input(mat, "Transmission Weight", 0.18 if alpha is not None else 0.0)
    if emission is not None:
        set_principled_input(mat, "Emission Color", emission)
        set_principled_input(mat, "Emission Strength", emission_strength)
    if alpha is not None:
        mat.blend_method = "BLEND"
        mat.use_screen_refraction = True
        mat.show_transparent_back = True
        mat.diffuse_color = (color[0], color[1], color[2], alpha)
    else:
        mat.diffuse_color = color
    return mat


def noisy_material(
    name: str,
    base: tuple[float, float, float, float],
    *,
    high: tuple[float, float, float, float],
    metallic: float = 0.0,
    roughness: float = 0.5,
    alpha: float | None = None,
    scale: float = 18.0,
    detail: float = 11.0,
) -> bpy.types.Material:
    mat = material(name, base, metallic=metallic, roughness=roughness, alpha=alpha)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = scale
    noise.inputs["Detail"].default_value = detail
    noise.inputs["Roughness"].default_value = 0.62

    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.20
    ramp.color_ramp.elements[0].color = base
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = high
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def clamp01(value: float) -> float:
    return max(0.0, min(1.0, value))


def wave_noise(x: float, y: float, seed: float) -> float:
    return 0.5 + 0.5 * math.sin(
        x * (17.0 + seed * 0.7)
        + y * (23.0 + seed * 0.9)
        + math.sin((x - y) * (7.0 + seed)) * 2.7
        + math.sin((x + y) * (31.0 - seed)) * 0.55
    )


def save_texture(path: Path, width: int, height: int, pixel_fn) -> None:
    image = bpy.data.images.new(path.stem, width=width, height=height, alpha=True)
    pixels: list[float] = [0.0] * (width * height * 4)
    for y in range(height):
        v = y / max(1, height - 1)
        for x in range(width):
            u = x / max(1, width - 1)
            r, g, b, a = pixel_fn(u, v)
            index = (y * width + x) * 4
            pixels[index : index + 4] = [clamp01(r), clamp01(g), clamp01(b), clamp01(a)]
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    bpy.data.images.remove(image)


def create_procedural_textures() -> dict[str, Path]:
    width = 512
    height = 512
    random.seed(6280)

    fleck_centers = [
        (random.random(), random.random(), random.uniform(0.018, 0.055), random.uniform(0.006, 0.026))
        for _ in range(58)
    ]

    def slime_albedo(u: float, v: float) -> tuple[float, float, float, float]:
        n1 = wave_noise(u, v, 2.0)
        n2 = wave_noise(u * 1.7 + 0.13, v * 0.8 - 0.24, 7.4)
        vein = abs(math.sin((u * 2.2 + math.sin(v * 7.0) * 0.14 + v * 0.35) * math.tau * 5.2))
        vein = max(0.0, 1.0 - vein * 3.2)
        shade = 0.50 * n1 + 0.32 * n2 + 0.28 * vein
        r = 0.018 + shade * 0.10
        g = 0.22 + shade * 0.52
        b = 0.105 + shade * 0.25
        return r, g, b, 1.0

    def slime_roughness(u: float, v: float) -> tuple[float, float, float, float]:
        n = wave_noise(u * 2.4, v * 1.9, 11.0)
        wet_streak = abs(math.sin((u + math.sin(v * 11.0) * 0.05) * math.tau * 7.0))
        value = 0.12 + n * 0.20 + (1.0 - wet_streak) * 0.08
        return value, value, value, 1.0

    def fleck_mask(u: float, v: float) -> tuple[float, float, float, float]:
        value = 0.0
        for cx, cy, rx, ry in fleck_centers:
            dx = (u - cx) / rx
            dy = (v - cy) / ry
            dist = dx * dx + dy * dy
            if dist < 1.0:
                chipped = wave_noise(u * 9.0, v * 9.0, 4.0)
                value = max(value, (1.0 - dist) * (0.55 + chipped * 0.65))
        value = 1.0 if value > 0.34 else max(0.0, value * 0.65)
        return 0.86 + value * 0.12, 0.86 + value * 0.10, 0.70 + value * 0.12, value

    def snout_albedo(u: float, v: float) -> tuple[float, float, float, float]:
        cell = abs(math.sin(u * math.tau * 23.0) * math.sin(v * math.tau * 19.0))
        pit = 1.0 if cell < 0.08 else 0.0
        mottled = wave_noise(u * 2.8, v * 2.8, 17.0)
        r = 0.11 + mottled * 0.19 - pit * 0.055
        g = 0.15 + mottled * 0.20 - pit * 0.050
        b = 0.10 + mottled * 0.10 - pit * 0.045
        return r, g, b, 1.0

    def snout_bump(u: float, v: float) -> tuple[float, float, float, float]:
        scale_cells = abs(math.sin(u * math.tau * 29.0) * math.sin(v * math.tau * 23.0))
        cracks = abs(math.sin((u + v * 0.37) * math.tau * 41.0))
        value = 0.42 + scale_cells * 0.44 - (1.0 if cracks < 0.035 else 0.0) * 0.28
        return value, value, value, 1.0

    def tooth_albedo(u: float, v: float) -> tuple[float, float, float, float]:
        streak = wave_noise(u * 3.0, v * 6.0, 23.0)
        root_dark = v * 0.22
        return 0.76 + streak * 0.20 - root_dark, 0.66 + streak * 0.18 - root_dark, 0.43 + streak * 0.16 - root_dark, 1.0

    def tongue_albedo(u: float, v: float) -> tuple[float, float, float, float]:
        streak = abs(math.sin((u + math.sin(v * 8.0) * 0.07) * math.tau * 8.0))
        wet = wave_noise(u * 2.0, v * 2.2, 31.0)
        return 0.46 + wet * 0.42, 0.035 + streak * 0.08, 0.022 + wet * 0.05, 1.0

    textures = {
        "slime_albedo": TEXTURE_DIR / "parvum_slime_albedo.png",
        "slime_roughness": TEXTURE_DIR / "parvum_slime_roughness.png",
        "fleck_mask": TEXTURE_DIR / "parvum_white_fleck_mask.png",
        "snout_albedo": TEXTURE_DIR / "parvum_snout_scale_albedo.png",
        "snout_bump": TEXTURE_DIR / "parvum_snout_scale_bump.png",
        "tooth_albedo": TEXTURE_DIR / "parvum_tooth_albedo.png",
        "tongue_albedo": TEXTURE_DIR / "parvum_tongue_albedo.png",
    }
    save_texture(textures["slime_albedo"], width, height, slime_albedo)
    save_texture(textures["slime_roughness"], width, height, slime_roughness)
    save_texture(textures["fleck_mask"], width, height, fleck_mask)
    save_texture(textures["snout_albedo"], width, height, snout_albedo)
    save_texture(textures["snout_bump"], width, height, snout_bump)
    save_texture(textures["tooth_albedo"], width, height, tooth_albedo)
    save_texture(textures["tongue_albedo"], width, height, tongue_albedo)
    return textures


def textured_material(
    name: str,
    base: tuple[float, float, float, float],
    texture_path: Path,
    *,
    roughness_path: Path | None = None,
    bump_path: Path | None = None,
    metallic: float = 0.0,
    roughness: float = 0.5,
    alpha: float | None = None,
    bump_strength: float = 0.08,
    transmission: float = 0.0,
) -> bpy.types.Material:
    mat = material(name, base, metallic=metallic, roughness=roughness, alpha=alpha)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    tex = nodes.new(type="ShaderNodeTexImage")
    tex.name = name + " albedo texture"
    tex.image = bpy.data.images.load(str(texture_path))
    tex.extension = "REPEAT"
    links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])

    if roughness_path is not None:
        rough_tex = nodes.new(type="ShaderNodeTexImage")
        rough_tex.name = name + " roughness texture"
        rough_tex.image = bpy.data.images.load(str(roughness_path))
        rough_tex.image.colorspace_settings.name = "Non-Color"
        links.new(rough_tex.outputs["Color"], bsdf.inputs["Roughness"])

    if bump_path is not None:
        bump_tex = nodes.new(type="ShaderNodeTexImage")
        bump_tex.name = name + " bump texture"
        bump_tex.image = bpy.data.images.load(str(bump_path))
        bump_tex.image.colorspace_settings.name = "Non-Color"
        bump = nodes.new(type="ShaderNodeBump")
        bump.inputs["Strength"].default_value = bump_strength
        bump.inputs["Distance"].default_value = 0.065
        links.new(bump_tex.outputs["Color"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    if "Transmission Weight" in bsdf.inputs:
        bsdf.inputs["Transmission Weight"].default_value = transmission
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.55
    elif "Specular" in bsdf.inputs:
        bsdf.inputs["Specular"].default_value = 0.45
    if "Alpha" in bsdf.inputs and alpha is not None:
        bsdf.inputs["Alpha"].default_value = alpha
    return mat


def build_materials(textures: dict[str, Path]) -> dict[str, bpy.types.Material]:
    return {
        "slime": textured_material(
            "translucent layered emerald slime",
            (0.016, 0.28, 0.12, 0.58),
            textures["slime_albedo"],
            roughness_path=textures["slime_roughness"],
            roughness=0.22,
            alpha=0.58,
            transmission=0.22,
        ),
        "outer_slime": textured_material(
            "glassy wet outer slime skin",
            (0.22, 0.98, 0.56, 0.22),
            textures["slime_albedo"],
            roughness_path=textures["slime_roughness"],
            roughness=0.16,
            alpha=0.18,
            transmission=0.24,
        ),
        "puddle": material("thin transparent floor slime spread", (0.025, 0.42, 0.18, 0.36), roughness=0.18, alpha=0.36),
        "internal": material("dark internal green currents", (0.0, 0.13, 0.065, 1), roughness=0.34),
        "highlight": material("white green wet specular ridges", (0.72, 1.0, 0.78, 0.50), roughness=0.09, alpha=0.50),
        "snout": textured_material(
            "rough grey green scaled snout",
            (0.12, 0.17, 0.11, 1),
            textures["snout_albedo"],
            bump_path=textures["snout_bump"],
            roughness=0.86,
            bump_strength=0.18,
        ),
        "scale_dark": material("dark snout scale pits", (0.030, 0.040, 0.030, 1), roughness=0.80),
        "mouth": material("deep black wet mouth cavity", (0.003, 0.004, 0.003, 1), roughness=0.18),
        "tongue": textured_material("red wet tongue", (0.48, 0.055, 0.025, 1), textures["tongue_albedo"], roughness=0.24),
        "tooth": textured_material("yellow ivory irregular teeth", (0.72, 0.62, 0.40, 1), textures["tooth_albedo"], roughness=0.58),
        "metal": textured_material(
            "chalky white metallic slime flecks",
            (0.72, 0.72, 0.62, 1),
            textures["fleck_mask"],
            metallic=0.35,
            roughness=0.36,
        ),
        "saliva": material("clear hanging saliva", (0.82, 1.0, 0.88, 0.45), roughness=0.05, alpha=0.45),
        "floor": material("dark neutral sample floor", (0.10, 0.115, 0.108, 1), roughness=0.74),
        "scale": material("matte scale marker", (0.12, 0.24, 0.34, 1), roughness=0.72),
        "scale_tick": material("light scale tick paint", (0.92, 0.94, 0.88, 1), roughness=0.55),
    }


def add_empty(name: str, parent: bpy.types.Object | None = None) -> bpy.types.Object:
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    return obj


def add_uv_sphere(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    *,
    segments: int = 48,
    rings: int = 24,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    bpy.ops.object.shade_smooth()
    return obj


def add_box(name: str, parent: bpy.types.Object, loc: tuple[float, float, float], scale: tuple[float, float, float], mat: bpy.types.Material) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def add_curve(
    name: str,
    parent: bpy.types.Object,
    points: list[tuple[float, float, float]],
    mat: bpy.types.Material,
    *,
    bevel_depth: float = 0.004,
    bevel_resolution: int = 3,
) -> bpy.types.Object:
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 3
    curve.bevel_depth = bevel_depth
    curve.bevel_resolution = bevel_resolution
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, co in zip(spline.points, points):
        point.co = (co[0], co[1], co[2], 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def add_torus(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    mat: bpy.types.Material,
    *,
    major_radius: float,
    minor_radius: float,
    scale: tuple[float, float, float],
    rot: tuple[float, float, float],
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_segments=96,
        minor_segments=18,
        major_radius=major_radius,
        minor_radius=minor_radius,
        location=loc,
        rotation=rot,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    bpy.ops.object.shade_smooth()
    return obj


def add_tooth(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    mat: bpy.types.Material,
    *,
    length: float,
    radius: float,
    points_down: bool,
    lean: float,
) -> bpy.types.Object:
    radius1, radius2 = (0.0, radius) if points_down else (radius, 0.0)
    bpy.ops.mesh.primitive_cone_add(vertices=9, radius1=radius1, radius2=radius2, depth=length, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler[0] = lean
    obj.data.materials.append(mat)
    obj.parent = parent
    bpy.ops.object.shade_smooth()
    return obj


def add_noise_displace(obj: bpy.types.Object, name: str, *, strength: float, scale: float, detail: float) -> None:
    texture = bpy.data.textures.new(name, "VORONOI")
    texture.noise_scale = scale
    texture.intensity = 0.45
    texture.contrast = detail
    modifier = obj.modifiers.new(name, "DISPLACE")
    modifier.strength = strength
    modifier.texture = texture
    obj.modifiers.new(name + " weighted normals", "WEIGHTED_NORMAL")


def build_parvum_model(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> dict[str, object]:
    floor = add_box("dark review floor plane", root, (0.0, 0.0, -0.011), (1.18, 1.18, 0.018), mats["floor"])
    model_root = add_empty("parvum animated model root", root)
    body_parts: list[bpy.types.Object] = []
    mouth_parts: list[bpy.types.Object] = []
    all_animated: list[bpy.types.Object] = [model_root]

    for name, loc, scale in [
        ("front thin slime floor rim", (0.0, -0.055, 0.022), (0.320, 0.390, 0.016)),
        ("left sagging floor slime rim", (-0.160, -0.012, 0.026), (0.200, 0.285, 0.014)),
        ("right sagging floor slime rim", (0.168, 0.002, 0.026), (0.190, 0.270, 0.014)),
        ("back thin slime floor rim", (0.0, 0.158, 0.022), (0.250, 0.230, 0.012)),
    ]:
        obj = add_uv_sphere(name, model_root, loc, scale, mats["puddle"], segments=48, rings=12)
        body_parts.append(obj)

    for name, loc, scale, mat_name, strength in [
        ("low broad translucent green slime mound", tuple(BODY_CENTER), tuple(BODY_RADII), "slime", 0.018),
        ("thin glossy outer slime skin", tuple(BODY_CENTER + Vector((0.0, 0.0, 0.006))), (0.268, 0.313, 0.220), "outer_slime", 0.010),
        ("left lower sagging slime lobe", (-0.195, -0.012, 0.125), (0.145, 0.235, 0.112), "slime", 0.012),
        ("right lower sagging slime lobe", (0.198, 0.000, 0.122), (0.140, 0.232, 0.108), "slime", 0.012),
        ("rear rounded slime mass", (0.0, 0.215, 0.152), (0.225, 0.175, 0.140), "slime", 0.012),
        ("front mouth cradle slime mass", (0.0, -0.214, 0.146), (0.184, 0.116, 0.118), "slime", 0.010),
        ("upper crest translucent slime dome", (0.0, 0.012, 0.308), (0.180, 0.205, 0.113), "slime", 0.010),
    ]:
        obj = add_uv_sphere(name, model_root, loc, scale, mats[mat_name], segments=80 if "mound" in name else 56, rings=36 if "mound" in name else 24)
        add_noise_displace(obj, name + " lumpy displacement", strength=strength, scale=0.55, detail=2.8)
        body_parts.append(obj)

    mouth_root = add_empty("parvum animated mouth snout root", model_root)
    snout = add_uv_sphere("front protruding rough snout with two nostrils", mouth_root, (0.0, -0.298, 0.214), (0.124, 0.077, 0.063), mats["snout"], segments=72, rings=28)
    add_noise_displace(snout, "pebbled snout scale displacement", strength=0.007, scale=0.19, detail=3.4)
    mouth_parts.append(snout)
    mouth_parts.append(add_torus("large oval fleshy mouth lip ring", mouth_root, (0.0, -0.351, 0.195), mats["snout"], major_radius=0.076, minor_radius=0.018, scale=(1.35, 0.78, 1.0), rot=(math.radians(90), 0.0, 0.0)))
    mouth_parts.append(add_uv_sphere("deep black open mouth cavity", mouth_root, (0.0, -0.370, 0.190), (0.100, 0.015, 0.073), mats["mouth"], segments=48, rings=16))
    upper_lip_cover = add_uv_sphere("continuous upper lip cover over tooth roots", mouth_root, (0.0, -0.375, 0.244), (0.108, 0.034, 0.024), mats["snout"], segments=56, rings=16)
    add_noise_displace(upper_lip_cover, "upper lip cover pebbled displacement", strength=0.0035, scale=0.16, detail=2.6)
    mouth_parts.append(upper_lip_cover)
    mouth_parts.append(add_uv_sphere("upper tooth root gum embedded in lip", mouth_root, (0.0, -0.386, 0.229), (0.092, 0.015, 0.012), mats["snout"], segments=40, rings=10))
    mouth_parts.append(add_uv_sphere("lower tooth root gum embedded in lip", mouth_root, (0.0, -0.386, 0.140), (0.086, 0.015, 0.010), mats["snout"], segments=40, rings=10))
    mouth_parts.append(add_uv_sphere("red tongue and inner flesh visible inside mouth", mouth_root, (0.0, -0.386, 0.153), (0.057, 0.033, 0.019), mats["tongue"], segments=40, rings=16))

    for index, loc in enumerate([(-0.037, -0.357, 0.256), (0.037, -0.357, 0.256)], start=1):
        mouth_parts.append(add_uv_sphere(f"small black nostril {index}", mouth_root, loc, (0.012, 0.004, 0.006), mats["mouth"], segments=24, rings=8))

    for index, x in enumerate([-0.074, -0.056, -0.038, -0.020, 0.0, 0.020, 0.038, 0.056, 0.074], start=1):
        length = 0.042 + 0.012 * (index % 3) + (0.006 if index in (2, 8) else 0.0)
        root_z = 0.229 - 0.006 * abs(x)
        mouth_parts.append(add_tooth(f"upper irregular tooth {index}", mouth_root, (x, -0.386, root_z - length * 0.5), mats["tooth"], length=length, radius=0.0067 + 0.001 * (index % 2), points_down=True, lean=math.radians(-3 + index % 4)))
    for index, x in enumerate([-0.068, -0.049, -0.030, -0.010, 0.011, 0.031, 0.050, 0.069], start=1):
        length = 0.037 + 0.012 * ((index + 1) % 3)
        root_z = 0.140 + 0.004 * abs(x)
        mouth_parts.append(add_tooth(f"lower irregular tooth {index}", mouth_root, (x, -0.386, root_z + length * 0.5), mats["tooth"], length=length, radius=0.0062 + 0.001 * (index % 2), points_down=False, lean=math.radians(3 - index % 5)))

    random.seed(6128)
    for index in range(28):
        x = random.uniform(-0.075, 0.075)
        z = random.uniform(0.190, 0.260)
        y = -0.362 - random.uniform(0.004, 0.010)
        mouth_parts.append(add_uv_sphere(f"snout raised dark scale pore {index + 1}", mouth_root, (x, y, z), (0.0045, 0.0016, 0.0032), mats["scale_dark"], segments=10, rings=6))

    current_lines = [
        [(-0.130, -0.262, 0.175), (-0.060, -0.282, 0.235), (0.020, -0.250, 0.263), (0.090, -0.225, 0.232)],
        [(-0.188, -0.102, 0.116), (-0.120, -0.080, 0.158), (-0.052, -0.060, 0.145), (0.038, -0.040, 0.178)],
        [(0.158, -0.132, 0.116), (0.120, -0.058, 0.162), (0.188, 0.020, 0.150), (0.138, 0.084, 0.188)],
        [(-0.145, 0.140, 0.130), (-0.068, 0.190, 0.166), (0.020, 0.168, 0.198), (0.104, 0.214, 0.170)],
        [(-0.100, -0.015, 0.276), (-0.038, 0.028, 0.325), (0.050, 0.004, 0.333), (0.106, -0.036, 0.286)],
    ]
    for index, points in enumerate(current_lines, start=1):
        body_parts.append(add_curve(f"visible internal green current line {index}", model_root, points, mats["internal"], bevel_depth=0.0035))

    all_animated.extend(body_parts)
    all_animated.append(mouth_root)
    all_animated.extend(mouth_parts)
    return {
        "root": model_root,
        "floor": floor,
        "body": body_parts,
        "mouth_root": mouth_root,
        "mouth": mouth_parts,
        "all": all_animated,
    }


def build_scale_group(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> list[bpy.types.Object]:
    scale_root = add_empty("scale check one meter reference group", root)
    objects: list[bpy.types.Object] = [scale_root]
    objects.append(add_box("one meter scale bar", scale_root, (0.42, 0.02, 0.50), (0.018, 0.018, 1.0), mats["scale"]))
    objects.append(add_box("forty centimeter height marker", scale_root, (0.42, 0.02, 0.40), (0.085, 0.018, 0.012), mats["scale_tick"]))
    objects.append(add_box("zero meter scale foot", scale_root, (0.42, 0.02, 0.004), (0.105, 0.035, 0.008), mats["scale_tick"]))
    objects.append(add_box("one meter scale cap", scale_root, (0.42, 0.02, 1.0), (0.105, 0.035, 0.008), mats["scale_tick"]))
    for obj in objects:
        obj.hide_render = True
        obj.hide_viewport = True
    return objects


def set_render_engine(preferred: str) -> None:
    scene = bpy.context.scene
    try:
        scene.render.engine = preferred
    except Exception:
        scene.render.engine = "BLENDER_WORKBENCH"


def configure_rendering() -> None:
    scene = bpy.context.scene
    set_render_engine("CYCLES")
    if scene.render.engine == "CYCLES":
        scene.cycles.samples = 48
        scene.cycles.use_denoising = True
    scene.render.resolution_x = 1400
    scene.render.resolution_y = 900
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.render.film_transparent = False
    if scene.world is None:
        scene.world = bpy.data.worlds.new("ParvumSampleWorld")
    scene.world.color = (0.025, 0.028, 0.026)


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -0.55, 1.18))
    key = bpy.context.object
    key.name = "large front wet slime softbox"
    key.data.energy = 310
    key.data.size = 1.45

    bpy.ops.object.light_add(type="AREA", location=(-0.62, 0.42, 0.85))
    left = bpy.context.object
    left.name = "cool translucent side rim"
    left.data.energy = 110
    left.data.size = 0.72
    left.data.color = (0.58, 0.90, 1.0)

    bpy.ops.object.light_add(type="AREA", location=(0.68, -0.38, 0.58))
    right = bpy.context.object
    right.name = "warm mouth teeth fill"
    right.data.energy = 85
    right.data.size = 0.50
    right.data.color = (1.0, 0.68, 0.48)


def add_camera(name: str, loc: tuple[float, float, float], target: tuple[float, float, float], *, ortho_scale: float) -> bpy.types.Object:
    bpy.ops.object.camera_add(location=loc)
    camera = bpy.context.object
    camera.name = "parvum camera " + name
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale
    camera.data.dof.use_dof = False
    return camera


def set_scale_group_visibility(scale_group: list[bpy.types.Object], visible: bool) -> None:
    for obj in scale_group:
        obj.hide_render = not visible
        obj.hide_viewport = not visible


def render_camera(camera: bpy.types.Object, output_path: Path, scale_group: list[bpy.types.Object], *, show_scale: bool = False) -> None:
    set_scale_group_visibility(scale_group, show_scale)
    bpy.context.scene.camera = camera
    bpy.context.scene.render.filepath = str(output_path)
    bpy.ops.render.render(write_still=True)
    set_scale_group_visibility(scale_group, False)


def export_assets(parts: dict[str, object]) -> None:
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "parvum.blend"))
    bpy.ops.object.select_all(action="DESELECT")
    export_objects = [obj for obj in parts["all"] if isinstance(obj, bpy.types.Object) and obj.type in {"EMPTY", "MESH"}]
    for obj in export_objects:
        obj.select_set(True)
    if export_objects:
        bpy.context.view_layer.objects.active = export_objects[0]
    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_DIR / "parvum.fbx"),
        use_selection=True,
        add_leaf_bones=False,
        bake_space_transform=False,
    )
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_DIR / "parvum.glb"),
        export_format="GLB",
        use_selection=True,
        export_animations=False,
    )


def generated_files() -> list[str]:
    return [
        "blender/parvum.blend",
        "exports/parvum.fbx",
        "exports/parvum.glb",
        "textures/parvum_slime_albedo.png",
        "textures/parvum_slime_roughness.png",
        "textures/parvum_white_fleck_mask.png",
        "textures/parvum_snout_scale_albedo.png",
        "textures/parvum_snout_scale_bump.png",
        "textures/parvum_tooth_albedo.png",
        "textures/parvum_tongue_albedo.png",
        "renders/01_front.png",
        "renders/02_side.png",
        "renders/03_back.png",
        "renders/04_top.png",
        "renders/05_bite_pose.png",
        "renders/06_scale_check.png",
        "index.html",
        "README.md",
        "ASSET_MANIFEST.json",
        "APPROVAL_STATUS.json",
        "TEXTURE_ANALYSIS.md",
    ]


def write_texture_analysis() -> None:
    analysis = """# 파르붐 텍스처/머티리얼 분석

## 기준 이미지 분석

- 외형: 낮고 넓은 반구형 점액 덩어리이며, 바닥 접촉부가 얇게 퍼져 있습니다.
- 색 분포: 주색은 반투명 에메랄드/녹색이고 내부에는 더 어두운 녹색 흐름이 겹칩니다.
- 표면 패턴: 표면 전체에 흐르는 듯한 점액 줄무늬와 불규칙한 내부 덩어리 무늬가 보입니다.
- 광택/거칠기: 몸체는 젖은 고광택 재질입니다. 거칠기는 낮지만 표면 요철 때문에 하이라이트가 끊어져 보입니다.
- 투명도: 몸체 외피는 반투명이며 내부 흐름이 비쳐야 합니다.
- 오염/손상: 흰색 또는 옅은 금속성 박락 조각이 몸체 곳곳에 붙어 있습니다.
- 요철감: 몸체는 완전히 매끈하지 않고 흘러내린 덩어리와 둔한 융기가 있습니다.
- 주둥이: 녹색 점액과 다른 회녹색 생체 피부이며, 비늘/모공 같은 거친 질감과 검은 콧구멍이 있습니다.
- 입/이빨: 입 안은 검고 젖어 있으며, 이빨은 누런 상아색이고 길이와 기울기가 불규칙합니다.

## 제작 반영

- `parvum_slime_albedo.png`: 녹색 점액의 색 변화와 내부 흐름 무늬를 직접 생성했습니다.
- `parvum_slime_roughness.png`: 젖은 광택과 끊긴 하이라이트를 만들기 위한 거칠기 변화를 직접 생성했습니다.
- `parvum_white_fleck_mask.png`: 흰색/옅은 금속성 박락 조각용 텍스처를 직접 생성했습니다.
- `parvum_snout_scale_albedo.png`: 주둥이의 회녹색 비늘성 피부색을 직접 생성했습니다.
- `parvum_snout_scale_bump.png`: 주둥이의 모공, 비늘, 균열 요철감을 범프용 텍스처로 직접 생성했습니다.
- `parvum_tooth_albedo.png`: 누런 상아색 이빨의 얼룩과 뿌리 쪽 어두운 색을 직접 생성했습니다.
- `parvum_tongue_albedo.png`: 붉고 젖은 혀 표면의 색 줄무늬를 직접 생성했습니다.

## 한계

- 이 샘플은 `artSample/` 승인용 정적 모델링/텍스처 샘플입니다.
- 애니메이션 GIF는 새 적대 개체 샘플 규칙상 필수 조건이 아니므로 포함하지 않았습니다.
- Unity 적용, 리깅, Animator, AnimationClip, AI, 공격/피격 판정은 포함하지 않았습니다.
"""
    (SAMPLE_ROOT / "TEXTURE_ANALYSIS.md").write_text(analysis, encoding="utf-8")


def write_docs_v2() -> None:
    write_texture_analysis()

    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "ENEMY-SEED-PARVUM",
        "title": "파르붐 승인용 3D 모델링/텍스처 샘플",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "generatedDate": date.today().isoformat(),
        "sampleRule": "적대 개체 샘플 제작 규칙 - 텍스처/머티리얼 적용 필수, 애니메이션 샘플 필수 아님",
        "scaleMeters": {
            "designHeight": 0.40,
            "designWidth": 0.35,
            "designDepth": 0.40,
            "note": "시각 샘플은 낮고 넓은 점액 테두리를 포함하며, 실제 충돌체는 승인 후 별도 정의합니다.",
        },
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt: 파르붐은 높이 40cm, 가로 약 35cm, 세로 약 40cm인 소형 씨앗체입니다.",
            "docs/GAME_DESIGN_SOURCE.txt: 전체 외형은 액체 덩어리에 입이 달린 모습이며, 근접해서 입으로 대상을 물어뜯습니다.",
            "image/parvum(파르붐).png, image/parvum-back.png, image/parvum-beside.png",
            "AGENTS.md: 적대 개체 샘플은 기준 이미지의 외형, 실루엣, 재질, 질감, 텍스처, 머티리얼을 최대한 맞춰야 합니다.",
        ],
        "textureMaterialWork": {
            "textureApplied": True,
            "materialApplied": True,
            "proceduralTexturesCreated": True,
            "animationSampleRequired": False,
            "notes": [
                "반투명 녹색 점액 알베도/거칠기 텍스처를 생성해 몸체 머티리얼에 연결했습니다.",
                "주둥이 비늘 알베도/범프 텍스처를 생성해 거친 생체 피부 머티리얼에 연결했습니다.",
                "흰색 박락, 이빨, 혀 텍스처를 생성해 각 부위 머티리얼에 적용했습니다.",
            ],
        },
        "includedParts": [
            "낮고 넓은 반투명 녹색 점액 덩어리",
            "바닥에 얇게 퍼진 점액 테두리",
            "내부 녹색 흐름과 젖은 표면 하이라이트",
            "흰색/옅은 금속성 박락 조각",
            "앞쪽으로 돌출된 거친 회녹색 주둥이와 콧구멍",
            "크게 열린 검은 입, 붉은 혀, 불규칙한 누런 이빨",
            "직접 생성한 텍스처 PNG와 Blender 머티리얼 노드 적용",
        ],
        "excludedParts": [
            "애니메이션 GIF",
            "Unity 씬, 프리팹, 런타임 에셋 연결",
            "Animator, AnimationClip, AI, 공격 판정, 피격 판정, 이동 로직",
        ],
        "generatedFiles": generated_files(),
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "ENEMY-SEED-PARVUM",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "textureApplied": True,
        "materialApplied": True,
        "animationSampleRequired": False,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 에셋에 연결하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# parvum

파르붐 적대 개체 모델링/텍스처 승인용 Blender 샘플입니다.

## 기준

- 원본 기획서 기준: 높이 40cm, 가로 약 35cm, 세로 약 40cm의 소형 씨앗체입니다.
- 외형 기준: 액체 덩어리에 입이 달린 모습이며, 근접해서 입으로 대상을 물어뜯습니다.
- 이미지 기준: `image/parvum(파르붐).png`, `image/parvum-back.png`, `image/parvum-beside.png`를 참고했습니다.
- 새 적대 개체 샘플 규칙 기준: 애니메이션 GIF는 필수 조건이 아니며, 외형/질감/텍스처/머티리얼 재현을 우선합니다.

## 포함

- 반투명 녹색 점액 몸체와 얇게 퍼진 바닥 점액
- 내부 녹색 흐름, 젖은 하이라이트, 흰색/옅은 금속성 박락 조각
- 회녹색 비늘성 주둥이, 콧구멍, 검은 입, 붉은 혀, 누런 불규칙 이빨
- 직접 생성한 텍스처 PNG 7종
- 텍스처를 연결한 Blender 머티리얼
- 정적 렌더 6종, Blender 원본, FBX, GLB, HTML 미리보기

## 제외

- 애니메이션 GIF
- Unity 씬, 프리팹, 런타임 에셋 연결
- Animator, AnimationClip, AI, 이동, 공격 판정, 피격 판정

## 승인 상태

현재 상태는 `미승인`입니다. 사용자 승인 전에는 Unity에 반영하지 않습니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    comparison_rows = [
        ("정면", "../../../image/parvum(파르붐).png", "renders/01_front.png", "입, 이빨, 콧구멍, 정면 실루엣 비교"),
        ("측면", "../../../image/parvum-beside.png", "renders/02_side.png", "돌출 주둥이와 낮은 점액 덩어리 비교"),
        ("후면", "../../../image/parvum-back.png", "renders/03_back.png", "입 없는 후면 점액 덩어리와 흰색 박락 비교"),
    ]
    comparison_html = "\n".join(
        f"""
      <article>
        <h3>{title}</h3>
        <div class="pair">
          <figure><a href="{ref}"><img src="{ref}" alt="{title} 원본 이미지"></a><figcaption>원본 이미지</figcaption></figure>
          <figure><a href="{render}"><img src="{render}" alt="{title} 샘플 렌더"></a><figcaption>샘플 렌더</figcaption></figure>
        </div>
        <p>{caption}</p>
      </article>"""
        for title, ref, render, caption in comparison_rows
    )
    render_cards = "\n".join(
        f'      <figure><a href="renders/{name}"><img src="renders/{name}" alt="{label}"></a><figcaption>{label}</figcaption></figure>'
        for name, label in [
            ("01_front.png", "01 정면"),
            ("02_side.png", "02 측면"),
            ("03_back.png", "03 후면"),
            ("04_top.png", "04 상단"),
            ("05_bite_pose.png", "05 입/주둥이 확인"),
            ("06_scale_check.png", "06 40cm 스케일 확인"),
        ]
    )
    texture_cards = "\n".join(
        f'      <figure><a href="textures/{name}"><img src="textures/{name}" alt="{label}"></a><figcaption>{label}</figcaption></figure>'
        for name, label in [
            ("parvum_slime_albedo.png", "점액 알베도"),
            ("parvum_slime_roughness.png", "점액 거칠기"),
            ("parvum_white_fleck_mask.png", "흰색 박락 마스크"),
            ("parvum_snout_scale_albedo.png", "주둥이 비늘 알베도"),
            ("parvum_snout_scale_bump.png", "주둥이 비늘 범프"),
            ("parvum_tooth_albedo.png", "이빨 알베도"),
            ("parvum_tongue_albedo.png", "혀 알베도"),
        ]
    )
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>parvum modeling texture sample</title>
  <style>
    body {{ margin: 0; background: #101412; color: #ece8dc; font-family: Arial, sans-serif; }}
    main {{ max-width: 1240px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    h2 {{ margin: 28px 0 12px; font-size: 20px; }}
    h3 {{ margin: 0 0 10px; font-size: 17px; }}
    p {{ color: #cec7b7; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    .texture-grid {{ display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; }}
    .pair {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }}
    article {{ border: 1px solid #3c4a42; background: #18201c; padding: 14px; margin-bottom: 14px; }}
    figure {{ margin: 0; border: 1px solid #35443b; background: #0b0f0d; padding: 8px; }}
    img {{ width: 100%; display: block; }}
    figcaption {{ margin-top: 8px; color: #d8d0bd; font-size: 14px; }}
    code {{ color: #c9efc8; }}
    @media (max-width: 820px) {{ .grid, .texture-grid, .pair {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>파르붐 모델링/텍스처 샘플</h1>
  <p>변경된 적대 개체 샘플 규칙에 맞춰 애니메이션 GIF를 제외하고, 기준 이미지의 외형, 실루엣, 색 분포, 재질, 질감, 텍스처, 머티리얼 재현에 집중한 승인용 샘플입니다. Unity 씬, 프리팹, 런타임 에셋에는 연결하지 않았습니다.</p>
  <p>승인 상태: <code>미승인</code>, Unity 적용 허용: <code>false</code></p>

  <h2>원본 이미지 비교</h2>
{comparison_html}

  <h2>정적 렌더</h2>
  <section class="grid">
{render_cards}
  </section>

  <h2>직접 생성 텍스처</h2>
  <section class="texture-grid">
{texture_cards}
  </section>

  <h2>검토 파일</h2>
  <p><code>TEXTURE_ANALYSIS.md</code>에 이미지 분석과 텍스처/머티리얼 반영 내용을 정리했습니다.</p>
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

    root = add_empty("parvum artSample approval root")
    textures = create_procedural_textures()
    mats = build_materials(textures)
    parts = build_parvum_model(root, mats)
    scale_group = build_scale_group(root, mats)
    add_render_lights()

    static_cameras = [
        ("front", (0.0, -1.28, 0.245), (0.0, -0.030, 0.205), 0.88, "01_front.png", False),
        ("side", (-1.20, -0.030, 0.245), (0.0, -0.025, 0.205), 0.88, "02_side.png", False),
        ("back", (0.0, 1.22, 0.245), (0.0, 0.035, 0.205), 0.88, "03_back.png", False),
        ("top", (0.0, -0.020, 1.40), (0.0, -0.010, 0.130), 0.82, "04_top.png", False),
        ("bite pose", (0.40, -1.18, 0.345), (0.0, -0.290, 0.190), 0.68, "05_bite_pose.png", False),
        ("scale check", (0.54, -1.48, 0.440), (0.08, -0.035, 0.320), 1.20, "06_scale_check.png", True),
    ]
    for name, loc, target, ortho_scale, output, show_scale in static_cameras:
        camera = add_camera(name, loc, target, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output, scale_group, show_scale=show_scale)

    export_assets(parts)
    write_docs_v2()


if __name__ == "__main__":
    main()
    bpy.ops.wm.quit_blender()
