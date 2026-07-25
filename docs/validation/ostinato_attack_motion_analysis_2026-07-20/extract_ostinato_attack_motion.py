import bpy
import csv
import json
import math
import os
import sys


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


args = sys.argv[sys.argv.index("--") + 1:]
require(len(args) == 3, "Expected arguments: <fbx_path> <csv_path> <json_path>")
fbx_path, csv_path, json_path = map(os.path.abspath, args)
os.makedirs(os.path.dirname(csv_path), exist_ok=True)
os.makedirs(os.path.dirname(json_path), exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx_path, use_anim=True)

armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
require(len(armatures) == 1, f"Expected one armature, found {len(armatures)}")
armature = armatures[0]

actions = list(bpy.data.actions)
attack_actions = [action for action in actions if "mixamo.com" in action.name.lower()]
require(len(attack_actions) == 1, f"Expected one mixamo.com action, found {[action.name for action in actions]}")
attack_action = attack_actions[0]

if armature.animation_data is None:
    armature.animation_data_create()
armature.animation_data.action = attack_action

source_first = int(round(attack_action.frame_range[0]))
source_last = int(round(attack_action.frame_range[1]))
require(source_first == 1 and source_last == 160, f"Unexpected attack range: {source_first}..{source_last}")

scene = bpy.context.scene
scene.render.fps = 60
scene.frame_start = source_first
scene.frame_end = source_last

bone_names = sorted(bone.name for bone in armature.pose.bones)
rows = []
frame_activity = []
previous = {}
bone_stats = {
    name: {
        "head_path": 0.0,
        "tail_path": 0.0,
        "rotation_path_degrees": 0.0,
        "max_step_activity": 0.0,
        "max_step_unity_frame": 0,
    }
    for name in bone_names
}

for source_frame in range(source_first, source_last + 1):
    unity_frame = source_frame - source_first
    scene.frame_set(source_frame)
    bpy.context.view_layer.update()
    total_activity = 0.0
    moving_bones = []

    for bone_name in bone_names:
        bone = armature.pose.bones[bone_name]
        head = armature.matrix_world @ bone.head
        tail = armature.matrix_world @ bone.tail
        rotation = (armature.matrix_world.to_quaternion() @ bone.matrix.to_quaternion()).normalized()
        euler = rotation.to_euler("XYZ")
        length = max((tail - head).length, 0.000001)

        head_step = 0.0
        tail_step = 0.0
        rotation_step_degrees = 0.0
        weighted_activity = 0.0
        if bone_name in previous:
            old_head, old_tail, old_rotation = previous[bone_name]
            head_step = (head - old_head).length
            tail_step = (tail - old_tail).length
            rotation_step_radians = old_rotation.rotation_difference(rotation).angle
            rotation_step_radians = min(rotation_step_radians, (2.0 * math.pi) - rotation_step_radians)
            rotation_step_degrees = math.degrees(rotation_step_radians)
            weighted_activity = ((head_step + tail_step) * 0.5) + (rotation_step_radians * length)
            total_activity += weighted_activity
            if weighted_activity > 0.00001:
                moving_bones.append((bone_name, weighted_activity))

            stats = bone_stats[bone_name]
            stats["head_path"] += head_step
            stats["tail_path"] += tail_step
            stats["rotation_path_degrees"] += rotation_step_degrees
            if weighted_activity > stats["max_step_activity"]:
                stats["max_step_activity"] = weighted_activity
                stats["max_step_unity_frame"] = unity_frame

        previous[bone_name] = (head.copy(), tail.copy(), rotation.copy())
        rows.append({
            "unity_frame": unity_frame,
            "source_frame": source_frame,
            "time_seconds": unity_frame / 60.0,
            "bone": bone_name,
            "head_x": head.x,
            "head_y": head.y,
            "head_z": head.z,
            "tail_x": tail.x,
            "tail_y": tail.y,
            "tail_z": tail.z,
            "rotation_w": rotation.w,
            "rotation_x": rotation.x,
            "rotation_y": rotation.y,
            "rotation_z": rotation.z,
            "euler_x_degrees": math.degrees(euler.x),
            "euler_y_degrees": math.degrees(euler.y),
            "euler_z_degrees": math.degrees(euler.z),
            "head_step": head_step,
            "tail_step": tail_step,
            "rotation_step_degrees": rotation_step_degrees,
            "weighted_activity": weighted_activity,
        })

    moving_bones.sort(key=lambda item: item[1], reverse=True)
    frame_activity.append({
        "unity_frame": unity_frame,
        "source_frame": source_frame,
        "time_seconds": unity_frame / 60.0,
        "activity": total_activity,
        "top_bones": [name for name, _ in moving_bones[:6]],
    })

fieldnames = list(rows[0].keys())
with open(csv_path, "w", newline="", encoding="utf-8") as handle:
    writer = csv.DictWriter(handle, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(rows)

ranked_bones = sorted(
    (
        {
            "bone": name,
            **stats,
            "combined_activity":
                ((stats["head_path"] + stats["tail_path"]) * 0.5) +
                math.radians(stats["rotation_path_degrees"]) * max(armature.pose.bones[name].length, 0.000001),
        }
        for name, stats in bone_stats.items()
    ),
    key=lambda item: item["combined_activity"],
    reverse=True,
)

ranked_frames = sorted(frame_activity, key=lambda item: item["activity"], reverse=True)
summary = {
    "fbx_path": fbx_path,
    "armature": armature.name,
    "actions": [
        {
            "name": action.name,
            "frame_start": action.frame_range[0],
            "frame_end": action.frame_range[1],
        }
        for action in actions
    ],
    "selected_action": attack_action.name,
    "source_frame_start": source_first,
    "source_frame_end": source_last,
    "unity_frame_start": 0,
    "unity_frame_end": source_last - source_first,
    "fps": 60,
    "duration_seconds": (source_last - source_first) / 60.0,
    "bones": bone_names,
    "frame_activity": frame_activity,
    "highest_activity_frames": ranked_frames[:30],
    "bone_activity_ranking": ranked_bones,
}
with open(json_path, "w", encoding="utf-8") as handle:
    json.dump(summary, handle, ensure_ascii=False, indent=2)

print(json.dumps({
    "selected_action": attack_action.name,
    "frames": len(frame_activity),
    "bones": len(bone_names),
    "csv": csv_path,
    "json": json_path,
}, ensure_ascii=False))
