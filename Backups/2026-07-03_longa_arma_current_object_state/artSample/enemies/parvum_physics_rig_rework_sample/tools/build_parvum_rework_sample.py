from __future__ import annotations

import json
import math
import random
from pathlib import Path

import bpy
from mathutils import Vector


SAMPLE_ROOT = Path(__file__).resolve().parents[1]
BLENDER_DIR = SAMPLE_ROOT / "blender"
EXPORT_DIR = SAMPLE_ROOT / "exports"
RENDER_DIR = SAMPLE_ROOT / "renders"
TEXTURE_DIR = SAMPLE_ROOT / "textures"

BODY_OBJECT_NAME = "Unified_Parvum_Reference_Matched_Single_Mesh"
SLIME_ALBEDO_TEXTURE = "parvum_slime_albedo.png"
SLIME_ROUGHNESS_TEXTURE = "parvum_slime_roughness.png"
SLIME_BUMP_TEXTURE = "parvum_slime_bump.png"
WHITE_FLECK_MASK_TEXTURE = "parvum_white_fleck_mask.png"
MUZZLE_ALBEDO_TEXTURE = "parvum_muzzle_scale_albedo.png"
MUZZLE_BUMP_TEXTURE = "parvum_muzzle_scale_bump.png"
MOUTH_ALBEDO_TEXTURE = "parvum_mouth_cavity_albedo.png"
TOOTH_ALBEDO_TEXTURE = "parvum_tooth_albedo.png"
TONGUE_ALBEDO_TEXTURE = "parvum_tongue_albedo.png"

GENERATED_TEXTURES = [
    "parvum_reference_matched_slime_albedo.png",
    "parvum_single_body_slime_albedo.png",
    "parvum_embedded_muzzle_albedo.png",
    SLIME_ALBEDO_TEXTURE,
    SLIME_ROUGHNESS_TEXTURE,
    SLIME_BUMP_TEXTURE,
    WHITE_FLECK_MASK_TEXTURE,
    MUZZLE_ALBEDO_TEXTURE,
    MUZZLE_BUMP_TEXTURE,
    MOUTH_ALBEDO_TEXTURE,
    TOOTH_ALBEDO_TEXTURE,
    TONGUE_ALBEDO_TEXTURE,
]

SHAPE_KEYS = [
    "Idle_Pulse_Surface_Jiggle",
    "Move_Squash_Forward_Slosh",
    "Attack_Bite_Core_Kick",
    "Hit_Recoil_Side_Wave",
    "Death_Flatten_Liquid_Spread",
]

MATERIAL_SLOTS = {
    "slime": 0,
    "slime_dark": 1,
    "slime_highlight": 2,
    "muzzle": 3,
    "muzzle_dark": 4,
    "mouth": 5,
    "tooth": 6,
    "tongue": 7,
}

WHITE_PATCHES = [
    (0.34, -0.38, 0.62, 0.075, 0.16, 0.070),
    (-0.28, -0.44, 0.54, 0.055, 0.13, 0.060),
    (0.48, -0.05, 0.36, 0.060, 0.16, 0.055),
    (-0.48, -0.08, 0.30, 0.045, 0.14, 0.050),
    (0.12, 0.22, 0.55, 0.052, 0.15, 0.060),
]

DARK_PATCHES = [
    (-0.20, -0.33, 0.48, 0.13, 0.20, 0.08),
    (0.22, 0.18, 0.36, 0.15, 0.22, 0.08),
    (-0.42, 0.12, 0.25, 0.12, 0.18, 0.07),
]

TEXTURE_USAGE = [
    ("점액 몸통 알베도", SLIME_ALBEDO_TEXTURE, "몸통 전체", "짙은 초록 점액의 내부 마블링과 색 변화"),
    ("점액 몸통 거칠기", SLIME_ROUGHNESS_TEXTURE, "몸통 전체", "젖은 부분과 탁한 부분의 roughness 차이"),
    ("점액 몸통 범프", SLIME_BUMP_TEXTURE, "몸통 전체", "흐르는 점액 주름과 미세 요철"),
    ("흰 박락 마스크", WHITE_FLECK_MASK_TEXTURE, "몸통 표면", "기준 이미지의 흰색 벗겨진 얼룩"),
    ("회녹색 주둥이 알베도", MUZZLE_ALBEDO_TEXTURE, "몸통 전면 주둥이 영역", "파충류성 회녹색 비늘 색 변화"),
    ("회녹색 주둥이 범프", MUZZLE_BUMP_TEXTURE, "몸통 전면 주둥이 영역", "비늘과 모공 요철"),
    ("입 안쪽 알베도", MOUTH_ALBEDO_TEXTURE, "입 내부", "검은 선이 아닌 젖은 어두운 구강"),
    ("치아 알베도", TOOTH_ALBEDO_TEXTURE, "치아", "누런 치아 얼룩과 색 변화"),
    ("혀 알베도", TONGUE_ALBEDO_TEXTURE, "혀", "젖은 붉은 혀 색 변화"),
]

MATERIAL_USAGE = [
    ("M_Parvum_Wet_Marbled_Green_Slime_Texture", "몸통 단일 메시 대부분", "점액 알베도, 거칠기, 범프, 흰 박락 마스크, 컬러 속성 블렌딩"),
    ("M_Parvum_Embedded_Grey_Green_Muzzle_Texture", "몸통 전면 주둥이 face 영역", "회녹색 알베도, 비늘 범프, 거칠기 응답"),
    ("M_Parvum_Dark_Muzzle_Pores", "콧구멍", "작은 어두운 모공"),
    ("M_Parvum_Deep_Mouth_Cavity_No_Line_Objects", "입 내부", "검은 두 줄 오브젝트 없는 어두운 젖은 구강"),
    ("M_Parvum_Irregular_Embedded_Teeth", "치아", "누런 알베도와 미세 거칠기"),
    ("M_Parvum_Mouth_Tongue_Detail", "혀", "젖은 붉은 표면"),
]


def ensure_dirs() -> None:
    for path in [BLENDER_DIR, EXPORT_DIR, RENDER_DIR, TEXTURE_DIR]:
        path.mkdir(parents=True, exist_ok=True)
    for path in RENDER_DIR.glob("*.png"):
        path.unlink()
    for path in EXPORT_DIR.glob("parvum_physics_rig_rework_sample.*"):
        path.unlink()
    for pattern in GENERATED_TEXTURES:
        for path in TEXTURE_DIR.glob(pattern):
            path.unlink()
    for path in BLENDER_DIR.glob("*.blend1"):
        path.unlink()


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def write_image(path: Path, size: int, pixel_func) -> None:
    image = bpy.data.images.new(path.stem, size, size, alpha=True)
    pixels: list[float] = []
    for y in range(size):
        for x in range(size):
            pixels.extend(pixel_func(x / size, y / size))
    image.pixels = pixels
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()


def create_slime_texture(path: Path) -> None:
    random.seed(path.name)
    base = (0.018, 0.30, 0.13)
    light = (0.10, 0.60, 0.28)
    deep = (0.008, 0.14, 0.06)

    def pixel(nx: float, ny: float) -> list[float]:
        veins = 0.5 + 0.5 * math.sin(nx * 54.0 + math.sin(ny * 21.0) * 4.8)
        swirl = 0.5 + 0.5 * math.sin((nx * 0.72 + ny * 1.18) * 36.0 + math.sin(nx * 17.0) * 2.0)
        broad = 0.5 + 0.5 * math.sin(nx * 10.0 - ny * 8.0 + math.sin((nx + ny) * 8.0))
        wet_cloud = random.random() * 0.10
        t = max(0.0, min(1.0, veins * 0.16 + swirl * 0.20 + broad * 0.10 + wet_cloud))
        r = base[0] * (1.0 - t) + light[0] * t + deep[0] * (1.0 - swirl) * 0.08
        g = base[1] * (1.0 - t) + light[1] * t + deep[1] * (1.0 - swirl) * 0.08
        b = base[2] * (1.0 - t) + light[2] * t + deep[2] * (1.0 - swirl) * 0.08
        return [r, g, b, 1.0]

    write_image(path, 768, pixel)


def create_slime_roughness_texture(path: Path) -> None:
    random.seed(path.name)

    def pixel(nx: float, ny: float) -> list[float]:
        streak = 0.5 + 0.5 * math.sin(nx * 42.0 + math.sin(ny * 18.0) * 3.0)
        flow = 0.5 + 0.5 * math.sin((nx * 0.4 + ny * 1.1) * 30.0)
        value = 0.24 + 0.34 * (1.0 - streak) + 0.14 * flow + random.random() * 0.04
        value = max(0.16, min(0.72, value))
        return [value, value, value, 1.0]

    write_image(path, 768, pixel)


def create_slime_bump_texture(path: Path) -> None:
    random.seed(path.name)

    def pixel(nx: float, ny: float) -> list[float]:
        ridges = 0.5 + 0.5 * math.sin(nx * 62.0 + math.sin(ny * 24.0) * 5.0)
        folds = 0.5 + 0.5 * math.sin((nx * 1.3 - ny * 0.9) * 28.0 + math.sin(nx * 18.0))
        value = 0.42 + ridges * 0.23 + folds * 0.21 + random.random() * 0.04
        value = max(0.05, min(0.95, value))
        return [value, value, value, 1.0]

    write_image(path, 768, pixel)


def create_white_fleck_mask_texture(path: Path) -> None:
    random.seed(path.name)
    centers = [
        (0.22, 0.24, 0.025, 0.045),
        (0.33, 0.31, 0.018, 0.034),
        (0.64, 0.28, 0.030, 0.048),
        (0.76, 0.42, 0.021, 0.039),
        (0.42, 0.70, 0.020, 0.034),
        (0.57, 0.62, 0.016, 0.030),
        (0.80, 0.72, 0.018, 0.030),
        (0.17, 0.61, 0.014, 0.028),
        (0.69, 0.78, 0.014, 0.026),
    ]

    def pixel(nx: float, ny: float) -> list[float]:
        value = 0.0
        for cx, cy, rx, ry in centers:
            dist = ((nx - cx) / rx) ** 2 + ((ny - cy) / ry) ** 2
            if dist < 1.0:
                ragged = (
                    0.35
                    + 0.35 * math.sin(nx * 211.0 + ny * 97.0)
                    + 0.20 * math.sin(nx * 379.0 - ny * 163.0)
                    + random.random() * 0.10
                )
                chipped = max(0.0, 1.0 - dist)
                value = max(value, chipped * max(0.0, ragged))
        value = 0.58 if value > 0.38 else max(0.0, value * 0.36 - 0.045)
        return [value, value, value, 1.0]

    write_image(path, 768, pixel)


def create_muzzle_texture(path: Path) -> None:
    random.seed(path.name)
    base = (0.210, 0.245, 0.205)
    green_grey = (0.360, 0.420, 0.330)
    pore = (0.060, 0.070, 0.055)

    def pixel(nx: float, ny: float) -> list[float]:
        scale_cells = (
            abs(math.sin(nx * 46.0 + math.sin(ny * 9.0) * 1.8))
            * abs(math.sin(ny * 38.0 + math.sin(nx * 11.0) * 1.3))
        )
        dirt = 0.5 + 0.5 * math.sin((nx + ny) * 31.0 + math.sin(nx * 23.0))
        pores = 1.0 if random.random() < 0.007 else 0.0
        t = max(0.0, min(1.0, scale_cells * 0.38 + dirt * 0.18))
        r = base[0] * (1.0 - t) + green_grey[0] * t
        g = base[1] * (1.0 - t) + green_grey[1] * t
        b = base[2] * (1.0 - t) + green_grey[2] * t
        if pores:
            r = r * 0.35 + pore[0] * 0.65
            g = g * 0.35 + pore[1] * 0.65
            b = b * 0.35 + pore[2] * 0.65
        return [r, g, b, 1.0]

    write_image(path, 768, pixel)


def create_muzzle_bump_texture(path: Path) -> None:
    random.seed(path.name)

    def pixel(nx: float, ny: float) -> list[float]:
        scales = (
            abs(math.sin(nx * 70.0 + math.sin(ny * 13.0) * 2.0))
            * abs(math.sin(ny * 58.0 + math.sin(nx * 12.0) * 1.8))
        )
        pores = 1.0 if random.random() < 0.012 else 0.0
        value = 0.40 + scales * 0.42 - pores * 0.32
        value = max(0.05, min(0.95, value))
        return [value, value, value, 1.0]

    write_image(path, 768, pixel)


def create_mouth_texture(path: Path) -> None:
    random.seed(path.name)

    def pixel(nx: float, ny: float) -> list[float]:
        wet = 0.5 + 0.5 * math.sin(nx * 24.0 - ny * 15.0)
        r = 0.030 + wet * 0.020
        g = 0.014 + wet * 0.009
        b = 0.012 + wet * 0.008
        return [r, g, b, 1.0]

    write_image(path, 512, pixel)


def create_tooth_texture(path: Path) -> None:
    random.seed(path.name)

    def pixel(nx: float, ny: float) -> list[float]:
        grain = 0.5 + 0.5 * math.sin(nx * 52.0 + ny * 9.0)
        stain = 0.5 + 0.5 * math.sin((nx + ny) * 38.0)
        r = 0.72 + grain * 0.10 - stain * 0.08
        g = 0.63 + grain * 0.08 - stain * 0.10
        b = 0.43 + grain * 0.05 - stain * 0.09
        return [max(0.0, min(1.0, r)), max(0.0, min(1.0, g)), max(0.0, min(1.0, b)), 1.0]

    write_image(path, 512, pixel)


def create_tongue_texture(path: Path) -> None:
    random.seed(path.name)

    def pixel(nx: float, ny: float) -> list[float]:
        wet = 0.5 + 0.5 * math.sin(nx * 16.0 + math.sin(ny * 18.0))
        stripe = 0.5 + 0.5 * math.sin(ny * 46.0)
        r = 0.42 + wet * 0.16
        g = 0.070 + stripe * 0.040
        b = 0.045 + wet * 0.040
        return [r, g, b, 1.0]

    write_image(path, 512, pixel)


def create_textures() -> None:
    create_slime_texture(TEXTURE_DIR / SLIME_ALBEDO_TEXTURE)
    create_slime_roughness_texture(TEXTURE_DIR / SLIME_ROUGHNESS_TEXTURE)
    create_slime_bump_texture(TEXTURE_DIR / SLIME_BUMP_TEXTURE)
    create_white_fleck_mask_texture(TEXTURE_DIR / WHITE_FLECK_MASK_TEXTURE)
    create_muzzle_texture(TEXTURE_DIR / MUZZLE_ALBEDO_TEXTURE)
    create_muzzle_bump_texture(TEXTURE_DIR / MUZZLE_BUMP_TEXTURE)
    create_mouth_texture(TEXTURE_DIR / MOUTH_ALBEDO_TEXTURE)
    create_tooth_texture(TEXTURE_DIR / TOOTH_ALBEDO_TEXTURE)
    create_tongue_texture(TEXTURE_DIR / TONGUE_ALBEDO_TEXTURE)


def make_material(
    name: str,
    color: tuple[float, float, float, float],
    roughness: float,
    specular: float,
    texture: str | None = None,
    color_attribute: str | None = None,
    roughness_texture: str | None = None,
    bump_texture: str | None = None,
    overlay_texture: str | None = None,
    overlay_color: tuple[float, float, float, float] | None = None,
    noise_bump: bool = False,
    coat: float = 0.0,
    bump_strength: float = 0.060,
    bump_distance: float = 0.040,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "OPAQUE"
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Alpha"].default_value = color[3]
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = 0.0
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = specular
        if "Coat Weight" in bsdf.inputs:
            bsdf.inputs["Coat Weight"].default_value = coat
        texture_coord = mat.node_tree.nodes.new("ShaderNodeTexCoord")
        if color_attribute:
            try:
                attr = mat.node_tree.nodes.new("ShaderNodeVertexColor")
                attr.layer_name = color_attribute
            except RuntimeError:
                attr = mat.node_tree.nodes.new("ShaderNodeAttribute")
                attr.attribute_name = color_attribute
            color_output = attr.outputs["Color"]
            if texture:
                tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
                tex.image = bpy.data.images.load(str(TEXTURE_DIR / texture))
                mat.node_tree.links.new(texture_coord.outputs["Generated"], tex.inputs["Vector"])
                try:
                    mix = mat.node_tree.nodes.new("ShaderNodeMixRGB")
                    mix.blend_type = "MULTIPLY"
                    mix.inputs["Fac"].default_value = 0.62
                    mat.node_tree.links.new(color_output, mix.inputs["Color1"])
                    mat.node_tree.links.new(tex.outputs["Color"], mix.inputs["Color2"])
                    color_output = mix.outputs["Color"]
                except RuntimeError:
                    pass
            if overlay_texture and overlay_color:
                overlay_tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
                overlay_tex.image = bpy.data.images.load(str(TEXTURE_DIR / overlay_texture))
                overlay_tex.image.colorspace_settings.name = "Non-Color"
                mat.node_tree.links.new(texture_coord.outputs["Generated"], overlay_tex.inputs["Vector"])
                overlay_mix = mat.node_tree.nodes.new("ShaderNodeMixRGB")
                overlay_mix.blend_type = "MIX"
                overlay_mix.inputs["Color2"].default_value = overlay_color
                mat.node_tree.links.new(color_output, overlay_mix.inputs["Color1"])
                mat.node_tree.links.new(overlay_tex.outputs["Color"], overlay_mix.inputs["Fac"])
                color_output = overlay_mix.outputs["Color"]
            mat.node_tree.links.new(color_output, bsdf.inputs["Base Color"])
        elif texture:
            tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
            tex.image = bpy.data.images.load(str(TEXTURE_DIR / texture))
            mat.node_tree.links.new(texture_coord.outputs["Generated"], tex.inputs["Vector"])
            mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        if roughness_texture:
            rough_tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
            rough_tex.image = bpy.data.images.load(str(TEXTURE_DIR / roughness_texture))
            rough_tex.image.colorspace_settings.name = "Non-Color"
            mat.node_tree.links.new(texture_coord.outputs["Generated"], rough_tex.inputs["Vector"])
            mat.node_tree.links.new(rough_tex.outputs["Color"], bsdf.inputs["Roughness"])
        if bump_texture:
            height_tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
            height_tex.image = bpy.data.images.load(str(TEXTURE_DIR / bump_texture))
            height_tex.image.colorspace_settings.name = "Non-Color"
            mat.node_tree.links.new(texture_coord.outputs["Generated"], height_tex.inputs["Vector"])
            bump = mat.node_tree.nodes.new("ShaderNodeBump")
            bump.inputs["Strength"].default_value = bump_strength
            bump.inputs["Distance"].default_value = bump_distance
            mat.node_tree.links.new(height_tex.outputs["Color"], bump.inputs["Height"])
            mat.node_tree.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
        if noise_bump:
            noise = mat.node_tree.nodes.new("ShaderNodeTexNoise")
            noise.inputs["Scale"].default_value = 42.0
            noise.inputs["Detail"].default_value = 13.0
            noise.inputs["Roughness"].default_value = 0.58
            bump = mat.node_tree.nodes.new("ShaderNodeBump")
            bump.inputs["Strength"].default_value = bump_strength * 0.55
            bump.inputs["Distance"].default_value = bump_distance * 0.70
            mat.node_tree.links.new(noise.outputs["Fac"], bump.inputs["Height"])
            if not bump_texture:
                mat.node_tree.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def make_materials() -> list[bpy.types.Material]:
    return [
        make_material(
            "M_Parvum_Wet_Marbled_Green_Slime_Texture",
            (0.040, 0.48, 0.23, 1.0),
            0.44,
            0.42,
            texture=SLIME_ALBEDO_TEXTURE,
            color_attribute="ParvumSurfaceColor",
            roughness_texture=SLIME_ROUGHNESS_TEXTURE,
            bump_texture=SLIME_BUMP_TEXTURE,
            overlay_texture=WHITE_FLECK_MASK_TEXTURE,
            overlay_color=(0.70, 0.76, 0.62, 1.0),
            noise_bump=True,
            coat=0.14,
            bump_strength=0.075,
            bump_distance=0.052,
        ),
        make_material("M_Parvum_Dark_Green_Internal_Marbling", (0.018, 0.24, 0.12, 1.0), 0.54, 0.54, texture=SLIME_ALBEDO_TEXTURE, roughness_texture=SLIME_ROUGHNESS_TEXTURE, bump_texture=SLIME_BUMP_TEXTURE, noise_bump=True, coat=0.08, bump_strength=0.045, bump_distance=0.035),
        make_material("M_Parvum_Green_Grey_Muzzle_Edge_Blend", (0.060, 0.330, 0.160, 1.0), 0.50, 0.22, texture=MUZZLE_ALBEDO_TEXTURE, bump_texture=MUZZLE_BUMP_TEXTURE, noise_bump=True, coat=0.04, bump_strength=0.050, bump_distance=0.030),
        make_material(
            "M_Parvum_Embedded_Grey_Green_Muzzle_Texture",
            (0.300, 0.350, 0.280, 1.0),
            0.64,
            0.18,
            texture=MUZZLE_ALBEDO_TEXTURE,
            roughness_texture=SLIME_ROUGHNESS_TEXTURE,
            bump_texture=MUZZLE_BUMP_TEXTURE,
            noise_bump=True,
            bump_strength=0.070,
            bump_distance=0.035,
        ),
        make_material("M_Parvum_Dark_Muzzle_Pores", (0.010, 0.012, 0.010, 1.0), 0.92, 0.0),
        make_material("M_Parvum_Deep_Mouth_Cavity_No_Line_Objects", (0.020, 0.012, 0.011, 1.0), 0.32, 0.34, texture=MOUTH_ALBEDO_TEXTURE, noise_bump=True, bump_strength=0.025, bump_distance=0.018),
        make_material("M_Parvum_Irregular_Embedded_Teeth", (0.82, 0.74, 0.50, 1.0), 0.38, 0.22, texture=TOOTH_ALBEDO_TEXTURE, noise_bump=True, bump_strength=0.018, bump_distance=0.012),
        make_material("M_Parvum_Mouth_Tongue_Detail", (0.55, 0.12, 0.07, 1.0), 0.36, 0.34, texture=TONGUE_ALBEDO_TEXTURE, noise_bump=True, coat=0.10, bump_strength=0.020, bump_distance=0.014),
    ]


def smoothstep(edge0: float, edge1: float, x: float) -> float:
    if edge0 == edge1:
        return 0.0
    t = max(0.0, min(1.0, (x - edge0) / (edge1 - edge0)))
    return t * t * (3.0 - 2.0 * t)


def gaussian(value: float, center: float, width: float) -> float:
    if width <= 0.0:
        return 0.0
    t = (value - center) / width
    return math.exp(-t * t)


def deform_body_point(point: Vector) -> Vector:
    flat_cut = -0.72
    upper_t = smoothstep(flat_cut, 1.0, point.z)
    bottom_t = 1.0 - upper_t
    vertical = math.sin(upper_t * math.pi * 0.5) ** 0.76

    raw_front = max(0.0, -point.y)
    raw_rear = max(0.0, point.y)
    raw_side = abs(point.x)
    raw_radial = min(1.0, math.sqrt(point.x * point.x + point.y * point.y))

    spread = 1.0 + 0.34 * bottom_t
    x = point.x * 0.68 * spread * (1.0 + 0.035 * math.sin(point.y * 4.1))
    y = point.y * (0.52 + 0.16 * bottom_t + 0.055 * raw_rear) * (1.0 + 0.08 * raw_front)

    dome_slope = max(0.55, 1.0 - 0.36 * raw_radial * raw_radial)
    z = 0.012 + 0.91 * vertical * dome_slope
    z -= 0.075 * raw_side * raw_side * bottom_t
    z -= 0.040 * max(0.0, raw_radial - 0.62) * (1.0 - upper_t * 0.30)
    z += 0.030 * raw_front * raw_front * (1.0 - bottom_t * 0.48)

    mid_height = upper_t * (1.0 - upper_t)
    z += 0.026 * mid_height * math.sin(point.x * 8.2 + point.y * 2.6)
    z += 0.018 * mid_height * math.sin(point.x * 3.9 - point.y * 6.4)
    top_t = smoothstep(0.80, 1.0, upper_t)
    z += top_t * (
        0.020 * math.sin(point.x * 18.0 + point.y * 3.0)
        + 0.014 * math.sin(point.y * 16.0 - point.x * 4.0)
    )

    front_zone = smoothstep(0.28, 0.92, raw_front)
    center_zone = gaussian(x, 0.0, 0.44)
    muzzle_height_zone = gaussian(z, 0.42, 0.24)
    muzzle_zone = (front_zone ** 2.0) * center_zone * muzzle_height_zone
    lower_support = (front_zone ** 2.0) * gaussian(x, 0.0, 0.45) * gaussian(z, 0.31, 0.17)

    y -= 0.060 * muzzle_zone
    y -= 0.020 * lower_support
    z += 0.006 * muzzle_zone
    z -= 0.003 * lower_support * smoothstep(0.36, 0.58, z)
    x *= 1.0 - 0.006 * muzzle_zone

    if point.z <= flat_cut:
        z = 0.010

    return Vector((x, y, max(0.010, z)))


def face_center(verts: list[tuple[float, float, float]], face: tuple[int, ...]) -> Vector:
    center = Vector((0.0, 0.0, 0.0))
    for index in face:
        center += Vector(verts[index])
    return center / len(face)


def muzzle_surface_factor(center: Vector) -> float:
    front = smoothstep(0.58, 0.82, -center.y)
    sx = abs(center.x) / 0.34
    sz = abs(center.z - 0.415) / 0.190
    oval = sx * sx + sz * sz
    return front * max(0.0, 1.0 - oval)


def stain_factor(center: Vector, patch: tuple[float, float, float, float, float, float]) -> float:
    px, py, pz, rx, ry, rz = patch
    return (
        gaussian(center.x, px, rx)
        * gaussian(center.y, py, ry)
        * gaussian(center.z, pz, rz)
    )


def body_material_index(center: Vector) -> int:
    if muzzle_surface_factor(center) > 0.58:
        return MATERIAL_SLOTS["muzzle"]
    return MATERIAL_SLOTS["slime"]


def mix_color(
    a: tuple[float, float, float],
    b: tuple[float, float, float],
    t: float,
) -> tuple[float, float, float]:
    t = max(0.0, min(1.0, t))
    return (
        a[0] * (1.0 - t) + b[0] * t,
        a[1] * (1.0 - t) + b[1] * t,
        a[2] * (1.0 - t) + b[2] * t,
    )


def surface_color(point: Vector) -> tuple[float, float, float, float]:
    marble = 0.5 + 0.5 * math.sin(point.x * 12.0 + point.y * 8.0 + math.sin(point.z * 18.0) * 1.4)
    wet = 0.5 + 0.5 * math.sin(point.x * 27.0 - point.y * 15.0 + point.z * 9.0)
    base = mix_color((0.014, 0.245, 0.110), (0.060, 0.520, 0.235), 0.28 + marble * 0.22)
    base = mix_color(base, (0.010, 0.145, 0.065), (1.0 - wet) * 0.25)

    muzzle = max(0.0, min(1.0, muzzle_surface_factor(point) * 1.85))
    scale_noise = 0.5 + 0.5 * math.sin(point.x * 82.0 + math.sin(point.z * 47.0) * 1.8)
    muzzle_color = mix_color((0.115, 0.142, 0.112), (0.300, 0.350, 0.265), 0.28 + wet * 0.20 + scale_noise * 0.18)
    base = mix_color(base, muzzle_color, smoothstep(0.05, 0.78, muzzle))

    for patch in DARK_PATCHES:
        darkness = stain_factor(point, patch)
        if darkness > 0.08:
            base = mix_color(base, (0.008, 0.13, 0.060), darkness * 0.28)
    for patch in WHITE_PATCHES:
        pale = stain_factor(point, patch)
        if pale > 0.10:
            base = mix_color(base, (0.34, 0.48, 0.33), pale * 0.14)

    top_wet = smoothstep(0.46, 0.86, point.z) * (0.25 + wet * 0.20)
    base = mix_color(base, (0.09, 0.57, 0.28), top_wet * 0.13)
    return (base[0], base[1], base[2], 1.0)


def apply_surface_colors(mesh: bpy.types.Mesh) -> None:
    color_attr = mesh.color_attributes.new(name="ParvumSurfaceColor", type="BYTE_COLOR", domain="CORNER")
    for poly in mesh.polygons:
        for loop_index in poly.loop_indices:
            vertex_index = mesh.loops[loop_index].vertex_index
            color_attr.data[loop_index].color = surface_color(mesh.vertices[vertex_index].co)


def front_surface_y_at(
    verts: list[tuple[float, float, float]],
    body_vert_count: int,
    x: float,
    z: float,
) -> float:
    nearest: list[tuple[float, float]] = []
    for vx, vy, vz in verts[:body_vert_count]:
        if vy > -0.20:
            continue
        dx = (vx - x) / 0.085
        dz = (vz - z) / 0.085
        nearest.append((dx * dx + dz * dz, vy))

    nearest.sort(key=lambda item: item[0])
    if not nearest:
        return -0.72
    local = nearest[:24]
    return min(vy for _distance, vy in local)


def append_muzzle_surface_patch(
    verts: list[tuple[float, float, float]],
    faces: list[tuple[int, ...]],
    material_indices: list[int],
) -> None:
    cx, cy, cz = (0.0, -0.890, 0.413)
    segments = 88
    rings = [
        (0.252, 0.170, -0.006, MATERIAL_SLOTS["muzzle"]),
        (0.220, 0.148, -0.010, MATERIAL_SLOTS["muzzle"]),
        (0.188, 0.124, -0.014, MATERIAL_SLOTS["muzzle"]),
    ]
    ring_indices: list[list[int]] = []
    for ring_index, (rx, rz, y_offset, _material) in enumerate(rings):
        current: list[int] = []
        for seg in range(segments):
            theta = math.tau * seg / segments
            wobble = 1.0 + 0.036 * math.sin(theta * 3.0 + ring_index * 0.35) + 0.022 * math.sin(theta * 5.0 - 0.7)
            x = cx + rx * wobble * math.cos(theta)
            z = cz + rz * wobble * math.sin(theta)
            y = cy + y_offset - 0.018 * math.cos(theta) * math.cos(theta)
            current.append(len(verts))
            verts.append((x, y, z))
        ring_indices.append(current)

    for ring_index in range(len(ring_indices) - 1):
        outer = ring_indices[ring_index]
        inner = ring_indices[ring_index + 1]
        material = rings[ring_index][3]
        for seg in range(segments):
            faces.append((
                outer[seg],
                outer[(seg + 1) % segments],
                inner[(seg + 1) % segments],
                inner[seg],
            ))
            material_indices.append(material)


def append_ellipse_disk(
    verts: list[tuple[float, float, float]],
    faces: list[tuple[int, ...]],
    material_indices: list[int],
    center: tuple[float, float, float],
    rx: float,
    rz: float,
    material_index: int,
    segments: int = 48,
    y_offset: float = 0.0,
    wobble: float = 0.0,
    center_recess: float = 0.0,
) -> None:
    cx, cy, cz = center
    base = len(verts)
    verts.append((cx, cy + y_offset + center_recess, cz))
    for seg in range(segments):
        theta = math.tau * seg / segments
        irregular = 1.0 + wobble * math.sin(theta * 3.0 + 0.4) + wobble * 0.55 * math.sin(theta * 5.0 - 0.8)
        verts.append((
            cx + rx * irregular * math.cos(theta),
            cy + y_offset,
            cz + rz * irregular * math.sin(theta),
        ))
    for seg in range(segments):
        faces.append((base, base + 1 + seg, base + 1 + ((seg + 1) % segments)))
        material_indices.append(material_index)


def append_surface_ellipse_disk(
    verts: list[tuple[float, float, float]],
    faces: list[tuple[int, ...]],
    material_indices: list[int],
    body_vert_count: int,
    center_xz: tuple[float, float],
    rx: float,
    rz: float,
    material_index: int,
    segments: int = 48,
    surface_offset: float = -0.006,
    wobble: float = 0.0,
    center_recess: float = 0.004,
) -> None:
    cx, cz = center_xz
    base = len(verts)
    center_y = front_surface_y_at(verts, body_vert_count, cx, cz) + surface_offset + center_recess
    verts.append((cx, center_y, cz))
    for seg in range(segments):
        theta = math.tau * seg / segments
        irregular = 1.0 + wobble * math.sin(theta * 3.0 + 0.4) + wobble * 0.55 * math.sin(theta * 5.0 - 0.8)
        x = cx + rx * irregular * math.cos(theta)
        z = cz + rz * irregular * math.sin(theta)
        y = front_surface_y_at(verts, body_vert_count, x, z) + surface_offset
        verts.append((x, y, z))
    for seg in range(segments):
        faces.append((base, base + 1 + seg, base + 1 + ((seg + 1) % segments)))
        material_indices.append(material_index)


def append_tooth(
    verts: list[tuple[float, float, float]],
    faces: list[tuple[int, ...]],
    material_indices: list[int],
    x: float,
    root_z: float,
    tip_z: float,
    width: float,
    y: float,
) -> None:
    half = width * 0.5
    depth = 0.014
    base = len(verts)
    verts.extend([
        (x - half, y, root_z),
        (x + half, y, root_z),
        (x, y, tip_z),
        (x - half * 0.55, y + depth, root_z - 0.004),
        (x + half * 0.55, y + depth, root_z - 0.004),
        (x, y + depth, tip_z),
    ])
    faces.extend([
        (base, base + 1, base + 2),
        (base + 3, base + 5, base + 4),
        (base, base + 3, base + 4, base + 1),
        (base + 1, base + 4, base + 5, base + 2),
        (base + 2, base + 5, base + 3, base),
    ])
    material_indices.extend([MATERIAL_SLOTS["tooth"]] * 5)


def append_surface_tooth(
    verts: list[tuple[float, float, float]],
    faces: list[tuple[int, ...]],
    material_indices: list[int],
    body_vert_count: int,
    x: float,
    root_z: float,
    tip_z: float,
    width: float,
) -> None:
    half = width * 0.5
    depth = 0.013
    root_y = front_surface_y_at(verts, body_vert_count, x, root_z) - 0.008
    tip_y = front_surface_y_at(verts, body_vert_count, x, tip_z) - 0.006
    base = len(verts)
    verts.extend([
        (x - half, root_y, root_z),
        (x + half, root_y, root_z),
        (x, tip_y, tip_z),
        (x - half * 0.55, root_y + depth, root_z - 0.004),
        (x + half * 0.55, root_y + depth, root_z - 0.004),
        (x, tip_y + depth, tip_z),
    ])
    faces.extend([
        (base, base + 1, base + 2),
        (base + 3, base + 5, base + 4),
        (base, base + 3, base + 4, base + 1),
        (base + 1, base + 4, base + 5, base + 2),
        (base + 2, base + 5, base + 3, base),
    ])
    material_indices.extend([MATERIAL_SLOTS["tooth"]] * 5)


def append_mouth_details(
    verts: list[tuple[float, float, float]],
    faces: list[tuple[int, ...]],
    material_indices: list[int],
) -> None:
    body_vert_count = len(verts)
    append_surface_ellipse_disk(
        verts,
        faces,
        material_indices,
        body_vert_count,
        (0.0, 0.405),
        0.188,
        0.124,
        MATERIAL_SLOTS["mouth"],
        segments=54,
        surface_offset=-0.006,
        wobble=0.045,
        center_recess=0.006,
    )
    append_surface_ellipse_disk(
        verts,
        faces,
        material_indices,
        body_vert_count,
        (0.0, 0.348),
        0.098,
        0.034,
        MATERIAL_SLOTS["tongue"],
        segments=34,
        surface_offset=-0.004,
        wobble=0.028,
        center_recess=0.004,
    )

    upper_xs = [-0.135, -0.108, -0.082, -0.055, -0.028, 0.0, 0.028, 0.055, 0.082, 0.108, 0.135]
    lower_xs = [-0.118, -0.090, -0.062, -0.034, -0.006, 0.024, 0.052, 0.080, 0.108]
    for i, x in enumerate(upper_xs):
        root_z = 0.500 - 0.012 * abs(x) / 0.135
        tip_z = 0.418 + 0.007 * (i % 2)
        append_surface_tooth(verts, faces, material_indices, body_vert_count, x, root_z, tip_z, 0.016 + 0.003 * (i % 3 == 0))
    for i, x in enumerate(lower_xs):
        root_z = 0.315 + 0.010 * abs(x) / 0.118
        tip_z = 0.390 - 0.006 * (i % 2)
        append_surface_tooth(verts, faces, material_indices, body_vert_count, x, root_z, tip_z, 0.015 + 0.003 * (i % 2 == 0))

    append_surface_ellipse_disk(verts, faces, material_indices, body_vert_count, (-0.062, 0.528), 0.020, 0.010, MATERIAL_SLOTS["muzzle_dark"], segments=18, surface_offset=-0.004, wobble=0.06, center_recess=0.002)
    append_surface_ellipse_disk(verts, faces, material_indices, body_vert_count, (0.062, 0.528), 0.020, 0.010, MATERIAL_SLOTS["muzzle_dark"], segments=18, surface_offset=-0.004, wobble=0.06, center_recess=0.002)


def create_reference_matched_mesh(materials: list[bpy.types.Material]) -> bpy.types.Object:
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=7, radius=1.0, location=(0.0, 0.0, 0.0))
    source = bpy.context.object
    source_mesh = source.data

    verts = [tuple(deform_body_point(vertex.co)) for vertex in source_mesh.vertices]
    faces = [tuple(poly.vertices) for poly in source_mesh.polygons]
    material_indices = [body_material_index(face_center(verts, face)) for face in faces]
    bpy.data.objects.remove(source, do_unlink=True)

    append_mouth_details(verts, faces, material_indices)

    mesh = bpy.data.meshes.new(f"{BODY_OBJECT_NAME}_data")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    for index, poly in enumerate(mesh.polygons):
        poly.material_index = material_indices[index]
    apply_surface_colors(mesh)

    body = bpy.data.objects.new(BODY_OBJECT_NAME, mesh)
    bpy.context.collection.objects.link(body)
    for material in materials:
        body.data.materials.append(material)
    body["UnityVisualRule"] = "One visible mesh. The body surface itself forms the muzzle; no separate front object is attached."
    body["UnityRootMotionRule"] = "Runtime movement must use Rigidbody/Collider; BlendShapes deform the single visible mesh."

    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    bpy.ops.object.shade_smooth()
    body.select_set(False)

    body.shape_key_add(name="Basis")
    basis = [vertex.co.copy() for vertex in body.data.vertices]
    for key_name in SHAPE_KEYS:
        key = body.shape_key_add(name=key_name)
        for i, base in enumerate(basis):
            x, y, z = base.x, base.y, base.z
            front = max(0.0, min(1.0, (-y - 0.28) / 0.68))
            rear = max(0.0, min(1.0, (y - 0.02) / 0.58))
            side = min(1.0, abs(x) / 0.80)
            height = max(0.0, min(1.0, z / 0.86))
            co = base.copy()
            if key_name == "Idle_Pulse_Surface_Jiggle":
                co.z += 0.015 * height * math.sin(7.0 * x + 4.0 * y)
                co.x += 0.006 * side * height * math.sin(9.0 * y)
                co.y += 0.004 * front * height * math.sin(10.0 * x)
            elif key_name == "Move_Squash_Forward_Slosh":
                co.y -= 0.030 * front * (0.45 + height)
                co.z -= 0.028 * front * height
                co.z += 0.018 * rear * height
                co.x *= 1.0 + 0.025 * height
            elif key_name == "Attack_Bite_Core_Kick":
                co.y -= 0.076 * front
                co.z += 0.018 * front * height
                if y < -0.88 and z > 0.39:
                    co.z += 0.024
                if y < -0.88 and z < 0.38:
                    co.z -= 0.020
            elif key_name == "Hit_Recoil_Side_Wave":
                co.x += 0.040 * height * (1.0 - rear * 0.2)
                co.z += 0.025 * height * math.sin((x + 0.4) * 10.0)
                co.y += 0.010 * height * math.sin(y * 9.0)
            elif key_name == "Death_Flatten_Liquid_Spread":
                co.x *= 1.20
                co.y *= 1.16
                co.z = 0.010 + (z - 0.010) * 0.34
            key.data[i].co = co

    body.modifiers.new("single mesh weighted normals", "WEIGHTED_NORMAL")
    return body


def add_hidden_tooling_metadata() -> None:
    specs = {
        "Hidden_Rigidbody_Root_Collider_Bounds": "Root Rigidbody + Collider volume. Not a visible mesh.",
        "Hidden_Jiggle_Surface_Left_Target": "Jiggle Physics target for left slime surface lag.",
        "Hidden_Jiggle_Surface_Right_Target": "Jiggle Physics target for right slime surface lag.",
        "Hidden_Jiggle_Rear_Mass_Target": "Jiggle Physics target for rear mass lag.",
        "Hidden_ConfigurableJoint_Mouth_Limit_Target": "Joint limit target so embedded mouth follows the slime body.",
        "Hidden_MotionPath_Attack_Rigidbody_Goal": "Motion Path target only; runtime movement is Rigidbody driven.",
    }
    for name, note in specs.items():
        empty = bpy.data.objects.new(name, None)
        empty.empty_display_type = "SPHERE"
        empty.empty_display_size = 0.04
        empty.hide_render = True
        empty.hide_viewport = True
        empty["UnityToolingNote"] = note
        bpy.context.collection.objects.link(empty)


def look_at(obj: bpy.types.Object, target: tuple[float, float, float]) -> None:
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_scene() -> bpy.types.Object:
    try:
        bpy.context.scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 720
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.world.color = (0.86, 0.90, 0.84)

    bpy.ops.object.light_add(type="AREA", location=(-0.55, -1.35, 1.45))
    key = bpy.context.object
    key.name = "large reference softbox for wet slime"
    key.data.energy = 165
    key.data.size = 1.45

    bpy.ops.object.light_add(type="AREA", location=(0.7, 0.35, 0.85))
    fill = bpy.context.object
    fill.name = "soft green fill"
    fill.data.energy = 55
    fill.data.size = 2.4

    bpy.ops.object.camera_add(location=(0.0, -2.05, 0.58))
    camera = bpy.context.object
    camera.name = "Review Camera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 2.22
    bpy.context.scene.camera = camera
    look_at(camera, (0.0, -0.22, 0.34))
    return camera


def set_shape_value(body: bpy.types.Object, key_name: str | None, value: float) -> None:
    if not body.data.shape_keys:
        return
    for key in body.data.shape_keys.key_blocks:
        key.value = 0.0
    if key_name and key_name in body.data.shape_keys.key_blocks:
        body.data.shape_keys.key_blocks[key_name].value = value


def render(
    camera: bpy.types.Object,
    filename: str,
    loc: tuple[float, float, float],
    target: tuple[float, float, float],
    scale: float,
) -> None:
    camera.location = loc
    camera.data.ortho_scale = scale
    look_at(camera, target)
    bpy.context.scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def render_outputs(camera: bpy.types.Object, body: bpy.types.Object) -> None:
    static_views = [
        ("01_front_single_slime_body_reference.png", (0.0, -2.05, 0.58), (0.0, -0.22, 0.34), 2.22),
        ("02_side_lower_mouth_merged_into_body.png", (1.82, -0.30, 0.52), (0.0, -0.20, 0.33), 2.24),
        ("03_back_no_lobe_boundaries.png", (0.0, 1.95, 0.55), (0.0, 0.04, 0.34), 2.20),
        ("04_top_single_surface_no_proxy_objects.png", (0.0, -0.04, 2.25), (0.0, -0.05, 0.18), 2.18),
        ("05_three_quarter_clean_runtime_visual.png", (1.16, -1.55, 0.78), (0.0, -0.20, 0.34), 2.28),
    ]
    set_shape_value(body, None, 0.0)
    for filename, loc, target, scale in static_views:
        render(camera, filename, loc, target, scale)

    states = [
        ("06_idle_single_surface_pulse_pose.png", "Idle_Pulse_Surface_Jiggle", 1.0),
        ("07_move_single_body_squash_slosh_pose.png", "Move_Squash_Forward_Slosh", 1.0),
        ("08_attack_mouth_supported_by_body_pose.png", "Attack_Bite_Core_Kick", 1.0),
        ("09_hit_recoil_single_mass_wave_pose.png", "Hit_Recoil_Side_Wave", 1.0),
        ("10_death_single_slime_flatten_pose.png", "Death_Flatten_Liquid_Spread", 1.0),
    ]
    for filename, key_name, value in states:
        set_shape_value(body, key_name, value)
        render(camera, filename, (0.0, -2.05, 0.58), (0.0, -0.22, 0.34), 2.22)
    set_shape_value(body, None, 0.0)


def write_docs() -> None:
    texture_lines = "\n".join(
        f"- `{filename}`: {label} / 적용 부위: {part} / 표현: {note}"
        for label, filename, part, note in TEXTURE_USAGE
    )
    material_lines = "\n".join(
        f"- `{name}`: {part} / 연결 내용: {note}"
        for name, part, note in MATERIAL_USAGE
    )

    readme = f"""# 파르붐 기준 이미지 재반영 단일 메시 샘플

이 샘플은 기준 이미지의 파르붐처럼 높은 초록 점액 몸체와 몸체 앞면에서 자연스럽게 솟아난 회녹색 주둥이, 열린 입, 치아, 혀가 한 덩어리로 보이도록 다시 만든 검토용 적대 개체 샘플입니다.

## 이번 수정 기준

- 보이는 오브젝트는 `Unified_Parvum_Reference_Matched_Single_Mesh` 하나입니다.
- 원통형 주둥이 오브젝트를 앞에 붙이지 않고, 몸체 표면 자체를 앞으로 밀어 주둥이 형상을 만들었습니다.
- 주둥이와 몸체 사이의 물리적 경계선, 내부 물방울, 검은 두 줄 오브젝트는 만들지 않았습니다.
- 초록 점액 몸체에는 알베도, 거칠기, 범프, 흰 박락 마스크 텍스처를 실제 재질 노드에 연결했습니다.
- 회녹색 주둥이는 별도 오브젝트가 아니라 같은 몸통 메시의 일부 face와 컬러 블렌딩으로 이어지며, 회녹색 비늘 알베도/범프 텍스처를 적용했습니다.
- 입, 치아, 혀도 단순 단색이 아니라 각 부위용 알베도 텍스처와 표면 특성을 적용했습니다.

## 사용 텍스처

{texture_lines}

## 사용 머티리얼

{material_lines}

## Unity 적용 상태

아직 Unity 씬, 프리팹, 런타임 에셋에는 연결하지 않았습니다. 사용자 승인 전까지 이 샘플은 `artSample/` 검토 산출물입니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    notes = """# 파르붐 물리 기반 모션 제작 메모

## 모델링 구조

- 보이는 모델은 `Unified_Parvum_Reference_Matched_Single_Mesh` 하나입니다.
- 몸체, 주둥이 표면, 입 안쪽, 치아, 혀 디테일은 모두 같은 메시 데이터 안에 포함됩니다.
- 별도의 원통형 주둥이, 내부 물방울, 검은 선, 독립 점액 덩어리 오브젝트는 사용하지 않았습니다.

## Shape Key

- `Idle_Pulse_Surface_Jiggle`: 한 덩어리 점액 표면의 약한 맥동입니다.
- `Move_Squash_Forward_Slosh`: 몸 전체가 낮아지고 앞쪽으로 쏠리는 이동 상태입니다.
- `Attack_Bite_Core_Kick`: 입 주변과 앞면 몸체가 함께 전진하는 공격 상태입니다.
- `Hit_Recoil_Side_Wave`: 충격이 한 덩어리 몸체 전체로 전달되는 피격 상태입니다.
- `Death_Flatten_Liquid_Spread`: 몸이 낮아지고 옆으로 퍼지는 사망 상태입니다.

## Unity 실제 적용 방식

- 루트 이동은 `Rigidbody + Collider` 기준이어야 합니다.
- Motion Path는 목표 경로나 목표점 편집용으로만 사용하고, 실제 이동은 Rigidbody velocity 또는 force로 추종해야 합니다.
- Jiggle Physics는 표면 보조 흔들림에 사용합니다.
- ConfigurableJoint는 입 주변 질량이 몸체에서 분리되어 보이지 않게 제한 추종할 때만 사용합니다.
"""
    (SAMPLE_ROOT / "PHYSICS_RIG_NOTES.md").write_text(notes, encoding="utf-8")

    texture_doc = f"""# 파르붐 텍스처 및 재질 분석

## 기준 이미지 표면 분석

- 몸체는 반투명한 짙은 초록 점액이며, 단색이 아니라 내부에 어두운 마블링과 젖은 광택이 섞여 있습니다.
- 표면에는 밝은 얼룩과 탁한 녹색 변화가 있고, 일부 영역은 미끄러운 젤처럼 반사됩니다.
- 앞면 주둥이는 몸체와 분리된 원통이 아니라 몸체 표면에서 이어져 나온 회녹색 파충류성 표면처럼 보여야 합니다.
- 치아는 순백 단색이 아니라 누런 얼룩과 약한 거칠기가 있으며, 혀와 입 안쪽은 젖은 표면으로 보여야 합니다.

## 반영 내용

- 몸통은 하나의 보이는 메시 구조를 유지하며, 텍스처/머티리얼 보강을 위해 몸통을 여러 오브젝트로 쪼개지 않았습니다.
- 몸체 재질은 컬러 속성 블렌딩, 점액 알베도, 거칠기, 범프, 흰 박락 마스크를 함께 사용합니다.
- 회녹색 주둥이는 같은 몸통 메시의 일부 face와 컬러 블렌딩으로 이어지며, 회녹색 비늘 알베도와 범프를 적용합니다.
- 입, 치아, 혀는 각각 별도 머티리얼 슬롯과 알베도 텍스처를 사용합니다.

## 사용 텍스처

{texture_lines}

## 사용 머티리얼

{material_lines}
"""
    (SAMPLE_ROOT / "TEXTURE_ANALYSIS.md").write_text(texture_doc, encoding="utf-8")

    manifest = {
        "sample": "parvum_physics_rig_rework_sample",
        "objectId": "ENEMY-SEED-PARVUM",
        "title": "파르붐 기준 이미지 재반영 단일 메시 샘플",
        "approvalState": "검토 필요",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "generatedDate": "2026-06-29",
        "sourceBasis": [
            "image/parvum(파르붐).png",
            "image/parvum-back.png",
            "image/parvum-beside.png",
            "사용자 지시: 몸체를 기준 이미지에 더 가깝게 변형",
            "사용자 지시: 주둥이를 원통형 오브젝트가 아니라 몸통과 자연스럽게 이어지도록 수정",
            "사용자 지시: 머티리얼과 텍스처 적용 규칙 준수",
        ],
        "visibleObjects": [BODY_OBJECT_NAME],
        "visualRule": "One visible mesh. The front body surface is deformed into the muzzle; no separate front object or separate slime body objects.",
        "modelingCorrections": [
            "원통형 주둥이 부착 구조 제거",
            "몸체 표면 변형으로 주둥이 돌출부 제작",
            "넓은 바닥과 높은 중앙 돔형 실루엣 유지",
            "주둥이와 몸체 경계가 물리적 부품 경계처럼 보이지 않도록 같은 메시 표면으로 구성",
            "초록 점액 몸체에 알베도, 거칠기, 범프, 흰 박락 마스크, 컬러 속성 블렌딩 적용",
            "회녹색 주둥이를 별도 오브젝트가 아니라 몸체 표면 face/컬러 블렌딩으로 통합하고 비늘 텍스처 적용",
            "입, 치아, 혀에 부위별 알베도 텍스처와 재질 특성 적용",
            "내부 물방울, 검은 두 줄, 분리된 점액 덩어리 오브젝트 미생성",
        ],
        "textures": [f"textures/{filename}" for _label, filename, _part, _note in TEXTURE_USAGE],
        "materials": [
            {"name": name, "part": part, "note": note}
            for name, part, note in MATERIAL_USAGE
        ],
        "shapeKeys": SHAPE_KEYS,
        "generatedFiles": [
            "blender/parvum_physics_rig_rework_sample.blend",
            "exports/parvum_physics_rig_rework_sample.fbx",
            "exports/parvum_physics_rig_rework_sample.glb",
            "renders/01_front_single_slime_body_reference.png",
            "renders/02_side_lower_mouth_merged_into_body.png",
            "renders/03_back_no_lobe_boundaries.png",
            "renders/04_top_single_surface_no_proxy_objects.png",
            "renders/05_three_quarter_clean_runtime_visual.png",
            "renders/06_idle_single_surface_pulse_pose.png",
            "renders/07_move_single_body_squash_slosh_pose.png",
            "renders/08_attack_mouth_supported_by_body_pose.png",
            "renders/09_hit_recoil_single_mass_wave_pose.png",
            "renders/10_death_single_slime_flatten_pose.png",
            "README.md",
            "PHYSICS_RIG_NOTES.md",
            "TEXTURE_ANALYSIS.md",
            "ASSET_MANIFEST.json",
            "APPROVAL_STATUS.json",
            "index.html",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(
        json.dumps(
            {
                "sample": "parvum_physics_rig_rework_sample",
                "approvalState": "검토 필요",
                "unityApplicationAllowed": False,
                "requiresUserApprovalBeforeUnity": True,
                "note": "사용자 승인 전 Unity 씬, 프리팹, 런타임 에셋에 연결하지 않습니다.",
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )

    cards = [
        ("01_front_single_slime_body_reference.png", "정면: 기준 이미지형 점액 몸체와 통합 주둥이"),
        ("02_side_lower_mouth_merged_into_body.png", "측면: 주둥이가 원통 부착물이 아니라 몸체 표면에서 이어진 상태"),
        ("03_back_no_lobe_boundaries.png", "후면: 독립 덩어리 경계 없는 단일 점액 몸체"),
        ("04_top_single_surface_no_proxy_objects.png", "상단: 보이는 보조 오브젝트 없이 단일 표면"),
        ("05_three_quarter_clean_runtime_visual.png", "3/4: Unity 적용 기준 검토 시점"),
        ("06_idle_single_surface_pulse_pose.png", "대기: 단일 표면 맥동"),
        ("07_move_single_body_squash_slosh_pose.png", "이동: 한 덩어리 몸체 쏠림"),
        ("08_attack_mouth_supported_by_body_pose.png", "공격: 입과 몸체가 함께 전진"),
        ("09_hit_recoil_single_mass_wave_pose.png", "피격: 단일 질량 파동"),
        ("10_death_single_slime_flatten_pose.png", "사망: 한 덩어리 점액 퍼짐"),
    ]
    figures = "\n".join(
        f"<figure><img src='renders/{filename}' alt='{caption}'><figcaption>{caption}</figcaption></figure>"
        for filename, caption in cards
    )
    texture_figures = "\n".join(
        f"<figure><img src='textures/{filename}' alt='{label}'><figcaption><strong>{label}</strong><br>{part}<br>{note}</figcaption></figure>"
        for label, filename, part, note in TEXTURE_USAGE
    )
    material_rows = "\n".join(
        f"<tr><td>{name}</td><td>{part}</td><td>{note}</td></tr>"
        for name, part, note in MATERIAL_USAGE
    )
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <title>파르붐 기준 이미지 재반영 단일 메시 샘플</title>
  <style>
    body {{ margin: 0; background: #f1f5ec; color: #18201b; font-family: system-ui, sans-serif; }}
    header {{ padding: 24px 28px 10px; }}
    h1 {{ margin: 0 0 8px; font-size: 24px; }}
    p {{ margin: 0; color: #4f5e53; line-height: 1.5; }}
    .compare {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(360px, 1fr)); gap: 14px; padding: 14px 24px 4px; }}
    main {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(360px, 1fr)); gap: 14px; padding: 18px 24px 28px; }}
    section.detail {{ padding: 4px 24px 28px; }}
    section.detail h2 {{ margin: 24px 0 12px; font-size: 20px; }}
    .texture-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 14px; }}
    table {{ width: 100%; border-collapse: collapse; background: #ffffff; border: 1px solid #ccd8ce; }}
    th, td {{ padding: 10px 12px; border-bottom: 1px solid #dde6de; text-align: left; vertical-align: top; font-size: 14px; }}
    th {{ background: #e7efe8; color: #26332a; }}
    figure {{ margin: 0; background: #ffffff; border: 1px solid #ccd8ce; border-radius: 6px; overflow: hidden; }}
    img {{ display: block; width: 100%; height: auto; }}
    figcaption {{ padding: 10px 12px; font-size: 14px; color: #334037; }}
  </style>
</head>
<body>
  <header>
    <h1>파르붐 기준 이미지 재반영 단일 메시 샘플</h1>
    <p>기준 이미지와 비교할 수 있도록 정면, 측면, 후면 기준 이미지를 함께 배치했습니다. Unity에는 아직 반영하지 않았습니다.</p>
  </header>
  <section class="compare">
    <figure><img src="../../../image/parvum(파르붐).png" alt="파르붐 기준 정면"><figcaption>기준 이미지 정면</figcaption></figure>
    <figure><img src="../../../image/parvum-beside.png" alt="파르붐 기준 측면"><figcaption>기준 이미지 측면</figcaption></figure>
    <figure><img src="../../../image/parvum-back.png" alt="파르붐 기준 후면"><figcaption>기준 이미지 후면</figcaption></figure>
  </section>
  <main>
    {figures}
  </main>
  <section class="detail">
    <h2>사용 텍스처</h2>
    <div class="texture-grid">
      {texture_figures}
    </div>
    <h2>사용 머티리얼</h2>
    <table>
      <thead><tr><th>머티리얼</th><th>적용 부위</th><th>연결/표현 내용</th></tr></thead>
      <tbody>
        {material_rows}
      </tbody>
    </table>
  </section>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def export_files() -> None:
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "parvum_physics_rig_rework_sample.blend"))
    for path in BLENDER_DIR.glob("*.blend1"):
        path.unlink()

    bpy.ops.object.select_all(action="DESELECT")
    body = bpy.data.objects.get(BODY_OBJECT_NAME)
    if body:
        body.select_set(True)
        bpy.context.view_layer.objects.active = body

    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_DIR / "parvum_physics_rig_rework_sample.fbx"),
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=True,
    )
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_DIR / "parvum_physics_rig_rework_sample.glb"),
        export_format="GLB",
        export_apply=True,
        export_vertex_color="ACTIVE",
        export_all_vertex_colors=True,
        use_selection=True,
    )


def main() -> None:
    ensure_dirs()
    clear_scene()
    create_textures()
    materials = make_materials()
    body = create_reference_matched_mesh(materials)
    add_hidden_tooling_metadata()
    camera = setup_scene()
    render_outputs(camera, body)
    write_docs()
    export_files()
    print("PARVUM_REFERENCE_MATCHED_INTEGRATED_MUZZLE_SAMPLE_DONE " + str(SAMPLE_ROOT))


if __name__ == "__main__":
    main()
