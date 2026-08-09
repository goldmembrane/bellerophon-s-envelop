import bpy
from mathutils import Vector


SOURCE = r"D:\Bellerophon2\Bellerophon\Assets\_Project\Art\Enemies\Ispant\Animations\Ispant_SheathSword.fbx"
OUTPUT = r"D:\Bellerophon2\Bellerophon\docs\validation\ispant_sheath_to_rifle_upright_trigger_grip_revision_2026-08-09"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SOURCE)
source = bpy.data.objects["Ispant_Sheath_RigidMusket"]
mesh = source.data.copy()
musket = bpy.data.objects.new("ApprovedMusketGeometry", mesh)
bpy.context.collection.objects.link(musket)

for item in list(bpy.context.scene.objects):
    item.hide_render = item != musket

material = bpy.data.materials.new("GeometryReview")
material.diffuse_color = (0.48, 0.58, 0.68, 1.0)
mesh.materials.clear()
mesh.materials.append(material)
for polygon in mesh.polygons:
    polygon.material_index = 0

minimum = Vector((
    min(vertex.co.x for vertex in mesh.vertices),
    min(vertex.co.y for vertex in mesh.vertices),
    min(vertex.co.z for vertex in mesh.vertices),
))
maximum = Vector((
    max(vertex.co.x for vertex in mesh.vertices),
    max(vertex.co.y for vertex in mesh.vertices),
    max(vertex.co.z for vertex in mesh.vertices),
))
center = (minimum + maximum) * 0.5

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.world = bpy.data.worlds.new("ReviewWorld")
scene.world.color = (0.025, 0.03, 0.04)

camera_data = bpy.data.cameras.new("ReviewCamera")
camera = bpy.data.objects.new("ReviewCamera", camera_data)
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera_data.type = "ORTHO"
camera_data.ortho_scale = max((maximum - minimum).length * 1.05, 1.0)

light_data = bpy.data.lights.new("ReviewKey", type="AREA")
light_data.energy = 1600
light_data.shape = "DISK"
light_data.size = 150
light = bpy.data.objects.new("ReviewKey", light_data)
bpy.context.collection.objects.link(light)
light.location = center + Vector((0, 0, 180))


def render(name, direction, up="Y"):
    direction = Vector(direction).normalized()
    camera.location = center + direction * 220
    camera.rotation_euler = (-direction).to_track_quat("-Z", up).to_euler()
    scene.render.filepath = OUTPUT + "\\" + name
    bpy.ops.render.render(write_still=True)


render("MusketGeometry_PositiveZ.png", (0, 0, 1))
render("MusketGeometry_NegativeZ.png", (0, 0, -1))
render("MusketGeometry_PositiveX.png", (1, 0, 0))
