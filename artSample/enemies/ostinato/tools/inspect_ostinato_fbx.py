from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[4]
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx"


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX), use_anim=True)

    print(f"SOURCE={SOURCE_FBX}")
    for obj in bpy.context.scene.objects:
        print(
            "OBJECT "
            f"name={obj.name!r} type={obj.type} "
            f"location={tuple(round(value, 6) for value in obj.location)} "
            f"rotation={tuple(round(value, 6) for value in obj.rotation_euler)} "
            f"scale={tuple(round(value, 6) for value in obj.scale)}"
        )
        if obj.type == "MESH":
            mesh = obj.data
            print(
                "MESH "
                f"vertices={len(mesh.vertices)} polygons={len(mesh.polygons)} "
                f"materials={[slot.name if slot else None for slot in mesh.materials]} "
                f"uv_layers={[layer.name for layer in mesh.uv_layers]} "
                f"color_attributes={[attribute.name for attribute in mesh.color_attributes]} "
                f"vertex_groups={[group.name for group in obj.vertex_groups]}"
            )
            world_corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
            minimum = tuple(round(min(corner[index] for corner in world_corners), 6) for index in range(3))
            maximum = tuple(round(max(corner[index] for corner in world_corners), 6) for index in range(3))
            print(f"BOUNDS min={minimum} max={maximum}")
            for group in obj.vertex_groups:
                weighted = []
                for vertex in mesh.vertices:
                    weight = next(
                        (membership.weight for membership in vertex.groups if membership.group == group.index),
                        0.0,
                    )
                    if weight >= 0.25:
                        weighted.append(obj.matrix_world @ vertex.co)
                if weighted:
                    group_min = tuple(round(min(point[index] for point in weighted), 6) for index in range(3))
                    group_max = tuple(round(max(point[index] for point in weighted), 6) for index in range(3))
                    print(
                        f"GROUP name={group.name!r} vertices={len(weighted)} "
                        f"min={group_min} max={group_max}"
                    )
        elif obj.type == "ARMATURE":
            print(f"ARMATURE bones={[bone.name for bone in obj.data.bones]}")


if __name__ == "__main__":
    main()
