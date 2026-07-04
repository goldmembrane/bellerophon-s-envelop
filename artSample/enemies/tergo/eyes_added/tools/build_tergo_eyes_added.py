from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


REPO_ROOT = Path(__file__).resolve().parents[5]
SAMPLE_ROOT = REPO_ROOT / "artSample/enemies/tergo/eyes_added"
SOURCE_FBX = REPO_ROOT / "Assets/_Project/Art/Enemies/Tergo/Models/tergo.fbx"
BLEND_PATH = SAMPLE_ROOT / "blender/tergo_eyes_added.blend"
FBX_PATH = SAMPLE_ROOT / "exports/tergo_eyes_added.fbx"
GLB_PATH = SAMPLE_ROOT / "exports/tergo_eyes_added.glb"
RENDER_DIR = SAMPLE_ROOT / "renders"


def ensure_dirs() -> None:
    for path in (BLEND_PATH.parent, FBX_PATH.parent, GLB_PATH.parent, RENDER_DIR):
        path.mkdir(parents=True, exist_ok=True)


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_material(name: str, color: tuple[float, float, float, float], roughness: float = 0.5,
                  metallic: float = 0.0, emission: tuple[float, float, float, float] | None = None,
                  emission_strength: float = 0.0) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
        if emission:
            bsdf.inputs["Emission Color"].default_value = emission
            bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat


def import_tergo() -> list[bpy.types.Object]:
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    imported = list(bpy.context.selected_objects)
    for obj in imported:
        obj.select_set(False)
    return imported


def mesh_bounds(mesh_objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    mins = Vector((1e9, 1e9, 1e9))
    maxs = Vector((-1e9, -1e9, -1e9))
    for obj in mesh_objects:
        for corner in obj.bound_box:
            point = obj.matrix_world @ Vector(corner)
            mins.x = min(mins.x, point.x)
            mins.y = min(mins.y, point.y)
            mins.z = min(mins.z, point.z)
            maxs.x = max(maxs.x, point.x)
            maxs.y = max(maxs.y, point.y)
            maxs.z = max(maxs.z, point.z)
    return mins, maxs


def estimate_head_front_y(mesh_objects: list[bpy.types.Object], bounds_min: Vector, bounds_max: Vector) -> float:
    dims = bounds_max - bounds_min
    center_x = (bounds_min.x + bounds_max.x) * 0.5
    z_min = bounds_min.z + dims.z * 0.80
    z_max = bounds_min.z + dims.z * 0.97
    x_limit = dims.x * 0.24
    points: list[Vector] = []
    for obj in mesh_objects:
        for vertex in obj.data.vertices:
            point = obj.matrix_world @ vertex.co
            if z_min <= point.z <= z_max and abs(point.x - center_x) <= x_limit:
                points.append(point)
    if not points:
        return bounds_min.y
    return min(point.y for point in points)


def estimate_local_front_y(mesh_objects: list[bpy.types.Object], x: float, z: float,
                           radius_x: float, radius_z: float, fallback: float) -> float:
    points: list[Vector] = []
    for obj in mesh_objects:
        for vertex in obj.data.vertices:
            point = obj.matrix_world @ vertex.co
            if abs(point.x - x) <= radius_x and abs(point.z - z) <= radius_z:
                points.append(point)
    if not points:
        return fallback
    return min(point.y for point in points)


def set_smooth(obj: bpy.types.Object) -> None:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try:
        bpy.ops.object.shade_smooth()
    finally:
        obj.select_set(False)


def add_uv_sphere(name: str, location: Vector, scale: tuple[float, float, float],
                  material: bpy.types.Material, segments: int = 32, rings: int = 16) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name + "_Mesh"
    obj.scale = scale
    obj.data.materials.append(material)
    set_smooth(obj)
    return obj


def add_torus(name: str, location: Vector, major_radius: float, minor_radius: float,
              scale_z: float, material: bpy.types.Material) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=48,
        minor_segments=8,
        location=location,
        rotation=(math.radians(90.0), 0.0, 0.0),
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name + "_Mesh"
    obj.scale.z = scale_z
    obj.data.materials.append(material)
    set_smooth(obj)
    return obj


def add_eye_pair(bounds_min: Vector, bounds_max: Vector, mesh_objects: list[bpy.types.Object],
                 head_front_y: float) -> dict[str, object]:
    dims = bounds_max - bounds_min
    height = dims.z
    center_x = (bounds_min.x + bounds_max.x) * 0.5

    eye_z = bounds_min.z + height * 0.928
    head_x_values: list[float] = []
    for obj in mesh_objects:
        for vertex in obj.data.vertices:
            point = obj.matrix_world @ vertex.co
            if abs(point.z - eye_z) <= height * 0.055:
                head_x_values.append(point.x)
    eye_center_x = (min(head_x_values) + max(head_x_values)) * 0.5 if head_x_values else center_x
    eye_spacing = height * 0.044

    socket_mat = make_material(
        "Tergo_EyeSocket_Wet_Dark_Green",
        (0.015, 0.032, 0.022, 1.0),
        roughness=0.82,
        metallic=0.0,
    )
    lens_mat = make_material(
        "Tergo_EyeLens_Burning_Amber",
        (1.0, 0.42, 0.045, 1.0),
        roughness=0.18,
        metallic=0.0,
        emission=(1.0, 0.33, 0.035, 1.0),
        emission_strength=2.8,
    )
    pupil_mat = make_material(
        "Tergo_EyeHotCore_Pale_Yellow",
        (1.0, 0.86, 0.32, 1.0),
        roughness=0.12,
        emission=(1.0, 0.62, 0.18, 1.0),
        emission_strength=4.5,
    )

    eye_objects: list[bpy.types.Object] = []
    positions: dict[str, Vector] = {}
    surface_front_y: dict[str, float] = {}
    side_offsets = {
        "L": {"socket": 0.018, "lens": 0.033, "glow": 0.036},
        "R": {"socket": 0.012, "lens": 0.024, "glow": 0.027},
    }
    for side, x in {
        "L": eye_center_x - eye_spacing * 0.5,
        "R": eye_center_x + eye_spacing * 0.5,
    }.items():
        local_front_y = estimate_local_front_y(
            mesh_objects,
            x,
            eye_z,
            radius_x=height * 0.035,
            radius_z=height * 0.05,
            fallback=head_front_y,
        )
        surface_front_y[side] = min(head_front_y, local_front_y)
        lens_y = surface_front_y[side] - height * side_offsets[side]["lens"]
        positions[side] = Vector((x, lens_y, eye_z))

    for side, pos in positions.items():
        local_front_y = surface_front_y[side]
        socket_y = local_front_y - height * side_offsets[side]["socket"]
        glow_y = local_front_y - height * side_offsets[side]["glow"]
        socket_center = Vector((pos.x, socket_y, pos.z - height * 0.001))
        eye_objects.append(add_uv_sphere(
            f"Tergo_{side}_EyeSocket_Depression",
            socket_center,
            (height * 0.0068, height * 0.009, height * 0.005),
            socket_mat,
            segments=32,
            rings=16,
        ))
        eye_objects.append(add_torus(
            f"Tergo_{side}_EyeSocket_Raised_Rim",
            Vector((pos.x, lens_y + height * 0.001, pos.z)),
            height * 0.0042,
            height * 0.00055,
            0.60,
            socket_mat,
        ))
        eye_objects.append(add_uv_sphere(
            f"Tergo_{side}_Glowing_Eye_Lens",
            pos,
            (height * 0.0047, height * 0.003, height * 0.0032),
            lens_mat,
            segments=32,
            rings=16,
        ))
        eye_objects.append(add_uv_sphere(
            f"Tergo_{side}_Eye_Hot_Core",
            Vector((pos.x, glow_y, pos.z + height * 0.0005)),
            (height * 0.00155, height * 0.0012, height * 0.0011),
            pupil_mat,
            segments=20,
            rings=10,
        ))

    for side, pos in positions.items():
        bpy.ops.object.light_add(type="POINT", location=(pos.x, pos.y - height * 0.05, pos.z + height * 0.005))
        light = bpy.context.object
        light.name = f"Tergo_{side}_Eye_Amber_Point_Light"
        light.data.name = light.name + "_Data"
        light.data.color = (1.0, 0.45, 0.08)
        light.data.energy = 4.0
        light.data.shadow_soft_size = 0.18
        eye_objects.append(light)

    return {
        "eyeZ": round(eye_z, 4),
        "eyeCenterX": round(eye_center_x, 4),
        "headFrontY": round(head_front_y, 4),
        "leftSurfaceY": round(surface_front_y["L"], 4),
        "leftSocketY": round(surface_front_y["L"] - height * side_offsets["L"]["socket"], 4),
        "leftFrontY": round(positions["L"].y, 4),
        "rightSurfaceY": round(surface_front_y["R"], 4),
        "rightSocketY": round(surface_front_y["R"] - height * side_offsets["R"]["socket"], 4),
        "rightFrontY": round(positions["R"].y, 4),
        "eyeSpacing": round(eye_spacing, 4),
        "objects": [obj.name for obj in eye_objects],
    }


def setup_scene(bounds_min: Vector, bounds_max: Vector) -> None:
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = 64
    scene.render.resolution_x = 2400
    scene.render.resolution_y = 1600
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.world = bpy.data.worlds.new("Tergo_Eyes_Review_World") if not scene.world else scene.world
    scene.world.color = (1.0, 1.0, 1.0)

    center = (bounds_min + bounds_max) * 0.5
    height = (bounds_max - bounds_min).z

    bpy.ops.object.light_add(type="AREA", location=(0.0, -2.2, center.z + height * 0.25))
    key = bpy.context.object
    key.name = "Review_Key_Light"
    key.data.energy = 430.0
    key.data.size = 3.1

    bpy.ops.object.light_add(type="AREA", location=(-1.7, 1.5, center.z + height * 0.38))
    fill = bpy.context.object
    fill.name = "Review_Fill_Light"
    fill.data.energy = 110.0
    fill.data.size = 4.2

    bpy.ops.mesh.primitive_plane_add(size=2.2, location=(center.x, center.y, bounds_min.z - 0.004))
    plane = bpy.context.object
    plane.name = "Review_Ground_Plane"
    plane.data.materials.append(make_material("Review_Matte_Warm_White", (0.86, 0.87, 0.84, 1.0), roughness=0.9))


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_camera(name: str, location: tuple[float, float, float], target: Vector,
                  ortho_scale: float, output: Path) -> None:
    bpy.ops.object.camera_add(location=location)
    camera = bpy.context.object
    camera.name = name
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale
    camera.data.lens = 70
    look_at(camera, target)
    bpy.context.scene.camera = camera
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)


def render_samples(bounds_min: Vector, bounds_max: Vector) -> None:
    center = (bounds_min + bounds_max) * 0.5
    dims = bounds_max - bounds_min
    full_target = Vector((center.x, center.y, bounds_min.z + dims.z * 0.52))
    head_target = Vector((center.x, bounds_min.y, bounds_min.z + dims.z * 0.925))

    render_camera(
        "Tergo_Eyes_Front_Camera",
        (center.x, bounds_min.y - dims.z * 2.20, bounds_min.z + dims.z * 0.52),
        full_target,
        dims.z * 1.64,
        RENDER_DIR / "tergo_eyes_front.png",
    )
    render_camera(
        "Tergo_Eyes_ThreeQuarter_Camera",
        (center.x + dims.x * 1.45, bounds_min.y - dims.z * 1.95, bounds_min.z + dims.z * 0.56),
        full_target,
        dims.z * 1.64,
        RENDER_DIR / "tergo_eyes_three_quarter.png",
    )
    render_camera(
        "Tergo_Eyes_Side_Camera",
        (center.x + dims.z * 2.15, center.y, bounds_min.z + dims.z * 0.52),
        full_target,
        dims.z * 1.64,
        RENDER_DIR / "tergo_eyes_side.png",
    )
    render_camera(
        "Tergo_Eyes_Closeup_Camera",
        (center.x, bounds_min.y - dims.z * 0.72, bounds_min.z + dims.z * 0.925),
        head_target,
        dims.z * 0.36,
        RENDER_DIR / "tergo_eyes_closeup.png",
    )
    render_camera(
        "Tergo_Eyes_Front_Large_Camera",
        (center.x, bounds_min.y - dims.z * 0.78, bounds_min.z + dims.z * 0.925),
        head_target,
        dims.z * 0.44,
        RENDER_DIR / "tergo_eyes_front_large.png",
    )
    render_camera(
        "Tergo_Eyes_Side_Large_Camera",
        (center.x + dims.z * 0.80, center.y, bounds_min.z + dims.z * 0.925),
        head_target,
        dims.z * 0.48,
        RENDER_DIR / "tergo_eyes_side_large.png",
    )


def write_docs_legacy(report: dict[str, object]) -> None:
    readme = f"""# Tergo 눈 추가 샘플

## 목적

기존 `Assets/_Project/Art/Enemies/Tergo/Models/tergo.fbx` 본체를 유지한 상태에서 눈만 추가한 승인용 샘플입니다. Unity 씬, 프리팹, 런타임 에셋에는 아직 반영하지 않았습니다.

## 반영 방식

- 본체 FBX는 그대로 임포트했습니다.
- 머리 전면 `-Y` 방향에 작은 주황 발광 렌즈 2개를 배치했습니다.
- 각 눈에는 어두운 젖은 녹색 안와, 얇은 돌출 테두리, 밝은 중심광을 추가했습니다.
- 몸통, 팔, 드릴, 다리, 스케일, 방향, 리깅 구조는 수정하지 않았습니다.

## 검토 파일

- `renders/tergo_eyes_front.png`
- `renders/tergo_eyes_three_quarter.png`
- `renders/tergo_eyes_side.png`
- `renders/tergo_eyes_closeup.png`
- `renders/tergo_eyes_front_large.png`
- `renders/tergo_eyes_side_large.png`
- `blender/tergo_eyes_added.blend`
- `exports/tergo_eyes_added.fbx`
- `exports/tergo_eyes_added.glb`

## 기준

- 원본 기획서: 인간형 중형 씨앗체, 드릴 형태 양팔, 높이 약 150cm.
- 기준 이미지: `image/tergo(테르고).png`, `image/tergo-beside.png`, `image/tergo-back.png`.
- 이번 샘플은 기준 이미지의 작은 주황 발광 눈을 기존 모델에 더하는 범위로 제한했습니다.

## 수치 기록

- 원본 bounds min: `{report["sourceBoundsMin"]}`
- 원본 bounds max: `{report["sourceBoundsMax"]}`
- 원본 dimensions: `{report["sourceDimensions"]}`
- 눈 중심 높이 Z: `{report["eyePlacement"]["eyeZ"]}`
- 왼쪽 안와/렌즈 Y: `{report["eyePlacement"]["leftSocketY"]}` / `{report["eyePlacement"]["leftFrontY"]}`
- 오른쪽 안와/렌즈 Y: `{report["eyePlacement"]["rightSocketY"]}` / `{report["eyePlacement"]["rightFrontY"]}`
- 눈 간격: `{report["eyePlacement"]["eyeSpacing"]}`

## 승인 전 제한

사용자 승인 전에는 이 샘플을 Unity 씬, 프리팹, 런타임 모델, AI, 피격 판정, 애니메이션에 연결하지 않습니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


def write_docs(report: dict[str, object]) -> None:
    placement = report["eyePlacement"]
    readme = f"""# Tergo 눈 추가 샘플

## 목적

기존 `Assets/_Project/Art/Enemies/Tergo/Models/tergo.fbx` 본체는 유지하고, 머리 전면에 작은 주황 발광 눈만 추가한 승인용 샘플입니다. 아직 Unity 씬, 프리팹, 런타임 에셋에는 적용하지 않았습니다.

## 반영 방식

- 원본 Tergo FBX를 그대로 임포트했습니다.
- 머리 전면 상단에 작은 주황 발광 렌즈 2개를 배치했습니다.
- 각 눈에는 어두운 젖은 소켓, 얕은 돌출 테두리, 밝은 중심광, 검토용 작은 포인트 라이트를 추가했습니다.
- 몸체 메시, Armature, 드릴 팔, 스케일, 방향, 리깅 구조는 수정하지 않았습니다.
- 정면 검토용 고해상도 렌더와 측면 검토용 고해상도 렌더를 별도로 생성했습니다.

## 검토 파일

- `index.html`
- `renders/tergo_eyes_front.png`
- `renders/tergo_eyes_three_quarter.png`
- `renders/tergo_eyes_side.png`
- `renders/tergo_eyes_closeup.png`
- `renders/tergo_eyes_front_large.png`
- `renders/tergo_eyes_side_large.png`
- `blender/tergo_eyes_added.blend`
- `exports/tergo_eyes_added.fbx`
- `exports/tergo_eyes_added.glb`

## 기준

- 원본 기획서: `docs/GAME_DESIGN_SOURCE.txt`
- 애니메이션 계획 문서: `docs/enemies/TERGO_ANIMATION_PLAN.md`
- 기준 이미지: `image/tergo(테르고).png`, `image/tergo-beside.png`, `image/tergo-back.png`

## 위치 기록

- 원본 bounds min: `{report["sourceBoundsMin"]}`
- 원본 bounds max: `{report["sourceBoundsMax"]}`
- 원본 dimensions: `{report["sourceDimensions"]}`
- 눈 중심 X: `{placement["eyeCenterX"]}`
- 눈 중심 Z: `{placement["eyeZ"]}`
- 머리 전면 기준 Y: `{placement["headFrontY"]}`
- 왼쪽 눈 surface/socket/lens Y: `{placement["leftSurfaceY"]}` / `{placement["leftSocketY"]}` / `{placement["leftFrontY"]}`
- 오른쪽 눈 surface/socket/lens Y: `{placement["rightSurfaceY"]}` / `{placement["rightSocketY"]}` / `{placement["rightFrontY"]}`
- 눈 간격: `{placement["eyeSpacing"]}`

## 승인 전 제한

사용자 승인 전에는 이 샘플을 Unity 씬, 프리팹, 런타임 모델, AI, 충돌, 피격 판정, 애니메이션에 연결하지 않습니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


def export_assets() -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        if obj.type not in {"ARMATURE", "MESH"}:
            continue
        if obj.name.startswith("Review_"):
            continue
        obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=False,
        object_types={"ARMATURE", "MESH", "LIGHT"},
    )
    bpy.ops.export_scene.gltf(
        filepath=str(GLB_PATH),
        export_format="GLB",
        use_selection=True,
        export_materials="EXPORT",
    )


def main() -> None:
    ensure_dirs()
    clear_scene()
    import_tergo()

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not mesh_objects:
        raise RuntimeError("No mesh objects were imported from Tergo FBX.")

    source_min, source_max = mesh_bounds(mesh_objects)
    head_front_y = estimate_head_front_y(mesh_objects, source_min, source_max)
    eye_report = add_eye_pair(source_min, source_max, mesh_objects, head_front_y)
    setup_scene(source_min, source_max)
    render_samples(source_min, source_max)
    export_assets()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    dims = source_max - source_min
    report = {
        "sample": "Tergo eyes added",
        "createdForApproval": True,
        "unityRuntimeApplied": False,
        "sourceFbx": str(SOURCE_FBX.relative_to(REPO_ROOT)).replace("\\", "/"),
        "sourceBoundsMin": [round(v, 4) for v in source_min],
        "sourceBoundsMax": [round(v, 4) for v in source_max],
        "sourceDimensions": [round(v, 4) for v in dims],
        "eyePlacement": eye_report,
        "addedOnly": [
            "Two glowing amber eye lenses",
            "Two pale hot cores",
            "Two dark wet eye sockets",
            "Two raised socket rims",
            "Two small amber point lights for render review",
        ],
        "notChanged": [
            "Tergo body mesh",
            "Tergo armature",
            "Tergo body scale",
            "Tergo drill arms",
            "Unity scene and prefabs",
            "Animations and runtime logic",
        ],
        "outputs": {
            "blend": str(BLEND_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "fbx": str(FBX_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "glb": str(GLB_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "renders": [
                "artSample/enemies/tergo/eyes_added/renders/tergo_eyes_front.png",
                "artSample/enemies/tergo/eyes_added/renders/tergo_eyes_three_quarter.png",
                "artSample/enemies/tergo/eyes_added/renders/tergo_eyes_side.png",
                "artSample/enemies/tergo/eyes_added/renders/tergo_eyes_closeup.png",
                "artSample/enemies/tergo/eyes_added/renders/tergo_eyes_front_large.png",
                "artSample/enemies/tergo/eyes_added/renders/tergo_eyes_side_large.png",
            ],
        },
    }
    write_docs(report)
    print("Tergo eyes added sample generated.")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
