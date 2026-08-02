import json
from collections import defaultdict
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
SOURCE_BLEND = (
    ROOT
    / "artSample"
    / "enemies"
    / "kursa"
    / "appearance_reference_sync"
    / "blender"
    / "Kursa_Appearance_ReferenceSync.blend"
)
REPORT = (
    ROOT
    / "docs"
    / "validation"
    / "kursa_approved_appearance_2026-08-02"
    / "Kursa_ShieldBasePose_Analysis.json"
)
SHIELD_MATERIALS = {
    "Kursa_Shield_Worn_Gunmetal",
    "Kursa_Shield_Frame_Steel",
}
LEFT_ARM_BONES = ("LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand")


def values(vector):
    return [round(float(value), 8) for value in vector]


def mesh_and_armature(scene):
    armatures = [obj for obj in scene.objects if obj.type == "ARMATURE"]
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one armature, found {len(armatures)}.")
    armature = armatures[0]
    meshes = [
        obj
        for obj in scene.objects
        if obj.type == "MESH"
        and any(
            modifier.type == "ARMATURE" and modifier.object == armature
            for modifier in obj.modifiers
        )
    ]
    if len(meshes) != 1:
        raise RuntimeError(f"Expected one skinned mesh, found {len(meshes)}.")
    return meshes[0], armature


def cluster_normals(polygons, threshold=0.985):
    clusters = []
    for polygon in sorted(polygons, key=lambda item: item.area, reverse=True):
        normal = polygon.normal.normalized()
        match = None
        for cluster in clusters:
            if normal.dot(cluster["normal"].normalized()) >= threshold:
                match = cluster
                break
        if match is None:
            match = {
                "normal": Vector((0.0, 0.0, 0.0)),
                "weighted_center": Vector((0.0, 0.0, 0.0)),
                "area": 0.0,
                "polygon_indices": [],
            }
            clusters.append(match)
        match["normal"] += normal * polygon.area
        match["weighted_center"] += polygon.center * polygon.area
        match["area"] += polygon.area
        match["polygon_indices"].append(polygon.index)

    result = []
    for cluster in clusters:
        area = cluster["area"]
        normal = cluster["normal"].normalized()
        result.append(
            {
                "normal": values(normal),
                "center": values(cluster["weighted_center"] / area),
                "area": round(area, 8),
                "polygon_count": len(cluster["polygon_indices"]),
                "polygon_indices": cluster["polygon_indices"],
                "angle_to_model_forward_degrees": round(
                    normal.angle(Vector((0.0, 0.0, 1.0))) * 57.29577951308232,
                    6,
                ),
                "angle_to_model_back_degrees": round(
                    normal.angle(Vector((0.0, 0.0, -1.0))) * 57.29577951308232,
                    6,
                ),
            }
        )
    return sorted(result, key=lambda item: item["area"], reverse=True)


def main():
    if Path(bpy.data.filepath).resolve() != SOURCE_BLEND.resolve():
        raise RuntimeError("The approved Kursa Blend was not opened.")

    scene = bpy.context.scene
    scene.frame_set(1)
    bpy.context.view_layer.update()
    mesh_obj, armature_obj = mesh_and_armature(scene)
    material_names = [slot.material.name for slot in mesh_obj.material_slots]
    shield_material_indices = {
        index
        for index, name in enumerate(material_names)
        if name in SHIELD_MATERIALS
    }
    if len(shield_material_indices) != 2:
        raise RuntimeError(
            f"Expected two shield materials, found {shield_material_indices}."
        )

    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_obj = mesh_obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_obj.to_mesh(
        preserve_all_data_layers=True,
        depsgraph=depsgraph,
    )
    try:
        shield_polygons = [
            polygon
            for polygon in evaluated_mesh.polygons
            if polygon.material_index in shield_material_indices
        ]
        worn_index = material_names.index("Kursa_Shield_Worn_Gunmetal")
        worn_polygons = [
            polygon
            for polygon in evaluated_mesh.polygons
            if polygon.material_index == worn_index
        ]
        if not shield_polygons or not worn_polygons:
            raise RuntimeError("Shield surface polygons were not found.")

        shield_vertices = sorted(
            {
                vertex_index
                for polygon in shield_polygons
                for vertex_index in polygon.vertices
            }
        )
        minimum = Vector((float("inf"), float("inf"), float("inf")))
        maximum = Vector((float("-inf"), float("-inf"), float("-inf")))
        for vertex_index in shield_vertices:
            coordinate = evaluated_mesh.vertices[vertex_index].co
            minimum.x = min(minimum.x, coordinate.x)
            minimum.y = min(minimum.y, coordinate.y)
            minimum.z = min(minimum.z, coordinate.z)
            maximum.x = max(maximum.x, coordinate.x)
            maximum.y = max(maximum.y, coordinate.y)
            maximum.z = max(maximum.z, coordinate.z)

        weight_totals = defaultdict(float)
        for vertex_index in shield_vertices:
            for group in mesh_obj.data.vertices[vertex_index].groups:
                weight_totals[mesh_obj.vertex_groups[group.group].name] += group.weight

        armature_to_mesh = mesh_obj.matrix_world.inverted() @ armature_obj.matrix_world
        bones = {}
        for name in LEFT_ARM_BONES:
            pose_bone = armature_obj.pose.bones.get(name)
            rest_bone = armature_obj.data.bones.get(name)
            if pose_bone is None or rest_bone is None:
                raise RuntimeError(f"Required left-arm bone is missing: {name}")
            bones[name] = {
                "parent": pose_bone.parent.name if pose_bone.parent else None,
                "pose_head_mesh_local": values(armature_to_mesh @ pose_bone.head),
                "pose_tail_mesh_local": values(armature_to_mesh @ pose_bone.tail),
                "pose_matrix": [
                    [round(float(value), 8) for value in row]
                    for row in pose_bone.matrix
                ],
                "rest_head_armature_local": values(rest_bone.head_local),
                "rest_tail_armature_local": values(rest_bone.tail_local),
            }

        normal_clusters = cluster_normals(worn_polygons)
        outward_cluster = max(
            (
                cluster
                for cluster in normal_clusters
                if cluster["normal"][2] > 0.0
            ),
            key=lambda cluster: cluster["area"],
        )
        outward_normal = Vector(outward_cluster["normal"]).normalized()
        model_forward = Vector((0.0, 0.0, 1.0))
        alignment_rotation = outward_normal.rotation_difference(model_forward)
        shoulder_pivot = armature_to_mesh @ armature_obj.pose.bones[
            "LeftShoulder"
        ].head
        aligned_positions = [
            shoulder_pivot
            + alignment_rotation
            @ (evaluated_mesh.vertices[index].co - shoulder_pivot)
            for index in shield_vertices
        ]
        aligned_minimum = Vector((float("inf"), float("inf"), float("inf")))
        aligned_maximum = Vector(
            (float("-inf"), float("-inf"), float("-inf"))
        )
        for coordinate in aligned_positions:
            aligned_minimum.x = min(aligned_minimum.x, coordinate.x)
            aligned_minimum.y = min(aligned_minimum.y, coordinate.y)
            aligned_minimum.z = min(aligned_minimum.z, coordinate.z)
            aligned_maximum.x = max(aligned_maximum.x, coordinate.x)
            aligned_maximum.y = max(aligned_maximum.y, coordinate.y)
            aligned_maximum.z = max(aligned_maximum.z, coordinate.z)

        aligned_hand_head = shoulder_pivot + alignment_rotation @ (
            (armature_to_mesh @ armature_obj.pose.bones["LeftHand"].head)
            - shoulder_pivot
        )
        shield_center = (minimum + maximum) * 0.5
        current_hand_head = (
            armature_to_mesh @ armature_obj.pose.bones["LeftHand"].head
        )
        center_pivot_hand_head = shield_center + alignment_rotation @ (
            current_hand_head - shield_center
        )
        arm_root = armature_to_mesh @ armature_obj.pose.bones["LeftArm"].head
        upper_length = (
            (armature_to_mesh @ armature_obj.pose.bones["LeftForeArm"].head)
            - arm_root
        ).length
        forearm_length = (current_hand_head - (
            armature_to_mesh @ armature_obj.pose.bones["LeftForeArm"].head
        )).length
        target_distance = (center_pivot_hand_head - arm_root).length
        report = {
            "result": "PASS",
            "source_blend": str(SOURCE_BLEND.relative_to(ROOT)).replace("\\", "/"),
            "frame": 1,
            "model_axes": {
                "forward": [0.0, 0.0, 1.0],
                "up": [0.0, 1.0, 0.0],
                "right": [1.0, 0.0, 0.0],
                "basis": "Approved sample front-facing material masks use local +Z.",
            },
            "mesh": mesh_obj.name,
            "armature": armature_obj.name,
            "material_order": material_names,
            "shield_material_indices": sorted(shield_material_indices),
            "shield_vertex_count": len(shield_vertices),
            "shield_bounds": {
                "min": values(minimum),
                "max": values(maximum),
                "center": values((minimum + maximum) * 0.5),
                "size": values(maximum - minimum),
            },
            "shield_dominant_vertex_groups": [
                {"name": name, "summed_weight": round(weight, 8)}
                for name, weight in sorted(
                    weight_totals.items(),
                    key=lambda item: item[1],
                    reverse=True,
                )[:12]
            ],
            "worn_surface_normal_clusters": normal_clusters,
            "proposed_rigid_left_arm_alignment": {
                "method": "Rotate the LeftShoulder pose globally so the entire descendant arm and shield move rigidly.",
                "pivot_mesh_local": values(shoulder_pivot),
                "source_outward_normal": values(outward_normal),
                "target_outward_normal": values(model_forward),
                "rotation_axis_mesh_local": values(alignment_rotation.axis),
                "rotation_degrees": round(
                    alignment_rotation.angle * 57.29577951308232,
                    6,
                ),
                "aligned_normal": values(alignment_rotation @ outward_normal),
                "shield_bounds_after": {
                    "min": values(aligned_minimum),
                    "max": values(aligned_maximum),
                    "center": values((aligned_minimum + aligned_maximum) * 0.5),
                    "size": values(aligned_maximum - aligned_minimum),
                },
                "left_hand_head_after_mesh_local": values(aligned_hand_head),
            },
            "proposed_center_preserving_ik_alignment": {
                "method": "Rotate the hand and shield rigidly around the shield center, then solve LeftArm and LeftForeArm to the transformed hand head.",
                "shield_center_preserved_mesh_local": values(shield_center),
                "left_hand_head_before_mesh_local": values(current_hand_head),
                "left_hand_head_target_mesh_local": values(
                    center_pivot_hand_head
                ),
                "left_arm_root_mesh_local": values(arm_root),
                "upper_arm_length": round(upper_length, 8),
                "forearm_length": round(forearm_length, 8),
                "target_distance": round(target_distance, 8),
                "minimum_reach": round(abs(upper_length - forearm_length), 8),
                "maximum_reach": round(upper_length + forearm_length, 8),
                "reachable": (
                    abs(upper_length - forearm_length)
                    <= target_distance
                    <= upper_length + forearm_length
                ),
            },
            "left_arm_bones": bones,
        }
    finally:
        evaluated_obj.to_mesh_clear()

    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
