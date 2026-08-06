import hashlib
import json
from pathlib import Path

import bpy


PROJECT_ROOT = Path(__file__).resolve().parents[4]
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / "ispant_draw_sword"
REVISION_DIR = SAMPLE_ROOT / "length_0_9m_revision"
SOURCE_BLEND = SAMPLE_ROOT / "Ispant_DrawSword_ArtSample.blend"
BLEND = REVISION_DIR / "Ispant_DrawSword_0_9m_ArtSample.blend"
FBX = REVISION_DIR / "Ispant_DrawSword_0_9m_ArtSample.fbx"
GLB = REVISION_DIR / "Ispant_DrawSword_0_9m_ArtSample.glb"
REPORT = REVISION_DIR / "Ispant_DrawSword_0_9m_ExportValidation.json"
EXPECTED_SOURCE_SHA256 = "F112EFF207D2EAB5FC89AF5735103877B51F973FBAD9B5EBD8D3DBEB44770FB9"
EXPECTED_MATERIALS = {
    "Ispant_LongSword_WornSteel",
    "Ispant_LongSword_BrownLeather",
    "Ispant_LongSword_DarkEngraving",
}
EXPECTED_VERTICES = {"BLEND": 2080, "FBX": 2080, "GLB": 3597}


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def sword_record(label):
    matches = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and obj.name.startswith("Ispant_Reference_LongSword")
    ]
    if len(matches) != 1:
        raise RuntimeError(f"{label}: expected one sword mesh, found {len(matches)}.")
    sword = matches[0]
    vertices = len(sword.data.vertices)
    triangles = sum(len(polygon.vertices) - 2 for polygon in sword.data.polygons)
    dimensions = tuple(round(value, 6) for value in sword.dimensions)
    materials = {slot.material.name for slot in sword.material_slots if slot.material}
    if vertices != EXPECTED_VERTICES[label]:
        raise RuntimeError(f"{label}: vertex count differs: {vertices}.")
    if triangles != 4092:
        raise RuntimeError(f"{label}: triangle count differs: {triangles}.")
    if dimensions != (0.198372, 0.076, 0.9):
        raise RuntimeError(f"{label}: dimensions differ: {dimensions}.")
    if materials != EXPECTED_MATERIALS:
        raise RuntimeError(f"{label}: materials differ: {materials}.")
    return {
        "vertices": vertices,
        "triangles": triangles,
        "dimensions_m": list(dimensions),
        "materials": sorted(materials),
        "parent": sword.parent.name if sword.parent else None,
        "parent_type": sword.parent_type,
        "parent_bone": sword.parent_bone,
    }


def validate_blend():
    bpy.ops.wm.open_mainfile(filepath=str(BLEND))
    record = sword_record("BLEND")
    sword = bpy.data.objects["Ispant_Reference_LongSword"]
    expected = {
        "blade_length_m": 0.6485,
        "grip_length_m": 0.17,
        "guard_width_m": 0.19,
        "pommel_length_m": 0.055,
        "overall_length_m": 0.9,
    }
    for name, value in expected.items():
        if abs(float(sword.get(name, -1.0)) - value) > 0.000001:
            raise RuntimeError(f"BLEND: {name} differs.")
    if sword.parent_type != "BONE" or sword.parent_bone != "mixamorig:RightHand":
        raise RuntimeError("BLEND: RightHand bone parenting differs.")
    record["approved_dimensions"] = expected
    return record


def validate_fbx():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(FBX))
    return sword_record("FBX")


def validate_glb():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(GLB))
    return sword_record("GLB")


def main():
    if sha256(SOURCE_BLEND) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("The approved source sample changed.")
    build_report = json.loads(
        (REVISION_DIR / "Ispant_DrawSword_0_9m_ArtSample_Report.json").read_text(encoding="utf-8")
    )
    if not build_report.get("hilt_geometry_and_uv_preserved"):
        raise RuntimeError("The build report did not preserve the approved hilt.")
    if not build_report.get("blade_topology_material_and_uv_preserved"):
        raise RuntimeError("The build report did not preserve the approved blade UV/topology.")
    result = {
        "result": "PASS",
        "approval_status": "PENDING_USER_APPROVAL",
        "unity_runtime_applied": False,
        "source_approved_blend_sha256": EXPECTED_SOURCE_SHA256,
        "artifacts": {
            "blend": {"sha256": sha256(BLEND), **validate_blend()},
            "fbx": {"sha256": sha256(FBX), **validate_fbx()},
            "glb": {"sha256": sha256(GLB), **validate_glb()},
        },
    }
    REPORT.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print("ISPANT_DRAW_SWORD_0_9M_EXPORT_VALIDATION=PASS")
    print(f"ISPANT_DRAW_SWORD_0_9M_EXPORT_VALIDATION_REPORT={REPORT}")


if __name__ == "__main__":
    main()
