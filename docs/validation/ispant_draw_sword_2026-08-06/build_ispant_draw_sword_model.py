import argparse
import hashlib
import json
import sys
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector


MUSKET_COMPONENTS = {41, 75, 76}
DRAWN_SWORD_COMPONENTS = {78, 79}
SHEATH_COMPONENTS = {77, 80}
SWORD_COMPONENTS = DRAWN_SWORD_COMPONENTS | SHEATH_COMPONENTS
WEAPON_COMPONENTS = MUSKET_COMPONENTS | SWORD_COMPONENTS
HANDLE_COMPONENT = 79


def parse_args():
    arguments = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--static", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--report", required=True)
    return parser.parse_args(arguments)


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def connected_components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        first, second = edge.vertices
        adjacency[first].add(second)
        adjacency[second].add(first)
    pending = set(range(len(mesh.vertices)))
    result = []
    while pending:
        seed = min(pending)
        pending.remove(seed)
        stack = [seed]
        vertices = []
        while stack:
            current = stack.pop()
            vertices.append(current)
            for neighbour in adjacency[current]:
                if neighbour in pending:
                    pending.remove(neighbour)
                    stack.append(neighbour)
        result.append(set(vertices))
    return result


def delete_vertices(obj, vertex_indices):
    editable = bmesh.new()
    editable.from_mesh(obj.data)
    editable.verts.ensure_lookup_table()
    bmesh.ops.delete(
        editable,
        geom=[editable.verts[index] for index in sorted(vertex_indices)],
        context="VERTS",
    )
    editable.to_mesh(obj.data)
    editable.free()
    obj.data.update()


def keep_components(source, components, selected, name):
    result = source.copy()
    result.data = source.data.copy()
    result.name = name
    result.data.name = name + "_Mesh"
    bpy.context.collection.objects.link(result)
    keep = set().union(*(components[index] for index in sorted(selected)))
    remove = set(range(len(result.data.vertices))) - keep
    delete_vertices(result, remove)
    for modifier in list(result.modifiers):
        result.modifiers.remove(modifier)
    result.vertex_groups.clear()
    return result


def mesh_world_vertices(obj):
    return [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]


def maximum_index_error(first, second):
    if len(first) != len(second):
        return float("inf")
    return max((first[index] - second[index]).length for index in range(len(first)))


def rigid_fit(source_points, destination_points):
    source = np.array([tuple(point) for point in source_points], dtype=np.float64)
    destination = np.array([tuple(point) for point in destination_points], dtype=np.float64)
    source_center = source.mean(axis=0)
    destination_center = destination.mean(axis=0)
    covariance = (source - source_center).T @ (destination - destination_center)
    first, _, second_transpose = np.linalg.svd(covariance)
    rotation = second_transpose.T @ first.T
    if np.linalg.det(rotation) < 0.0:
        second_transpose[-1, :] *= -1.0
        rotation = second_transpose.T @ first.T
    translation = destination_center - rotation @ source_center
    matrix = Matrix(
        (
            (rotation[0, 0], rotation[0, 1], rotation[0, 2], translation[0]),
            (rotation[1, 0], rotation[1, 1], rotation[1, 2], translation[1]),
            (rotation[2, 0], rotation[2, 1], rotation[2, 2], translation[2]),
            (0.0, 0.0, 0.0, 1.0),
        )
    )
    fitted = (rotation @ source.T).T + translation
    errors = np.linalg.norm(fitted - destination, axis=1)
    return matrix, float(errors.max()), float(np.sqrt(np.mean(errors * errors)))


def triangle_count(obj):
    return sum(len(polygon.vertices) - 2 for polygon in obj.data.polygons)


def main():
    args = parse_args()
    source_path = Path(args.source).resolve()
    static_path = Path(args.static).resolve()
    output_path = Path(args.output).resolve()
    report_path = Path(args.report).resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source_path))
    armature = bpy.data.objects.get("Armature")
    body = bpy.data.objects.get("Ispant_Armed_Body")
    crescent = bpy.data.objects.get("Ispant_Crescent_Ornament")
    eyes = bpy.data.objects.get("Ispant_Reference_Eye_Slits")
    if not armature or armature.type != "ARMATURE" or not body or not crescent or not eyes:
        raise RuntimeError("The supplied Ispant draw-sword FBX structure differs.")
    if len(armature.data.bones) != 33:
        raise RuntimeError("The supplied draw-sword FBX must contain 33 Mixamo bones.")
    actions = list(bpy.data.actions)
    if len(actions) != 1 or actions[0].name != "Armature|mixamo.com|Layer0":
        raise RuntimeError(
            "Expected exactly one Mixamo action, got {}".format(
                [action.name for action in actions]
            )
        )
    if tuple(round(value, 4) for value in actions[0].frame_range) != (1.0, 46.0):
        raise RuntimeError("The Mixamo draw-sword action frame range differs.")

    components = connected_components(body.data)
    if len(components) != 81:
        raise RuntimeError("Expected 81 draw-sword body components.")
    source_world_vertices = mesh_world_vertices(body)

    existing_objects = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(static_path))
    static_objects = [obj for obj in bpy.context.scene.objects if obj not in existing_objects]
    static_body = next(
        (
            obj
            for obj in static_objects
            if obj.type == "MESH" and obj.name.startswith("Ispant_Armed_Body")
        ),
        None,
    )
    if static_body is None:
        raise RuntimeError("The static Ispant body was not found.")
    geometry_error = maximum_index_error(
        source_world_vertices,
        mesh_world_vertices(static_body),
    )
    if geometry_error > 0.000002:
        raise RuntimeError(
            "Draw-sword/static body geometry differs by {}".format(geometry_error)
        )
    for obj in static_objects:
        bpy.data.objects.remove(obj, do_unlink=True)

    if armature.animation_data is None:
        armature.animation_data_create()
    armature.animation_data.action = actions[0]
    armature.data.pose_position = "POSE"
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 46
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()

    evaluated_body = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    evaluated_mesh = evaluated_body.to_mesh()
    handle_vertices = sorted(components[HANDLE_COMPONENT])
    try:
        handle_source_world = [
            body.matrix_world @ body.data.vertices[index].co
            for index in handle_vertices
        ]
        handle_evaluated_world = [
            evaluated_body.matrix_world @ evaluated_mesh.vertices[index].co
            for index in handle_vertices
        ]
        grip_transform, handle_fit_max_error, handle_fit_rms_error = rigid_fit(
            handle_source_world,
            handle_evaluated_world,
        )
    finally:
        evaluated_body.to_mesh_clear()

    armature.data.pose_position = "REST"
    bpy.context.view_layer.update()
    rigid_musket = keep_components(
        body,
        components,
        MUSKET_COMPONENTS,
        "Ispant_DrawSword_RigidMusket",
    )
    musket_world_at_rest = rigid_musket.matrix_world.copy()
    rigid_musket.parent = armature
    rigid_musket.parent_type = "BONE"
    rigid_musket.parent_bone = "mixamorig:Spine2"
    rigid_musket.matrix_world = musket_world_at_rest
    rigid_sheath = keep_components(
        body,
        components,
        SHEATH_COMPONENTS,
        "Ispant_DrawSword_RigidSheath",
    )
    sheath_world_at_rest = rigid_sheath.matrix_world.copy()
    rigid_sheath.parent = armature
    rigid_sheath.parent_type = "BONE"
    rigid_sheath.parent_bone = "mixamorig:Hips"
    rigid_sheath.matrix_world = sheath_world_at_rest

    armature.data.pose_position = "POSE"
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    rigid_sword = keep_components(
        body,
        components,
        DRAWN_SWORD_COMPONENTS,
        "Ispant_DrawSword_RigidSword",
    )
    rigid_sword.matrix_world = grip_transform @ rigid_sword.matrix_world
    sword_world_at_grip = rigid_sword.matrix_world.copy()
    rigid_sword.parent = armature
    rigid_sword.parent_type = "BONE"
    rigid_sword.parent_bone = "mixamorig:RightHand"
    rigid_sword.matrix_world = sword_world_at_grip

    delete_vertices(
        body,
        set().union(*(components[index] for index in sorted(WEAPON_COMPONENTS))),
    )
    body.name = "Ispant_Armed_Body"
    body.data.name = "Ispant_Armed_Body_AnimatedWithoutWeapons"

    export_objects = [
        armature,
        body,
        crescent,
        eyes,
        rigid_musket,
        rigid_sheath,
        rigid_sword,
    ]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in export_objects:
        obj.hide_set(False)
        obj.hide_render = False
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        use_armature_deform_only=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode="AUTO",
    )

    report = {
        "result": "PASS",
        "source": str(source_path),
        "source_sha256": sha256(source_path),
        "static_verification_source": str(static_path),
        "static_sha256": sha256(static_path),
        "output": str(output_path),
        "output_sha256": sha256(output_path),
        "source_action": actions[0].name,
        "source_frame_range": [1, 46],
        "source_bones": 33,
        "source_static_max_world_vertex_error": geometry_error,
        "body_components": 81,
        "musket_components": sorted(MUSKET_COMPONENTS),
        "drawn_sword_components": sorted(DRAWN_SWORD_COMPONENTS),
        "sheath_components": sorted(SHEATH_COMPONENTS),
        "handle_component": HANDLE_COMPONENT,
        "handle_fit_max_error": handle_fit_max_error,
        "handle_fit_rms_error": handle_fit_rms_error,
        "animated_body_vertices_after_split": len(body.data.vertices),
        "animated_body_triangles_after_split": triangle_count(body),
        "rigid_musket_vertices": len(rigid_musket.data.vertices),
        "rigid_musket_triangles": triangle_count(rigid_musket),
        "rigid_musket_parent_bone": rigid_musket.parent_bone,
        "rigid_musket_has_armature_modifier": any(
            modifier.type == "ARMATURE" for modifier in rigid_musket.modifiers
        ),
        "rigid_musket_has_vertex_groups": bool(rigid_musket.vertex_groups),
        "rigid_sheath_vertices": len(rigid_sheath.data.vertices),
        "rigid_sheath_triangles": triangle_count(rigid_sheath),
        "rigid_sheath_parent_bone": rigid_sheath.parent_bone,
        "rigid_sheath_has_armature_modifier": any(
            modifier.type == "ARMATURE" for modifier in rigid_sheath.modifiers
        ),
        "rigid_sheath_has_vertex_groups": bool(rigid_sheath.vertex_groups),
        "rigid_sword_vertices": len(rigid_sword.data.vertices),
        "rigid_sword_triangles": triangle_count(rigid_sword),
        "rigid_sword_parent_bone": rigid_sword.parent_bone,
        "rigid_sword_has_armature_modifier": any(
            modifier.type == "ARMATURE" for modifier in rigid_sword.modifiers
        ),
        "rigid_sword_has_vertex_groups": bool(rigid_sword.vertex_groups),
    }
    report_path.write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
