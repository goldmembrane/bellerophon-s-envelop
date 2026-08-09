import argparse
import sys
from pathlib import Path

import bmesh
import bpy


ARMATURE_NAME = "Armature"
BODY_NAME = "Ispant_Armed_Body"
CRESCENT_NAME = "Ispant_Crescent_Ornament"
EYES_NAME = "Ispant_Reference_Eye_Slits"
SOURCE_SWORD_NAME = "Ispant_ApprovedLongSword"
RIGID_MUSKET_NAME = "Ispant_ChangeToRifle_RigidMusket"
SPINE_BONE_NAME = "mixamorig:Spine2"
MUSKET_COMPONENTS = {41, 75, 76}
EXPECTED_BONES = 33
EXPECTED_SOURCE_BODY_TRIANGLES = 3518
EXPECTED_ANIMATED_BODY_TRIANGLES = 3364
EXPECTED_MUSKET_TRIANGLES = 154
EXPECTED_CRESCENT_TRIANGLES = 1253
EXPECTED_EYE_TRIANGLES = 312
EXPECTED_ACTION_FRAMES = (1.0, 213.0)
GEOMETRY_TOLERANCE = 0.000001
STATIC_GEOMETRY_NAMES = (BODY_NAME, CRESCENT_NAME, EYES_NAME)


def arguments():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--static-source", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def import_fbx(path, use_anim):
    bpy.ops.import_scene.fbx(
        filepath=str(path),
        use_anim=use_anim,
        ignore_leaf_bones=False,
    )


def world_vertices(obj):
    return [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]


def capture_source_geometry(source):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    import_fbx(source, use_anim=True)
    armature = bpy.data.objects.get(ARMATURE_NAME)
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("The supplied change-to-rifle Mixamo armature is missing.")
    armature.data.pose_position = "REST"
    bpy.context.view_layer.update()
    result = {}
    for name in STATIC_GEOMETRY_NAMES:
        obj = bpy.data.objects.get(name)
        if obj is None or obj.type != "MESH":
            raise RuntimeError(f"Required source geometry is missing: {name}")
        result[name] = [vertex.copy() for vertex in world_vertices(obj)]
    return result


def compare_static_geometry(source_geometry, static_source):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    import_fbx(static_source, use_anim=False)
    maximum_error = 0.0
    for name in STATIC_GEOMETRY_NAMES:
        obj = bpy.data.objects.get(name)
        if obj is None or obj.type != "MESH":
            raise RuntimeError(f"Required static geometry is missing: {name}")
        actual = world_vertices(obj)
        expected = source_geometry[name]
        if len(actual) != len(expected):
            raise RuntimeError(
                f"Static geometry vertex count differs for {name}: "
                f"{len(actual)} != {len(expected)}"
            )
        for expected_vertex, actual_vertex in zip(expected, actual):
            maximum_error = max(
                maximum_error,
                max(abs(expected_vertex[index] - actual_vertex[index]) for index in range(3)),
            )
    if maximum_error > GEOMETRY_TOLERANCE:
        raise RuntimeError(
            f"The supplied change-to-rifle geometry differs from the static Ispant: "
            f"{maximum_error}"
        )
    return maximum_error


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


def split_rigid_musket(body, armature):
    if triangle_count(body.data) != EXPECTED_SOURCE_BODY_TRIANGLES:
        raise RuntimeError("The supplied change-to-rifle body topology differs.")
    components = connected_components(body.data)
    if len(components) != 77:
        raise RuntimeError(f"Expected 77 body components, found {len(components)}")
    musket_indices = {
        vertex
        for component_index in MUSKET_COMPONENTS
        for vertex in components[component_index]
    }
    musket = body.copy()
    musket.data = body.data.copy()
    musket.name = RIGID_MUSKET_NAME
    musket.data.name = f"{RIGID_MUSKET_NAME}_Mesh"
    bpy.context.collection.objects.link(musket)
    keep_vertices(musket.data, musket_indices)
    for modifier in list(musket.modifiers):
        musket.modifiers.remove(modifier)
    while musket.vertex_groups:
        musket.vertex_groups.remove(musket.vertex_groups[0])
    remove_vertices(body.data, musket_indices)
    if triangle_count(body.data) != EXPECTED_ANIMATED_BODY_TRIANGLES:
        raise RuntimeError("The animated body topology differs after musket separation.")
    if triangle_count(musket.data) != EXPECTED_MUSKET_TRIANGLES:
        raise RuntimeError("The rigid musket topology differs.")

    armature.data.pose_position = "REST"
    bpy.context.view_layer.update()
    world_matrix = musket.matrix_world.copy()
    musket.parent = armature
    musket.parent_type = "BONE"
    musket.parent_bone = SPINE_BONE_NAME
    musket.matrix_world = world_matrix
    return musket


def configure_scene(source):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    import_fbx(source, use_anim=True)
    armature = bpy.data.objects.get(ARMATURE_NAME)
    body = bpy.data.objects.get(BODY_NAME)
    source_sword = bpy.data.objects.get(SOURCE_SWORD_NAME)
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("The supplied change-to-rifle Mixamo armature is missing.")
    if len(armature.data.bones) != EXPECTED_BONES:
        raise RuntimeError(f"Expected {EXPECTED_BONES} Mixamo bones, found {len(armature.data.bones)}")
    if SPINE_BONE_NAME not in armature.data.bones:
        raise RuntimeError(f"Required attachment bone is missing: {SPINE_BONE_NAME}")
    if body is None or body.type != "MESH":
        raise RuntimeError("The exact static Ispant body geometry is missing.")
    if source_sword is None or source_sword.type != "MESH":
        raise RuntimeError("The supplied source sword reference is missing.")
    if len(bpy.data.actions) != 1:
        raise RuntimeError(f"Expected one Mixamo action, found {len(bpy.data.actions)}")
    action = bpy.data.actions[0]
    if "mixamo.com" not in action.name.lower():
        raise RuntimeError(f"The sole action is not Mixamo: {action.name}")
    if tuple(action.frame_range) != EXPECTED_ACTION_FRAMES:
        raise RuntimeError(f"Mixamo frame range differs: {tuple(action.frame_range)}")
    armature.animation_data_create()
    armature.animation_data.action = action
    split_rigid_musket(body, armature)
    bpy.data.objects.remove(source_sword, do_unlink=True)
    armature.data.pose_position = "POSE"
    bpy.context.scene.frame_set(int(EXPECTED_ACTION_FRAMES[0]))
    bpy.context.view_layer.update()
    return armature, action


def export_fbx(output, armature, action):
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.frame_start = int(EXPECTED_ACTION_FRAMES[0])
    bpy.context.scene.frame_end = int(EXPECTED_ACTION_FRAMES[1])
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    expected = {BODY_NAME, CRESCENT_NAME, EYES_NAME, RIGID_MUSKET_NAME}
    if {obj.name for obj in meshes} != expected:
        raise RuntimeError(f"Derived renderer set differs: {sorted(obj.name for obj in meshes)}")
    if triangle_count(bpy.data.objects[CRESCENT_NAME].data) != EXPECTED_CRESCENT_TRIANGLES:
        raise RuntimeError("The crescent topology differs.")
    if triangle_count(bpy.data.objects[EYES_NAME].data) != EXPECTED_EYE_TRIANGLES:
        raise RuntimeError("The eye topology differs.")
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
    static_source = Path(args.static_source).resolve()
    output = Path(args.output).resolve()
    source_geometry = capture_source_geometry(source)
    geometry_error = compare_static_geometry(source_geometry, static_source)
    armature, action = configure_scene(source)
    export_fbx(output, armature, action)
    print(
        "IspantChangeToRifleBuilt"
        f" Source={source} StaticSource={static_source} Output={output}"
        f" Action={action.name} Frames=1-213"
        f" StaticGeometryMaximumWorldVertexError={geometry_error:.12f}"
        f" BodyTriangles={EXPECTED_ANIMATED_BODY_TRIANGLES}"
        f" RigidMusketTriangles={EXPECTED_MUSKET_TRIANGLES}"
        " SourceSwordRemoved=True ExistingSlot6WeaponsRemainInUnity=True"
    )


if __name__ == "__main__":
    main()
