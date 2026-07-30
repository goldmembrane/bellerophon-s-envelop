import bpy
import hashlib
import json
from pathlib import Path

from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
SOURCE = ROOT / "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx"
BLEND = SAMPLE_ROOT / "blender/Pahur_Appearance_ReferenceSync.blend"
FBX = SAMPLE_ROOT / "exports/Pahur_Appearance_ReferenceSync.fbx"
GLB = SAMPLE_ROOT / "exports/Pahur_Appearance_ReferenceSync.glb"
OUTPUT = SAMPLE_ROOT / "EXPORT_INSPECTION.json"
PRESERVATION = SAMPLE_ROOT / "GEOMETRY_PRESERVATION.json"


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def world_bounds(meshes):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in meshes
        for corner in obj.bound_box
    ]
    return {
        "min": [
            round(min(point[axis] for point in points), 6) for axis in range(3)
        ],
        "max": [
            round(max(point[axis] for point in points), 6) for axis in range(3)
        ],
    }


def inspect(path, kind):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if kind == "blend":
        bpy.ops.wm.open_mainfile(filepath=str(path), load_ui=False)
    elif kind == "fbx":
        bpy.ops.import_scene.fbx(filepath=str(path))
    elif kind == "glb":
        bpy.ops.import_scene.gltf(filepath=str(path))
    else:
        raise ValueError(kind)

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    armatures = [
        obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"
    ]
    model_meshes = [obj for obj in meshes if obj.name != "Review_Only_Floor"]
    return {
        "path": str(path.relative_to(ROOT)).replace("\\", "/"),
        "bytes": path.stat().st_size,
        "sha256": sha256(path),
        "mesh_objects": len(model_meshes),
        "vertices": sum(len(obj.data.vertices) for obj in model_meshes),
        "edges": sum(len(obj.data.edges) for obj in model_meshes),
        "polygons": sum(len(obj.data.polygons) for obj in model_meshes),
        "loops": sum(len(obj.data.loops) for obj in model_meshes),
        "uv_layers": sorted(
            {
                layer.name
                for obj in model_meshes
                for layer in obj.data.uv_layers
            }
        ),
        "material_slots": sum(
            len(obj.material_slots) for obj in model_meshes
        ),
        "vertex_groups": sorted(
            {
                group.name
                for obj in model_meshes
                for group in obj.vertex_groups
            }
        ),
        "armature_objects": len(armatures),
        "bones": sum(len(obj.data.bones) for obj in armatures),
        "actions": [
            {
                "name": action.name,
                "frame_range": [
                    round(action.frame_range[0], 6),
                    round(action.frame_range[1], 6),
                ],
            }
            for action in bpy.data.actions
        ],
        "world_bounds": world_bounds(model_meshes),
    }


def main():
    source = inspect(SOURCE, "fbx")
    blend = inspect(BLEND, "blend")
    fbx = inspect(FBX, "fbx")
    glb = inspect(GLB, "glb")
    preservation = json.loads(PRESERVATION.read_text(encoding="utf-8"))

    source_shape = {
        key: source[key]
        for key in ("vertices", "edges", "polygons", "loops", "bones")
    }
    blend_shape = {
        key: blend[key]
        for key in ("vertices", "edges", "polygons", "loops", "bones")
    }
    fbx_shape = {
        key: fbx[key]
        for key in ("vertices", "edges", "polygons", "loops", "bones")
    }
    glb_shape = {
        key: glb[key]
        for key in ("vertices", "edges", "polygons", "loops", "bones")
    }
    expected_shape = {
        **preservation["expected_after_shape"],
        "bones": source["bones"],
    }
    report = {
        "result": "PASS"
        if blend_shape == fbx_shape == expected_shape
        and preservation["result"] == "PASS"
        and preservation["approved_deletion"]["component_ids"] == [97]
        and preservation["approved_deletion"]["remaining_content_unchanged"]
        and source["vertex_groups"] == blend["vertex_groups"] == fbx["vertex_groups"]
        and source["uv_layers"] == blend["uv_layers"] == fbx["uv_layers"]
        else "FAIL",
        "canonical_structural_check": (
            "Review Blend and FBX must match the source after deleting only "
            "approved independent connected component 97. All remaining "
            "geometry content, vertex groups, UV layers, and bones are preserved."
        ),
        "approved_deletion": preservation["approved_deletion"],
        "glb_note": (
            "GLB is a portable visual-review export. Blender's glTF exporter "
            "limits skin influences to four and may triangulate/split primitives; "
            "the Blend and FBX are the canonical structure-preserving samples."
        ),
        "source": source,
        "blend": blend,
        "fbx": fbx,
        "glb": glb,
        "shape_comparison": {
            "source": source_shape,
            "expected_after_approved_deletion": expected_shape,
            "blend": blend_shape,
            "fbx": fbx_shape,
            "glb": glb_shape,
        },
    }
    OUTPUT.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(json.dumps(report, ensure_ascii=False, indent=2))
    if report["result"] != "PASS":
        raise RuntimeError("Canonical export structure check failed.")


if __name__ == "__main__":
    main()
