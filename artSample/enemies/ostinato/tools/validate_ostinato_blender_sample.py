from __future__ import annotations

import hashlib
import json
from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[4]
SAMPLE_ROOT = ROOT / "artSample/enemies/ostinato"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx"
EXPECTED_SOURCE_HASH = "35F85E29015DE71416F5A8DD76A86424451CCF89B1C1130AC7B690E6D8B1E533"
EXPECTED_MATERIALS = {
    "Ostinato_Chitin",
    "Ostinato_SoftTissue",
    "Ostinato_HookBlade",
    "Ostinato_CompoundEye",
}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def require(condition: bool, message: str):
    if not condition:
        raise RuntimeError(message)


def main():
    require(sha256(SOURCE_FBX) == EXPECTED_SOURCE_HASH, "Source FBX hash changed")
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.name.startswith("Ostinato_CurrentModel")]
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE" and obj.name.startswith("Ostinato_CurrentModel")]
    require(len(meshes) == 1, f"Expected one sample mesh, found {len(meshes)}")
    require(len(armatures) == 1, f"Expected one sample armature, found {len(armatures)}")

    mesh_object = meshes[0]
    armature = armatures[0]
    require(len(mesh_object.data.vertices) == 2649, "Unexpected sample vertex count")
    require(len(mesh_object.data.polygons) == 5279, "Unexpected sample polygon count")
    require(len(armature.data.bones) == 24, "Unexpected armature bone count")
    require("uv" in mesh_object.data.uv_layers, "Original UV layer is missing")
    require("OstinatoSampleUV" in mesh_object.data.uv_layers, "Sample UV layer is missing")
    require({slot.name for slot in mesh_object.data.materials} == EXPECTED_MATERIALS, "Material set does not match")

    image_paths = set()
    for material in mesh_object.data.materials:
        require(material.use_nodes, f"Material has no nodes: {material.name}")
        texture_nodes = [node for node in material.node_tree.nodes if node.bl_idname == "ShaderNodeTexImage"]
        require(len(texture_nodes) == 4, f"Expected four PBR texture nodes in {material.name}")
        for node in texture_nodes:
            require(node.image is not None, f"Texture node has no image in {material.name}")
            path = Path(bpy.path.abspath(node.image.filepath))
            require(path.exists() and path.stat().st_size > 0, f"Missing texture file: {path}")
            require(tuple(node.image.size) == (1024, 1024), f"Unexpected texture size: {path}")
            image_paths.add(path)
    require(len(image_paths) == 16, f"Expected 16 distinct texture files, found {len(image_paths)}")

    required_files = [
        SAMPLE_ROOT / "index.html",
        SAMPLE_ROOT / "README.md",
        SAMPLE_ROOT / "TEXTURE_ANALYSIS.md",
        SAMPLE_ROOT / "MATERIAL_SETTINGS.md",
        SAMPLE_ROOT / "ASSET_MANIFEST.json",
        SAMPLE_ROOT / "APPROVAL_STATUS.json",
        SAMPLE_ROOT / "blender/Ostinato_CurrentModel_TexturedSample.blend",
        SAMPLE_ROOT / "exports/Ostinato_CurrentModel_TexturedSample.fbx",
        SAMPLE_ROOT / "exports/Ostinato_CurrentModel_TexturedSample.glb",
        SAMPLE_ROOT / "exports/unity/Ostinato_ApprovedUnity.fbx",
        SAMPLE_ROOT / "exports/unity/unity_bake_manifest.json",
        SAMPLE_ROOT / "renders/01_front_blender_reference_material.png",
        SAMPLE_ROOT / "renders/02_side_blender_reference_material.png",
        SAMPLE_ROOT / "renders/03_back_blender_reference_material.png",
        SAMPLE_ROOT / "renders/04_three_quarter_blender_reference_material.png",
        SAMPLE_ROOT / "renders/05_head_blade_closeup.png",
        SAMPLE_ROOT / "renders/06_abdomen_closeup.png",
        SAMPLE_ROOT / "renders/08_unity_final_comparison.png",
    ]
    for path in required_files:
        require(path.exists() and path.stat().st_size > 0, f"Missing sample artifact: {path}")

    approval = json.loads((SAMPLE_ROOT / "APPROVAL_STATUS.json").read_text(encoding="utf-8"))
    require(approval["status"] in {"pending_user_review", "approved"}, "Unexpected approval status")
    require(
        approval["unity_runtime_applied"] is (approval["status"] == "approved"),
        "Unity runtime applied flag does not match approval status",
    )

    print(f"SourceHash={sha256(SOURCE_FBX)}")
    print(f"MeshVertices={len(mesh_object.data.vertices)}")
    print(f"MeshPolygons={len(mesh_object.data.polygons)}")
    print(f"ArmatureBones={len(armature.data.bones)}")
    print(f"UvLayers={[layer.name for layer in mesh_object.data.uv_layers]}")
    print(f"Materials={sorted(EXPECTED_MATERIALS)}")
    print(f"DistinctPbrTextures={len(image_paths)}")
    print(f"ApprovalStatus={approval['status']}")
    print(f"UnityRuntimeApplied={approval['unity_runtime_applied']}")
    print("SAMPLE_VALIDATION=PASS")


if __name__ == "__main__":
    main()
