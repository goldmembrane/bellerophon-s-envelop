import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def parse_args():
    arguments = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--report", required=True)
    return parser.parse_args(arguments)


def look_at(camera, point):
    camera.rotation_euler = (Vector(point) - camera.location).to_track_quat("-Z", "Y").to_euler()


def material(name, color, metallic=0.0, roughness=0.45, emission=None):
    result = bpy.data.materials.new(name)
    result.diffuse_color = (*color, 1.0)
    result.use_nodes = True
    shader = result.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if emission is not None:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 4.0
    return result


def world_bounds(obj):
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    minimum = [min(corner[index] for corner in corners) for index in range(3)]
    maximum = [max(corner[index] for corner in corners) for index in range(3)]
    return minimum, maximum


def connected_components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        first, second = edge.vertices
        adjacency[first].add(second)
        adjacency[second].add(first)
    pending = set(range(len(mesh.vertices)))
    sizes = []
    while pending:
        seed = pending.pop()
        stack = [seed]
        size = 0
        while stack:
            current = stack.pop()
            size += 1
            for neighbor in adjacency[current]:
                if neighbor in pending:
                    pending.remove(neighbor)
                    stack.append(neighbor)
        sizes.append(size)
    return sorted(sizes, reverse=True)


def main():
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()
    report_path = Path(args.report).resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(input_path))

    body_material = material("Diagnostic_Body", (0.69, 0.72, 0.74), metallic=0.62, roughness=0.34)
    crescent_material = material("Diagnostic_Crescent", (0.92, 0.94, 0.96), metallic=0.82, roughness=0.2)
    eye_material = material(
        "Diagnostic_Eyes", (0.04, 0.55, 0.72), metallic=0.1, roughness=0.2,
        emission=(0.03, 0.72, 1.0))

    mesh_objects = sorted(
        [obj for obj in bpy.context.scene.objects if obj.type == "MESH"],
        key=lambda obj: obj.name)
    report = {"input": str(input_path), "meshes": []}
    for obj in mesh_objects:
        if "Eye" in obj.name:
            assigned = eye_material
        elif "Crescent" in obj.name:
            assigned = crescent_material
        else:
            assigned = body_material
        obj.data.materials.clear()
        obj.data.materials.append(assigned)
        minimum, maximum = world_bounds(obj)
        report["meshes"].append({
            "name": obj.name,
            "vertices": len(obj.data.vertices),
            "polygons": len(obj.data.polygons),
            "components": connected_components(obj.data),
            "bounds_min": [round(value, 6) for value in minimum],
            "bounds_max": [round(value, 6) for value in maximum],
        })

    world = bpy.context.scene.world or bpy.data.worlds.new("DiagnosticWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.012, 0.017, 0.024, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.24

    for name, location, energy, size in (
        ("Key", (-3.0, -4.5, 4.5), 1150.0, 4.0),
        ("Fill", (3.5, -2.0, 2.8), 700.0, 3.0),
        ("Rim", (0.0, 2.5, 3.5), 950.0, 2.5),
    ):
        light_data = bpy.data.lights.new(name, "AREA")
        light_data.energy = energy
        light_data.shape = "DISK"
        light_data.size = size
        light = bpy.data.objects.new(name, light_data)
        light.location = location
        look_at(light, (0.0, 0.0, 0.9))
        bpy.context.collection.objects.link(light)

    camera_data = bpy.data.cameras.new("DiagnosticCamera")
    camera = bpy.data.objects.new("DiagnosticCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = (0.0, -5.5, 0.92)
    look_at(camera, (0.0, 0.0, 0.92))
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = 2.15
    bpy.context.scene.camera = camera

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 720
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.filepath = str(output_path)
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.ops.render.render(write_still=True)

    report["armatures"] = len([obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"])
    report["actions"] = len(bpy.data.actions)
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
