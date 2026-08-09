import argparse
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


BODY_NAME = "Ispant_Armed_Body"
SWORD_NAME = "Ispant_ApprovedLongSword"
MUSKET_NAME = "Ispant_RunningAttack_RigidMusket"
ARMATURE_NAME = "Armature"
SPINE_BONE_NAME = "mixamorig:Spine2"
MUSKET_COMPONENTS = {41, 75, 76}
EXPECTED_SOURCE_BODY_TRIANGLES = 3518
EXPECTED_ANIMATED_BODY_TRIANGLES = 3364
EXPECTED_MUSKET_TRIANGLES = 154
EXPECTED_ACTION_FRAMES = (1.0, 91.0)
EXPECTED_RUN_ACTION_FRAMES = (1.0, 39.0)
RUN_ACTION_NAME = "Armature|mixamo.com|Layer0"
RUN_ACTION_SUFFIX = "|mixamo.com|Layer0"
RUN_CYCLES = 2
LOWER_BODY_BONES = (
    "LeftUpLeg",
    "LeftLeg",
    "LeftFoot",
    "LeftToeBase",
    "RightUpLeg",
    "RightLeg",
    "RightFoot",
    "RightToeBase",
)
TARGET_PREFIX = "mixamorig:"


def arguments():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--run-source", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def set_frame(frame):
    whole = int(frame)
    bpy.context.scene.frame_set(whole, subframe=frame - whole)
    bpy.context.view_layer.update()


def key_pose_bone(pose_bone, frame, include_location=False):
    pose_bone.rotation_mode = "QUATERNION"
    if include_location:
        pose_bone.keyframe_insert(data_path="location", frame=frame, group=pose_bone.name)
    pose_bone.keyframe_insert(
        data_path="rotation_quaternion", frame=frame, group=pose_bone.name
    )
    pose_bone.keyframe_insert(data_path="scale", frame=frame, group=pose_bone.name)


def triangle_count(mesh):
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


def connected_components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        left, right = edge.vertices
        adjacency[left].add(right)
        adjacency[right].add(left)

    result = []
    visited = set()
    for seed in range(len(mesh.vertices)):
        if seed in visited:
            continue
        stack = [seed]
        visited.add(seed)
        component = []
        while stack:
            vertex = stack.pop()
            component.append(vertex)
            for adjacent in adjacency[vertex]:
                if adjacent not in visited:
                    visited.add(adjacent)
                    stack.append(adjacent)
        result.append(component)
    return result


def keep_vertices(mesh, keep_indices):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    remove = [vertex for vertex in bm.verts if vertex.index not in keep_indices]
    bmesh.ops.delete(bm, geom=remove, context="VERTS")
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def remove_vertices(mesh, remove_indices):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    remove = [vertex for vertex in bm.verts if vertex.index in remove_indices]
    bmesh.ops.delete(bm, geom=remove, context="VERTS")
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def calculate_musket_back_correction(body, armature, musket_indices):
    original_frame = bpy.context.scene.frame_current
    original_pose_position = armature.data.pose_position
    bind_center = sum(
        (body.matrix_world @ body.data.vertices[index].co for index in musket_indices),
        Vector(),
    ) / len(musket_indices)
    rest_spine = (
        armature.matrix_world @ armature.data.bones[SPINE_BONE_NAME].matrix_local
    )
    bind_center_in_spine = rest_spine.inverted() @ bind_center
    animated_centers_in_spine = []
    try:
        armature.data.pose_position = "POSE"
        for frame in range(
            int(EXPECTED_ACTION_FRAMES[0]), int(EXPECTED_ACTION_FRAMES[1]) + 1
        ):
            bpy.context.scene.frame_set(frame)
            bpy.context.view_layer.update()
            evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
            evaluated_mesh = evaluated.to_mesh()
            try:
                center = sum(
                    (
                        evaluated.matrix_world @ evaluated_mesh.vertices[index].co
                        for index in musket_indices
                    ),
                    Vector(),
                ) / len(musket_indices)
                spine = (
                    armature.matrix_world
                    @ armature.pose.bones[SPINE_BONE_NAME].matrix
                )
                animated_centers_in_spine.append(spine.inverted() @ center)
            finally:
                evaluated.to_mesh_clear()
    finally:
        armature.data.pose_position = original_pose_position
        bpy.context.scene.frame_set(original_frame)
        bpy.context.view_layer.update()
    average_center_in_spine = (
        sum(animated_centers_in_spine, Vector()) / len(animated_centers_in_spine)
    )
    return average_center_in_spine - bind_center_in_spine


def split_rigid_musket(body, armature):
    if triangle_count(body.data) != EXPECTED_SOURCE_BODY_TRIANGLES:
        raise RuntimeError(
            f"Source body topology differs: {triangle_count(body.data)} triangles"
        )
    components = connected_components(body.data)
    if len(components) != 77:
        raise RuntimeError(f"Expected 77 body components, found {len(components)}")
    musket_indices = {
        vertex
        for component_index in MUSKET_COMPONENTS
        for vertex in components[component_index]
    }
    back_correction = calculate_musket_back_correction(
        body, armature, sorted(musket_indices)
    )

    musket = body.copy()
    musket.data = body.data.copy()
    musket.name = MUSKET_NAME
    musket.data.name = f"{MUSKET_NAME}_Mesh"
    bpy.context.collection.objects.link(musket)
    keep_vertices(musket.data, musket_indices)
    for modifier in list(musket.modifiers):
        musket.modifiers.remove(modifier)
    while musket.vertex_groups:
        musket.vertex_groups.remove(musket.vertex_groups[0])

    remove_vertices(body.data, musket_indices)
    if triangle_count(body.data) != EXPECTED_ANIMATED_BODY_TRIANGLES:
        raise RuntimeError(
            f"Animated body topology differs: {triangle_count(body.data)} triangles"
        )
    if triangle_count(musket.data) != EXPECTED_MUSKET_TRIANGLES:
        raise RuntimeError(
            f"Rigid musket topology differs: {triangle_count(musket.data)} triangles"
        )

    armature.data.pose_position = "REST"
    bpy.context.view_layer.update()
    world_matrix = musket.matrix_world.copy()
    rest_spine = (
        armature.matrix_world @ armature.data.bones[SPINE_BONE_NAME].matrix_local
    )
    world_matrix.translation += rest_spine.to_3x3() @ back_correction
    musket.parent = armature
    musket.parent_type = "BONE"
    musket.parent_bone = SPINE_BONE_NAME
    musket.matrix_world = world_matrix
    armature.data.pose_position = "POSE"
    bpy.context.scene.frame_set(int(EXPECTED_ACTION_FRAMES[0]))
    bpy.context.view_layer.update()
    return musket, back_correction


def configure_scene(source):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(
        filepath=str(source),
        use_anim=True,
        ignore_leaf_bones=False,
    )
    armature = bpy.data.objects.get(ARMATURE_NAME)
    body = bpy.data.objects.get(BODY_NAME)
    sword = bpy.data.objects.get(SWORD_NAME)
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("The supplied Mixamo armature is missing.")
    if len(armature.data.bones) != 33:
        raise RuntimeError(f"Expected 33 Mixamo bones, found {len(armature.data.bones)}")
    if SPINE_BONE_NAME not in armature.data.bones:
        raise RuntimeError(f"Required attachment bone is missing: {SPINE_BONE_NAME}")
    if body is None or body.type != "MESH":
        raise RuntimeError("The exact approved Ispant body mesh is missing.")
    if sword is None or sword.type != "MESH":
        raise RuntimeError("The supplied approved sword reference is missing.")
    if len(bpy.data.actions) != 1:
        raise RuntimeError(f"Expected one Mixamo action, found {len(bpy.data.actions)}")
    action = bpy.data.actions[0]
    if "mixamo.com" not in action.name.lower():
        raise RuntimeError(f"The sole action is not the supplied Mixamo action: {action.name}")
    if tuple(action.frame_range) != EXPECTED_ACTION_FRAMES:
        raise RuntimeError(f"Mixamo frame range differs: {tuple(action.frame_range)}")

    musket, back_correction = split_rigid_musket(body, armature)
    bpy.data.objects.remove(sword, do_unlink=True)
    return armature, musket, action, back_correction


def import_run_armature(run_source):
    objects_before = set(bpy.data.objects)
    actions_before = set(bpy.data.actions)
    bpy.ops.import_scene.fbx(
        filepath=str(run_source),
        use_anim=True,
        ignore_leaf_bones=False,
    )
    imported_objects = [obj for obj in bpy.data.objects if obj not in objects_before]
    imported_armatures = [obj for obj in imported_objects if obj.type == "ARMATURE"]
    imported_actions = [action for action in bpy.data.actions if action not in actions_before]
    if len(imported_armatures) != 1:
        raise RuntimeError(
            f"Expected one running armature, found {len(imported_armatures)}"
        )
    if len(imported_actions) != 1:
        raise RuntimeError(f"Expected one running action, found {len(imported_actions)}")
    armature = imported_armatures[0]
    action = imported_actions[0]
    if action.name != RUN_ACTION_NAME and not action.name.endswith(RUN_ACTION_SUFFIX):
        raise RuntimeError(f"Running action differs: {action.name}")
    if tuple(action.frame_range) != EXPECTED_RUN_ACTION_FRAMES:
        raise RuntimeError(f"Running frame range differs: {tuple(action.frame_range)}")
    required = {
        f"{TARGET_PREFIX}Hips",
        *(f"{TARGET_PREFIX}{name}" for name in LOWER_BODY_BONES),
    }
    missing = sorted(required.difference(armature.data.bones.keys()))
    if missing:
        raise RuntimeError(f"Running lower-body bones are missing: {missing}")
    armature.animation_data_create()
    armature.animation_data.action = action
    return armature, action, imported_objects


def require_matching_rest_rig(attack_armature, run_armature):
    attack_names = [bone.name for bone in attack_armature.data.bones]
    run_names = [bone.name for bone in run_armature.data.bones]
    if attack_names != run_names:
        raise RuntimeError("The Ispant running rig bone order differs from the attack rig")
    maximum_error = 0.0
    for bone_name in attack_names:
        attack_matrix = attack_armature.data.bones[bone_name].matrix_local
        run_matrix = run_armature.data.bones[bone_name].matrix_local
        for row in range(4):
            for column in range(4):
                maximum_error = max(
                    maximum_error,
                    abs(attack_matrix[row][column] - run_matrix[row][column]),
                )
    if maximum_error > 0.000001:
        raise RuntimeError(
            f"The Ispant running REST rig differs from the attack rig: {maximum_error}"
        )
    return maximum_error


def sample_run_motion(run_armature):
    first, last = EXPECTED_RUN_ACTION_FRAMES
    duration = last - first
    count = int(EXPECTED_ACTION_FRAMES[1] - EXPECTED_ACTION_FRAMES[0]) + 1
    samples = []
    for index in range(count):
        if index == count - 1:
            run_frame = first
        else:
            phase = (index / (count - 1)) * RUN_CYCLES * duration
            run_frame = first + (phase % duration)
        set_frame(run_frame)
        poses = {}
        for suffix in ("Hips", *LOWER_BODY_BONES):
            bone_name = f"{TARGET_PREFIX}{suffix}"
            poses[bone_name] = run_armature.pose.bones[bone_name].matrix_basis.copy()
        samples.append(
            {
                "frame": run_frame,
                "poses": poses,
            }
        )
    return samples


def compose_running_lower_body(armature, action, run_source):
    required = {
        f"{TARGET_PREFIX}Hips",
        f"{TARGET_PREFIX}Spine",
        *(f"{TARGET_PREFIX}{name}" for name in LOWER_BODY_BONES),
    }
    missing = sorted(required.difference(armature.data.bones.keys()))
    if missing:
        raise RuntimeError(f"Target lower-body bones are missing: {missing}")

    first, last = map(int, EXPECTED_ACTION_FRAMES)

    run_armature, run_action, imported_objects = import_run_armature(run_source)
    rest_error = require_matching_rest_rig(armature, run_armature)
    run_samples = sample_run_motion(run_armature)

    for imported in imported_objects:
        bpy.data.objects.remove(imported, do_unlink=True)
    bpy.data.actions.remove(run_action)
    armature.animation_data.action = action

    for index, frame in enumerate(range(first, last + 1)):
        set_frame(frame)
        sample = run_samples[index]

        hips_name = f"{TARGET_PREFIX}Hips"
        armature.pose.bones[hips_name].matrix_basis = sample["poses"][hips_name]
        key_pose_bone(armature.pose.bones[hips_name], frame, include_location=True)
        bpy.context.view_layer.update()

        for source_name in LOWER_BODY_BONES:
            target_name = f"{TARGET_PREFIX}{source_name}"
            pose_bone = armature.pose.bones[target_name]
            pose_bone.matrix_basis = sample["poses"][target_name]
            key_pose_bone(pose_bone, frame, include_location=True)
            bpy.context.view_layer.update()

    set_frame(first)
    return rest_error


def export_fbx(output, armature, action):
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.frame_start = int(EXPECTED_ACTION_FRAMES[0])
    bpy.context.scene.frame_end = int(EXPECTED_ACTION_FRAMES[1])
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    expected_names = {
        BODY_NAME,
        "Ispant_Crescent_Ornament",
        "Ispant_Reference_Eye_Slits",
        MUSKET_NAME,
    }
    if {obj.name for obj in meshes} != expected_names:
        raise RuntimeError(
            f"Derived renderer set differs: {sorted(obj.name for obj in meshes)}"
        )
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
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode="AUTO",
        embed_textures=False,
        use_metadata=True,
    )
    if not output.exists():
        raise RuntimeError(f"Derived animation FBX was not created: {output}")


def main():
    args = arguments()
    source = Path(args.source).resolve()
    run_source = Path(args.run_source).resolve()
    output = Path(args.output).resolve()
    armature, _, action, back_correction = configure_scene(source)
    rest_error = compose_running_lower_body(armature, action, run_source)
    export_fbx(output, armature, action)
    print(
        "IspantRunningSwordAttackBuilt"
        f" Source={source} Output={output}"
        f" Action={action.name} Frames=1-91"
        f" RunSource={run_source} RunAction={RUN_ACTION_NAME}"
        f" RunFrames=1-39 RunCycles={RUN_CYCLES}"
        f" LowerBodyBones={len(LOWER_BODY_BONES)}"
        f" RestRigMaximumMatrixError={rest_error:.9f}"
        " LowerBodyLocalPoseCopiedExactly=True"
        " UpperBodyLocalAttackCurvesUnchanged=True"
        f" BodyTriangles={EXPECTED_ANIMATED_BODY_TRIANGLES}"
        f" RigidMusketTriangles={EXPECTED_MUSKET_TRIANGLES}"
        f" MusketBackCorrectionBoneLocal={tuple(round(value, 9) for value in back_correction)}"
        " SourceSwordRemoved=True"
    )


if __name__ == "__main__":
    main()
