import bpy
import hashlib
import json
import shutil
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[5]
SOURCE_SAMPLE_ROOT = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "appearance_reference_sync"
)
SAMPLE_ROOT = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "rig_attachment_correction"
)
SOURCE_BLEND = (
    SOURCE_SAMPLE_ROOT / "blender" / "Rebellion_Appearance_ReferenceSync.blend"
)
SOURCE_APPROVED_GLB = (
    SOURCE_SAMPLE_ROOT / "exports" / "Rebellion_Appearance_ReferenceSync.glb"
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
OUTPUT_BLEND = SAMPLE_ROOT / "blender" / "Rebellion_RigAttachmentCorrection.blend"
OUTPUT_GLB = SAMPLE_ROOT / "exports" / "Rebellion_RigAttachmentCorrection.glb"
REPORT_PATH = SAMPLE_ROOT / "RIG_ATTACHMENT_CORRECTION.json"
STATUS_PATH = SAMPLE_ROOT / "APPROVAL_STATUS.json"

EXPECTED_APPROVED_SHA256 = (
    "8DB44E37CDFB7B3C4D838C0C629A877871207C2A93CFF5660121925689680B51"
)
EXPECTED_ORIGINAL_SHA256 = (
    "BAF8D47AE39523F5EE7DC366DF4E2110D8280725A387133A3ABDBF687F3211E9"
)
LEG_BRANCH_ROOTS = ("Bone_013", "Bone_018", "Bone_023", "Bone_028")
BODY_ATTACHMENT_BONE = "Bone_008"
WEAPON_ATTACHMENT_BONE = "Bone_007"
INCORRECT_ATTACHMENT_BONE = "Bone_017"

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
    "Rebellion_Reference_Scan_Optic",
)
EXPORTED_BODY_DETAIL_OBJECTS = tuple(
    name
    for name in BODY_DETAIL_OBJECTS
    if name != "Rebellion_Reference_Scan_Optic"
)
BLEND_ONLY_BODY_DETAIL_OBJECTS = ("Rebellion_Reference_Scan_Optic",)
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


def weight_signature(mesh_object):
    digest = hashlib.sha256()
    for vertex in mesh_object.data.vertices:
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


def matrix_values(matrix):
    return [value for row in matrix for value in row]


def maximum_matrix_difference(first, second):
    return max(
        abs(a - b)
        for a, b in zip(matrix_values(first), matrix_values(second))
    )


def find_rig():
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one armature, found {len(armatures)}")
    skinned_objects = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
        and obj.vertex_groups
        and any(modifier.type == "ARMATURE" for modifier in obj.modifiers)
    ]
    if len(skinned_objects) != 1:
        raise RuntimeError(
            f"Expected one skinned mesh, found {len(skinned_objects)}"
        )
    return armatures[0], skinned_objects[0]


def require_original_structure(armature):
    if len(armature.data.bones) != 29:
        raise RuntimeError(
            f"Expected 29 Rebellion bones, found {len(armature.data.bones)}"
        )
    body_hub_children = tuple(
        sorted(child.name for child in armature.data.bones["Bone_002"].children)
    )
    if body_hub_children != tuple(sorted(LEG_BRANCH_ROOTS)):
        raise RuntimeError(
            "The original four independent Rebellion leg branches changed: "
            + repr(body_hub_children)
        )
    if armature.data.bones["Bone_008"].parent.name != "Bone_001":
        raise RuntimeError("Bone_008 is no longer the independent weapon branch root.")
    if armature.data.bones["Bone_007"].parent.name != "Bone_008":
        raise RuntimeError("Bone_007 is no longer under Bone_008.")
    if armature.data.bones["Bone_006"].parent.name != "Bone_007":
        raise RuntimeError("Bone_006 is no longer under Bone_007.")
    if armature.data.bones["Bone_017"].parent.name != "Bone_018":
        raise RuntimeError("Bone_017 is no longer part of its original leg chain.")
    return body_hub_children


def recess_vertices_to_reassign(skinned):
    source_group = skinned.vertex_groups[INCORRECT_ATTACHMENT_BONE]
    result = []
    for vertex in skinned.data.vertices:
        coordinate = skinned.matrix_world @ vertex.co
        memberships = {
            skinned.vertex_groups[membership.group].name: membership.weight
            for membership in vertex.groups
        }
        if not (
            abs(coordinate.x) <= 0.55
            and -1.35 <= coordinate.y <= -0.85
            and 1.15 <= coordinate.z <= 1.70
        ):
            continue
        if memberships != {source_group.name: 1.0}:
            continue
        result.append(vertex.index)
    if len(result) != 51:
        raise RuntimeError(
            f"Expected 51 derived recess vertices on Bone_017, found {len(result)}"
        )
    return result


def reassign_recess_vertices(skinned, indices):
    source_group = skinned.vertex_groups[INCORRECT_ATTACHMENT_BONE]
    target_group = skinned.vertex_groups[BODY_ATTACHMENT_BONE]
    source_group.remove(indices)
    target_group.add(indices, 1.0, "REPLACE")


def reparent_objects(armature, names, bone_name):
    states = {}
    for name in names:
        obj = bpy.data.objects.get(name)
        if obj is None:
            raise RuntimeError(f"Required Rebellion detail object is missing: {name}")
        if obj.parent != armature or obj.parent_type != "BONE":
            raise RuntimeError(f"{name} is not bone-parented to the Rebellion armature.")
        if obj.parent_bone != INCORRECT_ATTACHMENT_BONE:
            raise RuntimeError(
                f"{name} expected {INCORRECT_ATTACHMENT_BONE}, found {obj.parent_bone}"
            )
        world = obj.matrix_world.copy()
        states[name] = world
        obj.parent = armature
        obj.parent_type = "BONE"
        obj.parent_bone = bone_name
        obj.matrix_world = world
    bpy.context.view_layer.update()
    differences = {
        name: maximum_matrix_difference(states[name], bpy.data.objects[name].matrix_world)
        for name in names
    }
    if max(differences.values(), default=0.0) > 0.000001:
        raise RuntimeError(
            "Rebellion detail rest transforms changed while correcting bone parents: "
            + repr(differences)
        )
    return differences


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


def roundtrip_structure(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    armature, skinned = find_rig()
    detail_parents = {}
    for name in EXPORTED_BODY_DETAIL_OBJECTS + WEAPON_DETAIL_OBJECTS:
        obj = bpy.data.objects.get(name)
        if obj is None:
            raise RuntimeError(f"Roundtrip detail object is missing: {name}")
        detail_parents[name] = obj.parent_bone
    if any(
        detail_parents[name] != BODY_ATTACHMENT_BONE
        for name in EXPORTED_BODY_DETAIL_OBJECTS
    ):
        raise RuntimeError("Roundtrip body details are not attached to Bone_008.")
    if any(
        detail_parents[name] != WEAPON_ATTACHMENT_BONE
        for name in WEAPON_DETAIL_OBJECTS
    ):
        raise RuntimeError("Roundtrip gun details are not attached to Bone_007.")
    body_group = skinned.vertex_groups.get(BODY_ATTACHMENT_BONE)
    incorrect_group = skinned.vertex_groups.get(INCORRECT_ATTACHMENT_BONE)
    corrected_region_counts = {
        BODY_ATTACHMENT_BONE: 0,
        INCORRECT_ATTACHMENT_BONE: 0,
    }
    for vertex in skinned.data.vertices:
        coordinate = skinned.matrix_world @ vertex.co
        if not (
            abs(coordinate.x) <= 0.55
            and -1.35 <= coordinate.y <= -0.85
            and 1.15 <= coordinate.z <= 1.70
        ):
            continue
        for membership in vertex.groups:
            if body_group is not None and membership.group == body_group.index:
                corrected_region_counts[BODY_ATTACHMENT_BONE] += 1
            if incorrect_group is not None and membership.group == incorrect_group.index:
                corrected_region_counts[INCORRECT_ATTACHMENT_BONE] += 1
    return {
        "armatures": 1,
        "bones": len(armature.data.bones),
        "mesh_objects": len(
            [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
        ),
        "skinned_vertices": len(skinned.data.vertices),
        "skinned_polygons": len(skinned.data.polygons),
        "detail_parent_bones": detail_parents,
        "corrected_region_influence_counts": corrected_region_counts,
    }


def main():
    for path in (SOURCE_BLEND, SOURCE_APPROVED_GLB, ORIGINAL_UNITY_GLB):
        if not path.exists():
            raise FileNotFoundError(path)
    if sha256(SOURCE_APPROVED_GLB) != EXPECTED_APPROVED_SHA256:
        raise RuntimeError("The approved Rebellion sample GLB hash changed.")
    if sha256(ORIGINAL_UNITY_GLB) != EXPECTED_ORIGINAL_SHA256:
        raise RuntimeError("The original Unity Rebellion GLB hash changed.")

    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    armature, skinned = find_rig()
    leg_branches = require_original_structure(armature)
    geometry_before = geometry_signature(skinned)
    weights_before = weight_signature(skinned)
    vertices_before = len(skinned.data.vertices)
    polygons_before = len(skinned.data.polygons)
    candidates = recess_vertices_to_reassign(skinned)

    reassign_recess_vertices(skinned, candidates)
    body_transform_differences = reparent_objects(
        armature,
        BODY_DETAIL_OBJECTS,
        BODY_ATTACHMENT_BONE,
    )
    weapon_transform_differences = reparent_objects(
        armature,
        WEAPON_DETAIL_OBJECTS,
        WEAPON_ATTACHMENT_BONE,
    )
    geometry_after = geometry_signature(skinned)
    weights_after = weight_signature(skinned)
    if geometry_after != geometry_before:
        raise RuntimeError("The Rebellion skinned geometry changed.")
    if weights_after == weights_before:
        raise RuntimeError("The intended recess weight correction was not recorded.")
    if len(skinned.data.vertices) != vertices_before:
        raise RuntimeError("The Rebellion skinned vertex count changed.")
    if len(skinned.data.polygons) != polygons_before:
        raise RuntimeError("The Rebellion skinned polygon count changed.")

    OUTPUT_BLEND.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND))
    export_objects = [armature] + [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and obj.name != "Rebellion_Review_Ground"
    ]
    export_glb(OUTPUT_GLB, export_objects)
    corrected_sha256 = sha256(OUTPUT_GLB)
    UNITY_APPROVED_GLB.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(OUTPUT_GLB, UNITY_APPROVED_GLB)
    if sha256(UNITY_APPROVED_GLB) != corrected_sha256:
        raise RuntimeError("The corrected Unity GLB does not match the corrected sample.")

    roundtrip = roundtrip_structure(OUTPUT_GLB)
    report = {
        "result": "PASS",
        "scope": "Rebellion animation-support rig attachment correction",
        "original_unity_glb_sha256": sha256(ORIGINAL_UNITY_GLB),
        "approved_glb_before_sha256": EXPECTED_APPROVED_SHA256,
        "corrected_glb_sha256": corrected_sha256,
        "unity_corrected_glb_sha256": sha256(UNITY_APPROVED_GLB),
        "original_bone_count": 29,
        "original_leg_branch_roots": leg_branches,
        "weapon_branch": ["Bone_008", "Bone_007", "Bone_006"],
        "geometry_signature_before": geometry_before,
        "geometry_signature_after": geometry_after,
        "geometry_unchanged": geometry_before == geometry_after,
        "skinned_vertices": vertices_before,
        "skinned_polygons": polygons_before,
        "reassigned_recess_vertices": len(candidates),
        "recess_weight_from": INCORRECT_ATTACHMENT_BONE,
        "recess_weight_to": BODY_ATTACHMENT_BONE,
        "body_detail_objects": list(BODY_DETAIL_OBJECTS),
        "exported_body_detail_objects": list(EXPORTED_BODY_DETAIL_OBJECTS),
        "blend_only_body_detail_objects": list(BLEND_ONLY_BODY_DETAIL_OBJECTS),
        "body_detail_parent": BODY_ATTACHMENT_BONE,
        "weapon_detail_objects": list(WEAPON_DETAIL_OBJECTS),
        "weapon_detail_parent": WEAPON_ATTACHMENT_BONE,
        "maximum_rest_transform_difference": max(
            list(body_transform_differences.values())
            + list(weapon_transform_differences.values())
        ),
        "roundtrip": roundtrip,
    }
    REPORT_PATH.write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    status = {
        "status": "APPROVED_DIRECT_UNITY_APPLICATION",
        "approved_for_unity": True,
        "approved_date": "2026-07-25",
        "approval_basis": (
            "User approved the animation-support-only rig attachment correction "
            "and direct Unity application without a separate additional art sample review."
        ),
        "unity_asset": str(UNITY_APPROVED_GLB.relative_to(PROJECT_ROOT)).replace(
            "\\", "/"
        ),
        "unity_asset_sha256": corrected_sha256,
        "source_geometry_modified": False,
        "source_rig_hierarchy_modified": False,
        "rest_appearance_modified": False,
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
                "reassigned_recess_vertices": len(candidates),
                "body_details": len(BODY_DETAIL_OBJECTS),
                "weapon_details": len(WEAPON_DETAIL_OBJECTS),
                "roundtrip": roundtrip,
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
