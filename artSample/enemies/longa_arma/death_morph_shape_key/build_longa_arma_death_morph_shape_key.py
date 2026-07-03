from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


REPO_ROOT = Path(__file__).resolve().parents[4]
SAMPLE_ROOT = Path(__file__).resolve().parent
ORIGINAL_BLEND = REPO_ROOT / "enemies model" / "longa arma.blend"
DEAD_FBX = REPO_ROOT / "enemies model" / "dead.fbx"

BLEND_PATH = SAMPLE_ROOT / "blender" / "longa_arma_death_morph_shape_key.blend"
FBX_PATH = SAMPLE_ROOT / "exports" / "longa_arma_death_morph_shape_key.fbx"
GLB_PATH = SAMPLE_ROOT / "exports" / "longa_arma_death_morph_shape_key.glb"
MANIFEST_PATH = SAMPLE_ROOT / "ASSET_MANIFEST.json"
README_PATH = SAMPLE_ROOT / "README.md"
STATUS_PATH = SAMPLE_ROOT / "DEATH_MORPH_STATUS_2026-07-03.md"
HTML_PATH = SAMPLE_ROOT / "index.html"
RENDER_DIR = SAMPLE_ROOT / "renders"


def ensure_dirs() -> None:
    for path in [BLEND_PATH.parent, FBX_PATH.parent, GLB_PATH.parent, RENDER_DIR]:
        path.mkdir(parents=True, exist_ok=True)


def make_collection(name: str, *, hide_viewport: bool = False, hide_render: bool = False) -> bpy.types.Collection:
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    collection.hide_viewport = hide_viewport
    collection.hide_render = hide_render
    return collection


def move_to_collection(obj: bpy.types.Object, collection: bpy.types.Collection) -> None:
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    collection.objects.link(obj)


def load_original_as_hidden_reference() -> bpy.types.Collection:
    bpy.ops.wm.open_mainfile(filepath=str(ORIGINAL_BLEND))
    ref_collection = make_collection("REF_original_longa_arma_hidden", hide_viewport=True, hide_render=True)
    for obj in list(bpy.context.scene.objects):
        obj.name = f"REF_original_{obj.name}"
        obj.hide_viewport = True
        obj.hide_render = True
        move_to_collection(obj, ref_collection)
    return ref_collection


def make_mat(name: str, color: tuple[float, float, float, float], roughness: float) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = color
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    if color[3] < 1.0:
        mat.blend_method = "BLEND"
        bsdf.inputs["Alpha"].default_value = color[3]
    return mat


def group_bounds(objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    mins = Vector((float("inf"), float("inf"), float("inf")))
    maxs = Vector((float("-inf"), float("-inf"), float("-inf")))
    has_mesh = False
    for obj in objects:
        if obj.type != "MESH":
            continue
        has_mesh = True
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            mins.x = min(mins.x, world.x)
            mins.y = min(mins.y, world.y)
            mins.z = min(mins.z, world.z)
            maxs.x = max(maxs.x, world.x)
            maxs.y = max(maxs.y, world.y)
            maxs.z = max(maxs.z, world.z)
    if not has_mesh:
        return Vector((-1.0, -1.0, 0.0)), Vector((1.0, 1.0, 0.1))
    return mins, maxs


def align_dead_fbx(collection: bpy.types.Collection) -> tuple[bpy.types.Object, list[bpy.types.Object], Vector]:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(DEAD_FBX))
    imported = [obj for obj in bpy.data.objects if obj not in before]
    roots = [obj for obj in imported if obj.parent not in imported]
    empty = bpy.data.objects.new("DEATH_TARGET_dead_fbx_grounded", None)
    collection.objects.link(empty)
    for obj in imported:
        move_to_collection(obj, collection)
        obj.hide_viewport = False
        obj.hide_render = False
    for obj in roots:
        obj.parent = empty
        obj.matrix_parent_inverse = empty.matrix_world.inverted()

    candidates = [
        (0.0, 0.0, 0.0),
        (math.radians(90.0), 0.0, 0.0),
        (math.radians(-90.0), 0.0, 0.0),
        (0.0, math.radians(90.0), 0.0),
        (0.0, math.radians(-90.0), 0.0),
        (0.0, 0.0, math.radians(90.0)),
        (0.0, 0.0, math.radians(-90.0)),
    ]
    best_rotation = candidates[0]
    best_score = float("inf")
    for rotation in candidates:
        empty.rotation_euler = rotation
        bpy.context.view_layer.update()
        mins, maxs = group_bounds(imported)
        size = maxs - mins
        footprint = max(size.x, size.y, 0.0001)
        score = size.z + (size.z / footprint) * 0.35
        if score < best_score:
            best_score = score
            best_rotation = rotation
    empty.rotation_euler = best_rotation
    bpy.context.view_layer.update()

    mins, maxs = group_bounds(imported)
    center = (mins + maxs) * 0.5
    footprint = max(maxs.x - mins.x, maxs.y - mins.y, 0.0001)
    scale_factor = 3.3 / footprint
    empty.scale = (scale_factor, scale_factor, scale_factor)
    bpy.context.view_layer.update()

    mins, maxs = group_bounds(imported)
    center = (mins + maxs) * 0.5
    empty.location -= Vector((center.x, center.y, mins.z))
    bpy.context.view_layer.update()
    mins, maxs = group_bounds(imported)
    return empty, imported, maxs - mins


class MeshBuilder:
    def __init__(self) -> None:
        self.verts: list[Vector] = []
        self.faces: list[tuple[int, ...]] = []
        self.material_indices: list[int] = []

    def add_uv_ellipsoid(
        self,
        center: Vector,
        radius: Vector,
        *,
        rings: int = 6,
        segments: int = 12,
        material_index: int = 0,
    ) -> None:
        start = len(self.verts)
        for ring in range(rings + 1):
            v = ring / rings
            theta = -math.pi * 0.5 + math.pi * v
            z = math.sin(theta)
            r = math.cos(theta)
            for segment in range(segments):
                u = segment / segments
                angle = math.tau * u
                wobble = 1.0 + 0.035 * math.sin(angle * 3.0 + center.y)
                point = Vector((
                    center.x + math.cos(angle) * r * radius.x * wobble,
                    center.y + math.sin(angle) * r * radius.y * wobble,
                    center.z + z * radius.z,
                ))
                self.verts.append(point)
        for ring in range(rings):
            for segment in range(segments):
                a = start + ring * segments + segment
                b = start + ring * segments + ((segment + 1) % segments)
                c = start + (ring + 1) * segments + ((segment + 1) % segments)
                d = start + (ring + 1) * segments + segment
                self.faces.append((a, b, c, d))
                self.material_indices.append(material_index)

    def add_tube(
        self,
        start_point: Vector,
        end_point: Vector,
        radius_a: float,
        radius_b: float,
        *,
        rings: int = 4,
        segments: int = 8,
        material_index: int = 0,
    ) -> None:
        start = len(self.verts)
        direction = end_point - start_point
        axis = direction.normalized()
        ref = Vector((0.0, 0.0, 1.0))
        if abs(axis.dot(ref)) > 0.92:
            ref = Vector((1.0, 0.0, 0.0))
        axis_x = axis.cross(ref).normalized()
        axis_y = axis.cross(axis_x).normalized()
        for ring in range(rings + 1):
            t = ring / rings
            center = start_point.lerp(end_point, t)
            radius = radius_a * (1.0 - t) + radius_b * t
            for segment in range(segments):
                angle = math.tau * segment / segments
                self.verts.append(center + axis_x * math.cos(angle) * radius + axis_y * math.sin(angle) * radius)
        for ring in range(rings):
            for segment in range(segments):
                a = start + ring * segments + segment
                b = start + ring * segments + ((segment + 1) % segments)
                c = start + (ring + 1) * segments + ((segment + 1) % segments)
                d = start + (ring + 1) * segments + segment
                self.faces.append((a, b, c, d))
                self.material_indices.append(material_index)


def create_death_morph_mesh(collection: bpy.types.Collection, dead_size: Vector) -> bpy.types.Object:
    builder = MeshBuilder()
    body_mat = make_mat("M_death_morph_wet_body", (0.035, 0.18, 0.11, 1.0), 0.78)
    slime_mat = make_mat("M_death_morph_glossy_puddle", (0.03, 0.32, 0.17, 0.72), 0.34)
    blade_mat = make_mat("M_death_morph_dark_blade", (0.035, 0.040, 0.045, 1.0), 0.52)

    builder.add_uv_ellipsoid(Vector((0.0, 0.0, 1.05)), Vector((0.72, 1.05, 0.42)), material_index=0)
    builder.add_uv_ellipsoid(Vector((0.0, -0.92, 1.18)), Vector((0.54, 0.55, 0.36)), material_index=0)
    builder.add_uv_ellipsoid(Vector((0.0, -1.55, 1.25)), Vector((0.34, 0.44, 0.24)), material_index=0)
    builder.add_tube(Vector((0.0, -1.78, 1.20)), Vector((0.0, -2.25, 1.10)), 0.20, 0.08, material_index=0)
    for side in [-1.0, 1.0]:
        builder.add_tube(Vector((side * 0.48, -0.62, 0.93)), Vector((side * 0.90, -1.02, 0.10)), 0.14, 0.09, material_index=0)
        builder.add_tube(Vector((side * 0.45, 0.58, 0.88)), Vector((side * 0.82, 1.02, 0.10)), 0.15, 0.10, material_index=0)
        builder.add_uv_ellipsoid(Vector((side * 0.98, -1.06, 0.07)), Vector((0.23, 0.13, 0.055)), rings=3, material_index=0)
        builder.add_uv_ellipsoid(Vector((side * 0.94, 1.05, 0.07)), Vector((0.24, 0.13, 0.055)), rings=3, material_index=0)
    builder.add_tube(Vector((0.42, -0.86, 1.20)), Vector((1.45, -1.45, 0.30)), 0.12, 0.08, material_index=2)
    builder.add_uv_ellipsoid(Vector((1.58, -1.55, 0.24)), Vector((0.36, 0.08, 0.12)), rings=3, material_index=2)

    mesh = bpy.data.meshes.new("LongaArma_DeathMorph_ContinuousMesh")
    mesh.from_pydata([tuple(v) for v in builder.verts], [], builder.faces)
    mesh.update()
    for index, face in enumerate(mesh.polygons):
        face.material_index = builder.material_indices[index]
    obj = bpy.data.objects.new("LongaArma_DeathMorph_ShapeKeyMesh", mesh)
    obj.data.materials.append(body_mat)
    obj.data.materials.append(slime_mat)
    obj.data.materials.append(blade_mat)
    collection.objects.link(obj)

    obj.shape_key_add(name="Basis")
    sag = obj.shape_key_add(name="DEATH_01_body_sag")
    collapse = obj.shape_key_add(name="DEATH_02_collapse_liquid_mass")
    puddle = obj.shape_key_add(name="DEATH_03_dead_fbx_puddle_match")

    puddle_x = max(dead_size.x * 0.52, 1.65)
    puddle_y = max(dead_size.y * 0.52, 1.25)
    for index, vertex in enumerate(obj.data.vertices):
        base = vertex.co.copy()
        radial = math.sqrt((base.x / 1.25) ** 2 + (base.y / 1.65) ** 2)
        angle = math.atan2(base.y * 0.82, base.x + 0.001)
        wobble = 1.0 + 0.18 * math.sin(angle * 3.0 + index * 0.071) + 0.10 * math.cos(angle * 5.0)
        target_radius = min(1.18, 0.36 + radial * 0.52) * wobble
        target = Vector((
            math.cos(angle) * puddle_x * target_radius + 0.10 * math.sin(index * 0.37),
            math.sin(angle) * puddle_y * target_radius - 0.16 * max(0.0, -math.sin(angle)),
            0.025 + 0.020 * math.sin(index * 0.19),
        ))
        sag.data[index].co = Vector((base.x * 1.08, base.y * 1.04, max(0.04, base.z * 0.62 - 0.05)))
        collapse.data[index].co = Vector((
            base.x * 1.32 + target.x * 0.18,
            base.y * 1.18 + target.y * 0.18,
            max(0.035, base.z * 0.22),
        ))
        puddle.data[index].co = target

    obj.data.shape_keys.key_blocks["DEATH_01_body_sag"].slider_min = 0.0
    obj.data.shape_keys.key_blocks["DEATH_02_collapse_liquid_mass"].slider_min = 0.0
    obj.data.shape_keys.key_blocks["DEATH_03_dead_fbx_puddle_match"].slider_min = 0.0
    return obj


def animate_shape_keys(obj: bpy.types.Object) -> None:
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 96
    keys = obj.data.shape_keys.key_blocks
    schedule = [
        (1, 0.0, 0.0, 0.0),
        (24, 1.0, 0.0, 0.0),
        (48, 1.0, 1.0, 0.0),
        (72, 0.35, 1.0, 0.72),
        (96, 0.0, 0.0, 1.0),
    ]
    for frame, sag, collapse, puddle in schedule:
        bpy.context.scene.frame_set(frame)
        keys["DEATH_01_body_sag"].value = sag
        keys["DEATH_02_collapse_liquid_mass"].value = collapse
        keys["DEATH_03_dead_fbx_puddle_match"].value = puddle
        keys["DEATH_01_body_sag"].keyframe_insert("value", frame=frame)
        keys["DEATH_02_collapse_liquid_mass"].keyframe_insert("value", frame=frame)
        keys["DEATH_03_dead_fbx_puddle_match"].keyframe_insert("value", frame=frame)


def duplicate_stage(source: bpy.types.Object, collection: bpy.types.Collection, name: str, frame: int, offset_x: float) -> bpy.types.Object:
    bpy.context.scene.frame_set(frame)
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = source.evaluated_get(depsgraph)
    mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=depsgraph)
    obj = bpy.data.objects.new(name, mesh)
    obj.location.x = offset_x
    for mat in source.data.materials:
        mesh.materials.append(mat)
    collection.objects.link(obj)
    return obj


def create_stage_preview(source: bpy.types.Object, target_empty: bpy.types.Object, collection: bpy.types.Collection) -> None:
    offsets = [-4.8, -2.4, 0.0, 2.4]
    stages = [
        ("STAGE_00_body", 1),
        ("STAGE_01_sag", 24),
        ("STAGE_02_collapse", 48),
        ("STAGE_03_puddle_shape_key", 96),
    ]
    for (name, frame), x in zip(stages, offsets):
        duplicate_stage(source, collection, name, frame, x)
    target_empty.location.x = 4.8
    target_empty.name = "STAGE_04_dead_fbx_target_grounded"


def set_tree_hidden(obj: bpy.types.Object, hidden: bool) -> None:
    obj.hide_viewport = hidden
    obj.hide_render = hidden
    for child in obj.children:
        set_tree_hidden(child, hidden)


def set_collection_objects_hidden(collection: bpy.types.Collection, hidden: bool) -> None:
    for obj in collection.objects:
        set_tree_hidden(obj, hidden)


def add_lights_and_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -5.0, 6.0))
    light = bpy.context.object
    light.name = "Preview_Key_AreaLight"
    light.data.energy = 520.0
    light.data.size = 5.0
    bpy.ops.object.camera_add(location=(0.0, -8.0, 3.2), rotation=(math.radians(66.0), 0.0, 0.0))
    camera = bpy.context.object
    bpy.context.scene.camera = camera
    bpy.context.scene.render.resolution_x = 1400
    bpy.context.scene.render.resolution_y = 820
    bpy.context.scene.eevee.taa_render_samples = 64


def set_camera(location: tuple[float, float, float], rotation: tuple[float, float, float], ortho_scale: float) -> None:
    camera = bpy.context.scene.camera
    if camera is None:
        bpy.ops.object.camera_add()
        camera = bpy.context.object
        bpy.context.scene.camera = camera
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale
    camera.location = location
    camera.rotation_euler = rotation


def render_frame(path: Path, frame: int) -> None:
    bpy.context.scene.frame_set(frame)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def write_docs(morph_obj: bpy.types.Object, dead_size: Vector) -> None:
    manifest = {
        "sample_id": "longa_arma_death_morph_shape_key",
        "created_on": "2026-07-03",
        "source_original_blend": str(ORIGINAL_BLEND),
        "source_dead_fbx": str(DEAD_FBX),
        "outputs": {
            "blend": str(BLEND_PATH.relative_to(REPO_ROOT)),
            "fbx": str(FBX_PATH.relative_to(REPO_ROOT)),
            "glb": str(GLB_PATH.relative_to(REPO_ROOT)),
            "renders": [
                "renders/01_death_basis.png",
                "renders/02_death_sag.png",
                "renders/03_death_collapse.png",
                "renders/04_death_puddle.png",
                "renders/05_death_sequence_overview.png",
            ],
        },
        "shape_keys": [key.name for key in morph_obj.data.shape_keys.key_blocks],
        "death_target_bounds": {"x": dead_size.x, "y": dead_size.y, "z": dead_size.z},
        "unity_runtime_applied": False,
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    README_PATH.write_text(
        """# Longa Arma Death Morph Shape Key Sample

- 목적: 롱가 아르마가 바로 사라지고 웅덩이가 켜지는 방식이 아니라, 사망 전용 단일 Morph 메시가 `몸체 -> 주저앉음 -> 액체 덩어리 -> 웅덩이`로 변형되는 샘플입니다.
- 기준 원본: `enemies model/longa arma.blend`
- 최종 웅덩이 기준: `enemies model/dead.fbx`

## 포함 파일

- `blender/longa_arma_death_morph_shape_key.blend`
- `exports/longa_arma_death_morph_shape_key.fbx`
- `exports/longa_arma_death_morph_shape_key.glb`
- `renders/01_death_basis.png`
- `renders/02_death_sag.png`
- `renders/03_death_collapse.png`
- `renders/04_death_puddle.png`
- `renders/05_death_sequence_overview.png`

## Shape Key

- `DEATH_01_body_sag`: 몸통과 머리가 아래로 처지는 1차 녹아내림입니다.
- `DEATH_02_collapse_liquid_mass`: 전체 실루엣이 바닥 가까이 찌그러지는 중간 액체 덩어리입니다.
- `DEATH_03_dead_fbx_puddle_match`: `dead.fbx` 웅덩이 풋프린트를 기준으로 납작해진 최종 형태입니다.

## 주의

- 이 샘플은 Unity 런타임에 적용하지 않았습니다.
- 원본 `longa arma.blend`와 `dead.fbx`는 직접 수정하지 않았습니다.
- 기존 롱가 아르마 메시와 `dead.fbx`는 토폴로지가 달라 직접 Shape Key로 연결할 수 없으므로, 사망 전용 동일 토폴로지 Morph 메시를 별도로 만들었습니다.
""",
        encoding="utf-8",
    )
    STATUS_PATH.write_text(
        """# Longa Arma Death Morph Status - 2026-07-03

- Blender 샘플 생성 완료.
- 사망 전용 단일 Morph 메시와 3단계 Shape Key를 생성했습니다.
- `dead.fbx`는 바닥 기준으로 정렬한 참조 타깃으로 포함했습니다.
- Unity 적용은 하지 않았습니다.
""",
        encoding="utf-8",
    )
    HTML_PATH.write_text(
        """<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <title>Longa Arma Death Morph Shape Key Sample</title>
  <style>
    body { margin: 0; font-family: Arial, sans-serif; background: #111; color: #eee; }
    main { max-width: 1180px; margin: 0 auto; padding: 24px; }
    h1 { font-size: 24px; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
    figure { margin: 0; background: #1b1b1b; padding: 10px; }
    img { width: 100%; display: block; }
    figcaption { margin-top: 8px; color: #ccc; font-size: 14px; }
  </style>
</head>
<body>
<main>
  <h1>Longa Arma Death Morph Shape Key Sample</h1>
  <p>Unity 적용 전 검토용 샘플입니다. 본체, 처짐, 붕괴, 최종 웅덩이 단계를 확인하세요.</p>
  <section class="grid">
    <figure><img src="renders/01_death_basis.png"><figcaption>1. 사망 전 본체형 Morph 메시</figcaption></figure>
    <figure><img src="renders/02_death_sag.png"><figcaption>2. 몸체가 아래로 처지는 단계</figcaption></figure>
    <figure><img src="renders/03_death_collapse.png"><figcaption>3. 액체 덩어리처럼 붕괴되는 단계</figcaption></figure>
    <figure><img src="renders/04_death_puddle.png"><figcaption>4. dead.fbx 기준 웅덩이 풋프린트</figcaption></figure>
    <figure><img src="renders/05_death_sequence_overview.png"><figcaption>5. 전체 단계 비교</figcaption></figure>
  </section>
</main>
</body>
</html>
""",
        encoding="utf-8",
    )


def main() -> None:
    ensure_dirs()
    load_original_as_hidden_reference()
    sample_collection = make_collection("LONGA_ARMA_DEATH_MORPH_SAMPLE")
    stage_collection = make_collection("LONGA_ARMA_DEATH_MORPH_STAGE_PREVIEW")
    dead_empty, dead_objects, dead_size = align_dead_fbx(sample_collection)
    set_tree_hidden(dead_empty, True)
    morph = create_death_morph_mesh(sample_collection, dead_size)
    animate_shape_keys(morph)
    add_lights_and_camera()
    set_camera((0.0, -7.6, 4.4), (math.radians(60.0), 0.0, 0.0), 6.2)
    render_frame(RENDER_DIR / "01_death_basis.png", 1)
    render_frame(RENDER_DIR / "02_death_sag.png", 24)
    render_frame(RENDER_DIR / "03_death_collapse.png", 48)
    render_frame(RENDER_DIR / "04_death_puddle.png", 96)
    create_stage_preview(morph, dead_empty, stage_collection)
    set_tree_hidden(dead_empty, False)
    morph.hide_viewport = True
    morph.hide_render = True
    set_camera((0.0, -12.0, 6.2), (math.radians(60.0), 0.0, 0.0), 13.2)
    render_frame(RENDER_DIR / "05_death_sequence_overview.png", 1)
    morph.hide_viewport = False
    morph.hide_render = False

    bpy.ops.object.select_all(action="DESELECT")
    for obj in sample_collection.objects:
        obj.select_set(True)
    for obj in stage_collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = morph
    bpy.ops.export_scene.fbx(filepath=str(FBX_PATH), use_selection=True, bake_anim=True, add_leaf_bones=False)
    bpy.ops.export_scene.gltf(filepath=str(GLB_PATH), export_format="GLB", use_selection=True)
    write_docs(morph, dead_size)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print("Longa Arma death morph shape key sample created.")
    print(f"ShapeKeys={len(morph.data.shape_keys.key_blocks)}")
    print(f"DeadTargetObjects={len(dead_objects)}")
    print(f"Blend={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"GLB={GLB_PATH}")


if __name__ == "__main__":
    main()
