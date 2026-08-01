import bpy
import json
from pathlib import Path

from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"
SOURCE_INSPECTION = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
DIAGNOSTIC_DIR = SAMPLE_ROOT / "diagnostics/eye_pose_map"
OUTPUT = SAMPLE_ROOT / "EYE_POSE_MAP.json"


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def emission_material(name, color):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Emission Color"].default_value = (*color, 1.0)
    shader.inputs["Emission Strength"].default_value = 0.35
    shader.inputs["Roughness"].default_value = 0.82
    return material


def build_posed_head(mesh_obj, polygon_indices):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh(preserve_all_data_layers=True, depsgraph=depsgraph)
    used_vertices = sorted({
        vertex_index
        for polygon_index in polygon_indices
        for vertex_index in evaluated_mesh.polygons[polygon_index].vertices
    })
    remap = {old_index: new_index for new_index, old_index in enumerate(used_vertices)}
    vertices = [evaluated_mesh.vertices[index].co.copy() for index in used_vertices]
    faces = [
        [remap[index] for index in evaluated_mesh.polygons[polygon_index].vertices]
        for polygon_index in polygon_indices
    ]
    mesh = bpy.data.meshes.new("Kursa_Eye_Pose_Map_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Kursa_Eye_Pose_Map", mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.matrix_world = mesh_obj.matrix_world.copy()
    evaluated.to_mesh_clear()
    return obj


def main():
    DIAGNOSTIC_DIR.mkdir(parents=True, exist_ok=True)
    inspection = json.loads(SOURCE_INSPECTION.read_text(encoding="utf-8"))
    mesh_info = next(item for item in inspection["objects"] if item["type"] == "MESH")
    head_component = next(
        component for component in mesh_info["connected_components"]
        if component["component_id"] == 7
    )
    polygon_indices = sorted(head_component["polygon_indices"])

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    scene = bpy.context.scene
    scene.frame_set(1)
    source_obj = next(obj for obj in scene.objects if obj.type == "MESH")
    bpy.context.view_layer.update()
    posed_head = build_posed_head(source_obj, polygon_indices)
    source_obj.hide_render = True
    for obj in scene.objects:
        if obj != posed_head and obj.type not in {"CAMERA", "LIGHT"}:
            obj.hide_render = True

    bins = [
        ("x_lt_m7", (-1e9, -7.0), (0.38, 0.10, 0.70)),
        ("x_m7_m4", (-7.0, -4.0), (0.10, 0.28, 0.95)),
        ("x_m4_m1", (-4.0, -1.0), (0.06, 0.78, 0.94)),
        ("x_m1_p2", (-1.0, 2.0), (0.12, 0.82, 0.32)),
        ("x_p2_p5", (2.0, 5.0), (0.95, 0.62, 0.06)),
        ("x_ge_p5", (5.0, 1e9), (0.92, 0.12, 0.08)),
    ]
    hood_material = emission_material("PoseMap_Hood", (0.025, 0.04, 0.065))
    posed_head.data.materials.append(hood_material)
    for name, _bounds, color in bins:
        posed_head.data.materials.append(emission_material(f"PoseMap_{name}", color))

    records = []
    for diagnostic_index, source_index in enumerate(polygon_indices):
        source_polygon = source_obj.data.polygons[source_index]
        center = source_polygon.center
        material_index = 0
        bin_name = "hood"
        if 147.0 <= center.y <= 157.0 and center.z >= 5.2:
            for index, (name, (minimum, maximum), _color) in enumerate(bins, start=1):
                if minimum <= center.x < maximum:
                    material_index = index
                    bin_name = name
                    break
            records.append({
                "polygon_index": source_index,
                "bind_center": [round(float(value), 6) for value in center],
                "x_bin": bin_name,
            })
        posed_head.data.polygons[diagnostic_index].material_index = material_index

    world_points = [posed_head.matrix_world @ vertex.co for vertex in posed_head.data.vertices]
    low = Vector(tuple(min(point[axis] for point in world_points) for axis in range(3)))
    high = Vector(tuple(max(point[axis] for point in world_points) for axis in range(3)))
    center = (low + high) * 0.5
    center.z = low.z + (high.z - low.z) * 0.58
    radius = max(high - low)

    world = bpy.data.worlds.new("Eye_Pose_Map_World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.003, 0.005, 0.009, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.12
    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.lens = 80
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.look = "AgX - Medium High Contrast"

    views = []
    for name, direction in (
        ("global_front", Vector((0.0, -1.0, 0.04))),
        ("global_left_30", Vector((-0.5, -0.866, 0.04))),
        ("global_right_30", Vector((0.5, -0.866, 0.04))),
        ("global_left_60", Vector((-0.866, -0.5, 0.04))),
        ("global_right_60", Vector((0.866, -0.5, 0.04))),
    ):
        camera.location = center + direction.normalized() * radius * 2.25
        point_at(camera, center)
        path = DIAGNOSTIC_DIR / f"{name}.png"
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        views.append({"name": name, "image": path.relative_to(SAMPLE_ROOT).as_posix()})

    OUTPUT.write_text(json.dumps({
        "result": "ANALYSIS_ONLY",
        "source_model": "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx",
        "frame": 1,
        "color_basis": "Original bind-pose local X bins applied to evaluated frame-1 head polygons",
        "bins": [{"name": name, "bounds": bounds, "color": color} for name, bounds, color in bins],
        "candidate_polygons": records,
        "views": views,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(OUTPUT), "candidate_count": len(records)}))


if __name__ == "__main__":
    main()
