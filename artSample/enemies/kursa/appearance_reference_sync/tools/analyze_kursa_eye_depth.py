import bpy
import json
from pathlib import Path
from statistics import median

from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"
SOURCE_INSPECTION = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
MULTIVIEW_ANALYSIS = SAMPLE_ROOT / "EYE_MULTIVIEW_ANALYSIS.json"
DIAGNOSTIC_DIR = SAMPLE_ROOT / "diagnostics/eye_multiview"
OUTPUT = SAMPLE_ROOT / "EYE_DEPTH_ANALYSIS.json"
RESOLUTION = 320


def save_image(name, pixels):
    image = bpy.data.images.new(name, width=RESOLUTION, height=RESOLUTION, alpha=True)
    image.pixels = pixels
    image.filepath_raw = str(DIAGNOSTIC_DIR / f"{name}.png")
    image.file_format = "PNG"
    image.save()
    bpy.data.images.remove(image)


def main():
    inspection = json.loads(SOURCE_INSPECTION.read_text(encoding="utf-8"))
    multiview = json.loads(MULTIVIEW_ANALYSIS.read_text(encoding="utf-8"))
    mesh_info = next(item for item in inspection["objects"] if item["type"] == "MESH")
    head_component = next(
        component for component in mesh_info["connected_components"]
        if component["component_id"] == 7
    )
    head_polygon_indices = set(head_component["polygon_indices"])

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    scene = bpy.context.scene
    scene.frame_set(1)
    mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH")
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()

    face_polygon_indices = {
        polygon.index for polygon in mesh_obj.data.polygons
        if polygon.index in head_polygon_indices
        and 143.0 <= polygon.center.y <= 160.5
        and polygon.center.z >= 5.2
    }
    forward = Vector(multiview["posed_face_forward_world"]).normalized()
    world_up = Vector((0.0, 0.0, 1.0))
    vertical = (world_up - forward * world_up.dot(forward)).normalized()
    right = vertical.cross(forward).normalized()
    center = Vector(multiview["head_center_world"])
    horizontal_min = -0.16
    horizontal_max = 0.16
    vertical_min = -0.13
    vertical_max = 0.13
    ray_distance = 0.70

    depths = [[None for _ in range(RESOLUTION)] for _ in range(RESOLUTION)]
    polygon_ids = [[-1 for _ in range(RESOLUTION)] for _ in range(RESOLUTION)]
    hit_classes = [[0 for _ in range(RESOLUTION)] for _ in range(RESOLUTION)]
    local_coordinates = [[None for _ in range(RESOLUTION)] for _ in range(RESOLUTION)]
    for row in range(RESOLUTION):
        v = vertical_min + (vertical_max - vertical_min) * ((row + 0.5) / RESOLUTION)
        for column in range(RESOLUTION):
            u = horizontal_min + (horizontal_max - horizontal_min) * ((column + 0.5) / RESOLUTION)
            plane_point = center + right * u + vertical * v
            origin = plane_point + forward * ray_distance
            hit, location, _normal, polygon_index, hit_object, _matrix = scene.ray_cast(
                depsgraph, origin, -forward, distance=ray_distance * 2.0
            )
            if not hit or hit_object.original != mesh_obj:
                continue
            depth = (origin - location).dot(forward)
            depths[row][column] = float(depth)
            polygon_ids[row][column] = int(polygon_index)
            if polygon_index in face_polygon_indices:
                hit_classes[row][column] = 2
            elif polygon_index in head_polygon_indices:
                hit_classes[row][column] = 1
            local_point = mesh_obj.matrix_world.inverted() @ location
            local_coordinates[row][column] = [float(value) for value in local_point]

    valid_depths = [depth for row in depths for depth in row if depth is not None]
    depth_min = min(valid_depths)
    depth_max = max(valid_depths)
    depth_pixels = []
    class_pixels = []
    for row in range(RESOLUTION):
        for column in range(RESOLUTION):
            depth = depths[row][column]
            if depth is None:
                depth_pixels.extend((0.0, 0.0, 0.0, 1.0))
            else:
                normalized = (depth - depth_min) / max(1e-9, depth_max - depth_min)
                value = 1.0 - normalized
                depth_pixels.extend((value, value, value, 1.0))
            hit_class = hit_classes[row][column]
            if hit_class == 2:
                class_pixels.extend((0.75, 0.78, 0.80, 1.0))
            elif hit_class == 1:
                class_pixels.extend((0.08, 0.22, 0.40, 1.0))
            else:
                class_pixels.extend((0.0, 0.0, 0.0, 1.0))
    save_image("face_forward_depth", depth_pixels)
    save_image("face_forward_hit_class", class_pixels)

    # Record compact per-row depth statistics and deep runs. Deep runs are not
    # assumed to be eyes; they are candidates for comparison against all views.
    row_records = []
    deep_runs = []
    for row in range(RESOLUTION):
        row_depths = [depth for depth in depths[row] if depth is not None]
        if not row_depths:
            continue
        row_median = median(row_depths)
        threshold = row_median + (depth_max - depth_min) * 0.035
        run_start = None
        for column in range(RESOLUTION + 1):
            is_deep = (
                column < RESOLUTION
                and depths[row][column] is not None
                and depths[row][column] >= threshold
            )
            if is_deep and run_start is None:
                run_start = column
            elif not is_deep and run_start is not None:
                if column - run_start >= 4:
                    coords = [
                        local_coordinates[row][candidate]
                        for candidate in range(run_start, column)
                        if local_coordinates[row][candidate] is not None
                    ]
                    deep_runs.append({
                        "row": row,
                        "column_start": run_start,
                        "column_end": column - 1,
                        "local_bounds": {
                            "min": [min(point[axis] for point in coords) for axis in range(3)],
                            "max": [max(point[axis] for point in coords) for axis in range(3)],
                        },
                    })
                run_start = None
        row_records.append({
            "row": row,
            "vertical_coordinate": vertical_min + (vertical_max - vertical_min) * ((row + 0.5) / RESOLUTION),
            "hit_count": len(row_depths),
            "depth_min": min(row_depths),
            "depth_median": row_median,
            "depth_max": max(row_depths),
        })

    OUTPUT.write_text(json.dumps({
        "result": "ANALYSIS_ONLY",
        "source_model": "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx",
        "frame": 1,
        "resolution": [RESOLUTION, RESOLUTION],
        "posed_face_forward_world": [float(value) for value in forward],
        "face_plane": {
            "center_world": [float(value) for value in center],
            "right_world": [float(value) for value in right],
            "vertical_world": [float(value) for value in vertical],
            "horizontal_min": horizontal_min,
            "horizontal_max": horizontal_max,
            "vertical_min": vertical_min,
            "vertical_max": vertical_max,
        },
        "depth_range": [depth_min, depth_max],
        "face_polygon_count": len(face_polygon_indices),
        "images": [
            "diagnostics/eye_multiview/face_forward_depth.png",
            "diagnostics/eye_multiview/face_forward_hit_class.png",
        ],
        "row_statistics": row_records,
        "deep_runs": deep_runs,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "output": str(OUTPUT),
        "depth_range": [depth_min, depth_max],
        "deep_run_count": len(deep_runs),
    }))


if __name__ == "__main__":
    main()
