import bpy
import json
import os
import sys


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


args = sys.argv[sys.argv.index("--") + 1:]
require(len(args) == 2, "Expected <approved_fbx> <output_json>")
fbx_path, output_path = map(os.path.abspath, args)
os.makedirs(os.path.dirname(output_path), exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx_path, use_anim=False)

armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
require(len(armatures) == 1, f"Expected one armature, found {len(armatures)}")
require(len(meshes) == 1, f"Expected one mesh, found {len(meshes)}")
armature = armatures[0]
mesh_object = meshes[0]

material_names = [slot.material.name if slot.material else "None" for slot in mesh_object.material_slots]
blade_material_indices = [index for index, name in enumerate(material_names) if "hookblade" in name.lower()]
require(len(blade_material_indices) == 1, f"Expected one HookBlade material, found {material_names}")
blade_material_index = blade_material_indices[0]

blade_vertex_indices = sorted({
    vertex_index
    for polygon in mesh_object.data.polygons
    if polygon.material_index == blade_material_index
    for vertex_index in polygon.vertices
})
non_blade_vertex_indices = {
    vertex_index
    for polygon in mesh_object.data.polygons
    if polygon.material_index != blade_material_index
    for vertex_index in polygon.vertices
}
shared_boundary_vertex_indices = sorted(set(blade_vertex_indices) & non_blade_vertex_indices)

group_names = {group.index: group.name for group in mesh_object.vertex_groups}
left_groups = {index for index, name in group_names.items() if name.startswith("Left")}
right_groups = {index for index, name in group_names.items() if name.startswith("Right")}

left_vertices = []
right_vertices = []
blade_weight_totals = {name: 0.0 for name in group_names.values()}
for vertex_index in blade_vertex_indices:
    vertex = mesh_object.data.vertices[vertex_index]
    for item in vertex.groups:
        blade_weight_totals[group_names[item.group]] += item.weight
    left_weight = sum(item.weight for item in vertex.groups if item.group in left_groups)
    right_weight = sum(item.weight for item in vertex.groups if item.group in right_groups)
    world_position = mesh_object.matrix_world @ vertex.co
    entry = [world_position.x, world_position.y, world_position.z]
    if left_weight > right_weight:
        left_vertices.append(entry)
    elif right_weight > left_weight:
        right_vertices.append(entry)


def bounds(points):
    require(points, "Blade side contains no vertices")
    minimum = [min(point[axis] for point in points) for axis in range(3)]
    maximum = [max(point[axis] for point in points) for axis in range(3)]
    size = [maximum[axis] - minimum[axis] for axis in range(3)]
    return {"min": minimum, "max": maximum, "size": size}


result = {
    "fbx": fbx_path,
    "armature": armature.name,
    "mesh": mesh_object.name,
    "materials": material_names,
    "blade_material_index": blade_material_index,
    "blade_vertex_count": len(blade_vertex_indices),
    "shared_boundary_vertex_count": len(shared_boundary_vertex_indices),
    "shared_boundary_vertex_indices": shared_boundary_vertex_indices,
    "left_blade_vertex_count": len(left_vertices),
    "right_blade_vertex_count": len(right_vertices),
    "left_blade_bounds": bounds(left_vertices),
    "right_blade_bounds": bounds(right_vertices),
    "blade_weight_totals": dict(sorted(blade_weight_totals.items(), key=lambda item: item[1], reverse=True)),
    "bones": [bone.name for bone in armature.data.bones],
    "vertex_groups": [group.name for group in mesh_object.vertex_groups],
}
with open(output_path, "w", encoding="utf-8") as handle:
    json.dump(result, handle, ensure_ascii=False, indent=2)

print(json.dumps({
    "blade_vertices": len(blade_vertex_indices),
    "shared_boundary_vertices": len(shared_boundary_vertex_indices),
    "left": len(left_vertices),
    "right": len(right_vertices),
    "output": output_path,
}, ensure_ascii=False))
