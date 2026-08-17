import sys

import bpy


def tuple_text(values):
    return ",".join(f"{value:.6f}" for value in values)


source_path = sys.argv[-1]
bpy.ops.import_scene.fbx(filepath=source_path)

print("===FBX_OBJECTS===")
for obj in bpy.context.scene.objects:
    print(
        "OBJ"
        f"|name={obj.name}"
        f"|type={obj.type}"
        f"|parent={obj.parent.name if obj.parent else '<none>'}"
        f"|location={tuple_text(obj.location)}"
        f"|rotation={tuple_text(obj.rotation_euler)}"
        f"|scale={tuple_text(obj.scale)}"
    )

print("===MESH_DETAILS===")
for obj in bpy.context.scene.objects:
    if obj.type != "MESH":
        continue
    armature = next(
        (
            modifier.object.name
            for modifier in obj.modifiers
            if modifier.type == "ARMATURE" and modifier.object is not None
        ),
        "<none>",
    )
    materials = ",".join(
        material.name if material is not None else "<null>"
        for material in obj.data.materials
    )
    print(
        "MESH"
        f"|name={obj.name}"
        f"|vertices={len(obj.data.vertices)}"
        f"|polygons={len(obj.data.polygons)}"
        f"|materials={materials}"
        f"|vertexGroups={len(obj.vertex_groups)}"
        f"|armature={armature}"
    )

print("===ARMATURE_DETAILS===")
for obj in bpy.context.scene.objects:
    if obj.type != "ARMATURE":
        continue
    print(
        "ARMATURE"
        f"|name={obj.name}"
        f"|bones={len(obj.data.bones)}"
        f"|boneNames={','.join(bone.name for bone in obj.data.bones)}"
    )

print("===MESH_CONNECTED_COMPONENTS===")
for obj in bpy.context.scene.objects:
    if obj.type != "MESH" or len(obj.data.vertices) == 0:
        continue
    parents = list(range(len(obj.data.vertices)))

    def find(index):
        while parents[index] != index:
            parents[index] = parents[parents[index]]
            index = parents[index]
        return index

    def union(left, right):
        left_root = find(left)
        right_root = find(right)
        if left_root != right_root:
            parents[right_root] = left_root

    for edge in obj.data.edges:
        union(edge.vertices[0], edge.vertices[1])

    components = {}
    for vertex in obj.data.vertices:
        components.setdefault(find(vertex.index), []).append(vertex.index)

    ordered_components = sorted(
        components.values(), key=lambda indices: len(indices), reverse=True
    )
    for component_index, vertex_indices in enumerate(ordered_components):
        index_set = set(vertex_indices)
        coordinates = [obj.data.vertices[index].co for index in vertex_indices]
        minimum = tuple(min(co[axis] for co in coordinates) for axis in range(3))
        maximum = tuple(max(co[axis] for co in coordinates) for axis in range(3))
        dimensions = tuple(maximum[axis] - minimum[axis] for axis in range(3))
        polygons = [
            polygon
            for polygon in obj.data.polygons
            if polygon.vertices[0] in index_set
        ]
        print(
            "COMPONENT"
            f"|mesh={obj.name}"
            f"|index={component_index}"
            f"|vertices={len(vertex_indices)}"
            f"|polygons={len(polygons)}"
            f"|minimum={tuple_text(minimum)}"
            f"|maximum={tuple_text(maximum)}"
            f"|dimensions={tuple_text(dimensions)}"
            f"|materialIndices={','.join(str(value) for value in sorted(set(p.material_index for p in polygons)))}"
        )

print("===ACTIONS===")
for action in bpy.data.actions:
    print(
        "ACTION"
        f"|name={action.name}"
        f"|frameRange={tuple_text(action.frame_range)}"
        f"|slots={len(action.slots)}"
    )

print("===DOMINANT_VERTEX_GROUP_BOUNDS===")
for obj in bpy.context.scene.objects:
    if obj.type != "MESH" or len(obj.vertex_groups) == 0:
        continue
    grouped_vertices = {}
    for vertex in obj.data.vertices:
        if not vertex.groups:
            group_name = "<none>"
        else:
            dominant = max(vertex.groups, key=lambda value: value.weight)
            group_name = obj.vertex_groups[dominant.group].name
        grouped_vertices.setdefault(group_name, []).append(vertex)
    for group_name, vertices in sorted(
        grouped_vertices.items(), key=lambda item: len(item[1]), reverse=True
    ):
        world_coordinates = [obj.matrix_world @ vertex.co for vertex in vertices]
        minimum = tuple(
            min(coordinate[axis] for coordinate in world_coordinates)
            for axis in range(3)
        )
        maximum = tuple(
            max(coordinate[axis] for coordinate in world_coordinates)
            for axis in range(3)
        )
        print(
            "DOMINANT_GROUP"
            f"|mesh={obj.name}"
            f"|name={group_name}"
            f"|vertices={len(vertices)}"
            f"|minimum={tuple_text(minimum)}"
            f"|maximum={tuple_text(maximum)}"
        )

print("===DOMINANT_FACE_REGION_BOUNDS===")
for obj in bpy.context.scene.objects:
    if obj.type != "MESH" or len(obj.vertex_groups) == 0:
        continue
    dominant_names = {}
    for vertex in obj.data.vertices:
        if vertex.groups:
            dominant = max(vertex.groups, key=lambda value: value.weight)
            dominant_names[vertex.index] = obj.vertex_groups[dominant.group].name
        else:
            dominant_names[vertex.index] = "<none>"
    face_labels = {}
    for polygon in obj.data.polygons:
        names = [dominant_names[index] for index in polygon.vertices]
        face_labels[polygon.index] = max(set(names), key=names.count)
    edge_faces = {}
    for polygon in obj.data.polygons:
        vertices = list(polygon.vertices)
        for index in range(len(vertices)):
            edge = tuple(sorted((vertices[index], vertices[(index + 1) % len(vertices)])))
            edge_faces.setdefault(edge, []).append(polygon.index)
    adjacency = {polygon.index: set() for polygon in obj.data.polygons}
    for face_indices in edge_faces.values():
        for left in face_indices:
            for right in face_indices:
                if left != right and face_labels[left] == face_labels[right]:
                    adjacency[left].add(right)
    unvisited = set(adjacency)
    regions = []
    while unvisited:
        seed = unvisited.pop()
        stack = [seed]
        region = {seed}
        while stack:
            current = stack.pop()
            for neighbor in adjacency[current]:
                if neighbor in unvisited:
                    unvisited.remove(neighbor)
                    region.add(neighbor)
                    stack.append(neighbor)
        regions.append(region)
    for label in sorted(set(face_labels.values())):
        matching = [region for region in regions if face_labels[next(iter(region))] == label]
        matching.sort(key=len, reverse=True)
        for region_index, region in enumerate(matching):
            if len(region) < 3:
                continue
            vertex_indices = sorted(
                set(
                    vertex_index
                    for face_index in region
                    for vertex_index in obj.data.polygons[face_index].vertices
                )
            )
            world_coordinates = [
                obj.matrix_world @ obj.data.vertices[index].co
                for index in vertex_indices
            ]
            minimum = tuple(
                min(coordinate[axis] for coordinate in world_coordinates)
                for axis in range(3)
            )
            maximum = tuple(
                max(coordinate[axis] for coordinate in world_coordinates)
                for axis in range(3)
            )
            print(
                "FACE_REGION"
                f"|mesh={obj.name}"
                f"|label={label}"
                f"|index={region_index}"
                f"|faces={len(region)}"
                f"|vertices={len(vertex_indices)}"
                f"|minimum={tuple_text(minimum)}"
                f"|maximum={tuple_text(maximum)}"
            )

print("===UV_ISLAND_BOUNDS===")
for obj in bpy.context.scene.objects:
    if obj.type != "MESH" or obj.data.uv_layers.active is None:
        continue
    uv_data = obj.data.uv_layers.active.data
    edge_records = {}
    for polygon in obj.data.polygons:
        polygon_vertices = list(polygon.vertices)
        polygon_loops = list(polygon.loop_indices)
        for offset in range(len(polygon_vertices)):
            left_vertex = polygon_vertices[offset]
            right_vertex = polygon_vertices[(offset + 1) % len(polygon_vertices)]
            left_loop = polygon_loops[offset]
            right_loop = polygon_loops[(offset + 1) % len(polygon_loops)]
            edge = tuple(sorted((left_vertex, right_vertex)))
            uv_by_vertex = {
                left_vertex: tuple(round(value, 6) for value in uv_data[left_loop].uv),
                right_vertex: tuple(round(value, 6) for value in uv_data[right_loop].uv),
            }
            edge_records.setdefault(edge, []).append((polygon.index, uv_by_vertex))
    adjacency = {polygon.index: set() for polygon in obj.data.polygons}
    for records in edge_records.values():
        if len(records) != 2:
            continue
        (left_face, left_uv), (right_face, right_uv) = records
        shared_vertices = set(left_uv) & set(right_uv)
        if all(left_uv[index] == right_uv[index] for index in shared_vertices):
            adjacency[left_face].add(right_face)
            adjacency[right_face].add(left_face)
    unvisited = set(adjacency)
    islands = []
    while unvisited:
        seed = unvisited.pop()
        stack = [seed]
        island = {seed}
        while stack:
            current = stack.pop()
            for neighbor in adjacency[current]:
                if neighbor in unvisited:
                    unvisited.remove(neighbor)
                    island.add(neighbor)
                    stack.append(neighbor)
        islands.append(island)
    islands.sort(key=len, reverse=True)
    for island_index, island in enumerate(islands):
        if len(island) < 8:
            continue
        vertex_indices = sorted(
            set(
                vertex_index
                for face_index in island
                for vertex_index in obj.data.polygons[face_index].vertices
            )
        )
        world_coordinates = [
            obj.matrix_world @ obj.data.vertices[index].co
            for index in vertex_indices
        ]
        minimum = tuple(
            min(coordinate[axis] for coordinate in world_coordinates)
            for axis in range(3)
        )
        maximum = tuple(
            max(coordinate[axis] for coordinate in world_coordinates)
            for axis in range(3)
        )
        print(
            "UV_ISLAND"
            f"|mesh={obj.name}"
            f"|index={island_index}"
            f"|faces={len(island)}"
            f"|vertices={len(vertex_indices)}"
            f"|minimum={tuple_text(minimum)}"
            f"|maximum={tuple_text(maximum)}"
        )
