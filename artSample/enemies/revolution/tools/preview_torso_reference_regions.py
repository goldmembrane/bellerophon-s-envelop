from pathlib import Path
from PIL import Image, ImageDraw
import sys


reference_path = Path(sys.argv[1])
sample_root = Path(sys.argv[2])
render_root = sample_root / "renders"
reference = Image.open(reference_path).convert("RGB")

regions = {
    "A_upper_left_shell": (560, 125, 640, 205),
    "B_eye_side_panel": (620, 150, 700, 230),
    "C_upper_right_shell": (720, 170, 800, 250),
    "D_left_mid_panel": (540, 230, 620, 310),
    "E_lower_center_panel": (650, 240, 730, 320),
    "F_right_mid_panel": (740, 250, 820, 330),
}

sheet = Image.new("RGB", (1200, 760), (18, 23, 28))
draw = ImageDraw.Draw(sheet)
draw.text(
    (24, 18),
    "TORSO REFERENCE REGION CANDIDATES - REVIEW ONLY",
    fill=(236, 240, 243),
)

for index, (name, box) in enumerate(regions.items()):
    crop = reference.crop(box)
    preview = crop.resize(
        (320, 320),
        Image.Resampling.NEAREST,
    )
    column = index % 3
    row = index // 3
    x = 24 + column * 390
    y = 70 + row * 350
    sheet.paste(preview, (x, y))
    draw.rectangle(
        (x, y, x + 319, y + 319),
        outline=(110, 127, 140),
        width=2,
    )
    draw.text(
        (x, y + 325),
        f"{name} {box}",
        fill=(210, 220, 226),
    )

sheet.save(
    render_root /
    "10_torso_reference_region_candidates.png"
)
