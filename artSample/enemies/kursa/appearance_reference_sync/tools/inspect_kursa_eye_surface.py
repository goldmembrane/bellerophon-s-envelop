import bpy
import json
from pathlib import Path


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"
SOURCE_INSPECTION = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
OUTPUT = SAMPLE_ROOT / "EYE_SURFACE_INSPECTION.json"


def rounded(values):
    return [round(float(value), 6) for value in values]


def main():
    inspection = json.loads(SOURCE_INSPECTION.read_text(encoding="utf-8"))
    mesh_info = next(item for item in inspection["objects"] if item["type"] == "MESH")
    head = next(
        component for component in mesh_info["connected_components"]
        if component["component_id"] == 7
    )
    head_polygons = set(head["polygon_indices"])

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    mesh_obj = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")

    front_records = []
    for polygon in mesh_obj.data.polygons:
        if polygon.index not in head_polygons:
            continue
        center = polygon.center
        if 148.0 <= center.y <= 161.0 and center.z >= 5.2:
            front_records.append({
                "polygon_index": polygon.index,
                "center": rounded(center),
                "normal": rounded(polygon.normal),
                "vertices": list(polygon.vertices),
            })

    bins = {}
    for record in front_records:
        x, y, z = record["center"]
        key = f"x{round(x):+03d}_y{round(y):03d}"
        bucket = bins.setdefault(key, {"x": [], "y": [], "z": [], "normal_z": [], "polygons": []})
        bucket["x"].append(x)
        bucket["y"].append(y)
        bucket["z"].append(z)
        bucket["normal_z"].append(record["normal"][2])
        bucket["polygons"].append(record["polygon_index"])

    summarized_bins = []
    for key, bucket in bins.items():
        summarized_bins.append({
            "key": key,
            "x_center": round(sum(bucket["x"]) / len(bucket["x"]), 6),
            "y_center": round(sum(bucket["y"]) / len(bucket["y"]), 6),
            "z_min": round(min(bucket["z"]), 6),
            "z_max": round(max(bucket["z"]), 6),
            "normal_z_mean": round(sum(bucket["normal_z"]) / len(bucket["normal_z"]), 6),
            "polygons": bucket["polygons"],
        })
    summarized_bins.sort(key=lambda item: (item["y_center"], item["x_center"]))

    OUTPUT.write_text(json.dumps({
        "source_model": "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx",
        "head_component_id": 7,
        "coordinate_space": "Imported mesh local coordinates; X=screen horizontal, Y=vertical, +Z=front",
        "current_face_classifier": {"y_min": 143.0, "y_max": 160.5, "z_min": 5.2},
        "head_bounds_local": head["bounds_local"],
        "front_polygon_count": len(front_records),
        "front_polygons": front_records,
        "one_unit_bins": summarized_bins,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(OUTPUT), "front_polygon_count": len(front_records)}))


if __name__ == "__main__":
    main()
