import bpy
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
        "display": "Pahur_Armor_BlueGray_Worn",
        "metallic": 0.68,
        "roughness": 0.48,
    },
    "light_steel": {
        "display": "Pahur_Light_Steel_Panels",
        "metallic": 0.76,
        "roughness": 0.52,
    },
    "dark_mechanics": {
        "display": "Pahur_Dark_Mechanics",
        "metallic": 0.70,
        "roughness": 0.56,
    },
    "hood_navy_cloth": {
        "display": "Pahur_Hood_Navy_Cloth",
        "metallic": 0.02,
        "roughness": 0.82,
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
        "display": "Pahur_Optic_Blue_Emission",
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
}
MATERIAL_ORDER = list(MATERIAL_SPECS)


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
    mapping.inputs["Scale"].default_value = (
        5.0 if material_id == "hood_navy_cloth" else 3.2,
        5.0 if material_id == "hood_navy_cloth" else 3.2,
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
            TEXTURE_DIR / "pahur_shared_micro_normal.png",
            non_color=True,
        )
        normal_map = nodes.new("ShaderNodeNormalMap")
        normal_map.inputs["Strength"].default_value = (
            0.18 if material_id == "hood_navy_cloth" else 0.27
        )
        for texture in (albedo, roughness, metallic, normal_texture):
            links.new(mapping.outputs["Vector"], texture.inputs["Vector"])
        if material_id in {"hood_navy_cloth", "dark_mechanics"}:
            decal = image_node(
                nodes,
                TEXTURE_DIR / "pahur_head_reference_projection_decal.png",
            )
            links.new(uv.outputs["Generated"], decal.inputs["Vector"])
            separate_generated = nodes.new("ShaderNodeSeparateXYZ")
            front_mask = nodes.new("ShaderNodeMath")
            front_mask.operation = "GREATER_THAN"
            front_mask.inputs[1].default_value = 0.34
            decal_factor = nodes.new("ShaderNodeMath")
            decal_factor.operation = "MULTIPLY"
            base_mix = nodes.new("ShaderNodeMixRGB")
            emission_mix = nodes.new("ShaderNodeMixRGB")
            emission_mix.inputs[1].default_value = (0.0, 0.0, 0.0, 1.0)
            links.new(
                uv.outputs["Generated"], separate_generated.inputs["Vector"]
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
            shader.inputs["Emission Strength"].default_value = (
                2.4 if material_id == "dark_mechanics" else 1.9
            )
        else:
            links.new(albedo.outputs["Color"], shader.inputs["Base Color"])
        links.new(roughness.outputs["Color"], shader.inputs["Roughness"])
        links.new(metallic.outputs["Color"], shader.inputs["Metallic"])
        links.new(normal_texture.outputs["Color"], normal_map.inputs["Color"])
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
    center = polygon.center
    x, height, front = center.x, center.y, center.z

    if component_id in {6, 7}:
        return "flame_orange"
    if component_id == 5:
        return "optic_blue"
    if component_id in {4, 8, 10}:
        return "heat_bronze"
    if component_id == 9:
        return "fuel_tank_steel"
    if component_id in {2, 3}:
        return "hose_rubber"
    if component_id == 1:
        return "weapon_gunmetal"

    if group in {"Head", "neck"}:
        if height >= 126.0:
            return "hood_navy_cloth"
        return "dark_mechanics"

    if group in {"RightHand", "RightForeArm"} and x <= -19.0:
        return "weapon_gunmetal"

    if group in {"LeftHand", "RightHand"}:
        return "dark_mechanics"

    if group in {
        "LeftShoulder",
        "RightShoulder",
        "LeftArm",
        "RightArm",
    }:
        if height < 82.0:
            return "dark_mechanics"
        if front >= 20.0:
            return "light_steel"
        return "armor_bluegray"

    if group in {"Spine", "Spine01", "Spine02"}:
        if height < 73.0:
            return "dark_mechanics"
        if front >= 24.0 and abs(x) <= 15.0:
            return "light_steel"
        return "armor_bluegray"

    if group == "Hips":
        return "dark_mechanics"

    if group in {"LeftUpLeg", "RightUpLeg"}:
        if front >= 8.0 and height >= 38.0:
            return "light_steel"
        return "armor_bluegray"

    if group in {"LeftLeg", "RightLeg"}:
        if height <= 20.0:
            return "dark_mechanics"
        return "armor_bluegray"

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
    before_mesh = mesh_signature(mesh_obj)
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

    after_mesh = mesh_signature(mesh_obj)
    after_armature = armature_signature(armature_obj)
    preservation = {
        "result": "PASS"
        if before_mesh == after_mesh and before_armature == after_armature
        else "FAIL",
        "source_fbx": "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx",
        "source_fbx_sha256": sha256(SOURCE_FBX),
        "before_mesh": before_mesh,
        "after_mesh": after_mesh,
        "before_armature": before_armature,
        "after_armature": after_armature,
        "mesh_unchanged_except_material_assignment": before_mesh == after_mesh,
        "armature_unchanged": before_armature == after_armature,
        "scope": "Material slots and polygon material indices only.",
    }
    if preservation["result"] != "PASS":
        raise RuntimeError("Source mesh, UV, weights, or armature changed.")

    (SAMPLE_ROOT / "MATERIAL_ASSIGNMENT.json").write_text(
        json.dumps(
            {
                "assignment_basis": (
                    "Existing connected components, existing vertex weights, "
                    "and existing polygon centers."
                ),
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
        0.54,
        0.60,
        0.63,
        1.0,
    )
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.72

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.name = "Pahur_Review_Camera"
    camera.data.lens = 62
    scene.camera = camera

    for name, location, energy, size, color in (
        (
            "Key",
            center + Vector((-radius * 0.75, -radius, radius * 1.45)),
            180,
            radius * 1.4,
            (1.0, 0.90, 0.78),
        ),
        (
            "Fill",
            center + Vector((radius, -radius * 0.35, radius * 0.55)),
            85,
            radius * 1.1,
            (0.58, 0.76, 1.0),
        ),
        (
            "Rim",
            center + Vector((0.15 * radius, radius, radius * 1.1)),
            240,
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
    floor_shader.inputs["Base Color"].default_value = (0.24, 0.29, 0.31, 1.0)
    floor_shader.inputs["Roughness"].default_value = 0.80
    floor.data.materials.append(floor_mat)

    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = -0.55
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
