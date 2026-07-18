from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy.ndimage import distance_transform_edt


ROOT = Path(__file__).resolve().parents[4]
SAMPLE_ROOT = ROOT / "artSample" / "enemies" / "ostinato"
RENDER_DIR = SAMPLE_ROOT / "renders"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
EXPORT_DIR = SAMPLE_ROOT / "exports"
REFERENCE_DIR = ROOT / "image"
SOURCE_RENDER_DIR = (
    ROOT
    / "docs"
    / "validation"
    / "ostinato_placement_2026-07-18"
    / "source_orientation"
)
SOURCE_MODEL = ROOT / "Assets" / "_Project" / "Art" / "Enemies" / "Ostinato" / "Models" / "Ostinato.fbx"

REFERENCE_FRONT = next(REFERENCE_DIR.glob("ostinato(*.png"))
REFERENCE_SIDE = REFERENCE_DIR / "ostinato-beside.png"
REFERENCE_BACK = REFERENCE_DIR / "ostinato-back.png"
SOURCE_FRONT = SOURCE_RENDER_DIR / "Ostinato_Source_CameraPositiveZ.png"
SOURCE_SIDE = SOURCE_RENDER_DIR / "Ostinato_Source_CameraPositiveX.png"
SOURCE_BACK = SOURCE_RENDER_DIR / "Ostinato_Source_CameraNegativeZ.png"
SOURCE_THREE_QUARTER = SOURCE_RENDER_DIR / "Ostinato_Source_CameraNegativeX.png"

CHITIN_TEXTURE = TEXTURE_DIR / "ostinato_olive_rust_chitin_albedo.png"
TISSUE_TEXTURE = TEXTURE_DIR / "ostinato_red_brown_soft_tissue_albedo.png"
BLADE_TEXTURE = TEXTURE_DIR / "ostinato_worn_hook_blade_albedo.png"
EYE_TEXTURE = TEXTURE_DIR / "ostinato_red_compound_eye_albedo.png"
NORMAL_TEXTURE = TEXTURE_DIR / "ostinato_shell_tissue_normal.png"
ROUGHNESS_TEXTURE = TEXTURE_DIR / "ostinato_material_roughness.png"
MASK_TEXTURE = TEXTURE_DIR / "ostinato_material_region_mask_guide.png"

CANVAS = (1408, 768)
PAPER = (242, 239, 229)
REFERENCE_BY_VIEW = {
    "front": REFERENCE_FRONT,
    "side": REFERENCE_SIDE,
    "back": REFERENCE_BACK,
    "three_quarter": REFERENCE_SIDE,
}


def lerp(a, b, t):
    return a * (1.0 - t) + b * t


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def save_rgb(path: Path, data):
    Image.fromarray(np.clip(data, 0, 255).astype(np.uint8), "RGB").save(path)


def rgb_to_hsv_fields(rgb: np.ndarray):
    data = np.asarray(rgb, dtype=np.float32) / 255.0
    maximum = data.max(axis=2)
    minimum = data.min(axis=2)
    chroma = maximum - minimum
    saturation = chroma / np.maximum(maximum, 0.000001)
    hue = np.zeros_like(maximum)
    valid = chroma > 0.000001
    red, green, blue = data[..., 0], data[..., 1], data[..., 2]
    selector = valid & (maximum == red)
    hue[selector] = ((green[selector] - blue[selector]) / chroma[selector]) % 6.0
    selector = valid & (maximum == green)
    hue[selector] = ((blue[selector] - red[selector]) / chroma[selector]) + 2.0
    selector = valid & (maximum == blue)
    hue[selector] = ((red[selector] - green[selector]) / chroma[selector]) + 4.0
    return hue * 60.0, saturation, maximum


def reference_foreground(rgb: np.ndarray):
    hue, saturation, value = rgb_to_hsv_fields(rgb)
    background_distance = (255.0 - rgb).max(axis=2)
    # The white paper and soft floor shadow are excluded; dark outlines, steel, and antennae remain.
    return (background_distance > 18.0) & ((saturation > 0.075) | (value < 0.38))


def crop_reference(path: Path):
    rgb = np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)
    mask = reference_foreground(rgb)
    ys, xs = np.where(mask)
    if len(xs) == 0:
        raise RuntimeError(f"Could not isolate reference from {path}")
    padding = 3
    box = (
        max(0, int(xs.min()) - padding),
        max(0, int(ys.min()) - padding),
        min(rgb.shape[1], int(xs.max()) + padding + 1),
        min(rgb.shape[0], int(ys.max()) + padding + 1),
    )
    return rgb[box[1] : box[3], box[0] : box[2]], mask[box[1] : box[3], box[0] : box[2]]


def fill_from_reference_pixels(rgb: np.ndarray, mask: np.ndarray):
    if not np.any(mask):
        raise RuntimeError("Reference fill mask has no pixels")
    # Every empty pixel receives the exact RGB value of its closest classified reference pixel.
    # This avoids invented blur colors and the contour bands caused by repeated low-precision fills.
    nearest = distance_transform_edt(~mask, return_distances=False, return_indices=True)
    return np.clip(rgb[nearest[0], nearest[1]], 0, 255)


def material_masks(rgb: np.ndarray, foreground: np.ndarray, view: str):
    hue, saturation, value = rgb_to_hsv_fields(rgb)
    height, width = foreground.shape
    y, x = np.mgrid[0:height, 0:width].astype(np.float32)
    u = x / max(width - 1, 1)
    v = y / max(height - 1, 1)
    green = foreground & (hue >= 45.0) & (hue <= 145.0) & (saturation > 0.16)
    rust = foreground & ((hue < 45.0) | (hue > 330.0)) & (saturation > 0.18)
    blade_zone = ((u < 0.34) | (u > 0.66)) & (v > 0.12) & (v < 0.68)
    steel = foreground & blade_zone & (saturation < 0.26) & (value > 0.16) & (value < 0.94)
    if view == "front":
        eye_zone = (
            ((u > 0.40) & (u < 0.47) & (v > 0.05) & (v < 0.16))
            | ((u > 0.52) & (u < 0.60) & (v > 0.04) & (v < 0.15))
        )
    elif view == "side":
        eye_zone = (u > 0.44) & (u < 0.60) & (v > 0.03) & (v < 0.17)
    else:
        eye_zone = np.zeros_like(foreground)
    eye = foreground & eye_zone
    tissue = rust & ~eye
    return {"chitin": green, "soft_tissue": tissue, "hook_blade": steel, "compound_eye": eye}


def quantile_colors(rgb: np.ndarray, mask: np.ndarray):
    pixels = rgb[mask]
    if len(pixels) == 0:
        raise RuntimeError("Reference material mask has no pixels")
    quantiles = np.percentile(pixels, [15, 50, 85], axis=0).round().astype(int)
    return {"dark": quantiles[0].tolist(), "base": quantiles[1].tolist(), "light": quantiles[2].tolist()}


def rgb_hex(rgb):
    return "#" + "".join(f"{int(channel):02X}" for channel in rgb)


def analyze_references():
    per_view = {}
    combined = {name: [] for name in ("chitin", "soft_tissue", "hook_blade", "compound_eye")}
    for view, path in (("front", REFERENCE_FRONT), ("side", REFERENCE_SIDE), ("back", REFERENCE_BACK)):
        rgb, foreground = crop_reference(path)
        masks = material_masks(rgb, foreground, view)
        per_view[view] = {
            name: round(float(mask.sum()) / max(float(foreground.sum()), 1.0), 4)
            for name, mask in masks.items()
        }
        for name, mask in masks.items():
            if np.any(mask):
                combined[name].append(rgb[mask])
    palette = {
        name: quantile_colors(np.concatenate(pixels, axis=0)[:, None, :], np.ones((sum(len(p) for p in pixels), 1), dtype=bool))
        for name, pixels in combined.items()
        if pixels
    }
    return {"palette_rgb_quantiles": palette, "foreground_area_ratio_by_view": per_view}


def resize_rgb(data: np.ndarray, size):
    return np.asarray(
        Image.fromarray(np.clip(data, 0, 255).astype(np.uint8), "RGB").resize(size, Image.Resampling.LANCZOS),
        dtype=np.float32,
    )


def fractional_patch(rgb: np.ndarray, mask: np.ndarray, bounds):
    height, width = mask.shape
    u0, v0, u1, v1 = bounds
    x0, x1 = int(width * u0), max(int(width * u1), int(width * u0) + 1)
    y0, y1 = int(height * v0), max(int(height * v1), int(height * v0) + 1)
    patch_rgb = rgb[y0:y1, x0:x1]
    patch_mask = mask[y0:y1, x0:x1]
    return fill_from_reference_pixels(patch_rgb, patch_mask)


def raw_fractional_patch(rgb: np.ndarray, bounds):
    height, width = rgb.shape[:2]
    u0, v0, u1, v1 = bounds
    x0, x1 = int(width * u0), max(int(width * u1), int(width * u0) + 1)
    y0, y1 = int(height * v0), max(int(height * v1), int(height * v0) + 1)
    return rgb[y0:y1, x0:x1]


def mirrored_patch_fill(patch: np.ndarray, width: int, height: int):
    tile_width = max(48, width // 3)
    tile_height = max(64, height // 3)
    tile = resize_rgb(patch, (tile_width, tile_height))
    rows = []
    for row in range(math.ceil(height / tile_height)):
        columns = []
        for column in range(math.ceil(width / tile_width)):
            candidate = tile
            if column % 2:
                candidate = candidate[:, ::-1]
            if row % 2:
                candidate = candidate[::-1]
            columns.append(candidate)
        rows.append(np.concatenate(columns, axis=1))
    return np.concatenate(rows, axis=0)[:height, :width]


def reference_surface_field(width: int, height: int, view: str):
    rgb, foreground = crop_reference(REFERENCE_BY_VIEW[view])
    torso_bounds = {
        "front": (0.468, 0.202, 0.607, 0.422),
        "side": (0.599, 0.241, 0.698, 0.381),
        "back": (0.453, 0.099, 0.593, 0.319),
        "three_quarter": (0.599, 0.241, 0.698, 0.381),
    }[view]
    base = mirrored_patch_fill(raw_fractional_patch(rgb, torso_bounds), width, height)
    fallback_order = {
        "front": (REFERENCE_BACK, REFERENCE_SIDE, REFERENCE_FRONT),
        "side": (REFERENCE_BACK, REFERENCE_FRONT, REFERENCE_SIDE),
        "back": (REFERENCE_FRONT, REFERENCE_SIDE, REFERENCE_BACK),
        "three_quarter": (REFERENCE_BACK, REFERENCE_FRONT, REFERENCE_SIDE),
    }[view]
    result = base
    for path in fallback_order:
        candidate_rgb, candidate_mask = crop_reference(path)
        candidate = resize_rgb(candidate_rgb, (width, height))
        resized_mask = np.asarray(
            Image.fromarray((candidate_mask.astype(np.uint8) * 255), "L").resize(
                (width, height), Image.Resampling.NEAREST
            ),
            dtype=np.float32,
        ) / 255.0
        result = lerp(result, candidate, resized_mask[..., None])
    return result


def create_material_textures():
    size = 1024
    reference_rgb, foreground = crop_reference(REFERENCE_FRONT)
    # Tight crops keep the original painterly pixels and their spatial relationships intact.
    extracted = {
        "chitin": resize_rgb(raw_fractional_patch(reference_rgb, (0.468, 0.202, 0.607, 0.422)), (size, size)),
        "soft_tissue": resize_rgb(raw_fractional_patch(reference_rgb, (0.389, 0.635, 0.448, 0.735)), (size, size)),
        "hook_blade": resize_rgb(raw_fractional_patch(reference_rgb, (0.002, 0.449, 0.037, 0.549)), (size, size)),
        "compound_eye": resize_rgb(raw_fractional_patch(reference_rgb, (0.540, 0.060, 0.590, 0.130)), (size, size)),
    }

    save_rgb(CHITIN_TEXTURE, extracted["chitin"])
    save_rgb(TISSUE_TEXTURE, extracted["soft_tissue"])
    save_rgb(BLADE_TEXTURE, extracted["hook_blade"])
    save_rgb(EYE_TEXTURE, extracted["compound_eye"])

    # Height comes only from luminance variation already present in the reference painting.
    chitin = extracted["chitin"]
    height = chitin.mean(axis=2) / 255.0
    height = np.asarray(
        Image.fromarray(np.clip(height * 255.0, 0, 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(1.2)),
        dtype=np.float32,
    ) / 255.0
    dy, dx = np.gradient(height)
    strength = 9.0
    normal = np.dstack((-dx * strength, -dy * strength, np.ones_like(height)))
    normal /= np.maximum(np.linalg.norm(normal, axis=2, keepdims=True), 0.0001)
    normal = (normal * 0.5 + 0.5) * 255.0
    save_rgb(NORMAL_TEXTURE, normal)

    local_mean = np.asarray(
        Image.fromarray(np.clip(height * 255.0, 0, 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(5.0)),
        dtype=np.float32,
    ) / 255.0
    local_detail = np.abs(height - local_mean)
    roughness = np.clip(112.0 + local_detail * 620.0 + (1.0 - height) * 28.0, 102.0, 206.0)
    roughness_rgb = np.dstack((roughness, roughness, roughness))
    save_rgb(ROUGHNESS_TEXTURE, roughness_rgb)

    mask = np.zeros((size, size, 3), dtype=np.uint8)
    mask[: size // 2, : size // 2] = (255, 0, 0)
    mask[: size // 2, size // 2 :] = (0, 255, 0)
    mask[size // 2 :, : size // 2] = (0, 0, 255)
    mask[size // 2 :, size // 2 :] = (255, 255, 255)
    Image.fromarray(mask, "RGB").save(MASK_TEXTURE)


def isolate_model(source: Path):
    image = Image.open(source).convert("RGB")
    rgb = np.asarray(image).astype(np.float32)
    luminance = rgb.mean(axis=2)
    candidate = luminance > 14.0
    row_count = candidate.sum(axis=1)
    candidate[row_count > image.width * 0.34, :] = False
    candidate[int(image.height * 0.72) :, :] = False

    ys, xs = np.where(candidate)
    center = image.width * 0.5
    keep = np.abs(xs - center) < image.width * 0.38
    ys, xs = ys[keep], xs[keep]
    mask = np.zeros(candidate.shape, dtype=np.uint8)
    mask[ys, xs] = 255
    mask_image = Image.fromarray(mask, "L").filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(0.55))
    mask = np.asarray(mask_image).astype(np.float32) / 255.0
    strong = mask > 0.12
    ys, xs = np.where(strong)
    if len(xs) == 0:
        raise RuntimeError(f"Could not isolate model from {source}")
    padding = 5
    box = (
        max(0, int(xs.min()) - padding),
        max(0, int(ys.min()) - padding),
        min(image.width, int(xs.max()) + padding + 1),
        min(image.height, int(ys.max()) + padding + 1),
    )
    return rgb[box[1] : box[3], box[0] : box[2]], mask[box[1] : box[3], box[0] : box[2]]


def material_color_field(width: int, height: int, view: str):
    # The preview no longer invents pigment, rings, scratches, or highlights. It transfers the
    # corresponding reference painting directly into the unchanged model silhouette.
    return reference_surface_field(width, height, view)


def create_material_render(source: Path, output: Path, view: str):
    source_rgb, alpha = isolate_model(source)
    height, width = alpha.shape
    source_luma = source_rgb.mean(axis=2) / 255.0
    valid = alpha > 0.12
    low = np.percentile(source_luma[valid], 4)
    high = np.percentile(source_luma[valid], 97)
    shade = np.clip((source_luma - low) / max(high - low, 0.05), 0.0, 1.0)
    # Keep only a restrained geometry cue from the FBX capture; the reference already contains
    # the target lighting, wear, pigment, and material response.
    shade = 0.92 + shade * 0.16
    target = material_color_field(width, height, view) * shade[..., None]
    edge = np.asarray(Image.fromarray((alpha * 255).astype(np.uint8), "L").filter(ImageFilter.FIND_EDGES)).astype(np.float32) / 255.0
    target *= (1.0 - edge[..., None] * 0.12)

    rgba = np.dstack((np.clip(target, 0, 255).astype(np.uint8), (alpha * 255).astype(np.uint8)))
    cutout = Image.fromarray(rgba, "RGBA")
    target_height = 650
    scale = target_height / max(cutout.height, 1)
    cutout = cutout.resize((max(1, int(cutout.width * scale)), target_height), Image.Resampling.LANCZOS)

    canvas = Image.new("RGB", CANVAS, PAPER)
    draw = ImageDraw.Draw(canvas, "RGBA")
    center_x = CANVAS[0] // 2
    base_y = 723
    shadow_width = int(cutout.width * 0.72)
    draw.ellipse(
        (center_x - shadow_width // 2, base_y - 23, center_x + shadow_width // 2, base_y + 13),
        fill=(36, 31, 24, 48),
    )
    canvas.paste(cutout, (center_x - cutout.width // 2, base_y - cutout.height), cutout)
    canvas.save(output)


def fit_image(source: Path, size):
    image = Image.open(source).convert("RGB")
    image.thumbnail(size, Image.Resampling.LANCZOS)
    out = Image.new("RGB", size, PAPER)
    out.paste(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    return out


def create_comparison_and_breakdown():
    cell = (704, 384)
    comparison = Image.new("RGB", (1408, 1152), (226, 220, 205))
    pairs = [
        (REFERENCE_FRONT, RENDER_DIR / "01_front_current_model_reference_material.png"),
        (REFERENCE_SIDE, RENDER_DIR / "02_side_current_model_reference_material.png"),
        (REFERENCE_BACK, RENDER_DIR / "03_back_current_model_reference_material.png"),
    ]
    for row, (reference, generated) in enumerate(pairs):
        comparison.paste(fit_image(reference, cell), (0, row * cell[1]))
        comparison.paste(fit_image(generated, cell), (cell[0], row * cell[1]))
    comparison.save(RENDER_DIR / "04_reference_side_by_side_overview.png")

    tiles = [
        CHITIN_TEXTURE,
        TISSUE_TEXTURE,
        BLADE_TEXTURE,
        EYE_TEXTURE,
        NORMAL_TEXTURE,
        ROUGHNESS_TEXTURE,
    ]
    breakdown = Image.new("RGB", (1536, 1024), (29, 25, 20))
    for index, tile in enumerate(tiles):
        thumb = fit_image(tile, (480, 430))
        x = 24 + (index % 3) * 504
        y = 24 + (index // 3) * 500
        breakdown.paste(thumb, (x, y))
        draw = ImageDraw.Draw(breakdown)
        draw.text((x + 8, y + 442), tile.stem, fill=(231, 222, 200))
    breakdown.save(RENDER_DIR / "06_texture_material_breakdown.png")


def write_material_spec():
    reference_analysis = analyze_references()
    palette = reference_analysis["palette_rgb_quantiles"]
    spec = {
        "sample": "ostinato_current_model_material_sample",
        "source_model": "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx",
        "source_sha256": sha256(SOURCE_MODEL),
        "geometry_changed": False,
        "renderer_structure": {"renderer_count": 1, "mesh": "char1", "vertex_count": 3728, "submesh_count": 1},
        "reference_reproduction": {
            "target": "direct_reference_pixel_transfer",
            "references": [
                "image/ostinato(오스티나토).png",
                "image/ostinato-beside.png",
                "image/ostinato-back.png",
            ],
            **reference_analysis,
            "invented_procedural_pigment": False,
            "invented_decorative_seams": False,
        },
        "material_intent": {
            "chitin": {"base": rgb_hex(palette["chitin"]["base"]), "metallic": 0.0, "roughness": 0.48, "specular": 0.46},
            "soft_tissue": {"base": rgb_hex(palette["soft_tissue"]["base"]), "metallic": 0.0, "roughness": 0.72},
            "hook_blade": {"base": rgb_hex(palette["hook_blade"]["base"]), "metallic": 0.88, "roughness": 0.26},
            "compound_eye": {"base": rgb_hex(palette["compound_eye"]["base"]), "metallic": 0.05, "roughness": 0.16, "specular": 0.88},
        },
        "preview_projection": {
            "method": "direct_reference_pixel_transfer_by_view",
            "front": "front_reference_normalized_to_unchanged_front_silhouette",
            "side": "side_reference_normalized_to_unchanged_side_silhouette",
            "back": "back_reference_normalized_to_unchanged_back_silhouette",
            "three_quarter": "side_reference_normalized_to_unchanged_three_quarter_silhouette",
            "geometry_or_uv_changed": False,
        },
        "textures": [
            f"textures/{path.name}"
            for path in [CHITIN_TEXTURE, TISSUE_TEXTURE, BLADE_TEXTURE, EYE_TEXTURE, NORMAL_TEXTURE, ROUGHNESS_TEXTURE, MASK_TEXTURE]
        ],
        "unity_runtime_applied": False,
        "note": "Direct reference-pixel screen projection for visual approval; final Unity textures must transfer the same reference-derived distribution to the unchanged FBX UV/vertex layout after user approval.",
    }
    (EXPORT_DIR / "ostinato_current_model_material_spec.json").write_text(
        json.dumps(spec, ensure_ascii=False, indent=2), encoding="utf-8"
    )


def main():
    for folder in (RENDER_DIR, TEXTURE_DIR, EXPORT_DIR):
        folder.mkdir(parents=True, exist_ok=True)
    create_material_textures()
    create_material_render(SOURCE_FRONT, RENDER_DIR / "01_front_current_model_reference_material.png", "front")
    create_material_render(SOURCE_SIDE, RENDER_DIR / "02_side_current_model_reference_material.png", "side")
    create_material_render(SOURCE_BACK, RENDER_DIR / "03_back_current_model_reference_material.png", "back")
    create_material_render(
        SOURCE_THREE_QUARTER,
        RENDER_DIR / "05_three_quarter_current_model_reference_material.png",
        "three_quarter",
    )
    create_comparison_and_breakdown()
    write_material_spec()
    print("Ostinato material-only art sample generated")
    print(f"SourceSha256={sha256(SOURCE_MODEL)}")
    print("GeometryChanged=False")


if __name__ == "__main__":
    main()
