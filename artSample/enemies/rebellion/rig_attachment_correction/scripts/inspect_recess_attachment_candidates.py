import bpy
import json
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[5]
APPROVED_BLEND = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "appearance_reference_sync"
    / "blender"
    / "Rebellion_Appearance_ReferenceSync.blend"
)
OUTPUT_PATH = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "rig_attachment_correction"
    / "analysis"
    / "recess_attachment_candidates.json"
)


def main():
    bpy.ops.wm.open_mainfile(filepath=str(APPROVED_BLEND))
    armature = next(
        obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"
    )
    skinned = next(
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
        and obj.vertex_groups
        and any(modifier.type == "ARMATURE" for modifier in obj.modifiers)
    )
    source_group = skinned.vertex_groups["Bone_017"]
    candidates = []
    for vertex in skinned.data.vertices:
        coordinate = skinned.matrix_world @ vertex.co
        weights = {
            skinned.vertex_groups[membership.group].name: membership.weight
            for membership in vertex.groups
        }
        if not (
            abs(coordinate.x) <= 0.55
            and -1.35 <= coordinate.y <= -0.85
            and 1.15 <= coordinate.z <= 1.70
        ):
            continue
        if weights.get(source_group.name, 0.0) < 0.999:
            continue
        candidates.append(
            {
                "index": vertex.index,
                "position": list(coordinate),
                "weights": weights,
            }
        )

    detail_objects = sorted(
        (
            obj
            for obj in bpy.context.scene.objects
            if obj.name.startswith(
                (
                    "Rebellion_Front_",
                    "Rebellion_Gun_",
                    "Rebellion_Panel_",
                    "Rebellion_Scan_",
                )
            )
        ),
        key=lambda item: item.name,
    )
    body_bone = armature.data.bones["Bone_002"]
    result = {
        "approved_blend": str(APPROVED_BLEND.relative_to(PROJECT_ROOT)).replace(
            "\\", "/"
        ),
        "armature": armature.name,
        "skinned_mesh": skinned.name,
        "bone_002_children": sorted(child.name for child in body_bone.children),
        "bone_017_parent": armature.data.bones["Bone_017"].parent.name,
        "candidate_vertex_count": len(candidates),
        "candidate_vertices": candidates,
        "detail_object_count": len(detail_objects),
        "detail_objects": [
            {
                "name": obj.name,
                "parent": obj.parent.name if obj.parent else None,
                "parent_type": obj.parent_type,
                "parent_bone": obj.parent_bone,
            }
            for obj in detail_objects
        ],
        "all_mesh_objects": [
            {
                "name": obj.name,
                "parent": obj.parent.name if obj.parent else None,
                "parent_type": obj.parent_type,
                "parent_bone": obj.parent_bone,
                "vertices": len(obj.data.vertices),
                "polygons": len(obj.data.polygons),
            }
            for obj in sorted(
                (item for item in bpy.context.scene.objects if item.type == "MESH"),
                key=lambda item: item.name,
            )
        ],
    }
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(
        json.dumps(result, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "result": "PASS",
                "candidate_vertex_count": len(candidates),
                "detail_object_count": len(detail_objects),
                "bone_002_children": result["bone_002_children"],
                "detail_parent_bones": sorted(
                    {item["parent_bone"] for item in result["detail_objects"]}
                ),
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
