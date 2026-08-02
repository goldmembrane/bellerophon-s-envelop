import hashlib
import json
import struct
from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[1]
WALKING_FBX = ROOT / "enemies model" / "KUŠkursa walking.fbx"
STATIC_FBX = (
    ROOT
    / "Assets/_Project/Art/Enemies/Kursa/ApprovedAppearance/Models"
    / "Kursa_Appearance_RuntimeProjection.fbx"
)
REPORT = (
    ROOT
    / "docs/validation/kursa_move_animation_2026-08-02"
    / "Kursa_Walking_Source_Inspection.json"
)


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def matrix_values(matrix):
    return [value for row in matrix for value in row]


def float_digest(values):
    digest = hashlib.sha256()
    for value in values:
        digest.update(struct.pack("<d", float(value)))
    return digest.hexdigest().upper()


def mesh_signatures(mesh_object):
    mesh = mesh_object.data
    return {
        "vertices": float_digest(
            coordinate
            for vertex in mesh.vertices
            for coordinate in vertex.co
        ),
        "polygon_indices": hashlib.sha256(
            json.dumps(
                [list(polygon.vertices) for polygon in mesh.polygons],
                separators=(",", ":"),
            ).encode("utf-8")
        ).hexdigest().upper(),
        "uv0": float_digest(
            coordinate
            for item in (mesh.uv_layers[0].data if mesh.uv_layers else [])
            for coordinate in item.uv
        ),
    }


def animation_metrics(armature, action):
    animation_data = armature.animation_data_create()
    for track in animation_data.nla_tracks:
        track.mute = True
    animation_data.action = action
    start = int(round(action.frame_range[0]))
    end = int(round(action.frame_range[1]))
    bones = list(armature.pose.bones)
    arm_names = (
        "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
        "RightShoulder", "RightArm", "RightForeArm", "RightHand",
    )
    arm_baseline = {}
    root_positions = []
    maximum_scale_error = 0.0
    arm_rotation_ranges = {name: 0.0 for name in arm_names}
    for frame in range(start, end + 1):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        if frame == start:
            arm_baseline = {
                name: armature.pose.bones[name].matrix_basis.to_quaternion().copy()
                for name in arm_names
            }
        hips = armature.pose.bones["Hips"]
        root_positions.append(list(armature.matrix_world @ hips.head))
        for bone in bones:
            scale = bone.matrix_basis.to_scale()
            maximum_scale_error = max(
                maximum_scale_error,
                abs(scale.x - 1.0),
                abs(scale.y - 1.0),
                abs(scale.z - 1.0),
            )
        for name in arm_names:
            rotation = armature.pose.bones[name].matrix_basis.to_quaternion()
            arm_rotation_ranges[name] = max(
                arm_rotation_ranges[name],
                arm_baseline[name].rotation_difference(rotation).angle,
            )
    ranges = []
    for axis in range(3):
        values = [position[axis] for position in root_positions]
        ranges.append(max(values) - min(values))
    bpy.context.scene.frame_set(start)
    bpy.context.view_layer.update()
    first = {bone.name: bone.matrix_basis.copy() for bone in bones}
    bpy.context.scene.frame_set(end)
    bpy.context.view_layer.update()
    loop_error = max(
        max(abs(a - b) for a, b in zip(matrix_values(first[bone.name]), matrix_values(bone.matrix_basis)))
        for bone in bones
    )
    return {
        "frame_range": [start, end],
        "hips_world_axis_ranges": ranges,
        "maximum_bone_scale_error": maximum_scale_error,
        "arm_rotation_ranges_radians": arm_rotation_ranges,
        "loop_matrix_component_error": loop_error,
    }


def inspect_fbx(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path), use_anim=True)
    armatures = [item for item in bpy.context.scene.objects if item.type == "ARMATURE"]
    meshes = [item for item in bpy.context.scene.objects if item.type == "MESH"]
    actions = []
    for action in bpy.data.actions:
        actions.append(
            {
                "name": action.name,
                "frame_range": list(action.frame_range),
                "users": action.users,
            }
        )
    result = {
        "path": str(path.relative_to(ROOT)).replace("\\", "/"),
        "sha256": sha256(path),
        "scene_fps": bpy.context.scene.render.fps,
        "objects": [
            {
                "name": item.name,
                "type": item.type,
                "parent": item.parent.name if item.parent else None,
                "scale": list(item.scale),
            }
            for item in bpy.context.scene.objects
        ],
        "armatures": [
            {
                "name": armature.name,
                "bones": [bone.name for bone in armature.data.bones],
                "bone_rest_matrices": {
                    bone.name: matrix_values(bone.matrix_local)
                    for bone in armature.data.bones
                },
                "active_action": (
                    armature.animation_data.action.name
                    if armature.animation_data and armature.animation_data.action
                    else None
                ),
                "nla_strips": [
                    {
                        "track": track.name,
                        "strip": strip.name,
                        "action": strip.action.name if strip.action else None,
                        "frame_start": strip.frame_start,
                        "frame_end": strip.frame_end,
                    }
                    for track in (
                        armature.animation_data.nla_tracks
                        if armature.animation_data else []
                    )
                    for strip in track.strips
                ],
            }
            for armature in armatures
        ],
        "meshes": [
            {
                "name": mesh.name,
                "vertices": len(mesh.data.vertices),
                "polygons": len(mesh.data.polygons),
                "materials": [
                    material.name if material else None
                    for material in mesh.data.materials
                ],
                "uv_layers": [layer.name for layer in mesh.data.uv_layers],
                "vertex_groups": [group.name for group in mesh.vertex_groups],
                "armature_modifiers": [
                    modifier.object.name if modifier.object else None
                    for modifier in mesh.modifiers
                    if modifier.type == "ARMATURE"
                ],
                "signatures": mesh_signatures(mesh),
            }
            for mesh in meshes
        ],
        "actions": actions,
    }
    mixamo = [action for action in bpy.data.actions if "mixamo" in action.name.lower()]
    if len(armatures) == 1 and len(mixamo) == 1:
        result["mixamo_animation_metrics"] = animation_metrics(armatures[0], mixamo[0])
    return result


def main():
    if not WALKING_FBX.exists() or not STATIC_FBX.exists():
        raise RuntimeError("A required Kursa FBX is missing.")
    report = {
        "result": "PASS",
        "walking": inspect_fbx(WALKING_FBX),
        "approved_static": inspect_fbx(STATIC_FBX),
    }
    walking_actions = report["walking"]["actions"]
    mixamo_actions = [
        action for action in walking_actions if "mixamo" in action["name"].lower()
    ]
    report["walking"]["mixamo_action_count"] = len(mixamo_actions)
    report["walking"]["mixamo_actions"] = mixamo_actions
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
