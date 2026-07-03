from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


SAMPLE_DIR = Path("artSample/stage3_reproduction_sample")
SLOT_DIR = SAMPLE_DIR / "modeled_slots"
RENDER_DIR = SAMPLE_DIR / "renders"
BOARD_SIZE = (1536, 1024)
BACKGROUND = (18, 20, 19)
LINE = (76, 80, 76)

TARGETS = (
    ("01", "cockpit_helm_and_status"),
    ("02", "control_room_cctv_terminal"),
    ("03", "engine_room_power_terminal"),
    ("04", "supply_room_storage_cabinet"),
    ("05", "cargo_hold_props_and_terminal"),
    ("06", "armory_turret_grip_mount"),
    ("07", "first_person_equipment"),
)


def slot_path(item_id: str, slug: str, slot_name: str) -> Path:
    path = SLOT_DIR / f"{item_id}_{slug}_{slot_name}.png"
    if not path.exists():
        raise FileNotFoundError(path)
    return path


def paste_fit(canvas: Image.Image, image_path: Path, rect: tuple[int, int, int, int]) -> None:
    x, y, width, height = rect
    source = Image.open(image_path).convert("RGB")
    fitted = cover(source, width, height)
    canvas.paste(fitted, (x, y))


def cover(source: Image.Image, width: int, height: int) -> Image.Image:
    scale = max(width / source.width, height / source.height)
    new_size = (max(1, round(source.width * scale)), max(1, round(source.height * scale)))
    resized = source.resize(new_size, Image.Resampling.LANCZOS)
    left = max(0, (resized.width - width) // 2)
    top = max(0, (resized.height - height) // 2)
    return resized.crop((left, top, left + width, top + height))


def draw_lines(canvas: Image.Image, lines: tuple[tuple[int, int, int, int], ...]) -> None:
    draw = ImageDraw.Draw(canvas)
    for line in lines:
        draw.line(line, fill=LINE, width=3)


def create_board(item_id: str, slug: str) -> Image.Image:
    board = Image.new("RGB", BOARD_SIZE, BACKGROUND)

    if item_id == "01":
        slots = (
            ("left_close", (0, 0, 470, 360)),
            ("center_close", (0, 360, 470, 345)),
            ("screen_close", (0, 705, 470, 319)),
            ("main", (470, 0, 1066, 690)),
            ("screen_close", (470, 690, 266, 334)),
            ("left_close", (736, 690, 266, 334)),
            ("center_close", (1002, 690, 266, 334)),
            ("screen_close", (1268, 690, 268, 334)),
        )
        lines = ((470, 0, 470, 1024), (0, 360, 470, 360), (0, 705, 1536, 705))
    elif item_id == "02":
        slots = (
            ("large_screen", (0, 0, 512, 280)),
            ("large_screen", (0, 280, 512, 184)),
            ("main", (0, 464, 512, 280)),
            ("button_panel", (0, 744, 512, 106)),
            ("pipe_detail", (0, 850, 512, 174)),
            ("main", (512, 0, 1024, 1024)),
        )
        lines = ((512, 0, 512, 1024), (0, 280, 512, 280), (0, 464, 512, 464), (0, 744, 512, 744), (0, 850, 512, 850))
    elif item_id == "03":
        slots = (
            ("terminal", (0, 0, 444, 444)),
            ("breaker", (0, 444, 444, 210)),
            ("pipe", (0, 654, 444, 370)),
            ("main", (444, 0, 1092, 705)),
            ("pipe", (444, 705, 680, 319)),
            ("terminal", (1124, 705, 412, 319)),
        )
        lines = ((444, 0, 444, 1024), (0, 444, 444, 444), (0, 654, 444, 654), (444, 705, 1536, 705), (1124, 705, 1124, 1024))
    elif item_id == "04":
        slots = (
            ("door", (0, 0, 325, 426)),
            ("cabinet_iso", (325, 0, 325, 426)),
            ("handle", (0, 426, 650, 178)),
            ("cabinet_iso", (0, 604, 650, 420)),
            ("main", (650, 0, 886, 1024)),
        )
        lines = ((650, 0, 650, 1024), (325, 0, 325, 426), (0, 426, 650, 426), (0, 604, 650, 604))
    elif item_id == "05":
        slots = (
            ("panel", (0, 0, 448, 235)),
            ("large_crate", (0, 235, 448, 330)),
            ("main", (0, 565, 448, 214)),
            ("terminal", (0, 779, 448, 245)),
            ("main", (448, 0, 1088, 673)),
            ("terminal", (448, 673, 272, 351)),
            ("terminal", (720, 673, 272, 351)),
            ("terminal", (992, 673, 272, 351)),
            ("terminal", (1264, 673, 272, 351)),
        )
        lines = ((448, 0, 448, 1024), (0, 235, 448, 235), (0, 565, 448, 565), (0, 779, 448, 779), (448, 673, 1536, 673))
    elif item_id == "06":
        slots = (
            ("rail", (0, 0, 622, 260)),
            ("sight", (0, 260, 622, 205)),
            ("grips", (0, 465, 622, 282)),
            ("sight", (0, 747, 311, 277)),
            ("grips", (311, 747, 311, 277)),
            ("main", (622, 0, 914, 1024)),
        )
        lines = ((622, 0, 622, 1024), (0, 260, 622, 260), (0, 465, 622, 465), (0, 747, 622, 747), (311, 747, 311, 1024))
    elif item_id == "07":
        slots = (
            ("staff_full", (0, 0, 200, 1024)),
            ("hook", (200, 0, 310, 245)),
            ("staff_full", (200, 245, 310, 298)),
            ("main", (200, 543, 310, 194)),
            ("wrist", (200, 737, 310, 287)),
            ("main", (510, 0, 1026, 1024)),
        )
        lines = ((200, 0, 200, 1024), (510, 0, 510, 1024), (200, 245, 510, 245), (200, 543, 510, 543), (200, 737, 510, 737))
    else:
        raise ValueError(item_id)

    for slot_name, rect in slots:
        paste_fit(board, slot_path(item_id, slug, slot_name), rect)
    draw_lines(board, lines)
    return board


def main() -> None:
    RENDER_DIR.mkdir(parents=True, exist_ok=True)
    for item_id, slug in TARGETS:
        board = create_board(item_id, slug)
        board.save(RENDER_DIR / f"{item_id}_{slug}_modeled_v001.png")
        print(f"Composed modeled board {item_id}_{slug}_modeled_v001.png")


if __name__ == "__main__":
    main()
