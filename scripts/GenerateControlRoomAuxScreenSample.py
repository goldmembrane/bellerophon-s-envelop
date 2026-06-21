from __future__ import annotations

import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "control_room_aux_screen"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
DISPLAY_TEXTURE_PATH = PROJECT_ROOT / "Assets" / "Heavy Station Kit" / "_common" / "Textures" / "GUI" / "C2_ElC2Disp.png"

MAIN_SCREEN_WIDTH = 4.70
MAIN_SCREEN_HEIGHT = 1.48
MAIN_SCREEN_CENTER_Z = 1.78
MAIN_SCREEN_TOP_Z = MAIN_SCREEN_CENTER_Z + MAIN_SCREEN_HEIGHT * 0.5
MAIN_SCREEN_RIGHT_X = MAIN_SCREEN_WIDTH * 0.5
AUX_SCREEN_WIDTH = 1.42
AUX_SCREEN_HEIGHT = 0.44
AUX_SCREEN_CENTER_X = MAIN_SCREEN_RIGHT_X + AUX_SCREEN_WIDTH * 0.5 + 0.70
AUX_SCREEN_CENTER_Z = MAIN_SCREEN_TOP_Z + AUX_SCREEN_HEIGHT * 0.5 + 0.20


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


def image_material(name: str, image_path: Path, *, emission_strength: float = 0.25) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (1.0, 1.0, 1.0, 1.0)
    image = bpy.data.images.load(str(image_path), check_existing=True)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    tex = nodes.new(type="ShaderNodeTexImage")
    tex.name = name + " texture"
    tex.image = image
    tex.extension = "CLIP"
    tex.interpolation = "Linear"
    if bsdf is not None:
        if "Base Color" in bsdf.inputs:
            links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        if "Emission Color" in bsdf.inputs:
            links.new(tex.outputs["Color"], bsdf.inputs["Emission Color"])
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emission_strength
        if "Alpha" in bsdf.inputs and "Alpha" in tex.outputs:
            links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
            mat.blend_method = "BLEND"
    return mat


def noisy_metal(name: str, base: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = material(name, base, metallic=0.24, roughness=0.86)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat
    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 31
    noise.inputs["Detail"].default_value = 8
    noise.inputs["Roughness"].default_value = 0.58
    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.21
    ramp.color_ramp.elements[0].color = (base[0] * 0.50, base[1] * 0.50, base[2] * 0.50, 1)
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
    bevel_width: float = 0.014,
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
    vertices: int = 24,
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
    obj.data.extrude = 0.004
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def add_image_plane(
    name: str,
    parent: bpy.types.Object,
    center: tuple[float, float, float],
    width: float,
    height: float,
    mat: bpy.types.Material,
) -> bpy.types.Object:
    x, y, z = center
    mesh = bpy.data.meshes.new(name + " mesh")
    verts = [
        (x - width * 0.5, y, z - height * 0.5),
        (x + width * 0.5, y, z - height * 0.5),
        (x + width * 0.5, y, z + height * 0.5),
        (x - width * 0.5, y, z + height * 0.5),
    ]
    mesh.from_pydata(verts, [], [(0, 1, 2, 3)])
    mesh.update()
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for loop, uv in zip(uv_layer.data, ((0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0))):
        loop.uv = uv
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    return obj


def add_screen_grid(parent: bpy.types.Object, mats: dict[str, bpy.types.Material], center_x: float, center_z: float) -> None:
    for i, x_offset in enumerate((-0.45, 0.0, 0.45), start=1):
        add_box(
            f"CR-07 dim horizontal status division {i}",
            parent,
            (center_x + x_offset, -0.334, center_z),
            (0.030, 0.012, AUX_SCREEN_HEIGHT - 0.10),
            mats["screen_line"],
            bevel_width=0.001,
        )
    for i, z_offset in enumerate((-0.11, 0.11), start=1):
        add_box(
            f"CR-07 dim scanline guide {i}",
            parent,
            (center_x, -0.336, center_z + z_offset),
            (AUX_SCREEN_WIDTH - 0.12, 0.010, 0.014),
            mats["screen_line"],
            bevel_width=0.001,
        )
    add_box(
        "CR-07 inactive header strip",
        parent,
        (center_x - 0.36, -0.338, center_z + 0.155),
        (0.50, 0.010, 0.030),
        mats["header_strip"],
        bevel_width=0.001,
    )
    add_box(
        "CR-07 inactive right telemetry strip",
        parent,
        (center_x + 0.44, -0.338, center_z - 0.135),
        (0.36, 0.010, 0.026),
        mats["header_strip"],
        bevel_width=0.001,
    )


def add_corner_bolts(
    parent: bpy.types.Object,
    mats: dict[str, bpy.types.Material],
    label: str,
    center_x: float,
    center_z: float,
    width: float,
    height: float,
    radius: float,
) -> None:
    for sx in (-1, 1):
        for sz in (-1, 1):
            add_cylinder(
                f"{label} bolt {sx:+d} {sz:+d}",
                parent,
                (center_x + sx * width * 0.46, -0.355, center_z + sz * height * 0.40),
                radius,
                0.018,
                mats["bolt"],
                (math.radians(90), 0, 0),
                18,
            )


def add_aux_screen(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    x = AUX_SCREEN_CENTER_X
    z = AUX_SCREEN_CENTER_Z
    frame_width = AUX_SCREEN_WIDTH + 0.28
    frame_height = AUX_SCREEN_HEIGHT + 0.22

    add_box("CR-07 wall-side mounting pad", root, (x, -0.092, z), (frame_width + 0.30, 0.105, frame_height + 0.22), mats["mount"], bevel_width=0.018)
    add_box("CR-07 black vibration gasket", root, (x, -0.160, z), (frame_width + 0.13, 0.065, frame_height + 0.10), mats["rubber"], bevel_width=0.014)
    add_box("CR-07 horizontal armored frame", root, (x, -0.222, z), (frame_width, 0.130, frame_height), mats["frame"], bevel_width=0.024)
    add_box("CR-07 smoked glass bevel lip", root, (x, -0.294, z), (AUX_SCREEN_WIDTH + 0.08, 0.025, AUX_SCREEN_HEIGHT + 0.07), mats["glass"], bevel_width=0.010)
    add_box("CR-07 inactive horizontal auxiliary display surface", root, (x, -0.326, z), (AUX_SCREEN_WIDTH, 0.018, AUX_SCREEN_HEIGHT), mats["screen"], bevel_width=0.006)
    add_image_plane("CR-07 C2_ElC2Disp full display texture", root, (x, -0.346, z), AUX_SCREEN_WIDTH, AUX_SCREEN_HEIGHT, mats["display_texture"])
    add_corner_bolts(root, mats, "CR-07 compact frame", x, z, frame_width, frame_height, 0.027)

    add_box("CR-07 left bracket bolted to main screen bay", root, (x - frame_width * 0.5 - 0.13, -0.180, z), (0.13, 0.150, frame_height * 0.78), mats["bracket"], bevel_width=0.012)
    add_box("CR-07 upper right anti-vibration clamp", root, (x + frame_width * 0.34, -0.165, z + frame_height * 0.50 + 0.08), (0.42, 0.120, 0.075), mats["bracket"], bevel_width=0.010)
    add_box("CR-07 lower right anti-vibration clamp", root, (x + frame_width * 0.34, -0.165, z - frame_height * 0.50 - 0.08), (0.42, 0.120, 0.075), mats["bracket"], bevel_width=0.010)

    cable_x = x + frame_width * 0.5 + 0.10
    add_box("CR-07 right cable socket", root, (cable_x, -0.222, z), (0.10, 0.150, 0.32), mats["socket"], bevel_width=0.010)
    add_cylinder("CR-07 round side cable gland", root, (cable_x + 0.080, -0.222, z), 0.034, 0.100, mats["conduit"], (0, math.radians(90), 0), 18)
    add_box("CR-07 short cable run into upper wall conduit", root, (cable_x + 0.120, -0.112, z + 0.28), (0.050, 0.055, 0.58), mats["conduit"], bevel_width=0.010)
    add_box("CR-07 small service latch", root, (x + 0.18, -0.352, z - frame_height * 0.48), (0.34, 0.012, 0.055), mats["latch"], bevel_width=0.004)


def add_main_screen_context(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_box("control room future screen wall context panel", root, (0.90, 0.060, 1.95), (7.30, 0.160, 3.15), mats["wall"], bevel_width=0.014)
    add_box("CR-06 large main screen reference mount", root, (0.0, -0.050, MAIN_SCREEN_CENTER_Z), (MAIN_SCREEN_WIDTH + 0.34, 0.100, MAIN_SCREEN_HEIGHT + 0.30), mats["main_mount"], bevel_width=0.020)
    add_box("CR-06 large main screen dark inactive surface", root, (0.0, -0.165, MAIN_SCREEN_CENTER_Z), (MAIN_SCREEN_WIDTH, 0.030, MAIN_SCREEN_HEIGHT), mats["main_screen"], bevel_width=0.010)
    add_box("main screen upper structural lintel reference", root, (0.0, -0.170, MAIN_SCREEN_TOP_Z + 0.19), (MAIN_SCREEN_WIDTH + 0.56, 0.095, 0.105), mats["rail"], bevel_width=0.008)
    add_box("main screen lower service sill reference", root, (0.0, -0.170, MAIN_SCREEN_CENTER_Z - MAIN_SCREEN_HEIGHT * 0.5 - 0.19), (MAIN_SCREEN_WIDTH + 0.56, 0.095, 0.105), mats["rail"], bevel_width=0.008)
    for x in (-2.70, 2.70):
        add_box("vertical bay rib framing CR-06 context", root, (x, -0.145, MAIN_SCREEN_CENTER_Z), (0.10, 0.105, MAIN_SCREEN_HEIGHT + 0.58), mats["rail"], bevel_width=0.008)
    add_box("CR-07 reserved upper right placement zone outline", root, (AUX_SCREEN_CENTER_X, -0.190, AUX_SCREEN_CENTER_Z), (AUX_SCREEN_WIDTH + 0.52, 0.020, AUX_SCREEN_HEIGHT + 0.44), mats["zone"], bevel_width=0.004)
    add_text_label("CR-06 reference label", root, "CR-06 대형 메인 스크린 기준", (0.0, -0.215, MAIN_SCREEN_CENTER_Z - 0.02), 0.095, mats["label"])
    add_text_label("CR-07 placement label", root, "CR-07 보조 스크린", (AUX_SCREEN_CENTER_X, -0.370, AUX_SCREEN_CENTER_Z + 0.34), 0.050, mats["label"])


def add_wall_dressing(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> None:
    add_box("upper control room wall conduit continuing behind CR-07", root, (1.45, -0.018, 3.24), (6.00, 0.052, 0.070), mats["conduit"], bevel_width=0.006)
    add_box("right wall auxiliary screen cable raceway", root, (4.28, -0.020, 2.95), (0.070, 0.060, 1.20), mats["conduit"], bevel_width=0.006)
    for x in (-2.30, -1.15, 0.0, 1.15, 2.30):
        add_cylinder("main screen bay top bolt", root, (x, -0.226, MAIN_SCREEN_TOP_Z + 0.19), 0.025, 0.014, mats["bolt"], (math.radians(90), 0, 0), 16)
    for z in (1.42, 1.78, 2.14, 2.50, 2.86, 3.22):
        add_box("subtle right wall service seam", root, (4.32, -0.030, z), (0.84, 0.020, 0.018), mats["seam"], bevel_width=0.001)


def build_aux_screen_sample(mats: dict[str, bpy.types.Material]) -> None:
    root = add_empty("CR-07 upper right horizontal auxiliary screen sample")
    add_main_screen_context(root, mats)
    add_aux_screen(root, mats)
    add_wall_dressing(root, mats)


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 56
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("ControlRoomAuxScreenWorld")
    scene.world.color = (0.010, 0.012, 0.013)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -4.8, 4.2))
    key = bpy.context.object
    key.name = "large front inspection softbox"
    key.data.energy = 500
    key.data.size = 5.6
    key.data.color = (0.86, 0.96, 1.0)

    bpy.ops.object.light_add(type="AREA", location=(3.4, -2.8, 2.8))
    edge = bpy.context.object
    edge.name = "right upper auxiliary screen edge fill"
    edge.data.energy = 170
    edge.data.size = 2.4
    edge.data.color = (0.62, 0.80, 1.0)

    bpy.ops.object.light_add(type="POINT", location=(2.9, -1.9, 1.25))
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
    camera.name = "control room auxiliary screen camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "control_room_aux_screen.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "control_room_aux_screen.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "control_room_aux_screen.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "CR-07",
        "title": "통제실 가로형 보조 스크린",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "sourceBasis": [
            "docs/CONTROL_ROOM_OBJECTS.md - CR-07 가로형 보조 스크린.",
            "사용자 확인 - CR-07은 대형 메인 스크린보다 조금 더 위에 두고, C2_ElC2Disp.png 에셋을 디스플레이에 꽉 차게 넣는다.",
            "docs/GAME_DESIGN_SOURCE.txt - 통제실에는 대형 스크린, 가로형 스크린, 세로형 보조 스크린 여러 개가 필요하다.",
        ],
        "generatedFiles": [
            "blender/control_room_aux_screen.blend",
            "exports/control_room_aux_screen.fbx",
            "exports/control_room_aux_screen.glb",
            "renders/01_context_overview.png",
            "renders/02_front_alignment.png",
            "renders/03_aux_screen_closeup.png",
            "renders/04_side_mount_depth.png",
            "renders/05_top_right_relation.png",
            "index.html",
            "README.md",
            "ASSET_MANIFEST.json",
            "APPROVAL_STATUS.json",
        ],
        "includedParts": [
            "CR-06 대형 메인 스크린 기준 패널",
            "CR-06 표시 면보다 조금 더 위쪽 오른쪽 바깥 영역의 CR-07 가로형 보조 스크린",
            "보조 스크린 베젤, 유리 립, C2_ElC2Disp.png가 꽉 찬 표시 면",
            "벽면 장착 패드, 방진 가스켓, 좌측 브래킷, 우측 케이블 소켓",
            "상부 벽면 케이블 레이스웨이와 고정 볼트",
            "Unity 반영 시 위치 기준을 확인하기 위한 CR-07 배치 라벨",
        ],
        "excludedParts": [
            "실제 구역 상태 UI",
            "CCTV 영상 피드",
            "상호작용 로직",
            "Unity 씬 배치",
            "CR-08 세로형 보조 스크린 묶음",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "CR-07",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 CargoRunMvp 씬, 프리팹, 런타임 UI 흐름에 반영하지 않는다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# control_room_aux_screen

CR-07 통제실 가로형 보조 스크린 승인용 Blender 샘플입니다.

## 목적

CR-06 대형 메인 스크린보다 조금 더 위에 붙는 CR-07 가로형 보조 스크린의 위치, 비율, 장착 구조, 디스플레이 에셋 적용 상태를 확인하기 위한 샘플입니다.
실제 구역 상태 UI, CCTV 영상, 상호작용 로직은 포함하지 않았습니다.

## 반영 기준

- CR-06 대형 메인 스크린을 기준 패널로 함께 보여줍니다.
- CR-07은 CR-06 표시 면을 침범하지 않고, 메인 스크린보다 조금 더 위쪽 오른쪽 벽면에 붙는 얇은 가로형 보조 화면입니다.
- 디스플레이에는 `Assets/Heavy Station Kit/_common/Textures/GUI/C2_ElC2Disp.png`를 화면 면 전체에 꽉 차게 넣었습니다.
- 벽면 장착 패드, 방진 가스켓, 좌측 브래킷, 우측 케이블 소켓, 상부 케이블 레이스웨이로 실제 Unity 배치 시 부품 경계를 드러냈습니다.
- 이 샘플은 승인 전 검토용이며 Unity 씬에는 적용하지 않았습니다.

## 포함

- `blender/control_room_aux_screen.blend`
- `exports/control_room_aux_screen.fbx`
- `exports/control_room_aux_screen.glb`
- `renders/*.png` 5개 구도
- `index.html`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 제외

- 실제 구역 상태 UI
- CCTV 영상 피드
- 상호작용 로직
- Unity 씬 배치
- CR-08 세로형 보조 스크린 묶음
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_context_overview.png", "01 CR-06 기준과 CR-07 오른쪽 상단 배치"),
        ("02_front_alignment.png", "02 전면 정렬과 화면 비율"),
        ("03_aux_screen_closeup.png", "03 CR-07 베젤과 C2_ElC2Disp 표시 면"),
        ("04_side_mount_depth.png", "04 측면 장착 깊이와 케이블 소켓"),
        ("05_top_right_relation.png", "05 대형 메인 스크린 오른쪽 상단 관계"),
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
  <title>control_room_aux_screen review</title>
  <style>
    body {{ margin: 0; background: #111514; color: #e9e1d2; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #c9c0ad; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #3d4544; background: #1b2020; padding: 10px; }}
    img {{ width: 100%; display: block; background: #0b0e0e; }}
    figcaption {{ margin-top: 8px; color: #ded2bc; font-size: 14px; }}
    @media (max-width: 800px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>control_room_aux_screen</h1>
  <p>CR-07 가로형 보조 스크린 샘플입니다. CR-06 대형 메인 스크린보다 조금 더 위쪽의 오른쪽 바깥 영역에 붙는 얇은 보조 화면으로 설계했고, 디스플레이에는 C2_ElC2Disp.png 에셋을 화면 전체에 꽉 차게 넣었습니다. 화면 비율, 위쪽 배치, 장착 패드, 브래킷, 케이블 소켓을 확인할 수 있도록 구성했습니다.</p>
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
        "wall": noisy_metal("control room screen wall worn metal", (0.18, 0.23, 0.23, 1)),
        "main_mount": noisy_metal("CR-06 inactive main screen mount", (0.24, 0.27, 0.25, 1)),
        "main_screen": material("CR-06 dark inactive main screen glass", (0.012, 0.026, 0.030, 1), roughness=0.34, emission=(0.0, 0.020, 0.024, 1), emission_strength=0.10),
        "mount": noisy_metal("CR-07 wall side mounting pad", (0.21, 0.24, 0.22, 1)),
        "rubber": material("CR-07 black vibration gasket", (0.010, 0.012, 0.012, 1), roughness=0.94),
        "frame": noisy_metal("CR-07 compact armored frame", (0.29, 0.32, 0.29, 1)),
        "glass": material("CR-07 smoked glass lip", (0.010, 0.020, 0.024, 1), roughness=0.28),
        "screen": material("CR-07 inactive auxiliary display", (0.010, 0.055, 0.060, 1), roughness=0.36, emission=(0.02, 0.18, 0.19, 1), emission_strength=0.18),
        "display_texture": image_material("CR-07 C2_ElC2Disp full display texture", DISPLAY_TEXTURE_PATH, emission_strength=0.35),
        "screen_line": material("CR-07 dim screen division lines", (0.18, 0.72, 0.70, 1), roughness=0.55, emission=(0.08, 0.40, 0.38, 1), emission_strength=0.30),
        "header_strip": material("CR-07 dim inactive telemetry strip", (0.26, 0.45, 0.38, 1), roughness=0.58, emission=(0.10, 0.22, 0.18, 1), emission_strength=0.18),
        "bracket": noisy_metal("CR-07 dark screen bracket", (0.12, 0.13, 0.12, 1)),
        "socket": noisy_metal("CR-07 right cable socket metal", (0.08, 0.09, 0.09, 1)),
        "conduit": noisy_metal("CR-07 upper wall conduit", (0.045, 0.055, 0.055, 1)),
        "bolt": noisy_metal("CR-07 recessed bolt heads", (0.36, 0.36, 0.31, 1)),
        "latch": noisy_metal("CR-07 small service latch", (0.38, 0.40, 0.34, 1)),
        "rail": noisy_metal("main screen bay structural rail", (0.31, 0.33, 0.29, 1)),
        "zone": material("CR-07 amber reserved placement outline", (0.95, 0.50, 0.10, 1), roughness=0.52, emission=(0.45, 0.16, 0.02, 1), emission_strength=0.22),
        "seam": noisy_metal("right wall subtle service seam", (0.09, 0.11, 0.11, 1)),
        "label": material("control room pale placement label", (0.78, 0.88, 0.84, 1), roughness=0.70, emission=(0.16, 0.30, 0.28, 1), emission_strength=0.12),
    }

    build_aux_screen_sample(mats)
    add_render_lights()

    cameras = [
        ("context_overview", (4.30, -6.4, 2.70), (1.70, -0.12, 2.05), 29, "01_context_overview.png", None),
        ("front_alignment", (1.60, -6.7, 2.10), (1.60, -0.12, 2.10), 48, "02_front_alignment.png", 6.20),
        ("aux_screen_closeup", (4.05, -3.85, 2.96), (4.05, -0.22, 2.96), 67, "03_aux_screen_closeup.png", 1.95),
        ("side_mount_depth", (5.25, -2.50, 2.75), (3.85, -0.14, 2.95), 55, "04_side_mount_depth.png", None),
        ("top_right_relation", (3.35, -4.45, 3.15), (3.35, -0.18, 2.85), 42, "05_top_right_relation.png", 2.70),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()
    print(f"Generated {SAMPLE_NAME} Blender sample at {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
