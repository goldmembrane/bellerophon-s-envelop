from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "engine_room_overclock_control"
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
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    set_principled_input(mat, "Base Color", color)
    set_principled_input(mat, "Metallic", metallic)
    set_principled_input(mat, "Roughness", roughness)
    if emission is not None:
        set_principled_input(mat, "Emission Color", emission)
        set_principled_input(mat, "Emission Strength", emission_strength)
    mat.diffuse_color = color
    return mat


def noisy_metal(name: str, base: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = material(name, base, metallic=0.34, roughness=0.86)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 38
    noise.inputs["Detail"].default_value = 9
    noise.inputs["Roughness"].default_value = 0.62
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.22
    ramp.color_ramp.elements[0].color = (base[0] * 0.42, base[1] * 0.42, base[2] * 0.42, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.38, 1),
        min(base[1] * 1.38, 1),
        min(base[2] * 1.38, 1),
        1,
    )
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def transparent_emission(
    name: str,
    color: tuple[float, float, float, float],
    strength: float,
    alpha: float,
) -> bpy.types.Material:
    mat = material(name, color, roughness=0.28, emission=color, emission_strength=strength)
    mat.blend_method = "BLEND"
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None and "Alpha" in bsdf.inputs:
        bsdf.inputs["Alpha"].default_value = alpha
    mat.diffuse_color = (color[0], color[1], color[2], alpha)
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
    vertices: int = 24,
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


def add_sphere(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    radius: float,
    mat: bpy.types.Material,
    segments: int = 32,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=16, radius=radius, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_bolt(parent: bpy.types.Object, name: str, x: float, z: float, mat: bpy.types.Material) -> None:
    add_cylinder(name, parent, (x, -0.246, z), 0.032, 0.018, mat, (math.radians(90), 0, 0), 18)
    add_box(f"{name} slot", parent, (x, -0.258, z), (0.048, 0.007, 0.008), mat, bevel_width=0.001)


def add_status_lamp(
    parent: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    center_x: float,
    center_z: float,
    active: bool,
) -> None:
    if not active:
        return

    lens_mat = mats["lamp_on"] if active else mats["lamp_off"]
    add_cylinder(
        "red blinking beacon recessed collar",
        parent,
        (center_x, -0.286, center_z),
        0.180,
        0.050,
        mats["dark_ring"],
        (math.radians(90), 0, 0),
        36,
    )
    add_sphere("red blinking beacon lens", parent, (center_x, -0.326, center_z), 0.125, lens_mat, 36)


def lever_endpoint(center_x: float, up: bool) -> tuple[float, float, float]:
    x_offset = 0.210
    z_offset = 0.710 if up else -0.710
    return (center_x + x_offset, -0.365, 1.200 + z_offset)


def add_lever(
    parent: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    center_x: float,
    *,
    up: bool,
) -> None:
    pivot = (center_x - 0.160, -0.340, 1.200)
    end = lever_endpoint(center_x, up)
    add_cylinder("lever pivot axle cap", parent, pivot, 0.145, 0.060, mats["pivot"], (math.radians(90), 0, 0), 36)
    add_cylinder("lever pivot inner bolt", parent, (pivot[0], -0.384, pivot[2]), 0.055, 0.026, mats["bolt"], (math.radians(90), 0, 0), 24)
    add_cylinder_between("spring return lever black steel arm", parent, pivot, end, 0.045, mats["lever_arm"], 24)
    add_sphere("rubberized lever hand grip", parent, end, 0.118, mats["grip"], 32)

    stop_z = 1.940 if up else 0.460
    add_box(
        "mechanical stop currently contacted by lever",
        parent,
        (center_x + 0.030, -0.372, stop_z),
        (0.520, 0.035, 0.070),
        mats["stop_contact"],
        bevel_width=0.008,
    )


def add_control_state(
    root: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    name: str,
    center_x: float,
    *,
    lever_up: bool,
    lamp_active: bool,
    add_motion_arrow: bool,
) -> bpy.types.Object:
    state_root = add_empty(name, root)
    width = 2.36
    height = 2.52
    add_box(f"{name} wall placement proxy", state_root, (center_x, 0.062, 1.260), (2.74, 0.150, 2.84), mats["wall"], bevel_width=0.010)
    add_box(f"{name} armored overclock control backplate", state_root, (center_x, -0.065, 1.260), (width, 0.120, height), mats["backplate"], bevel_width=0.022)
    add_box(f"{name} dark recessed service gasket", state_root, (center_x, -0.145, 1.260), (width - 0.17, 0.050, height - 0.20), mats["rubber"], bevel_width=0.014)
    add_box(f"{name} raised lever face plate", state_root, (center_x, -0.206, 1.160), (width - 0.31, 0.100, height - 0.42), mats["face"], bevel_width=0.018)

    add_box(f"{name} upper hard stop block", state_root, (center_x + 0.035, -0.315, 1.950), (0.560, 0.040, 0.090), mats["stop"], bevel_width=0.008)
    add_box(f"{name} lower hard stop block", state_root, (center_x + 0.035, -0.315, 0.450), (0.560, 0.040, 0.090), mats["stop"], bevel_width=0.008)

    add_box(f"{name} protective left rail", state_root, (center_x - 0.515, -0.308, 1.200), (0.056, 0.080, 1.770), mats["guard"], bevel_width=0.009)
    add_box(f"{name} protective right rail", state_root, (center_x + 0.500, -0.308, 1.200), (0.056, 0.080, 1.770), mats["guard"], bevel_width=0.009)
    add_box(f"{name} protective upper rail", state_root, (center_x - 0.005, -0.307, 2.110), (1.070, 0.080, 0.058), mats["guard"], bevel_width=0.009)
    add_box(f"{name} protective lower rail", state_root, (center_x - 0.005, -0.307, 0.285), (1.070, 0.080, 0.058), mats["guard"], bevel_width=0.009)

    add_status_lamp(state_root, mats, center_x + 0.920, 1.730, lamp_active)
    add_lever(state_root, mats, center_x, up=lever_up)

    add_box(f"{name} lower cable trunk", state_root, (center_x, -0.103, -0.150), (0.150, 0.125, 0.780), mats["conduit"], bevel_width=0.013)
    add_cylinder(f"{name} cable gland left", state_root, (center_x - 0.680, -0.160, -0.455), 0.050, 0.240, mats["conduit"], (0, math.radians(90), 0), 20)
    add_cylinder(f"{name} cable gland right", state_root, (center_x + 0.680, -0.160, -0.455), 0.050, 0.240, mats["conduit"], (0, math.radians(90), 0), 20)

    for sx in (-1, 1):
        for sz in (-1, 1):
            add_bolt(state_root, f"{name} corner bolt", center_x + sx * 1.055, 1.260 + sz * 1.120, mats["bolt"])

    scratches = [
        (-0.520, 2.170, 0.135, 0.020, -8),
        (0.310, 0.760, 0.160, 0.020, 10),
        (0.610, 0.345, 0.115, 0.018, -14),
    ]
    for index, (x, z, sx, sz, angle) in enumerate(scratches, start=1):
        add_box(
            f"{name} chipped exposed metal {index}",
            state_root,
            (center_x + x, -0.322, z),
            (sx, 0.010, sz),
            mats["wear"],
            (0, 0, math.radians(angle)),
            bevel_width=0.001,
        )

    if add_motion_arrow:
        add_cylinder_between(
            f"{name} downward motion arrow shaft",
            state_root,
            (center_x - 0.640, -0.378, 1.830),
            (center_x - 0.640, -0.378, 0.710),
            0.018,
            mats["motion"],
            18,
        )
        add_cylinder_between(
            f"{name} downward motion arrow left head",
            state_root,
            (center_x - 0.640, -0.378, 0.710),
            (center_x - 0.730, -0.378, 0.840),
            0.018,
            mats["motion"],
            18,
        )
        add_cylinder_between(
            f"{name} downward motion arrow right head",
            state_root,
            (center_x - 0.640, -0.378, 0.710),
            (center_x - 0.550, -0.378, 0.840),
            0.018,
            mats["motion"],
            18,
        )

    return state_root


def build_sample(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("ER-10 spring return overclock lever sample")
    add_control_state(
        root,
        mats,
        "state 01 idle lever up beacon off",
        -2.45,
        lever_up=True,
        lamp_active=False,
        add_motion_arrow=False,
    )
    add_control_state(
        root,
        mats,
        "state 02 user pulls lever down",
        0.0,
        lever_up=False,
        lamp_active=False,
        add_motion_arrow=True,
    )
    add_control_state(
        root,
        mats,
        "state 03 lever returned up overclock active beacon blinking",
        2.45,
        lever_up=True,
        lamp_active=True,
        add_motion_arrow=False,
    )


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 64
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("EngineRoomOverclockControlWorld")
    scene.world.color = (0.010, 0.012, 0.011)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -5.0, 4.4))
    key = bpy.context.object
    key.name = "large soft front inspection light"
    key.data.energy = 520
    key.data.size = 6.4
    key.data.color = (0.94, 0.98, 0.92)

    bpy.ops.object.light_add(type="AREA", location=(-4.8, -2.6, 2.6))
    fill = bpy.context.object
    fill.name = "cool side fill for lever guard"
    fill.data.energy = 150
    fill.data.size = 4.0
    fill.data.color = (0.65, 0.82, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(3.2, -1.8, 1.8))
    red = bpy.context.object
    red.name = "red beacon spill for active state"
    red.data.energy = 110
    red.data.color = (1.0, 0.05, 0.02)


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
    camera.name = "engine room overclock control camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "engine_room_overclock_control.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "engine_room_overclock_control.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "engine_room_overclock_control.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-10",
        "title": "동력실 오버클럭 복귀형 레버 스위치 샘플",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt:123 - 동력기계는 운행당 1회 오버클럭 가능하며 1분 뒤 원상복귀합니다.",
            "docs/GAME_DESIGN_SOURCE.txt:124 - 스크린과 상호작용 시 오버클럭을 활성화합니다.",
            "사용자 확인: 오버클럭 스위치는 레버 형식이며 평소에는 위에 있습니다.",
            "사용자 확인: 조작할 때 위에서 아래로 내리고, 내리면 다시 위로 되돌아옵니다.",
            "사용자 확인: 되돌아온 뒤 레버 옆 빨간 점조등이 켜져 오버클럭 활성화 상태를 알립니다.",
        ],
        "generatedFiles": [
            "blender/engine_room_overclock_control.blend",
            "exports/engine_room_overclock_control.fbx",
            "exports/engine_room_overclock_control.glb",
            "renders/01_state_sequence.png",
            "renders/02_idle_lever_up.png",
            "renders/03_pull_down_position.png",
            "renders/04_returned_up_beacon_on.png",
            "renders/05_side_mount.png",
            "renders/06_red_beacon_detail.png",
        ],
        "includedParts": [
            "벽면 부착형 오버클럭 조작 패널",
            "평상시 위쪽 위치의 복귀형 레버",
            "조작 중 아래로 내려간 레버 상태",
            "복귀 후 위쪽 위치로 돌아온 레버 상태",
            "오버클럭 활성화를 알리는 레버 옆 빨간 점조등",
            "레버 보호 레일, 상하 하드 스톱, 피벗 축, 케이블 글랜드",
            "ER-09 근처 벽면에 붙일 수 있는 얕은 장착 깊이",
        ],
        "excludedParts": [
            "실제 오버클럭 게임 로직",
            "운행당 1회 사용 제한 로직",
            "1분 타이머 UI",
            "스크린 내구도 20% 이하 파괴 연동",
            "Unity 씬, 프리팹, 런타임 자산 연결",
            "상호작용 프롬프트와 입력 처리",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-10",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산 또는 UI 흐름에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# engine_room_overclock_control

ER-10 오버클럭 상호작용 장치의 승인용 Blender 샘플입니다.

## 목적

동력실 ER-09 스크린 근처에 붙일 수 있는 물리 조작 장치를 검토하기 위한 샘플입니다.  
실제 상호작용 로직, 운행당 1회 제한, 1분 타이머, 스크린 파괴 상태 연동은 포함하지 않습니다.

## 사용자 확인 사양

- 오버클럭 스위치는 레버 형식입니다.
- 평소에는 레버가 위쪽에 있습니다.
- 조작할 때는 위에서 아래로 내립니다.
- 레버를 아래로 내리면 다시 위쪽으로 되돌아옵니다.
- 되돌아온 뒤 레버 옆 빨간 점조등이 켜지며 오버클럭 활성화 상태를 알립니다.

## 포함

- 벽면 부착형 조작 패널
- 평상시 위쪽 레버 상태
- 조작 중 아래로 내려간 레버 상태
- 복귀 후 위쪽 레버와 빨간 점조등 점등 상태
- 보호 레일, 상하 하드 스톱, 피벗 축, 케이블 글랜드
- ER-09 주변 벽면에 붙일 수 있는 얕은 장착 깊이

## 제외

- 실제 오버클럭 게임 로직
- 운행당 1회 사용 제한 로직
- 1분 타이머 UI
- 스크린 내구도 20% 이하 파괴 연동
- Unity 씬, 프리팹, 런타임 자산 연결
- 상호작용 프롬프트와 입력 처리
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_state_sequence.png", "01 평상시, 조작 중, 활성화 상태 순서"),
        ("02_idle_lever_up.png", "02 평상시 위쪽 레버와 꺼진 점조등"),
        ("03_pull_down_position.png", "03 위에서 아래로 내린 조작 상태"),
        ("04_returned_up_beacon_on.png", "04 복귀 후 위쪽 레버와 빨간 점조등 점등"),
        ("05_side_mount.png", "05 얕은 벽면 장착 깊이"),
        ("06_red_beacon_detail.png", "06 빨간 점조등 세부"),
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
  <title>engine_room_overclock_control review</title>
  <style>
    body {{ margin: 0; background: #151817; color: #e8e1d2; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #c8c0af; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #3e453f; background: #202521; padding: 10px; }}
    img {{ width: 100%; display: block; background: #0c0f0e; }}
    figcaption {{ margin-top: 8px; color: #d9cfba; font-size: 14px; }}
    @media (max-width: 800px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>engine_room_overclock_control</h1>
  <p>ER-10 오버클럭 레버 스위치 샘플입니다. 평소 위쪽 레버, 조작 중 아래로 내려간 레버, 복귀 후 위쪽 레버와 빨간 점조등 점등 상태를 분리해 볼 수 있게 구성했습니다. 사용자 승인 전에는 Unity 씬이나 런타임 자산에 연결하지 않습니다.</p>
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
        "wall": noisy_metal("engine room wall behind overclock control", (0.17, 0.20, 0.18, 1)),
        "backplate": noisy_metal("dark armored overclock backplate", (0.13, 0.15, 0.14, 1)),
        "face": noisy_metal("worn raised lever face plate", (0.23, 0.25, 0.22, 1)),
        "rubber": material("black service gasket", (0.010, 0.012, 0.011, 1), roughness=0.92),
        "slot": material("deep black lever travel slot", (0.004, 0.005, 0.005, 1), roughness=0.78),
        "stop": noisy_metal("lever hard stop block", (0.34, 0.33, 0.25, 1)),
        "stop_contact": material("fresh wear on contacted hard stop", (0.72, 0.63, 0.34, 1), metallic=0.24, roughness=0.50),
        "guard": noisy_metal("lever protective guard rail", (0.07, 0.08, 0.075, 1)),
        "dark_ring": noisy_metal("black lamp collar ring", (0.030, 0.033, 0.031, 1)),
        "lamp_off": material("dark red beacon lens off", (0.19, 0.012, 0.008, 1), roughness=0.34, emission=(0.030, 0.0, 0.0, 1), emission_strength=0.03),
        "lamp_on": material("red beacon lens on", (1.0, 0.030, 0.012, 1), roughness=0.22, emission=(1.0, 0.020, 0.006, 1), emission_strength=2.4),
        "lamp_halo": transparent_emission("transparent red blink halo", (1.0, 0.020, 0.006, 1), 1.7, 0.26),
        "lamp_tick": material("red blink tick marks", (1.0, 0.040, 0.018, 1), roughness=0.34, emission=(1.0, 0.020, 0.006, 1), emission_strength=1.6),
        "pivot": noisy_metal("lever pivot axle metal", (0.28, 0.28, 0.24, 1)),
        "bolt": noisy_metal("small recessed bolt heads", (0.40, 0.39, 0.32, 1)),
        "lever_arm": noisy_metal("spring return lever black steel arm", (0.035, 0.039, 0.037, 1)),
        "grip": material("rubberized worn red lever grip", (0.72, 0.050, 0.032, 1), roughness=0.62, emission=(0.08, 0.005, 0.002, 1), emission_strength=0.05),
        "conduit": noisy_metal("overclock control cable conduit", (0.045, 0.050, 0.047, 1)),
        "wear": material("scraped exposed metal on overclock control", (0.68, 0.66, 0.56, 1), metallic=0.42, roughness=0.55),
        "motion": material("amber motion direction marker", (1.0, 0.52, 0.080, 1), roughness=0.38, emission=(1.0, 0.30, 0.040, 1), emission_strength=0.60),
    }

    build_sample(mats)
    add_render_lights()

    cameras = [
        ("state_sequence", (0.0, -6.4, 1.25), (0.0, -0.12, 1.16), 44, "01_state_sequence.png", 3.45),
        ("idle_lever_up", (-2.45, -4.4, 1.20), (-2.45, -0.16, 1.18), 56, "02_idle_lever_up.png", None),
        ("pull_down_position", (0.0, -4.4, 1.20), (0.0, -0.16, 1.18), 56, "03_pull_down_position.png", None),
        ("returned_up_beacon_on", (2.45, -4.4, 1.20), (2.45, -0.16, 1.18), 56, "04_returned_up_beacon_on.png", None),
        ("side_mount", (4.55, -2.55, 1.35), (2.45, -0.12, 1.18), 52, "05_side_mount.png", None),
        ("red_beacon_detail", (3.37, -2.55, 1.74), (3.37, -0.28, 1.73), 78, "06_red_beacon_detail.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} lever sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
