from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "control_room_vertical_aux_screens"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"

MAIN_SCREEN_WIDTH = 4.70
MAIN_SCREEN_HEIGHT = 1.48
MAIN_SCREEN_CENTER_Z = 1.78
MAIN_SCREEN_TOP_Z = MAIN_SCREEN_CENTER_Z + MAIN_SCREEN_HEIGHT * 0.5
MAIN_SCREEN_RIGHT_X = MAIN_SCREEN_WIDTH * 0.5

CR07_WIDTH = 1.42
CR07_HEIGHT = 0.44
CR07_CENTER_X = MAIN_SCREEN_RIGHT_X + CR07_WIDTH * 0.5 + 0.70
CR07_CENTER_Z = MAIN_SCREEN_TOP_Z + CR07_HEIGHT * 0.5 + 0.20

CR08_PANEL_COUNT = 3
CR08_PANEL_WIDTH = 0.20
CR08_PANEL_HEIGHT = 2.20
CR08_PANEL_GAP = 0.12
CR08_BANK_WIDTH = CR08_PANEL_COUNT * CR08_PANEL_WIDTH + (CR08_PANEL_COUNT - 1) * CR08_PANEL_GAP
CR08_BANK_CENTER_X = -3.28
CR08_BANK_CENTER_Z = MAIN_SCREEN_CENTER_Z


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
    roughness: float = 0.78,
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
    mat = material(name, base, metallic=0.26, roughness=0.86)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat
    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 34
    noise.inputs["Detail"].default_value = 8
    noise.inputs["Roughness"].default_value = 0.60
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[0].color = (base[0] * 0.50, base[1] * 0.50, base[2] * 0.50, 1)
    ramp.color_ramp.elements[1].position = 1.0
    ramp.color_ramp.elements[1].color = (
        min(base[0] * 1.40, 1),
        min(base[1] * 1.40, 1),
        min(base[2] * 1.40, 1),
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
    vertices: int = 20,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_text_label(
    name: str,
    parent: bpy.types.Object,
    text: str,
    loc: tuple[float, float, float],
    size: float,
    mat: bpy.types.Material,
    rot: tuple[float, float, float] = (math.radians(90), 0.0, 0.0),
) -> bpy.types.Object:
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.003
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def add_corner_bolts(
    parent: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    label: str,
    center_x: float,
    center_z: float,
    width: float,
    height: float,
    radius: float = 0.020,
) -> None:
    for sx in (-1, 1):
        for sz in (-1, 1):
            add_cylinder(
                f"{label} bolt {sx:+d} {sz:+d}",
                parent,
                (center_x + sx * width * 0.44, -0.354, center_z + sz * height * 0.43),
                radius,
                0.016,
                mats["bolt"],
                (math.radians(90), 0, 0),
                16,
            )


def add_zone_band(
    parent: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    panel_x: float,
    panel_z: float,
    index: int,
    total: int,
    color_mat: bpy.types.Material,
    label: str,
) -> None:
    band_h = (CR08_PANEL_HEIGHT - 0.22) / total
    top = panel_z + CR08_PANEL_HEIGHT * 0.5 - 0.16
    z = top - band_h * (index + 0.5)
    add_box(
        f"CR-08 {label} status color band",
        parent,
        (panel_x, -0.358, z),
        (CR08_PANEL_WIDTH - 0.090, 0.012, band_h * 0.72),
        color_mat,
        bevel_width=0.002,
    )
    add_box(
        f"CR-08 {label} slim divider",
        parent,
        (panel_x, -0.361, z - band_h * 0.45),
        (CR08_PANEL_WIDTH - 0.070, 0.010, 0.008),
        mats["screen_line"],
        bevel_width=0.001,
    )
    add_text_label(
        f"CR-08 {label} short label",
        parent,
        label,
        (panel_x, -0.374, z),
        0.028,
        mats["screen_text"],
    )


def add_vertical_panel(
    root: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    index: int,
    title: str,
    mode: str,
    center_x: float,
    center_z: float,
) -> None:
    frame_w = CR08_PANEL_WIDTH + 0.18
    frame_h = CR08_PANEL_HEIGHT + 0.20
    add_box(f"CR-08 {title} rear mounting pad", root, (center_x, -0.094, center_z), (frame_w + 0.16, 0.105, frame_h + 0.16), mats["mount"], bevel_width=0.015)
    add_box(f"CR-08 {title} black gasket", root, (center_x, -0.158, center_z), (frame_w + 0.060, 0.060, frame_h + 0.060), mats["rubber"], bevel_width=0.010)
    add_box(f"CR-08 {title} vertical armored frame", root, (center_x, -0.222, center_z), (frame_w, 0.130, frame_h), mats["frame"], bevel_width=0.020)
    add_box(f"CR-08 {title} smoked vertical glass lip", root, (center_x, -0.295, center_z), (CR08_PANEL_WIDTH + 0.040, 0.026, CR08_PANEL_HEIGHT + 0.050), mats["glass"], bevel_width=0.008)
    add_box(f"CR-08 {title} inactive vertical display surface", root, (center_x, -0.329, center_z), (CR08_PANEL_WIDTH, 0.018, CR08_PANEL_HEIGHT), mats["screen"], bevel_width=0.005)
    add_box(f"CR-08 {title} top header strip", root, (center_x, -0.360, center_z + CR08_PANEL_HEIGHT * 0.5 - 0.055), (CR08_PANEL_WIDTH - 0.060, 0.012, 0.060), mats["header"], bevel_width=0.002)
    add_text_label(f"CR-08 {title} title", root, mode, (center_x, -0.376, center_z + CR08_PANEL_HEIGHT * 0.5 - 0.055), 0.030, mats["screen_text"])

    labels = ("BRG", "CARGO", "WPN", "STORE", "ENG", "CTRL")
    band_mats = (mats["green"], mats["amber"], mats["red"], mats["blue"], mats["amber"], mats["green"])
    for i, label in enumerate(labels):
        add_zone_band(root, mats, center_x, center_z, i, len(labels), band_mats[(i + index) % len(band_mats)], label)

    add_corner_bolts(root, mats, f"CR-08 {title} compact frame", center_x, center_z, frame_w, frame_h)
    add_box(f"CR-08 {title} lower service latch", root, (center_x, -0.355, center_z - frame_h * 0.49), (0.22, 0.012, 0.044), mats["latch"], bevel_width=0.004)


def add_main_screen_context(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_box("control room future screen wall context panel", root, (0.30, 0.060, 1.95), (8.55, 0.160, 3.15), mats["wall"], bevel_width=0.014)
    add_box("CR-06 large main screen reference mount", root, (0.0, -0.050, MAIN_SCREEN_CENTER_Z), (MAIN_SCREEN_WIDTH + 0.34, 0.100, MAIN_SCREEN_HEIGHT + 0.30), mats["main_mount"], bevel_width=0.020)
    add_box("CR-06 large main screen dark inactive surface", root, (0.0, -0.165, MAIN_SCREEN_CENTER_Z), (MAIN_SCREEN_WIDTH, 0.030, MAIN_SCREEN_HEIGHT), mats["main_screen"], bevel_width=0.010)
    add_box("main screen upper structural lintel reference", root, (0.0, -0.170, MAIN_SCREEN_TOP_Z + 0.19), (MAIN_SCREEN_WIDTH + 0.56, 0.095, 0.105), mats["rail"], bevel_width=0.008)
    add_box("main screen lower service sill reference", root, (0.0, -0.170, MAIN_SCREEN_CENTER_Z - MAIN_SCREEN_HEIGHT * 0.5 - 0.19), (MAIN_SCREEN_WIDTH + 0.56, 0.095, 0.105), mats["rail"], bevel_width=0.008)
    add_text_label("CR-06 reference label", root, "CR-06 MAIN SCREEN CONTEXT", (0.0, -0.215, MAIN_SCREEN_CENTER_Z - 0.02), 0.072, mats["label"])


def add_cr07_context(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    frame_w = CR07_WIDTH + 0.28
    frame_h = CR07_HEIGHT + 0.22
    add_box("CR-07 context reserved horizontal auxiliary frame", root, (CR07_CENTER_X, -0.232, CR07_CENTER_Z), (frame_w, 0.105, frame_h), mats["cr07_ghost"], bevel_width=0.018)
    add_box("CR-07 context dark horizontal display", root, (CR07_CENTER_X, -0.310, CR07_CENTER_Z), (CR07_WIDTH, 0.018, CR07_HEIGHT), mats["cr07_screen"], bevel_width=0.006)
    add_text_label("CR-07 context label", root, "CR-07 CONTEXT", (CR07_CENTER_X, -0.342, CR07_CENTER_Z), 0.045, mats["label"])


def add_vertical_bank(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_box(
        "CR-08 left side vertical screen reserved placement outline",
        root,
        (CR08_BANK_CENTER_X, -0.185, CR08_BANK_CENTER_Z),
        (CR08_BANK_WIDTH + 0.42, 0.020, CR08_PANEL_HEIGHT + 0.46),
        mats["zone"],
        bevel_width=0.004,
    )
    modes = ("ZONE", "CCTV", "LOCK")
    for i in range(CR08_PANEL_COUNT):
        offset = (i - (CR08_PANEL_COUNT - 1) * 0.5) * (CR08_PANEL_WIDTH + CR08_PANEL_GAP)
        add_vertical_panel(root, mats, i, f"panel {i + 1}", modes[i], CR08_BANK_CENTER_X + offset, CR08_BANK_CENTER_Z)
    add_text_label("CR-08 placement label", root, "CR-08 VERTICAL AUX SCREEN BANK - SAMPLE COUNT 3", (CR08_BANK_CENTER_X, -0.382, CR08_BANK_CENTER_Z - CR08_PANEL_HEIGHT * 0.5 - 0.18), 0.050, mats["label"])


def add_wall_dressing(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_box("upper control room wall conduit continuing behind CR-08 and CR-07", root, (0.20, -0.018, 3.24), (8.00, 0.052, 0.070), mats["conduit"], bevel_width=0.006)
    add_box("CR-08 left vertical screen cable raceway", root, (CR08_BANK_CENTER_X, -0.028, CR08_BANK_CENTER_Z + 0.98), (CR08_BANK_WIDTH + 0.35, 0.060, 0.070), mats["conduit"], bevel_width=0.006)
    add_box("CR-08 shared lower service trunk", root, (CR08_BANK_CENTER_X, -0.030, CR08_BANK_CENTER_Z - 0.96), (CR08_BANK_WIDTH + 0.20, 0.055, 0.060), mats["conduit"], bevel_width=0.006)
    for x_offset in (-0.36, -0.12, 0.12, 0.36):
        add_cylinder("CR-08 shared cable gland", root, (CR08_BANK_CENTER_X + x_offset, -0.065, CR08_BANK_CENTER_Z + 0.98), 0.024, 0.060, mats["bolt"], (0, math.radians(90), 0), 16)


def build_sample(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("CR-08 vertical auxiliary screen bank sample")
    add_main_screen_context(root, mats)
    add_cr07_context(root, mats)
    add_vertical_bank(root, mats)
    add_wall_dressing(root, mats)


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 56
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("ControlRoomVerticalAuxScreensWorld")
    scene.world.color = (0.010, 0.012, 0.013)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -4.9, 4.3))
    key = bpy.context.object
    key.name = "large front inspection softbox"
    key.data.energy = 520
    key.data.size = 5.8
    key.data.color = (0.86, 0.96, 1.0)

    bpy.ops.object.light_add(type="AREA", location=(-3.8, -2.5, 2.9))
    edge = bpy.context.object
    edge.name = "left vertical screen edge fill"
    edge.data.energy = 210
    edge.data.size = 2.2
    edge.data.color = (0.58, 0.78, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(-2.8, -1.7, 1.05))
    warm = bpy.context.object
    warm.name = "low amber control room bounce"
    warm.data.energy = 65
    warm.data.color = (1.0, 0.58, 0.30)


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
    camera.name = "control room vertical auxiliary screens camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "control_room_vertical_aux_screens.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "control_room_vertical_aux_screens.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "control_room_vertical_aux_screens.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "CR-08",
        "title": "통제실 세로형 보조 스크린 묶음",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/CONTROL_ROOM_OBJECTS.md - CR-08 세로형 보조 스크린 묶음",
            "docs/GAME_DESIGN_SOURCE.txt - 통제실에는 대형 스크린, 가로형 스크린, 세로형 스크린 여러 개가 필요함",
            "사용자 승인 범위 - CR-06 대형 메인 스크린 및 CR-07 가로형 보조 스크린과의 위치 관계를 보여주는 샘플 제작",
        ],
        "designInference": [
            "원본은 세로형 스크린의 정확한 개수를 명시하지 않으므로 샘플에서는 폭을 대폭 줄이고 높이를 확실히 늘린 3개 패널 묶음을 제안안으로 표시함",
            "CR-07과 충돌하지 않도록 CR-06 왼쪽 보조 베이에 배치하는 안으로 구성함",
            "화면 내용은 실제 UI가 아니라 구역 상태/CCTV/폐쇄 상태를 암시하는 더미 표시임",
        ],
        "generatedFiles": [
            "blender/control_room_vertical_aux_screens.blend",
            "exports/control_room_vertical_aux_screens.fbx",
            "exports/control_room_vertical_aux_screens.glb",
            "renders/01_context_overview.png",
            "renders/02_front_alignment.png",
            "renders/03_vertical_bank_closeup.png",
            "renders/04_side_mount_depth.png",
            "renders/05_cr07_relation_clearance.png",
            "index.html",
            "README.md",
            "ASSET_MANIFEST.json",
            "APPROVAL_STATUS.json",
        ],
        "includedParts": [
            "CR-06 대형 메인 스크린 기준 패널",
            "CR-07 가로형 보조 스크린 위치 관계용 컨텍스트 프레임",
            "CR-08 폭을 대폭 줄이고 높이를 확실히 늘린 세로형 보조 스크린 3패널 묶음 제안안",
            "ZONE/CCTV/LOCK 더미 표시 화면",
            "좌측 보조 베이 장착 프레임, 공용 케이블 레이스웨이, 하단 서비스 트렁크",
        ],
        "excludedParts": [
            "실제 구역 상태 UI",
            "실제 CCTV 영상 피드",
            "실제 복도 폐쇄 로직",
            "상호작용 로직",
            "Unity 씬 반영",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "CR-08",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 CargoRunMvp 씬, 프리팹, 런타임 UI 흐름에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# control_room_vertical_aux_screens

CR-08 통제실 세로형 보조 스크린 묶음 승인용 Blender 샘플입니다.

## 목적

원본 기획서에는 통제실에 대형 스크린, 가로형 스크린, 세로형 스크린 여러 개가 필요하다고 되어 있습니다. 이 샘플은 CR-06 대형 메인 스크린과 CR-07 가로형 보조 스크린을 기준으로, CR-08 세로형 보조 스크린 묶음이 어느 위치와 비율로 붙을지 확인하기 위한 승인용 샘플입니다.

## 배치 기준

- CR-08은 CR-06 대형 메인 스크린의 왼쪽 보조 베이에 붙는 폭을 대폭 줄이고 높이를 확실히 늘린 3개 세로 패널 묶음 제안안입니다.
- CR-07은 오른쪽 상단 컨텍스트 프레임으로만 표시해서 CR-08과 충돌하지 않는지 확인할 수 있게 했습니다.
- 세로 패널 개수는 원본에 확정 수량이 없으므로 이번 샘플에서는 3개로 제안했습니다.
- 패널 안의 `ZONE`, `CCTV`, `LOCK` 표시는 실제 UI가 아니라 기능 방향을 암시하는 더미 화면입니다.
- Unity 반영은 하지 않았습니다.

## 포함

- `blender/control_room_vertical_aux_screens.blend`
- `exports/control_room_vertical_aux_screens.fbx`
- `exports/control_room_vertical_aux_screens.glb`
- `renders/*.png` 5개 구도
- `index.html`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- 실제 구역 상태 UI
- 실제 CCTV 영상 피드
- 실제 복도 폐쇄 로직
- 상호작용 로직
- Unity 씬 반영
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_context_overview.png", "01 CR-06, CR-07, CR-08 전체 위치 관계"),
        ("02_front_alignment.png", "02 전면 정렬과 메인 스크린 비가림 확인"),
        ("03_vertical_bank_closeup.png", "03 CR-08 폭을 대폭 줄인 긴 세로형 3패널 묶음 근접"),
        ("04_side_mount_depth.png", "04 장착 깊이와 케이블 레이스웨이"),
        ("05_cr07_relation_clearance.png", "05 CR-07과 CR-08 간격 확인"),
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
  <title>CR-08 vertical auxiliary screens sample</title>
  <style>
    :root {{
      color-scheme: dark;
      --bg: #111412;
      --panel: #1d2320;
      --line: #405047;
      --text: #efe7d4;
      --muted: #c9bfa9;
      --accent: #6fc8bd;
      --warn: #d58b3a;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      background:
        linear-gradient(90deg, rgba(111,200,189,.08) 1px, transparent 1px) 0 0 / 44px 44px,
        radial-gradient(circle at 20% 0%, rgba(213,139,58,.16), transparent 34%),
        var(--bg);
      color: var(--text);
      font-family: "Segoe UI", "Malgun Gothic", sans-serif;
    }}
    main {{ max-width: 1320px; margin: 0 auto; padding: 28px; }}
    header {{
      display: grid;
      grid-template-columns: 1fr auto;
      gap: 18px;
      align-items: end;
      margin-bottom: 22px;
      border-bottom: 1px solid var(--line);
      padding-bottom: 16px;
    }}
    h1 {{ margin: 0; font-size: clamp(24px, 3vw, 38px); font-weight: 720; letter-spacing: 0; }}
    p {{ margin: 8px 0 0; color: var(--muted); line-height: 1.6; max-width: 880px; }}
    .badge {{ border: 1px solid var(--warn); color: #f3c17f; padding: 8px 10px; font-size: 13px; white-space: nowrap; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid var(--line); background: color-mix(in srgb, var(--panel), black 10%); padding: 10px; }}
    figure:first-child {{ grid-column: 1 / -1; }}
    img {{ width: 100%; display: block; background: #080a09; }}
    figcaption {{ margin-top: 9px; color: #e1d6c0; font-size: 14px; }}
    .notes {{ margin-top: 18px; display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; }}
    .note {{ border-left: 3px solid var(--accent); background: rgba(29,35,32,.72); padding: 12px 14px; color: var(--muted); line-height: 1.5; }}
    @media (max-width: 860px) {{
      header, .grid, .notes {{ grid-template-columns: 1fr; }}
      figure:first-child {{ grid-column: auto; }}
      main {{ padding: 18px; }}
      .badge {{ white-space: normal; }}
    }}
  </style>
</head>
<body>
<main>
  <header>
    <div>
      <h1>CR-08 세로형 보조 스크린 묶음</h1>
      <p>CR-06 대형 메인 스크린 왼쪽 보조 베이에 폭을 대폭 줄이고 높이를 확실히 늘린 세로형 3패널 묶음을 배치한 승인용 샘플입니다. CR-07은 오른쪽 상단 위치 관계 확인용 컨텍스트로만 표시했습니다.</p>
    </div>
    <div class="badge">Unity 반영 전 승인용</div>
  </header>
  <section class="grid">
{cards}
  </section>
  <section class="notes">
    <div class="note">세로 패널 개수는 원본에 확정되어 있지 않아 폭을 대폭 줄이고 높이를 확실히 늘린 3개 제안안으로 표시했습니다.</div>
    <div class="note">패널 안의 표시는 실제 UI가 아니라 구역 상태, CCTV, 복도 폐쇄 표시 방향을 암시하는 더미입니다.</div>
    <div class="note">CR-01, CR-07, 동력실, 조종실 오브젝트에는 반영하지 않았습니다.</div>
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
        "wall": noisy_metal("control room screen wall worn metal", (0.18, 0.23, 0.23, 1)),
        "main_mount": noisy_metal("CR-06 inactive main screen mount", (0.24, 0.27, 0.25, 1)),
        "main_screen": material("CR-06 dark inactive main screen glass", (0.012, 0.026, 0.030, 1), roughness=0.34, emission=(0.0, 0.020, 0.024, 1), emission_strength=0.10),
        "mount": noisy_metal("CR-08 wall side mounting pad", (0.21, 0.24, 0.22, 1)),
        "rubber": material("CR-08 black vibration gasket", (0.010, 0.012, 0.012, 1), roughness=0.94),
        "frame": noisy_metal("CR-08 vertical armored frame", (0.29, 0.32, 0.29, 1)),
        "glass": material("CR-08 smoked vertical glass lip", (0.010, 0.020, 0.024, 1), roughness=0.28),
        "screen": material("CR-08 inactive vertical display", (0.010, 0.048, 0.052, 1), roughness=0.36, emission=(0.02, 0.16, 0.17, 1), emission_strength=0.18),
        "screen_line": material("CR-08 dim screen division lines", (0.18, 0.72, 0.70, 1), roughness=0.55, emission=(0.08, 0.40, 0.38, 1), emission_strength=0.30),
        "screen_text": material("CR-08 pale display lettering", (0.74, 0.92, 0.88, 1), roughness=0.55, emission=(0.14, 0.42, 0.36, 1), emission_strength=0.22),
        "header": material("CR-08 inactive header strip", (0.25, 0.45, 0.38, 1), roughness=0.58, emission=(0.10, 0.22, 0.18, 1), emission_strength=0.16),
        "green": material("CR-08 green sample status", (0.09, 0.78, 0.47, 1), roughness=0.50, emission=(0.04, 0.38, 0.18, 1), emission_strength=0.35),
        "amber": material("CR-08 amber sample status", (0.95, 0.54, 0.13, 1), roughness=0.55, emission=(0.45, 0.18, 0.03, 1), emission_strength=0.28),
        "red": material("CR-08 red sample status", (0.90, 0.18, 0.14, 1), roughness=0.55, emission=(0.42, 0.04, 0.03, 1), emission_strength=0.28),
        "blue": material("CR-08 blue sample status", (0.12, 0.42, 0.90, 1), roughness=0.55, emission=(0.03, 0.14, 0.42, 1), emission_strength=0.25),
        "bolt": noisy_metal("CR-08 recessed bolt heads", (0.36, 0.36, 0.31, 1)),
        "latch": noisy_metal("CR-08 small service latch", (0.38, 0.40, 0.34, 1)),
        "rail": noisy_metal("main screen bay structural rail", (0.31, 0.33, 0.29, 1)),
        "zone": material("CR-08 amber reserved placement outline", (0.95, 0.50, 0.10, 1), roughness=0.52, emission=(0.45, 0.16, 0.02, 1), emission_strength=0.18),
        "conduit": noisy_metal("CR-08 upper wall conduit", (0.045, 0.055, 0.055, 1)),
        "label": material("control room pale placement label", (0.78, 0.88, 0.84, 1), roughness=0.70, emission=(0.16, 0.30, 0.28, 1), emission_strength=0.12),
        "cr07_ghost": noisy_metal("CR-07 context ghost frame", (0.16, 0.18, 0.17, 1)),
        "cr07_screen": material("CR-07 context inactive screen", (0.010, 0.034, 0.038, 1), roughness=0.42, emission=(0.01, 0.09, 0.10, 1), emission_strength=0.10),
    }

    build_sample(mats)
    add_render_lights()

    cameras = [
        ("context_overview", (1.20, -6.7, 2.62), (-0.30, -0.12, 2.02), 29, "01_context_overview.png", None),
        ("front_alignment", (-0.20, -6.8, 2.06), (-0.20, -0.12, 2.04), 48, "02_front_alignment.png", 6.35),
        ("vertical_bank_closeup", (-3.28, -3.95, 1.86), (-3.28, -0.22, 1.86), 67, "03_vertical_bank_closeup.png", 3.05),
        ("side_mount_depth", (-4.40, -2.62, 2.20), (-3.30, -0.14, 1.90), 55, "04_side_mount_depth.png", None),
        ("cr07_relation_clearance", (1.50, -5.45, 2.90), (1.15, -0.17, 2.48), 40, "05_cr07_relation_clearance.png", 4.45),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} Blender sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
