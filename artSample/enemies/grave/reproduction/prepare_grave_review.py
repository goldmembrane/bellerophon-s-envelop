from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parent
REFERENCE_PATH = ROOT / "source" / "grave_reference.png"
RGBA_PATH = ROOT / "review" / "grave_reproduction_front_rgba.png"
FRONT_PATH = ROOT / "review" / "grave_reproduction_front.png"
COMPARISON_PATH = ROOT / "review" / "grave_reference_comparison.png"

rgba = Image.open(RGBA_PATH).convert("RGBA")
white = Image.new("RGBA", rgba.size, (255, 255, 255, 255))
front = Image.alpha_composite(white, rgba).convert("RGB")
front.save(FRONT_PATH)

reference = Image.open(REFERENCE_PATH).convert("RGB")
if reference.size != front.size:
    raise RuntimeError(f"comparison size mismatch: {reference.size} != {front.size}")

header_height = 64
gutter = 24
comparison = Image.new("RGB", (reference.width * 2 + gutter, reference.height + header_height), "white")
comparison.paste(reference, (0, header_height))
comparison.paste(front, (reference.width + gutter, header_height))
draw = ImageDraw.Draw(comparison)
font_path = Path(r"C:\Windows\Fonts\malgun.ttf")
font = ImageFont.truetype(str(font_path), 28) if font_path.exists() else ImageFont.load_default()
draw.text((24, 15), "기준 이미지", fill=(24, 24, 24), font=font)
draw.text((reference.width + gutter + 24, 15), "그라베 재현 아트 샘플", fill=(24, 24, 24), font=font)
draw.rectangle((reference.width, 0, reference.width + gutter - 1, comparison.height), fill=(235, 235, 235))
comparison.save(COMPARISON_PATH)

print(f"GRAVE_FINAL_FRONT={FRONT_PATH}")
print(f"GRAVE_FINAL_COMPARISON={COMPARISON_PATH}")
