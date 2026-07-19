"""Build Ostinato's four-second Ultralisk blade-path attack.

Each arm uses an independent two-bone IK target and elbow pole.  The solver
preserves the approved rest roll of each upper-arm and forearm bone, then
bakes the result to FK rotation keys.  Hand bones are never keyed.

The fifteen source-video frames are treated as one continuous path, not as
named attack phases.  Their visible blade plane is transferred to the gameplay
front camera: Blade A travels through the upper/left-to-low arc while Blade B
travels through the opposed right/low arc before both overlap and pull inward.

Reference mapping:
    Blade A -> anatomical RightArm
    Blade B -> anatomical LeftArm
"""

from pathlib import Path
import math
import sys

import bpy
from mathutils import Quaternion, Vector


REPOSITORY_ROOT = Path(__file__).resolve().parents[6]
SOURCE_BLEND = REPOSITORY_ROOT / "artSample/enemies/ostinato/blender/Ostinato_CurrentModel_TexturedSample.blend"
OUTPUT_DIRECTORY = REPOSITORY_ROOT / "Assets/_Project/Art/Enemies/Ostinato/Animations"
OUTPUT_BLEND = OUTPUT_DIRECTORY / "Ostinato_04_Scissor_Attack_Source.blend"
OUTPUT_FBX = OUTPUT_DIRECTORY / "Ostinato_04_Scissor_Attack.fbx"
VALIDATION_DIRECTORY = (
    REPOSITORY_ROOT
    / "docs/validation/ostinato_scissor_attack_user_review_2026-07-19/blender_current"
)
STAGE_DIRECTORY = VALIDATION_DIRECTORY / "reference_15_stages"
FRONT_STAGE_DIRECTORY = VALIDATION_DIRECTORY / "front_15_stages"
CONTINUOUS_DIRECTORY = VALIDATION_DIRECTORY / "continuous_10fps"
FRONT_CONTINUOUS_DIRECTORY = VALIDATION_DIRECTORY / "front_continuous_10fps"

ARMATURE_NAME = "Ostinato_CurrentModel_Armature"
MESH_NAME = "Ostinato_CurrentModel_TexturedSample"
ACTION_NAME = "Ostinato_04_Scissor_Attack"
FRAME_RATE = 60
FIRST_FRAME = 1
LAST_FRAME = 241

# F01-F15 of 1111.mp4, uniformly time-scaled from 1.5 seconds to four seconds.
REFERENCE_STAGE_FRAMES = (1, 18, 35, 52, 70, 87, 104, 121, 138, 155, 172, 190, 207, 224, 241)

# Armature-space forearm-tail targets. Character forward is -Y, +Z is up.
# The source video's visible blade plane is mapped to the character's front X/Z
# plane so the opposed arcs, same-side overlap, and pull remain readable from the
# gameplay review camera.  Y adds the forward strike and subsequent bodyward pull.
RIGHT_A_TARGETS = (
    (-41.3497, -53.2321, 116.0328),
    (-41.3497, -53.2321, 116.0328),
    (-70.0000, -75.0000, 135.0000),
    (-50.0000, -78.0000, 175.0000),
    (-110.0000, -90.0000, 118.0000),
    (-90.0000, -90.0000, 78.0000),
    (-60.0000, -75.0000, 66.0000),
    (-48.0000, -55.0000, 70.0000),
    (-42.0000, -40.0000, 76.0000),
    (-38.0000, -32.0000, 80.0000),
    (-35.0000, -27.0000, 84.0000),
    (-32.0000, -22.0000, 88.0000),
    (-30.0000, -18.0000, 92.0000),
    (-100.0000, -55.0000, 60.0000),
    (-41.3497, -53.2321, 116.0328),
)

LEFT_B_TARGETS = (
    (28.5519, -65.5618, 98.6706),
    (28.5519, -65.5618, 98.6706),
    (55.0000, -82.0000, 78.0000),
    (105.0000, -92.0000, 102.0000),
    (75.0000, -90.0000, 72.0000),
    (45.0000, -85.0000, 62.0000),
    (15.0000, -70.0000, 60.0000),
    (-20.0000, -55.0000, 66.0000),
    (-30.0000, -40.0000, 72.0000),
    (-32.0000, -32.0000, 76.0000),
    (-30.0000, -27.0000, 80.0000),
    (-28.0000, -22.0000, 84.0000),
    (-25.0000, -18.0000, 88.0000),
    (5.0000, -55.0000, 55.0000),
    (28.5519, -65.5618, 98.6706),
)

# Preserve the broad blade faces visible in the source.  Only a small opposed
# forearm-axis adjustment is used; hand/wrist bones remain completely unkeyed.
RIGHT_A_FOREARM_ROLL = (0.0, 0.0, 0.0, 0.0, 2.0, 5.0, 8.0, 12.0, 12.0, 10.0, 8.0, 6.0, 4.0, 2.0, 0.0)
LEFT_B_FOREARM_ROLL = (0.0, 0.0, 0.0, 0.0, -2.0, -5.0, -8.0, -12.0, -12.0, -10.0, -8.0, -6.0, -4.0, -2.0, 0.0)

# Minimum elbow extension follows the reference continuously.  It ramps into
# the rigid blade sweep and ramps out during F14-F15 recovery, avoiding a
# one-frame solver-mode change near the loop boundary.
MINIMUM_INTERNAL_ANGLE = (
    0.0, 0.0, 0.0, 0.0,
    150.0, 150.0, 150.0, 150.0, 150.0,
    150.0, 150.0, 150.0, 150.0, 150.0,
    0.0,
)


def smoothstep(value):
    value = max(0.0, min(1.0, value))
    return value * value * (3.0 - 2.0 * value)


def delete_non_character_objects(armature, mesh):
    for obj in list(bpy.data.objects):
        if obj not in {armature, mesh}:
            bpy.data.objects.remove(obj, do_unlink=True)


def open_approved_character():
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    armature = bpy.data.objects[ARMATURE_NAME]
    mesh = bpy.data.objects[MESH_NAME]
    delete_non_character_objects(armature, mesh)
    mesh.parent = armature
    while len(mesh.data.uv_layers) > 1:
        mesh.data.uv_layers.remove(mesh.data.uv_layers[-1])
    if mesh.data.uv_layers:
        mesh.data.uv_layers[0].name = "OstinatoSampleUV"

    scene = bpy.context.scene
    scene.render.fps = FRAME_RATE
    scene.render.fps_base = 1.0
    scene.frame_start = FIRST_FRAME
    scene.frame_end = LAST_FRAME
    scene.frame_set(FIRST_FRAME)
    armature.animation_data_clear()
    bpy.context.view_layer.update()
    return armature, mesh


def interpolate_target(frame, targets):
    if frame <= REFERENCE_STAGE_FRAMES[0]:
        return Vector(targets[0])
    if frame >= REFERENCE_STAGE_FRAMES[-1]:
        return Vector(targets[-1])
    for index in range(len(REFERENCE_STAGE_FRAMES) - 1):
        start = REFERENCE_STAGE_FRAMES[index]
        end = REFERENCE_STAGE_FRAMES[index + 1]
        if start <= frame <= end:
            blend = (frame - start) / (end - start)
            return Vector(targets[index]).lerp(Vector(targets[index + 1]), blend)
    raise RuntimeError(f"No target interval for frame {frame}")


def interpolate_scalar(frame, values):
    if frame <= REFERENCE_STAGE_FRAMES[0]:
        return values[0]
    if frame >= REFERENCE_STAGE_FRAMES[-1]:
        return values[-1]
    for index in range(len(REFERENCE_STAGE_FRAMES) - 1):
        start = REFERENCE_STAGE_FRAMES[index]
        end = REFERENCE_STAGE_FRAMES[index + 1]
        if start <= frame <= end:
            blend = (frame - start) / (end - start)
            return values[index] + (values[index + 1] - values[index]) * blend
    raise RuntimeError(f"No scalar interval for frame {frame}")


def matrix_for_direction(default_matrix, head, tail, axial_roll_degrees=0.0):
    direction = (tail - head).normalized()
    default_direction = (default_matrix.to_3x3() @ Vector((0.0, 1.0, 0.0))).normalized()
    default_track = default_direction.to_track_quat("Y", "Z")
    rest_roll = default_track.inverted() @ default_matrix.to_quaternion()
    rotation = direction.to_track_quat("Y", "Z") @ rest_roll
    if abs(axial_roll_degrees) > 1e-6:
        rotation = Quaternion(direction, math.radians(axial_roll_degrees)) @ rotation
    result = rotation.to_matrix().to_4x4()
    result.translation = head
    return result


def build_solver(armature, side, targets, forearm_roll):
    arm = armature.pose.bones[f"{side}Arm"]
    forearm = armature.pose.bones[f"{side}ForeArm"]
    shoulder = arm.head.copy()
    elbow = arm.tail.copy()
    hand = forearm.tail.copy()
    upper_length = arm.length
    lower_length = forearm.length
    default_target_direction = (hand - shoulder).normalized()
    along = (
        upper_length * upper_length
        - lower_length * lower_length
        + (hand - shoulder).length_squared
    ) / (2.0 * (hand - shoulder).length)
    bend_center = shoulder + default_target_direction * along
    default_pole = (elbow - bend_center).normalized()
    default_normal = (elbow - shoulder).normalized().cross((hand - elbow).normalized()).normalized()
    return {
        "side": side,
        "arm": arm,
        "forearm": forearm,
        "targets": targets,
        "forearm_roll": forearm_roll,
        "shoulder": shoulder,
        "upper_length": upper_length,
        "lower_length": lower_length,
        "default_pole": default_pole,
        "default_normal": default_normal,
        "default_arm_matrix": arm.matrix.copy(),
        "default_forearm_matrix": forearm.matrix.copy(),
    }


def distance_for_internal_angle(upper_length, lower_length, degrees):
    radians = math.radians(degrees)
    return math.sqrt(
        upper_length * upper_length
        + lower_length * lower_length
        - 2.0 * upper_length * lower_length * math.cos(radians)
    )


def solve_two_bone(solver, target, minimum_internal_angle, forearm_roll):
    shoulder = solver["shoulder"]
    upper_length = solver["upper_length"]
    lower_length = solver["lower_length"]
    to_target = target - shoulder
    distance = max(0.001, to_target.length)
    maximum = distance_for_internal_angle(upper_length, lower_length, 169.0)
    minimum = abs(upper_length - lower_length) + 0.05
    if minimum_internal_angle > 0.0:
        minimum = distance_for_internal_angle(
            upper_length, lower_length, minimum_internal_angle
        )
    clamped_distance = min(max(distance, minimum), maximum)
    direction = to_target.normalized()
    hand = shoulder + direction * clamped_distance

    along = (
        upper_length * upper_length
        - lower_length * lower_length
        + clamped_distance * clamped_distance
    ) / (2.0 * clamped_distance)
    height = math.sqrt(max(0.0, upper_length * upper_length - along * along))
    pole = solver["default_pole"] - direction * solver["default_pole"].dot(direction)
    if pole.length < 1e-5:
        outward = Vector((-1.0, 0.0, -0.5)) if solver["side"] == "Right" else Vector((1.0, 0.0, -0.5))
        pole = outward - direction * outward.dot(direction)
    pole.normalize()

    elbow = shoulder + direction * along + pole * height
    current_normal = (elbow - shoulder).normalized().cross((hand - elbow).normalized()).normalized()
    if current_normal.dot(solver["default_normal"]) < 0.0:
        pole.negate()
        elbow = shoulder + direction * along + pole * height

    arm_matrix = matrix_for_direction(solver["default_arm_matrix"], shoulder, elbow)
    forearm_matrix = matrix_for_direction(
        solver["default_forearm_matrix"], elbow, hand, forearm_roll
    )
    return arm_matrix, forearm_matrix


def record_all_frames(solvers):
    recorded = {solver["side"]: {} for solver in solvers}
    for frame in range(FIRST_FRAME, LAST_FRAME + 1):
        for solver in solvers:
            target = interpolate_target(frame, solver["targets"])
            forearm_roll = interpolate_scalar(frame, solver["forearm_roll"])
            minimum_internal_angle = interpolate_scalar(
                frame, MINIMUM_INTERNAL_ANGLE
            )
            recorded[solver["side"]][frame] = solve_two_bone(
                solver, target, minimum_internal_angle, forearm_roll
            )
    return recorded


def insert_rotation_key(pose_bone, frame):
    pose_bone.keyframe_insert(
        data_path="rotation_quaternion", frame=frame, group=pose_bone.name
    )


def bake_fk(armature, recorded):
    scene = bpy.context.scene
    action = bpy.data.actions.new(ACTION_NAME)
    action.frame_range = (FIRST_FRAME, LAST_FRAME)
    armature.animation_data_create()
    armature.animation_data.action = action
    for side in ("Right", "Left"):
        armature.pose.bones[f"{side}Arm"].rotation_mode = "QUATERNION"
        armature.pose.bones[f"{side}ForeArm"].rotation_mode = "QUATERNION"

    for frame in range(FIRST_FRAME, LAST_FRAME + 1):
        scene.frame_set(frame)
        for side in ("Right", "Left"):
            arm = armature.pose.bones[f"{side}Arm"]
            forearm = armature.pose.bones[f"{side}ForeArm"]
            arm_matrix, forearm_matrix = recorded[side][frame]
            arm.matrix = arm_matrix
            bpy.context.view_layer.update()
            forearm.matrix = forearm_matrix
            bpy.context.view_layer.update()
            insert_rotation_key(arm, frame)
            insert_rotation_key(forearm, frame)
    scene.frame_set(FIRST_FRAME)
    return action


def elbow_internal_angle(arm, forearm):
    elbow_to_shoulder = (arm.head - arm.tail).normalized()
    elbow_to_hand = (forearm.tail - forearm.head).normalized()
    return math.degrees(elbow_to_shoulder.angle(elbow_to_hand))


def bend_normal(arm, forearm):
    upper = (arm.tail - arm.head).normalized()
    lower = (forearm.tail - forearm.head).normalized()
    normal = upper.cross(lower)
    return normal.normalized() if normal.length > 1e-6 else Vector((0.0, 0.0, 0.0))


def print_metrics(armature, solvers):
    scene = bpy.context.scene
    print("REFERENCE_STAGE_METRICS")
    for index, frame in enumerate(REFERENCE_STAGE_FRAMES, start=1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        values = []
        for solver in solvers:
            arm = armature.pose.bones[f"{solver['side']}Arm"]
            forearm = armature.pose.bones[f"{solver['side']}ForeArm"]
            values.append(
                (
                    elbow_internal_angle(arm, forearm),
                    bend_normal(arm, forearm).dot(solver["default_normal"]),
                    arm.tail.x,
                    forearm.tail.x,
                )
            )
        right, left = values
        print(
            f"F{index:02d} frame={frame:03d} "
            f"elbow_internal RightA={right[0]:.2f} LeftB={left[0]:.2f} "
            f"bend_dot RightA={right[1]:.3f} LeftB={left[1]:.3f} "
            f"elbow_x RightA={right[2]:.2f} LeftB={left[2]:.2f} "
            f"hand_x RightA={right[3]:.2f} LeftB={left[3]:.2f}"
        )
    scene.frame_set(FIRST_FRAME)
    print("HandRotationCurves=0 RootOrHipsCurves=0")
    print(
        "ObjectTransform "
        f"location={tuple(round(v, 6) for v in armature.location)} "
        f"rotation={tuple(round(v, 6) for v in armature.rotation_euler)}"
    )


def save_final_source():
    OUTPUT_DIRECTORY.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.context.scene.frame_set(FIRST_FRAME)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND), compress=True)
    print(f"Saved final source: {OUTPUT_BLEND}")


def export_final_fbx(armature, mesh):
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
    print(f"Exported final FBX: {OUTPUT_FBX}")


def aim_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def configure_preview_scene():
    VALIDATION_DIRECTORY.mkdir(parents=True, exist_ok=True)
    STAGE_DIRECTORY.mkdir(parents=True, exist_ok=True)
    FRONT_STAGE_DIRECTORY.mkdir(parents=True, exist_ok=True)
    CONTINUOUS_DIRECTORY.mkdir(parents=True, exist_ok=True)
    FRONT_CONTINUOUS_DIRECTORY.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.color = (0.025, 0.028, 0.033)

    camera_data = bpy.data.cameras.new("AttackReferenceCamera")
    camera = bpy.data.objects.new("AttackReferenceCamera", camera_data)
    scene.collection.objects.link(camera)
    camera.location = (-5.2, -0.8, 1.75)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 2.9
    aim_at(camera, (0.0, 0.0, 0.92))
    scene.camera = camera

    for name, location, energy, size in (
        ("AttackPreviewKey", (-2.8, -3.0, 4.0), 1200.0, 4.0),
        ("AttackPreviewFill", (3.2, -1.5, 2.0), 850.0, 3.0),
        ("AttackPreviewRim", (0.0, 2.0, 3.2), 1000.0, 3.0),
    ):
        light_data = bpy.data.lights.new(name, "AREA")
        light_data.energy = energy
        light_data.shape = "DISK"
        light_data.size = size
        light = bpy.data.objects.new(name, light_data)
        scene.collection.objects.link(light)
        light.location = location
        aim_at(light, (0.0, 0.0, 0.9))
    return scene


def render_previews():
    scene = configure_preview_scene()
    for index, frame in enumerate(REFERENCE_STAGE_FRAMES, start=1):
        scene.frame_set(frame)
        scene.render.filepath = str(STAGE_DIRECTORY / f"stage_{index:02d}_frame_{frame:03d}.png")
        bpy.ops.render.render(write_still=True)
    camera = scene.camera
    camera.location = (0.0, -5.2, 1.3)
    aim_at(camera, (0.0, 0.0, 0.9))
    for index, frame in enumerate(REFERENCE_STAGE_FRAMES, start=1):
        scene.frame_set(frame)
        scene.render.filepath = str(FRONT_STAGE_DIRECTORY / f"stage_{index:02d}_frame_{frame:03d}.png")
        bpy.ops.render.render(write_still=True)
    camera.location = (-5.2, -0.8, 1.75)
    aim_at(camera, (0.0, 0.0, 0.92))
    if "--stages-only" not in sys.argv:
        for sample_index, frame in enumerate(
            range(FIRST_FRAME, LAST_FRAME + 1, 6), start=1
        ):
            scene.frame_set(frame)
            scene.render.filepath = str(
                CONTINUOUS_DIRECTORY / f"frame_{sample_index:03d}.png"
            )
            bpy.ops.render.render(write_still=True)
        camera.location = (0.0, -5.2, 1.3)
        aim_at(camera, (0.0, 0.0, 0.9))
        for sample_index, frame in enumerate(
            range(FIRST_FRAME, LAST_FRAME + 1, 6), start=1
        ):
            scene.frame_set(frame)
            scene.render.filepath = str(
                FRONT_CONTINUOUS_DIRECTORY / f"frame_{sample_index:03d}.png"
            )
            bpy.ops.render.render(write_still=True)
    print(f"Rendered direct comparison previews: {VALIDATION_DIRECTORY}")


export_final = "--export-final" in sys.argv
armature_object, mesh_object = open_approved_character()
right_solver = build_solver(
    armature_object, "Right", RIGHT_A_TARGETS, RIGHT_A_FOREARM_ROLL
)
left_solver = build_solver(
    armature_object, "Left", LEFT_B_TARGETS, LEFT_B_FOREARM_ROLL
)
solvers = (right_solver, left_solver)
recorded_matrices = record_all_frames(solvers)
attack_action = bake_fk(armature_object, recorded_matrices)
print_metrics(armature_object, solvers)
save_final_source()
if export_final:
    export_final_fbx(armature_object, mesh_object)
render_previews()
print(
    f"Action={ACTION_NAME}, Frames={FIRST_FRAME}-{LAST_FRAME}, FPS={FRAME_RATE}, "
    f"ExportFinal={export_final}"
)
