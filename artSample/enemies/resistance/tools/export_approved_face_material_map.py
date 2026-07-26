import json
from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "geometry" / "approved_face_material_map.json"


def rounded(vector):
    return [round(float(component), 7) for component in vector]


def vector_object(vector):
    values = rounded(vector)
    return {"x": values[0], "y": values[1], "z": values[2]}


mesh_objects = [
    item
    for item in bpy.data.objects
    if item.type == "MESH" and item.name == "char1"
]
if len(mesh_objects) != 1:
    raise RuntimeError(
        f"Expected one char1 mesh, found {len(mesh_objects)}."
    )

mesh_object = mesh_objects[0]
mesh = mesh_object.data
if any(len(polygon.vertices) != 3 for polygon in mesh.polygons):
    raise RuntimeError("Approved Resistance mesh is not fully triangulated.")

material_names = [
    slot.material.name if slot.material else ""
    for slot in mesh_object.material_slots
]
faces = []
for polygon in mesh.polygons:
    faces.append(
        {
            "polygon_index": polygon.index,
            "material_index": polygon.material_index,
            "material_name": material_names[polygon.material_index],
            "world_vertices": [
                vector_object(
                    mesh_object.matrix_world @ mesh.vertices[index].co
                )
                for index in polygon.vertices
            ],
        }
    )

world_vertices = [
    mesh_object.matrix_world @ vertex.co
    for vertex in mesh.vertices
]
minimum = [
    min(vertex[axis] for vertex in world_vertices)
    for axis in range(3)
]
maximum = [
    max(vertex[axis] for vertex in world_vertices)
    for axis in range(3)
]

payload = {
    "source_blend": bpy.data.filepath,
    "mesh_object": mesh_object.name,
    "vertex_count": len(mesh.vertices),
    "polygon_count": len(mesh.polygons),
    "material_names": material_names,
    "world_bounds_min": rounded(minimum),
    "world_bounds_max": rounded(maximum),
    "matrix_world": [
        rounded(row)
        for row in mesh_object.matrix_world
    ],
    "faces": faces,
}

OUTPUT.parent.mkdir(parents=True, exist_ok=True)
OUTPUT.write_text(
    json.dumps(payload, ensure_ascii=False, indent=2),
    encoding="utf-8",
)
print(f"Approved Resistance face material map written: {OUTPUT}")
