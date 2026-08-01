from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
RENDERS = SAMPLE / "renders"
REFERENCE = SAMPLE / "reference"
TEXTURES = SAMPLE / "textures"
USER_SHAPE_SCREENSHOT = Path(r"C:\Users\gus68\OneDrive\바탕 화면\111.png")

BG = (20, 27, 23)
PANEL = (36, 47, 41)
TEXT = (237, 242, 230)
MUTED = (183, 199, 186)
ACCENT = (104, 163, 207)


def font(size, bold=False):
    name = "arialbd.ttf" if bold else "arial.ttf"
    try:
        return ImageFont.truetype(name, size)
    except OSError:
        return ImageFont.load_default()


def fit(image, box, contain=True):
    image = image.convert("RGB")
    scale = min(box[0] / image.width, box[1] / image.height) if contain else max(
        box[0] / image.width, box[1] / image.height
    )
    resized = image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.Resampling.LANCZOS,
    )
    if contain:
        canvas = Image.new("RGB", box, PANEL)
        canvas.paste(resized, ((box[0] - resized.width) // 2, (box[1] - resized.height) // 2))
        return canvas
    left = (resized.width - box[0]) // 2
    top = (resized.height - box[1]) // 2
    return resized.crop((left, top, left + box[0], top + box[1]))


def labeled_pair(title, left_path, left_label, right_path, right_label, output):
    canvas = Image.new("RGB", (1600, 940), BG)
    draw = ImageDraw.Draw(canvas)
    draw.text((48, 34), title, fill=TEXT, font=font(38, True))
    draw.text((48, 84), "Reference image compared with the material-only FBX sample", fill=MUTED, font=font(22))
    for x, path, label in ((48, left_path, left_label), (816, right_path, right_label)):
        canvas.paste(fit(Image.open(path), (736, 736)), (x, 142))
        draw.rectangle((x, 878, x + 736, 926), fill=(28, 37, 32))
        draw.text((x + 18, 888), label, fill=ACCENT, font=font(21, True))
    canvas.save(RENDERS / output)


def texture_board():
    material_ids = [
        "armor_gunmetal", "armor_bluegray", "light_steel", "dark_mechanics",
        "torso_mechanical", "hood_cloth", "face_metal", "shield_worn", "shield_frame",
    ]
    canvas = Image.new("RGB", (1600, 1450), BG)
    draw = ImageDraw.Draw(canvas)
    draw.text((48, 32), "Kursa material and texture breakdown", fill=TEXT, font=font(38, True))
    draw.text((48, 82), "Nine reference-derived material groups; no mesh deformation", fill=MUTED, font=font(22))
    for index, material_id in enumerate(material_ids):
        col, row = index % 4, index // 4
        x, y = 48 + col * 388, 144 + row * 418
        image = Image.open(TEXTURES / f"kursa_{material_id}_albedo.png")
        canvas.paste(fit(image, (348, 330), contain=False), (x, y))
        draw.rectangle((x, y + 330, x + 348, y + 380), fill=(28, 37, 32))
        draw.text((x + 12, y + 344), material_id, fill=ACCENT, font=font(19, True))
    canvas.save(RENDERS / "06_texture_atlas_and_material_breakdown.png")


def eye_multiview_board():
    views = [
        ("Left 60°", "07_head_left_60_detail.png"),
        ("Left 30°", "07_head_left_three_quarter_detail.png"),
        ("Front", "07_head_front_detail.png"),
        ("Right 30°", "07_head_three_quarter_detail.png"),
        ("Right 60°", "07_head_right_60_detail.png"),
    ]
    canvas = Image.new("RGB", (1600, 1160), BG)
    draw = ImageDraw.Draw(canvas)
    draw.text((48, 30), "Kursa eye-surface alignment — five-view check", fill=TEXT, font=font(38, True))
    draw.text((48, 80), "Fixed centers, shared no-shear visual plane, and equal 2.05 depth masks; projection dimensions remain 2x", fill=MUTED, font=font(22))
    tile = (480, 460)
    positions = [(48, 142), (560, 142), (1072, 142), (304, 650), (816, 650)]
    for (label, filename), (x, y) in zip(views, positions):
        canvas.paste(fit(Image.open(RENDERS / filename), tile), (x, y))
        draw.rectangle((x, y + tile[1], x + tile[0], y + tile[1] + 44), fill=(28, 37, 32))
        draw.text((x + 14, y + tile[1] + 9), label, fill=ACCENT, font=font(20, True))
    canvas.save(RENDERS / "09_eye_surface_multiview.png")


def eye_cavity_evidence_board():
    diagnostic = SAMPLE / "diagnostics"
    views = [
        (diagnostic / "user_eye_target_zoom.png", "User-supplied target area"),
        (diagnostic / "user_eye_target_match.png", "Target crop / current matched region"),
        (diagnostic / "render02_head_target_zoom.png", "Exact matched area on the model"),
        (RENDERS / "02_three_quarter_kursa_reference_match.png", "Final three-quarter render"),
    ]
    canvas = Image.new("RGB", (1600, 1160), BG)
    draw = ImageDraw.Draw(canvas)
    draw.text((48, 30), "Kursa user-specified eye area — image match evidence", fill=TEXT, font=font(38, True))
    draw.text((48, 80), "Fixed surface rays -> shared visual plane at about 90-degree screen axes -> unclipped reference-eye shapes", fill=MUTED, font=font(22))
    positions = [(48, 142), (816, 142), (48, 650), (816, 650)]
    for (path, label), (x, y) in zip(views, positions):
        canvas.paste(fit(Image.open(path), (736, 430)), (x, y))
        draw.rectangle((x, y + 430, x + 736, y + 474), fill=(28, 37, 32))
        draw.text((x + 14, y + 439), label, fill=ACCENT, font=font(20, True))
    canvas.save(RENDERS / "14_eye_cavity_geometry_verification.png")


def eye_shape_correction_board():
    screenshot = Image.open(USER_SHAPE_SCREENSHOT).convert("RGB")
    content_mask = screenshot.point(
        lambda value: 255 if value < 245 else 0
    ).convert("L")
    content_box = content_mask.getbbox()
    if content_box:
        screenshot = screenshot.crop(content_box)
    views = [
        (screenshot, "User-reported distorted-eye view"),
        (Image.open(RENDERS / "02_three_quarter_kursa_reference_match.png"), "Corrected three-quarter view"),
        (Image.open(RENDERS / "07_head_three_quarter_detail.png"), "Corrected eye-shape detail"),
    ]
    canvas = Image.new("RGB", (1600, 760), BG)
    draw = ImageDraw.Draw(canvas)
    draw.text((48, 30), "Kursa eye-shape distortion correction", fill=TEXT, font=font(38, True))
    draw.text((48, 80), "Location retained; per-surface shear and right-eye vertical clipping removed", fill=MUTED, font=font(22))
    positions = [(48, 142), (560, 142), (1072, 142)]
    for (image, label), (x, y) in zip(views, positions):
        canvas.paste(fit(image, (480, 520)), (x, y))
        draw.rectangle((x, y + 520, x + 480, y + 564), fill=(28, 37, 32))
        draw.text((x + 14, y + 529), label, fill=ACCENT, font=font(18, True))
    draw.text((48, 724), "Final blue-shape aspect ratios: left 1.39:1 / right 1.37:1", fill=TEXT, font=font(22, True))
    canvas.save(RENDERS / "15_eye_shape_distortion_correction.png")


def main():
    labeled_pair(
        "Kursa full appearance comparison",
        REFERENCE / "Kursa_reference.png", "Original appearance reference",
        RENDERS / "01_front_kursa_reference_match.png", "Current FBX — material-only sample",
        "03_reference_side_by_side_overview.png",
    )
    labeled_pair(
        "Head, hood, optics and glyph comparison",
        REFERENCE / "Kursa_reference_head_and_hood.png", "Reference crop",
        RENDERS / "07_head_front_detail.png", "Existing head surfaces with projected reference detail",
        "08_head_detail_comparison.png",
    )
    labeled_pair(
        "Shield and left-arm comparison",
        REFERENCE / "Kursa_reference_shield.png", "Reference crop",
        RENDERS / "10_shield_arm_detail.png", "Existing shield surface with worn gunmetal material",
        "11_shield_arm_comparison.png",
    )
    labeled_pair(
        "Torso material distribution comparison",
        REFERENCE / "Kursa_reference_torso_and_arms.png", "Reference crop",
        RENDERS / "12_torso_front_detail.png", "Current torso geometry with synchronized palette",
        "13_torso_detail_comparison.png",
    )
    texture_board()
    eye_multiview_board()
    eye_cavity_evidence_board()
    eye_shape_correction_board()
    print("Built 8 Kursa review boards")


if __name__ == "__main__":
    main()
