import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE_GLB = PROJECT_ROOT / "enemies model" / "accelerando.glb"
OUTPUT_DIR = PROJECT_ROOT / "artSample" / "enemies" / "accelerando" / "antenna_connection_color_fix" / "renders"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_material(name, color, roughness=0.65, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    return mat


def import_mesh():
    bpy.ops.import_scene.gltf(filepath=str(SOURCE_GLB))
    for obj in list(bpy.context.scene.objects):
        if obj.name != "Mesh1.0":
            bpy.data.objects.remove(obj, do_unlink=True)
    mesh_obj = bpy.data.objects["Mesh1.0"]
    mesh_obj.data.materials.clear()
    mesh_obj.data.materials.append(make_material("Original neutral gray", (0.42, 0.45, 0.45, 1.0)))
    return mesh_obj


def calculate_bounds(obj):
    world_corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    min_corner = Vector((min(v.x for v in world_corners), min(v.y for v in world_corners), min(v.z for v in world_corners)))
    max_corner = Vector((max(v.x for v in world_corners), max(v.y for v in world_corners), max(v.z for v in world_corners)))
    return min_corner, max_corner


def setup_scene():
    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 64
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 1000

    light_data = bpy.data.lights.new("Key_Light", "AREA")
    light_data.energy = 600
    light_data.size = 4
    light = bpy.data.objects.new("Key_Light", light_data)
    bpy.context.collection.objects.link(light)
    light.location = (0, -5, 6)

    fill_data = bpy.data.lights.new("Fill_Light", "POINT")
    fill_data.energy = 130
    fill = bpy.data.objects.new("Fill_Light", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = (-4, 3, 4)

    camera_data = bpy.data.cameras.new("Render_Camera")
    camera = bpy.data.objects.new("Render_Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    camera.data.lens = 70
    camera.data.type = "ORTHO"
    return camera


def look_at(camera, target):
    direction = target - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_view(camera, obj, filename, direction):
    min_corner, max_corner = calculate_bounds(obj)
    center = (min_corner + max_corner) * 0.5
    size = max_corner - min_corner
    distance = max(size.x, size.y, size.z) * 3.2
    camera.location = center + Vector(direction).normalized() * distance + Vector((0, 0, size.z * 0.15))
    look_at(camera, center + Vector((0, 0, size.z * 0.05)))
    camera.data.ortho_scale = max(size.x, size.y, size.z) * 1.25
    bpy.context.scene.render.filepath = str(OUTPUT_DIR / filename)
    bpy.ops.render.render(write_still=True)


def main():
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    clear_scene()
    mesh_obj = import_mesh()
    camera = setup_scene()
    render_view(camera, mesh_obj, "accelerando_original_front.png", (0, -1, 0))
    render_view(camera, mesh_obj, "accelerando_original_side.png", (1, 0, 0))
    render_view(camera, mesh_obj, "accelerando_original_oblique.png", (1, -1, 0))


if __name__ == "__main__":
    main()
