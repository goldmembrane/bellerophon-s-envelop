import bpy
import json
from collections import defaultdict
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[5]
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / "rebellion" / "rig_attachment_correction"
SOURCE_GLB = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "appearance_reference_sync"
    / "source"
    / "Rebellion_Unity_Source_Unmodified.glb"
)
APPROVED_GLB = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "appearance_reference_sync"
    / "exports"
    / "Rebellion_Appearance_ReferenceSync.glb"
)
OUTPUT_PATH = SAMPLE_ROOT / "analysis" / "body_bone_analysis.json"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (
        bpy.data.meshes,
        bpy.data.armatures,
        bpy.data.materials,
        bpy.data.images,
    ):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def find_rig_objects():
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one armature, found {len(armatures)}")
    skinned = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
        and obj.vertex_groups
        and any(modifier.type == "ARMATURE" for modifier in obj.modifiers)
    ]
    if len(skinned) != 1:
        raise RuntimeError(f"Expected one skinned mesh, found {len(skinned)}")
    return armatures[0], skinned[0]


def in_region(name, coordinate):
    x, y, z = coordinate
    if name == "front_panel":
        return abs(x) <= 0.65 and -1.35 <= y <= -0.70 and 1.10 <= z <= 1.70
    if name == "upper_body":
        return abs(x) <= 1.25 and abs(y) <= 1.35 and 0.95 <= z <= 1.72
    if name == "lower_legs":
        return z <= 0.50
    raise KeyError(name)


def summarize_region(mesh_object, region_name):
    weight_totals = defaultdict(float)
    influence_counts = defaultdict(int)
    dominant_counts = defaultdict(int)
    vertex_count = 0
    for vertex in mesh_object.data.vertices:
        coordinate = mesh_object.matrix_world @ vertex.co
        if not in_region(region_name, coordinate):
            continue
        vertex_count += 1
        influences = []
        for membership in vertex.groups:
            group_name = mesh_object.vertex_groups[membership.group].name
            weight_totals[group_name] += membership.weight
            influence_counts[group_name] += 1
            influences.append((group_name, membership.weight))
        if influences:
            dominant_name = max(influences, key=lambda item: item[1])[0]
            dominant_counts[dominant_name] += 1
    return {
        "vertex_count": vertex_count,
        "weight_totals": dict(
            sorted(weight_totals.items(), key=lambda item: (-item[1], item[0]))
        ),
        "influence_counts": dict(
            sorted(influence_counts.items(), key=lambda item: (-item[1], item[0]))
        ),
        "dominant_counts": dict(
            sorted(dominant_counts.items(), key=lambda item: (-item[1], item[0]))
        ),
    }


def summarize_groups(mesh_object):
    accumulators = defaultdict(lambda: {"weight": 0.0, "position": [0.0, 0.0, 0.0]})
    for vertex in mesh_object.data.vertices:
        coordinate = mesh_object.matrix_world @ vertex.co
        for membership in vertex.groups:
            group_name = mesh_object.vertex_groups[membership.group].name
            entry = accumulators[group_name]
            entry["weight"] += membership.weight
            entry["position"][0] += coordinate.x * membership.weight
            entry["position"][1] += coordinate.y * membership.weight
            entry["position"][2] += coordinate.z * membership.weight
    result = {}
    for group_name, entry in sorted(accumulators.items()):
        weight = entry["weight"]
        result[group_name] = {
            "total_weight": weight,
            "weighted_centroid": [
                component / weight if weight else 0.0
                for component in entry["position"]
            ],
        }
    return result


def summarize_dominant_regions(mesh_object):
    accumulators = defaultdict(
        lambda: {
            "count": 0,
            "sum": [0.0, 0.0, 0.0],
            "minimum": [float("inf"), float("inf"), float("inf")],
            "maximum": [float("-inf"), float("-inf"), float("-inf")],
        }
    )
    for vertex in mesh_object.data.vertices:
        if not vertex.groups:
            continue
        dominant = max(vertex.groups, key=lambda membership: membership.weight)
        group_name = mesh_object.vertex_groups[dominant.group].name
        coordinate = mesh_object.matrix_world @ vertex.co
        entry = accumulators[group_name]
        entry["count"] += 1
        for axis, component in enumerate(coordinate):
            entry["sum"][axis] += component
            entry["minimum"][axis] = min(entry["minimum"][axis], component)
            entry["maximum"][axis] = max(entry["maximum"][axis], component)
    result = {}
    for group_name, entry in sorted(accumulators.items()):
        result[group_name] = {
            "vertex_count": entry["count"],
            "centroid": [
                component / entry["count"] for component in entry["sum"]
            ],
            "bounds_min": entry["minimum"],
            "bounds_max": entry["maximum"],
        }
    return result


def summarize_objects():
    return [
        {
            "name": obj.name,
            "type": obj.type,
            "parent": obj.parent.name if obj.parent else None,
            "parent_type": obj.parent_type,
            "parent_bone": obj.parent_bone,
            "world_location": list(obj.matrix_world.translation),
        }
        for obj in sorted(bpy.context.scene.objects, key=lambda item: item.name)
    ]


def inspect_asset(path):
    clear_scene()
    bpy.ops.import_scene.gltf(filepath=str(path))
    armature, skinned = find_rig_objects()
    hierarchy = {}
    for bone in sorted(armature.data.bones, key=lambda item: item.name):
        hierarchy[bone.name] = {
            "parent": bone.parent.name if bone.parent else None,
            "head_local": list(bone.head_local),
            "tail_local": list(bone.tail_local),
        }
    return {
        "path": str(path.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "mesh": skinned.name,
        "vertices": len(skinned.data.vertices),
        "polygons": len(skinned.data.polygons),
        "bones": len(armature.data.bones),
        "regions": {
            name: summarize_region(skinned, name)
            for name in ("front_panel", "upper_body", "lower_legs")
        },
        "groups": summarize_groups(skinned),
        "dominant_regions": summarize_dominant_regions(skinned),
        "bone_hierarchy": hierarchy,
        "objects": summarize_objects(),
    }


def main():
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    result = {
        "source": inspect_asset(SOURCE_GLB),
        "approved": inspect_asset(APPROVED_GLB),
    }
    OUTPUT_PATH.write_text(
        json.dumps(result, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "result": "PASS",
                "output": str(OUTPUT_PATH.relative_to(PROJECT_ROOT)).replace("\\", "/"),
                "source_front_panel_weights": result["source"]["regions"]["front_panel"][
                    "weight_totals"
                ],
                "approved_front_panel_weights": result["approved"]["regions"][
                    "front_panel"
                ]["weight_totals"],
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
