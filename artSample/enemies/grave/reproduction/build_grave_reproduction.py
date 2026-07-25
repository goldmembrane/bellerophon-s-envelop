import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parent
SOURCE_FBX = ROOT / "source" / "grave_base.fbx"
TEXTURE_DIR = ROOT / "textures"
BLEND_PATH = ROOT / "grave_reproduction.blend"
FBX_PATH = ROOT / "grave_reproduction.fbx"
WORK_PREVIEW = ROOT / "review" / "work_preview_rgba.png"

TARGET_HEIGHT = 1.6
SOURCE_HEIGHT = 1.7
OVERALL_WIDTH_SCALE = 1.18
TORSO_WIDTH_SCALE = 1.04
DEPTH_SCALE = 0.79

LEFT_ARM_GROUPS = {"LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand"}
RIGHT_ARM_GROUPS = {"RightShoulder", "RightArm", "RightForeArm", "RightHand"}


def reshape_world(point):
    source = point.copy()
    point.x *= OVERALL_WIDTH_SCALE
    if source.z > 0.38 and abs(source.x) < 0.225:
        torso_blend = min(1.0, max(0.0, (source.z - 0.38) / 0.28))
        point.x *= 1.0 + (TORSO_WIDTH_SCALE - 1.0) * torso_blend
    point.y *= DEPTH_SCALE
    point.z *= TARGET_HEIGHT / SOURCE_HEIGHT
    return point


def set_input(node, name, value):
    socket = node.inputs.get(name)
    if socket is not None:
        socket.default_value = value


def load_image(name, colorspace="sRGB"):
    image = bpy.data.images.load(str(TEXTURE_DIR / name), check_existing=True)
    image.colorspace_settings.name = colorspace
    return image


def make_textured_material(name, albedo_name):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    output.location = (620, 20)
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.location = (340, 20)
    set_input(shader, "Roughness", 0.78)
    set_input(shader, "Metallic", 0.0)
    set_input(shader, "Specular IOR Level", 0.22)

    uv_map = nodes.new("ShaderNodeUVMap")
    uv_map.uv_map = "GraveReferenceUV"
    uv_map.location = (-760, 20)

    albedo = nodes.new("ShaderNodeTexImage")
    albedo.name = "Grave Albedo"
    albedo.image = load_image(albedo_name)
    albedo.location = (-520, 150)
    albedo.interpolation = "Linear"
    links.new(uv_map.outputs["UV"], albedo.inputs["Vector"])
    links.new(albedo.outputs["Color"], shader.inputs["Base Color"])

    roughness = nodes.new("ShaderNodeTexImage")
    roughness.name = "Grave Roughness"
    roughness.image = load_image("grave_fabric_roughness.png", "Non-Color")
    roughness.location = (-520, -40)
    roughness.interpolation = "Linear"
    links.new(uv_map.outputs["UV"], roughness.inputs["Vector"])
    links.new(roughness.outputs["Color"], shader.inputs["Roughness"])

    normal_texture = nodes.new("ShaderNodeTexImage")
    normal_texture.name = "Grave Fabric Normal"
    normal_texture.image = load_image("grave_fabric_normal.png", "Non-Color")
    normal_texture.location = (-520, -250)
    normal_texture.interpolation = "Linear"
    links.new(uv_map.outputs["UV"], normal_texture.inputs["Vector"])
    normal_map = nodes.new("ShaderNodeNormalMap")
    normal_map.location = (60, -210)
    normal_map.inputs["Strength"].default_value = 0.16
    links.new(normal_texture.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], shader.inputs["Normal"])
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def point_at(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))

for obj in list(bpy.context.scene.objects):
    if obj.name == "Cube" or obj.type in {"CAMERA", "LIGHT"}:
        bpy.data.objects.remove(obj, do_unlink=True)

body = bpy.data.objects.get("char1")
rig = next((obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"), None)
if body is None or rig is None:
    raise RuntimeError("기준 FBX에서 char1 메시 또는 Armature를 찾지 못했습니다.")

body.name = "Grave_Body"
body.data.name = "Grave_Body_Mesh"
rig.name = "Grave_Rig"
rig.data.name = "Grave_Rig_Data"

inverse_body = body.matrix_world.inverted()
for vertex in body.data.vertices:
    world = body.matrix_world @ vertex.co
    world = reshape_world(world)
    left_weight = 0.0
    right_weight = 0.0
    for assignment in vertex.groups:
        group_name = body.vertex_groups[assignment.group].name
        if group_name in LEFT_ARM_GROUPS:
            left_weight += assignment.weight
        elif group_name in RIGHT_ARM_GROUPS:
            right_weight += assignment.weight
    arm_drop = min(1.0, max(0.0, (1.10 - world.z) / 0.75))
    world.x += 0.07 * arm_drop * (left_weight - right_weight)
    vertex.co = inverse_body @ world
body.data.update()

for polygon in body.data.polygons:
    polygon.use_smooth = True
body.data.update()

front_material = make_textured_material("Grave_Suit_Front_Mat", "grave_front_albedo.png")
textile_material = make_textured_material("Grave_Textile_BackSide_Mat", "grave_textile_albedo.png")
body.data.materials.clear()
body.data.materials.append(front_material)
body.data.materials.append(textile_material)

uv_layer = body.data.uv_layers.get("GraveReferenceUV") or body.data.uv_layers.new(name="GraveReferenceUV")
body.data.uv_layers.active = uv_layer
uv_layer.active_render = True
normal_matrix = body.matrix_world.to_3x3().inverted().transposed()

world_points = [body.matrix_world @ vertex.co for vertex in body.data.vertices]
minimum_x = min(point.x for point in world_points)
maximum_x = max(point.x for point in world_points)
minimum_z = min(point.z for point in world_points)
maximum_z = max(point.z for point in world_points)

for polygon in body.data.polygons:
    world_normal = (normal_matrix @ polygon.normal).normalized()
    is_front = world_normal.y < 0.45
    polygon.material_index = 0 if is_front else 1
    for loop_index in polygon.loop_indices:
        vertex_index = body.data.loops[loop_index].vertex_index
        world = body.matrix_world @ body.data.vertices[vertex_index].co
        if is_front:
            u = (world.x - minimum_x) / max(0.0001, maximum_x - minimum_x)
            v = (world.z - minimum_z) / max(0.0001, maximum_z - minimum_z)
        else:
            u = world.x * 1.65 + world.y * 2.35
            v = world.z * 1.65
        uv_layer.data[loop_index].uv = (u, v)

body["reproduction_target"] = "image/grave(그라베).png"
body["target_height_m"] = TARGET_HEIGHT
body["front_surface_inference"] = False
body["back_surface_inference"] = True
rig["source_fbx_sha256"] = "D6B44E97909B8B3D40A87E008A9E1916B4656BAC54A6DD55B438E890406750A6"

bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=str(FBX_PATH),
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    apply_unit_scale=True,
    add_leaf_bones=False,
    bake_anim=False,
    path_mode="RELATIVE",
)

plane_material = bpy.data.materials.new("Review_Backdrop_Mat")
plane_material.use_nodes = True
plane_shader = plane_material.node_tree.nodes.get("Principled BSDF")
set_input(plane_shader, "Base Color", (0.94, 0.945, 0.95, 1.0))
set_input(plane_shader, "Roughness", 0.92)
bpy.ops.mesh.primitive_plane_add(size=8.0, location=(0.0, 0.0, -0.012))
plane = bpy.context.object
plane.name = "Review_Backdrop"
plane.data.materials.append(plane_material)

camera_data = bpy.data.cameras.new("Grave_Review_Camera")
camera = bpy.data.objects.new("Grave_Review_Camera", camera_data)
bpy.context.collection.objects.link(camera)
camera_data.type = "ORTHO"
camera_data.ortho_scale = 3.28
camera.location = (0.016, -5.0, 0.822)
point_at(camera, Vector((0.016, 0.0, 0.822)))
bpy.context.scene.camera = camera

key_data = bpy.data.lights.new("Grave_Review_Key", "AREA")
key_data.energy = 72.0
key_data.shape = "DISK"
key_data.size = 3.5
key = bpy.data.objects.new("Grave_Review_Key", key_data)
bpy.context.collection.objects.link(key)
key.location = (-2.4, -3.0, 3.2)
point_at(key, Vector((0.0, 0.0, 0.85)))

fill_data = bpy.data.lights.new("Grave_Review_Fill", "AREA")
fill_data.energy = 28.0
fill_data.size = 3.0
fill = bpy.data.objects.new("Grave_Review_Fill", fill_data)
bpy.context.collection.objects.link(fill)
fill.location = (2.2, -1.8, 1.8)
point_at(fill, Vector((0.0, 0.0, 0.8)))

world = bpy.context.scene.world or bpy.data.worlds.new("Grave_Review_World")
bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.96, 0.965, 0.97, 1.0)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.58

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1408
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = True
scene.render.use_freestyle = False
scene.render.filepath = str(WORK_PREVIEW)
scene.view_settings.look = "AgX - Medium High Contrast"

bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
bpy.ops.render.render(write_still=True)

print(f"GRAVE_REPRODUCTION_BLEND={BLEND_PATH}")
print(f"GRAVE_REPRODUCTION_FBX={FBX_PATH}")
print(f"GRAVE_WORK_PREVIEW={WORK_PREVIEW}")
print(
    "GRAVE_REPRODUCTION_BOUNDS="
    f"({minimum_x:.6f},{minimum_z:.6f})-({maximum_x:.6f},{maximum_z:.6f})"
)
