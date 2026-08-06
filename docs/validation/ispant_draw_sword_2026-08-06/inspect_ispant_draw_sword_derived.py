import bpy
from mathutils import Matrix, Vector


SOURCE = r"D:\Bellerophon2\Bellerophon\Assets\_Project\Art\Enemies\Ispant\Animations\Ispant_DrawSword.fbx"


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


def matrix_error(first, second):
    return max(
        abs(first[row][column] - second[row][column])
        for row in range(4)
        for column in range(4)
    )


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SOURCE)
armature = bpy.data.objects["Armature"]
body = bpy.data.objects["Ispant_Armed_Body"]
musket = bpy.data.objects["Ispant_DrawSword_RigidMusket"]
sheathe = bpy.data.objects["Ispant_DrawSword_RigidSheath"]
sword = bpy.data.objects["Ispant_DrawSword_RigidSword"]
components = connected_components(sword.data)
print(
    "STRUCTURE|actions={}|bones={}|body_verts={}|body_tris={}|musket_verts={}|musket_tris={}|musket_parent={}|musket_parent_type={}|musket_modifiers={}|musket_groups={}|sheath_verts={}|sheath_tris={}|sheath_parent={}|sheath_parent_type={}|sheath_modifiers={}|sheath_groups={}|sword_verts={}|sword_tris={}|sword_components={}|sword_parent={}|sword_parent_type={}|modifiers={}|groups={}".format(
        [(action.name, tuple(action.frame_range)) for action in bpy.data.actions],
        len(armature.data.bones),
        len(body.data.vertices),
        sum(len(polygon.vertices) - 2 for polygon in body.data.polygons),
        len(musket.data.vertices),
        sum(len(polygon.vertices) - 2 for polygon in musket.data.polygons),
        musket.parent_bone,
        musket.parent_type,
        [modifier.type for modifier in musket.modifiers],
        [group.name for group in musket.vertex_groups],
        len(sheathe.data.vertices),
        sum(len(polygon.vertices) - 2 for polygon in sheathe.data.polygons),
        sheathe.parent_bone,
        sheathe.parent_type,
        [modifier.type for modifier in sheathe.modifiers],
        [group.name for group in sheathe.vertex_groups],
        len(sword.data.vertices),
        sum(len(polygon.vertices) - 2 for polygon in sword.data.polygons),
        [(len(component), sum(1 for polygon in sword.data.polygons if polygon.vertices[0] in component)) for component in components],
        sword.parent_bone,
        sword.parent_type,
        [modifier.type for modifier in sword.modifiers],
        [group.name for group in sword.vertex_groups],
    )
)

scene = bpy.context.scene
scene.frame_start = 1
scene.frame_end = 46
initial_attachment = None
maximum_attachment_error = 0.0
minimum_handle_distance = float("inf")
maximum_handle_distance = 0.0
for frame in range(1, 47):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    hand = armature.pose.bones["mixamorig:RightHand"]
    hand_head = armature.matrix_world @ hand.head
    hand_tail = armature.matrix_world @ hand.tail
    hand_center = (hand_head + hand_tail) * 0.5
    sword_vertices = [sword.matrix_world @ vertex.co for vertex in sword.data.vertices]
    handle_vertices = [
        sword.matrix_world @ sword.data.vertices[index].co
        for index in components[1]
    ]
    handle_center = sum(handle_vertices, Vector()) / len(handle_vertices)
    handle_distance = (handle_center - hand_center).length
    minimum_handle_distance = min(minimum_handle_distance, handle_distance)
    maximum_handle_distance = max(maximum_handle_distance, handle_distance)
    hand_world = armature.matrix_world @ hand.matrix
    attachment = hand_world.inverted() @ sword.matrix_world
    if initial_attachment is None:
        initial_attachment = attachment.copy()
    maximum_attachment_error = max(
        maximum_attachment_error,
        matrix_error(initial_attachment, attachment),
    )
    if frame in (1, 12, 23, 34, 46):
        print(
            "FRAME|{}|hand_center={}|handle_center={}|handle_distance={}|sword_nearest_to_hand={}".format(
                frame,
                tuple(round(value, 6) for value in hand_center),
                tuple(round(value, 6) for value in handle_center),
                round(handle_distance, 6),
                round(min((vertex - hand_center).length for vertex in sword_vertices), 6),
            )
        )

print(
    "RESULT|minimum_handle_distance={}|maximum_handle_distance={}|maximum_attachment_error={}".format(
        minimum_handle_distance,
        maximum_handle_distance,
        maximum_attachment_error,
    )
)
