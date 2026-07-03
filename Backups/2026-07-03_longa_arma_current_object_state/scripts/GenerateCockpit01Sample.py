from __future__ import annotations

import json
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "cockpit_01"
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR):
        path.mkdir(parents=True, exist_ok=True)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def material(name: str, color: tuple[float, float, float, float], roughness: float = 0.82) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = 0.0
    mat.diffuse_color = color
    return mat


def add_box(
    name: str,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)

    bevel = obj.modifiers.new("small structural bevel", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 1
    obj.modifiers.new("weighted normals", "WEIGHTED_NORMAL")
    return obj


def add_empty(name: str) -> bpy.types.Object:
    empty = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(empty)
    return empty


def look_at(obj: bpy.types.Object, target: tuple[float, float, float]) -> None:
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_camera(
    name: str,
    loc: tuple[float, float, float],
    target: tuple[float, float, float],
    lens: float,
    orthographic_scale: float | None = None,
) -> bpy.types.Object:
    camera_data = bpy.data.cameras.new(name)
    camera = bpy.data.objects.new(name, camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = loc
    if orthographic_scale is None:
        camera_data.lens = lens
    else:
        camera_data.type = "ORTHO"
        camera_data.ortho_scale = orthographic_scale
    camera_data.clip_end = 100
    if orthographic_scale is not None and loc[0] == target[0] and loc[1] == target[1]:
        camera.rotation_euler = (0.0, 0.0, 0.0)
    else:
        look_at(camera, target)
    return camera


def render_camera(camera: bpy.types.Object, output_name: str) -> None:
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.filepath = str(RENDER_DIR / output_name)
    bpy.ops.render.render(write_still=True)


def configure_rendering() -> None:
    scene = bpy.context.scene
    for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            continue

    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("Cockpit01World")
    scene.world.color = (0.025, 0.028, 0.027)
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0
    scene.view_settings.gamma = 1


def add_lights() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -2.5, 6.0))
    key = bpy.context.object
    key.name = "large soft inspection light"
    key.data.energy = 500
    key.data.size = 7.0

    bpy.ops.object.light_add(type="POINT", location=(0, 2.8, 2.6))
    fill = bpy.context.object
    fill.name = "front aperture fill light"
    fill.data.energy = 95
    fill.data.color = (0.6, 0.72, 0.78)


def build_cockpit_structure() -> None:
    wall_mat = material("structure wall clay", (0.32, 0.34, 0.32, 1))
    floor_mat = material("dark structural floor", (0.12, 0.13, 0.12, 1))
    cut_mat = material("future opening edge", (0.72, 0.52, 0.18, 1), 0.74)
    frame_mat = material("upper structural frame", (0.20, 0.22, 0.21, 1))

    root = add_empty("Cockpit 01 - structure only")

    # Source-plan blockout: broad forward room with a narrower rear stem.
    front_half_width = 5.0
    front_y = 4.0
    cross_rear_y = -1.25
    stem_back_y = -4.35
    stem_half_width = 1.8
    wall = 0.24
    height = 3.0
    wall_z = height * 0.5

    objects: list[bpy.types.Object] = []

    # T-shaped floor, split into two slabs so the plan is readable from above.
    objects.append(add_box("front wide bay floor slab", (0, 1.38, -0.09), (10.0, 5.25, 0.18), floor_mat))
    objects.append(add_box("rear center stem floor slab", (0, -2.80, -0.09), (3.6, 3.1, 0.18), floor_mat))

    # Broad bay side walls. The gaps are only future direction marks, not corridor meshes.
    side_segments = [(-0.58, 1.35), (3.35, 1.30)]
    for side_name, x in (("left", -front_half_width), ("right", front_half_width)):
        for index, (y, length) in enumerate(side_segments, start=1):
            objects.append(add_box(f"{side_name} bay outer wall segment {index}", (x, y, wall_z), (wall, length, height), wall_mat))

    # Rear shoulder wall of the broad bay, split around the narrow rear stem.
    rear_side_width = front_half_width - stem_half_width
    objects.append(add_box("rear bay wall left shoulder", (-3.4, cross_rear_y, wall_z), (rear_side_width, wall, height), wall_mat))
    objects.append(add_box("rear bay wall right shoulder", (3.4, cross_rear_y, wall_z), (rear_side_width, wall, height), wall_mat))

    # Narrow rear stem side walls. The back remains an open structural mouth.
    stem_center_y = (cross_rear_y + stem_back_y) * 0.5
    stem_length = abs(cross_rear_y - stem_back_y)
    objects.append(add_box("rear stem left wall", (-stem_half_width, stem_center_y, wall_z), (wall, stem_length, height), wall_mat))
    objects.append(add_box("rear stem right wall", (stem_half_width, stem_center_y, wall_z), (wall, stem_length, height), wall_mat))

    # Forward opening is represented only as a broad structural aperture.
    objects.append(add_box("front aperture upper lintel", (0, front_y, height + 0.08), (10.1, wall, 0.28), frame_mat))
    objects.append(add_box("front aperture left return", (-front_half_width, front_y, wall_z), (wall, wall, height), frame_mat))
    objects.append(add_box("front aperture right return", (front_half_width, front_y, wall_z), (wall, wall, height), frame_mat))

    # Upper rim only, leaving the room open for inspection.
    objects.append(add_box("upper left bay rim", (-front_half_width, 1.38, height + 0.18), (wall, 5.25, 0.22), frame_mat))
    objects.append(add_box("upper right bay rim", (front_half_width, 1.38, height + 0.18), (wall, 5.25, 0.22), frame_mat))
    objects.append(add_box("upper rear left shoulder rim", (-3.4, cross_rear_y, height + 0.18), (rear_side_width, wall, 0.22), frame_mat))
    objects.append(add_box("upper rear right shoulder rim", (3.4, cross_rear_y, height + 0.18), (rear_side_width, wall, 0.22), frame_mat))
    objects.append(add_box("upper rear stem left rim", (-stem_half_width, stem_center_y, height + 0.18), (wall, stem_length, 0.22), frame_mat))
    objects.append(add_box("upper rear stem right rim", (stem_half_width, stem_center_y, height + 0.18), (wall, stem_length, 0.22), frame_mat))

    # Floor-edge markers show planned directions without adding connected corridors.
    objects.append(add_box("left future opening floor edge only", (-4.84, 1.40, 0.025), (0.42, 2.45, 0.05), cut_mat))
    objects.append(add_box("right future opening floor edge only", (4.84, 1.40, 0.025), (0.42, 2.45, 0.05), cut_mat))
    objects.append(add_box("rear future cargo opening edge only", (0, stem_back_y + 0.12, 0.025), (3.35, 0.42, 0.05), cut_mat))
    objects.append(add_box("front broad aperture edge only", (0, front_y - 0.12, 0.025), (9.5, 0.42, 0.05), cut_mat))

    for obj in objects:
        obj.parent = root


def write_docs() -> None:
    readme = """# cockpit_01

조종실 전체 구조만 확인하기 위한 Blender 시안입니다.

## 원본 기획서에서 확인한 구조

- 화물선은 조종실, 운송창고, 무기실, 비품실, 동력실, 통제실 6구역으로 구성됩니다.
- 조종실은 위에서 내려다보면 `ㅜ` 형태입니다.
- 전면 기준 왼쪽은 동력실 방향, 오른쪽은 통제실 방향, 뒤쪽은 운송창고 방향입니다.
- 뒤쪽 운송창고 방향에는 아래쪽으로 내려가는 경사가 존재합니다.
- 조종대, 유리창 세부 구조, 콘솔, 화면 UI는 이번 시안 범위에서 제외했습니다.

## 이번 시안 범위

- 방 자체의 큰 평면 실루엣과 벽체 구조만 제작했습니다.
- 복도는 연결하지 않았습니다.
- 좌측, 우측, 후방, 전방의 연결 예정 위치는 바닥 가장자리 표시만 남겼습니다.
- 전면은 넓은 개구부만 표현했고 유리 패널이나 프레임 내부 구조는 만들지 않았습니다.
- 천장은 구조 둘레만 두어 위에서 평면을 확인할 수 있게 했습니다.

## 승인 후 Unity 반영 방식

사용자 승인 후 이 구조를 조종실 바깥/방 껍데기 기준으로 옮깁니다.
조종대, 유리 디테일, 콘솔, 복도 연결은 별도 승인 단계로 분리합니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    approval = {
        "sample": "cockpit_01",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "scope": "조종실 전체 구조 블록아웃. 조종대, 유리 디테일, 콘솔, 복도 연결 제외.",
        "sourceBasis": [
            "docs/GAME_DESIGN_SOURCE.txt: 화물선 6구역, 조종실 ㅜ자 구조, 좌/우/후방 방향",
            "docs/MVP_IMPLEMENTATION_ORDER.md: 조종실 전면 및 연결 방향 식별 요구",
        ],
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    images = [
        ("01_top.png", "상단 구조"),
        ("02_front.png", "전면 개구부"),
        ("03_rear.png", "후방 중심 줄기"),
        ("04_diag.png", "대각 구조"),
    ]
    cards = "\n".join(
        f'<figure><a href="renders/{name}"><img src="renders/{name}" alt="{label}"></a><figcaption>{label}</figcaption></figure>'
        for name, label in images
    )
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>cockpit_01</title>
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
  <h1>cockpit_01</h1>
  <p>조종실 방 자체의 전체 구조만 확인하는 Blender 시안입니다. 복도, 조종대, 유리 내부 구조, 콘솔, 화면은 포함하지 않았습니다.</p>
  <section class="grid">
    {cards}
  </section>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def export_assets() -> None:
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "cockpit_01.blend"))
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "cockpit_01.glb"), export_format="GLB")
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "cockpit_01.fbx"), use_selection=False)


def main() -> None:
    ensure_dirs()
    reset_scene()
    configure_rendering()
    build_cockpit_structure()
    add_lights()

    cameras = [
        ("cam_top", (0, -0.2, 13.5), (0, -0.2, 0), 42, "01_top.png", 11.2),
        ("cam_front", (0, 8.5, 3.0), (0, 0.8, 1.1), 35, "02_front.png", None),
        ("cam_rear", (0, -9.5, 4.2), (0, -1.6, 1.1), 38, "03_rear.png", None),
        ("cam_diag", (8.2, -7.4, 5.3), (0, 0, 1.2), 34, "04_diag.png", None),
    ]
    for name, loc, target, lens, output, ortho_scale in cameras:
        camera = add_camera(name, loc, target, lens, ortho_scale)
        render_camera(camera, output)

    export_assets()
    write_docs()
    print(f"cockpit_01 sample generated: {SAMPLE_ROOT}")


if __name__ == "__main__":
    main()
