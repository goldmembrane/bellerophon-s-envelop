import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[4]
SAMPLE_DIR = ROOT / "artSample" / "enemies" / "tergo_death_melt_puddle"
BLENDER_DIR = SAMPLE_DIR / "blender"
EXPORT_DIR = SAMPLE_DIR / "exports"
RENDER_DIR = SAMPLE_DIR / "renders"
SOURCE_FBX = ROOT / "Assets" / "_Project" / "Art" / "Enemies" / "Tergo" / "Models" / "tergo_dying.fbx"

SHAPE_KEYS = [
    "DEATH_TERGO_01_weight_sag",
    "DEATH_TERGO_02_crush_collapse",
    "DEATH_TERGO_03_melt_spread",
]


def ensure_dirs():
    for directory in (BLENDER_DIR, EXPORT_DIR, RENDER_DIR):
        directory.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def import_source():
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not mesh_objects:
        raise RuntimeError(f"No mesh objects imported from {SOURCE_FBX}")
    return max(mesh_objects, key=lambda obj: len(obj.data.vertices))


def select_death_action():
    actions = list(bpy.data.actions)
    if not actions:
        return None

    for action in actions:
        if "mixamo" in action.name.lower():
            return action

    return max(actions, key=lambda action: action.frame_range[1] - action.frame_range[0])


def bind_death_action_to_armatures():
    action = select_death_action()
    if action == None:
        return None

    for armature in [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]:
        armature.animation_data_create()
        armature.animation_data.action = action
    bpy.context.scene.frame_start = math.floor(action.frame_range[0])
    bpy.context.scene.frame_end = math.ceil(action.frame_range[1])
    return action


def find_imported_animation_end_frame():
    action = bind_death_action_to_armatures()
    if action != None:
        return action.frame_range[1]
    return bpy.context.scene.frame_end


def set_scene_to_final_death_pose():
    final_frame = find_imported_animation_end_frame()
    frame = math.floor(final_frame)
    bpy.context.scene.frame_set(frame, subframe=final_frame - frame)
    bpy.context.view_layer.update()
    return final_frame


def create_final_pose_basis_mesh(source):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = source.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh()
    try:
        vertices = [source.matrix_world @ vertex.co for vertex in evaluated_mesh.vertices]
        faces = [[vertex for vertex in polygon.vertices] for polygon in evaluated_mesh.polygons]
        mesh = bpy.data.meshes.new("Tergo_Death_FinalPose_MeltPuddle_Mesh")
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
    finally:
        evaluated.to_mesh_clear()

    obj = bpy.data.objects.new("Tergo_Death_FinalPose_MeltPuddle_Basis", mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def make_body_material():
    material = bpy.data.materials.new("Tergo wet green death body")
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.12, 0.36, 0.18, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.42
        bsdf.inputs["Metallic"].default_value = 0.0
        if "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = 1.0
    return material


def assign_material(obj, material):
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.material_index = 0


def bounds_for_vertices(vertices):
    coords = [vertex.co.copy() for vertex in vertices]
    mins = Vector((min(v.x for v in coords), min(v.y for v in coords), min(v.z for v in coords)))
    maxs = Vector((max(v.x for v in coords), max(v.y for v in coords), max(v.z for v in coords)))
    return mins, maxs


def axis_vectors(mins, maxs):
    extents = maxs - mins
    axes = [0, 1, 2]
    vertical_axis = max(axes, key=lambda axis: extents[axis])
    horizontal_axes = [axis for axis in axes if axis != vertical_axis]
    return vertical_axis, horizontal_axes, extents


def create_shape_keys(obj):
    mesh = obj.data
    if mesh.shape_keys:
        obj.shape_key_clear()
    basis = obj.shape_key_add(name="Basis")
    mins, maxs = bounds_for_vertices(mesh.vertices)
    vertical_axis = 2
    horizontal_axes = [0, 1]
    extents = maxs - mins
    height = max(extents[vertical_axis], 0.0001)
    center = (mins + maxs) * 0.5
    ground = mins[vertical_axis]
    longest_horizontal_extent = max(extents[axis] for axis in horizontal_axes)
    puddle_radius_base = max(longest_horizontal_extent * 0.42, height * 1.8, 0.8)
    horizontal_radius = max(longest_horizontal_extent * 0.5, 0.0001)

    for key_name in SHAPE_KEYS:
        shape = obj.shape_key_add(name=key_name)
        for index, source_point in enumerate(basis.data):
            co = source_point.co.copy()
            h = (co[vertical_axis] - ground) / height
            radial = Vector((0.0, 0.0, 0.0))
            for axis in horizontal_axes:
                radial[axis] = co[axis] - center[axis]
            radial_len = max(radial.length, 0.0001)
            original_ratio = min(radial_len / horizontal_radius, 1.0)
            wobble = 1.0 + 0.08 * math.sin(index * 0.73) + 0.05 * math.cos(index * 1.31)

            if key_name.endswith("weight_sag"):
                compressed_height = ground + height * (0.02 + h * 0.28)
                spread = 1.05 + 0.2 * h
            elif key_name.endswith("crush_collapse"):
                compressed_height = ground + height * (0.008 + h * 0.055)
                spread = 1.35 + 0.5 * h
            else:
                compressed_height = ground + max(height * 0.006, 0.008) + height * h * 0.012
                angle = math.atan2(radial.y, radial.x) + 0.14 * math.sin(index * 0.19)
                radius = puddle_radius_base * (0.26 + 0.62 * h) * (0.96 + 0.06 * wobble)
                result = co.copy()
                result[vertical_axis] = compressed_height
                result[horizontal_axes[0]] = center[horizontal_axes[0]] + math.cos(angle) * radius * 1.35
                result[horizontal_axes[1]] = center[horizontal_axes[1]] + math.sin(angle) * radius * 0.82
                shape.data[index].co = result
                continue

            result = co.copy()
            result[vertical_axis] = compressed_height
            for axis in horizontal_axes:
                result[axis] = center[axis] + radial[axis] * spread
            shape.data[index].co = result

    return {
        "vertical_axis": ["x", "y", "z"][vertical_axis],
        "shape_keys": SHAPE_KEYS,
        "vertex_count": len(mesh.vertices),
        "polygon_count": len(mesh.polygons),
    }


def normalize_for_review(objects):
    bpy.context.view_layer.update()
    all_corners = []
    for obj in objects:
        if obj.type == "MESH":
            all_corners.extend([obj.matrix_world @ Vector(corner) for corner in obj.bound_box])
    mins = Vector((min(v.x for v in all_corners), min(v.y for v in all_corners), min(v.z for v in all_corners)))
    maxs = Vector((max(v.x for v in all_corners), max(v.y for v in all_corners), max(v.z for v in all_corners)))
    center = (mins + maxs) * 0.5
    height = max((maxs - mins).z, (maxs - mins).y, (maxs - mins).x, 0.0001)
    scale = 2.2 / height
    root = bpy.data.objects.new("TergoDeathMeltPuddleRoot", None)
    bpy.context.collection.objects.link(root)
    for obj in objects:
        if obj.parent is None:
            obj.parent = root
    root.location = -center
    root.scale = (scale, scale, scale)
    bpy.context.view_layer.update()
    return root


def normalize_mesh_for_review(obj):
    mesh = obj.data
    mins, maxs = bounds_for_vertices(mesh.vertices)
    center = (mins + maxs) * 0.5
    extents = maxs - mins
    scale = 2.2 / max(max(extents.x, extents.y), extents.z, 0.0001)
    ground = mins.z

    for vertex in mesh.vertices:
        vertex.co.x = (vertex.co.x - center.x) * scale
        vertex.co.y = (vertex.co.y - center.y) * scale
        vertex.co.z = (vertex.co.z - ground) * scale

    obj.location = (0.0, 0.0, 0.0)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)
    mesh.update()


def duplicate_stage(source, name, location, weights):
    obj = source.copy()
    obj.data = source.data.copy()
    bpy.context.collection.objects.link(obj)
    obj.animation_data_clear()
    obj.name = name
    obj.location = location
    if obj.data.shape_keys:
        for key_block in obj.data.shape_keys.key_blocks:
            key_block.value = weights.get(key_block.name, 0.0)
    return obj


def create_stage_strip(source):
    strip_collection = bpy.data.collections.new("Review_Stage_Strip")
    bpy.context.scene.collection.children.link(strip_collection)
    stages = [
        ("00_final_death_lie_pose", Vector((-3.45, 0.0, 0.0)), {}),
        ("01_weight_sag", Vector((-1.15, 0.0, 0.0)), {SHAPE_KEYS[0]: 1.0}),
        ("02_crush_collapse", Vector((1.15, 0.0, 0.0)), {SHAPE_KEYS[1]: 1.0}),
        ("03_melt_spread_puddle", Vector((3.45, 0.0, 0.0)), {SHAPE_KEYS[2]: 1.0}),
    ]
    copies = []
    for name, location, weights in stages:
        copy = duplicate_stage(source, name, location, weights)
        for collection in copy.users_collection:
            collection.objects.unlink(copy)
        strip_collection.objects.link(copy)
        copies.append(copy)
    return copies


def add_ground():
    material = bpy.data.materials.new("dark review floor")
    material.diffuse_color = (0.035, 0.04, 0.045, 1.0)
    bpy.ops.mesh.primitive_plane_add(size=11.0, location=(0, 0, -0.01))
    floor = bpy.context.object
    floor.name = "검토용 바닥 평면"
    floor.data.materials.append(material)
    return floor


def setup_lighting():
    bpy.ops.object.light_add(type="AREA", location=(-3.0, -4.0, 5.0))
    key = bpy.context.object
    key.name = "검토용 키 라이트"
    key.data.energy = 650
    key.data.size = 4.0
    bpy.ops.object.light_add(type="POINT", location=(3.5, 2.0, 2.2))
    fill = bpy.context.object
    fill.name = "검토용 습윤 하이라이트"
    fill.data.energy = 80


def setup_camera(name, location, rotation, lens=42):
    bpy.ops.object.camera_add(location=location, rotation=rotation)
    camera = bpy.context.object
    camera.name = name
    camera.data.lens = lens
    bpy.context.scene.camera = camera
    return camera


def point_camera_at(camera, target):
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_image(path, camera):
    bpy.context.scene.camera = camera
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def export_files(master, stage_objects, armatures):
    bpy.ops.object.select_all(action="DESELECT")
    master.select_set(True)
    bpy.context.view_layer.objects.active = master
    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_DIR / "tergo_death_melt_puddle_blendshape.fbx"),
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=False,
        use_mesh_modifiers=False,
    )

    bpy.ops.object.select_all(action="DESELECT")
    for obj in stage_objects:
        obj.select_set(True)
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_DIR / "tergo_death_melt_puddle_preview.glb"),
        export_format="GLB",
        export_morph=True,
        use_selection=True,
    )


def write_manifest(info):
    files = []
    for path in sorted(SAMPLE_DIR.rglob("*")):
        if path.is_file() and path.name != "ASSET_MANIFEST.json":
            files.append(
                {
                    "path": str(path.relative_to(SAMPLE_DIR)).replace("\\", "/"),
                    "bytes": path.stat().st_size,
                }
            )
    manifest = {
        "sample": "tergo_death_melt_puddle",
        "source": str(SOURCE_FBX.relative_to(ROOT)).replace("\\", "/"),
        "method": "Final death-pose based BlendShape melt puddle sample",
        "status": "사용자 검토 대기",
        "unityRuntimeConnected": False,
        "blendShapes": info["shape_keys"],
        "verticalAxisDetected": info["vertical_axis"],
        "basisPose": "tergo_dying.fbx animation final lying pose",
        "sourceAnimationEndFrame": info["source_animation_end_frame"],
        "vertexCount": info["vertex_count"],
        "polygonCount": info["polygon_count"],
        "files": files,
        "notes": [
            "사망 애니메이션의 마지막 누운 포즈를 Basis로 사용한다.",
            "최종 단계는 머리, 몸통, 팔, 다리를 남기지 않고 낮은 웅덩이 형태로 압착한다.",
            "본/Transform 납작화 방식은 사용하지 않는다.",
            "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 에셋에 연결하지 않는다.",
        ],
    }
    (SAMPLE_DIR / "ASSET_MANIFEST.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


def main():
    ensure_dirs()
    clear_scene()
    source_body = import_source()
    source_animation_end_frame = set_scene_to_final_death_pose()
    body = create_final_pose_basis_mesh(source_body)
    for obj in bpy.context.scene.objects:
        if obj != body:
            obj.hide_render = True
            obj.hide_viewport = True
    normalize_mesh_for_review(body)
    material = make_body_material()
    assign_material(body, material)
    info = create_shape_keys(body)
    info["source_animation_end_frame"] = source_animation_end_frame
    body.name = "Tergo_Death_MeltPuddle_BlendShape_Model"
    body.data.name = "Tergo_Death_MeltPuddle_Mesh"
    stage_objects = create_stage_strip(body)
    body.hide_render = True
    body.hide_viewport = False
    add_ground()
    setup_lighting()

    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 64
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 1000
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.world.color = (0.025, 0.028, 0.032)

    overview_camera = setup_camera(
        "Camera_Overview",
        (0.0, -9.5, 4.4),
        (math.radians(66), 0.0, 0.0),
        30,
    )
    side_camera = setup_camera(
        "Camera_Side_Height_Check",
        (3.45, -5.2, 0.65),
        (0.0, 0.0, 0.0),
        82,
    )
    point_camera_at(side_camera, (3.45, 0.0, 0.035))
    final_camera = setup_camera(
        "Camera_Final_Puddle",
        (3.45, -3.6, 2.0),
        (0.0, 0.0, 0.0),
        64,
    )
    point_camera_at(final_camera, (3.45, 0.0, 0.05))

    render_image(RENDER_DIR / "tergo_death_melt_puddle_overview.png", overview_camera)
    render_image(RENDER_DIR / "tergo_death_melt_puddle_side_height.png", side_camera)
    render_image(RENDER_DIR / "tergo_death_melt_puddle_final_puddle.png", final_camera)

    export_files(body, stage_objects, [])
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "tergo_death_melt_puddle.blend"))
    write_manifest(info)


if __name__ == "__main__":
    main()
