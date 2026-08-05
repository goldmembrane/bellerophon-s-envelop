from __future__ import annotations

import math
import sys
from collections import defaultdict, deque
from pathlib import Path

import bpy
import bmesh
import numpy as np
from mathutils import Matrix, Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE_FBX = ROOT / "enemies model" / "išpant-armed.fbx"
SAMPLE_DIR = ROOT / "artSample" / "enemies" / "ispant_armed"
TEXTURE_DIR = SAMPLE_DIR / "textures"
DIAGNOSTIC_DIR = SAMPLE_DIR / "diagnostics"
BLEND_PATH = SAMPLE_DIR / "Ispant_Armed_Appearance_Sample.blend"
GLB_PATH = SAMPLE_DIR / "Ispant_Armed_Appearance_Sample.glb"
DIAGNOSTIC_PATH = DIAGNOSTIC_DIR / "Ispant_Appearance_Diagnostic_01.png"
FACE_DIAGNOSTIC_PATH = DIAGNOSTIC_DIR / "Ispant_Face_Diagnostic.png"
FINAL_PATH = SAMPLE_DIR / "Ispant_Armed_Appearance_FinalReview.png"

TEXTURE_SIZE = 512
FINAL_MODE = "--final" in sys.argv
BUILD_ONLY_MODE = "--build-only" in sys.argv
FACE_DIAGNOSTIC_MODE = "--face-diagnostic" in sys.argv

# 사용자가 제거를 지정한 우측 저해상도 보조 총기의 원본 연결 표면 번호입니다.
REMOVED_STICK_WEAPON_COMPONENT_INDICES = (57, 79, 92)

# 사용자가 제거를 지정한 허리띠 본체, 파우치, 버클과 연결 고정부의 원본 연결 표면 번호입니다.
# 대각선 가슴 스트랩은 별도 연결 표면 22번이므로 이 집합에 포함하지 않습니다.
REMOVED_WAIST_BELT_COMPONENT_INDICES = (48, 50, 55, 63, 65, 70, 82, 84, 85, 90, 91, 94, 96, 98, 99)

# 흰색 장갑 연결 표면을 동일한 로컬 패널 좌표로 정규화하는 전용 UV 레이어입니다.
MECHANICAL_ARMOR_UV_NAME = "IspantMechanicalUV"

# 기준 이미지의 얼굴 전면 패턴을 좌우 대칭 평면 투영으로 고정하는 전용 UV 레이어입니다.
HELMET_FACE_UV_NAME = "IspantHelmetFaceUV"


def ensure_directories() -> None:
    SAMPLE_DIR.mkdir(parents=True, exist_ok=True)
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)
    DIAGNOSTIC_DIR.mkdir(parents=True, exist_ok=True)


def save_rgba_image(path: Path, pixels: np.ndarray, colorspace: str) -> None:
    height, width, channels = pixels.shape
    if channels == 3:
        alpha = np.ones((height, width, 1), dtype=np.float32)
        pixels = np.concatenate((pixels, alpha), axis=2)
    pixels = np.clip(pixels, 0.0, 1.0).astype(np.float32)
    image = bpy.data.images.get(path.name)
    if image is None:
        image = bpy.data.images.new(path.name, width=width, height=height, alpha=True)
    elif image.size[0] != width or image.size[1] != height:
        image.scale(width, height)
    image.colorspace_settings.name = colorspace
    image.pixels.foreach_set(pixels.reshape(-1))
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()


def normal_from_height(height: np.ndarray, strength: float) -> np.ndarray:
    gradient_y, gradient_x = np.gradient(height)
    normal_x = -gradient_x * strength
    normal_y = -gradient_y * strength
    normal_z = np.ones_like(height)
    length = np.sqrt(normal_x * normal_x + normal_y * normal_y + normal_z * normal_z)
    normal = np.stack((normal_x / length, normal_y / length, normal_z / length), axis=2)
    return normal * 0.5 + 0.5


def layered_noise(size: int, seed: int) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:size, 0:size].astype(np.float32) / float(size)
    noise = (
        np.sin((x * 9.0 + y * 5.0) * math.tau + 0.7) * 0.30
        + np.sin((x * 23.0 - y * 17.0) * math.tau + 1.8) * 0.18
        + np.sin((x * 61.0 + y * 47.0) * math.tau + 2.4) * 0.08
        + rng.normal(0.0, 0.15, (size, size)).astype(np.float32)
    )
    noise = (noise - noise.min()) / max(float(noise.max() - noise.min()), 1.0e-6)
    scratches = np.zeros((size, size), dtype=np.float32)
    for _ in range(70):
        start_x = int(rng.integers(0, size))
        start_y = int(rng.integers(0, size))
        length = int(rng.integers(size // 28, size // 7))
        slope = float(rng.uniform(-0.28, 0.28))
        thickness = int(rng.integers(1, 3))
        for step in range(length):
            px = (start_x + step) % size
            py = int(start_y + step * slope) % size
            scratches[max(0, py - thickness) : min(size, py + thickness + 1), px] = 1.0
    return x, y, np.clip(noise, 0.0, 1.0), np.clip(scratches, 0.0, 1.0)


def generate_texture_set(
    name: str,
    base_rgb: tuple[float, float, float],
    roughness: float,
    metallic: float,
    seed: int,
    style: str,
) -> dict[str, Path]:
    x, y, noise, scratches = layered_noise(TEXTURE_SIZE, seed)
    base = np.zeros((TEXTURE_SIZE, TEXTURE_SIZE, 3), dtype=np.float32)
    base[:] = np.array(base_rgb, dtype=np.float32)

    if style == "armor":
        # 모든 흰색 장갑 부품에 같은 위치로 반복되는 대칭형 기계 판재 구조입니다.
        edge_distance = np.minimum.reduce((x, 1.0 - x, y, 1.0 - y))
        outer_seam = np.clip((0.072 - edge_distance) / 0.018, 0.0, 1.0)
        outer_frame = np.clip((0.145 - edge_distance) / 0.050, 0.0, 1.0) * (1.0 - outer_seam)
        outer_rim = np.clip((0.016 - np.abs(edge_distance - 0.145)) / 0.010, 0.0, 1.0)

        centered_x = np.abs(x - 0.5)
        centered_y = np.abs(y - 0.5)
        chamfer_field = np.maximum.reduce(
            (centered_x - 0.31, centered_y - 0.31, centered_x + centered_y - 0.48)
        )
        inset_seam = np.clip((0.018 - np.abs(chamfer_field)) / 0.010, 0.0, 1.0)
        inset_bevel = np.clip((0.052 - np.abs(chamfer_field)) / 0.028, 0.0, 1.0) * (1.0 - inset_seam)
        inset_plate = (chamfer_field < -0.018).astype(np.float32)
        seam = np.maximum(outer_seam, inset_seam)

        rivet_dx = np.minimum(np.abs(x - 0.27), np.abs(x - 0.73))
        rivet_dy = np.minimum(np.abs(y - 0.27), np.abs(y - 0.73))
        rivet_distance = np.sqrt(rivet_dx * rivet_dx + rivet_dy * rivet_dy)
        rivet_ring = np.clip((0.048 - rivet_distance) / 0.012, 0.0, 1.0)
        rivet_core = np.clip((0.024 - rivet_distance) / 0.010, 0.0, 1.0)

        slot_distance_x = np.abs(x - 0.5)
        slot_distance_y = np.minimum(np.abs(y - 0.155), np.abs(y - 0.845))
        service_slot = np.clip((0.060 - slot_distance_x) / 0.012, 0.0, 1.0)
        service_slot *= np.clip((0.016 - slot_distance_y) / 0.007, 0.0, 1.0)

        chipped = np.clip(
            (noise - 0.83) * 3.0
            + scratches * 0.18
            + np.maximum(outer_frame, inset_bevel) * np.clip((noise - 0.65) * 1.15, 0.0, 0.30),
            0.0,
            1.0,
        )
        grime = np.clip((0.39 - noise) * 1.10 + seam * 0.24, 0.0, 0.42)
        brushed = np.sin((x * 128.0 + noise * 0.22) * math.tau) * 0.5 + 0.5

        variation = (noise - 0.5)[..., None] * np.array((0.055, 0.052, 0.045), dtype=np.float32)
        base = base + variation
        base *= 1.0 - inset_plate[..., None] * 0.070
        base *= 1.0 - outer_frame[..., None] * 0.16
        base += (outer_rim + inset_bevel)[..., None] * np.array((0.075, 0.072, 0.062), dtype=np.float32)
        base *= 1.0 - grime[..., None] * 0.24
        exposed = np.array((0.22, 0.24, 0.26), dtype=np.float32)
        groove = np.array((0.030, 0.038, 0.045), dtype=np.float32)
        fastener_ring = np.array((0.055, 0.065, 0.075), dtype=np.float32)
        fastener_core = np.array((0.38, 0.42, 0.45), dtype=np.float32)
        base = base * (1.0 - chipped[..., None] * 0.74) + exposed * chipped[..., None] * 0.74
        base = base * (1.0 - seam[..., None] * 0.86) + groove * seam[..., None] * 0.86
        base = base * (1.0 - service_slot[..., None] * 0.90) + groove * service_slot[..., None] * 0.90
        base = base * (1.0 - rivet_ring[..., None] * 0.90) + fastener_ring * rivet_ring[..., None] * 0.90
        base = base * (1.0 - rivet_core[..., None] * 0.94) + fastener_core * rivet_core[..., None] * 0.94

        height = (
            noise * 0.018
            + brushed * 0.008
            + outer_frame * 0.060
            + outer_rim * 0.16
            + inset_plate * 0.035
            + inset_bevel * 0.13
            + rivet_ring * 0.12
            + rivet_core * 0.34
            - seam * 0.44
            - service_slot * 0.38
            - scratches * 0.045
            - chipped * 0.055
        )
        rough = np.clip(
            roughness
            + inset_plate * 0.045
            + grime * 0.15
            + (noise - 0.5) * 0.050
            - seam * 0.15
            - chipped * 0.18
            - rivet_core * 0.27
            - service_slot * 0.12,
            0.12,
            0.94,
        )
        exposed_structure = np.maximum.reduce((chipped, seam, rivet_ring, rivet_core, service_slot))
        metal = np.clip(
            metallic * (1.0 - exposed_structure)
            + 0.92 * chipped
            + 0.82 * seam
            + 0.88 * rivet_ring
            + 0.98 * rivet_core
            + 0.86 * service_slot,
            0.0,
            1.0,
        )
    elif style == "leather":
        grain = np.sin((x * 5.0 + noise * 0.22) * math.tau) * 0.5 + 0.5
        pores = np.clip((noise - 0.58) * 2.5, 0.0, 1.0)
        base *= (0.72 + noise[..., None] * 0.42)
        base *= (0.86 + grain[..., None] * 0.14)
        base *= (1.0 - scratches[..., None] * 0.30)
        height = grain * 0.14 + noise * 0.18 - pores * 0.12 - scratches * 0.22
        rough = np.clip(roughness + pores * 0.12 + scratches * 0.08, 0.35, 0.98)
        metal = np.zeros_like(noise)
    elif style == "wood":
        grain = np.sin((x * 8.0 + noise * 0.75 + np.sin(y * math.tau * 2.0) * 0.18) * math.tau)
        grain = grain * 0.5 + 0.5
        dark = np.array((0.065, 0.022, 0.010), dtype=np.float32)
        light = np.array(base_rgb, dtype=np.float32)
        base = dark[None, None, :] * (1.0 - grain[..., None]) + light[None, None, :] * grain[..., None]
        base *= (0.78 + noise[..., None] * 0.30)
        base *= (1.0 - scratches[..., None] * 0.22)
        height = grain * 0.22 + noise * 0.12 - scratches * 0.16
        rough = np.clip(roughness + (1.0 - grain) * 0.12 + scratches * 0.08, 0.22, 0.9)
        metal = np.zeros_like(noise)
    else:
        brushed = np.sin((x * 96.0 + noise * 0.35) * math.tau) * 0.5 + 0.5
        base *= (0.80 + noise[..., None] * 0.15 + brushed[..., None] * 0.05)
        base *= (1.0 - scratches[..., None] * 0.14)
        height = noise * 0.035 + brushed * 0.028 - scratches * 0.11
        rough = np.clip(roughness + (noise - 0.5) * 0.10 + scratches * 0.06, 0.12, 0.92)
        metal = np.full_like(noise, metallic)

    paths = {
        "base": TEXTURE_DIR / f"{name}_basecolor.png",
        "roughness": TEXTURE_DIR / f"{name}_roughness.png",
        "metallic": TEXTURE_DIR / f"{name}_metallic.png",
        "normal": TEXTURE_DIR / f"{name}_normal.png",
    }
    save_rgba_image(paths["base"], base, "sRGB")
    save_rgba_image(paths["roughness"], np.repeat(rough[..., None], 3, axis=2), "Non-Color")
    save_rgba_image(paths["metallic"], np.repeat(metal[..., None], 3, axis=2), "Non-Color")
    save_rgba_image(paths["normal"], normal_from_height(height, 11.0), "Non-Color")
    return paths


def segment_mask(
    x: np.ndarray,
    y: np.ndarray,
    start: tuple[float, float],
    end: tuple[float, float],
    width: float,
    feather: float = 0.007,
) -> np.ndarray:
    start_x, start_y = start
    end_x, end_y = end
    delta_x = end_x - start_x
    delta_y = end_y - start_y
    length_squared = delta_x * delta_x + delta_y * delta_y
    projection = np.clip(
        ((x - start_x) * delta_x + (y - start_y) * delta_y) / max(length_squared, 1.0e-8),
        0.0,
        1.0,
    )
    closest_x = start_x + projection * delta_x
    closest_y = start_y + projection * delta_y
    distance = np.sqrt((x - closest_x) ** 2 + (y - closest_y) ** 2)
    return np.clip((width + feather - distance) / feather, 0.0, 1.0)


def generate_helmet_face_texture_set() -> dict[str, Path]:
    """기준 이미지에서 확인되는 냉정한 대칭 기계 얼굴 패턴만 생성합니다."""
    size = TEXTURE_SIZE
    v, u = np.mgrid[0:size, 0:size].astype(np.float32) / float(size - 1)
    x = (u - 0.5) * 2.0
    abs_x = np.abs(x)

    # 마모는 좌우대칭의 저진폭 변화로 제한해 정밀한 판재 인상을 우선합니다.
    symmetric_wear = (
        np.sin((abs_x * 13.0 + v * 5.0) * math.tau + 0.35) * 0.5
        + np.sin((abs_x * 29.0 - v * 17.0) * math.tau + 1.15) * 0.25
    )
    symmetric_wear = (symmetric_wear - symmetric_wear.min()) / max(
        float(symmetric_wear.max() - symmetric_wear.min()),
        1.0e-6,
    )

    ivory = np.array((0.56, 0.56, 0.53), dtype=np.float32)
    base = np.broadcast_to(ivory, (size, size, 3)).copy()
    base += (symmetric_wear - 0.5)[..., None] * np.array((0.020, 0.019, 0.016), dtype=np.float32)

    # 사용자 제공 얼굴 기준의 검은 눈 소켓을 기존 청록색 눈 둘레에 연속 면으로 구성합니다.
    eye_ratio = np.clip((abs_x - 0.16) / 0.78, 0.0, 1.0)
    eye_lower = 0.495 + eye_ratio * 0.040
    eye_upper = 0.570 + eye_ratio * 0.045
    eye_socket = (
        (abs_x >= 0.16)
        & (abs_x <= 0.94)
        & (v >= eye_lower)
        & (v <= eye_upper)
    ).astype(np.float32)

    # 기준 이미지처럼 콧등 접점에서 급상승하고, 눈 중앙에서는 완만해졌다가 외측 끝까지 다시 급상승합니다.
    forehead_seam = np.maximum.reduce(
        (
            segment_mask(abs_x, v, (0.00, 0.580), (0.16, 0.635), 0.008, 0.004),
            segment_mask(abs_x, v, (0.16, 0.635), (0.42, 0.658), 0.008, 0.004),
            segment_mask(abs_x, v, (0.42, 0.658), (0.68, 0.678), 0.008, 0.004),
            segment_mask(abs_x, v, (0.68, 0.678), (1.00, 0.797), 0.008, 0.004),
        )
    )

    # 두 눈 사이에는 기준 이미지처럼 짧고 넓은 건메탈 콧등판을 채웁니다.
    bridge_width = 0.070 + np.clip((v - 0.495) / 0.080, 0.0, 1.0) * 0.075
    bridge_plate = (
        (v >= 0.495)
        & (v <= 0.575)
        & (abs_x <= bridge_width)
    ).astype(np.float32)

    # 기준 이미지에서 눈 바깥부터 턱까지 이어지는 외측·내측 볼 프레임입니다.
    outer_cheek = np.maximum.reduce(
        (
            segment_mask(abs_x, v, (0.90, 0.535), (0.86, 0.355), 0.010, 0.004),
            segment_mask(abs_x, v, (0.86, 0.355), (0.65, 0.125), 0.010, 0.004),
        )
    )
    inner_cheek = np.maximum.reduce(
        (
            segment_mask(abs_x, v, (0.73, 0.490), (0.68, 0.345), 0.007, 0.0035),
            segment_mask(abs_x, v, (0.68, 0.345), (0.50, 0.185), 0.007, 0.0035),
        )
    )

    # 하단 턱판은 기준 이미지의 넓은 사다리꼴 경계를 닫힌 구조로 재현합니다.
    chin_seam = np.maximum.reduce(
        (
            segment_mask(abs_x, v, (0.08, 0.245), (0.40, 0.245), 0.008, 0.004),
            segment_mask(abs_x, v, (0.40, 0.245), (0.34, 0.125), 0.008, 0.004),
            segment_mask(abs_x, v, (0.34, 0.125), (0.08, 0.105), 0.008, 0.004),
            segment_mask(abs_x, v, (0.08, 0.105), (0.00, 0.135), 0.008, 0.004),
        )
    )
    chin_plate = ((abs_x < 0.39) & (v > 0.125) & (v < 0.235)).astype(np.float32)

    # 초승달 장착부 아래의 소형 이마 점검판과 두 개의 수평 슬롯입니다.
    forehead_outer = ((abs_x < 0.105) & (v > 0.820) & (v < 0.945)).astype(np.float32)
    forehead_inner = ((abs_x < 0.073) & (v > 0.840) & (v < 0.922)).astype(np.float32)
    forehead_border = np.clip(forehead_outer - forehead_inner, 0.0, 1.0)
    forehead_slot = np.maximum(
        ((abs_x < 0.050) & (np.abs(v - 0.865) < 0.005)).astype(np.float32),
        ((abs_x < 0.050) & (np.abs(v - 0.895) < 0.005)).astype(np.float32),
    )

    seam = np.maximum.reduce((forehead_seam, outer_cheek, inner_cheek, chin_seam))
    shallow_bevel = np.clip(
        np.maximum.reduce(
            (
                segment_mask(abs_x, v, (0.00, 0.580), (0.16, 0.635), 0.018, 0.005),
                segment_mask(abs_x, v, (0.16, 0.635), (0.42, 0.658), 0.018, 0.005),
                segment_mask(abs_x, v, (0.42, 0.658), (0.68, 0.678), 0.018, 0.005),
                segment_mask(abs_x, v, (0.68, 0.678), (1.00, 0.797), 0.018, 0.005),
                segment_mask(abs_x, v, (0.90, 0.535), (0.86, 0.355), 0.020, 0.005),
                segment_mask(abs_x, v, (0.86, 0.355), (0.65, 0.125), 0.020, 0.005),
            )
        )
        - seam,
        0.0,
        1.0,
    )

    gunmetal = np.array((0.135, 0.150, 0.160), dtype=np.float32)
    groove = np.array((0.080, 0.090, 0.095), dtype=np.float32)
    plate_tone = np.array((0.52, 0.52, 0.49), dtype=np.float32)
    base = base * (1.0 - chin_plate[..., None] * 0.12) + plate_tone * chin_plate[..., None] * 0.12
    base += shallow_bevel[..., None] * np.array((0.050, 0.048, 0.042), dtype=np.float32)
    base = base * (1.0 - eye_socket[..., None] * 0.92) + gunmetal * eye_socket[..., None] * 0.92
    base = base * (1.0 - bridge_plate[..., None] * 0.72) + gunmetal * bridge_plate[..., None] * 0.72
    base = base * (1.0 - seam[..., None] * 0.86) + groove * seam[..., None] * 0.86
    base = base * (1.0 - forehead_border[..., None] * 0.76) + groove * forehead_border[..., None] * 0.76
    base = base * (1.0 - forehead_slot[..., None] * 0.66) + gunmetal * forehead_slot[..., None] * 0.66
    base = np.clip(base, 0.0, 1.0)

    rough = np.full((size, size), 0.48, dtype=np.float32)
    rough += (symmetric_wear - 0.5) * 0.022
    rough = rough * (1.0 - eye_socket) + 0.32 * eye_socket
    rough = rough * (1.0 - bridge_plate) + 0.35 * bridge_plate
    rough = rough * (1.0 - seam) + 0.37 * seam
    rough = rough * (1.0 - forehead_border) + 0.41 * forehead_border
    rough = np.clip(rough, 0.16, 0.86)

    metal = np.full((size, size), 0.16, dtype=np.float32)
    metal = metal * (1.0 - eye_socket) + 0.72 * eye_socket
    metal = metal * (1.0 - bridge_plate) + 0.62 * bridge_plate
    metal = metal * (1.0 - seam) + 0.55 * seam
    metal = metal * (1.0 - forehead_border) + 0.52 * forehead_border
    metal = np.clip(metal, 0.0, 1.0)

    height = (
        symmetric_wear * 0.012
        + shallow_bevel * 0.10
        + forehead_inner * 0.055
        + chin_plate * 0.030
        - eye_socket * 0.14
        - bridge_plate * 0.09
        - seam * 0.22
        - forehead_border * 0.17
        - forehead_slot * 0.18
    )
    paths = {
        "base": TEXTURE_DIR / "helmet_face_basecolor.png",
        "roughness": TEXTURE_DIR / "helmet_face_roughness.png",
        "metallic": TEXTURE_DIR / "helmet_face_metallic.png",
        "normal": TEXTURE_DIR / "helmet_face_normal.png",
    }
    save_rgba_image(paths["base"], base, "sRGB")
    save_rgba_image(paths["roughness"], np.repeat(rough[..., None], 3, axis=2), "Non-Color")
    save_rgba_image(paths["metallic"], np.repeat(metal[..., None], 3, axis=2), "Non-Color")
    save_rgba_image(paths["normal"], normal_from_height(height, 9.0), "Non-Color")
    return paths


def create_pbr_material(
    name: str,
    texture_paths: dict[str, Path],
    uv_map_name: str | None = None,
) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.inputs["Specular IOR Level"].default_value = 0.5
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])

    base_node = nodes.new("ShaderNodeTexImage")
    base_node.image = bpy.data.images.load(str(texture_paths["base"]), check_existing=True)
    base_node.label = "Base Color"
    links.new(base_node.outputs["Color"], shader.inputs["Base Color"])

    rough_node = nodes.new("ShaderNodeTexImage")
    rough_node.image = bpy.data.images.load(str(texture_paths["roughness"]), check_existing=True)
    rough_node.image.colorspace_settings.name = "Non-Color"
    rough_node.label = "Roughness"
    links.new(rough_node.outputs["Color"], shader.inputs["Roughness"])

    metallic_node = nodes.new("ShaderNodeTexImage")
    metallic_node.image = bpy.data.images.load(str(texture_paths["metallic"]), check_existing=True)
    metallic_node.image.colorspace_settings.name = "Non-Color"
    metallic_node.label = "Metallic"
    links.new(metallic_node.outputs["Color"], shader.inputs["Metallic"])

    normal_node = nodes.new("ShaderNodeTexImage")
    normal_node.image = bpy.data.images.load(str(texture_paths["normal"]), check_existing=True)
    normal_node.image.colorspace_settings.name = "Non-Color"
    normal_map = nodes.new("ShaderNodeNormalMap")
    normal_map.inputs["Strength"].default_value = 0.48
    links.new(normal_node.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], shader.inputs["Normal"])
    if uv_map_name is not None:
        uv_map = nodes.new("ShaderNodeUVMap")
        uv_map.uv_map = uv_map_name
        uv_map.label = f"규칙적 기계 장갑 UV: {uv_map_name}"
        for texture_node in (base_node, rough_node, metallic_node, normal_node):
            links.new(uv_map.outputs["UV"], texture_node.inputs["Vector"])
    return material


def create_helmet_material(texture_paths: dict[str, Path]) -> bpy.types.Material:
    return create_pbr_material("Ispant_Helmet", texture_paths, MECHANICAL_ARMOR_UV_NAME)


def create_helmet_face_material(texture_paths: dict[str, Path]) -> bpy.types.Material:
    return create_pbr_material("Ispant_Helmet_Face", texture_paths, HELMET_FACE_UV_NAME)


def create_eye_material() -> bpy.types.Material:
    material = bpy.data.materials.new("Ispant_Eye_Cyan")
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (0.015, 0.24, 0.32, 1.0)
    shader.inputs["Metallic"].default_value = 0.18
    shader.inputs["Roughness"].default_value = 0.20
    shader.inputs["Emission Color"].default_value = (0.015, 0.48, 0.70, 1.0)
    shader.inputs["Emission Strength"].default_value = 2.4
    return material


def create_reference_eye_slits(source_obj: bpy.types.Object) -> bpy.types.Object:
    """기준 이미지에 노출된 좌우 청록색 슬릿만 기존 헬멧 전면에 배치합니다."""
    inverse_world = source_obj.matrix_world.inverted()
    world_vertices: list[Vector] = []
    faces: list[list[int]] = []

    # 기준 이미지와 기존 헬멧의 전면 경계에서 읽은 좌우 대칭 슬릿입니다.
    # 외곽으로 갈수록 헬멧 표면이 뒤로 물러나는 값만 반영하며 숨은 눈 구조는 만들지 않습니다.
    helmet_center_x = 0.006
    radial_steps = (0.024, 0.042, 0.060, 0.078)
    evaluated_surface_y = (-0.199, -0.192, -0.181, -0.165)
    for side in (-1.0, 1.0):
        front_lower: list[Vector] = []
        front_upper: list[Vector] = []
        for radius, surface_y in zip(radial_steps, evaluated_surface_y, strict=True):
            ratio = (radius - radial_steps[0]) / (radial_steps[-1] - radial_steps[0])
            # 폭·두께는 유지하고 바깥쪽 상승량만 사용자 제공 화난 눈 각도에 맞춥니다.
            lower_z = 1.7915 + 0.014 * ratio
            front_lower.append(Vector((helmet_center_x + side * radius, surface_y, lower_z)))
            front_upper.append(Vector((helmet_center_x + side * radius, surface_y, lower_z + 0.017)))
        front_points = front_lower + front_upper
        back_points = [Vector((point.x, point.y + 0.004, point.z)) for point in front_points]
        start = len(world_vertices)
        world_vertices.extend(front_points)
        world_vertices.extend(back_points)
        width_count = len(radial_steps)
        back_start = start + width_count * 2
        for index in range(width_count - 1):
            faces.append(
                [
                    start + index,
                    start + index + 1,
                    start + width_count + index + 1,
                    start + width_count + index,
                ]
            )
            faces.append(
                [
                    back_start + width_count + index,
                    back_start + width_count + index + 1,
                    back_start + index + 1,
                    back_start + index,
                ]
            )
            faces.append(
                [
                    start + index,
                    back_start + index,
                    back_start + index + 1,
                    start + index + 1,
                ]
            )
            faces.append(
                [
                    start + width_count + index + 1,
                    back_start + width_count + index + 1,
                    back_start + width_count + index,
                    start + width_count + index,
                ]
            )
        faces.extend(
            [
                [start, start + width_count, back_start + width_count, back_start],
                [
                    start + width_count - 1,
                    back_start + width_count - 1,
                    back_start + width_count * 2 - 1,
                    start + width_count * 2 - 1,
                ],
            ]
        )

    local_vertices = [tuple(inverse_world @ point) for point in world_vertices]
    mesh = bpy.data.meshes.new("Ispant_Reference_Eye_Slits_Mesh")
    mesh.from_pydata(local_vertices, [], faces)
    mesh.update()
    eyes = bpy.data.objects.new("Ispant_Reference_Eye_Slits", mesh)
    bpy.context.collection.objects.link(eyes)
    eyes.data.materials.append(create_eye_material())
    eyes["sample_scope"] = "청록색 눈 슬릿의 폭·두께·토폴로지는 유지하고 바깥쪽 상승 각도만 14.5도로 조정"
    eyes["outer_rise_m"] = 0.014
    eyes["slit_angle_degrees"] = math.degrees(math.atan2(0.014, radial_steps[-1] - radial_steps[0]))
    if source_obj.parent and source_obj.parent.type == "ARMATURE":
        eyes.parent = source_obj.parent
        eyes.parent_type = "BONE"
        eyes.parent_bone = "Head"
        eyes.matrix_world = source_obj.matrix_world.copy()
    else:
        eyes.parent = source_obj.parent
        eyes.matrix_world = source_obj.matrix_world.copy()
    bevel = eyes.modifiers.new("Eye_Slit_Edge_Bevel", "BEVEL")
    bevel.width = 0.10
    bevel.segments = 2
    return eyes


def create_rubber_material() -> bpy.types.Material:
    material = bpy.data.materials.new("Ispant_Rubber_Black")
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (0.012, 0.016, 0.020, 1.0)
    shader.inputs["Metallic"].default_value = 0.10
    shader.inputs["Roughness"].default_value = 0.68
    return material


def connected_components(mesh: bpy.types.Mesh) -> list[dict[str, object]]:
    adjacency: dict[int, set[int]] = defaultdict(set)
    for edge in mesh.edges:
        a, b = edge.vertices
        adjacency[a].add(b)
        adjacency[b].add(a)
    seen: set[int] = set()
    components: list[dict[str, object]] = []
    for start in range(len(mesh.vertices)):
        if start in seen:
            continue
        queue = deque([start])
        seen.add(start)
        vertex_ids: list[int] = []
        while queue:
            current = queue.popleft()
            vertex_ids.append(current)
            for neighbor in adjacency[current]:
                if neighbor not in seen:
                    seen.add(neighbor)
                    queue.append(neighbor)
        points = [mesh.vertices[index].co.copy() for index in vertex_ids]
        minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
        maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
        components.append(
            {
                "count": len(vertex_ids),
                "vertices": set(vertex_ids),
                "minimum": minimum,
                "maximum": maximum,
                "center": (minimum + maximum) * 0.5,
                "size": maximum - minimum,
            }
        )
    components.sort(key=lambda item: int(item["count"]), reverse=True)
    return components


def create_component_local_mechanical_uv(
    obj: bpy.types.Object,
    components: list[dict[str, object]],
    armor_material_names: set[str],
) -> None:
    """Create a uniform, component-local panel UV without altering the source UV."""
    mesh = obj.data
    original_layer = mesh.uv_layers.get("uv")
    if original_layer is None:
        raise RuntimeError("Source UV layer 'uv' was not found.")

    existing_layer = mesh.uv_layers.get(MECHANICAL_ARMOR_UV_NAME)
    if existing_layer is not None:
        mesh.uv_layers.remove(existing_layer)
    mechanical_layer = mesh.uv_layers.new(name=MECHANICAL_ARMOR_UV_NAME)

    for loop_uv in mechanical_layer.data:
        loop_uv.uv = (0.5, 0.5)

    vertex_to_component: dict[int, int] = {}
    for component_index, component in enumerate(components):
        for vertex_index in component["vertices"]:
            vertex_to_component[vertex_index] = component_index

    component_polygons: dict[int, list[bpy.types.MeshPolygon]] = defaultdict(list)
    for polygon in mesh.polygons:
        material_name = mesh.materials[polygon.material_index].name
        if material_name not in armor_material_names:
            continue
        component_index = vertex_to_component[polygon.vertices[0]]
        component_polygons[component_index].append(polygon)

    for polygons in component_polygons.values():
        vertex_indices = sorted({vertex_index for polygon in polygons for vertex_index in polygon.vertices})
        world_points = np.asarray(
            [tuple(obj.matrix_world @ mesh.vertices[vertex_index].co) for vertex_index in vertex_indices],
            dtype=np.float64,
        )
        centered_points = world_points - world_points.mean(axis=0)
        covariance = centered_points.T @ centered_points / max(len(centered_points) - 1, 1)
        eigenvalues, eigenvectors = np.linalg.eigh(covariance)
        projection_axes = eigenvectors[:, np.argsort(eigenvalues)[::-1][:2]]
        projected_points = centered_points @ projection_axes
        minimum = projected_points.min(axis=0)
        span = np.maximum(np.ptp(projected_points, axis=0), 1.0e-6)
        normalized_uv = 0.04 + ((projected_points - minimum) / span) * 0.92
        uv_by_vertex = dict(zip(vertex_indices, normalized_uv, strict=True))

        for polygon in polygons:
            for loop_index in polygon.loop_indices:
                vertex_index = mesh.loops[loop_index].vertex_index
                mechanical_layer.data[loop_index].uv = tuple(uv_by_vertex[vertex_index])

    original_layer.active_render = True
    mechanical_layer.active_render = False
    mesh.uv_layers.active_index = mesh.uv_layers.find(original_layer.name)
    obj["mechanical_armor_uv"] = (
        "Component-local regular panel coordinates; source uv remains the active render layer."
    )


def create_helmet_face_uv(obj: bpy.types.Object) -> None:
    """얼굴 전면 폴리곤을 기준 이미지용 좌우 대칭 평면 UV에 고정합니다."""
    mesh = obj.data
    original_layer = mesh.uv_layers.get("uv")
    if original_layer is None:
        raise RuntimeError("Source UV layer 'uv' was not found.")
    existing_layer = mesh.uv_layers.get(HELMET_FACE_UV_NAME)
    if existing_layer is not None:
        mesh.uv_layers.remove(existing_layer)
    face_layer = mesh.uv_layers.new(name=HELMET_FACE_UV_NAME)
    for loop_uv in face_layer.data:
        loop_uv.uv = (-1.0, -1.0)

    face_polygon_count = 0
    for polygon in mesh.polygons:
        material_name = mesh.materials[polygon.material_index].name
        if material_name != "Ispant_Helmet_Face":
            continue
        face_polygon_count += 1
        for loop_index in polygon.loop_indices:
            vertex_index = mesh.loops[loop_index].vertex_index
            world_point = obj.matrix_world @ mesh.vertices[vertex_index].co
            u = (world_point.x + 0.075) / 0.170
            v = (world_point.z - 1.630) / 0.320
            face_layer.data[loop_index].uv = (u, v)

    if face_polygon_count < 80:
        raise RuntimeError(f"얼굴 전면 폴리곤 수가 예상보다 적습니다: {face_polygon_count}")
    original_layer.active_render = True
    face_layer.active_render = False
    mesh.uv_layers.active_index = mesh.uv_layers.find(original_layer.name)
    obj["helmet_face_uv"] = (
        f"Reference-symmetric planar face UV; polygons={face_polygon_count}; source uv preserved."
    )


def identify_ring_component(components: list[dict[str, object]], obj: bpy.types.Object) -> int:
    candidates: list[tuple[float, int]] = []
    for index, component in enumerate(components):
        minimum = obj.matrix_world @ component["minimum"]
        maximum = obj.matrix_world @ component["maximum"]
        size = maximum - minimum
        center = (minimum + maximum) * 0.5
        if center.z > 2.0 and 0.15 < abs(size.x) < 0.35 and abs(size.y) < 0.06 and 0.15 < abs(size.z) < 0.35:
            candidates.append((center.z, index))
    if len(candidates) != 1:
        raise RuntimeError(f"머리 원형 장식을 하나로 특정하지 못했습니다: {candidates}")
    return candidates[0][1]


def assign_materials(
    obj: bpy.types.Object,
    components: list[dict[str, object]],
    ring_index: int,
    materials: dict[str, bpy.types.Material],
) -> None:
    obj.data.materials.clear()
    material_order = ["armor", "helmet", "face", "gunmetal", "leather", "wood", "steel", "copper", "rubber"]
    for key in material_order:
        obj.data.materials.append(materials[key])
    slot = {key: index for index, key in enumerate(material_order)}
    vertex_to_component: dict[int, int] = {}
    for component_index, component in enumerate(components):
        for vertex_index in component["vertices"]:
            vertex_to_component[vertex_index] = component_index

    leather_components = {22, 48, 50, 53, 55, 63, 68, 72, 83, 85, 86, 90, 94, 98}
    joint_components = {23, 25, 27, 30, 32, 33, 34, 38, 40, 44, 47, 49, 52, 56, 60, 61, 62, 65, 66, 70, 76, 80, 81, 82, 84, 87, 88, 89, 93, 95, 96, 97, 99}
    copper_components = {47, 52, 58, 59, 80, 81, 93, 97}
    rubber_components = {2, 3}
    steel_weapon_components = {38, 44, 60, 74, 76, 92, 95}
    wood_weapon_components = {57, 79}

    for polygon in obj.data.polygons:
        component_index = vertex_to_component[polygon.vertices[0]]
        center_local = sum((obj.data.vertices[index].co for index in polygon.vertices), Vector()) / len(polygon.vertices)
        center = obj.matrix_world @ center_local
        material_key = "armor"
        if component_index == ring_index:
            material_key = "steel"
        elif component_index == 1:
            if center.y < 0.025 and 1.63 < center.z < 1.93:
                material_key = "face"
            else:
                material_key = "helmet"
        elif component_index == 8:
            if abs(center.x) < 0.38 and 1.02 < center.z < 1.62:
                material_key = "leather"
            elif center.z > 1.68 and center.x < -0.18:
                material_key = "wood"
            elif center.x > 0.42 and 0.72 < center.z < 1.26:
                material_key = "wood"
            else:
                material_key = "steel"
        elif component_index in steel_weapon_components:
            material_key = "steel"
        elif component_index in wood_weapon_components:
            material_key = "wood"
        elif component_index in leather_components:
            material_key = "leather"
        elif component_index in copper_components:
            material_key = "copper"
        elif component_index in rubber_components:
            material_key = "rubber"
        elif component_index in joint_components:
            material_key = "gunmetal"

        polygon.material_index = slot[material_key]


def remove_mesh_vertices(obj: bpy.types.Object, vertex_indices: set[int]) -> None:
    mesh_data = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh_data)
    bm.verts.ensure_lookup_table()
    vertices = [bm.verts[index] for index in sorted(vertex_indices)]
    bmesh.ops.delete(bm, geom=vertices, context="VERTS")
    bm.to_mesh(mesh_data)
    bm.free()
    mesh_data.update()


def create_crescent(
    source_obj: bpy.types.Object,
    ring_component: dict[str, object],
    material: bpy.types.Material,
) -> bpy.types.Object:
    minimum: Vector = ring_component["minimum"]
    maximum: Vector = ring_component["maximum"]
    center = (minimum + maximum) * 0.5
    size = maximum - minimum
    outer_radius = min(size.x, size.y) * 0.50
    inner_radius = outer_radius * 0.82
    offset = outer_radius * 0.36
    depth = max(size.z, outer_radius * 0.13)
    intersection_x = (outer_radius * outer_radius - inner_radius * inner_radius + offset * offset) / (2.0 * offset)
    outer_angle = math.acos(intersection_x / outer_radius)
    inner_angle = math.acos((intersection_x - offset) / inner_radius)
    segments = 36
    outer_points: list[tuple[float, float]] = []
    for step in range(segments + 1):
        angle = outer_angle + (math.tau - 2.0 * outer_angle) * step / segments
        outer_points.append((math.cos(angle) * outer_radius, math.sin(angle) * outer_radius))
    inner_points: list[tuple[float, float]] = []
    for step in range(segments + 1):
        angle = inner_angle + (math.tau - 2.0 * inner_angle) * step / segments
        inner_points.append((offset + math.cos(angle) * inner_radius, math.sin(angle) * inner_radius))
    outline = outer_points + inner_points

    vertices: list[tuple[float, float, float]] = []
    for z_offset in (-depth * 0.5, depth * 0.5):
        vertices.extend((center.x + x_value, center.y + y_value, center.z + z_offset) for x_value, y_value in outline)
    count = len(outline)
    faces: list[list[int]] = []
    inner_start = len(outer_points)
    for index in range(segments):
        outer_a = index
        outer_b = index + 1
        inner_a = inner_start + index
        inner_b = inner_start + index + 1
        faces.append([outer_a, outer_b, inner_b, inner_a])
        faces.append([count + inner_a, count + inner_b, count + outer_b, count + outer_a])
        faces.append([outer_a, count + outer_a, count + outer_b, outer_b])
        faces.append([inner_b, count + inner_b, count + inner_a, inner_a])
    faces.append([0, inner_start, count + inner_start, count])
    faces.append([segments, count + segments, count + inner_start + segments, inner_start + segments])
    mesh = bpy.data.meshes.new("Ispant_Crescent_Ornament_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    crescent = bpy.data.objects.new("Ispant_Crescent_Ornament", mesh)
    bpy.context.collection.objects.link(crescent)
    crescent.parent = source_obj.parent
    crescent.matrix_world = source_obj.matrix_world.copy()
    crescent.data.materials.append(material)
    crescent["sample_scope"] = "원형 장식만 기준 이미지의 초승달 형태로 교체"
    if source_obj.parent and source_obj.parent.type == "ARMATURE":
        modifier = crescent.modifiers.new("Armature", "ARMATURE")
        modifier.object = source_obj.parent
        head_group = crescent.vertex_groups.new(name="Head")
        head_group.add(list(range(len(mesh.vertices))), 1.0, "REPLACE")
    bevel = crescent.modifiers.new("Crescent_Edge_Bevel", "BEVEL")
    bevel.width = outer_radius * 0.045
    bevel.segments = 3
    return crescent


def look_at(obj: bpy.types.Object, target: tuple[float, float, float]) -> None:
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def create_text(label: str, location: tuple[float, float, float], size: float) -> bpy.types.Object:
    bpy.ops.object.text_add(location=location, rotation=(math.pi * 0.5, 0.0, 0.0))
    text = bpy.context.object
    text.data.body = label
    text.data.align_x = "CENTER"
    text.data.align_y = "CENTER"
    text.data.size = size
    material = bpy.data.materials.get("Review_Label")
    if material is None:
        material = bpy.data.materials.new("Review_Label")
        material.use_nodes = True
        shader = material.node_tree.nodes.get("Principled BSDF")
        shader.inputs["Base Color"].default_value = (0.72, 0.84, 0.92, 1.0)
        shader.inputs["Emission Color"].default_value = (0.18, 0.35, 0.52, 1.0)
        shader.inputs["Emission Strength"].default_value = 0.8
    text.data.materials.append(material)
    return text


def add_review_lighting() -> None:
    world = bpy.context.scene.world
    if world is None:
        world = bpy.data.worlds.new("Ispant_Review_World")
        bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.014, 0.020, 0.030, 1.0)
    background.inputs["Strength"].default_value = 0.30
    for location, energy, size in (
        ((-3.8, -4.5, 5.2), 1500.0, 3.5),
        ((3.4, -2.2, 3.2), 950.0, 3.0),
        ((0.0, 3.5, 4.5), 1300.0, 2.5),
    ):
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.data.energy = energy
        light.data.size = size
        look_at(light, (0.0, 0.05, 1.15))


def add_ground() -> None:
    bpy.ops.mesh.primitive_plane_add(size=18.0, location=(0.0, 0.0, -0.012))
    plane = bpy.context.object
    plane.name = "Review_Ground"
    material = bpy.data.materials.new("Review_Ground_Material")
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (0.025, 0.035, 0.050, 1.0)
    shader.inputs["Metallic"].default_value = 0.18
    shader.inputs["Roughness"].default_value = 0.64
    plane.data.materials.append(material)


def duplicate_character(
    armature: bpy.types.Object,
    meshes: list[bpy.types.Object],
    label: str,
    location_x: float,
    rotation_z: float,
) -> tuple[bpy.types.Object, list[bpy.types.Object]]:
    duplicate_armature = armature.copy()
    duplicate_armature.data = armature.data.copy()
    bpy.context.collection.objects.link(duplicate_armature)
    duplicate_armature.name = f"Review_{label}_Armature"
    duplicate_armature.location.x += location_x
    duplicate_armature.rotation_euler.z += rotation_z
    duplicate_meshes: list[bpy.types.Object] = []
    for source in meshes:
        duplicate = source.copy()
        duplicate.data = source.data.copy()
        bpy.context.collection.objects.link(duplicate)
        duplicate.parent = duplicate_armature
        duplicate.matrix_parent_inverse = source.matrix_parent_inverse.copy()
        duplicate.matrix_basis = source.matrix_basis.copy()
        duplicate.name = f"Review_{label}_{source.name}"
        for modifier in duplicate.modifiers:
            if modifier.type == "ARMATURE":
                modifier.object = duplicate_armature
        duplicate_meshes.append(duplicate)
    return duplicate_armature, duplicate_meshes


def create_static_review_snapshot(
    source: bpy.types.Object,
    label: str,
    location_x: float,
    rotation_z: float,
) -> bpy.types.Object:
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = source.evaluated_get(depsgraph)
    snapshot_mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=depsgraph)
    snapshot = bpy.data.objects.new(f"Review_{label}_{source.name}_Snapshot", snapshot_mesh)
    bpy.context.collection.objects.link(snapshot)
    view_transform = Matrix.Translation(Vector((location_x, 0.0, 0.0))) @ Matrix.Rotation(rotation_z, 4, "Z")
    snapshot.matrix_world = view_transform @ source.matrix_world
    snapshot["review_only"] = "최종 검토 이미지용 평가 완료 정적 메시"
    return snapshot


def add_crescent_detail(crescent: bpy.types.Object) -> None:
    evaluated = crescent.evaluated_get(bpy.context.evaluated_depsgraph_get())
    evaluated_mesh = evaluated.to_mesh()
    world_vertices = [crescent.matrix_world @ vertex.co for vertex in evaluated_mesh.vertices]
    center = sum(world_vertices, Vector()) / len(world_vertices)
    destination = Vector((0.0, -0.65, 2.63))
    vertices = [tuple((vertex - center) * 2.35 + destination) for vertex in world_vertices]
    faces = [list(polygon.vertices) for polygon in evaluated_mesh.polygons]
    mesh = bpy.data.meshes.new("Review_Crescent_Detail_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    detail = bpy.data.objects.new("Review_Crescent_Detail", mesh)
    bpy.context.collection.objects.link(detail)
    detail.data.materials.append(crescent.data.materials[0])
    evaluated.to_mesh_clear()
    create_text("CRESCENT DETAIL", (0.0, -0.68, 2.93), 0.085)


def setup_render(path: Path, final: bool) -> None:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.image_settings.file_format = "PNG"
    scene.render.resolution_percentage = 100
    scene.render.resolution_x = 2048 if final else 900
    scene.render.resolution_y = 1152 if final else 900
    scene.render.filepath = str(path)
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.render.resolution_percentage = 100
    scene.render.image_settings.color_depth = "8"


def render_diagnostic() -> None:
    add_ground()
    add_review_lighting()
    bpy.ops.object.camera_add(location=(0.0, -5.8, 1.18))
    camera = bpy.context.object
    camera.data.lens = 58.0
    look_at(camera, (0.0, 0.06, 1.12))
    bpy.context.scene.camera = camera
    setup_render(DIAGNOSTIC_PATH, final=False)
    bpy.ops.render.render(write_still=True)


def render_face_diagnostic() -> None:
    """원본 얼굴 크롭과 직접 대조할 정면 확대 진단만 렌더합니다."""
    add_review_lighting()
    bpy.ops.object.camera_add(location=(0.0, -1.38, 1.785))
    camera = bpy.context.object
    camera.data.lens = 72.0
    look_at(camera, (0.0, -0.165, 1.785))
    bpy.context.scene.camera = camera
    setup_render(FACE_DIAGNOSTIC_PATH, final=False)
    bpy.context.scene.render.resolution_x = 1200
    bpy.context.scene.render.resolution_y = 900
    bpy.ops.render.render(write_still=True)


def render_final(
    armature: bpy.types.Object,
    meshes: list[bpy.types.Object],
    crescent: bpy.types.Object,
) -> None:
    for label, location_x, rotation_z in (
        ("FRONT", -2.15, 0.0),
        ("THREE_QUARTER", 0.0, math.radians(-38.0)),
        ("BACK", 2.15, math.pi),
    ):
        for source in meshes:
            create_static_review_snapshot(source, label, location_x, rotation_z)
    armature.hide_render = True
    for source in meshes:
        source.hide_render = True
    create_text("FRONT", (-2.15, -0.72, 2.34), 0.09)
    create_text("THREE QUARTER", (0.0, -0.72, 2.34), 0.09)
    create_text("BACK", (2.15, -0.72, 2.34), 0.09)
    add_crescent_detail(crescent)
    add_ground()
    add_review_lighting()
    bpy.ops.object.camera_add(location=(0.0, -10.0, 1.42))
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 7.0
    look_at(camera, (0.0, 0.08, 1.42))
    bpy.context.scene.camera = camera
    setup_render(FINAL_PATH, final=True)
    bpy.ops.render.render(write_still=True)


def export_sample(armature: bpy.types.Object, meshes: list[bpy.types.Object]) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.gltf(
        filepath=str(GLB_PATH),
        export_format="GLB",
        use_selection=True,
        export_animations=False,
        export_apply=False,
        export_cameras=False,
        export_lights=False,
        export_extras=True,
    )


def main() -> None:
    ensure_directories()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    for imported_name in ("Cube", "Camera", "Light"):
        imported = bpy.data.objects.get(imported_name)
        if imported is not None:
            bpy.data.objects.remove(imported, do_unlink=True)
    armature = bpy.data.objects.get("Armature")
    body = bpy.data.objects.get("char1")
    if armature is None or body is None or body.type != "MESH":
        raise RuntimeError("원본 FBX의 Armature/char1 스킨드 메시를 찾지 못했습니다.")

    texture_sets = {
        "armor": generate_texture_set("armor_ivory", (0.61, 0.60, 0.55), 0.49, 0.14, 101, "armor"),
        "face": generate_helmet_face_texture_set(),
        "gunmetal": generate_texture_set("gunmetal", (0.085, 0.095, 0.105), 0.36, 0.92, 203, "metal"),
        "leather": generate_texture_set("leather_brown", (0.28, 0.12, 0.055), 0.72, 0.0, 307, "leather"),
        "wood": generate_texture_set("musket_wood", (0.35, 0.12, 0.038), 0.54, 0.0, 409, "wood"),
        "steel": generate_texture_set("steel_silver", (0.64, 0.67, 0.70), 0.38, 0.96, 503, "metal"),
        "copper": generate_texture_set("copper_accent", (0.40, 0.15, 0.055), 0.34, 0.91, 601, "metal"),
    }
    materials = {}
    for key, value in texture_sets.items():
        if key == "armor":
            materials[key] = create_pbr_material("Ispant_Armor", value, MECHANICAL_ARMOR_UV_NAME)
        elif key == "face":
            materials[key] = create_helmet_face_material(value)
        else:
            materials[key] = create_pbr_material(f"Ispant_{key.title()}", value)
    normal_strengths = {
        "armor": 0.66,
        "face": 0.66,
        "gunmetal": 0.30,
        "leather": 0.30,
        "wood": 0.16,
        "steel": 0.14,
        "copper": 0.18,
    }
    for key, strength in normal_strengths.items():
        normal_node = next(node for node in materials[key].node_tree.nodes if node.type == "NORMAL_MAP")
        normal_node.inputs["Strength"].default_value = strength
    materials["helmet"] = create_helmet_material(texture_sets["armor"])
    helmet_normal = next(node for node in materials["helmet"].node_tree.nodes if node.type == "NORMAL_MAP")
    helmet_normal.inputs["Strength"].default_value = 0.66
    for armor_key in ("armor", "helmet", "face"):
        armor_shader = next(node for node in materials[armor_key].node_tree.nodes if node.type == "BSDF_PRINCIPLED")
        armor_shader.inputs["Coat Weight"].default_value = 0.16
        armor_shader.inputs["Coat Roughness"].default_value = 0.34
    materials["rubber"] = create_rubber_material()

    components = connected_components(body.data)
    ring_index = identify_ring_component(components, body)
    ring_component = components[ring_index]
    if int(ring_component["count"]) != 48:
        raise RuntimeError(f"예상한 48정점 원형 장식과 일치하지 않습니다: {ring_component['count']}")
    stick_components = [components[index] for index in REMOVED_STICK_WEAPON_COMPONENT_INDICES]
    stick_vertex_counts = tuple(int(component["count"]) for component in stick_components)
    if stick_vertex_counts != (15, 11, 8):
        raise RuntimeError(f"사용자 지정 막대형 보조 총기 구조가 예상과 다릅니다: {stick_vertex_counts}")
    waist_belt_components = [components[index] for index in REMOVED_WAIST_BELT_COMPONENT_INDICES]
    waist_belt_vertex_counts = tuple(int(component["count"]) for component in waist_belt_components)
    expected_waist_belt_vertex_counts = (17, 17, 15, 13, 13, 12, 10, 10, 10, 8, 8, 8, 7, 5, 5)
    if waist_belt_vertex_counts != expected_waist_belt_vertex_counts:
        raise RuntimeError(
            "사용자 지정 허리띠 구조가 예상과 다릅니다: "
            f"{waist_belt_vertex_counts}"
        )
    if 22 in REMOVED_WAIST_BELT_COMPONENT_INDICES:
        raise RuntimeError("보존 대상 대각선 가슴 스트랩 연결 표면 22번이 제거 집합에 포함되었습니다.")
    assign_materials(body, components, ring_index, materials)
    create_component_local_mechanical_uv(
        body,
        components,
        {"Ispant_Armor", "Ispant_Helmet"},
    )
    create_helmet_face_uv(body)
    removed_stick_vertices = set().union(*(component["vertices"] for component in stick_components))
    removed_waist_belt_vertices = set().union(
        *(component["vertices"] for component in waist_belt_components)
    )
    remove_mesh_vertices(
        body,
        set(ring_component["vertices"]) | removed_stick_vertices | removed_waist_belt_vertices,
    )
    crescent = create_crescent(body, ring_component, materials["steel"])
    eye_slits = create_reference_eye_slits(body)
    body.name = "Ispant_Armed_Body"
    body.data.name = "Ispant_Armed_Body_Mesh"
    armature.name = "Ispant_Armed_Rig"
    armature["source_fbx"] = "enemies model/išpant-armed.fbx"
    armature["design_height_m"] = 1.8
    armature["approval_state"] = "ART_SAMPLE_PENDING_USER_APPROVAL"
    body["removed_user_marked_stick_components"] = ",".join(
        str(index) for index in REMOVED_STICK_WEAPON_COMPONENT_INDICES
    )
    body["removed_waist_belt_components"] = ",".join(
        str(index) for index in REMOVED_WAIST_BELT_COMPONENT_INDICES
    )
    body["preserved_diagonal_chest_strap_component"] = "22"
    body["helmet_face_pattern"] = (
        "User-reference steep-center shallow-middle steep-outer edge-connected upper-eye frame, black eye sockets, filled short nose bridge, double cheek frames and closed chin panel"
    )
    body["preserved_scope"] = (
        "대각선 가슴 스트랩, 원본 UV, 본 계층, 스킨 가중치, 무기와 신체 비율 유지"
    )

    meshes = [body, crescent, eye_slits]
    export_sample(armature, meshes)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    if BUILD_ONLY_MODE:
        pass
    elif FACE_DIAGNOSTIC_MODE:
        render_face_diagnostic()
    elif FINAL_MODE:
        render_final(armature, meshes, crescent)
    else:
        render_diagnostic()
    print(f"ISPANT_SAMPLE_BODY_VERTICES={len(body.data.vertices)}")
    print(f"ISPANT_SAMPLE_BODY_POLYGONS={len(body.data.polygons)}")
    print(f"ISPANT_CRESCENT_VERTICES={len(crescent.data.vertices)}")
    print(f"ISPANT_CRESCENT_POLYGONS={len(crescent.data.polygons)}")
    print(f"ISPANT_EYE_SLIT_VERTICES={len(eye_slits.data.vertices)}")
    print(f"ISPANT_EYE_SLIT_POLYGONS={len(eye_slits.data.polygons)}")
    print(f"ISPANT_REMOVED_STICK_VERTICES={len(removed_stick_vertices)}")
    print(f"ISPANT_REMOVED_WAIST_BELT_VERTICES={len(removed_waist_belt_vertices)}")
    print(f"ISPANT_OUTPUT_BLEND={BLEND_PATH}")
    print(f"ISPANT_OUTPUT_GLB={GLB_PATH}")
    if BUILD_ONLY_MODE:
        render_output = "NONE"
    elif FACE_DIAGNOSTIC_MODE:
        render_output = FACE_DIAGNOSTIC_PATH
    elif FINAL_MODE:
        render_output = FINAL_PATH
    else:
        render_output = DIAGNOSTIC_PATH
    print(f"ISPANT_OUTPUT_RENDER={render_output}")


if __name__ == "__main__":
    main()
