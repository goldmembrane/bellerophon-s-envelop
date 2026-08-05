import argparse
import hashlib
import json
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


MUSKET_COMPONENTS = {41, 75, 76}
SWORD_COMPONENTS = {77, 78, 79, 80}
WEAPON_COMPONENTS = MUSKET_COMPONENTS | SWORD_COMPONENTS


def parse_args():
    arguments = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--walking", required=True)
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
            for neighbor in adjacency[current]:
                if neighbor in pending:
                    pending.remove(neighbor)
                    stack.append(neighbor)
        result.append(set(vertices))
    return result


def delete_vertices(obj, vertex_indices):
    mesh = obj.data
    editable = bmesh.new()
    editable.from_mesh(mesh)
    editable.verts.ensure_lookup_table()
    bmesh.ops.delete(
        editable,
        geom=[editable.verts[index] for index in sorted(vertex_indices)],
        context="VERTS")
    editable.to_mesh(mesh)
    editable.free()
    mesh.update()


def keep_components(
        source,
        component_sets,
        selected_components,
        name,
        armature,
        parent_bone):
    result = source.copy()
    result.data = source.data.copy()
    result.name = name
    result.data.name = name + "_Mesh"
    bpy.context.collection.objects.link(result)
    keep = set().union(*(component_sets[index] for index in sorted(selected_components)))
    remove = set(range(len(result.data.vertices))) - keep
    delete_vertices(result, remove)
    for modifier in list(result.modifiers):
        result.modifiers.remove(modifier)
    result.vertex_groups.clear()
    world_matrix = result.matrix_world.copy()
    result.parent = armature
    result.parent_type = "BONE"
    result.parent_bone = parent_bone
    result.matrix_world = world_matrix
    return result


def material_face_counts(obj, component_sets, selected_components):
    selected_vertices = set().union(
        *(component_sets[index] for index in sorted(selected_components)))
    counts = {}
    for polygon in obj.data.polygons:
        if all(vertex in selected_vertices for vertex in polygon.vertices):
            material = obj.material_slots[polygon.material_index].material
            name = material.name if material else "<null>"
            counts[name] = counts.get(name, 0) + 1
    return counts


def mesh_world_vertices(obj):
    return [tuple(obj.matrix_world @ vertex.co) for vertex in obj.data.vertices]


def maximum_index_error(first, second):
    if len(first) != len(second):
        return float("inf")
    return max(
        (Vector(first[index]) - Vector(second[index])).length
        for index in range(len(first)))


def main():
    args = parse_args()
    walking_path = Path(args.walking).resolve()
    static_path = Path(args.static).resolve()
    output_path = Path(args.output).resolve()
    report_path = Path(args.report).resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(walking_path))
    armature = bpy.data.objects.get("Armature")
    body = bpy.data.objects.get("Ispant_Armed_Body")
    crescent = bpy.data.objects.get("Ispant_Crescent_Ornament")
    eyes = bpy.data.objects.get("Ispant_Reference_Eye_Slits")
    if not armature or armature.type != "ARMATURE" or not body or not crescent or not eyes:
        raise RuntimeError("The supplied Ispant walking FBX structure differs.")
    if len(armature.data.bones) != 33:
        raise RuntimeError(f"Expected 33 Mixamo bones, got {len(armature.data.bones)}")
    actions = list(bpy.data.actions)
    if len(actions) != 1 or actions[0].name != "Armature|mixamo.com|Layer0":
        raise RuntimeError(f"Expected one Mixamo action, got {[action.name for action in actions]}")
    if tuple(round(value, 4) for value in actions[0].frame_range) != (1.0, 62.0):
        raise RuntimeError(f"Mixamo action frame range differs: {tuple(actions[0].frame_range)}")

    walking_components = connected_components(body.data)
    if len(walking_components) != 81:
        raise RuntimeError(f"Expected 81 walking body components, got {len(walking_components)}")
    walking_world_vertices = mesh_world_vertices(body)
    for obj in list(bpy.context.scene.objects):
        if obj.name == "Ispant_Static" and obj.type == "EMPTY":
            bpy.data.objects.remove(obj, do_unlink=True)

    existing_objects = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(static_path))
    static_objects = [obj for obj in bpy.context.scene.objects if obj not in existing_objects]
    static_body = next(
        (obj for obj in static_objects if obj.type == "MESH" and obj.name.startswith("Ispant_Armed_Body")),
        None)
    if static_body is None:
        raise RuntimeError("The static Ispant body was not found.")
    static_components = connected_components(static_body.data)
    if len(static_components) != 81:
        raise RuntimeError(f"Expected 81 static body components, got {len(static_components)}")
    geometry_error = maximum_index_error(walking_world_vertices, mesh_world_vertices(static_body))
    if geometry_error > 0.000002:
        raise RuntimeError(f"Walking/static body geometry differs by {geometry_error}")

    musket_face_counts = material_face_counts(
        static_body, static_components, MUSKET_COMPONENTS)
    sword_face_counts = material_face_counts(
        static_body, static_components, SWORD_COMPONENTS)
    armature.data.pose_position = "REST"
    bpy.context.view_layer.update()
    fixed_musket = keep_components(
        static_body, static_components, MUSKET_COMPONENTS,
        "Ispant_Fixed_Musket", armature, "mixamorig:Spine2")
    fixed_sword = keep_components(
        static_body, static_components, SWORD_COMPONENTS,
        "Ispant_Fixed_Sword", armature, "mixamorig:Hips")
    delete_vertices(
        body,
        set().union(*(walking_components[index] for index in sorted(WEAPON_COMPONENTS))))
    body.name = "Ispant_Armed_Body"
    body.data.name = "Ispant_Armed_Body_AnimatedWithoutWeapons"

    for obj in static_objects:
        if obj not in {fixed_musket, fixed_sword} and obj.name in bpy.data.objects:
            bpy.data.objects.remove(obj, do_unlink=True)

    if armature.animation_data is None:
        armature.animation_data_create()
    armature.animation_data.action = actions[0]
    armature.data.pose_position = "POSE"
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 62
    bpy.context.scene.frame_set(1)

    export_objects = [armature, body, crescent, eyes, fixed_musket, fixed_sword]
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
        path_mode="AUTO")

    report = {
        "result": "PASS",
        "walking_source": str(walking_path),
        "walking_sha256": sha256(walking_path),
        "static_source": str(static_path),
        "static_sha256": sha256(static_path),
        "output": str(output_path),
        "output_sha256": sha256(output_path),
        "source_action": actions[0].name,
        "source_frame_range": [1, 62],
        "source_bones": 33,
        "walking_static_max_world_vertex_error": geometry_error,
        "body_components": 81,
        "musket_components": sorted(MUSKET_COMPONENTS),
        "sword_components": sorted(SWORD_COMPONENTS),
        "musket_material_faces": musket_face_counts,
        "sword_material_faces": sword_face_counts,
        "animated_body_vertices_after_split": len(body.data.vertices),
        "fixed_musket_vertices": len(fixed_musket.data.vertices),
        "fixed_sword_vertices": len(fixed_sword.data.vertices),
        "fixed_musket_parent_bone": fixed_musket.parent_bone,
        "fixed_sword_parent_bone": fixed_sword.parent_bone,
        "weapons_have_armature_modifier": any(
            modifier.type == "ARMATURE"
            for obj in (fixed_musket, fixed_sword)
            for modifier in obj.modifiers),
        "weapons_have_vertex_groups": bool(
            fixed_musket.vertex_groups or fixed_sword.vertex_groups),
    }
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
