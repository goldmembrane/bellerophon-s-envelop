from pathlib import Path
import math
import random
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
REFERENCE = ROOT / "image/pāḫḫur(파후르).png"
SIZE = 512


PALETTE = {
    "armor_bluegray": {
        "base": (65, 32, 27),
        "accent": (127, 69, 55),
        "metallic": 184,
        "roughness": 132,
        "seed": 104,
        "style": "rigid_plate",
    },
    "light_steel": {
        "base": (101, 112, 116),
        "accent": (174, 183, 183),
        "metallic": 198,
        "roughness": 128,
        "seed": 209,
        "style": "brushed_plate",
    },
    "leg_steel": {
        "base": (45, 25, 23),
        "accent": (88, 48, 42),
        "metallic": 187,
        "roughness": 138,
        "seed": 257,
        "style": "rigid_plate",
    },
    "dark_mechanics": {
        "base": (19, 27, 33),
        "accent": (61, 74, 82),
        "metallic": 164,
        "roughness": 151,
        "seed": 307,
        "style": "mechanical_joint",
    },
    "torso_rigid_shell": {
        "base": (55, 27, 24),
        "accent": (106, 56, 48),
        "metallic": 184,
        "roughness": 138,
        "seed": 331,
        "style": "rigid_plate",
    },
    "torso_center_plate": {
        "base": (96, 106, 109),
        "accent": (154, 162, 163),
        "metallic": 210,
        "roughness": 120,
        "seed": 337,
        "style": "brushed_plate",
    },
    "torso_inner_mechanics": {
        "base": (18, 25, 30),
        "accent": (52, 63, 70),
        "metallic": 172,
        "roughness": 158,
        "seed": 347,
        "style": "mechanical_joint",
    },
    "torso_pelvis_plate": {
        "base": (72, 37, 32),
        "accent": (132, 74, 60),
        "metallic": 198,
        "roughness": 132,
        "seed": 349,
        "style": "rigid_plate",
    },
    "shoulder_machine_blue": {
        "base": (62, 30, 26),
        "accent": (122, 64, 52),
        "metallic": 184,
        "roughness": 132,
        "seed": 104,
        "style": "rigid_plate",
    },
    "left_arm_machine": {
        "base": (62, 30, 26),
        "accent": (122, 64, 52),
        "metallic": 184,
        "roughness": 132,
        "seed": 104,
        "style": "rigid_plate",
    },
    "left_hand_machine": {
        "base": (13, 18, 22),
        "accent": (55, 65, 72),
        "metallic": 180,
        "roughness": 139,
        "seed": 307,
        "style": "mechanical_joint",
    },
    "hood_navy_cloth": {
        "base": (35, 16, 18),
        "accent": (76, 37, 38),
        "metallic": 4,
        "roughness": 234,
        "seed": 401,
        "style": "cloth",
    },
    "face_metal": {
        "base": (66, 73, 77),
        "accent": (132, 141, 144),
        "metallic": 218,
        "roughness": 88,
        "seed": 457,
        "style": "face",
    },
    "weapon_gunmetal": {
        "base": (32, 35, 37),
        "accent": (100, 103, 101),
        "metallic": 214,
        "roughness": 112,
        "seed": 503,
        "style": "weapon",
    },
    "heat_bronze": {
        "base": (100, 54, 27),
        "accent": (224, 117, 36),
        "metallic": 198,
        "roughness": 117,
        "seed": 601,
        "style": "heat",
    },
    "fuel_tank_steel": {
        "base": (54, 55, 54),
        "accent": (126, 125, 120),
        "metallic": 210,
        "roughness": 104,
        "seed": 709,
        "style": "tank",
    },
    "hose_rubber": {
        "base": (18, 21, 22),
        "accent": (72, 70, 62),
        "metallic": 20,
        "roughness": 201,
        "seed": 809,
        "style": "hose",
    },
    "orange_trim": {
        "base": (170, 52, 10),
        "accent": (255, 143, 24),
        "metallic": 148,
        "roughness": 112,
        "seed": 907,
        "style": "trim",
    },
}


REFERENCE_CROPS = {
    "reference_head_detail_crop.png": (646, 18, 791, 164),
    "reference_hood_navy_direct_crop.png": (660, 24, 754, 92),
    "reference_armor_bluegray_direct_crop.png": (617, 137, 691, 223),
    "reference_light_steel_direct_crop.png": (654, 143, 735, 221),
    "reference_dark_mechanics_direct_crop.png": (664, 238, 736, 330),
    "reference_heat_bronze_direct_crop.png": (615, 285, 690, 379),
    "reference_fuel_tank_direct_crop.png": (578, 73, 646, 273),
    "reference_blue_optic_direct_crop.png": (665, 91, 721, 119),
    "reference_flame_mark_direct_crop.png": (680, 24, 742, 102),
}


def clamp(value):
    return max(0, min(255, int(round(value))))


def draw_surface_relief(draw, style, dark=42, raised=196):
    if style in {"rigid_plate", "brushed_plate"}:
        # The replacement mesh already contains its plate boundaries. Adding
        # broad texture-space seams makes the fragmented UVs look melted.
        # Only a restrained directional metal grain belongs in the normal map.
        for y in range(18, SIZE, 31):
            draw.line((0, y, SIZE, y), fill=raised, width=1)
    elif style == "mechanical_joint":
        # Fine, shallow machining bands; no diagonal weave or large fake seam.
        for y in range(24, SIZE, 48):
            draw.line((0, y, SIZE, y), fill=dark, width=2)
            draw.line((0, y + 3, SIZE, y + 3), fill=raised, width=1)
    elif style == "shoulder_machine":
        for inset in (18, 52, 88):
            draw.rounded_rectangle(
                (inset, inset, SIZE - inset, SIZE - inset),
                radius=68,
                outline=dark,
                width=7,
            )
            draw.arc(
                (inset + 8, inset + 8, SIZE - inset - 8, SIZE - inset - 8),
                205,
                332,
                fill=raised,
                width=3,
            )
        for x, y in (
            (86, 92),
            (SIZE - 86, 92),
            (86, SIZE - 92),
            (SIZE - 86, SIZE - 92),
        ):
            draw.ellipse((x - 13, y - 13, x + 13, y + 13), fill=dark)
            draw.ellipse((x - 6, y - 6, x + 6, y + 6), fill=raised)
    elif style == "limb_machine":
        for y in (12, 126, 254, 382, 500):
            draw.line((20, y, SIZE - 20, y), fill=dark, width=8)
            draw.line((28, y + 8, SIZE - 28, y + 8), fill=raised, width=3)
        draw.line((78, 24, 78, SIZE - 24), fill=dark, width=6)
        draw.line((SIZE - 78, 24, SIZE - 78, SIZE - 24), fill=dark, width=6)
        draw.line((88, 24, 88, SIZE - 24), fill=raised, width=2)
        draw.line((SIZE - 88, 24, SIZE - 88, SIZE - 24), fill=raised, width=2)
        for y in (68, 190, 318, 442):
            draw.ellipse(
                (SIZE // 2 - 17, y - 17, SIZE // 2 + 17, y + 17),
                fill=dark,
            )
            draw.ellipse(
                (SIZE // 2 - 8, y - 8, SIZE // 2 + 8, y + 8),
                fill=raised,
            )
    elif style == "hand_machine":
        for x in (48, 150, 256, 362, 464):
            draw.line((x, 12, x, SIZE - 12), fill=dark, width=7)
            draw.line((x + 8, 20, x + 8, SIZE - 20), fill=raised, width=2)
        for y in (112, 244, 376):
            draw.line((12, y, SIZE - 12, y), fill=dark, width=7)
            draw.line((20, y + 8, SIZE - 20, y + 8), fill=raised, width=2)
    elif style == "cloth":
        for y, bend in ((76, -8), (174, 7), (278, -5), (382, 9)):
            points = [(0, y + 8), (128, y), (256, y + bend), (384, y), (512, y + 8)]
            draw.line(points, fill=dark, width=6, joint="curve")
            raised_points = [(x, py + 7) for x, py in points]
            draw.line(raised_points, fill=raised, width=3, joint="curve")
    elif style == "weapon":
        for y in (104, 248, 392):
            draw.line((18, y, SIZE - 18, y), fill=dark, width=16)
            draw.line((18, y + 13, SIZE - 18, y + 13), fill=raised, width=5)
        for x in range(48, SIZE, 64):
            draw.ellipse((x - 13, 62, x + 13, 94), fill=dark)
            draw.ellipse((x - 7, 68, x + 7, 88), fill=raised)
    elif style == "tank":
        # The reference cylinder is smooth; keep only near-flat micro relief.
        return
    elif style == "hose":
        for y in range(0, SIZE, 28):
            draw.line((0, y, SIZE, y + 7), fill=raised, width=8)
            draw.line((0, y + 12, SIZE, y + 19), fill=dark, width=7)
    elif style in {"trim", "heat"}:
        for x in range(-SIZE, SIZE * 2, 80):
            draw.polygon(
                [(x, 0), (x + 24, 0), (x - 128, SIZE), (x - 152, SIZE)],
                fill=raised,
            )


def make_surface_texture(name, spec):
    rng = random.Random(spec["seed"])
    base = spec["base"]
    accent = spec["accent"]
    image = Image.new("RGB", (SIZE, SIZE), base)
    pixels = image.load()
    phase = rng.random() * math.tau
    style = spec["style"]

    rigid_metal = style in {
        "rigid_plate",
        "brushed_plate",
        "mechanical_joint",
        "weapon",
        "tank",
        "heat",
        "trim",
    }
    for y in range(SIZE):
        for x in range(SIZE):
            if style == "face":
                # The reference face is a smooth metal shell. Keep only a
                # broad lighting-scale tonal drift, without scratches, seams,
                # vents, grain, or any mouth-like mark.
                broad = math.sin((x * 0.006) + phase) * 0.5
                broad += math.sin((y * 0.004) - phase * 0.7) * 0.5
                blend = 0.16 + broad * 0.025
            elif rigid_metal:
                broad = math.sin((x * 0.010) + phase) * 0.45
                broad += math.sin((y * 0.006) - phase * 0.7) * 0.25
                grain = rng.uniform(-1.0, 1.0)
                blend = 0.105 + broad * 0.018 + grain * 0.018
            else:
                broad = math.sin((x * 0.022) + phase) * 0.4
                broad += math.sin((y * 0.014) - phase * 0.7) * 0.3
                grain = rng.uniform(-1.0, 1.0)
                blend = 0.10 + broad * 0.025 + grain * 0.035
            pixels[x, y] = tuple(
                clamp(base[channel] * (1.0 - blend) + accent[channel] * blend)
                for channel in range(3)
            )

    draw = ImageDraw.Draw(image, "RGBA")
    if spec["metallic"] > 100 and style != "face":
        for _ in range(30):
            x = rng.randrange(-60, SIZE)
            y = rng.randrange(SIZE)
            length = rng.randrange(8, 38)
            alpha = rng.randrange(10, 28)
            draw.line(
                (x, y, x + length, y + rng.choice((-1, 0, 1))),
                fill=(225, 228, 220, alpha),
                width=1,
            )
        for _ in range(12):
            x = rng.randrange(SIZE)
            y = rng.randrange(SIZE)
            radius = rng.randrange(1, 3)
            draw.ellipse(
                (x - radius, y - radius, x + radius, y + radius),
                fill=(15, 17, 16, rng.randrange(14, 36)),
            )
    elif style != "face":
        for y in range(0, SIZE, 7):
            draw.line(
                (0, y, SIZE, y + rng.choice((-1, 0, 1))),
                fill=(150, 168, 178, 15),
                width=1,
            )

    if style == "brushed_plate":
        for y in range(6, SIZE, 12):
            draw.line((0, y, SIZE, y), fill=(222, 226, 220, 9), width=1)
    elif style == "rigid_plate":
        for y in range(9, SIZE, 17):
            draw.line((0, y, SIZE, y), fill=(196, 207, 207, 7), width=1)
    elif style == "mechanical_joint":
        for y in range(24, SIZE, 48):
            draw.line((0, y, SIZE, y), fill=(4, 8, 10, 28), width=2)
            draw.line((0, y + 4, SIZE, y + 4), fill=(116, 130, 135, 11), width=1)
    elif style == "shoulder_machine":
        for inset in (18, 52, 88):
            draw.rounded_rectangle(
                (inset, inset, SIZE - inset, SIZE - inset),
                radius=68,
                outline=(14, 30, 42, 92),
                width=7,
            )
            draw.arc(
                (inset + 8, inset + 8, SIZE - inset - 8, SIZE - inset - 8),
                205,
                332,
                fill=(132, 157, 166, 54),
                width=3,
            )
        for x, y in (
            (86, 92),
            (SIZE - 86, 92),
            (86, SIZE - 92),
            (SIZE - 86, SIZE - 92),
        ):
            draw.ellipse(
                (x - 13, y - 13, x + 13, y + 13),
                fill=(17, 34, 46, 118),
                outline=(116, 143, 153, 92),
                width=2,
            )
    elif style == "limb_machine":
        for index, y in enumerate((12, 126, 254, 382, 500)):
            draw.line((20, y, SIZE - 20, y), fill=(14, 29, 40, 104), width=8)
            draw.line(
                (28, y + 8, SIZE - 28, y + 8),
                fill=(124, 150, 159, 48),
                width=3,
            )
            if index < 4:
                fill = (
                    (31, 62, 84, 30)
                    if index % 2 == 0
                    else (82, 105, 118, 20)
                )
                draw.rectangle((38, y + 22, SIZE - 38, y + 110), fill=fill)
        for x in (78, SIZE - 78):
            draw.line((x, 24, x, SIZE - 24), fill=(14, 29, 40, 92), width=6)
        for y in (68, 190, 318, 442):
            draw.ellipse(
                (SIZE // 2 - 17, y - 17, SIZE // 2 + 17, y + 17),
                fill=(17, 34, 46, 118),
                outline=(116, 143, 153, 92),
                width=2,
            )
    elif style == "hand_machine":
        for x in (48, 150, 256, 362, 464):
            draw.line((x, 12, x, SIZE - 12), fill=(5, 10, 13, 112), width=7)
            draw.line(
                (x + 8, 20, x + 8, SIZE - 20),
                fill=(91, 108, 116, 48),
                width=2,
            )
        for y in (112, 244, 376):
            draw.line((12, y, SIZE - 12, y), fill=(5, 10, 13, 120), width=7)
            draw.line(
                (20, y + 8, SIZE - 20, y + 8),
                fill=(91, 108, 116, 48),
                width=2,
            )
    elif style == "cloth":
        for y, bend in ((76, -8), (174, 7), (278, -5), (382, 9)):
            points = [(0, y + 8), (128, y), (256, y + bend), (384, y), (512, y + 8)]
            draw.line(points, fill=(1, 5, 12, 118), width=13, joint="curve")
            raised_points = [(x, py + 8) for x, py in points]
            draw.line(
                raised_points,
                fill=(91, 113, 135, 58),
                width=6,
                joint="curve",
            )
        for y in range(4, SIZE, 14):
            draw.line((0, y, SIZE, y + 1), fill=(105, 122, 140, 18), width=1)
    elif style == "weapon":
        for y in (104, 248, 392):
            draw.line((18, y, SIZE - 18, y), fill=(6, 8, 9, 150), width=10)
            draw.line((18, y + 10, SIZE - 18, y + 10), fill=(182, 186, 178, 48), width=3)
        for x in range(48, SIZE, 64):
            draw.ellipse((x - 9, 68, x + 9, 90), fill=(3, 5, 6, 180))
    elif style == "tank":
        # Broad, low-contrast reflection bands only; no ribbed pattern.
        draw.rectangle((74, 0, 142, SIZE), fill=(185, 188, 181, 13))
        draw.rectangle((148, 0, 172, SIZE), fill=(222, 220, 207, 8))
        for _ in range(14):
            x = rng.randrange(18, SIZE - 42)
            y = rng.randrange(18, SIZE - 18)
            draw.line(
                (x, y, x + rng.randrange(12, 42), y + rng.choice((-1, 0, 1))),
                fill=(210, 209, 198, rng.randrange(10, 23)),
                width=1,
            )
    elif style == "hose":
        for y in range(0, SIZE, 28):
            draw.line((0, y, SIZE, y + 7), fill=(102, 104, 94, 35), width=6)
            draw.line((0, y + 10, SIZE, y + 17), fill=(3, 5, 5, 65), width=4)
    elif style in {"trim", "heat"}:
        for x in range(-SIZE, SIZE * 2, 80):
            draw.polygon(
                [(x, 0), (x + 24, 0), (x - 128, SIZE), (x - 152, SIZE)],
                fill=(255, 190, 52, 42),
            )

    blur_radius = 1.6 if style == "face" else 0.55 if rigid_metal else 0.35
    return image.filter(ImageFilter.GaussianBlur(radius=blur_radius))


def height_to_normal(height, strength):
    height = height.filter(ImageFilter.GaussianBlur(radius=0.55))
    hp = height.load()
    normal = Image.new("RGB", height.size, (128, 128, 255))
    np = normal.load()
    width, height_px = height.size
    for y in range(height_px):
        for x in range(width):
            left = hp[max(0, x - 1), y] / 255.0
            right = hp[min(width - 1, x + 1), y] / 255.0
            down = hp[x, max(0, y - 1)] / 255.0
            up = hp[x, min(height_px - 1, y + 1)] / 255.0
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


def make_surface_normal(spec):
    rng = random.Random(spec["seed"] + 15000)
    style = spec["style"]
    if style == "face":
        return Image.new("RGB", (SIZE, SIZE), (128, 128, 255))
    rigid_plate = style in {"rigid_plate", "brushed_plate"}
    wave_strength = (
        0.45
        if rigid_plate
        else 0.7
        if style in {"tank", "mechanical_joint"}
        else 2.0
        if style == "cloth"
        else 1.5
    )
    noise_strength = (
        0.8
        if rigid_plate
        else 1.1
        if style in {"tank", "mechanical_joint"}
        else 3.0
        if style == "cloth"
        else 2.2
    )
    height = Image.new("L", (SIZE, SIZE), 128)
    pixels = height.load()
    for y in range(SIZE):
        for x in range(SIZE):
            pixels[x, y] = clamp(
                128
                + math.sin(x * 0.083) * wave_strength
                + math.sin(y * 0.061) * wave_strength
                + rng.uniform(-noise_strength, noise_strength)
            )
    draw_surface_relief(
        ImageDraw.Draw(height),
        spec["style"],
        dark=38,
        raised=214,
    )
    normal_strength = (
        0.7
        if rigid_plate
        else 0.8
        if style == "tank"
        else 1.15
        if style == "mechanical_joint"
        else 1.45
        if style == "cloth"
        else 2.2
    )
    return height_to_normal(height, normal_strength)


def make_roughness(spec):
    rng = random.Random(spec["seed"] + 9000)
    base = spec["roughness"]
    style = spec["style"]
    wave_scale = 2.0 if style == "tank" else 1.0
    variation = (
        0.0
        if style == "face"
        else 5.0
        if style in {"tank", "rigid_plate", "brushed_plate"}
        else 7.0
        if style == "mechanical_joint"
        else 12.0
    )
    image = Image.new("L", (SIZE, SIZE), base)
    pixels = image.load()
    for y in range(SIZE):
        for x in range(SIZE):
            wave = (
                0.0
                if style == "face"
                else (
                    math.sin(x * 0.047) * 7
                    + math.sin(y * 0.029) * 5
                )
                / wave_scale
            )
            pixels[x, y] = clamp(base + wave + rng.uniform(-variation, variation))
    return image.filter(ImageFilter.GaussianBlur(radius=0.7))


def make_metallic(spec):
    rng = random.Random(spec["seed"] + 12000)
    base = spec["metallic"]
    image = Image.new("L", (SIZE, SIZE), base)
    pixels = image.load()
    for y in range(SIZE):
        for x in range(SIZE):
            variation = 2 if spec["style"] == "face" else 7
            variation = 0 if spec["style"] == "face" else 7
            pixels[x, y] = clamp(base + rng.uniform(-variation, variation))
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


def make_mechanical_front_maps():
    width = 1024
    overlay = Image.new("RGBA", (width, width), (0, 0, 0, 0))
    height = Image.new("L", (width, width), 128)
    color = ImageDraw.Draw(overlay, "RGBA")
    relief = ImageDraw.Draw(height)

    steel = (132, 150, 158, 242)
    light = (186, 198, 199, 230)
    red_brown = (82, 39, 32, 244)
    dark = (13, 22, 29, 248)
    groove = (4, 9, 13, 255)
    edge = (213, 224, 222, 205)
    orange = (239, 82, 17, 248)
    warm_red_light = (201, 68, 45, 250)

    def panel(points, fill, outline=edge, seam_width=8):
        color.polygon(points, fill=fill)
        color.line(points + [points[0]], fill=groove, width=seam_width, joint="curve")
        color.line(points[1:3], fill=outline, width=max(2, seam_width // 3))
        relief.polygon(points, fill=188)
        relief.line(points + [points[0]], fill=28, width=seam_width + 7, joint="curve")
        relief.line(points[1:3], fill=226, width=max(4, seam_width // 2))

    def rivet(x, y, radius=7):
        color.ellipse(
            (x - radius, y - radius, x + radius, y + radius),
            fill=(10, 16, 20, 245),
            outline=(210, 219, 218, 230),
            width=3,
        )
        relief.ellipse(
            (x - radius - 2, y - radius - 2, x + radius + 2, y + radius + 2),
            fill=218,
        )
        relief.ellipse(
            (x - radius // 2, y - radius // 2, x + radius // 2, y + radius // 2),
            fill=86,
        )

    def vent_box(box, count=4, horizontal=True):
        color.rounded_rectangle(box, radius=9, fill=dark, outline=edge, width=4)
        relief.rounded_rectangle(box, radius=9, fill=62, outline=210, width=5)
        x0, y0, x1, y1 = box
        if horizontal:
            gap = (y1 - y0) / (count + 1)
            for index in range(1, count + 1):
                y = int(y0 + gap * index)
                color.line((x0 + 9, y, x1 - 9, y), fill=(2, 5, 7, 255), width=4)
                relief.line((x0 + 9, y, x1 - 9, y), fill=18, width=5)
        else:
            gap = (x1 - x0) / (count + 1)
            for index in range(1, count + 1):
                x = int(x0 + gap * index)
                color.line((x, y0 + 8, x, y1 - 8), fill=(2, 5, 7, 255), width=4)
                relief.line((x, y0 + 8, x, y1 - 8), fill=18, width=5)

    center = 624

    # Face plate beneath the retained dark red-brown hood.
    panel([(574, 140), (674, 140), (684, 207), (654, 265), (594, 265), (564, 207)], dark)
    panel([(582, 166), (617, 175), (613, 199), (575, 191)], warm_red_light)
    panel([(631, 175), (666, 166), (673, 191), (635, 199)], warm_red_light)
    color.line((624, 199, 624, 251), fill=edge, width=5)
    relief.line((624, 199, 624, 251), fill=220, width=6)
    vent_box((601, 220, 647, 253), count=4, horizontal=False)

    # Layered shoulder shells and the central chest breastplate.
    panel([(505, 270), (559, 250), (585, 282), (570, 346), (506, 355), (480, 316)], red_brown)
    panel([(689, 282), (715, 250), (769, 270), (794, 316), (768, 355), (704, 346)], red_brown)
    panel([(548, 275), (700, 275), (726, 339), (688, 432), (560, 432), (522, 339)], steel, seam_width=11)
    panel([(570, 300), (678, 300), (697, 345), (673, 391), (575, 391), (551, 345)], light)
    panel([(600, 405), (648, 405), (661, 444), (646, 467), (602, 467), (587, 444)], red_brown)
    vent_box((605, 318, 643, 372), count=3, horizontal=True)
    for point in ((544, 303), (704, 303), (562, 406), (686, 406)):
        rivet(*point)

    # Segmented abdomen shells create the reference's stacked machine core.
    for index, (top, half_width) in enumerate(((454, 70), (493, 62), (531, 54), (568, 45))):
        panel(
            [
                (center - half_width, top),
                (center + half_width, top),
                (center + half_width - 8, top + 32),
                (center, top + 44),
                (center - half_width + 8, top + 32),
            ],
            red_brown if index % 2 == 0 else dark,
            seam_width=7,
        )
    color.rectangle((610, 476, 638, 533), fill=orange)
    relief.rectangle((610, 476, 638, 533), fill=206)
    for y in range(484, 530, 10):
        color.line((614, y, 634, y), fill=(255, 194, 74, 255), width=3)
        relief.line((614, y, 634, y), fill=236, width=3)

    # Arm armor bands and elbow mechanics.
    for mirror in (-1, 1):
        shoulder_x = center + mirror * 128
        outer_x = center + mirror * 205
        panel(
            [
                (shoulder_x, 338),
                (outer_x, 366),
                (outer_x + mirror * 8, 420),
                (shoulder_x + mirror * 18, 408),
            ],
            red_brown,
            seam_width=8,
        )
        panel(
            [
                (shoulder_x + mirror * 18, 423),
                (outer_x + mirror * 13, 439),
                (outer_x + mirror * 17, 510),
                (shoulder_x + mirror * 30, 500),
            ],
            steel,
            seam_width=8,
        )
        rivet(outer_x, 428, 9)
        vent_box(
            (
                min(shoulder_x + mirror * 31, outer_x + mirror * 18) - 17,
                456,
                max(shoulder_x + mirror * 31, outer_x + mirror * 18) + 17,
                483,
            ),
            count=3,
            horizontal=False,
        )

    # Thigh, knee, shin and foot plates.
    for mirror in (-1, 1):
        inner = center + mirror * 30
        outer = center + mirror * 104
        panel(
            [
                (inner, 577),
                (outer, 588),
                (outer + mirror * 6, 694),
                (inner + mirror * 16, 720),
            ],
            steel,
            seam_width=9,
        )
        panel(
            [
                (inner + mirror * 18, 710),
                (outer + mirror * 8, 702),
                (outer + mirror * 11, 770),
                (inner + mirror * 21, 779),
            ],
            red_brown,
            seam_width=9,
        )
        rivet(outer + mirror * 2, 737, 10)
        panel(
            [
                (inner + mirror * 22, 782),
                (outer + mirror * 8, 781),
                (outer + mirror * 1, 916),
                (inner + mirror * 14, 934),
            ],
            dark,
            seam_width=9,
        )
        vent_box(
            (
                min(inner + mirror * 35, outer + mirror * 2) - 12,
                825,
                max(inner + mirror * 35, outer + mirror * 2) + 12,
                890,
            ),
            count=4,
            horizontal=True,
        )
        panel(
            [
                (inner + mirror * 12, 925),
                (outer, 917),
                (outer + mirror * 18, 966),
                (inner - mirror * 3, 978),
            ],
            steel,
            seam_width=8,
        )

    # Small orange service markers and warm red status lights.
    for x, y in ((530, 390), (718, 390), (550, 684), (698, 684), (559, 906), (689, 906)):
        color.rounded_rectangle((x - 14, y - 5, x + 14, y + 5), radius=3, fill=orange)
        relief.rounded_rectangle((x - 14, y - 5, x + 14, y + 5), radius=3, fill=205)
    for x, y in ((515, 324), (733, 324), (541, 754), (707, 754)):
        color.rounded_rectangle((x - 6, y - 10, x + 6, y + 10), radius=3, fill=warm_red_light)
        relief.rounded_rectangle((x - 6, y - 10, x + 6, y + 10), radius=3, fill=224)

    overlay = overlay.filter(ImageFilter.GaussianBlur(radius=0.35))
    normal = height_to_normal(height, 8.5)
    return overlay, height.filter(ImageFilter.GaussianBlur(radius=0.45)), normal


def make_torso_rigid_overlay_maps():
    width = 1024
    overlay = Image.new("RGBA", (width, width), (0, 0, 0, 0))
    emission = Image.new("RGB", (width, width), (0, 0, 0))
    metallic = Image.new("L", (width, width), 184)
    roughness = Image.new("L", (width, width), 138)
    color = ImageDraw.Draw(overlay, "RGBA")
    glow = ImageDraw.Draw(emission)
    metal = ImageDraw.Draw(metallic)
    rough = ImageDraw.Draw(roughness)

    # The source mesh is offset inside its object bounds. Component 1 and the
    # lower torso components share a local center near x=19, which maps to
    # Generated-X ~= 0.648, or 664 px in this 1024 px projection texture.
    center = 664
    seam = (17, 25, 30, 252)
    dark = (24, 34, 40, 252)
    dark_red_brown = (63, 30, 27, 252)
    red_brown = (91, 44, 36, 252)
    steel = (100, 112, 118, 252)
    light_steel = (136, 145, 147, 252)
    orange = (211, 78, 29, 250)
    warm_red_light = (190, 61, 40, 250)

    def panel(points, fill, metallic_value, roughness_value):
        color.polygon(points, fill=fill)
        metal.polygon(points, fill=metallic_value)
        rough.polygon(points, fill=roughness_value)

    # The collar and the jagged upper edge remain one restrained red-brown
    # shell. A single dark underlay suppresses the irregular upper facets
    # without introducing separate projected collar fragments.
    panel(
        [
            (center - 72, 205),
            (center + 72, 205),
            (center + 96, 244),
            (center + 78, 262),
            (center - 78, 262),
            (center - 96, 244),
        ],
        dark,
        164,
        164,
    )

    # A compact dark housing locks the two chest plates to the torso instead of
    # laying one oversized bright sticker across the full body.
    panel(
        [
            (center - 102, 273),
            (center - 73, 254),
            (center + 73, 254),
            (center + 102, 273),
            (center + 97, 341),
            (center + 76, 356),
            (center - 76, 356),
            (center - 97, 341),
        ],
        seam,
        150,
        168,
    )
    panel(
        [
            (center - 96, 276),
            (center - 69, 260),
            (center - 8, 260),
            (center - 8, 350),
            (center - 72, 350),
            (center - 91, 337),
        ],
        light_steel,
        220,
        112,
    )
    panel(
        [
            (center + 8, 260),
            (center + 69, 260),
            (center + 96, 276),
            (center + 91, 337),
            (center + 72, 350),
            (center + 8, 350),
        ],
        red_brown,
        198,
        126,
    )
    color.rectangle((center - 4, 262, center + 4, 350), fill=seam)
    metal.rectangle((center - 4, 262, center + 4, 350), fill=148)
    rough.rectangle((center - 4, 262, center + 4, 350), fill=176)

    # Keep a small centered status core in the approved warm red palette.
    color.rounded_rectangle(
        (center - 18, 286, center + 18, 326),
        radius=5,
        fill=seam,
    )
    color.rounded_rectangle(
        (center - 11, 293, center + 11, 319),
        radius=3,
        fill=warm_red_light,
    )
    metal.rounded_rectangle(
        (center - 18, 286, center + 18, 326),
        radius=5,
        fill=132,
    )
    rough.rounded_rectangle(
        (center - 18, 286, center + 18, 326),
        radius=5,
        fill=92,
    )
    glow.rounded_rectangle(
        (center - 11, 293, center + 11, 319),
        radius=3,
        fill=(151, 34, 20),
    )

    # Two restrained amber service indicators sit at the lower chest edge.
    for x0, x1 in (
        (center - 91, center - 57),
        (center + 57, center + 91),
    ):
        color.rounded_rectangle((x0, 340, x1, 351), radius=3, fill=orange)
        metal.rounded_rectangle((x0, 340, x1, 351), radius=3, fill=156)
        rough.rounded_rectangle((x0, 340, x1, 351), radius=3, fill=108)
        glow.rounded_rectangle((x0, 340, x1, 351), radius=3, fill=(178, 48, 8))

    # Four compact horizontal abdomen modules now occupy the lower half of the
    # existing torso shell instead of being projected below it.
    abdomen_bands = (
        (362, 380, 80, 76, dark_red_brown, 174, 150),
        (383, 401, 76, 70, dark, 164, 162),
        (404, 422, 70, 64, dark_red_brown, 176, 148),
        (425, 443, 64, 56, dark, 164, 164),
    )
    for (
        top,
        bottom,
        top_half,
        bottom_half,
        fill,
        metallic_value,
        roughness_value,
    ) in abdomen_bands:
        points = [
            (center - top_half, top),
            (center + top_half, top),
            (center + bottom_half, bottom),
            (center - bottom_half, bottom),
        ]
        panel(
            points,
            fill,
            metallic_value,
            roughness_value,
        )
        color.line(
            (
                center - top_half + 7,
                top + 3,
                center + top_half - 7,
                top + 3,
            ),
            fill=(115, 132, 140, 150),
            width=2,
        )

    # A short horizontal heat/status insert sits inside the upper abdomen.
    color.rounded_rectangle(
        (center - 20, 371, center + 20, 394),
        radius=4,
        fill=seam,
    )
    color.rounded_rectangle(
        (center - 13, 376, center + 13, 389),
        radius=3,
        fill=orange,
    )
    metal.rounded_rectangle(
        (center - 20, 371, center + 20, 394),
        radius=4,
        fill=148,
    )
    rough.rounded_rectangle(
        (center - 20, 371, center + 20, 394),
        radius=4,
        fill=104,
    )
    glow.rounded_rectangle(
        (center - 11, 378, center + 11, 387),
        radius=2,
        fill=(178, 47, 5),
    )
    color.line(
        (center - 9, 381, center + 9, 381),
        fill=(255, 187, 90, 255),
        width=2,
    )
    color.line(
        (center - 9, 386, center + 9, 386),
        fill=(255, 187, 90, 255),
        width=2,
    )

    # The pelvis is a short, centered housing directly under the module stack.
    panel(
        [
            (center - 54, 450),
            (center + 54, 450),
            (center + 44, 486),
            (center, 500),
            (center - 44, 486),
        ],
        dark,
        166,
        166,
    )
    panel(
        [
            (center - 18, 455),
            (center + 18, 455),
            (center + 20, 482),
            (center, 490),
            (center - 20, 482),
        ],
        steel,
        205,
        122,
    )

    return (
        overlay,
        emission.filter(ImageFilter.GaussianBlur(radius=0.35)),
        metallic,
        roughness,
    )


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
                outline=(255, 171, 139),
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
    # Use the observed emblem pixels from the supplied reference instead of
    # inventing a substitute icon. A feathered oval keeps the surrounding
    # reference cloth from becoming a rectangular patch.
    reference = Image.open(REFERENCE).convert("RGB")
    flame_crop = reference.crop(REFERENCE_CROPS["reference_flame_mark_direct_crop.png"])
    flame_crop = flame_crop.resize((280, 315), Image.Resampling.LANCZOS)
    alpha = Image.new("L", flame_crop.size, 0)
    alpha_pixels = alpha.load()
    flame_pixels = flame_crop.load()
    width, height = flame_crop.size
    for y in range(height):
        for x in range(width):
            red, green, blue = flame_pixels[x, y]
            orange_flame = (
                red > 105
                and red > blue * 1.35
                and red > green * 1.03
            )
            dx = (x - width * 0.50) / width
            dy = (y - height * 0.64) / height
            circular_core = (dx * dx + dy * dy) <= 0.012
            alpha_pixels[x, y] = 255 if orange_flame or circular_core else 0
    alpha = alpha.filter(ImageFilter.GaussianBlur(radius=1.6))
    flame_decal = flame_crop.convert("RGBA")
    flame_decal.putalpha(alpha)
    image.alpha_composite(flame_decal, (266, 164))
    return image


def make_face_projection_maps():
    size = 1024
    overlay = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    height = Image.new("L", (size, size), 128)
    emission = Image.new("RGB", (size, size), (0, 0, 0))
    color = ImageDraw.Draw(overlay, "RGBA")
    glow = ImageDraw.Draw(emission)

    socket = (24, 9, 8, 255)
    optic = (135, 45, 33, 255)
    optic_hot = (255, 164, 132, 255)

    # This texture contains one optic only. The face shader places two
    # independent copies in tangent frames fitted to the existing left and
    # right upper-face surfaces. No face-wide rectangular projection remains.
    socket_points = [
        (72, 354),
        (850, 304),
        (952, 394),
        (896, 674),
        (158, 724),
        (62, 620),
    ]
    eye_points = [
        (142, 394),
        (818, 350),
        (878, 412),
        (832, 624),
        (210, 668),
        (134, 594),
    ]
    inner_glow = [
        (206, 430),
        (776, 392),
        (818, 432),
        (786, 582),
        (252, 620),
        (198, 566),
    ]
    color.polygon(socket_points, fill=socket)
    color.polygon(eye_points, fill=optic)
    color.polygon(inner_glow, fill=(192, 62, 43, 255))
    color.line(inner_glow[:2], fill=optic_hot, width=8)
    glow.polygon(eye_points, fill=(83, 17, 11))
    glow.polygon(inner_glow, fill=(157, 39, 24))
    glow.line(inner_glow[:2], fill=(255, 129, 94), width=7)

    return (
        overlay.filter(ImageFilter.GaussianBlur(radius=0.28)),
        height,
        emission.filter(ImageFilter.GaussianBlur(radius=1.0)),
    )


def make_palette_sheet(generated):
    width = 1400
    height = 1190
    sheet = Image.new("RGB", (width, height), (20, 27, 31))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    title_font = font
    draw.text((34, 28), "PAHUR MATERIAL / TEXTURE BREAKDOWN", fill=(236, 241, 242), font=title_font)
    draw.text(
        (34, 54),
        "Current FBX mesh preserved — approved red-brown palette and procedural wear",
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
    if "--red-brown-palette-only" in sys.argv:
        # This mode deliberately avoids opening the concept reference. It
        # rewrites only the approved blue-family color assets while preserving
        # their existing roughness, metallic, normal, and height maps.
        red_brown_materials = (
            "armor_bluegray",
            "leg_steel",
            "torso_rigid_shell",
            "torso_pelvis_plate",
            "shoulder_machine_blue",
            "left_arm_machine",
            "hood_navy_cloth",
        )
        for name in red_brown_materials:
            make_surface_texture(name, PALETTE[name]).save(
                TEXTURE_DIR / f"pahur_{name}_albedo.png"
            )

        optic = make_emission("optic_blue", (205, 68, 43), (25, 7, 5))
        optic.save(TEXTURE_DIR / "pahur_optic_blue_emission.png")

        mechanical_overlay, _, _ = make_mechanical_front_maps()
        mechanical_overlay.save(
            TEXTURE_DIR / "pahur_mechanical_front_overlay.png"
        )

        torso_overlay, torso_emission, _, _ = make_torso_rigid_overlay_maps()
        torso_overlay.save(TEXTURE_DIR / "pahur_torso_rigid_overlay.png")
        torso_emission.save(TEXTURE_DIR / "pahur_torso_rigid_emission.png")

        face_overlay, _, face_emission = make_face_projection_maps()
        face_overlay.save(TEXTURE_DIR / "pahur_face_reference_overlay.png")
        face_emission.save(TEXTURE_DIR / "pahur_face_reference_emission.png")

        current_albedos = {
            name: Image.open(
                TEXTURE_DIR / f"pahur_{name}_albedo.png"
            ).convert("RGB")
            for name in PALETTE
        }
        current_albedos["optic_blue_emission"] = optic
        current_albedos["flame_orange_emission"] = Image.open(
            TEXTURE_DIR / "pahur_flame_orange_emission.png"
        ).convert("RGB")
        make_palette_sheet(current_albedos).save(
            SAMPLE_ROOT / "renders/06_texture_atlas_and_material_breakdown.png"
        )
        print("Created approved red-brown palette assets without opening the reference image.")
        return

    reference = Image.open(REFERENCE).convert("RGB")
    if "--head-only" in sys.argv:
        for filename in (
            "reference_head_detail_crop.png",
            "reference_hood_navy_direct_crop.png",
            "reference_blue_optic_direct_crop.png",
            "reference_flame_mark_direct_crop.png",
        ):
            crop = reference.crop(REFERENCE_CROPS[filename])
            ImageEnhance.Contrast(crop).enhance(1.04).save(TEXTURE_DIR / filename)

        for name in ("hood_navy_cloth", "face_metal"):
            spec = PALETTE[name]
            make_surface_texture(name, spec).save(
                TEXTURE_DIR / f"pahur_{name}_albedo.png"
            )
            make_roughness(spec).save(
                TEXTURE_DIR / f"pahur_{name}_roughness.png"
            )
            make_metallic(spec).save(
                TEXTURE_DIR / f"pahur_{name}_metallic.png"
            )
            make_surface_normal(spec).save(
                TEXTURE_DIR / f"pahur_{name}_normal.png"
            )

        make_head_projection_decal().save(
            TEXTURE_DIR / "pahur_head_reference_projection_decal.png"
        )
        face_overlay, face_height, face_emission = make_face_projection_maps()
        face_overlay.save(TEXTURE_DIR / "pahur_face_reference_overlay.png")
        face_height.save(TEXTURE_DIR / "pahur_face_reference_height.png")
        face_emission.save(TEXTURE_DIR / "pahur_face_reference_emission.png")
        current_albedos = {
            name: Image.open(
                TEXTURE_DIR / f"pahur_{name}_albedo.png"
            ).convert("RGB")
            for name in PALETTE
        }
        current_albedos["optic_blue_emission"] = Image.open(
            TEXTURE_DIR / "pahur_optic_blue_emission.png"
        ).convert("RGB")
        current_albedos["flame_orange_emission"] = Image.open(
            TEXTURE_DIR / "pahur_flame_orange_emission.png"
        ).convert("RGB")
        make_palette_sheet(current_albedos).save(
            SAMPLE_ROOT / "renders/06_texture_atlas_and_material_breakdown.png"
        )
        print("Created head-only cloth and face texture set.")
        return
    if "--shoulder-left-arm-only" in sys.argv:
        for name in (
            "shoulder_machine_blue",
            "left_arm_machine",
            "left_hand_machine",
        ):
            spec = PALETTE[name]
            make_surface_texture(name, spec).save(
                TEXTURE_DIR / f"pahur_{name}_albedo.png"
            )
            make_roughness(spec).save(
                TEXTURE_DIR / f"pahur_{name}_roughness.png"
            )
            make_metallic(spec).save(
                TEXTURE_DIR / f"pahur_{name}_metallic.png"
            )
            make_surface_normal(spec).save(
                TEXTURE_DIR / f"pahur_{name}_normal.png"
            )
        print("Created shoulder and left-arm mechanical texture set.")
        return

    for filename, box in REFERENCE_CROPS.items():
        crop = reference.crop(box)
        crop = ImageEnhance.Contrast(crop).enhance(1.04)
        crop.save(TEXTURE_DIR / filename)

    generated = {}
    for name, spec in PALETTE.items():
        albedo = make_surface_texture(name, spec)
        albedo_path = TEXTURE_DIR / f"pahur_{name}_albedo.png"
        albedo.save(albedo_path)
        make_roughness(spec).save(TEXTURE_DIR / f"pahur_{name}_roughness.png")
        make_metallic(spec).save(TEXTURE_DIR / f"pahur_{name}_metallic.png")
        make_surface_normal(spec).save(TEXTURE_DIR / f"pahur_{name}_normal.png")
        generated[name] = albedo

    normal = make_micro_normal()
    normal.save(TEXTURE_DIR / "pahur_shared_micro_normal.png")
    mechanical_overlay, mechanical_height, mechanical_normal = make_mechanical_front_maps()
    mechanical_overlay.save(TEXTURE_DIR / "pahur_mechanical_front_overlay.png")
    mechanical_height.save(TEXTURE_DIR / "pahur_mechanical_front_height.png")
    mechanical_normal.save(TEXTURE_DIR / "pahur_mechanical_front_normal.png")
    optic = make_emission("optic_blue", (40, 184, 255), (3, 18, 31))
    optic.save(TEXTURE_DIR / "pahur_optic_blue_emission.png")
    flame = make_emission("flame_orange", (255, 111, 12), (43, 10, 2))
    flame.save(TEXTURE_DIR / "pahur_flame_orange_emission.png")
    decal = make_head_projection_decal()
    decal.save(TEXTURE_DIR / "pahur_head_reference_projection_decal.png")
    face_overlay, face_height, face_emission = make_face_projection_maps()
    face_overlay.save(TEXTURE_DIR / "pahur_face_reference_overlay.png")
    face_height.save(TEXTURE_DIR / "pahur_face_reference_height.png")
    face_emission.save(TEXTURE_DIR / "pahur_face_reference_emission.png")
    generated["optic_blue_emission"] = optic
    generated["flame_orange_emission"] = flame
    make_palette_sheet(generated).save(
        SAMPLE_ROOT / "renders/06_texture_atlas_and_material_breakdown.png"
    )
    print(f"Created {len(list(TEXTURE_DIR.glob('*.png')))} texture files.")


if __name__ == "__main__":
    main()
