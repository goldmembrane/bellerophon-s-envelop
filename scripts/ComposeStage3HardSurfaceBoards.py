from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


SAMPLE_ROOT = Path("artSample/stage3_hardsurface_reproduction_sample")
SLOT_ROOT = SAMPLE_ROOT / "slots"
RENDER_ROOT = SAMPLE_ROOT / "renders"
BOARD_SIZE = (1536, 1024)
BACKGROUND = (18, 20, 19)
LINE = (76, 80, 76)
ITEM_ID = "01"
SLUG = "cockpit_helm_and_status"
FILE_SUFFIX = "hardsurface_structure_v006"


def slot_path(slot_name: str) -> Path:
    path = SLOT_ROOT / f"{ITEM_ID}_{SLUG}_{slot_name}.png"
    if not path.exists():
        raise FileNotFoundError(path)
    return path


def cover(source: Image.Image, width: int, height: int, scale_mul: float = 1.0, offset: tuple[int, int] = (0, 0)) -> Image.Image:
    scale = max(width / source.width, height / source.height) * scale_mul
    new_size = (max(1, round(source.width * scale)), max(1, round(source.height * scale)))
    resized = source.resize(new_size, Image.Resampling.LANCZOS)
    left = max(0, min(resized.width - width, (resized.width - width) // 2 + offset[0]))
    top = max(0, min(resized.height - height, (resized.height - height) // 2 + offset[1]))
    return resized.crop((left, top, left + width, top + height))


def tone_match(source: Image.Image, scale: float = 1.0, offset: int = 0) -> Image.Image:
    if scale == 1.0 and offset == 0:
        return source
    lut = [max(0, min(255, int(value * scale + offset))) for value in range(256)]
    return source.point(lut * 3)


def paste_fit(
    canvas: Image.Image,
    slot_name: str,
    rect: tuple[int, int, int, int],
    scale_mul: float = 1.0,
    offset: tuple[int, int] = (0, 0),
    tone_scale: float = 1.0,
    tone_offset: int = 0,
) -> None:
    x, y, width, height = rect
    source = Image.open(slot_path(slot_name)).convert("RGB")
    canvas.paste(tone_match(cover(source, width, height, scale_mul, offset), tone_scale, tone_offset), (x, y))


def create_board() -> Image.Image:
    board = Image.new("RGB", BOARD_SIZE, BACKGROUND)
    slots = (
        ("left_close", (0, 0, 470, 360), 1.08, (4, -8), 1.00, 0),
        ("center_close", (0, 360, 470, 345), 1.08, (0, -8), 1.00, 0),
        ("screen_close", (0, 705, 470, 319), 1.04, (0, -4), 1.00, 0),
        ("main", (470, 0, 1066, 690), 1.02, (0, -10), 1.00, 0),
        ("screen_close", (470, 690, 266, 334), 1.04, (0, -8), 1.00, 0),
        ("left_close", (736, 690, 266, 334), 1.04, (0, -4), 1.00, 0),
        ("center_close", (1002, 690, 266, 334), 1.08, (-2, -8), 1.00, 0),
        ("screen_close", (1268, 690, 268, 334), 1.04, (0, -8), 1.00, 0),
    )
    for slot_name, rect, scale_mul, offset, tone_scale, tone_offset in slots:
        paste_fit(board, slot_name, rect, scale_mul, offset, tone_scale, tone_offset)

    draw = ImageDraw.Draw(board)
    for line in ((470, 0, 470, 1024), (0, 360, 470, 360), (0, 705, 1536, 705)):
        draw.line(line, fill=LINE, width=3)
    return board


def main() -> None:
    RENDER_ROOT.mkdir(parents=True, exist_ok=True)
    output = RENDER_ROOT / f"{ITEM_ID}_{SLUG}_{FILE_SUFFIX}.png"
    create_board().save(output)
    print(f"Composed {output}")


if __name__ == "__main__":
    main()
