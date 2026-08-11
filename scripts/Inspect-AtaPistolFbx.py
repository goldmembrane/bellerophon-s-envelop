import bpy
import os
import sys


def fmt(values):
    return "(" + ",".join(f"{value:.6f}" for value in values) + ")"


def loose_components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        left, right = edge.vertices
        adjacency[left].add(right)
        adjacency[right].add(left)

    remaining = set(range(len(mesh.vertices)))
    components = []
    while remaining:
        seed = remaining.pop()
        found = {seed}
        stack = [seed]
        while stack:
            current = stack.pop()
            for other in adjacency[current]:
                if other in remaining:
                    remaining.remove(other)
                    found.add(other)
                    stack.append(other)
        components.append(found)
    return components


def component_description(mesh, component):
    points = [mesh.vertices[index].co for index in component]
    minimum = [min(point[axis] for point in points) for axis in range(3)]
    maximum = [max(point[axis] for point in points) for axis in range(3)]
    center = [(minimum[axis] + maximum[axis]) * 0.5 for axis in range(3)]
    size = [maximum[axis] - minimum[axis] for axis in range(3)]
    polygons = sum(
        1 for polygon in mesh.polygons if polygon.vertices[0] in component
    )
    return len(component), polygons, center, size


args = sys.argv[sys.argv.index("--") + 1 :]
if len(args) != 1:
    raise RuntimeError("Expected one FBX path argument.")

source = os.path.abspath(args[0])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source, use_anim=True)

print(f"ATA_FBX_SOURCE={source}")
print(f"OBJECT_COUNT={len(bpy.data.objects)}")
for obj in sorted(bpy.data.objects, key=lambda value: value.name):
    parent = obj.parent.name if obj.parent else "None"
    print(
        "OBJECT="
        + obj.name
        + f"|TYPE={obj.type}|PARENT={parent}|LOCATION={fmt(obj.location)}"
    )
    if obj.type == "MESH":
        mesh = obj.data
        materials = ",".join(
            slot.material.name if slot.material else "None" for slot in obj.material_slots
        )
        groups = ",".join(group.name for group in obj.vertex_groups)
        print(
            f"MESH={mesh.name}|VERTICES={len(mesh.vertices)}|EDGES={len(mesh.edges)}"
            f"|POLYGONS={len(mesh.polygons)}|MATERIALS={materials}|VERTEX_GROUPS={groups}"
        )
        components = loose_components(mesh)
        descriptions = []
        for rank, component in enumerate(
            sorted(components, key=len, reverse=True)[:80]
        ):
            vertices, polygons, center, size = component_description(mesh, component)
            descriptions.append(
                f"C{rank}:V{vertices}:P{polygons}:Center{fmt(center)}:Size{fmt(size)}"
            )
        print(f"LOOSE_COMPONENT_COUNT={len(components)}|" + ";".join(descriptions))
        print(
            "CUSTOM_PROPERTIES="
            + ",".join(f"{key}={obj[key]}" for key in obj.keys() if key != "_RNA_UI")
        )
    elif obj.type == "ARMATURE":
        print("BONES=" + ",".join(bone.name for bone in obj.data.bones))

print("ACTIONS=" + ",".join(action.name for action in bpy.data.actions))
