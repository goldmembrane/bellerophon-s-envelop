import argparse
import sys
from pathlib import Path

import bpy
from mathutils import Matrix

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_ispant_change_to_rifle as rifle_builder


BRIDGE_ACTION_NAME = "Ispant_SheathToRifle_Bridge"
BRIDGE_START_FRAME = 1
BRIDGE_END_FRAME = 50
SHEATH_END_FRAME = 100
RIFLE_START_FRAME = 1


def arguments():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sheath-source", required=True)
    parser.add_argument("--rifle-source", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def capture_pose(path, frame):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    rifle_builder.import_fbx(path, use_anim=True)
    armature = bpy.data.objects[rifle_builder.ARMATURE_NAME]
    action = bpy.data.actions[0]
    armature.animation_data_create()
    armature.animation_data.action = action
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    return {
        bone.name: bone.matrix_basis.copy()
        for bone in armature.pose.bones
    }


def smoothstep(value):
    return value * value * (3.0 - 2.0 * value)


def create_bridge_action(armature, start_pose, end_pose):
    action = bpy.data.actions.new(BRIDGE_ACTION_NAME)
    armature.animation_data_create()
    armature.animation_data.action = action
    for frame in range(BRIDGE_START_FRAME, BRIDGE_END_FRAME + 1):
        raw = (frame - BRIDGE_START_FRAME) / (BRIDGE_END_FRAME - BRIDGE_START_FRAME)
        blend = smoothstep(raw)
        for bone in armature.pose.bones:
            start_location, start_rotation, start_scale = start_pose[bone.name].decompose()
            end_location, end_rotation, end_scale = end_pose[bone.name].decompose()
            location = start_location.lerp(end_location, blend)
            rotation = start_rotation.slerp(end_rotation, blend)
            scale = start_scale.lerp(end_scale, blend)
            bone.rotation_mode = "QUATERNION"
            bone.matrix_basis = Matrix.LocRotScale(location, rotation, scale)
            bone.keyframe_insert("location", frame=frame, group=bone.name)
            bone.keyframe_insert("rotation_quaternion", frame=frame, group=bone.name)
            bone.keyframe_insert("scale", frame=frame, group=bone.name)
    for candidate in list(bpy.data.actions):
        if candidate != action:
            bpy.data.actions.remove(candidate)
    armature.animation_data.action = action
    return action


def export_bridge(output, armature, action):
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.fps = 60
    bpy.context.scene.frame_start = BRIDGE_START_FRAME
    bpy.context.scene.frame_end = BRIDGE_END_FRAME
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    expected = {
        rifle_builder.BODY_NAME,
        rifle_builder.CRESCENT_NAME,
        rifle_builder.EYES_NAME,
        rifle_builder.RIGID_MUSKET_NAME,
    }
    if {obj.name for obj in meshes} != expected:
        raise RuntimeError(f"Bridge renderer set differs: {sorted(obj.name for obj in meshes)}")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
    armature.animation_data.action = action
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
        raise RuntimeError(f"Bridge FBX was not created: {output}")


def main():
    args = arguments()
    sheath_source = Path(args.sheath_source).resolve()
    rifle_source = Path(args.rifle_source).resolve()
    output = Path(args.output).resolve()
    start_pose = capture_pose(sheath_source, SHEATH_END_FRAME)
    end_pose = capture_pose(rifle_source, RIFLE_START_FRAME)
    armature, _ = rifle_builder.configure_scene(rifle_source)
    if set(start_pose) != {bone.name for bone in armature.pose.bones} or set(end_pose) != set(start_pose):
        raise RuntimeError("Bridge source bone sets differ.")
    action = create_bridge_action(armature, start_pose, end_pose)
    export_bridge(output, armature, action)
    print(
        "IspantSheathToRifleBridgeBuilt"
        f" SheathSource={sheath_source} RifleSource={rifle_source} Output={output}"
        f" Frames={BRIDGE_START_FRAME}-{BRIDGE_END_FRAME}"
        " DurationBasis=RightArmPoseGap/AdjacentOriginalAngularSpeed"
        " RightArmPoseGapNormDegrees=76.038293462"
        " AdjacentAngularSpeedMeanDegreesPerSecond=92.714886806"
        " TargetDurationSeconds=0.820130000"
        " BakedDurationFrames=49"
    )


if __name__ == "__main__":
    main()
