import bpy
import json
import sys
from collections import Counter, defaultdict, deque


marker = sys.argv.index("--") + 1
source_path = sys.argv[marker]
output_path = sys.argv[marker + 1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source_path)
mesh_object = next(
    obj for obj in bpy.context.scene.objects
    if obj.type == "MESH"
)
mesh = mesh_object.data

vertex_to_polygons = defaultdict(list)
for polygon in mesh.polygons:
    for vertex_index in polygon.vertices:
        vertex_to_polygons[vertex_index].append(polygon.index)

remaining = set(range(len(mesh.polygons)))
components = []
while remaining:
    seed = next(iter(remaining))
    queue = deque([seed])
    remaining.remove(seed)
    polygon_indices = []
    vertex_indices = set()
    while queue:
        polygon_index = queue.popleft()
        polygon_indices.append(polygon_index)
        polygon = mesh.polygons[polygon_index]
        for vertex_index in polygon.vertices:
            vertex_indices.add(vertex_index)
            for neighbor in vertex_to_polygons[vertex_index]:
                if neighbor in remaining:
                    remaining.remove(neighbor)
                    queue.append(neighbor)

    points = [
        mesh_object.matrix_world @ mesh.vertices[index].co
        for index in vertex_indices
    ]
    minimum = [
        min(point[axis] for point in points)
        for axis in range(3)
    ]
    maximum = [
        max(point[axis] for point in points)
        for axis in range(3)
    ]
    group_weights = Counter()
    for vertex_index in vertex_indices:
        for membership in mesh.vertices[vertex_index].groups:
            group_name = mesh_object.vertex_groups[
                membership.group
            ].name
            group_weights[group_name] += membership.weight

    components.append({
        "polygon_indices": sorted(polygon_indices),
        "polygon_count": len(polygon_indices),
        "vertex_count": len(vertex_indices),
        "minimum": minimum,
        "maximum": maximum,
        "center": [
            (minimum[axis] + maximum[axis]) * 0.5
            for axis in range(3)
        ],
        "dimensions": [
            maximum[axis] - minimum[axis]
            for axis in range(3)
        ],
        "group_weight_sums": dict(
            group_weights.most_common(8)
        ),
    })

components.sort(
    key=lambda entry: entry["polygon_count"],
    reverse=True,
)
for index, component in enumerate(components):
    component["component_id"] = index

with open(output_path, "w", encoding="utf-8") as handle:
    json.dump(
        {
            "source": source_path,
            "component_count": len(components),
            "components": components,
        },
        handle,
        ensure_ascii=False,
        indent=2,
    )
