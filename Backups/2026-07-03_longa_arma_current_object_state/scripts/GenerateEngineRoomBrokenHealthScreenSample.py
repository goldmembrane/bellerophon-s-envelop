from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "engine_room_broken_health_screen"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR):
        path.mkdir(parents=True, exist_ok=True)


def clean_generated_files() -> None:
    for directory in (BLENDER_DIR, RENDER_DIR, EXPORT_DIR):
        if not directory.exists():
            continue

        for item in directory.iterdir():
            if item.is_file():
                item.unlink()
            elif item.is_dir():
                shutil.rmtree(item)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def set_principled_input(mat: bpy.types.Material, name: str, value) -> None:
    if not mat.use_nodes:
        return

    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None and name in bsdf.inputs:
        bsdf.inputs[name].default_value = value


def material(
    name: str,
    color: tuple[float, float, float, float],
    *,
    metallic: float = 0.0,
    roughness: float = 0.75,
    emission: tuple[float, float, float, float] | None = None,
    emission_strength: float = 0.0,
    alpha: float = 1.0,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    set_principled_input(mat, "Base Color", color)
    set_principled_input(mat, "Metallic", metallic)
    set_principled_input(mat, "Roughness", roughness)
    if emission is not None:
        set_principled_input(mat, "Emission Color", emission)
        set_principled_input(mat, "Emission Strength", emission_strength)
    if alpha < 1.0:
        mat.blend_method = "BLEND"
        mat.use_screen_refraction = False
        mat.show_transparent_back = False
        if hasattr(mat, "shadow_method"):
            mat.shadow_method = "NONE"
        set_principled_input(mat, "Alpha", alpha)
    mat.diffuse_color = (color[0], color[1], color[2], alpha)
    return mat


def noisy_metal(name: str, base: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = material(name, base, metallic=0.32, roughness=0.88)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 38
    noise.inputs["Detail"].default_value = 8
    noise.inputs["Roughness"].default_value = 0.64
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.20
    ramp.color_ramp.elements[0].color = (base[0] * 0.40, base[1] * 0.40, base[2] * 0.40, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.34, 1),
        min(base[1] * 1.34, 1),
        min(base[2] * 1.34, 1),
        1,
    )
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def add_empty(name: str, parent: bpy.types.Object | None = None) -> bpy.types.Object:
    empty = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(empty)
    if parent is not None:
        empty.parent = parent
    return empty


def add_box(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
    bevel_width: float = 0.012,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    obj.parent = parent
    if bevel_width > 0:
        bevel = obj.modifiers.new("hard surface bevel", "BEVEL")
        bevel.width = bevel_width
        bevel.segments = 1
        obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_cylinder(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    radius: float,
    depth: float,
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0),
    vertices: int = 32,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_cylinder_between(
    name: str,
    parent: bpy.types.Object,
    start: tuple[float, float, float],
    end: tuple[float, float, float],
    radius: float,
    mat: bpy.types.Material,
    vertices: int = 18,
) -> bpy.types.Object:
    start_vec = Vector(start)
    end_vec = Vector(end)
    center = (start_vec + end_vec) * 0.5
    direction = end_vec - start_vec
    length = direction.length
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=length, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_bolt(parent: bpy.types.Object, name: str, x: float, z: float, mat: bpy.types.Material, radius: float = 0.042) -> None:
    add_cylinder(name, parent, (x, -0.236, z), radius, 0.026, mat, (math.radians(90), 0, 0), 20)
    add_box(f"{name} slot", parent, (x, -0.252, z), (radius * 1.42, 0.010, radius * 0.22), mat, bevel_width=0.001)


def add_corner_bolts(parent: bpy.types.Object, mats: dict[str, bpy.types.Material], width: float, height: float, z_center: float) -> None:
    for sx in (-1, 1):
        for sz in (-1, 1):
            add_bolt(parent, "ER-11 ER-09 asset screen corner bolt", sx * width * 0.45, z_center + sz * height * 0.42, mats["bolt"])


def add_wear(parent: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    chips = [
        (-1.10, 2.52, 0.13, 0.020, 6),
        (-0.62, 2.54, 0.19, 0.018, -4),
        (0.76, 2.49, 0.16, 0.020, 9),
        (1.22, 1.27, 0.13, 0.018, -11),
        (-1.33, 1.52, 0.11, 0.014, 14),
        (0.12, 0.54, 0.18, 0.016, -7),
    ]
    for index, (x, z, width, height, angle) in enumerate(chips, start=1):
        add_box(
            f"ER-11 ER-09 style worn exposed metal chip {index}",
            parent,
            (x, -0.264, z),
            (width, 0.010, height),
            mats["wear"],
            (0, 0, math.radians(angle)),
            bevel_width=0.001,
        )


def add_runtime_ui_anchor_markers(parent: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    for index, (x, z) in enumerate([(-0.96, 1.94), (0.96, 1.94), (-0.96, 0.86), (0.96, 0.86)], start=1):
        add_box(
            f"ER-11 dark runtime UI registration tab {index}",
            parent,
            (x, -0.329, z),
            (0.070, 0.010, 0.070),
            mats["marker_off"],
            bevel_width=0.004,
        )


def add_off_screen_sample(parent: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("ER-11 ER-09 based powered off display sample", parent)

    add_box("ER-11 ER-09 engine room side wall placement proxy", root, (0, 0.075, 1.45), (5.60, 0.18, 2.95), mats["wall"], bevel_width=0.014)
    add_box("ER-11 ER-09 screen installation height rail", root, (0, -0.032, 0.42), (5.36, 0.034, 0.070), mats["rail"], bevel_width=0.004)
    add_box("ER-11 ER-09 upper conduit rail continuing through wall", root, (0, -0.034, 2.86), (5.18, 0.050, 0.085), mats["conduit"], bevel_width=0.006)
    for x in (-2.44, 2.44):
        add_box("ER-11 ER-09 wall vertical rib framing screen bay", root, (x, -0.040, 1.52), (0.105, 0.075, 2.55), mats["rib"], bevel_width=0.006)

    add_box("ER-11 ER-09 scaled big screen prefab footprint backplate", root, (0, -0.092, 1.48), (2.86, 0.135, 2.20), mats["mount"], bevel_width=0.024)
    add_box("ER-11 ER-09 dark vibration pad behind asset screen", root, (0, -0.165, 1.48), (2.66, 0.070, 2.03), mats["rubber"], bevel_width=0.018)
    add_box("ER-11 ER-09 worn asset screen armored frame", root, (0, -0.214, 1.48), (2.50, 0.155, 1.86), mats["frame"], bevel_width=0.030)
    add_box("ER-11 ER-09 slightly recessed glass bevel lip", root, (0, -0.292, 1.48), (2.15, 0.020, 1.40), mats["glass_lip"], bevel_width=0.012)

    add_box("ER-11 powered off dead black display surface", root, (0, -0.326, 1.48), (2.02, 0.018, 1.27), mats["dead_screen"], bevel_width=0.006)
    add_box("ER-11 subtle unlit glass reflection strip", root, (-0.46, -0.338, 1.92), (0.72, 0.006, 0.040), mats["glass_reflection"], (0, 0, math.radians(-3.0)), 0.001)
    add_box("ER-11 no signal dark lower reflection", root, (0.44, -0.338, 0.98), (0.42, 0.006, 0.030), mats["glass_reflection"], (0, 0, math.radians(4.0)), 0.001)
    add_runtime_ui_anchor_markers(root, mats)

    add_box("ER-11 ER-09 left side hinge lug from asset mount", root, (-1.45, -0.190, 1.48), (0.140, 0.190, 0.76), mats["hinge"], bevel_width=0.012)
    add_box("ER-11 ER-09 right side cable socket block", root, (1.45, -0.190, 1.48), (0.190, 0.205, 0.62), mats["hinge"], bevel_width=0.012)
    add_cylinder("ER-11 ER-09 right screen conduit socket", root, (1.66, -0.190, 1.48), 0.060, 0.24, mats["conduit"], (0, math.radians(90), 0), 22)
    add_cylinder("ER-11 ER-09 upper cable coupler", root, (1.20, -0.064, 2.70), 0.040, 0.75, mats["conduit"], (0, math.radians(90), 0), 18)
    add_box("ER-11 ER-09 short cable drop from conduit to screen", root, (1.36, -0.096, 2.44), (0.070, 0.070, 0.50), mats["conduit"], bevel_width=0.012)

    add_box("ER-11 ER-10 lower reserved connector cover closed inactive", root, (0, -0.220, 0.38), (1.08, 0.150, 0.30), mats["reserve_off"], bevel_width=0.018)
    add_box("ER-11 lower cover dead inactive seam", root, (0, -0.308, 0.38), (0.88, 0.014, 0.052), mats["dead_screen"], bevel_width=0.003)
    add_cylinder("ER-11 left reserved connector screw", root, (-0.42, -0.314, 0.38), 0.024, 0.010, mats["bolt"], (math.radians(90), 0, 0), 16)
    add_cylinder("ER-11 right reserved connector screw", root, (0.42, -0.314, 0.38), 0.024, 0.010, mats["bolt"], (math.radians(90), 0, 0), 16)

    add_corner_bolts(root, mats, 2.50, 1.86, 1.48)
    add_wear(root, mats)


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 72
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("EngineRoomPoweredOffScreenWorld")
    scene.world.color = (0.010, 0.012, 0.011)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -4.8, 4.0))
    key = bpy.context.object
    key.name = "large soft front inspection light"
    key.data.energy = 390
    key.data.size = 5.8
    key.data.color = (0.94, 0.98, 0.92)

    bpy.ops.object.light_add(type="AREA", location=(-3.2, -2.5, 2.4))
    fill = bpy.context.object
    fill.name = "cool side fill for dead glass"
    fill.data.energy = 120
    fill.data.size = 3.8
    fill.data.color = (0.62, 0.80, 1.0)


def add_camera(
    name: str,
    loc: tuple[float, float, float],
    target: tuple[float, float, float],
    lens: float,
    *,
    ortho_scale: float | None = None,
) -> bpy.types.Object:
    bpy.ops.object.camera_add(location=loc)
    camera = bpy.context.object
    camera.name = "engine room powered off screen camera " + name
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = lens
    if ortho_scale is not None:
        camera.data.type = "ORTHO"
        camera.data.ortho_scale = ortho_scale
    camera.data.dof.use_dof = False
    return camera


def render_camera(camera: bpy.types.Object, output_path: Path) -> None:
    bpy.context.scene.camera = camera
    bpy.context.scene.render.filepath = str(output_path)
    bpy.ops.render.render(write_still=True)


def export_assets() -> None:
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "engine_room_broken_health_screen.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "engine_room_broken_health_screen.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "engine_room_broken_health_screen.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-11",
        "title": "동력실 내구도 스크린 꺼짐 상태 샘플",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt:124 - 동력실 내구도 20% 이하에서는 스크린 파괴로 내구도 파악 및 오버클럭 활성화가 불가능합니다.",
            "사용자 확인: 기존 ER-09를 기반으로 하되 이번 artSample에서는 이펙트 없이 디스플레이가 꺼진 상태만 표현합니다.",
            "사용자 제한: 기존 동력실 오브젝트와 조종실 오브젝트는 건드리지 않고 artSample 승인용 샘플만 제작합니다.",
        ],
        "generatedFiles": [
            "blender/engine_room_broken_health_screen.blend",
            "exports/engine_room_broken_health_screen.fbx",
            "exports/engine_room_broken_health_screen.glb",
            "renders/01_front_destroyed.png",
            "renders/02_cracked_display_detail.png",
            "renders/03_disabled_overclock_port.png",
            "renders/04_exposed_cables.png",
            "renders/05_side_mount_damage.png",
            "renders/06_frame_and_glass_damage.png",
        ],
        "includedParts": [
            "ER-09 메인 스크린 비율과 프레임을 유지한 꺼진 디스플레이",
            "검은 화면과 약한 비활성 유리 반사",
            "닫힌 하단 커넥터 커버",
            "승인 전 검토용 벽면 배치 프록시",
        ],
        "excludedParts": [
            "Unity 씬, 프리팹, 런타임 자산 연결",
            "기존 ER-01~ER-10 동력실 오브젝트 수정",
            "조종실 관련 오브젝트 또는 파일 수정",
            "실제 내구도 20% 이하 상태 전환 로직",
            "오버클럭 차단 로직",
            "상호작용 입력 처리",
            "연기, 불꽃, 전기 스파크, 애니메이션 오버레이",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-11",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산 또는 UI 흐름에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# engine_room_broken_health_screen

ER-11 동력실 내구도 스크린 꺼짐 상태의 승인용 Blender 샘플입니다.

## 목적

동력실 내구도 20% 이하에서 스크린이 기능을 잃어 내구도 파악과 오버클럭 활성화가 불가능한 상태를 검토하기 위한 샘플입니다.  
기존 ER-09 스크린 형태를 유지하고, 디스플레이가 꺼진 검은 화면만으로 상태를 표현합니다.  
기존 동력실 오브젝트, 조종실 오브젝트, Unity 씬, 프리팹, 런타임 자산은 수정하지 않았습니다.

## 포함

- ER-09 메인 스크린 비율과 프레임을 유지한 꺼진 디스플레이
- 검은 화면과 약한 비활성 유리 반사
- 닫힌 하단 커넥터 커버
- 승인 전 검토용 벽면 배치 프록시

## 제외

- Unity 씬, 프리팹, 런타임 자산 연결
- 기존 ER-01~ER-10 동력실 오브젝트 수정
- 조종실 관련 오브젝트 또는 파일 수정
- 실제 내구도 20% 이하 상태 전환 로직
- 오버클럭 차단 로직
- 상호작용 입력 처리
- 연기, 불꽃, 전기 스파크, 애니메이션 오버레이
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_front_destroyed.png", "01 꺼진 스크린 전체"),
        ("02_cracked_display_detail.png", "02 꺼진 디스플레이 세부"),
        ("03_disabled_overclock_port.png", "03 비활성 하단 커넥터"),
        ("04_exposed_cables.png", "04 상단 케이블 연결부"),
        ("05_side_mount_damage.png", "05 측면 장착 깊이"),
        ("06_frame_and_glass_damage.png", "06 프레임과 꺼진 유리면"),
    ]
    cards = "\n".join(
        f'    <figure><a href="renders/{name}"><img src="renders/{name}" alt="{label}"></a><figcaption>{label}</figcaption></figure>'
        for name, label in images
    )
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>engine_room_broken_health_screen review</title>
  <style>
    body {{ margin: 0; background: #151817; color: #e8e1d2; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #c8c0af; line-height: 1.55; }}
    .primary {{ border: 1px solid #3e453f; background: #202521; padding: 10px; margin: 18px 0; }}
    .primary img {{ width: 100%; display: block; background: #0c0f0e; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #3e453f; background: #202521; padding: 10px; }}
    img {{ width: 100%; display: block; background: #0c0f0e; }}
    figcaption {{ margin-top: 8px; color: #d9cfba; font-size: 14px; }}
    @media (max-width: 800px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>engine_room_broken_health_screen</h1>
  <p>ER-11 동력실 스크린 꺼짐 상태 승인용 샘플입니다. ER-09 메인 스크린 비율과 프레임은 유지하고, 화면은 꺼진 검은 디스플레이로만 표현했습니다. 기존 동력실과 조종실 오브젝트는 수정하지 않았습니다.</p>
  <section class="primary" aria-label="꺼진 디스플레이 미리보기">
    <img src="renders/01_front_destroyed.png" alt="꺼진 스크린 전체">
  </section>
  <section class="grid">
{cards}
  </section>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def main() -> None:
    ensure_dirs()
    clean_generated_files()
    reset_scene()
    configure_rendering()

    mats = {
        "wall": noisy_metal("ER-11 dark engine room wall proxy", (0.15, 0.18, 0.16, 1)),
        "rail": noisy_metal("ER-11 screen installation rail", (0.34, 0.34, 0.28, 1)),
        "rib": noisy_metal("ER-11 wall rib metal", (0.10, 0.12, 0.11, 1)),
        "conduit": noisy_metal("ER-11 conduit metal", (0.050, 0.055, 0.052, 1)),
        "mount": noisy_metal("ER-11 rear mount plate", (0.16, 0.18, 0.16, 1)),
        "rubber": material("ER-11 black rubber vibration pad", (0.010, 0.011, 0.010, 1), roughness=0.94),
        "frame": noisy_metal("ER-11 ER-09 dark frame", (0.20, 0.22, 0.20, 1)),
        "glass_lip": material("ER-11 smoky glass bevel lip", (0.010, 0.016, 0.015, 1), roughness=0.30),
        "hinge": noisy_metal("ER-11 ER-09 hinge and socket metal", (0.10, 0.11, 0.10, 1)),
        "wear": material("ER-11 scraped exposed metal", (0.70, 0.66, 0.54, 1), metallic=0.46, roughness=0.52),
        "dead_screen": material("ER-11 powered off dead black display", (0.002, 0.004, 0.004, 1), roughness=0.34, emission=(0.0, 0.002, 0.002, 1), emission_strength=0.02),
        "glass_reflection": material("ER-11 faint unlit glass reflection", (0.08, 0.14, 0.13, 1), roughness=0.28, alpha=0.42),
        "marker_off": material("ER-11 inactive UI registration tab", (0.020, 0.035, 0.034, 1), roughness=0.55),
        "reserve_off": noisy_metal("ER-11 inactive overclock connector cover", (0.08, 0.08, 0.075, 1)),
        "bolt": noisy_metal("ER-11 bolt heads", (0.32, 0.31, 0.26, 1)),
    }

    root = add_empty("ER-11 engine room powered off screen asset sample")
    add_off_screen_sample(root, mats)
    add_render_lights()

    cameras = [
        ("front_powered_off", (0.0, -6.0, 1.52), (0.0, -0.12, 1.48), 45, "01_front_destroyed.png", 3.58),
        ("dead_display_detail", (0.0, -3.15, 1.52), (0.0, -0.30, 1.48), 74, "02_cracked_display_detail.png", None),
        ("disabled_overclock_port", (0.0, -2.8, 0.50), (0.0, -0.24, 0.38), 74, "03_disabled_overclock_port.png", None),
        ("upper_cable_connection", (2.2, -3.0, 2.25), (1.16, -0.26, 2.48), 68, "04_exposed_cables.png", None),
        ("side_mount_depth", (3.7, -2.7, 1.55), (1.10, -0.10, 1.40), 48, "05_side_mount_damage.png", None),
        ("frame_dead_glass", (-1.6, -3.0, 1.72), (-0.40, -0.24, 1.55), 66, "06_frame_and_glass_damage.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} powered-off display sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
