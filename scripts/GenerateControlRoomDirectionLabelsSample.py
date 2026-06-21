import json
import math
import os
from pathlib import Path

import bpy


SCRIPT_PATH = Path(__file__).resolve()
PROJECT_ROOT = SCRIPT_PATH.parents[1]
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "control_room_direction_labels"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
BLEND_DIR = SAMPLE_ROOT / "blender"

for directory in (SAMPLE_ROOT, RENDER_DIR, EXPORT_DIR, BLEND_DIR):
    directory.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def set_units():
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def make_mat(name, color, metallic=0.0, roughness=0.55, emission=None, strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if emission:
            bsdf.inputs["Emission Color"].default_value = emission
            bsdf.inputs["Emission Strength"].default_value = strength
    return mat


def assign_mat(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def add_box(name, loc, scale, mat, rot=(0.0, 0.0, 0.0), bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign_mat(obj, mat)
    if bevel > 0:
        modifier = obj.modifiers.new(f"{name}_bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        modifier.affect = "EDGES"
        obj.modifiers.new(f"{name}_weighted_normals", "WEIGHTED_NORMAL")
    return obj


def add_text(name, text, loc, size, mat, align="CENTER", rot=(math.radians(90), 0.0, 0.0)):
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.body = text
    obj.data.align_x = align
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.006
    obj.data.resolution_u = 12
    assign_mat(obj, mat)
    return obj


def add_triangle(name, points, mat):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(points, [], [(0, 1, 2)])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign_mat(obj, mat)
    return obj


def add_floor_arrow(name, center, angle, color_mat, length=0.72, width=0.18):
    x, y, z = center
    shaft_len = length * 0.58
    add_box(
        f"{name}_shaft",
        (x - math.cos(angle) * length * 0.12, y - math.sin(angle) * length * 0.12, z),
        (shaft_len, width, 0.028),
        color_mat,
        rot=(0.0, 0.0, angle),
        bevel=0.015,
    )
    tip_x = x + math.cos(angle) * length * 0.36
    tip_y = y + math.sin(angle) * length * 0.36
    nx = -math.sin(angle)
    ny = math.cos(angle)
    bx = tip_x - math.cos(angle) * width * 1.85
    by = tip_y - math.sin(angle) * width * 1.85
    points = [
        (tip_x, tip_y, z + 0.02),
        (bx + nx * width * 1.45, by + ny * width * 1.45, z + 0.02),
        (bx - nx * width * 1.45, by - ny * width * 1.45, z + 0.02),
    ]
    return add_triangle(f"{name}_tip", points, color_mat)


def add_label_panel(code, main_text, sub_text, loc, color_mat, text_mat, arrow_angle, panel_width=1.22):
    x, y, z = loc
    panel = add_box(
        f"{code}_wall_label_panel",
        (x, y, z),
        (panel_width, 0.08, 0.44),
        mats["dark_panel"],
        bevel=0.035,
    )
    add_box(f"{code}_label_top_trim", (x, y - 0.047, z + 0.245), (panel_width + 0.08, 0.025, 0.035), color_mat, bevel=0.012)
    add_box(f"{code}_label_bottom_trim", (x, y - 0.047, z - 0.245), (panel_width + 0.08, 0.025, 0.035), color_mat, bevel=0.012)
    add_box(f"{code}_label_left_bracket", (x - panel_width / 2 - 0.065, y - 0.045, z), (0.05, 0.035, 0.38), mats["bracket"], bevel=0.012)
    add_box(f"{code}_label_right_bracket", (x + panel_width / 2 + 0.065, y - 0.045, z), (0.05, 0.035, 0.38), mats["bracket"], bevel=0.012)
    for sx in (-1, 1):
        for sz in (-1, 1):
            add_box(
                f"{code}_bolt_{sx}_{sz}",
                (x + sx * (panel_width / 2 - 0.08), y - 0.091, z + sz * 0.16),
                (0.045, 0.018, 0.045),
                mats["bolt"],
                bevel=0.018,
            )
    main_size = 0.13 if len(main_text) <= 7 else 0.095
    add_text(f"{code}_label_main_en", main_text, (x, y - 0.095, z + 0.075), main_size, text_mat)
    add_text(f"{code}_label_sub_ko", sub_text, (x, y - 0.098, z - 0.112), 0.064, mats["muted_text"])
    add_floor_arrow(f"{code}_floor_arrow", (x, y - 0.62, 0.05), arrow_angle, color_mat)
    return panel


def add_wall_context():
    floor_center_y = -1.02
    add_box("control_room_floor_reference", (0, floor_center_y, -0.035), (8.7, 7.8, 0.07), mats["floor"], bevel=0.02)
    add_box("north_wall_context", (0, 2.86, 1.1), (8.8, 0.16, 2.2), mats["wall"], bevel=0.02)
    add_box("east_wall_context", (4.4, -1.02, 1.1), (0.16, 7.9, 2.2), mats["wall"], bevel=0.02)
    add_box("west_wall_upper_context", (-4.4, 1.55, 1.1), (0.16, 2.65, 2.2), mats["wall"], bevel=0.02)
    add_box("west_wall_lower_context", (-4.4, -3.42, 1.1), (0.16, 2.52, 2.2), mats["wall"], bevel=0.02)
    add_box("south_wall_left_context", (-2.55, -4.95, 1.1), (3.3, 0.16, 2.2), mats["wall"], bevel=0.02)
    add_box("south_wall_right_context", (2.75, -4.95, 1.1), (3.0, 0.16, 2.2), mats["wall"], bevel=0.02)
    add_box("inner_partition_reference", (0, 1.06, 1.0), (5.55, 0.12, 2.0), mats["partition"], bevel=0.025)
    add_box("inner_partition_door_cutout_hint", (-1.55, 1.0, 1.06), (1.05, 0.16, 1.82), mats["door_dark"], bevel=0.018)

    add_box("cockpit_corridor_stub", (-4.93, -0.92, 0.02), (1.22, 0.82, 0.04), mats["corridor_floor"], rot=(0, 0, math.radians(40)), bevel=0.02)
    add_box("engine_corridor_stub", (-4.88, -3.0, 0.02), (1.38, 0.72, 0.04), mats["corridor_floor"], bevel=0.02)
    add_box("cargo_corridor_stub", (-0.72, -5.34, 0.02), (0.88, 1.1, 0.04), mats["corridor_floor"], bevel=0.02)
    add_box("armory_corridor_stub", (0.66, -5.34, 0.02), (0.88, 1.1, 0.04), mats["corridor_floor"], bevel=0.02)

    add_text("context_title", "CR-17 CONTROL ROOM DIRECTION LABELS", (0, 3.15, 2.55), 0.145, mats["white_text"])
    add_text("context_note", "English primary labels with Korean support text for localization-ready signage", (0, 3.12, 2.25), 0.067, mats["muted_text"])


def add_measure_tags():
    add_box("left_pair_spacing_marker", (-4.05, -1.96, 0.035), (0.06, 1.24, 0.035), mats["spacing_marker"], bevel=0.01)
    add_text("left_pair_spacing_text", "LEFT CORRIDORS SEPARATED", (-3.82, -1.96, 0.18), 0.052, mats["muted_text"], align="LEFT", rot=(math.radians(90), 0, math.radians(90)))
    add_box("south_pair_adjacency_marker", (-0.03, -4.55, 0.04), (1.62, 0.055, 0.035), mats["spacing_marker"], bevel=0.01)
    add_text("south_pair_adjacency_text", "SOUTH CORRIDORS ADJACENT", (-0.03, -4.42, 0.18), 0.052, mats["muted_text"])


def add_labels():
    add_label_panel("CR17_COCKPIT", "COCKPIT", "조종실", (-3.7, -0.92, 1.36), mats["cockpit_blue"], mats["bright_text"], math.radians(140), panel_width=1.15)
    add_label_panel("CR17_ENGINE", "ENGINE ROOM", "동력실", (-3.7, -3.0, 1.36), mats["engine_amber"], mats["bright_text"], math.radians(180), panel_width=1.15)
    add_label_panel("CR17_CARGO", "CARGO HOLD", "이송창고", (-0.72, -4.46, 1.3), mats["cargo_green"], mats["bright_text"], math.radians(270), panel_width=1.2)
    add_label_panel("CR17_ARMORY", "ARMORY", "무기실", (0.72, -4.46, 1.3), mats["armory_red"], mats["bright_text"], math.radians(270), panel_width=1.2)


def add_lighting():
    bpy.ops.object.light_add(type="AREA", location=(0, -2.8, 6.4))
    light = bpy.context.object
    light.name = "large_softbox_over_control_room"
    light.data.energy = 620
    light.data.size = 6.5
    bpy.ops.object.light_add(type="POINT", location=(-3.7, -2.0, 2.0))
    left = bpy.context.object
    left.name = "left_label_glow_support"
    left.data.color = (0.55, 0.68, 1.0)
    left.data.energy = 90
    bpy.ops.object.light_add(type="POINT", location=(0.0, -4.35, 2.0))
    south = bpy.context.object
    south.name = "south_label_glow_support"
    south.data.color = (0.9, 0.75, 0.48)
    south.data.energy = 105


def setup_render():
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 64
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.render.resolution_x = 1800
    scene.render.resolution_y = 1200
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.color = (0.015, 0.017, 0.02)


def look_at(obj, target):
    dx = target[0] - obj.location.x
    dy = target[1] - obj.location.y
    dz = target[2] - obj.location.z
    direction = mathutils.Vector((dx, dy, dz))
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def create_camera(name, loc, target, lens=38):
    bpy.ops.object.camera_add(location=loc)
    camera = bpy.context.object
    camera.name = name
    direction = mathutils.Vector((target[0] - loc[0], target[1] - loc[1], target[2] - loc[2]))
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = lens
    camera.data.dof.use_dof = False
    return camera


def render_camera(filename, loc, target, lens=38):
    camera = create_camera(f"camera_{Path(filename).stem}", loc, target, lens)
    bpy.context.scene.camera = camera
    bpy.context.scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def export_assets():
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_DIR / "control_room_direction_labels.blend"))
    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_DIR / "control_room_direction_labels.fbx"),
        use_selection=False,
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=False,
    )
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_DIR / "control_room_direction_labels.glb"),
        export_format="GLB",
        export_apply=True,
    )


def write_html():
    html = """<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>CR-17 통제실 복도 방향 표시 레이블 샘플</title>
  <style>
    :root { color-scheme: dark; --bg:#111418; --panel:#1c2229; --line:#36414b; --text:#eef3f6; --muted:#a8b3bd; --accent:#f5b84f; }
    * { box-sizing: border-box; }
    body { margin:0; font-family: "Malgun Gothic", "Noto Sans KR", Arial, sans-serif; background: var(--bg); color: var(--text); }
    main { max-width: 1180px; margin: 0 auto; padding: 28px 18px 42px; }
    h1 { margin: 0 0 8px; font-size: 28px; letter-spacing: 0; }
    p { margin: 0; line-height: 1.65; color: var(--muted); }
    .summary { border-left: 4px solid var(--accent); padding: 8px 0 8px 14px; margin: 18px 0 24px; }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 18px; }
    figure { margin: 0; background: var(--panel); border: 1px solid var(--line); border-radius: 6px; overflow: hidden; }
    img { width: 100%; display: block; background: #0b0e11; }
    figcaption { padding: 12px 14px 14px; font-size: 14px; line-height: 1.55; color: var(--muted); }
    b { color: var(--text); font-weight: 700; }
    .links { margin-top: 22px; padding: 14px 16px; border: 1px solid var(--line); border-radius: 6px; background: #151a20; }
    .links a { color: #9bd1ff; }
  </style>
</head>
<body>
  <main>
    <h1>CR-17 통제실 복도 방향 표시 레이블 샘플</h1>
    <p class="summary">COCKPIT, ENGINE ROOM, CARGO HOLD, ARMORY를 메인 표기로 두고 한국어를 보조 표기로 넣은 승인용 샘플입니다. 천장은 제외했고, 기존 통제실 출입구 관계를 알아볼 수 있도록 기준 벽과 복도 바닥만 낮은 밀도로 표시했습니다.</p>
    <section class="grid">
      <figure><img src="renders/01_context_overview.png" alt="통제실 방향 표시 레이블 전체 샘플"><figcaption><b>전체 구성</b><br>좌측의 조종실/동력실 복도와 하단의 이송창고/무기실 복도에 물리 레이블을 배치했습니다.</figcaption></figure>
      <figure><img src="renders/02_left_corridor_labels.png" alt="좌측 조종실 동력실 레이블"><figcaption><b>좌측 복도 레이블</b><br>조종실과 동력실은 서로 떨어진 구조가 드러나도록 별도 패널과 화살표를 배치했습니다.</figcaption></figure>
      <figure><img src="renders/03_south_adjacent_labels.png" alt="하단 이송창고 무기실 레이블"><figcaption><b>하단 인접 복도 레이블</b><br>이송창고와 무기실은 6시 방향에서 바로 옆에 붙어 있는 관계를 색상과 위치로 구분했습니다.</figcaption></figure>
      <figure><img src="renders/04_panel_closeup.png" alt="레이블 패널 클로즈업"><figcaption><b>패널 상세</b><br>금속 패널, 컬러 트림, 체결 볼트, 영어 메인 표기와 한국어 보조 표기를 포함했습니다.</figcaption></figure>
      <figure><img src="renders/05_topdown_layout.png" alt="방향 레이블 탑다운 레이아웃"><figcaption><b>탑다운 확인</b><br>통제실을 위에서 내려다본 기준으로 각 레이블과 복도 관계를 확인하는 이미지입니다.</figcaption></figure>
    </section>
    <section class="links">
      <p>내보낸 파일: <a href="exports/control_room_direction_labels.glb">GLB</a>, <a href="exports/control_room_direction_labels.fbx">FBX</a>, <a href="blender/control_room_direction_labels.blend">Blender</a></p>
    </section>
  </main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def write_metadata():
    manifest = {
        "sample": "control_room_direction_labels",
        "object_id": "CR-17",
        "created_for": "통제실 복도 방향 표시 레이블",
        "outputs": [
            "index.html",
            "renders/01_context_overview.png",
            "renders/02_left_corridor_labels.png",
            "renders/03_south_adjacent_labels.png",
            "renders/04_panel_closeup.png",
            "renders/05_topdown_layout.png",
            "exports/control_room_direction_labels.glb",
            "exports/control_room_direction_labels.fbx",
            "blender/control_room_direction_labels.blend",
        ],
        "unity_application": "approval_required_before_unity",
        "notes": [
            "천장 없이 통제실 기준 벽, 복도 바닥, 레이블 패널만 표시한다.",
            "방향 레이블은 영어를 메인 표기로 두고 한국어를 보조 표기로 둔다.",
            "조종실/동력실은 좌측 분리 복도, 이송창고/무기실은 6시 방향 인접 복도 관계로 배치한다.",
            "샘플 승인 전에는 Unity 씬, 프리팹, 런타임 자산에 연결하지 않는다.",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    approval = {
        "status": "pending_user_approval",
        "object_id": "CR-17",
        "sample_root": "artSample/control_room_direction_labels",
        "approved_for_unity": False,
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")
    readme = """# CR-17 통제실 복도 방향 표시 레이블 샘플

이 샘플은 통제실 안에서 조종실, 동력실, 이송창고, 무기실 방향을 빠르게 읽기 위한 물리 레이블 패널입니다.

- 조종실/동력실: 통제실 탑다운 기준 좌측에 서로 떨어진 복도로 표현했습니다.
- 이송창고/무기실: 탑다운 기준 6시 방향에서 바로 붙어 있는 복도로 표현했습니다.
- 구성: 벽면 레이블 패널, 컬러 트림, 체결 볼트, 영어 메인 표기, 한국어 보조 표기, 바닥 방향 화살표.
- Unity 반영 상태: 사용자 승인 전이며, 실제 씬/프리팹/런타임 자산에는 연결하지 않았습니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")


import mathutils


clear_scene()
set_units()

mats = {
    "floor": make_mat("brushed dark floor", (0.075, 0.085, 0.092, 1), metallic=0.15, roughness=0.7),
    "wall": make_mat("control room wall reference", (0.19, 0.205, 0.215, 1), metallic=0.05, roughness=0.68),
    "partition": make_mat("inner partition reference", (0.125, 0.14, 0.15, 1), metallic=0.1, roughness=0.66),
    "door_dark": make_mat("door opening dark hint", (0.025, 0.03, 0.036, 1), metallic=0.0, roughness=0.78),
    "corridor_floor": make_mat("corridor floor reference", (0.105, 0.115, 0.12, 1), metallic=0.08, roughness=0.72),
    "dark_panel": make_mat("dark label panel", (0.035, 0.041, 0.047, 1), metallic=0.32, roughness=0.48),
    "bracket": make_mat("gunmetal label bracket", (0.16, 0.17, 0.17, 1), metallic=0.55, roughness=0.38),
    "bolt": make_mat("dark bolt heads", (0.035, 0.037, 0.038, 1), metallic=0.8, roughness=0.3),
    "white_text": make_mat("white sign text", (0.88, 0.95, 1.0, 1), emission=(0.88, 0.95, 1.0, 1), strength=0.35),
    "bright_text": make_mat("bright label text", (0.92, 0.97, 1.0, 1), emission=(0.72, 0.9, 1.0, 1), strength=0.62),
    "muted_text": make_mat("muted engraved text", (0.62, 0.68, 0.72, 1), emission=(0.18, 0.24, 0.29, 1), strength=0.16),
    "cockpit_blue": make_mat("cockpit blue label", (0.12, 0.45, 0.95, 1), metallic=0.18, roughness=0.36, emission=(0.04, 0.18, 0.55, 1), strength=0.45),
    "engine_amber": make_mat("engine amber label", (0.95, 0.47, 0.08, 1), metallic=0.18, roughness=0.36, emission=(0.55, 0.19, 0.02, 1), strength=0.42),
    "cargo_green": make_mat("cargo green label", (0.12, 0.65, 0.38, 1), metallic=0.18, roughness=0.36, emission=(0.03, 0.33, 0.16, 1), strength=0.42),
    "armory_red": make_mat("armory red label", (0.82, 0.12, 0.12, 1), metallic=0.18, roughness=0.36, emission=(0.44, 0.04, 0.03, 1), strength=0.42),
    "spacing_marker": make_mat("layout relation marker", (0.92, 0.78, 0.36, 1), metallic=0.05, roughness=0.46, emission=(0.5, 0.3, 0.08, 1), strength=0.18),
}

setup_render()
add_wall_context()
add_labels()
add_measure_tags()
add_lighting()

render_camera("01_context_overview.png", (4.9, -8.1, 5.1), (-0.3, -2.0, 1.0), lens=34)
render_camera("02_left_corridor_labels.png", (-1.65, -5.25, 2.25), (-3.75, -2.0, 1.15), lens=45)
render_camera("03_south_adjacent_labels.png", (0.0, -7.2, 2.15), (0.0, -4.55, 1.1), lens=48)
render_camera("04_panel_closeup.png", (-0.25, -5.72, 1.65), (-0.72, -4.48, 1.28), lens=72)
render_camera("05_topdown_layout.png", (0.0, -1.0, 9.7), (0.0, -1.0, 0.0), lens=45)
export_assets()
write_html()
write_metadata()

print(f"Generated CR-17 sample at {SAMPLE_ROOT}")
