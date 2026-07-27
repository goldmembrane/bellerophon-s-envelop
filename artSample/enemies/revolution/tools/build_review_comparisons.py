from pathlib import Path
from PIL import Image, ImageDraw
import sys


reference_path = Path(sys.argv[1])
sample_root = Path(sys.argv[2])
render_root = sample_root / "renders"


def fit(image, width, height, background):
    source = image.convert("RGB")
    ratio = min(width / source.width, height / source.height)
    resized = source.resize(
        (round(source.width * ratio), round(source.height * ratio)),
        Image.Resampling.LANCZOS,
    )
    canvas = Image.new("RGB", (width, height), background)
    canvas.paste(
        resized,
        (
            (width - resized.width) // 2,
            (height - resized.height) // 2,
        ),
    )
    return canvas


def comparison(left_path, right_path, left_label, right_label, output_path):
    background = (20, 25, 30)
    panel = (30, 36, 42)
    width = 1600
    height = 700
    image = Image.new("RGB", (width, height), background)
    draw = ImageDraw.Draw(image)
    left = fit(Image.open(left_path), 760, 590, panel)
    right = fit(Image.open(right_path), 760, 590, panel)
    image.paste(left, (20, 70))
    image.paste(right, (820, 70))
    draw.text((20, 24), left_label, fill=(230, 235, 239))
    draw.text((820, 24), right_label, fill=(230, 235, 239))
    draw.rectangle((19, 69, 780, 660), outline=(89, 104, 116), width=2)
    draw.rectangle((819, 69, 1580, 660), outline=(89, 104, 116), width=2)
    image.save(output_path)


comparison(
    render_root / "00_source_front_neutral.png",
    render_root / "02_front_reference_material.png",
    "REPLACED FBX - UNMATERIALIZED",
    "SAME FBX - REFERENCE MATERIAL SAMPLE",
    render_root / "06_before_after_same_mesh.png",
)
comparison(
    reference_path,
    render_root / "03_three_quarter_reference_material.png",
    "USER REFERENCE IMAGE",
    "REPLACED FBX - THREE QUARTER REVIEW",
    render_root / "07_reference_and_sample.png",
)
