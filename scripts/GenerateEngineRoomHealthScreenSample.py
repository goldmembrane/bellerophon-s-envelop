from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "engine_room_health_screen"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
COMPARISON_DIR = SAMPLE_ROOT / "unity_applied_comparison"

ASSET_PACK_ROOT = PROJECT_ROOT / "Assets" / "Sci-Fi Styled Modular Pack"
HEAVY_STATION_ROOT = PROJECT_ROOT / "Assets" / "Heavy Station Kit"
PRIMARY_PREFAB = ASSET_PACK_ROOT / "Prefabs" / "Decorative elements" / "big_screen.prefab"
SCREEN_TEXTURE = HEAVY_STATION_ROOT / "BASE" / "Textures" / "Displays" / "B2_Eq41_E.png"
SCREEN_TEXTURE_UV_RECT = (0.0, 0.75, 0.5, 1.0)
SECONDARY_PREFABS = [
    ASSET_PACK_ROOT / "Prefabs" / "Decorative elements" / "console_screen.prefab",
    ASSET_PACK_ROOT / "Prefabs" / "Decorative elements" / "computer_station.prefab",
    ASSET_PACK_ROOT / "Prefabs" / "Walls" / "decorative_wall_4_computer.prefab",
]


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR, COMPARISON_DIR):
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


def image_material(
    name: str,
    texture_path: Path,
    fallback_color: tuple[float, float, float, float],
    *,
    metallic: float = 0.0,
    roughness: float = 0.62,
    emission_strength: float = 0.0,
) -> bpy.types.Material:
    mat = material(
        name,
        fallback_color,
        metallic=metallic,
        roughness=roughness,
        emission=fallback_color,
        emission_strength=emission_strength,
    )
    if not texture_path.exists():
        return mat

    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    image_node = nodes.new(type="ShaderNodeTexImage")
    image_node.image = bpy.data.images.load(str(texture_path), check_existing=True)
    links.new(image_node.outputs["Color"], bsdf.inputs["Base Color"])
    if emission_strength > 0 and "Emission Color" in bsdf.inputs:
        links.new(image_node.outputs["Color"], bsdf.inputs["Emission Color"])
    return mat


def noisy_metal(name: str, base: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = material(name, base, metallic=0.32, roughness=0.86)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 34
    noise.inputs["Detail"].default_value = 8
    noise.inputs["Roughness"].default_value = 0.64
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.24
    ramp.color_ramp.elements[0].color = (base[0] * 0.42, base[1] * 0.42, base[2] * 0.42, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.35, 1),
        min(base[1] * 1.35, 1),
        min(base[2] * 1.35, 1),
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


def add_textured_panel(
    name: str,
    parent: bpy.types.Object,
    center: tuple[float, float, float],
    width: float,
    height: float,
    mat: bpy.types.Material,
    uv_rect: tuple[float, float, float, float] = (0.0, 0.0, 1.0, 1.0),
) -> bpy.types.Object:
    x, y, z = center
    hw = width * 0.5
    hh = height * 0.5
    mesh = bpy.data.meshes.new(name + " mesh")
    mesh.from_pydata(
        [
            (x - hw, y, z - hh),
            (x + hw, y, z - hh),
            (x + hw, y, z + hh),
            (x - hw, y, z + hh),
        ],
        [],
        [(0, 1, 2, 3)],
    )
    mesh.update()
    uv_layer = mesh.uv_layers.new(name="UVMap")
    u_min, v_min, u_max, v_max = uv_rect
    for loop, uv in zip(uv_layer.data, [(u_min, v_min), (u_max, v_min), (u_max, v_max), (u_min, v_max)]):
        loop.uv = uv
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def add_bolt(parent: bpy.types.Object, name: str, x: float, z: float, mat: bpy.types.Material, radius: float = 0.042) -> None:
    add_cylinder(name, parent, (x, -0.236, z), radius, 0.026, mat, (math.radians(90), 0, 0), 20)
    add_box(f"{name} slot", parent, (x, -0.252, z), (radius * 1.42, 0.010, radius * 0.22), mat, bevel_width=0.001)


def add_corner_bolts(parent: bpy.types.Object, mats: dict[str, bpy.types.Material], width: float, height: float, z_center: float) -> None:
    for sx in (-1, 1):
        for sz in (-1, 1):
            add_bolt(parent, "asset screen corner bolt", sx * width * 0.45, z_center + sz * height * 0.42, mats["bolt"])


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
            f"worn exposed metal chip {index}",
            parent,
            (x, -0.264, z),
            (width, 0.010, height),
            mats["wear"],
            (0, 0, math.radians(angle)),
            bevel_width=0.001,
        )


def add_runtime_ui_anchor_markers(parent: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    marker_positions = [(-0.96, 1.94), (0.96, 1.94), (-0.96, 0.86), (0.96, 0.86)]
    for index, (x, z) in enumerate(marker_positions, start=1):
        add_box(
            f"runtime UI corner registration tab {index}",
            parent,
            (x, -0.329, z),
            (0.070, 0.010, 0.070),
            mats["marker"],
            bevel_width=0.004,
        )


def add_asset_like_wall_screen(parent: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    screen = add_empty("ER-09 asset based wall health screen proxy", parent)

    add_box("engine room side wall placement proxy", screen, (0, 0.075, 1.45), (5.60, 0.18, 2.95), mats["wall"], bevel_width=0.014)
    add_box("screen installation height rail", screen, (0, -0.032, 0.42), (5.36, 0.034, 0.070), mats["rail"], bevel_width=0.004)
    add_box("upper conduit rail continuing through wall", screen, (0, -0.034, 2.86), (5.18, 0.050, 0.085), mats["conduit"], bevel_width=0.006)
    for x in (-2.44, 2.44):
        add_box("wall vertical rib framing screen bay", screen, (x, -0.040, 1.52), (0.105, 0.075, 2.55), mats["rib"], bevel_width=0.006)

    add_box("scaled big_screen prefab footprint backplate", screen, (0, -0.092, 1.48), (2.86, 0.135, 2.20), mats["mount"], bevel_width=0.024)
    add_box("dark vibration pad behind asset screen", screen, (0, -0.165, 1.48), (2.66, 0.070, 2.03), mats["rubber"], bevel_width=0.018)
    add_box("worn asset screen armored frame", screen, (0, -0.214, 1.48), (2.50, 0.155, 1.86), mats["frame"], bevel_width=0.030)

    add_box("slightly recessed glass bevel lip", screen, (0, -0.292, 1.48), (2.15, 0.020, 1.40), mats["glass_lip"], bevel_width=0.012)
    add_textured_panel("B2_Eq41_E single display tile surface", screen, (0, -0.326, 1.48), 2.02, 1.27, mats["computer_screen"], SCREEN_TEXTURE_UV_RECT)
    add_runtime_ui_anchor_markers(screen, mats)

    add_box("left side hinge lug from asset mount", screen, (-1.45, -0.190, 1.48), (0.140, 0.190, 0.76), mats["hinge"], bevel_width=0.012)
    add_box("right side cable socket block", screen, (1.45, -0.190, 1.48), (0.190, 0.205, 0.62), mats["hinge"], bevel_width=0.012)
    add_cylinder("right screen conduit socket", screen, (1.66, -0.190, 1.48), 0.060, 0.24, mats["conduit"], (0, math.radians(90), 0), 22)
    add_cylinder("upper cable coupler", screen, (1.20, -0.064, 2.70), 0.040, 0.75, mats["conduit"], (0, math.radians(90), 0), 18)
    add_box("short cable drop from conduit to screen", screen, (1.36, -0.096, 2.44), (0.070, 0.070, 0.50), mats["conduit"], bevel_width=0.012)

    add_box("ER-10 lower reserved connector cover", screen, (0, -0.220, 0.38), (1.08, 0.150, 0.30), mats["reserve"], bevel_width=0.018)
    add_box("lower cover inactive access seam", screen, (0, -0.308, 0.38), (0.88, 0.014, 0.052), mats["blank_surface"], bevel_width=0.003)
    add_cylinder("left reserved connector screw", screen, (-0.42, -0.314, 0.38), 0.024, 0.010, mats["bolt"], (math.radians(90), 0, 0), 16)
    add_cylinder("right reserved connector screw", screen, (0.42, -0.314, 0.38), 0.024, 0.010, mats["bolt"], (math.radians(90), 0, 0), 16)

    add_corner_bolts(screen, mats, 2.50, 1.86, 1.48)
    add_wear(screen, mats)


def build_health_screen_sample(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("ER-09 engine room health screen asset sample")
    add_asset_like_wall_screen(root, mats)


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 56
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("EngineRoomHealthScreenWorld")
    scene.world.color = (0.010, 0.012, 0.011)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -4.5, 4.2))
    key = bpy.context.object
    key.name = "large soft front inspection light"
    key.data.energy = 460
    key.data.size = 5.8
    key.data.color = (0.94, 0.98, 0.92)

    bpy.ops.object.light_add(type="AREA", location=(-3.4, -2.6, 2.4))
    fill = bpy.context.object
    fill.name = "cool side fill for worn frame"
    fill.data.energy = 130
    fill.data.size = 3.6
    fill.data.color = (0.65, 0.82, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(2.8, -1.9, 1.0))
    warm = bpy.context.object
    warm.name = "warm low bounce for connector cover"
    warm.data.energy = 60
    warm.data.color = (1.0, 0.56, 0.30)


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
    camera.name = "engine room health screen camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "engine_room_health_screen.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "engine_room_health_screen.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "engine_room_health_screen.glb"), export_format="GLB")


def relative(path: Path) -> str:
    return path.relative_to(PROJECT_ROOT).as_posix()


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-09",
        "title": "동력기계 내구도 스크린 컴퓨터 화면 에셋 샘플",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "assetBasis": {
            "primaryPrefab": relative(PRIMARY_PREFAB),
            "displayTexture": relative(SCREEN_TEXTURE),
            "secondaryPrefabs": [relative(path) for path in SECONDARY_PREFABS],
            "primaryPrefabUse": "벽면 부착 스크린 하우징 기준입니다.",
            "displayTextureUse": "2열 x 4행으로 반복된 텍스처 중 좌상단 1개 화면만 디스플레이 전체에 꽉 차게 매핑합니다.",
            "blenderSampleUse": "승인용 배치/비율/부품 경계 샘플입니다. 사용자 승인 전에는 Unity에 연결하지 않습니다.",
        },
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt:124 - 동력실 내부 옆면 스크린으로 동력기계 내구도를 확인합니다.",
            "docs/ENGINE_ROOM_OBJECTS.md - ER-09 내구도 스크린과 ER-10 연결 여지를 분리해 관리합니다.",
            "사용자 확인: B2_Eq41_E.png의 반복 화면 중 한 장만 디스플레이 전체에 꽉 차게 넣고, 세부 내구도 UI는 별도로 구현합니다.",
        ],
        "generatedFiles": [
            "blender/engine_room_health_screen.blend",
            "exports/engine_room_health_screen.fbx",
            "exports/engine_room_health_screen.glb",
            "renders/01_front_all_states.png",
            "renders/02_normal_green.png",
            "renders/03_warning_orange.png",
            "renders/04_side_mount.png",
            "renders/05_detail_reserved_port.png",
        ],
        "includedParts": [
            "big_screen 프리팹 비율 기반 벽면 스크린 하우징",
            "B2_Eq41_E.png의 좌상단 단일 화면 타일을 입힌 디스플레이 면",
            "런타임 UI 정렬용 코너 탭",
            "후면 서비스 플레이트, 진동 패드, 볼트, 힌지, 케이블 소켓",
            "ER-10 오버클럭 장치 연결을 위한 하단 예비 커버",
            "동력실 옆면 벽 배치 기준 프록시",
        ],
        "excludedParts": [
            "내구도 색상 상태 UI",
            "내구도 수치, 게이지, 경고 문구, 파형 표시",
            "스크린 파괴 상태 ER-11",
            "실제 오버클럭 상호작용 장치 ER-10",
            "Unity 씬 배치와 충돌 설정",
            "상호작용 로직",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "ER-09",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# engine_room_health_screen

ER-09 동력기계 내구도 스크린의 승인용 Blender 샘플입니다.

## 목적

동력실 옆면에 붙는 물리 스크린 하우징에 기존 에셋의 컴퓨터 화면 텍스처를 넣어 확인하기 위한 샘플입니다.  
내구도 색상, 수치, 게이지, 경고 문구 같은 세부 표시 정보는 모델링하지 않고 런타임 UI 구현 대상으로 남겼습니다.

## 에셋 기준

- 주 후보: `Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/big_screen.prefab`
- 화면 텍스처: `Assets/Heavy Station Kit/BASE/Textures/Displays/B2_Eq41_E.png`
- 보조 후보: `console_screen.prefab`, `computer_station.prefab`, `decorative_wall_4_computer.prefab`
- 승인 후 Unity 적용 시에는 실제 프리팹 또는 그에 맞춘 편집 가능한 부품 구조로 옮깁니다.

## 포함

- 벽면 부착형 스크린 프레임
- `B2_Eq41_E.png`의 반복 화면 중 좌상단 단일 화면 타일이 꽉 차게 들어간 디스플레이 면
- 런타임 UI 정렬용 코너 탭
- 후면 서비스 플레이트, 볼트, 힌지, 케이블 소켓
- ER-10 오버클럭 장치 연결을 위한 하단 예비 커버
- 동력실 옆면 벽 배치 기준 프록시

## 제외

- 내구도 색상 상태 UI
- 내구도 수치, 게이지, 경고 문구, 파형 표시
- 스크린 파괴 상태
- 실제 오버클럭 상호작용 장치
- Unity 씬 배치와 충돌 설정
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_front_all_states.png", "01 전체 벽면 배치와 B2_Eq41_E 화면"),
        ("02_normal_green.png", "02 디스플레이 면 확대"),
        ("03_warning_orange.png", "03 케이블과 후면 장착부"),
        ("04_side_mount.png", "04 측면 깊이와 벽 고정 기준"),
        ("05_detail_reserved_port.png", "05 ER-10 하단 예비 연결부"),
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
  <title>engine_room_health_screen review</title>
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
  <h1>engine_room_health_screen</h1>
  <p>ER-09 동력기계 내구도 스크린 샘플입니다. 기존 하우징 모델은 유지하고, 디스플레이 면에는 B2_Eq41_E.png의 반복 화면 중 좌상단 단일 화면 타일만 꽉 차게 넣었습니다. 내구도 색상과 수치 같은 세부 UI는 모델링하지 않았습니다.</p>
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

    texture_dir = ASSET_PACK_ROOT / "Textures"
    mats = {
        "wall": image_material("asset pack computer wall texture", texture_dir / "computer_wall_texture.png", (0.18, 0.22, 0.20, 1), metallic=0.18, roughness=0.86),
        "rail": noisy_metal("screen installation rail", (0.34, 0.34, 0.28, 1)),
        "rib": noisy_metal("wall vertical rib", (0.12, 0.14, 0.13, 1)),
        "mount": image_material("asset pack base metal for screen mount", texture_dir / "base-color.png", (0.18, 0.20, 0.18, 1), metallic=0.24, roughness=0.82),
        "rubber": material("black rubber vibration pad", (0.012, 0.014, 0.013, 1), roughness=0.92),
        "frame": noisy_metal("worn asset screen frame", (0.23, 0.26, 0.23, 1)),
        "glass_lip": material("smoked glass bevel lip", (0.012, 0.018, 0.017, 1), roughness=0.26),
        "computer_screen": image_material("computer wall texture display asset surface", SCREEN_TEXTURE, (0.004, 0.007, 0.007, 1), roughness=0.42, emission_strength=0.46),
        "blank_surface": material("blank black inactive surface", (0.004, 0.007, 0.007, 1), roughness=0.68, emission=(0.0, 0.012, 0.010, 1), emission_strength=0.08),
        "marker": material("dim runtime UI corner registration tab", (0.10, 0.22, 0.22, 1), roughness=0.55, emission=(0.02, 0.08, 0.08, 1), emission_strength=0.20),
        "hinge": noisy_metal("dark hinge and cable socket metal", (0.10, 0.11, 0.10, 1)),
        "conduit": noisy_metal("screen side conduit", (0.045, 0.050, 0.047, 1)),
        "bolt": noisy_metal("recessed bolt heads", (0.34, 0.34, 0.30, 1)),
        "wear": material("scraped bright exposed metal", (0.68, 0.66, 0.56, 1), metallic=0.42, roughness=0.55),
        "reserve": noisy_metal("inactive lower overclock connector cover", (0.11, 0.12, 0.11, 1)),
    }

    build_health_screen_sample(mats)
    add_render_lights()

    cameras = [
        ("front_all_states", (0.0, -6.0, 1.58), (0.0, -0.10, 1.48), 45, "01_front_all_states.png", None),
        ("normal_green", (0.0, -3.9, 1.50), (0.0, -0.18, 1.48), 58, "02_normal_green.png", None),
        ("warning_orange", (2.6, -3.5, 1.86), (1.14, -0.12, 1.72), 52, "03_warning_orange.png", None),
        ("side_mount", (3.8, -2.9, 1.70), (1.24, -0.10, 1.45), 47, "04_side_mount.png", None),
        ("reserved_port", (0.0, -2.8, 0.55), (0.0, -0.18, 0.38), 68, "05_detail_reserved_port.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} asset-based Blender sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
