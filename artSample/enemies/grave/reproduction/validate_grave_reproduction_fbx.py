from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parent
FBX_PATH = ROOT / "grave_reproduction.fbx"
REPORT_PATH = ROOT / "review" / "fbx_validation.txt"


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(FBX_PATH))

objects = sorted((obj.name, obj.type) for obj in bpy.context.scene.objects)
require(objects == [("Grave_Body", "MESH"), ("Grave_Rig", "ARMATURE")], f"unexpected objects: {objects}")

body = bpy.data.objects["Grave_Body"]
rig = bpy.data.objects["Grave_Rig"]
dimensions = tuple(float(value) for value in body.dimensions)
materials = [material.name for material in body.data.materials]
uv_layers = [layer.name for layer in body.data.uv_layers]
armature_targets = [modifier.object.name for modifier in body.modifiers if modifier.type == "ARMATURE" and modifier.object]

require(0.70 <= dimensions[0] <= 1.00, f"width out of range: {dimensions[0]:.6f}")
require(1.55 <= dimensions[1] <= 1.65, f"height out of range: {dimensions[1]:.6f}")
require(0.45 <= dimensions[2] <= 0.55, f"depth out of range: {dimensions[2]:.6f}")
require(materials == ["Grave_Suit_Front_Mat", "Grave_Textile_BackSide_Mat"], f"unexpected materials: {materials}")
require("uv" in uv_layers and "GraveReferenceUV" in uv_layers, f"missing UV layer: {uv_layers}")
require(len(body.data.vertices) == 2296, f"unexpected vertex count: {len(body.data.vertices)}")
require(len(body.data.polygons) == 4582, f"unexpected polygon count: {len(body.data.polygons)}")
require(len(rig.data.bones) == 24, f"unexpected bone count: {len(rig.data.bones)}")
require(armature_targets == ["Grave_Rig"], f"unexpected armature target: {armature_targets}")

expected_images = {
    "grave_front_albedo.png",
    "grave_textile_albedo.png",
    "grave_fabric_normal.png",
    "grave_fabric_roughness.png",
}
linked_images = {Path(image.filepath_from_user()).name for image in bpy.data.images if image.source == "FILE"}
require(expected_images.issubset(linked_images), f"missing linked images: {sorted(expected_images - linked_images)}")

material_polygons = {
    index: sum(1 for polygon in body.data.polygons if polygon.material_index == index)
    for index in range(len(materials))
}
lines = [
    "Grave FBX 재임포트 검증: 통과",
    f"오브젝트: {objects}",
    f"치수(X 폭 / Y 높이 / Z 깊이): {dimensions[0]:.6f} / {dimensions[1]:.6f} / {dimensions[2]:.6f} m",
    f"메시: 정점 {len(body.data.vertices)}, 폴리곤 {len(body.data.polygons)}",
    f"리그: 본 {len(rig.data.bones)}, Armature 대상 {armature_targets}",
    f"머티리얼: {materials}",
    f"머티리얼별 폴리곤: {material_polygons}",
    f"UV: {uv_layers}",
    f"연결 텍스처: {sorted(expected_images)}",
]
REPORT_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("GRAVE_FBX_VALIDATION=PASS")
print(f"GRAVE_FBX_SIZE={dimensions[0]:.6f},{dimensions[1]:.6f},{dimensions[2]:.6f}")
print(f"GRAVE_FBX_REPORT={REPORT_PATH}")
