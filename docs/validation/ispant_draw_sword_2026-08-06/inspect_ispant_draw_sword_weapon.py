import bpy
from mathutils import Vector


SOURCE = r"D:\Bellerophon2\Bellerophon\enemies model\išpant draw sword.fbx"
SWORD_COMPONENTS = (77, 78, 79, 80)


def connected_components(mesh):
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
            for neighbour in adjacency[current]:
                if neighbour in pending:
                    pending.remove(neighbour)
                    stack.append(neighbour)
        result.append(set(vertices))
    return result


def component_polygons(mesh, component):
    return [
        polygon
        for polygon in mesh.polygons
        if all(vertex_index in component for vertex_index in polygon.vertices)
    ]


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SOURCE)
armature = bpy.data.objects["Armature"]
body = bpy.data.objects["Ispant_Armed_Body"]
components = connected_components(body.data)
print("COMPONENT_COUNT|{}".format(len(components)))
for component_index in SWORD_COMPONENTS:
    component = components[component_index]
    polygons = component_polygons(body.data, component)
    coordinates = [body.data.vertices[index].co for index in component]
    minimum = tuple(round(min(value[axis] for value in coordinates), 6) for axis in range(3))
    maximum = tuple(round(max(value[axis] for value in coordinates), 6) for axis in range(3))
    center = tuple(
        round(sum(value[axis] for value in coordinates) / len(coordinates), 6)
        for axis in range(3)
    )
    material_counts = {}
    for polygon in polygons:
        material = body.material_slots[polygon.material_index].material
        material_name = material.name if material else "NULL"
        material_counts[material_name] = material_counts.get(material_name, 0) + 1
    weights = {}
    for vertex_index in component:
        vertex = body.data.vertices[vertex_index]
        for group_weight in vertex.groups:
            group_name = body.vertex_groups[group_weight.group].name
            record = weights.setdefault(group_name, [0, 0.0, 0.0])
            record[0] += 1
            record[1] += group_weight.weight
            record[2] = max(record[2], group_weight.weight)
    weight_summary = {
        name: (count, round(total / count, 6), round(maximum_weight, 6))
        for name, (count, total, maximum_weight) in sorted(weights.items())
    }
    print(
        "COMPONENT|{}|verts={}|polys={}|bounds={}..{}|center={}|materials={}|weights={}".format(
            component_index,
            len(component),
            len(polygons),
            minimum,
            maximum,
            center,
            material_counts,
            weight_summary,
        )
    )

sword_vertices = set().union(*(components[index] for index in SWORD_COMPONENTS))
scene = bpy.context.scene
scene.frame_start = 1
scene.frame_end = 46
for frame in (1, 12, 23, 34, 46):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    pose_hand = armature.pose.bones["mixamorig:RightHand"]
    pose_index = armature.pose.bones["mixamorig:RightHandIndex1"]
    hand_head = armature.matrix_world @ pose_hand.head
    hand_tail = armature.matrix_world @ pose_hand.tail
    index_head = armature.matrix_world @ pose_index.head
    evaluated_body = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    evaluated_mesh = evaluated_body.to_mesh()
    try:
        base_world_vertices = {
            index: body.matrix_world @ body.data.vertices[index].co
            for index in sword_vertices
        }
        evaluated_world_vertices = {
            index: evaluated_body.matrix_world @ evaluated_mesh.vertices[index].co
            for index in sword_vertices
        }
        if frame == 1:
            for first_component in SWORD_COMPONENTS:
                for second_component in SWORD_COMPONENTS:
                    if second_component <= first_component:
                        continue
                    base_gap = min(
                        (base_world_vertices[first] - base_world_vertices[second]).length
                        for first in components[first_component]
                        for second in components[second_component]
                    )
                    evaluated_gap = min(
                        (evaluated_world_vertices[first] - evaluated_world_vertices[second]).length
                        for first in components[first_component]
                        for second in components[second_component]
                    )
                    print(
                        "PAIR|{}-{}|base_min_gap={}|frame1_min_gap={}".format(
                            first_component,
                            second_component,
                            round(base_gap, 6),
                            round(evaluated_gap, 6),
                        )
                    )
        world_vertices = [
            evaluated_world_vertices[index]
            for index in sword_vertices
        ]
        minimum_hand_distance = min((vertex - hand_head).length for vertex in world_vertices)
        minimum_base_hand_distance = min(
            (vertex - hand_head).length for vertex in base_world_vertices.values()
        )
        maximum_edge_length_error = 0.0
        maximum_edge_relative_error = 0.0
        for edge in body.data.edges:
            first, second = edge.vertices
            if first not in sword_vertices or second not in sword_vertices:
                continue
            base_length = (base_world_vertices[first] - base_world_vertices[second]).length
            evaluated_length = (
                evaluated_world_vertices[first] - evaluated_world_vertices[second]
            ).length
            error = abs(evaluated_length - base_length)
            maximum_edge_length_error = max(maximum_edge_length_error, error)
            if base_length > 0.000001:
                maximum_edge_relative_error = max(
                    maximum_edge_relative_error,
                    error / base_length,
                )
        center = sum(world_vertices, Vector()) / len(world_vertices)
        print(
            "FRAME|{}|hand_head={}|hand_tail={}|index_head={}|sword_center={}|nearest_to_hand={}|base_nearest_to_hand={}|max_edge_error={}|max_edge_relative_error={}".format(
                frame,
                tuple(round(value, 6) for value in hand_head),
                tuple(round(value, 6) for value in hand_tail),
                tuple(round(value, 6) for value in index_head),
                tuple(round(value, 6) for value in center),
                round(minimum_hand_distance, 6),
                round(minimum_base_hand_distance, 6),
                round(maximum_edge_length_error, 9),
                round(maximum_edge_relative_error, 6),
            )
        )
        if frame == 1:
            component_by_vertex = {
                vertex_index: component_index
                for component_index in SWORD_COMPONENTS
                for vertex_index in components[component_index]
            }
            nearest = sorted(
                sword_vertices,
                key=lambda index: (evaluated_world_vertices[index] - hand_head).length,
            )[:12]
            for vertex_index in nearest:
                vertex = body.data.vertices[vertex_index]
                weights = sorted(
                    (
                        body.vertex_groups[group_weight.group].name,
                        round(group_weight.weight, 6),
                    )
                    for group_weight in vertex.groups
                )
                print(
                    "NEAREST|vertex={}|component={}|distance={}|base={}|evaluated={}|weights={}".format(
                        vertex_index,
                        component_by_vertex[vertex_index],
                        round((evaluated_world_vertices[vertex_index] - hand_head).length, 6),
                        tuple(round(value, 6) for value in base_world_vertices[vertex_index]),
                        tuple(round(value, 6) for value in evaluated_world_vertices[vertex_index]),
                        weights,
                    )
                )
    finally:
        evaluated_body.to_mesh_clear()
