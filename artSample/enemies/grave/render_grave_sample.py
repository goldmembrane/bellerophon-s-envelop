import math
from pathlib import Path

import bpy
from mathutils import Vector


sample_dir = Path(__file__).resolve().parent
fbx_path = sample_dir / "grave.fbx"
output_path = sample_dir / "grave_static_preview.png"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(fbx_path))

character = bpy.data.objects.get("char1")
if character is None:
    raise RuntimeError("grave.fbx에서 char1 캐릭터 메시를 찾지 못했습니다.")

for obj in bpy.context.scene.objects:
    obj.hide_render = obj != character and obj.type in {"MESH", "LIGHT", "CAMERA"}

corners = [character.matrix_world @ Vector(corner) for corner in character.bound_box]
minimum = Vector((min(v.x for v in corners), min(v.y for v in corners), min(v.z for v in corners)))
maximum = Vector((max(v.x for v in corners), max(v.y for v in corners), max(v.z for v in corners)))
center = (minimum + maximum) * 0.5
size = maximum - minimum

world = bpy.context.scene.world or bpy.data.worlds.new("GravePreviewWorld")
bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.035, 0.045, 0.055, 1.0)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.32

camera_data = bpy.data.cameras.new("GravePreviewCamera")
camera = bpy.data.objects.new("GravePreviewCamera", camera_data)
bpy.context.collection.objects.link(camera)
bpy.context.scene.camera = camera
camera_data.type = "ORTHO"
camera_data.ortho_scale = max(size.x, size.z) * 1.28
camera.location = center + Vector((0.0, -4.0, size.z * 0.04))


def point_at(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


point_at(camera, center + Vector((0.0, 0.0, size.z * 0.02)))

key_data = bpy.data.lights.new("GravePreviewKey", "AREA")
key_data.energy = 900.0
key_data.shape = "DISK"
key_data.size = 4.0
key = bpy.data.objects.new("GravePreviewKey", key_data)
bpy.context.collection.objects.link(key)
key.location = center + Vector((-2.5, -3.0, 3.0))
point_at(key, center)

fill_data = bpy.data.lights.new("GravePreviewFill", "AREA")
fill_data.energy = 450.0
fill_data.size = 3.0
fill = bpy.data.objects.new("GravePreviewFill", fill_data)
bpy.context.collection.objects.link(fill)
fill.location = center + Vector((2.0, -1.5, 1.2))
point_at(fill, center)

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = str(output_path)
scene.render.film_transparent = False
scene.view_settings.look = "AgX - Medium High Contrast"
bpy.ops.render.render(write_still=True)

print(f"GRAVE_SAMPLE_RENDER={output_path}")
