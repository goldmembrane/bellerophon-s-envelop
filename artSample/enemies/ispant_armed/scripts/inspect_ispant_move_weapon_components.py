import argparse
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def parse_args():
    arguments = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(arguments)


def create_material(name, color, metallic=0.0, roughness=0.42, emission=None):
    result = bpy.data.materials.new(name)
    result.diffuse_color = (*color, 1.0)
    result.use_nodes = True
    shader = result.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if emission:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 3.5
    return result


def components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        first, second = edge.vertices
        adjacency[first].add(second)
        adjacency[second].add(first)
    pending = set(range(len(mesh.vertices)))
    result = []
    while pending:
        seed = min(pending)
        pending.remove(seed)
        stack = [seed]
        vertices = []
        while stack:
            current = stack.pop()
            vertices.append(current)
            for neighbor in adjacency[current]:
                if neighbor in pending:
                    pending.remove(neighbor)
                    stack.append(neighbor)
        result.append(set(vertices))
    return result


def look_at(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat("-Z", "Y").to_euler()


def world_bounds(objects):
    corners = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    minimum = Vector(tuple(min(corner[index] for corner in corners) for index in range(3)))
    maximum = Vector(tuple(max(corner[index] for corner in corners) for index in range(3)))
    return minimum, maximum


def main():
    args = parse_args()
    output = Path(args.output).resolve()
    output.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(Path(args.input).resolve()))
    bpy.context.scene.frame_set(1)

    body = bpy.data.objects["Ispant_Armed_Body"]
    body_components = components(body.data)
    if len(body_components) != 81:
        raise RuntimeError(f"Expected 81 body components, got {len(body_components)}")

    armor = create_material("Diagnostic_AnimatedBody", (0.50, 0.53, 0.57), metallic=0.58)
    musket = create_material("Diagnostic_Musket_41", (0.95, 0.16, 0.07), metallic=0.25)
    sword = create_material("Diagnostic_Sword_77_78", (0.05, 0.46, 1.0), metallic=0.82, roughness=0.2)
    candidates = create_material("Diagnostic_OtherSteel", (0.15, 0.90, 0.24), metallic=0.72)
    cyan = create_material("Diagnostic_Eyes", (0.03, 0.72, 1.0), emission=(0.03, 0.72, 1.0))

    body.data.materials.clear()
    for assigned in (armor, musket, sword, candidates):
        body.data.materials.append(assigned)
    vertex_component = {}
    for component_index, vertex_indices in enumerate(body_components):
        for vertex_index in vertex_indices:
            vertex_component[vertex_index] = component_index
    for polygon in body.data.polygons:
        component_index = vertex_component[polygon.vertices[0]]
        if component_index == 41:
            polygon.material_index = 1
        elif component_index in (77, 78):
            polygon.material_index = 2
        elif component_index in (75, 76, 79, 80):
            polygon.material_index = 3
        else:
            polygon.material_index = 0

    for obj in bpy.context.scene.objects:
        if obj.type != "MESH" or obj == body:
            continue
        obj.data.materials.clear()
        obj.data.materials.append(cyan if "Eye" in obj.name else armor)

    world = bpy.context.scene.world or bpy.data.worlds.new("DiagnosticWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.01, 0.014, 0.02, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.2

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    minimum, maximum = world_bounds(meshes)
    center = (minimum + maximum) * 0.5
    size = maximum - minimum

    for name, location, energy, radius in (
        ("Key", center + Vector((-2.5, -4.0, 3.3)), 1200.0, 3.5),
        ("Fill", center + Vector((3.0, -2.0, 2.0)), 650.0, 2.5),
        ("Rim", center + Vector((0.0, 2.5, 3.0)), 900.0, 2.0),
    ):
        light_data = bpy.data.lights.new(name, "AREA")
        light_data.energy = energy
        light_data.shape = "DISK"
        light_data.size = radius
        light = bpy.data.objects.new(name, light_data)
        light.location = location
        look_at(light, center)
        bpy.context.collection.objects.link(light)

    camera_data = bpy.data.cameras.new("DiagnosticCamera")
    camera = bpy.data.objects.new("DiagnosticCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = center + Vector((0.0, -6.0, 0.0))
    look_at(camera, center)
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = max(size.x, size.z) * 1.18
    bpy.context.scene.camera = camera

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(output)
    scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
