import argparse
import os
import shutil

import bpy
from mathutils import Vector


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--custom-reference", required=True)
    parser.add_argument("--mixamo-reference", required=True)
    parser.add_argument("--death-reference", required=True)
    parser.add_argument("--output-folder", required=True)
    return parser.parse_args(__import__("sys").argv[__import__("sys").argv.index("--") + 1:])


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def bounds(points):
    minimum = Vector((
        min(point.x for point in points),
        min(point.y for point in points),
        min(point.z for point in points),
    ))
    maximum = Vector((
        max(point.x for point in points),
        max(point.y for point in points),
        max(point.z for point in points),
    ))
    return minimum, maximum


def object_points_in_space(obj, target_inverse):
    return [target_inverse @ obj.matrix_world @ vertex.co for vertex in obj.data.vertices]


def require_single(kind):
    values = [obj for obj in bpy.context.scene.objects if obj.type == kind]
    if len(values) != 1:
        raise RuntimeError(f"Expected one {kind}, got {len(values)}")
    return values[0]


def require_named_mesh(name):
    values = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.name == name]
    if len(values) != 1:
        raise RuntimeError(f"Expected one mesh named {name}, got {len(values)}")
    return values[0]


def import_source(source_path):
    names_before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=source_path)
    imported = [obj for obj in bpy.context.scene.objects if obj not in names_before]
    meshes = [obj for obj in imported if obj.type == "MESH"]
    if len(meshes) != 1:
        raise RuntimeError(f"Expected the replacement FBX to contain one mesh, got {len(meshes)}")
    return meshes[0]


def extract_source_textures(source_path, output_folder):
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=source_path)
    os.makedirs(output_folder, exist_ok=True)
    outputs = {
        "base_color": "Ispant_New_BaseColor.jpg",
        "normal": "Ispant_New_Normal.jpg",
        "texture_0_metallic.png": "Ispant_New_Metallic.png",
        "texture_0_roughness.png": "Ispant_New_Roughness.png",
    }
    for image_name, file_name in outputs.items():
        image = bpy.data.images.get(image_name)
        if image is None or image.packed_file is None:
            raise RuntimeError(f"Packed source texture is missing: {image_name}")
        output_path = os.path.join(output_folder, file_name)
        with open(output_path, "wb") as output:
            output.write(image.packed_file.data)
        print(f"Extracted {image_name}: bytes={image.packed_file.size}, output={output_path}")


def target_bounds(reference_body):
    target_inverse = reference_body.matrix_world.inverted()
    appearance = [
        obj for obj in bpy.context.scene.objects
        if obj.type == "MESH" and obj.name in {"Ispant_Armed_Body", "Ispant_Crescent_Ornament"}
    ]
    if len(appearance) != 2:
        raise RuntimeError(f"Expected body and crescent reference meshes, got {len(appearance)}")
    points = [point for obj in appearance for point in object_points_in_space(obj, target_inverse)]
    return bounds(points)


def aligned_mesh(source, reference_body, output_name):
    source_data = source.data.copy()
    source_data.name = output_name + "_Mesh"
    result = bpy.data.objects.new(output_name, source_data)
    bpy.context.scene.collection.objects.link(result)
    result.parent = reference_body.parent
    result.matrix_parent_inverse = reference_body.matrix_parent_inverse.copy()
    result.matrix_basis = reference_body.matrix_basis.copy()

    source_points = [Vector((vertex.co.x, vertex.co.z, -vertex.co.y)) for vertex in source.data.vertices]
    source_min, source_max = bounds(source_points)
    target_min, target_max = target_bounds(reference_body)
    source_height = source_max.y - source_min.y
    target_height = target_max.y - target_min.y
    if source_height <= 0.0 or target_height <= 0.0:
        raise RuntimeError("Source or reference height is invalid")
    scale = target_height / source_height
    source_center = (source_min + source_max) * 0.5
    target_center = (target_min + target_max) * 0.5
    for vertex, oriented in zip(result.data.vertices, source_points):
        relative = oriented - source_center
        vertex.co = Vector((
            target_center.x + relative.x * scale,
            target_min.y + (oriented.y - source_min.y) * scale,
            target_center.z + relative.z * scale,
        ))
    return result, scale


def transfer_weights(reference_body, result, armature):
    for group in reference_body.vertex_groups:
        result.vertex_groups.new(name=group.name)
    modifier = result.modifiers.new("TransferExistingIspantRig", "DATA_TRANSFER")
    modifier.object = reference_body
    modifier.use_vert_data = True
    modifier.data_types_verts = {"VGROUP_WEIGHTS"}
    modifier.vert_mapping = "POLYINTERP_NEAREST"
    modifier.layers_vgroup_select_src = "ALL"
    modifier.layers_vgroup_select_dst = "NAME"
    bpy.context.view_layer.objects.active = result
    result.select_set(True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)

    armature_modifier = result.modifiers.new("ExistingIspantRig", "ARMATURE")
    armature_modifier.object = armature
    result.parent = armature

    unweighted = 0
    for vertex in result.data.vertices:
        if not vertex.groups:
            unweighted += 1
            result.vertex_groups[0].add([vertex.index], 1.0, "REPLACE")
    return unweighted


def collapse_mixamo_finger_weights(result):
    for side in ("Left", "Right"):
        hand_name = f"mixamorig:{side}Hand"
        prefix = hand_name + "Index"
        hand_group = result.vertex_groups.get(hand_name)
        if hand_group is None:
            hand_group = result.vertex_groups.new(name=hand_name)
        finger_groups = [group for group in result.vertex_groups if group.name.startswith(prefix)]
        finger_indices = {group.index for group in finger_groups}
        for vertex in result.data.vertices:
            moved_weight = sum(
                assignment.weight for assignment in vertex.groups
                if assignment.group in finger_indices
            )
            if moved_weight > 0.0:
                hand_group.add([vertex.index], moved_weight, "ADD")
        for group in finger_groups:
            result.vertex_groups.remove(group)
    bpy.context.view_layer.objects.active = result
    result.select_set(True)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)


def export_variant(source_path, reference_path, output_path, output_name, unit_correction=1.0):
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=reference_path)
    armature = require_single("ARMATURE")
    reference_body = require_named_mesh("Ispant_Armed_Body")
    source = import_source(source_path)
    result, scale = aligned_mesh(source, reference_body, output_name)
    unweighted = transfer_weights(reference_body, result, armature)
    if any(group.name.startswith("mixamorig:") for group in result.vertex_groups):
        collapse_mixamo_finger_weights(result)
    if unit_correction != 1.0:
        for vertex in result.data.vertices:
            vertex.co *= unit_correction

    if armature.animation_data is not None:
        armature.animation_data_clear()
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)

    for obj in list(bpy.context.scene.objects):
        if obj not in {armature, result}:
            bpy.data.objects.remove(obj, do_unlink=True)
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    result.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        add_leaf_bones=False,
        use_armature_deform_only=True,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
        axis_forward="-Z",
        axis_up="Y",
    )
    print(
        f"Exported {output_name}: vertices={len(result.data.vertices)}, "
        f"polygons={len(result.data.polygons)}, groups={len(result.vertex_groups)}, "
        f"scale={scale:.8f}, unweighted={unweighted}, output={output_path}"
    )


def main():
    args = parse_args()
    os.makedirs(args.output_folder, exist_ok=True)
    source_copy = os.path.join(args.output_folder, "Ispant_New_Source.fbx")
    shutil.copy2(args.source, source_copy)
    extract_source_textures(args.source, os.path.join(args.output_folder, "Textures"))
    export_variant(
        args.source,
        args.custom_reference,
        os.path.join(args.output_folder, "Ispant_New_CustomRig.fbx"),
        "Ispant_New_Body_Custom",
        0.01,
    )
    export_variant(
        args.source,
        args.mixamo_reference,
        os.path.join(args.output_folder, "Ispant_New_MixamoRig.fbx"),
        "Ispant_New_Body_Mixamo",
    )
    export_variant(
        args.source,
        args.death_reference,
        os.path.join(args.output_folder, "Ispant_New_DeathRig.fbx"),
        "Ispant_New_Body_Death",
        0.01,
    )
    print("Copied replacement source to " + source_copy)


if __name__ == "__main__":
    main()
