from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[4]
SAMPLE_DIR = ROOT / "artSample/enemies/resistance"
REFERENCE_IMAGE = ROOT / "image/résistance(레지스탕스).png"
RENDER_DIR = SAMPLE_DIR / "renders"
TEXTURE_DIR = SAMPLE_DIR / "textures"


def fit_image(path, size, background):
    image = Image.open(path).convert("RGB")
    image.thumbnail(size, Image.Resampling.LANCZOS)
    output = Image.new("RGB", size, background)
    output.paste(
        image,
        (
            (size[0] - image.width) // 2,
            (size[1] - image.height) // 2,
        ),
    )
    return output


def create_reference_comparison():
    cell_size = (1000, 1000)
    background = (240, 242, 244)
    comparison = Image.new(
        "RGB",
        (cell_size[0] * 2, cell_size[1]),
        background,
    )
    comparison.paste(
        fit_image(REFERENCE_IMAGE, cell_size, background),
        (0, 0),
    )
    comparison.paste(
        fit_image(
            RENDER_DIR / "01_front_resistance_reference_match.png",
            cell_size,
            background,
        ),
        (cell_size[0], 0),
    )
    comparison.save(
        RENDER_DIR / "03_reference_side_by_side_overview.png"
    )


def load_font(size):
    candidates = [
        Path("C:/Windows/Fonts/malgun.ttf"),
        Path("C:/Windows/Fonts/arial.ttf"),
    ]
    for candidate in candidates:
        if candidate.is_file():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def create_texture_breakdown():
    entries = [
        ("WORN SILVER", "resistance_worn_silver_albedo.png"),
        ("DARK MECHANICS", "resistance_dark_mechanics_albedo.png"),
        ("CYAN ACCENT", "resistance_cyan_emission_albedo.png"),
        ("BRONZE ACCENT", "resistance_bronze_accents_albedo.png"),
        ("OLIVE BANDANA", "resistance_bandana_olive_albedo.png"),
        ("ROUGHNESS", "resistance_surface_roughness.png"),
        ("MICRO BUMP", "resistance_surface_micro_bump.png"),
    ]
    width, height = 1800, 1000
    background = (30, 35, 43)
    board = Image.new("RGB", (width, height), background)
    draw = ImageDraw.Draw(board)
    title_font = load_font(36)
    label_font = load_font(22)
    draw.text(
        (46, 28),
        "RESISTANCE UNCHANGED MODEL MATERIAL BREAKDOWN",
        fill=(235, 239, 244),
        font=title_font,
    )
    cell_width = 410
    cell_height = 390
    start_x = 45
    start_y = 100
    gap_x = 30
    gap_y = 55
    for index, (label, file_name) in enumerate(entries):
        row = index // 4
        column = index % 4
        x = start_x + column * (cell_width + gap_x)
        y = start_y + row * (cell_height + gap_y)
        swatch = fit_image(
            TEXTURE_DIR / file_name,
            (cell_width, cell_height - 42),
            (18, 21, 27),
        )
        board.paste(swatch, (x, y))
        draw.rectangle(
            (x, y, x + cell_width, y + cell_height - 42),
            outline=(88, 99, 116),
            width=2,
        )
        draw.text(
            (x + 8, y + cell_height - 34),
            label,
            fill=(214, 222, 232),
            font=label_font,
        )
    board.save(
        RENDER_DIR / "06_texture_atlas_and_material_breakdown.png"
    )


def main():
    RENDER_DIR.mkdir(parents=True, exist_ok=True)
    create_reference_comparison()
    create_texture_breakdown()
    print("Generated Resistance reference comparison and texture breakdown.")


if __name__ == "__main__":
    main()
