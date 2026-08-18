import bpy
import bmesh
import collections
import hashlib
import math
import os
from mathutils import Vector
from mathutils.bvhtree import BVHTree


SOURCE_PATH = r"D:\Bellerophon2\Bellerophon\enemies model\išpant-new.fbx"
OUTPUT_ROOT = r"D:\Bellerophon2\Bellerophon\artSample\enemies\ispant\long_sword_separation"
BLEND_PATH = os.path.join(OUTPUT_ROOT, "Ispant_LongSword_Separated_Sample.blend")
FBX_PATH = os.path.join(OUTPUT_ROOT, "Ispant_LongSword_Separated_Sample.fbx")
REVIEW_PATH = os.path.join(OUTPUT_ROOT, "Ispant_LongSword_Separated_Review.png")
UNITY_SOURCE_PATH = r"D:\Bellerophon2\Bellerophon\Assets\_Project\Art\Enemies\Ispant\Models\Ispant_New_Direct_Source.fbx"
UNITY_TEMP_PATH = r"D:\Bellerophon2\Bellerophon\Assets\_Project\Art\Enemies\Ispant\Models\Ispant_New_Direct_Source.__no_sword_tmp.fbx"
EXPECTED_SOURCE_SHA256 = "7B682C49CDB2F4A563B736857E544DD7FCCD7213A5C375DEB92F56CE3A2E51B7"
EXPECTED_UNITY_PRE_LEFT_LEG_PATCH_SHA256 = "95CFE22D341038B1A492457C8DD7E74BDAAF11A1102BBD89ECC037659F63D8AB"

# The first group is the single connected long-sword surface retained from
# the initial pass. The second group records the 270 body faces that were
# misclassified in the hilt correction: 242 hip/waist faces plus 28 left-
# forearm faces in U530. They must remain in the body and are never included
# in SWORD_UV_ISLANDS. The later 12-face mirrored left-leg repair is tracked
# separately and never changes the source-face classification below.
BLADE_AND_SCABBARD_UV_ISLANDS = {
    488, 491, 492, 493, 494, 495, 501, 515, 516, 520,
    537, 539, 543, 547, 552, 553, 556, 559, 560, 564,
    586, 593, 598, 602, 606, 607, 608, 613, 616,
}
MISCLASSIFIED_BODY_UV_ISLANDS = {
    374, 398, 433, 436, 443, 446, 447, 455, 462,
    464, 469, 498, 503, 509, 513, 517, 530,
}
SWORD_UV_ISLANDS = BLADE_AND_SCABBARD_UV_ISLANDS
MIRRORED_LEFT_LEG_SOURCE_FACE_INDICES = {
    1304, 1382, 1401, 1423, 1428, 1431,
    1494, 1537, 1587, 1681, 1688, 1787,
}
EXPECTED_ORIGINAL_FACES = 10028
EXPECTED_RESTORED_BODY_FACES = 270
EXPECTED_MIRRORED_LEFT_LEG_VERTICES = 13
EXPECTED_MIRRORED_LEFT_LEG_FACES = 12
EXPECTED_MIRRORED_LEFT_LEG_BOUNDARY_EDGES = 12
EXPECTED_BODY_VERTICES = 4895
EXPECTED_BODY_FACES = 9798
EXPECTED_SWORD_VERTICES = 143
EXPECTED_SWORD_FACES = 242
EXPECTED_BOUNDARY_EDGES = 46
EXPECTED_SWORD_BOUNDS_MIN = Vector((0.195403650, -0.150591552, 0.174190491))
EXPECTED_SWORD_BOUNDS_MAX = Vector((0.437850595, 0.062338043, 1.105124474))


def file_sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def uv_islands(mesh):
    uv_data = mesh.uv_layers.active.data
    edge_faces = collections.defaultdict(list)
    for polygon in mesh.polygons:
        loops = list(polygon.loop_indices)
        for index, loop_index in enumerate(loops):
            next_loop_index = loops[(index + 1) % len(loops)]
            vertex_a = mesh.loops[loop_index].vertex_index
            vertex_b = mesh.loops[next_loop_index].vertex_index
            uv_a = tuple(round(value, 6) for value in uv_data[loop_index].uv)
            uv_b = tuple(round(value, 6) for value in uv_data[next_loop_index].uv)
            pair = sorted(((vertex_a, uv_a), (vertex_b, uv_b)))
            edge_faces[(pair[0], pair[1])].append(polygon.index)

    adjacency = [set() for _ in mesh.polygons]
    for faces in edge_faces.values():
        for first in range(len(faces)):
            for second in range(first + 1, len(faces)):
                adjacency[faces[first]].add(faces[second])
                adjacency[faces[second]].add(faces[first])

    result = []
    visited = set()
    for seed in range(len(mesh.polygons)):
        if seed in visited:
            continue
        pending = [seed]
        visited.add(seed)
        faces = []
        while pending:
            face = pending.pop()
            faces.append(face)
            for neighbor in adjacency[face]:
                if neighbor not in visited:
                    visited.add(neighbor)
                    pending.append(neighbor)
        result.append(faces)
    return result


def face_signature(obj, polygon):
    mesh = obj.data
    uv_data = mesh.uv_layers.active.data
    loop_values = []
    for loop_index in polygon.loop_indices:
        vertex = mesh.vertices[mesh.loops[loop_index].vertex_index]
        coordinate = tuple(round(value, 6) for value in vertex.co)
        uv = tuple(round(value, 6) for value in uv_data[loop_index].uv)
        weights = tuple(sorted(
            (obj.vertex_groups[group.group].name, round(group.weight, 6))
            for group in vertex.groups
            if group.weight > 0.000001
        ))
        loop_values.append((coordinate, uv, weights))
    rotations = [
        tuple(loop_values[index:] + loop_values[:index])
        for index in range(len(loop_values))
    ]
    return polygon.material_index, min(rotations)


def face_signatures(obj):
    return collections.Counter(face_signature(obj, polygon) for polygon in obj.data.polygons)


def mirrored_patch_signatures(obj):
    patch_faces = list(obj.data.polygons)[-EXPECTED_MIRRORED_LEFT_LEG_FACES:]
    return collections.Counter(face_signature(obj, polygon) for polygon in patch_faces)


def validate_mirrored_left_leg_patch(body):
    patch_faces = list(body.data.polygons)[-EXPECTED_MIRRORED_LEFT_LEG_FACES:]
    patch_vertices = {
        vertex_index
        for polygon in patch_faces
        for vertex_index in polygon.vertices
    }
    if len(patch_faces) != EXPECTED_MIRRORED_LEFT_LEG_FACES:
        raise RuntimeError("Mirrored left-leg patch face count changed")
    if len(patch_vertices) != EXPECTED_MIRRORED_LEFT_LEG_VERTICES:
        raise RuntimeError("Mirrored left-leg patch vertex count changed")
    edge_use = collections.Counter()
    for polygon in patch_faces:
        vertices = list(polygon.vertices)
        for index, vertex in enumerate(vertices):
            edge_use[tuple(sorted((vertex, vertices[(index + 1) % len(vertices)])))] += 1
    boundary_edges = sum(1 for use_count in edge_use.values() if use_count == 1)
    if boundary_edges != EXPECTED_MIRRORED_LEFT_LEG_BOUNDARY_EDGES:
        raise RuntimeError(
            f"Mirrored left-leg boundary changed: {boundary_edges}; "
            f"expected {EXPECTED_MIRRORED_LEFT_LEG_BOUNDARY_EDGES}"
        )
    return mirrored_patch_signatures(body)


def world_bounds(obj, face_indices=None):
    mesh = obj.data
    if face_indices is None:
        vertex_indices = range(len(mesh.vertices))
    else:
        vertex_indices = sorted({
            vertex
            for face_index in face_indices
            for vertex in mesh.polygons[face_index].vertices
        })
    points = [obj.matrix_world @ mesh.vertices[index].co for index in vertex_indices]
    minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    return minimum, maximum


def geometric_boundary_edges(mesh, selected_faces):
    edge_faces = collections.defaultdict(list)
    for polygon in mesh.polygons:
        vertices = list(polygon.vertices)
        for index, vertex in enumerate(vertices):
            edge = tuple(sorted((vertex, vertices[(index + 1) % len(vertices)])))
            edge_faces[edge].append(polygon.index)
    return [
        edge
        for edge, faces in edge_faces.items()
        if any(face in selected_faces for face in faces)
        and any(face not in selected_faces for face in faces)
    ]


def separate_faces(source, selected_faces):
    working = source.copy()
    working.data = source.data.copy()
    bpy.context.collection.objects.link(working)
    working.name = "Ispant_Body_Without_LongSword"

    bpy.ops.object.select_all(action="DESELECT")
    working.select_set(True)
    bpy.context.view_layer.objects.active = working
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_mode(type="FACE")
    bpy.ops.mesh.select_all(action="DESELECT")
    bpy.ops.object.mode_set(mode="OBJECT")
    for face_index in selected_faces:
        working.data.polygons[face_index].select = True
    before = set(bpy.data.objects)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_mode(type="FACE")
    edit_mesh = bmesh.from_edit_mesh(working.data)
    selected_count = sum(1 for face in edit_mesh.faces if face.select)
    if selected_count != EXPECTED_SWORD_FACES:
        raise RuntimeError(
            f"Blender face selection expanded to {selected_count}; expected {EXPECTED_SWORD_FACES}"
        )
    bpy.ops.mesh.separate(type="SELECTED")
    bpy.ops.object.mode_set(mode="OBJECT")
    created = list(set(bpy.data.objects) - before)
    if len(created) != 1:
        raise RuntimeError(f"Expected one separated sword object, found {len(created)}")
    sword = created[0]
    sword.name = "Ispant_LongSword_Exact"
    return working, sword


def leg_symmetry_plane_x(armature):
    samples = []
    for left_name, right_name in (
        ("LeftUpLeg", "RightUpLeg"),
        ("LeftLeg", "RightLeg"),
        ("LeftFoot", "RightFoot"),
    ):
        left_bone = armature.data.bones.get(left_name)
        right_bone = armature.data.bones.get(right_name)
        if left_bone is None or right_bone is None:
            raise RuntimeError(f"Missing paired leg bones: {left_name}, {right_name}")
        samples.extend((
            (left_bone.head_local.x + right_bone.head_local.x) * 0.5,
            (left_bone.tail_local.x + right_bone.tail_local.x) * 0.5,
        ))
    return sum(samples) / len(samples)


def mirrored_group_name(name):
    if name.startswith("Right"):
        return "Left" + name[len("Right"):]
    if name.startswith("Left"):
        return "Right" + name[len("Left"):]
    return name


def add_mirrored_left_leg_patch(original, body, armature):
    source_faces = sorted(MIRRORED_LEFT_LEG_SOURCE_FACE_INDICES)
    if max(source_faces) >= len(original.data.polygons):
        raise RuntimeError("Mirrored left-leg source-face indices exceed the source mesh")
    source_vertices = sorted({
        vertex_index
        for face_index in source_faces
        for vertex_index in original.data.polygons[face_index].vertices
    })
    if len(source_faces) != EXPECTED_MIRRORED_LEFT_LEG_FACES:
        raise RuntimeError("Mirrored left-leg source-face count changed")
    if len(source_vertices) != EXPECTED_MIRRORED_LEFT_LEG_VERTICES:
        raise RuntimeError("Mirrored left-leg source-vertex count changed")

    plane_x = leg_symmetry_plane_x(armature)
    body_group_by_name = {group.name: group.index for group in body.vertex_groups}
    source_group_name = {group.index: group.name for group in original.vertex_groups}
    working = bmesh.new()
    working.from_mesh(body.data)
    deform_layer = working.verts.layers.deform.verify()
    uv_layer = working.loops.layers.uv.verify()

    mirrored_vertex_by_source = {}
    source_by_mirrored_vertex = {}
    for source_vertex_index in source_vertices:
        coordinate = original.data.vertices[source_vertex_index].co
        mirrored_vertex = working.verts.new((
            2.0 * plane_x - coordinate.x,
            coordinate.y,
            coordinate.z,
        ))
        mirrored_vertex_by_source[source_vertex_index] = mirrored_vertex
        source_by_mirrored_vertex[mirrored_vertex] = source_vertex_index
        deformation = mirrored_vertex[deform_layer]
        for membership in original.data.vertices[source_vertex_index].groups:
            source_name = source_group_name[membership.group]
            target_name = mirrored_group_name(source_name)
            if target_name not in body_group_by_name:
                raise RuntimeError(f"Missing mirrored vertex group: {target_name}")
            deformation[body_group_by_name[target_name]] = membership.weight

    original_uv = original.data.uv_layers.active.data
    for source_face_index in source_faces:
        source_polygon = original.data.polygons[source_face_index]
        source_uv_by_vertex = {
            original.data.loops[loop_index].vertex_index: original_uv[loop_index].uv.copy()
            for loop_index in source_polygon.loop_indices
        }
        mirrored_face = working.faces.new([
            mirrored_vertex_by_source[vertex_index]
            for vertex_index in reversed(source_polygon.vertices)
        ])
        mirrored_face.material_index = source_polygon.material_index
        for loop in mirrored_face.loops:
            loop[uv_layer].uv = source_uv_by_vertex[source_by_mirrored_vertex[loop.vert]]

    working.normal_update()
    working.to_mesh(body.data)
    working.free()
    body.data.update()

    patch_vertices = set(mirrored_vertex_by_source.values())
    patch_faces = [
        face
        for face in body.data.polygons
        if all(vertex_index >= len(body.data.vertices) - EXPECTED_MIRRORED_LEFT_LEG_VERTICES
               for vertex_index in face.vertices)
    ]
    if len(patch_vertices) != EXPECTED_MIRRORED_LEFT_LEG_VERTICES:
        raise RuntimeError("Mirrored left-leg patch vertex creation changed")
    if len(patch_faces) != EXPECTED_MIRRORED_LEFT_LEG_FACES:
        raise RuntimeError(f"Mirrored left-leg patch face creation changed: {len(patch_faces)}")
    return plane_x


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def evaluated_review_copy(source, name):
    dependency_graph = bpy.context.evaluated_depsgraph_get()
    evaluated = source.evaluated_get(dependency_graph)
    mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=dependency_graph)
    result = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(result)
    result.matrix_world = source.matrix_world.copy()
    return result


def center_review_object(obj, target_x, target_z, scale=1.0):
    obj.scale = tuple(component * scale for component in obj.scale)
    bpy.context.view_layer.update()
    points = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    center = (minimum + maximum) * 0.5
    obj.location.x += target_x - center.x
    obj.location.z += target_z - center.z


def create_review_render(original, body, sword):
    bpy.context.scene.frame_set(1)
    original_display = evaluated_review_copy(original, "Review_Original")
    body_display = evaluated_review_copy(body, "Review_Body_Without_Sword")
    sword_display = evaluated_review_copy(sword, "Review_Separated_Sword")
    center_review_object(original_display, -1.35, 0.98)
    center_review_object(body_display, 0.0, 0.98)
    center_review_object(sword_display, 1.35, 0.98, 1.35)

    for source in (original, body, sword):
        source.hide_render = True
    for scene_object in bpy.context.scene.objects:
        if scene_object.type in {"ARMATURE", "CAMERA", "LIGHT"}:
            scene_object.hide_render = True

    floor_material = bpy.data.materials.new("ReviewFloor")
    floor_material.diffuse_color = (0.055, 0.065, 0.08, 1.0)
    bpy.ops.mesh.primitive_plane_add(size=7.0, location=(0.0, 0.15, -0.015))
    floor = bpy.context.object
    floor.name = "Review_Floor"
    floor.data.materials.append(floor_material)

    camera_data = bpy.data.cameras.new("ReviewCamera")
    camera = bpy.data.objects.new("ReviewCamera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = (0.0, -7.5, 1.15)
    camera_data.lens = 58
    look_at(camera, (0.0, 0.0, 0.95))
    bpy.context.scene.camera = camera

    for name, location, energy, size in (
        ("Key", (-2.5, -3.5, 5.0), 1300.0, 4.0),
        ("Fill", (3.5, -2.0, 3.0), 850.0, 3.0),
        ("Rim", (0.0, 2.0, 4.0), 1000.0, 3.0),
    ):
        light_data = bpy.data.lights.new(name, "AREA")
        light_data.energy = energy
        light_data.shape = "DISK"
        light_data.size = size
        light = bpy.data.objects.new(name, light_data)
        bpy.context.scene.collection.objects.link(light)
        light.location = location
        look_at(light, (0.0, 0.0, 1.0))

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1536
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = REVIEW_PATH
    scene.render.film_transparent = False
    if scene.world is None:
        scene.world = bpy.data.worlds.new("ReviewWorld")
    scene.world.color = (0.025, 0.03, 0.04)
    scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.ops.render.render(write_still=True)


def validate_exported_fbx(expected_patch_signatures):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=FBX_PATH, use_anim=False)
    imported = list(set(bpy.data.objects) - before)
    imported_meshes = {obj.name: obj for obj in imported if obj.type == "MESH"}
    body = next((obj for name, obj in imported_meshes.items() if name.startswith("Ispant_Body_Without_LongSword")), None)
    sword = next((obj for name, obj in imported_meshes.items() if name.startswith("Ispant_LongSword_Exact")), None)
    if body is None or sword is None:
        raise RuntimeError("Exported FBX does not contain the named body and sword meshes")
    if len(body.data.polygons) != EXPECTED_BODY_FACES:
        raise RuntimeError("Exported FBX body face count changed")
    if len(sword.data.polygons) != EXPECTED_SWORD_FACES:
        raise RuntimeError("Exported FBX sword face count changed")
    if len(body.data.vertices) != EXPECTED_BODY_VERTICES or len(sword.data.vertices) != EXPECTED_SWORD_VERTICES:
        raise RuntimeError("Exported FBX body or sword vertex count changed")
    if expected_patch_signatures - face_signatures(body):
        raise RuntimeError("Exported FBX changed mirrored-patch coordinates, UVs, weights, or winding")
    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)


def validate_unity_export(path, expected_vertex_groups, expected_patch_signatures):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False)
    imported = list(set(bpy.data.objects) - before)
    imported_meshes = [obj for obj in imported if obj.type == "MESH"]
    imported_armatures = [obj for obj in imported if obj.type == "ARMATURE"]
    if len(imported_meshes) != 1 or len(imported_armatures) != 1:
        raise RuntimeError(
            "Unity export must contain exactly one mesh and one armature: "
            f"meshes={len(imported_meshes)}, armatures={len(imported_armatures)}"
        )
    mesh = imported_meshes[0]
    armature = imported_armatures[0]
    if not mesh.name.startswith("char1"):
        raise RuntimeError(f"Unity mesh name changed: {mesh.name}")
    if len(mesh.data.vertices) != EXPECTED_BODY_VERTICES or len(mesh.data.polygons) != EXPECTED_BODY_FACES:
        raise RuntimeError(
            f"Unity body geometry changed: vertices={len(mesh.data.vertices)}, "
            f"faces={len(mesh.data.polygons)}"
        )
    if len(mesh.data.uv_layers) != 1 or len(mesh.data.materials) != 1:
        raise RuntimeError(
            f"Unity body UV/material structure changed: uv={len(mesh.data.uv_layers)}, "
            f"materials={len(mesh.data.materials)}"
        )
    if mesh.parent != armature or not any(modifier.type == "ARMATURE" for modifier in mesh.modifiers):
        raise RuntimeError("Unity body is no longer parented and skinned to the armature")
    if len(armature.data.bones) != 24:
        raise RuntimeError(f"Unity armature bone count changed: {len(armature.data.bones)}")
    if {group.name for group in mesh.vertex_groups} != expected_vertex_groups:
        raise RuntimeError("Unity body vertex-group set changed")
    if expected_patch_signatures - face_signatures(mesh):
        raise RuntimeError("Unity export changed mirrored-patch coordinates, UVs, weights, or winding")
    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)


def deploy_unity_without_long_sword():
    if file_sha256(UNITY_SOURCE_PATH) != EXPECTED_UNITY_PRE_LEFT_LEG_PATCH_SHA256:
        raise RuntimeError("Unity source FBX is not the approved pre-left-leg-patch state")
    if file_sha256(SOURCE_PATH) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("Original source FBX hash changed before deployment")
    if os.path.exists(UNITY_TEMP_PATH):
        os.remove(UNITY_TEMP_PATH)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE_PATH, use_anim=True)
    original = bpy.data.objects.get("char1")
    armature = bpy.data.objects.get("Armature")
    if original is None or original.type != "MESH" or armature is None:
        raise RuntimeError("Unity source char1 mesh or Armature is missing")
    if len(original.data.polygons) != EXPECTED_ORIGINAL_FACES or len(armature.data.bones) != 24:
        raise RuntimeError("Unity source mesh or armature structure changed")

    original_object_name = original.name
    original_mesh_name = original.data.name
    original_materials = tuple(original.data.materials)
    original_vertex_groups = {group.name for group in original.vertex_groups}
    islands = uv_islands(original.data)
    if max(SWORD_UV_ISLANDS) >= len(islands):
        raise RuntimeError("Unity source UV-island structure changed")
    selected_faces = {face for island in SWORD_UV_ISLANDS for face in islands[island]}
    boundary = geometric_boundary_edges(original.data, selected_faces)
    minimum, maximum = world_bounds(original, selected_faces)
    if len(selected_faces) != EXPECTED_SWORD_FACES:
        raise RuntimeError(f"Unity sword face count is {len(selected_faces)}")
    if len(boundary) != EXPECTED_BOUNDARY_EDGES:
        raise RuntimeError(f"Unity sword boundary is {len(boundary)} edges")
    if (minimum - EXPECTED_SWORD_BOUNDS_MIN).length > 0.00001 or (maximum - EXPECTED_SWORD_BOUNDS_MAX).length > 0.00001:
        raise RuntimeError("Unity sword bounds differ from the approved sample")

    original_signatures = face_signatures(original)
    original.name = "Source_char1_Reference"
    body, sword = separate_faces(original, selected_faces)
    symmetry_plane_x = add_mirrored_left_leg_patch(original, body, armature)
    expected_patch_signatures = validate_mirrored_left_leg_patch(body)
    body.name = original_object_name
    body.data.name = original_mesh_name
    if len(body.data.vertices) != EXPECTED_BODY_VERTICES or len(body.data.polygons) != EXPECTED_BODY_FACES:
        raise RuntimeError("Unity body geometry differs from the restored-body sample")
    if len(sword.data.vertices) != EXPECTED_SWORD_VERTICES:
        raise RuntimeError("Unity removed-sword vertex count differs from the approved sample")
    combined_signatures = face_signatures(body) + face_signatures(sword)
    if original_signatures - combined_signatures:
        raise RuntimeError("Unity separation lost or changed an original source face")
    if sum((combined_signatures - original_signatures).values()) != EXPECTED_MIRRORED_LEFT_LEG_FACES:
        raise RuntimeError("Unity body does not contain exactly the mirrored left-leg patch")
    if tuple(body.data.materials) != original_materials:
        raise RuntimeError("Unity body material slots changed")
    if {group.name for group in body.vertex_groups} != original_vertex_groups:
        raise RuntimeError("Unity body vertex groups changed before export")

    bpy.ops.object.select_all(action="DESELECT")
    for export_object in (armature, body):
        export_object.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.export_scene.fbx(
        filepath=UNITY_TEMP_PATH,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
    )
    validate_unity_export(
        UNITY_TEMP_PATH,
        original_vertex_groups,
        expected_patch_signatures,
    )
    os.replace(UNITY_TEMP_PATH, UNITY_SOURCE_PATH)

    print("===ISPANT_UNITY_LONG_SWORD_REMOVAL_PASS===")
    print(f"SourceName={original_object_name}")
    print(f"BodyVertices={len(body.data.vertices)}")
    print(f"BodyFaces={len(body.data.polygons)}")
    print(f"RemovedSwordVertices={len(sword.data.vertices)}")
    print(f"RemovedSwordFaces={len(sword.data.polygons)}")
    print(f"RestoredBodyFaces={EXPECTED_RESTORED_BODY_FACES}")
    print(f"MirroredLeftLegVertices={EXPECTED_MIRRORED_LEFT_LEG_VERTICES}")
    print(f"MirroredLeftLegFaces={EXPECTED_MIRRORED_LEFT_LEG_FACES}")
    print(f"LegSymmetryPlaneX={symmetry_plane_x:.9f}")
    print(f"BoundaryEdges={len(boundary)}")
    print(f"ArmatureBones={len(armature.data.bones)}")
    print(f"MaterialSlots={len(body.data.materials)}")
    print(f"VertexGroups={len(body.vertex_groups)}")
    print(f"OutputSha256={file_sha256(UNITY_SOURCE_PATH)}")
    print(f"Output={UNITY_SOURCE_PATH}")


def analyze_selected_region_symmetry():
    """Report which currently removed source faces correspond to the opposite leg."""
    if file_sha256(SOURCE_PATH) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("Source FBX hash changed before symmetry analysis")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE_PATH, use_anim=True)
    original = bpy.data.objects.get("char1")
    armature = bpy.data.objects.get("Armature")
    if original is None or original.type != "MESH" or armature is None:
        raise RuntimeError("Symmetry analysis requires source char1 and Armature")

    mesh = original.data
    islands = uv_islands(mesh)
    selected_islands = sorted(SWORD_UV_ISLANDS)
    selected_faces = {
        face_index
        for island_index in selected_islands
        for face_index in islands[island_index]
    }
    group_name_by_index = {group.index: group.name for group in original.vertex_groups}
    left_leg_groups = {"LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase"}
    right_leg_groups = {"RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"}

    symmetry_samples = []
    for left_name, right_name in (
        ("LeftUpLeg", "RightUpLeg"),
        ("LeftLeg", "RightLeg"),
        ("LeftFoot", "RightFoot"),
    ):
        left_bone = armature.data.bones.get(left_name)
        right_bone = armature.data.bones.get(right_name)
        if left_bone is not None and right_bone is not None:
            symmetry_samples.extend((
                (left_bone.head_local.x + right_bone.head_local.x) * 0.5,
                (left_bone.tail_local.x + right_bone.tail_local.x) * 0.5,
            ))
    if not symmetry_samples:
        raise RuntimeError("Paired leg bones are missing")
    symmetry_plane_x = sum(symmetry_samples) / len(symmetry_samples)

    def vertex_group_weight(vertex_index, group_names):
        return sum(
            membership.weight
            for membership in mesh.vertices[vertex_index].groups
            if group_name_by_index.get(membership.group) in group_names
        )

    def polygon_group_weight(polygon, group_names):
        return sum(
            vertex_group_weight(vertex_index, group_names)
            for vertex_index in polygon.vertices
        ) / len(polygon.vertices)

    def polygon_center(polygon):
        return sum(
            (mesh.vertices[vertex_index].co for vertex_index in polygon.vertices),
            Vector(),
        ) / len(polygon.vertices)

    right_leg_polygons = [
        polygon
        for polygon in mesh.polygons
        if polygon_group_weight(polygon, right_leg_groups) > 0.05
        and polygon_group_weight(polygon, right_leg_groups)
        > polygon_group_weight(polygon, left_leg_groups)
    ]
    right_leg_vertex_indices = sorted({
        vertex_index
        for polygon in right_leg_polygons
        for vertex_index in polygon.vertices
    })
    right_leg_vertex_map = {
        vertex_index: mapped_index
        for mapped_index, vertex_index in enumerate(right_leg_vertex_indices)
    }
    right_leg_bvh = BVHTree.FromPolygons(
        [mesh.vertices[index].co.copy() for index in right_leg_vertex_indices],
        [
            [right_leg_vertex_map[index] for index in polygon.vertices]
            for polygon in right_leg_polygons
        ],
        all_triangles=False,
    )

    print("===ISPANT_REMOVED_REGION_SYMMETRY_ANALYSIS===")
    print(f"SymmetryPlaneX={symmetry_plane_x:.9f}")
    print(f"RightLegPoolFaces={len(right_leg_polygons)}")
    print(f"SelectedFaces={len(selected_faces)}")
    print(f"SelectedUvIslands={len(selected_islands)}")
    all_face_measurements = []
    for island_index in selected_islands:
        face_indices = islands[island_index]
        vertex_indices = sorted({
            vertex_index
            for face_index in face_indices
            for vertex_index in mesh.polygons[face_index].vertices
        })
        points = [mesh.vertices[index].co for index in vertex_indices]
        minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
        maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
        left_weights = []
        right_weights = []
        mirror_distances = []
        mirror_vertex_max_distances = []
        mirror_normal_agreements = []
        counterpart_shape_errors = []
        accumulated_weights = collections.Counter()
        for face_index in face_indices:
            polygon = mesh.polygons[face_index]
            left_weights.append(polygon_group_weight(polygon, left_leg_groups))
            right_weights.append(polygon_group_weight(polygon, right_leg_groups))
            center = polygon_center(polygon)
            mirrored_center = Vector((
                2.0 * symmetry_plane_x - center.x,
                center.y,
                center.z,
            ))
            nearest = right_leg_bvh.find_nearest(mirrored_center)
            mirror_distances.append(nearest[3] if nearest else math.inf)
            mirrored_vertex_distances = []
            for vertex_index in polygon.vertices:
                coordinate = mesh.vertices[vertex_index].co
                mirrored_coordinate = Vector((
                    2.0 * symmetry_plane_x - coordinate.x,
                    coordinate.y,
                    coordinate.z,
                ))
                vertex_nearest = right_leg_bvh.find_nearest(mirrored_coordinate)
                mirrored_vertex_distances.append(
                    vertex_nearest[3] if vertex_nearest else math.inf
                )
            mirror_vertex_max_distances.append(max(mirrored_vertex_distances))
            if nearest:
                mirrored_normal = Vector((
                    -polygon.normal.x,
                    polygon.normal.y,
                    polygon.normal.z,
                )).normalized()
                mirror_normal_agreements.append(abs(mirrored_normal.dot(nearest[1])))
                counterpart = right_leg_polygons[nearest[2]]
                source_edges = sorted(
                    (mesh.vertices[polygon.vertices[index]].co
                     - mesh.vertices[polygon.vertices[(index + 1) % len(polygon.vertices)]].co).length
                    for index in range(len(polygon.vertices))
                )
                counterpart_edges = sorted(
                    (mesh.vertices[counterpart.vertices[index]].co
                     - mesh.vertices[counterpart.vertices[(index + 1) % len(counterpart.vertices)]].co).length
                    for index in range(len(counterpart.vertices))
                )
                if len(source_edges) == len(counterpart_edges):
                    counterpart_shape_errors.append(sum(
                        abs(source - target) / max(source, target, 0.000001)
                        for source, target in zip(source_edges, counterpart_edges)
                    ) / len(source_edges))
                else:
                    counterpart_shape_errors.append(1.0)
            else:
                mirror_normal_agreements.append(0.0)
                counterpart_shape_errors.append(1.0)
            all_face_measurements.append((
                face_index,
                island_index,
                mirror_distances[-1],
                mirror_vertex_max_distances[-1],
                mirror_normal_agreements[-1],
                counterpart_shape_errors[-1],
            ))
            for vertex_index in polygon.vertices:
                for membership in mesh.vertices[vertex_index].groups:
                    accumulated_weights[group_name_by_index[membership.group]] += (
                        membership.weight / len(polygon.vertices)
                    )
        sorted_distances = sorted(mirror_distances)
        top_weights = ",".join(
            f"{name}:{weight:.2f}"
            for name, weight in accumulated_weights.most_common(4)
        )
        print(
            f"U{island_index} faces={len(face_indices)} verts={len(vertex_indices)} "
            f"bounds=({minimum.x:.4f},{minimum.y:.4f},{minimum.z:.4f}).."
            f"({maximum.x:.4f},{maximum.y:.4f},{maximum.z:.4f}) "
            f"leftW={sum(left_weights) / len(left_weights):.3f} "
            f"rightW={sum(right_weights) / len(right_weights):.3f} "
            f"mirrorDMedian={sorted_distances[len(sorted_distances) // 2]:.5f} "
            f"mirrorDMax={max(mirror_distances):.5f} "
            f"vertexMaxMedian={sorted(mirror_vertex_max_distances)[len(mirror_vertex_max_distances) // 2]:.5f} "
            f"normalMedian={sorted(mirror_normal_agreements)[len(mirror_normal_agreements) // 2]:.3f} "
            f"shapeErrorMedian={sorted(counterpart_shape_errors)[len(counterpart_shape_errors) // 2]:.3f} "
            f"top={top_weights}"
        )

    print("===FACE_THRESHOLD_COUNTS===")
    for distance_limit in (0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0):
        matching = [
            measurement
            for measurement in all_face_measurements
            if measurement[3] <= distance_limit
            and measurement[4] >= 0.65
            and measurement[5] <= 0.45
        ]
        print(
            f"Distance<={distance_limit:.2f} faces={len(matching)} "
            f"islands={sorted({measurement[1] for measurement in matching})}"
        )


def analyze_mirrored_right_leg_gaps():
    """Find right-leg source faces whose mirrors are absent from the current left leg."""
    if file_sha256(SOURCE_PATH) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("Source FBX hash changed before mirrored-gap analysis")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE_PATH, use_anim=True)
    original = bpy.data.objects.get("char1")
    armature = bpy.data.objects.get("Armature")
    if original is None or armature is None:
        raise RuntimeError("Mirrored-gap analysis requires source char1 and Armature")
    mesh = original.data
    islands = uv_islands(mesh)
    removed_faces = {
        face_index
        for island_index in SWORD_UV_ISLANDS
        for face_index in islands[island_index]
    }
    group_name_by_index = {group.index: group.name for group in original.vertex_groups}
    left_groups = {"LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase"}
    right_groups = {"RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"}

    def vertex_weight(vertex_index, names):
        return sum(
            membership.weight
            for membership in mesh.vertices[vertex_index].groups
            if group_name_by_index.get(membership.group) in names
        )

    def face_weight(polygon, names):
        return sum(vertex_weight(index, names) for index in polygon.vertices) / len(polygon.vertices)

    symmetry_values = []
    for left_name, right_name in (
        ("LeftUpLeg", "RightUpLeg"),
        ("LeftLeg", "RightLeg"),
        ("LeftFoot", "RightFoot"),
    ):
        left_bone = armature.data.bones.get(left_name)
        right_bone = armature.data.bones.get(right_name)
        symmetry_values.extend((
            (left_bone.head_local.x + right_bone.head_local.x) * 0.5,
            (left_bone.tail_local.x + right_bone.tail_local.x) * 0.5,
        ))
    plane_x = sum(symmetry_values) / len(symmetry_values)

    left_faces = [
        polygon
        for polygon in mesh.polygons
        if polygon.index not in removed_faces
        and face_weight(polygon, left_groups) > 0.05
        and face_weight(polygon, left_groups) > face_weight(polygon, right_groups)
    ]
    right_faces = [
        polygon
        for polygon in mesh.polygons
        if face_weight(polygon, right_groups) > 0.05
        and face_weight(polygon, right_groups) > face_weight(polygon, left_groups)
    ]

    left_vertices = sorted({index for polygon in left_faces for index in polygon.vertices})
    left_map = {index: mapped for mapped, index in enumerate(left_vertices)}
    left_bvh = BVHTree.FromPolygons(
        [mesh.vertices[index].co.copy() for index in left_vertices],
        [[left_map[index] for index in polygon.vertices] for polygon in left_faces],
        all_triangles=False,
    )

    removed_vertices = sorted({
        index
        for face_index in removed_faces
        for index in mesh.polygons[face_index].vertices
    })
    removed_points = [mesh.vertices[index].co for index in removed_vertices]
    removed_minimum = Vector(tuple(min(point[axis] for point in removed_points) for axis in range(3)))
    removed_maximum = Vector(tuple(max(point[axis] for point in removed_points) for axis in range(3)))

    measurements = {}
    for polygon in right_faces:
        center = sum(
            (mesh.vertices[index].co for index in polygon.vertices),
            Vector(),
        ) / len(polygon.vertices)
        mirrored_center = Vector((2.0 * plane_x - center.x, center.y, center.z))
        if not (
            removed_minimum.x - 3.0 <= mirrored_center.x <= removed_maximum.x + 3.0
            and removed_minimum.y - 3.0 <= mirrored_center.y <= removed_maximum.y + 3.0
            and removed_minimum.z - 3.0 <= mirrored_center.z <= removed_maximum.z + 3.0
        ):
            continue
        nearest = left_bvh.find_nearest(mirrored_center)
        measurements[polygon.index] = nearest[3] if nearest else math.inf

    edge_faces = collections.defaultdict(list)
    for polygon in right_faces:
        if polygon.index not in measurements:
            continue
        vertices = list(polygon.vertices)
        for index, vertex in enumerate(vertices):
            edge_faces[tuple(sorted((vertex, vertices[(index + 1) % len(vertices)])))].append(polygon.index)
    adjacency = collections.defaultdict(set)
    for face_indices in edge_faces.values():
        for first in face_indices:
            for second in face_indices:
                if first != second:
                    adjacency[first].add(second)

    print("===ISPANT_MIRRORED_RIGHT_LEG_GAP_ANALYSIS===")
    print(f"SymmetryPlaneX={plane_x:.9f}")
    print(f"LeftExistingFaces={len(left_faces)}")
    print(f"RightSourceFaces={len(right_faces)}")
    print(f"RightFacesInRemovedBounds={len(measurements)}")
    for threshold in (0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0, 7.5, 10.0):
        candidates = {index for index, distance in measurements.items() if distance > threshold}
        components = []
        visited = set()
        for seed in sorted(candidates):
            if seed in visited:
                continue
            pending = [seed]
            visited.add(seed)
            component = []
            while pending:
                current = pending.pop()
                component.append(current)
                for neighbor in adjacency[current]:
                    if neighbor in candidates and neighbor not in visited:
                        visited.add(neighbor)
                        pending.append(neighbor)
            components.append(component)
        largest = sorted((len(component) for component in components), reverse=True)[:8]
        print(
            f"GapDistance>{threshold:.2f} faces={len(candidates)} "
            f"components={len(components)} largest={largest}"
        )
    print("===GAP_COMPONENTS_AT_2_5===")
    candidates = {index for index, distance in measurements.items() if distance > 2.5}
    visited = set()
    components = []
    for seed in sorted(candidates):
        if seed in visited:
            continue
        pending = [seed]
        visited.add(seed)
        component = []
        while pending:
            current = pending.pop()
            component.append(current)
            for neighbor in adjacency[current]:
                if neighbor in candidates and neighbor not in visited:
                    visited.add(neighbor)
                    pending.append(neighbor)
        components.append(component)
    for component_index, component in enumerate(sorted(components, key=len, reverse=True)[:12]):
        points = []
        for face_index in component:
            for vertex_index in mesh.polygons[face_index].vertices:
                point = mesh.vertices[vertex_index].co
                points.append(Vector((2.0 * plane_x - point.x, point.y, point.z)))
        minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
        maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
        distances = sorted(measurements[index] for index in component)
        print(
            f"C{component_index} faces={len(component)} "
            f"distanceMedian={distances[len(distances) // 2]:.5f} "
            f"distanceMax={max(distances):.5f} "
            f"bounds=({minimum.x:.4f},{minimum.y:.4f},{minimum.z:.4f}).."
            f"({maximum.x:.4f},{maximum.y:.4f},{maximum.z:.4f})"
        )
        print(f"C{component_index} sourceFaceIndices={sorted(component)}")
        print(
            f"C{component_index} sourceVertices="
            f"{len({vertex for face_index in component for vertex in mesh.polygons[face_index].vertices})}"
        )


def main():
    os.makedirs(OUTPUT_ROOT, exist_ok=True)
    if file_sha256(SOURCE_PATH) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("Source FBX hash changed")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE_PATH, use_anim=True)
    original = bpy.data.objects.get("char1")
    if original is None or original.type != "MESH":
        raise RuntimeError("Source char1 mesh is missing")
    if len(original.data.polygons) != EXPECTED_ORIGINAL_FACES:
        raise RuntimeError("Source face count changed")

    islands = uv_islands(original.data)
    if max(SWORD_UV_ISLANDS) >= len(islands):
        raise RuntimeError("Source UV-island structure changed")
    selected_faces = {face for island in SWORD_UV_ISLANDS for face in islands[island]}
    if len(selected_faces) != EXPECTED_SWORD_FACES:
        raise RuntimeError(f"Sword face count is {len(selected_faces)}, expected {EXPECTED_SWORD_FACES}")
    boundary = geometric_boundary_edges(original.data, selected_faces)
    if len(boundary) != EXPECTED_BOUNDARY_EDGES:
        raise RuntimeError(f"Sword boundary is {len(boundary)} edges, expected {EXPECTED_BOUNDARY_EDGES}")
    minimum, maximum = world_bounds(original, selected_faces)
    if (minimum - EXPECTED_SWORD_BOUNDS_MIN).length > 0.00001 or (maximum - EXPECTED_SWORD_BOUNDS_MAX).length > 0.00001:
        raise RuntimeError("Sword bounds differ from the analyzed source region")

    original.name = "Source_char1_Reference"
    original_signatures = face_signatures(original)
    body, sword = separate_faces(original, selected_faces)
    armature = bpy.data.objects.get("Armature")
    if armature is None:
        raise RuntimeError("Source armature is missing")
    symmetry_plane_x = add_mirrored_left_leg_patch(original, body, armature)
    expected_patch_signatures = validate_mirrored_left_leg_patch(body)
    if len(body.data.polygons) != EXPECTED_BODY_FACES:
        raise RuntimeError(
            f"Body face count changed after mirrored repair: {len(body.data.polygons)}; "
            f"separated object faces: {len(sword.data.polygons)}"
        )
    if len(sword.data.polygons) != EXPECTED_SWORD_FACES:
        raise RuntimeError(
            f"Sword face count changed during separation: {len(sword.data.polygons)}; "
            f"body faces: {len(body.data.polygons)}"
        )
    if len(body.data.vertices) != EXPECTED_BODY_VERTICES or len(sword.data.vertices) != EXPECTED_SWORD_VERTICES:
        raise RuntimeError(
            f"Vertex counts changed during separation: body={len(body.data.vertices)}, "
            f"sword={len(sword.data.vertices)}"
        )
    combined_signatures = face_signatures(body) + face_signatures(sword)
    if original_signatures - combined_signatures:
        raise RuntimeError("Coordinates, UVs, weights, or winding changed on an original face")
    if sum((combined_signatures - original_signatures).values()) != EXPECTED_MIRRORED_LEFT_LEG_FACES:
        raise RuntimeError("Sample does not contain exactly the mirrored left-leg patch")
    if tuple(body.data.materials) != tuple(original.data.materials) or tuple(sword.data.materials) != tuple(original.data.materials):
        raise RuntimeError("Material slots changed during separation")

    original.hide_viewport = True
    original.hide_render = True
    bpy.ops.object.select_all(action="DESELECT")
    for export_object in (armature, body, sword):
        export_object.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
    )
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    validate_exported_fbx(expected_patch_signatures)
    if os.environ.get("ISPANT_SKIP_REVIEW_RENDER") != "1":
        create_review_render(original, body, sword)

    print("===ISPANT_LONG_SWORD_SEPARATION_PASS===")
    print(f"SourceSha256={file_sha256(SOURCE_PATH)}")
    print(f"OriginalFaces={len(original.data.polygons)}")
    print(f"BodyFaces={len(body.data.polygons)}")
    print(f"SwordFaces={len(sword.data.polygons)}")
    print(f"OriginalVertices={len(original.data.vertices)}")
    print(f"BodyVertices={len(body.data.vertices)}")
    print(f"SwordVertices={len(sword.data.vertices)}")
    print(f"MirroredLeftLegVertices={EXPECTED_MIRRORED_LEFT_LEG_VERTICES}")
    print(f"MirroredLeftLegFaces={EXPECTED_MIRRORED_LEFT_LEG_FACES}")
    print(f"LegSymmetryPlaneX={symmetry_plane_x:.9f}")
    print(f"BoundaryEdges={len(boundary)}")
    print("SwordBoundsMin=" + ",".join(f"{value:.6f}" for value in minimum))
    print("SwordBoundsMax=" + ",".join(f"{value:.6f}" for value in maximum))
    print(f"Blend={BLEND_PATH}")
    print(f"Fbx={FBX_PATH}")
    print(f"Review={REVIEW_PATH}")


if __name__ == "__main__":
    if os.environ.get("ISPANT_ANALYZE_MIRRORED_GAPS") == "1":
        analyze_mirrored_right_leg_gaps()
    elif os.environ.get("ISPANT_ANALYZE_REMOVED_REGION") == "1":
        analyze_selected_region_symmetry()
    elif os.environ.get("ISPANT_DEPLOY_UNITY_NO_SWORD") == "1":
        deploy_unity_without_long_sword()
    else:
        main()
