import argparse
import json
import os
import sys

import bpy
from mathutils import Vector


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output")
    parser.add_argument("--inspect-only", action="store_true")
    parser.add_argument("--preview-dir")
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def import_fbx(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False)


def render_preview(obj, output_path, center, direction, scale):
    scene = bpy.context.scene
    for candidate in scene.objects:
        candidate.hide_render = candidate != obj
    camera_data = bpy.data.cameras.new("MusketBoreInspectionCamera")
    camera = bpy.data.objects.new("MusketBoreInspectionCamera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.location = Vector(center) + Vector(direction).normalized() * scale * 3.0
    camera.rotation_euler = (Vector(center) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = scale
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "SINGLE"
    scene.display.shading.single_color = (0.1, 0.15, 0.2)
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    if scene.world is None:
        scene.world = bpy.data.worlds.new("MusketBoreInspectionWorld")
    scene.world.color = (0.015, 0.02, 0.03)
    scene.render.filepath = output_path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)


def create_bore():
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if len(meshes) != 1:
        raise RuntimeError("Expected exactly one musket mesh.")
    obj = meshes[0]
    original_material_count = len(obj.data.materials)
    world_vertices = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    minimum = Vector(
        tuple(min(vertex[index] for vertex in world_vertices) for index in range(3))
    )
    maximum = Vector(
        tuple(max(vertex[index] for vertex in world_vertices) for index in range(3))
    )
    long_axis = max(range(3), key=lambda index: (maximum - minimum)[index])
    if long_axis != 2:
        raise RuntimeError("Inspected musket barrel axis changed; expected world Z.")
    high_points = [
        vertex
        for vertex in world_vertices
        if vertex[2] >= maximum[2] - 0.006 and vertex[0] > 0.0
    ]
    if len(high_points) < 16:
        raise RuntimeError("Could not isolate the inspected muzzle ring.")
    center_x = (
        min(vertex.x for vertex in high_points)
        + max(vertex.x for vertex in high_points)
    ) * 0.5
    center_y = (
        min(vertex.y for vertex in high_points)
        + max(vertex.y for vertex in high_points)
    ) * 0.5
    source_radius = (0.020 / 1.12) * 0.5
    bore_depth = 0.14
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=64,
        radius=source_radius,
        depth=bore_depth,
        end_fill_type="NGON",
        location=(center_x, center_y, maximum.z - bore_depth * 0.45),
    )
    cutter = bpy.context.object
    cutter.name = "Musket20mmBoreCutter"
    modifier = obj.modifiers.new(name="Musket20mmBore", type="BOOLEAN")
    modifier.operation = "DIFFERENCE"
    modifier.solver = "EXACT"
    modifier.object = cutter
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    cutter.select_set(False)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.data.objects.remove(cutter, do_unlink=True)
    for polygon in obj.data.polygons:
        if polygon.material_index >= original_material_count:
            polygon.material_index = max(0, original_material_count - 1)
    while len(obj.data.materials) > original_material_count:
        obj.data.materials.pop(index=len(obj.data.materials) - 1)
    return {
        "center": [center_x, center_y, maximum.z],
        "sourceRadius": source_radius,
        "displayDiameterMeters": source_radius * 2.0 * 1.12,
        "depth": bore_depth,
    }


def export_fbx(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=False,
        use_mesh_modifiers=False,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
    )


def slice_report(world_vertices, axis, minimum, maximum, high_end):
    threshold = minimum + (maximum - minimum) * (0.92 if high_end else 0.08)
    selected = [
        vertex for vertex in world_vertices
        if (vertex[axis] >= threshold if high_end else vertex[axis] <= threshold)
    ]
    perpendicular = [index for index in range(3) if index != axis]
    return {
        "count": len(selected),
        "minimum": [min(vertex[index] for vertex in selected) for index in perpendicular],
        "maximum": [max(vertex[index] for vertex in selected) for index in perpendicular],
    }


def inspect_scene(preview_dir=None):
    report = {"objects": []}
    for obj in bpy.context.scene.objects:
        item = {
            "name": obj.name,
            "type": obj.type,
            "location": list(obj.location),
            "rotationEuler": list(obj.rotation_euler),
            "scale": list(obj.scale),
            "dimensions": list(obj.dimensions),
        }
        if obj.type == "MESH":
            mesh = obj.data
            world_vertices = [obj.matrix_world @ vertex.co for vertex in mesh.vertices]
            minimum = Vector(
                tuple(min(vertex[index] for vertex in world_vertices) for index in range(3))
            )
            maximum = Vector(
                tuple(max(vertex[index] for vertex in world_vertices) for index in range(3))
            )
            item["vertexCount"] = len(mesh.vertices)
            item["polygonCount"] = len(mesh.polygons)
            item["worldMinimum"] = list(minimum)
            item["worldMaximum"] = list(maximum)
            item["worldSize"] = list(maximum - minimum)
            item["materials"] = [slot.material.name if slot.material else None for slot in obj.material_slots]
            long_axis = max(range(3), key=lambda index: (maximum - minimum)[index])
            item["longAxis"] = long_axis
            item["lowEndSlice"] = slice_report(
                world_vertices, long_axis, minimum[long_axis], maximum[long_axis], False
            )
            item["highEndSlice"] = slice_report(
                world_vertices, long_axis, minimum[long_axis], maximum[long_axis], True
            )
            item["highEndPoints"] = [
                list(vertex)
                for vertex in world_vertices
                if vertex[long_axis] >= maximum[long_axis] - 0.006
            ]
            if preview_dir:
                os.makedirs(preview_dir, exist_ok=True)
                low_center = (minimum + maximum) * 0.5
                high_center = low_center.copy()
                low_center[long_axis] = minimum[long_axis]
                high_center[long_axis] = maximum[long_axis]
                direction = Vector((0.0, 0.0, 0.0))
                direction[long_axis] = 1.0
                cross_size = max(
                    (maximum - minimum)[index] for index in range(3) if index != long_axis
                )
                render_preview(
                    obj,
                    os.path.join(preview_dir, "musket_low_end.png"),
                    low_center,
                    -direction,
                    cross_size * 1.6,
                )
                render_preview(
                    obj,
                    os.path.join(preview_dir, "musket_high_end.png"),
                    high_center,
                    direction,
                    cross_size * 1.6,
                )
        report["objects"].append(item)
    print("MUSKET_BORE_INSPECTION=" + json.dumps(report, ensure_ascii=False))


def main():
    args = parse_args()
    if args.preview_dir and not os.path.isabs(args.preview_dir):
        raise ValueError("--preview-dir must be an absolute path inside the approved project.")
    if args.output and not os.path.isabs(args.output):
        raise ValueError("--output must be an absolute path inside the approved project.")
    import_fbx(args.input)
    if args.inspect_only:
        inspect_scene(args.preview_dir)
        return
    if not args.output:
        raise RuntimeError("Bore generation requires --output.")
    bore = create_bore()
    inspect_scene(args.preview_dir)
    export_fbx(args.output)
    print("MUSKET_BORE_GENERATED=" + json.dumps(bore, ensure_ascii=False))


if __name__ == "__main__":
    main()
