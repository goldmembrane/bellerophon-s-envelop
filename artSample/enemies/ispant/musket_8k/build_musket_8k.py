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
UNITY_DIRECT_FBX_PATH = r"D:\Bellerophon2\Bellerophon\Assets\_Project\Art\Enemies\Ispant\Models\Ispant_New_Direct_Source.fbx"
UNITY_BASECOLOR_PATH = r"D:\Bellerophon2\Bellerophon\Assets\_Project\Art\Enemies\Ispant\Models\Textures\Ispant_New_Direct_Source_BaseColor.png"
TEXTURE_ROOT = r"C:\Users\gus68\Downloads\Meshy_AI_Crescent_Iron_Sentine_biped (1)\Meshy_AI_Crescent_Iron_Sentine_biped"
OUTPUT_ROOT = r"D:\Bellerophon2\Bellerophon\artSample\enemies\ispant\musket_8k"
BLEND_PATH = os.path.join(OUTPUT_ROOT, "Ispant_Musket_8K_Textured.blend")
FBX_PATH = os.path.join(OUTPUT_ROOT, "Ispant_Musket_8K_Textured.fbx")
REVIEW_PATH = os.path.join(OUTPUT_ROOT, "Ispant_Musket_8K_Review.png")

EXPECTED_SEGMENT_SHA256 = "EAEB45D54E510A5CABFDAF9C36A26606A04518D8F812E1C1B8B5B84C645A0EF0"
EXPECTED_SOURCE_FBX_SHA256 = "7B682C49CDB2F4A563B736857E544DD7FCCD7213A5C375DEB92F56CE3A2E51B7"
EXPECTED_UNITY_BASECOLOR_SHA256 = "7DE6705FB7BD60E2D347023EFE51E96598845D611631F40D01C64EACC5249570"
TEXTURES = {
    "BaseColor": (
        "Meshy_AI_Crescent_Iron_Sentine_biped_texture_0.png",
        "Ispant_Musket_8K_BaseColor.png",
        "7DE6705FB7BD60E2D347023EFE51E96598845D611631F40D01C64EACC5249570",
    ),
    "Metallic": (
        "Meshy_AI_Crescent_Iron_Sentine_biped_texture_0_metallic.png",
        "Ispant_Musket_8K_Metallic.png",
        "674812FCDE6B2879D15E40BDCE0BDC1BB152C75D7B74AC3371B2C96BE478920D",
    ),
    "Normal": (
        "Meshy_AI_Crescent_Iron_Sentine_biped_texture_0_normal.png",
        "Ispant_Musket_8K_Normal.png",
        "11F5A8254E2FA46BF5F7EC49426F1BAD8F49CA254264EFE9FA15A73731E50C07",
    ),
    "Roughness": (
        "Meshy_AI_Crescent_Iron_Sentine_biped_texture_0_roughness.png",
        "Ispant_Musket_8K_Roughness.png",
        "45B468DCDC7E5624A0D74ED639F586759B682BB67DED3A0666C1104889689432",
    ),
}
MUSKET_UV_ISLANDS = {
    0, 1, 2, 3, 4, 5, 7, 44, 45, 46, 50, 53, 54, 67, 91, 115, 130,
    136, 137, 160, 185, 186, 214, 216, 243, 246, 275, 279, 312, 344,
    353, 379, 390, 429, 463, 470, 505, 540, 550, 571, 611, 617, 618,
    619, 620, 621, 622,
}
EXPECTED_SOURCE_FACES = 10028
EXPECTED_MUSKET_UV_FACES = 541
DECIMATE_RATIO = 0.0837
EXPECTED_ORIGINAL_VERTICES = 95624
EXPECTED_ORIGINAL_FACES = 191272
EXPECTED_TARGET_VERTICES = 7992
EXPECTED_TARGET_FACES = 16008
REGISTRATION_SIGNS = np.array((1.0, 1.0, -1.0), dtype=np.float64)
ICP_ITERATIONS = 15
ICP_KEEP_PERCENTILE = 72.0


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


def separate_source_musket(source, selected_faces):
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
    if sum(1 for face in edit_mesh.faces if face.select) != EXPECTED_MUSKET_UV_FACES:
        raise RuntimeError("Source musket face selection changed")
    bpy.ops.mesh.separate(type="SELECTED")
    bpy.ops.object.mode_set(mode="OBJECT")
    created = list(set(bpy.data.objects) - before)
    if len(created) != 1:
        raise RuntimeError("Source musket separation did not produce one object")
    musket = created[0]
    musket.name = "UV_Source_Ispant_Musket_Exact"
    bpy.data.objects.remove(working, do_unlink=True)
    return musket


def connected_components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        first, second = edge.vertices
        adjacency[first].add(second)
        adjacency[second].add(first)
    remaining = set(range(len(mesh.vertices)))
    sizes = []
    while remaining:
        stack = [remaining.pop()]
        size = 1
        while stack:
            current = stack.pop()
            neighbors = adjacency[current] & remaining
            remaining.difference_update(neighbors)
            stack.extend(neighbors)
            size += len(neighbors)
        sizes.append(size)
    return sorted(sizes, reverse=True)


def world_points(obj):
    return np.array([tuple(obj.matrix_world @ vertex.co) for vertex in obj.data.vertices], dtype=np.float64)


def canonicalize(points):
    center = points.mean(axis=0)
    centered = points - center
    covariance = np.cov(centered, rowvar=False)
    eigenvalues, eigenvectors = np.linalg.eigh(covariance)
    basis = eigenvectors[:, np.argsort(eigenvalues)[::-1]]
    if np.linalg.det(basis) < 0.0:
        basis[:, 2] *= -1.0
    local = centered @ basis
    minimum = local.min(axis=0)
    maximum = local.max(axis=0)
    return (local - (minimum + maximum) * 0.5) / (maximum - minimum)


def make_proxy(source, name, coordinates):
    proxy = source.copy()
    proxy.data = source.data.copy()
    proxy.name = name
    bpy.context.scene.collection.objects.link(proxy)
    proxy.modifiers.clear()
    proxy.matrix_world = Matrix.Identity(4)
    for index, coordinate in enumerate(coordinates):
        proxy.data.vertices[index].co = coordinate
    return proxy


def nearest_surface(points, bvh):
    nearest_points = []
    distances = []
    for point in points:
        nearest = bvh.find_nearest(Vector(tuple(point)))
        nearest_points.append(tuple(nearest[0]))
        distances.append(nearest[3])
    return np.array(nearest_points), np.array(distances)


def similarity_step(points, targets, keep):
    source = points[keep]
    destination = targets[keep]
    source_center = source.mean(axis=0)
    destination_center = destination.mean(axis=0)
    source_zero = source - source_center
    destination_zero = destination - destination_center
    u, singular, vt = np.linalg.svd(source_zero.T @ destination_zero)
    rotation = u @ vt
    if np.linalg.det(rotation) < 0.0:
        u[:, -1] *= -1.0
        rotation = u @ vt
    scale = singular.sum() / np.square(source_zero).sum()
    return source_center, destination_center, rotation, scale


def apply_similarity(points, step):
    source_center, destination_center, rotation, scale = step
    return (points - source_center) @ rotation * scale + destination_center


def transfer_source_uv(source, target):
    source_coordinates = canonicalize(world_points(source))
    initial_coordinates = canonicalize(world_points(target)) * REGISTRATION_SIGNS
    source_proxy = make_proxy(source, "_UV_Source_Canonical", source_coordinates)
    source_bvh = BVHTree.FromPolygons(
        [Vector(tuple(point)) for point in source_coordinates],
        [tuple(polygon.vertices) for polygon in source_proxy.data.polygons],
        all_triangles=True,
    )
    sample_step = max(1, len(initial_coordinates) // 6000)
    sample = initial_coordinates[::sample_step].copy()
    improved_coordinates = initial_coordinates.copy()
    _, initial_distances = nearest_surface(sample, source_bvh)
    for _ in range(ICP_ITERATIONS):
        nearest_points, distances = nearest_surface(sample, source_bvh)
        threshold = np.percentile(distances, ICP_KEEP_PERCENTILE)
        keep = distances <= threshold
        step = similarity_step(sample, nearest_points, keep)
        sample = apply_similarity(sample, step)
        improved_coordinates = apply_similarity(improved_coordinates, step)
    _, final_distances = nearest_surface(improved_coordinates[::sample_step], source_bvh)
    target_proxy = make_proxy(target, "_UV_Target_ICP_Canonical", improved_coordinates)
    while target_proxy.data.uv_layers:
        target_proxy.data.uv_layers.remove(target_proxy.data.uv_layers[0])
    source_uv = source_proxy.data.uv_layers.active
    if source_uv is None:
        raise RuntimeError("Source musket UV is missing")
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
    per_vertex_uv = [set() for _ in target.data.vertices]
    for loop in target.data.loops:
        uv = target_uv.data[loop.index].uv
        per_vertex_uv[loop.vertex_index].add((round(uv.x, 7), round(uv.y, 7)))
    registration = {
        "initial_mean": float(initial_distances.mean()),
        "initial_p95": float(np.percentile(initial_distances, 95.0)),
        "final_mean": float(final_distances.mean()),
        "final_p95": float(np.percentile(final_distances, 95.0)),
    }
    return minimum, maximum, sum(len(values) for values in per_vertex_uv), registration


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


def create_unity_direct_material(images):
    material = bpy.data.materials.new("Ispant_Musket_8K_UnityDirect")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    output.location = (500, 0)
    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (220, 0)
    base = nodes.new("ShaderNodeTexImage")
    base.name = "BaseColor"
    base.image = images["BaseColor"]
    base.location = (-220, 0)
    principled.inputs["Metallic"].default_value = 1.0
    principled.inputs["Roughness"].default_value = 1.0
    links.new(base.outputs["Color"], principled.inputs["Base Color"])
    links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    return material


def world_bounds(obj):
    points = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    minimum = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    maximum = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    return minimum, maximum


def clay_material():
    material = bpy.data.materials.new("OriginalSegmentClay")
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (0.12, 0.16, 0.19, 1.0)
    principled.inputs["Metallic"].default_value = 0.65
    principled.inputs["Roughness"].default_value = 0.32
    return material


def label_material():
    material = bpy.data.materials.new("ReviewLabel")
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (0.75, 0.82, 0.88, 1.0)
    principled.inputs["Roughness"].default_value = 0.5
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


def create_review_render(unity_source, selected_faces, textured, unity_material):
    for obj in bpy.context.scene.objects:
        obj.hide_render = True

    source_points = world_points(unity_source)
    source_review_coordinates = np.column_stack(
        (source_points[:, 0], source_points[:, 2], -source_points[:, 1])
    )
    reference = make_proxy(unity_source, "Review_Unity_Placed_Source", source_review_coordinates)
    reference.hide_render = False
    reference.data.materials.clear()
    reference.data.materials.append(clay_material())
    reference.data.materials.append(unity_material)
    for polygon in reference.data.polygons:
        polygon.material_index = 1 if polygon.index in selected_faces else 0
    reference_minimum, reference_maximum = world_bounds(reference)
    reference_center = (reference_minimum + reference_maximum) * 0.5
    reference_scale = 3.0 / (reference_maximum.y - reference_minimum.y)
    reference.matrix_world = (
        Matrix.Translation(Vector((-3.8, 0.35, 0.0)))
        @ Matrix.Scale(reference_scale, 4)
        @ Matrix.Translation(-reference_center)
    )

    top = world_baked_review_copy(textured, "Review_Textured_Top")
    top.hide_render = False
    position_review_object(top, (0.0, 0.45, 0.0), 3.2)

    angled = world_baked_review_copy(textured, "Review_Textured_Angled")
    angled.hide_render = False
    position_review_object(angled, (3.8, 0.45, 0.0), 3.2, rotation_x=0.58)

    labels = label_material()
    for x, label_text in (
        (-3.8, "UNITY PLACED SOURCE"),
        (0.0, "UNITY-MATCHED 7,992 VERTS"),
        (3.8, "UNITY MATERIAL 3/4 VIEW"),
    ):
        bpy.ops.object.text_add(location=(x, -1.38, 0.05))
        label = bpy.context.object
        label.data.body = label_text
        label.data.align_x = "CENTER"
        label.data.size = 0.17
        label.data.materials.append(labels)
        label.hide_render = False

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
        key_data = bpy.data.lights.new(f"Key_{x}", type="AREA")
        key_data.energy = 850.0
        key_data.shape = "DISK"
        key_data.size = 3.2
        key = bpy.data.objects.new(f"Key_{x}", key_data)
        bpy.context.scene.collection.objects.link(key)
        key.location = (x - 0.6, -0.3, 4.5)
        key.hide_render = False

        rim_data = bpy.data.lights.new(f"Rim_{x}", type="AREA")
        rim_data.energy = 500.0
        rim_data.size = 2.0
        rim = bpy.data.objects.new(f"Rim_{x}", rim_data)
        bpy.context.scene.collection.objects.link(rim)
        rim.location = (x + 1.1, 0.8, 2.8)
        rim.hide_render = False

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
    scene.world.color = (0.006, 0.008, 0.012)
    bpy.ops.render.render(write_still=True)


def validate_export():
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=FBX_PATH, use_anim=False)
    imported = [obj for obj in set(bpy.data.objects) - before if obj.type == "MESH"]
    if len(imported) != 1:
        raise RuntimeError(f"Expected one reimported mesh, found {len(imported)}")
    mesh = imported[0].data
    materials = [material for material in mesh.materials if material is not None]
    image_nodes = []
    metallic = None
    roughness = None
    for material in materials:
        if not material.use_nodes:
            continue
        for node in material.node_tree.nodes:
            if node.type == "TEX_IMAGE" and node.image is not None:
                image_nodes.append(node.image.name)
            elif node.type == "BSDF_PRINCIPLED":
                metallic = float(node.inputs["Metallic"].default_value)
                roughness = float(node.inputs["Roughness"].default_value)
    result = (
        len(mesh.vertices),
        len(mesh.polygons),
        len(mesh.uv_layers),
        connected_components(mesh),
        len(materials),
        len(image_nodes),
        metallic,
        roughness,
    )
    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)
    if result[:3] != (EXPECTED_TARGET_VERTICES, EXPECTED_TARGET_FACES, 1):
        raise RuntimeError(f"FBX reimport differs: {result}")
    if result[3] != [EXPECTED_TARGET_VERTICES]:
        raise RuntimeError(f"FBX reimport component changed: {result[3]}")
    if result[4] != 1 or result[5] < 1:
        raise RuntimeError(f"FBX reimport material differs: {result[4:6]}")
    return result


def main():
    os.makedirs(OUTPUT_ROOT, exist_ok=True)
    if file_sha256(SEGMENT_PATH) != EXPECTED_SEGMENT_SHA256:
        raise RuntimeError("Segment GLB hash changed")
    if file_sha256(SOURCE_FBX_PATH) != EXPECTED_SOURCE_FBX_SHA256:
        raise RuntimeError("Source FBX hash changed")
    if file_sha256(UNITY_DIRECT_FBX_PATH) != EXPECTED_SOURCE_FBX_SHA256:
        raise RuntimeError("Unity direct FBX differs from the user source")
    if file_sha256(UNITY_BASECOLOR_PATH) != EXPECTED_UNITY_BASECOLOR_SHA256:
        raise RuntimeError("Unity direct BaseColor changed")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=SEGMENT_PATH)
    segment = bpy.data.objects.get("mesh_0")
    if segment is None:
        raise RuntimeError("mesh_0 is missing")
    baked_mesh = segment.data.copy()
    baked_mesh.transform(segment.matrix_world)
    segment.data = baked_mesh
    segment.parent = None
    segment.matrix_world = Matrix.Identity(4)
    for obj in list(bpy.context.scene.objects):
        if obj != segment:
            bpy.data.objects.remove(obj, do_unlink=True)
    segment.name = "Ispant_Musket_Segment_Original_95624"
    if (len(segment.data.vertices), len(segment.data.polygons)) != (
        EXPECTED_ORIGINAL_VERTICES,
        EXPECTED_ORIGINAL_FACES,
    ):
        raise RuntimeError("Original musket geometry count changed")
    if connected_components(segment.data) != [EXPECTED_ORIGINAL_VERTICES]:
        raise RuntimeError("Original musket is no longer one connected component")

    target = segment.copy()
    target.data = segment.data.copy()
    target.name = "Ispant_Musket_8K_Textured"
    bpy.context.scene.collection.objects.link(target)
    bpy.ops.object.select_all(action="DESELECT")
    target.select_set(True)
    bpy.context.view_layer.objects.active = target
    decimate = target.modifiers.new(name="Target8K", type="DECIMATE")
    decimate.decimate_type = "COLLAPSE"
    decimate.ratio = DECIMATE_RATIO
    decimate.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    if (len(target.data.vertices), len(target.data.polygons)) != (
        EXPECTED_TARGET_VERTICES,
        EXPECTED_TARGET_FACES,
    ):
        raise RuntimeError("Decimated geometry count changed")
    if connected_components(target.data) != [EXPECTED_TARGET_VERTICES]:
        raise RuntimeError("Decimated musket is no longer one connected component")

    bpy.ops.import_scene.fbx(filepath=SOURCE_FBX_PATH, use_anim=False)
    source_mesh = bpy.data.objects.get("char1")
    if source_mesh is None or len(source_mesh.data.polygons) != EXPECTED_SOURCE_FACES:
        raise RuntimeError("Source char1 mesh changed")
    source_mesh.modifiers.clear()
    islands = uv_islands(source_mesh.data)
    selected_faces = {face for island in MUSKET_UV_ISLANDS for face in islands[island]}
    if len(selected_faces) != EXPECTED_MUSKET_UV_FACES:
        raise RuntimeError("Source musket UV-island selection changed")
    source_musket = separate_source_musket(source_mesh, selected_faces)
    source_musket.modifiers.clear()
    for obj in list(bpy.context.scene.objects):
        if obj not in (segment, target, source_mesh, source_musket):
            bpy.data.objects.remove(obj, do_unlink=True)

    uv_minimum, uv_maximum, per_vertex_uv_count, registration = transfer_source_uv(source_musket, target)
    if not all(0.0 <= value <= 1.0 for value in (*uv_minimum, *uv_maximum)):
        raise RuntimeError("Transferred UV is outside the texture tile")

    images = copy_and_load_textures()
    if file_sha256(os.path.join(OUTPUT_ROOT, TEXTURES["BaseColor"][1])) != file_sha256(UNITY_BASECOLOR_PATH):
        raise RuntimeError("Sample BaseColor differs from the Unity-placed BaseColor")
    material = create_unity_direct_material(images)
    target.data.materials.clear()
    target.data.materials.append(material)
    for polygon in target.data.polygons:
        polygon.use_smooth = True

    segment.hide_viewport = True
    segment.hide_render = True
    source_musket.hide_viewport = True
    source_musket.hide_render = True
    source_mesh.hide_viewport = True
    source_mesh.hide_render = True
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
    create_review_render(source_mesh, selected_faces, target, material)

    print("===ISPANT_MUSKET_8K_PASS===")
    print(f"SegmentSha256={file_sha256(SEGMENT_PATH)}")
    print(f"SourceFbxSha256={file_sha256(SOURCE_FBX_PATH)}")
    print(f"OriginalVertices={EXPECTED_ORIGINAL_VERTICES}|OriginalFaces={EXPECTED_ORIGINAL_FACES}")
    print(f"TargetVertices={len(target.data.vertices)}|TargetFaces={len(target.data.polygons)}")
    print(f"Components={connected_components(target.data)}")
    print(f"MusketUVIslands={len(MUSKET_UV_ISLANDS)}|MusketUVFaces={EXPECTED_MUSKET_UV_FACES}")
    print(f"UVMin={uv_minimum}|UVMax={uv_maximum}|PerVertexUVCount={per_vertex_uv_count}")
    print(
        f"RegistrationInitialMean={registration['initial_mean']:.9f}|"
        f"RegistrationInitialP95={registration['initial_p95']:.9f}|"
        f"RegistrationFinalMean={registration['final_mean']:.9f}|"
        f"RegistrationFinalP95={registration['final_p95']:.9f}"
    )
    print("MaterialProfile=UnityDirect|BaseColorLinked=1|Metallic=1.0|Roughness=1.0|ExtraMapsLinked=0")
    print(
        f"ReimportVertices={reimport_result[0]}|ReimportFaces={reimport_result[1]}|"
        f"ReimportUVLayers={reimport_result[2]}|ReimportComponents={reimport_result[3]}|"
        f"ReimportMaterials={reimport_result[4]}|ReimportImageNodes={reimport_result[5]}|"
        f"ReimportMetallic={reimport_result[6]}|ReimportRoughness={reimport_result[7]}"
    )
    for role, (_, output_name, _) in TEXTURES.items():
        print(f"{role}Sha256={file_sha256(os.path.join(OUTPUT_ROOT, output_name))}")
    print(f"FbxSha256={file_sha256(FBX_PATH)}")
    print(f"Blend={BLEND_PATH}")
    print(f"Fbx={FBX_PATH}")
    print(f"Review={REVIEW_PATH}")


if __name__ == "__main__":
    main()
