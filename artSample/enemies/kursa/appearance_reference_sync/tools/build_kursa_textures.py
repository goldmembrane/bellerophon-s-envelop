from pathlib import Path
import hashlib
import json
import math
import random

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
REFERENCE = ROOT / "image/KUŠkursa(쿠르사).png"
OUTPUT = SAMPLE_ROOT / "TEXTURE_ANALYSIS.md"

SIZE = 512
MATERIALS = {
    "armor_gunmetal": {
        "base": (61, 65, 65),
        "metallic": 0.74,
        "roughness": 0.59,
        "style": "worn_metal",
        "source": "몸통·팔다리 중성 건메탈의 #505454 계열",
    },
    "armor_bluegray": {
        "base": (31, 42, 53),
        "metallic": 0.66,
        "roughness": 0.57,
        "style": "painted_metal",
        "source": "국소 남색 장갑의 #27333E 계열",
    },
    "light_steel": {
        "base": (104, 108, 106),
        "metallic": 0.78,
        "roughness": 0.52,
        "style": "brushed_metal",
        "source": "밝은 흉부·테두리 강철의 #848785 계열",
    },
    "dark_mechanics": {
        "base": (12, 15, 16),
        "metallic": 0.56,
        "roughness": 0.72,
        "style": "dark_mechanics",
        "source": "관절·케이블의 #141715 계열",
    },
    "torso_mechanical": {
        "base": (46, 50, 51),
        "metallic": 0.72,
        "roughness": 0.60,
        "style": "torso_mechanical",
        "source": "복층 흉갑의 중암도 판금과 기계 이음부 계열",
    },
    "hood_cloth": {
        "base": (31, 40, 49),
        "metallic": 0.02,
        "roughness": 0.84,
        "style": "cloth",
        "source": "두건의 #1F2831 청흑색 계열",
    },
    "face_metal": {
        "base": (84, 88, 88),
        "metallic": 0.82,
        "roughness": 0.38,
        "style": "face_metal",
        "source": "안면의 #606565 중암도 금속 계열",
    },
    "shield_worn": {
        "base": (45, 47, 45),
        "metallic": 0.52,
        "roughness": 0.80,
        "style": "shield_wear",
        "source": "방패 주조색의 #343633 계열",
    },
    "shield_frame": {
        "base": (92, 96, 94),
        "metallic": 0.80,
        "roughness": 0.54,
        "style": "brushed_metal",
        "source": "방패 가장자리의 #717472 계열",
    },
}


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def clamp(value):
    return max(0, min(255, round(value)))


def noise_field(seed, scale=64):
    rng = random.Random(seed)
    image = Image.new("L", (scale, scale))
    image.putdata([rng.randrange(48, 208) for _ in range(scale * scale)])
    return image.resize((SIZE, SIZE), Image.Resampling.BICUBIC).filter(
        ImageFilter.GaussianBlur(2.2)
    )


def normal_from_height(height):
    source = height.load()
    normal = Image.new("RGB", height.size)
    target = normal.load()
    for y in range(SIZE):
        up = max(0, y - 1)
        down = min(SIZE - 1, y + 1)
        for x in range(SIZE):
            left = max(0, x - 1)
            right = min(SIZE - 1, x + 1)
            dx = (source[right, y] - source[left, y]) / 255.0
            dy = (source[x, down] - source[x, up]) / 255.0
            target[x, y] = (
                clamp(128 - dx * 68),
                clamp(128 + dy * 68),
                244,
            )
    return normal


def add_scratches(draw, rng, count, color, width_range=(1, 2), long=False):
    for _ in range(count):
        x = rng.randrange(SIZE)
        y = rng.randrange(SIZE)
        length = rng.randrange(18, 110 if long else 54)
        slope = rng.uniform(-0.34, 0.34)
        draw.line(
            (x, y, x + length, y + length * slope),
            fill=color,
            width=rng.randint(*width_range),
        )


def add_panel_detail(
    albedo, height, roughness, metallic, rng, base, intensity=1.0, panel_step=128
):
    albedo_draw = ImageDraw.Draw(albedo, "RGB")
    height_draw = ImageDraw.Draw(height)
    roughness_draw = ImageDraw.Draw(roughness)
    metallic_draw = ImageDraw.Draw(metallic)
    for y in range(0, SIZE, panel_step):
        for x in range(0, SIZE, panel_step):
            inset = rng.randint(16, 28)
            notch = rng.randint(18, 34)
            points = [
                (x + inset, y + 6),
                (x + panel_step - inset, y + 6),
                (x + panel_step - 7, y + notch),
                (x + panel_step - 7, y + panel_step - notch),
                (x + panel_step - inset, y + panel_step - 7),
                (x + inset, y + panel_step - 7),
                (x + 7, y + panel_step - notch),
                (x + 7, y + notch),
            ]
            seam = tuple(clamp(channel - 30 * intensity) for channel in base)
            edge = tuple(clamp(channel + 21 * intensity) for channel in base)
            albedo_draw.line(points + [points[0]], fill=seam, width=4, joint="curve")
            albedo_draw.line(points + [points[0]], fill=edge, width=1, joint="curve")
            height_draw.line(points + [points[0]], fill=91, width=4, joint="curve")
            height_draw.line(points + [points[0]], fill=148, width=1, joint="curve")
            roughness_draw.line(points + [points[0]], fill=205, width=4, joint="curve")
            for rx, ry in ((x + inset + 7, y + 18), (x + panel_step - inset - 7, y + 18)):
                radius = 3
                albedo_draw.ellipse((rx-radius, ry-radius, rx+radius, ry+radius), fill=seam)
                albedo_draw.ellipse((rx-1, ry-1, rx+1, ry+1), fill=edge)
                height_draw.ellipse((rx-radius, ry-radius, rx+radius, ry+radius), fill=101)
                height_draw.ellipse((rx-1, ry-1, rx+1, ry+1), fill=158)
                roughness_draw.ellipse((rx-radius, ry-radius, rx+radius, ry+radius), fill=184)
                metallic_draw.ellipse((rx-radius, ry-radius, rx+radius, ry+radius), fill=224)


def add_chips(albedo, height, roughness, metallic, rng, base, count, large=False):
    albedo_draw = ImageDraw.Draw(albedo, "RGB")
    height_draw = ImageDraw.Draw(height)
    roughness_draw = ImageDraw.Draw(roughness)
    metallic_draw = ImageDraw.Draw(metallic)
    for _ in range(count):
        x = rng.randrange(SIZE)
        y = rng.randrange(SIZE)
        radius = rng.randrange(2, 9 if large else 5)
        points = []
        for index in range(rng.randint(5, 8)):
            angle = math.tau * index / 7 + rng.uniform(-0.25, 0.25)
            distance = radius * rng.uniform(0.55, 1.25)
            points.append((x + math.cos(angle) * distance, y + math.sin(angle) * distance))
        dark = tuple(clamp(channel - rng.randint(18, 32)) for channel in base)
        exposed = tuple(clamp(channel + rng.randint(24, 44)) for channel in base)
        albedo_draw.polygon(points, fill=dark)
        height_draw.polygon(points, fill=rng.randint(88, 108))
        roughness_draw.polygon(points, fill=rng.randint(184, 224))
        metallic_draw.polygon(points, fill=rng.randint(215, 244))
        if rng.random() < 0.68:
            albedo_draw.line(points[:3], fill=exposed, width=1)
            height_draw.line(points[:3], fill=158, width=1)


def build_set(material_id, spec):
    seed = int(hashlib.sha256(material_id.encode("utf-8")).hexdigest()[:8], 16)
    rng = random.Random(seed)
    grain = noise_field(seed, 96)
    stain = noise_field(seed ^ 0xA5A5A5A5, 18).filter(ImageFilter.GaussianBlur(8.0))
    grain_pixels = grain.load()
    stain_pixels = stain.load()
    base = spec["base"]
    style = spec["style"]

    albedo = Image.new("RGB", (SIZE, SIZE))
    albedo_pixels = albedo.load()
    variation = 13 if style in {"worn_metal", "painted_metal", "brushed_metal", "torso_mechanical"} else 9
    if style == "shield_wear":
        variation = 18
    elif style == "dark_mechanics":
        variation = 8
    for y in range(SIZE):
        for x in range(SIZE):
            fine = (grain_pixels[x, y] - 128) / 128.0 * variation
            broad = (stain_pixels[x, y] - 128) / 128.0 * variation * 0.9
            amount = fine + broad
            albedo_pixels[x, y] = tuple(clamp(channel + amount) for channel in base)
    albedo_draw = ImageDraw.Draw(albedo, "RGB")

    height = Image.new("L", (SIZE, SIZE), 128)
    height_pixels = height.load()
    for y in range(SIZE):
        for x in range(SIZE):
            micro = (grain_pixels[x, y] - 128) * (0.22 if style != "cloth" else 0.08)
            height_pixels[x, y] = clamp(128 + micro)
    height_draw = ImageDraw.Draw(height)
    roughness = Image.new(
        "L", (SIZE, SIZE), clamp(spec["roughness"] * 255)
    )
    roughness_draw = ImageDraw.Draw(roughness)
    metallic = Image.new(
        "L", (SIZE, SIZE), clamp(spec["metallic"] * 255)
    )
    metallic_draw = ImageDraw.Draw(metallic)

    if style in {"worn_metal", "painted_metal", "brushed_metal", "torso_mechanical", "shield_wear"}:
        panel_strength = 0.94 if style == "torso_mechanical" else 0.78 if style == "painted_metal" else 0.56
        if style != "shield_wear":
            add_panel_detail(
                albedo,
                height,
                roughness,
                metallic,
                rng,
                base,
                panel_strength,
                96 if style == "torso_mechanical" else 128,
            )
        scratch_count = 82 if style == "shield_wear" else 78
        scratch_color = tuple(
            clamp(channel + (34 if style == "shield_wear" else 30))
            for channel in base
        )
        add_scratches(
            albedo_draw,
            rng,
            scratch_count,
            scratch_color,
            (1, 3 if style == "shield_wear" else 2),
            style == "shield_wear",
        )
        add_scratches(
            height_draw,
            rng,
            scratch_count,
            156 if style == "shield_wear" else 154,
            (1, 2),
            style == "shield_wear",
        )
        add_scratches(
            roughness_draw,
            rng,
            scratch_count,
            clamp((spec["roughness"] + 0.12) * 255),
            (1, 2),
            style == "shield_wear",
        )
        add_chips(
            albedo,
            height,
            roughness,
            metallic,
            rng,
            base,
            36 if style == "shield_wear" else 20,
            False,
        )
    if style == "shield_wear":
        # Broad, dirty wear patches and impact pits match the reference's
        # rough cast shield rather than a polished sheet of metal.
        for _ in range(34):
            x = rng.randrange(SIZE)
            y = rng.randrange(SIZE)
            radius = rng.randrange(4, 18)
            albedo_draw.ellipse(
                (x - radius, y - radius, x + radius, y + radius),
                outline=(24, 27, 26),
                width=rng.randint(1, 3),
            )
            height_draw.ellipse(
                (x - radius, y - radius, x + radius, y + radius),
                outline=rng.randint(88, 112),
                width=rng.randint(1, 3),
            )
            roughness_draw.ellipse(
                (x - radius, y - radius, x + radius, y + radius),
                outline=rng.randint(205, 238),
                width=rng.randint(2, 5),
            )
        stain_mask = Image.new("L", (SIZE, SIZE), 0)
        stain_draw = ImageDraw.Draw(stain_mask)
        for _ in range(22):
            x = rng.randrange(-60, SIZE - 30)
            y = rng.randrange(-50, SIZE - 20)
            width = rng.randrange(55, 165)
            height_size = rng.randrange(30, 105)
            stain_draw.ellipse(
                (x, y, x + width, y + height_size),
                fill=rng.randint(58, 128),
            )
        stain_mask = stain_mask.filter(ImageFilter.GaussianBlur(24.0))
        darkened = albedo.point(lambda value: max(0, value - 24))
        albedo = Image.composite(darkened, albedo, stain_mask)
        rough_stain = Image.new("L", (SIZE, SIZE), 226)
        roughness = Image.composite(rough_stain, roughness, stain_mask)
        low_metal = Image.new("L", (SIZE, SIZE), 142)
        metallic = Image.composite(low_metal, metallic, stain_mask)
    elif style == "cloth":
        for offset in range(0, SIZE, 5):
            height_draw.line((0, offset, SIZE, offset), fill=137, width=1)
            height_draw.line((offset, 0, offset, SIZE), fill=121, width=1)
            roughness_draw.line((0, offset, SIZE, offset), fill=226, width=1)
    elif style == "dark_mechanics":
        for offset in range(0, SIZE, 24):
            height_draw.line((offset, 0, offset, SIZE), fill=92, width=3)
            height_draw.line((offset + 4, 0, offset + 4, SIZE), fill=150, width=1)
            roughness_draw.line((offset, 0, offset, SIZE), fill=214, width=3)
        for _ in range(90):
            x = rng.randrange(SIZE)
            y = rng.randrange(SIZE)
            radius = rng.randrange(2, 7)
            albedo_draw.ellipse((x-radius, y-radius, x+radius, y+radius), fill=(4, 6, 7))
            roughness_draw.ellipse((x-radius, y-radius, x+radius, y+radius), fill=rng.randint(130, 180))
            metallic_draw.ellipse((x-radius, y-radius, x+radius, y+radius), fill=190)
    elif style == "face_metal":
        add_panel_detail(albedo, height, roughness, metallic, rng, base, 0.34)
        add_chips(albedo, height, roughness, metallic, rng, base, 18)

    albedo.save(TEXTURE_DIR / f"kursa_{material_id}_albedo.png")
    roughness.save(TEXTURE_DIR / f"kursa_{material_id}_roughness.png")
    metallic.save(TEXTURE_DIR / f"kursa_{material_id}_metallic.png")
    normal_from_height(height).save(
        TEXTURE_DIR / f"kursa_{material_id}_normal.png"
    )


def extract_reference_decal(box, filename, mode):
    reference = Image.open(REFERENCE).convert("RGB")
    crop = reference.crop(box)
    rgba = Image.new("RGBA", crop.size, (0, 0, 0, 0))
    source = crop.load()
    target = rgba.load()
    for y in range(crop.height):
        for x in range(crop.width):
            red, green, blue = source[x, y]
            if mode == "optic":
                chroma = blue - max(red, green)
                alpha = (
                    clamp((chroma - 8) * 12 + max(0, blue - 105) * 0.45)
                    if chroma >= 12 and blue >= 60
                    else 0
                )
            else:
                green_coolness = green - red
                blue_coolness = blue - red
                brightness = (green + blue) * 0.5
                alpha = (
                    clamp(
                        (min(green_coolness, blue_coolness) - 6) * 10
                        + max(0, brightness - 105) * 0.65
                    )
                    if green_coolness >= 9
                    and blue_coolness >= 12
                    and brightness >= 72
                    else 0
                )
            if alpha > 18:
                target[x, y] = (red, green, blue, alpha)
    alpha = rgba.getchannel("A").filter(ImageFilter.GaussianBlur(0.7))
    rgba.putalpha(alpha)
    rgba.resize((512, 512), Image.Resampling.LANCZOS).save(
        TEXTURE_DIR / filename
    )


def extract_reference_surface(box, filename, size):
    crop = Image.open(REFERENCE).convert("RGB").crop(box)
    rgba = Image.new("RGBA", crop.size, (0, 0, 0, 0))
    source = crop.load()
    target = rgba.load()
    for y in range(crop.height):
        for x in range(crop.width):
            red, green, blue = source[x, y]
            background_distance = 255 - min(red, green, blue)
            alpha = clamp(max(0, background_distance - 5) * 5.2)
            if alpha > 10:
                target[x, y] = (red, green, blue, alpha)
    alpha = rgba.getchannel("A").filter(ImageFilter.GaussianBlur(0.55))
    rgba.putalpha(alpha)
    rgba.resize(size, Image.Resampling.LANCZOS).save(TEXTURE_DIR / filename)


def main():
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)
    for material_id, spec in MATERIALS.items():
        build_set(material_id, spec)

    extract_reference_decal(
        (684, 48, 789, 106),
        "kursa_eye_reference_overlay.png",
        "optic",
    )
    extract_reference_decal(
        (700, 64, 728, 92),
        "kursa_eye_left_reference_overlay.png",
        "optic",
    )
    extract_reference_decal(
        (731, 64, 759, 92),
        "kursa_eye_right_reference_overlay.png",
        "optic",
    )
    extract_reference_decal(
        (681, 10, 789, 78),
        "kursa_hood_reference_decal.png",
        "glyph",
    )
    extract_reference_decal(
        (628, 105, 799, 198),
        "kursa_scarf_reference_decal.png",
        "glyph",
    )
    extract_reference_decal(
        (625, 130, 820, 245),
        "kursa_torso_reference_glyph.png",
        "glyph",
    )
    extract_reference_surface(
        (625, 130, 820, 415),
        "kursa_torso_reference_surface.png",
        (700, 1024),
    )
    extract_reference_surface(
        (846, 126, 980, 568),
        "kursa_shield_reference_surface.png",
        (384, 1024),
    )

    lines = [
        "# 쿠르사 기준 이미지 색·텍스처·머티리얼 분석",
        "",
        "## 기준",
        "",
        "- 기준 이미지: `image/KUŠkursa(쿠르사).png`",
        "- 메시 형상은 현재 Unity 배치 FBX를 그대로 유지합니다.",
        "- 색과 표식은 기준 이미지에서 추출하고, 존재하지 않는 부품이나 패널 형상은 생성하지 않습니다.",
        "",
        "## 재질군",
        "",
    ]
    for material_id, spec in MATERIALS.items():
        lines.append(
            f"- `{material_id}`: {spec['source']}, metallic={spec['metallic']}, roughness={spec['roughness']}"
        )
    lines.extend(
        [
            "",
            "## 표면 정보",
            "",
            "- 몸체 금속은 판금 이음선, 리벳형 점 디테일, 도장 박리, 노출 금속, 미세 요철, 기름때를 알베도·거칠기·금속성·노멀에 분리해 반영합니다.",
            "- 중앙 흉갑은 더 촘촘한 판금 이음선과 리벳형 점 디테일을 가진 별도 기계 장갑 재질로 분리합니다.",
            "- 중앙 흉갑의 청록 표식은 기준 이미지에서 색상 영역만 분리한 투명 텍스처이며 사진형 흉갑 윤곽은 투영하지 않습니다.",
            "- 방패는 어두운 무광 주조 금속 위에 번진 오염, 충격 패임, 길이가 다른 긁힘만 사용하고 기준 이미지 방패 윤곽은 투영하지 않습니다.",
            "- 관절과 손은 홈, 검은 오일 얼룩, 국소 금속 반사를 사용해 외장과 분리합니다.",
            "- 두건은 비금속 천으로 처리하고 미세 직조 높이만 사용합니다.",
            "- 얼굴은 기준 안면의 금속판 느낌을 유지하되 판금선과 박리를 가장 낮은 강도로 제한합니다.",
            "- 눈과 두건·목둘레 표식은 기준 이미지의 해당 영역만 분리한 투명 텍스처입니다.",
            "- 흉갑·방패 직접 분리본은 비교 분석 자료로만 남기고 최종 머티리얼에는 사용하지 않습니다.",
            "",
            "## 원본 무결성",
            "",
            f"- 기준 이미지 SHA-256: `{sha256(REFERENCE)}`",
            "- 이 스크립트는 원본 FBX와 Unity 에셋을 읽거나 수정하지 않습니다.",
        ]
    )
    OUTPUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(
        json.dumps(
            {
                "material_sets": len(MATERIALS),
                "pbr_textures": len(MATERIALS) * 4,
                "reference_decals": 6,
                "reference_sha256": sha256(REFERENCE),
            }
        )
    )


if __name__ == "__main__":
    main()
