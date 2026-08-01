import bpy
import json
from pathlib import Path


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
EXPORTS = SAMPLE / "exports"
OUTPUT = SAMPLE / "EXPORT_INSPECTION.json"


def inspect_current():
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    return {
        "mesh_objects": len(meshes),
        "vertices": sum(len(obj.data.vertices) for obj in meshes),
        "edges": sum(len(obj.data.edges) for obj in meshes),
        "polygons": sum(len(obj.data.polygons) for obj in meshes),
        "loops": sum(len(obj.data.loops) for obj in meshes),
        "material_slots": sum(len(obj.data.materials) for obj in meshes),
        "armature_objects": len(armatures),
        "bones": sum(len(obj.data.bones) for obj in armatures),
        "actions": len(bpy.data.actions),
    }


def inspect_fbx(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    return inspect_current()


def inspect_glb(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    return inspect_current()


def main():
    expected = {"vertices": 2109, "polygons": 3913, "bones": 24}
    fbx = inspect_fbx(EXPORTS / "Kursa_Appearance_ReferenceSync.fbx")
    glb = inspect_glb(EXPORTS / "Kursa_Appearance_ReferenceSync.glb")
    result = {
        "result": "PASS" if all(
            data["vertices"] == expected["vertices"]
            and data["polygons"] == expected["polygons"]
            and data["bones"] == expected["bones"]
            and data["material_slots"] >= 8
            for data in (fbx, glb)
        ) else "REVIEW",
        "expected_source_counts": expected,
        "fbx_reimport": fbx,
        "glb_reimport": glb,
        "note": "Blend-file geometry preservation is authoritative; exchange formats are additionally reimport-inspected for review portability.",
    }
    OUTPUT.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result))


if __name__ == "__main__":
    main()
