from pathlib import Path

from PIL import Image


root = Path(__file__).resolve().parent
source = Image.open(root / "review" / "work_preview_rgba.png").convert("RGBA")
background = Image.new("RGBA", source.size, (255, 255, 255, 255))
result = Image.alpha_composite(background, source).convert("RGB")
output = root / "review" / "work_preview.png"
result.save(output)
print(f"GRAVE_WORK_PREVIEW_COMPOSITED={output}")

