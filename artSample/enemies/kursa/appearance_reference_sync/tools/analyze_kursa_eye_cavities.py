import bpy
import json
import math
from pathlib import Path
from statistics import median

from mathutils import Matrix, Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"
SOURCE_INSPECTION = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
DIAGNOSTIC_DIR = SAMPLE_ROOT / "diagnostics/eye_cavities"
OUTPUT = SAMPLE_ROOT / "EYE_CAVITY_ANALYSIS.json"


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
    shader.inputs["Strength"].default_value = 0.65
    links.new(shader.outputs["Emission"], output.inputs["Surface"])
    return material


def build_posed_head(source_obj, head_polygons):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_obj = source_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_obj.to_mesh(
        preserve_all_data_layers=True, depsgraph=depsgraph
    )
    used_vertices = sorted({
        vertex_index
        for polygon_index in head_polygons
        for vertex_index in evaluated_mesh.polygons[polygon_index].vertices
    })
    remap = {old: new for new, old in enumerate(used_vertices)}
    vertices = [evaluated_mesh.vertices[index].co.copy() for index in used_vertices]
    faces = [
        [remap[index] for index in evaluated_mesh.polygons[polygon_index].vertices]
        for polygon_index in head_polygons
    ]
    mesh = bpy.data.meshes.new("Kursa_Eye_Cavity_Diagnostic_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Kursa_Eye_Cavity_Diagnostic", mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.matrix_world = source_obj.matrix_world.copy()
    evaluated_records = {}
    normal_matrix = source_obj.matrix_world.to_3x3().inverted().transposed()
    for polygon_index in head_polygons:
        polygon = evaluated_mesh.polygons[polygon_index]
        evaluated_records[polygon_index] = {
            "center_local": polygon.center.copy(),
            "center_world": source_obj.matrix_world @ polygon.center,
            "normal_local": polygon.normal.copy(),
            "normal_world": (normal_matrix @ polygon.normal).normalized(),
            "area": float(polygon.area),
            "vertices_local": [
                evaluated_mesh.vertices[index].co.copy()
                for index in polygon.vertices
            ],
        }
    evaluated_obj.to_mesh_clear()
    return obj, evaluated_records


def percentile(values, fraction):
    ordered = sorted(values)
    if not ordered:
        return 0.0
    position = (len(ordered) - 1) * fraction
    low = math.floor(position)
    high = math.ceil(position)
    if low == high:
        return ordered[low]
    blend = position - low
    return ordered[low] * (1.0 - blend) + ordered[high] * blend


def measured_patch(evaluated, polygon_indices):
    total_area = sum(evaluated[index]["area"] for index in polygon_indices)
    center = sum(
        (evaluated[index]["center_local"] * evaluated[index]["area"] for index in polygon_indices),
        Vector((0.0, 0.0, 0.0)),
    ) / total_area
    normal = sum(
        (evaluated[index]["normal_local"] * evaluated[index]["area"] for index in polygon_indices),
        Vector((0.0, 0.0, 0.0)),
    ).normalized()
    object_up = Vector((0.0, 1.0, 0.0))
    vertical = (object_up - normal * object_up.dot(normal)).normalized()
    horizontal = vertical.cross(normal).normalized()
    vertices = [
        vertex
        for index in polygon_indices
        for vertex in evaluated[index]["vertices_local"]
    ]
    u_values = [vertex.dot(horizontal) for vertex in vertices]
    v_values = [vertex.dot(vertical) for vertex in vertices]
    n_values = [vertex.dot(normal) for vertex in vertices]
    return {
        "center": [round(float(value), 6) for value in center],
        "normal": [round(float(value), 6) for value in normal],
        "horizontal": [round(float(value), 6) for value in horizontal],
        "vertical": [round(float(value), 6) for value in vertical],
        "measured_size": [
            round(max(u_values) - min(u_values), 6),
            round(max(v_values) - min(v_values), 6),
        ],
        "normal_span": round(max(n_values) - min(n_values), 6),
        "total_area": round(total_area, 6),
        "polygons": polygon_indices,
    }


def main():
    DIAGNOSTIC_DIR.mkdir(parents=True, exist_ok=True)
    inspection = json.loads(SOURCE_INSPECTION.read_text(encoding="utf-8"))
    mesh_info = next(item for item in inspection["objects"] if item["type"] == "MESH")
    head_component = next(
        item for item in mesh_info["connected_components"]
        if item["component_id"] == 7
    )
    head_polygons = sorted(head_component["polygon_indices"])
    head_set = set(head_polygons)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    scene = bpy.context.scene
    scene.frame_set(1)
    bpy.context.view_layer.update()
    source_obj = next(obj for obj in scene.objects if obj.type == "MESH")
    posed_head, evaluated = build_posed_head(source_obj, head_polygons)

    # The production review camera defines model front as world -Y.  Candidate
    # eye surfaces are restricted using the original, undeformed face classifier.
    camera_forward = Vector((0.0, -1.0, 0.0))
    candidates = []
    for polygon_index in head_polygons:
        bind_polygon = source_obj.data.polygons[polygon_index]
        record = evaluated[polygon_index]
        center = record["center_world"]
        normal = record["normal_world"]
        if not (149.0 <= bind_polygon.center.y <= 157.2 and bind_polygon.center.z >= 5.2):
            continue
        # Keep surfaces that can form the visible front eye band, while retaining
        # oblique cavity walls. Rear/hood faces are excluded by the bind filter.
        facing = max(0.0, normal.dot(camera_forward))
        candidates.append({
            "polygon_index": polygon_index,
            "center_world": center,
            "normal_world": normal,
            "facing": facing,
            "area": record["area"],
        })

    # Compare each polygon's world-Y depth against nearby frontmost polygons in
    # screen X/Z. A positive value means the polygon is behind its surrounding rim.
    for item in candidates:
        center = item["center_world"]
        neighbors = []
        for other in candidates:
            if other is item:
                continue
            delta_x = other["center_world"].x - center.x
            delta_z = other["center_world"].z - center.z
            screen_distance = math.sqrt(delta_x * delta_x + delta_z * delta_z)
            if screen_distance <= 0.043:
                neighbors.append((screen_distance, other))
        neighbors.sort(key=lambda pair: pair[0])
        local = [other for _, other in neighbors[:18]]
        front_depth = percentile([other["center_world"].y for other in local], 0.20)
        local_median = median([other["center_world"].y for other in local]) if local else center.y
        item["rim_depth"] = float(center.y - front_depth)
        item["median_depth"] = float(center.y - local_median)
        item["neighbor_count"] = len(local)
        item["score"] = item["rim_depth"] * (0.35 + 0.65 * item["facing"])

    # Focus on the narrow band directly beneath the brow by taking the highest
    # screen-Z quarter of the classified eye band. This avoids cheek/nose minima.
    z_values = [item["center_world"].z for item in candidates]
    z_cut = percentile(z_values, 0.58)
    eye_band = [item for item in candidates if item["center_world"].z >= z_cut]
    ranked = sorted(eye_band, key=lambda item: item["score"], reverse=True)
    top = ranked[:36]

    # Cluster the deepest candidates in screen space; the two separated clusters
    # represent the two visible recessed eye sockets.
    selected = [item for item in ranked if item["rim_depth"] >= 0.006][:28]
    clusters = []
    for item in selected:
        point = Vector((item["center_world"].x, item["center_world"].z))
        matching = None
        for cluster in clusters:
            if (point - cluster["screen_center"]).length <= 0.034:
                matching = cluster
                break
        if matching is None:
            matching = {"items": [], "screen_center": point.copy()}
            clusters.append(matching)
        matching["items"].append(item)
        matching["screen_center"] = sum(
            (Vector((entry["center_world"].x, entry["center_world"].z)) for entry in matching["items"]),
            Vector((0.0, 0.0)),
        ) / len(matching["items"])
    clusters.sort(
        key=lambda cluster: max(item["score"] for item in cluster["items"]),
        reverse=True,
    )
    confirmed_eye_polygons = {
        "screen_left": [211, 512, 2302],
        "screen_right": [1909, 1910, 1911, 2174],
    }

    # Diagnostic mesh: neutral head, candidate band cyan, deepest polygons red.
    materials = [
        emission_material("Cavity_Hood", (0.025, 0.045, 0.07)),
        emission_material("Cavity_Face", (0.22, 0.28, 0.34)),
        emission_material("Cavity_EyeBand", (0.08, 0.38, 0.62)),
        emission_material("Cavity_Deep", (0.95, 0.16, 0.05)),
        emission_material("Cavity_Top", (1.0, 0.73, 0.08)),
    ]
    for material in materials:
        posed_head.data.materials.append(material)
    diagnostic_by_source = {
        source_index: diagnostic_index
        for diagnostic_index, source_index in enumerate(head_polygons)
    }
    candidate_ids = {item["polygon_index"] for item in candidates}
    top_ids = {item["polygon_index"] for item in top}
    selected_ids = {item["polygon_index"] for item in selected}
    for source_index, diagnostic_index in diagnostic_by_source.items():
        bind_polygon = source_obj.data.polygons[source_index]
        material_index = 0
        if 143.0 <= bind_polygon.center.y <= 160.5 and bind_polygon.center.z >= 5.2:
            material_index = 1
        if source_index in candidate_ids:
            material_index = 2
        if source_index in top_ids:
            material_index = 4
        if source_index in selected_ids:
            material_index = 3
        posed_head.data.polygons[diagnostic_index].material_index = material_index

    source_obj.hide_render = True
    for obj in scene.objects:
        if obj.type not in {"CAMERA", "LIGHT"} and obj != posed_head:
            obj.hide_render = True

    candidate_centers = [item["center_world"] for item in candidates]
    center = Vector((
        (min(point.x for point in candidate_centers) + max(point.x for point in candidate_centers)) * 0.5,
        min(point.y for point in candidate_centers) - 0.01,
        (min(point.z for point in candidate_centers) + max(point.z for point in candidate_centers)) * 0.5,
    ))
    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 0.34
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1400
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    world = bpy.data.worlds.new("Cavity_Diagnostic_World")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.004, 0.008, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.1
    scene.world = world
    up = Vector((0.0, 0.0, 1.0))
    views = []
    for name, yaw in (("front", 0), ("left_25", -25), ("right_25", 25), ("left_50", -50), ("right_50", 50)):
        direction = Matrix.Rotation(math.radians(yaw), 4, up) @ Vector((0.0, -1.0, 0.0))
        camera.location = center + direction * 1.1
        point_at(camera, center)
        output = DIAGNOSTIC_DIR / f"cavity_{name}.png"
        scene.render.filepath = str(output)
        bpy.ops.render.render(write_still=True)
        views.append({"name": name, "image": output.relative_to(SAMPLE_ROOT).as_posix()})

    cluster_colors = [
        (0.98, 0.08, 0.62),
        (0.12, 0.95, 0.20),
        (1.00, 0.50, 0.04),
        (0.95, 0.88, 0.05),
        (0.48, 0.16, 1.00),
        (0.04, 0.88, 0.95),
    ]
    for index, color in enumerate(cluster_colors):
        posed_head.data.materials.append(emission_material(f"Cavity_Cluster_{index + 1}", color))
    for source_index, diagnostic_index in diagnostic_by_source.items():
        bind_polygon = source_obj.data.polygons[source_index]
        material_index = 0
        if 143.0 <= bind_polygon.center.y <= 160.5 and bind_polygon.center.z >= 5.2:
            material_index = 1
        if source_index in candidate_ids:
            material_index = 2
        for cluster_index, cluster in enumerate(clusters[:len(cluster_colors)]):
            if source_index in {item["polygon_index"] for item in cluster["items"]}:
                material_index = 5 + cluster_index
                break
        posed_head.data.polygons[diagnostic_index].material_index = material_index
    for name, yaw in (("cluster_front", 0), ("cluster_left_25", -25), ("cluster_right_25", 25)):
        direction = Matrix.Rotation(math.radians(yaw), 4, up) @ Vector((0.0, -1.0, 0.0))
        camera.location = center + direction * 1.1
        point_at(camera, center)
        output = DIAGNOSTIC_DIR / f"{name}.png"
        scene.render.filepath = str(output)
        bpy.ops.render.render(write_still=True)
        views.append({"name": name, "image": output.relative_to(SAMPLE_ROOT).as_posix()})

    confirmed_materials = [
        emission_material("Confirmed_Left_Cavity", (1.00, 0.36, 0.035)),
        emission_material("Confirmed_Right_Cavity", (0.10, 0.90, 0.23)),
    ]
    for material in confirmed_materials:
        posed_head.data.materials.append(material)
    for source_index, diagnostic_index in diagnostic_by_source.items():
        bind_polygon = source_obj.data.polygons[source_index]
        material_index = 0
        if 143.0 <= bind_polygon.center.y <= 160.5 and bind_polygon.center.z >= 5.2:
            material_index = 1
        if source_index in candidate_ids:
            material_index = 2
        if source_index in set(confirmed_eye_polygons["screen_left"]):
            material_index = 11
        if source_index in set(confirmed_eye_polygons["screen_right"]):
            material_index = 12
        posed_head.data.polygons[diagnostic_index].material_index = material_index
    for name, yaw in (("confirmed_front", 0), ("confirmed_left_25", -25), ("confirmed_right_25", 25)):
        direction = Matrix.Rotation(math.radians(yaw), 4, up) @ Vector((0.0, -1.0, 0.0))
        camera.location = center + direction * 1.1
        point_at(camera, center)
        output = DIAGNOSTIC_DIR / f"{name}.png"
        scene.render.filepath = str(output)
        bpy.ops.render.render(write_still=True)
        views.append({"name": name, "image": output.relative_to(SAMPLE_ROOT).as_posix()})

    def serialize(item):
        return {
            "polygon_index": item["polygon_index"],
            "center_world": [round(float(value), 7) for value in item["center_world"]],
            "normal_world": [round(float(value), 7) for value in item["normal_world"]],
            "facing": round(item["facing"], 7),
            "area": round(item["area"], 7),
            "rim_depth": round(item["rim_depth"], 7),
            "median_depth": round(item["median_depth"], 7),
            "neighbor_count": item["neighbor_count"],
            "score": round(item["score"], 7),
        }

    confirmed_eye_patches = {
        name: measured_patch(evaluated, polygon_indices)
        for name, polygon_indices in confirmed_eye_polygons.items()
    }
    OUTPUT.write_text(json.dumps({
        "result": "ANALYSIS_ONLY",
        "source_model": "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx",
        "frame": 1,
        "front_axis_world": [0.0, -1.0, 0.0],
        "method": "Screen-space local rim-depth comparison on evaluated frame-1 geometry",
        "candidate_count": len(candidates),
        "eye_band_world_z_cut": round(z_cut, 7),
        "ranked_candidates": [serialize(item) for item in ranked],
        "clusters": [
            {
                "screen_center": [round(float(value), 7) for value in cluster["screen_center"]],
                "polygons": [item["polygon_index"] for item in cluster["items"]],
                "max_score": round(max(item["score"] for item in cluster["items"]), 7),
            }
            for cluster in clusters
        ],
        "confirmed_eye_cavity_patches": confirmed_eye_patches,
        "views": views,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "output": str(OUTPUT),
        "candidate_count": len(candidates),
        "z_cut": z_cut,
        "top": [serialize(item) for item in ranked[:12]],
        "clusters": [
            {
                "screen_center": [float(value) for value in cluster["screen_center"]],
                "polygons": [item["polygon_index"] for item in cluster["items"]],
            }
            for cluster in clusters[:6]
        ],
        "confirmed_eye_cavity_patches": confirmed_eye_patches,
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
