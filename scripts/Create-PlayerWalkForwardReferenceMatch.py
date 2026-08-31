from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


PROJECT_ROOT = Path.cwd()
SOURCE_PATH = PROJECT_ROOT / "player model" / "transfer standard walk.fbx"
OUTPUT_PATH = (
    PROJECT_ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Player"
    / "Animations"
    / "Player_Walk_Forward_Reference.fbx"
)

REQUIRED_BONE_SUFFIXES = (
    "Hips",
    "Spine",
    "Spine01",
    "Spine02",
    "Head",
    "LeftUpLeg",
    "LeftLeg",
    "LeftFoot",
    "LeftToeBase",
    "RightUpLeg",
    "RightLeg",
    "RightFoot",
    "RightToeBase",
    "LeftArm",
    "LeftForeArm",
    "LeftHand",
    "RightArm",
    "RightForeArm",
    "RightHand",
)
TORSO_BONE_SUFFIXES = ("Spine", "Spine01", "Spine02")
OUTPUT_ACTION_NAME = "Player_Walk_Forward_Reference"


def require_single_armature() -> bpy.types.Object:
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one armature, found {len(armatures)}")
    return armatures[0]


def require_action(armature: bpy.types.Object) -> bpy.types.Action:
    if armature.animation_data is None or armature.animation_data.action is None:
        raise RuntimeError("Imported armature has no active Action")
    return armature.animation_data.action


def bone_by_suffix(armature: bpy.types.Object, suffix: str) -> bpy.types.PoseBone:
    matches = [bone for bone in armature.pose.bones if bone.name.endswith(suffix)]
    if len(matches) != 1:
        raise RuntimeError(f"Expected one bone ending with {suffix}, found {len(matches)}")
    return matches[0]


def world_head(armature: bpy.types.Object, bone: bpy.types.PoseBone) -> Vector:
    return armature.matrix_world @ bone.head


def axis_range(values: list[Vector], axis: int) -> tuple[float, float]:
    components = [value[axis] for value in values]
    return min(components), max(components)


def inspect_source(
    armature: bpy.types.Object,
    action: bpy.types.Action,
    bones: dict[str, bpy.types.PoseBone],
) -> dict[str, object]:
    scene = bpy.context.scene
    frame_start = int(round(action.frame_range[0]))
    frame_end = int(round(action.frame_range[1]))
    samples: dict[str, list[Vector]] = {name: [] for name in bones}
    for frame in range(frame_start, frame_end + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        for name, bone in bones.items():
            samples[name].append(world_head(armature, bone).copy())

    hips = samples["Hips"]
    left_foot = samples["LeftFoot"]
    right_foot = samples["RightFoot"]
    left_up_leg = samples["LeftUpLeg"]
    right_up_leg = samples["RightUpLeg"]
    first_hip_line = right_up_leg[0] - left_up_leg[0]
    torso_lean_degrees = [
        math.degrees(math.atan2(head.x - hip.x, head.z - hip.z))
        for head, hip in zip(samples["Head"], hips)
    ]
    foot_separations = [
        abs(left.x - right.x)
        for left, right in zip(left_foot, right_foot)
    ]
    phase_frames = sorted({
        frame_start,
        frame_start + (frame_end - frame_start) // 4,
        frame_start + (frame_end - frame_start) // 2,
        frame_start + 3 * (frame_end - frame_start) // 4,
        frame_end,
    })
    phase_samples = {}
    for frame in phase_frames:
        index = frame - frame_start
        phase_samples[str(frame)] = {
            "hips": tuple(float(value) for value in hips[index]),
            "head": tuple(float(value) for value in samples["Head"][index]),
            "left_foot": tuple(float(value) for value in left_foot[index]),
            "right_foot": tuple(float(value) for value in right_foot[index]),
            "left_hand": tuple(float(value) for value in samples["LeftHand"][index]),
            "right_hand": tuple(float(value) for value in samples["RightHand"][index]),
        }
    dimensions = tuple(float(value) for value in armature.dimensions)
    report = {
        "source": str(SOURCE_PATH),
        "output": str(OUTPUT_PATH),
        "armature": armature.name,
        "action": action.name,
        "frame_start": frame_start,
        "frame_end": frame_end,
        "fps": scene.render.fps,
        "action_slots": len(action.slots),
        "bones": {name: bone.name for name, bone in bones.items()},
        "armature_dimensions": dimensions,
        "armature_matrix": [list(row) for row in armature.matrix_world],
        "first_hip_line": tuple(float(value) for value in first_hip_line),
        "mean_torso_lean_degrees": sum(torso_lean_degrees) / len(torso_lean_degrees),
        "torso_lean_range_degrees": (
            min(torso_lean_degrees),
            max(torso_lean_degrees),
        ),
        "foot_separation_range": (
            min(foot_separations),
            max(foot_separations),
        ),
        "hips_ranges": [axis_range(hips, axis) for axis in range(3)],
        "hips_forward_endpoint_delta": hips[-1].y - hips[0].y,
        "left_foot_ranges": [axis_range(left_foot, axis) for axis in range(3)],
        "right_foot_ranges": [axis_range(right_foot, axis) for axis in range(3)],
        "first_heads": {
            name: tuple(float(value) for value in values[0])
            for name, values in samples.items()
        },
        "phase_samples": phase_samples,
    }
    return report


def hierarchy_order(armature: bpy.types.Object) -> list[bpy.types.PoseBone]:
    def depth(bone: bpy.types.PoseBone) -> int:
        result = 0
        parent = bone.parent
        while parent is not None:
            result += 1
            parent = parent.parent
        return result

    return sorted(armature.pose.bones, key=lambda bone: (depth(bone), bone.name))


def capture_source_pose(
    armature: bpy.types.Object,
    action: bpy.types.Action,
    hips_name: str,
) -> tuple[int, int, dict[int, dict[str, Matrix]], dict[int, float]]:
    scene = bpy.context.scene
    frame_start = int(round(action.frame_range[0]))
    frame_end = int(round(action.frame_range[1]))
    poses: dict[int, dict[str, Matrix]] = {}
    hips_forward: dict[int, float] = {}
    for frame in range(frame_start, frame_end + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        poses[frame] = {
            bone.name: bone.matrix_basis.copy()
            for bone in armature.pose.bones
        }
        hips_forward[frame] = armature.pose.bones[hips_name].matrix.translation.y
    return frame_start, frame_end, poses, hips_forward


def rotate_bone_about_head(
    bone: bpy.types.PoseBone,
    angle_radians: float,
    axis: str = "Y",
) -> None:
    pivot = bone.head.copy()
    correction = (
        Matrix.Translation(pivot)
        @ Matrix.Rotation(angle_radians, 4, axis)
        @ Matrix.Translation(-pivot)
    )
    bone.matrix = correction @ bone.matrix


def center_torso_over_pelvis(
    armature: bpy.types.Object,
    bones: dict[str, bpy.types.PoseBone],
) -> None:
    hips = bones["Hips"]
    head = bones["Head"]
    remaining = len(TORSO_BONE_SUFFIXES)
    for suffix in TORSO_BONE_SUFFIXES:
        center_line = head.head - hips.head
        if abs(center_line.z) <= 1.0e-6:
            raise RuntimeError("Torso center line has no vertical extent")
        lean = math.atan2(center_line.x, center_line.z)
        rotate_bone_about_head(bones[suffix], -lean / remaining)
        bpy.context.view_layer.update()
        remaining -= 1


def level_pelvis(
    bones: dict[str, bpy.types.PoseBone],
) -> None:
    hip_line = bones["LeftUpLeg"].head - bones["RightUpLeg"].head
    if hip_line.x < 0.0:
        hip_line.negate()
    horizontal = math.hypot(hip_line.x, hip_line.y)
    if horizontal <= 1.0e-6:
        raise RuntimeError("Pelvis hip line has no horizontal extent")
    roll = math.atan2(hip_line.z, horizontal)
    rotate_bone_about_head(bones["Hips"], roll, "Y")


def rotate_bone_to_direction(
    bone: bpy.types.PoseBone,
    direction: Vector,
) -> None:
    current = bone.tail - bone.head
    if current.length <= 1.0e-6 or direction.length <= 1.0e-6:
        raise RuntimeError("Leg IK received a zero-length bone direction")
    rotation = current.normalized().rotation_difference(direction.normalized())
    pivot = bone.head.copy()
    bone.matrix = (
        Matrix.Translation(pivot)
        @ rotation.to_matrix().to_4x4()
        @ Matrix.Translation(-pivot)
        @ bone.matrix
    )


def solve_leg_ik(
    up_leg: bpy.types.PoseBone,
    leg: bpy.types.PoseBone,
    foot: bpy.types.PoseBone,
    target_foot_matrix: Matrix,
    pole_position: Vector,
) -> None:
    hip = up_leg.head.copy()
    target = target_foot_matrix.translation.copy()
    first_length = (leg.head - hip).length
    second_length = (target - pole_position).length
    target_vector = target - hip
    target_distance = target_vector.length
    if (first_length <= 1.0e-6 or second_length <= 1.0e-6 or target_distance <= 1.0e-6):
        raise RuntimeError("Leg IK received a degenerate leg pose")
    if target_distance >= first_length + second_length or target_distance <= abs(
        first_length - second_length
    ):
        raise RuntimeError("Leg IK target is outside the original leg reach")

    forward = target_vector / target_distance
    pole_vector = pole_position - hip
    pole_plane = pole_vector - forward * pole_vector.dot(forward)
    if pole_plane.length <= 1.0e-6:
        raise RuntimeError("Leg IK pole lies on the hip-to-foot axis")
    pole_direction = pole_plane.normalized()
    along = (
        first_length * first_length
        - second_length * second_length
        + target_distance * target_distance
    ) / (2.0 * target_distance)
    height_squared = first_length * first_length - along * along
    if height_squared <= 0.0:
        raise RuntimeError("Leg IK cannot preserve the original knee bend")
    desired_knee = (
        hip
        + forward * along
        + pole_direction * math.sqrt(height_squared)
    )

    rotate_bone_to_direction(up_leg, desired_knee - hip)
    bpy.context.view_layer.update()
    rotate_bone_to_direction(leg, target - leg.head)
    bpy.context.view_layer.update()
    foot.matrix = target_foot_matrix
    bpy.context.view_layer.update()


def stabilize_chest_facing(
    armature: bpy.types.Object,
    bones: dict[str, bpy.types.PoseBone],
) -> None:
    remaining = len(TORSO_BONE_SUFFIXES)
    for suffix in TORSO_BONE_SUFFIXES:
        shoulder_line = bones["LeftArm"].head - bones["RightArm"].head
        if shoulder_line.x < 0.0:
            shoulder_line.negate()
        horizontal = math.hypot(shoulder_line.x, shoulder_line.y)
        if horizontal <= 1.0e-6:
            raise RuntimeError("Chest shoulder line has no horizontal extent")
        yaw = math.atan2(shoulder_line.y, shoulder_line.x)
        roll = math.atan2(shoulder_line.z, horizontal)
        rotate_bone_about_head(bones[suffix], -yaw / remaining, "Z")
        bpy.context.view_layer.update()
        rotate_bone_about_head(bones[suffix], roll / remaining, "Y")
        bpy.context.view_layer.update()
        remaining -= 1


def create_reference_action(
    armature: bpy.types.Object,
    source_action: bpy.types.Action,
    bones: dict[str, bpy.types.PoseBone],
) -> bpy.types.Action:
    scene = bpy.context.scene
    hips_name = bones["Hips"].name
    frame_start, frame_end, source_pose, hips_forward = capture_source_pose(
        armature,
        source_action,
        hips_name,
    )
    forward_start = hips_forward[frame_start]
    forward_end = hips_forward[frame_end]
    accumulated_forward = forward_end - forward_start
    if abs(accumulated_forward) <= 1.0e-4:
        raise RuntimeError("Source Action has no accumulated forward travel")

    animation_data = armature.animation_data_create()
    reference_action = bpy.data.actions.new(OUTPUT_ACTION_NAME)
    reference_action.use_frame_range = True
    reference_action.frame_start = frame_start
    reference_action.frame_end = frame_end
    animation_data.action = reference_action

    ordered_bones = hierarchy_order(armature)
    for bone in ordered_bones:
        bone.rotation_mode = "QUATERNION"

    frame_span = frame_end - frame_start
    for frame in range(frame_start, frame_end + 1):
        scene.frame_set(frame)
        phase = (frame - frame_start) / frame_span
        forward_correction = -accumulated_forward * phase
        for bone in ordered_bones:
            bone.matrix_basis = source_pose[frame][bone.name]
        bpy.context.view_layer.update()

        hips = bones["Hips"]
        hips.matrix = (
            Matrix.Translation((0.0, forward_correction, 0.0))
            @ hips.matrix
        )
        bpy.context.view_layer.update()

        leg_targets = {
            side: {
                "foot_matrix": bones[f"{side}Foot"].matrix.copy(),
                "pole": bones[f"{side}Leg"].head.copy(),
            }
            for side in ("Left", "Right")
        }
        level_pelvis(bones)
        bpy.context.view_layer.update()
        for side in ("Left", "Right"):
            solve_leg_ik(
                bones[f"{side}UpLeg"],
                bones[f"{side}Leg"],
                bones[f"{side}Foot"],
                leg_targets[side]["foot_matrix"],
                leg_targets[side]["pole"],
            )
        center_torso_over_pelvis(armature, bones)
        stabilize_chest_facing(armature, bones)
        center_torso_over_pelvis(armature, bones)
        for bone in ordered_bones:
            bone.keyframe_insert(
                data_path="location",
                frame=frame,
                group=bone.name,
            )
            bone.keyframe_insert(
                data_path="rotation_quaternion",
                frame=frame,
                group=bone.name,
            )
            bone.keyframe_insert(
                data_path="scale",
                frame=frame,
                group=bone.name,
            )

    scene.frame_start = frame_start
    scene.frame_end = frame_end
    scene.render.fps = 60
    scene.frame_set(frame_start)
    bpy.context.view_layer.update()
    return reference_action


def export_reference_fbx(
    armature: bpy.types.Object,
    action: bpy.types.Action,
) -> None:
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    for obj in bpy.context.scene.objects:
        obj.select_set(obj == armature or obj.type == "MESH")
    bpy.context.view_layer.objects.active = armature
    armature.animation_data.action = action
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_PATH),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        use_space_transform=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        use_armature_deform_only=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=False,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode="AUTO",
        embed_textures=False,
    )


def inspect_export() -> dict[str, object]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(OUTPUT_PATH), use_anim=True)
    armature = require_single_armature()
    action = require_action(armature)
    bones = {
        suffix: bone_by_suffix(armature, suffix)
        for suffix in REQUIRED_BONE_SUFFIXES
    }
    report = inspect_source(armature, action, bones)
    if abs(float(report["hips_forward_endpoint_delta"])) > 1.0e-4:
        raise RuntimeError(
            "Exported Action retained accumulated forward travel: "
            + str(report["hips_forward_endpoint_delta"])
        )
    return report


def main() -> None:
    if not SOURCE_PATH.is_file():
        raise FileNotFoundError(SOURCE_PATH)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_PATH), use_anim=True)
    armature = require_single_armature()
    action = require_action(armature)
    bones = {
        suffix: bone_by_suffix(armature, suffix)
        for suffix in REQUIRED_BONE_SUFFIXES
    }
    source_report = inspect_source(armature, action, bones)
    print("PLAYER_WALK_REFERENCE_SOURCE=" + json.dumps(
        source_report,
        ensure_ascii=False,
        sort_keys=True,
    ))
    reference_action = create_reference_action(armature, action, bones)
    export_reference_fbx(armature, reference_action)
    print("PLAYER_WALK_REFERENCE_EXPORT=" + json.dumps(
        inspect_export(),
        ensure_ascii=False,
        sort_keys=True,
    ))


if __name__ == "__main__":
    main()
