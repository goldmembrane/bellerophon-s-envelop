import bpy
import json
import sys
from pathlib import Path


def vec(value):
    return [round(float(component), 9) for component in value]


def main():
    arguments = sys.argv[sys.argv.index("--") + 1 :]
    if len(arguments) != 1:
        raise RuntimeError("Usage: blender --background --python inspect_kursa_runtime.py -- <fbx>")

    source = Path(arguments[0]).resolve()
    if not source.is_file():
        raise FileNotFoundError(source)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source), use_anim=True)
    scene = bpy.context.scene
    scene.frame_set(1)

    armatures = [item for item in scene.objects if item.type == "ARMATURE"]
    meshes = [item for item in scene.objects if item.type == "MESH"]
    if len(armatures) != 1 or len(meshes) != 1:
        raise RuntimeError(
            f"Expected one armature and one mesh, got {len(armatures)} and {len(meshes)}"
        )

    armature = armatures[0]
    mesh = meshes[0]
    bones = {}
    for bone in armature.data.bones:
        bones[bone.name] = {
            "parent": bone.parent.name if bone.parent else None,
            "head_local": vec(bone.head_local),
            "tail_local": vec(bone.tail_local),
            "length": round(float(bone.length), 9),
        }

    pose_bones = {}
    for bone in armature.pose.bones:
        pose_bones[bone.name] = {
            "rotation_mode": bone.rotation_mode,
            "location": vec(bone.location),
            "rotation_quaternion": vec(bone.rotation_quaternion),
            "scale": vec(bone.scale),
            "matrix_translation": vec(bone.matrix.translation),
        }

    result = {
        "source": str(source),
        "scene_frame": scene.frame_current,
        "objects": [
            {
                "name": item.name,
                "type": item.type,
                "parent": item.parent.name if item.parent else None,
                "location": vec(item.location),
                "rotation_euler": vec(item.rotation_euler),
                "scale": vec(item.scale),
            }
            for item in sorted(scene.objects, key=lambda entry: entry.name)
        ],
        "mesh": {
            "name": mesh.name,
            "vertices": len(mesh.data.vertices),
            "edges": len(mesh.data.edges),
            "polygons": len(mesh.data.polygons),
            "materials": [slot.material.name if slot.material else None for slot in mesh.material_slots],
            "vertex_groups": [group.name for group in mesh.vertex_groups],
            "armature_modifiers": [
                modifier.object.name if modifier.object else None
                for modifier in mesh.modifiers
                if modifier.type == "ARMATURE"
            ],
        },
        "armature": {
            "name": armature.name,
            "bones": bones,
            "pose": pose_bones,
            "action": (
                armature.animation_data.action.name
                if armature.animation_data and armature.animation_data.action
                else None
            ),
        },
        "actions": [
            {
                "name": action.name,
                "frame_range": [float(action.frame_range[0]), float(action.frame_range[1])],
                "slots": len(action.slots) if hasattr(action, "slots") else None,
            }
            for action in bpy.data.actions
        ],
    }
    print("KURSA_RUNTIME_INSPECTION_BEGIN")
    print(json.dumps(result, ensure_ascii=False, indent=2))
    print("KURSA_RUNTIME_INSPECTION_END")


if __name__ == "__main__":
    main()
