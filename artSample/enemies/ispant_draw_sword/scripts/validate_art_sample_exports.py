import hashlib
import json
from pathlib import Path

import bpy


PROJECT_ROOT = Path(__file__).resolve().parents[4]
SAMPLE_DIR = PROJECT_ROOT / "artSample" / "enemies" / "ispant_draw_sword"
SOURCE_FBX = (
    PROJECT_ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Enemies"
    / "Ispant"
    / "Animations"
    / "Ispant_DrawSword.fbx"
)
EXPECTED_SOURCE_SHA256 = "B9DEB78C6BECA61C81EE5ECD86C4763E56186B8925EED29720B4B62ED482CE42"
EXPECTED_MATERIALS = {
    "Ispant_LongSword_WornSteel",
    "Ispant_LongSword_BrownLeather",
    "Ispant_LongSword_DarkEngraving",
}
REPORT_PATH = SAMPLE_DIR / "Ispant_DrawSword_ExportValidation.json"


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def sword_record(format_name):
    swords = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and obj.name.startswith("Ispant_Reference_LongSword")
    ]
    if len(swords) != 1:
        raise RuntimeError(f"{format_name}: expected one sword mesh, found {len(swords)}")
    sword = swords[0]
    materials = {
        slot.material.name
        for slot in sword.material_slots
        if slot.material is not None
    }
    missing = EXPECTED_MATERIALS - materials
    if missing:
        raise RuntimeError(f"{format_name}: missing sword materials {sorted(missing)}")
    if len(sword.data.vertices) < 100:
        raise RuntimeError(f"{format_name}: sword mesh is unexpectedly sparse")
    return {
        "sword_object": sword.name,
        "sword_mesh_count": len(swords),
        "vertices": len(sword.data.vertices),
        "triangles": sum(len(polygon.vertices) - 2 for polygon in sword.data.polygons),
        "materials": sorted(materials),
        "parent": sword.parent.name if sword.parent else None,
        "parent_type": sword.parent_type,
        "parent_bone": sword.parent_bone,
        "dimensions_m": [round(value, 6) for value in sword.dimensions],
    }


def validate_blend(path):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    record = sword_record("BLEND")
    sword = bpy.data.objects[record["sword_object"]]
    if sword.parent_type != "BONE" or sword.parent_bone != "mixamorig:RightHand":
        raise RuntimeError("BLEND: sword is not parented to mixamorig:RightHand")
    return record


def validate_fbx(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    return sword_record("FBX")


def validate_glb(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    return sword_record("GLB")


def main():
    source_hash = sha256(SOURCE_FBX)
    if source_hash != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("Current Unity draw-sword source FBX hash changed")

    blend = SAMPLE_DIR / "Ispant_DrawSword_ArtSample.blend"
    fbx = SAMPLE_DIR / "Ispant_DrawSword_ArtSample.fbx"
    glb = SAMPLE_DIR / "Ispant_DrawSword_ArtSample.glb"
    validation = {
        "result": "PASS",
        "unity_runtime_applied": False,
        "source_fbx_sha256": source_hash,
        "artifacts": {
            "blend": {"sha256": sha256(blend), **validate_blend(blend)},
            "fbx": {"sha256": sha256(fbx), **validate_fbx(fbx)},
            "glb": {"sha256": sha256(glb), **validate_glb(glb)},
        },
    }
    REPORT_PATH.write_text(
        json.dumps(validation, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print("ISPANT_DRAW_SWORD_EXPORT_VALIDATION=PASS")
    print(f"ISPANT_DRAW_SWORD_EXPORT_VALIDATION_REPORT={REPORT_PATH}")


if __name__ == "__main__":
    main()
