import bpy
from collections import Counter, deque
import hashlib
import json
import sys
from pathlib import Path


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx"
OUTPUT_JSON = (
    ROOT
    / "artSample/enemies/pahur/appearance_reference_sync/SOURCE_MODEL_INSPECTION.json"
)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def connected_components(obj):
    mesh = obj.data
    polygons_by_vertex = [[] for _ in mesh.vertices]
    for polygon in mesh.polygons:
        for vertex_index in polygon.vertices:
            polygons_by_vertex[vertex_index].append(polygon.index)

    remaining = set(range(len(mesh.polygons)))
    components = []
    while remaining:
        start = min(remaining)
        queue = deque([start])
        remaining.remove(start)
        polygon_indices = []
        vertex_indices = set()
        while queue:
            polygon_index = queue.popleft()
            polygon = mesh.polygons[polygon_index]
            polygon_indices.append(polygon_index)
            for vertex_index in polygon.vertices:
                vertex_indices.add(vertex_index)
                for neighbor in polygons_by_vertex[vertex_index]:
                    if neighbor in remaining:
                        remaining.remove(neighbor)
                        queue.append(neighbor)

        group_weights = Counter()
        for vertex_index in vertex_indices:
            for membership in mesh.vertices[vertex_index].groups:
                group_weights[obj.vertex_groups[membership.group].name] += (
                    membership.weight
                )
        center = sum(
            (mesh.vertices[index].co for index in vertex_indices),
            start=mesh.vertices[next(iter(vertex_indices))].co.copy() * 0.0,
        ) / len(vertex_indices)
        components.append(
            {
                "component_id": len(components),
                "polygon_count": len(polygon_indices),
                "vertex_count": len(vertex_indices),
                "polygon_indices": sorted(polygon_indices),
                "center_local": [round(value, 6) for value in center],
                "bounds_local": {
                    "min": [
                        round(
                            min(mesh.vertices[index].co[axis] for index in vertex_indices),
                            6,
                        )
                        for axis in range(3)
                    ],
                    "max": [
                        round(
                            max(mesh.vertices[index].co[axis] for index in vertex_indices),
                            6,
                        )
                        for axis in range(3)
                    ],
                },
                "dominant_vertex_groups": [
                    {"name": name, "weight_sum": round(weight, 6)}
                    for name, weight in group_weights.most_common(4)
                ],
            }
        )
    return sorted(components, key=lambda item: item["component_id"])


def dominant_group_polygon_stats(obj):
    stats = {}
    for polygon in obj.data.polygons:
        weights = Counter()
        for vertex_index in polygon.vertices:
            for membership in obj.data.vertices[vertex_index].groups:
                weights[obj.vertex_groups[membership.group].name] += membership.weight
        group = weights.most_common(1)[0][0] if weights else "<none>"
        entry = stats.setdefault(
            group,
            {
                "polygon_count": 0,
                "center_min": [float("inf")] * 3,
                "center_max": [float("-inf")] * 3,
            },
        )
        entry["polygon_count"] += 1
        for axis in range(3):
            entry["center_min"][axis] = min(
                entry["center_min"][axis], polygon.center[axis]
            )
            entry["center_max"][axis] = max(
                entry["center_max"][axis], polygon.center[axis]
            )
    return {
        group: {
            "polygon_count": entry["polygon_count"],
            "center_min": [round(value, 6) for value in entry["center_min"]],
            "center_max": [round(value, 6) for value in entry["center_max"]],
        }
        for group, entry in sorted(stats.items())
    }


def main() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))

    objects = []
    for obj in sorted(bpy.context.scene.objects, key=lambda item: item.name):
        entry = {
            "name": obj.name,
            "type": obj.type,
            "parent": obj.parent.name if obj.parent else None,
            "location": [round(value, 6) for value in obj.location],
            "rotation_euler": [round(value, 6) for value in obj.rotation_euler],
            "scale": [round(value, 6) for value in obj.scale],
        }
        if obj.type == "MESH":
            mesh = obj.data
            entry.update(
                {
                    "vertices": len(mesh.vertices),
                    "edges": len(mesh.edges),
                    "polygons": len(mesh.polygons),
                    "loops": len(mesh.loops),
                    "uv_layers": [layer.name for layer in mesh.uv_layers],
                    "color_attributes": [attr.name for attr in mesh.color_attributes],
                    "material_slots": [
                        slot.material.name if slot.material else None
                        for slot in obj.material_slots
                    ],
                    "material_index_counts": {
                        str(index): sum(
                            1 for polygon in mesh.polygons if polygon.material_index == index
                        )
                        for index in sorted(
                            {polygon.material_index for polygon in mesh.polygons}
                        )
                    },
                    "vertex_groups": [group.name for group in obj.vertex_groups],
                    "armature_modifiers": [
                        modifier.object.name
                        for modifier in obj.modifiers
                        if modifier.type == "ARMATURE" and modifier.object
                    ],
                    "bounds": {
                        "min": [
                            round(min(vertex.co[axis] for vertex in mesh.vertices), 6)
                            for axis in range(3)
                        ],
                        "max": [
                            round(max(vertex.co[axis] for vertex in mesh.vertices), 6)
                            for axis in range(3)
                        ],
                    },
                    "connected_components": connected_components(obj),
                    "dominant_group_polygon_stats": dominant_group_polygon_stats(obj),
                }
            )
        elif obj.type == "ARMATURE":
            entry["bones"] = [
                {
                    "name": bone.name,
                    "parent": bone.parent.name if bone.parent else None,
                    "head": [round(value, 6) for value in bone.head_local],
                    "tail": [round(value, 6) for value in bone.tail_local],
                }
                for bone in obj.data.bones
            ]
        objects.append(entry)

    materials = []
    for material in sorted(bpy.data.materials, key=lambda item: item.name):
        materials.append(
            {
                "name": material.name,
                "diffuse_color": [
                    round(value, 6) for value in material.diffuse_color
                ],
                "use_nodes": material.use_nodes,
            }
        )

    actions = []
    for action in sorted(bpy.data.actions, key=lambda item: item.name):
        layers = getattr(action, "layers", [])
        actions.append(
            {
                "name": action.name,
                "frame_range": [
                    round(action.frame_range[0], 6),
                    round(action.frame_range[1], 6),
                ],
                "layer_count": len(layers),
            }
        )

    report = {
        "source_fbx": str(SOURCE_FBX.relative_to(ROOT)).replace("\\", "/"),
        "source_fbx_bytes": SOURCE_FBX.stat().st_size,
        "source_fbx_sha256": sha256(SOURCE_FBX),
        "scene_fps": bpy.context.scene.render.fps,
        "objects": objects,
        "materials": materials,
        "actions": actions,
    }
    OUTPUT_JSON.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
