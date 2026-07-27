from pathlib import Path
from PIL import Image, ImageDraw
import hashlib
import json
import sys


reference_path = Path(sys.argv[1])
sample_root = Path(sys.argv[2])
texture_root = sample_root / "textures"
render_root = sample_root / "renders"
texture_root.mkdir(parents=True, exist_ok=True)
render_root.mkdir(parents=True, exist_ok=True)

reference = Image.open(reference_path).convert("RGB")

# Direct crops from dense visible material regions. No resize, paint,
# filtering, inferred maps, or generated source pixels are used.
regions = {
    "body_panel": (756, 188, 788, 220),
    "body_wear": (650, 174, 682, 206),
    "body_light_steel": (712, 132, 744, 164),
    "weapon_housing": (436, 420, 452, 436),
    "leg_armor": (796, 548, 828, 580),
    "copper_mechanics": (862, 268, 886, 292),
    "dark_mechanics": (700, 212, 732, 244),
    "blue_optic": (657, 108, 675, 126),
}

entries = []
for region_id, box in regions.items():
    crop = reference.crop(box)
    file_name = f"reference_{region_id}_direct_crop.png"
    output_path = texture_root / file_name
    crop.save(output_path)
    entries.append({
        "id": region_id,
        "box_xyxy": list(box),
        "size": list(crop.size),
        "file": f"textures/{file_name}",
        "operation": "direct RGB crop; no resize, repaint, filtering, or generated pixels",
        "sha256": hashlib.sha256(
            output_path.read_bytes()
        ).hexdigest().upper(),
    })

provenance = {
    "reference_file": str(reference_path),
    "reference_size": list(reference.size),
    "reference_sha256": hashlib.sha256(
        reference_path.read_bytes()
    ).hexdigest().upper(),
    "regions": entries,
    "restrictions": [
        "No source pixel was generated.",
        "No crop was resized, repainted, or filtered.",
        "No metallic, roughness, normal, height, or emission map was inferred.",
        "The original FBX UV layer remains unchanged.",
    ],
}
(sample_root / "CROP_PROVENANCE.json").write_text(
    json.dumps(provenance, ensure_ascii=False, indent=2),
    encoding="utf-8",
)

sheet = Image.new("RGB", (1320, 820), (20, 26, 32))
draw = ImageDraw.Draw(sheet)
draw.text(
    (24, 18),
    "REFERENCE MATERIAL REGIONS — DIRECT 1:1 CROPS",
    fill=(236, 240, 243),
)

positions = {
    "body_panel": (24, 72),
    "body_wear": (154, 72),
    "body_light_steel": (284, 72),
    "weapon_housing": (414, 72),
    "leg_armor": (570, 72),
    "copper_mechanics": (726, 72),
    "dark_mechanics": (882, 72),
    "blue_optic": (1038, 72),
}

for entry in entries:
    region_id = entry["id"]
    crop = Image.open(
        sample_root / entry["file"]
    ).convert("RGB")
    x, y = positions[region_id]
    sheet.paste(crop, (x, y))
    draw.rectangle(
        (x, y, x + crop.width - 1, y + crop.height - 1),
        outline=(126, 144, 158),
        width=2,
    )
    draw.text(
        (x, y - 20),
        region_id,
        fill=(208, 217, 224),
    )
    draw.text(
        (x, y + crop.height + 8),
        str(tuple(entry["box_xyxy"])),
        fill=(166, 181, 192),
    )

reference_preview = reference.copy()
preview_width = 820
preview_height = round(
    reference_preview.height *
    preview_width /
    reference_preview.width
)
reference_preview = reference_preview.resize(
    (preview_width, preview_height),
    Image.Resampling.LANCZOS,
)
sheet.paste(reference_preview, (24, 315))
draw.rectangle(
    (
        24,
        315,
        24 + reference_preview.width - 1,
        315 + reference_preview.height - 1,
    ),
    outline=(126, 144, 158),
    width=2,
)
draw.text(
    (870, 330),
    "The large image is review-only.",
    fill=(208, 217, 224),
)
draw.text(
    (870, 360),
    "Material inputs are the direct crops above.",
    fill=(208, 217, 224),
)
draw.text(
    (870, 400),
    "No derived PBR maps.",
    fill=(208, 217, 224),
)
sheet.save(
    render_root / "01_reference_material_region_provenance.png"
)
