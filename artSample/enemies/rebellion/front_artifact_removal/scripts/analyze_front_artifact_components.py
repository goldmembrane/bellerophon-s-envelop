import bpy
import json
from collections import Counter, defaultdict, deque
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[5]
SOURCE_BLEND = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "rig_attachment_correction"
    / "blender"
    / "Rebellion_RigAttachmentCorrection.blend"
)
OUTPUT_PATH = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "front_artifact_removal"
    / "analysis"
    / "mesh_components.json"
)
LEG_BONES = {
    *(f"Bone_{index:03d}" for index in range(9, 14)),
    *(f"Bone_{index:03d}" for index in range(14, 19)),
    *(f"Bone_{index:03d}" for index in range(19, 24)),
    *(f"Bone_{index:03d}" for index in range(24, 29)),
}


def find_skinned_mesh():
    candidates = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
        and obj.vertex_groups
        and any(modifier.type == "ARMATURE" for modifier in obj.modifiers)
    ]
    if len(candidates) != 1:
        raise RuntimeError(f"Expected one skinned mesh, found {len(candidates)}")
    return candidates[0]


def connected_components(mesh):
    vertex_neighbors = defaultdict(set)
    vertex_polygons = defaultdict(list)
    used_vertices = set()
    for polygon in mesh.polygons:
        indices = list(polygon.vertices)
        used_vertices.update(indices)
        for index in indices:
            vertex_polygons[index].append(polygon.index)
            vertex_neighbors[index].update(other for other in indices if other != index)

    components = []
    remaining = set(used_vertices)
    while remaining:
        seed = min(remaining)
        queue = deque([seed])
        vertices = set()
        while queue:
            index = queue.popleft()
            if index not in remaining:
                continue
            remaining.remove(index)
            vertices.add(index)
            queue.extend(vertex_neighbors[index] & remaining)
        polygons = {
            polygon_index
            for vertex_index in vertices
            for polygon_index in vertex_polygons[vertex_index]
        }
        components.append((vertices, polygons))
    return components


def vector_list(vector):
    return [round(value, 6) for value in vector]


def component_record(mesh_object, component_index, vertices, polygons):
    world_positions = [
        mesh_object.matrix_world @ mesh_object.data.vertices[index].co
        for index in vertices
    ]
    minimum = world_positions[0].copy()
    maximum = world_positions[0].copy()
    for position in world_positions[1:]:
        for axis in range(3):
            minimum[axis] = min(minimum[axis], position[axis])
            maximum[axis] = max(maximum[axis], position[axis])

    bone_weight_sums = Counter()
    bone_vertex_counts = Counter()
    single_bone_counts = Counter()
    for vertex_index in vertices:
        vertex = mesh_object.data.vertices[vertex_index]
        memberships = []
        for membership in vertex.groups:
            name = mesh_object.vertex_groups[membership.group].name
            bone_weight_sums[name] += membership.weight
            bone_vertex_counts[name] += 1
            memberships.append((name, membership.weight))
        if len(memberships) == 1 and memberships[0][1] >= 0.999999:
            single_bone_counts[memberships[0][0]] += 1

    material_counts = Counter(
        mesh_object.data.polygons[index].material_index for index in polygons
    )
    leg_weight = sum(
        weight
        for name, weight in bone_weight_sums.items()
        if name in LEG_BONES
    )
    total_weight = sum(bone_weight_sums.values())
    extent = maximum - minimum
    return {
        "component_index": component_index,
        "vertices": len(vertices),
        "polygons": len(polygons),
        "vertex_indices": sorted(vertices),
        "polygon_indices": sorted(polygons),
        "bounds_min": vector_list(minimum),
        "bounds_max": vector_list(maximum),
        "extent": vector_list(extent),
        "centroid": vector_list(sum(world_positions, world_positions[0] * 0) / len(world_positions)),
        "material_polygon_counts": dict(sorted(material_counts.items())),
        "leg_weight_fraction": round(leg_weight / total_weight, 9)
        if total_weight
        else 0.0,
        "bone_weight_sums": [
            [name, round(weight, 6)]
            for name, weight in bone_weight_sums.most_common()
        ],
        "bone_vertex_counts": [
            [name, count] for name, count in bone_vertex_counts.most_common()
        ],
        "single_bone_vertex_counts": [
            [name, count] for name, count in single_bone_counts.most_common()
        ],
    }


def main():
    if not SOURCE_BLEND.exists():
        raise FileNotFoundError(SOURCE_BLEND)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    mesh_object = find_skinned_mesh()
    components = connected_components(mesh_object.data)
    records = [
        component_record(mesh_object, index, vertices, polygons)
        for index, (vertices, polygons) in enumerate(components)
    ]
    records.sort(key=lambda item: item["polygons"], reverse=True)
    output = {
        "source": str(SOURCE_BLEND.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "mesh": mesh_object.name,
        "vertex_count": len(mesh_object.data.vertices),
        "polygon_count": len(mesh_object.data.polygons),
        "component_count": len(records),
        "components": records,
    }
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(
        json.dumps(output, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "result": "PASS",
                "component_count": len(records),
                "components": [
                    {
                        key: record[key]
                        for key in (
                            "component_index",
                            "vertices",
                            "polygons",
                            "bounds_min",
                            "bounds_max",
                            "extent",
                            "leg_weight_fraction",
                            "bone_weight_sums",
                            "single_bone_vertex_counts",
                        )
                    }
                    for record in records
                ],
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
