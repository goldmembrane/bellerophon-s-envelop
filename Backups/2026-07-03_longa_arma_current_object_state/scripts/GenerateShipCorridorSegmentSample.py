from __future__ import annotations

import json
import math
import shutil
from datetime import date
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_NAME = "ship_corridor_segment"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"

CORRIDOR_LENGTH = 6.2
CORRIDOR_WIDTH = 1.28
FLOOR_THICKNESS = 0.16
WALL_HEIGHT = 1.22
WALL_THICKNESS = 0.18
SHUTTER_HOUSING_HEIGHT = 0.18
SHUTTER_SIDE_OVERLAP = 0.08
SHUTTER_FLOOR_OVERLAP = 0.04


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR):
        path.mkdir(parents=True, exist_ok=True)


def clean_generated_files() -> None:
    for directory in (BLENDER_DIR, RENDER_DIR, EXPORT_DIR):
        if directory.exists():
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
    mat = material(name, base, metallic=0.20, roughness=0.86)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    noise = nodes.new(type="ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 32
    noise.inputs["Detail"].default_value = 9
    noise.inputs["Roughness"].default_value = 0.58

    ramp = nodes.new(type="ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[0].color = (base[0] * 0.50, base[1] * 0.50, base[2] * 0.50, 1)
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
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.rotation_euler = rot
    obj.data.materials.append(mat)
    obj.parent = parent
    if bevel_width > 0:
        bevel = obj.modifiers.new("hard surface bevel", "BEVEL")
        bevel.width = bevel_width
        bevel.segments = 1
        obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_text_label(
    name: str,
    parent: bpy.types.Object,
    text: str,
    loc: tuple[float, float, float],
    rot_z: float,
    mat: bpy.types.Material,
    *,
    size: float = 0.20,
) -> bpy.types.Object:
    bpy.ops.object.text_add(location=loc, rotation=(0.0, 0.0, rot_z))
    obj = bpy.context.object
    obj.name = name
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.006
    obj.data.materials.append(mat)
    obj.parent = parent
    return obj


def add_ramp_floor(
    name: str,
    parent: bpy.types.Object,
    origin: tuple[float, float],
    *,
    length: float,
    width: float,
    low_z: float,
    high_z: float,
    thickness: float,
    mat: bpy.types.Material,
) -> bpy.types.Object:
    ox, oy = origin
    half_l = length * 0.5
    half_w = width * 0.5
    vertices = [
        (ox - half_l, oy - half_w, low_z),
        (ox - half_l, oy + half_w, low_z),
        (ox + half_l, oy + half_w, high_z),
        (ox + half_l, oy - half_w, high_z),
        (ox - half_l, oy - half_w, low_z - thickness),
        (ox - half_l, oy + half_w, low_z - thickness),
        (ox + half_l, oy + half_w, high_z - thickness),
        (ox + half_l, oy - half_w, high_z - thickness),
    ]
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    mesh = bpy.data.meshes.new(name + " mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def build_materials() -> dict[str, bpy.types.Material]:
    return {
        "base": noisy_metal("corridor sample dark inspection base", (0.065, 0.072, 0.074, 1)),
        "floor": noisy_metal("corridor worn modular floor", (0.17, 0.19, 0.18, 1)),
        "ramp_floor": noisy_metal("corridor down slope worn floor", (0.20, 0.18, 0.13, 1)),
        "wall": noisy_metal("corridor armored side wall", (0.10, 0.13, 0.13, 1)),
        "rail": noisy_metal("corridor raised side rail", (0.30, 0.31, 0.28, 1)),
        "rib": noisy_metal("corridor deck cross rib", (0.47, 0.45, 0.38, 1)),
        "slot": noisy_metal("black closure shutter recessed slot", (0.020, 0.025, 0.027, 1)),
        "shutter": noisy_metal("full height lowered corridor closure shutter", (0.055, 0.065, 0.067, 1)),
        "shutter_face": noisy_metal("dark armored shutter face panels", (0.12, 0.135, 0.13, 1)),
        "label_back": noisy_metal("corridor label mounting plate", (0.030, 0.037, 0.038, 1)),
        "label": material("white corridor stencil paint", (0.86, 0.91, 0.86, 1), roughness=0.66, emission=(0.45, 0.52, 0.48, 1), emission_strength=0.08),
        "amber": material("amber slope caution paint", (0.95, 0.58, 0.12, 1), roughness=0.58, emission=(0.78, 0.35, 0.04, 1), emission_strength=0.18),
        "red": material("red closure warning light strip", (0.88, 0.08, 0.05, 1), roughness=0.55, emission=(0.70, 0.02, 0.01, 1), emission_strength=0.24),
        "cyan": material("cyan route display placeholder", (0.04, 0.55, 0.68, 1), roughness=0.38, emission=(0.02, 0.40, 0.55, 1), emission_strength=0.48),
        "measure": material("temporary scale blue marker", (0.16, 0.45, 0.82, 1), roughness=0.62, emission=(0.04, 0.18, 0.42, 1), emission_strength=0.16),
    }


def module_height_at(x: float, origin_x: float, length: float, low_z: float, high_z: float) -> float:
    t = max(0.0, min(1.0, (x - (origin_x - length * 0.5)) / length))
    return low_z + (high_z - low_z) * t


def add_common_corridor_parts(
    root: bpy.types.Object,
    prefix: str,
    origin: tuple[float, float],
    mats: dict[str, bpy.types.Material],
    *,
    slope: bool,
) -> None:
    ox, oy = origin
    length = CORRIDOR_LENGTH
    half_l = length * 0.5
    pitch = math.atan2(0.58, length) if slope else 0.0
    center_z = 0.18 + (0.29 if slope else 0.0)

    if slope:
        add_ramp_floor(
            prefix + " sloped floor slab",
            root,
            origin,
            length=length,
            width=CORRIDOR_WIDTH,
            low_z=0.16,
            high_z=0.74,
            thickness=FLOOR_THICKNESS,
            mat=mats["ramp_floor"],
        )
    else:
        add_box(prefix + " straight floor slab", root, (ox, oy, 0.08), (length, CORRIDOR_WIDTH, FLOOR_THICKNESS), mats["floor"], bevel_width=0.018)

    for side, side_y in (("left", oy + CORRIDOR_WIDTH * 0.5 + WALL_THICKNESS * 0.5), ("right", oy - CORRIDOR_WIDTH * 0.5 - WALL_THICKNESS * 0.5)):
        add_box(
            f"{prefix} {side} armored wall",
            root,
            (ox, side_y, center_z + WALL_HEIGHT * 0.5),
            (length, WALL_THICKNESS, WALL_HEIGHT),
            mats["wall"],
            (0.0, -pitch, 0.0),
            bevel_width=0.014,
        )

    for index, x in enumerate((-2.32, -1.15, 0.0, 1.15, 2.32), start=1):
        z = module_height_at(ox + x, ox, length, 0.22, 0.80) if slope else 0.22
        add_box(
            f"{prefix} cross deck rib {index}",
            root,
            (ox + x, oy, z),
            (0.09, CORRIDOR_WIDTH + 0.18, 0.085),
            mats["rib"],
            bevel_width=0.008,
        )

    for end_name, x in (("low end" if slope else "end a", ox - half_l + 0.26), ("high end" if slope else "end b", ox + half_l - 0.26)):
        floor_z = module_height_at(x, ox, length, 0.16, 0.74) if slope else 0.16
        ceiling_z = floor_z + WALL_HEIGHT
        housing_vertical_span = SHUTTER_HOUSING_HEIGHT / math.cos(pitch)
        shutter_bottom_z = floor_z - SHUTTER_FLOOR_OVERLAP
        shutter_height = WALL_HEIGHT - housing_vertical_span + SHUTTER_FLOOR_OVERLAP
        shutter_center_z = shutter_bottom_z + shutter_height * 0.5
        housing_z = ceiling_z - housing_vertical_span * 0.5
        shutter_width = CORRIDOR_WIDTH + SHUTTER_SIDE_OVERLAP
        add_box(
            f"{prefix} {end_name} overhead shutter housing",
            root,
            (x, oy, housing_z),
            (0.46, shutter_width, SHUTTER_HOUSING_HEIGHT),
            mats["slot"],
            (0.0, -pitch, 0.0),
            bevel_width=0.006,
        )
        add_box(
            f"{prefix} {end_name} lowered full height closure shutter",
            root,
            (x, oy, shutter_center_z),
            (0.19, shutter_width, shutter_height),
            mats["shutter"],
            bevel_width=0.006,
        )
        add_box(
            f"{prefix} {end_name} shutter center armor face",
            root,
            (x - 0.101, oy, shutter_center_z + 0.03),
            (0.032, shutter_width - 0.16, shutter_height - 0.24),
            mats["shutter_face"],
            bevel_width=0.004,
        )
        add_box(
            f"{prefix} {end_name} red full height closure warning strip",
            root,
            (x - 0.122, oy, floor_z + 0.28),
            (0.035, shutter_width - 0.10, 0.075),
            mats["red"],
            bevel_width=0.004,
        )

    label_x = ox - 0.72
    label_y = oy + CORRIDOR_WIDTH * 0.22
    label_z = module_height_at(label_x, ox, length, 0.24, 0.82) if slope else 0.22
    add_box(
        prefix + " blank route label floor mounting plate",
        root,
        (label_x, label_y, label_z + 0.025),
        (1.22, 0.32, 0.035),
        mats["label_back"],
        (0.0, -pitch, 0.0),
        bevel_width=0.006,
    )
    add_text_label(
        prefix + " route label slot text",
        root,
        "ROUTE LABEL SLOT",
        (label_x, label_y, label_z + 0.052),
        0.0,
        mats["label"],
        size=0.095,
    )

    screen_x = ox + 0.95
    screen_z = module_height_at(screen_x, ox, length, 0.28, 0.86) if slope else 0.33
    add_box(
        prefix + " small corridor status display placeholder",
        root,
        (screen_x, oy - CORRIDOR_WIDTH * 0.5 - 0.126, screen_z + 0.05),
        (0.74, 0.075, 0.24),
        mats["cyan"],
        (0.0, -pitch, 0.0),
        bevel_width=0.006,
    )

    if slope:
        add_box(prefix + " slope amber left edge stripe", root, (ox, oy + CORRIDOR_WIDTH * 0.5 - 0.10, 0.54), (length - 0.82, 0.045, 0.035), mats["amber"], (0.0, -pitch, 0.0), bevel_width=0.004)
        add_box(prefix + " slope amber right edge stripe", root, (ox, oy - CORRIDOR_WIDTH * 0.5 + 0.10, 0.54), (length - 0.82, 0.045, 0.035), mats["amber"], (0.0, -pitch, 0.0), bevel_width=0.004)
        add_text_label(prefix + " down slope floor stencil", root, "DOWN SLOPE", (ox, oy, 0.50), 0.0, mats["amber"], size=0.18)
    else:
        add_text_label(prefix + " straight module floor stencil", root, "HORIZONTAL CORRIDOR", (ox, oy, 0.27), 0.0, mats["label"], size=0.16)


def build_scene() -> None:
    root = add_empty("ship corridor segment sample root")
    mats = build_materials()
    add_box("corridor segment review base plate", root, (0.0, -0.25, -0.05), (10.2, 7.2, 0.06), mats["base"], bevel_width=0.0)

    add_common_corridor_parts(root, "horizontal corridor sample", (-0.35, 1.55), mats, slope=False)
    add_common_corridor_parts(root, "sloped corridor sample", (-0.35, -2.15), mats, slope=True)

    add_text_label("sample title", root, "SHIP CORRIDOR SEGMENT SAMPLE", (0.0, 3.25, 0.26), 0.0, mats["label"], size=0.22)
    add_text_label("sample scope", root, "TWO STANDALONE SAMPLES - HORIZONTAL / SLOPED", (0.0, 2.88, 0.24), 0.0, mats["amber"], size=0.13)
    add_text_label("temporary scale note", root, "TEMP SCALE: WIDTH / LENGTH / SLOPE TBD", (-3.10, -3.55, 0.20), 0.0, mats["measure"], size=0.12)


def configure_rendering() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 40
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("ShipCorridorSegmentWorld")
    scene.world.color = (0.010, 0.011, 0.012)
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False


def add_render_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, 0.0, 6.2))
    top = bpy.context.object
    top.name = "large corridor segment inspection softbox"
    top.data.energy = 760
    top.data.size = 7.2

    bpy.ops.object.light_add(type="AREA", location=(-5.2, 2.8, 3.2))
    west = bpy.context.object
    west.name = "warm corridor wall fill"
    west.data.energy = 165
    west.data.size = 3.6
    west.data.color = (1.0, 0.72, 0.42)

    bpy.ops.object.light_add(type="AREA", location=(5.4, -2.6, 3.2))
    east = bpy.context.object
    east.name = "cool corridor display fill"
    east.data.energy = 170
    east.data.size = 3.6
    east.data.color = (0.50, 0.76, 1.0)


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
    camera.name = "ship corridor segment camera " + name
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
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "ship_corridor_segment.blend"))
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "ship_corridor_segment.fbx"), add_leaf_bones=False, bake_space_transform=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "ship_corridor_segment.glb"), export_format="GLB")


def write_docs() -> None:
    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "SHIP-CORRIDOR-SEGMENT",
        "title": "화물선 복도 단품 샘플",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "generatedDate": date.today().isoformat(),
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt - 화물선은 6개 방을 이어주는 복도가 존재한다.",
            "docs/GAME_DESIGN_SOURCE.txt - 조종실/동력실/통제실/무기실에서 운송창고로 이어지는 복도는 아래쪽 경사가 존재한다고 명시되어 있다.",
            "docs/GAME_DESIGN_SOURCE.txt - 통제실 손상도에 따라 복도 폐쇄가 발생하며, 폐쇄된 복도 문을 임시 개방하는 장치와 복도 정화 장치가 존재한다.",
            "docs/MVP_IMPLEMENTATION_ORDER.md - 방 크기, 복도 폭, 경사 길이는 임시 스케일로 진행하고 추후 실제 길이를 확정한다.",
        ],
        "includedParts": [
            "방 연결 없이 분리된 수평 복도 샘플 1개",
            "방 연결 없이 분리된 아래쪽 경사 복도 샘플 1개",
            "바닥, 양쪽 벽, 크로스 리브",
            "복도 폭 끝까지 닿고 바닥 틈 없이 내려와 양쪽 벽 안쪽 면과 맞물리는 셔터 상부 블록과 폐쇄 셔터",
            "경사 복도 경사각에 맞춰 기울어진 셔터 상부 블록",
            "폐쇄 셔터 경고 라이트 스트립",
            "방향 문구를 붙일 수 있는 blank route label mounting plate",
            "상태 표시 화면 자리와 임시 스케일 표시",
            "Blender 원본 모델, FBX, GLB 범용 모델 파일",
        ],
        "excludedParts": [
            "구역끼리 연결한 네트워크 배치",
            "조종실, 운송창고, 무기실, 비품실, 동력실, 통제실 룸 앵커",
            "Unity 씬, 프리팹, 런타임 자산 반영",
            "충돌체, 네비게이션, 플레이어 이동 로직",
            "실제 복도 폐쇄/개방/정화 시스템 로직",
            "최종 복도 폭, 길이, 경사 길이 확정",
        ],
        "generatedFiles": [
            "blender/ship_corridor_segment.blend",
            "exports/ship_corridor_segment.fbx",
            "exports/ship_corridor_segment.glb",
            "renders/01_horizontal_overview.png",
            "renders/02_horizontal_top.png",
            "renders/03_horizontal_shutter_front.png",
            "renders/04_horizontal_side.png",
            "renders/05_sloped_overview.png",
            "renders/06_sloped_top.png",
            "renders/07_sloped_shutter_front.png",
            "renders/08_sloped_side.png",
            "index.html",
            "README.md",
            "ASSET_MANIFEST.json",
            "APPROVAL_STATUS.json",
        ],
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "SHIP-CORRIDOR-SEGMENT",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산, UI 흐름에 반영하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# ship_corridor_segment

화물선 복도 단품 승인용 Blender 샘플입니다.

## 목적

각 구역을 실제로 연결하기 전에, 복도 그 자체의 형태를 검토하기 위한 `artSample/` 전용 샘플입니다.
방 또는 구역 앵커는 포함하지 않았고, 수평 복도 샘플 1개와 경사 복도 샘플 1개만 서로 분리해서 배치했습니다.

## 원본 기준

- 화물선에는 6개 방을 이어주는 복도가 존재합니다.
- 조종실, 동력실, 통제실, 무기실에서 운송창고로 이어지는 복도는 아래쪽 경사가 존재한다고 명시되어 있습니다.
- 통제실 손상도에 따라 복도 폐쇄가 발생합니다.
- 폐쇄된 복도 문을 임시 개방하는 장치와 복도 전체를 임시 폐쇄하고 정화하는 장치가 존재합니다.
- 방 크기, 복도 폭, 경사 길이는 임시 스케일로 진행하고 추후 실제 값을 확정합니다.

## 샘플 구성

- 수평 복도 샘플 1개
- 아래쪽 경사 복도 샘플 1개
- 각 샘플의 전체 사선, 상단, 셔터 정면, 측면 구도 렌더
- 바닥, 양쪽 벽, 크로스 리브
- 복도 폭 끝까지 닿고 바닥 틈 없이 내려와 양쪽 벽 안쪽 면과 맞물리는 셔터 상부 블록과 폐쇄 셔터
- 경사 복도 경사각에 맞춰 기울어진 셔터 상부 블록
- 폐쇄 셔터 경고 라이트 스트립
- 방향 문구 부착용 blank route label mounting plate
- 상태 표시 화면 자리와 임시 스케일 표시

## 제외

- 구역끼리 연결한 네트워크 배치
- 조종실, 운송창고, 무기실, 비품실, 동력실, 통제실 룸 앵커
- Unity 씬, 프리팹, 런타임 자산 반영
- 충돌체, 네비게이션, 플레이어 이동 로직
- 실제 복도 폐쇄/개방/정화 시스템 로직
- 최종 복도 폭, 길이, 경사 길이 확정
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    images = [
        ("01_horizontal_overview.png", "01 수평 복도 전체 사선"),
        ("02_horizontal_top.png", "02 수평 복도 상단"),
        ("03_horizontal_shutter_front.png", "03 수평 복도 셔터 정면"),
        ("04_horizontal_side.png", "04 수평 복도 측면"),
        ("05_sloped_overview.png", "05 경사 복도 전체 사선"),
        ("06_sloped_top.png", "06 경사 복도 상단"),
        ("07_sloped_shutter_front.png", "07 경사 복도 셔터 정면"),
        ("08_sloped_side.png", "08 경사 복도 측면"),
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
  <title>ship_corridor_segment review</title>
  <style>
    body {{ margin: 0; background: #101313; color: #e7e0d5; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    p {{ color: #c7beb0; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    figure {{ margin: 0; border: 1px solid #444a47; background: #1b2020; padding: 10px; }}
    img {{ width: 100%; display: block; background: #080b0b; }}
    figcaption {{ margin-top: 8px; color: #ded3bd; font-size: 14px; }}
    @media (max-width: 800px) {{ .grid {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>ship_corridor_segment</h1>
  <p>화물선 복도 단품 승인용 Blender 샘플입니다. 방 또는 구역 앵커 없이 수평 복도 샘플 1개와 아래쪽 경사 복도 샘플 1개만 배치했습니다. 각 복도에는 복도 폭 끝까지 닿고 바닥 틈 없이 내려와 양쪽 벽 안쪽 면과 맞물리는 셔터 상부 블록과 폐쇄 셔터, 경고 라이트 스트립, 방향 문구 부착 위치, 상태 표시 화면 자리를 넣었습니다. 경사 복도의 셔터 상부 블록은 복도 경사각에 맞춰 기울였습니다. 수평 복도와 경사 복도를 각각 전체 사선, 상단, 셔터 정면, 측면 구도로 확인할 수 있습니다. 이 샘플은 artSample 검토용이며 Unity 씬, 프리팹, 런타임 자산에는 반영하지 않았습니다.</p>
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
    build_scene()
    add_render_lights()

    cameras = [
        ("horizontal overview", (5.40, 5.05, 3.20), (-0.35, 1.55, 0.82), 46, "01_horizontal_overview.png", None),
        ("horizontal top", (-0.35, 1.55, 6.15), (-0.35, 1.55, 0.0), 50, "02_horizontal_top.png", 7.0),
        ("horizontal shutter front", (6.35, 1.55, 1.70), (2.50, 1.55, 0.86), 38, "03_horizontal_shutter_front.png", None),
        ("horizontal side", (-0.35, 7.20, 2.05), (-0.35, 1.55, 0.80), 38, "04_horizontal_side.png", None),
        ("sloped overview", (5.40, -6.75, 3.35), (-0.35, -2.15, 0.94), 46, "05_sloped_overview.png", None),
        ("sloped top", (-0.35, -2.15, 6.45), (-0.35, -2.15, 0.25), 50, "06_sloped_top.png", 7.0),
        ("sloped shutter front", (6.35, -2.15, 1.92), (2.50, -2.15, 1.04), 38, "07_sloped_shutter_front.png", None),
        ("sloped side", (-0.35, -7.80, 2.25), (-0.35, -2.15, 0.94), 38, "08_sloped_side.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale=ortho_scale)
        render_camera(camera, RENDER_DIR / output)

    export_assets()
    write_docs()


if __name__ == "__main__":
    main()
