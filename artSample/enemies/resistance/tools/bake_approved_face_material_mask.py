from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "textures" / "resistance_approved_face_material_mask.png"
SIZE = 2048

MATERIAL_VALUES = {
    "M_Resistance_Worn_Silver": 0.0,
    "M_Resistance_Dark_Mechanics": 0.25,
    "M_Resistance_Bronze_Accents": 0.5,
    "M_Resistance_Bandana_Olive": 0.75,
}


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
mesh = mesh_object.data
if not mesh.uv_layers or mesh.uv_layers.active is None:
    raise RuntimeError("Approved Resistance mesh has no active UV map.")

source_material_names = [
    slot.material.name if slot.material else ""
    for slot in mesh_object.material_slots
]
source_face_materials = [
    source_material_names[polygon.material_index]
    for polygon in mesh.polygons
]
unexpected = sorted(
    set(source_face_materials) - set(MATERIAL_VALUES)
)
if unexpected:
    raise RuntimeError(
        "Unexpected approved Resistance face materials: "
        + ", ".join(unexpected)
    )

image = bpy.data.images.new(
    "ResistanceApprovedFaceMaterialMask",
    width=SIZE,
    height=SIZE,
    alpha=False,
    float_buffer=False,
    is_data=True,
)
image.file_format = "PNG"
image.filepath_raw = str(OUTPUT)
generated_materials = {}
for material_name, value in MATERIAL_VALUES.items():
    material = bpy.data.materials.new(
        "Bake_" + material_name
    )
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (
        value,
        value,
        value,
        1.0,
    )
    emission.inputs["Strength"].default_value = 1.0
    image_node = nodes.new("ShaderNodeTexImage")
    image_node.image = image
    nodes.active = image_node
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    generated_materials[material_name] = material

mesh.materials.clear()
ordered_names = list(MATERIAL_VALUES)
for material_name in ordered_names:
    mesh.materials.append(generated_materials[material_name])
material_indices = {
    material_name: index
    for index, material_name in enumerate(ordered_names)
}
for polygon, material_name in zip(
    mesh.polygons,
    source_face_materials,
):
    polygon.material_index = material_indices[material_name]

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

OUTPUT.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.object.bake(
    type="EMIT",
    pass_filter={"EMIT"},
    use_clear=True,
    margin=8,
)
image.save()
print(f"Approved Resistance UV face-material mask baked: {OUTPUT}")
