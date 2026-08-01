import bpy
import json
import math
from pathlib import Path

from mathutils import Matrix, Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"
SOURCE_INSPECTION = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
DIAGNOSTIC_DIR = SAMPLE_ROOT / "diagnostics/eye_multiview"
OUTPUT = SAMPLE_ROOT / "EYE_MULTIVIEW_ANALYSIS.json"


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def ray_depth(scene, depsgraph, mesh_obj, plane_point, forward, distance):
    origin = plane_point + forward * distance
    hit, location, _normal, _index, hit_object, _matrix = scene.ray_cast(
        depsgraph, origin, -forward, distance=distance * 2.0
    )
    if not hit or hit_object.original != mesh_obj:
        return None
    return float((origin - location).dot(forward))


def solve_face_axis(scene, depsgraph, mesh_obj, base_forward, base_center):
    world_up = Vector((0.0, 0.0, 1.0))
    results = []
    for yaw_degrees in range(-120, 121, 3):
        forward = (
            Matrix.Rotation(math.radians(yaw_degrees), 4, world_up) @ base_forward
        ).normalized()
        vertical = (world_up - forward * world_up.dot(forward)).normalized()
        right = vertical.cross(forward).normalized()
        for center_step in range(-24, 25):
            center_offset = center_step * 0.005
            center = base_center + right * center_offset
            error = 0.0
            comparisons = 0
            matched = 0
            for row in range(13):
                v = -0.050 + 0.105 * ((row + 0.5) / 13.0)
                for column in range(11):
                    u = 0.105 * ((column + 0.5) / 11.0)
                    left_depth = ray_depth(
                        scene, depsgraph, mesh_obj,
                        center - right * u + vertical * v, forward, 0.65,
                    )
                    right_depth = ray_depth(
                        scene, depsgraph, mesh_obj,
                        center + right * u + vertical * v, forward, 0.65,
                    )
                    comparisons += 1
                    if left_depth is None and right_depth is None:
                        continue
                    if left_depth is None or right_depth is None:
                        error += 0.10
                        continue
                    matched += 1
                    error += abs(left_depth - right_depth)
            coverage = matched / comparisons
            score = error / comparisons + (1.0 - coverage) * 0.035
            results.append({
                "yaw_degrees": yaw_degrees,
                "center_offset": center_offset,
                "score": score,
                "coverage": coverage,
                "forward": forward,
                "center": center,
            })
    results.sort(key=lambda item: item["score"])
    return results[0], results[:12]


def diagnostic_material(name, base_color):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    wire = nodes.new("ShaderNodeWireframe")
    wire.inputs["Size"].default_value = 0.003
    mix = nodes.new("ShaderNodeMixRGB")
    mix.blend_type = "MIX"
    mix.inputs[1].default_value = (*base_color, 1.0)
    mix.inputs[2].default_value = (0.003, 0.005, 0.008, 1.0)
    links.new(wire.outputs["Fac"], mix.inputs[0])
    links.new(mix.outputs["Color"], shader.inputs["Base Color"])
    shader.inputs["Metallic"].default_value = 0.12
    shader.inputs["Roughness"].default_value = 0.72
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def build_posed_head(mesh_obj, head_polygon_indices):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh(preserve_all_data_layers=True, depsgraph=depsgraph)

    used_vertices = sorted({
        vertex_index
        for polygon_index in head_polygon_indices
        for vertex_index in evaluated_mesh.polygons[polygon_index].vertices
    })
    remap = {old_index: new_index for new_index, old_index in enumerate(used_vertices)}
    vertices = [evaluated_mesh.vertices[index].co.copy() for index in used_vertices]
    faces = [
        [remap[index] for index in evaluated_mesh.polygons[polygon_index].vertices]
        for polygon_index in head_polygon_indices
    ]
    diagnostic_mesh = bpy.data.meshes.new("Kursa_Eye_Diagnostic_Head_Mesh")
    diagnostic_mesh.from_pydata(vertices, [], faces)
    diagnostic_mesh.update()
    diagnostic_obj = bpy.data.objects.new("Kursa_Eye_Diagnostic_Head", diagnostic_mesh)
    bpy.context.scene.collection.objects.link(diagnostic_obj)
    diagnostic_obj.matrix_world = mesh_obj.matrix_world.copy()

    original_to_diagnostic = {
        polygon_index: diagnostic_index
        for diagnostic_index, polygon_index in enumerate(head_polygon_indices)
    }
    evaluated.to_mesh_clear()
    return diagnostic_obj, original_to_diagnostic


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
    scene = bpy.context.scene
    scene.frame_set(1)
    mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH")
    armature_obj = next(obj for obj in scene.objects if obj.type == "ARMATURE")
    bpy.context.view_layer.update()

    # Derive the posed face-forward direction from the original front-facing
    # polygons instead of assuming that the character/world axes face the camera.
    source_front_polygons = [
        polygon for polygon in mesh_obj.data.polygons
        if polygon.index in set(head_polygon_indices)
        and 143.0 <= polygon.center.y <= 160.5
        and polygon.center.z >= 5.2
        and polygon.normal.z >= 0.65
    ]
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_obj = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_obj.to_mesh(preserve_all_data_layers=True, depsgraph=depsgraph)
    normal_matrix = mesh_obj.matrix_world.to_3x3().inverted().transposed()
    forward = Vector((0.0, 0.0, 0.0))
    for source_polygon in source_front_polygons:
        posed_polygon = evaluated_mesh.polygons[source_polygon.index]
        forward += (normal_matrix @ posed_polygon.normal).normalized() * posed_polygon.area
    forward.normalize()
    rejected_average_normal = forward.copy()
    headfront_bone = armature_obj.pose.bones["headfront"]
    forward = (
        armature_obj.matrix_world.to_3x3()
        @ (headfront_bone.tail - headfront_bone.head)
    ).normalized()
    headfront_head = armature_obj.matrix_world @ headfront_bone.head
    up = Vector((0.0, 0.0, 1.0))
    base_center = headfront_head + up * 0.075
    solved_axis, best_axis_candidates = solve_face_axis(
        scene, depsgraph, mesh_obj, forward, base_center
    )
    forward = solved_axis["forward"]
    evaluated_obj.to_mesh_clear()

    diagnostic_obj, original_to_diagnostic = build_posed_head(mesh_obj, head_polygon_indices)
    mesh_obj.hide_render = True
    for obj in scene.objects:
        if obj.type not in {"CAMERA", "LIGHT"} and obj != diagnostic_obj:
            obj.hide_render = True

    hood_material = diagnostic_material("Eye_Diagnostic_Hood", (0.025, 0.04, 0.065))
    face_material = diagnostic_material("Eye_Diagnostic_Face", (0.11, 0.14, 0.17))
    diagnostic_obj.data.materials.append(hood_material)
    diagnostic_obj.data.materials.append(face_material)
    face_polygon_indices = []
    for source_polygon_index, diagnostic_polygon_index in original_to_diagnostic.items():
        polygon = mesh_obj.data.polygons[source_polygon_index]
        is_face = 143.0 <= polygon.center.y <= 160.5 and polygon.center.z >= 5.2
        diagnostic_obj.data.polygons[diagnostic_polygon_index].material_index = 1 if is_face else 0
        if is_face:
            face_polygon_indices.append(source_polygon_index)

    center = solved_axis["center"]
    low = center - Vector((0.16, 0.16, 0.14))
    high = center + Vector((0.16, 0.16, 0.14))
    radius = 0.32
    distance = 0.82

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.name = "Eye_Multiview_Camera"
    camera.data.lens = 78
    scene.camera = camera

    world = bpy.data.worlds.new("Eye_Diagnostic_World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.025, 0.035, 0.05, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.24

    for name, direction, energy, size in (
        ("EyeDiag_Key", forward + up * 0.65, 145, radius * 0.9),
        ("EyeDiag_Fill", -forward + up * 0.25, 46, radius * 0.8),
        ("EyeDiag_Top", up, 82, radius * 0.7),
    ):
        bpy.ops.object.light_add(type="AREA")
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.size = size
        light.location = center + direction.normalized() * radius * 2.0
        point_at(light, center)

    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.look = "AgX - Medium High Contrast"

    views = [
        ("actual_front", 0.0, 0.0),
        ("left_30", -30.0, 0.0),
        ("left_60", -60.0, 0.0),
        ("left_90", -90.0, 0.0),
        ("right_30", 30.0, 0.0),
        ("right_60", 60.0, 0.0),
        ("right_90", 90.0, 0.0),
        ("top_25", 0.0, 25.0),
        ("bottom_25", 0.0, -25.0),
    ]
    view_records = []
    for name, yaw_degrees, pitch_degrees in views:
        yaw = Matrix.Rotation(math.radians(yaw_degrees), 4, up)
        direction = (yaw @ forward).normalized()
        side = direction.cross(up).normalized()
        pitch = Matrix.Rotation(math.radians(pitch_degrees), 4, side)
        direction = (pitch @ direction).normalized()
        camera.location = center + direction * distance
        point_at(camera, center)
        output_path = DIAGNOSTIC_DIR / f"no_eye_{name}.png"
        scene.render.filepath = str(output_path)
        bpy.ops.render.render(write_still=True)
        view_records.append({
            "name": name,
            "yaw_degrees": yaw_degrees,
            "pitch_degrees": pitch_degrees,
            "camera_location": [round(float(value), 6) for value in camera.location],
            "image": output_path.relative_to(SAMPLE_ROOT).as_posix(),
        })

    OUTPUT.write_text(json.dumps({
        "result": "ANALYSIS_ONLY",
        "source_model": "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx",
        "frame": 1,
        "head_component_id": 7,
        "head_polygon_count": len(head_polygon_indices),
        "face_polygon_count": len(face_polygon_indices),
        "forward_basis_polygon_count": len(source_front_polygons),
        "rejected_average_polygon_normal_world": [round(float(value), 8) for value in rejected_average_normal],
        "forward_basis": "bilateral depth-symmetry search around posed headfront bone",
        "posed_face_forward_world": [round(float(value), 8) for value in forward],
        "headfront_head_world": [round(float(value), 8) for value in headfront_head],
        "solved_face_axis": {
            "yaw_from_headfront_degrees": solved_axis["yaw_degrees"],
            "horizontal_center_offset": solved_axis["center_offset"],
            "symmetry_score": solved_axis["score"],
            "matched_ray_coverage": solved_axis["coverage"],
        },
        "best_axis_candidates": [
            {
                "yaw_from_headfront_degrees": item["yaw_degrees"],
                "horizontal_center_offset": item["center_offset"],
                "symmetry_score": item["score"],
                "matched_ray_coverage": item["coverage"],
            }
            for item in best_axis_candidates
        ],
        "head_center_world": [round(float(value), 6) for value in center],
        "head_bounds_world": {
            "min": [round(float(value), 6) for value in low],
            "max": [round(float(value), 6) for value in high],
        },
        "eye_overlay_enabled": False,
        "diagnostic_geometry_only": True,
        "views": view_records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "output": str(OUTPUT),
        "posed_face_forward_world": [round(float(value), 8) for value in forward],
        "views": len(view_records),
    }))


if __name__ == "__main__":
    main()
