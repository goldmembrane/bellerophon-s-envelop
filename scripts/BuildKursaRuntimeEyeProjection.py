import hashlib
import json
from pathlib import Path

import bpy
from math import atan2

from mathutils import Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree


ROOT = Path(__file__).resolve().parents[1]
SAMPLE_ROOT = ROOT / "artSample" / "enemies" / "kursa" / "appearance_reference_sync"
SOURCE_BLEND = SAMPLE_ROOT / "blender" / "Kursa_Appearance_ReferenceSync.blend"
SOURCE_APPROVED_FBX = SAMPLE_ROOT / "exports" / "Kursa_Appearance_ReferenceSync.fbx"
OUTPUT_FBX = (
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
REPORT = (
    ROOT
    / "docs"
    / "validation"
    / "kursa_approved_appearance_2026-08-02"
    / "Kursa_RuntimeProjection_Export.json"
)

LEFT = {
    "center": Vector((3.343094, 151.815475, 24.579956)),
    "surface_normal": Vector((-0.182243, -0.571750, 0.799931)).normalized(),
    "size": (8.348116, 8.988050),
    "depth": 2.05,
    "polygon": 3801,
}
RIGHT = {
    "center": Vector((5.916458, 152.454803, 19.357758)),
    "surface_normal": Vector((0.257649, -0.965079, -0.047329)).normalized(),
    "size": (10.076670, 8.897684),
    "depth": 2.05,
    "polygon": 3627,
}
# The former target-view normal was authored for the old three-quarter face
# presentation.  The corrected Head pose now presents the intrinsic eye line
# to model-local forward, so derive the projection plane from the two retained
# eye centers and the model-up hint.  This removes per-surface horizontal shear
# without changing either center, patch size, depth, texture, or mesh geometry.
EYE_HORIZONTAL = (RIGHT["center"] - LEFT["center"]).normalized()
EYE_VERTICAL_HINT = (
    Vector((0.0, 1.0, 0.0))
    - EYE_HORIZONTAL * Vector((0.0, 1.0, 0.0)).dot(EYE_HORIZONTAL)
).normalized()
PROJECTION_NORMAL = EYE_HORIZONTAL.cross(EYE_VERTICAL_HINT).normalized()
if PROJECTION_NORMAL.z < 0.0:
    PROJECTION_NORMAL.negate()
VERTICAL = (
    Vector((0.0, 1.0, 0.0))
    - PROJECTION_NORMAL
    * Vector((0.0, 1.0, 0.0)).dot(PROJECTION_NORMAL)
).normalized()
HORIZONTAL = VERTICAL.cross(PROJECTION_NORMAL).normalized()

LEFT_UV = "KursaEyeLeftProjection"
RIGHT_UV = "KursaEyeRightProjection"
DEPTH_UV = "KursaEyeSignedDepth"
LEFT_POSE_BONES = ("LeftArm", "LeftForeArm", "LeftHand")
RIGHT_POSE_BONES = ("RightArm", "RightForeArm", "RightHand")
POSE_BONES = LEFT_POSE_BONES
# The runtime front view shows the lower face plate centered about 2.5 sample
# units to the visual right of the eye midpoint. Only the visible front jaw is
# translated; the correction fades to zero above the jaw hinge and behind the
# front plate so the eyes, nose, hood, neck, and head direction stay unchanged.
CHIN_LATERAL_CORRECTION = -2.5
CHIN_VERTICAL_BLEND_START = -6.5
CHIN_VERTICAL_BLEND_END = -9.5
CHIN_FORWARD_BLEND_START = -10.0
CHIN_FORWARD_BLEND_END = -6.0


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def projection(position, patch):
    delta = position - patch["center"]
    uv = (
        delta.dot(HORIZONTAL) / patch["size"][0] + 0.5,
        delta.dot(VERTICAL) / patch["size"][1] + 0.5,
    )
    signed_depth = delta.dot(patch["surface_normal"]) / patch["depth"]
    return uv, signed_depth


def smooth_unit(value):
    value = max(0.0, min(1.0, value))
    return value * value * (3.0 - 2.0 * value)


def bone_deform_matrix(mesh_obj, armature_obj, bone_name):
    bone = armature_obj.data.bones[bone_name]
    pose_bone = armature_obj.pose.bones[bone_name]
    return (
        mesh_obj.matrix_world.inverted()
        @ armature_obj.matrix_world
        @ pose_bone.matrix
        @ bone.matrix_local.inverted()
        @ armature_obj.matrix_world.inverted()
        @ mesh_obj.matrix_world
    )


def blended_deform_linear(mesh_obj, armature_obj, vertex, deform_matrices):
    matrix = Matrix(((0.0, 0.0, 0.0),) * 3)
    total_weight = 0.0
    for group in vertex.groups:
        name = mesh_obj.vertex_groups[group.group].name
        deform = deform_matrices.get(name)
        if deform is None or group.weight <= 0.0:
            continue
        linear = deform.to_3x3()
        for row in range(3):
            for column in range(3):
                matrix[row][column] += linear[row][column] * group.weight
        total_weight += group.weight
    if total_weight <= 1e-6:
        raise RuntimeError(
            f"Chin vertex {vertex.index} has no deform-bone weight."
        )
    if total_weight < 1.0:
        for axis in range(3):
            matrix[axis][axis] += 1.0 - total_weight
    if abs(matrix.determinant()) <= 1e-8:
        raise RuntimeError(
            f"Chin vertex {vertex.index} has a singular blended deform matrix."
        )
    return matrix


def apply_front_chin_alignment(mesh_obj, armature_obj, evaluated_mesh):
    material_names = [
        slot.material.name if slot.material else ""
        for slot in mesh_obj.material_slots
    ]
    face_indices = [
        index
        for index, name in enumerate(material_names)
        if "face" in name.lower() and "metal" in name.lower()
    ]
    if len(face_indices) != 1:
        raise RuntimeError(
            f"Expected one face-metal material for chin alignment: {material_names}"
        )
    face_index = face_indices[0]
    face_vertices = sorted(
        {
            vertex_index
            for polygon in evaluated_mesh.polygons
            if polygon.material_index == face_index
            for vertex_index in polygon.vertices
        }
    )
    deform_matrices = {
        bone.name: bone_deform_matrix(mesh_obj, armature_obj, bone.name)
        for bone in armature_obj.data.bones
        if bone.use_deform and bone.name in armature_obj.pose.bones
    }
    selected = []
    eye_midpoint = (LEFT["center"] + RIGHT["center"]) * 0.5
    for index in face_vertices:
        evaluated_position = evaluated_mesh.vertices[index].co.copy()
        delta = evaluated_position - eye_midpoint
        horizontal = delta.dot(HORIZONTAL)
        vertical = delta.dot(VERTICAL)
        forward = delta.dot(PROJECTION_NORMAL)
        vertical_weight = smooth_unit(
            (CHIN_VERTICAL_BLEND_START - vertical)
            / (CHIN_VERTICAL_BLEND_START - CHIN_VERTICAL_BLEND_END)
        )
        forward_weight = smooth_unit(
            (forward - CHIN_FORWARD_BLEND_START)
            / (CHIN_FORWARD_BLEND_END - CHIN_FORWARD_BLEND_START)
        )
        correction_weight = vertical_weight * forward_weight
        if correction_weight <= 0.001:
            continue
        vertex = mesh_obj.data.vertices[index]
        desired_evaluated_delta = (
            HORIZONTAL * CHIN_LATERAL_CORRECTION * correction_weight
        )
        deform_linear = blended_deform_linear(
            mesh_obj,
            armature_obj,
            vertex,
            deform_matrices,
        )
        base_delta = deform_linear.inverted() @ desired_evaluated_delta
        vertex.co += base_delta
        selected.append(
            {
                "index": index,
                "horizontal_before": horizontal,
                "vertical": vertical,
                "forward": forward,
                "weight": correction_weight,
                "requested_evaluated_lateral_delta":
                    CHIN_LATERAL_CORRECTION * correction_weight,
                "base_delta": list(base_delta),
            }
        )
    if not selected:
        raise RuntimeError("The visible front-chin selection is empty.")
    return {
        "selected_vertex_indices": [item["index"] for item in selected],
        "selected_vertices": len(selected),
        "lateral_correction": CHIN_LATERAL_CORRECTION,
        "vertical_blend": [
            CHIN_VERTICAL_BLEND_START,
            CHIN_VERTICAL_BLEND_END,
        ],
        "forward_blend": [
            CHIN_FORWARD_BLEND_START,
            CHIN_FORWARD_BLEND_END,
        ],
        "vertices": selected,
    }


def stable_mesh_signature(mesh):
    return {
        "vertices": [tuple(round(value, 8) for value in vertex.co) for vertex in mesh.vertices],
        "polygons": [
            (tuple(polygon.vertices), polygon.material_index)
            for polygon in mesh.polygons
        ],
        "uv0": [
            tuple(round(value, 8) for value in item.uv)
            for item in mesh.uv_layers[0].data
        ],
    }


def stable_hash(value):
    return hashlib.sha256(
        json.dumps(value, separators=(",", ":"), sort_keys=True).encode("utf-8")
    ).hexdigest().upper()


def stable_weight_signature(mesh_obj):
    group_names = {
        group.index: group.name
        for group in mesh_obj.vertex_groups
    }
    return [
        sorted(
            (group_names[group.group], round(group.weight, 8))
            for group in vertex.groups
        )
        for vertex in mesh_obj.data.vertices
    ]


def stable_topology_material_uv0_signature(mesh):
    return {
        "polygons": [
            (tuple(polygon.vertices), polygon.material_index)
            for polygon in mesh.polygons
        ],
        "loops": [loop.vertex_index for loop in mesh.loops],
        "uv0": [
            tuple(round(value, 8) for value in item.uv)
            for item in mesh.uv_layers[0].data
        ],
    }


def pose_vertex_indices(mesh_obj, bone_names):
    result = set()
    for vertex in mesh_obj.data.vertices:
        strongest = sorted(
            vertex.groups,
            key=lambda group: group.weight,
            reverse=True,
        )[:4]
        if any(
            mesh_obj.vertex_groups[group.group].name in bone_names
            and group.weight > 0.0
            for group in strongest
        ):
            result.add(vertex.index)
    return result


def position_bounds(positions):
    minimum = Vector((float("inf"), float("inf"), float("inf")))
    maximum = Vector((float("-inf"), float("-inf"), float("-inf")))
    for coordinate in positions:
        for axis in range(3):
            minimum[axis] = min(minimum[axis], coordinate[axis])
            maximum[axis] = max(maximum[axis], coordinate[axis])
    return minimum, maximum


def influence_surface_data(mesh_obj, evaluated_mesh, bone_names):
    group_names = {
        group.index: group.name
        for group in mesh_obj.vertex_groups
    }
    polygons = []
    for polygon in mesh_obj.data.polygons:
        total_weight = sum(
            sum(
                group.weight
                for group in mesh_obj.data.vertices[index].groups
                if group_names[group.group] in bone_names
            )
            for index in polygon.vertices
        )
        if total_weight > len(polygon.vertices) * 0.5:
            polygons.append(tuple(polygon.vertices))
    if not polygons:
        raise RuntimeError(
            f"The influence surface is empty for bones {sorted(bone_names)}."
        )
    vertex_indices = sorted({index for polygon in polygons for index in polygon})
    remap = {
        original: remapped
        for remapped, original in enumerate(vertex_indices)
    }
    positions = [
        evaluated_mesh.vertices[index].co.copy()
        for index in vertex_indices
    ]
    remapped_polygons = [
        tuple(remap[index] for index in polygon)
        for polygon in polygons
    ]
    return {
        "polygons": polygons,
        "vertex_indices": vertex_indices,
        "positions": positions,
        "bvh": BVHTree.FromPolygons(
            positions,
            remapped_polygons,
            all_triangles=True,
        ),
    }


def right_arm_thigh_surface_separation(mesh_obj, evaluated_mesh):
    arm = influence_surface_data(
        mesh_obj,
        evaluated_mesh,
        set(RIGHT_POSE_BONES),
    )
    thigh = influence_surface_data(
        mesh_obj,
        evaluated_mesh,
        {"RightUpLeg"},
    )
    overlap_count = len(arm["bvh"].overlap(thigh["bvh"]))
    minimum_clearance = float("inf")
    for position in arm["positions"]:
        nearest = thigh["bvh"].find_nearest(position)
        if nearest is not None:
            minimum_clearance = min(minimum_clearance, nearest[3])
    for position in thigh["positions"]:
        nearest = arm["bvh"].find_nearest(position)
        if nearest is not None:
            minimum_clearance = min(minimum_clearance, nearest[3])
    if minimum_clearance == float("inf"):
        raise RuntimeError("The right arm-to-thigh surface clearance is unavailable.")
    return {
        "arm_polygons": len(arm["polygons"]),
        "thigh_polygons": len(thigh["polygons"]),
        "overlap_count": overlap_count,
        "minimum_clearance": minimum_clearance,
    }


def rigid_rotation(pivot, rotation):
    return (
        Matrix.Translation(pivot)
        @ rotation.to_matrix().to_4x4()
        @ Matrix.Translation(-pivot)
    )


def shield_data(mesh_obj, evaluated_mesh):
    material_names = [slot.material.name for slot in mesh_obj.material_slots]
    worn_index = material_names.index("Kursa_Shield_Worn_Gunmetal")
    frame_index = material_names.index("Kursa_Shield_Frame_Steel")
    shield_polygons = [
        polygon
        for polygon in evaluated_mesh.polygons
        if polygon.material_index in {worn_index, frame_index}
    ]
    worn_polygons = [
        polygon
        for polygon in evaluated_mesh.polygons
        if polygon.material_index == worn_index
    ]
    shield_vertices = sorted(
        {
            vertex_index
            for polygon in shield_polygons
            for vertex_index in polygon.vertices
        }
    )
    minimum = Vector((float("inf"), float("inf"), float("inf")))
    maximum = Vector((float("-inf"), float("-inf"), float("-inf")))
    for index in shield_vertices:
        coordinate = evaluated_mesh.vertices[index].co
        minimum.x = min(minimum.x, coordinate.x)
        minimum.y = min(minimum.y, coordinate.y)
        minimum.z = min(minimum.z, coordinate.z)
        maximum.x = max(maximum.x, coordinate.x)
        maximum.y = max(maximum.y, coordinate.y)
        maximum.z = max(maximum.z, coordinate.z)

    center = sum(
        (evaluated_mesh.vertices[index].co for index in shield_vertices),
        Vector((0.0, 0.0, 0.0)),
    ) / len(shield_vertices)

    clusters = []
    for polygon in sorted(worn_polygons, key=lambda item: item.area, reverse=True):
        normal = polygon.normal.normalized()
        match = next(
            (
                cluster
                for cluster in clusters
                if normal.dot(cluster["normal"].normalized()) >= 0.985
            ),
            None,
        )
        if match is None:
            match = {"normal": Vector((0.0, 0.0, 0.0)), "area": 0.0}
            clusters.append(match)
        match["normal"] += normal * polygon.area
        match["area"] += polygon.area
    outward = max(
        (cluster for cluster in clusters if cluster["normal"].z > 0.0),
        key=lambda cluster: cluster["area"],
    )["normal"].normalized()
    covariance = Matrix(((0.0, 0.0, 0.0),) * 3)
    for index in shield_vertices:
        delta = evaluated_mesh.vertices[index].co - center
        planar = delta - outward * delta.dot(outward)
        for row in range(3):
            for column in range(3):
                covariance[row][column] += planar[row] * planar[column]
    long_axis = Vector((0.0, 1.0, 0.0))
    long_axis -= outward * long_axis.dot(outward)
    if long_axis.length < 1e-6:
        long_axis = Vector((1.0, 0.0, 0.0))
        long_axis -= outward * long_axis.dot(outward)
    long_axis.normalize()
    for _ in range(64):
        candidate = covariance @ long_axis
        candidate -= outward * candidate.dot(outward)
        if candidate.length < 1e-9:
            raise RuntimeError("The shield long-axis covariance is degenerate.")
        candidate.normalize()
        if (candidate - long_axis).length < 1e-10 or (
            candidate + long_axis
        ).length < 1e-10:
            long_axis = candidate
            break
        long_axis = candidate
    if long_axis.y < 0.0:
        long_axis.negate()
    return {
        "vertices": shield_vertices,
        "vertex_positions": {
            index: evaluated_mesh.vertices[index].co.copy()
            for index in shield_vertices
        },
        "outward_normal": outward,
        "long_axis": long_axis,
        "bounds_min": minimum,
        "bounds_max": maximum,
        "bounds_center": (minimum + maximum) * 0.5,
        "bounds_size": maximum - minimum,
    }


def evaluated_mesh(mesh_obj):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_obj = mesh_obj.evaluated_get(depsgraph)
    mesh = evaluated_obj.to_mesh(
        preserve_all_data_layers=True,
        depsgraph=depsgraph,
    )
    return evaluated_obj, mesh


def torso_centerline_x_at_y(armature_to_mesh, armature_obj, target_y):
    hips_head = armature_to_mesh @ armature_obj.pose.bones["Hips"].head
    spine02_head = armature_to_mesh @ armature_obj.pose.bones["Spine02"].head
    if hips_head.y <= target_y <= spine02_head.y:
        factor = (target_y - hips_head.y) / (spine02_head.y - hips_head.y)
        centerline_x = hips_head.x + (spine02_head.x - hips_head.x) * factor
        return centerline_x, "HipsToSpine02", hips_head, spine02_head
    for bone_name in ("Spine02", "Spine01", "Spine"):
        pose_bone = armature_obj.pose.bones[bone_name]
        head = armature_to_mesh @ pose_bone.head
        tail = armature_to_mesh @ pose_bone.tail
        minimum_y = min(head.y, tail.y)
        maximum_y = max(head.y, tail.y)
        if minimum_y <= target_y <= maximum_y:
            if abs(tail.y - head.y) < 1e-6:
                raise RuntimeError(
                    f"The torso centerline bone {bone_name} has no vertical span."
                )
            factor = (target_y - head.y) / (tail.y - head.y)
            centerline_x = head.x + (tail.x - head.x) * factor
            return centerline_x, bone_name, head, tail
    raise RuntimeError(
        f"No torso centerline bone spans the shield center height {target_y}."
    )


def capture_action_pose(scene, armature_obj, bone_names):
    action = armature_obj.animation_data.action
    if action is None:
        raise RuntimeError("The approved Kursa action is missing.")
    start = int(action.frame_range[0])
    end = int(action.frame_range[1])
    matrices = {}
    for frame in range(start, end + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        matrices[frame] = {
            name: armature_obj.pose.bones[name].matrix_basis.copy()
            for name in bone_names
        }
    return action, start, end, matrices


def apply_center_preserving_shield_rest_pose(scene, mesh_obj, armature_obj):
    all_bone_names = tuple(bone.name for bone in armature_obj.data.bones)
    action, action_start, action_end, original_action_pose = capture_action_pose(
        scene,
        armature_obj,
        all_bone_names,
    )
    scene.frame_set(1)
    bpy.context.view_layer.update()

    evaluated_obj, frame_mesh = evaluated_mesh(mesh_obj)
    try:
        before_shield = shield_data(mesh_obj, frame_mesh)
    finally:
        evaluated_obj.to_mesh_clear()

    model_forward = Vector((0.0, 0.0, 1.0))
    model_up = Vector((0.0, 1.0, 0.0))
    forward_rotation = before_shield["outward_normal"].rotation_difference(
        model_forward
    )
    forward_aligned_long_axis = forward_rotation @ before_shield["long_axis"]
    forward_aligned_long_axis -= model_forward * forward_aligned_long_axis.dot(
        model_forward
    )
    forward_aligned_long_axis.normalize()
    roll_angle = atan2(
        model_forward.dot(forward_aligned_long_axis.cross(model_up)),
        forward_aligned_long_axis.dot(model_up),
    )
    roll_rotation = Quaternion(model_forward, roll_angle)
    alignment_rotation = roll_rotation @ forward_rotation
    shield_center = before_shield["bounds_center"]
    armature_to_mesh = mesh_obj.matrix_world.inverted() @ armature_obj.matrix_world
    mesh_to_armature = armature_to_mesh.inverted()

    left_arm = armature_obj.pose.bones["LeftArm"]
    left_forearm = armature_obj.pose.bones["LeftForeArm"]
    left_hand = armature_obj.pose.bones["LeftHand"]
    arm_matrix = armature_to_mesh @ left_arm.matrix
    forearm_matrix = armature_to_mesh @ left_forearm.matrix
    hand_matrix = armature_to_mesh @ left_hand.matrix
    arm_root = armature_to_mesh @ left_arm.head
    elbow = armature_to_mesh @ left_forearm.head
    hand_head = armature_to_mesh @ left_hand.head

    right_arm = armature_obj.pose.bones["RightArm"]
    right_forearm = armature_obj.pose.bones["RightForeArm"]
    right_hand = armature_obj.pose.bones["RightHand"]
    right_arm_root = armature_to_mesh @ right_arm.head
    right_elbow = armature_to_mesh @ right_forearm.head
    right_hand_head = armature_to_mesh @ right_hand.head

    centered_rotation = rigid_rotation(shield_center, alignment_rotation)
    rotated_minimum = Vector((float("inf"), float("inf"), float("inf")))
    rotated_maximum = Vector((float("-inf"), float("-inf"), float("-inf")))
    for coordinate in before_shield["vertex_positions"].values():
        rotated = centered_rotation @ coordinate
        rotated_minimum.x = min(rotated_minimum.x, rotated.x)
        rotated_minimum.y = min(rotated_minimum.y, rotated.y)
        rotated_minimum.z = min(rotated_minimum.z, rotated.z)
        rotated_maximum.x = max(rotated_maximum.x, rotated.x)
        rotated_maximum.y = max(rotated_maximum.y, rotated.y)
        rotated_maximum.z = max(rotated_maximum.z, rotated.z)
    bounds_center_correction = shield_center - (
        rotated_minimum + rotated_maximum
    ) * 0.5
    shield_transform = (
        Matrix.Translation(bounds_center_correction)
        @ centered_rotation
    )
    target_hand_head = shield_transform @ hand_head
    upper_length = (elbow - arm_root).length
    forearm_length = (hand_head - elbow).length
    target_vector = target_hand_head - arm_root
    target_distance = target_vector.length
    uncorrected_target_distance = target_distance
    minimum_reach = abs(upper_length - forearm_length)
    maximum_reach = upper_length + forearm_length
    reach_correction = Vector((0.0, 0.0, 0.0))
    reach_margin = 1e-4
    if target_distance > maximum_reach:
        reachable_distance = maximum_reach - reach_margin
        reach_correction = (
            -target_vector.normalized()
            * (target_distance - reachable_distance)
        )
    elif target_distance < minimum_reach:
        reachable_distance = minimum_reach + reach_margin
        reach_correction = (
            target_vector.normalized()
            * (reachable_distance - target_distance)
        )

    if reach_correction.length > 0.0:
        shield_transform = Matrix.Translation(reach_correction) @ shield_transform
        target_hand_head = shield_transform @ hand_head
        target_vector = target_hand_head - arm_root
        target_distance = target_vector.length

    baseline_shield_center = shield_center + reach_correction
    (
        torso_centerline_x,
        torso_centerline_bone,
        torso_centerline_head,
        torso_centerline_tail,
    ) = torso_centerline_x_at_y(
        armature_to_mesh,
        armature_obj,
        baseline_shield_center.y,
    )
    baseline_lateral_gap = baseline_shield_center.x - torso_centerline_x
    target_lateral_gap = baseline_lateral_gap * 0.5
    target_shield_center_x = torso_centerline_x + target_lateral_gap
    lateral_center_correction = Vector((
        target_shield_center_x - baseline_shield_center.x,
        0.0,
        0.0,
    ))
    shield_transform = (
        Matrix.Translation(lateral_center_correction) @ shield_transform
    )
    target_hand_head = shield_transform @ hand_head
    target_vector = target_hand_head - arm_root
    target_distance = target_vector.length

    if not minimum_reach <= target_distance <= maximum_reach:
        raise RuntimeError(
            "The half-gap torso-front shield target is outside the approved arm reach."
        )

    target_direction = target_vector.normalized()
    along = (
        upper_length * upper_length
        - forearm_length * forearm_length
        + target_distance * target_distance
    ) / (2.0 * target_distance)
    height = max(0.0, upper_length * upper_length - along * along) ** 0.5
    current_elbow_offset = elbow - (
        arm_root + target_direction * (elbow - arm_root).dot(target_direction)
    )
    if current_elbow_offset.length < 1e-6:
        raise RuntimeError("The approved elbow plane is degenerate.")
    target_elbow = (
        arm_root
        + target_direction * along
        + current_elbow_offset.normalized() * height
    )

    upper_rotation = (elbow - arm_root).rotation_difference(
        target_elbow - arm_root
    )
    upper_transform = rigid_rotation(arm_root, upper_rotation)
    arm_matrix = upper_transform @ arm_matrix
    forearm_matrix = upper_transform @ forearm_matrix
    transformed_elbow = upper_transform @ elbow
    transformed_hand_head = upper_transform @ hand_head
    forearm_rotation = (
        transformed_hand_head - transformed_elbow
    ).rotation_difference(target_hand_head - target_elbow)
    forearm_transform = rigid_rotation(target_elbow, forearm_rotation)
    forearm_matrix = forearm_transform @ forearm_matrix
    desired_hand_matrix = shield_transform @ hand_matrix

    left_arm.matrix = mesh_to_armature @ arm_matrix
    bpy.context.view_layer.update()
    left_forearm.matrix = mesh_to_armature @ forearm_matrix
    bpy.context.view_layer.update()
    left_hand.matrix = mesh_to_armature @ desired_hand_matrix
    bpy.context.view_layer.update()

    connection_error = max(
        ((armature_to_mesh @ left_forearm.head) - target_elbow).length,
        ((armature_to_mesh @ left_hand.head) - target_hand_head).length,
    )
    if connection_error > 1e-4:
        raise RuntimeError(
            f"The solved left-arm chain is disconnected: {connection_error}."
        )

    allowed_vertex_indices = pose_vertex_indices(mesh_obj, POSE_BONES)
    evaluated_obj, solved_mesh = evaluated_mesh(mesh_obj)
    try:
        solved_positions = {
            index: solved_mesh.vertices[index].co.copy()
            for index in allowed_vertex_indices
        }
    finally:
        evaluated_obj.to_mesh_clear()

    bpy.context.view_layer.objects.active = armature_obj
    armature_obj.select_set(True)
    bpy.ops.object.mode_set(mode="POSE")
    for pose_bone in armature_obj.pose.bones:
        pose_bone.select = pose_bone.name in POSE_BONES
    bpy.ops.pose.armature_apply(selected=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    for index, coordinate in solved_positions.items():
        mesh_obj.data.vertices[index].co = coordinate
    mesh_obj.data.update()
    bpy.context.view_layer.update()

    armature_obj.animation_data.action = action
    maximum_animation_error = 0.0
    maximum_animation_error_detail = None
    for frame in range(action_start, action_end + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        for name in all_bone_names:
            actual = armature_obj.pose.bones[name].matrix_basis
            expected = original_action_pose[frame][name]
            for row in range(4):
                for column in range(4):
                    error = abs(actual[row][column] - expected[row][column])
                    if error > maximum_animation_error:
                        maximum_animation_error = error
                        maximum_animation_error_detail = {
                            "frame": frame,
                            "bone": name,
                            "row": row,
                            "column": column,
                            "actual": actual[row][column],
                            "expected": expected[row][column],
                        }
    if maximum_animation_error > 1e-3:
        raise RuntimeError(
            "The embedded animation local channels changed: "
            f"{maximum_animation_error}; detail={maximum_animation_error_detail}."
        )

    armature_obj.animation_data.action = None
    for pose_bone in armature_obj.pose.bones:
        pose_bone.matrix_basis.identity()
    scene.frame_set(1)
    bpy.context.view_layer.update()

    evaluated_obj, rest_mesh = evaluated_mesh(mesh_obj)
    try:
        after_shield = shield_data(mesh_obj, rest_mesh)
        right_arm_vertex_indices = pose_vertex_indices(
            mesh_obj,
            RIGHT_POSE_BONES,
        )
        right_arm_thigh_separation = right_arm_thigh_surface_separation(
            mesh_obj,
            rest_mesh,
        )
        final_right_positions = {
            index: rest_mesh.vertices[index].co.copy()
            for index in right_arm_vertex_indices
        }
        maximum_solved_position_error = max(
            (rest_mesh.vertices[index].co - solved_positions[index]).length
            for index in solved_positions
        )
    finally:
        evaluated_obj.to_mesh_clear()
    final_angle = after_shield["outward_normal"].angle(model_forward)
    final_vertical_angle = after_shield["long_axis"].angle(model_up)
    center_shift = (
        after_shield["bounds_center"] - before_shield["bounds_center"]
    )
    final_lateral_gap = after_shield["bounds_center"].x - torso_centerline_x
    if final_angle > 0.001:
        raise RuntimeError(
            f"The shield rest-pose normal is not forward: {final_angle}."
        )
    if final_vertical_angle > 0.001:
        raise RuntimeError(
            "The shield rest-pose long axis is not vertical: "
            f"{final_vertical_angle}."
        )
    if after_shield["bounds_center"].z <= 0.0:
        raise RuntimeError(
            "The shield center is not in front of the character after alignment: "
            f"{after_shield['bounds_center']}."
        )
    if abs(final_lateral_gap - target_lateral_gap) > 1e-3:
        raise RuntimeError(
            "The shield lateral gap was not reduced to the approved 50 percent: "
            f"actual={final_lateral_gap}, target={target_lateral_gap}."
        )
    if abs(after_shield["bounds_center"].y - baseline_shield_center.y) > 1e-3:
        raise RuntimeError("The shield height changed during the lateral adjustment.")
    if abs(after_shield["bounds_center"].z - baseline_shield_center.z) > 1e-3:
        raise RuntimeError("The shield front depth changed during the lateral adjustment.")

    if maximum_solved_position_error > 1e-3:
        raise RuntimeError(
            "The baked left-arm mesh differs from the solved shield pose: "
            f"{maximum_solved_position_error}."
        )
    final_right_centroid = sum(
        final_right_positions.values(),
        Vector((0.0, 0.0, 0.0)),
    ) / len(final_right_positions)
    final_right_bounds_min, final_right_bounds_max = position_bounds(
        final_right_positions.values()
    )
    final_right_centerline_x = torso_centerline_x_at_y(
        armature_to_mesh,
        armature_obj,
        final_right_centroid.y,
    )[0]
    final_right_centroid_lateral_gap = abs(
        final_right_centroid.x - final_right_centerline_x
    )
    model_down = Vector((0.0, -1.0, 0.0))
    source_right_upper_down_angle = (
        right_elbow - right_arm_root
    ).angle(model_down)
    source_right_forearm_down_angle = (
        right_hand_head - right_elbow
    ).angle(model_down)

    return {
        "source_outward_normal": [float(value) for value in before_shield["outward_normal"]],
        "target_outward_normal": [0.0, 0.0, 1.0],
        "source_angle_degrees": forward_rotation.angle * 57.29577951308232,
        "final_angle_degrees": final_angle * 57.29577951308232,
        "source_long_axis": [float(value) for value in before_shield["long_axis"]],
        "target_long_axis": [0.0, 1.0, 0.0],
        "source_vertical_angle_degrees": before_shield["long_axis"].angle(model_up) * 57.29577951308232,
        "forward_alignment_degrees": forward_rotation.angle * 57.29577951308232,
        "roll_correction_degrees": roll_angle * 57.29577951308232,
        "final_long_axis": [float(value) for value in after_shield["long_axis"]],
        "final_vertical_angle_degrees": final_vertical_angle * 57.29577951308232,
        "rotation_axis_mesh_local": [float(value) for value in alignment_rotation.axis],
        "bounds_center_correction": [float(value) for value in bounds_center_correction],
        "reach_correction": [float(value) for value in reach_correction],
        "reach_correction_distance": reach_correction.length,
        "torso_centerline_bone": torso_centerline_bone,
        "torso_centerline_bone_head": [float(value) for value in torso_centerline_head],
        "torso_centerline_bone_tail": [float(value) for value in torso_centerline_tail],
        "torso_centerline_x": torso_centerline_x,
        "baseline_shield_center": [float(value) for value in baseline_shield_center],
        "baseline_lateral_gap": baseline_lateral_gap,
        "target_lateral_gap_ratio": 0.5,
        "target_lateral_gap": target_lateral_gap,
        "target_shield_center_x": target_shield_center_x,
        "lateral_center_correction": [float(value) for value in lateral_center_correction],
        "final_lateral_gap": final_lateral_gap,
        "shield_center_before": [float(value) for value in before_shield["bounds_center"]],
        "shield_center_after": [float(value) for value in after_shield["bounds_center"]],
        "shield_center_shift": [float(value) for value in center_shift],
        "shield_bounds_size_before": [float(value) for value in before_shield["bounds_size"]],
        "shield_bounds_size_after": [float(value) for value in after_shield["bounds_size"]],
        "left_hand_head_before": [float(value) for value in hand_head],
        "left_hand_head_target": [float(value) for value in target_hand_head],
        "upper_arm_length": upper_length,
        "forearm_length": forearm_length,
        "minimum_reach": minimum_reach,
        "maximum_reach": maximum_reach,
        "uncorrected_target_distance": uncorrected_target_distance,
        "target_distance": target_distance,
        "connection_error": connection_error,
        "right_arm": {
            "extension_ratio": 1.0,
            "outward_offset": 0.0,
            "target_thigh_clearance": 0.0,
            "source_arm_root": [float(value) for value in right_arm_root],
            "source_elbow": [float(value) for value in right_elbow],
            "source_hand_head": [float(value) for value in right_hand_head],
            "target_elbow": [float(value) for value in right_elbow],
            "target_hand_head": [float(value) for value in right_hand_head],
            "upper_arm_length": (right_elbow - right_arm_root).length,
            "forearm_length": (right_hand_head - right_elbow).length,
            "target_distance": (right_hand_head - right_arm_root).length,
            "target_vertical_distance": 0.0,
            "elbow_bend_offset": 0.0,
            "source_upper_down_angle_degrees": (
                source_right_upper_down_angle * 57.29577951308232
            ),
            "source_forearm_down_angle_degrees": (
                source_right_forearm_down_angle * 57.29577951308232
            ),
            "target_upper_down_angle_degrees": (
                source_right_upper_down_angle * 57.29577951308232
            ),
            "target_forearm_down_angle_degrees": (
                source_right_forearm_down_angle * 57.29577951308232
            ),
            "source_mesh_centroid": [float(value) for value in final_right_centroid],
            "final_mesh_centroid": [float(value) for value in final_right_centroid],
            "source_mesh_bounds_min": [float(value) for value in final_right_bounds_min],
            "source_mesh_bounds_max": [float(value) for value in final_right_bounds_max],
            "final_mesh_bounds_min": [float(value) for value in final_right_bounds_min],
            "final_mesh_bounds_max": [float(value) for value in final_right_bounds_max],
            "source_mesh_centroid_lateral_gap": final_right_centroid_lateral_gap,
            "final_mesh_centroid_lateral_gap": final_right_centroid_lateral_gap,
            "thigh_surface_arm_polygons": right_arm_thigh_separation["arm_polygons"],
            "thigh_surface_polygons": right_arm_thigh_separation["thigh_polygons"],
            "thigh_surface_overlap_count": right_arm_thigh_separation["overlap_count"],
            "thigh_surface_clearance": right_arm_thigh_separation["minimum_clearance"],
            "maximum_baked_mesh_position_error": maximum_solved_position_error,
            "connection_error": 0.0,
        },
        "embedded_animation_frames": [action_start, action_end],
        "embedded_animation_maximum_local_matrix_error": maximum_animation_error,
        "base_pose_source": "Approved embedded action frame 1, preserving its front-of-character shield placement.",
        "method": "Only the approved shield orientation and half-gap torso-front left-arm solve are baked as rest. The right-arm source pose is left unchanged, and the embedded action's local motion channels remain unchanged.",
    }


def main():
    if Path(bpy.data.filepath).resolve() != SOURCE_BLEND.resolve():
        raise RuntimeError("The approved Kursa Blend must be opened before projection export.")

    scene = bpy.context.scene
    scene.frame_set(1)
    bpy.context.view_layer.update()

    armatures = [obj for obj in scene.objects if obj.type == "ARMATURE"]
    if len(armatures) != 1:
        raise RuntimeError(
            f"Expected one approved armature, found {len(armatures)}."
        )

    armature_obj = armatures[0]
    # The approved Blend also contains its review floor. Only the unique mesh
    # deformed by the approved armature is eligible for the runtime FBX.
    mesh_objects = [
        obj
        for obj in scene.objects
        if obj.type == "MESH"
        and any(
            modifier.type == "ARMATURE" and modifier.object == armature_obj
            for modifier in obj.modifiers
        )
    ]
    if len(mesh_objects) != 1:
        raise RuntimeError(
            f"Expected one mesh bound to the approved armature, found {len(mesh_objects)}."
        )
    mesh_obj = mesh_objects[0]
    mesh = mesh_obj.data
    if len(mesh.uv_layers) != 1:
        raise RuntimeError(f"Expected exactly one approved UV0 layer, found {len(mesh.uv_layers)}.")

    before = stable_mesh_signature(mesh)
    before_hash = stable_hash(before)
    before_topology_uv0_hash = stable_hash(
        stable_topology_material_uv0_signature(mesh)
    )
    before_weights_hash = stable_hash(stable_weight_signature(mesh_obj))
    before_positions = [vertex.co.copy() for vertex in mesh.vertices]
    before_bone_matrices = {
        bone.name: bone.matrix_local.copy()
        for bone in armature_obj.data.bones
    }
    before_bone_count = len(armature_obj.data.bones)
    before_action_names = sorted(action.name for action in bpy.data.actions)
    original_uv_name = mesh.uv_layers[0].name

    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_obj = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_obj.to_mesh(
        preserve_all_data_layers=True,
        depsgraph=depsgraph,
    )
    try:
        if len(evaluated_mesh.vertices) != len(mesh.vertices):
            raise RuntimeError("Frame-1 evaluation changed the approved vertex count.")
        if len(evaluated_mesh.polygons) != len(mesh.polygons):
            raise RuntimeError("Frame-1 evaluation changed the approved polygon count.")

        left_layer = mesh.uv_layers.new(name=LEFT_UV)
        right_layer = mesh.uv_layers.new(name=RIGHT_UV)
        depth_layer = mesh.uv_layers.new(name=DEPTH_UV)

        left_inside_loops = 0
        right_inside_loops = 0
        for loop in mesh.loops:
            evaluated_position = evaluated_mesh.vertices[loop.vertex_index].co
            left_uv, left_depth = projection(evaluated_position, LEFT)
            right_uv, right_depth = projection(evaluated_position, RIGHT)
            left_layer.data[loop.index].uv = left_uv
            right_layer.data[loop.index].uv = right_uv
            depth_layer.data[loop.index].uv = (left_depth, right_depth)

            if (
                0.0 <= left_uv[0] <= 1.0
                and 0.0 <= left_uv[1] <= 1.0
                and abs(left_depth) < 1.0
            ):
                left_inside_loops += 1
            if (
                0.0 <= right_uv[0] <= 1.0
                and 0.0 <= right_uv[1] <= 1.0
                and abs(right_depth) < 1.0
            ):
                right_inside_loops += 1
        after_eye_hash = stable_hash(stable_mesh_signature(mesh))
        if before_hash != after_eye_hash:
            raise RuntimeError(
                "Geometry, material assignment, or UV0 changed while adding eye data."
            )
        chin_result = apply_front_chin_alignment(
            mesh_obj,
            armature_obj,
            evaluated_mesh,
        )
    finally:
        evaluated_obj.to_mesh_clear()

    after_hash = after_eye_hash

    expected_layers = [original_uv_name, LEFT_UV, RIGHT_UV, DEPTH_UV]
    actual_layers = [layer.name for layer in mesh.uv_layers]
    if actual_layers != expected_layers:
        raise RuntimeError(f"Unexpected UV layer order: {actual_layers}")
    if left_inside_loops == 0 or right_inside_loops == 0:
        raise RuntimeError(
            f"Approved eye projection has no covered loops: {left_inside_loops}, {right_inside_loops}."
        )

    allowed_left_vertex_indices = pose_vertex_indices(mesh_obj, LEFT_POSE_BONES)
    allowed_right_vertex_indices = pose_vertex_indices(mesh_obj, RIGHT_POSE_BONES)
    allowed_vertex_indices = (
        allowed_left_vertex_indices
        | set(chin_result["selected_vertex_indices"])
    )
    if not allowed_left_vertex_indices:
        raise RuntimeError("No vertices are weighted to the approved left-arm pose bones.")
    if not allowed_right_vertex_indices:
        raise RuntimeError("No vertices are weighted to the approved right-arm pose bones.")

    pose_result = apply_center_preserving_shield_rest_pose(
        scene,
        mesh_obj,
        armature_obj,
    )

    after_pose_topology_uv0_hash = stable_hash(
        stable_topology_material_uv0_signature(mesh)
    )
    after_pose_weights_hash = stable_hash(stable_weight_signature(mesh_obj))
    if before_topology_uv0_hash != after_pose_topology_uv0_hash:
        raise RuntimeError(
            "Topology, material assignment, or approved UV0 changed during the base-pose edit."
        )
    if before_weights_hash != after_pose_weights_hash:
        raise RuntimeError("Skin weights changed during the base-pose edit.")
    if len(armature_obj.data.bones) != before_bone_count:
        raise RuntimeError("Bone count changed during the base-pose edit.")
    if sorted(action.name for action in bpy.data.actions) != before_action_names:
        raise RuntimeError("Embedded action set changed during the base-pose edit.")

    changed_vertex_indices = {
        index
        for index, (old, new) in enumerate(zip(before_positions, mesh.vertices))
        if (old - new.co).length > 1e-5
    }
    unauthorized_changed_vertices = sorted(
        changed_vertex_indices - allowed_vertex_indices
    )
    if unauthorized_changed_vertices:
        raise RuntimeError(
            "Vertices outside the approved left-arm and front-chin regions changed: "
            f"{unauthorized_changed_vertices[:20]}"
        )
    if not changed_vertex_indices:
        raise RuntimeError("The approved left-arm shield-pose edit changed no vertices.")
    changed_left_vertex_indices = (
        changed_vertex_indices & allowed_left_vertex_indices
    )
    changed_right_vertex_indices = (
        changed_vertex_indices & allowed_right_vertex_indices
    )
    changed_chin_vertex_indices = (
        changed_vertex_indices & set(chin_result["selected_vertex_indices"])
    )
    if not changed_left_vertex_indices or changed_right_vertex_indices:
        raise RuntimeError(
            "The shield-pose edit must change the left arm and leave the right arm unchanged."
        )
    if changed_chin_vertex_indices != set(chin_result["selected_vertex_indices"]):
        raise RuntimeError(
            "The front-chin correction did not change exactly its selected vertices."
        )

    changed_bones = sorted(
        bone.name
        for bone in armature_obj.data.bones
        if max(
            abs(bone.matrix_local[row][column] - before_bone_matrices[bone.name][row][column])
            for row in range(4)
            for column in range(4)
        ) > 1e-6
    )
    unexpected_changed_bones = sorted(set(changed_bones) - set(POSE_BONES))
    if unexpected_changed_bones:
        raise RuntimeError(
            f"Bones outside the approved left-arm chain changed: {unexpected_changed_bones}"
        )
    if set(changed_bones) != set(POSE_BONES):
        raise RuntimeError(
            "The approved shield rest pose must change exactly LeftArm, "
            "LeftForeArm, and LeftHand: "
            f"{changed_bones}"
        )

    actual_layers = [layer.name for layer in mesh.uv_layers]
    if actual_layers != expected_layers:
        raise RuntimeError(
            f"The approved eye projection layers changed after the pose edit: {actual_layers}"
        )

    for obj in scene.objects:
        obj.select_set(False)
    armature_obj.select_set(True)
    mesh_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj

    OUTPUT_FBX.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_FBX),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        apply_unit_scale=False,
        apply_scale_options="FBX_SCALE_NONE",
        use_space_transform=True,
        path_mode="COPY",
        embed_textures=True,
    )

    report = {
        "result": "PASS",
        "source_blend": str(SOURCE_BLEND.relative_to(ROOT)).replace("\\", "/"),
        "source_blend_sha256": sha256(SOURCE_BLEND),
        "source_approved_fbx_sha256": sha256(SOURCE_APPROVED_FBX),
        "output_fbx": str(OUTPUT_FBX.relative_to(ROOT)).replace("\\", "/"),
        "output_fbx_sha256": sha256(OUTPUT_FBX),
        "frame": 1,
        "mesh": mesh_obj.name,
        "armature": armature_obj.name,
        "vertices": len(mesh.vertices),
        "polygons": len(mesh.polygons),
        "loops": len(mesh.loops),
        "bones": len(armature_obj.data.bones),
        "actions": len(bpy.data.actions),
        "uv_layers": actual_layers,
        "geometry_material_uv0_hash_before": before_hash,
        "geometry_material_uv0_hash_after_eye_projection": after_hash,
        "geometry_material_uv0_preserved_while_adding_eye_projection": before_hash == after_hash,
        "topology_material_uv0_hash_before": before_topology_uv0_hash,
        "topology_material_uv0_hash_after_pose": after_pose_topology_uv0_hash,
        "topology_material_uv0_preserved_after_pose": before_topology_uv0_hash == after_pose_topology_uv0_hash,
        "skin_weights_hash_before": before_weights_hash,
        "skin_weights_hash_after_pose": after_pose_weights_hash,
        "skin_weights_preserved_after_pose": before_weights_hash == after_pose_weights_hash,
        "allowed_pose_vertices": len(allowed_vertex_indices),
        "allowed_left_arm_vertices": len(allowed_left_vertex_indices),
        "allowed_right_arm_vertices": len(allowed_right_vertex_indices),
        "changed_pose_vertices": len(changed_vertex_indices),
        "changed_left_arm_vertices": len(changed_left_vertex_indices),
        "changed_right_arm_vertices": len(changed_right_vertex_indices),
        "changed_chin_vertices": len(changed_chin_vertex_indices),
        "unauthorized_changed_vertices": len(unauthorized_changed_vertices),
        "changed_rest_bones": changed_bones,
        "base_pose": pose_result,
        "chin_alignment": chin_result,
        "covered_loops": {
            "left": left_inside_loops,
            "right": right_inside_loops,
        },
        "approved_projection": {
            "left": {
                "center": list(LEFT["center"]),
                "surface_normal": list(LEFT["surface_normal"]),
                "size": list(LEFT["size"]),
                "depth": LEFT["depth"],
                "polygon": LEFT["polygon"],
            },
            "right": {
                "center": list(RIGHT["center"]),
                "surface_normal": list(RIGHT["surface_normal"]),
                "size": list(RIGHT["size"]),
                "depth": RIGHT["depth"],
                "polygon": RIGHT["polygon"],
            },
            "projection_normal": list(PROJECTION_NORMAL),
        },
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
