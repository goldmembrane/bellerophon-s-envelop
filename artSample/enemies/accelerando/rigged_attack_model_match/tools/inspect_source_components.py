import bpy
from collections import deque


SOURCE_PATH = r"D:\Bellerophon2\Bellerophon\enemies model\accelerando.glb"


def format_vec(values):
    return "(" + ", ".join(f"{value:.5f}" for value in values) + ")"


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SOURCE_PATH)

mesh_object = bpy.data.objects.get("Mesh1.0")
if mesh_object is None or mesh_object.type != "MESH":
    raise RuntimeError("Mesh1.0 was not imported as a mesh.")

mesh = mesh_object.data
neighbors = [set() for _ in mesh.vertices]
vertex_polygons = [[] for _ in mesh.vertices]
for polygon in mesh.polygons:
    for vertex_index in polygon.vertices:
        vertex_polygons[vertex_index].append(polygon.index)
    for edge_key in polygon.edge_keys:
        first, second = edge_key
        neighbors[first].add(second)
        neighbors[second].add(first)

remaining = set(range(len(mesh.vertices)))
components = []
while remaining:
    start = remaining.pop()
    queue = deque([start])
    vertices = {start}
    while queue:
        current = queue.popleft()
        for neighbor in neighbors[current]:
            if neighbor in remaining:
                remaining.remove(neighbor)
                vertices.add(neighbor)
                queue.append(neighbor)
    polygon_indices = set()
    for vertex_index in vertices:
        polygon_indices.update(vertex_polygons[vertex_index])
    coordinates = [mesh.vertices[index].co for index in vertices]
    minimum = tuple(min(coordinate[axis] for coordinate in coordinates) for axis in range(3))
    maximum = tuple(max(coordinate[axis] for coordinate in coordinates) for axis in range(3))
    group_totals = {}
    for vertex_index in vertices:
        for membership in mesh.vertices[vertex_index].groups:
            group_name = mesh_object.vertex_groups[membership.group].name
            group_totals[group_name] = group_totals.get(group_name, 0.0) + membership.weight
    dominant_groups = sorted(group_totals.items(), key=lambda item: item[1], reverse=True)[:6]
    components.append(
        {
            "vertices": len(vertices),
            "polygons": len(polygon_indices),
            "minimum": minimum,
            "maximum": maximum,
            "groups": dominant_groups,
        }
    )

components.sort(key=lambda item: item["vertices"], reverse=True)
print("ACCELERANDO_COMPONENT_REPORT_BEGIN")
print(f"object={mesh_object.name} vertices={len(mesh.vertices)} polygons={len(mesh.polygons)} components={len(components)}")
for index, component in enumerate(components):
    groups = ", ".join(f"{name}:{weight:.2f}" for name, weight in component["groups"])
    print(
        f"component[{index:02d}] vertices={component['vertices']} polygons={component['polygons']} "
        f"min={format_vec(component['minimum'])} max={format_vec(component['maximum'])} groups=[{groups}]"
    )
print("ACCELERANDO_COMPONENT_REPORT_END")

armature_object = bpy.data.objects.get("UniRigArmature")
if armature_object is None or armature_object.type != "ARMATURE":
    raise RuntimeError("UniRigArmature was not imported as an armature.")

print("ACCELERANDO_BONE_REPORT_BEGIN")
print(f"armature_matrix_world={tuple(round(value, 5) for row in armature_object.matrix_world for value in row)}")
print(f"mesh_matrix_world={tuple(round(value, 5) for row in mesh_object.matrix_world for value in row)}")
for bone in armature_object.data.bones:
    parent_name = bone.parent.name if bone.parent else "None"
    print(
        f"bone={bone.name} parent={parent_name} "
        f"head={format_vec(bone.head_local)} tail={format_vec(bone.tail_local)} length={bone.length:.5f}"
    )
print("ACCELERANDO_BONE_REPORT_END")

print("ACCELERANDO_SPATIAL_FACE_REPORT_BEGIN")
regions = {
    "display_plate": lambda center: center.z < 0.145 and max(abs(center.x), abs(center.y)) > 1.25,
    "positive_rod": lambda center: 0.965 < center.x < 1.125 and -1.52 < center.y < -0.88 and 0.18 < center.z < 1.22,
    "negative_rod": lambda center: -1.125 < center.x < -0.965 and -1.52 < center.y < -0.88 and 0.18 < center.z < 1.22,
    "positive_mace": lambda center: center.x > 1.18 and center.y < -0.18 and center.z < 1.10,
    "negative_mace": lambda center: center.x < -1.18 and center.y < -0.18 and center.z < 1.10,
}
for region_name, predicate in regions.items():
    polygon_indices = [polygon.index for polygon in mesh.polygons if predicate(polygon.center)]
    vertex_indices = {vertex_index for polygon_index in polygon_indices for vertex_index in mesh.polygons[polygon_index].vertices}
    group_totals = {}
    for vertex_index in vertex_indices:
        for membership in mesh.vertices[vertex_index].groups:
            group_name = mesh_object.vertex_groups[membership.group].name
            group_totals[group_name] = group_totals.get(group_name, 0.0) + membership.weight
    dominant_groups = sorted(group_totals.items(), key=lambda item: item[1], reverse=True)[:8]
    groups = ", ".join(f"{name}:{weight:.2f}" for name, weight in dominant_groups)
    print(f"region={region_name} polygons={len(polygon_indices)} vertices={len(vertex_indices)} groups=[{groups}]")
print("ACCELERANDO_SPATIAL_FACE_REPORT_END")
