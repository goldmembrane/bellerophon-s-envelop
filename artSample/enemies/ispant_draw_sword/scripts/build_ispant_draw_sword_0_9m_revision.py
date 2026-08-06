import hashlib
import json
from pathlib import Path

import bpy


PROJECT_ROOT = Path(__file__).resolve().parents[4]
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / "ispant_draw_sword"
REVISION_DIR = SAMPLE_ROOT / "length_0_9m_revision"
SOURCE_BLEND = SAMPLE_ROOT / "Ispant_DrawSword_ArtSample.blend"
OUTPUT_BLEND = REVISION_DIR / "Ispant_DrawSword_0_9m_ArtSample.blend"
OUTPUT_FBX = REVISION_DIR / "Ispant_DrawSword_0_9m_ArtSample.fbx"
OUTPUT_GLB = REVISION_DIR / "Ispant_DrawSword_0_9m_ArtSample.glb"
REPORT_PATH = REVISION_DIR / "Ispant_DrawSword_0_9m_ArtSample_Report.json"
EXPECTED_SOURCE_SHA256 = "F112EFF207D2EAB5FC89AF5735103877B51F973FBAD9B5EBD8D3DBEB44770FB9"
EXPECTED_VERTICES = 2080
EXPECTED_TRIANGLES = 4092
ORIGINAL_BLADE_LENGTH = 0.82
TARGET_BLADE_LENGTH = 0.6485
TARGET_OVERALL_LENGTH = 0.9
BLADE_SCALE = TARGET_BLADE_LENGTH / ORIGINAL_BLADE_LENGTH
PRESERVED_HILT_MAX_Z = 0.017


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def connected_components(mesh):
    adjacency = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        first, second = edge.vertices
        adjacency[first].add(second)
        adjacency[second].add(first)
    pending = set(range(len(mesh.vertices)))
    result = []
    while pending:
        seed = min(pending)
        pending.remove(seed)
        stack = [seed]
        component = []
        while stack:
            current = stack.pop()
            component.append(current)
            for neighbor in adjacency[current]:
                if neighbor in pending:
                    pending.remove(neighbor)
                    stack.append(neighbor)
        result.append(tuple(sorted(component)))
    return result


def subset_signature(mesh, vertex_indices, include_positions):
    selected = set(vertex_indices)
    payload = {
        "vertices": [
            (
                index,
                tuple(round(value, 9) for value in mesh.vertices[index].co)
                if include_positions
                else None,
            )
            for index in sorted(selected)
        ],
        "polygons": [],
    }
    uv_layer = mesh.uv_layers.active
    if uv_layer is None:
        raise RuntimeError("The approved sword UV layer is missing.")
    for polygon in mesh.polygons:
        if not all(index in selected for index in polygon.vertices):
            continue
        payload["polygons"].append(
            {
                "vertices": list(polygon.vertices),
                "material": polygon.material_index,
                "uv": [
                    tuple(round(value, 9) for value in uv_layer.data[loop_index].uv)
                    for loop_index in polygon.loop_indices
                ],
            }
        )
    encoded = json.dumps(payload, separators=(",", ":"), sort_keys=True).encode("utf-8")
    return {
        "vertex_count": len(selected),
        "polygon_count": len(payload["polygons"]),
        "sha256": hashlib.sha256(encoded).hexdigest().upper(),
    }


def triangle_count(mesh):
    return sum(len(polygon.vertices) - 2 for polygon in mesh.polygons)


def export_revision(armature, meshes):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in [armature] + meshes:
        obj.hide_set(False)
        obj.hide_render = False
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.gltf(
        filepath=str(OUTPUT_GLB),
        export_format="GLB",
        use_selection=True,
        export_animations=True,
        export_apply=False,
        export_cameras=False,
        export_lights=False,
        export_extras=True,
    )
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_FBX),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode="COPY",
        embed_textures=True,
    )


def main():
    REVISION_DIR.mkdir(parents=True, exist_ok=True)
    source_hash = sha256(SOURCE_BLEND)
    if source_hash != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("The approved 1.0715 m source BLEND hash differs.")
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    sword = bpy.data.objects.get("Ispant_Reference_LongSword")
    armature = bpy.data.objects.get("Armature")
    if sword is None or armature is None:
        raise RuntimeError("The approved sword sample structure differs.")
    if len(sword.data.vertices) != EXPECTED_VERTICES or triangle_count(sword.data) != EXPECTED_TRIANGLES:
        raise RuntimeError("The approved sword topology differs.")
    if abs(sword.dimensions.z - 1.0715) > 0.00001:
        raise RuntimeError("The approved source sword length differs.")

    components = connected_components(sword.data)
    blade_components = [
        component
        for component in components
        if max(sword.data.vertices[index].co.z for index in component) > PRESERVED_HILT_MAX_Z
    ]
    if len(blade_components) != 3:
        raise RuntimeError(f"Expected blade plus two etching components, found {len(blade_components)}.")
    blade_indices = {index for component in blade_components for index in component}
    hilt_indices = set(range(len(sword.data.vertices))) - blade_indices
    hilt_before = subset_signature(sword.data, hilt_indices, include_positions=True)
    blade_uv_before = subset_signature(sword.data, blade_indices, include_positions=False)

    for index in blade_indices:
        sword.data.vertices[index].co.z *= BLADE_SCALE
    sword.data.update()
    bpy.context.view_layer.update()
    sword["blade_length_m"] = TARGET_BLADE_LENGTH
    sword["grip_length_m"] = 0.17
    sword["guard_width_m"] = 0.19
    sword["pommel_length_m"] = 0.055
    sword["overall_length_m"] = TARGET_OVERALL_LENGTH
    sword["revision_rule"] = "Blade vertices only; approved hilt geometry and UV preserved"

    hilt_after = subset_signature(sword.data, hilt_indices, include_positions=True)
    blade_uv_after = subset_signature(sword.data, blade_indices, include_positions=False)
    if hilt_before != hilt_after:
        raise RuntimeError("The approved hilt geometry or UV changed.")
    if blade_uv_before != blade_uv_after:
        raise RuntimeError("The approved blade topology, material assignment, or UV changed.")
    local_z = [vertex.co.z for vertex in sword.data.vertices]
    measured_length = max(local_z) - min(local_z)
    if abs(measured_length - TARGET_OVERALL_LENGTH) > 0.00001:
        raise RuntimeError(f"The revised sword length differs: {measured_length}.")
    if len(sword.data.vertices) != EXPECTED_VERTICES or triangle_count(sword.data) != EXPECTED_TRIANGLES:
        raise RuntimeError("The revised sword topology changed.")

    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND), check_existing=False)
    export_meshes = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
        and obj.name not in {"Ispant_DrawSword_RigidSword", "Ispant_DrawSword_RigidSheath"}
    ]
    export_revision(armature, export_meshes)
    report = {
        "result": "PASS",
        "approval_status": "PENDING_USER_APPROVAL",
        "unity_runtime_applied": False,
        "source_approved_blend": str(SOURCE_BLEND),
        "source_approved_blend_sha256": source_hash,
        "original_overall_length_m": 1.0715,
        "target_overall_length_m": TARGET_OVERALL_LENGTH,
        "measured_overall_length_m": measured_length,
        "original_blade_length_m": ORIGINAL_BLADE_LENGTH,
        "target_blade_length_m": TARGET_BLADE_LENGTH,
        "blade_scale": BLADE_SCALE,
        "grip_length_m": 0.17,
        "guard_width_m": 0.19,
        "pommel_length_m": 0.055,
        "vertices": len(sword.data.vertices),
        "triangles": triangle_count(sword.data),
        "blade_component_count": len(blade_components),
        "hilt_signature_before": hilt_before,
        "hilt_signature_after": hilt_after,
        "hilt_geometry_and_uv_preserved": hilt_before == hilt_after,
        "blade_uv_signature_before": blade_uv_before,
        "blade_uv_signature_after": blade_uv_after,
        "blade_topology_material_and_uv_preserved": blade_uv_before == blade_uv_after,
        "output_blend": str(OUTPUT_BLEND),
        "output_blend_sha256": sha256(OUTPUT_BLEND),
        "output_fbx": str(OUTPUT_FBX),
        "output_fbx_sha256": sha256(OUTPUT_FBX),
        "output_glb": str(OUTPUT_GLB),
        "output_glb_sha256": sha256(OUTPUT_GLB),
    }
    REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print("ISPANT_DRAW_SWORD_0_9M_REVISION=PASS")
    print(f"ISPANT_DRAW_SWORD_0_9M_LENGTH={measured_length}")
    print(f"ISPANT_DRAW_SWORD_0_9M_HILT_SHA256={hilt_after['sha256']}")


if __name__ == "__main__":
    main()
