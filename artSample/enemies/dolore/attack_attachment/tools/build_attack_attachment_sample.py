import hashlib
import json
import math
import shutil
import struct
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample" / "enemies" / "dolore" / "attack_attachment"
BASE_BLEND = ROOT / "artSample" / "enemies" / "dolore" / "blender" / "Dolore_CurrentModel_ReferenceSync.blend"
SOURCE_ATTACK = ROOT / "enemies model" / "dolore attack.glb"
REFERENCE_COMPOSITE = ROOT / "image" / "dolore-attack.png"
REFERENCE_ATTACK = ROOT / "image" / "dolore attack model.png"
FLESH_TEXTURE_ROOT = ROOT / "artSample" / "enemies" / "dolore" / "textures"
BLEND_PATH = SAMPLE_ROOT / "blender" / "Dolore_AttackAttachment_Sample.blend"
EXPORT_PATH = SAMPLE_ROOT / "exports" / "Dolore_AttackAttachment_Sample.glb"
RENDER_ROOT = SAMPLE_ROOT / "renders"
MANIFEST_PATH = SAMPLE_ROOT / "ASSET_MANIFEST.json"

BASE_VERTEX_SIGNATURE = "6481749BF97CE0AE570E41FB07604232FDC41CD6072DA2ED94D5E595EF1268C1"
BASE_TOPOLOGY_SIGNATURE = "D68776656C4534ACCB8FBA2041C71066A0CC0FA7270E390B2F42384CB5A124C7"
ATTACHMENT_SCALE = 0.0015
ROOT_EXIT_BONE_NAME = "Bone_000"
ROOT_EXIT_COMPENSATION_BONE_NAME = "Bone_012"
FRAME_FRONT_DIRECTION = Vector((0.0, -1.0, 0.0))


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def mesh_signature(mesh):
    vertex_digest = hashlib.sha256()
    topology_digest = hashlib.sha256()
    for vertex in mesh.vertices:
        vertex_digest.update(struct.pack("<3d", *vertex.co))
    for polygon in mesh.polygons:
        topology_digest.update(struct.pack("<I", len(polygon.vertices)))
        for index in polygon.vertices:
            topology_digest.update(struct.pack("<I", index))
    return {
        "vertices": len(mesh.vertices),
        "edges": len(mesh.edges),
        "polygons": len(mesh.polygons),
        "vertex_position_sha256": vertex_digest.hexdigest().upper(),
        "polygon_topology_sha256": topology_digest.hexdigest().upper(),
    }


def bounds_world(objects):
    points = []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        evaluated = obj.evaluated_get(depsgraph)
        if obj.type == "MESH":
            mesh = evaluated.to_mesh()
            points.extend(evaluated.matrix_world @ vertex.co for vertex in mesh.vertices)
            evaluated.to_mesh_clear()
        else:
            points.extend(evaluated.matrix_world @ Vector(corner) for corner in obj.bound_box)
    if not points:
        raise RuntimeError("No points were available for bounds calculation.")
    minimum = Vector((min(point.x for point in points), min(point.y for point in points), min(point.z for point in points)))
    maximum = Vector((max(point.x for point in points), max(point.y for point in points), max(point.z for point in points)))
    return minimum, maximum


def material_bounds(mesh_object, material_name):
    mesh = mesh_object.data
    material_index = next(index for index, item in enumerate(mesh.materials) if item.name == material_name)
    indices = {vertex_index for polygon in mesh.polygons if polygon.material_index == material_index for vertex_index in polygon.vertices}
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh_object.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh()
    points = [evaluated.matrix_world @ evaluated_mesh.vertices[index].co for index in indices]
    minimum = Vector((min(point.x for point in points), min(point.y for point in points), min(point.z for point in points)))
    maximum = Vector((max(point.x for point in points), max(point.y for point in points), max(point.z for point in points)))
    evaluated.to_mesh_clear()
    return minimum, maximum


def load_image(path, non_color=False):
    image = bpy.data.images.load(str(path), check_existing=True)
    image.colorspace_settings.name = "Non-Color" if non_color else "sRGB"
    return image


def create_flesh_material():
    material = bpy.data.materials.get("Dolore_Attack_Flesh") or bpy.data.materials.new("Dolore_Attack_Flesh")
    material.use_nodes = True
    material.diffuse_color = (0.32, 0.018, 0.014, 1.0)
    material.metallic = 0.0
    material.roughness = 0.27
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.inputs["Base Color"].default_value = (0.32, 0.018, 0.014, 1.0)
    shader.inputs["Roughness"].default_value = 0.38
    shader.inputs["Coat Weight"].default_value = 0.16
    shader.inputs["Coat Roughness"].default_value = 0.24
    material.node_tree.links.new(shader.outputs["BSDF"], output.inputs["Surface"])

    albedo = nodes.new("ShaderNodeTexImage")
    albedo.name = "Approved_Flesh_Albedo"
    albedo.image = load_image(FLESH_TEXTURE_ROOT / "dolore_flesh_albedo.png")
    albedo_multiply = nodes.new("ShaderNodeMixRGB")
    albedo_multiply.blend_type = "MULTIPLY"
    albedo_multiply.inputs[0].default_value = 1.0
    albedo_multiply.inputs[2].default_value = (0.42, 0.16, 0.13, 1.0)
    material.node_tree.links.new(albedo.outputs["Color"], albedo_multiply.inputs[1])
    material.node_tree.links.new(albedo_multiply.outputs["Color"], shader.inputs["Base Color"])

    roughness = nodes.new("ShaderNodeTexImage")
    roughness.name = "Approved_Flesh_Roughness"
    roughness.image = load_image(FLESH_TEXTURE_ROOT / "dolore_flesh_roughness.png", non_color=True)
    roughness_multiply = nodes.new("ShaderNodeMath")
    roughness_multiply.operation = "MULTIPLY"
    roughness_multiply.inputs[1].default_value = 0.35
    roughness_add = nodes.new("ShaderNodeMath")
    roughness_add.operation = "ADD"
    roughness_add.inputs[1].default_value = 0.25
    material.node_tree.links.new(roughness.outputs["Color"], roughness_multiply.inputs[0])
    material.node_tree.links.new(roughness_multiply.outputs[0], roughness_add.inputs[0])
    material.node_tree.links.new(roughness_add.outputs[0], shader.inputs["Roughness"])

    height = nodes.new("ShaderNodeTexImage")
    height.name = "Approved_Flesh_Height"
    height.image = load_image(FLESH_TEXTURE_ROOT / "dolore_flesh_height.png", non_color=True)
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.22
    bump.inputs["Distance"].default_value = 0.08
    material.node_tree.links.new(height.outputs["Color"], bump.inputs["Height"])
    material.node_tree.links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    return material


def unwrap_attack_mesh(mesh_object):
    mesh = mesh_object.data
    while mesh.uv_layers:
        mesh.uv_layers.remove(mesh.uv_layers[0])
    uv_layer = mesh.uv_layers.new(name="Dolore_Attack_ContinuousUV")
    minimum = Vector((min(vertex.co.x for vertex in mesh.vertices),
                      min(vertex.co.y for vertex in mesh.vertices),
                      min(vertex.co.z for vertex in mesh.vertices)))
    maximum = Vector((max(vertex.co.x for vertex in mesh.vertices),
                      max(vertex.co.y for vertex in mesh.vertices),
                      max(vertex.co.z for vertex in mesh.vertices)))
    size = maximum - minimum
    for loop in mesh.loops:
        point = mesh.vertices[loop.vertex_index].co
        uv_layer.data[loop.index].uv = (
            (point.x - minimum.x) / size.x,
            (point.z - minimum.z) / size.z,
        )


def pose_root_exit_forward(armature):
    root_bone = armature.data.bones[ROOT_EXIT_BONE_NAME]
    compensation_bone = armature.data.bones[ROOT_EXIT_COMPENSATION_BONE_NAME]
    root_pose = armature.pose.bones[ROOT_EXIT_BONE_NAME]
    compensation_pose = armature.pose.bones[ROOT_EXIT_COMPENSATION_BONE_NAME]

    rest_direction = (root_bone.tail_local - root_bone.head_local).normalized()
    rotation = rest_direction.rotation_difference(FRAME_FRONT_DIRECTION)
    pivot = Matrix.Translation(root_bone.head_local)
    root_pose.matrix = pivot @ rotation.to_matrix().to_4x4() @ pivot.inverted() @ root_bone.matrix_local
    bpy.context.view_layer.update()

    # 첫 본의 정면 돌출만 남기고, 두 번째 본부터는 제공 GLB의 기존 곡선을 유지합니다.
    compensation_pose.matrix = compensation_bone.matrix_local.copy()
    bpy.context.view_layer.update()


def pose_bone_world_direction(armature, bone_name):
    pose_bone = armature.pose.bones[bone_name]
    direction = armature.matrix_world.to_3x3() @ (pose_bone.tail - pose_bone.head)
    return direction.normalized()


def apply_attack_pose_as_rest(armature):
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.update()


def import_attack():
    objects_before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE_ATTACK))
    imported = [item for item in bpy.data.objects if item not in objects_before]
    mesh_object = max((item for item in imported if item.type == "MESH"), key=lambda item: len(item.data.vertices))
    armature = next((modifier.object for modifier in mesh_object.modifiers if modifier.type == "ARMATURE"), None)
    if armature is None:
        raise RuntimeError("The supplied attack GLB mesh does not retain its armature.")
    for item in imported:
        if item not in {mesh_object, armature}:
            bpy.data.objects.remove(item, do_unlink=True)

    mesh_object.name = "Dolore_Attack_Tentacle"
    mesh_object.data.name = "Dolore_Attack_Tentacle_Mesh"
    armature.name = "Dolore_Attack_Rig"
    source_signature = mesh_signature(mesh_object.data)
    unwrap_attack_mesh(mesh_object)
    mesh_object.data.materials.clear()
    mesh_object.data.materials.append(create_flesh_material())
    for polygon in mesh_object.data.polygons:
        polygon.material_index = 0
        polygon.use_smooth = True

    anchor = bpy.data.objects.new("Dolore_Attack_Attachment", None)
    bpy.context.scene.collection.objects.link(anchor)
    armature.parent = anchor
    armature.matrix_parent_inverse = anchor.matrix_world.inverted()
    pose_root_exit_forward(armature)
    apply_attack_pose_as_rest(armature)

    base_model = bpy.data.objects["Dolore_CurrentModel"]
    portrait_minimum, portrait_maximum = material_bounds(base_model, "Dolore_Faded_Portrait")
    portrait_center = (portrait_minimum + portrait_maximum) * 0.5
    local_minimum, local_maximum = bounds_world([mesh_object])
    root_threshold = local_minimum.x + (local_maximum.x - local_minimum.x) * 0.18
    root_indices = [vertex.index for vertex in mesh_object.data.vertices if vertex.co.x <= root_threshold]
    anchor.scale = (ATTACHMENT_SCALE,) * 3
    anchor.rotation_euler = (0.0, math.radians(-15.0), 0.0)
    desired_root = Vector((
        portrait_center.x - (portrait_maximum.x - portrait_minimum.x) * 0.06,
        portrait_minimum.y - 0.00018,
        portrait_center.z - (portrait_maximum.z - portrait_minimum.z) * 0.08,
    ))
    anchor.location = Vector()
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh_object.evaluated_get(depsgraph)
    evaluated_mesh = evaluated.to_mesh()
    root_world = sum((evaluated.matrix_world @ evaluated_mesh.vertices[index].co for index in root_indices), Vector()) / len(root_indices)
    evaluated.to_mesh_clear()
    anchor.location += desired_root - root_world
    bpy.context.view_layer.update()
    return mesh_object, armature, anchor, source_signature


def look_at(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def configure_render():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.world.color = (0.025, 0.032, 0.031)


def render_view(camera, name, location, target, lens=60.0):
    camera.location = location
    camera.data.lens = lens
    look_at(camera, target)
    bpy.context.scene.render.filepath = str(RENDER_ROOT / f"{name}.png")
    bpy.ops.render.render(write_still=True)


def render_reviews(base_model, attack_mesh):
    configure_render()
    camera = bpy.data.objects.get("Dolore_Review_Camera")
    if camera is None:
        camera_data = bpy.data.cameras.new("Dolore_Attack_Review_Camera")
        camera = bpy.data.objects.new("Dolore_Attack_Review_Camera", camera_data)
        bpy.context.scene.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    minimum, maximum = bounds_world([base_model, attack_mesh])
    center = (minimum + maximum) * 0.5
    size = maximum - minimum
    distance = max(size.x, size.y, size.z) * 3.45
    render_view(camera, "01_front_attached", center + Vector((0.0, -distance, size.z * 0.10)), center, 62.0)
    render_view(camera, "02_three_quarter_attached", center + Vector((distance * 0.56, -distance, size.z * 0.22)), center, 60.0)
    render_view(camera, "03_side_attachment", center + Vector((distance, 0.0, size.z * 0.12)), center, 62.0)

    portrait_minimum, portrait_maximum = material_bounds(base_model, "Dolore_Faded_Portrait")
    close_target = (portrait_minimum + portrait_maximum) * 0.5 + Vector((0.0016, -0.0003, -0.0002))
    close_distance = max((portrait_maximum - portrait_minimum).x, (portrait_maximum - portrait_minimum).z) * 2.15
    render_view(camera, "04_attachment_closeup", close_target + Vector((0.0, -close_distance, 0.0004)), close_target, 72.0)


def save_side_by_side(left_path, right_path, output_path):
    width = 1024
    height = 768
    left = bpy.data.images.load(str(left_path), check_existing=False)
    right = bpy.data.images.load(str(right_path), check_existing=False)
    left.scale(width, height)
    right.scale(width, height)
    left_pixels = np.empty(width * height * 4, dtype=np.float32)
    right_pixels = np.empty(width * height * 4, dtype=np.float32)
    left.pixels.foreach_get(left_pixels)
    right.pixels.foreach_get(right_pixels)
    canvas = np.zeros((height, width * 2, 4), dtype=np.float32)
    canvas[:, :width, :] = left_pixels.reshape((height, width, 4))
    canvas[:, width:, :] = right_pixels.reshape((height, width, 4))
    comparison = bpy.data.images.new("Dolore_Attack_Reference_Comparison", width=width * 2, height=height, alpha=True)
    comparison.pixels.foreach_set(canvas.ravel())
    comparison.file_format = "PNG"
    comparison.filepath_raw = str(output_path)
    comparison.save()


def export_sample(base_model, base_rig, attack_mesh, attack_rig, anchor):
    stage_objects = [item for item in bpy.context.scene.objects if item not in {base_model, base_rig, attack_mesh, attack_rig, anchor}]
    hidden = [(item, item.hide_render) for item in stage_objects]
    for item, _ in hidden:
        item.hide_render = True
    bpy.ops.object.select_all(action="DESELECT")
    for item in (base_model, base_rig, attack_mesh, attack_rig, anchor):
        item.select_set(True)
    bpy.context.view_layer.objects.active = anchor
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_PATH),
        export_format="GLB",
        use_selection=True,
        export_animations=False,
        export_skins=True,
        export_yup=True,
    )
    for item, was_hidden in hidden:
        item.hide_render = was_hidden


def main():
    for path in (SAMPLE_ROOT / "blender", SAMPLE_ROOT / "exports", RENDER_ROOT,
                 SAMPLE_ROOT / "reference", SAMPLE_ROOT / "source"):
        path.mkdir(parents=True, exist_ok=True)
    shutil.copy2(REFERENCE_COMPOSITE, SAMPLE_ROOT / "reference" / "dolore_attack_composite.png")
    shutil.copy2(REFERENCE_ATTACK, SAMPLE_ROOT / "reference" / "dolore_attack_isolated.png")
    shutil.copy2(SOURCE_ATTACK, SAMPLE_ROOT / "source" / "dolore_attack_source.glb")

    bpy.ops.wm.open_mainfile(filepath=str(BASE_BLEND))
    base_model = bpy.data.objects["Dolore_CurrentModel"]
    base_rig = bpy.data.objects["Dolore_Rig"]
    base_before = mesh_signature(base_model.data)
    attack_mesh, attack_rig, anchor, attack_before = import_attack()
    attack_after = mesh_signature(attack_mesh.data)
    base_after = mesh_signature(base_model.data)
    if base_before != base_after:
        raise RuntimeError("Approved Dolore base geometry changed while building the attachment sample.")
    if base_after["vertex_position_sha256"] != BASE_VERTEX_SIGNATURE or base_after["polygon_topology_sha256"] != BASE_TOPOLOGY_SIGNATURE:
        raise RuntimeError("Approved Dolore base geometry no longer matches its approved signature.")
    if attack_before != attack_after:
        raise RuntimeError("Supplied attack geometry changed while preparing its sample material.")

    render_reviews(base_model, attack_mesh)
    save_side_by_side(REFERENCE_COMPOSITE, RENDER_ROOT / "01_front_attached.png",
                      RENDER_ROOT / "05_reference_comparison.png")
    export_sample(base_model, base_rig, attack_mesh, attack_rig, anchor)
    for image in list(bpy.data.images):
        if image.source != "FILE" or image.packed_file is not None or not image.filepath:
            continue
        absolute_path = Path(bpy.path.abspath(image.filepath))
        if not absolute_path.exists():
            bpy.data.images.remove(image)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    attack_minimum, attack_maximum = bounds_world([attack_mesh])
    portrait_minimum, portrait_maximum = material_bounds(base_model, "Dolore_Faded_Portrait")
    root_exit_direction = pose_bone_world_direction(attack_rig, ROOT_EXIT_BONE_NAME)
    downstream_direction = pose_bone_world_direction(attack_rig, ROOT_EXIT_COMPENSATION_BONE_NAME)
    downstream_rest_direction = (
        attack_rig.matrix_world.to_3x3() @
        (attack_rig.data.bones[ROOT_EXIT_COMPENSATION_BONE_NAME].tail_local -
         attack_rig.data.bones[ROOT_EXIT_COMPENSATION_BONE_NAME].head_local)
    ).normalized()
    manifest = {
        "sample": "Dolore attack attachment art sample",
        "status": "awaiting_user_approval",
        "source_attack_glb": str(SOURCE_ATTACK),
        "source_attack_sha256": sha256(SOURCE_ATTACK),
        "placement_reference": str(REFERENCE_COMPOSITE),
        "isolated_reference": str(REFERENCE_ATTACK),
        "base_blend": str(BASE_BLEND),
        "base_geometry_before": base_before,
        "base_geometry_after": base_after,
        "base_geometry_preserved": base_before == base_after,
        "attack_geometry_before": attack_before,
        "attack_geometry_after": attack_after,
        "attack_geometry_preserved": attack_before == attack_after,
        "attack_rig_bones": len(attack_rig.data.bones),
        "attachment_parent": anchor.name,
        "attachment_location": [round(value, 9) for value in anchor.location],
        "attachment_scale": [round(value, 9) for value in anchor.scale],
        "attachment_rotation_euler": [round(value, 9) for value in anchor.rotation_euler],
        "root_exit_bone": ROOT_EXIT_BONE_NAME,
        "root_exit_direction_world": [round(value, 9) for value in root_exit_direction],
        "root_exit_front_direction_world": [round(value, 9) for value in FRAME_FRONT_DIRECTION],
        "root_exit_front_alignment": round(root_exit_direction.dot(FRAME_FRONT_DIRECTION), 9),
        "root_exit_pose_applied_as_rest": True,
        "downstream_curve_bone": ROOT_EXIT_COMPENSATION_BONE_NAME,
        "downstream_curve_rest_alignment": round(downstream_direction.dot(downstream_rest_direction), 9),
        "attack_world_bounds": {
            "min": [round(value, 9) for value in attack_minimum],
            "max": [round(value, 9) for value in attack_maximum],
        },
        "portrait_world_bounds": {
            "min": [round(value, 9) for value in portrait_minimum],
            "max": [round(value, 9) for value in portrait_maximum],
        },
        "attack_material": "Dolore_Attack_Flesh",
        "attack_texture_source": "existing approved Dolore flesh texture set",
        "unity_applied": False,
        "animation_applied": False,
        "review_files": [
            "renders/01_front_attached.png",
            "renders/02_three_quarter_attached.png",
            "renders/03_side_attachment.png",
            "renders/04_attachment_closeup.png",
            "renders/05_reference_comparison.png",
        ],
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print("DOLORE_ATTACK_ATTACHMENT_BUILD=PASS")


if __name__ == "__main__":
    main()
