from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageOps


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/front_alignment_2026-08-03"
RENDER_DIR = SAMPLE_ROOT / "renders"
REFERENCE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync/reference"
FONT_PATH = Path(r"C:\Windows\Fonts\malgun.ttf")

BACKGROUND = (34, 38, 43)
PANEL_BACKGROUND = (78, 83, 89)
TEXT = (241, 244, 247)


def font(size):
    return ImageFont.truetype(str(FONT_PATH), size)


def load(path):
    return Image.open(path).convert("RGB")


def crop_reference(image):
    grayscale = image.convert("L")
    mask = grayscale.point(lambda value: 255 if value < 242 else 0)
    box = mask.getbbox()
    if box is None:
        return image
    left, top, right, bottom = box
    margin_x = max(12, int((right - left) * 0.06))
    margin_y = max(12, int((bottom - top) * 0.04))
    return image.crop(
        (
            max(0, left - margin_x),
            max(0, top - margin_y),
            min(image.width, right + margin_x),
            min(image.height, bottom + margin_y),
        )
    )


def panel(image, label, size=(640, 760), crop=False):
    width, height = size
    result = Image.new("RGB", size, PANEL_BACKGROUND)
    source = crop_reference(image) if crop else image
    fitted = ImageOps.contain(source, (width - 32, height - 88), Image.Resampling.LANCZOS)
    result.paste(fitted, ((width - fitted.width) // 2, 70 + (height - 88 - fitted.height) // 2))
    draw = ImageDraw.Draw(result)
    draw.rectangle((0, 0, width, 62), fill=BACKGROUND)
    draw.text((20, 14), label, font=font(28), fill=TEXT)
    return result


def grid(items, columns, panel_size, destination):
    panels = [panel(image, label, panel_size, crop) for image, label, crop in items]
    rows = (len(panels) + columns - 1) // columns
    canvas = Image.new(
        "RGB", (panel_size[0] * columns, panel_size[1] * rows), BACKGROUND
    )
    for index, item in enumerate(panels):
        x = (index % columns) * panel_size[0]
        y = (index // columns) * panel_size[1]
        canvas.paste(item, (x, y))
    canvas.save(destination, quality=95)


def main():
    grid(
        (
            (
                load(REFERENCE_ROOT / "Kursa_reference.png"),
                "원본 정면 기준",
                True,
            ),
            (load(RENDER_DIR / "01_current_front.png"), "현재 정적 자세", False),
            (load(RENDER_DIR / "04_candidate_front.png"), "정면 정렬 후보", False),
        ),
        3,
        (640, 760),
        RENDER_DIR / "13_reference_current_candidate.png",
    )
    grid(
        (
            (
                load(REFERENCE_ROOT / "Kursa_reference_torso_and_arms.png"),
                "원본 몸통·양팔",
                False,
            ),
            (load(RENDER_DIR / "02_current_upper_front.png"), "현재 상체", False),
            (
                load(RENDER_DIR / "10_candidate_upper_front_no_shield.png"),
                "후보 상체 — 방패 숨김",
                False,
            ),
            (
                load(REFERENCE_ROOT / "Kursa_reference_head_and_hood.png"),
                "원본 얼굴·후드",
                False,
            ),
            (load(RENDER_DIR / "03_current_face_front.png"), "현재 얼굴", False),
            (load(RENDER_DIR / "06_candidate_face_front.png"), "후보 얼굴 정면", False),
        ),
        3,
        (600, 650),
        RENDER_DIR / "14_upper_face_comparison.png",
    )
    grid(
        (
            (
                load(RENDER_DIR / "07_candidate_yaw_minus25.png"),
                "후보 -25도 — 방패 포함",
                False,
            ),
            (
                load(RENDER_DIR / "08_candidate_yaw_plus25.png"),
                "후보 +25도 — 방패 포함",
                False,
            ),
            (
                load(RENDER_DIR / "11_candidate_yaw_minus25_no_shield.png"),
                "후보 -25도 — 방패 숨김",
                False,
            ),
            (
                load(RENDER_DIR / "12_candidate_yaw_plus25_no_shield.png"),
                "후보 +25도 — 방패 숨김",
                False,
            ),
        ),
        4,
        (520, 620),
        RENDER_DIR / "15_candidate_yaw_review.png",
    )


if __name__ == "__main__":
    main()
