from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / "negatif" / "appearance_reference_sync"
REFERENCE = PROJECT_ROOT / "image" / "négatif(네거티프).png"
RENDER = SAMPLE_ROOT / "renders" / "01_reference_matched_three_quarter.png"
COMPARISON = SAMPLE_ROOT / "renders" / "05_reference_comparison.png"
TEXTURE_BOARD = SAMPLE_ROOT / "renders" / "06_material_texture_breakdown.png"
TEXTURE_DIR = SAMPLE_ROOT / "textures"


def font(size, bold=False):
    candidates = [
        Path("C:/Windows/Fonts/malgunbd.ttf" if bold else "C:/Windows/Fonts/malgun.ttf"),
        Path("C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def fit(image, box):
    source = image.convert("RGB")
    scale = min(box[0] / source.width, box[1] / source.height)
    resized = source.resize(
        (max(1, int(source.width * scale)), max(1, int(source.height * scale))),
        Image.Resampling.LANCZOS,
    )
    panel = Image.new("RGB", box, (238, 237, 232))
    panel.paste(resized, ((box[0] - resized.width) // 2, (box[1] - resized.height) // 2))
    return panel


def comparison_board():
    width, height = 2600, 1080
    board = Image.new("RGB", (width, height), (24, 21, 19))
    draw = ImageDraw.Draw(board)
    title_font = font(48, True)
    label_font = font(31, True)
    draw.text((70, 38), "니게티프 외형 동기화 · 기준 이미지 비교", font=title_font, fill=(244, 235, 222))
    panel_size = (1190, 820)
    left = fit(Image.open(REFERENCE), panel_size)
    right = fit(Image.open(RENDER), panel_size)
    board.paste(left, (70, 135))
    board.paste(right, (1340, 135))
    draw.text((70, 975), "기준 이미지", font=label_font, fill=(241, 178, 108))
    draw.text((1340, 975), "현재 FBX · 재질/텍스처 샘플", font=label_font, fill=(241, 178, 108))
    COMPARISON.parent.mkdir(parents=True, exist_ok=True)
    board.save(COMPARISON, quality=95)


def texture_breakdown():
    entries = [
        ("마모 금속", "negatif_worn_bronze_albedo.png"),
        ("어두운 기계부", "negatif_dark_mechanism_albedo.png"),
        ("캔버스 주머니", "negatif_canvas_albedo.png"),
        ("가죽 스트랩", "negatif_leather_albedo.png"),
        ("구리 포인트", "negatif_copper_accent_albedo.png"),
        ("주황 발광부", "negatif_amber_eye_albedo.png"),
    ]
    tile_w, tile_h = 480, 480
    width, height = 1680, 1160
    board = Image.new("RGB", (width, height), (25, 22, 20))
    draw = ImageDraw.Draw(board)
    draw.text((60, 36), "니게티프 재질 텍스처 분해", font=font(46, True), fill=(244, 235, 222))
    for index, (label, filename) in enumerate(entries):
        column = index % 3
        row = index // 3
        x = 60 + column * 540
        y = 120 + row * 510
        image = Image.open(TEXTURE_DIR / filename).convert("RGB")
        image = image.resize((tile_w, tile_h - 50), Image.Resampling.LANCZOS)
        board.paste(image, (x, y + 48))
        draw.text((x, y), label, font=font(28, True), fill=(236, 173, 103))
    board.save(TEXTURE_BOARD, quality=95)


if __name__ == "__main__":
    comparison_board()
    texture_breakdown()
    print(f"NEGATIF_COMPARISON_BOARD={COMPARISON}")
    print(f"NEGATIF_TEXTURE_BOARD={TEXTURE_BOARD}")
