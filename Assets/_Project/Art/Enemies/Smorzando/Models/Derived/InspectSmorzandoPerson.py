import bpy
import sys
from mathutils import Vector


def argument_after_double_dash():
    if "--" not in sys.argv:
        raise RuntimeError("Source FBX path is required after --")
    return sys.argv[sys.argv.index("--") + 1]


source_path = argument_after_double_dash()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source_path)
bpy.context.view_layer.update()

print("SMORZANDO_IMPORT_OBJECTS_BEGIN")
for obj in bpy.context.scene.objects:
    if obj.type != "MESH":
        print(f"OBJECT name={obj.name} type={obj.type}")
        continue

    world_corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    minimum = tuple(min(corner[axis] for corner in world_corners) for axis in range(3))
    maximum = tuple(max(corner[axis] for corner in world_corners) for axis in range(3))
    polygons = len(obj.data.polygons)
    vertices = len(obj.data.vertices)
    modifiers = ",".join(modifier.type for modifier in obj.modifiers)
    material_names = [slot.material.name if slot.material else "None" for slot in obj.material_slots]
    material_polygon_counts = {
        index: sum(1 for polygon in obj.data.polygons if polygon.material_index == index)
        for index in range(len(material_names))
    }
    shape_keys = list(obj.data.shape_keys.key_blocks.keys()) if obj.data.shape_keys else []
    print(
        f"MESH name={obj.name} vertices={vertices} polygons={polygons} "
        f"min={minimum} max={maximum} modifiers={modifiers} "
        f"location={tuple(obj.location)} rotation={tuple(obj.rotation_euler)} "
        f"scale={tuple(obj.scale)} matrix={tuple(tuple(row) for row in obj.matrix_world)}"
    )
    print(
        f"MESH_DETAILS name={obj.name} materials={material_names} "
        f"material_polygon_counts={material_polygon_counts} shape_keys={shape_keys}"
    )
print("SMORZANDO_IMPORT_OBJECTS_END")
