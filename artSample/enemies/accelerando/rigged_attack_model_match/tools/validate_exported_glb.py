import bpy
import json
from pathlib import Path


PROJECT_ROOT = Path(r"D:\Bellerophon2\Bellerophon")
GLB_PATH = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "accelerando"
    / "rigged_attack_model_match"
    / "exports"
    / "accelerando_rigged_attack_model_match.glb"
)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(GLB_PATH))

armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
if len(armatures) != 1:
    raise RuntimeError(f"Expected one armature, found {len(armatures)}.")
armature = armatures[0]
if len(armature.data.bones) != 18:
    raise RuntimeError(f"Expected 18 bones, found {len(armature.data.bones)}.")

body = next(
    (
        obj for obj in bpy.context.scene.objects
        if obj.type == "MESH" and "RiggedAttack_Body" in obj.name
    ),
    None,
)
if body is None:
    raise RuntimeError("Exported rigged body mesh is missing.")
if len(body.vertex_groups) != 18:
    raise RuntimeError(f"Expected 18 body vertex groups, found {len(body.vertex_groups)}.")
if not any(modifier.type == "ARMATURE" for modifier in body.modifiers):
    raise RuntimeError("Exported body has no Armature modifier.")

link_counts = {}
for side_name in ("Left", "Right"):
    link_counts[side_name] = sum(
        1
        for obj in bpy.context.scene.objects
        if obj.name.startswith(f"Accelerando_{side_name}_ConnectedChain_Link_")
    )
    if link_counts[side_name] != 12:
        raise RuntimeError(f"{side_name} exported link count is {link_counts[side_name]}.")
    if bpy.data.objects.get(f"Accelerando_{side_name}_MaceHead") is None:
        raise RuntimeError(f"{side_name} exported mace head is missing.")

socket_rings = [obj.name for obj in bpy.context.scene.objects if obj.name.endswith("MaceSocket_Ring")]
if socket_rings:
    raise RuntimeError(f"Visible socket ring objects remain: {socket_rings}")

weighted_vertices = sum(1 for vertex in body.data.vertices if vertex.groups)
if weighted_vertices <= 0:
    raise RuntimeError("Exported body contains no weighted vertices.")

report = {
    "glb": str(GLB_PATH),
    "armature": armature.name,
    "bone_count": len(armature.data.bones),
    "body": body.name,
    "body_vertices": len(body.data.vertices),
    "body_polygons": len(body.data.polygons),
    "body_vertex_groups": len(body.vertex_groups),
    "weighted_vertices": weighted_vertices,
    "chain_links": link_counts,
    "visible_mace_socket_ring_count": len(socket_rings),
    "mace_heads": [
        bpy.data.objects[f"Accelerando_{side_name}_MaceHead"].name
        for side_name in ("Left", "Right")
    ],
    "material_names": sorted(
        {
            material.name
            for obj in bpy.context.scene.objects
            if obj.type == "MESH"
            for material in obj.data.materials
            if material is not None
        }
    ),
}
print("ACCELERANDO_EXPORTED_GLB_VALIDATION_BEGIN")
print(json.dumps(report, ensure_ascii=False, indent=2))
print("ACCELERANDO_EXPORTED_GLB_VALIDATION_END")
