import hashlib
import json
from pathlib import Path

import bpy


PROJECT_ROOT = Path(__file__).resolve().parents[3]
SOURCE_FBX = PROJECT_ROOT / "enemies model" / "con spirito.fbx"
OUTPUT_FBX = PROJECT_ROOT / "Assets" / "_Project" / "Art" / "Enemies" / "ConSpirito" / "Models" / "con_spirito_rerigged.fbx"
REPORT_PATH = PROJECT_ROOT / "docs" / "validation" / "con_spirito" / "con_spirito_rerig_report.json"

LEG_CHAINS = {
    "frontleg": ["frontleg", "frontleg0", "frontleg1", "frontleg2"],
    "R_frontleg": ["R_frontleg", "R_frontleg0", "R_frontleg1", "R_frontleg2"],
    "backleg": ["backleg", "backleg0", "backleg1", "backleg2"],
    "R_backleg": ["R_backleg", "R_backleg0", "R_backleg1", "R_backleg2"],
}

REMOVED_CHILD_LEG_BONES = [name for chain_names in LEG_CHAINS.values() for name in chain_names[1:]]


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def import_fbx(path):
    bpy.ops.import_scene.fbx(filepath=str(path))


def mesh_objects():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def armature_objects():
    return [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]


def mesh_coordinate_hash(mesh_obj):
    digest = hashlib.sha256()
    for vertex in mesh_obj.data.vertices:
        co = vertex.co
        digest.update(f"{co.x:.9f},{co.y:.9f},{co.z:.9f};".encode("ascii"))
    return digest.hexdigest()


def mesh_data_bounds(mesh_obj):
    vertices = mesh_obj.data.vertices
    if not vertices:
        return []

    mins = [vertices[0].co.x, vertices[0].co.y, vertices[0].co.z]
    maxs = [vertices[0].co.x, vertices[0].co.y, vertices[0].co.z]
    for vertex in vertices[1:]:
        co = vertex.co
        mins[0] = min(mins[0], co.x)
        mins[1] = min(mins[1], co.y)
        mins[2] = min(mins[2], co.z)
        maxs[0] = max(maxs[0], co.x)
        maxs[1] = max(maxs[1], co.y)
        maxs[2] = max(maxs[2], co.z)

    return [[round(value, 9) for value in mins], [round(value, 9) for value in maxs]]


def mesh_summary(mesh_obj):
    return {
        "name": mesh_obj.name,
        "vertex_count": len(mesh_obj.data.vertices),
        "polygon_count": len(mesh_obj.data.polygons),
        "uv_layers": [layer.name for layer in mesh_obj.data.uv_layers],
        "material_slots": [slot.material.name if slot.material else "" for slot in mesh_obj.material_slots],
        "vertex_groups": [group.name for group in mesh_obj.vertex_groups],
        "data_bounds": mesh_data_bounds(mesh_obj),
        "object_bound_box": [[round(value, 9) for value in corner] for corner in mesh_obj.bound_box],
        "coordinate_hash": mesh_coordinate_hash(mesh_obj),
    }


def armature_summary(armature_obj):
    return {
        "name": armature_obj.name,
        "bones": [bone.name for bone in armature_obj.data.bones],
    }


def action_summary():
    summaries = []
    for action in bpy.data.actions:
        frame_start, frame_end = action.frame_range
        curve_count = 0
        animated_bones = []
        for layer in action.layers:
            for strip in layer.strips:
                for channelbag in strip.channelbags:
                    curve_count += len(channelbag.fcurves)
                    for fcurve in channelbag.fcurves:
                        bone_name = extract_bone_name_from_data_path(fcurve.data_path)
                        if bone_name and bone_name not in animated_bones:
                            animated_bones.append(bone_name)

        summaries.append(
            {
                "name": action.name,
                "frame_start": int(frame_start),
                "frame_end": int(frame_end),
                "curve_count": curve_count,
                "animated_bones": animated_bones,
            }
        )

    return summaries


def extract_bone_name_from_data_path(data_path):
    prefix = 'pose.bones["'
    if prefix not in data_path:
        return None

    return data_path.split(prefix, 1)[1].split('"]', 1)[0]


def group_weight(vertex, group_index):
    for group in vertex.groups:
        if group.group == group_index:
            return group.weight
    return 0.0


def collapse_leg_vertex_groups(mesh_obj):
    changes = {}
    for destination_name, chain_names in LEG_CHAINS.items():
        destination_group = mesh_obj.vertex_groups.get(destination_name)
        if destination_group is None:
            destination_group = mesh_obj.vertex_groups.new(name=destination_name)

        source_groups = [
            mesh_obj.vertex_groups.get(source_name)
            for source_name in chain_names
            if mesh_obj.vertex_groups.get(source_name) is not None
        ]
        source_indices = [group.index for group in source_groups]
        assigned_vertices = 0

        for vertex in mesh_obj.data.vertices:
            total_weight = sum(group_weight(vertex, index) for index in source_indices)
            if total_weight > 0.0:
                destination_group.add([vertex.index], min(total_weight, 1.0), "REPLACE")
                assigned_vertices += 1

        removed_groups = []
        for source_name in chain_names[1:]:
            group = mesh_obj.vertex_groups.get(source_name)
            if group is not None:
                mesh_obj.vertex_groups.remove(group)
                removed_groups.append(source_name)

        changes[destination_name] = {
            "merged_from": chain_names,
            "assigned_vertices": assigned_vertices,
            "removed_child_groups": removed_groups,
        }

    return changes


def rerig_armature(armature_obj):
    removed_bones = []
    leg_tail_targets = {}

    bpy.context.view_layer.objects.active = armature_obj
    armature_obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    edit_bones = armature_obj.data.edit_bones

    for destination_name, chain_names in LEG_CHAINS.items():
        destination_bone = edit_bones.get(destination_name)
        distal_bone = edit_bones.get(chain_names[-1])
        if destination_bone is not None and distal_bone is not None:
            leg_tail_targets[destination_name] = [distal_bone.tail.x, distal_bone.tail.y, distal_bone.tail.z]

    for destination_name, tail in leg_tail_targets.items():
        destination_bone = edit_bones.get(destination_name)
        if destination_bone is not None:
            destination_bone.tail = tail

    for chain_names in LEG_CHAINS.values():
        for child_name in reversed(chain_names[1:]):
            child_bone = edit_bones.get(child_name)
            if child_bone is not None:
                edit_bones.remove(child_bone)
                removed_bones.append(child_name)

    bpy.ops.object.mode_set(mode="OBJECT")

    remaining_leg_bones = [
        bone.name
        for bone in armature_obj.data.bones
        if bone.name in LEG_CHAINS or any(bone.name in chain[1:] for chain in LEG_CHAINS.values())
    ]

    return {
        "removed_child_bones": removed_bones,
        "remaining_leg_bones": remaining_leg_bones,
        "leg_tail_targets": {name: [round(value, 9) for value in tail] for name, tail in leg_tail_targets.items()},
    }


def prune_removed_child_bone_animation_curves():
    pruned = {}
    for action in bpy.data.actions:
        removed_paths = []
        for layer in action.layers:
            for strip in layer.strips:
                for channelbag in strip.channelbags:
                    for fcurve in list(channelbag.fcurves):
                        bone_name = extract_bone_name_from_data_path(fcurve.data_path)
                        if bone_name in REMOVED_CHILD_LEG_BONES:
                            removed_paths.append(f"{fcurve.data_path}[{fcurve.array_index}]")
                            channelbag.fcurves.remove(fcurve)

        if removed_paths:
            pruned[action.name] = removed_paths

    return pruned


def configure_scene_animation_range():
    if not bpy.data.actions:
        return None

    action = bpy.data.actions[0]
    frame_start, frame_end = action.frame_range
    bpy.context.scene.frame_start = int(frame_start)
    bpy.context.scene.frame_end = int(frame_end)

    armatures = armature_objects()
    if armatures:
        armature = armatures[0]
        if armature.animation_data is None:
            armature.animation_data_create()
        armature.animation_data.action = action

    return {
        "action": action.name,
        "frame_start": int(frame_start),
        "frame_end": int(frame_end),
    }


def export_fbx(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=False,
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode="AUTO",
    )


def collect_scene_summary():
    meshes = mesh_objects()
    armatures = armature_objects()
    return {
        "meshes": [mesh_summary(mesh) for mesh in meshes],
        "armatures": [armature_summary(armature) for armature in armatures],
        "actions": action_summary(),
    }


def main():
    if not SOURCE_FBX.exists():
        raise FileNotFoundError(SOURCE_FBX)

    reset_scene()
    import_fbx(SOURCE_FBX)

    meshes = mesh_objects()
    armatures = armature_objects()
    if not meshes:
        raise RuntimeError("No mesh objects were imported from the source FBX.")
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one armature, found {len(armatures)}.")

    before_summary = collect_scene_summary()
    vertex_group_changes = {mesh.name: collapse_leg_vertex_groups(mesh) for mesh in meshes}
    rig_changes = rerig_armature(armatures[0])
    pruned_animation_curves = prune_removed_child_bone_animation_curves()
    animation_export_range = configure_scene_animation_range()
    after_summary = collect_scene_summary()

    geometry_unchanged = True
    geometry_mismatches = {}
    before_meshes = {mesh["name"]: mesh for mesh in before_summary["meshes"]}
    for mesh in after_summary["meshes"]:
        before_mesh = before_meshes.get(mesh["name"])
        if before_mesh is None:
            geometry_unchanged = False
            geometry_mismatches[mesh["name"]] = {"missing_before": True}
            continue
        mismatches = {}
        for key in ["vertex_count", "polygon_count", "data_bounds", "coordinate_hash", "uv_layers", "material_slots"]:
            if before_mesh[key] != mesh[key]:
                mismatches[key] = {
                    "before": before_mesh[key],
                    "after": mesh[key],
                }
        if before_mesh["object_bound_box"] != mesh["object_bound_box"]:
            mismatches["object_bound_box_note"] = {
                "before": before_mesh["object_bound_box"],
                "after": mesh["object_bound_box"],
                "note": "Non-blocking armature-deformed preview bounds; mesh data coordinates are checked separately.",
            }

        blocking_mismatches = {key: value for key, value in mismatches.items() if key != "object_bound_box_note"}
        if blocking_mismatches:
            geometry_unchanged = False
            geometry_mismatches[mesh["name"]] = blocking_mismatches

    if not geometry_unchanged:
        REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
        REPORT_PATH.write_text(
            json.dumps(
                {
                    "source_fbx": str(SOURCE_FBX),
                    "operation": "rig_only_leg_chain_collapse_failed_before_export",
                    "model_form_changed": "unknown",
                    "geometry_mismatches": geometry_mismatches,
                    "before": before_summary,
                    "after_rig_edit": after_summary,
                    "vertex_group_changes": vertex_group_changes,
                    "rig_changes": rig_changes,
                    "pruned_animation_curves": pruned_animation_curves,
                    "animation_export_range": animation_export_range,
                },
                indent=2,
            ),
            encoding="utf-8",
        )
        raise RuntimeError("Mesh geometry/material/UV data changed during rig-only operation.")

    export_fbx(OUTPUT_FBX)

    reset_scene()
    import_fbx(OUTPUT_FBX)
    exported_summary = collect_scene_summary()

    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    report = {
        "source_fbx": str(SOURCE_FBX),
        "output_fbx": str(OUTPUT_FBX),
        "operation": "rig_only_leg_chain_collapse",
        "model_form_changed": False,
        "geometry_unchanged_before_export": geometry_unchanged,
        "geometry_mismatches": geometry_mismatches,
        "leg_interpretation": "Each original leg chain keeps one representative bone; mesh vertices/faces/UV/materials stay unchanged.",
        "vertex_group_changes": vertex_group_changes,
        "rig_changes": rig_changes,
        "pruned_animation_curves": pruned_animation_curves,
        "animation_export_range": animation_export_range,
        "before": before_summary,
        "after_rig_edit": after_summary,
        "after_export_import": exported_summary,
    }
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
