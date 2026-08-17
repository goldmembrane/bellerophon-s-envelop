import argparse
import collections
import math
import os

import bmesh
import bpy
from mathutils import Vector


UPPER_LIP_BONE = "Fuga_UpperLip"
LOWER_LIP_BONE = "Fuga_LowerLip"
UPPER_PARENT_BONE = "Bone_003"
LOWER_PARENT_BONE = "Bone_002"
EXPECTED_SOURCE_VERTEX_COUNT = 3158
EXPECTED_RIGGED_VERTEX_COUNT = 3155
EXPECTED_ORIGINAL_BONE_COUNT = 26
EXPECTED_RIGGED_BONE_COUNT = 28
EXPECTED_SOURCE_POLYGON_COUNT = 3057
EXPECTED_RIGGED_POLYGON_COUNT = 3045
EXPECTED_INTER_LIP_FACE_COUNT = 12
EXPECTED_EXPORTED_UPPER_LIP_VERTEX_COUNT = 32
EXPECTED_EXPORTED_LOWER_LIP_VERTEX_COUNT = 11

UPPER_LIP_VERTICES = (
    537, 540, 541, 542, 543, 552, 553, 554, 556, 557, 558, 572, 573, 574, 583,
    1588, 2036, 2038, 2043, 2044, 2046, 2047, 2048, 2052, 2053, 2054, 2056,
    2063, 2064, 2065, 2066, 2067, 2068,
)
LOWER_LIP_VERTICES = (
    539, 545, 549, 575, 587, 2041, 2042, 2051, 2055, 2059, 2060, 2062, 2069, 2091,
)

# A shared Unity model-space hinge at Z=0.660000 gives every identified lip vertex
# a positive opening displacement while remaining behind the visible V-shaped seam.
LIP_HINGE = Vector((0.007721, -0.660000, 0.719717))
LIP_BONE_TAIL = LIP_HINGE + Vector((0.0, -0.100000, 0.0))
POSITION_TOLERANCE = 0.00001

# A small set of measured Unity model-space points converted to Blender glTF space protects vertex order.
EXPECTED_VERTEX_POSITIONS = {
    541: Vector((0.007721, -0.934433, 0.719717)),
    557: Vector((0.095581, -0.829804, 0.740350)),
    2038: Vector((-0.101562, -0.838944, 0.737247)),
    2069: Vector((0.007721, -0.934433, 0.719717)),
}


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(_script_args())


def _script_args():
    import sys

    return sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def clear_scene():
    bpy.ops.object.mode_set(mode="OBJECT") if bpy.context.object and bpy.context.object.mode != "OBJECT" else None
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for data_collection in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.images):
        for block in list(data_collection):
            if block.users == 0:
                data_collection.remove(block)


def import_model(path):
    bpy.ops.import_scene.gltf(filepath=path)
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.vertex_groups]
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one Fuga armature, found {len(armatures)}")
    if len(meshes) != 1:
        raise RuntimeError(f"Expected one skinned Fuga mesh, found {len(meshes)}")
    return armatures[0], meshes[0]


def require_original_contract(armature, mesh):
    if len(armature.data.bones) != EXPECTED_ORIGINAL_BONE_COUNT:
        raise RuntimeError(
            f"Expected {EXPECTED_ORIGINAL_BONE_COUNT} original bones, found {len(armature.data.bones)}"
        )
    if len(mesh.data.vertices) != EXPECTED_SOURCE_VERTEX_COUNT:
        raise RuntimeError(f"Expected {EXPECTED_SOURCE_VERTEX_COUNT} vertices, found {len(mesh.data.vertices)}")
    if len(mesh.data.polygons) != EXPECTED_SOURCE_POLYGON_COUNT:
        raise RuntimeError(
            f"Expected {EXPECTED_SOURCE_POLYGON_COUNT} source polygons, found {len(mesh.data.polygons)}"
        )
    existing_names = {bone.name for bone in armature.data.bones}
    for name in (UPPER_PARENT_BONE, LOWER_PARENT_BONE):
        if name not in existing_names:
            raise RuntimeError(f"Required parent bone is missing: {name}")
    for name in (UPPER_LIP_BONE, LOWER_LIP_BONE):
        if name in existing_names or mesh.vertex_groups.get(name) is not None:
            raise RuntimeError(f"Fuga lip rig already exists: {name}")
    if set(UPPER_LIP_VERTICES).intersection(LOWER_LIP_VERTICES):
        raise RuntimeError("Upper and lower lip vertex sets overlap")
    for index, expected in EXPECTED_VERTEX_POSITIONS.items():
        actual = mesh.data.vertices[index].co
        if (actual - expected).length > POSITION_TOLERANCE:
            raise RuntimeError(
                f"Fuga vertex order changed at {index}: actual={tuple(actual)}, expected={tuple(expected)}"
            )


def add_lip_bones(armature):
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    upper = armature.data.edit_bones.new(UPPER_LIP_BONE)
    upper.head = LIP_HINGE
    upper.tail = LIP_BONE_TAIL
    upper.parent = armature.data.edit_bones[UPPER_PARENT_BONE]
    upper.use_connect = False
    upper.use_deform = True

    lower = armature.data.edit_bones.new(LOWER_LIP_BONE)
    lower.head = LIP_HINGE
    lower.tail = LIP_BONE_TAIL
    lower.parent = armature.data.edit_bones[LOWER_PARENT_BONE]
    lower.use_connect = False
    lower.use_deform = True
    bpy.ops.object.mode_set(mode="OBJECT")


def assign_exclusive_weights(mesh):
    upper_group = mesh.vertex_groups.new(name=UPPER_LIP_BONE)
    lower_group = mesh.vertex_groups.new(name=LOWER_LIP_BONE)
    upper_set = set(UPPER_LIP_VERTICES)
    lower_set = set(LOWER_LIP_VERTICES)
    for vertex in mesh.data.vertices:
        if vertex.index not in upper_set and vertex.index not in lower_set:
            continue
        for group in list(vertex.groups):
            mesh.vertex_groups[group.group].remove([vertex.index])
    upper_group.add(list(UPPER_LIP_VERTICES), 1.0, "REPLACE")
    lower_group.add(list(LOWER_LIP_VERTICES), 1.0, "REPLACE")


def remove_inter_lip_faces(mesh):
    upper_set = set(UPPER_LIP_VERTICES)
    lower_set = set(LOWER_LIP_VERTICES)
    face_indices = [
        polygon.index
        for polygon in mesh.data.polygons
        if upper_set.intersection(polygon.vertices) and lower_set.intersection(polygon.vertices)
    ]
    if len(face_indices) != EXPECTED_INTER_LIP_FACE_COUNT:
        raise RuntimeError(
            f"Expected {EXPECTED_INTER_LIP_FACE_COUNT} upper/lower bridge faces, found {len(face_indices)}"
        )

    editable = bmesh.new()
    editable.from_mesh(mesh.data)
    editable.faces.ensure_lookup_table()
    bmesh.ops.delete(
        editable,
        geom=[editable.faces[index] for index in face_indices],
        context="FACES_ONLY",
    )
    editable.to_mesh(mesh.data)
    editable.free()
    mesh.data.update()

    if len(mesh.data.polygons) != EXPECTED_RIGGED_POLYGON_COUNT:
        raise RuntimeError(
            f"Expected {EXPECTED_RIGGED_POLYGON_COUNT} polygons after separating the lips, "
            f"found {len(mesh.data.polygons)}"
        )
    remaining = [
        polygon.index
        for polygon in mesh.data.polygons
        if upper_set.intersection(polygon.vertices) and lower_set.intersection(polygon.vertices)
    ]
    if remaining:
        raise RuntimeError(f"Upper/lower bridge faces remain after separation: {remaining}")


def capture_non_lip_weights(mesh):
    lip_vertices = set(UPPER_LIP_VERTICES).union(LOWER_LIP_VERTICES)
    result = {}
    for vertex in mesh.data.vertices:
        if vertex.index in lip_vertices:
            continue
        result[vertex.index] = {
            mesh.vertex_groups[item.group].name: item.weight
            for item in vertex.groups
            if item.weight > 0.0000001
        }
    return result


def capture_geometry_contract(mesh):
    return {
        "positions": [vertex.co.copy() for vertex in mesh.data.vertices],
        "polygons": [
            (tuple(polygon.vertices), polygon.material_index)
            for polygon in mesh.data.polygons
        ],
        "uv_layers": [
            (
                layer.name,
                [(item.uv.x, item.uv.y) for item in layer.data],
            )
            for layer in mesh.data.uv_layers
        ],
        "materials": [material.name if material else None for material in mesh.data.materials],
    }


def require_geometry_contract(mesh, expected):
    actual_positions = [vertex.co for vertex in mesh.data.vertices]
    if len(actual_positions) != len(expected["positions"]):
        raise RuntimeError("Rig export changed the Fuga position count")
    for index, expected_position in enumerate(expected["positions"]):
        if (actual_positions[index] - expected_position).length > POSITION_TOLERANCE:
            raise RuntimeError(f"Rig export changed vertex position {index}")

    actual_polygons = [
        (tuple(polygon.vertices), polygon.material_index)
        for polygon in mesh.data.polygons
    ]
    if actual_polygons != expected["polygons"]:
        raise RuntimeError("Rig export changed the Fuga triangle topology or material slots")

    actual_materials = [material.name if material else None for material in mesh.data.materials]
    if actual_materials != expected["materials"]:
        raise RuntimeError(
            f"Rig export changed the Fuga material assignment: actual={actual_materials}, expected={expected['materials']}"
        )

    actual_uv_layers = [
        (
            layer.name,
            [(item.uv.x, item.uv.y) for item in layer.data],
        )
        for layer in mesh.data.uv_layers
    ]
    if len(actual_uv_layers) != len(expected["uv_layers"]):
        raise RuntimeError("Rig export changed the Fuga UV layer count")
    for (actual_name, actual_values), (expected_name, expected_values) in zip(
        actual_uv_layers, expected["uv_layers"]
    ):
        if actual_name != expected_name or len(actual_values) != len(expected_values):
            raise RuntimeError("Rig export changed the Fuga UV layout structure")
        for index, (actual_uv, expected_uv) in enumerate(zip(actual_values, expected_values)):
            if abs(actual_uv[0] - expected_uv[0]) > POSITION_TOLERANCE or abs(actual_uv[1] - expected_uv[1]) > POSITION_TOLERANCE:
                raise RuntimeError(f"Rig export changed UV {actual_name}/{index}")


def capture_surface_contract(mesh):
    def vertex_weights(vertex):
        return tuple(sorted(
            (
                mesh.vertex_groups[item.group].name,
                item.weight,
            )
            for item in vertex.groups
            if item.weight > 0.00001
        ))

    result = []
    for polygon in mesh.data.polygons:
        corners = []
        for loop_index in polygon.loop_indices:
            vertex = mesh.data.vertices[mesh.data.loops[loop_index].vertex_index]
            uvs = tuple(
                (layer.data[loop_index].uv.x, layer.data[loop_index].uv.y)
                for layer in mesh.data.uv_layers
            )
            corners.append((
                tuple(vertex.co),
                uvs,
                vertex_weights(vertex),
            ))
        result.append((polygon.material_index, tuple(corners)))
    return result


def require_surface_contract(mesh, expected):
    actual = capture_surface_contract(mesh)
    if len(actual) != len(expected):
        raise RuntimeError(
            f"Rig export changed retained triangle count: actual={len(actual)}, expected={len(expected)}"
        )

    def vectors_equal(left, right):
        return len(left) == len(right) and all(
            abs(left_value - right_value) <= 0.0001
            for left_value, right_value in zip(left, right)
        )

    def weights_equal(left, right):
        return len(left) == len(right) and all(
            left_name == right_name and abs(left_weight - right_weight) <= 0.0001
            for (left_name, left_weight), (right_name, right_weight) in zip(left, right)
        )

    def corners_equal(left, right):
        left_position, left_uvs, left_weights = left
        right_position, right_uvs, right_weights = right
        return (
            vectors_equal(left_position, right_position)
            and len(left_uvs) == len(right_uvs)
            and all(vectors_equal(left_uv, right_uv) for left_uv, right_uv in zip(left_uvs, right_uvs))
            and weights_equal(left_weights, right_weights)
        )

    for polygon_index, ((expected_material, expected_corners), (actual_material, actual_corners)) in enumerate(
        zip(expected, actual)
    ):
        if expected_material != actual_material:
            raise RuntimeError(f"Rig export changed material slot at polygon {polygon_index}")
        unused = list(actual_corners)
        for expected_corner in expected_corners:
            match_index = next(
                (index for index, candidate in enumerate(unused) if corners_equal(expected_corner, candidate)),
                -1,
            )
            if match_index < 0:
                raise RuntimeError(
                    "Rig export changed a retained triangle position, UV, or skin weight at polygon "
                    f"{polygon_index}"
                )
            unused.pop(match_index)


def require_non_lip_weights(mesh, expected_weights):
    for index, expected in expected_weights.items():
        vertex = mesh.data.vertices[index]
        actual = {
            mesh.vertex_groups[item.group].name: item.weight
            for item in vertex.groups
            if item.weight > 0.0000001
        }
        if set(actual) != set(expected):
            raise RuntimeError(
                f"Non-lip bone influences changed at vertex {index}: actual={sorted(actual)}, expected={sorted(expected)}"
            )
        for name, expected_weight in expected.items():
            if not math.isclose(actual[name], expected_weight, abs_tol=0.00001):
                raise RuntimeError(
                    f"Non-lip bone weight changed at vertex {index}/{name}: "
                    f"actual={actual[name]}, expected={expected_weight}"
                )


def require_rigged_contract(armature, mesh, exported):
    if len(armature.data.bones) != EXPECTED_RIGGED_BONE_COUNT:
        raise RuntimeError(
            f"Expected {EXPECTED_RIGGED_BONE_COUNT} rigged bones, found {len(armature.data.bones)}"
        )
    expected_vertex_count = EXPECTED_RIGGED_VERTEX_COUNT if exported else EXPECTED_SOURCE_VERTEX_COUNT
    if len(mesh.data.vertices) != expected_vertex_count:
        raise RuntimeError(
            f"Rigged vertex count is incorrect: expected={expected_vertex_count}, actual={len(mesh.data.vertices)}"
        )
    if len(mesh.data.polygons) != EXPECTED_RIGGED_POLYGON_COUNT:
        raise RuntimeError(f"Rigged polygon count is incorrect: {len(mesh.data.polygons)}")
    if not exported:
        for index, expected in EXPECTED_VERTEX_POSITIONS.items():
            if (mesh.data.vertices[index].co - expected).length > POSITION_TOLERANCE:
                raise RuntimeError(f"Rigged vertex order changed at {index}")

    upper_bone = armature.data.bones.get(UPPER_LIP_BONE)
    lower_bone = armature.data.bones.get(LOWER_LIP_BONE)
    if upper_bone is None or upper_bone.parent is None or upper_bone.parent.name != UPPER_PARENT_BONE:
        raise RuntimeError("Upper lip bone parent is incorrect")
    if lower_bone is None or lower_bone.parent is None or lower_bone.parent.name != LOWER_PARENT_BONE:
        raise RuntimeError("Lower lip bone parent is incorrect")
    if (upper_bone.head_local - LIP_HINGE).length > POSITION_TOLERANCE:
        raise RuntimeError("Upper lip hinge changed")
    if (lower_bone.head_local - LIP_HINGE).length > POSITION_TOLERANCE:
        raise RuntimeError("Lower lip hinge changed")

    upper_group = mesh.vertex_groups.get(UPPER_LIP_BONE)
    lower_group = mesh.vertex_groups.get(LOWER_LIP_BONE)
    if upper_group is None or lower_group is None:
        raise RuntimeError("Lip vertex groups are missing")
    upper_set = set(UPPER_LIP_VERTICES)
    lower_set = set(LOWER_LIP_VERTICES)
    upper_indices = set()
    lower_indices = set()
    for vertex in mesh.data.vertices:
        weights = {mesh.vertex_groups[item.group].name: item.weight for item in vertex.groups}
        upper_weight = weights.get(UPPER_LIP_BONE, 0.0)
        lower_weight = weights.get(LOWER_LIP_BONE, 0.0)
        if upper_weight > 0.999999 and lower_weight == 0.0:
            upper_indices.add(vertex.index)
        elif lower_weight > 0.999999 and upper_weight == 0.0:
            lower_indices.add(vertex.index)
        elif upper_weight != 0.0 or lower_weight != 0.0:
            raise RuntimeError(f"Lip rig has a mixed or partial weight at vertex {vertex.index}")

        if not exported and vertex.index in upper_set:
            if not math.isclose(upper_weight, 1.0, abs_tol=0.000001) or lower_weight != 0.0:
                raise RuntimeError(f"Upper lip weight is incorrect at vertex {vertex.index}")
        elif not exported and vertex.index in lower_set:
            if not math.isclose(lower_weight, 1.0, abs_tol=0.000001) or upper_weight != 0.0:
                raise RuntimeError(f"Lower lip weight is incorrect at vertex {vertex.index}")
        elif not exported and vertex.index not in upper_set and vertex.index not in lower_set and (
            upper_weight != 0.0 or lower_weight != 0.0
        ):
            raise RuntimeError(f"Lip rig affects a non-lip vertex: {vertex.index}")

    expected_upper_count = EXPECTED_EXPORTED_UPPER_LIP_VERTEX_COUNT if exported else len(UPPER_LIP_VERTICES)
    expected_lower_count = EXPECTED_EXPORTED_LOWER_LIP_VERTEX_COUNT if exported else len(LOWER_LIP_VERTICES)
    if len(upper_indices) != expected_upper_count or len(lower_indices) != expected_lower_count:
        raise RuntimeError(
            "Lip vertex counts are incorrect: "
            f"upper={len(upper_indices)}/{expected_upper_count}, lower={len(lower_indices)}/{expected_lower_count}"
        )
    if any(
        upper_indices.intersection(polygon.vertices) and lower_indices.intersection(polygon.vertices)
        for polygon in mesh.data.polygons
    ):
        raise RuntimeError("A polygon still connects the upper and lower lip bones")


def export_model(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(
        filepath=path,
        export_format="GLB",
        use_selection=True,
        export_animations=True,
        export_all_influences=True,
        export_yup=True,
    )


def main():
    args = parse_args()
    input_path = os.path.abspath(args.input)
    output_path = os.path.abspath(args.output)
    if os.path.normcase(input_path) == os.path.normcase(output_path):
        raise RuntimeError("Input and output must differ so the original can be validated before replacement")

    clear_scene()
    armature, mesh = import_model(input_path)
    require_original_contract(armature, mesh)
    add_lip_bones(armature)
    assign_exclusive_weights(mesh)
    remove_inter_lip_faces(mesh)
    separated_surface = capture_surface_contract(mesh)
    require_rigged_contract(armature, mesh, exported=False)
    export_model(output_path)

    clear_scene()
    exported_armature, exported_mesh = import_model(output_path)
    require_rigged_contract(exported_armature, exported_mesh, exported=True)
    require_surface_contract(exported_mesh, separated_surface)
    print(
        "FugaLipRigGenerated Result=PASS"
        f", Output={output_path}"
        f", Bones={len(exported_armature.data.bones)}"
        f", Vertices={len(exported_mesh.data.vertices)}"
        f", UpperLipVertices={EXPECTED_EXPORTED_UPPER_LIP_VERTEX_COUNT}"
        f", LowerLipVertices={EXPECTED_EXPORTED_LOWER_LIP_VERTEX_COUNT}"
        f", InterLipFacesRemoved={EXPECTED_INTER_LIP_FACE_COUNT}"
        ", NonLipVerticesAffected=0"
        ", RetainedGeometryAndUvsChanged=False"
    )


if __name__ == "__main__":
    main()
