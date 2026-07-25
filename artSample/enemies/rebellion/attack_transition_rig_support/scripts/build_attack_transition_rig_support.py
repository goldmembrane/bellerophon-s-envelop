import bpy
import hashlib
import json
import shutil
from collections import defaultdict, deque
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[5]
SOURCE_ROOT = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "front_artifact_removal"
)
OUTPUT_ROOT = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "attack_transition_rig_support"
)
SOURCE_BLEND = SOURCE_ROOT / "blender" / "Rebellion_FrontArtifactRemoved.blend"
SOURCE_GLB = SOURCE_ROOT / "exports" / "Rebellion_FrontArtifactRemoved.glb"
OUTPUT_BLEND = (
    OUTPUT_ROOT / "blender" / "Rebellion_AttackTransitionRigSupport.blend"
)
OUTPUT_GLB = (
    OUTPUT_ROOT / "exports" / "Rebellion_AttackTransitionRigSupport.glb"
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
REPORT_PATH = OUTPUT_ROOT / "ATTACK_TRANSITION_RIG_SUPPORT.json"
STATUS_PATH = OUTPUT_ROOT / "APPROVAL_STATUS.json"
ANALYSIS_PATH = OUTPUT_ROOT / "analysis" / "DISC_REGION_ANALYSIS.json"

EXPECTED_SOURCE_SHA256 = (
    "712FE23B96B773204F2F1A56588F00B9CF5AEA81D6E9A60CA830FD3FEC89E24A"
)
EXPECTED_ORIGINAL_SHA256 = (
    "BAF8D47AE39523F5EE7DC366DF4E2110D8280725A387133A3ABDBF687F3211E9"
)
BODY_BONE = "Bone_008"
DISC_MATERIAL_NAME = "Rebellion_Worn_Disc_Steel"
MIN_POLYGON_CENTER_Z = 1.30
MAX_POLYGON_CENTER_RADIUS = 1.31
SEED_MINIMUM_Z = 1.43
EXPECTED_DISC_POLYGONS = 398
EXPECTED_DISC_VERTICES = 273
EXPECTED_BOUNDARY_VERTICES = 39
LEG_BONES = tuple(
    f"Bone_{index:03d}"
    for start in (9, 14, 19, 24)
    for index in range(start, start + 5)
)
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
    if armature.data.bones["Bone_007"].parent.name != "Bone_008":
        raise RuntimeError("Bone_007 must remain under Bone_008.")
    if armature.data.bones["Bone_006"].parent.name != "Bone_007":
        raise RuntimeError("Bone_006 must remain under Bone_007.")
    expected_leg_roots = {"Bone_013", "Bone_018", "Bone_023", "Bone_028"}
    actual_leg_roots = {
        bone.name for bone in armature.data.bones["Bone_002"].children
    }
    if actual_leg_roots != expected_leg_roots:
        raise RuntimeError(
            "The four Rebellion leg roots changed: " + repr(actual_leg_roots)
        )
    for name in BODY_DETAIL_OBJECTS:
        obj = bpy.data.objects.get(name)
        if obj is None or obj.parent_bone != BODY_BONE:
            raise RuntimeError(f"{name} is not attached to {BODY_BONE}.")
    for name in WEAPON_DETAIL_OBJECTS:
        obj = bpy.data.objects.get(name)
        if obj is None or obj.parent_bone != "Bone_007":
            raise RuntimeError(f"{name} is not attached to Bone_007.")


def material_index(mesh_object):
    result = next(
        (
            index
            for index, slot in enumerate(mesh_object.material_slots)
            if slot.material
            and slot.material.name.startswith(DISC_MATERIAL_NAME)
        ),
        None,
    )
    if result is None:
        raise RuntimeError(f"Missing material: {DISC_MATERIAL_NAME}")
    return result


def select_disc_region(mesh_object):
    mesh = mesh_object.data
    target_material_index = material_index(mesh_object)
    edge_to_polygons = defaultdict(list)
    polygon_edges = {}
    for polygon in mesh.polygons:
        edges = []
        vertices = list(polygon.vertices)
        for index, first in enumerate(vertices):
            second = vertices[(index + 1) % len(vertices)]
            edge = tuple(sorted((first, second)))
            edges.append(edge)
            edge_to_polygons[edge].append(polygon.index)
        polygon_edges[polygon.index] = edges

    allowed = set()
    seeds = set()
    for polygon in mesh.polygons:
        if polygon.material_index != target_material_index:
            continue
        center = mesh_object.matrix_world @ polygon.center
        radius = (center.x * center.x + center.y * center.y) ** 0.5
        if (
            center.z >= MIN_POLYGON_CENTER_Z
            and radius <= MAX_POLYGON_CENTER_RADIUS
        ):
            allowed.add(polygon.index)
            if center.z >= SEED_MINIMUM_Z:
                seeds.add(polygon.index)

    selected_polygons = set()
    queue = deque(sorted(seeds))
    while queue:
        polygon_index = queue.popleft()
        if polygon_index in selected_polygons or polygon_index not in allowed:
            continue
        selected_polygons.add(polygon_index)
        for edge in polygon_edges[polygon_index]:
            for neighbor in edge_to_polygons[edge]:
                if neighbor in allowed and neighbor not in selected_polygons:
                    queue.append(neighbor)

    selected_vertices = {
        vertex_index
        for polygon_index in selected_polygons
        for vertex_index in mesh.polygons[polygon_index].vertices
    }
    nonselected_vertices = {
        vertex_index
        for polygon in mesh.polygons
        if polygon.index not in selected_polygons
        for vertex_index in polygon.vertices
    }
    boundary_vertices = selected_vertices & nonselected_vertices
    return selected_polygons, selected_vertices, boundary_vertices


def require_expected_selection(
    selected_polygons,
    selected_vertices,
    boundary_vertices,
):
    actual = (
        len(selected_polygons),
        len(selected_vertices),
        len(boundary_vertices),
    )
    expected = (
        EXPECTED_DISC_POLYGONS,
        EXPECTED_DISC_VERTICES,
        EXPECTED_BOUNDARY_VERTICES,
    )
    if actual != expected:
        raise RuntimeError(
            f"Disc selection changed. Expected {expected}, found {actual}."
        )


def assign_to_body(mesh_object, indices):
    index_list = sorted(indices)
    for vertex_group in mesh_object.vertex_groups:
        vertex_group.remove(index_list)
    mesh_object.vertex_groups[BODY_BONE].add(index_list, 1.0, "REPLACE")


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
    selected_polygons, selected_vertices, boundary_vertices = select_disc_region(
        skinned
    )
    leg_influenced = []
    non_body_influenced = []
    for vertex_index in selected_vertices:
        memberships = {
            skinned.vertex_groups[membership.group].name: membership.weight
            for membership in skinned.data.vertices[vertex_index].groups
            if membership.weight > 0.000001
        }
        if any(name in LEG_BONES for name in memberships):
            leg_influenced.append(vertex_index)
        if memberships != {BODY_BONE: 1.0}:
            non_body_influenced.append(
                [vertex_index, memberships]
            )
    if leg_influenced:
        raise RuntimeError(
            "Roundtrip disc region retains leg weights: "
            + repr(leg_influenced[:20])
        )
    if non_body_influenced:
        raise RuntimeError(
            "Roundtrip disc region is not exclusive to Bone_008: "
            + repr(non_body_influenced[:5])
        )
    return {
        "armatures": 1,
        "bones": len(armature.data.bones),
        "mesh_objects": len(
            [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
        ),
        "skinned_vertices": len(skinned.data.vertices),
        "skinned_polygons": len(skinned.data.polygons),
        "disc_polygons": len(selected_polygons),
        "disc_vertices": len(selected_vertices),
        "disc_boundary_vertices": len(boundary_vertices),
        "disc_leg_influenced_vertices": len(leg_influenced),
        "disc_non_body_influenced_vertices": len(non_body_influenced),
    }


def main():
    for path in (
        SOURCE_BLEND,
        SOURCE_GLB,
        UNITY_APPROVED_GLB,
        ORIGINAL_UNITY_GLB,
        ANALYSIS_PATH,
    ):
        if not path.exists():
            raise FileNotFoundError(path)
    if sha256(SOURCE_GLB) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("The front-artifact-corrected source GLB hash changed.")
    if sha256(UNITY_APPROVED_GLB) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("The Unity approved Rebellion GLB hash changed.")
    if sha256(ORIGINAL_UNITY_GLB) != EXPECTED_ORIGINAL_SHA256:
        raise RuntimeError("The original Unity Rebellion GLB hash changed.")

    analysis = json.loads(ANALYSIS_PATH.read_text(encoding="utf-8"))
    if (
        analysis["selected_polygons"] != EXPECTED_DISC_POLYGONS
        or analysis["selected_vertices"] != EXPECTED_DISC_VERTICES
        or analysis["boundary_vertices_shared_with_unselected_faces"]
        != EXPECTED_BOUNDARY_VERTICES
    ):
        raise RuntimeError("The reviewed disc region analysis changed.")

    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    armature, skinned = find_rig()
    require_structure(armature)
    selected_polygons, selected_vertices, boundary_vertices = select_disc_region(
        skinned
    )
    require_expected_selection(
        selected_polygons,
        selected_vertices,
        boundary_vertices,
    )
    geometry_before = geometry_signature(skinned)
    vertices_before = len(skinned.data.vertices)
    polygons_before = len(skinned.data.polygons)
    non_target_weights_before = weight_signature(skinned, selected_vertices)

    assign_to_body(skinned, selected_vertices)

    geometry_after = geometry_signature(skinned)
    non_target_weights_after = weight_signature(skinned, selected_vertices)
    if geometry_after != geometry_before:
        raise RuntimeError("Attack transition rig support changed mesh geometry.")
    if non_target_weights_after != non_target_weights_before:
        raise RuntimeError("Attack transition rig support changed non-disc weights.")
    for vertex_index in selected_vertices:
        memberships = {
            skinned.vertex_groups[membership.group].name: membership.weight
            for membership in skinned.data.vertices[vertex_index].groups
        }
        if memberships != {BODY_BONE: 1.0}:
            raise RuntimeError(
                f"Disc vertex {vertex_index} is not exclusively on {BODY_BONE}: "
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
        raise RuntimeError("Unity GLB does not match the rig-support GLB.")

    roundtrip = roundtrip_check(OUTPUT_GLB)
    report = {
        "result": "PASS",
        "scope": "Rebellion attack transition disc-only lift rig support",
        "source_glb_sha256": EXPECTED_SOURCE_SHA256,
        "corrected_glb_sha256": corrected_sha256,
        "unity_corrected_glb_sha256": sha256(UNITY_APPROVED_GLB),
        "original_unity_glb_sha256": sha256(ORIGINAL_UNITY_GLB),
        "target_bone": BODY_BONE,
        "disc_selection": {
            "material": DISC_MATERIAL_NAME,
            "minimum_polygon_center_z": MIN_POLYGON_CENTER_Z,
            "maximum_polygon_center_radius": MAX_POLYGON_CENTER_RADIUS,
            "seed_minimum_z": SEED_MINIMUM_Z,
            "polygons": len(selected_polygons),
            "vertices": len(selected_vertices),
            "boundary_vertices": len(boundary_vertices),
            "visually_reviewed_as_disc_shell_only": True,
        },
        "geometry_signature_before": geometry_before,
        "geometry_signature_after": geometry_after,
        "geometry_unchanged": geometry_before == geometry_after,
        "non_disc_weight_signature_before": non_target_weights_before,
        "non_disc_weight_signature_after": non_target_weights_after,
        "non_disc_weights_unchanged": (
            non_target_weights_before == non_target_weights_after
        ),
        "vertices_before": vertices_before,
        "vertices_after": vertices_after,
        "polygons_before": polygons_before,
        "polygons_after": polygons_after,
        "bone_hierarchy_unchanged": True,
        "roundtrip": roundtrip,
    }
    REPORT_PATH.write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    status = {
        "status": "APPROVED_DIRECT_UNITY_ANIMATION_SUPPORT",
        "approved_for_unity": True,
        "approved_date": "2026-07-25",
        "approval_basis": (
            "User approved disc-only skin-weight correction required for the "
            "Rebellion attack-mode transition animation."
        ),
        "unity_asset": str(UNITY_APPROVED_GLB.relative_to(PROJECT_ROOT)).replace(
            "\\", "/"
        ),
        "unity_asset_sha256": corrected_sha256,
        "geometry_modified": False,
        "bone_hierarchy_modified": False,
        "non_disc_weights_modified": False,
        "disc_vertices_reweighted": len(selected_vertices),
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
                "disc_vertices_reweighted": len(selected_vertices),
                "geometry_unchanged": geometry_before == geometry_after,
                "non_disc_weights_unchanged": (
                    non_target_weights_before == non_target_weights_after
                ),
                "roundtrip": roundtrip,
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
