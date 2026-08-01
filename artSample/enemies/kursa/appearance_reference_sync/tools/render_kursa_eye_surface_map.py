import bpy
import json
import math
from pathlib import Path

from mathutils import Matrix, Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"
SOURCE_INSPECTION = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
DIAGNOSTIC_DIR = SAMPLE_ROOT / "diagnostics/eye_surface_map"
OUTPUT = SAMPLE_ROOT / "EYE_SURFACE_MAP.json"
EYE_PATCH_POLYGONS = {
    "left": [3801],
    "right": [3627],
}


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def emission_material(name, color):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeEmission")
    shader.inputs["Color"].default_value = (*color, 1.0)
    shader.inputs["Strength"].default_value = 0.8
    links.new(shader.outputs["Emission"], output.inputs["Surface"])
    return material


def build_head(mesh_obj, polygon_indices):
    used_vertices = sorted({
        vertex_index
        for polygon_index in polygon_indices
        for vertex_index in mesh_obj.data.polygons[polygon_index].vertices
    })
    remap = {old_index: new_index for new_index, old_index in enumerate(used_vertices)}
    vertices = [mesh_obj.data.vertices[index].co.copy() for index in used_vertices]
    faces = [
        [remap[index] for index in mesh_obj.data.polygons[polygon_index].vertices]
        for polygon_index in polygon_indices
    ]
    mesh = bpy.data.meshes.new("Kursa_Eye_Surface_Map_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Kursa_Eye_Surface_Map", mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def weighted_patch(mesh, polygon_indices):
    total_area = sum(mesh.polygons[index].area for index in polygon_indices)
    center = Vector((0.0, 0.0, 0.0))
    normal = Vector((0.0, 0.0, 0.0))
    for index in polygon_indices:
        polygon = mesh.polygons[index]
        center += polygon.center * polygon.area
        normal += polygon.normal * polygon.area
    center /= total_area
    normal.normalize()
    return {
        "center": [round(float(value), 6) for value in center],
        "normal": [round(float(value), 6) for value in normal],
        "total_area": round(float(total_area), 6),
        "polygons": polygon_indices,
    }


def main():
    DIAGNOSTIC_DIR.mkdir(parents=True, exist_ok=True)
    inspection = json.loads(SOURCE_INSPECTION.read_text(encoding="utf-8"))
    mesh_info = next(item for item in inspection["objects"] if item["type"] == "MESH")
    head_component = next(
        component for component in mesh_info["connected_components"]
        if component["component_id"] == 7
    )
    head_polygon_indices = sorted(head_component["polygon_indices"])

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    source_obj = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_obj = source_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_obj.to_mesh(
        preserve_all_data_layers=True, depsgraph=depsgraph
    )
    bind_patch_records = {
        name: weighted_patch(source_obj.data, polygons)
        for name, polygons in EYE_PATCH_POLYGONS.items()
    }
    posed_patch_records = {
        name: weighted_patch(evaluated_mesh, polygons)
        for name, polygons in EYE_PATCH_POLYGONS.items()
    }
    evaluated_obj.to_mesh_clear()
    head_obj = build_head(source_obj, head_polygon_indices)
    source_obj.hide_render = True
    for obj in bpy.context.scene.objects:
        if obj != head_obj and obj.type not in {"CAMERA", "LIGHT"}:
            obj.hide_render = True

    materials = [
        emission_material("Map_Hood", (0.045, 0.065, 0.09)),
        emission_material("Map_Face", (0.23, 0.29, 0.35)),
        emission_material("Map_EyeBand_Deep", (0.06, 0.14, 0.27)),
        emission_material("Map_EyeBand_Mid", (0.08, 0.55, 0.86)),
        emission_material("Map_EyeBand_Front", (0.85, 0.18, 0.08)),
        emission_material("Map_NoseCenter", (0.96, 0.78, 0.08)),
    ]
    for material in materials:
        head_obj.data.materials.append(material)

    records = []
    for diagnostic_polygon, source_polygon_index in zip(
        head_obj.data.polygons, head_polygon_indices
    ):
        source_polygon = source_obj.data.polygons[source_polygon_index]
        center = source_polygon.center
        normal = source_polygon.normal
        material_index = 0
        category = "hood"
        if 143.0 <= center.y <= 160.5 and center.z >= 5.2:
            material_index = 1
            category = "face"
        if 149.0 <= center.y <= 156.0 and center.z >= 5.2:
            if center.z < 8.5:
                material_index = 2
                category = "eye_band_deep"
            elif center.z < 11.5:
                material_index = 3
                category = "eye_band_mid"
            else:
                material_index = 4
                category = "eye_band_front"
            if -8.0 <= center.x <= -3.0:
                material_index = 5
                category = "nose_center"
        diagnostic_polygon.material_index = material_index
        if category not in {"hood", "face"}:
            records.append({
                "polygon_index": source_polygon_index,
                "category": category,
                "center": [round(float(value), 6) for value in center],
                "normal": [round(float(value), 6) for value in normal],
                "area": round(float(source_polygon.area), 6),
                "vertices": list(source_polygon.vertices),
            })

    low = Vector(tuple(min(vertex.co[axis] for vertex in head_obj.data.vertices) for axis in range(3)))
    high = Vector(tuple(max(vertex.co[axis] for vertex in head_obj.data.vertices) for axis in range(3)))
    center = (low + high) * 0.5
    center.y = 151.8
    extent = high - low

    scene = bpy.context.scene
    world = bpy.data.worlds.new("Eye_Surface_Map_World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.003, 0.005, 0.009, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.1
    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = max(extent.x, extent.y) * 1.08
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.look = "AgX - Medium High Contrast"

    forward = Vector((0.0, 0.0, 1.0))
    up_axis = Vector((0.0, 1.0, 0.0))
    views = []
    orbit_angles = list(range(-180, 181, 15))
    for angle in orbit_angles:
        name = f"rest_yaw_{angle:+04d}"
        direction = Matrix.Rotation(math.radians(angle), 4, up_axis) @ forward
        camera.location = center + direction * 80.0
        point_at(camera, center)
        output_path = DIAGNOSTIC_DIR / f"{name}.png"
        scene.render.filepath = str(output_path)
        bpy.ops.render.render(write_still=True)
        views.append({
            "name": name,
            "angle_degrees": angle,
            "image": output_path.relative_to(SAMPLE_ROOT).as_posix(),
        })

    category_counts = {}
    for record in records:
        category_counts[record["category"]] = category_counts.get(record["category"], 0) + 1
    OUTPUT.write_text(json.dumps({
        "result": "ANALYSIS_ONLY",
        "source_model": "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx",
        "coordinate_space": "Original imported mesh local coordinates",
        "head_component_id": 7,
        "eye_band_filter": {"y_min": 149.0, "y_max": 156.0, "z_min": 5.2},
        "legend": {
            "eye_band_deep": "blue, local Z < 8.5",
            "eye_band_mid": "cyan, 8.5 <= local Z < 11.5",
            "eye_band_front": "red, local Z >= 11.5",
            "nose_center": "yellow, -8 <= local X <= -3",
        },
        "category_counts": category_counts,
        "bind_pose_surface_patches": bind_patch_records,
        "frame_1_evaluated_surface_patches": posed_patch_records,
        "candidate_polygons": records,
        "views": views,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "output": str(OUTPUT),
        "candidate_count": len(records),
        "category_counts": category_counts,
    }))


if __name__ == "__main__":
    main()
