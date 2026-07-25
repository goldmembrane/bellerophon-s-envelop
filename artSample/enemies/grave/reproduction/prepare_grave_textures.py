from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "source" / "grave_reference.png"
TEXTURES = ROOT / "textures"
TEXTURES.mkdir(parents=True, exist_ok=True)

REFERENCE_CROP = (539, 38, 869, 732)
FRONT_SIZE = (1024, 2048)
rng = np.random.default_rng(731945)


def make_textile(size):
    width, height = size
    coarse = rng.normal(0.0, 1.0, (max(8, height // 32), max(8, width // 32)))
    coarse_min = float(coarse.min())
    coarse_max = float(coarse.max())
    coarse_image = Image.fromarray(
        np.uint8((coarse - coarse_min) / max(0.001, coarse_max - coarse_min) * 255.0),
        mode="L",
    ).resize(size, Image.Resampling.BICUBIC)
    coarse_array = np.asarray(coarse_image, dtype=np.float32) - 127.5

    fine = rng.normal(0.0, 10.0, (height, width)).astype(np.float32)
    yy, xx = np.indices((height, width))
    weave = (
        np.sin((xx + yy) * np.pi / 3.6) * 4.0
        + np.sin((xx - yy) * np.pi / 4.2) * 3.2
        + np.sin(xx * np.pi / 1.8) * 1.8
    )
    gray = 126.0 + coarse_array * 0.105 + fine + weave
    gray = np.clip(gray, 54.0, 188.0)
    rgb = np.stack((gray * 0.98, gray, gray * 1.01), axis=-1)
    return np.uint8(np.clip(rgb, 0, 255))


reference = Image.open(SOURCE).convert("RGB")
crop = reference.crop(REFERENCE_CROP).resize(FRONT_SIZE, Image.Resampling.LANCZOS)
crop_array = np.asarray(crop, dtype=np.float32)
textile_array = make_textile(FRONT_SIZE).astype(np.float32)

gray = crop_array.mean(axis=2)
chroma = crop_array.max(axis=2) - crop_array.min(axis=2)
ink_alpha = np.clip((247.0 - gray) / 25.0, 0.0, 1.0)
ink_alpha *= np.clip((42.0 - chroma) / 42.0 + 0.72, 0.0, 1.0)
ink_alpha = Image.fromarray(np.uint8(ink_alpha * 255.0), mode="L").filter(ImageFilter.GaussianBlur(0.7))
ink_alpha_array = np.asarray(ink_alpha, dtype=np.float32)[..., None] / 255.0
front_array = textile_array * (1.0 - ink_alpha_array) + crop_array * ink_alpha_array
front_image = Image.fromarray(np.uint8(np.clip(front_array, 0, 255)), mode="RGB")
front_image.save(TEXTURES / "grave_front_albedo.png")

back_size = (1024, 1024)
back_array = make_textile(back_size)
Image.fromarray(back_array, mode="RGB").save(TEXTURES / "grave_textile_albedo.png")

normal_width, normal_height = FRONT_SIZE
height_field = rng.normal(0.0, 1.0, (normal_height, normal_width)).astype(np.float32)
height_image = Image.fromarray(
    np.uint8(np.clip((height_field + 3.0) / 6.0 * 255.0, 0.0, 255.0)), mode="L"
).filter(ImageFilter.GaussianBlur(0.65))
height_array = np.asarray(height_image, dtype=np.float32) / 255.0
gradient_y, gradient_x = np.gradient(height_array)
strength = 5.5
nx = -gradient_x * strength
ny = -gradient_y * strength
nz = np.ones_like(nx)
length = np.sqrt(nx * nx + ny * ny + nz * nz)
normal = np.stack((nx / length, ny / length, nz / length), axis=-1)
normal = np.uint8(np.clip((normal * 0.5 + 0.5) * 255.0, 0.0, 255.0))
Image.fromarray(normal, mode="RGB").save(TEXTURES / "grave_fabric_normal.png")

roughness_noise = rng.normal(0.0, 9.0, (normal_height, normal_width))
roughness = np.uint8(np.clip(196.0 + roughness_noise, 165.0, 225.0))
Image.fromarray(roughness, mode="L").save(TEXTURES / "grave_fabric_roughness.png")

print(f"GRAVE_TEXTURES_READY={TEXTURES}")

