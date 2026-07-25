from pathlib import Path
import shutil

from PIL import Image, ImageDraw, ImageFont


GRAVE_ROOT = Path(__file__).resolve().parents[1]
REPRODUCTION_ROOT = GRAVE_ROOT / "reproduction"
RENDER_ROOT = GRAVE_ROOT / "renders"
TEXTURE_ROOT = GRAVE_ROOT / "textures"
REFERENCE = REPRODUCTION_ROOT / "source" / "grave_reference.png"
FONT_PATH = Path(r"C:\Windows\Fonts\malgun.ttf")


def font(size):
    return ImageFont.truetype(str(FONT_PATH), size) if FONT_PATH.exists() else ImageFont.load_default()


def composite_white(source, destination):
    rgba = Image.open(source).convert("RGBA")
    white = Image.new("RGBA", rgba.size, (255, 255, 255, 255))
    Image.alpha_composite(white, rgba).convert("RGB").save(destination)


RENDER_ROOT.mkdir(parents=True, exist_ok=True)
TEXTURE_ROOT.mkdir(parents=True, exist_ok=True)

shutil.copy2(
    REPRODUCTION_ROOT / "review" / "grave_reproduction_front.png",
    RENDER_ROOT / "01_front_grave_reference_match.png",
)

for stem in ("02_side_grave_inferred_surface", "04_three_quarter_grave_material", "05_close_grave_suit_application"):
    composite_white(RENDER_ROOT / f"{stem}_rgba.png", RENDER_ROOT / f"{stem}.png")

texture_names = (
    "grave_front_albedo.png",
    "grave_textile_albedo.png",
    "grave_fabric_normal.png",
    "grave_fabric_roughness.png",
)
for texture_name in texture_names:
    shutil.copy2(REPRODUCTION_ROOT / "textures" / texture_name, TEXTURE_ROOT / texture_name)

reference = Image.open(REFERENCE).convert("RGB")
front = Image.open(RENDER_ROOT / "01_front_grave_reference_match.png").convert("RGB")
overview = Image.new("RGB", (2816, 832), "white")
overview.paste(reference, (0, 64))
overview.paste(front, (1408, 64))
draw = ImageDraw.Draw(overview)
draw.text((24, 14), "정면 기준 이미지", fill=(25, 25, 25), font=font(28))
draw.text((1432, 14), "그라베 재현 렌더", fill=(25, 25, 25), font=font(28))
draw.rectangle((1398, 0, 1417, 832), fill=(230, 230, 230))
overview.save(RENDER_ROOT / "03_reference_side_by_side_overview.png")

canvas = Image.new("RGB", (1600, 980), (245, 245, 242))
draw = ImageDraw.Draw(canvas)
draw.text((40, 24), "그라베 텍스처·머티리얼 분석", fill=(28, 28, 28), font=font(34))
cards = [
    ("grave_front_albedo.png", "전면 턱시도 알베도", "기준 이미지의 나비넥타이·라펠·조끼·단추·주머니 선화"),
    ("grave_textile_albedo.png", "후면·옆면 직물 알베도", "정면 한 장만 존재하므로 보이는 회색 직물 표면을 연장한 추론"),
    ("grave_fabric_normal.png", "직물 노멀", "교차 섬유의 미세 요철, 강도 0.16"),
    ("grave_fabric_roughness.png", "직물 거칠기", "비금속 무광 직물의 불규칙한 반사 분포"),
]
for index, (name, title, description) in enumerate(cards):
    column = index % 2
    row = index // 2
    x = 40 + column * 780
    y = 90 + row * 435
    draw.rounded_rectangle((x, y, x + 740, y + 400), radius=16, fill=(255, 255, 255), outline=(190, 190, 185), width=2)
    texture = Image.open(TEXTURE_ROOT / name).convert("RGB")
    texture.thumbnail((700, 285))
    image_x = x + (740 - texture.width) // 2
    image_y = y + 18
    canvas.paste(texture, (image_x, image_y))
    text_y = y + 315
    draw.text((x + 20, text_y), title, fill=(30, 30, 30), font=font(23))
    draw.text((x + 20, text_y + 38), description, fill=(82, 82, 78), font=font(15))
canvas.save(RENDER_ROOT / "06_texture_material_breakdown.png")

print(f"GRAVE_REVIEW_OVERVIEW={RENDER_ROOT / '03_reference_side_by_side_overview.png'}")
print(f"GRAVE_REVIEW_TEXTURE_BREAKDOWN={RENDER_ROOT / '06_texture_material_breakdown.png'}")
