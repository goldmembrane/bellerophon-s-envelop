import bpy
import bmesh
import collections
import hashlib
import itertools
import math
import os
import shutil
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree


PROJECT_ROOT = r"D:\Bellerophon2\Bellerophon"
ORIGINAL_SOURCE_PATH = os.path.join(PROJECT_ROOT, "enemies model", "išpant-new.fbx")
APPROVED_SWORD_PATH = os.path.join(
    PROJECT_ROOT,
    "artSample",
    "enemies",
    "ispant",
    "long_sword_10k",
    "Ispant_LongSword_10K_Textured.fbx",
)
BODY_SAMPLE_PATH = os.path.join(
    PROJECT_ROOT,
    "artSample",
    "enemies",
    "ispant",
    "long_sword_separation",
    "Ispant_LongSword_Separated_Sample.fbx",
)
UNITY_SOURCE_PATH = os.path.join(
    PROJECT_ROOT,
    "Assets",
    "_Project",
    "Art",
    "Enemies",
    "Ispant",
    "Models",
    "Ispant_New_Direct_Source.fbx",
)
UNITY_SWORD_TEXTURE_ROOT = os.path.join(
    os.path.dirname(UNITY_SOURCE_PATH),
    "Textures",
    "Ispant_LongSword_10K",
)
UNITY_SWORD_MATERIAL_PATH = os.path.join(
    os.path.dirname(UNITY_SOURCE_PATH),
    "Materials",
    "Ispant_LongSword_10K_PBR.mat",
)
UNITY_SWORD_SHADER_PATH = os.path.join(
    os.path.dirname(UNITY_SOURCE_PATH),
    "Shaders",
    "IspantLongSwordPBR.shader",
)
UNITY_SOURCE_META_PATH = UNITY_SOURCE_PATH + ".meta"
OUTPUT_ROOT = os.path.dirname(APPROVED_SWORD_PATH)
PLACEMENT_BLEND_PATH = os.path.join(OUTPUT_ROOT, "Ispant_LongSword_10K_UnityPlacement.blend")
PLACEMENT_FBX_PATH = os.path.join(OUTPUT_ROOT, "Ispant_LongSword_10K_UnityPlacement.fbx")
PLACEMENT_REVIEW_PATH = os.path.join(OUTPUT_ROOT, "Ispant_LongSword_10K_UnityPlacement_Review.png")
UNITY_TEMP_PATH = os.path.join(
    os.path.dirname(UNITY_SOURCE_PATH),
    "Ispant_New_Direct_Source.__approved_sword_tmp.fbx",
)

EXPECTED_ORIGINAL_SHA256 = "7B682C49CDB2F4A563B736857E544DD7FCCD7213A5C375DEB92F56CE3A2E51B7"
EXPECTED_APPROVED_SWORD_SHA256 = "2A6ECA3E5CD74C2E40B03BA5B2D4FD51E2B1E73B7BA2431B7E9D4BE5C6579252"
EXPECTED_BODY_SAMPLE_SHA256 = "4E5507C789A1E43A743597B7D90042F9CA0333CD63775C9A601DE9A7C5A073CD"
EXPECTED_UNITY_REPLACEMENT_SOURCE_SHA256 = "5CE54F6117AF08F141BC18A0E46C823AD07877D815DA2906D59CA2967A4974FF"

SWORD_UV_ISLANDS = {
    488, 491, 492, 493, 494, 495, 501, 515, 516, 520,
    537, 539, 543, 547, 552, 553, 556, 559, 560, 564,
    586, 593, 598, 602, 606, 607, 608, 613, 616,
}
EXPECTED_REFERENCE_FACES = 242
EXPECTED_APPROVED_VERTICES = 9975
EXPECTED_APPROVED_FACES = 19950
EXPECTED_BODY_VERTICES = 4895
EXPECTED_BODY_FACES = 9798
EXPECTED_BODY_UV_LAYERS = 1
EXPECTED_BODY_MATERIALS = 1
EXPECTED_ARMATURE_BONES = 24
EXPECTED_BODY_VERTEX_GROUPS = 22
EXPECTED_ALIGNMENT_SCALE = 12.345491599084
MAX_NORMALIZED_ALIGNMENT_ERROR = 0.025
MAX_FBX_POSITION_ERROR = 0.00005

APPROVED_TEXTURES = {
    "Ispant_LongSword_10K_BaseColor.png": "7DE6705FB7BD60E2D347023EFE51E96598845D611631F40D01C64EACC5249570",
    "Ispant_LongSword_10K_Metallic.png": "674812FCDE6B2879D15E40BDCE0BDC1BB152C75D7B74AC3371B2C96BE478920D",
    "Ispant_LongSword_10K_Normal.png": "11F5A8254E2FA46BF5F7EC49426F1BAD8F49CA254264EFE9FA15A73731E50C07",
    "Ispant_LongSword_10K_Roughness.png": "45B468DCDC7E5624A0D74ED639F586759B682BB67DED3A0666C1104889689432",
}


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
        island_faces = []
        while pending:
            face = pending.pop()
            island_faces.append(face)
            for neighbor in adjacency[face]:
                if neighbor not in visited:
                    visited.add(neighbor)
                    pending.append(neighbor)
        result.append(island_faces)
    return result


def separate_reference_sword(source):
    islands = uv_islands(source.data)
    selected_faces = {
        face
        for island_index in SWORD_UV_ISLANDS
        for face in islands[island_index]
    }
    if len(selected_faces) != EXPECTED_REFERENCE_FACES:
        raise RuntimeError(f"Reference sword face count changed: {len(selected_faces)}")

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
    if sum(1 for face in edit_mesh.faces if face.select) != EXPECTED_REFERENCE_FACES:
        raise RuntimeError("Reference sword selection changed in Edit Mode")
    bpy.ops.mesh.separate(type="SELECTED")
    bpy.ops.object.mode_set(mode="OBJECT")
    created = list(set(bpy.data.objects) - before)
    if len(created) != 1:
        raise RuntimeError("Reference sword separation did not create exactly one object")
    sword = created[0]
    sword.name = "Ispant_Original_LongSword_Placement_Reference"
    bpy.data.objects.remove(working, do_unlink=True)
    return sword


def world_points(obj):
    return np.array(
        [tuple(obj.matrix_world @ vertex.co) for vertex in obj.data.vertices],
        dtype=np.float64,
    )


def world_polygons(obj):
    points = world_points(obj)
    polygons = [list(polygon.vertices) for polygon in obj.data.polygons]
    return points, polygons


def pca_frame(points):
    center = points.mean(axis=0)
    centered = points - center
    covariance = np.cov(centered, rowvar=False)
    eigenvalues, eigenvectors = np.linalg.eigh(covariance)
    order = np.argsort(eigenvalues)[::-1]
    basis = eigenvectors[:, order]
    if np.linalg.det(basis) < 0.0:
        basis[:, 2] *= -1.0
    local = centered @ basis
    extents = np.ptp(local, axis=0)
    return center, basis, extents


def bvh_from_points(points, polygons):
    return BVHTree.FromPolygons(
        [Vector(tuple(point)) for point in points],
        polygons,
        all_triangles=False,
    )


def nearest_surface_points(bvh, points):
    nearest = []
    distances = []
    for point in points:
        result = bvh.find_nearest(Vector(tuple(point)))
        if result is None:
            raise RuntimeError("BVH nearest-surface lookup failed")
        nearest.append(tuple(result[0]))
        distances.append(result[3])
    return np.array(nearest, dtype=np.float64), np.array(distances, dtype=np.float64)


def umeyama_similarity(source, target):
    source_center = source.mean(axis=0)
    target_center = target.mean(axis=0)
    source_centered = source - source_center
    target_centered = target - target_center
    covariance = source_centered.T @ target_centered
    left, singular, right_transposed = np.linalg.svd(covariance)
    row_rotation = left @ right_transposed
    if np.linalg.det(row_rotation) < 0.0:
        right_transposed[-1, :] *= -1.0
        singular[-1] *= -1.0
        row_rotation = left @ right_transposed
    scale = singular.sum() / np.square(source_centered).sum()
    translation = target_center - scale * (source_center @ row_rotation)
    return scale, row_rotation.T, translation


def rigid_transform_with_fixed_scale(source, target, scale):
    scaled_source = source * scale
    source_center = scaled_source.mean(axis=0)
    target_center = target.mean(axis=0)
    source_centered = scaled_source - source_center
    target_centered = target - target_center
    covariance = source_centered.T @ target_centered
    left, _, right_transposed = np.linalg.svd(covariance)
    row_rotation = left @ right_transposed
    if np.linalg.det(row_rotation) < 0.0:
        right_transposed[-1, :] *= -1.0
        row_rotation = left @ right_transposed
    translation = target_center - source_center @ row_rotation
    return row_rotation.T, translation


def apply_similarity(points, scale, rotation, translation):
    return scale * (points @ rotation.T) + translation


def similarity_matrix(scale, rotation, translation):
    matrix = Matrix.Identity(4)
    for row in range(3):
        for column in range(3):
            matrix[row][column] = float(scale * rotation[row, column])
        matrix[row][3] = float(translation[row])
    return matrix


def alignment_metrics(source_points, source_polygons, target_points, target_polygons):
    target_bvh = bvh_from_points(target_points, target_polygons)
    source_center, source_basis, source_extents = pca_frame(source_points)
    target_center, target_basis, target_extents = pca_frame(target_points)
    scale = float(target_extents[0] / source_extents[0])
    sampled_indices = np.linspace(
        0,
        len(source_points) - 1,
        min(2500, len(source_points)),
        dtype=np.int64,
    )
    source_sample = source_points[sampled_indices]

    candidates = []
    for signs in itertools.product((-1.0, 1.0), repeat=3):
        if math.prod(signs) < 0.0:
            continue
        rotation = target_basis @ np.diag(signs) @ source_basis.T
        translation = target_center - scale * (rotation @ source_center)
        transformed = apply_similarity(source_sample, scale, rotation, translation)
        _, distances = nearest_surface_points(target_bvh, transformed)
        candidates.append((float(np.mean(distances)), scale, rotation, translation, signs))
    _, scale, rotation, translation, signs = min(candidates, key=lambda item: item[0])

    previous_error = math.inf
    for _ in range(30):
        transformed = apply_similarity(source_sample, scale, rotation, translation)
        nearest, distances = nearest_surface_points(target_bvh, transformed)
        cutoff = np.percentile(distances, 90.0)
        keep = distances <= cutoff
        rotation, translation = rigid_transform_with_fixed_scale(
            source_sample[keep],
            nearest[keep],
            scale,
        )
        error = float(np.mean(distances[keep]))
        if abs(previous_error - error) < 1e-10:
            break
        previous_error = error

    transformed_all = apply_similarity(source_points, scale, rotation, translation)
    source_bounds_center = (transformed_all.min(axis=0) + transformed_all.max(axis=0)) * 0.5
    target_bounds_center = (target_points.min(axis=0) + target_points.max(axis=0)) * 0.5
    translation += target_bounds_center - source_bounds_center
    transformed_all = apply_similarity(source_points, scale, rotation, translation)
    _, forward_distances = nearest_surface_points(target_bvh, transformed_all)
    transformed_source_bvh = bvh_from_points(transformed_all, source_polygons)
    _, reverse_distances = nearest_surface_points(transformed_source_bvh, target_points)
    target_length = float(np.max(target_extents))
    return {
        "scale": scale,
        "rotation": rotation,
        "translation": translation,
        "matrix": similarity_matrix(scale, rotation, translation),
        "pca_signs": signs,
        "forward_mean": float(np.mean(forward_distances)),
        "forward_p95": float(np.percentile(forward_distances, 95.0)),
        "forward_max": float(np.max(forward_distances)),
        "reverse_mean": float(np.mean(reverse_distances)),
        "reverse_p95": float(np.percentile(reverse_distances, 95.0)),
        "reverse_max": float(np.max(reverse_distances)),
        "target_length": target_length,
        "normalized_bidirectional_mean": float(
            (np.mean(forward_distances) + np.mean(reverse_distances)) * 0.5 / target_length
        ),
    }


def compute_placement_alignment():
    if file_sha256(ORIGINAL_SOURCE_PATH) != EXPECTED_ORIGINAL_SHA256:
        raise RuntimeError("Original Ispant source hash changed")
    if file_sha256(APPROVED_SWORD_PATH) != EXPECTED_APPROVED_SWORD_SHA256:
        raise RuntimeError("Approved sword FBX hash changed")
    if file_sha256(UNITY_SOURCE_PATH) != EXPECTED_UNITY_REPLACEMENT_SOURCE_SHA256:
        raise RuntimeError("Unity Ispant FBX is not the approved sword-replacement source state")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=ORIGINAL_SOURCE_PATH, use_anim=False)
    source = bpy.data.objects.get("char1")
    if source is None:
        raise RuntimeError("Original char1 is missing")
    reference_sword = separate_reference_sword(source)
    reference_points, reference_polygons = world_polygons(reference_sword)

    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=APPROVED_SWORD_PATH, use_anim=False)
    imported = [
        obj for obj in set(bpy.data.objects) - before
        if obj.type == "MESH"
    ]
    if len(imported) != 1:
        raise RuntimeError(f"Approved sword import count changed: {len(imported)}")
    approved = imported[0]
    if len(approved.data.vertices) != EXPECTED_APPROVED_VERTICES:
        raise RuntimeError("Approved sword vertex count changed")
    if len(approved.data.polygons) != EXPECTED_APPROVED_FACES:
        raise RuntimeError("Approved sword face count changed")
    approved_points, approved_polygons = world_polygons(approved)
    metrics = alignment_metrics(
        approved_points,
        approved_polygons,
        reference_points,
        reference_polygons,
    )
    return metrics, reference_points, approved_points


def analyze_placement():
    metrics, reference_points, approved_points = compute_placement_alignment()

    transformed = apply_similarity(
        approved_points,
        metrics["scale"],
        metrics["rotation"],
        metrics["translation"],
    )
    minimum = transformed.min(axis=0)
    maximum = transformed.max(axis=0)
    print("===ISPANT_APPROVED_LONG_SWORD_PLACEMENT_ANALYSIS===")
    print(f"ApprovedSwordSha256={file_sha256(APPROVED_SWORD_PATH)}")
    print(f"ApprovedVertices={EXPECTED_APPROVED_VERTICES}")
    print(f"ApprovedFaces={EXPECTED_APPROVED_FACES}")
    print(f"ReferenceFaces={EXPECTED_REFERENCE_FACES}")
    print(f"PcaSigns={metrics['pca_signs']}")
    print(f"UniformScale={metrics['scale']:.12f}")
    print("Rotation=" + repr(metrics["rotation"].tolist()))
    print("Translation=" + repr(metrics["translation"].tolist()))
    print(f"ForwardMean={metrics['forward_mean']:.9f}")
    print(f"ForwardP95={metrics['forward_p95']:.9f}")
    print(f"ForwardMax={metrics['forward_max']:.9f}")
    print(f"ReverseMean={metrics['reverse_mean']:.9f}")
    print(f"ReverseP95={metrics['reverse_p95']:.9f}")
    print(f"ReverseMax={metrics['reverse_max']:.9f}")
    print(f"NormalizedBidirectionalMean={metrics['normalized_bidirectional_mean']:.9f}")
    print("PlacedBoundsMin=" + ",".join(f"{value:.9f}" for value in minimum))
    print("PlacedBoundsMax=" + ",".join(f"{value:.9f}" for value in maximum))


def face_signature(obj, polygon, include_coordinates=True):
    mesh = obj.data
    uv_data = mesh.uv_layers.active.data
    group_name_by_index = {group.index: group.name for group in obj.vertex_groups}
    loop_values = []
    for loop_index in polygon.loop_indices:
        vertex_index = mesh.loops[loop_index].vertex_index
        coordinate = obj.matrix_world @ mesh.vertices[vertex_index].co
        uv = uv_data[loop_index].uv
        weights = tuple(sorted(
            (group_name_by_index[membership.group], round(membership.weight, 5))
            for membership in mesh.vertices[vertex_index].groups
            if membership.weight > 0.00001
        ))
        value = (
            tuple(round(value, 5) for value in uv),
            weights,
        )
        if include_coordinates:
            value = (tuple(round(value, 4) for value in coordinate),) + value
        loop_values.append(value)
    rotations = [
        tuple(loop_values[index:] + loop_values[:index])
        for index in range(len(loop_values))
    ]
    return polygon.material_index, min(rotations)


def face_signatures(obj, include_coordinates=True):
    return collections.Counter(
        face_signature(obj, polygon, include_coordinates)
        for polygon in obj.data.polygons
    )


def mesh_topology(obj):
    return tuple(tuple(polygon.vertices) for polygon in obj.data.polygons)


def validate_texture_sources():
    for file_name, expected_hash in APPROVED_TEXTURES.items():
        path = os.path.join(OUTPUT_ROOT, file_name)
        if file_sha256(path) != expected_hash:
            raise RuntimeError(f"Approved texture hash changed: {file_name}")


def copy_approved_textures_to_unity():
    os.makedirs(UNITY_SWORD_TEXTURE_ROOT, exist_ok=True)
    copied = []
    for file_name, expected_hash in APPROVED_TEXTURES.items():
        source = os.path.join(OUTPUT_ROOT, file_name)
        target = os.path.join(UNITY_SWORD_TEXTURE_ROOT, file_name)
        shutil.copy2(source, target)
        actual_hash = file_sha256(target)
        if actual_hash != expected_hash:
            raise RuntimeError(f"Unity texture copy differs from approved source: {file_name}")
        copied.append((target, actual_hash))
    return copied


def validate_unity_material_binding():
    required_fragments = {
        UNITY_SWORD_SHADER_PATH: (
            'Shader "Bellerophon/Ispant/LongSwordApprovedPBR"',
            "_BaseMap",
            "_MetallicMap",
            "_RoughnessMap",
            "_NormalMap",
            "_StudioReflection",
        ),
        UNITY_SWORD_MATERIAL_PATH: (
            "guid: e71bb67bb49949d688c65d03541cf7f3",
            "guid: 8791355503803e742a02cd88dbf8f023",
            "guid: 0ee137432fe6cc2439507c84578b3e6c",
            "guid: 19c4241782d36ef46bf1d98ec3dee43f",
            "guid: d1ea752f4cd6da74b82d3f88e1ecea88",
            "_NormalStrength: 1",
            "_StudioReflection: 3.3",
        ),
        UNITY_SOURCE_META_PATH: (
            "name: Ispant_LongSword_10K_PBR",
            "guid: cfa1e3eb1dcb4c3b978b047ce54c7461",
        ),
    }
    for path, fragments in required_fragments.items():
        with open(path, "r", encoding="utf-8") as stream:
            content = stream.read()
        missing = [fragment for fragment in fragments if fragment not in content]
        if missing:
            raise RuntimeError(
                f"Unity approved-sword material binding changed: {path}: {missing}"
            )


def material_image_names(obj):
    names = set()
    for material in obj.data.materials:
        if material is None or not material.use_nodes:
            continue
        for node in material.node_tree.nodes:
            if node.type == "TEX_IMAGE" and node.image is not None:
                names.add(os.path.basename(node.image.filepath))
    return names


def validate_body_object(body, armature):
    if len(body.data.vertices) != EXPECTED_BODY_VERTICES:
        raise RuntimeError(f"Body vertex count changed: {len(body.data.vertices)}")
    if len(body.data.polygons) != EXPECTED_BODY_FACES:
        raise RuntimeError(f"Body face count changed: {len(body.data.polygons)}")
    if len(body.data.uv_layers) != EXPECTED_BODY_UV_LAYERS:
        raise RuntimeError("Body UV-layer count changed")
    if len(body.data.materials) != EXPECTED_BODY_MATERIALS:
        raise RuntimeError("Body material count changed")
    if len(body.vertex_groups) != EXPECTED_BODY_VERTEX_GROUPS:
        raise RuntimeError("Body vertex-group count changed")
    if len(armature.data.bones) != EXPECTED_ARMATURE_BONES:
        raise RuntimeError("Armature bone count changed")
    if body.parent != armature:
        raise RuntimeError("Body parent is not the Armature")
    if not any(modifier.type == "ARMATURE" for modifier in body.modifiers):
        raise RuntimeError("Body Armature modifier is missing")


def validate_approved_sword_object(sword, armature):
    if len(sword.data.vertices) != EXPECTED_APPROVED_VERTICES:
        raise RuntimeError("Placed sword vertex count changed")
    if len(sword.data.polygons) != EXPECTED_APPROVED_FACES:
        raise RuntimeError("Placed sword face count changed")
    if len(sword.data.uv_layers) != 1 or len(sword.data.materials) != 1:
        raise RuntimeError("Placed sword UV/material structure changed")
    if len(sword.vertex_groups) != 0:
        raise RuntimeError("Placed sword unexpectedly has vertex groups")
    if any(modifier.type == "ARMATURE" for modifier in sword.modifiers):
        raise RuntimeError("Placed sword unexpectedly has an Armature modifier")
    if sword.parent != armature:
        raise RuntimeError("Placed sword is not parented to the Armature root")
    if set(APPROVED_TEXTURES) - material_image_names(sword):
        raise RuntimeError("Placed sword material is missing approved texture images")


def import_current_body_signature():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=UNITY_SOURCE_PATH, use_anim=False)
    body = bpy.data.objects.get("char1")
    armature = bpy.data.objects.get("Armature")
    if body is None or armature is None:
        raise RuntimeError("Current Unity body or Armature is missing")
    validate_body_object(body, armature)
    return face_signatures(body), {group.name for group in body.vertex_groups}


def create_placement_scene(metrics, current_body_signatures, current_vertex_groups):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=UNITY_SOURCE_PATH, use_anim=False)
    armature = bpy.data.objects.get("Armature")
    body = bpy.data.objects.get("char1")
    if body is None or armature is None:
        raise RuntimeError("Current Unity body or Armature is missing")
    validate_body_object(body, armature)
    if {group.name for group in body.vertex_groups} != current_vertex_groups:
        raise RuntimeError("Current Unity body vertex groups changed during replacement")
    if face_signatures(body) != current_body_signatures:
        raise RuntimeError("Current Unity body changed during replacement")

    existing_swords = [
        obj for obj in bpy.context.scene.objects
        if obj.type == "MESH" and obj.name.startswith("Ispant_Approved_LongSword_10K")
    ]
    if len(existing_swords) != 1:
        raise RuntimeError(f"Current Unity sword count changed: {len(existing_swords)}")
    existing_sword = existing_swords[0]
    if len(existing_sword.data.vertices) != EXPECTED_APPROVED_VERTICES:
        raise RuntimeError("Current Unity sword vertex count changed")
    if len(existing_sword.data.polygons) != EXPECTED_APPROVED_FACES:
        raise RuntimeError("Current Unity sword face count changed")
    existing_sword_mesh = existing_sword.data
    bpy.data.objects.remove(existing_sword, do_unlink=True)
    if existing_sword_mesh.users == 0:
        bpy.data.meshes.remove(existing_sword_mesh)
    bpy.data.orphans_purge(do_recursive=True)

    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=APPROVED_SWORD_PATH, use_anim=False)
    imported = [
        obj for obj in set(bpy.data.objects) - before
        if obj.type == "MESH"
    ]
    if len(imported) != 1:
        raise RuntimeError("Approved sword import did not create exactly one mesh")
    sword = imported[0]
    sword.name = "Ispant_Approved_LongSword_10K"
    sword.data.name = "Ispant_Approved_LongSword_10K"
    sword.matrix_world = metrics["matrix"] @ sword.matrix_world
    sword_world = sword.matrix_world.copy()
    sword.parent = armature
    sword.matrix_parent_inverse = armature.matrix_world.inverted()
    sword.matrix_world = sword_world
    validate_approved_sword_object(sword, armature)
    return body, armature, sword


def export_placement_fbx(path, body, armature, sword):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in (body, armature, sword):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
    )


def validate_exported_placement(
    path,
    expected_body_signatures,
    expected_sword_signatures,
    expected_body_points,
    expected_sword_points,
    expected_body_topology,
    expected_sword_topology,
):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False)
    imported = list(set(bpy.data.objects) - before)
    armatures = [obj for obj in imported if obj.type == "ARMATURE"]
    meshes = [obj for obj in imported if obj.type == "MESH"]
    if len(armatures) != 1 or len(meshes) != 2:
        raise RuntimeError(
            f"Placement FBX hierarchy changed: armatures={len(armatures)}, meshes={len(meshes)}"
        )
    armature = armatures[0]
    body = next((obj for obj in meshes if obj.name.startswith("char1")), None)
    sword = next((
        obj for obj in meshes
        if obj.name.startswith("Ispant_Approved_LongSword_10K")
    ), None)
    if body is None or sword is None:
        raise RuntimeError("Placement FBX body or approved sword name changed")
    validate_body_object(body, armature)
    validate_approved_sword_object(sword, armature)
    if mesh_topology(body) != expected_body_topology:
        raise RuntimeError("Placement FBX changed body topology or face winding")
    if mesh_topology(sword) != expected_sword_topology:
        raise RuntimeError("Placement FBX changed sword topology or face winding")
    body_position_error = float(np.max(np.abs(world_points(body) - expected_body_points)))
    sword_position_error = float(np.max(np.abs(world_points(sword) - expected_sword_points)))
    print(f"BodyMaxPositionError={body_position_error:.9f}")
    print(f"SwordMaxPositionError={sword_position_error:.9f}")
    if body_position_error > MAX_FBX_POSITION_ERROR:
        raise RuntimeError("Placement FBX body position error exceeds tolerance")
    if sword_position_error > MAX_FBX_POSITION_ERROR:
        raise RuntimeError("Placement FBX sword position error exceeds tolerance")

    imported_body_signatures = face_signatures(body, include_coordinates=False)
    if imported_body_signatures != expected_body_signatures:
        missing = expected_body_signatures - imported_body_signatures
        added = imported_body_signatures - expected_body_signatures
        print(f"BodySignatureMissing={sum(missing.values())}")
        print(f"BodySignatureAdded={sum(added.values())}")
        if missing:
            print("BodySignatureMissingFirst=" + repr(next(iter(missing))))
        if added:
            print("BodySignatureAddedFirst=" + repr(next(iter(added))))
        raise RuntimeError("Placement FBX changed body geometry, UVs, weights, or winding")
    imported_sword_signatures = face_signatures(sword, include_coordinates=False)
    if imported_sword_signatures != expected_sword_signatures:
        missing = expected_sword_signatures - imported_sword_signatures
        added = imported_sword_signatures - expected_sword_signatures
        print(f"SwordSignatureMissing={sum(missing.values())}")
        print(f"SwordSignatureAdded={sum(added.values())}")
        if missing:
            print("SwordSignatureMissingFirst=" + repr(next(iter(missing))))
        if added:
            print("SwordSignatureAddedFirst=" + repr(next(iter(added))))
        raise RuntimeError("Placement FBX changed sword geometry, UVs, or winding")
    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def evaluated_world_copy(source, name):
    dependency_graph = bpy.context.evaluated_depsgraph_get()
    evaluated = source.evaluated_get(dependency_graph)
    mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=dependency_graph)
    mesh.transform(source.matrix_world)
    result = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(result)
    result.matrix_world = Matrix.Identity(4)
    return result


def translate_world_object(obj, offset):
    obj.location += Vector(offset)


def create_placement_review(body, sword):
    for obj in bpy.context.scene.objects:
        obj.hide_render = True

    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=ORIGINAL_SOURCE_PATH, use_anim=False)
    imported = list(set(bpy.data.objects) - before)
    original = next((
        obj for obj in imported
        if obj.type == "MESH" and obj.name.startswith("char1")
    ), None)
    if original is None:
        raise RuntimeError("Original review body is missing")
    for obj in imported:
        obj.hide_render = True

    original_display = evaluated_world_copy(original, "Review_Original_With_OldSword")
    body_display = evaluated_world_copy(body, "Review_Body_With_ApprovedSword")
    sword_display = evaluated_world_copy(sword, "Review_ApprovedSword_10K")
    translate_world_object(original_display, (-1.35, 0.0, 0.0))
    translate_world_object(body_display, (1.35, 0.0, 0.0))
    translate_world_object(sword_display, (1.35, 0.0, 0.0))
    for display in (original_display, body_display, sword_display):
        display.hide_render = False

    floor_material = bpy.data.materials.new("PlacementReviewFloor")
    floor_material.diffuse_color = (0.045, 0.052, 0.065, 1.0)
    bpy.ops.mesh.primitive_plane_add(size=7.0, location=(0.0, 0.15, -0.015))
    floor = bpy.context.object
    floor.data.materials.append(floor_material)
    floor.hide_render = False

    camera_data = bpy.data.cameras.new("PlacementReviewCamera")
    camera = bpy.data.objects.new("PlacementReviewCamera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = (0.0, -7.4, 1.15)
    camera_data.lens = 58
    look_at(camera, (0.0, 0.0, 0.95))
    camera.hide_render = False
    bpy.context.scene.camera = camera

    for name, location, energy, size in (
        ("PlacementKey", (-2.8, -3.5, 5.0), 1300.0, 4.0),
        ("PlacementFill", (3.8, -2.0, 3.0), 900.0, 3.0),
        ("PlacementRim", (0.0, 2.0, 4.0), 1100.0, 3.0),
    ):
        light_data = bpy.data.lights.new(name, "AREA")
        light_data.energy = energy
        light_data.shape = "DISK"
        light_data.size = size
        light = bpy.data.objects.new(name, light_data)
        bpy.context.scene.collection.objects.link(light)
        light.location = location
        look_at(light, (0.0, 0.0, 1.0))
        light.hide_render = False

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1536
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PLACEMENT_REVIEW_PATH
    scene.render.film_transparent = False
    if scene.world is None:
        scene.world = bpy.data.worlds.new("PlacementReviewWorld")
    scene.world.color = (0.022, 0.027, 0.038)
    scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.ops.render.render(write_still=True)


def build_placement(deploy_unity=False):
    validate_texture_sources()
    metrics, _, _ = compute_placement_alignment()
    if abs(metrics["scale"] - EXPECTED_ALIGNMENT_SCALE) > 0.0000001:
        raise RuntimeError("Approved placement scale changed")
    if metrics["normalized_bidirectional_mean"] > MAX_NORMALIZED_ALIGNMENT_ERROR:
        raise RuntimeError("Approved sword no longer matches the original placement surface")

    current_body_signatures, current_vertex_groups = import_current_body_signature()
    body, armature, sword = create_placement_scene(
        metrics,
        current_body_signatures,
        current_vertex_groups,
    )
    expected_body_signatures = face_signatures(body, include_coordinates=False)
    expected_sword_signatures = face_signatures(sword, include_coordinates=False)
    expected_body_points = world_points(body)
    expected_sword_points = world_points(sword)
    expected_body_topology = mesh_topology(body)
    expected_sword_topology = mesh_topology(sword)
    bpy.ops.wm.save_as_mainfile(filepath=PLACEMENT_BLEND_PATH)
    backup_path = PLACEMENT_BLEND_PATH + "1"
    if os.path.exists(backup_path):
        os.remove(backup_path)
    export_placement_fbx(PLACEMENT_FBX_PATH, body, armature, sword)
    validate_exported_placement(
        PLACEMENT_FBX_PATH,
        expected_body_signatures,
        expected_sword_signatures,
        expected_body_points,
        expected_sword_points,
        expected_body_topology,
        expected_sword_topology,
    )
    if os.environ.get("ISPANT_SKIP_PLACEMENT_REVIEW") != "1":
        create_placement_review(body, sword)

    output_hash = file_sha256(PLACEMENT_FBX_PATH)
    print("===ISPANT_APPROVED_LONG_SWORD_PLACEMENT_PASS===")
    print(f"BodyVertices={len(body.data.vertices)}")
    print(f"BodyFaces={len(body.data.polygons)}")
    print(f"SwordVertices={len(sword.data.vertices)}")
    print(f"SwordFaces={len(sword.data.polygons)}")
    print(f"SwordUvLayers={len(sword.data.uv_layers)}")
    print(f"SwordMaterials={len(sword.data.materials)}")
    print(f"UniformScale={metrics['scale']:.12f}")
    print(f"NormalizedBidirectionalMean={metrics['normalized_bidirectional_mean']:.9f}")
    print(f"PlacementFbxSha256={output_hash}")
    print(f"Blend={PLACEMENT_BLEND_PATH}")
    print(f"Fbx={PLACEMENT_FBX_PATH}")
    print(f"Review={PLACEMENT_REVIEW_PATH}")

    if deploy_unity:
        if file_sha256(UNITY_SOURCE_PATH) != EXPECTED_UNITY_REPLACEMENT_SOURCE_SHA256:
            raise RuntimeError("Unity Ispant FBX changed before approved-sword replacement")
        if os.path.exists(UNITY_TEMP_PATH):
            os.remove(UNITY_TEMP_PATH)
        shutil.copy2(PLACEMENT_FBX_PATH, UNITY_TEMP_PATH)
        if file_sha256(UNITY_TEMP_PATH) != output_hash:
            raise RuntimeError("Unity temporary FBX differs from validated placement FBX")
        validate_exported_placement(
            UNITY_TEMP_PATH,
            expected_body_signatures,
            expected_sword_signatures,
            expected_body_points,
            expected_sword_points,
            expected_body_topology,
            expected_sword_topology,
        )
        os.replace(UNITY_TEMP_PATH, UNITY_SOURCE_PATH)
        copied_textures = copy_approved_textures_to_unity()
        validate_unity_material_binding()
        print("===ISPANT_APPROVED_LONG_SWORD_UNITY_DEPLOY_PASS===")
        print("CurrentSwordRemovedAndApprovedSourceReimported=1")
        print(f"UnityOutputSha256={file_sha256(UNITY_SOURCE_PATH)}")
        print(f"UnityOutput={UNITY_SOURCE_PATH}")
        for texture_path, texture_hash in copied_textures:
            print(f"UnityTexture={texture_path}|{texture_hash}")
        print(f"UnityMaterial={UNITY_SWORD_MATERIAL_PATH}")
        print(f"UnityShader={UNITY_SWORD_SHADER_PATH}")


if __name__ == "__main__":
    if os.environ.get("ISPANT_BUILD_PLACEMENT") == "1":
        build_placement(
            deploy_unity=os.environ.get("ISPANT_DEPLOY_APPROVED_SWORD") == "1"
        )
    else:
        analyze_placement()
