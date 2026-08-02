import hashlib
import json
from math import cos, pi
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


ROOT = Path(__file__).resolve().parents[1]
SOURCE_FBX = (
    ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Enemies"
    / "Kursa"
    / "ApprovedAppearance"
    / "Models"
    / "Kursa_Appearance_RuntimeProjection.fbx"
)
OUTPUT_FBX = (
    ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Enemies"
    / "Kursa"
    / "Animations"
    / "Kursa_02_GroundedIdle.fbx"
)
REPORT = (
    ROOT
    / "docs"
    / "validation"
    / "kursa_idle_animation_2026-08-02"
    / "Kursa_02_GroundedIdle_Blender.json"
)

EXPECTED_SOURCE_SHA256 = (
    "D9F30E87DE6C8D2438D8A8C56D7CD1E394E8F3E6CD15248A1F69CFB8F62472E9"
)
ACTION_NAME = "Kursa_02_GroundedIdle"
FPS = 60
LOOP_SECONDS = 2.0
END_FRAME = int(FPS * LOOP_SECONDS)
# Generate the source-space travel that becomes exactly 0.03 Unity units
# after the approved CargoRunMvp Kursa placement scale is applied.
KURSA_PLACEMENT_SCALE = 0.5495786
TARGET_UNITY_VERTICAL_TRAVEL = 0.03
VERTICAL_TRAVEL_SAMPLE = (
    TARGET_UNITY_VERTICAL_TRAVEL / KURSA_PLACEMENT_SCALE * 100.0
)
FOOT_HEAD_TOLERANCE_WORLD = 0.0001
GROUND_TOLERANCE_WORLD = 0.001
KEY_TIMES = (0.0, 0.5, 1.0, 1.5, 2.0)
ANIMATED_BONES = (
    "Hips",
    "LeftUpLeg",
    "LeftLeg",
    "LeftFoot",
    "RightUpLeg",
    "RightLeg",
    "RightFoot",
)


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def rigid_rotation(pivot, rotation):
    return (
        Matrix.Translation(pivot)
        @ rotation.to_matrix().to_4x4()
        @ Matrix.Translation(-pivot)
    )


def downward_offset_sample(time_seconds):
    return (
        VERTICAL_TRAVEL_SAMPLE
        * 0.5
        * (1.0 - cos(2.0 * pi * time_seconds / LOOP_SECONDS))
    )


def reset_pose(armature_obj, base_basis):
    for pose_bone in armature_obj.pose.bones:
        pose_bone.matrix_basis = base_basis[pose_bone.name].copy()
    bpy.context.view_layer.update()


def solve_leg(armature_obj, side, target_foot_matrix):
    upper = armature_obj.pose.bones[f"{side}UpLeg"]
    lower = armature_obj.pose.bones[f"{side}Leg"]
    foot = armature_obj.pose.bones[f"{side}Foot"]

    upper_matrix = upper.matrix.copy()
    lower_matrix = lower.matrix.copy()
    root = upper.head.copy()
    knee = lower.head.copy()
    ankle = foot.head.copy()
    target_ankle = target_foot_matrix.translation.copy()
    upper_length = (knee - root).length
    lower_length = (ankle - knee).length
    target_vector = target_ankle - root
    target_distance = target_vector.length
    minimum_reach = abs(upper_length - lower_length)
    maximum_reach = upper_length + lower_length
    if not minimum_reach < target_distance < maximum_reach:
        raise RuntimeError(
            f"{side} leg target is outside its unscaled reach: "
            f"distance={target_distance}, range=({minimum_reach},{maximum_reach})."
        )

    target_direction = target_vector.normalized()
    along = (
        upper_length * upper_length
        - lower_length * lower_length
        + target_distance * target_distance
    ) / (2.0 * target_distance)
    height = max(0.0, upper_length * upper_length - along * along) ** 0.5
    knee_offset = knee - (
        root + target_direction * (knee - root).dot(target_direction)
    )
    if knee_offset.length < 1e-6:
        raise RuntimeError(f"{side} knee plane is degenerate.")
    target_knee = (
        root
        + target_direction * along
        + knee_offset.normalized() * height
    )

    upper_rotation = (knee - root).rotation_difference(target_knee - root)
    upper_transform = rigid_rotation(root, upper_rotation)
    upper_matrix = upper_transform @ upper_matrix
    lower_matrix = upper_transform @ lower_matrix
    transformed_knee = upper_transform @ knee
    transformed_ankle = upper_transform @ ankle
    lower_rotation = (
        transformed_ankle - transformed_knee
    ).rotation_difference(target_ankle - target_knee)
    lower_transform = rigid_rotation(target_knee, lower_rotation)
    lower_matrix = lower_transform @ lower_matrix

    upper.matrix = upper_matrix
    bpy.context.view_layer.update()
    lower.matrix = lower_matrix
    bpy.context.view_layer.update()
    foot.matrix = target_foot_matrix
    bpy.context.view_layer.update()

    error = max(
        (lower.head - target_knee).length,
        (foot.head - target_ankle).length,
    )
    return {
        "connection_error_sample": error,
        "target_distance_sample": target_distance,
        "minimum_reach_sample": minimum_reach,
        "maximum_reach_sample": maximum_reach,
    }


def key_pose(armature_obj, frame):
    for bone_name in ANIMATED_BONES:
        pose_bone = armature_obj.pose.bones[bone_name]
        pose_bone.rotation_mode = "QUATERNION"
        pose_bone.keyframe_insert(
            data_path="location",
            frame=frame,
            group=bone_name,
        )
        pose_bone.keyframe_insert(
            data_path="rotation_quaternion",
            frame=frame,
            group=bone_name,
        )


def evaluated_mesh_minimum_world_z(mesh_obj):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_obj = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_obj.to_mesh(
        preserve_all_data_layers=True,
        depsgraph=depsgraph,
    )
    try:
        return min(
            (mesh_obj.matrix_world @ vertex.co).z
            for vertex in evaluated_mesh.vertices
        )
    finally:
        evaluated_obj.to_mesh_clear()


def inspect_action(scene, armature_obj, mesh_obj, foot_targets):
    samples = []
    hips_heights = []
    ground_heights = []
    maximum_foot_error = 0.0
    for time_seconds in KEY_TIMES:
        frame = int(round(time_seconds * FPS))
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        hips_world = armature_obj.matrix_world @ armature_obj.pose.bones["Hips"].head
        hips_heights.append(hips_world.z)
        foot_errors = {}
        for side in ("Left", "Right"):
            foot_world = (
                armature_obj.matrix_world
                @ armature_obj.pose.bones[f"{side}Foot"].head
            )
            target_world = armature_obj.matrix_world @ foot_targets[side].translation
            error = (foot_world - target_world).length
            foot_errors[side] = error
            maximum_foot_error = max(maximum_foot_error, error)
        ground_height = evaluated_mesh_minimum_world_z(mesh_obj)
        ground_heights.append(ground_height)
        samples.append(
            {
                "time": time_seconds,
                "frame": frame,
                "expected_downward_offset_world": (
                    downward_offset_sample(time_seconds)
                    * armature_obj.matrix_world.to_scale().z
                ),
                "hips_world_z": hips_world.z,
                "left_foot_error_world": foot_errors["Left"],
                "right_foot_error_world": foot_errors["Right"],
                "mesh_minimum_world_z": ground_height,
            }
        )

    hips_travel = max(hips_heights) - min(hips_heights)
    ground_variation = max(ground_heights) - min(ground_heights)
    loop_hips_error = abs(hips_heights[0] - hips_heights[-1])
    projected_hips_travel = hips_travel * KURSA_PLACEMENT_SCALE
    projected_ground_variation = ground_variation * KURSA_PLACEMENT_SCALE
    if abs(projected_hips_travel - TARGET_UNITY_VERTICAL_TRAVEL) > 0.0001:
        raise RuntimeError(
            "Kursa idle projected hips travel differs from 0.03 Unity units: "
            f"source={hips_travel}, projected={projected_hips_travel}."
        )
    if maximum_foot_error > FOOT_HEAD_TOLERANCE_WORLD:
        raise RuntimeError(
            f"Kursa idle foot heads are not fixed: {maximum_foot_error}."
        )
    if projected_ground_variation > GROUND_TOLERANCE_WORLD:
        raise RuntimeError(
            "Kursa idle projected ground contact varies too much: "
            f"source={ground_variation}, projected={projected_ground_variation}."
        )
    if loop_hips_error > 1e-6:
        raise RuntimeError(
            f"Kursa idle loop boundary differs at the hips: {loop_hips_error}."
        )
    return {
        "samples": samples,
        "hips_vertical_travel_world": hips_travel,
        "hips_vertical_travel_unity_projected": projected_hips_travel,
        "maximum_foot_head_error_world": maximum_foot_error,
        "mesh_ground_variation_world": ground_variation,
        "mesh_ground_variation_unity_projected": projected_ground_variation,
        "loop_hips_error_world": loop_hips_error,
    }


def main():
    if not SOURCE_FBX.exists():
        raise RuntimeError(f"Kursa runtime source FBX is missing: {SOURCE_FBX}")
    source_hash = sha256(SOURCE_FBX)
    if source_hash != EXPECTED_SOURCE_SHA256:
        raise RuntimeError(
            "The approved Kursa runtime source hash changed: "
            f"actual={source_hash}, expected={EXPECTED_SOURCE_SHA256}."
        )

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX), use_anim=False)

    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.render.fps_base = 1.0
    scene.frame_start = 0
    scene.frame_end = END_FRAME
    armatures = [obj for obj in scene.objects if obj.type == "ARMATURE"]
    meshes = [obj for obj in scene.objects if obj.type == "MESH"]
    if len(armatures) != 1 or len(meshes) != 1:
        raise RuntimeError(
            f"Expected one armature and one mesh, found {len(armatures)} and {len(meshes)}."
        )
    armature_obj = armatures[0]
    mesh_obj = meshes[0]
    missing_bones = sorted(
        set(ANIMATED_BONES) - {bone.name for bone in armature_obj.pose.bones}
    )
    if missing_bones:
        raise RuntimeError(f"Kursa idle bones are missing: {missing_bones}")

    armature_obj.animation_data_clear()
    for pose_bone in armature_obj.pose.bones:
        pose_bone.matrix_basis.identity()
    bpy.context.view_layer.update()
    base_basis = {
        bone.name: bone.matrix_basis.copy()
        for bone in armature_obj.pose.bones
    }
    hips = armature_obj.pose.bones["Hips"]
    foot_targets = {
        side: armature_obj.pose.bones[f"{side}Foot"].matrix.copy()
        for side in ("Left", "Right")
    }
    armature_location = armature_obj.location.copy()
    armature_rotation = armature_obj.rotation_euler.copy()
    armature_scale = armature_obj.scale.copy()

    armature_obj.animation_data_create()
    action = bpy.data.actions.new(name=ACTION_NAME)
    armature_obj.animation_data.action = action
    maximum_connection_error = 0.0
    reach = {"Left": {}, "Right": {}}
    for frame in range(END_FRAME + 1):
        scene.frame_set(frame)
        reset_pose(armature_obj, base_basis)
        time_seconds = frame / FPS
        offset = downward_offset_sample(time_seconds)
        hips.matrix = Matrix.Translation(Vector((0.0, 0.0, -offset))) @ hips.matrix
        bpy.context.view_layer.update()
        for side in ("Left", "Right"):
            metrics = solve_leg(armature_obj, side, foot_targets[side])
            reach[side] = metrics
            maximum_connection_error = max(
                maximum_connection_error,
                metrics["connection_error_sample"],
            )
        key_pose(armature_obj, frame)

    if maximum_connection_error > 0.01:
        raise RuntimeError(
            "Kursa idle leg chain connection error is too large: "
            f"{maximum_connection_error}."
        )
    if (
        armature_obj.location != armature_location
        or armature_obj.rotation_euler != armature_rotation
        or armature_obj.scale != armature_scale
    ):
        raise RuntimeError("The Kursa animation changed the armature object transform.")

    inspection = inspect_action(
        scene,
        armature_obj,
        mesh_obj,
        foot_targets,
    )
    OUTPUT_FBX.parent.mkdir(parents=True, exist_ok=True)
    for obj in scene.objects:
        obj.select_set(False)
    armature_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_FBX),
        use_selection=True,
        object_types={"ARMATURE"},
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=False,
        bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        apply_unit_scale=False,
        apply_scale_options="FBX_SCALE_NONE",
        use_space_transform=True,
        path_mode="AUTO",
    )

    report = {
        "result": "PASS",
        "source_fbx": str(SOURCE_FBX.relative_to(ROOT)).replace("\\", "/"),
        "source_fbx_sha256": source_hash,
        "output_fbx": str(OUTPUT_FBX.relative_to(ROOT)).replace("\\", "/"),
        "output_fbx_sha256": sha256(OUTPUT_FBX),
        "action": ACTION_NAME,
        "frame_rate": FPS,
        "frames": [0, END_FRAME],
        "loop_seconds": LOOP_SECONDS,
        "vertical_travel_sample": VERTICAL_TRAVEL_SAMPLE,
        "kursa_placement_scale": KURSA_PLACEMENT_SCALE,
        "vertical_travel_source_world": (
            TARGET_UNITY_VERTICAL_TRAVEL / KURSA_PLACEMENT_SCALE
        ),
        "vertical_travel_unity_projected": TARGET_UNITY_VERTICAL_TRAVEL,
        "timeline": [
            {
                "time": time,
                "downward_offset_source_world": downward_offset_sample(time) / 100.0,
                "downward_offset_unity_projected": (
                    downward_offset_sample(time) / 100.0 * KURSA_PLACEMENT_SCALE
                ),
            }
            for time in KEY_TIMES
        ],
        "animated_bones": list(ANIMATED_BONES),
        "maximum_leg_connection_error_sample": maximum_connection_error,
        "reach": reach,
        "inspection": inspection,
        "root_motion": False,
        "bone_scaling": False,
        "method": (
            "Blender armature animation moves Hips from the current approved rest height "
            "down by the source distance that projects to a smooth 0.03-Unity-unit "
            "cycle at the approved placement scale while deterministic two-bone leg IK "
            "and fixed foot matrices preserve both grounded feet."
        ),
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(
        json.dumps(report, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
