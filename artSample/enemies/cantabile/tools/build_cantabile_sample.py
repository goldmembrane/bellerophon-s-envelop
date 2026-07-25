import io
import json
import math
import struct
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[4]
SAMPLE_DIR = ROOT / "artSample/enemies/cantabile"
SOURCE_GLB = ROOT / "Assets/_Project/Art/Enemies/Cantabile/Models/cantabille.glb"
REFERENCE_FRONT = ROOT / "image/cantabile(칸타빌레).png"
REFERENCE_SIDE = ROOT / "image/cantabile-beside.png"

RENDER_DIR = SAMPLE_DIR / "renders"
TEXTURE_DIR = SAMPLE_DIR / "textures"
EXPORT_DIR = SAMPLE_DIR / "exports"

ATLAS_PATH = TEXTURE_DIR / "cantabile_current_model_material_atlas_albedo.png"
WING_TEXTURE_PATH = TEXTURE_DIR / "cantabile_wing_blue_black_spots_albedo.png"
BODY_TEXTURE_PATH = TEXTURE_DIR / "cantabile_body_brown_fur_albedo.png"
EYE_LIMB_TEXTURE_PATH = TEXTURE_DIR / "cantabile_eye_limb_dark_gloss_albedo.png"
BUMP_TEXTURE_PATH = TEXTURE_DIR / "cantabile_fur_wing_vein_bump.png"
OUTPUT_GLB = EXPORT_DIR / "cantabile_current_model_colored_sample.glb"

COMPONENT_TYPES = {
    5120: (1, np.int8),
    5121: (1, np.uint8),
    5122: (2, np.int16),
    5123: (2, np.uint16),
    5125: (4, np.uint32),
    5126: (4, np.float32),
}

TYPE_COUNTS = {
    "SCALAR": 1,
    "VEC2": 2,
    "VEC3": 3,
    "VEC4": 4,
    "MAT4": 16,
}


def read_glb(path):
    data = path.read_bytes()
    magic, version, total_length = struct.unpack_from("<4sII", data, 0)
    if magic != b"glTF" or version != 2 or total_length != len(data):
        raise ValueError(f"Unsupported GLB header: {path}")

    offset = 12
    chunks = []
    while offset < len(data):
        chunk_length, chunk_type = struct.unpack_from("<I4s", data, offset)
        offset += 8
        chunks.append((chunk_type, data[offset:offset + chunk_length]))
        offset += chunk_length

    gltf_json = next(chunk for chunk_type, chunk in chunks if chunk_type == b"JSON")
    gltf_bin = next(chunk for chunk_type, chunk in chunks if chunk_type == b"BIN\x00")
    return json.loads(gltf_json.decode("utf-8").rstrip("\x00 \t\r\n")), gltf_bin


def accessor_array(gltf, bin_chunk, accessor_index):
    accessor = gltf["accessors"][accessor_index]
    buffer_view = gltf["bufferViews"][accessor["bufferView"]]
    component_size, dtype = COMPONENT_TYPES[accessor["componentType"]]
    component_count = TYPE_COUNTS[accessor["type"]]
    byte_offset = buffer_view.get("byteOffset", 0) + accessor.get("byteOffset", 0)
    byte_stride = buffer_view.get("byteStride", component_count * component_size)
    count = accessor["count"]
    packed_size = component_count * component_size

    output = np.empty((count, component_count), dtype=dtype)
    for i in range(count):
        start = byte_offset + i * byte_stride
        output[i] = np.frombuffer(bin_chunk[start:start + packed_size], dtype=dtype, count=component_count)
    return output


def lerp(a, b, t):
    return a * (1.0 - t) + b * t


def smoothstep(edge0, edge1, value):
    if edge0 == edge1:
        return 1.0 if value >= edge1 else 0.0
    x = max(0.0, min(1.0, (value - edge0) / (edge1 - edge0)))
    return x * x * (3.0 - 2.0 * x)


def classify_and_color(gltf, bin_chunk):
    primitive = gltf["meshes"][0]["primitives"][0]
    attributes = primitive["attributes"]
    positions = accessor_array(gltf, bin_chunk, attributes["POSITION"]).astype(np.float32)
    normals = accessor_array(gltf, bin_chunk, attributes["NORMAL"]).astype(np.float32)
    indices = accessor_array(gltf, bin_chunk, primitive["indices"]).reshape(-1).astype(np.int64)

    joint_sets = [accessor_array(gltf, bin_chunk, attributes[name]).astype(np.int32) for name in ("JOINTS_0", "JOINTS_1", "JOINTS_2")]
    weight_sets = [accessor_array(gltf, bin_chunk, attributes[name]).astype(np.float32) for name in ("WEIGHTS_0", "WEIGHTS_1", "WEIGHTS_2")]
    joints = np.concatenate(joint_sets, axis=1)
    weights = np.concatenate(weight_sets, axis=1)
    dominant_indices = weights.argmax(axis=1)
    dominant_joints = joints[np.arange(len(joints)), dominant_indices]

    min_pos = positions.min(axis=0)
    max_pos = positions.max(axis=0)
    size = np.maximum(max_pos - min_pos, 0.0001)
    normalized = (positions - min_pos) / size

    colors = np.zeros((len(positions), 4), dtype=np.float32)
    uvs = np.zeros((len(positions), 2), dtype=np.float32)
    wing_joints = set(range(31, 45))

    cyan = np.array([0.07, 0.80, 0.96], dtype=np.float32)
    sky = np.array([0.33, 0.92, 0.98], dtype=np.float32)
    blue = np.array([0.02, 0.38, 0.90], dtype=np.float32)
    deep_blue = np.array([0.018, 0.08, 0.28], dtype=np.float32)
    black = np.array([0.012, 0.013, 0.014], dtype=np.float32)
    white = np.array([0.94, 0.92, 0.84], dtype=np.float32)
    brown = np.array([0.42, 0.25, 0.13], dtype=np.float32)
    tan = np.array([0.74, 0.52, 0.34], dtype=np.float32)
    dark_brown = np.array([0.12, 0.08, 0.05], dtype=np.float32)
    eye_black = np.array([0.018, 0.021, 0.026], dtype=np.float32)
    limb_tip = np.array([0.08, 0.13, 0.18], dtype=np.float32)

    spot_centers = [
        (0.88, 0.91, 0.095),
        (0.74, 0.83, 0.070),
        (0.94, 0.70, 0.085),
        (0.93, 0.50, 0.080),
        (0.90, 0.28, 0.080),
        (0.76, 0.13, 0.070),
    ]

    for i, position in enumerate(positions):
        x, y, z = position
        nx, ny, nz = normalized[i]
        ax = abs(x)
        axn = max(0.0, min(1.0, (ax - 0.10) / 1.76))
        wing_by_joint = int(dominant_joints[i]) in wing_joints
        wing_by_shape = ax > 0.46 and z < 0.18
        is_wing = wing_by_joint or wing_by_shape

        if is_wing:
            inner = smoothstep(0.0, 0.48, axn)
            outer = smoothstep(0.70, 1.0, axn)
            vertical_glow = 1.0 - abs(ny - 0.48) * 1.10
            vertical_glow = max(0.0, min(1.0, vertical_glow))
            base = lerp(blue, sky, min(1.0, 0.18 + vertical_glow * 0.80))
            base = lerp(base, cyan, 0.28 + inner * 0.18)
            base = lerp(base, deep_blue, outer * 0.42)

            lower_edge = 1.0 - smoothstep(0.0, 0.09, ny)
            rim = max(smoothstep(0.81, 0.98, axn), smoothstep(0.89, 1.0, ny), lower_edge)
            color = lerp(base, black, rim * 0.86)

            root_y = 0.47 + 0.08 * (1.0 - axn)
            vein_strength = 0.0
            for slope in (-0.52, -0.28, -0.08, 0.18, 0.38):
                expected = root_y + slope * axn + 0.08 * math.sin(axn * math.pi)
                distance = abs(ny - expected)
                vein_strength = max(vein_strength, 1.0 - smoothstep(0.006, 0.023, distance))
            vein_strength = max(vein_strength, 1.0 - smoothstep(0.000, 0.018, abs(axn - 0.36)))
            color = lerp(color, deep_blue * 0.58, vein_strength * 0.72)

            scallop = 0.5 + 0.5 * math.sin((ny * 13.0 + axn * 2.1) * math.pi)
            if rim > 0.45 and scallop > 0.78:
                color = lerp(color, black * 0.65, 0.45)

            for sx, sy, radius in spot_centers:
                distance = math.sqrt(((axn - sx) / radius) ** 2 + ((ny - sy) / (radius * 0.72)) ** 2)
                if distance < 1.0:
                    color = lerp(color, white, 0.72 + (1.0 - distance) * 0.25)

            if axn < 0.18:
                color = lerp(color, black, (1.0 - axn / 0.18) * 0.38)

            atlas_u = 0.02 + axn * 0.46
            atlas_v = 0.52 + ny * 0.46
            if x < 0:
                atlas_u = 0.52 + (1.0 - axn) * 0.46
            uvs[i] = [atlas_u, atlas_v]
        else:
            fur_wave = 0.5 + 0.5 * math.sin(y * 22.0 + z * 17.0 + ax * 5.0)
            vertical = smoothstep(0.0, 1.0, ny)
            color = lerp(dark_brown, tan, 0.34 + vertical * 0.23 + fur_wave * 0.20)
            central_width = smoothstep(0.0, 0.34, 1.0 - ax / 0.34)
            color = lerp(color, brown, central_width * 0.48)

            if y > 1.18 and z > 0.18 and ax > 0.10:
                antenna_band = 0.5 + 0.5 * math.sin(ax * 44.0 + y * 18.0)
                color = lerp(dark_brown, tan, 0.40 + antenna_band * 0.28)

            left_eye = ((x + 0.15) / 0.14) ** 2 + ((y - 0.83) / 0.22) ** 2 + ((z - 0.32) / 0.12) ** 2
            right_eye = ((x - 0.15) / 0.14) ** 2 + ((y - 0.83) / 0.22) ** 2 + ((z - 0.32) / 0.12) ** 2
            eye_factor = max(0.0, 1.0 - min(left_eye, right_eye))
            if eye_factor > 0.0:
                highlight = max(0.0, normals[i][2]) * 0.20
                color = lerp(color, eye_black + highlight, min(0.98, eye_factor))

            if ax > 0.25 and y < 0.58 and z < 0.05:
                tip = smoothstep(0.28, 0.90, ax) * smoothstep(0.58, 0.0, y)
                color = lerp(color, limb_tip, tip * 0.72)

            crease = 0.5 + 0.5 * math.sin((ny * 10.0 + nz * 5.0) * math.pi)
            if ax < 0.36 and crease > 0.84:
                color = lerp(color, dark_brown, 0.28)

            body_u = 0.04 + max(0.0, min(1.0, (x + 0.36) / 0.72)) * 0.42
            body_v = 0.04 + ny * 0.42
            uvs[i] = [body_u, body_v]

        shade = 0.82 + max(0.0, normals[i][2]) * 0.16 + max(0.0, normals[i][1]) * 0.10
        colors[i, :3] = np.clip(color * shade, 0.0, 1.0)
        colors[i, 3] = 1.0

    return positions, indices, colors, uvs


def wing_color(u, v):
    cyan = np.array([35, 205, 238], dtype=np.float32)
    sky = np.array([96, 229, 246], dtype=np.float32)
    blue = np.array([7, 86, 214], dtype=np.float32)
    deep = np.array([4, 18, 52], dtype=np.float32)
    black = np.array([5, 6, 6], dtype=np.float32)
    white = np.array([238, 234, 216], dtype=np.float32)
    glow = max(0.0, min(1.0, 1.0 - abs(v - 0.48) * 1.15))
    color = lerp(blue, sky, 0.22 + glow * 0.70)
    color = lerp(color, cyan, 0.25)
    rim = max(smoothstep(0.80, 1.0, u), smoothstep(0.89, 1.0, v), 1.0 - smoothstep(0.0, 0.09, v))
    color = lerp(color, black, rim * 0.86)
    for slope in (-0.52, -0.28, -0.08, 0.18, 0.38):
        expected = 0.47 + slope * u + 0.08 * math.sin(u * math.pi)
        vein = 1.0 - smoothstep(0.004, 0.020, abs(v - expected))
        color = lerp(color, deep, max(0.0, vein) * 0.62)
    for sx, sy, radius in [(0.88, 0.91, 0.09), (0.74, 0.83, 0.07), (0.94, 0.70, 0.08), (0.92, 0.50, 0.075), (0.90, 0.28, 0.075)]:
        distance = math.sqrt(((u - sx) / radius) ** 2 + ((v - sy) / (radius * 0.72)) ** 2)
        if distance < 1.0:
            color = lerp(color, white, 0.72 + (1.0 - distance) * 0.25)
    return np.clip(color, 0, 255).astype(np.uint8)


def body_color(u, v):
    dark = np.array([47, 31, 18], dtype=np.float32)
    brown = np.array([116, 72, 38], dtype=np.float32)
    tan = np.array([184, 134, 82], dtype=np.float32)
    wave = 0.5 + 0.5 * math.sin(v * 55.0 + u * 29.0)
    color = lerp(dark, tan, 0.36 + v * 0.24 + wave * 0.18)
    center = 1.0 - min(1.0, abs(u - 0.5) * 2.0)
    color = lerp(color, brown, center * 0.42)
    if wave > 0.78:
        color = lerp(color, dark, 0.22)
    return np.clip(color, 0, 255).astype(np.uint8)


def create_textures():
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)

    wing = Image.new("RGB", (512, 512))
    body = Image.new("RGB", (512, 512))
    eye_limb = Image.new("RGB", (512, 512))
    bump = Image.new("RGB", (512, 512))

    wing_pixels = wing.load()
    body_pixels = body.load()
    eye_pixels = eye_limb.load()
    bump_pixels = bump.load()
    for y in range(512):
        v = 1.0 - y / 511.0
        for x in range(512):
            u = x / 511.0
            wing_pixels[x, y] = tuple(int(c) for c in wing_color(u, v))
            body_pixels[x, y] = tuple(int(c) for c in body_color(u, v))
            eye_band = int(16 + 42 * (0.5 + 0.5 * math.sin(u * 40.0 + v * 22.0)))
            if v > 0.58:
                eye_pixels[x, y] = (eye_band, eye_band + 4, eye_band + 8)
            else:
                limb = int(26 + 54 * v + 16 * math.sin(u * 28.0))
                eye_pixels[x, y] = (limb, max(0, limb - 8), max(0, limb - 15))
            vein = int(120 + 75 * (0.5 + 0.5 * math.sin(u * 65.0 + v * 39.0)))
            fur = int(100 + 65 * (0.5 + 0.5 * math.sin(u * 33.0) * math.sin(v * 49.0)))
            bump_pixels[x, y] = (vein if u > 0.5 else fur, vein if u > 0.5 else fur, 255)

    wing.save(WING_TEXTURE_PATH)
    body.save(BODY_TEXTURE_PATH)
    eye_limb.save(EYE_LIMB_TEXTURE_PATH)
    bump.save(BUMP_TEXTURE_PATH)

    atlas = Image.new("RGB", (1024, 1024), (8, 8, 8))
    atlas.paste(body.resize((512, 512), Image.Resampling.BICUBIC), (0, 512))
    atlas.paste(eye_limb.resize((512, 512), Image.Resampling.BICUBIC), (512, 512))
    atlas.paste(wing.resize((512, 512), Image.Resampling.BICUBIC), (0, 0))
    atlas.paste(wing.transpose(Image.Transpose.FLIP_LEFT_RIGHT).resize((512, 512), Image.Resampling.BICUBIC), (512, 0))
    atlas.save(ATLAS_PATH)
    return atlas


def write_colorized_glb(gltf, bin_chunk, colors, uvs, atlas):
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    gltf = json.loads(json.dumps(gltf))
    primitive = gltf["meshes"][0]["primitives"][0]

    png_buffer = io.BytesIO()
    atlas.save(png_buffer, format="PNG")
    png_bytes = png_buffer.getvalue()
    uv_bytes = uvs.astype("<f4").tobytes()
    color_bytes = colors.astype("<f4").tobytes()

    new_bin = bytearray(bin_chunk)

    def append_blob(blob):
        while len(new_bin) % 4:
            new_bin.append(0)
        start = len(new_bin)
        new_bin.extend(blob)
        while len(new_bin) % 4:
            new_bin.append(0)
        return start, len(blob)

    uv_offset, uv_length = append_blob(uv_bytes)
    color_offset, color_length = append_blob(color_bytes)
    image_offset, image_length = append_blob(png_bytes)

    uv_view = len(gltf["bufferViews"])
    gltf["bufferViews"].append({"buffer": 0, "byteOffset": uv_offset, "byteLength": uv_length, "target": 34962})
    color_view = len(gltf["bufferViews"])
    gltf["bufferViews"].append({"buffer": 0, "byteOffset": color_offset, "byteLength": color_length, "target": 34962})
    image_view = len(gltf["bufferViews"])
    gltf["bufferViews"].append({"buffer": 0, "byteOffset": image_offset, "byteLength": image_length})

    uv_accessor = len(gltf["accessors"])
    gltf["accessors"].append({
        "bufferView": uv_view,
        "byteOffset": 0,
        "componentType": 5126,
        "count": len(uvs),
        "type": "VEC2",
        "min": [0.0, 0.0],
        "max": [1.0, 1.0],
    })
    color_accessor = len(gltf["accessors"])
    gltf["accessors"].append({
        "bufferView": color_view,
        "byteOffset": 0,
        "componentType": 5126,
        "count": len(colors),
        "type": "VEC4",
        "min": [0.0, 0.0, 0.0, 1.0],
        "max": [1.0, 1.0, 1.0, 1.0],
    })

    primitive["attributes"]["TEXCOORD_0"] = uv_accessor
    primitive["attributes"]["COLOR_0"] = color_accessor
    gltf["images"] = [{"name": "cantabile_current_model_material_atlas_albedo", "bufferView": image_view, "mimeType": "image/png"}]
    gltf["textures"] = [{"source": 0}]
    gltf["materials"] = [{
        "name": "M_Cantabile_CurrentModel_Sample_Albedo",
        "doubleSided": True,
        "pbrMetallicRoughness": {
            "baseColorTexture": {"index": 0},
            "baseColorFactor": [1.0, 1.0, 1.0, 1.0],
            "metallicFactor": 0.0,
            "roughnessFactor": 0.72,
        },
    }]
    primitive["material"] = 0
    gltf["buffers"][0]["byteLength"] = len(new_bin)

    json_bytes = json.dumps(gltf, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    while len(json_bytes) % 4:
        json_bytes += b" "
    total_length = 12 + 8 + len(json_bytes) + 8 + len(new_bin)
    out = bytearray()
    out.extend(struct.pack("<4sII", b"glTF", 2, total_length))
    out.extend(struct.pack("<I4s", len(json_bytes), b"JSON"))
    out.extend(json_bytes)
    out.extend(struct.pack("<I4s", len(new_bin), b"BIN\x00"))
    out.extend(new_bin)
    OUTPUT_GLB.write_bytes(out)


def render_preview(positions, indices, colors, output_path, yaw_degrees, zoom=1.0):
    yaw = math.radians(yaw_degrees)
    cos_y = math.cos(yaw)
    sin_y = math.sin(yaw)
    x = positions[:, 0] * cos_y + positions[:, 2] * sin_y
    y = positions[:, 1]
    depth = -positions[:, 0] * sin_y + positions[:, 2] * cos_y
    projected = np.stack([x, y, depth], axis=1)

    width, height = 1600, 900
    scale_factor = 2
    canvas = Image.new("RGB", (width * scale_factor, height * scale_factor), (248, 248, 246))
    draw = ImageDraw.Draw(canvas)
    min_xy = projected[:, :2].min(axis=0)
    max_xy = projected[:, :2].max(axis=0)
    size = np.maximum(max_xy - min_xy, 0.001)
    margin = 90 * scale_factor
    scale = min((width * scale_factor - margin * 2) / size[0], (height * scale_factor - margin * 2) / size[1]) * zoom
    screen = np.empty((len(projected), 2), dtype=np.float32)
    screen[:, 0] = (projected[:, 0] - (min_xy[0] + max_xy[0]) * 0.5) * scale + width * scale_factor * 0.5
    screen[:, 1] = height * scale_factor - ((projected[:, 1] - min_xy[1]) * scale + margin)

    triangles = indices.reshape(-1, 3)
    order = np.argsort(projected[triangles, 2].mean(axis=1))
    for tri_index in order:
        tri = triangles[tri_index]
        pts = [(float(screen[v, 0]), float(screen[v, 1])) for v in tri]
        col = np.clip(colors[tri, :3].mean(axis=0), 0.0, 1.0)
        rgb = tuple(int(round(c * 255)) for c in col)
        draw.polygon(pts, fill=rgb)

    canvas.resize((width, height), Image.Resampling.LANCZOS).save(output_path)


def fit_image(source, size):
    image = Image.open(source).convert("RGB")
    image.thumbnail(size, Image.Resampling.LANCZOS)
    out = Image.new("RGB", size, (248, 248, 246))
    out.paste(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    return out


def create_breakdown_images():
    cell = (800, 450)
    front = fit_image(REFERENCE_FRONT, cell)
    generated_front = fit_image(RENDER_DIR / "01_front_cantabile_reference_match.png", cell)
    side = fit_image(REFERENCE_SIDE, cell)
    generated_side = fit_image(RENDER_DIR / "02_side_cantabile_beside_reference_match.png", cell)
    comparison = Image.new("RGB", (cell[0] * 2, cell[1] * 2), (238, 238, 235))
    comparison.paste(front, (0, 0))
    comparison.paste(generated_front, (cell[0], 0))
    comparison.paste(side, (0, cell[1]))
    comparison.paste(generated_side, (cell[0], cell[1]))
    comparison.save(RENDER_DIR / "03_reference_side_by_side_overview.png")

    atlas = Image.open(ATLAS_PATH).convert("RGB")
    overview = Image.new("RGB", (1600, 900), (248, 248, 246))
    overview.paste(fit_image(ATLAS_PATH, (760, 760)), (40, 70))
    overview.paste(fit_image(WING_TEXTURE_PATH, (360, 360)), (860, 70))
    overview.paste(fit_image(BODY_TEXTURE_PATH, (360, 360)), (1220, 70))
    overview.paste(fit_image(EYE_LIMB_TEXTURE_PATH, (360, 360)), (860, 470))
    overview.paste(fit_image(BUMP_TEXTURE_PATH, (360, 360)), (1220, 470))
    overview.save(RENDER_DIR / "06_texture_atlas_and_material_breakdown.png")


def main():
    RENDER_DIR.mkdir(parents=True, exist_ok=True)
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)

    gltf, bin_chunk = read_glb(SOURCE_GLB)
    positions, indices, colors, uvs = classify_and_color(gltf, bin_chunk)
    atlas = create_textures()
    write_colorized_glb(gltf, bin_chunk, colors, uvs, atlas)

    render_preview(positions, indices, colors, RENDER_DIR / "01_front_cantabile_reference_match.png", yaw_degrees=0)
    render_preview(positions, indices, colors, RENDER_DIR / "02_side_cantabile_beside_reference_match.png", yaw_degrees=-34)
    render_preview(positions, indices, colors, RENDER_DIR / "04_three_quarter_current_model_material.png", yaw_degrees=34)
    render_preview(positions, indices, colors, RENDER_DIR / "05_close_current_model_color_application.png", yaw_degrees=-16, zoom=1.35)
    create_breakdown_images()

    print(f"Generated {OUTPUT_GLB}")
    print(f"Generated {RENDER_DIR}")
    print(f"Generated {TEXTURE_DIR}")


if __name__ == "__main__":
    main()
