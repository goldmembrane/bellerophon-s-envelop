import bpy
import bmesh
from collections import Counter
import hashlib
import json
import math
from pathlib import Path

from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx"
INSPECTION_JSON = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
RENDER_DIR = SAMPLE_ROOT / "renders"
BLENDER_DIR = SAMPLE_ROOT / "blender"
EXPORT_DIR = SAMPLE_ROOT / "exports"

MATERIAL_SPECS = {
    "armor_bluegray": {
        "display": "Pahur_Armor_RedBrown_RigidPlate",
        "metallic": 0.64,
        "roughness": 0.52,
    },
    "light_steel": {
        "display": "Pahur_Light_Steel_Panels",
        "metallic": 0.72,
        "roughness": 0.50,
    },
    "leg_steel": {
        "display": "Pahur_Dark_RedBrown_Leg_Steel",
        "metallic": 0.66,
        "roughness": 0.54,
    },
    "dark_mechanics": {
        "display": "Pahur_Dark_Mechanics",
        "metallic": 0.60,
        "roughness": 0.60,
    },
    "torso_rigid_shell": {
        "display": "Pahur_Torso_Outer_RedBrown_Armor",
        "metallic": 0.72,
        "roughness": 0.54,
    },
    "torso_center_plate": {
        "display": "Pahur_Torso_Center_LightSteel_Plate",
        "metallic": 0.82,
        "roughness": 0.47,
    },
    "torso_inner_mechanics": {
        "display": "Pahur_Torso_Inner_Dark_Mechanics",
        "metallic": 0.67,
        "roughness": 0.62,
    },
    "torso_pelvis_plate": {
        "display": "Pahur_Torso_Pelvis_RedBrown_Plate",
        "metallic": 0.77,
        "roughness": 0.52,
    },
    "shoulder_machine_blue": {
        "display": "Pahur_Shoulder_Mechanical_RedBrown",
        "metallic": 0.64,
        "roughness": 0.52,
    },
    "left_arm_machine": {
        "display": "Pahur_LeftArm_Segmented_Mechanical",
        "metallic": 0.64,
        "roughness": 0.52,
    },
    "left_hand_machine": {
        "display": "Pahur_LeftHand_Articulated_Mechanical",
        "metallic": 0.70,
        "roughness": 0.56,
    },
    "hood_navy_cloth": {
        "display": "Pahur_Hood_Dark_RedBrown_Cloth",
        "metallic": 0.02,
        "roughness": 0.82,
    },
    "face_metal": {
        "display": "Pahur_Faceplate_Dark_Metal",
        "metallic": 0.86,
        "roughness": 0.30,
    },
    "weapon_gunmetal": {
        "display": "Pahur_Weapon_Gunmetal",
        "metallic": 0.82,
        "roughness": 0.43,
    },
    "heat_bronze": {
        "display": "Pahur_Heat_Bronze",
        "metallic": 0.76,
        "roughness": 0.40,
    },
    "fuel_tank_steel": {
        "display": "Pahur_Fuel_Tank_Worn_Steel",
        "metallic": 0.78,
        "roughness": 0.55,
    },
    "hose_rubber": {
        "display": "Pahur_Hose_Rubber",
        "metallic": 0.06,
        "roughness": 0.78,
    },
    "optic_blue": {
        "display": "Pahur_Optic_WarmRed_Emission",
        "metallic": 0.24,
        "roughness": 0.22,
        "emission": 3.6,
    },
    "flame_orange": {
        "display": "Pahur_Flame_Orange_Emission",
        "metallic": 0.30,
        "roughness": 0.28,
        "emission": 2.8,
    },
    "orange_trim": {
        "display": "Pahur_Orange_Armor_Trim",
        "metallic": 0.58,
        "roughness": 0.42,
    },
}
MATERIAL_ORDER = list(MATERIAL_SPECS)

# These sets describe only the connected surfaces already present in the
# replacement FBX.
WEAPON_COMPONENTS = {
    4, 8, 15, 26, 35, 39, 46, 47, 50, 57, 65, 72, 91, 99, 103, 104
}
BACKPACK_TANK_COMPONENTS = {7, 11, 22}
BACKPACK_HOSE_COMPONENTS = {24, 28, 38, 71, 105}
BACKPACK_METAL_COMPONENTS = {5, 9, 36, 64}
BACKPACK_BRONZE_COMPONENTS = {42, 66, 79}
HOOD_TIE_COMPONENTS = {44}
# Explicit source-polygon mask established from the replacement FBX head
# topology views. This replaces the rejected height-threshold guess and keeps
# the existing wrapped-cloth surface separate from the metal faceplate.
HEADWRAP_SURFACE_POLYGONS = {
    86, 87, 88, 89, 90, 91, 92, 343, 344, 345, 590, 591,
    592, 593, 594, 595, 596, 825, 913, 914, 915, 916, 917, 918,
    919, 920, 952, 953, 954, 955, 956, 957, 1087, 1088, 1089, 1351,
    1352, 1353, 1354, 1629, 1630, 1631, 1746, 1747, 1748, 1749, 1750, 1792,
    1793, 1796, 1797, 1854, 1855, 1856, 1857, 1858, 1859, 1972, 1973, 1974,
    1975, 1976, 2275, 2276, 2277, 2278, 2279, 2286, 2287, 2288, 2342, 2343,
    2344, 2345, 2346, 2347, 2499, 2501, 2544, 2545, 2546, 2547, 2581, 2582,
    2583, 2612, 2613, 2614, 2615, 2616, 2639, 2706, 2707, 2848, 2849, 2850,
    2851, 2867, 2929, 2930, 2931, 2932, 3015, 3016, 3090, 3091, 3092, 3172,
    3173, 3312, 3354, 3355, 3448, 3449, 3541, 3586, 3611, 3624, 3625, 3671,
    3715, 3725, 3764, 3926, 3964, 3967,
}
# The projection is normalized only across the existing head surface, rather
# than across the full character bounding box.
HEAD_PROJECTION_X_MIN = 7.0
HEAD_PROJECTION_X_MAX = 32.0
HEAD_PROJECTION_Y_MIN = 150.0
HEAD_PROJECTION_Y_MAX = 181.0
# Each eye uses its own frame-1 tangent frame fitted to the evaluated
# upper-face polygons recorded in EYE_SURFACE_ANALYSIS.json. The two local
# frames keep the optics on their respective angled faceplates instead of
# projecting one rectangular image across the whole face.
EYE_SURFACE_PROJECTIONS = (
    {
        "name": "LeftEyeSurface",
        "origin": (11.602, 162.825, 14.043),
        "u_axis": (0.806456, 0.0, 0.591294),
        "v_axis": (-0.171074, 0.957232, 0.233325),
        "width": 5.0,
        # Match the reference eye's taller silhouette without changing its
        # horizontal size, surface anchor, or angry rotation.
        "height": 4.5,
        # Both U axes point from the outer corner toward the face center.
        # Negative rotation lowers the inner corner inside the fitted plane.
        "rotation_degrees": -16.0,
    },
    {
        "name": "RightEyeSurface",
        "origin": (19.267, 162.884, 14.631),
        "u_axis": (-0.952586, 0.0, 0.304270),
        "v_axis": (0.054923, 0.983573, 0.171950),
        "width": 5.0,
        # Match the reference eye's taller silhouette without changing its
        # horizontal size, surface anchor, or angry rotation.
        "height": 4.5,
        "rotation_degrees": -14.0,
    },
)
# User-approved deletion scope: the misplaced upper-center plate is an
# independent connected island. It is deleted only from the review sample;
# the source FBX remains untouched.
DELETED_COMPONENTS = {97}

# Each remaining connected torso surface receives one continuous material.
# The mapping follows actual mesh separation and never subdivides triangles
# with projected artwork or creates new geometry.
TORSO_COMPONENT_MATERIALS = {
    1: "torso_rigid_shell",
    27: "torso_inner_mechanics",
    61: "torso_rigid_shell",
    63: "torso_pelvis_plate",
    84: "torso_pelvis_plate",
    96: "torso_inner_mechanics",
}


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def stable_hash(value):
    return hashlib.sha256(
        json.dumps(value, sort_keys=True, separators=(",", ":")).encode("utf-8")
    ).hexdigest().upper()


def mesh_signature(obj):
    mesh = obj.data
    return {
        "vertices": len(mesh.vertices),
        "edges": len(mesh.edges),
        "polygons": len(mesh.polygons),
        "loops": len(mesh.loops),
        "coordinates_hash": stable_hash(
            [[float(value) for value in vertex.co] for vertex in mesh.vertices]
        ),
        "topology_hash": stable_hash(
            [list(polygon.vertices) for polygon in mesh.polygons]
        ),
        "uv_hash": stable_hash(
            {
                layer.name: [
                    [float(item.uv.x), float(item.uv.y)] for item in layer.data
                ]
                for layer in mesh.uv_layers
            }
        ),
        "weights_hash": stable_hash(
            [
                [
                    [membership.group, float(membership.weight)]
                    for membership in vertex.groups
                ]
                for vertex in mesh.vertices
            ]
        ),
        "vertex_groups": [group.name for group in obj.vertex_groups],
    }


def armature_signature(obj):
    return {
        "bones": len(obj.data.bones),
        "bone_hash": stable_hash(
            [
                {
                    "name": bone.name,
                    "parent": bone.parent.name if bone.parent else None,
                    "head": [float(value) for value in bone.head_local],
                    "tail": [float(value) for value in bone.tail_local],
                }
                for bone in obj.data.bones
            ]
        ),
    }


def canonical_cycle(values):
    values = tuple(values)
    candidates = []
    for sequence in (values, tuple(reversed(values))):
        candidates.extend(
            sequence[offset:] + sequence[:offset]
            for offset in range(len(sequence))
        )
    return min(candidates)


def remaining_content_signature(
    obj, excluded_vertices=frozenset(), excluded_polygons=frozenset()
):
    mesh = obj.data
    vertex_records = []
    for vertex in mesh.vertices:
        if vertex.index in excluded_vertices:
            continue
        vertex_records.append(
            {
                "coordinate": [round(float(value), 9) for value in vertex.co],
                "weights": sorted(
                    [
                        [
                            obj.vertex_groups[membership.group].name,
                            round(float(membership.weight), 9),
                        ]
                        for membership in vertex.groups
                    ]
                ),
            }
        )

    edge_records = []
    for edge in mesh.edges:
        if any(index in excluded_vertices for index in edge.vertices):
            continue
        edge_records.append(
            sorted(
                [
                    [round(float(value), 9) for value in mesh.vertices[index].co]
                    for index in edge.vertices
                ]
            )
        )

    polygon_records = []
    uv_layers = list(mesh.uv_layers)
    for polygon in mesh.polygons:
        if polygon.index in excluded_polygons:
            continue
        corners = []
        for loop_index in polygon.loop_indices:
            vertex_index = mesh.loops[loop_index].vertex_index
            corner = [
                [round(float(value), 9) for value in mesh.vertices[vertex_index].co]
            ]
            corner.extend(
                [
                    round(float(layer.data[loop_index].uv.x), 9),
                    round(float(layer.data[loop_index].uv.y), 9),
                ]
                for layer in uv_layers
            )
            corners.append(tuple(tuple(value) if isinstance(value, list) else value for value in corner))
        polygon_records.append(canonical_cycle(corners))

    return {
        "vertices_hash": stable_hash(
            sorted(vertex_records, key=lambda item: json.dumps(item, sort_keys=True))
        ),
        "edges_hash": stable_hash(sorted(edge_records)),
        "polygons_uv_hash": stable_hash(sorted(polygon_records)),
    }


def delete_approved_components(obj, component_by_polygon):
    mesh = obj.data
    deleted_polygon_indices = {
        polygon_index
        for polygon_index, component_id in component_by_polygon.items()
        if component_id in DELETED_COMPONENTS
    }
    deleted_vertex_indices = {
        vertex_index
        for polygon in mesh.polygons
        if polygon.index in deleted_polygon_indices
        for vertex_index in polygon.vertices
    }
    shared_vertices = {
        vertex_index
        for polygon in mesh.polygons
        if polygon.index not in deleted_polygon_indices
        for vertex_index in polygon.vertices
        if vertex_index in deleted_vertex_indices
    }
    if shared_vertices:
        raise RuntimeError(
            "Approved plate is not an independent connected island: "
            f"{sorted(shared_vertices)}"
        )

    before = mesh_signature(obj)
    remaining_before = remaining_content_signature(
        obj, deleted_vertex_indices, deleted_polygon_indices
    )
    deleted_loop_count = sum(
        polygon.loop_total
        for polygon in mesh.polygons
        if polygon.index in deleted_polygon_indices
    )

    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    bmesh.ops.delete(
        bm,
        geom=[bm.verts[index] for index in sorted(deleted_vertex_indices)],
        context="VERTS",
    )
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()

    after = mesh_signature(obj)
    remaining_after = remaining_content_signature(obj)
    removed = {
        "vertices": before["vertices"] - after["vertices"],
        "edges": before["edges"] - after["edges"],
        "polygons": before["polygons"] - after["polygons"],
        "loops": before["loops"] - after["loops"],
    }
    expected_removed = {
        "vertices": len(deleted_vertex_indices),
        "edges": removed["edges"],
        "polygons": len(deleted_polygon_indices),
        "loops": deleted_loop_count,
    }
    deletion = {
        "component_ids": sorted(DELETED_COMPONENTS),
        "source_polygon_indices": sorted(deleted_polygon_indices),
        "source_vertex_indices": sorted(deleted_vertex_indices),
        "removed": removed,
        "expected_removed": expected_removed,
        "remaining_content_before": remaining_before,
        "remaining_content_after": remaining_after,
        "remaining_content_unchanged": remaining_before == remaining_after,
    }
    if (
        removed != expected_removed
        or len(deleted_polygon_indices) != 18
        or len(deleted_vertex_indices) != 11
        or remaining_before != remaining_after
    ):
        raise RuntimeError(
            "Deletion exceeded approved connected component 97: "
            f"{json.dumps(deletion, ensure_ascii=False)}"
        )
    return before, after, deletion


def bounds_of(objects):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects
        if obj.type == "MESH"
        for corner in obj.bound_box
    ]
    low = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    high = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    return low, high


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat(
        "-Z", "Y"
    ).to_euler()


def image_node(nodes, path, non_color=False):
    node = nodes.new("ShaderNodeTexImage")
    node.image = bpy.data.images.load(str(path), check_existing=True)
    node.image.colorspace_settings.name = "Non-Color" if non_color else "sRGB"
    node.interpolation = "Linear"
    node.extension = "REPEAT"
    return node


def head_surface_projection(nodes, links, texture_coordinates):
    mapping = nodes.new("ShaderNodeMapping")
    mapping.vector_type = "POINT"
    scale_x = 1.0 / (HEAD_PROJECTION_X_MAX - HEAD_PROJECTION_X_MIN)
    scale_y = 1.0 / (HEAD_PROJECTION_Y_MAX - HEAD_PROJECTION_Y_MIN)
    mapping.inputs["Location"].default_value = (
        -HEAD_PROJECTION_X_MIN * scale_x,
        -HEAD_PROJECTION_Y_MIN * scale_y,
        0.0,
    )
    mapping.inputs["Scale"].default_value = (scale_x, scale_y, 1.0)
    links.new(texture_coordinates.outputs["Object"], mapping.inputs["Vector"])
    return mapping


def surface_eye_layer(nodes, links, texture_coordinates, projection):
    delta = nodes.new("ShaderNodeVectorMath")
    delta.name = f"{projection['name']}_Delta"
    delta.operation = "SUBTRACT"
    delta.inputs[1].default_value = projection["origin"]
    links.new(texture_coordinates.outputs["Object"], delta.inputs[0])

    rotation = math.radians(projection.get("rotation_degrees", 0.0))
    base_u = Vector(projection["u_axis"])
    base_v = Vector(projection["v_axis"])
    rotated_u = base_u * math.cos(rotation) + base_v * math.sin(rotation)
    rotated_v = -base_u * math.sin(rotation) + base_v * math.cos(rotation)
    projected_axes = []
    for axis_name, axis, extent in (
        ("U", rotated_u, projection["width"]),
        ("V", rotated_v, projection["height"]),
    ):
        dot = nodes.new("ShaderNodeVectorMath")
        dot.name = f"{projection['name']}_{axis_name}_Dot"
        dot.operation = "DOT_PRODUCT"
        dot.inputs[1].default_value = axis
        scale = nodes.new("ShaderNodeMath")
        scale.operation = "MULTIPLY"
        scale.inputs[1].default_value = 1.0 / extent
        offset = nodes.new("ShaderNodeMath")
        offset.operation = "ADD"
        offset.inputs[1].default_value = 0.5
        links.new(delta.outputs["Vector"], dot.inputs[0])
        links.new(dot.outputs["Value"], scale.inputs[0])
        links.new(scale.outputs[0], offset.inputs[0])
        projected_axes.append(offset)

    combined = nodes.new("ShaderNodeCombineXYZ")
    combined.name = f"{projection['name']}_TangentCoordinates"
    links.new(projected_axes[0].outputs[0], combined.inputs["X"])
    links.new(projected_axes[1].outputs[0], combined.inputs["Y"])

    overlay = image_node(
        nodes,
        TEXTURE_DIR / "pahur_face_reference_overlay.png",
    )
    overlay.name = f"{projection['name']}_Overlay"
    overlay.extension = "CLIP"
    emission = image_node(
        nodes,
        TEXTURE_DIR / "pahur_face_reference_emission.png",
    )
    emission.name = f"{projection['name']}_Emission"
    emission.extension = "CLIP"
    links.new(combined.outputs["Vector"], overlay.inputs["Vector"])
    links.new(combined.outputs["Vector"], emission.inputs["Vector"])

    local_distance = nodes.new("ShaderNodeVectorMath")
    local_distance.name = f"{projection['name']}_LocalDistance"
    local_distance.operation = "LENGTH"
    location_limit = nodes.new("ShaderNodeMath")
    location_limit.operation = "LESS_THAN"
    location_limit.inputs[1].default_value = projection["width"] * 0.72
    links.new(delta.outputs["Vector"], local_distance.inputs[0])
    links.new(local_distance.outputs["Value"], location_limit.inputs[0])

    factor = nodes.new("ShaderNodeMath")
    factor.name = f"{projection['name']}_SurfaceMask"
    factor.operation = "MULTIPLY"
    links.new(overlay.outputs["Alpha"], factor.inputs[0])
    links.new(location_limit.outputs[0], factor.inputs[1])
    return overlay, emission, factor


def build_material(material_id, spec):
    material = bpy.data.materials.new(spec["display"])
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    for node in list(nodes):
        nodes.remove(node)

    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    uv = nodes.new("ShaderNodeTexCoord")
    mapping = nodes.new("ShaderNodeMapping")
    texture_scale = (
        0.8
        if material_id == "hood_navy_cloth"
        else 1.0
        if material_id in {"shoulder_machine_blue", "left_arm_machine"}
        else 1.5
        if material_id == "left_hand_machine"
        else 1.0
        if material_id in {"armor_bluegray", "light_steel", "leg_steel"}
        else 1.5
    )
    mapping.inputs["Scale"].default_value = (
        texture_scale,
        texture_scale,
        1.0,
    )
    links.new(uv.outputs["UV"], mapping.inputs["Vector"])

    if material_id in {"optic_blue", "flame_orange"}:
        texture_name = (
            "pahur_optic_blue_emission.png"
            if material_id == "optic_blue"
            else "pahur_flame_orange_emission.png"
        )
        albedo = image_node(nodes, TEXTURE_DIR / texture_name)
        links.new(mapping.outputs["Vector"], albedo.inputs["Vector"])
        links.new(albedo.outputs["Color"], shader.inputs["Base Color"])
        links.new(albedo.outputs["Color"], shader.inputs["Emission Color"])
        shader.inputs["Emission Strength"].default_value = spec["emission"]
        shader.inputs["Metallic"].default_value = spec["metallic"]
        shader.inputs["Roughness"].default_value = spec["roughness"]
    else:
        albedo = image_node(
            nodes, TEXTURE_DIR / f"pahur_{material_id}_albedo.png"
        )
        roughness = image_node(
            nodes,
            TEXTURE_DIR / f"pahur_{material_id}_roughness.png",
            non_color=True,
        )
        metallic = image_node(
            nodes,
            TEXTURE_DIR / f"pahur_{material_id}_metallic.png",
            non_color=True,
        )
        normal_texture = image_node(
            nodes,
            TEXTURE_DIR / f"pahur_{material_id}_normal.png",
            non_color=True,
        )
        normal_map = nodes.new("ShaderNodeNormalMap")
        normal_map.inputs["Strength"].default_value = (
            0.12
            if material_id == "hood_navy_cloth"
            else 0.0
            if material_id == "face_metal"
            else 0.08
            if material_id == "fuel_tank_steel"
            else 0.06
            if material_id
            in {
                "torso_rigid_shell",
                "torso_center_plate",
                "torso_pelvis_plate",
            }
            else 0.10
            if material_id == "torso_inner_mechanics"
            else 0.14
            if material_id
            in {"armor_bluegray", "light_steel", "leg_steel", "weapon_gunmetal"}
            else 0.14
            if material_id in {"shoulder_machine_blue", "left_arm_machine"}
            else 0.16
            if material_id == "left_hand_machine"
            else 0.18
        )
        for texture in (albedo, roughness, metallic, normal_texture):
            links.new(mapping.outputs["Vector"], texture.inputs["Vector"])
        if material_id == "face_metal":
            current_face_color = albedo.outputs["Color"]
            eye_emission_layers = []
            for eye_projection in EYE_SURFACE_PROJECTIONS:
                eye_overlay, eye_emission, eye_factor = surface_eye_layer(
                    nodes,
                    links,
                    uv,
                    eye_projection,
                )
                face_mix = nodes.new("ShaderNodeMixRGB")
                face_mix.name = f"{eye_projection['name']}_BaseMix"
                links.new(eye_factor.outputs[0], face_mix.inputs[0])
                links.new(current_face_color, face_mix.inputs[1])
                links.new(eye_overlay.outputs["Color"], face_mix.inputs[2])
                current_face_color = face_mix.outputs["Color"]

                emission_mask = nodes.new("ShaderNodeMixRGB")
                emission_mask.name = f"{eye_projection['name']}_EmissionMask"
                emission_mask.inputs[1].default_value = (0.0, 0.0, 0.0, 1.0)
                links.new(eye_factor.outputs[0], emission_mask.inputs[0])
                links.new(eye_emission.outputs["Color"], emission_mask.inputs[2])
                eye_emission_layers.append(emission_mask.outputs["Color"])

            emission_add = nodes.new("ShaderNodeVectorMath")
            emission_add.operation = "ADD"
            links.new(eye_emission_layers[0], emission_add.inputs[0])
            links.new(eye_emission_layers[1], emission_add.inputs[1])
            links.new(current_face_color, shader.inputs["Base Color"])
            links.new(emission_add.outputs["Vector"], shader.inputs["Emission Color"])
            shader.inputs["Emission Strength"].default_value = 1.55
        elif material_id == "hood_navy_cloth":
            decal = image_node(
                nodes,
                TEXTURE_DIR / "pahur_head_reference_projection_decal.png",
            )
            decal.extension = "CLIP"
            hood_projection = head_surface_projection(nodes, links, uv)
            links.new(hood_projection.outputs["Vector"], decal.inputs["Vector"])
            separate_generated = nodes.new("ShaderNodeSeparateXYZ")
            front_mask = nodes.new("ShaderNodeMath")
            front_mask.operation = "GREATER_THAN"
            front_mask.inputs[1].default_value = 0.0
            decal_factor = nodes.new("ShaderNodeMath")
            decal_factor.operation = "MULTIPLY"
            base_mix = nodes.new("ShaderNodeMixRGB")
            emission_mix = nodes.new("ShaderNodeMixRGB")
            emission_mix.inputs[1].default_value = (0.0, 0.0, 0.0, 1.0)
            links.new(
                uv.outputs["Object"], separate_generated.inputs["Vector"]
            )
            links.new(separate_generated.outputs["Z"], front_mask.inputs[0])
            links.new(decal.outputs["Alpha"], decal_factor.inputs[0])
            links.new(front_mask.outputs[0], decal_factor.inputs[1])
            links.new(decal_factor.outputs[0], base_mix.inputs[0])
            links.new(albedo.outputs["Color"], base_mix.inputs[1])
            links.new(decal.outputs["Color"], base_mix.inputs[2])
            links.new(base_mix.outputs["Color"], shader.inputs["Base Color"])
            links.new(decal_factor.outputs[0], emission_mix.inputs[0])
            links.new(decal.outputs["Color"], emission_mix.inputs[2])
            links.new(
                emission_mix.outputs["Color"], shader.inputs["Emission Color"]
            )
            shader.inputs["Emission Strength"].default_value = 0.0
        elif material_id in {
            "shoulder_machine_blue",
            "left_arm_machine",
            "left_hand_machine",
        }:
            machine_wave = nodes.new("ShaderNodeTexWave")
            machine_wave.wave_type = "BANDS"
            machine_wave.bands_direction = "Y"
            machine_wave.inputs["Scale"].default_value = (
                5.0
                if material_id == "shoulder_machine_blue"
                else 6.0
                if material_id == "left_arm_machine"
                else 10.0
            )
            machine_wave.inputs["Distortion"].default_value = 0.20
            machine_wave.inputs["Detail"].default_value = 1.0
            machine_band = nodes.new("ShaderNodeMath")
            machine_band.operation = "LESS_THAN"
            machine_band.inputs[1].default_value = (
                0.04
                if material_id == "shoulder_machine_blue"
                else 0.05
                if material_id == "left_arm_machine"
                else 0.08
            )
            # Keep mechanical segmentation subordinate to the torso material.
            machine_band_strength = nodes.new("ShaderNodeMath")
            machine_band_strength.operation = "MULTIPLY"
            machine_band_strength.inputs[1].default_value = (
                0.18
                if material_id == "shoulder_machine_blue"
                else 0.14
                if material_id == "left_arm_machine"
                else 0.10
            )
            machine_mix = nodes.new("ShaderNodeMixRGB")
            machine_mix.inputs[2].default_value = (
                (0.060, 0.025, 0.020, 1.0)
                if material_id in {"shoulder_machine_blue", "left_arm_machine"}
                else (0.025, 0.045, 0.060, 1.0)
            )
            links.new(uv.outputs["Generated"], machine_wave.inputs["Vector"])
            links.new(machine_wave.outputs["Fac"], machine_band.inputs[0])
            links.new(machine_band.outputs[0], machine_band_strength.inputs[0])
            links.new(machine_band_strength.outputs[0], machine_mix.inputs[0])
            links.new(albedo.outputs["Color"], machine_mix.inputs[1])
            links.new(machine_mix.outputs["Color"], shader.inputs["Base Color"])
        else:
            links.new(albedo.outputs["Color"], shader.inputs["Base Color"])
        links.new(roughness.outputs["Color"], shader.inputs["Roughness"])
        links.new(metallic.outputs["Color"], shader.inputs["Metallic"])
        links.new(normal_texture.outputs["Color"], normal_map.inputs["Color"])
        if material_id in {
            "shoulder_machine_blue",
            "left_arm_machine",
            "left_hand_machine",
        }:
            machine_bump = nodes.new("ShaderNodeBump")
            machine_bump.inputs["Strength"].default_value = 0.10
            machine_bump.inputs["Distance"].default_value = 0.020
            links.new(machine_wave.outputs["Fac"], machine_bump.inputs["Height"])
            links.new(normal_map.outputs["Normal"], machine_bump.inputs["Normal"])
            links.new(machine_bump.outputs["Normal"], shader.inputs["Normal"])
        else:
            links.new(normal_map.outputs["Normal"], shader.inputs["Normal"])

    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def dominant_group(obj, polygon):
    weights = Counter()
    for vertex_index in polygon.vertices:
        for membership in obj.data.vertices[vertex_index].groups:
            weights[obj.vertex_groups[membership.group].name] += membership.weight
    return weights.most_common(1)[0][0] if weights else None


def classify_polygon(polygon, component_id, group):
    x, height = polygon.center.x, polygon.center.y

    if component_id in BACKPACK_TANK_COMPONENTS:
        return "fuel_tank_steel"
    if component_id in BACKPACK_HOSE_COMPONENTS:
        return "hose_rubber"
    if component_id in BACKPACK_METAL_COMPONENTS:
        return "fuel_tank_steel"
    if component_id in BACKPACK_BRONZE_COMPONENTS:
        return "heat_bronze"
    if component_id in HOOD_TIE_COMPONENTS:
        return "hood_navy_cloth"
    if component_id in WEAPON_COMPONENTS:
        return "weapon_gunmetal"
    if component_id in TORSO_COMPONENT_MATERIALS:
        return TORSO_COMPONENT_MATERIALS[component_id]

    if group == "Head":
        # Use the reviewed source-topology mask, not a coordinate threshold.
        if polygon.index in HEADWRAP_SURFACE_POLYGONS:
            return "hood_navy_cloth"
        return "face_metal"
    if group == "neck":
        return "dark_mechanics"

    if group in {"RightHand", "RightForeArm", "RightArm"} and x <= -26.0:
        return "weapon_gunmetal"

    if group in {"LeftHand", "RightHand"}:
        return "left_hand_machine" if group == "LeftHand" else "dark_mechanics"

    if group in {"LeftShoulder", "RightShoulder"}:
        return "shoulder_machine_blue"

    if group in {"LeftArm", "RightArm"}:
        if group == "LeftArm":
            return "left_arm_machine"
        return "armor_bluegray"

    if group in {"LeftForeArm", "RightForeArm"}:
        if group == "LeftForeArm":
            return "left_arm_machine"
        return "armor_bluegray"

    if group in {"Spine", "Spine01", "Spine02"}:
        return "dark_mechanics" if height < 122.0 else "armor_bluegray"

    if group == "Hips":
        return "dark_mechanics"

    if group in {"LeftUpLeg", "RightUpLeg"}:
        return "leg_steel"

    if group in {"LeftLeg", "RightLeg"}:
        return "leg_steel"

    if group in {
        "LeftFoot",
        "RightFoot",
        "LeftToeBase",
        "RightToeBase",
    }:
        return "dark_mechanics"

    return "armor_bluegray"


def render_view(scene, camera, center, distance, angle, elevation, filename, zoom=1.0):
    radians = math.radians(angle)
    camera.location = center + Vector(
        (
            distance * math.sin(radians) / zoom,
            -distance * math.cos(radians) / zoom,
            distance * elevation / zoom,
        )
    )
    point_at(camera, center)
    scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def main():
    for directory in (RENDER_DIR, BLENDER_DIR, EXPORT_DIR):
        directory.mkdir(parents=True, exist_ok=True)
    inspection = json.loads(INSPECTION_JSON.read_text(encoding="utf-8"))
    component_data = next(
        item for item in inspection["objects"] if item["type"] == "MESH"
    )["connected_components"]
    component_by_polygon = {
        polygon_index: component["component_id"]
        for component in component_data
        for polygon_index in component["polygon_indices"]
    }

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    scene = bpy.context.scene
    mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH")
    armature_obj = next(obj for obj in scene.objects if obj.type == "ARMATURE")
    source_mesh = mesh_signature(mesh_obj)
    before_armature = armature_signature(armature_obj)

    materials = {
        material_id: build_material(material_id, spec)
        for material_id, spec in MATERIAL_SPECS.items()
    }
    mesh_obj.data.materials.clear()
    for material_id in MATERIAL_ORDER:
        mesh_obj.data.materials.append(materials[material_id])
    material_indices = {
        material_id: index for index, material_id in enumerate(MATERIAL_ORDER)
    }

    assignments = []
    counts = Counter()
    for polygon in mesh_obj.data.polygons:
        group = dominant_group(mesh_obj, polygon)
        component_id = component_by_polygon[polygon.index]
        if component_id in DELETED_COMPONENTS:
            continue
        material_id = classify_polygon(polygon, component_id, group)
        polygon.material_index = material_indices[material_id]
        counts[material_id] += 1
        assignments.append(
            {
                "polygon_index": polygon.index,
                "component_id": component_id,
                "dominant_group": group,
                "material": material_id,
            }
        )

    before_mesh, after_mesh, deletion = delete_approved_components(
        mesh_obj, component_by_polygon
    )
    after_armature = armature_signature(armature_obj)
    preservation = {
        "result": "PASS"
        if (
            source_mesh == before_mesh
            and deletion["remaining_content_unchanged"]
            and before_armature == after_armature
        )
        else "FAIL",
        "source_fbx": "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx",
        "source_fbx_sha256": sha256(SOURCE_FBX),
        "source_mesh": source_mesh,
        "before_deletion_mesh": before_mesh,
        "after_mesh": after_mesh,
        "expected_after_shape": {
            key: after_mesh[key]
            for key in ("vertices", "edges", "polygons", "loops")
        },
        "approved_deletion": deletion,
        "before_armature": before_armature,
        "after_armature": after_armature,
        "remaining_mesh_content_unchanged": deletion[
            "remaining_content_unchanged"
        ],
        "armature_unchanged": before_armature == after_armature,
        "scope": (
            "Only independent connected component 97 is deleted from the "
            "review sample. All remaining geometry, UVs, weights, and the "
            "armature are unchanged."
        ),
    }
    if preservation["result"] != "PASS":
        raise RuntimeError(
            "Changes exceeded the approved deletion of connected component 97."
        )

    (SAMPLE_ROOT / "MATERIAL_ASSIGNMENT.json").write_text(
        json.dumps(
            {
                "assignment_basis": (
                    "Replacement-FBX connected components, explicit reviewed "
                    "head surface polygon indices, and existing vertex weights "
                    "only. Independent "
                    "connected component 97 is intentionally deleted from the "
                    "review sample by user approval. All remaining source "
                    "surfaces retain their geometry, UVs, and weights."
                ),
                "deleted_component_ids": sorted(DELETED_COMPONENTS),
                "material_order": MATERIAL_ORDER,
                "polygon_material_counts": dict(counts),
                "assignments": assignments,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    (SAMPLE_ROOT / "GEOMETRY_PRESERVATION.json").write_text(
        json.dumps(preservation, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    low, high = bounds_of([mesh_obj])
    center = (low + high) * 0.5
    center.z = low.z + (high.z - low.z) * 0.52
    extent = high - low
    radius = max(extent.x, extent.y, extent.z)
    distance = radius * 1.82

    world = bpy.data.worlds.new("Pahur_Review_World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (
        0.86,
        0.89,
        0.90,
        1.0,
    )
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.70

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.name = "Pahur_Review_Camera"
    camera.data.lens = 62
    scene.camera = camera

    for name, location, energy, size, color in (
        (
            "Key",
            center + Vector((-radius * 0.75, -radius, radius * 1.45)),
            330,
            radius * 1.4,
            (1.0, 0.90, 0.78),
        ),
        (
            "Fill",
            center + Vector((radius, -radius * 0.35, radius * 0.55)),
            145,
            radius * 1.1,
            (0.58, 0.76, 1.0),
        ),
        (
            "Rim",
            center + Vector((0.15 * radius, radius, radius * 1.1)),
            250,
            radius,
            (0.55, 0.72, 1.0),
        ),
    ):
        bpy.ops.object.light_add(type="AREA")
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.shape = "DISK"
        light.data.size = size
        light.data.color = color
        light.location = location
        point_at(light, center)

    bpy.ops.mesh.primitive_plane_add(size=7.0, location=(0.0, 0.0, low.z - 0.006))
    floor = bpy.context.object
    floor.name = "Review_Only_Floor"
    floor_mat = bpy.data.materials.new("Review_Only_Floor_Material")
    floor_mat.use_nodes = True
    floor_shader = floor_mat.node_tree.nodes.get("Principled BSDF")
    floor_shader.inputs["Base Color"].default_value = (0.72, 0.76, 0.77, 1.0)
    floor_shader.inputs["Roughness"].default_value = 0.86
    floor.data.materials.append(floor_mat)

    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 1280
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = -0.05
    scene.frame_set(1)

    render_view(
        scene,
        camera,
        center,
        distance,
        0,
        0.06,
        "01_front_pahur_reference_match.png",
    )
    render_view(
        scene,
        camera,
        center,
        distance,
        32,
        0.09,
        "02_three_quarter_pahur_reference_match.png",
    )
    render_view(
        scene,
        camera,
        center,
        distance,
        90,
        0.06,
        "04_side_current_model_material.png",
    )
    render_view(
        scene,
        camera,
        center,
        distance,
        180,
        0.06,
        "05_rear_current_model_material.png",
    )
    head_center = center.copy()
    head_center.z = low.z + extent.z * 0.86
    render_view(
        scene,
        camera,
        head_center,
        distance,
        0,
        0.02,
        "07_head_front_detail.png",
        zoom=2.75,
    )
    render_view(
        scene,
        camera,
        head_center,
        distance,
        30,
        0.02,
        "07_head_three_quarter_detail.png",
        zoom=2.75,
    )
    upper_body_center = center.copy()
    upper_body_center.z = low.z + extent.z * 0.67
    render_view(
        scene,
        camera,
        upper_body_center,
        distance,
        0,
        0.02,
        "10_shoulders_left_arm_detail.png",
        zoom=2.05,
    )
    torso_center = center.copy()
    torso_center.z = low.z + extent.z * 0.59
    render_view(
        scene,
        camera,
        torso_center,
        distance,
        0,
        0.01,
        "12_torso_front_detail.png",
        zoom=2.65,
    )

    floor.hide_render = True
    camera.hide_render = True
    for obj in scene.objects:
        obj.select_set(False)
    armature_obj.hide_render = False
    mesh_obj.hide_render = False
    armature_obj.select_set(True)
    mesh_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj

    blend_path = BLENDER_DIR / "Pahur_Appearance_ReferenceSync.blend"
    fbx_path = EXPORT_DIR / "Pahur_Appearance_ReferenceSync.fbx"
    glb_path = EXPORT_DIR / "Pahur_Appearance_ReferenceSync.glb"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=True,
        apply_unit_scale=False,
        apply_scale_options="FBX_SCALE_NONE",
        use_space_transform=True,
        path_mode="COPY",
        embed_textures=True,
    )
    bpy.ops.export_scene.gltf(
        filepath=str(glb_path),
        export_format="GLB",
        use_selection=True,
        export_skins=True,
        export_animations=True,
        export_materials="EXPORT",
    )

    print(
        json.dumps(
            {
                "preservation": preservation["result"],
                "material_counts": dict(counts),
                "blend": str(blend_path),
                "fbx": str(fbx_path),
                "glb": str(glb_path),
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
