import hashlib
import json
from pathlib import Path

import bpy
import numpy as np


SAMPLE_ROOT = Path(__file__).resolve().parents[1]
UNITY_EXPORT_ROOT = SAMPLE_ROOT / "exports" / "unity"
TEXTURE_ROOT = UNITY_EXPORT_ROOT / "Textures"
MODEL_PATH = UNITY_EXPORT_ROOT / "Ostinato_ApprovedUnity.fbx"
MANIFEST_PATH = UNITY_EXPORT_ROOT / "unity_bake_manifest.json"
BAKE_SIZE = 2048
MARGIN = 24

MATERIALS = {
    "Ostinato_Chitin": "Chitin",
    "Ostinato_SoftTissue": "SoftTissue",
    "Ostinato_HookBlade": "HookBlade",
    "Ostinato_CompoundEye": "CompoundEye",
}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def require_sample_objects():
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.name.startswith("Ostinato_CurrentModel")]
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE" and obj.name.startswith("Ostinato_CurrentModel")]
    if len(meshes) != 1 or len(armatures) != 1:
        raise RuntimeError(f"Expected one approved mesh and armature, found meshes={len(meshes)}, armatures={len(armatures)}")
    return meshes[0], armatures[0]


def require_materials():
    result = {}
    for material_name, label in MATERIALS.items():
        material = bpy.data.materials.get(material_name)
        if material is None or not material.use_nodes:
            raise RuntimeError(f"Approved material is missing: {material_name}")
        principled = next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
        output = next((node for node in material.node_tree.nodes if node.type == "OUTPUT_MATERIAL" and node.is_active_output), None)
        if principled is None or output is None:
            raise RuntimeError(f"Approved material graph is incomplete: {material_name}")
        result[material_name] = (material, principled, output, label)
    return result


def create_target_image(material, label, channel, is_data):
    image_name = f"Ostinato_{label}_{channel}"
    image = bpy.data.images.get(image_name)
    if image is not None:
        bpy.data.images.remove(image)
    image = bpy.data.images.new(image_name, width=BAKE_SIZE, height=BAKE_SIZE, alpha=False, float_buffer=False)
    image.colorspace_settings.name = "Non-Color" if is_data else "sRGB"
    node = material.node_tree.nodes.new("ShaderNodeTexImage")
    node.name = f"UnityBakeTarget_{channel}"
    node.label = f"Unity Bake {channel}"
    node.image = image
    material.node_tree.nodes.active = node
    node.select = True
    return image, node


def socket_source(input_socket):
    return input_socket.links[0].from_socket if input_socket.is_linked else None


def connect_emission(material, output, source_socket, value):
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    for link in list(output.inputs["Surface"].links):
        links.remove(link)
    emission = nodes.new("ShaderNodeEmission")
    emission.name = "UnityBakeEmission"
    if source_socket is not None:
        links.new(source_socket, emission.inputs["Color"])
    else:
        scalar = float(value)
        emission.inputs["Color"].default_value = (scalar, scalar, scalar, 1.0)
    emission.inputs["Strength"].default_value = 1.0
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return emission


def restore_principled(material, principled, output, temporary_nodes):
    links = material.node_tree.links
    for link in list(output.inputs["Surface"].links):
        links.remove(link)
    links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    for node in temporary_nodes:
        material.node_tree.nodes.remove(node)


def save_images(targets, channel):
    saved = []
    for _, (_, image, target_node, label) in targets.items():
        path = TEXTURE_ROOT / f"Ostinato_{label}_{channel}.png"
        image.filepath_raw = str(path)
        image.file_format = "PNG"
        image.save()
        saved.append(path)
        target_node.select = False
    return saved


def bake_emission(materials, channel, input_name):
    targets = {}
    temporary = {}
    for material_name, (material, principled, output, label) in materials.items():
        image, target_node = create_target_image(material, label, channel, is_data=channel != "BaseColor")
        input_socket = principled.inputs[input_name]
        emission = connect_emission(material, output, socket_source(input_socket), input_socket.default_value)
        targets[material_name] = (material, image, target_node, label)
        temporary[material_name] = [emission]
    bpy.ops.object.bake(type="EMIT", use_clear=True, margin=MARGIN)
    saved = save_images(targets, channel)
    for material_name, (material, principled, output, _) in materials.items():
        restore_principled(material, principled, output, temporary[material_name])
    return saved


def bake_normal(materials):
    targets = {}
    for material_name, (material, _, _, label) in materials.items():
        image, target_node = create_target_image(material, label, "Normal", is_data=True)
        targets[material_name] = (material, image, target_node, label)
    bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", use_clear=True, margin=MARGIN)
    return save_images(targets, "Normal")


def pack_metallic_smoothness():
    packed_paths = []
    for label in MATERIALS.values():
        metallic_path = TEXTURE_ROOT / f"Ostinato_{label}_Metallic.png"
        roughness_path = TEXTURE_ROOT / f"Ostinato_{label}_Roughness.png"
        metallic_image = bpy.data.images.load(str(metallic_path), check_existing=False)
        roughness_image = bpy.data.images.load(str(roughness_path), check_existing=False)
        pixel_count = BAKE_SIZE * BAKE_SIZE
        metallic_pixels = np.empty(pixel_count * 4, dtype=np.float32)
        roughness_pixels = np.empty(pixel_count * 4, dtype=np.float32)
        metallic_image.pixels.foreach_get(metallic_pixels)
        roughness_image.pixels.foreach_get(roughness_pixels)
        metallic_pixels = metallic_pixels.reshape(pixel_count, 4)
        roughness_pixels = roughness_pixels.reshape(pixel_count, 4)

        packed_pixels = np.zeros((pixel_count, 4), dtype=np.float32)
        packed_pixels[:, 0] = metallic_pixels[:, 0]
        packed_pixels[:, 1] = metallic_pixels[:, 0]
        packed_pixels[:, 2] = metallic_pixels[:, 0]
        packed_pixels[:, 3] = 1.0 - roughness_pixels[:, 0]

        packed_image = bpy.data.images.new(
            f"Ostinato_{label}_MetallicSmoothness",
            width=BAKE_SIZE,
            height=BAKE_SIZE,
            alpha=True,
            float_buffer=False,
        )
        packed_image.colorspace_settings.name = "Non-Color"
        packed_image.pixels.foreach_set(packed_pixels.ravel())
        packed_path = TEXTURE_ROOT / f"Ostinato_{label}_MetallicSmoothness.png"
        packed_image.filepath_raw = str(packed_path)
        packed_image.file_format = "PNG"
        packed_image.save()
        packed_paths.append(packed_path)
        bpy.data.images.remove(metallic_image)
        bpy.data.images.remove(roughness_image)
    return packed_paths


def export_model(mesh_object, armature_object):
    sample_uv = mesh_object.data.uv_layers.get("OstinatoSampleUV")
    if sample_uv is None:
        raise RuntimeError("Approved OstinatoSampleUV is missing before Unity export.")
    for uv_layer in list(mesh_object.data.uv_layers):
        if uv_layer != sample_uv:
            mesh_object.data.uv_layers.remove(uv_layer)
    mesh_object.data.uv_layers.active = sample_uv
    sample_uv.active_render = True

    bpy.ops.object.select_all(action="DESELECT")
    mesh_object.select_set(True)
    armature_object.select_set(True)
    bpy.context.view_layer.objects.active = mesh_object
    bpy.ops.export_scene.fbx(
        filepath=str(MODEL_PATH),
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="RELATIVE",
        use_armature_deform_only=True,
    )


def main():
    UNITY_EXPORT_ROOT.mkdir(parents=True, exist_ok=True)
    TEXTURE_ROOT.mkdir(parents=True, exist_ok=True)

    mesh_object, armature_object = require_sample_objects()
    uv_layer = mesh_object.data.uv_layers.get("OstinatoSampleUV")
    if uv_layer is None:
        raise RuntimeError("Approved OstinatoSampleUV is missing.")
    mesh_object.data.uv_layers.active = uv_layer
    uv_layer.active_render = True

    vertex_count = len(mesh_object.data.vertices)
    polygon_count = len(mesh_object.data.polygons)
    bone_names = [bone.name for bone in armature_object.data.bones]
    materials = require_materials()

    bpy.context.scene.render.engine = "CYCLES"
    bpy.ops.object.select_all(action="DESELECT")
    mesh_object.select_set(True)
    bpy.context.view_layer.objects.active = mesh_object

    texture_paths = []
    texture_paths.extend(bake_emission(materials, "BaseColor", "Base Color"))
    texture_paths.extend(bake_emission(materials, "Roughness", "Roughness"))
    texture_paths.extend(bake_emission(materials, "Metallic", "Metallic"))
    texture_paths.extend(bake_normal(materials))
    texture_paths.extend(pack_metallic_smoothness())
    export_model(mesh_object, armature_object)

    if len(mesh_object.data.vertices) != vertex_count or len(mesh_object.data.polygons) != polygon_count:
        raise RuntimeError("Approved sample geometry changed during Unity bake.")
    if [bone.name for bone in armature_object.data.bones] != bone_names:
        raise RuntimeError("Approved sample rig changed during Unity bake.")

    manifest = {
        "source_blend": "artSample/enemies/ostinato/blender/Ostinato_CurrentModel_TexturedSample.blend",
        "model": str(MODEL_PATH.relative_to(SAMPLE_ROOT)).replace("\\", "/"),
        "model_sha256": sha256(MODEL_PATH),
        "bake_size": BAKE_SIZE,
        "bake_margin": MARGIN,
        "uv_layer": "OstinatoSampleUV",
        "exported_uv_layers": [layer.name for layer in mesh_object.data.uv_layers],
        "vertices_blender": vertex_count,
        "polygons": polygon_count,
        "bones": bone_names,
        "materials": list(MATERIALS.keys()),
        "textures": [str(path.relative_to(SAMPLE_ROOT)).replace("\\", "/") for path in texture_paths],
        "geometry_changed": False,
        "rig_changed": False,
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"UnityBakeModel={MODEL_PATH}")
    print(f"UnityBakeTextures={len(texture_paths)}")
    print(f"UnityBakeModelSha256={manifest['model_sha256']}")
    print("GeometryChanged=False")
    print("RigChanged=False")


if __name__ == "__main__":
    main()
