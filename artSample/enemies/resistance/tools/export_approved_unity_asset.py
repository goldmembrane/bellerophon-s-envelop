from pathlib import Path
import hashlib
import json

import bpy


ROOT = Path(__file__).resolve().parents[1]
PROJECT_ROOT = ROOT.parents[2]
TEXTURE_DIR = ROOT / "textures"
UNITY_MODEL_DIR = (
    PROJECT_ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Enemies"
    / "Resistance"
    / "Models"
)
FBX_PATH = UNITY_MODEL_DIR / "ResistanceApprovedAppearance.fbx"
REPORT_PATH = ROOT / "geometry" / "approved_unity_export_report.json"
SIZE = 2048
GRID = 78
MARGIN_PIXELS = 3


def geometry_signature(mesh_object, armature_object):
    digest = hashlib.sha256()
    mesh = mesh_object.data
    for vertex in mesh.vertices:
        digest.update(
            (
                f"v:{vertex.co.x:.9f},{vertex.co.y:.9f},"
                f"{vertex.co.z:.9f};"
            ).encode("ascii")
        )
    for polygon in mesh.polygons:
        digest.update(
            (
                "p:"
                + ",".join(str(index) for index in polygon.vertices)
                + ";"
            ).encode("ascii")
        )
    for bone in armature_object.data.bones:
        digest.update(
            (
                f"b:{bone.name}:{bone.head_local.x:.9f},"
                f"{bone.head_local.y:.9f},{bone.head_local.z:.9f}:"
                f"{bone.tail_local.x:.9f},{bone.tail_local.y:.9f},"
                f"{bone.tail_local.z:.9f};"
            ).encode("utf-8")
        )
    return digest.hexdigest().upper()


mesh_object = bpy.data.objects.get("char1")
armature_object = bpy.data.objects.get("Armature")
if mesh_object is None or mesh_object.type != "MESH":
    raise RuntimeError("Approved Resistance char1 mesh is missing.")
if armature_object is None or armature_object.type != "ARMATURE":
    raise RuntimeError("Approved Resistance Armature is missing.")

mesh = mesh_object.data
if (
    len(mesh.vertices) != 3004
    or len(mesh.polygons) != 6037
    or len(armature_object.data.bones) != 24
):
    raise RuntimeError("Approved Resistance geometry contract changed.")
if any(len(polygon.vertices) != 3 for polygon in mesh.polygons):
    raise RuntimeError("Approved Resistance mesh is not triangulated.")

signature_before = geometry_signature(mesh_object, armature_object)
source_uv = mesh.uv_layers.get("uv")
if source_uv is None:
    raise RuntimeError("Approved Resistance source UV is missing.")

atlas_uv = mesh.uv_layers.get("ApprovedUnityAtlas")
if atlas_uv is None:
    atlas_uv = mesh.uv_layers.new(
        name="ApprovedUnityAtlas",
        do_init=False,
    )

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
    for loop_index, corner in zip(polygon.loop_indices, corners):
        atlas_uv.data[loop_index].uv = corner

mesh.uv_layers.active = atlas_uv
atlas_uv.active_render = True

materials = [
    slot.material
    for slot in mesh_object.material_slots
    if slot.material is not None
]
if len(materials) != 5:
    raise RuntimeError("Approved Resistance material slots changed.")

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


def bake_target(name, filename, bake_type, is_data):
    image = bpy.data.images.get(name)
    if image is not None:
        bpy.data.images.remove(image)
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
        nodes = material.node_tree.nodes
        image_node = nodes.new("ShaderNodeTexImage")
        image_node.name = name + "_" + material.name
        image_node.image = image
        nodes.active = image_node
    bpy.ops.object.bake(
        type=bake_type,
        use_clear=True,
        margin=1,
    )
    image.save()
    return image.filepath_raw


roughness_path = bake_target(
    "ResistanceApprovedTriangleRoughness",
    "resistance_approved_triangle_roughness.png",
    "ROUGHNESS",
    True,
)
normal_path = bake_target(
    "ResistanceApprovedTriangleNormal",
    "resistance_approved_triangle_normal.png",
    "NORMAL",
    True,
)

# The approved atlas becomes UV0 in the exported copy. Removing the source UV
# changes only texture coordinates in this in-memory export, never geometry.
mesh.uv_layers.remove(source_uv)
atlas_uv.name = "uv"
mesh.uv_layers.active = atlas_uv
atlas_uv.active_render = True

signature_after_uv = geometry_signature(mesh_object, armature_object)
if signature_after_uv != signature_before:
    raise RuntimeError("Approved Resistance geometry changed while preparing UVs.")

for item in bpy.context.selected_objects:
    item.select_set(False)
mesh_object.select_set(True)
armature_object.select_set(True)
bpy.context.view_layer.objects.active = armature_object
UNITY_MODEL_DIR.mkdir(parents=True, exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=str(FBX_PATH),
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    use_mesh_modifiers=False,
    add_leaf_bones=False,
    use_armature_deform_only=False,
    bake_anim=False,
    mesh_smooth_type="FACE",
    path_mode="AUTO",
)

report = {
    "source_blend": bpy.data.filepath,
    "export_fbx": str(FBX_PATH),
    "vertex_count": len(mesh.vertices),
    "edge_count": len(mesh.edges),
    "polygon_count": len(mesh.polygons),
    "loop_count": len(mesh.loops),
    "bone_count": len(armature_object.data.bones),
    "material_slots": [material.name for material in materials],
    "export_uv_layers": [layer.name for layer in mesh.uv_layers],
    "geometry_signature_before": signature_before,
    "geometry_signature_after_uv": signature_after_uv,
    "geometry_changed": False,
    "roughness_atlas": roughness_path,
    "normal_atlas": normal_path,
}
REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
REPORT_PATH.write_text(
    json.dumps(report, ensure_ascii=False, indent=2),
    encoding="utf-8",
)
print("RESISTANCE_APPROVED_UNITY_EXPORT=" + json.dumps(report))
