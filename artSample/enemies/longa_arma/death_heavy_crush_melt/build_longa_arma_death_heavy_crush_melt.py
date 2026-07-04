from __future__ import annotations

import json
import math
from datetime import datetime
from pathlib import Path

import bpy
from mathutils import Vector


SCRIPT_PATH = Path(__file__).resolve()
SAMPLE_ROOT = SCRIPT_PATH.parent
REPO_ROOT = SCRIPT_PATH.parents[4]

SOURCE_RUNTIME_BLEND = REPO_ROOT / "artSample/enemies/longa_arma/runtime_lowpoly/blender/longa_arma_runtime_lowpoly.blend"
SOURCE_WALKING_FBX = REPO_ROOT / "Assets/_Project/Art/Enemies/LongaArma/Models/longa_arma_walking.fbx"
SOURCE_ORIGINAL_BLEND = REPO_ROOT / "enemies model/longa arma.blend"

BLEND_PATH = SAMPLE_ROOT / "blender/longa_arma_death_heavy_crush_melt.blend"
FBX_PATH = SAMPLE_ROOT / "exports/longa_arma_death_heavy_crush_melt.fbx"
GLB_PATH = SAMPLE_ROOT / "exports/longa_arma_death_heavy_crush_melt.glb"
RENDER_ROOT = SAMPLE_ROOT / "renders"
FRAME_ROOT = SAMPLE_ROOT / "frames"
MANIFEST_PATH = SAMPLE_ROOT / "ASSET_MANIFEST.json"
README_PATH = SAMPLE_ROOT / "README.md"
STATUS_PATH = SAMPLE_ROOT / "DEATH_HEAVY_CRUSH_MELT_STATUS_2026-07-04.md"
HTML_PATH = SAMPLE_ROOT / "index.html"

MESH_NAME = "LongaArma_DeathHeavyCrushMelt_Mesh"
ACTION_NAME = "LongaArma_Death_HeavyCrushMelt"
RENDER_WIDTH = 1400
RENDER_HEIGHT = 900

SHAPE_KEY_NAMES = [
    "DEATH_HEAVY_01_weight_sag",
    "DEATH_HEAVY_02_crush_collapse",
    "DEATH_HEAVY_03_melt_spread",
]

KEY_SCHEDULE = [
    (1, 0.0, 0.0, 0.0),
    (16, 0.82, 0.0, 0.0),
    (28, 1.0, 0.16, 0.0),
    (42, 0.18, 1.0, 0.0),
    (58, 0.0, 0.45, 0.55),
    (76, 0.0, 0.0, 0.80),
    (96, 0.0, 0.0, 1.0),
]

FRAME_SAMPLES = [1, 8, 16, 24, 32, 42, 50, 58, 66, 76, 86, 96]
STATIC_RENDERS = [
    ("01_death_start_existing_pose.png", 1),
    ("02_heavy_sag.png", 16),
    ("03_crush_collapse.png", 42),
    ("04_melt_spread.png", 96),
]


def ensure_dirs() -> None:
    for path in [BLEND_PATH.parent, FBX_PATH.parent, GLB_PATH.parent, RENDER_ROOT, FRAME_ROOT]:
        path.mkdir(parents=True, exist_ok=True)


def clamp01(value: float) -> float:
    return max(0.0, min(1.0, value))


def smoothstep(edge0: float, edge1: float, value: float) -> float:
    if abs(edge1 - edge0) < 0.000001:
        return 1.0 if value >= edge1 else 0.0
    t = clamp01((value - edge0) / (edge1 - edge0))
    return t * t * (3.0 - 2.0 * t)


def mesh_bounds(obj: bpy.types.Object) -> dict[str, Vector]:
    coords = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    min_vec = Vector((min(v.x for v in coords), min(v.y for v in coords), min(v.z for v in coords)))
    max_vec = Vector((max(v.x for v in coords), max(v.y for v in coords), max(v.z for v in coords)))
    return {"min": min_vec, "max": max_vec, "size": max_vec - min_vec, "center": (min_vec + max_vec) * 0.5}


def local_bounds(mesh: bpy.types.Mesh) -> dict[str, Vector]:
    coords = [vertex.co.copy() for vertex in mesh.vertices]
    min_vec = Vector((min(v.x for v in coords), min(v.y for v in coords), min(v.z for v in coords)))
    max_vec = Vector((max(v.x for v in coords), max(v.y for v in coords), max(v.z for v in coords)))
    return {"min": min_vec, "max": max_vec, "size": max_vec - min_vec, "center": (min_vec + max_vec) * 0.5}


def reset_scene_animation_state() -> None:
    bpy.context.scene.frame_set(1)
    for obj in bpy.context.scene.objects:
        if obj.type == "ARMATURE":
            obj.animation_data_clear()
            for pose_bone in obj.pose.bones:
                pose_bone.location = (0.0, 0.0, 0.0)
                pose_bone.rotation_euler = (0.0, 0.0, 0.0)
                pose_bone.rotation_quaternion = (1.0, 0.0, 0.0, 0.0)
                pose_bone.scale = (1.0, 1.0, 1.0)
        if obj.type == "MESH" and obj.data.shape_keys is not None:
            for key_block in obj.data.shape_keys.key_blocks:
                key_block.value = 0.0
    bpy.context.view_layer.update()


def find_runtime_mesh() -> bpy.types.Object:
    mesh = bpy.data.objects.get("LongaArma_Runtime_LowPoly")
    if mesh is not None and mesh.type == "MESH":
        return mesh

    candidates = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not candidates:
        raise RuntimeError("No mesh object found in runtime Longa Arma blend.")
    candidates.sort(key=lambda obj: len(obj.data.polygons), reverse=True)
    return candidates[0]


def find_walking_mesh() -> bpy.types.Object:
    mesh = bpy.data.objects.get("char1")
    if mesh is not None and mesh.type == "MESH":
        return mesh

    candidates = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not candidates:
        raise RuntimeError("No mesh object found in walking Longa Arma FBX.")
    candidates.sort(key=lambda obj: len(obj.data.polygons), reverse=True)
    return candidates[0]


def make_clean_mesh_from_runtime() -> bpy.types.Object:
    if not SOURCE_WALKING_FBX.exists():
        raise FileNotFoundError(f"Missing Unity walking source FBX: {SOURCE_WALKING_FBX}")

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_WALKING_FBX))
    reset_scene_animation_state()
    source = find_walking_mesh()
    source.name = "char1"
    source.data.name = MESH_NAME + "_Data"
    if len(source.data.materials) == 0:
        assign_fallback_materials(source)
    return source


def center_mesh_on_floor(obj: bpy.types.Object) -> None:
    bounds = local_bounds(obj.data)
    offset = Vector((bounds["center"].x, bounds["center"].y, bounds["min"].z))
    for vertex in obj.data.vertices:
        vertex.co -= offset
    obj.location = (0.0, 0.0, 0.0)
    obj.data.update()


def make_material(name: str, color: tuple[float, float, float, float], roughness: float) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        if "Base Color" in principled.inputs:
            principled.inputs["Base Color"].default_value = color
        if "Roughness" in principled.inputs:
            principled.inputs["Roughness"].default_value = roughness
    return mat


def assign_fallback_materials(obj: bpy.types.Object) -> None:
    body = make_material("M_LongaArma_ExistingWetBody_Fallback", (0.16, 0.38, 0.27, 1.0), 0.72)
    blade = make_material("M_LongaArma_ExistingDarkBlade_Fallback", (0.03, 0.035, 0.035, 1.0), 0.48)
    obj.data.materials.append(body)
    obj.data.materials.append(blade)
    bounds = local_bounds(obj.data)
    size = bounds["size"]
    min_vec = bounds["min"]
    for poly in obj.data.polygons:
        center = sum((obj.data.vertices[index].co for index in poly.vertices), Vector()) / max(1, len(poly.vertices))
        xn = (center.x - min_vec.x) / max(size.x, 0.0001)
        zn = (center.z - min_vec.z) / max(size.z, 0.0001)
        poly.material_index = 1 if xn < 0.30 and zn < 0.62 else 0


def clear_shape_key_animation(obj: bpy.types.Object) -> None:
    if obj.data.shape_keys is not None:
        obj.data.shape_keys.animation_data_clear()
        while len(obj.data.shape_keys.key_blocks) > 0:
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            obj.active_shape_key_index = 0
            bpy.ops.object.shape_key_remove()


def add_heavy_crush_shape_keys(obj: bpy.types.Object) -> None:
    clear_shape_key_animation(obj)
    obj.shape_key_add(name="Basis")
    keys = {name: obj.shape_key_add(name=name) for name in SHAPE_KEY_NAMES}

    bounds = local_bounds(obj.data)
    min_vec = bounds["min"]
    size = bounds["size"]
    center = bounds["center"]
    longest = max(size.x, size.z, 0.0001)

    for vertex in obj.data.vertices:
        base = vertex.co.copy()
        dx = base.x - center.x
        dz = base.z - center.z
        height = base.y - min_vec.y
        xn = (base.x - min_vec.x) / max(size.x, 0.0001)
        zn = (base.z - min_vec.z) / max(size.z, 0.0001)
        yn = height / max(size.y, 0.0001)
        radial = math.sqrt((dx / longest) ** 2 + (dz / longest) ** 2)
        angle = math.atan2(dz, dx + 0.000001)
        lumpy = 1.0 + 0.08 * math.sin(angle * 3.0 + vertex.index * 0.037) + 0.05 * math.cos((xn + zn) * math.tau)

        foot_lock = 1.0 - smoothstep(0.015, 0.16, yn)
        upper_weight = smoothstep(0.16, 0.92, yn)
        center_pull = smoothstep(0.28, 0.84, yn)
        side_push = 0.04 * math.sin(angle * 2.0 + vertex.index * 0.11)

        sag_xz = 1.02 + 0.11 * upper_weight
        sag_y = min_vec.y + 0.018 + height * (0.56 - 0.12 * upper_weight)
        sag_y = base.y * foot_lock + sag_y * (1.0 - foot_lock)
        keys["DEATH_HEAVY_01_weight_sag"].data[vertex.index].co = Vector((
            center.x + dx * sag_xz + side_push * center_pull,
            sag_y,
            center.z + dz * (1.01 + 0.07 * upper_weight),
        ))

        crush_x = 1.24 + 0.34 * upper_weight + 0.08 * lumpy
        crush_z = 1.18 + 0.30 * upper_weight + 0.06 * math.sin(angle * 4.0)
        crush_y = min_vec.y + 0.020 + height * (0.19 - 0.08 * smoothstep(0.45, 1.0, yn))
        crush_y = max(min_vec.y + 0.012, crush_y)
        keys["DEATH_HEAVY_02_crush_collapse"].data[vertex.index].co = Vector((
            center.x + dx * crush_x - 0.035 * center_pull,
            crush_y,
            center.z + dz * crush_z,
        ))

        spread_weight = smoothstep(0.05, 0.95, yn)
        spread_x = 1.30 + 0.36 * spread_weight + radial * 0.23
        spread_z = 1.24 + 0.35 * spread_weight + radial * 0.17
        flow = Vector((
            center.x + dx * spread_x + math.cos(angle) * 0.052 * lumpy,
            min_vec.y + 0.014 + 0.052 * (1.0 - smoothstep(0.28, 1.0, radial)) + 0.010 * math.sin(vertex.index * 0.17),
            center.z + dz * spread_z + math.sin(angle) * 0.066 * lumpy,
        ))
        keys["DEATH_HEAVY_03_melt_spread"].data[vertex.index].co = flow

    for key in keys.values():
        key.slider_min = 0.0
        key.slider_max = 1.0
    obj.data.update()


def animate_shape_keys(obj: bpy.types.Object) -> None:
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = 96
    scene.render.fps = 24

    keys = obj.data.shape_keys.key_blocks
    for frame, sag, crush, spread in KEY_SCHEDULE:
        scene.frame_set(frame)
        values = {
            "DEATH_HEAVY_01_weight_sag": sag,
            "DEATH_HEAVY_02_crush_collapse": crush,
            "DEATH_HEAVY_03_melt_spread": spread,
        }
        for name, value in values.items():
            keys[name].value = value
            keys[name].keyframe_insert("value", frame=frame)

    if obj.data.shape_keys.animation_data is not None:
        action = obj.data.shape_keys.animation_data.action
        if action is not None:
            action.name = ACTION_NAME
            for fcurve in getattr(action, "fcurves", []):
                for keyframe in fcurve.keyframe_points:
                    keyframe.interpolation = "BEZIER"


def evaluated_stage_mesh(source: bpy.types.Object, frame: int, name: str, offset: Vector) -> bpy.types.Object:
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = source.evaluated_get(depsgraph)
    mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=depsgraph)
    for material in source.data.materials:
        mesh.materials.append(material)
    obj = bpy.data.objects.new(name, mesh)
    obj.location = offset
    bpy.context.scene.collection.objects.link(obj)
    return obj


def build_overview_strip(source: bpy.types.Object) -> list[bpy.types.Object]:
    stages = [
        ("Stage_01_start", 1),
        ("Stage_02_heavy_sag", 16),
        ("Stage_03_crush", 42),
        ("Stage_04_melt_spread", 96),
    ]
    offsets = [-3.9, -1.3, 1.3, 3.9]
    objects = []
    for (name, frame), offset_x in zip(stages, offsets):
        objects.append(evaluated_stage_mesh(source, frame, name, Vector((offset_x, 0.0, 0.0))))
    return objects


def configure_render_scene() -> bpy.types.Object:
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = RENDER_WIDTH
    scene.render.resolution_y = RENDER_HEIGHT
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0.42
    scene.view_settings.gamma = 1.0
    if scene.world is None:
        scene.world = bpy.data.worlds.new("World")
    scene.world.color = (0.045, 0.052, 0.048)
    try:
        scene.eevee.taa_render_samples = 64
    except Exception:
        pass

    camera_data = bpy.data.cameras.new("LongaArma_DeathHeavyCrushMelt_Camera")
    camera = bpy.data.objects.new("LongaArma_DeathHeavyCrushMelt_Camera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.type = "ORTHO"

    key_data = bpy.data.lights.new("LongaArma_DeathHeavyCrushMelt_Key", "AREA")
    key = bpy.data.objects.new("LongaArma_DeathHeavyCrushMelt_Key", key_data)
    bpy.context.scene.collection.objects.link(key)
    key.location = (-3.0, -4.2, 4.0)
    key.data.energy = 760.0
    key.data.size = 5.2

    fill_data = bpy.data.lights.new("LongaArma_DeathHeavyCrushMelt_Fill", "POINT")
    fill = bpy.data.objects.new("LongaArma_DeathHeavyCrushMelt_Fill", fill_data)
    bpy.context.scene.collection.objects.link(fill)
    fill.location = (3.0, 3.4, 2.2)
    fill.data.energy = 80.0

    add_floor_plane()
    return camera


def add_floor_plane() -> None:
    mat = make_material("M_DeathHeavyCrushMelt_MatteFloor", (0.07, 0.075, 0.070, 1.0), 0.88)
    bpy.ops.mesh.primitive_plane_add(size=11.0, location=(0.0, 0.0, -0.004))
    plane = bpy.context.object
    plane.name = "Preview_Matte_Floor"
    plane.data.materials.append(mat)


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def set_camera_for_objects(camera: bpy.types.Object, objects: list[bpy.types.Object], direction: Vector, scale: float = 1.35) -> None:
    mins = Vector((float("inf"), float("inf"), float("inf")))
    maxs = Vector((float("-inf"), float("-inf"), float("-inf")))
    for obj in objects:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            mins.x = min(mins.x, world.x)
            mins.y = min(mins.y, world.y)
            mins.z = min(mins.z, world.z)
            maxs.x = max(maxs.x, world.x)
            maxs.y = max(maxs.y, world.y)
            maxs.z = max(maxs.z, world.z)
    size = maxs - mins
    center = (mins + maxs) * 0.5
    distance = max(size.x, size.y, size.z) * 3.0 + 0.8
    camera.location = center + direction.normalized() * distance + Vector((0.0, 0.0, size.z * 0.32))
    look_at(camera, center + Vector((0.0, 0.0, size.z * 0.12)))
    aspect = RENDER_WIDTH / RENDER_HEIGHT
    camera.data.ortho_scale = max(size.z * 1.75, size.x / aspect * 1.35, size.y * 1.10) * scale


def render_current(path: Path, frame: int, camera: bpy.types.Object, target: bpy.types.Object) -> None:
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    set_camera_for_objects(camera, [target], Vector((-1.0, -0.62, 0.24)), 1.28)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def render_all_previews(obj: bpy.types.Object, camera: bpy.types.Object) -> list[str]:
    rendered = []
    for file_name, frame in STATIC_RENDERS:
        output = RENDER_ROOT / file_name
        render_current(output, frame, camera, obj)
        rendered.append(str(output.relative_to(SAMPLE_ROOT)).replace("\\", "/"))

    for frame in FRAME_SAMPLES:
        output = FRAME_ROOT / f"frame_{frame:03d}.png"
        render_current(output, frame, camera, obj)
        rendered.append(str(output.relative_to(SAMPLE_ROOT)).replace("\\", "/"))

    overview_objects = build_overview_strip(obj)
    obj.hide_render = True
    obj.hide_viewport = True
    set_camera_for_objects(camera, overview_objects, Vector((0.0, -1.0, 0.28)), 1.16)
    overview_path = RENDER_ROOT / "06_sequence_overview.png"
    bpy.context.scene.frame_set(1)
    bpy.context.scene.render.filepath = str(overview_path)
    bpy.ops.render.render(write_still=True)
    obj.hide_render = False
    obj.hide_viewport = False
    rendered.append(str(overview_path.relative_to(SAMPLE_ROOT)).replace("\\", "/"))
    return rendered


def export_assets(obj: bpy.types.Object) -> None:
    export_objects = {obj}
    if obj.parent is not None and obj.parent.type == "ARMATURE":
        export_objects.add(obj.parent)
    for modifier in obj.modifiers:
        if modifier.type == "ARMATURE" and getattr(modifier, "object", None) is not None:
            export_objects.add(modifier.object)

    bpy.ops.object.select_all(action="DESELECT")
    for export_object in export_objects:
        export_object.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
    )
    bpy.ops.object.select_all(action="DESELECT")
    for export_object in export_objects:
        export_object.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.gltf(
        filepath=str(GLB_PATH),
        export_format="GLB",
        use_selection=True,
        export_materials="EXPORT",
        export_animations=True,
        export_animation_mode="ACTIONS",
        export_morph=True,
        export_morph_animation=True,
        export_force_sampling=True,
    )
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))


def write_docs(rendered_files: list[str], obj: bpy.types.Object) -> None:
    created_at = datetime.now().strftime("%Y-%m-%d %H:%M:%S KST")
    shape_keys = [key.name for key in obj.data.shape_keys.key_blocks] if obj.data.shape_keys else []
    bounds = local_bounds(obj.data)
    manifest = {
        "enemyId": "longa_arma",
        "sampleId": "death_heavy_crush_melt",
        "createdAt": created_at,
        "status": "pending_user_review",
        "sourceRuntimeBlend": str(SOURCE_RUNTIME_BLEND.relative_to(REPO_ROOT)).replace("\\", "/"),
        "sourceWalkingFbx": str(SOURCE_WALKING_FBX.relative_to(REPO_ROOT)).replace("\\", "/"),
        "sourceOriginalBlend": str(SOURCE_ORIGINAL_BLEND.relative_to(REPO_ROOT)).replace("\\", "/"),
        "deadFbxUsed": False,
        "unityRuntimeApplied": False,
        "goal": "Existing Longa Arma Unity walking mesh crushes downward under heavy weight, then spreads into a flat melt without swapping to dead.fbx.",
        "unityModelingUnifiedWith": "Assets/_Project/Art/Enemies/LongaArma/Models/longa_arma_walking.fbx",
        "sourceMeshObject": "char1",
        "meshName": obj.name,
        "vertexCount": len(obj.data.vertices),
        "polygonCount": len(obj.data.polygons),
        "shapeKeys": shape_keys,
        "frameRange": {"start": 1, "end": 96, "fps": 24},
        "keySchedule": [
            {
                "frame": frame,
                "weightSag": sag,
                "crushCollapse": crush,
                "meltSpread": spread,
            }
            for frame, sag, crush, spread in KEY_SCHEDULE
        ],
        "bounds": {
            "x": round(bounds["size"].x, 4),
            "y": round(bounds["size"].y, 4),
            "z": round(bounds["size"].z, 4),
        },
        "outputs": {
            "blend": str(BLEND_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "fbx": str(FBX_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "glb": str(GLB_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "renders": rendered_files,
            "html": str(HTML_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        },
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    README_PATH.write_text(
        f"""# Longa Arma 사망 Heavy Crush Melt 샘플

- 생성 시각: {created_at}
- 목적: `dead.fbx`를 사용하지 않고 기존 Longa Arma 런타임 메시가 무거워져 주저앉고, 압착되고, 바닥으로 퍼지는 사망 연출을 검토합니다.
- 기준 모델: `artSample/enemies/longa_arma/runtime_lowpoly/blender/longa_arma_runtime_lowpoly.blend`
- 원본 참고: `enemies model/longa arma.blend`
- Unity 적용 상태: 적용하지 않음

## 검토 방식

- `index.html`에서 프레임 타임라인을 확인합니다.
- `frames/`에는 1~96프레임 중 주요 12개 프레임이 있습니다.
- `renders/06_sequence_overview.png`는 시작, 처짐, 붕괴, 최종 퍼짐 상태를 한 장에 비교합니다.

## 모션 구성

- 1프레임: 기존 Longa Arma 자세와 동일한 시작 상태입니다.
- 16프레임: 몸 전체가 갑자기 무거워진 것처럼 아래로 처집니다.
- 42프레임: 몸통, 머리, 다리, 칼날 팔이 바닥 쪽으로 압착됩니다.
- 76프레임: 기존 메시의 정체성을 유지한 채 바닥으로 넓게 퍼집니다.
- 96프레임: 새 웅덩이 모델로 교체하지 않고 같은 메시가 바닥으로 넓게 퍼진 최종 상태입니다.

## 포함 파일

- `blender/longa_arma_death_heavy_crush_melt.blend`
- `exports/longa_arma_death_heavy_crush_melt.fbx`
- `exports/longa_arma_death_heavy_crush_melt.glb`
- `renders/*.png`
- `frames/*.png`
- `ASSET_MANIFEST.json`
- `DEATH_HEAVY_CRUSH_MELT_STATUS_2026-07-04.md`
- `index.html`

## 주의

- 이 샘플은 Unity 씬, 프리팹, 런타임 에셋에 연결하지 않았습니다.
- `dead.fbx`는 사용하지 않았습니다.
- 기존 Longa Arma 메시에서 생성한 동일 토폴로지 Shape Key 변형만 사용했습니다.
""",
        encoding="utf-8",
    )

    STATUS_PATH.write_text(
        f"""# Longa Arma Death Heavy Crush Melt Status - 2026-07-04

## 결과

- 샘플 폴더: `artSample/enemies/longa_arma/death_heavy_crush_melt/`
- 생성 시각: {created_at}
- 기준 메시: 기존 `runtime_lowpoly` Longa Arma 메시의 평가 결과
- `dead.fbx` 사용 여부: 사용하지 않음
- Unity 적용 여부: 적용하지 않음

## 생성된 변형

- `DEATH_HEAVY_01_weight_sag`
- `DEATH_HEAVY_02_crush_collapse`
- `DEATH_HEAVY_03_melt_spread`

## 검토 포인트

- 첫 프레임이 기존 Longa Arma로 보이는지 확인해야 합니다.
- 중간 프레임에서 새 사망 모델로 바뀐 듯 보이면 반려 대상입니다.
- 최종 프레임은 완전한 별도 웅덩이가 아니라 기존 몸체가 눌려 바닥으로 퍼진 형태입니다.
- 승인 전까지 Unity 사망 모션 개체에는 연결하지 않습니다.

## 실행하지 않은 항목

- Unity Refresh 또는 Bridge 명령
- `ApplyLongaArmaDeathMeltPuddle`
- Harness/EditMode/PlayMode/Build/Smoke/Validate
- Git 커밋/푸시
- `dead.fbx` 사용 또는 수정
""",
        encoding="utf-8",
    )

    frame_items = "\n".join(
        f'      <button type="button" data-src="frames/frame_{frame:03d}.png">F{frame}</button>' for frame in FRAME_SAMPLES
    )
    render_items = "\n".join(
        f'      <figure><img src="renders/{file_name}" alt="{file_name}"><figcaption>{file_name}</figcaption></figure>'
        for file_name, _frame in STATIC_RENDERS
    )
    HTML_PATH.write_text(
        f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Longa Arma Death Heavy Crush Melt</title>
  <style>
    body {{
      margin: 0;
      background: #101312;
      color: #e8eee9;
      font-family: Arial, sans-serif;
    }}
    main {{
      max-width: 1160px;
      margin: 0 auto;
      padding: 22px;
    }}
    h1 {{ font-size: 24px; margin: 0 0 10px; }}
    p {{ color: #bcc7bf; line-height: 1.55; }}
    .viewer {{
      display: grid;
      grid-template-columns: 1fr;
      gap: 10px;
      margin: 18px 0 26px;
    }}
    .viewer img {{
      width: 100%;
      background: #080a09;
      border: 1px solid #2f3832;
      display: block;
    }}
    .frames {{
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }}
    button {{
      background: #27332b;
      color: #e8eee9;
      border: 1px solid #465448;
      padding: 8px 10px;
      cursor: pointer;
    }}
    button:hover {{ background: #344238; }}
    .grid {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
      gap: 14px;
    }}
    figure {{
      margin: 0;
      background: #171d1a;
      border: 1px solid #2d352f;
      padding: 8px;
    }}
    figure img {{ width: 100%; display: block; }}
    figcaption {{ margin-top: 7px; color: #c7d1c9; font-size: 13px; }}
  </style>
</head>
<body>
<main>
  <h1>Longa Arma Death Heavy Crush Melt</h1>
  <p>기존 Longa Arma 메시가 무거워져 처지고, 압착되고, 바닥으로 퍼지는 사망 모션 샘플입니다. `dead.fbx`는 사용하지 않았고 Unity에는 아직 적용하지 않았습니다.</p>
  <section class="viewer">
    <img id="frameView" src="frames/frame_001.png" alt="Longa Arma death frame preview">
    <div class="frames">
{frame_items}
    </div>
  </section>
  <section class="grid">
{render_items}
    <figure><img src="renders/06_sequence_overview.png" alt="sequence overview"><figcaption>06_sequence_overview.png</figcaption></figure>
  </section>
</main>
<script>
  const image = document.getElementById('frameView');
  document.querySelectorAll('button[data-src]').forEach((button) => {{
    button.addEventListener('click', () => {{
      image.src = button.dataset.src;
    }});
  }});
</script>
</body>
</html>
""",
        encoding="utf-8",
    )


def main() -> None:
    ensure_dirs()
    obj = make_clean_mesh_from_runtime()
    add_heavy_crush_shape_keys(obj)
    animate_shape_keys(obj)
    camera = configure_render_scene()
    rendered_files = render_all_previews(obj, camera)
    export_assets(obj)
    write_docs(rendered_files, obj)
    print("LONGA_ARMA_DEATH_HEAVY_CRUSH_MELT_SAMPLE_CREATED")
    print(f"Mesh={obj.name}")
    print(f"Vertices={len(obj.data.vertices)}")
    print(f"Polygons={len(obj.data.polygons)}")
    print(f"DeadFbxUsed=False")
    print(f"Blend={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"GLB={GLB_PATH}")


if __name__ == "__main__":
    main()
