import bpy
from collections import Counter, deque
import hashlib
import json
from pathlib import Path


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
OUTPUT_JSON = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def stable_hash(value) -> str:
    return hashlib.sha256(
        json.dumps(value, sort_keys=True, separators=(",", ":")).encode("utf-8")
    ).hexdigest().upper()


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
                            min(
                                mesh.vertices[index].co[axis]
                                for index in vertex_indices
                            ),
                            6,
                        )
                        for axis in range(3)
                    ],
                    "max": [
                        round(
                            max(
                                mesh.vertices[index].co[axis]
                                for index in vertex_indices
                            ),
                            6,
                        )
                        for axis in range(3)
                    ],
                },
                "dominant_vertex_groups": [
                    {"name": name, "weight_sum": round(weight, 6)}
                    for name, weight in group_weights.most_common(5)
                ],
            }
        )
    return components


def mesh_signature(obj):
    mesh = obj.data
    return {
        "vertices": len(mesh.vertices),
        "edges": len(mesh.edges),
        "polygons": len(mesh.polygons),
        "loops": len(mesh.loops),
        "coordinates_hash": stable_hash(
            [[round(float(value), 9) for value in vertex.co] for vertex in mesh.vertices]
        ),
        "topology_hash": stable_hash(
            [list(polygon.vertices) for polygon in mesh.polygons]
        ),
        "uv_hash": stable_hash(
            {
                layer.name: [
                    [round(float(item.uv.x), 9), round(float(item.uv.y), 9)]
                    for item in layer.data
                ]
                for layer in mesh.uv_layers
            }
        ),
        "weights_hash": stable_hash(
            [
                [
                    [membership.group, round(float(membership.weight), 9)]
                    for membership in vertex.groups
                ]
                for vertex in mesh.vertices
            ]
        ),
        "vertex_groups": [group.name for group in obj.vertex_groups],
    }


def armature_signature(obj):
    return {
        "bones": len(obj.data.bones),
        "bone_hash": stable_hash(
            [
                {
                    "name": bone.name,
                    "parent": bone.parent.name if bone.parent else None,
                    "head": [round(float(value), 9) for value in bone.head_local],
                    "tail": [round(float(value), 9) for value in bone.tail_local],
                }
                for bone in obj.data.bones
            ]
        ),
    }


def main() -> None:
    SAMPLE_ROOT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))

    objects = []
    mesh_signatures = {}
    armature_signatures = {}
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
            signature = mesh_signature(obj)
            mesh_signatures[obj.name] = signature
            entry.update(
                {
                    **signature,
                    "uv_layers": [layer.name for layer in mesh.uv_layers],
                    "color_attributes": [attr.name for attr in mesh.color_attributes],
                    "material_slots": [
                        slot.material.name if slot.material else None
                        for slot in obj.material_slots
                    ],
                    "material_index_counts": {
                        str(index): sum(
                            1
                            for polygon in mesh.polygons
                            if polygon.material_index == index
                        )
                        for index in sorted(
                            {polygon.material_index for polygon in mesh.polygons}
                        )
                    },
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
                }
            )
        elif obj.type == "ARMATURE":
            signature = armature_signature(obj)
            armature_signatures[obj.name] = signature
            entry.update(signature)
            entry["bone_names"] = [bone.name for bone in obj.data.bones]
        objects.append(entry)

    materials = []
    for material in sorted(bpy.data.materials, key=lambda item: item.name):
        materials.append(
            {
                "name": material.name,
                "diffuse_color": [round(value, 6) for value in material.diffuse_color],
                "use_nodes": material.use_nodes,
            }
        )

    actions = []
    for action in sorted(bpy.data.actions, key=lambda item: item.name):
        actions.append(
            {
                "name": action.name,
                "frame_range": [
                    round(action.frame_range[0], 6),
                    round(action.frame_range[1], 6),
                ],
            }
        )

    report = {
        "source_fbx": str(SOURCE_FBX.relative_to(ROOT)).replace("\\", "/"),
        "source_fbx_bytes": SOURCE_FBX.stat().st_size,
        "source_fbx_sha256": sha256(SOURCE_FBX),
        "scene_fps": bpy.context.scene.render.fps,
        "objects": objects,
        "mesh_signatures": mesh_signatures,
        "armature_signatures": armature_signatures,
        "materials": materials,
        "actions": actions,
    }
    OUTPUT_JSON.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "source_fbx_sha256": report["source_fbx_sha256"],
                "mesh_count": len(mesh_signatures),
                "armature_count": len(armature_signatures),
                "material_count": len(materials),
                "action_count": len(actions),
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
