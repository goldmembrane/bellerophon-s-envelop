from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
REFERENCE = ROOT / "image/pāḫḫur(파후르).png"
FRONT_RENDER = SAMPLE_ROOT / "renders/01_front_pahur_reference_match.png"
HEAD_RENDER = SAMPLE_ROOT / "renders/07_head_front_detail.png"
HEAD_THREE_QUARTER_RENDER = (
    SAMPLE_ROOT / "renders/07_head_three_quarter_detail.png"
)
SHOULDER_ARM_RENDER = SAMPLE_ROOT / "renders/10_shoulders_left_arm_detail.png"
TORSO_RENDER = SAMPLE_ROOT / "renders/12_torso_front_detail.png"
OUTPUT = SAMPLE_ROOT / "renders/03_reference_side_by_side_overview.png"
HEAD_OUTPUT = SAMPLE_ROOT / "renders/08_head_detail_comparison.png"
SHOULDER_ARM_OUTPUT = (
    SAMPLE_ROOT / "renders/11_shoulders_left_arm_comparison.png"
)
TORSO_OUTPUT = SAMPLE_ROOT / "renders/13_torso_detail_comparison.png"


def fitted(image, width, height):
    result = Image.new("RGB", (width, height), (225, 230, 231))
    copy = image.copy()
    copy.thumbnail((width, height), Image.Resampling.LANCZOS)
    x = (width - copy.width) // 2
    y = (height - copy.height) // 2
    result.paste(copy, (x, y))
    return result


def fitted_enlarged(image, width, height):
    result = Image.new("RGB", (width, height), (225, 230, 231))
    copy = image.copy()
    scale = min(width / copy.width, height / copy.height)
    copy = copy.resize(
        (max(1, round(copy.width * scale)), max(1, round(copy.height * scale))),
        Image.Resampling.LANCZOS,
    )
    x = (width - copy.width) // 2
    y = (height - copy.height) // 2
    result.paste(copy, (x, y))
    return result


def main():
    reference = Image.open(REFERENCE).convert("RGB")
    render = Image.open(FRONT_RENDER).convert("RGB")
    head_render = Image.open(HEAD_RENDER).convert("RGB")
    head_three_quarter_render = Image.open(HEAD_THREE_QUARTER_RENDER).convert("RGB")
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
        "파후르 — 기준 이미지 / 교체 FBX 외형 동기화 비교",
        fill=(237, 242, 241),
        font=font,
    )
    draw.text(
        (44, 54),
        "원본 FBX는 유지하고, 검토 샘플에서 상단의 잘못 배치된 독립 판 메시만 실제 삭제했습니다.",
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
        "기준 이미지: image/파후르 기준 이미지",
        fill=(218, 226, 226),
        font=font,
    )
    draw.text(
        (896, 942),
        "아트 샘플: 교체 Pahur.fbx + 기준 이미지 기반 텍스처·머티리얼",
        fill=(218, 226, 226),
        font=font,
    )
    canvas.save(OUTPUT)
    print(OUTPUT)

    head_canvas = Image.new("RGB", (2000, 760), (24, 32, 35))
    head_draw = ImageDraw.Draw(head_canvas)
    head_draw.text(
        (44, 28),
        "파후르 머리 세부 비교 — 안쪽이 낮아지는 화난 눈 각도",
        fill=(237, 242, 241),
        font=font,
    )
    head_draw.text(
        (44, 56),
        "기준 이미지와 정면·3/4에서 바깥 눈꼬리는 높고 중앙 쪽 끝은 낮게 유지되는지 비교합니다.",
        fill=(166, 182, 184),
        font=font,
    )
    reference_head = reference.crop((626, 0, 820, 190))
    render_head = head_render.crop((165, 130, 1115, 1080))
    render_head_three_quarter = head_three_quarter_render.crop(
        (165, 130, 1115, 1080)
    )
    left_head = fitted_enlarged(reference_head, 600, 610)
    center_head = fitted_enlarged(render_head, 600, 610)
    right_head = fitted_enlarged(render_head_three_quarter, 600, 610)
    head_canvas.paste(left_head, (44, 104))
    head_canvas.paste(center_head, (700, 104))
    head_canvas.paste(right_head, (1356, 104))
    head_draw.rectangle((44, 104, 644, 714), outline=(83, 101, 103), width=2)
    head_draw.rectangle((700, 104, 1300, 714), outline=(83, 101, 103), width=2)
    head_draw.rectangle((1356, 104, 1956, 714), outline=(83, 101, 103), width=2)
    head_draw.text((44, 724), "기준 이미지 머리 확대", fill=(218, 226, 226), font=font)
    head_draw.text(
        (700, 724),
        "현재 FBX 메시 보존 머리 정면",
        fill=(218, 226, 226),
        font=font,
    )
    head_draw.text(
        (1356, 724),
        "현재 FBX 메시 보존 머리 3/4",
        fill=(218, 226, 226),
        font=font,
    )
    head_canvas.save(HEAD_OUTPUT)
    print(HEAD_OUTPUT)

    shoulder_arm_render = Image.open(SHOULDER_ARM_RENDER).convert("RGB")
    shoulder_canvas = Image.new("RGB", (1600, 760), (24, 32, 35))
    shoulder_draw = ImageDraw.Draw(shoulder_canvas)
    shoulder_draw.text(
        (44, 28),
        "파후르 상반신 비교 — 청회색 장갑 / 밝은 흉부판 / 기계 관절",
        fill=(237, 242, 241),
        font=font,
    )
    shoulder_draw.text(
        (44, 56),
        "기존 장갑 형상 위에 원화의 명도 구조를 맞추고, 흘러내리던 UV 패턴과 강한 가짜 범프를 제거했습니다.",
        fill=(166, 182, 184),
        font=font,
    )
    reference_upper = reference.crop((540, 82, 930, 560))
    render_upper = shoulder_arm_render.crop((110, 120, 1220, 1160))
    left_upper = fitted_enlarged(reference_upper, 720, 610)
    right_upper = fitted_enlarged(render_upper, 720, 610)
    shoulder_canvas.paste(left_upper, (44, 104))
    shoulder_canvas.paste(right_upper, (836, 104))
    shoulder_draw.rectangle(
        (44, 104, 764, 714), outline=(83, 101, 103), width=2
    )
    shoulder_draw.rectangle(
        (836, 104, 1556, 714), outline=(83, 101, 103), width=2
    )
    shoulder_draw.text(
        (44, 724),
        "기준 이미지의 어깨·왼팔 확대",
        fill=(218, 226, 226),
        font=font,
    )
    shoulder_draw.text(
        (836, 724),
        "교체 FBX 메시 보존·기준 이미지 재질 확대",
        fill=(218, 226, 226),
        font=font,
    )
    shoulder_canvas.save(SHOULDER_ARM_OUTPUT)
    print(SHOULDER_ARM_OUTPUT)

    torso_render = Image.open(TORSO_RENDER).convert("RGB")
    torso_canvas = Image.new("RGB", (1600, 760), (24, 32, 35))
    torso_draw = ImageDraw.Draw(torso_canvas)
    torso_draw.text(
        (44, 28),
        "파후르 몸통 정면 비교 — 중앙 상단 독립 판 실제 삭제",
        fill=(237, 242, 241),
        font=font,
    )
    torso_draw.text(
        (44, 56),
        "연결 표면 97의 정점 11개·면 18개를 삭제하고 나머지 몸통 표면은 그대로 유지했습니다.",
        fill=(166, 182, 184),
        font=font,
    )
    reference_torso = reference.crop((555, 105, 875, 560))
    render_torso = torso_render.crop((170, 125, 1110, 1165))
    left_torso = fitted_enlarged(reference_torso, 720, 610)
    right_torso = fitted_enlarged(render_torso, 720, 610)
    torso_canvas.paste(left_torso, (44, 104))
    torso_canvas.paste(right_torso, (836, 104))
    torso_draw.rectangle((44, 104, 764, 714), outline=(83, 101, 103), width=2)
    torso_draw.rectangle(
        (836, 104, 1556, 714), outline=(83, 101, 103), width=2
    )
    torso_draw.text(
        (44, 724),
        "기준 이미지 몸통 구조",
        fill=(218, 226, 226),
        font=font,
    )
    torso_draw.text(
        (836, 724),
        "원본 FBX 보존·검토 샘플의 중앙 상단 판 삭제",
        fill=(218, 226, 226),
        font=font,
    )
    torso_canvas.save(TORSO_OUTPUT)
    print(TORSO_OUTPUT)


if __name__ == "__main__":
    main()
