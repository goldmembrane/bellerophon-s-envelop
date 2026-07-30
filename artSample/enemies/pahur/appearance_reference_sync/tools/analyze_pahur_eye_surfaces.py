import json
from collections import defaultdict
from pathlib import Path

import bpy


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx"
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
ASSIGNMENT_PATH = SAMPLE_ROOT / "MATERIAL_ASSIGNMENT.json"
OUTPUT_PATH = SAMPLE_ROOT / "EYE_SURFACE_ANALYSIS.json"
EYE_SURFACE_FITS = {
    "left_eye": {
        "source_polygon_indices": [
            737,
            738,
            739,
            1881,
            1882,
            1885,
            2330,
            2948,
            2950,
            3057,
            3058,
            3320,
        ],
        "evaluated_origin": [11.602, 162.825, 14.043],
        "u_axis": [0.806456, 0.0, 0.591294],
        "v_axis": [-0.171074, 0.957232, 0.233325],
        "projection_width": 5.0,
        "projection_height": 3.0,
        "rotation_degrees": -16.0,
    },
    "right_eye": {
        "source_polygon_indices": [
            979,
            981,
            982,
            983,
            984,
            1693,
            2203,
            2204,
            2205,
            2206,
            2207,
            3075,
            3076,
        ],
        "evaluated_origin": [19.267, 162.884, 14.631],
        "u_axis": [-0.952586, 0.0, 0.304270],
        "v_axis": [0.054923, 0.983573, 0.171950],
        "projection_width": 5.0,
        "projection_height": 3.0,
        "rotation_degrees": -14.0,
    },
}


def rounded_vector(vector):
    return [round(float(value), 6) for value in vector]


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    mesh_object = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    mesh = mesh_object.data
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_object = mesh_object.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_object.to_mesh()
    assignment = json.loads(ASSIGNMENT_PATH.read_text(encoding="utf-8"))
    face_indices = {
        item["polygon_index"]
        for item in assignment["assignments"]
        if item["material"] == "face_metal"
    }

    edge_to_faces = defaultdict(list)
    for polygon_index in face_indices:
        polygon = mesh.polygons[polygon_index]
        vertices = list(polygon.vertices)
        for offset, start in enumerate(vertices):
            end = vertices[(offset + 1) % len(vertices)]
            edge_to_faces[tuple(sorted((start, end)))].append(polygon_index)

    adjacency = defaultdict(set)
    for touching_faces in edge_to_faces.values():
        for face_index in touching_faces:
            adjacency[face_index].update(
                other for other in touching_faces if other != face_index
            )

    uv_layer = mesh.uv_layers.active
    polygons = []
    for polygon_index in sorted(face_indices):
        polygon = mesh.polygons[polygon_index]
        evaluated_polygon = evaluated_mesh.polygons[polygon_index]
        coordinates = [mesh.vertices[index].co for index in polygon.vertices]
        evaluated_coordinates = [
            evaluated_mesh.vertices[index].co for index in polygon.vertices
        ]
        loops = list(polygon.loop_indices)
        uvs = (
            [rounded_vector(uv_layer.data[index].uv) for index in loops]
            if uv_layer
            else []
        )
        polygons.append(
            {
                "polygon_index": polygon_index,
                "center": rounded_vector(polygon.center),
                "normal": rounded_vector(polygon.normal),
                "evaluated_center": rounded_vector(evaluated_polygon.center),
                "evaluated_normal": rounded_vector(evaluated_polygon.normal),
                "area": round(float(polygon.area), 6),
                "vertex_indices": list(polygon.vertices),
                "vertices": [rounded_vector(coordinate) for coordinate in coordinates],
                "evaluated_vertices": [
                    rounded_vector(coordinate)
                    for coordinate in evaluated_coordinates
                ],
                "uvs": uvs,
                "face_neighbors": sorted(adjacency[polygon_index]),
            }
        )

    axis_bounds = {
        axis: [
            round(
                min(
                    coordinate[axis_index]
                    for item in polygons
                    for coordinate in item["vertices"]
                ),
                6,
            ),
            round(
                max(
                    coordinate[axis_index]
                    for item in polygons
                    for coordinate in item["vertices"]
                ),
                6,
            ),
        ]
        for axis_index, axis in enumerate(("x", "y", "z"))
    }
    OUTPUT_PATH.write_text(
        json.dumps(
            {
                "source": "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx",
                "purpose": (
                    "Read-only surface record for constraining the two eye "
                    "materials to existing upper-face polygons."
                ),
                "mesh_changed": False,
                "evaluated_frame": bpy.context.scene.frame_current,
                "face_polygon_count": len(polygons),
                "face_axis_bounds": axis_bounds,
                "eye_surface_fits": EYE_SURFACE_FITS,
                "polygons": polygons,
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )
    evaluated_object.to_mesh_clear()
    print(
        f"Recorded {len(polygons)} face polygons to {OUTPUT_PATH} "
        f"with bounds {axis_bounds}."
    )


if __name__ == "__main__":
    main()
