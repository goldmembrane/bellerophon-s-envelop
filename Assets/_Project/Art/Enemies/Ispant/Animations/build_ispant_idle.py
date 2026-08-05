import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


ARMATURE_NAME = "Ispant_Armed_Rig"
ACTION_NAME = "Ispant_Idle"
FPS = 60
FRAME_START = 1
FRAME_END = 121
LOOP_FRAMES = FRAME_END - FRAME_START
VERTICAL_TRAVEL_METERS = 0.015
FOOT_TOLERANCE_METERS = 0.001


def arguments():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def world_head(armature, pose_bone):
    return armature.matrix_world @ pose_bone.head


def remove_existing_animation(armature):
    if armature.animation_data is not None:
        armature.animation_data_clear()
    for action in list(bpy.data.actions):
        if action.name == ACTION_NAME:
            bpy.data.actions.remove(action)
    for bone in armature.pose.bones:
        bone.location = (0.0, 0.0, 0.0)
        bone.rotation_mode = "QUATERNION"
        bone.rotation_quaternion = (1.0, 0.0, 0.0, 0.0)
        bone.scale = (1.0, 1.0, 1.0)
        for constraint in list(bone.constraints):
            bone.constraints.remove(constraint)


def create_target(name, matrix_world):
    target = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(target)
    target.empty_display_type = "PLAIN_AXES"
    target.empty_display_size = 0.08
    target.matrix_world = matrix_world.copy()
    return target


def add_leg_constraints(armature, side):
    lower_leg = armature.pose.bones[f"{side}Leg"]
    foot = armature.pose.bones[f"{side}Foot"]
    ankle_matrix = armature.matrix_world @ foot.matrix
    ankle_target = create_target(f"Ispant_{side}AnkleTarget", ankle_matrix)

    ik = lower_leg.constraints.new("IK")
    ik.name = f"Ispant_{side}FootLockIK"
    ik.target = ankle_target
    ik.chain_count = 2
    ik.use_stretch = False
    ik.influence = 1.0

    rotation_lock = foot.constraints.new("COPY_ROTATION")
    rotation_lock.name = f"Ispant_{side}FootRotationLock"
    rotation_lock.target = ankle_target
    rotation_lock.owner_space = "WORLD"
    rotation_lock.target_space = "WORLD"
    rotation_lock.influence = 1.0
    return ankle_target


def key_hips_cycle(armature):
    hips = armature.pose.bones["Hips"]
    armature.animation_data_create()
    action = bpy.data.actions.new(ACTION_NAME)
    armature.animation_data.action = action
    travel_armature_units = (
        VERTICAL_TRAVEL_METERS / armature.matrix_world.to_scale().z
    )
    baseline = hips.location.copy()
    vertical_to_bone_local = hips.bone.matrix_local.to_3x3().inverted()
    for frame in range(FRAME_START, FRAME_END + 1):
        phase = (frame - FRAME_START) / LOOP_FRAMES
        hips.location = baseline.copy()
        hips.location += vertical_to_bone_local @ Vector(
            (
                0.0,
                0.0,
                -0.5 * travel_armature_units * (1.0 - math.cos(phase * math.tau)),
            )
        )
        hips.keyframe_insert(data_path="location", frame=frame, group="Hips")
    hips.location = baseline
    return action


def bake_constraints(armature, action):
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="POSE")
    armature.animation_data.action = action
    bpy.ops.nla.bake(
        frame_start=FRAME_START,
        frame_end=FRAME_END,
        step=1,
        only_selected=False,
        visual_keying=True,
        clear_constraints=True,
        clear_parents=False,
        use_current_action=True,
        clean_curves=False,
        bake_types={"POSE"},
        channel_types={"LOCATION", "ROTATION", "SCALE"},
    )
    bpy.ops.object.mode_set(mode="OBJECT")


def inspect_motion(armature):
    tracked = {
        name: []
        for name in (
            "Hips",
            "Head",
            "LeftHand",
            "RightHand",
            "LeftFoot",
            "RightFoot",
        )
    }
    for frame in range(FRAME_START, FRAME_END + 1):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        for name in tracked:
            tracked[name].append(world_head(armature, armature.pose.bones[name]))

    vertical_travel = {
        name: max(value.z for value in samples) - min(value.z for value in samples)
        for name, samples in tracked.items()
    }
    foot_errors = {
        name: max((value - samples[0]).length for value in samples)
        for name, samples in tracked.items()
        if name.endswith("Foot")
    }
    loop_errors = {
        name: (samples[-1] - samples[0]).length
        for name, samples in tracked.items()
    }
    if abs(vertical_travel["Hips"] - VERTICAL_TRAVEL_METERS) > 0.0005:
        raise RuntimeError(
            f"Hips vertical travel differs: {vertical_travel['Hips']:.9f}"
        )
    if foot_errors["LeftFoot"] > FOOT_TOLERANCE_METERS:
        raise RuntimeError(
            f"Left foot moved: {foot_errors['LeftFoot']:.9f}"
        )
    if foot_errors["RightFoot"] > FOOT_TOLERANCE_METERS:
        raise RuntimeError(
            f"Right foot moved: {foot_errors['RightFoot']:.9f}"
        )
    if max(loop_errors.values()) > 0.00002:
        raise RuntimeError(f"Loop boundary differs: {max(loop_errors.values()):.9f}")
    minimum_follow_travel = VERTICAL_TRAVEL_METERS * 0.8
    if (
        vertical_travel["LeftHand"] < minimum_follow_travel
        or vertical_travel["RightHand"] < minimum_follow_travel
    ):
        raise RuntimeError("The arms do not follow the torso through the idle cycle.")

    return {
        "duration_seconds": LOOP_FRAMES / FPS,
        "frame_start": FRAME_START,
        "frame_end": FRAME_END,
        "fps": FPS,
        "vertical_travel_meters": vertical_travel,
        "foot_position_error_meters": foot_errors,
        "maximum_loop_error_meters": max(loop_errors.values()),
        "root_object_translation": list(armature.location),
    }


def export_animation(armature, output_path):
    output = Path(output_path)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    exported_meshes = [
        obj
        for obj in bpy.data.objects
        if obj.type == "MESH"
        and (obj.find_armature() == armature or obj.parent == armature)
    ]
    if len(exported_meshes) != 3:
        raise RuntimeError(
            f"Expected the approved body, crescent, and eye meshes, found {len(exported_meshes)}"
        )
    for mesh in exported_meshes:
        mesh.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.context.scene.frame_start = FRAME_START
    bpy.context.scene.frame_end = FRAME_END
    bpy.context.scene.render.fps = FPS
    bpy.ops.export_scene.fbx(
        filepath=str(output),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        axis_forward="-Z",
        axis_up="Y",
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=False,
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        use_armature_deform_only=False,
        armature_nodetype="NULL",
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode="AUTO",
        embed_textures=False,
        use_metadata=True,
    )
    if not output.exists():
        raise RuntimeError(f"Animation FBX was not created: {output}")


def main():
    args = arguments()
    armature = bpy.data.objects.get(ARMATURE_NAME)
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError(f"Required armature is missing: {ARMATURE_NAME}")
    if len(armature.data.bones) != 24:
        raise RuntimeError(f"Expected 24 bones, found {len(armature.data.bones)}")

    bpy.context.scene.render.fps = FPS
    bpy.context.scene.frame_start = FRAME_START
    bpy.context.scene.frame_end = FRAME_END
    remove_existing_animation(armature)
    bpy.context.view_layer.update()
    targets = [
        add_leg_constraints(armature, "Left"),
        add_leg_constraints(armature, "Right"),
    ]
    action = key_hips_cycle(armature)
    bake_constraints(armature, action)
    for target in targets:
        bpy.data.objects.remove(target, do_unlink=True)
    metrics = inspect_motion(armature)
    export_animation(armature, args.output)
    metrics["output"] = str(Path(args.output).resolve())
    metrics["action"] = action.name
    metrics["bones"] = len(armature.data.bones)
    print("ISPANT_IDLE_BUILD=" + json.dumps(metrics, sort_keys=True))


if __name__ == "__main__":
    main()
