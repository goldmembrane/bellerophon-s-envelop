import bpy
from collections import defaultdict, deque
import colorsys
import json
import math
from pathlib import Path

from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx"
INSPECTION_JSON = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
OUTPUT_JSON = SAMPLE_ROOT / "TORSO_SURFACE_ANALYSIS.json"
RENDER_DIR = SAMPLE_ROOT / "renders"

# These are the existing connected surfaces that make up the torso. The
# analysis never edits their vertices, edges, UVs, weights, or transforms.
TORSO_COMPONENTS = {1, 27, 61, 63, 84, 96, 97}
CLUSTER_ANGLE_DEGREES = 32.0


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat(
        "-Z", "Y"
    ).to_euler()


def bounds_of(objects):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects
        if obj.type == "MESH"
        for corner in obj.bound_box
    ]
    return (
        Vector(tuple(min(point[axis] for point in points) for axis in range(3))),
        Vector(tuple(max(point[axis] for point in points) for axis in range(3))),
    )


def flat_material(name, color):
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    shader = result.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = 0.0
    shader.inputs["Roughness"].default_value = 0.78
    shader.inputs["Emission Color"].default_value = (*color, 1.0)
    shader.inputs["Emission Strength"].default_value = 0.35
    return result


def build_polygon_adjacency(mesh):
    edge_polygons = defaultdict(list)
    for polygon in mesh.polygons:
        for edge_key in polygon.edge_keys:
            edge_polygons[tuple(sorted(edge_key))].append(polygon.index)

    adjacency = defaultdict(set)
    for polygon_indices in edge_polygons.values():
        if len(polygon_indices) != 2:
            continue
        left, right = polygon_indices
        adjacency[left].add(right)
        adjacency[right].add(left)
    return adjacency


def cluster_component(mesh, polygon_indices, adjacency, angle_degrees):
    allowed = set(polygon_indices)
    angle_limit = math.radians(angle_degrees)
    remaining = set(polygon_indices)
    clusters = []

    while remaining:
        seed = remaining.pop()
        cluster = [seed]
        queue = deque([seed])
        while queue:
            current = queue.popleft()
            current_normal = mesh.polygons[current].normal
            for neighbor in adjacency[current]:
                if neighbor not in remaining or neighbor not in allowed:
                    continue
                if current_normal.angle(mesh.polygons[neighbor].normal) > angle_limit:
                    continue
                remaining.remove(neighbor)
                cluster.append(neighbor)
                queue.append(neighbor)
        clusters.append(sorted(cluster))
    return sorted(clusters, key=lambda item: (-len(item), item[0]))


def cluster_record(mesh, component_id, cluster_id, polygon_indices):
    centers = [mesh.polygons[index].center for index in polygon_indices]
    normals = [mesh.polygons[index].normal for index in polygon_indices]
    vertices = {
        vertex_index
        for polygon_index in polygon_indices
        for vertex_index in mesh.polygons[polygon_index].vertices
    }
    points = [mesh.vertices[index].co for index in vertices]
    center = sum(centers, Vector()) / len(centers)
    normal = sum(normals, Vector()).normalized()
    return {
        "component_id": component_id,
        "surface_id": cluster_id,
        "polygon_count": len(polygon_indices),
        "polygon_indices": polygon_indices,
        "center_local": [round(value, 6) for value in center],
        "average_normal_local": [round(value, 6) for value in normal],
        "bounds_local": {
            "min": [
                round(min(point[axis] for point in points), 6)
                for axis in range(3)
            ],
            "max": [
                round(max(point[axis] for point in points), 6)
                for axis in range(3)
            ],
        },
    }


def render_view(scene, camera, center, distance, angle, filename, zoom):
    radians = math.radians(angle)
    camera.location = center + Vector(
        (
            distance * math.sin(radians) / zoom,
            -distance * math.cos(radians) / zoom,
            distance * 0.02 / zoom,
        )
    )
    point_at(camera, center)
    scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def configure_scene(scene, mesh):
    low, high = bounds_of([mesh])
    extent = high - low
    center = (low + high) * 0.5
    center.z = low.z + extent.z * 0.60
    distance = max(extent.x, extent.y, extent.z) * 2.15

    world = bpy.data.worlds.new("TorsoSurfaceAnalysisWorld")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (
        0.008,
        0.010,
        0.014,
        1.0,
    )
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.15

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.lens = 58
    scene.camera = camera

    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 1280
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"
    return camera, center, distance


def assign_component_colors(mesh, component_data):
    mesh.data.materials.clear()
    mesh.data.materials.append(flat_material("NonTorso", (0.025, 0.028, 0.032)))
    for polygon in mesh.data.polygons:
        polygon.material_index = 0

    for component_id in sorted(TORSO_COMPONENTS):
        hue = (component_id * 0.61803398875) % 1.0
        color = colorsys.hsv_to_rgb(hue, 0.68, 0.92)
        material_index = len(mesh.data.materials)
        mesh.data.materials.append(
            flat_material(f"TorsoComponent_{component_id:03d}", color)
        )
        component = next(
            item
            for item in component_data
            if item["component_id"] == component_id
        )
        for polygon_index in component["polygon_indices"]:
            mesh.data.polygons[polygon_index].material_index = material_index


def assign_surface_colors(mesh, surface_records):
    mesh.data.materials.clear()
    mesh.data.materials.append(flat_material("NonTorso", (0.025, 0.028, 0.032)))
    for polygon in mesh.data.polygons:
        polygon.material_index = 0

    ordered = sorted(
        surface_records,
        key=lambda item: (
            item["component_id"],
            item["surface_id"],
        ),
    )
    for global_index, record in enumerate(ordered):
        hue = (global_index * 0.61803398875) % 1.0
        value = 0.92 if record["polygon_count"] >= 4 else 0.52
        color = colorsys.hsv_to_rgb(hue, 0.72, value)
        material_index = len(mesh.data.materials)
        mesh.data.materials.append(
            flat_material(
                (
                    f"TorsoSurface_{record['component_id']:03d}_"
                    f"{record['surface_id']:03d}"
                ),
                color,
            )
        )
        for polygon_index in record["polygon_indices"]:
            mesh.data.polygons[polygon_index].material_index = material_index


def main():
    RENDER_DIR.mkdir(parents=True, exist_ok=True)
    inspection = json.loads(INSPECTION_JSON.read_text(encoding="utf-8"))
    component_data = next(
        item for item in inspection["objects"] if item["type"] == "MESH"
    )["connected_components"]

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    scene = bpy.context.scene
    mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH")
    mesh = mesh_obj.data
    adjacency = build_polygon_adjacency(mesh)

    threshold_counts = {}
    surface_records = []
    for angle in (18.0, 24.0, 32.0, 40.0):
        per_component = {}
        for component_id in sorted(TORSO_COMPONENTS):
            component = next(
                item
                for item in component_data
                if item["component_id"] == component_id
            )
            clusters = cluster_component(
                mesh,
                component["polygon_indices"],
                adjacency,
                angle,
            )
            per_component[str(component_id)] = {
                "surface_count": len(clusters),
                "surface_polygon_counts": [len(cluster) for cluster in clusters],
            }
            if angle == CLUSTER_ANGLE_DEGREES:
                surface_records.extend(
                    cluster_record(mesh, component_id, cluster_id, cluster)
                    for cluster_id, cluster in enumerate(clusters)
                )
        threshold_counts[str(int(angle))] = per_component

    OUTPUT_JSON.write_text(
        json.dumps(
            {
                "source_model": str(SOURCE_FBX.relative_to(ROOT)),
                "torso_components": sorted(TORSO_COMPONENTS),
                "surface_rule": (
                    "Adjacent polygons in the same existing connected component "
                    f"with a normal angle <= {CLUSTER_ANGLE_DEGREES} degrees."
                ),
                "threshold_comparison": threshold_counts,
                "surfaces": surface_records,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )

    camera, center, distance = configure_scene(scene, mesh_obj)
    assign_component_colors(mesh_obj, component_data)
    render_view(
        scene,
        camera,
        center,
        distance,
        0,
        "14_torso_connected_components.png",
        2.65,
    )
    render_view(
        scene,
        camera,
        center,
        distance,
        32,
        "14_torso_connected_components_three_quarter.png",
        2.45,
    )
    assign_surface_colors(mesh_obj, surface_records)
    render_view(
        scene,
        camera,
        center,
        distance,
        0,
        "14_torso_surface_regions.png",
        2.65,
    )
    print(
        json.dumps(
            {
                "result": "PASS",
                "component_count": len(TORSO_COMPONENTS),
                "surface_count": len(surface_records),
                "analysis": str(OUTPUT_JSON),
            }
        )
    )


if __name__ == "__main__":
    main()
