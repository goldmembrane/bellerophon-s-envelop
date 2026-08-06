from __future__ import annotations

import hashlib
import json
import math
import shutil
import sys
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector


SCRIPT_PATH = Path(__file__).resolve()
PROJECT_ROOT = SCRIPT_PATH.parents[6]
SOURCE_BLEND = (
    PROJECT_ROOT /
    "artSample/enemies/ispant_draw_sword/length_0_9m_revision/"
    "Ispant_DrawSword_0_9m_ArtSample.blend"
)
SOURCE_REPORT = (
    PROJECT_ROOT /
    "artSample/enemies/ispant_draw_sword/length_0_9m_revision/"
    "Ispant_DrawSword_0_9m_ArtSample_Report.json"
)
OUTPUT_ROOT = SCRIPT_PATH.parent
MODEL_DIR = OUTPUT_ROOT / "Models"
TEXTURE_DIR = OUTPUT_ROOT / "Textures"
OUTPUT_FBX = MODEL_DIR / "Ispant_ApprovedLongSword.fbx"
OUTPUT_STATIC_FBX = MODEL_DIR / "Ispant_ApprovedLongSword_StaticMount.fbx"
OUTPUT_MOVE_MOUNT_FBX = MODEL_DIR / "Ispant_ApprovedLongSword_MoveMount.fbx"
OUTPUT_DRAW_MOUNT_FBX = MODEL_DIR / "Ispant_ApprovedLongSword_DrawMount.fbx"
OUTPUT_REPORT = OUTPUT_ROOT / "Ispant_ApprovedLongSword_Export.json"
STATIC_SOURCE_FBX = (
    PROJECT_ROOT /
    "Assets/_Project/Art/Enemies/Ispant/ApprovedAppearance/Models/Ispant_Armed_Approved.fbx"
)
STATIC_REFERENCE_FBX = PROJECT_ROOT / "enemies model/Ispant_Static.fbx"
DRAW_MOUNT_SOURCE_FBX = (
    PROJECT_ROOT /
    "artSample/enemies/ispant_draw_sword/length_0_9m_revision/"
    "Ispant_DrawSword_0_9m_ArtSample.fbx"
)
REFERENCE_DRAW_FBX = (
    PROJECT_ROOT / "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_DrawSword.fbx"
)
MOVE_SOURCE_FBX = (
    PROJECT_ROOT / "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Move.fbx"
)

EXPECTED_BLEND_SHA256 = "52E995ED3B121C5363E53FFA8BB832D7BF9FF4795560711AA7D12105C9CABA3D"
EXPECTED_STATIC_SHA256 = "E33AE0B988CD7CA6FE96D42D7D5E057F1CB57800009EBF7413EE0694BC6825FA"
EXPECTED_STATIC_REFERENCE_SHA256 = "14A011FA502815AD37CB4817B0BCD353C92AF6227BABE0118C09CA70A5484506"
EXPECTED_DRAW_MOUNT_SHA256 = "4058A90B57C8ABA7BCAF185B9BF5D1D1C47C2F0E0991A43BE5178E071D543208"
EXPECTED_REFERENCE_DRAW_SHA256 = "B9DEB78C6BECA61C81EE5ECD86C4763E56186B8925EED29720B4B62ED482CE42"
EXPECTED_MOVE_SHA256 = "25E2CEC76F1FB3AF0A406E450649D38399799581B0F2B4644995B108BAFC0FA8"
EXPECTED_OBJECT_NAME = "Ispant_Reference_LongSword"
EXPECTED_VERTEX_COUNT = 2080
EXPECTED_TRIANGLE_COUNT = 4092
EXPECTED_DIMENSIONS = Vector((0.198372, 0.076, 0.9))
EXPECTED_MATERIALS = (
    "Ispant_LongSword_WornSteel",
    "Ispant_LongSword_BrownLeather",
    "Ispant_LongSword_DarkEngraving",
)
EXPECTED_GRIP_CENTER_Z = -0.103
REFERENCE_SWORD_COMPONENTS = (77, 78, 79, 80)
REFERENCE_BLADE_COMPONENT = 78
REFERENCE_HANDLE_COMPONENT = 79


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def assert_close(actual: float, expected: float, label: str, tolerance: float = 1e-5) -> None:
    if not math.isclose(actual, expected, rel_tol=0.0, abs_tol=tolerance):
        raise RuntimeError(f"{label}: expected {expected}, got {actual}")


def validate_source(source: bpy.types.Object) -> dict:
    source_hash = sha256(SOURCE_BLEND)
    if source_hash != EXPECTED_BLEND_SHA256:
        raise RuntimeError(
            f"Approved blend hash mismatch: expected {EXPECTED_BLEND_SHA256}, got {source_hash}"
        )

    source.data.calc_loop_triangles()
    vertex_count = len(source.data.vertices)
    triangle_count = len(source.data.loop_triangles)
    dimensions = source.dimensions.copy()
    materials = tuple(slot.material.name for slot in source.material_slots)

    if vertex_count != EXPECTED_VERTEX_COUNT:
        raise RuntimeError(f"Approved vertex count mismatch: {vertex_count}")
    if triangle_count != EXPECTED_TRIANGLE_COUNT:
        raise RuntimeError(f"Approved triangle count mismatch: {triangle_count}")
    for index, expected in enumerate(EXPECTED_DIMENSIONS):
        assert_close(dimensions[index], expected, f"dimension[{index}]")
    if materials != EXPECTED_MATERIALS:
        raise RuntimeError(f"Approved material slots mismatch: {materials}")

    return {
        "source_blend": str(SOURCE_BLEND),
        "source_blend_sha256": source_hash,
        "source_report": str(SOURCE_REPORT),
        "source_report_sha256": sha256(SOURCE_REPORT),
        "object_name": source.name,
        "mesh_name": source.data.name,
        "vertices": vertex_count,
        "triangles": triangle_count,
        "dimensions_m": [float(value) for value in dimensions],
        "material_slots": list(materials),
        "grip_center_local_m": [0.0, 0.0, EXPECTED_GRIP_CENTER_Z],
    }


def duplicate_at_origin(source: bpy.types.Object) -> bpy.types.Object:
    duplicate = source.copy()
    duplicate.data = source.data.copy()
    duplicate.name = "Ispant_ApprovedLongSword"
    duplicate.data.name = "Ispant_ApprovedLongSword_Mesh"
    bpy.context.collection.objects.link(duplicate)
    duplicate.parent = None
    duplicate.parent_type = "OBJECT"
    duplicate.matrix_world = Matrix.Identity(4)
    duplicate.hide_render = False
    duplicate.hide_set(False)

    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    duplicate.select_set(True)
    bpy.context.view_layer.objects.active = duplicate
    return duplicate


def make_bake_target(image: bpy.types.Image, materials: list[bpy.types.Material]) -> list[bpy.types.Node]:
    targets = []
    for material in materials:
        material.use_nodes = True
        nodes = material.node_tree.nodes
        for node in nodes:
            node.select = False
        target = nodes.new("ShaderNodeTexImage")
        target.name = f"__ApprovedLongSwordBake_{image.name}"
        target.image = image
        target.select = True
        nodes.active = target
        targets.append(target)
    return targets


def linear_to_srgb(value: float) -> float:
    value = max(0.0, min(1.0, value))
    if value <= 0.0031308:
        return value * 12.92
    return 1.055 * pow(value, 1.0 / 2.4) - 0.055


def save_image(image: bpy.types.Image, path: Path, *, non_color: bool) -> None:
    # Blender's image.save() writes the float buffer values directly. The approved
    # base-color bake is scene-linear, while Unity imports BaseColor as sRGB. Encode
    # only that color channel so Unity decodes back to the approved linear values.
    if not non_color:
        pixels = list(image.pixels)
        for index in range(0, len(pixels), 4):
            pixels[index] = linear_to_srgb(pixels[index])
            pixels[index + 1] = linear_to_srgb(pixels[index + 1])
            pixels[index + 2] = linear_to_srgb(pixels[index + 2])
        image.pixels = pixels
        image.update()
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()


def bake_texture_set(sword: bpy.types.Object) -> dict:
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.bake.margin = 16
    scene.render.bake.use_clear = True

    materials = [slot.material for slot in sword.material_slots]
    if any(material is None for material in materials):
        raise RuntimeError("Approved sword contains an empty material slot")

    result = {}
    for channel in ("BaseColor", "Roughness", "Metallic", "Normal"):
        image = bpy.data.images.new(
            f"Ispant_ApprovedLongSword_{channel}",
            width=1024,
            height=1024,
            alpha=True,
            float_buffer=False,
        )
        if channel != "BaseColor":
            image.colorspace_settings.name = "Non-Color"
        targets = make_bake_target(image, materials)
        original_links = []
        temporary_nodes = []

        try:
            if channel == "BaseColor":
                scene.render.bake.use_pass_direct = False
                scene.render.bake.use_pass_indirect = False
                scene.render.bake.use_pass_color = True
                bpy.ops.object.bake(type="DIFFUSE")
            elif channel == "Roughness":
                bpy.ops.object.bake(type="ROUGHNESS")
            elif channel == "Normal":
                scene.render.bake.normal_space = "TANGENT"
                bpy.ops.object.bake(type="NORMAL")
            else:
                socket_name = "Metallic"
                for material in materials:
                    node_tree = material.node_tree
                    output = next(
                        node for node in node_tree.nodes
                        if node.bl_idname == "ShaderNodeOutputMaterial" and node.is_active_output
                    )
                    incoming = list(output.inputs["Surface"].links)
                    original_sockets = [link.from_socket for link in incoming]
                    original_links.append((node_tree, output, original_sockets))
                    for link in incoming:
                        node_tree.links.remove(link)

                    principled = next(
                        node for node in node_tree.nodes
                        if node.bl_idname == "ShaderNodeBsdfPrincipled"
                    )
                    value_socket = principled.inputs[socket_name]
                    if value_socket.is_linked:
                        raise RuntimeError(f"Approved {socket_name} unexpectedly uses a linked input")
                    value = float(value_socket.default_value)
                    emission = node_tree.nodes.new("ShaderNodeEmission")
                    emission.inputs["Color"].default_value = (value, value, value, 1.0)
                    emission.inputs["Strength"].default_value = 1.0
                    node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
                    temporary_nodes.append((node_tree, emission))
                bpy.ops.object.bake(type="EMIT")

            image.update()
            pixels = list(image.pixels)
            rgb_values = pixels[0::4] + pixels[1::4] + pixels[2::4]
            pixel_min = min(rgb_values)
            pixel_max = max(rgb_values)
            if pixel_max <= 0.0:
                raise RuntimeError(f"Approved {channel} bake produced an empty texture")
            output_path = TEXTURE_DIR / f"Ispant_ApprovedLongSword_{channel}.png"
            save_image(image, output_path, non_color=channel != "BaseColor")
            result[channel] = {
                "path": str(output_path),
                "sha256": sha256(output_path),
                "width": image.size[0],
                "height": image.size[1],
                "pixel_min": pixel_min,
                "pixel_max": pixel_max,
            }
        finally:
            for node_tree, emission in temporary_nodes:
                node_tree.nodes.remove(emission)
            for node_tree, output, original_sockets in original_links:
                for from_socket in original_sockets:
                    node_tree.links.new(from_socket, output.inputs["Surface"])
            for material, target in zip(materials, targets):
                material.node_tree.nodes.remove(target)
            bpy.data.images.remove(image)

    return result


def export_fbx(sword: bpy.types.Object) -> None:
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    sword.select_set(True)
    bpy.context.view_layer.objects.active = sword
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_FBX),
        use_selection=True,
        object_types={"MESH"},
        use_mesh_modifiers=False,
        use_triangles=False,
        add_leaf_bones=False,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_NONE",
        bake_space_transform=False,
        path_mode="AUTO",
        embed_textures=False,
    )


def connected_components(mesh: bpy.types.Mesh) -> list[set[int]]:
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


def delete_vertices(obj: bpy.types.Object, vertex_indices: set[int]) -> None:
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


def append_approved_sword() -> bpy.types.Object:
    with bpy.data.libraries.load(str(SOURCE_BLEND), link=False) as (source, target):
        if EXPECTED_OBJECT_NAME not in source.objects:
            raise RuntimeError("Approved sword object is missing from its locked blend")
        target.objects = [EXPECTED_OBJECT_NAME]
    sword = target.objects[0]
    bpy.context.collection.objects.link(sword)
    sword.parent = None
    sword.matrix_parent_inverse = Matrix.Identity(4)
    sword.name = "Ispant_ApprovedLongSword"
    return sword


def static_sword_alignment(
    body: bpy.types.Object,
    components: list[set[int]],
    handle_component: int,
    blade_component: int,
) -> tuple[Matrix, Vector, Vector]:
    handle_vertices = components[handle_component]
    blade_vertices = components[blade_component]
    handle_center = sum(
        (body.matrix_world @ body.data.vertices[index].co for index in handle_vertices),
        Vector(),
    ) / len(handle_vertices)
    blade_points = np.array(
        [
            tuple(body.matrix_world @ body.data.vertices[index].co)
            for index in sorted(blade_vertices)
        ],
        dtype=np.float64,
    )
    blade_center = blade_points.mean(axis=0)
    _, _, axes = np.linalg.svd(blade_points - blade_center)
    length_axis = Vector(axes[0])
    if length_axis.dot(Vector(blade_center) - handle_center) < 0.0:
        length_axis.negate()
    width_axis = Vector(axes[1])
    width_axis -= length_axis * width_axis.dot(length_axis)
    width_axis.normalize()
    normal_axis = length_axis.cross(width_axis).normalized()
    if normal_axis.dot(Vector((0.0, -1.0, 0.0))) < 0.0:
        normal_axis.negate()
        width_axis.negate()
    rotation = Matrix((width_axis, normal_axis, length_axis)).transposed().to_4x4()
    local_grip_center = Vector((0.0, 0.0, EXPECTED_GRIP_CENTER_Z))
    translation = handle_center - rotation.to_3x3() @ local_grip_center
    return Matrix.Translation(translation) @ rotation, handle_center, length_axis


def position_key(value: Vector) -> tuple[float, float, float]:
    return tuple(round(float(axis), 5) for axis in value)


def component_signature(
    mesh: bpy.types.Mesh,
    component: set[int],
    transform: Matrix,
) -> tuple[int, int, list[float]]:
    unique_by_key = {
        position_key(transform @ mesh.vertices[index].co):
            (transform @ mesh.vertices[index].co)
        for index in component
    }
    points = list(unique_by_key.values())
    distances = sorted(
        (points[first] - points[second]).length
        for first in range(len(points))
        for second in range(first + 1, len(points))
    )
    maximum = distances[-1] if distances else 1.0
    normalized = [round(value / maximum, 5) for value in distances]
    component_triangles = sum(
        len(polygon.vertices) - 2
        for polygon in mesh.polygons
        if polygon.vertices[0] in component
    )
    return len(points), component_triangles, normalized


def match_component_by_shape(
    label: str,
    reference_signature: tuple[int, int, list[float]],
    reference_center: Vector,
    static_signatures: list[tuple[int, int, list[float]]],
    static_centers: list[Vector],
    static_points: list[list[Vector]],
    excluded: set[int],
    proximity_component: int | None,
) -> int:
    candidates = []
    comparable = []
    position_ordered = []
    proximity_ordered = []
    reference_vertices, reference_triangles, reference_distances = reference_signature
    for index, signature in enumerate(static_signatures):
        if index in excluded:
            continue
        vertices, triangles, distances = signature
        if vertices != reference_vertices or triangles != reference_triangles:
            continue
        if len(distances) != len(reference_distances):
            continue
        maximum_error = max(
            (abs(first - second) for first, second in zip(reference_distances, distances)),
            default=0.0,
        )
        comparable.append((index, maximum_error))
        if maximum_error <= 0.0001:
            candidates.append((index, maximum_error))
    if len(candidates) != 1:
        if len(candidates) == 0 and len(comparable) == 1:
            return comparable[0][0]
        if len(candidates) == 0 and comparable:
            ordered = sorted(comparable, key=lambda item: item[1])
            separated = len(ordered) == 1 or ordered[1][1] - ordered[0][1] >= 0.05
            if ordered[0][1] <= 0.01 and separated:
                return ordered[0][0]
            position_ordered = sorted(
                (
                    (index, (static_centers[index] - reference_center).length)
                    for index, _ in comparable
                ),
                key=lambda item: item[1],
            )
            position_separated = (
                len(position_ordered) == 1 or
                position_ordered[1][1] - position_ordered[0][1] >= 0.02
            )
            if position_ordered[0][1] <= 0.02 and position_separated:
                return position_ordered[0][0]
            if proximity_component is not None:
                anchor_points = static_points[proximity_component]
                proximity_ordered = sorted(
                    (
                        (
                            index,
                            min(
                                (point - anchor).length
                                for point in static_points[index]
                                for anchor in anchor_points
                            ),
                        )
                        for index, _ in comparable
                    ),
                    key=lambda item: item[1],
                )
                proximity_separated = (
                    len(proximity_ordered) == 1 or
                    proximity_ordered[1][1] - proximity_ordered[0][1] >= 0.02
                )
                if proximity_ordered[0][1] <= 0.12 and proximity_separated:
                    return proximity_ordered[0][0]
        same_triangle_counts = [
            (index, signature[0], signature[1])
            for index, signature in enumerate(static_signatures)
            if signature[1] == reference_triangles
        ]
        raise RuntimeError(
            f"The {label} shape signature did not produce one exact static match: {candidates}; "
            f"referenceVertices={reference_vertices}, referenceTriangles={reference_triangles}, "
            f"staticSameTriangles={same_triangle_counts}, comparable={comparable}, "
            f"positionComparable={position_ordered}, proximityComparable={proximity_ordered}"
        )
    return candidates[0][0]


def identify_static_sword_components(
    static_body: bpy.types.Object,
    static_components: list[set[int]],
) -> tuple[set[int], int, int, list[bpy.types.Object]]:
    if sha256(STATIC_REFERENCE_FBX) != EXPECTED_STATIC_REFERENCE_SHA256:
        raise RuntimeError("The locked static geometry reference FBX hash differs")
    existing = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(STATIC_REFERENCE_FBX))
    imported = [obj for obj in bpy.context.scene.objects if obj not in existing]
    reference_candidates = []
    for obj in imported:
        if obj.type != "MESH":
            continue
        obj.data.calc_loop_triangles()
        if len(obj.data.loop_triangles) == 3596:
            reference_candidates.append(obj)
    if len(reference_candidates) != 1:
        raise RuntimeError(
            f"The locked static geometry reference body is not unique: {[obj.name for obj in reference_candidates]}"
        )
    reference_body = reference_candidates[0]
    reference_components = connected_components(reference_body.data)
    if len(reference_components) != 81:
        raise RuntimeError(
            f"Expected 81 locked static reference components, got {len(reference_components)}"
        )
    reference_vertex_component = {
        vertex: component_index
        for component_index, vertices in enumerate(reference_components)
        for vertex in vertices
    }
    reference_sword_triangles = sum(
        len(polygon.vertices) - 2
        for polygon in reference_body.data.polygons
        if reference_vertex_component[polygon.vertices[0]] in REFERENCE_SWORD_COMPONENTS
    )
    if reference_sword_triangles != 78:
        raise RuntimeError(
            f"The locked static reference sword must contain 78 triangles, got {reference_sword_triangles}"
        )
    static_signatures = [
        component_signature(static_body.data, component, static_body.matrix_world)
        for component in static_components
    ]
    static_centers = [
        sum(
            (static_body.matrix_world @ static_body.data.vertices[index].co for index in component),
            Vector(),
        ) / len(component)
        for component in static_components
    ]
    static_points = [
        [
            static_body.matrix_world @ static_body.data.vertices[index].co
            for index in component
        ]
        for component in static_components
    ]
    references = []
    for component_index in REFERENCE_SWORD_COMPONENTS:
        label = (
            "handle" if component_index == REFERENCE_HANDLE_COMPONENT
            else "blade" if component_index == REFERENCE_BLADE_COMPONENT
            else f"sheath_{component_index}"
        )
        reference_component = reference_components[component_index]
        reference_center = sum(
            (
                reference_body.matrix_world @ reference_body.data.vertices[index].co
                for index in reference_component
            ),
            Vector(),
        ) / len(reference_component)
        references.append((
            label,
            component_signature(
                reference_body.data,
                reference_component,
                reference_body.matrix_world,
            ),
            reference_center,
        ))
    matches = {}
    excluded = set()
    for label, signature, reference_center in references:
        proximity_component = None
        if label == "handle" and "blade" in matches:
            proximity_component = matches["blade"]
        elif label.startswith("sheath_") and matches:
            previous_sheath = next(
                (value for key, value in matches.items() if key.startswith("sheath_")),
                None,
            )
            proximity_component = previous_sheath
        match = match_component_by_shape(
            label,
            signature,
            reference_center,
            static_signatures,
            static_centers,
            static_points,
            excluded,
            proximity_component,
        )
        matches[label] = match
        excluded.add(match)
    if len(excluded) != 4:
        raise RuntimeError(f"Expected four shape-matched static sword components, got {matches}")
    return excluded, matches["handle"], matches["blade"], imported


def export_static_mount() -> dict:
    if sha256(STATIC_SOURCE_FBX) != EXPECTED_STATIC_SHA256:
        raise RuntimeError("The current Unity approved static Ispant FBX hash differs")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(STATIC_SOURCE_FBX))
    body_candidates = []
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        obj.data.calc_loop_triangles()
        if len(obj.data.loop_triangles) == 3596:
            body_candidates.append(obj)
    body = body_candidates[0] if len(body_candidates) == 1 else None
    armature = next(
        (obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"),
        None,
    )
    if body is None or armature is None:
        raise RuntimeError(
            "The locked static Ispant structure differs: "
            f"bodyCandidates={[obj.name for obj in body_candidates]}, "
            f"armatures={[obj.name for obj in bpy.context.scene.objects if obj.type == 'ARMATURE']}"
        )
    components = connected_components(body.data)
    if len(components) != 81:
        raise RuntimeError(f"Expected 81 static body components, got {len(components)}")
    sword_components, handle_component, blade_component, reference_objects = (
        identify_static_sword_components(body, components)
    )
    component_by_vertex = {
        vertex: component_index
        for component_index, vertices in enumerate(components)
        for vertex in vertices
    }
    removed_triangles = sum(
        len(polygon.vertices) - 2
        for polygon in body.data.polygons
        if component_by_vertex[polygon.vertices[0]] in sword_components
    )
    if removed_triangles != 78:
        raise RuntimeError(f"Expected 78 legacy sword triangles, got {removed_triangles}")

    transform, handle_center, length_axis = static_sword_alignment(
        body,
        components,
        handle_component,
        blade_component,
    )
    sword = append_approved_sword()
    sword.matrix_world = transform
    sword_world = sword.matrix_world.copy()
    sword.parent = armature
    sword.parent_type = "BONE"
    sword.parent_bone = "Hips"
    sword.matrix_world = sword_world

    old_sword_vertices = set().union(
        *(components[index] for index in sorted(sword_components))
    )
    delete_vertices(body, old_sword_vertices)
    body.data.calc_loop_triangles()
    remaining_triangles = len(body.data.loop_triangles)
    if remaining_triangles != 3518:
        raise RuntimeError(
            f"Expected 3518 no-sword body triangles, got {remaining_triangles}"
        )

    for obj in reference_objects:
        if obj.name in bpy.data.objects:
            bpy.data.objects.remove(obj, do_unlink=True)

    export_objects = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type in {"ARMATURE", "MESH"}
    ]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in export_objects:
        obj.hide_set(False)
        obj.hide_render = False
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_STATIC_FBX),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        global_scale=0.01,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        use_armature_deform_only=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )
    return {
        "source_fbx": str(STATIC_SOURCE_FBX),
        "source_fbx_sha256": sha256(STATIC_SOURCE_FBX),
        "source_components": len(components),
        "removed_component_indices": sorted(sword_components),
        "handle_component_index": handle_component,
        "blade_component_index": blade_component,
        "removed_triangles": removed_triangles,
        "body_triangles_after_removal": remaining_triangles,
        "handle_center_world_m": [float(value) for value in handle_center],
        "blade_length_axis_world": [float(value) for value in length_axis],
        "sword_parent_bone": "Hips",
        "output_fbx": str(OUTPUT_STATIC_FBX),
        "output_fbx_sha256": sha256(OUTPUT_STATIC_FBX),
    }


def copy_draw_mount() -> dict:
    source_hash = sha256(DRAW_MOUNT_SOURCE_FBX)
    if source_hash != EXPECTED_DRAW_MOUNT_SHA256:
        raise RuntimeError("The approved draw-sword sample FBX hash differs")
    shutil.copy2(DRAW_MOUNT_SOURCE_FBX, OUTPUT_DRAW_MOUNT_FBX)
    output_hash = sha256(OUTPUT_DRAW_MOUNT_FBX)
    if output_hash != source_hash:
        raise RuntimeError("The copied approved draw-sword mount FBX differs")
    return {
        "source_fbx": str(DRAW_MOUNT_SOURCE_FBX),
        "source_fbx_sha256": source_hash,
        "output_fbx": str(OUTPUT_DRAW_MOUNT_FBX),
        "output_fbx_sha256": output_hash,
        "byte_exact_copy": True,
    }


def export_move_mount() -> dict:
    if sha256(MOVE_SOURCE_FBX) != EXPECTED_MOVE_SHA256:
        raise RuntimeError("The current Unity move FBX hash differs")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(MOVE_SOURCE_FBX))
    armature = bpy.data.objects.get("Armature")
    fixed_sword = bpy.data.objects.get("Ispant_Fixed_Sword")
    if armature is None or fixed_sword is None:
        raise RuntimeError("The current Ispant move structure differs")
    armature.data.pose_position = "REST"
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    move_components = connected_components(fixed_sword.data)
    if len(move_components) != 4:
        raise RuntimeError(f"Expected four move sword components, got {len(move_components)}")

    existing = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(STATIC_REFERENCE_FBX))
    reference_objects = [obj for obj in bpy.context.scene.objects if obj not in existing]
    reference_candidates = []
    for obj in reference_objects:
        if obj.type != "MESH":
            continue
        obj.data.calc_loop_triangles()
        if len(obj.data.loop_triangles) == 3596:
            reference_candidates.append(obj)
    if len(reference_candidates) != 1:
        raise RuntimeError("The move mount static reference body is not unique")
    reference_body = reference_candidates[0]
    reference_components = connected_components(reference_body.data)
    move_signatures = [
        component_signature(fixed_sword.data, component, fixed_sword.matrix_world)
        for component in move_components
    ]
    move_points = [
        [fixed_sword.matrix_world @ fixed_sword.data.vertices[index].co for index in component]
        for component in move_components
    ]
    move_centers = [sum(points, Vector()) / len(points) for points in move_points]
    matches = {}
    excluded = set()
    for component_index in REFERENCE_SWORD_COMPONENTS:
        label = (
            "handle" if component_index == REFERENCE_HANDLE_COMPONENT
            else "blade" if component_index == REFERENCE_BLADE_COMPONENT
            else f"sheath_{component_index}"
        )
        reference_component = reference_components[component_index]
        reference_center = sum(
            (
                reference_body.matrix_world @ reference_body.data.vertices[index].co
                for index in reference_component
            ),
            Vector(),
        ) / len(reference_component)
        proximity_component = matches.get("blade") if label == "handle" else None
        if label.startswith("sheath_"):
            proximity_component = next(
                (value for key, value in matches.items() if key.startswith("sheath_")),
                None,
            )
        match = match_component_by_shape(
            label,
            component_signature(
                reference_body.data,
                reference_component,
                reference_body.matrix_world,
            ),
            reference_center,
            move_signatures,
            move_centers,
            move_points,
            excluded,
            proximity_component,
        )
        matches[label] = match
        excluded.add(match)
    if len(excluded) != 4:
        raise RuntimeError(f"The move sword component map differs: {matches}")

    transform, handle_center, length_axis = static_sword_alignment(
        fixed_sword,
        move_components,
        matches["handle"],
        matches["blade"],
    )
    sword = append_approved_sword()
    sword.matrix_world = transform
    sword_world = sword.matrix_world.copy()
    sword.parent = armature
    sword.parent_type = "BONE"
    sword.parent_bone = "mixamorig:Hips"
    sword.matrix_world = sword_world

    for obj in reference_objects:
        if obj.name in bpy.data.objects:
            bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.objects.remove(fixed_sword, do_unlink=True)
    armature.data.pose_position = "POSE"
    bpy.context.scene.frame_set(1)
    export_objects = [
        obj for obj in bpy.context.scene.objects if obj.type in {"ARMATURE", "MESH"}
    ]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in export_objects:
        obj.hide_set(False)
        obj.hide_render = False
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_MOVE_MOUNT_FBX),
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
        embed_textures=False,
    )
    return {
        "source_fbx": str(MOVE_SOURCE_FBX),
        "source_fbx_sha256": sha256(MOVE_SOURCE_FBX),
        "component_map": matches,
        "handle_center_world_m": [float(value) for value in handle_center],
        "blade_length_axis_world": [float(value) for value in length_axis],
        "sword_parent_bone": "mixamorig:Hips",
        "output_fbx": str(OUTPUT_MOVE_MOUNT_FBX),
        "output_fbx_sha256": sha256(OUTPUT_MOVE_MOUNT_FBX),
    }


def main() -> None:
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    source = bpy.data.objects.get(EXPECTED_OBJECT_NAME)
    if source is None or source.type != "MESH":
        raise RuntimeError(f"Missing approved source object: {EXPECTED_OBJECT_NAME}")

    report = validate_source(source)
    sword = duplicate_at_origin(source)
    report["textures"] = bake_texture_set(sword)
    export_fbx(sword)
    report["output_fbx"] = str(OUTPUT_FBX)
    report["output_fbx_sha256"] = sha256(OUTPUT_FBX)
    report["static_mount"] = export_static_mount()
    report["move_mount"] = export_move_mount()
    report["draw_mount"] = copy_draw_mount()
    report["result"] = "PASS"
    OUTPUT_REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"APPROVED_LONGSWORD_EXPORT_FAILED: {exc}", file=sys.stderr)
        raise
