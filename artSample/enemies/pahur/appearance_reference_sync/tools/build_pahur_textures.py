from pathlib import Path
import math
import random

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
REFERENCE = ROOT / "image/pāḫḫur(파후르).png"
SIZE = 512


PALETTE = {
    "armor_bluegray": {
        "base": (48, 66, 82),
        "accent": (95, 112, 126),
        "metallic": 190,
        "roughness": 132,
        "seed": 104,
    },
    "light_steel": {
        "base": (137, 146, 148),
        "accent": (191, 197, 194),
        "metallic": 205,
        "roughness": 142,
        "seed": 209,
    },
    "dark_mechanics": {
        "base": (19, 25, 29),
        "accent": (53, 62, 68),
        "metallic": 180,
        "roughness": 150,
        "seed": 307,
    },
    "hood_navy_cloth": {
        "base": (19, 32, 46),
        "accent": (40, 57, 72),
        "metallic": 4,
        "roughness": 214,
        "seed": 401,
    },
    "weapon_gunmetal": {
        "base": (35, 40, 42),
        "accent": (85, 88, 87),
        "metallic": 214,
        "roughness": 125,
        "seed": 503,
    },
    "heat_bronze": {
        "base": (108, 61, 34),
        "accent": (213, 107, 34),
        "metallic": 198,
        "roughness": 117,
        "seed": 601,
    },
    "fuel_tank_steel": {
        "base": (69, 70, 66),
        "accent": (124, 119, 103),
        "metallic": 202,
        "roughness": 152,
        "seed": 709,
    },
    "hose_rubber": {
        "base": (24, 26, 25),
        "accent": (65, 62, 55),
        "metallic": 20,
        "roughness": 201,
        "seed": 809,
    },
}


REFERENCE_CROPS = {
    "reference_hood_navy_direct_crop.png": (660, 24, 754, 92),
    "reference_armor_bluegray_direct_crop.png": (617, 137, 691, 223),
    "reference_light_steel_direct_crop.png": (654, 143, 735, 221),
    "reference_dark_mechanics_direct_crop.png": (664, 238, 736, 330),
    "reference_heat_bronze_direct_crop.png": (615, 285, 690, 379),
    "reference_fuel_tank_direct_crop.png": (578, 73, 646, 273),
    "reference_blue_optic_direct_crop.png": (665, 91, 721, 119),
    "reference_flame_mark_direct_crop.png": (691, 46, 731, 91),
}


def clamp(value):
    return max(0, min(255, int(round(value))))


def make_surface_texture(spec):
    rng = random.Random(spec["seed"])
    base = spec["base"]
    accent = spec["accent"]
    image = Image.new("RGB", (SIZE, SIZE), base)
    pixels = image.load()
    phase = rng.random() * math.tau

    for y in range(SIZE):
        for x in range(SIZE):
            broad = math.sin((x * 0.031) + phase) * 0.5
            broad += math.sin((y * 0.019) - phase * 0.7) * 0.5
            grain = rng.uniform(-1.0, 1.0)
            blend = 0.10 + broad * 0.035 + grain * 0.055
            pixels[x, y] = tuple(
                clamp(base[channel] * (1.0 - blend) + accent[channel] * blend)
                for channel in range(3)
            )

    draw = ImageDraw.Draw(image, "RGBA")
    if spec["metallic"] > 100:
        for _ in range(86):
            x = rng.randrange(-60, SIZE)
            y = rng.randrange(SIZE)
            length = rng.randrange(10, 90)
            alpha = rng.randrange(18, 55)
            draw.line(
                (x, y, x + length, y + rng.randrange(-3, 4)),
                fill=(225, 228, 220, alpha),
                width=rng.choice((1, 1, 2)),
            )
        for _ in range(34):
            x = rng.randrange(SIZE)
            y = rng.randrange(SIZE)
            radius = rng.randrange(1, 5)
            draw.ellipse(
                (x - radius, y - radius, x + radius, y + radius),
                fill=(15, 17, 16, rng.randrange(25, 80)),
            )
    else:
        for y in range(0, SIZE, 7):
            draw.line(
                (0, y, SIZE, y + rng.choice((-1, 0, 1))),
                fill=(150, 168, 178, 15),
                width=1,
            )

    return image.filter(ImageFilter.GaussianBlur(radius=0.35))


def make_roughness(spec):
    rng = random.Random(spec["seed"] + 9000)
    base = spec["roughness"]
    image = Image.new("L", (SIZE, SIZE), base)
    pixels = image.load()
    for y in range(SIZE):
        for x in range(SIZE):
            wave = math.sin(x * 0.047) * 7 + math.sin(y * 0.029) * 5
            pixels[x, y] = clamp(base + wave + rng.uniform(-18, 18))
    return image.filter(ImageFilter.GaussianBlur(radius=0.7))


def make_metallic(spec):
    rng = random.Random(spec["seed"] + 12000)
    base = spec["metallic"]
    image = Image.new("L", (SIZE, SIZE), base)
    pixels = image.load()
    for y in range(SIZE):
        for x in range(SIZE):
            pixels[x, y] = clamp(base + rng.uniform(-7, 7))
    return image.filter(ImageFilter.GaussianBlur(radius=0.45))


def make_micro_normal():
    rng = random.Random(7373)
    height = Image.new("L", (SIZE, SIZE), 128)
    pixels = height.load()
    for y in range(SIZE):
        for x in range(SIZE):
            value = 128
            value += math.sin(x * 0.083) * 8
            value += math.sin(y * 0.061) * 6
            value += rng.uniform(-18, 18)
            pixels[x, y] = clamp(value)
    height = height.filter(ImageFilter.GaussianBlur(radius=0.8))
    hp = height.load()
    normal = Image.new("RGB", (SIZE, SIZE), (128, 128, 255))
    np = normal.load()
    strength = 2.2
    for y in range(SIZE):
        for x in range(SIZE):
            left = hp[max(0, x - 1), y] / 255.0
            right = hp[min(SIZE - 1, x + 1), y] / 255.0
            down = hp[x, max(0, y - 1)] / 255.0
            up = hp[x, min(SIZE - 1, y + 1)] / 255.0
            nx = (left - right) * strength
            ny = (down - up) * strength
            nz = 1.0
            length = math.sqrt(nx * nx + ny * ny + nz * nz)
            np[x, y] = (
                clamp((nx / length * 0.5 + 0.5) * 255),
                clamp((ny / length * 0.5 + 0.5) * 255),
                clamp((nz / length * 0.5 + 0.5) * 255),
            )
    return normal


def make_emission(name, color, dark):
    image = Image.new("RGB", (SIZE, SIZE), dark)
    draw = ImageDraw.Draw(image)
    if name == "optic_blue":
        for y in range(SIZE):
            intensity = math.exp(-((y - SIZE * 0.5) / 88.0) ** 2)
            row = tuple(
                clamp(dark[channel] * (1.0 - intensity) + color[channel] * intensity)
                for channel in range(3)
            )
            draw.line((0, y, SIZE, y), fill=row)
        for x in range(40, SIZE, 96):
            draw.rounded_rectangle(
                (x, 190, x + 56, 322),
                radius=18,
                outline=(175, 235, 255),
                width=8,
            )
    else:
        for radius in range(210, 20, -4):
            t = 1.0 - radius / 210.0
            fill = (
                clamp(255),
                clamp(80 + 160 * t),
                clamp(12 + 48 * t),
            )
            draw.ellipse(
                (
                    SIZE // 2 - radius,
                    SIZE // 2 - radius,
                    SIZE // 2 + radius,
                    SIZE // 2 + radius,
                ),
                fill=fill,
            )
        draw.polygon(
            [
                (256, 40),
                (176, 190),
                (228, 171),
                (153, 350),
                (270, 300),
                (238, 464),
                (368, 270),
                (310, 290),
                (370, 112),
            ],
            fill=(255, 128, 18),
        )
    return image.filter(ImageFilter.GaussianBlur(radius=1.2))


def make_head_projection_decal():
    image = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image, "RGBA")
    center_x = 624

    flame = [
        (center_x, 28),
        (center_x - 18, 63),
        (center_x - 7, 57),
        (center_x - 27, 92),
        (center_x - 13, 88),
        (center_x - 20, 114),
        (center_x, 126),
        (center_x + 22, 108),
        (center_x + 13, 86),
        (center_x + 29, 72),
        (center_x + 8, 73),
        (center_x + 15, 43),
        (center_x + 2, 59),
    ]
    draw.polygon(flame, fill=(255, 98, 8, 246))
    draw.ellipse(
        (center_x - 16, 87, center_x + 16, 120),
        fill=(255, 133, 13, 255),
    )
    draw.ellipse(
        (center_x - 7, 96, center_x + 7, 111),
        fill=(20, 43, 60, 255),
    )

    eye_y = 156
    draw.polygon(
        [
            (center_x - 70, eye_y),
            (center_x - 16, eye_y + 6),
            (center_x - 21, eye_y + 20),
            (center_x - 73, eye_y + 14),
        ],
        fill=(37, 177, 255, 242),
    )
    draw.polygon(
        [
            (center_x + 16, eye_y + 6),
            (center_x + 70, eye_y),
            (center_x + 73, eye_y + 14),
            (center_x + 21, eye_y + 20),
        ],
        fill=(37, 177, 255, 242),
    )
    draw.line(
        (center_x - 67, eye_y + 5, center_x - 20, eye_y + 11),
        fill=(190, 239, 255, 255),
        width=3,
    )
    draw.line(
        (center_x + 20, eye_y + 11, center_x + 67, eye_y + 5),
        fill=(190, 239, 255, 255),
        width=3,
    )
    return image.filter(ImageFilter.GaussianBlur(radius=0.7))


def make_palette_sheet(generated):
    width = 1400
    height = 840
    sheet = Image.new("RGB", (width, height), (20, 27, 31))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    title_font = font
    draw.text((34, 28), "PAHUR MATERIAL / TEXTURE BREAKDOWN", fill=(236, 241, 242), font=title_font)
    draw.text(
        (34, 54),
        "Current FBX mesh preserved — reference-derived palette and procedural wear",
        fill=(169, 183, 188),
        font=font,
    )
    tile_w, tile_h = 300, 300
    names = list(generated.keys())
    for index, name in enumerate(names):
        col = index % 4
        row = index // 4
        x = 34 + col * 338
        y = 100 + row * 350
        preview = generated[name].resize((tile_w, tile_h), Image.Resampling.LANCZOS)
        sheet.paste(preview, (x, y))
        draw.rectangle((x, y, x + tile_w, y + tile_h), outline=(88, 106, 111), width=2)
        draw.text((x, y + tile_h + 12), name, fill=(228, 233, 232), font=font)
    return sheet


def main():
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)
    reference = Image.open(REFERENCE).convert("RGB")
    for filename, box in REFERENCE_CROPS.items():
        crop = reference.crop(box)
        crop = ImageEnhance.Contrast(crop).enhance(1.04)
        crop.save(TEXTURE_DIR / filename)

    generated = {}
    for name, spec in PALETTE.items():
        albedo = make_surface_texture(spec)
        albedo_path = TEXTURE_DIR / f"pahur_{name}_albedo.png"
        albedo.save(albedo_path)
        make_roughness(spec).save(TEXTURE_DIR / f"pahur_{name}_roughness.png")
        make_metallic(spec).save(TEXTURE_DIR / f"pahur_{name}_metallic.png")
        generated[name] = albedo

    normal = make_micro_normal()
    normal.save(TEXTURE_DIR / "pahur_shared_micro_normal.png")
    optic = make_emission("optic_blue", (40, 184, 255), (3, 18, 31))
    optic.save(TEXTURE_DIR / "pahur_optic_blue_emission.png")
    flame = make_emission("flame_orange", (255, 111, 12), (43, 10, 2))
    flame.save(TEXTURE_DIR / "pahur_flame_orange_emission.png")
    decal = make_head_projection_decal()
    decal.save(TEXTURE_DIR / "pahur_head_reference_projection_decal.png")
    generated["optic_blue_emission"] = optic
    generated["flame_orange_emission"] = flame
    make_palette_sheet(generated).save(
        SAMPLE_ROOT / "renders/06_texture_atlas_and_material_breakdown.png"
    )
    print(f"Created {len(list(TEXTURE_DIR.glob('*.png')))} texture files.")


if __name__ == "__main__":
    main()
