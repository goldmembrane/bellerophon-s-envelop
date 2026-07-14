import bpy
import bmesh
import hashlib
import json
import math
from pathlib import Path
from mathutils import Matrix, Vector


PROJECT_ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / "accelerando" / "rigged_attack_model_match"
SOURCE_GLB = PROJECT_ROOT / "enemies model" / "accelerando.glb"
REFERENCE_BLEND = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "accelerando"
    / "antenna_tip_ring_embedded_connection_fix"
    / "exports"
    / "accelerando_antenna_tip_ring_embedded_connection_sample.blend"
)
EXPORT_DIR = SAMPLE_ROOT / "exports"
RENDER_DIR = SAMPLE_ROOT / "renders"
OUTPUT_BLEND = EXPORT_DIR / "accelerando_rigged_attack_model_match.blend"
OUTPUT_GLB = EXPORT_DIR / "accelerando_rigged_attack_model_match.glb"
MANIFEST_PATH = SAMPLE_ROOT / "asset_manifest.json"

EXPECTED_SOURCE_SHA256 = "F5F5B605C66C582C0B9B0CB29433FE812EC952D8DFF7D5BE39055489E0367B69"
BODY_NAME = "Accelerando_RiggedAttack_Body"
ARMATURE_NAME = "UniRigArmature"
ROOT_NAME = "Accelerando_RiggedAttackModel"

SIDES = {
    "Left": {
        "sign": -1.0,
        "root_bone": "Bone_011",
        "mid_bone": "Bone_010",
        "tip_bone": "Bone_009",
        "attachment_bone": "Bone_010",
        "chain_start": Vector((-1.04, -1.22, 1.30)),
        "mace_pivot": Vector((-1.43, -1.22, 0.43)),
    },
    "Right": {
        "sign": 1.0,
        "root_bone": "Bone_008",
        "mid_bone": "Bone_007",
        "tip_bone": "Bone_006",
        "attachment_bone": "Bone_007",
        "chain_start": Vector((1.04, -1.22, 1.30)),
        "mace_pivot": Vector((1.43, -1.22, 0.43)),
    },
}
ATTACK_BONES = {
    side_data[key]
    for side_data in SIDES.values()
    for key in ("root_bone", "mid_bone", "tip_bone")
}


def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def ensure_output_directories():
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    RENDER_DIR.mkdir(parents=True, exist_ok=True)


def remove_object(obj):
    if obj is not None and obj.name in bpy.data.objects:
        bpy.data.objects.remove(obj, do_unlink=True)


def remove_reference_render_helpers():
    for obj in list(bpy.context.scene.objects):
        if (
            obj.type in {"CAMERA", "LIGHT"}
            or obj.name.startswith("RenderOnly_")
            or obj.name in {"Key_Area_Light", "Rim_Area_Light", "Render_Camera"}
        ):
            remove_object(obj)


def create_match_material(name, base_color, metallic, roughness, noise_scale, bump_strength):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (*base_color, 1.0)
    material.metallic = metallic
    material.roughness = roughness

    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    principled = nodes.new("ShaderNodeBsdfPrincipled")
    noise = nodes.new("ShaderNodeTexNoise")
    ramp = nodes.new("ShaderNodeValToRGB")
    bump = nodes.new("ShaderNodeBump")

    output.location = (560, 0)
    principled.location = (300, 0)
    ramp.location = (40, 60)
    noise.location = (-240, 60)
    bump.location = (40, -170)

    noise.inputs["Scale"].default_value = noise_scale
    noise.inputs["Detail"].default_value = 3.0
    noise.inputs["Roughness"].default_value = 0.64
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.08

    def adjusted(factor):
        return tuple(max(0.0, min(1.0, channel * factor)) for channel in base_color) + (1.0,)

    ramp.color_ramp.elements[0].position = 0.24
    ramp.color_ramp.elements[0].color = adjusted(0.58)
    ramp.color_ramp.elements[1].position = 0.78
    ramp.color_ramp.elements[1].color = adjusted(1.24)

    principled.inputs["Metallic"].default_value = metallic
    principled.inputs["Roughness"].default_value = roughness
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], principled.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])
    links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    return material


def build_match_materials():
    return {
        "flesh": create_match_material(
            "M_Accelerando_WetTaupeFlesh_Match",
            (0.39, 0.32, 0.27),
            metallic=0.0,
            roughness=0.28,
            noise_scale=56.0,
            bump_strength=0.10,
        ),
        "shell": create_match_material(
            "M_Accelerando_DarkShell_Match",
            (0.14, 0.12, 0.10),
            metallic=0.0,
            roughness=0.68,
            noise_scale=42.0,
            bump_strength=0.065,
        ),
        "metal": create_match_material(
            "M_Accelerando_RustyMetal_Match",
            (0.30, 0.15, 0.08),
            metallic=0.72,
            roughness=0.54,
            noise_scale=72.0,
            bump_strength=0.115,
        ),
    }


def classify_material(material_name):
    lowered = material_name.lower()
    if any(token in lowered for token in ("metal", "iron", "rust", "chain")):
        return "metal"
    if any(token in lowered for token in ("shell", "socketlip", "bulge", "saddle")):
        return "shell"
    return "flesh"


def remap_object_materials(obj, materials):
    if obj.type != "MESH":
        return
    for index, material_slot in enumerate(obj.data.materials):
        source_name = material_slot.name if material_slot else ""
        obj.data.materials[index] = materials[classify_material(source_name)]


def delete_faces(obj, delete_predicate):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    faces_to_delete = [face for face in bm.faces if delete_predicate(face)]
    if faces_to_delete:
        bmesh.ops.delete(bm, geom=faces_to_delete, context="FACES")
    loose_vertices = [vertex for vertex in bm.verts if not vertex.link_faces]
    if loose_vertices:
        bmesh.ops.delete(bm, geom=loose_vertices, context="VERTS")
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return len(faces_to_delete)


def duplicate_body_for_mace(source_body, side_name, metal_index, pivot):
    sign = SIDES[side_name]["sign"]
    mace = source_body.copy()
    mace.data = source_body.data.copy()
    bpy.context.collection.objects.link(mace)
    mace.name = f"Accelerando_{side_name}_MaceHead"
    mace.data.name = f"Accelerando_{side_name}_MaceHead_Mesh"

    delete_faces(
        mace,
        lambda face: face.material_index != metal_index or (face.calc_center_median().x * sign) <= 0.0,
    )
    for modifier in list(mace.modifiers):
        mace.modifiers.remove(modifier)
    mace.vertex_groups.clear()
    for vertex in mace.data.vertices:
        vertex.co -= pivot
    mace.location = pivot
    mace["physics_role"] = "Rigidbody mace head; ConfigurableJoint chain endpoint in Unity"
    mace["visible_socket_ring"] = False
    return mace


def split_body_and_maces(materials):
    body = bpy.data.objects.get("Accelerando_ConnectedColored_Body")
    if body is None or body.type != "MESH":
        raise RuntimeError("Approved reference body mesh is missing.")

    metal_index = None
    for index, material in enumerate(body.data.materials):
        if material and classify_material(material.name) == "metal":
            metal_index = index
            break
    if metal_index is None:
        raise RuntimeError("Approved reference body has no metal material slot.")

    maces = {
        side_name: duplicate_body_for_mace(body, side_name, metal_index, side_data["mace_pivot"])
        for side_name, side_data in SIDES.items()
    }
    delete_faces(body, lambda face: face.material_index == metal_index)
    body.name = BODY_NAME
    body.data.name = "Accelerando_RiggedAttack_Body_Mesh"
    for modifier in list(body.modifiers):
        body.modifiers.remove(modifier)
    remap_object_materials(body, materials)
    for mace in maces.values():
        remap_object_materials(mace, materials)
    return body, maces


def isolate_attack_weights_to_antennae(body):
    attack_groups = {
        group.index: group
        for group in body.vertex_groups
        if group.name in ATTACK_BONES
    }
    root_group = body.vertex_groups.get("Bone_000")
    if len(attack_groups) != len(ATTACK_BONES) or root_group is None:
        raise RuntimeError("Required attack or root vertex groups are missing.")

    adjusted_vertices = 0
    for vertex in body.data.vertices:
        coordinate = vertex.co
        is_antenna_region = abs(coordinate.x) > 0.42 and coordinate.y < -0.45 and coordinate.z > 0.55
        if is_antenna_region:
            continue

        memberships = [(membership.group, membership.weight) for membership in vertex.groups]
        removed_weight = sum(weight for group_index, weight in memberships if group_index in attack_groups)
        if removed_weight <= 0.000001:
            continue

        remaining = [(group_index, weight) for group_index, weight in memberships if group_index not in attack_groups]
        for group in attack_groups.values():
            group.remove([vertex.index])
        remaining_total = sum(weight for _group_index, weight in remaining)
        if remaining_total > 0.000001:
            for group_index, weight in remaining:
                body.vertex_groups[group_index].add([vertex.index], weight / remaining_total, "REPLACE")
        else:
            root_group.add([vertex.index], 1.0, "REPLACE")
        adjusted_vertices += 1

    body["attack_weight_isolation"] = "Attack bone weights kept only in bilateral antenna regions"
    body["attack_weight_adjusted_vertex_count"] = adjusted_vertices
    return adjusted_vertices


def remove_obsolete_mace_support_geometry(body):
    def is_obsolete_support(face):
        center = face.calc_center_median()
        lateral = abs(center.x)
        is_upper_rod_cap = (
            0.88 < lateral < 1.26
            and center.y < -0.64
            and 0.84 < center.z < 1.35
        )
        is_lower_rod_hand_or_socket = (
            0.88 < lateral < 1.42
            and center.y < -0.30
            and center.z < 0.78
        )
        return is_upper_rod_cap or is_lower_rod_hand_or_socket

    removed_faces = delete_faces(body, is_obsolete_support)
    body["removed_obsolete_mace_support_face_count"] = removed_faces
    return removed_faces


def import_source_armature():
    existing_objects = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE_GLB))
    imported_objects = [obj for obj in bpy.data.objects if obj not in existing_objects]
    armatures = [obj for obj in imported_objects if obj.type == "ARMATURE"]
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one imported armature, found {len(armatures)}.")
    armature = armatures[0]
    armature.name = ARMATURE_NAME
    armature.data.name = "UniRigArmature_Rig"
    for obj in imported_objects:
        if obj != armature:
            remove_object(obj)
    return armature


def create_root_and_parent_objects(body, armature, maces):
    root = bpy.data.objects.new(ROOT_NAME, None)
    bpy.context.collection.objects.link(root)
    root["sample_status"] = "USER_APPROVAL_REQUIRED"
    root["source_model"] = "enemies model/accelerando.glb"
    root["unity_target"] = "CargoRunMvp / Accelerando_AntennaStrike / Model"
    root["attack_control_scope"] = "Antenna bones only: Bone_008,007,006 and Bone_011,010,009"
    root["chain_runtime_role"] = "ConfigurableJoint physics; 12 links per side"

    armature.parent = root
    armature.matrix_parent_inverse = root.matrix_world.inverted()
    body_world = body.matrix_world.copy()
    body.parent = armature
    body.matrix_world = body_world
    modifier = body.modifiers.new("Accelerando_RiggedAttack_Armature", "ARMATURE")
    modifier.object = armature
    modifier.use_deform_preserve_volume = True

    for mace in maces.values():
        world_matrix = mace.matrix_world.copy()
        mace.parent = root
        mace.matrix_world = world_matrix

    for obj in list(bpy.context.scene.objects):
        if obj in {root, armature, body, *maces.values()}:
            continue
        if obj.type == "MESH" and obj.name.startswith("Accelerando_"):
            world_matrix = obj.matrix_world.copy()
            obj.parent = root
            obj.matrix_world = world_matrix
    return root


def remove_visible_mace_socket_rings():
    removed = []
    for side_name in SIDES:
        object_name = f"Accelerando_{side_name}_MaceSocket_Ring"
        obj = bpy.data.objects.get(object_name)
        if obj is not None:
            removed.append(object_name)
            remove_object(obj)
    return removed


def parent_tip_connection_objects_to_bones(armature):
    for side_name, side_data in SIDES.items():
        candidates = [
            obj
            for obj in bpy.context.scene.objects
            if obj.type == "MESH"
            and obj.name.startswith(f"Accelerando_{side_name}_AntennaTip")
        ]
        for obj in candidates:
            world_matrix = obj.matrix_world.copy()
            obj.parent = armature
            obj.parent_type = "BONE"
            obj.parent_bone = side_data["attachment_bone"]
            obj.matrix_world = world_matrix


def create_physics_anchors(root, armature):
    anchors = {}
    for side_name, side_data in SIDES.items():
        tip_anchor = bpy.data.objects.new(f"Accelerando_{side_name}_AntennaPhysicsAnchor", None)
        bpy.context.collection.objects.link(tip_anchor)
        tip_anchor.empty_display_type = "SPHERE"
        tip_anchor.empty_display_size = 0.055
        tip_anchor.matrix_world = Matrix.Translation(side_data["chain_start"])
        world_matrix = tip_anchor.matrix_world.copy()
        tip_anchor.parent = armature
        tip_anchor.parent_type = "BONE"
        tip_anchor.parent_bone = side_data["attachment_bone"]
        tip_anchor.matrix_world = world_matrix
        tip_anchor["physics_role"] = "Kinematic antenna anchor for first ConfigurableJoint link"

        mace_anchor = bpy.data.objects.new(f"Accelerando_{side_name}_MacePhysicsAnchor", None)
        bpy.context.collection.objects.link(mace_anchor)
        mace_anchor.empty_display_type = "SPHERE"
        mace_anchor.empty_display_size = 0.055
        mace_anchor.location = side_data["mace_pivot"]
        mace_anchor.parent = root
        mace_anchor["physics_role"] = "Hidden mace endpoint; no rendered socket ring"
        anchors[side_name] = (tip_anchor, mace_anchor)
    return anchors


def annotate_rig(armature):
    bone_names = {bone.name for bone in armature.data.bones}
    expected = {f"Bone_{index:03d}" for index in range(18)}
    if bone_names != expected:
        raise RuntimeError(f"Rig bone set changed: {sorted(bone_names)}")
    for bone in armature.data.bones:
        bone["attack_control"] = "AntennaOnly" if bone.name in ATTACK_BONES else "Excluded"
    armature["source_bone_count"] = len(bone_names)
    armature["skin_weight_group_count"] = 18


def validate_neutral_structure(body, armature, maces, removed_socket_names):
    if len(armature.data.bones) != 18:
        raise RuntimeError("Armature bone count is not 18.")
    if len(body.vertex_groups) != 18:
        raise RuntimeError(f"Body vertex group count is {len(body.vertex_groups)}, expected 18.")
    if not any(modifier.type == "ARMATURE" and modifier.object == armature for modifier in body.modifiers):
        raise RuntimeError("Body has no Armature modifier bound to UniRigArmature.")
    for side_name in SIDES:
        links = [
            obj for obj in bpy.context.scene.objects
            if obj.name.startswith(f"Accelerando_{side_name}_ConnectedChain_Link_")
        ]
        if len(links) != 12:
            raise RuntimeError(f"{side_name} chain link count is {len(links)}, expected 12.")
        if side_name not in maces or len(maces[side_name].data.polygons) == 0:
            raise RuntimeError(f"{side_name} mace head mesh is empty.")
    for object_name in removed_socket_names:
        if bpy.data.objects.get(object_name) is not None:
            raise RuntimeError(f"Visible socket ring still exists: {object_name}")
    if any(obj.name.endswith("MaceSocket_Ring") for obj in bpy.context.scene.objects):
        raise RuntimeError("A visible MaceSocket_Ring object remains in the sample.")


def descendants(root):
    result = []
    stack = list(root.children)
    while stack:
        current = stack.pop()
        result.append(current)
        stack.extend(current.children)
    return result


def export_neutral_sample(root):
    export_objects = [root, *descendants(root)]
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    for obj in export_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(
        filepath=str(OUTPUT_GLB),
        export_format="GLB",
        use_selection=True,
        export_animations=False,
        export_skins=True,
        export_all_influences=True,
        export_apply=False,
    )
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND))
    return export_objects


def create_flat_material(name, color, emission_strength=0.0):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (*color, 1.0)
    principled.inputs["Roughness"].default_value = 0.42
    if emission_strength > 0.0:
        principled.inputs["Emission Color"].default_value = (*color, 1.0)
        principled.inputs["Emission Strength"].default_value = emission_strength
    return material


def setup_render_scene(materials):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"

    world = scene.world or bpy.data.worlds.new("RiggedAttack_RenderWorld")
    scene.world = world
    world.color = (0.022, 0.026, 0.034)

    floor_material = create_flat_material("RenderOnly_FloorMaterial", (0.055, 0.061, 0.072))
    bpy.ops.mesh.primitive_plane_add(size=7.0, location=(0.0, 0.0, -0.012))
    floor = bpy.context.object
    floor.name = "RenderOnly_Floor"
    floor.data.materials.append(floor_material)

    key_data = bpy.data.lights.new("RenderOnly_Key", "AREA")
    key_data.energy = 980.0
    key_data.shape = "DISK"
    key_data.size = 4.5
    key = bpy.data.objects.new("RenderOnly_Key", key_data)
    bpy.context.collection.objects.link(key)
    key.location = (0.0, -4.5, 5.2)

    fill_data = bpy.data.lights.new("RenderOnly_Fill", "AREA")
    fill_data.energy = 520.0
    fill_data.size = 3.2
    fill = bpy.data.objects.new("RenderOnly_Fill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = (3.8, -1.8, 3.1)

    rim_data = bpy.data.lights.new("RenderOnly_Rim", "AREA")
    rim_data.energy = 760.0
    rim_data.size = 3.0
    rim = bpy.data.objects.new("RenderOnly_Rim", rim_data)
    bpy.context.collection.objects.link(rim)
    rim.location = (-3.8, 2.8, 4.0)

    camera_data = bpy.data.cameras.new("RenderOnly_Camera")
    camera = bpy.data.objects.new("RenderOnly_Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.data.type = "ORTHO"
    camera.data.lens = 70.0
    scene.camera = camera
    return camera, floor


def calculate_bounds(objects):
    corners = []
    for obj in objects:
        if obj.type != "MESH" or obj.hide_render:
            continue
        corners.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    minimum = Vector(tuple(min(corner[axis] for corner in corners) for axis in range(3)))
    maximum = Vector(tuple(max(corner[axis] for corner in corners) for axis in range(3)))
    return minimum, maximum


def look_at(camera, target):
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()


def render_view(camera, model_objects, filename, direction, ortho_multiplier=1.28):
    minimum, maximum = calculate_bounds(model_objects)
    center = (minimum + maximum) * 0.5
    size = maximum - minimum
    distance = max(size.x, size.y, size.z) * 3.6
    camera.location = center + Vector(direction).normalized() * distance + Vector((0.0, 0.0, size.z * 0.12))
    look_at(camera, center + Vector((0.0, 0.0, size.z * 0.08)))
    camera.data.ortho_scale = max(size.x, size.y, size.z) * ortho_multiplier
    bpy.context.scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def evaluated_world_vertices(obj):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        return [evaluated.matrix_world @ vertex.co for vertex in mesh.vertices]
    finally:
        evaluated.to_mesh_clear()


def set_attack_pose(armature):
    pose_values = {
        "Bone_008": (math.radians(24.0), math.radians(-8.0), math.radians(-24.0)),
        "Bone_007": (math.radians(12.0), math.radians(-4.0), math.radians(-12.0)),
        "Bone_006": (math.radians(6.0), 0.0, math.radians(-7.0)),
        "Bone_011": (math.radians(24.0), math.radians(8.0), math.radians(24.0)),
        "Bone_010": (math.radians(12.0), math.radians(4.0), math.radians(12.0)),
        "Bone_009": (math.radians(6.0), 0.0, math.radians(7.0)),
    }
    for pose_bone in armature.pose.bones:
        pose_bone.rotation_mode = "XYZ"
        pose_bone.location = (0.0, 0.0, 0.0)
        pose_bone.scale = (1.0, 1.0, 1.0)
        pose_bone.rotation_euler = pose_values.get(pose_bone.name, (0.0, 0.0, 0.0))
    bpy.context.view_layer.update()


def reset_pose(armature):
    for pose_bone in armature.pose.bones:
        pose_bone.matrix_basis.identity()
    bpy.context.view_layer.update()


def quadratic_bezier(start, control, end, t):
    return ((1.0 - t) ** 2) * start + 2.0 * (1.0 - t) * t * control + (t ** 2) * end


def apply_pose_to_chain_and_mace(anchors, maces):
    rest_state = {}
    bpy.context.view_layer.update()
    for side_name, side_data in SIDES.items():
        links = sorted(
            [
                obj for obj in bpy.context.scene.objects
                if obj.name.startswith(f"Accelerando_{side_name}_ConnectedChain_Link_")
            ],
            key=lambda obj: obj.name,
        )
        tip_anchor, mace_anchor = anchors[side_name]
        mace = maces[side_name]
        rest_state[side_name] = {
            "links": [(link, link.matrix_world.copy()) for link in links],
            "mace_object": mace,
            "mace_matrix": mace.matrix_world.copy(),
            "mace_anchor_object": mace_anchor,
            "mace_anchor_matrix": mace_anchor.matrix_world.copy(),
        }
        start = tip_anchor.matrix_world.translation.copy()
        sign = side_data["sign"]
        end = side_data["mace_pivot"] + Vector((-sign * 0.22, -0.62, 0.28))
        control = (start + end) * 0.5 + Vector((0.0, -0.16, -0.10))
        for index, link in enumerate(links):
            t = (index + 1.0) / (len(links) + 1.0)
            link.location = quadratic_bezier(start, control, end, t)
        mace.location = end
        mace_anchor.location = end
    bpy.context.view_layer.update()
    return rest_state


def restore_chain_and_mace(rest_state):
    for side_state in rest_state.values():
        for link, matrix_world in side_state["links"]:
            link.matrix_world = matrix_world
        side_state["mace_object"].matrix_world = side_state["mace_matrix"]
        side_state["mace_anchor_object"].matrix_world = side_state["mace_anchor_matrix"]
    bpy.context.view_layer.update()


def create_bone_overlay(armature):
    target_material = create_flat_material("RenderOnly_AttackBone", (1.0, 0.34, 0.04), emission_strength=0.8)
    excluded_material = create_flat_material("RenderOnly_ExcludedBone", (0.10, 0.48, 0.78), emission_strength=0.35)
    overlay_objects = []
    for pose_bone in armature.pose.bones:
        head = armature.matrix_world @ pose_bone.head
        tail = armature.matrix_world @ pose_bone.tail
        direction = tail - head
        length = direction.length
        if length <= 0.0001:
            continue
        radius = 0.035 if pose_bone.name in ATTACK_BONES else 0.022
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=length, location=(head + tail) * 0.5)
        cylinder = bpy.context.object
        cylinder.name = f"RenderOnly_Bone_{pose_bone.name}"
        cylinder.rotation_mode = "QUATERNION"
        cylinder.rotation_quaternion = direction.to_track_quat("Z", "Y")
        cylinder.data.materials.append(target_material if pose_bone.name in ATTACK_BONES else excluded_material)
        overlay_objects.append(cylinder)

        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=radius * 1.55, location=head)
        joint = bpy.context.object
        joint.name = f"RenderOnly_Joint_{pose_bone.name}"
        joint.data.materials.append(target_material if pose_bone.name in ATTACK_BONES else excluded_material)
        overlay_objects.append(joint)
    return overlay_objects


def remove_overlay(overlay_objects):
    for obj in overlay_objects:
        remove_object(obj)


def validate_pose(body, armature, rest_vertices, posed_vertices):
    if len(rest_vertices) != len(posed_vertices):
        raise RuntimeError("Evaluated body vertex count changed during pose validation.")
    moved_distances = [(posed - rest).length for rest, posed in zip(rest_vertices, posed_vertices)]
    moved_count = sum(distance > 0.0005 for distance in moved_distances)
    max_distance = max(moved_distances)
    if moved_count <= 0 or max_distance < 0.04:
        raise RuntimeError("Antenna pose did not deform the skinned body.")
    for pose_bone in armature.pose.bones:
        if pose_bone.name not in ATTACK_BONES and not pose_bone.matrix_basis.is_identity:
            raise RuntimeError(f"Excluded bone was directly posed: {pose_bone.name}")

    def extents(vertices):
        minimum = Vector(tuple(min(vertex[axis] for vertex in vertices) for axis in range(3)))
        maximum = Vector(tuple(max(vertex[axis] for vertex in vertices) for axis in range(3)))
        return maximum - minimum

    rest_extent = extents(rest_vertices)
    pose_extent = extents(posed_vertices)
    for axis in range(3):
        ratio = pose_extent[axis] / max(rest_extent[axis], 0.0001)
        if ratio < 0.58 or ratio > 1.65:
            raise RuntimeError(f"Body bounds collapsed during antenna pose on axis {axis}: {ratio:.3f}")
    return {
        "moved_vertex_count": moved_count,
        "maximum_vertex_displacement": round(max_distance, 6),
        "rest_extent": [round(value, 6) for value in rest_extent],
        "pose_extent": [round(value, 6) for value in pose_extent],
    }


def create_contact_sheet():
    try:
        from PIL import Image, ImageDraw
    except ModuleNotFoundError:
        return False
    entries = [
        ("accelerando_rigged_attack_front.png", "정면 · Unity 색/재질 일치"),
        ("accelerando_rigged_attack_side.png", "측면 · 체인 12링/철퇴 분리"),
        ("accelerando_rigged_attack_oblique.png", "사선 · 하단 소켓 링 제거"),
        ("accelerando_rigged_attack_rig_overlay.png", "리그 · 주황색=공격 제어 본"),
        ("accelerando_rigged_attack_pose_front.png", "포즈 · 더듬이 본만 제어"),
        ("accelerando_rigged_attack_pose_oblique.png", "포즈 · 체인/철퇴 전방 흔들림 계획"),
    ]
    sheet = Image.new("RGB", (1600, 1500), (18, 21, 27))
    draw = ImageDraw.Draw(sheet)
    for index, (filename, label) in enumerate(entries):
        image = Image.open(RENDER_DIR / filename).convert("RGB")
        image.thumbnail((780, 455), Image.Resampling.LANCZOS)
        column = index % 2
        row = index // 2
        x = 10 + column * 795
        y = 12 + row * 495
        sheet.paste(image, (x + (780 - image.width) // 2, y + 32 + (455 - image.height) // 2))
        draw.text((x + 14, y + 8), label, fill=(232, 231, 226))
    sheet.save(RENDER_DIR / "accelerando_rigged_attack_contact_sheet.png")
    return True


def write_manifest(
    body,
    armature,
    maces,
    pose_metrics,
    contact_sheet_created,
    adjusted_weight_vertices,
    removed_support_faces,
):
    manifest = {
        "status": "USER_APPROVAL_REQUIRED",
        "source": {
            "path": "enemies model/accelerando.glb",
            "sha256": sha256(SOURCE_GLB),
            "expected_sha256": EXPECTED_SOURCE_SHA256,
        },
        "reference": {
            "path": "artSample/enemies/accelerando/antenna_tip_ring_embedded_connection_fix/exports/accelerando_antenna_tip_ring_embedded_connection_sample.blend",
            "sha256": sha256(REFERENCE_BLEND),
        },
        "rig": {
            "armature": armature.name,
            "bone_count": len(armature.data.bones),
            "skin_vertex_group_count": len(body.vertex_groups),
            "attack_control_bones": sorted(ATTACK_BONES),
            "excluded_bones_directly_posed": [],
            "attack_weight_adjusted_vertex_count": adjusted_weight_vertices,
        },
        "model": {
            "body_object": body.name,
            "body_vertices": len(body.data.vertices),
            "body_polygons": len(body.data.polygons),
            "left_mace_vertices": len(maces["Left"].data.vertices),
            "right_mace_vertices": len(maces["Right"].data.vertices),
            "chain_links_per_side": 12,
            "visible_mace_socket_ring_count": 0,
            "removed_obsolete_mace_support_faces": removed_support_faces,
        },
        "unity_material_match": {
            "flesh_base_color": [0.39, 0.32, 0.27],
            "flesh_smoothness": 0.72,
            "shell_base_color": [0.14, 0.12, 0.10],
            "shell_smoothness": 0.32,
            "metal_base_color": [0.30, 0.15, 0.08],
            "metal_metallic": 0.72,
            "metal_smoothness": 0.46,
        },
        "pose_validation": pose_metrics,
        "exports": {
            "blend": str(OUTPUT_BLEND.relative_to(PROJECT_ROOT)).replace("\\", "/"),
            "blend_sha256": sha256(OUTPUT_BLEND),
            "glb": str(OUTPUT_GLB.relative_to(PROJECT_ROOT)).replace("\\", "/"),
            "glb_sha256": sha256(OUTPUT_GLB),
            "contact_sheet_created": contact_sheet_created,
        },
        "unity_runtime_applied": False,
    }
    with open(MANIFEST_PATH, "w", encoding="utf-8") as stream:
        json.dump(manifest, stream, ensure_ascii=False, indent=2)
        stream.write("\n")


def main():
    ensure_output_directories()
    if sha256(SOURCE_GLB) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("Source accelerando.glb hash changed before sample generation.")

    bpy.ops.wm.open_mainfile(filepath=str(REFERENCE_BLEND))
    remove_reference_render_helpers()
    materials = build_match_materials()
    for obj in list(bpy.context.scene.objects):
        remap_object_materials(obj, materials)

    removed_socket_names = remove_visible_mace_socket_rings()
    body, maces = split_body_and_maces(materials)
    removed_support_faces = remove_obsolete_mace_support_geometry(body)
    adjusted_weight_vertices = isolate_attack_weights_to_antennae(body)
    armature = import_source_armature()
    root = create_root_and_parent_objects(body, armature, maces)
    parent_tip_connection_objects_to_bones(armature)
    anchors = create_physics_anchors(root, armature)
    annotate_rig(armature)
    validate_neutral_structure(body, armature, maces, removed_socket_names)

    export_objects = export_neutral_sample(root)
    rest_vertices = evaluated_world_vertices(body)

    camera, _floor = setup_render_scene(materials)
    render_objects = [obj for obj in export_objects if obj.type == "MESH"]
    render_view(camera, render_objects, "accelerando_rigged_attack_front.png", (0.0, -1.0, 0.0))
    render_view(camera, render_objects, "accelerando_rigged_attack_side.png", (1.0, 0.0, 0.0))
    render_view(camera, render_objects, "accelerando_rigged_attack_oblique.png", (1.0, -1.0, 0.28))

    overlay = create_bone_overlay(armature)
    render_view(camera, [*render_objects, *overlay], "accelerando_rigged_attack_rig_overlay.png", (0.0, -1.0, 0.12), 1.35)
    remove_overlay(overlay)

    set_attack_pose(armature)
    rest_state = apply_pose_to_chain_and_mace(anchors, maces)
    posed_vertices = evaluated_world_vertices(body)
    pose_metrics = validate_pose(body, armature, rest_vertices, posed_vertices)
    render_view(camera, render_objects, "accelerando_rigged_attack_pose_front.png", (0.0, -1.0, 0.0))
    render_view(camera, render_objects, "accelerando_rigged_attack_pose_oblique.png", (1.0, -1.0, 0.28))
    overlay = create_bone_overlay(armature)
    render_view(camera, [*render_objects, *overlay], "accelerando_rigged_attack_pose_rig_overlay.png", (1.0, -1.0, 0.22), 1.36)
    remove_overlay(overlay)

    restore_chain_and_mace(rest_state)
    reset_pose(armature)
    contact_sheet_created = create_contact_sheet()
    write_manifest(
        body,
        armature,
        maces,
        pose_metrics,
        contact_sheet_created,
        adjusted_weight_vertices,
        removed_support_faces,
    )
    print("ACCELERANDO_RIGGED_ATTACK_SAMPLE_COMPLETE")
    print(json.dumps(pose_metrics, ensure_ascii=False))


if __name__ == "__main__":
    main()
