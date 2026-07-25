"""Generate Ostinato's looping head-and-torso hit recoil animation.

The approved character mesh and armature are reused without geometry or
material changes.  The animation keeps the object root and legs fixed, drives
the spine chain backward, exaggerates the neck/head response, and returns to
the exact rest pose before the 0.70 second loop boundary.
"""

from pathlib import Path
import math

import bpy


REPOSITORY_ROOT = Path(__file__).resolve().parents[6]
SOURCE_BLEND = (
    REPOSITORY_ROOT
    / "artSample/enemies/ostinato/blender/Ostinato_CurrentModel_TexturedSample.blend"
)
OUTPUT_DIRECTORY = REPOSITORY_ROOT / "Assets/_Project/Art/Enemies/Ostinato/Animations"
OUTPUT_BLEND = OUTPUT_DIRECTORY / "Ostinato_05_Hit_Recoil_Source.blend"
OUTPUT_FBX = OUTPUT_DIRECTORY / "Ostinato_05_Hit_Recoil.fbx"

ARMATURE_NAME = "Ostinato_CurrentModel_Armature"
MESH_NAME = "Ostinato_CurrentModel_TexturedSample"
ACTION_NAME = "Ostinato_05_Hit_Recoil"
FRAME_RATE = 60
FIRST_FRAME = 1
LAST_FRAME = 43

# Frame 12 is 0.183 seconds after frame 1.  Frame 31 is exactly 0.50 seconds
# after frame 1, so the remaining frames hold the approved rest pose.
POSE_KEYS = (
    (1, 0.0),
    (4, 0.35),
    (12, 1.0),
    (18, 0.62),
    (25, 0.20),
    (31, 0.0),
    (43, 0.0),
)

# The character faces Blender -Y.  Negative local X bends this mostly vertical
# chain toward +Y, which is backward for the character.  The cumulative torso
# recoil is 27 degrees; neck and head add another 32 degrees so the head motion
# remains visibly larger than the body flinch.
MAXIMUM_LOCAL_X_DEGREES = {
    "Spine02": -7.0,
    "Spine01": -9.0,
    "Spine": -11.0,
    "neck": -18.0,
    "Head": -14.0,
}


def open_approved_character():
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    armature = bpy.data.objects[ARMATURE_NAME]
    mesh = bpy.data.objects[MESH_NAME]

    # The approved Unity FBX exposes this rig root as `Armature`.  Animation
    # bindings must use the same path so the clip resolves on the unchanged
    # approved scene model rather than requiring a replacement mesh instance.
    armature.name = "Armature"
    armature.data.name = "Armature"

    for obj in list(bpy.data.objects):
        if obj not in {armature, mesh}:
            bpy.data.objects.remove(obj, do_unlink=True)

    mesh.parent = armature
    while len(mesh.data.uv_layers) > 1:
        mesh.data.uv_layers.remove(mesh.data.uv_layers[-1])
    if mesh.data.uv_layers:
        mesh.data.uv_layers[0].name = "OstinatoSampleUV"

    armature.animation_data_clear()
    for pose_bone in armature.pose.bones:
        pose_bone.matrix_basis.identity()

    scene = bpy.context.scene
    scene.render.fps = FRAME_RATE
    scene.render.fps_base = 1.0
    scene.frame_start = FIRST_FRAME
    scene.frame_end = LAST_FRAME
    scene.frame_set(FIRST_FRAME)
    return armature, mesh


def create_hit_action(armature):
    action = bpy.data.actions.new(ACTION_NAME)
    action.use_frame_range = True
    action.frame_start = FIRST_FRAME
    action.frame_end = LAST_FRAME
    armature.animation_data_create()
    armature.animation_data.action = action

    for frame, weight in POSE_KEYS:
        for bone_name, maximum_degrees in MAXIMUM_LOCAL_X_DEGREES.items():
            pose_bone = armature.pose.bones[bone_name]
            pose_bone.rotation_mode = "XYZ"
            pose_bone.rotation_euler = (
                math.radians(maximum_degrees * weight),
                0.0,
                0.0,
            )
            pose_bone.keyframe_insert(
                data_path="rotation_euler",
                frame=frame,
                group=bone_name,
            )

    armature["BellerophonAnimationContract"] = (
        "OstinatoHitRecoil.v1;duration=0.70;fps=60;peak=0.183333;return=0.50;"
        "rootMotion=false;loop=true"
    )
    return action


def assert_animation_contract(armature, action):
    if action.name != ACTION_NAME:
        raise RuntimeError(f"Unexpected action name: {action.name}")
    if int(action.frame_start) != FIRST_FRAME or int(action.frame_end) != LAST_FRAME:
        raise RuntimeError(
            f"Unexpected action frame range: {action.frame_start}..{action.frame_end}"
        )
    if armature.location.length > 1e-5 or any(abs(value) > 1e-5 for value in armature.rotation_euler):
        raise RuntimeError("The animated armature object root is not fixed.")

    scene = bpy.context.scene
    for neutral_frame in (FIRST_FRAME, 31, LAST_FRAME):
        scene.frame_set(neutral_frame)
        bpy.context.view_layer.update()
        for bone_name in MAXIMUM_LOCAL_X_DEGREES:
            pose_bone = armature.pose.bones[bone_name]
            if abs(math.degrees(pose_bone.rotation_euler.x)) > 0.001:
                raise RuntimeError(
                    f"{bone_name} did not return to rest at frame {neutral_frame}."
                )

    scene.frame_set(12)
    bpy.context.view_layer.update()
    peak_values = {
        bone_name: math.degrees(armature.pose.bones[bone_name].rotation_euler.x)
        for bone_name in MAXIMUM_LOCAL_X_DEGREES
    }
    for bone_name, expected in MAXIMUM_LOCAL_X_DEGREES.items():
        if abs(peak_values[bone_name] - expected) > 0.01:
            raise RuntimeError(
                f"{bone_name} peak rotation {peak_values[bone_name]} != {expected}."
            )

    print(
        "HIT_RECOIL_CONTRACT "
        f"Action={action.name} Frames={FIRST_FRAME}..{LAST_FRAME} "
        "Duration=0.70 PeakFrame=12 ReturnFrame=31 "
        f"PeakRotations={peak_values}"
    )


def save_and_export(armature, mesh):
    OUTPUT_DIRECTORY.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND))

    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = mesh
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_FBX),
        use_selection=True,
        add_leaf_bones=False,
        path_mode="RELATIVE",
        use_armature_deform_only=True,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
    )
    print(f"Saved source blend: {OUTPUT_BLEND}")
    print(f"Exported hit recoil FBX: {OUTPUT_FBX}")


def main():
    armature, mesh = open_approved_character()
    action = create_hit_action(armature)
    assert_animation_contract(armature, action)
    save_and_export(armature, mesh)


if __name__ == "__main__":
    main()
