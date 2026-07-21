import hashlib
import json
import struct
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample" / "enemies" / "dolore" / "attack_attachment"
SOURCE_ATTACK = ROOT / "enemies model" / "dolore attack.glb"
BLEND_PATH = SAMPLE_ROOT / "blender" / "Dolore_AttackAttachment_Sample.blend"
EXPORT_PATH = SAMPLE_ROOT / "exports" / "Dolore_AttackAttachment_Sample.glb"
MANIFEST_PATH = SAMPLE_ROOT / "ASSET_MANIFEST.json"
RESULT_JSON = SAMPLE_ROOT / "SAMPLE_INSPECTION.json"
RESULT_TEXT = SAMPLE_ROOT / "SAMPLE_INSPECTION.txt"


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def mesh_signature(mesh):
    vertex_digest = hashlib.sha256()
    topology_digest = hashlib.sha256()
    for vertex in mesh.vertices:
        vertex_digest.update(struct.pack("<3d", *vertex.co))
    for polygon in mesh.polygons:
        topology_digest.update(struct.pack("<I", len(polygon.vertices)))
        for index in polygon.vertices:
            topology_digest.update(struct.pack("<I", index))
    return {
        "vertices": len(mesh.vertices),
        "polygons": len(mesh.polygons),
        "vertex_position_sha256": vertex_digest.hexdigest().upper(),
        "polygon_topology_sha256": topology_digest.hexdigest().upper(),
    }


def main():
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    required = [
        BLEND_PATH,
        EXPORT_PATH,
        SAMPLE_ROOT / "renders" / "01_front_attached.png",
        SAMPLE_ROOT / "renders" / "02_three_quarter_attached.png",
        SAMPLE_ROOT / "renders" / "03_side_attachment.png",
        SAMPLE_ROOT / "renders" / "04_attachment_closeup.png",
        SAMPLE_ROOT / "renders" / "05_reference_comparison.png",
    ]
    checks = {"required_files_present": all(path.exists() and path.stat().st_size > 0 for path in required)}
    checks["source_attack_hash_preserved"] = manifest["source_attack_sha256"] == sha256(SOURCE_ATTACK)

    bpy.ops.wm.open_mainfile(filepath=str(BLEND_PATH))
    base = bpy.data.objects.get("Dolore_CurrentModel")
    attack = bpy.data.objects.get("Dolore_Attack_Tentacle")
    rig = bpy.data.objects.get("Dolore_Attack_Rig")
    anchor = bpy.data.objects.get("Dolore_Attack_Attachment")
    checks["blend_has_base_model"] = base is not None and base.type == "MESH"
    checks["blend_has_attack_mesh"] = attack is not None and attack.type == "MESH"
    checks["blend_has_attack_rig"] = rig is not None and rig.type == "ARMATURE" and len(rig.data.bones) == 13
    checks["blend_attack_parented_to_attachment"] = rig is not None and anchor is not None and rig.parent == anchor
    checks["blend_attack_keeps_armature_modifier"] = attack is not None and any(
        item.type == "ARMATURE" and item.object == rig for item in attack.modifiers)
    checks["blend_base_geometry_preserved"] = base is not None and (
        mesh_signature(base.data)["vertex_position_sha256"] == manifest["base_geometry_after"]["vertex_position_sha256"] and
        mesh_signature(base.data)["polygon_topology_sha256"] == manifest["base_geometry_after"]["polygon_topology_sha256"])
    checks["blend_attack_geometry_preserved"] = attack is not None and (
        mesh_signature(attack.data)["vertex_position_sha256"] == manifest["attack_geometry_after"]["vertex_position_sha256"] and
        mesh_signature(attack.data)["polygon_topology_sha256"] == manifest["attack_geometry_after"]["polygon_topology_sha256"])
    checks["blend_attack_has_uv"] = attack is not None and len(attack.data.uv_layers) > 0
    checks["blend_attack_has_flesh_material"] = attack is not None and [item.name for item in attack.data.materials] == ["Dolore_Attack_Flesh"]
    root_pose = rig.pose.bones.get(manifest["root_exit_bone"]) if rig is not None else None
    root_direction = (
        rig.matrix_world.to_3x3() @ (root_pose.tail - root_pose.head)
        if root_pose is not None else Vector()
    )
    if root_direction.length > 0.0:
        root_direction.normalize()
    checks["blend_root_exit_faces_frame_front"] = (
        root_pose is not None and
        root_direction.dot(Vector(manifest["root_exit_front_direction_world"])) >= 0.999 and
        manifest["root_exit_front_alignment"] >= 0.999)
    checks["blend_downstream_curve_preserved"] = manifest["downstream_curve_rest_alignment"] >= 0.999
    checks["sample_not_applied_to_unity"] = manifest["unity_applied"] is False
    checks["sample_has_no_animation"] = manifest["animation_applied"] is False

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(EXPORT_PATH))
    export_meshes = [item for item in bpy.context.scene.objects if item.type == "MESH"]
    export_attack_rig = next(
        (item for item in bpy.context.scene.objects if item.type == "ARMATURE" and len(item.data.bones) == 13),
        None)
    with EXPORT_PATH.open("rb") as handle:
        handle.seek(12)
        json_length = struct.unpack("<I", handle.read(4))[0]
        json_type = handle.read(4)
        glb_document = json.loads(handle.read(json_length).decode("utf-8").rstrip("\x00 "))
    checks["glb_contains_base_and_attack_meshes"] = (
        json_type == b"JSON" and
        [item.get("name") for item in glb_document.get("meshes", [])] ==
        ["char1", "Dolore_Attack_Tentacle_Mesh"])
    checks["glb_contains_attack_material"] = any(
        any(material and material.name.startswith("Dolore_Attack_Flesh") for material in item.data.materials)
        for item in export_meshes)
    export_root_pose = (
        export_attack_rig.pose.bones.get(manifest["root_exit_bone"])
        if export_attack_rig is not None else None)
    export_root_direction = (
        export_attack_rig.matrix_world.to_3x3() @ (export_root_pose.tail - export_root_pose.head)
        if export_root_pose is not None else Vector()
    )
    if export_root_direction.length > 0.0:
        export_root_direction.normalize()
    checks["glb_root_exit_faces_frame_front"] = (
        manifest["root_exit_pose_applied_as_rest"] is True and
        export_root_pose is not None and
        export_root_direction.dot(Vector(manifest["root_exit_front_direction_world"])) >= 0.999)

    passed = all(checks.values())
    result = {"result": "PASS" if passed else "FAIL", "checks": checks, "manifest": manifest}
    RESULT_JSON.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    lines = ["Result=" + result["result"], "SourceAttackSha256=" + manifest["source_attack_sha256"]]
    lines.extend(f"Check.{name}={value}" for name, value in checks.items())
    RESULT_TEXT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))
    if not passed:
        raise RuntimeError("Dolore attack attachment sample inspection failed.")


if __name__ == "__main__":
    main()
