import json
import math
from pathlib import Path

import bpy
from mathutils import Matrix


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample" / "enemies" / "dolore" / "attack_attachment"
SOURCE_PATH = ROOT / "enemies model" / "dolore attack.glb"
BLEND_PATH = SAMPLE_ROOT / "blender" / "Dolore_AttackAttachment_Sample.blend"
EXPORT_PATH = SAMPLE_ROOT / "exports" / "Dolore_AttackAttachment_Sample.glb"
RESULT_JSON = SAMPLE_ROOT / "RIG_INSPECTION.json"
RESULT_TEXT = SAMPLE_ROOT / "RIG_INSPECTION.txt"
TEST_ROTATION_DEGREES = 10.0
MEANINGFUL_DISPLACEMENT_RATIO = 0.00001
MAJOR_DISPLACEMENT_RATIO = 0.001


def evaluated_positions(mesh_object):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh_object.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh()
    points = [evaluated.matrix_world @ vertex.co for vertex in evaluated_mesh.vertices]
    evaluated.to_mesh_clear()
    return points


def inspect_current_scene(label, mesh_object):
    modifier = next((item for item in mesh_object.modifiers if item.type == "ARMATURE"), None)
    armature = modifier.object if modifier is not None else None
    if armature is None:
        return {
            "label": label,
            "rig_present": False,
            "armature_modifier": False,
            "usable_for_body_motion": False,
        }

    bone_names = [bone.name for bone in armature.data.bones]
    bone_name_set = set(bone_names)
    group_names = {group.index: group.name for group in mesh_object.vertex_groups if group.name in bone_name_set}
    weighted_vertex_counts = {name: 0 for name in bone_names}
    maximum_weights = {name: 0.0 for name in bone_names}
    total_weights = []
    influence_counts = []
    for vertex in mesh_object.data.vertices:
        valid_assignments = [item for item in vertex.groups if item.group in group_names and item.weight > 0.0]
        total_weights.append(sum(item.weight for item in valid_assignments))
        influence_counts.append(len(valid_assignments))
        for assignment in valid_assignments:
            bone_name = group_names[assignment.group]
            weighted_vertex_counts[bone_name] += 1
            maximum_weights[bone_name] = max(maximum_weights[bone_name], assignment.weight)

    original_basis = {bone.name: bone.matrix_basis.copy() for bone in armature.pose.bones}
    for pose_bone in armature.pose.bones:
        pose_bone.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    baseline = evaluated_positions(mesh_object)
    minimum = [min(point[axis] for point in baseline) for axis in range(3)]
    maximum = [max(point[axis] for point in baseline) for axis in range(3)]
    diagonal = math.sqrt(sum((maximum[axis] - minimum[axis]) ** 2 for axis in range(3)))

    deformation = []
    for pose_bone in armature.pose.bones:
        for item in armature.pose.bones:
            item.matrix_basis = Matrix.Identity(4)
        pose_bone.rotation_mode = "XYZ"
        pose_bone.rotation_euler.z = math.radians(TEST_ROTATION_DEGREES)
        bpy.context.view_layer.update()
        posed = evaluated_positions(mesh_object)
        displacements = [(after - before).length for before, after in zip(baseline, posed)]
        maximum_displacement = max(displacements)
        displacement_ratio = maximum_displacement / diagonal if diagonal > 0.0 else 0.0
        deformation.append({
            "bone": pose_bone.name,
            "weighted_vertices": weighted_vertex_counts[pose_bone.name],
            "maximum_weight": round(maximum_weights[pose_bone.name], 9),
            "moved_vertices": sum(1 for value in displacements if value > diagonal * 1e-9),
            "maximum_displacement": round(maximum_displacement, 9),
            "maximum_displacement_ratio": round(displacement_ratio, 9),
            "meaningful_deformation": displacement_ratio >= MEANINGFUL_DISPLACEMENT_RATIO,
            "major_deformation": displacement_ratio >= MAJOR_DISPLACEMENT_RATIO,
        })

    for pose_bone in armature.pose.bones:
        pose_bone.matrix_basis = original_basis[pose_bone.name]
    bpy.context.view_layer.update()

    chain = [{"bone": bone.name, "parent": bone.parent.name if bone.parent else None} for bone in armature.data.bones]
    single_chain = all(
        index == 0 and item["parent"] is None or
        index > 0 and item["parent"] == chain[index - 1]["bone"]
        for index, item in enumerate(chain))
    root_locked_meaningful_bones = [
        item["bone"] for item in deformation[1:] if item["meaningful_deformation"]]
    major_deformation_bones = [item["bone"] for item in deformation if item["major_deformation"]]
    unweighted_vertices = sum(1 for value in total_weights if value < 0.999)
    usable = (
        len(armature.data.bones) == 13 and
        single_chain and
        unweighted_vertices == 0 and
        len(root_locked_meaningful_bones) >= 10)
    return {
        "label": label,
        "rig_present": True,
        "armature_modifier": True,
        "armature": armature.name,
        "bone_count": len(armature.data.bones),
        "vertex_group_count": len(group_names),
        "single_connected_chain": single_chain,
        "chain": chain,
        "vertex_count": len(mesh_object.data.vertices),
        "unweighted_vertices": unweighted_vertices,
        "minimum_total_weight": round(min(total_weights), 9),
        "maximum_total_weight": round(max(total_weights), 9),
        "vertices_with_more_than_four_influences": sum(1 for value in influence_counts if value > 4),
        "test_rotation_degrees": TEST_ROTATION_DEGREES,
        "meaningful_deformation_bones": [item["bone"] for item in deformation if item["meaningful_deformation"]],
        "root_locked_meaningful_bones": root_locked_meaningful_bones,
        "major_deformation_bones": major_deformation_bones,
        "terminal_bones_without_direct_weights": [
            item["bone"] for item in deformation[1:] if item["weighted_vertices"] == 0],
        "deformation": deformation,
        "usable_for_body_motion": usable,
        "fine_terminal_articulation_limited": any(
            item["weighted_vertices"] == 0 for item in deformation[-2:]),
    }


def inspect_source():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE_PATH))
    mesh = max((item for item in bpy.context.scene.objects if item.type == "MESH"),
               key=lambda item: len(item.data.vertices))
    return inspect_current_scene("source_glb", mesh)


def inspect_blend():
    bpy.ops.wm.open_mainfile(filepath=str(BLEND_PATH))
    return inspect_current_scene("sample_blend", bpy.data.objects["Dolore_Attack_Tentacle"])


def inspect_export():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(EXPORT_PATH))
    mesh = next(item for item in bpy.context.scene.objects
                if item.type == "MESH" and item.name.startswith("Dolore_Attack_Tentacle"))
    return inspect_current_scene("sample_glb", mesh)


def main():
    inspections = [inspect_source(), inspect_blend(), inspect_export()]
    usable = all(item["usable_for_body_motion"] for item in inspections)
    result = {
        "result": "USABLE" if usable else "RIGGING_REQUIRED",
        "rig_present": all(item["rig_present"] for item in inspections),
        "requires_rerigging_for_body_motion": not usable,
        "fine_terminal_articulation_limited": any(
            item.get("fine_terminal_articulation_limited", True) for item in inspections),
        "summary_ko": (
            "13본 연결 리그와 전체 정점 스킨 웨이트가 있어 촉수 몸통 움직임에 사용할 수 있습니다. "
            "Bone_002와 Bone_001은 직접 웨이트가 없어 못 끝의 세밀한 독립 관절 제어에는 제한이 있습니다."
            if usable else
            "촉수 몸통 움직임에 필요한 리깅 조건을 충족하지 못했습니다."
        ),
        "inspections": inspections,
    }
    RESULT_JSON.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    lines = [
        "Result=" + result["result"],
        "RigPresent=" + str(result["rig_present"]),
        "RequiresReriggingForBodyMotion=" + str(result["requires_rerigging_for_body_motion"]),
        "FineTerminalArticulationLimited=" + str(result["fine_terminal_articulation_limited"]),
        "Summary=" + result["summary_ko"],
    ]
    for inspection in inspections:
        prefix = inspection["label"]
        lines.extend([
            f"{prefix}.BoneCount={inspection['bone_count']}",
            f"{prefix}.SingleConnectedChain={inspection['single_connected_chain']}",
            f"{prefix}.UnweightedVertices={inspection['unweighted_vertices']}",
            f"{prefix}.RootLockedMeaningfulBones={','.join(inspection['root_locked_meaningful_bones'])}",
            f"{prefix}.TerminalBonesWithoutDirectWeights={','.join(inspection['terminal_bones_without_direct_weights'])}",
            f"{prefix}.UsableForBodyMotion={inspection['usable_for_body_motion']}",
        ])
    RESULT_TEXT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))
    if not usable:
        raise RuntimeError("The supplied tentacle rig is not usable for continuous body motion.")


if __name__ == "__main__":
    main()
