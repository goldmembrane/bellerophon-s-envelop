import bpy
from collections import Counter
import hashlib
import json
import math
from pathlib import Path

from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"
INSPECTION_JSON = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
RENDER_DIR = SAMPLE_ROOT / "renders"
BLENDER_DIR = SAMPLE_ROOT / "blender"
EXPORT_DIR = SAMPLE_ROOT / "exports"

MATERIAL_SPECS = {
    "armor_gunmetal": ("Kursa_Armor_Gunmetal", 0.74, 0.59),
    "armor_bluegray": ("Kursa_Armor_BlueGray", 0.66, 0.57),
    "light_steel": ("Kursa_Light_Steel", 0.78, 0.52),
    "dark_mechanics": ("Kursa_Dark_Mechanics", 0.56, 0.72),
    "torso_mechanical": ("Kursa_Torso_Mechanical_Plates", 0.72, 0.60),
    "hood_cloth": ("Kursa_Hood_Navy_Cloth", 0.02, 0.84),
    "face_metal": ("Kursa_Face_Metal_Blue_Optics", 0.82, 0.38),
    "shield_worn": ("Kursa_Shield_Worn_Gunmetal", 0.52, 0.80),
    "shield_frame": ("Kursa_Shield_Frame_Steel", 0.80, 0.54),
}
MATERIAL_ORDER = list(MATERIAL_SPECS)

# IDs come from the exact imported FBX's connected-surface inspection.
SHIELD_BODY = {34}
SHIELD_FRAME = {4}
BLUEGRAY_PANELS = {5, 15, 16, 19, 30, 39}
LIGHT_STEEL_PANELS = {0, 41}
TORSO_MECHANICAL_COMPONENTS = {12, 23}
# Eye locations are ray-projected from the two dark-pixel centroids inside the
# exact face area supplied by the user. The 48 x 27 crop maps to pixels
# x=534..606/y=154..194 of review render 02; the two centroids map to
# (550, 176) and (586, 174) on the evaluated frame-1 mesh.
# The surface normals remain the depth-mask anchors. A shared visual-plane
# normal removes the severe per-eye shear caused by the recessed right polygon.
EYE_SURFACE_PATCHES = {
    "left": {
        "center": (3.343094, 151.815475, 24.579956),
        "normal": (-0.182243, -0.571750, 0.799931),
        "projection_normal": (0.552875, -0.117583, 0.824926),
        "size": (8.348116, 8.988050),
        "depth": 2.05,
        "polygons": [3801],
    },
    "right": {
        "center": (5.916458, 152.454803, 19.357758),
        "normal": (0.257649, -0.965079, -0.047329),
        "projection_normal": (0.552875, -0.117583, 0.824926),
        "size": (10.076670, 8.897684),
        "depth": 2.05,
        "polygons": [3627],
    },
}
DARK_COMPONENTS = {
    3, 6, 9, 10, 11, 17, 20, 21, 22, 24, 27, 28, 29, 31,
    33, 35, 37, 38, 40, 42, 43, 44, 45, 46,
}


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def stable_hash(value):
    payload = json.dumps(value, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(payload.encode("utf-8")).hexdigest().upper()


def mesh_signature(obj):
    mesh = obj.data
    return {
        "vertices": len(mesh.vertices),
        "edges": len(mesh.edges),
        "polygons": len(mesh.polygons),
        "loops": len(mesh.loops),
        "coordinates_hash": stable_hash(
            [[float(v) for v in vertex.co] for vertex in mesh.vertices]
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
                [[membership.group, float(membership.weight)] for membership in vertex.groups]
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


def action_signature():
    return [
        {
            "name": action.name,
            "frame_range": [float(value) for value in action.frame_range],
            "slots": len(action.slots) if hasattr(action, "slots") else None,
        }
        for action in bpy.data.actions
    ]


def image_node(nodes, path, non_color=False):
    node = nodes.new("ShaderNodeTexImage")
    node.image = bpy.data.images.load(str(path), check_existing=True)
    node.image.colorspace_settings.name = "Non-Color" if non_color else "sRGB"
    node.interpolation = "Linear"
    node.extension = "REPEAT"
    return node


def local_projection(nodes, links, coordinates, x_min, x_max, y_min, y_max):
    mapping = nodes.new("ShaderNodeMapping")
    mapping.vector_type = "POINT"
    scale_x = 1.0 / (x_max - x_min)
    scale_y = 1.0 / (y_max - y_min)
    mapping.inputs["Location"].default_value = (
        -x_min * scale_x,
        -y_min * scale_y,
        0.0,
    )
    mapping.inputs["Scale"].default_value = (scale_x, scale_y, 1.0)
    links.new(coordinates.outputs["Object"], mapping.inputs["Vector"])
    return mapping


def oriented_projection(
    nodes, links, coordinates, center, surface_normal, projection_normal, size, depth
):
    center = Vector(center)
    surface_normal = Vector(surface_normal).normalized()
    projection_normal = Vector(projection_normal).normalized()
    object_up = Vector((0.0, 1.0, 0.0))
    vertical = (
        object_up - projection_normal * object_up.dot(projection_normal)
    ).normalized()
    horizontal = vertical.cross(projection_normal).normalized()

    def projected_axis(axis, center_value, extent):
        dot = nodes.new("ShaderNodeVectorMath")
        dot.operation = "DOT_PRODUCT"
        dot.inputs[1].default_value = tuple(axis)
        links.new(coordinates.outputs["Object"], dot.inputs[0])
        remap = nodes.new("ShaderNodeMapRange")
        remap.clamp = False
        remap.inputs["From Min"].default_value = center_value - extent * 0.5
        remap.inputs["From Max"].default_value = center_value + extent * 0.5
        remap.inputs["To Min"].default_value = 0.0
        remap.inputs["To Max"].default_value = 1.0
        links.new(dot.outputs["Value"], remap.inputs["Value"])
        return remap.outputs["Result"]

    u = projected_axis(horizontal, center.dot(horizontal), size[0])
    v = projected_axis(vertical, center.dot(vertical), size[1])
    combine = nodes.new("ShaderNodeCombineXYZ")
    links.new(u, combine.inputs["X"])
    links.new(v, combine.inputs["Y"])

    normal_dot = nodes.new("ShaderNodeVectorMath")
    normal_dot.operation = "DOT_PRODUCT"
    normal_dot.inputs[1].default_value = tuple(surface_normal)
    links.new(coordinates.outputs["Object"], normal_dot.inputs[0])
    plane_delta = nodes.new("ShaderNodeMath")
    plane_delta.operation = "SUBTRACT"
    plane_delta.inputs[1].default_value = center.dot(surface_normal)
    links.new(normal_dot.outputs["Value"], plane_delta.inputs[0])
    plane_distance = nodes.new("ShaderNodeMath")
    plane_distance.operation = "ABSOLUTE"
    links.new(plane_delta.outputs[0], plane_distance.inputs[0])
    plane_mask = nodes.new("ShaderNodeMath")
    plane_mask.operation = "LESS_THAN"
    plane_mask.inputs[1].default_value = depth
    links.new(plane_distance.outputs[0], plane_mask.inputs[0])
    return combine.outputs["Vector"], plane_mask.outputs[0]


def oriented_overlay_layer(
    nodes, links, coordinates, base_color, filename, patch, emission
):
    projection, plane_mask = oriented_projection(
        nodes, links, coordinates,
        patch["center"], patch["normal"], patch["projection_normal"],
        patch["size"], patch["depth"],
    )
    decal = image_node(nodes, TEXTURE_DIR / filename)
    decal.extension = "CLIP"
    links.new(projection, decal.inputs["Vector"])
    factor = nodes.new("ShaderNodeMath")
    factor.operation = "MULTIPLY"
    links.new(decal.outputs["Alpha"], factor.inputs[0])
    links.new(plane_mask, factor.inputs[1])

    tint = nodes.new("ShaderNodeMixRGB")
    tint.blend_type = "MULTIPLY"
    tint.inputs[0].default_value = 0.72
    tint.inputs[2].default_value = (0.34, 0.67, 1.0, 1.0)
    links.new(decal.outputs["Color"], tint.inputs[1])

    mix = nodes.new("ShaderNodeMixRGB")
    links.new(factor.outputs[0], mix.inputs[0])
    links.new(base_color, mix.inputs[1])
    links.new(tint.outputs["Color"], mix.inputs[2])
    return mix.outputs["Color"], tint.outputs["Color"], factor.outputs[0], emission


def overlay_layer(
    nodes, links, coordinates, base_color, filename, bounds, emission, opacity=1.0
):
    projection = local_projection(nodes, links, coordinates, *bounds)
    decal = image_node(nodes, TEXTURE_DIR / filename)
    decal.extension = "CLIP"
    links.new(projection.outputs["Vector"], decal.inputs["Vector"])

    separate = nodes.new("ShaderNodeSeparateXYZ")
    front = nodes.new("ShaderNodeMath")
    front.operation = "GREATER_THAN"
    front.inputs[1].default_value = 0.0
    factor = nodes.new("ShaderNodeMath")
    factor.operation = "MULTIPLY"
    links.new(coordinates.outputs["Object"], separate.inputs["Vector"])
    links.new(separate.outputs["Z"], front.inputs[0])
    links.new(decal.outputs["Alpha"], factor.inputs[0])
    links.new(front.outputs[0], factor.inputs[1])

    if opacity != 1.0:
        opacity_node = nodes.new("ShaderNodeMath")
        opacity_node.operation = "MULTIPLY"
        opacity_node.inputs[1].default_value = opacity
        links.new(factor.outputs[0], opacity_node.inputs[0])
        factor_output = opacity_node.outputs[0]
    else:
        factor_output = factor.outputs[0]

    tint = nodes.new("ShaderNodeMixRGB")
    tint.blend_type = "MULTIPLY"
    tint.inputs[0].default_value = 0.72
    tint.inputs[2].default_value = (0.34, 0.67, 1.0, 1.0)
    links.new(decal.outputs["Color"], tint.inputs[1])

    mix = nodes.new("ShaderNodeMixRGB")
    links.new(factor_output, mix.inputs[0])
    links.new(base_color, mix.inputs[1])
    links.new(tint.outputs["Color"], mix.inputs[2])
    return mix.outputs["Color"], tint.outputs["Color"], factor_output, emission


def reference_surface_layer(
    nodes,
    links,
    coordinates,
    base_color,
    filename,
    bounds,
    opacity,
    front_threshold=None,
):
    projection = local_projection(nodes, links, coordinates, *bounds)
    overlay = image_node(nodes, TEXTURE_DIR / filename)
    overlay.extension = "CLIP"
    links.new(projection.outputs["Vector"], overlay.inputs["Vector"])
    factor_output = overlay.outputs["Alpha"]
    if front_threshold is not None:
        separate = nodes.new("ShaderNodeSeparateXYZ")
        front = nodes.new("ShaderNodeMath")
        front.operation = "GREATER_THAN"
        front.inputs[1].default_value = front_threshold
        front_factor = nodes.new("ShaderNodeMath")
        front_factor.operation = "MULTIPLY"
        links.new(coordinates.outputs["Object"], separate.inputs["Vector"])
        links.new(separate.outputs["Z"], front.inputs[0])
        links.new(overlay.outputs["Alpha"], front_factor.inputs[0])
        links.new(front.outputs[0], front_factor.inputs[1])
        factor_output = front_factor.outputs[0]
    strength = nodes.new("ShaderNodeMath")
    strength.operation = "MULTIPLY"
    strength.inputs[1].default_value = opacity
    links.new(factor_output, strength.inputs[0])
    mix = nodes.new("ShaderNodeMixRGB")
    links.new(strength.outputs[0], mix.inputs[0])
    links.new(base_color, mix.inputs[1])
    links.new(overlay.outputs["Color"], mix.inputs[2])
    return mix.outputs["Color"]


def build_material(material_id, spec):
    display, metallic_value, roughness_value = spec
    material = bpy.data.materials.new(display)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    coordinates = nodes.new("ShaderNodeTexCoord")
    mapping = nodes.new("ShaderNodeMapping")
    texture_scale = 1.05 if material_id == "shield_worn" else 1.25
    mapping.inputs["Scale"].default_value = (texture_scale, texture_scale, 1.0)
    links.new(coordinates.outputs["UV"], mapping.inputs["Vector"])

    prefix = TEXTURE_DIR / f"kursa_{material_id}"
    albedo = image_node(nodes, Path(str(prefix) + "_albedo.png"))
    roughness = image_node(nodes, Path(str(prefix) + "_roughness.png"), True)
    metallic = image_node(nodes, Path(str(prefix) + "_metallic.png"), True)
    normal_texture = image_node(nodes, Path(str(prefix) + "_normal.png"), True)
    normal = nodes.new("ShaderNodeNormalMap")
    normal_strength = {
        "shield_worn": 0.70,
        "shield_frame": 0.44,
        "armor_gunmetal": 0.46,
        "armor_bluegray": 0.48,
        "light_steel": 0.40,
        "dark_mechanics": 0.58,
        "torso_mechanical": 0.54,
        "face_metal": 0.30,
        "hood_cloth": 0.18,
    }[material_id]
    normal.inputs["Strength"].default_value = normal_strength
    for texture in (albedo, roughness, metallic, normal_texture):
        links.new(mapping.outputs["Vector"], texture.inputs["Vector"])

    color = albedo.outputs["Color"]
    emission_color = None
    emission_factor = None
    emission_strength = 0.0
    if material_id == "torso_mechanical":
        color, _, _, _ = overlay_layer(
            nodes,
            links,
            coordinates,
            color,
            "kursa_torso_reference_glyph.png",
            (-20.5, 6.0, 116.0, 142.5),
            0.0,
            0.27,
        )
    elif material_id == "face_metal":
        color, left_emission, left_factor, emission_strength = oriented_overlay_layer(
            nodes, links, coordinates, color,
            "kursa_eye_left_reference_overlay.png",
            EYE_SURFACE_PATCHES["left"], 2.2,
        )
        color, right_emission, right_factor, _ = oriented_overlay_layer(
            nodes, links, coordinates, color,
            "kursa_eye_right_reference_overlay.png",
            EYE_SURFACE_PATCHES["right"], 2.2,
        )
        emission_add = nodes.new("ShaderNodeVectorMath")
        emission_add.operation = "ADD"
        links.new(left_emission, emission_add.inputs[0])
        links.new(right_emission, emission_add.inputs[1])
        factor_max = nodes.new("ShaderNodeMath")
        factor_max.operation = "MAXIMUM"
        links.new(left_factor, factor_max.inputs[0])
        links.new(right_factor, factor_max.inputs[1])
        emission_color = emission_add.outputs["Vector"]
        emission_factor = factor_max.outputs[0]
    elif material_id == "hood_cloth":
        color, hood_emission, hood_factor, _ = overlay_layer(
            nodes, links, coordinates, color,
            "kursa_hood_reference_decal.png",
            (-15.5, 4.0, 156.0, 170.2),
            0.28,
            0.46,
        )
        color, scarf_emission, scarf_factor, _ = overlay_layer(
            nodes, links, coordinates, color,
            "kursa_scarf_reference_decal.png",
            (-22.0, 8.0, 128.0, 150.0),
            0.18,
            0.38,
        )
        emission_add = nodes.new("ShaderNodeVectorMath")
        emission_add.operation = "ADD"
        links.new(hood_emission, emission_add.inputs[0])
        links.new(scarf_emission, emission_add.inputs[1])
        factor_add = nodes.new("ShaderNodeMath")
        factor_add.operation = "MAXIMUM"
        links.new(hood_factor, factor_add.inputs[0])
        links.new(scarf_factor, factor_add.inputs[1])
        emission_color = emission_add.outputs["Vector"]
        emission_factor = factor_add.outputs[0]
        emission_strength = 0.20

    links.new(color, shader.inputs["Base Color"])
    links.new(roughness.outputs["Color"], shader.inputs["Roughness"])
    links.new(metallic.outputs["Color"], shader.inputs["Metallic"])
    links.new(normal_texture.outputs["Color"], normal.inputs["Color"])
    links.new(normal.outputs["Normal"], shader.inputs["Normal"])
    shader.inputs["Metallic"].default_value = metallic_value
    shader.inputs["Roughness"].default_value = roughness_value
    if emission_color is not None:
        emission_mix = nodes.new("ShaderNodeMixRGB")
        emission_mix.inputs[1].default_value = (0.0, 0.0, 0.0, 1.0)
        links.new(emission_factor, emission_mix.inputs[0])
        links.new(emission_color, emission_mix.inputs[2])
        links.new(emission_mix.outputs["Color"], shader.inputs["Emission Color"])
        shader.inputs["Emission Strength"].default_value = emission_strength
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def dominant_group(obj, polygon):
    weights = Counter()
    for vertex_index in polygon.vertices:
        for membership in obj.data.vertices[vertex_index].groups:
            weights[obj.vertex_groups[membership.group].name] += membership.weight
    return weights.most_common(1)[0][0] if weights else None


def classify_polygon(polygon, component_id, group):
    if component_id in SHIELD_BODY:
        return "shield_worn"
    if component_id in SHIELD_FRAME:
        return "shield_frame"
    if component_id == 7:
        # The existing protruding front facial planes occupy the front (+Z)
        # middle of the inspected head component; all wrapping surfaces remain cloth.
        if 143.0 <= polygon.center.y <= 160.5 and polygon.center.z >= 5.2:
            return "face_metal"
        return "hood_cloth"
    if component_id in TORSO_MECHANICAL_COMPONENTS:
        return "torso_mechanical"
    if component_id in BLUEGRAY_PANELS:
        return "armor_bluegray"
    if component_id in LIGHT_STEEL_PANELS:
        return "light_steel"
    if component_id in DARK_COMPONENTS:
        return "dark_mechanics"
    if group in {"LeftHand", "RightHand", "neck", "Hips"}:
        return "dark_mechanics"
    return "armor_gunmetal"


def bounds_of(objects):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects if obj.type == "MESH" for corner in obj.bound_box
    ]
    low = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    high = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    return low, high


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def render_view(scene, camera, center, distance, angle, elevation, filename, zoom=1.0):
    radians = math.radians(angle)
    camera.location = center + Vector((
        distance * math.sin(radians) / zoom,
        -distance * math.cos(radians) / zoom,
        distance * elevation / zoom,
    ))
    point_at(camera, center)
    scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def main():
    for directory in (RENDER_DIR, BLENDER_DIR, EXPORT_DIR):
        directory.mkdir(parents=True, exist_ok=True)
    inspection = json.loads(INSPECTION_JSON.read_text(encoding="utf-8"))
    mesh_info = next(item for item in inspection["objects"] if item["type"] == "MESH")
    component_by_polygon = {
        polygon_index: component["component_id"]
        for component in mesh_info["connected_components"]
        for polygon_index in component["polygon_indices"]
    }

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    scene = bpy.context.scene
    mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH")
    armature_obj = next(obj for obj in scene.objects if obj.type == "ARMATURE")
    before_mesh = mesh_signature(mesh_obj)
    before_armature = armature_signature(armature_obj)
    before_actions = action_signature()

    materials = {
        material_id: build_material(material_id, spec)
        for material_id, spec in MATERIAL_SPECS.items()
    }
    mesh_obj.data.materials.clear()
    for material_id in MATERIAL_ORDER:
        mesh_obj.data.materials.append(materials[material_id])
    material_indices = {name: index for index, name in enumerate(MATERIAL_ORDER)}

    assignments = []
    counts = Counter()
    for polygon in mesh_obj.data.polygons:
        component_id = component_by_polygon[polygon.index]
        group = dominant_group(mesh_obj, polygon)
        material_id = classify_polygon(polygon, component_id, group)
        polygon.material_index = material_indices[material_id]
        counts[material_id] += 1
        assignments.append({
            "polygon_index": polygon.index,
            "component_id": component_id,
            "dominant_group": group,
            "material": material_id,
        })

    after_mesh = mesh_signature(mesh_obj)
    after_armature = armature_signature(armature_obj)
    after_actions = action_signature()
    preservation = {
        "result": "PASS" if (
            before_mesh == after_mesh
            and before_armature == after_armature
            and before_actions == after_actions
        ) else "FAIL",
        "source_fbx": "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx",
        "source_fbx_sha256": sha256(SOURCE_FBX),
        "before_mesh": before_mesh,
        "after_mesh": after_mesh,
        "before_armature": before_armature,
        "after_armature": after_armature,
        "before_actions": before_actions,
        "after_actions": after_actions,
        "scope": "Material slots and polygon material indices only; geometry, UVs, weights, armature, and animation data are unchanged.",
    }
    if preservation["result"] != "PASS":
        raise RuntimeError("Geometry or rig data changed while applying materials.")

    (SAMPLE_ROOT / "MATERIAL_ASSIGNMENT.json").write_text(
        json.dumps({
            "assignment_basis": "Exact imported-FBX connected components, existing vertex weights, and visually inspected existing head planes; no new geometry.",
            "material_order": MATERIAL_ORDER,
            "polygon_material_counts": dict(counts),
            "component_sets": {
                "shield_body": sorted(SHIELD_BODY),
                "shield_frame": sorted(SHIELD_FRAME),
                "bluegray_panels": sorted(BLUEGRAY_PANELS),
                "light_steel_panels": sorted(LIGHT_STEEL_PANELS),
                "torso_mechanical_components": sorted(TORSO_MECHANICAL_COMPONENTS),
                "dark_components": sorted(DARK_COMPONENTS),
            },
            "eye_projection": {
                "textures": [
                    "textures/kursa_eye_left_reference_overlay.png",
                    "textures/kursa_eye_right_reference_overlay.png",
                ],
                "coordinate_space": "Imported mesh local coordinates",
                "rejected_single_plane_bounds": [-9.25, 9.25, 145.0, 162.8],
                "rejected_horizontal_offset": 5.25,
                "surface_patches": EYE_SURFACE_PATCHES,
                "basis": "Eye centers and surface depth-mask normals remain ray-mapped to the user's exact face crop. Both UV projections use the shared target-view visual-plane normal (0.552875, -0.117583, 0.824926), and the right depth mask is 2.05 to prevent vertical clipping. Both patch dimensions remain enlarged to 2x; no eye geometry added.",
            },
            "assignments": assignments,
        }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
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

    world = bpy.data.worlds.new("Kursa_Review_World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.86, 0.89, 0.90, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.42

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.name = "Kursa_Review_Camera"
    camera.data.lens = 62
    scene.camera = camera
    for name, location, energy, size, color in (
        ("Key", center + Vector((-radius * 0.75, -radius, radius * 1.45)), 420, radius * 1.15, (1.0, 0.90, 0.78)),
        ("Fill", center + Vector((radius, -radius * 0.35, radius * 0.55)), 72, radius * 0.95, (0.58, 0.76, 1.0)),
        ("Rim", center + Vector((0.15 * radius, radius, radius * 1.1)), 260, radius, (0.55, 0.72, 1.0)),
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
    floor_material = bpy.data.materials.new("Review_Only_Floor_Material")
    floor_material.use_nodes = True
    floor_shader = floor_material.node_tree.nodes.get("Principled BSDF")
    floor_shader.inputs["Base Color"].default_value = (0.62, 0.66, 0.67, 1.0)
    floor_shader.inputs["Roughness"].default_value = 0.86
    floor.data.materials.append(floor_material)

    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 1280
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = -0.18
    scene.frame_set(1)

    render_view(scene, camera, center, distance, 0, 0.06, "01_front_kursa_reference_match.png")
    render_view(scene, camera, center, distance, 32, 0.09, "02_three_quarter_kursa_reference_match.png")
    render_view(scene, camera, center, distance, 90, 0.06, "04_side_current_model_material.png")
    render_view(scene, camera, center, distance, 180, 0.06, "05_rear_current_model_material.png")
    head_center = center.copy()
    head_center.z = low.z + extent.z * 0.86
    render_view(scene, camera, head_center, distance, 0, 0.02, "07_head_front_detail.png", zoom=2.75)
    render_view(scene, camera, head_center, distance, 30, 0.02, "07_head_three_quarter_detail.png", zoom=2.75)
    render_view(scene, camera, head_center, distance, -30, 0.02, "07_head_left_three_quarter_detail.png", zoom=2.75)
    render_view(scene, camera, head_center, distance, 60, 0.02, "07_head_right_60_detail.png", zoom=2.75)
    render_view(scene, camera, head_center, distance, -60, 0.02, "07_head_left_60_detail.png", zoom=2.75)
    shield_center = center.copy()
    shield_center.x += extent.x * 0.18
    shield_center.z = low.z + extent.z * 0.62
    render_view(scene, camera, shield_center, distance, 15, 0.03, "10_shield_arm_detail.png", zoom=1.78)
    torso_center = center.copy()
    torso_center.z = low.z + extent.z * 0.62
    render_view(scene, camera, torso_center, distance, 0, 0.02, "12_torso_front_detail.png", zoom=2.45)

    floor.hide_render = True
    camera.hide_render = True
    for obj in scene.objects:
        obj.select_set(False)
    armature_obj.select_set(True)
    mesh_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj

    blend_path = BLENDER_DIR / "Kursa_Appearance_ReferenceSync.blend"
    fbx_path = EXPORT_DIR / "Kursa_Appearance_ReferenceSync.fbx"
    glb_path = EXPORT_DIR / "Kursa_Appearance_ReferenceSync.glb"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path), use_selection=True,
        object_types={"ARMATURE", "MESH"}, add_leaf_bones=False,
        bake_anim=True, apply_unit_scale=False,
        apply_scale_options="FBX_SCALE_NONE", use_space_transform=True,
        path_mode="COPY", embed_textures=True,
    )
    bpy.ops.export_scene.gltf(
        filepath=str(glb_path), export_format="GLB", use_selection=True,
        export_skins=True, export_animations=True, export_materials="EXPORT",
    )
    print(json.dumps({
        "preservation": preservation["result"],
        "material_counts": dict(counts),
        "blend": str(blend_path), "fbx": str(fbx_path), "glb": str(glb_path),
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
