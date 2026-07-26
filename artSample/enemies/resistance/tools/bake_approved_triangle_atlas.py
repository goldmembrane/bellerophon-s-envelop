from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[1]
TEXTURE_DIR = ROOT / "textures"
SIZE = 2048
GRID = 78
MARGIN_PIXELS = 3


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
if any(len(polygon.vertices) != 3 for polygon in mesh.polygons):
    raise RuntimeError("Approved Resistance mesh is not triangulated.")
if len(mesh.polygons) > GRID * GRID:
    raise RuntimeError("Triangle atlas grid is too small.")

atlas_uv = mesh.uv_layers.get("UnityPrimitiveAtlas")
if atlas_uv is None:
    atlas_uv = mesh.uv_layers.new(
        name="UnityPrimitiveAtlas",
        do_init=False,
    )
mesh.uv_layers.active = atlas_uv
atlas_uv.active_render = True

margin = MARGIN_PIXELS / SIZE
tile = 1.0 / GRID
for polygon in mesh.polygons:
    column = polygon.index % GRID
    row = polygon.index // GRID
    minimum_u = column * tile + margin
    minimum_v = row * tile + margin
    maximum_u = (column + 1) * tile - margin
    maximum_v = (row + 1) * tile - margin
    corners = (
        (minimum_u, minimum_v),
        (maximum_u, minimum_v),
        (minimum_u, maximum_v),
    )
    for loop_index, corner in zip(
        polygon.loop_indices,
        corners,
    ):
        atlas_uv.data[loop_index].uv = corner

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
scene.render.bake.margin = 1
scene.render.bake.use_clear = True
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGB"
scene.render.image_settings.color_depth = "8"
TEXTURE_DIR.mkdir(parents=True, exist_ok=True)


def create_target(name, filename):
    image = bpy.data.images.new(
        name,
        width=SIZE,
        height=SIZE,
        alpha=False,
        float_buffer=False,
        is_data=False,
    )
    image.file_format = "PNG"
    image.filepath_raw = str(TEXTURE_DIR / filename)
    for material in materials:
        nodes = material.node_tree.nodes
        image_node = nodes.new("ShaderNodeTexImage")
        image_node.name = name + "_" + material.name
        image_node.image = image
        nodes.active = image_node
    return image


albedo = create_target(
    "ResistanceApprovedTriangleAlbedo",
    "resistance_approved_triangle_albedo.png",
)
bpy.ops.object.bake(
    type="DIFFUSE",
    pass_filter={"COLOR"},
    use_clear=True,
    margin=1,
)
albedo.save()

emission = create_target(
    "ResistanceApprovedTriangleEmission",
    "resistance_approved_triangle_emission.png",
)
bpy.ops.object.bake(
    type="EMIT",
    pass_filter={"EMIT"},
    use_clear=True,
    margin=1,
)
emission.save()

print(
    "Approved Resistance per-triangle atlases baked: "
    + str(albedo.filepath_raw)
    + ", "
    + str(emission.filepath_raw)
)
