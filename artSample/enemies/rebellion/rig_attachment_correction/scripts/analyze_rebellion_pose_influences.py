import bpy
import json
import math
from mathutils import Quaternion, Vector
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[5]
SOURCE_GLB = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "appearance_reference_sync"
    / "source"
    / "Rebellion_Unity_Source_Unmodified.glb"
)
OUTPUT_PATH = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "rig_attachment_correction"
    / "analysis"
    / "pose_influence_analysis.json"
)
TARGET_BONES = (
    "Bone_002",
    "Bone_008",
    "Bone_007",
    "Bone_006",
    "Bone_023",
    "Bone_017",
)
DIAGNOSTIC_ANGLE_DEGREES = 15.0


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def evaluated_positions(mesh_object):
    dependency_graph = bpy.context.evaluated_depsgraph_get()
    evaluated_object = mesh_object.evaluated_get(dependency_graph)
    evaluated_mesh = evaluated_object.to_mesh()
    try:
        return [
            evaluated_object.matrix_world @ vertex.co
            for vertex in evaluated_mesh.vertices
        ]
    finally:
        evaluated_object.to_mesh_clear()


def analyze_pose(armature, mesh_object, bone_name, rest_positions):
    pose_bone = armature.pose.bones[bone_name]
    pose_bone.rotation_mode = "QUATERNION"
    original_basis = pose_bone.matrix_basis.copy()
    try:
        pose_bone.rotation_quaternion = Quaternion(
            (1.0, 0.0, 0.0),
            math.radians(DIAGNOSTIC_ANGLE_DEGREES),
        )
        bpy.context.view_layer.update()
        posed_positions = evaluated_positions(mesh_object)
    finally:
        pose_bone.matrix_basis = original_basis
        bpy.context.view_layer.update()

    affected = []
    maximum_displacement = 0.0
    for index, (rest, posed) in enumerate(zip(rest_positions, posed_positions)):
        displacement = (posed - rest).length
        maximum_displacement = max(maximum_displacement, displacement)
        if displacement > 0.00001:
            affected.append((index, rest, displacement))

    if not affected:
        return {
            "affected_vertices": 0,
            "maximum_displacement": maximum_displacement,
            "rest_bounds_min": None,
            "rest_bounds_max": None,
            "rest_centroid": None,
        }

    minimum = Vector(
        (
            min(item[1].x for item in affected),
            min(item[1].y for item in affected),
            min(item[1].z for item in affected),
        )
    )
    maximum = Vector(
        (
            max(item[1].x for item in affected),
            max(item[1].y for item in affected),
            max(item[1].z for item in affected),
        )
    )
    centroid = sum((item[1] for item in affected), Vector()) / len(affected)
    return {
        "affected_vertices": len(affected),
        "maximum_displacement": maximum_displacement,
        "rest_bounds_min": list(minimum),
        "rest_bounds_max": list(maximum),
        "rest_centroid": list(centroid),
    }


def main():
    clear_scene()
    bpy.ops.import_scene.gltf(filepath=str(SOURCE_GLB))
    armature = next(
        obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"
    )
    mesh_object = next(
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
        and obj.vertex_groups
        and any(modifier.type == "ARMATURE" for modifier in obj.modifiers)
    )
    rest_positions = evaluated_positions(mesh_object)
    results = {
        bone_name: analyze_pose(
            armature,
            mesh_object,
            bone_name,
            rest_positions,
        )
        for bone_name in TARGET_BONES
    }
    output = {
        "source": str(SOURCE_GLB.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "diagnostic_rotation_degrees": DIAGNOSTIC_ANGLE_DEGREES,
        "diagnostic_local_axis": "X",
        "results": results,
    }
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(
        json.dumps(output, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "result": "PASS",
                "output": str(OUTPUT_PATH.relative_to(PROJECT_ROOT)).replace("\\", "/"),
                "results": results,
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
