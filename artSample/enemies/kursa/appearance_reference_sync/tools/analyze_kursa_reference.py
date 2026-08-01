from collections import Counter
import json
from pathlib import Path

from PIL import Image, ImageStat


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
REFERENCE = ROOT / "image/KUŠkursa(쿠르사).png"
REFERENCE_DIR = SAMPLE_ROOT / "reference"
OUTPUT = SAMPLE_ROOT / "REFERENCE_IMAGE_ANALYSIS.json"


REGIONS = {
    "head_and_hood": (650, 10, 818, 176),
    "torso_and_arms": (548, 125, 868, 442),
    "legs": (555, 382, 846, 758),
    "shield": (827, 126, 980, 566),
}


def non_background_pixels(image):
    pixels = []
    for red, green, blue in image.convert("RGB").getdata():
        maximum = max(red, green, blue)
        minimum = min(red, green, blue)
        if maximum < 242 or maximum - minimum > 14:
            pixels.append((red, green, blue))
    return pixels


def palette(pixels, count=10):
    if not pixels:
        return []
    sample = Image.new("RGB", (len(pixels), 1))
    sample.putdata(pixels)
    quantized = sample.quantize(colors=count, method=Image.Quantize.MEDIANCUT)
    color_counts = Counter(quantized.getdata())
    palette_values = quantized.getpalette()
    total = sum(color_counts.values())
    result = []
    for index, amount in color_counts.most_common(count):
        rgb = palette_values[index * 3 : index * 3 + 3]
        result.append(
            {
                "rgb": rgb,
                "hex": "#{:02X}{:02X}{:02X}".format(*rgb),
                "fraction": round(amount / total, 6),
            }
        )
    return result


def selected_pixels(image, predicate):
    return [
        (red, green, blue)
        for red, green, blue in image.convert("RGB").getdata()
        if predicate(red, green, blue)
    ]


def region_report(image, box, filename):
    crop = image.crop(box)
    crop.save(REFERENCE_DIR / filename)
    pixels = non_background_pixels(crop)
    mean = ImageStat.Stat(crop.convert("RGB")).mean
    return {
        "box": list(box),
        "saved_crop": f"reference/{filename}",
        "mean_rgb_including_background": [round(value, 3) for value in mean],
        "dominant_non_background_palette": palette(pixels),
    }


def main():
    REFERENCE_DIR.mkdir(parents=True, exist_ok=True)
    image = Image.open(REFERENCE).convert("RGB")
    image.save(REFERENCE_DIR / "Kursa_reference.png")
    regions = {
        name: region_report(image, box, f"Kursa_reference_{name}.png")
        for name, box in REGIONS.items()
    }
    report = {
        "source": "image/KUŠkursa(쿠르사).png",
        "copied_reference": "reference/Kursa_reference.png",
        "dimensions": [image.width, image.height],
        "analysis_scope": (
            "색 배치와 표면 재질만 분석한다. 기준 이미지의 형상 차이는 "
            "현재 쿠르사 FBX 메시를 변형하거나 새 부품을 만드는 근거로 사용하지 않는다."
        ),
        "whole_character_palette": palette(non_background_pixels(image), 14),
        "blue_armor_and_optic_palette": palette(
            selected_pixels(
                image,
                lambda red, green, blue: (
                    blue >= red + 12
                    and blue >= green + 4
                    and blue < 235
                    and red < 150
                ),
            ),
            10,
        ),
        "cyan_glyph_palette": palette(
            selected_pixels(
                image,
                lambda red, green, blue: (
                    green >= red + 16
                    and blue >= red + 18
                    and green >= 70
                    and blue >= 85
                    and red < 150
                ),
            ),
            10,
        ),
        "regions": regions,
        "observed_material_intent": [
            "청회색과 중성 건메탈 장갑판",
            "거의 검은 내부 관절과 케이블",
            "남색 계열 국소 장갑판",
            "무광에 가까운 짙은 두건과 청록색 문자",
            "푸른 광학 눈",
            "긁힘과 가장자리 마모가 누적된 무광 제압방패",
            "소량의 밝은 강철 테두리와 체결부",
        ],
    }
    OUTPUT.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "dimensions": report["dimensions"],
                "palette_entries": len(report["whole_character_palette"]),
                "regions": list(regions),
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
