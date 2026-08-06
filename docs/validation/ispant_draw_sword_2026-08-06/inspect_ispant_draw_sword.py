import os

import bpy


PATHS = (
    r"D:\Bellerophon2\Bellerophon\enemies model\išpant draw sword.fbx",
    r"D:\Bellerophon2\Bellerophon\enemies model\Ispant_Static.fbx",
)


def connected_components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        first, second = edge.vertices
        adjacency[first].add(second)
        adjacency[second].add(first)

    vertex_polygons = [[] for _ in mesh.vertices]
    for polygon in mesh.polygons:
        for vertex_index in polygon.vertices:
            vertex_polygons[vertex_index].append(polygon.index)

    seen = set()
    result = []
    for start in range(len(mesh.vertices)):
        if start in seen:
            continue
        stack = [start]
        seen.add(start)
        vertices = []
        polygon_indices = set()
        while stack:
            vertex_index = stack.pop()
            vertices.append(vertex_index)
            polygon_indices.update(vertex_polygons[vertex_index])
            for neighbour in adjacency[vertex_index]:
                if neighbour not in seen:
                    seen.add(neighbour)
                    stack.append(neighbour)

        coordinates = [mesh.vertices[index].co for index in vertices]
        minimum = tuple(round(min(value[axis] for value in coordinates), 6) for axis in range(3))
        maximum = tuple(round(max(value[axis] for value in coordinates), 6) for axis in range(3))
        material_counts = {}
        for polygon_index in polygon_indices:
            material_index = mesh.polygons[polygon_index].material_index
            material_counts[material_index] = material_counts.get(material_index, 0) + 1
        result.append(
            (
                len(vertices),
                len(polygon_indices),
                minimum,
                maximum,
                tuple(sorted(material_counts.items())),
            )
        )
    return sorted(result, reverse=True)


for path in PATHS:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    print("FILE|" + os.path.basename(path))
    for action in bpy.data.actions:
        print(
            "ACTION|{}|range={}|slots={}".format(
                action.name,
                tuple(action.frame_range),
                len(getattr(action, "slots", [])),
            )
        )
    for obj in sorted(bpy.context.scene.objects, key=lambda item: item.name):
        parent = obj.parent.name if obj.parent else "NONE"
        print(
            "OBJECT|{}|type={}|parent={}|loc={}|rot={}|scale={}".format(
                obj.name,
                obj.type,
                parent,
                tuple(round(value, 6) for value in obj.location),
                tuple(round(value, 6) for value in obj.rotation_euler),
                tuple(round(value, 6) for value in obj.scale),
            )
        )
        if obj.type == "ARMATURE":
            print("BONES|{}|{}".format(obj.name, ",".join(bone.name for bone in obj.data.bones)))
        if obj.type == "MESH":
            mesh = obj.data
            materials = [
                slot.material.name if slot.material else "NULL"
                for slot in obj.material_slots
            ]
            triangle_count = sum(len(polygon.vertices) - 2 for polygon in mesh.polygons)
            print(
                "MESH|{}|verts={}|polys={}|tris={}|materials={}|groups={}".format(
                    obj.name,
                    len(mesh.vertices),
                    len(mesh.polygons),
                    triangle_count,
                    materials,
                    [group.name for group in obj.vertex_groups],
                )
            )
            print("COMPONENTS|{}|{}".format(obj.name, connected_components(mesh)))
