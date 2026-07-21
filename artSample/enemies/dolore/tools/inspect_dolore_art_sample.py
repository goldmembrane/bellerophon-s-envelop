import bpy
import hashlib
import json
import struct
from mathutils import Vector
from pathlib import Path


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample" / "enemies" / "dolore"
BLEND_PATH = SAMPLE_ROOT / "blender" / "Dolore_CurrentModel_ReferenceSync.blend"
FBX_PATH = SAMPLE_ROOT / "exports" / "Dolore_CurrentModel_ReferenceSync.fbx"
GLB_PATH = SAMPLE_ROOT / "exports" / "Dolore_CurrentModel_ReferenceSync.glb"
SOURCE_FBX = ROOT / "enemies model" / "dolore.fbx"


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
        for vertex_index in polygon.vertices:
            topology_digest.update(struct.pack("<I", vertex_index))
    return {
        "vertices": len(mesh.vertices),
        "edges": len(mesh.edges),
        "polygons": len(mesh.polygons),
        "vertex_position_sha256": vertex_digest.hexdigest().upper(),
        "polygon_topology_sha256": topology_digest.hexdigest().upper(),
    }


def transform_signature(obj):
    return {
        "location": [round(value, 9) for value in obj.location],
        "rotation_euler": [round(value, 9) for value in obj.rotation_euler],
        "scale": [round(value, 9) for value in obj.scale],
    }


def world_bounds(mesh_objects):
    points = []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in mesh_objects:
        evaluated = obj.evaluated_get(depsgraph)
        points.extend(evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return minimum, maximum


def import_source_signature():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX), use_anim=False)
    mesh = bpy.data.objects.get("char1")
    armature = bpy.data.objects.get("Armature")
    if mesh is None or armature is None:
        raise RuntimeError("Source FBX is missing char1 or Armature.")
    minimum, maximum = world_bounds([mesh])
    return {
        "geometry": mesh_signature(mesh.data),
        "transform": transform_signature(mesh),
        "bones": len(armature.data.bones),
        "size_m": [round(value, 6) for value in (maximum - minimum)],
    }


def scene_summary(label):
    mesh_objects = [
        obj for obj in bpy.context.scene.objects
        if obj.type == "MESH" and not any(collection.name == "Review_Stage" for collection in obj.users_collection)
    ]
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    minimum, maximum = world_bounds(mesh_objects)
    materials = sorted({material.name for obj in mesh_objects for material in obj.data.materials if material})
    return {
        "label": label,
        "mesh_objects": len(mesh_objects),
        "mesh_names": sorted(obj.name for obj in mesh_objects),
        "armatures": len(armatures),
        "bones": max((len(obj.data.bones) for obj in armatures), default=0),
        "materials": materials,
        "uv_mesh_count": sum(1 for obj in mesh_objects if len(obj.data.uv_layers) > 0),
        "size_m": [round(value, 6) for value in (maximum - minimum)],
    }


def inspect_blend():
    bpy.ops.wm.open_mainfile(filepath=str(BLEND_PATH))
    mesh = bpy.data.objects.get("Dolore_CurrentModel")
    armature = bpy.data.objects.get("Dolore_Rig")
    if mesh is None or armature is None:
        raise RuntimeError("Blend file is missing Dolore_CurrentModel or Dolore_Rig.")
    summary = scene_summary("blend")
    summary.update({
        "geometry": mesh_signature(mesh.data),
        "transform": transform_signature(mesh),
        "uv_layers": len(mesh.data.uv_layers),
        "model_modifier_types": [modifier.type for modifier in mesh.modifiers],
    })
    return summary


def inspect_fbx():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(FBX_PATH), use_anim=False)
    return scene_summary("fbx")


def inspect_glb():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(GLB_PATH))
    return scene_summary("glb")


def main():
    required_files = [
        BLEND_PATH,
        FBX_PATH,
        GLB_PATH,
        SAMPLE_ROOT / "textures" / "dolore_body_albedo.png",
        SAMPLE_ROOT / "textures" / "dolore_body_roughness.png",
        SAMPLE_ROOT / "textures" / "dolore_body_height.png",
        SAMPLE_ROOT / "textures" / "dolore_frame_albedo.png",
        SAMPLE_ROOT / "textures" / "dolore_frame_roughness.png",
        SAMPLE_ROOT / "textures" / "dolore_frame_height.png",
        SAMPLE_ROOT / "textures" / "dolore_portrait.png",
        SAMPLE_ROOT / "renders" / "01_reference_matched_three_quarter.png",
        SAMPLE_ROOT / "renders" / "02_front.png",
        SAMPLE_ROOT / "renders" / "03_side.png",
        SAMPLE_ROOT / "renders" / "04_back.png",
        SAMPLE_ROOT / "renders" / "05_material_closeup.png",
        SAMPLE_ROOT / "renders" / "06_reference_comparison_static.png",
    ]
    missing = [str(path) for path in required_files if not path.is_file() or path.stat().st_size == 0]
    source = import_source_signature()
    blend = inspect_blend()
    fbx = inspect_fbx()
    glb = inspect_glb()
    checks = {
        "required_files_present": not missing,
        "blend_geometry_exactly_preserved": blend["geometry"] == source["geometry"],
        "blend_transform_exactly_preserved": blend["transform"] == source["transform"],
        "blend_single_source_mesh": blend["mesh_objects"] == 1,
        "blend_only_source_armature_modifier": blend["model_modifier_types"] == ["ARMATURE"],
        "blend_rig_preserved": blend["bones"] == source["bones"] == 27,
        "blend_has_uv_for_materials": blend["uv_layers"] >= 1,
        "blend_has_three_reference_materials": len(blend["materials"]) == 3,
        "blend_size_unchanged": blend["size_m"] == source["size_m"],
        "fbx_single_model_mesh": fbx["mesh_objects"] == 1,
        "fbx_rig_preserved": fbx["bones"] == 27,
        "fbx_has_materials": len(fbx["materials"]) == 3,
        "glb_single_model_mesh": glb["mesh_objects"] == 1,
        "glb_has_materials": len(glb["materials"]) == 3,
    }
    result = "PASS" if all(checks.values()) else "FAIL"
    report = {
        "result": result,
        "source_fbx_sha256": sha256(SOURCE_FBX),
        "geometry_policy": "source_mesh_only_no_shape_changes",
        "source": source,
        "blend": blend,
        "fbx": fbx,
        "glb": glb,
        "missing": missing,
        "checks": checks,
    }
    with (SAMPLE_ROOT / "SAMPLE_INSPECTION.json").open("w", encoding="utf-8") as handle:
        json.dump(report, handle, ensure_ascii=False, indent=2)
    lines = [f"Result={result}", f"SourceFbxSha256={report['source_fbx_sha256']}"]
    for name, passed in checks.items():
        lines.append(f"Check.{name}={passed}")
    (SAMPLE_ROOT / "SAMPLE_INSPECTION.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"DOLORE_MATERIAL_SYNC_INSPECTION={result}")
    if result != "PASS":
        raise RuntimeError("Dolore material synchronization inspection failed.")


if __name__ == "__main__":
    main()
