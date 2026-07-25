import bpy
import hashlib
import json
import shutil
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[5]
SOURCE_ROOT = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "rig_attachment_correction"
)
OUTPUT_ROOT = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "front_artifact_removal"
)
SOURCE_BLEND = (
    SOURCE_ROOT / "blender" / "Rebellion_RigAttachmentCorrection.blend"
)
SOURCE_GLB = SOURCE_ROOT / "exports" / "Rebellion_RigAttachmentCorrection.glb"
OUTPUT_BLEND = (
    OUTPUT_ROOT / "blender" / "Rebellion_FrontArtifactRemoved.blend"
)
OUTPUT_GLB = OUTPUT_ROOT / "exports" / "Rebellion_FrontArtifactRemoved.glb"
UNITY_APPROVED_GLB = (
    PROJECT_ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Enemies"
    / "Rebellion"
    / "ApprovedAppearance"
    / "Rebellion_ApprovedAppearance.glb"
)
ORIGINAL_UNITY_GLB = (
    PROJECT_ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Enemies"
    / "Rebellion"
    / "Models"
    / "Rebellion.glb"
)
REPORT_PATH = OUTPUT_ROOT / "FRONT_ARTIFACT_REMOVAL.json"
STATUS_PATH = OUTPUT_ROOT / "APPROVAL_STATUS.json"

EXPECTED_SOURCE_SHA256 = (
    "2FCDD1322554251B2E4461946E98B97A83CF1CD9B53225E0ED1442742C29400C"
)
EXPECTED_ORIGINAL_SHA256 = (
    "BAF8D47AE39523F5EE7DC366DF4E2110D8280725A387133A3ABDBF687F3211E9"
)
BODY_BONE = "Bone_008"
LEG_BONES = tuple(
    f"Bone_{index:03d}"
    for start in (9, 14, 19, 24)
    for index in range(start, start + 5)
)
EXPECTED_ARTIFACT_VERTEX_COUNT = 30
BODY_DETAIL_OBJECTS = (
    "Rebellion_Front_Recess_Backplate",
    "Rebellion_Panel_Fastener_00",
    "Rebellion_Panel_Fastener_01",
    "Rebellion_Panel_Fastener_02",
    "Rebellion_Panel_Fastener_03",
    "Rebellion_Panel_Vent_00",
    "Rebellion_Panel_Vent_01",
    "Rebellion_Panel_Vent_02",
    "Rebellion_Panel_Vent_03",
    "Rebellion_Scan_Lens",
)
WEAPON_DETAIL_OBJECTS = (
    "Rebellion_Gun_Hub",
    "Rebellion_Gun_Barrel_00",
    "Rebellion_Gun_Barrel_01",
    "Rebellion_Gun_Barrel_02",
    "Rebellion_Gun_Barrel_03",
    "Rebellion_Gun_Barrel_04",
    "Rebellion_Gun_Barrel_05",
    "Rebellion_Gun_Barrel_06",
)


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def geometry_signature(mesh_object):
    digest = hashlib.sha256()
    for vertex in mesh_object.data.vertices:
        digest.update(
            f"v:{vertex.co.x:.9f},{vertex.co.y:.9f},{vertex.co.z:.9f};".encode(
                "ascii"
            )
        )
    for polygon in mesh_object.data.polygons:
        digest.update(
            ("p:" + ",".join(str(index) for index in polygon.vertices) + ";").encode(
                "ascii"
            )
        )
        digest.update(f"m:{polygon.material_index};".encode("ascii"))
    return digest.hexdigest().upper()


def weight_signature(mesh_object, excluded_vertices=()):
    excluded = set(excluded_vertices)
    digest = hashlib.sha256()
    for vertex in mesh_object.data.vertices:
        if vertex.index in excluded:
            continue
        weights = sorted(
            (
                mesh_object.vertex_groups[membership.group].name,
                membership.weight,
            )
            for membership in vertex.groups
        )
        for name, weight in weights:
            digest.update(f"{vertex.index}:{name}:{weight:.9f};".encode("ascii"))
    return digest.hexdigest().upper()


def find_rig():
    armatures = [
        obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"
    ]
    skinned_objects = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
        and obj.vertex_groups
        and any(modifier.type == "ARMATURE" for modifier in obj.modifiers)
    ]
    if len(armatures) != 1 or len(skinned_objects) != 1:
        raise RuntimeError(
            f"Expected one armature and one skinned mesh, found "
            f"{len(armatures)} and {len(skinned_objects)}"
        )
    return armatures[0], skinned_objects[0]


def require_structure(armature):
    if len(armature.data.bones) != 29:
        raise RuntimeError(
            f"Expected 29 Rebellion bones, found {len(armature.data.bones)}"
        )
    if armature.data.bones["Bone_008"].parent.name != "Bone_001":
        raise RuntimeError("Bone_008 must remain under Bone_001.")
    for name in BODY_DETAIL_OBJECTS:
        obj = bpy.data.objects.get(name)
        if obj is None or obj.parent_bone != "Bone_008":
            raise RuntimeError(f"{name} is not attached to Bone_008.")
    for name in WEAPON_DETAIL_OBJECTS:
        obj = bpy.data.objects.get(name)
        if obj is None or obj.parent_bone != "Bone_007":
            raise RuntimeError(f"{name} is not attached to Bone_007.")


def in_recess_volume(position):
    return (
        abs(position.x) <= 0.55
        and -1.35 <= position.y <= -0.85
        and 1.15 <= position.z <= 1.70
    )


def artifact_vertices(mesh_object):
    indices = []
    weight_details = {}
    for vertex in mesh_object.data.vertices:
        world = mesh_object.matrix_world @ vertex.co
        if not in_recess_volume(world):
            continue
        memberships = {
            mesh_object.vertex_groups[membership.group].name: membership.weight
            for membership in vertex.groups
        }
        leg_weights = {
            name: weight
            for name, weight in memberships.items()
            if name in LEG_BONES and weight > 0.000001
        }
        if not leg_weights:
            continue
        indices.append(vertex.index)
        weight_details[str(vertex.index)] = {
            "position": [round(value, 6) for value in world],
            "leg_weights": {
                name: round(weight, 9)
                for name, weight in sorted(leg_weights.items())
            },
        }
    if len(indices) != EXPECTED_ARTIFACT_VERTEX_COUNT:
        raise RuntimeError(
            f"Expected {EXPECTED_ARTIFACT_VERTEX_COUNT} front artifact vertices, "
            f"found {len(indices)}"
        )
    return indices, weight_details


def assign_to_body(mesh_object, indices):
    for vertex_group in mesh_object.vertex_groups:
        vertex_group.remove(indices)
    mesh_object.vertex_groups[BODY_BONE].add(indices, 1.0, "REPLACE")


def export_glb(path, export_objects):
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in export_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = export_objects[0]
    bpy.ops.export_scene.gltf(
        filepath=str(path),
        export_format="GLB",
        use_selection=True,
        export_apply=False,
        export_animations=False,
        export_cameras=False,
        export_lights=False,
        export_materials="EXPORT",
        export_all_influences=True,
        export_influence_nb=8,
    )


def roundtrip_check(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    armature, skinned = find_rig()
    require_structure(armature)
    selected = []
    leg_influenced = []
    for vertex in skinned.data.vertices:
        world = skinned.matrix_world @ vertex.co
        if not in_recess_volume(world):
            continue
        selected.append(vertex.index)
        for membership in vertex.groups:
            name = skinned.vertex_groups[membership.group].name
            if name in LEG_BONES and membership.weight > 0.000001:
                leg_influenced.append(vertex.index)
                break
    if leg_influenced:
        raise RuntimeError(
            "Roundtrip front recess still has animated leg influences: "
            + repr(leg_influenced[:20])
        )
    return {
        "armatures": 1,
        "bones": len(armature.data.bones),
        "skinned_renderers": 1,
        "skinned_vertices": len(skinned.data.vertices),
        "skinned_polygons": len(skinned.data.polygons),
        "front_recess_vertices": len(selected),
        "front_recess_leg_influenced_vertices": len(leg_influenced),
    }


def main():
    for path in (SOURCE_BLEND, SOURCE_GLB, UNITY_APPROVED_GLB, ORIGINAL_UNITY_GLB):
        if not path.exists():
            raise FileNotFoundError(path)
    if sha256(SOURCE_GLB) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("The corrected source Rebellion GLB hash changed.")
    if sha256(UNITY_APPROVED_GLB) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("The Unity approved Rebellion GLB hash changed.")
    if sha256(ORIGINAL_UNITY_GLB) != EXPECTED_ORIGINAL_SHA256:
        raise RuntimeError("The original Unity Rebellion GLB hash changed.")

    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    armature, skinned = find_rig()
    require_structure(armature)
    geometry_before = geometry_signature(skinned)
    vertices_before = len(skinned.data.vertices)
    polygons_before = len(skinned.data.polygons)
    target_indices, original_leg_weights = artifact_vertices(skinned)
    non_target_weights_before = weight_signature(skinned, target_indices)

    assign_to_body(skinned, target_indices)

    geometry_after = geometry_signature(skinned)
    non_target_weights_after = weight_signature(skinned, target_indices)
    if geometry_after != geometry_before:
        raise RuntimeError("Front artifact correction changed mesh geometry.")
    if non_target_weights_after != non_target_weights_before:
        raise RuntimeError("Front artifact correction changed non-target weights.")
    for index in target_indices:
        memberships = {
            skinned.vertex_groups[membership.group].name: membership.weight
            for membership in skinned.data.vertices[index].groups
        }
        if memberships != {BODY_BONE: 1.0}:
            raise RuntimeError(
                f"Target vertex {index} is not exclusively on {BODY_BONE}: "
                + repr(memberships)
            )
    vertices_after = len(skinned.data.vertices)
    polygons_after = len(skinned.data.polygons)

    OUTPUT_BLEND.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND))
    export_objects = [armature] + [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and obj.name != "Rebellion_Review_Ground"
    ]
    export_glb(OUTPUT_GLB, export_objects)
    corrected_sha256 = sha256(OUTPUT_GLB)
    shutil.copy2(OUTPUT_GLB, UNITY_APPROVED_GLB)
    if sha256(UNITY_APPROVED_GLB) != corrected_sha256:
        raise RuntimeError("Unity GLB does not match the artifact-corrected GLB.")

    roundtrip = roundtrip_check(OUTPUT_GLB)
    report = {
        "result": "PASS",
        "scope": "Rebellion front recess animated square artifact removal",
        "cause": (
            "Boolean-generated front recess vertices inherited animated leg bone "
            "weights and flipped out of the disc during the move clip."
        ),
        "fix": (
            "Reassigned only the 30 front recess vertices carrying leg weights "
            "to Bone_008. No geometry was deleted."
        ),
        "source_glb_sha256": EXPECTED_SOURCE_SHA256,
        "corrected_glb_sha256": corrected_sha256,
        "unity_corrected_glb_sha256": sha256(UNITY_APPROVED_GLB),
        "original_unity_glb_sha256": sha256(ORIGINAL_UNITY_GLB),
        "target_vertex_count": len(target_indices),
        "target_vertex_indices": target_indices,
        "target_original_leg_weights": original_leg_weights,
        "target_bone": BODY_BONE,
        "geometry_signature_before": geometry_before,
        "geometry_signature_after": geometry_after,
        "geometry_unchanged": geometry_before == geometry_after,
        "non_target_weight_signature_before": non_target_weights_before,
        "non_target_weight_signature_after": non_target_weights_after,
        "non_target_weights_unchanged": (
            non_target_weights_before == non_target_weights_after
        ),
        "vertices_before": vertices_before,
        "vertices_after": vertices_after,
        "polygons_before": polygons_before,
        "polygons_after": polygons_after,
        "roundtrip": roundtrip,
    }
    REPORT_PATH.write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    status = {
        "status": "APPROVED_DIRECT_UNITY_CORRECTION",
        "approved_for_unity": True,
        "approved_date": "2026-07-25",
        "approval_basis": (
            "User requested removal of only the square object flapping in front "
            "of the Rebellion weapon and approved direct correction."
        ),
        "unity_asset": str(UNITY_APPROVED_GLB.relative_to(PROJECT_ROOT)).replace(
            "\\", "/"
        ),
        "unity_asset_sha256": corrected_sha256,
        "geometry_modified": False,
        "non_target_weights_modified": False,
        "target_vertices_reweighted": len(target_indices),
    }
    STATUS_PATH.write_text(
        json.dumps(status, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "result": "PASS",
                "corrected_sha256": corrected_sha256,
                "target_vertices_reweighted": len(target_indices),
                "geometry_unchanged": geometry_before == geometry_after,
                "non_target_weights_unchanged": (
                    non_target_weights_before == non_target_weights_after
                ),
                "roundtrip": roundtrip,
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
