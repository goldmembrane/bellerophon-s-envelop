from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
REFERENCE = ROOT / "image/pāḫḫur(파후르).png"
FRONT_RENDER = SAMPLE_ROOT / "renders/01_front_pahur_reference_match.png"
OUTPUT = SAMPLE_ROOT / "renders/03_reference_side_by_side_overview.png"


def fitted(image, width, height):
    result = Image.new("RGB", (width, height), (225, 230, 231))
    copy = image.copy()
    copy.thumbnail((width, height), Image.Resampling.LANCZOS)
    x = (width - copy.width) // 2
    y = (height - copy.height) // 2
    result.paste(copy, (x, y))
    return result


def main():
    reference = Image.open(REFERENCE).convert("RGB")
    render = Image.open(FRONT_RENDER).convert("RGB")
    canvas = Image.new("RGB", (1760, 980), (24, 32, 35))
    draw = ImageDraw.Draw(canvas)
    font_path = Path(r"C:\Windows\Fonts\malgun.ttf")
    font = (
        ImageFont.truetype(str(font_path), 20)
        if font_path.exists()
        else ImageFont.load_default()
    )
    draw.text(
        (44, 30),
        "PAHUR — REFERENCE / CURRENT FBX MATERIAL SAMPLE",
        fill=(237, 242, 241),
        font=font,
    )
    draw.text(
        (44, 54),
        "Silhouette and mesh are intentionally preserved; comparison is color/material/texture only.",
        fill=(166, 182, 184),
        font=font,
    )
    left = fitted(reference, 820, 820)
    right = fitted(render, 820, 820)
    canvas.paste(left, (44, 106))
    canvas.paste(right, (896, 106))
    draw.rectangle((44, 106, 864, 926), outline=(83, 101, 103), width=2)
    draw.rectangle((896, 106, 1716, 926), outline=(83, 101, 103), width=2)
    draw.text(
        (44, 942),
        "REFERENCE: image/파후르 기준 이미지",
        fill=(218, 226, 226),
        font=font,
    )
    draw.text(
        (896, 942),
        "SAMPLE: current Pahur.fbx + review materials",
        fill=(218, 226, 226),
        font=font,
    )
    canvas.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
