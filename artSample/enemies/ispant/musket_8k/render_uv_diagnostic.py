import importlib.util
import os

import bpy
from mathutils import Matrix, Vector


ROOT = r"D:\Bellerophon2\Bellerophon\artSample\enemies\ispant\musket_8k"
BUILDER_PATH = os.path.join(ROOT, "build_musket_8k.py")
FINAL_FBX_PATH = os.path.join(ROOT, "Ispant_Musket_8K_Textured.fbx")
UNITY_SOURCE_FBX_PATH = r"D:\Bellerophon2\Bellerophon\Assets\_Project\Art\Enemies\Ispant\Models\Ispant_New_Direct_Source.fbx"
OUTPUT_PATH = os.path.join(ROOT, "Ispant_Musket_8K_UV_BeforeAfter.png")


spec = importlib.util.spec_from_file_location("musket_builder", BUILDER_PATH)
builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)


def emission_material(name, color):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = color
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def transfer_previous_pca_uv(source, target):
    source_proxy = builder.make_proxy(
        source,
        "_PreviousUV_SourceCanonical",
        builder.canonicalize(builder.world_points(source)),
    )
    target_proxy = builder.make_proxy(
        target,
        "_PreviousUV_TargetCanonical",
        builder.canonicalize(builder.world_points(target)) * builder.REGISTRATION_SIGNS,
    )
    while target_proxy.data.uv_layers:
        target_proxy.data.uv_layers.remove(target_proxy.data.uv_layers[0])
    source_uv = source_proxy.data.uv_layers.active
    target_proxy.data.uv_layers.new(name=source_uv.name)
    bpy.ops.object.select_all(action="DESELECT")
    target_proxy.select_set(True)
    bpy.context.view_layer.objects.active = target_proxy
    modifier = target_proxy.modifiers.new(name="PreviousPCAUV", type="DATA_TRANSFER")
    modifier.object = source_proxy
    modifier.use_loop_data = True
    modifier.data_types_loops = {"UV"}
    modifier.loop_mapping = "POLYINTERP_NEAREST"
    modifier.layers_uv_select_src = source_uv.name
    modifier.layers_uv_select_dst = source_uv.name
    bpy.ops.object.modifier_apply(modifier=modifier.name)

    while target.data.uv_layers:
        target.data.uv_layers.remove(target.data.uv_layers[0])
    target_uv = target.data.uv_layers.new(name=target_proxy.data.uv_layers.active.name)
    for index, item in enumerate(target_proxy.data.uv_layers.active.data):
        target_uv.data[index].uv = item.uv
    bpy.data.objects.remove(source_proxy, do_unlink=True)
    bpy.data.objects.remove(target_proxy, do_unlink=True)


def display_copy(source, name, center_y):
    result = builder.world_baked_review_copy(source, name)
    minimum, maximum = builder.world_bounds(result)
    center = (minimum + maximum) * 0.5
    scale = 8.3 / (maximum.x - minimum.x)
    result.matrix_world = (
        Matrix.Translation(Vector((0.0, center_y, 0.0)))
        @ Matrix.Scale(scale, 4)
        @ Matrix.Translation(-center)
    )
    bpy.context.view_layer.update()
    fitted_minimum, fitted_maximum = builder.world_bounds(result)
    fitted_center = (fitted_minimum + fitted_maximum) * 0.5
    fitted_size = fitted_maximum - fitted_minimum
    frame_scale = min(9.0 / fitted_size.x, 1.15 / fitted_size.y)
    result.matrix_world = (
        Matrix.Translation(Vector((0.0, center_y, 0.0)))
        @ Matrix.Scale(frame_scale, 4)
        @ Matrix.Translation(-fitted_center)
        @ result.matrix_world
    )
    return result


def add_panel(name, center_y, color):
    bpy.ops.mesh.primitive_plane_add(size=2.0, location=(0.0, center_y, -0.18))
    panel = bpy.context.object
    panel.name = name
    panel.scale = (5.45, 1.02, 1.0)
    panel.data.materials.append(emission_material(f"{name}_Material", color))
    return panel


def add_text(text, location, size, material, align="LEFT"):
    bpy.ops.object.text_add(location=location)
    label = bpy.context.object
    label.data.body = text
    label.data.align_x = align
    label.data.size = size
    label.data.materials.append(material)
    return label


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FINAL_FBX_PATH, use_anim=False)
final_meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if len(final_meshes) != 1:
    raise RuntimeError(f"Expected one final mesh, found {len(final_meshes)}")
corrected = final_meshes[0]
corrected.modifiers.clear()

previous = corrected.copy()
previous.data = corrected.data.copy()
previous.name = "Previous_PCA_UV"
bpy.context.scene.collection.objects.link(previous)

bpy.ops.import_scene.fbx(filepath=UNITY_SOURCE_FBX_PATH, use_anim=False)
source = bpy.data.objects.get("char1")
if source is None:
    raise RuntimeError("Unity source char1 is missing")
source.modifiers.clear()
islands = builder.uv_islands(source.data)
selected_faces = {
    face
    for island in builder.MUSKET_UV_ISLANDS
    for face in islands[island]
}
source_musket = builder.separate_source_musket(source, selected_faces)
source_musket.modifiers.clear()
transfer_previous_pca_uv(source_musket, previous)

previous_display = display_copy(previous, "Display_Previous_PCA_UV", 0.66)
corrected_display = display_copy(corrected, "Display_Current_ICP_UV", -1.50)
for obj in bpy.context.scene.objects:
    obj.hide_render = obj not in (previous_display, corrected_display)

previous_panel = add_panel("PreviousPanel", 0.78, (0.055, 0.043, 0.039, 1.0))
current_panel = add_panel("CurrentPanel", -1.38, (0.032, 0.052, 0.047, 1.0))
previous_panel.hide_render = False
current_panel.hide_render = False

white = emission_material("TextWhite", (0.90, 0.92, 0.91, 1.0))
muted = emission_material("TextMuted", (0.48, 0.54, 0.53, 1.0))
rust = emission_material("TextRust", (0.82, 0.32, 0.17, 1.0))
green = emission_material("TextGreen", (0.32, 0.78, 0.58, 1.0))

add_text("UV TEXTURE ALIGNMENT", (0.0, 3.03, 0.1), 0.30, white, "CENTER")
add_text("동일 메시 · 동일 방향 · 동일 배율 · 동일 카메라", (0.0, 2.62, 0.1), 0.15, muted, "CENTER")
for x, label in ((-3.65, "개머리판"), (-2.05, "격발부"), (2.65, "총열 · 총구")):
    add_text(label, (x, 2.13, 0.1), 0.16, muted, "CENTER")

add_text("BEFORE  ·  이전 PCA UV", (0.0, 1.50, 0.1), 0.18, rust, "CENTER")
add_text("AFTER  ·  현재 ICP 보정 UV", (0.0, -0.66, 0.1), 0.18, green, "CENTER")
add_text("채택", (4.55, -0.67, 0.1), 0.15, green, "CENTER")
add_text(
    "색 · 조명 · 형상은 고정하고 UV만 비교했습니다.",
    (0.0, -2.72, 0.1),
    0.14,
    muted,
    "CENTER",
)

camera_data = bpy.data.cameras.new("DiagnosticCamera")
camera = bpy.data.objects.new("DiagnosticCamera", camera_data)
bpy.context.scene.collection.objects.link(camera)
camera_target = Vector((0.0, 0.16, 0.0))
camera.location = (0.0, 0.16, 10.0)
camera.rotation_euler = (camera_target - camera.location).to_track_quat("-Z", "Y").to_euler()
camera_data.type = "ORTHO"
camera_data.ortho_scale = 11.8
bpy.context.scene.camera = camera

for center_y in (0.78, -1.38):
    key_data = bpy.data.lights.new(f"Key_{center_y}", type="AREA")
    key_data.energy = 900.0
    key_data.shape = "DISK"
    key_data.size = 5.0
    key = bpy.data.objects.new(f"Key_{center_y}", key_data)
    bpy.context.scene.collection.objects.link(key)
    key.location = (-1.2, center_y - 0.2, 4.5)
    rim_data = bpy.data.lights.new(f"Rim_{center_y}", type="AREA")
    rim_data.energy = 420.0
    rim_data.size = 3.0
    rim = bpy.data.objects.new(f"Rim_{center_y}", rim_data)
    bpy.context.scene.collection.objects.link(rim)
    rim.location = (3.0, center_y + 0.4, 3.0)

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 2400
scene.render.resolution_y = 1300
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = OUTPUT_PATH
scene.render.film_transparent = False
if scene.world is None:
    scene.world = bpy.data.worlds.new("DiagnosticWorld")
scene.world.color = (0.006, 0.008, 0.008)
bpy.ops.render.render(write_still=True)

print("===UV_DIAGNOSTIC_RENDER_PASS===")
print(f"PreviousVertices={len(previous.data.vertices)}|PreviousFaces={len(previous.data.polygons)}")
print(f"CurrentVertices={len(corrected.data.vertices)}|CurrentFaces={len(corrected.data.polygons)}")
print("Composition=Stacked|SameMesh=1|SameDirection=1|SameScale=1|SameCamera=1")
print(f"FbxSha256={builder.file_sha256(FINAL_FBX_PATH)}")
print(f"Output={OUTPUT_PATH}")
