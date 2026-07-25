import bpy
import json
from collections import Counter, defaultdict
from pathlib import Path
from mathutils import Vector


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
    / "front_weight_regions.json"
)
LEG_BONES = tuple(
    f"Bone_{index:03d}"
    for start in (9, 14, 19, 24)
    for index in range(start, start + 5)
)


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


def bounds(positions):
    if not positions:
        return None
    minimum = positions[0].copy()
    maximum = positions[0].copy()
    for position in positions[1:]:
        for axis in range(3):
            minimum[axis] = min(minimum[axis], position[axis])
            maximum[axis] = max(maximum[axis], position[axis])
    return {
        "min": [round(value, 6) for value in minimum],
        "max": [round(value, 6) for value in maximum],
    }


def analyze_region(mesh_object, name, predicate):
    selected = []
    for vertex in mesh_object.data.vertices:
        world = mesh_object.matrix_world @ vertex.co
        if predicate(world):
            selected.append((vertex, world))

    bone_weight_sums = Counter()
    influenced_positions = defaultdict(list)
    strongly_influenced_positions = defaultdict(list)
    sole_influenced_positions = defaultdict(list)
    vertex_records = []
    for vertex, world in selected:
        memberships = []
        for membership in vertex.groups:
            bone_name = mesh_object.vertex_groups[membership.group].name
            memberships.append((bone_name, membership.weight))
            bone_weight_sums[bone_name] += membership.weight
            if bone_name in LEG_BONES and membership.weight > 0.000001:
                influenced_positions[bone_name].append(world)
            if bone_name in LEG_BONES and membership.weight >= 0.5:
                strongly_influenced_positions[bone_name].append(world)
        if len(memberships) == 1 and memberships[0][0] in LEG_BONES:
            sole_influenced_positions[memberships[0][0]].append(world)
        leg_memberships = [
            [bone_name, round(weight, 6)]
            for bone_name, weight in memberships
            if bone_name in LEG_BONES and weight > 0.000001
        ]
        if leg_memberships:
            vertex_records.append(
                {
                    "index": vertex.index,
                    "position": [round(value, 6) for value in world],
                    "leg_weights": leg_memberships,
                }
            )

    def per_bone_records(mapping):
        return {
            bone_name: {
                "count": len(positions),
                "bounds": bounds(positions),
            }
            for bone_name, positions in mapping.items()
        }

    return {
        "name": name,
        "vertex_count": len(selected),
        "leg_influenced_vertex_count": len(vertex_records),
        "bone_weight_sums": [
            [bone_name, round(weight, 6)]
            for bone_name, weight in bone_weight_sums.most_common()
        ],
        "leg_influenced": per_bone_records(influenced_positions),
        "leg_weight_at_least_half": per_bone_records(strongly_influenced_positions),
        "sole_leg_weight": per_bone_records(sole_influenced_positions),
        "vertices_with_leg_weights": vertex_records,
    }


def main():
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    mesh_object = find_skinned_mesh()
    regions = [
        analyze_region(
            mesh_object,
            "exact_recess_correction_volume",
            lambda position: (
                abs(position.x) <= 0.55
                and -1.35 <= position.y <= -0.85
                and 1.15 <= position.z <= 1.70
            ),
        ),
        analyze_region(
            mesh_object,
            "front_recess_and_weapon",
            lambda position: (
                abs(position.x) <= 0.8
                and -1.4 <= position.y <= -0.5
                and 1.0 <= position.z <= 1.8
            ),
        ),
        analyze_region(
            mesh_object,
            "central_disc",
            lambda position: (
                abs(position.x) <= 0.9
                and abs(position.y) <= 1.3
                and 1.25 <= position.z <= 1.8
            ),
        ),
        analyze_region(
            mesh_object,
            "entire_upper_disc",
            lambda position: position.z >= 1.25,
        ),
    ]
    output = {
        "source": str(SOURCE_BLEND.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "mesh": mesh_object.name,
        "regions": regions,
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
                "regions": [
                    {
                        "name": region["name"],
                        "vertex_count": region["vertex_count"],
                        "leg_influenced_vertex_count": region[
                            "leg_influenced_vertex_count"
                        ],
                        "leg_weight_at_least_half": region[
                            "leg_weight_at_least_half"
                        ],
                        "sole_leg_weight": region["sole_leg_weight"],
                    }
                    for region in regions
                ],
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
