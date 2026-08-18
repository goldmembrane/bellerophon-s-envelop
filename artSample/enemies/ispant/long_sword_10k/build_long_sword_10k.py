import bpy
import bmesh
import collections
import hashlib
import os
import shutil
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree


SEGMENT_PATH = r"C:\Users\gus68\Downloads\išpant-segment.glb"
SOURCE_FBX_PATH = r"D:\Bellerophon2\Bellerophon\enemies model\išpant-new.fbx"
TEXTURE_ROOT = r"C:\Users\gus68\Downloads\Meshy_AI_Crescent_Iron_Sentine_biped (1)\Meshy_AI_Crescent_Iron_Sentine_biped"
OUTPUT_ROOT = r"D:\Bellerophon2\Bellerophon\artSample\enemies\ispant\long_sword_10k"
BLEND_PATH = os.path.join(OUTPUT_ROOT, "Ispant_LongSword_10K_Textured.blend")
FBX_PATH = os.path.join(OUTPUT_ROOT, "Ispant_LongSword_10K_Textured.fbx")
REVIEW_PATH = os.path.join(OUTPUT_ROOT, "Ispant_LongSword_10K_Review.png")

EXPECTED_SEGMENT_SHA256 = "EAEB45D54E510A5CABFDAF9C36A26606A04518D8F812E1C1B8B5B84C645A0EF0"
EXPECTED_SOURCE_FBX_SHA256 = "7B682C49CDB2F4A563B736857E544DD7FCCD7213A5C375DEB92F56CE3A2E51B7"
TEXTURES = {
    "BaseColor": (
        "Meshy_AI_Crescent_Iron_Sentine_biped_texture_0.png",
        "Ispant_LongSword_10K_BaseColor.png",
        "7DE6705FB7BD60E2D347023EFE51E96598845D611631F40D01C64EACC5249570",
    ),
    "Metallic": (
        "Meshy_AI_Crescent_Iron_Sentine_biped_texture_0_metallic.png",
        "Ispant_LongSword_10K_Metallic.png",
        "674812FCDE6B2879D15E40BDCE0BDC1BB152C75D7B74AC3371B2C96BE478920D",
    ),
    "Normal": (
        "Meshy_AI_Crescent_Iron_Sentine_biped_texture_0_normal.png",
        "Ispant_LongSword_10K_Normal.png",
        "11F5A8254E2FA46BF5F7EC49426F1BAD8F49CA254264EFE9FA15A73731E50C07",
    ),
    "Roughness": (
        "Meshy_AI_Crescent_Iron_Sentine_biped_texture_0_roughness.png",
        "Ispant_LongSword_10K_Roughness.png",
        "45B468DCDC7E5624A0D74ED639F586759B682BB67DED3A0666C1104889689432",
    ),
}

SWORD_UV_ISLANDS = {
    488, 491, 492, 493, 494, 495, 501, 515, 516, 520,
    537, 539, 543, 547, 552, 553, 556, 559, 560, 564,
    586, 593, 598, 602, 606, 607, 608, 613, 616,
}
EXPECTED_SOURCE_FACES = 10028
EXPECTED_SWORD_FACES = 242
DECIMATE_RATIO = 0.212
EXPECTED_TARGET_VERTICES = 9975
EXPECTED_TARGET_FACES = 19950
REGISTRATION_SIGNS = np.array((-1.0, -1.0, -1.0), dtype=np.float64)


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


def separate_source_sword(source, selected_faces):
    working = source.copy()
    working.data = source.data.copy()
    bpy.context.scene.collection.objects.link(working)

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
    edit_mesh = bmesh.from_edit_mesh(working.data)
    if sum(1 for face in edit_mesh.faces if face.select) != EXPECTED_SWORD_FACES:
        raise RuntimeError("Source sword face selection changed")
    bpy.ops.mesh.separate(type="SELECTED")
    bpy.ops.object.mode_set(mode="OBJECT")
    created = list(set(bpy.data.objects) - before)
    if len(created) != 1:
        raise RuntimeError("Source sword separation did not produce one object")
    sword = created[0]
    sword.name = "UV_Source_Ispant_LongSword_Exact"
    bpy.data.objects.remove(working, do_unlink=True)
    return sword


def connected_components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        first, second = edge.vertices
        adjacency[first].add(second)
        adjacency[second].add(first)
    remaining = set(range(len(mesh.vertices)))
    count = 0
    while remaining:
        count += 1
        stack = [remaining.pop()]
        while stack:
            current = stack.pop()
            neighbors = adjacency[current] & remaining
            remaining.difference_update(neighbors)
            stack.extend(neighbors)
    return count


def world_points(obj):
    return np.array([tuple(obj.matrix_world @ vertex.co) for vertex in obj.data.vertices], dtype=np.float64)


def canonicalize(points):
    center = points.mean(axis=0)
    centered = points - center
    covariance = np.cov(centered, rowvar=False)
    eigenvalues, eigenvectors = np.linalg.eigh(covariance)
    order = np.argsort(eigenvalues)[::-1]
    basis = eigenvectors[:, order]
    if np.linalg.det(basis) < 0.0:
        basis[:, 2] *= -1.0
    local = centered @ basis
    minimum = local.min(axis=0)
    maximum = local.max(axis=0)
    canonical = (local - (minimum + maximum) * 0.5) / (maximum - minimum)
    return canonical


def make_proxy(source, name, coordinates):
    proxy = source.copy()
    proxy.data = source.data.copy()
    proxy.name = name
    bpy.context.scene.collection.objects.link(proxy)
    proxy.matrix_world = Matrix.Identity(4)
    for index, coordinate in enumerate(coordinates):
        proxy.data.vertices[index].co = coordinate
    return proxy


def transfer_source_uv(source, target):
    source_canonical = canonicalize(world_points(source))
    target_canonical = canonicalize(world_points(target)) * REGISTRATION_SIGNS
    source_proxy = make_proxy(source, "_UV_Source_Canonical", source_canonical)
    target_proxy = make_proxy(target, "_UV_Target_Canonical", target_canonical)
    while target_proxy.data.uv_layers:
        target_proxy.data.uv_layers.remove(target_proxy.data.uv_layers[0])
    source_uv = source_proxy.data.uv_layers.active
    if source_uv is None:
        raise RuntimeError("Source sword UV is missing")
    target_proxy.data.uv_layers.new(name=source_uv.name)

    bpy.ops.object.select_all(action="DESELECT")
    target_proxy.select_set(True)
    bpy.context.view_layer.objects.active = target_proxy
    modifier = target_proxy.modifiers.new(name="TransferSourceUV", type="DATA_TRANSFER")
    modifier.object = source_proxy
    modifier.use_loop_data = True
    modifier.data_types_loops = {"UV"}
    modifier.loop_mapping = "POLYINTERP_NEAREST"
    modifier.layers_uv_select_src = source_uv.name
    modifier.layers_uv_select_dst = source_uv.name
    bpy.ops.object.modifier_apply(modifier=modifier.name)

    while target.data.uv_layers:
        target.data.uv_layers.remove(target.data.uv_layers[0])
    target_uv = target.data.uv_layers.new(name=target_proxy.data.uv_layers.active.name)
    for index, item in enumerate(target_proxy.data.uv_layers.active.data):
        target_uv.data[index].uv = item.uv

    bpy.data.objects.remove(source_proxy, do_unlink=True)
    bpy.data.objects.remove(target_proxy, do_unlink=True)
    values = [tuple(item.uv) for item in target_uv.data]
    minimum = tuple(min(value[axis] for value in values) for axis in range(2))
    maximum = tuple(max(value[axis] for value in values) for axis in range(2))
    return minimum, maximum


def copy_and_load_textures():
    images = {}
    for role, (source_name, output_name, expected_hash) in TEXTURES.items():
        source_path = os.path.join(TEXTURE_ROOT, source_name)
        output_path = os.path.join(OUTPUT_ROOT, output_name)
        if file_sha256(source_path) != expected_hash:
            raise RuntimeError(f"{role} texture hash changed")
        shutil.copy2(source_path, output_path)
        if file_sha256(output_path) != expected_hash:
            raise RuntimeError(f"{role} texture copy differs from source")
        image = bpy.data.images.load(output_path, check_existing=False)
        if role != "BaseColor":
            image.colorspace_settings.name = "Non-Color"
        images[role] = image
    return images


def create_pbr_material(images):
    material = bpy.data.materials.new("Ispant_LongSword_10K_PBR")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    output.location = (700, 0)
    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (420, 0)
    base = nodes.new("ShaderNodeTexImage")
    base.name = "BaseColor"
    base.image = images["BaseColor"]
    base.location = (-500, 260)
    metallic = nodes.new("ShaderNodeTexImage")
    metallic.name = "Metallic"
    metallic.image = images["Metallic"]
    metallic.location = (-500, 40)
    roughness = nodes.new("ShaderNodeTexImage")
    roughness.name = "Roughness"
    roughness.image = images["Roughness"]
    roughness.location = (-500, -180)
    normal_texture = nodes.new("ShaderNodeTexImage")
    normal_texture.name = "Normal"
    normal_texture.image = images["Normal"]
    normal_texture.location = (-500, -400)
    normal_map = nodes.new("ShaderNodeNormalMap")
    normal_map.location = (120, -310)
    links.new(base.outputs["Color"], principled.inputs["Base Color"])
    links.new(metallic.outputs["Color"], principled.inputs["Metallic"])
    links.new(roughness.outputs["Color"], principled.inputs["Roughness"])
    links.new(normal_texture.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], principled.inputs["Normal"])
    links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    return material


def world_bounds(obj):
    points = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    return minimum, maximum


def emission_material(name, color):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = color
    material.node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def world_baked_review_copy(source, name):
    mesh = source.data.copy()
    mesh.transform(source.matrix_world)
    result = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(result)
    result.matrix_world = Matrix.Identity(4)
    return result


def position_review_object(obj, target, length, rotation_x=0.0):
    minimum, maximum = world_bounds(obj)
    center = (minimum + maximum) * 0.5
    scale = length / (maximum.x - minimum.x)
    obj.matrix_world = (
        Matrix.Translation(Vector(target))
        @ Matrix.Rotation(rotation_x, 4, "X")
        @ Matrix.Scale(scale, 4)
        @ Matrix.Translation(-center)
        @ obj.matrix_world
    )
    bpy.context.view_layer.update()
    positioned_minimum, positioned_maximum = world_bounds(obj)
    print(
        f"ReviewBounds={obj.name}|Min={tuple(round(value, 5) for value in positioned_minimum)}|"
        f"Max={tuple(round(value, 5) for value in positioned_maximum)}"
    )


def create_review_render(original, textured):
    for obj in bpy.context.scene.objects:
        obj.hide_render = True

    reference = world_baked_review_copy(original, "Review_Original_47052")
    reference.hide_render = False
    reference.data.materials.clear()
    reference.data.materials.append(emission_material("OriginalSegmentBlue", (0.22, 0.55, 0.78, 1.0)))
    position_review_object(reference, (-3.8, 0.5, 0.0), 3.2)

    top = world_baked_review_copy(textured, "Review_Textured_Top")
    top.hide_render = False
    position_review_object(top, (0.0, 0.5, 0.0), 3.2)

    angled = world_baked_review_copy(textured, "Review_Textured_Angled")
    angled.hide_render = False
    position_review_object(angled, (3.8, 0.5, 0.0), 3.2, rotation_x=0.55)

    label_material = emission_material("ReviewLabel", (0.92, 0.94, 0.98, 1.0))
    for x, label_text in (
        (-3.8, "ORIGINAL SEGMENT 47,052 VERTS"),
        (0.0, "TEXTURED 9,975 VERTS"),
        (3.8, "PBR 3/4 VIEW"),
    ):
        bpy.ops.object.text_add(location=(x, -1.45, 0.05))
        label = bpy.context.object
        label.data.body = label_text
        label.data.align_x = "CENTER"
        label.data.size = 0.18
        label.data.materials.append(label_material)

    camera_data = bpy.data.cameras.new("ReviewCamera")
    camera = bpy.data.objects.new("ReviewCamera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = (0.0, 0.0, 10.0)
    camera.rotation_euler = (Vector((0.0, 0.0, 0.0)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = 11.5
    camera.hide_render = False
    bpy.context.scene.camera = camera

    for x in (-3.8, 0.0, 3.8):
        light_data = bpy.data.lights.new(f"ReviewLight_{x}", type="AREA")
        light_data.energy = 650.0
        light_data.shape = "DISK"
        light_data.size = 3.0
        light = bpy.data.objects.new(f"ReviewLight_{x}", light_data)
        bpy.context.scene.collection.objects.link(light)
        light.location = (x, 0.0, 5.0)
        light.hide_render = False

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 2048
    scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = REVIEW_PATH
    scene.render.film_transparent = False
    if scene.world is None:
        scene.world = bpy.data.worlds.new("ReviewWorld")
    scene.world.color = (0.008, 0.010, 0.015)
    bpy.ops.render.render(write_still=True)


def validate_export():
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=FBX_PATH, use_anim=False)
    imported = [obj for obj in set(bpy.data.objects) - before if obj.type == "MESH"]
    if len(imported) != 1:
        raise RuntimeError(f"Expected one reimported mesh, found {len(imported)}")
    mesh = imported[0].data
    result = (len(mesh.vertices), len(mesh.polygons), len(mesh.uv_layers))
    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)
    if result != (EXPECTED_TARGET_VERTICES, EXPECTED_TARGET_FACES, 1):
        raise RuntimeError(f"FBX reimport differs: {result}")
    return result


def main():
    os.makedirs(OUTPUT_ROOT, exist_ok=True)
    if file_sha256(SEGMENT_PATH) != EXPECTED_SEGMENT_SHA256:
        raise RuntimeError("Segment GLB hash changed")
    if file_sha256(SOURCE_FBX_PATH) != EXPECTED_SOURCE_FBX_SHA256:
        raise RuntimeError("Source FBX hash changed")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=SEGMENT_PATH)
    segment = bpy.data.objects.get("mesh_10")
    if segment is None:
        raise RuntimeError("mesh_10 is missing")
    baked_segment_mesh = segment.data.copy()
    baked_segment_mesh.transform(segment.matrix_world)
    segment.data = baked_segment_mesh
    segment.parent = None
    segment.matrix_world = Matrix.Identity(4)
    for obj in list(bpy.context.scene.objects):
        if obj != segment:
            bpy.data.objects.remove(obj, do_unlink=True)
    segment.name = "Ispant_LongSword_Segment_Original_47052"
    original_vertex_count = len(segment.data.vertices)
    original_face_count = len(segment.data.polygons)

    target = segment.copy()
    target.data = segment.data.copy()
    target.name = "Ispant_LongSword_10K_Textured"
    bpy.context.scene.collection.objects.link(target)
    bpy.ops.object.select_all(action="DESELECT")
    target.select_set(True)
    bpy.context.view_layer.objects.active = target
    decimate = target.modifiers.new(name="Target10K", type="DECIMATE")
    decimate.decimate_type = "COLLAPSE"
    decimate.ratio = DECIMATE_RATIO
    decimate.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    if len(target.data.vertices) != EXPECTED_TARGET_VERTICES or len(target.data.polygons) != EXPECTED_TARGET_FACES:
        raise RuntimeError("Decimated geometry count changed")
    if connected_components(target.data) != 1:
        raise RuntimeError("Decimated sword is no longer one connected component")

    bpy.ops.import_scene.fbx(filepath=SOURCE_FBX_PATH, use_anim=False)
    source_mesh = bpy.data.objects.get("char1")
    if source_mesh is None or len(source_mesh.data.polygons) != EXPECTED_SOURCE_FACES:
        raise RuntimeError("Source char1 mesh changed")
    islands = uv_islands(source_mesh.data)
    selected_faces = {face for island in SWORD_UV_ISLANDS for face in islands[island]}
    if len(selected_faces) != EXPECTED_SWORD_FACES:
        raise RuntimeError("Source sword UV-island selection changed")
    source_sword = separate_source_sword(source_mesh, selected_faces)
    for obj in list(bpy.context.scene.objects):
        if obj not in (segment, target, source_sword):
            bpy.data.objects.remove(obj, do_unlink=True)

    uv_minimum, uv_maximum = transfer_source_uv(source_sword, target)
    per_vertex_uv = [set() for _ in target.data.vertices]
    target_uv = target.data.uv_layers.active
    for loop in target.data.loops:
        uv = target_uv.data[loop.index].uv
        per_vertex_uv[loop.vertex_index].add((round(uv.x, 7), round(uv.y, 7)))
    if sum(len(values) for values in per_vertex_uv) != EXPECTED_TARGET_VERTICES:
        raise RuntimeError("UV transfer introduced split vertex mappings")

    images = copy_and_load_textures()
    material = create_pbr_material(images)
    target.data.materials.clear()
    target.data.materials.append(material)
    for polygon in target.data.polygons:
        polygon.use_smooth = True

    segment.hide_viewport = True
    segment.hide_render = True
    source_sword.hide_viewport = True
    source_sword.hide_render = True
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    bpy.ops.object.select_all(action="DESELECT")
    target.select_set(True)
    bpy.context.view_layer.objects.active = target
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        object_types={"MESH"},
        use_mesh_modifiers=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
    )
    reimport_result = validate_export()
    create_review_render(segment, target)

    print("===ISPANT_LONG_SWORD_10K_PASS===")
    print(f"SegmentSha256={file_sha256(SEGMENT_PATH)}")
    print(f"SourceFbxSha256={file_sha256(SOURCE_FBX_PATH)}")
    print(f"OriginalVertices={original_vertex_count}|OriginalFaces={original_face_count}")
    print(f"TargetVertices={len(target.data.vertices)}|TargetFaces={len(target.data.polygons)}")
    print(f"Components={connected_components(target.data)}")
    print(f"UVMin={uv_minimum}|UVMax={uv_maximum}")
    print(f"ReimportVertices={reimport_result[0]}|ReimportFaces={reimport_result[1]}|ReimportUVLayers={reimport_result[2]}")
    for role, (_, output_name, _) in TEXTURES.items():
        print(f"{role}Sha256={file_sha256(os.path.join(OUTPUT_ROOT, output_name))}")
    print(f"Blend={BLEND_PATH}")
    print(f"Fbx={FBX_PATH}")
    print(f"Review={REVIEW_PATH}")


if __name__ == "__main__":
    main()
