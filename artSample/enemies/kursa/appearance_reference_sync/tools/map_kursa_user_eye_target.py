import bpy
import json
import math
from pathlib import Path

from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"
SOURCE_INSPECTION = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
OUTPUT = SAMPLE_ROOT / "USER_EYE_TARGET_MAP.json"

# The user's 48 x 27 non-white crop matches this exact area of the generated
# 1280 x 1280 three-quarter render at 1.5 source pixels per crop pixel.
TARGET_BOX = (534, 154, 606, 194)


def bounds_of(objects):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects if obj.type == "MESH" for corner in obj.bound_box
    ]
    low = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    high = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    return low, high


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def screen_position(scene, camera, world_point):
    ndc = world_to_camera_view(scene, camera, world_point)
    return Vector((
        ndc.x * scene.render.resolution_x,
        (1.0 - ndc.y) * scene.render.resolution_y,
    ))


def ray_direction_from_pixel(scene, depsgraph, camera, pixel_x, pixel_y):
    width = scene.render.resolution_x
    height = scene.render.resolution_y
    projection = camera.calc_matrix_camera(
        depsgraph,
        x=width,
        y=height,
        scale_x=1.0,
        scale_y=1.0,
    )
    clip = Vector((
        2.0 * ((pixel_x + 0.5) / width) - 1.0,
        1.0 - 2.0 * ((pixel_y + 0.5) / height),
        -1.0,
        1.0,
    ))
    camera_point = projection.inverted() @ clip
    camera_point /= camera_point.w
    world_point = camera.matrix_world @ camera_point
    return (world_point.xyz - camera.matrix_world.translation).normalized()


def main():
    inspection = json.loads(SOURCE_INSPECTION.read_text(encoding="utf-8"))
    mesh_info = next(item for item in inspection["objects"] if item["type"] == "MESH")
    head_component = next(
        component for component in mesh_info["connected_components"]
        if component["component_id"] == 7
    )
    head_polygons = set(head_component["polygon_indices"])

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    scene = bpy.context.scene
    mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH")
    low, high = bounds_of([mesh_obj])
    center = (low + high) * 0.5
    center.z = low.z + (high.z - low.z) * 0.52
    extent = high - low
    radius = max(extent.x, extent.y, extent.z)
    distance = radius * 1.82

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.lens = 62
    radians = math.radians(32)
    camera.location = center + Vector((
        distance * math.sin(radians),
        -distance * math.cos(radians),
        distance * 0.09,
    ))
    point_at(camera, center)
    scene.camera = camera
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 1280
    scene.render.resolution_percentage = 100
    scene.frame_set(1)
    bpy.context.view_layer.update()

    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_obj = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_obj.to_mesh(
        preserve_all_data_layers=True, depsgraph=depsgraph
    )
    normal_matrix = mesh_obj.matrix_world.to_3x3().inverted().transposed()
    x0, y0, x1, y1 = TARGET_BOX
    expanded = (x0 - 30, y0 - 30, x1 + 30, y1 + 30)
    records = []
    for polygon in evaluated_mesh.polygons:
        if polygon.index not in head_polygons:
            continue
        center_world = mesh_obj.matrix_world @ polygon.center
        screen = screen_position(scene, camera, center_world)
        if not (expanded[0] <= screen.x <= expanded[2] and expanded[1] <= screen.y <= expanded[3]):
            continue
        normal_world = (normal_matrix @ polygon.normal).normalized()
        bind_polygon = mesh_obj.data.polygons[polygon.index]
        inside = x0 <= screen.x <= x1 and y0 <= screen.y <= y1
        ray = center_world - camera.location
        hit, hit_location, _hit_normal, hit_index, hit_object, _matrix = scene.ray_cast(
            depsgraph,
            camera.location,
            ray.normalized(),
            distance=ray.length + 0.01,
        )
        visible_at_center = bool(
            hit
            and hit_object is not None
            and hit_object.original == mesh_obj
            and hit_index == polygon.index
            and (hit_location - center_world).length <= 0.012
        )
        records.append({
            "polygon_index": polygon.index,
            "inside_target_box": inside,
            "visible_at_center": visible_at_center,
            "screen_center": [round(float(screen.x), 3), round(float(screen.y), 3)],
            "evaluated_center_local": [round(float(value), 6) for value in polygon.center],
            "evaluated_normal_local": [round(float(value), 6) for value in polygon.normal],
            "evaluated_center_world": [round(float(value), 7) for value in center_world],
            "evaluated_normal_world": [round(float(value), 7) for value in normal_world],
            "bind_center_local": [round(float(value), 6) for value in bind_polygon.center],
            "bind_normal_local": [round(float(value), 6) for value in bind_polygon.normal],
            "area": round(float(polygon.area), 6),
            "vertices": list(polygon.vertices),
        })
    evaluated_obj.to_mesh_clear()
    pixel_hits = []
    for pixel_y in range(y0, y1, 2):
        for pixel_x in range(x0, x1, 2):
            direction = ray_direction_from_pixel(
                scene, depsgraph, camera, pixel_x, pixel_y
            )
            hit, location, normal, hit_index, hit_object, _matrix = scene.ray_cast(
                depsgraph,
                camera.matrix_world.translation,
                direction,
                distance=100.0,
            )
            if not hit or hit_object is None or hit_object.original != mesh_obj:
                continue
            location_local = mesh_obj.matrix_world.inverted() @ location
            normal_local = (
                mesh_obj.matrix_world.to_3x3().transposed() @ normal
            ).normalized()
            pixel_hits.append({
                "pixel": [pixel_x, pixel_y],
                "polygon_index": hit_index,
                "location_world": [round(float(value), 7) for value in location],
                "normal_world": [round(float(value), 7) for value in normal],
                "location_local": [round(float(value), 6) for value in location_local],
                "normal_local": [round(float(value), 6) for value in normal_local],
            })
    records.sort(key=lambda item: (not item["inside_target_box"], item["screen_center"][1], item["screen_center"][0]))
    OUTPUT.write_text(json.dumps({
        "result": "ANALYSIS_ONLY",
        "source_model": "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx",
        "frame": 1,
        "matched_render": "renders/02_three_quarter_kursa_reference_match.png",
        "user_crop_nonwhite_size": [48, 27],
        "matched_source_box": list(TARGET_BOX),
        "matched_scale": 1.5,
        "camera": {
            "lens": 62,
            "azimuth_degrees": 32,
            "elevation_ratio": 0.09,
            "resolution": [1280, 1280],
        },
        "inside_target_box": [item for item in records if item["inside_target_box"]],
        "visible_inside_target_box": [
            item for item in records
            if item["inside_target_box"] and item["visible_at_center"]
        ],
        "target_box_pixel_hits_step_2": pixel_hits,
        "expanded_context": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "output": str(OUTPUT),
        "inside_count": sum(item["inside_target_box"] for item in records),
        "visible_inside_count": sum(
            item["inside_target_box"] and item["visible_at_center"]
            for item in records
        ),
        "inside": [
            [item["polygon_index"], item["screen_center"]]
            for item in records
            if item["inside_target_box"] and item["visible_at_center"]
        ],
    }))


if __name__ == "__main__":
    main()
