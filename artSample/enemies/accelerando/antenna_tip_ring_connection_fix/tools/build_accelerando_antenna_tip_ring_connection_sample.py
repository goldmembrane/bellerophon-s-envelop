# -*- coding: utf-8 -*-
import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE_BLEND = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "accelerando"
    / "antenna_connection_color_fix"
    / "exports"
    / "accelerando_connected_colored_sample.blend"
)
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / "accelerando" / "antenna_tip_ring_connection_fix"
EXPORT_DIR = SAMPLE_ROOT / "exports"
RENDER_DIR = SAMPLE_ROOT / "renders"


def ensure_dirs():
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    RENDER_DIR.mkdir(parents=True, exist_ok=True)


def open_source_sample():
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))


def get_material(name):
    mat = bpy.data.materials.get(name)
    if mat:
        return mat

    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.04, 0.025, 0.018, 1.0)
    bsdf.inputs["Metallic"].default_value = 0.82
    bsdf.inputs["Roughness"].default_value = 0.48
    return mat


def remove_previous_fix_objects():
    prefixes = (
        "Accelerando_Left_AntennaTip_MountedAnchor",
        "Accelerando_Right_AntennaTip_MountedAnchor",
        "RenderOnly_",
        "Key_Area_Light",
        "Rim_Area_Light",
        "Render_Camera",
    )
    for obj in list(bpy.data.objects):
        if obj.name.startswith(prefixes):
            bpy.data.objects.remove(obj, do_unlink=True)


def add_torus(name, location, material, rotation=(0.0, 0.0, 0.0), scale=(1.0, 1.0, 1.0), radius=0.08, minor=0.018):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=radius,
        minor_radius=minor,
        major_segments=28,
        minor_segments=10,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    obj.scale = scale
    obj.data.materials.append(material)
    return obj


def add_cylinder_between(name, start, end, material, radius=0.024, vertices=18):
    start = Vector(start)
    end = Vector(end)
    midpoint = (start + end) * 0.5
    direction = end - start
    length = direction.length
    if length <= 0.0001:
        return None

    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=length,
        location=midpoint,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(material)
    return obj


def add_sphere(name, location, material, radius=0.035, scale=(1.0, 1.0, 1.0)):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=20,
        ring_count=10,
        radius=radius,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    obj.scale = scale
    obj.data.materials.append(material)
    return obj


def add_box(name, location, material, scale=(0.1, 0.04, 0.02), rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    return obj


def add_tip_anchor_geometry():
    metal = get_material("Accelerando rusty iron mace and chain")

    for sign, side in [(-1.0, "Left"), (1.0, "Right")]:
        chain_ring = bpy.data.objects.get(f"Accelerando_{side}_AntennaTip_Ring")
        if not chain_ring:
            raise RuntimeError(f"Missing antenna tip ring object for {side}")

        chain_center = Vector(chain_ring.location)
        mounted_hoop_center = chain_center
        tip_socket = chain_center + Vector((sign * -0.022, 0.0, 0.052))
        lower_pin = chain_center + Vector((0.0, 0.0, -0.018))
        upper_pin = tip_socket + Vector((0.0, 0.0, 0.010))
        saddle_center = chain_center + Vector((sign * -0.014, -0.012, 0.048))

        add_torus(
            f"Accelerando_{side}_AntennaTip_MountedAnchorHoop",
            mounted_hoop_center,
            metal,
            rotation=(math.radians(88), 0.0, math.radians(8 * sign)),
            scale=(0.98, 1.30, 1.0),
            radius=0.092,
            minor=0.022,
        )
        add_torus(
            f"Accelerando_{side}_AntennaTip_MountedAnchorCollar",
            tip_socket,
            metal,
            rotation=(0.0, math.radians(8 * sign), 0.0),
            scale=(1.12, 0.72, 0.52),
            radius=0.078,
            minor=0.019,
        )
        add_box(
            f"Accelerando_{side}_AntennaTip_MountedAnchorSaddle",
            saddle_center,
            metal,
            scale=(0.165, 0.048, 0.030),
            rotation=(0.0, math.radians(4 * sign), math.radians(2 * sign)),
        )
        add_cylinder_between(
            f"Accelerando_{side}_AntennaTip_MountedAnchorPin",
            upper_pin,
            lower_pin,
            metal,
            radius=0.024,
            vertices=18,
        )
        add_cylinder_between(
            f"Accelerando_{side}_AntennaTip_MountedAnchorStem",
            tip_socket + Vector((sign * 0.006, 0.0, -0.006)),
            mounted_hoop_center + Vector((sign * 0.006, 0.0, 0.006)),
            metal,
            radius=0.026,
            vertices=18,
        )
        for y_offset, label in [(-0.040, "FrontYoke"), (0.030, "BackYoke")]:
            add_cylinder_between(
                f"Accelerando_{side}_AntennaTip_MountedAnchor{label}",
                chain_center + Vector((sign * -0.014, y_offset, 0.054)),
                chain_center + Vector((sign * -0.004, y_offset, -0.010)),
                metal,
                radius=0.017,
                vertices=14,
            )
        add_sphere(
            f"Accelerando_{side}_AntennaTip_MountedAnchorRivet",
            tip_socket + Vector((0.0, -0.020, -0.008)),
            metal,
            radius=0.036,
            scale=(1.0, 0.75, 0.75),
        )

        chain_ring.location = chain_center


def shade_model_objects():
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            try:
                bpy.ops.object.shade_flat()
            finally:
                obj.select_set(False)


def model_objects():
    return [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and not obj.name.startswith("RenderOnly_")
    ]


def calculate_bounds(objects):
    corners = []
    for obj in objects:
        corners.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    min_corner = Vector((min(v.x for v in corners), min(v.y for v in corners), min(v.z for v in corners)))
    max_corner = Vector((max(v.x for v in corners), max(v.y for v in corners), max(v.z for v in corners)))
    return min_corner, max_corner


def export_sample(objects):
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_DIR / "accelerando_antenna_tip_ring_connection_sample.glb"),
        export_format="GLB",
        use_selection=True,
    )
    bpy.ops.wm.save_as_mainfile(filepath=str(EXPORT_DIR / "accelerando_antenna_tip_ring_connection_sample.blend"))


def create_render_material(name, color_a, color_b=None, metallic=0.0, roughness=0.7):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if color_b is None:
        bsdf.inputs["Base Color"].default_value = color_a
    else:
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 18.0
        noise.inputs["Detail"].default_value = 7.0
        ramp = nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].color = color_a
        ramp.color_ramp.elements[1].color = color_b
        links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def setup_render_scene():
    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 96
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "High Contrast"
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 1000

    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.color = (0.78, 0.76, 0.71)

    floor_mat = create_render_material(
        "RenderOnly_WarmStoneFloor",
        (0.64, 0.60, 0.53, 1.0),
        (0.82, 0.79, 0.71, 1.0),
        roughness=0.72,
    )
    bpy.ops.mesh.primitive_plane_add(size=6.0, location=(0, 0, -0.012))
    floor = bpy.context.object
    floor.name = "RenderOnly_Floor"
    floor.data.materials.append(floor_mat)

    key_data = bpy.data.lights.new("Key_Area_Light", "AREA")
    key_data.energy = 900
    key_data.size = 4.5
    key = bpy.data.objects.new("Key_Area_Light", key_data)
    bpy.context.collection.objects.link(key)
    key.location = (0, -4.8, 5.8)

    rim_data = bpy.data.lights.new("Rim_Area_Light", "AREA")
    rim_data.energy = 260
    rim_data.size = 3.0
    rim = bpy.data.objects.new("Rim_Area_Light", rim_data)
    bpy.context.collection.objects.link(rim)
    rim.location = (-4.0, 3.2, 3.8)

    camera_data = bpy.data.cameras.new("Render_Camera")
    camera = bpy.data.objects.new("Render_Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    camera.data.type = "ORTHO"
    camera.data.lens = 70
    return camera


def look_at(camera, target):
    direction = target - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_view(camera, objects, filename, direction, ortho_multiplier=1.28):
    min_corner, max_corner = calculate_bounds(objects)
    center = (min_corner + max_corner) * 0.5
    size = max_corner - min_corner
    distance = max(size.x, size.y, size.z) * 3.6
    camera.location = center + Vector(direction).normalized() * distance + Vector((0, 0, size.z * 0.12))
    look_at(camera, center + Vector((0, 0, size.z * 0.08)))
    camera.data.ortho_scale = max(size.x, size.y, size.z) * ortho_multiplier
    bpy.context.scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def render_closeup(camera, side, filename):
    sign = -1.0 if side == "Left" else 1.0
    target = Vector((sign * 1.035, -1.215, 1.285))
    camera.location = target + Vector((0.0, -1.15, 0.08))
    look_at(camera, target + Vector((0.0, 0.0, 0.01)))
    camera.data.ortho_scale = 0.52
    bpy.context.scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def render_side_closeup(camera, side, filename):
    sign = -1.0 if side == "Left" else 1.0
    target = Vector((sign * 1.035, -1.215, 1.285))
    camera.location = target + Vector((sign * 1.15, 0.0, 0.08))
    look_at(camera, target + Vector((0.0, 0.0, 0.01)))
    camera.data.ortho_scale = 0.45
    bpy.context.scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def write_docs():
    readme = """# 아첼레란도 더듬이 끝 고리 연결 보정 샘플

## 목적

기존 승인 샘플을 가까이서 볼 때 더듬이 끝과 사슬 시작부가 떠 보이는 문제를 줄이기 위해, 더듬이 끝에 장착형 금속 고리와 짧은 연결 핀을 추가했습니다.

## 승인 대상 파일

- `index.html`
- `exports/accelerando_antenna_tip_ring_connection_sample.glb`
- `exports/accelerando_antenna_tip_ring_connection_sample.blend`
- `renders/accelerando_antenna_tip_ring_connection_front.png`
- `renders/accelerando_antenna_tip_ring_connection_side.png`
- `renders/accelerando_antenna_tip_ring_connection_oblique.png`
- `renders/accelerando_antenna_tip_ring_connection_closeup_left.png`
- `renders/accelerando_antenna_tip_ring_connection_closeup_right.png`
- `renders/accelerando_antenna_tip_ring_connection_side_closeup_left.png`
- `renders/accelerando_antenna_tip_ring_connection_side_closeup_right.png`

## 반영 내용

- 기준은 기존 승인 샘플 `antenna_connection_color_fix`의 `.blend` 파일입니다.
- 양쪽 더듬이 끝에 금속 장착 고리, 칼라, 연결 핀, 리벳을 추가했습니다.
- `AntennaTip_Ring`, 장착 고리, 첫 사슬 링크의 중심을 겹치게 맞춰 측면 근접 시점에서도 분리되어 보이지 않도록 보강했습니다.
- 몸통, 껍질, 철퇴, 사슬의 기존 색과 재질 의도는 유지했습니다.

## Unity 적용 계획

샘플 승인 후 Unity 적용 단계에서는 `Assets/_Project/Art/Enemies/Accelerando/Models/`에 새 GLB를 별도 모델로 임포트하고, `CargoRunMvp`의 `Approved Accelerando Enemy Placement` 아래 Accelerando 7개 리뷰 오브젝트가 이 모델을 사용하도록 교체하는 방식이 적합합니다. 씬 적용은 별도 승인 후 진행해야 합니다.

## 적용하지 않은 항목

- Unity `Assets/`와 `CargoRunMvp.unity`에는 적용하지 않았습니다.
- 기존 승인 샘플 파일은 덮어쓰지 않았습니다.
- 런타임 프리팹, 애니메이션, 충돌, 배치 상태는 변경하지 않았습니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    html = """<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <title>아첼레란도 더듬이 끝 고리 연결 보정 샘플</title>
  <style>
    body { margin: 0; font-family: "Malgun Gothic", Arial, sans-serif; background: #201f1d; color: #eee8dd; }
    main { max-width: 1180px; margin: 0 auto; padding: 28px; }
    h1 { font-size: 28px; margin: 0 0 12px; }
    p { line-height: 1.65; color: #d8d0c2; }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 18px; margin-top: 22px; }
    figure { margin: 0; background: #2c2a27; border: 1px solid #4b463f; padding: 12px; }
    img { width: 100%; height: auto; display: block; }
    figcaption { margin-top: 8px; color: #c8bdaa; font-size: 14px; }
    code { color: #f0c175; }
  </style>
</head>
<body>
  <main>
    <h1>아첼레란도 더듬이 끝 고리 연결 보정 샘플</h1>
    <p>기존 승인 샘플의 더듬이 끝과 사슬 시작부 사이에 장착형 금속 고리, 칼라, 연결 핀을 추가해 가까운 시점에서도 사슬이 공중에 떠 보이지 않도록 만든 검토용 샘플입니다.</p>
    <p>Unity 적용 대상은 승인 후 <code>Approved Accelerando Enemy Placement</code> 아래 Accelerando 리뷰 오브젝트 교체 단계에서 별도로 다뤄야 합니다.</p>
    <div class="grid">
      <figure><img src="renders/accelerando_antenna_tip_ring_connection_front.png"><figcaption>정면</figcaption></figure>
      <figure><img src="renders/accelerando_antenna_tip_ring_connection_side.png"><figcaption>측면</figcaption></figure>
      <figure><img src="renders/accelerando_antenna_tip_ring_connection_oblique.png"><figcaption>사선</figcaption></figure>
      <figure><img src="renders/accelerando_antenna_tip_ring_connection_closeup_left.png"><figcaption>왼쪽 더듬이 끝 고리 확대</figcaption></figure>
      <figure><img src="renders/accelerando_antenna_tip_ring_connection_closeup_right.png"><figcaption>오른쪽 더듬이 끝 고리 확대</figcaption></figure>
      <figure><img src="renders/accelerando_antenna_tip_ring_connection_side_closeup_left.png"><figcaption>왼쪽 더듬이 끝 고리 측면 확대</figcaption></figure>
      <figure><img src="renders/accelerando_antenna_tip_ring_connection_side_closeup_right.png"><figcaption>오른쪽 더듬이 끝 고리 측면 확대</figcaption></figure>
    </div>
  </main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def main():
    ensure_dirs()
    open_source_sample()
    remove_previous_fix_objects()
    add_tip_anchor_geometry()
    shade_model_objects()

    objects = model_objects()
    export_sample(objects)

    camera = setup_render_scene()
    render_view(camera, objects, "accelerando_antenna_tip_ring_connection_front.png", (0, -1, 0))
    render_view(camera, objects, "accelerando_antenna_tip_ring_connection_side.png", (1, 0, 0))
    render_view(camera, objects, "accelerando_antenna_tip_ring_connection_oblique.png", (1, -1, 0), 1.34)
    render_closeup(camera, "Left", "accelerando_antenna_tip_ring_connection_closeup_left.png")
    render_closeup(camera, "Right", "accelerando_antenna_tip_ring_connection_closeup_right.png")
    render_side_closeup(camera, "Left", "accelerando_antenna_tip_ring_connection_side_closeup_left.png")
    render_side_closeup(camera, "Right", "accelerando_antenna_tip_ring_connection_side_closeup_right.png")
    write_docs()


if __name__ == "__main__":
    main()
