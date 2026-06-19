import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
MODEL_DIR = ROOT / "model"
RENDER_DIR = ROOT / "renders"


def ensure_dirs():
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    RENDER_DIR.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_mat(name, color, roughness=0.45, metallic=0.0, alpha=1.0, emission=None, emission_strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Alpha"].default_value = alpha
        if emission is not None:
            bsdf.inputs["Emission Color"].default_value = emission
            bsdf.inputs["Emission Strength"].default_value = emission_strength
    if alpha < 1.0:
        mat.blend_method = "BLEND"
        mat.use_screen_refraction = True
        mat.show_transparent_back = True
    return mat


def set_material_emission(mat, color, strength):
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Emission Color"].default_value = color
        bsdf.inputs["Emission Strength"].default_value = strength
        bsdf.inputs["Base Color"].default_value = color


def assign(obj, mat):
    obj.data.materials.append(mat)
    return obj


def cylinder(name, radius, depth, loc, vertices=96, mat=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.object
    obj.name = name
    if mat:
        assign(obj, mat)
    return obj


def torus(name, major_radius, minor_radius, loc, mat=None):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=96,
        minor_segments=16,
        major_radius=major_radius,
        minor_radius=minor_radius,
        location=loc,
    )
    obj = bpy.context.object
    obj.name = name
    if mat:
        assign(obj, mat)
    return obj


def cube(name, scale, loc, mat=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if mat:
        assign(obj, mat)
    return obj


def uv_sphere(name, radius, loc, scale=(1.0, 1.0, 1.0), mat=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=96, ring_count=32, radius=radius, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if mat:
        assign(obj, mat)
    return obj


def add_label(text, loc, rot, size, mat):
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = "ENGRAVED_WARNING_LABEL"
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.004
    assign(obj, mat)
    return obj


def shade_smooth(*objects):
    for obj in objects:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.shade_smooth()
        obj.select_set(False)


def create_warning_light():
    metal = make_mat("dark gunmetal", (0.05, 0.055, 0.055, 1), roughness=0.36, metallic=0.75)
    edge = make_mat("worn steel edges", (0.45, 0.44, 0.40, 1), roughness=0.3, metallic=0.8)
    pale_ring = make_mat("pale mounting ring", (0.78, 0.76, 0.70, 1), roughness=0.42, metallic=0.35)
    rubber = make_mat("black rubber gasket", (0.01, 0.01, 0.012, 1), roughness=0.75, metallic=0.0)
    red_lens = make_mat(
        "transparent red lens",
        (1.0, 0.03, 0.015, 0.42),
        roughness=0.08,
        metallic=0.0,
        alpha=0.42,
        emission=(1.0, 0.02, 0.01, 1),
        emission_strength=0.3,
    )
    red_core = make_mat(
        "resonance red emitter",
        (1.0, 0.05, 0.015, 1),
        roughness=0.12,
        metallic=0.0,
        emission=(1.0, 0.03, 0.01, 1),
        emission_strength=3.4,
    )
    glow = make_mat(
        "soft red warning glow",
        (1.0, 0.02, 0.01, 0.18),
        roughness=0.0,
        alpha=0.18,
        emission=(1.0, 0.02, 0.01, 1),
        emission_strength=1.2,
    )
    highlight = make_mat(
        "hot lens highlight",
        (1.0, 0.55, 0.46, 1),
        roughness=0.05,
        emission=(1.0, 0.18, 0.12, 1),
        emission_strength=2.2,
    )

    ceiling_plate = cylinder("CEILING_BLACK_BACKING_DISC_against_ceiling", 0.64, 0.08, (0, 0, 0.78), mat=metal)
    rear_cap = cylinder("BLACK_CYLINDRICAL_BASE_reference_shape", 0.46, 0.30, (0, 0, 0.60), mat=metal)
    rear_gasket = cylinder("BLACK_REAR_RUBBER_GASKET", 0.50, 0.05, (0, 0, 0.735), mat=rubber)
    pale_band = cylinder("PALE_RING_BETWEEN_BASE_AND_LENS", 0.48, 0.045, (0, 0, 0.445), mat=pale_ring)
    black_lip = cylinder("BLACK_LENS_SOCKET_LIP", 0.41, 0.08, (0, 0, 0.395), mat=rubber)

    lens_body = cylinder("RED_TRANSPARENT_CYLINDRICAL_LENS_BODY", 0.34, 0.40, (0, 0, 0.185), vertices=96, mat=red_lens)
    lens_dome = uv_sphere("RED_TRANSPARENT_ROUNDED_DOME_END", 0.34, (0, 0, -0.015), scale=(1.0, 1.0, 0.72), mat=red_lens)
    emitter = uv_sphere("INNER_GLOWING_BULB_VISIBLE_THROUGH_LENS", 0.16, (0, 0, 0.18), scale=(1.0, 1.0, 1.0), mat=red_core)
    highlight_spot = uv_sphere("SMALL_BRIGHT_SPECULAR_HIGHLIGHT_ON_LENS", 0.035, (0.11, -0.27, 0.03), scale=(1.0, 1.0, 0.6), mat=highlight)
    glow_cone = cylinder("TRANSPARENT_RED_DOWNWARD_GLOW_VOLUME", 0.38, 0.62, (0, 0, -0.22), vertices=96, mat=glow)

    base_ridges = [
        torus("BLACK_BASE_RIB_01", 0.46, 0.009, (0, 0, 0.705), mat=rubber),
        torus("BLACK_BASE_RIB_02", 0.46, 0.008, (0, 0, 0.645), mat=rubber),
        torus("BLACK_BASE_RIB_03", 0.46, 0.008, (0, 0, 0.545), mat=rubber),
    ]
    red_lens_ridges = [
        torus("RED_LENS_RIB_01", 0.343, 0.006, (0, 0, 0.285), mat=red_lens),
        torus("RED_LENS_RIB_02", 0.343, 0.006, (0, 0, 0.075), mat=red_lens),
    ]

    bolts = []
    for idx, angle in enumerate((45, 135, 225, 315), start=1):
        rad = math.radians(angle)
        x = math.cos(rad) * 0.55
        y = math.sin(rad) * 0.55
        bolt = cylinder(f"VISIBLE_CEILING_BOLT_{idx:02d}", 0.045, 0.035, (x, y, 0.835), vertices=24, mat=edge)
        bolts.append(bolt)

    shade_smooth(
        ceiling_plate,
        rear_cap,
        rear_gasket,
        pale_band,
        black_lip,
        lens_body,
        lens_dome,
        emitter,
        highlight_spot,
        glow_cone,
        *base_ridges,
        *red_lens_ridges,
        *bolts,
    )
    return [
        ceiling_plate,
        rear_cap,
        rear_gasket,
        pale_band,
        black_lip,
        lens_body,
        lens_dome,
        emitter,
        highlight_spot,
        glow_cone,
        *base_ridges,
        *red_lens_ridges,
        *bolts,
    ]


def set_resonance_state(active):
    red_lens = bpy.data.materials.get("transparent red lens")
    red_core = bpy.data.materials.get("resonance red emitter")
    glow = bpy.data.materials.get("soft red warning glow")
    spill = bpy.data.objects.get("red practical spill")
    glow_obj = bpy.data.objects.get("TRANSPARENT_RED_DOWNWARD_GLOW_VOLUME")

    if active:
        if red_lens:
            set_material_emission(red_lens, (1.0, 0.03, 0.01, 0.55), 1.6)
        if red_core:
            set_material_emission(red_core, (1.0, 0.03, 0.01, 1), 6.0)
        if glow:
            set_material_emission(glow, (1.0, 0.02, 0.01, 0.28), 2.1)
        if spill:
            spill.data.energy = 240
        if glow_obj:
            glow_obj.hide_render = False
    else:
        if red_lens:
            set_material_emission(red_lens, (0.12, 0.018, 0.015, 0.38), 0.0)
        if red_core:
            set_material_emission(red_core, (0.08, 0.008, 0.006, 1), 0.0)
        if glow:
            set_material_emission(glow, (0.08, 0.006, 0.004, 0.0), 0.0)
        if spill:
            spill.data.energy = 0
        if glow_obj:
            glow_obj.hide_render = True


def add_ceiling_context():
    mat = make_mat("sample ceiling dark panel", (0.12, 0.13, 0.13, 1), roughness=0.6, metallic=0.1)
    panel = cube("SAMPLE_ENGINE_ROOM_CEILING_CONTEXT_PANEL", (2.1, 2.1, 0.035), (0, 0, 0.76), mat=mat)
    return panel


def setup_lighting():
    bpy.ops.object.light_add(type="AREA", location=(0, -3.0, 2.5))
    key = bpy.context.object
    key.name = "large softbox key"
    key.data.energy = 450
    key.data.size = 4.0

    bpy.ops.object.light_add(type="POINT", location=(-1.2, 1.4, 1.2))
    fill = bpy.context.object
    fill.name = "red practical spill"
    fill.data.color = (1.0, 0.08, 0.03)
    fill.data.energy = 160

    bpy.context.scene.world.color = (0.015, 0.018, 0.021)


def setup_camera(name, loc, rot, focal=60):
    bpy.ops.object.camera_add(location=loc, rotation=rot)
    camera = bpy.context.object
    camera.name = name
    camera.data.lens = focal
    bpy.context.scene.camera = camera
    return camera


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render(path, camera_loc, target=(0, 0, 0.32), focal=65):
    camera = setup_camera("render_camera", camera_loc, (0, 0, 0), focal=focal)
    look_at(camera, target)
    bpy.context.scene.camera = camera
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)


def export_models():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(filepath=str(MODEL_DIR / "cantabile_warning_light.glb"), export_format="GLB")
    bpy.ops.export_scene.fbx(filepath=str(MODEL_DIR / "cantabile_warning_light.fbx"), use_selection=False)


def create_preview_grid():
    try:
        from PIL import Image, ImageDraw
    except Exception:
        return

    images = [
        ("01 front ceiling mount", RENDER_DIR / "01_front_ceiling_mount.png"),
        ("02 red lens detail", RENDER_DIR / "02_red_lens_detail.png"),
        ("03 black base side", RENDER_DIR / "03_black_base_side.png"),
        ("04 ceiling attachment", RENDER_DIR / "04_ceiling_attachment.png"),
        ("05 resonance active glow", RENDER_DIR / "05_resonance_active_glow.png"),
        ("06 scale context", RENDER_DIR / "06_scale_context.png"),
    ]
    loaded = []
    for label, path in images:
        if path.exists():
            img = Image.open(path).convert("RGB").resize((420, 420))
            loaded.append((label, img))
    if not loaded:
        return

    columns = 3
    rows = math.ceil(len(loaded) / columns)
    canvas = Image.new("RGB", (420 * columns, 474 * rows), (18, 20, 22))
    draw = ImageDraw.Draw(canvas)
    for index, (label, img) in enumerate(loaded):
        x = (index % columns) * 420
        y = (index // columns) * 474
        canvas.paste(img, (x, y))
        draw.text((x + 14, y + 432), label, fill=(230, 226, 216))
    canvas.save(RENDER_DIR / "preview_grid.png")


def create_state_comparison():
    try:
        from PIL import Image, ImageDraw
    except Exception:
        return

    off_path = RENDER_DIR / "off_state.png"
    active_path = RENDER_DIR / "resonance_active_state.png"
    if not off_path.exists() or not active_path.exists():
        return

    off = Image.open(off_path).convert("RGB").resize((720, 720))
    active = Image.open(active_path).convert("RGB").resize((720, 720))
    canvas = Image.new("RGB", (1440, 800), (18, 20, 22))
    canvas.paste(off, (0, 0))
    canvas.paste(active, (720, 0))
    draw = ImageDraw.Draw(canvas)
    draw.text((28, 742), "평상시: 꺼짐", fill=(230, 226, 216))
    draw.text((748, 742), "칸타빌레 공명 중: 붉은 경고등 활성", fill=(255, 140, 120))
    canvas.save(RENDER_DIR / "state_comparison.png")


def create_index_html():
    html = """<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>engine_room_cantabile_warning_light review</title>
  <style>
    body { margin: 0; background: #151817; color: #e8e1d2; font-family: Arial, sans-serif; }
    main { max-width: 1280px; margin: 0 auto; padding: 24px; }
    h1 { margin: 0 0 8px; font-size: 28px; }
    p { color: #c8c0af; line-height: 1.55; }
    .primary { border: 1px solid #3e453f; background: #202521; padding: 10px; margin: 18px 0; }
    .primary img { width: 100%; display: block; background: #0c0f0e; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
    figure { margin: 0; border: 1px solid #3e453f; background: #202521; padding: 10px; }
    img { width: 100%; display: block; background: #0c0f0e; }
    figcaption { margin-top: 8px; color: #d9cfba; font-size: 14px; }
    @media (max-width: 800px) { .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
<main>
  <h1>engine_room_cantabile_warning_light</h1>
  <p>ER-20 칸타빌레 공명 상태를 알리는 천장 경고등 승인용 샘플입니다. 첨부 이미지의 검은 원통형 베이스와 붉은 투명 돔 렌즈 형태를 천장 부착 방향으로 재구성했습니다. 동력실 내부 원통의 빛 강화 이펙트와 함께 쓰이며, 기존 동력실과 조종실 오브젝트는 수정하지 않았습니다.</p>
  <section class="primary" aria-label="천장 부착 경고등 대표 미리보기">
    <img src="renders/01_front_ceiling_mount.png" alt="천장 부착 경고등 전체">
  </section>
  <section class="grid">
    <figure><a href="renders/01_front_ceiling_mount.png"><img src="renders/01_front_ceiling_mount.png" alt="01 천장 부착 경고등 전체"></a><figcaption>01 천장 부착 경고등 전체</figcaption></figure>
    <figure><a href="renders/02_red_lens_detail.png"><img src="renders/02_red_lens_detail.png" alt="02 붉은 투명 돔 렌즈 세부"></a><figcaption>02 붉은 투명 돔 렌즈 세부</figcaption></figure>
    <figure><a href="renders/03_black_base_side.png"><img src="renders/03_black_base_side.png" alt="03 검은 원통 베이스 측면"></a><figcaption>03 검은 원통 베이스 측면</figcaption></figure>
    <figure><a href="renders/04_ceiling_attachment.png"><img src="renders/04_ceiling_attachment.png" alt="04 천장 부착면과 고정 볼트"></a><figcaption>04 천장 부착면과 고정 볼트</figcaption></figure>
    <figure><a href="renders/05_resonance_active_glow.png"><img src="renders/05_resonance_active_glow.png" alt="05 공명 활성 붉은 경고광"></a><figcaption>05 공명 활성 붉은 경고광</figcaption></figure>
    <figure><a href="renders/06_scale_context.png"><img src="renders/06_scale_context.png" alt="06 천장 패널 기준 축척"></a><figcaption>06 천장 패널 기준 축척</figcaption></figure>
  </section>
</main>
</body>
</html>
"""
    (ROOT / "index.html").write_text(html, encoding="utf-8")


def configure_render():
    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 96
    bpy.context.scene.render.resolution_x = 1200
    bpy.context.scene.render.resolution_y = 1200
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.eevee.taa_render_samples = 64


def main():
    ensure_dirs()
    clear_scene()
    create_warning_light()
    add_ceiling_context()
    setup_lighting()
    configure_render()
    export_models()
    set_resonance_state(False)
    render(RENDER_DIR / "01_front_ceiling_mount.png", (0, -2.55, 0.52), target=(0, 0, 0.34), focal=68)
    render(RENDER_DIR / "02_red_lens_detail.png", (0.36, -1.05, 0.08), target=(0, 0, 0.08), focal=105)
    render(RENDER_DIR / "03_black_base_side.png", (2.15, -0.55, 0.55), target=(0, 0, 0.48), focal=82)
    render(RENDER_DIR / "04_ceiling_attachment.png", (0.08, -0.2, 2.0), target=(0, 0, 0.72), focal=82)
    set_resonance_state(True)
    render(RENDER_DIR / "05_resonance_active_glow.png", (0, -2.55, 0.46), target=(0, 0, 0.2), focal=68)
    render(RENDER_DIR / "06_scale_context.png", (1.45, -1.8, 1.45), target=(0, 0, 0.48), focal=72)
    create_state_comparison()
    create_preview_grid()
    create_index_html()


if __name__ == "__main__":
    main()
