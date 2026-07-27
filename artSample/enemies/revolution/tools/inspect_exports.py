import bpy
import hashlib
import json
import os
import sys


marker = sys.argv.index("--") + 1
sample_root = sys.argv[marker]
export_root = os.path.join(sample_root, "exports")


def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def scene_summary(path, kind):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if kind == "blend":
        bpy.ops.wm.open_mainfile(filepath=path)
    elif kind == "fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif kind == "glb":
        bpy.ops.import_scene.gltf(filepath=path)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    armatures = [
        obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"
    ]
    return {
        "file": os.path.relpath(path, sample_root).replace(os.sep, "/"),
        "sha256": sha256(path),
        "meshes": len(meshes),
        "vertices": sum(len(obj.data.vertices) for obj in meshes),
        "polygons": sum(len(obj.data.polygons) for obj in meshes),
        "loops": sum(len(obj.data.loops) for obj in meshes),
        "material_slots": sum(len(obj.data.materials) for obj in meshes),
        "armatures": len(armatures),
        "bones": sum(len(obj.data.bones) for obj in armatures),
        "actions": sorted(action.name for action in bpy.data.actions),
    }


files = {
    "blend": "revolution_replaced_model_reference_sample.blend",
    "fbx": "revolution_replaced_model_reference_sample.fbx",
    "glb": "revolution_replaced_model_reference_sample.glb",
}
report = {
    kind: scene_summary(os.path.join(export_root, name), kind)
    for kind, name in files.items()
}
with open(
    os.path.join(sample_root, "EXPORT_INSPECTION.json"),
    "w",
    encoding="utf-8",
) as handle:
    json.dump(report, handle, ensure_ascii=False, indent=2)
