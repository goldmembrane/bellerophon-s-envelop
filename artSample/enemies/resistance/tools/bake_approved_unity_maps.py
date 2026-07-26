from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[1]
TEXTURE_DIR = ROOT / "textures"
SIZE = 2048


mesh_objects = [
    item
    for item in bpy.data.objects
    if item.type == "MESH" and item.name == "char1"
]
if len(mesh_objects) != 1:
    raise RuntimeError(
        f"Expected one char1 mesh, found {len(mesh_objects)}."
    )

mesh_object = mesh_objects[0]
if not mesh_object.data.uv_layers:
    raise RuntimeError("Approved Resistance mesh has no UV map.")

materials = [
    slot.material
    for slot in mesh_object.material_slots
    if slot.material is not None
]
if not materials:
    raise RuntimeError("Approved Resistance mesh has no materials.")

for item in bpy.context.selected_objects:
    item.select_set(False)
mesh_object.hide_render = False
mesh_object.hide_set(False)
mesh_object.select_set(True)
bpy.context.view_layer.objects.active = mesh_object
for item in bpy.context.scene.objects:
    if item != mesh_object and item.type == "MESH":
        item.hide_render = True

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.render.bake.margin = 8
scene.render.bake.use_clear = True
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGB"
scene.render.image_settings.color_depth = "8"
TEXTURE_DIR.mkdir(parents=True, exist_ok=True)


def create_target(name, filename, is_data):
    image = bpy.data.images.new(
        name,
        width=SIZE,
        height=SIZE,
        alpha=False,
        float_buffer=False,
        is_data=is_data,
    )
    image.file_format = "PNG"
    image.filepath_raw = str(TEXTURE_DIR / filename)
    for material in materials:
        if not material.use_nodes:
            raise RuntimeError(
                f"Approved material {material.name} has no nodes."
            )
        nodes = material.node_tree.nodes
        image_node = nodes.new("ShaderNodeTexImage")
        image_node.name = name + "_" + material.name
        image_node.image = image
        nodes.active = image_node
    return image


albedo = create_target(
    "ResistanceApprovedUnityAlbedo",
    "resistance_approved_unity_albedo.png",
    False,
)
bpy.ops.object.bake(
    type="DIFFUSE",
    pass_filter={"COLOR"},
    use_clear=True,
    margin=8,
)
albedo.save()

emission = create_target(
    "ResistanceApprovedUnityEmission",
    "resistance_approved_unity_emission.png",
    False,
)
bpy.ops.object.bake(
    type="EMIT",
    pass_filter={"EMIT"},
    use_clear=True,
    margin=8,
)
emission.save()

print(
    "Approved Resistance Unity maps baked: "
    + str(albedo.filepath_raw)
    + ", "
    + str(emission.filepath_raw)
)
