from pathlib import Path

import bpy
from mathutils import Vector


GRAVE_ROOT = Path(__file__).resolve().parents[1]
REPRODUCTION_ROOT = GRAVE_ROOT / "reproduction"
RENDER_ROOT = GRAVE_ROOT / "renders"
EXPORT_ROOT = GRAVE_ROOT / "exports"


def point_at(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def render_view(camera, filename, location, target, ortho_scale):
    camera.location = location
    camera.data.ortho_scale = ortho_scale
    point_at(camera, target)
    bpy.context.scene.render.filepath = str(RENDER_ROOT / filename)
    bpy.ops.render.render(write_still=True)


RENDER_ROOT.mkdir(parents=True, exist_ok=True)
EXPORT_ROOT.mkdir(parents=True, exist_ok=True)

body = bpy.data.objects.get("Grave_Body")
rig = bpy.data.objects.get("Grave_Rig")
camera = bpy.data.objects.get("Grave_Review_Camera")
if body is None or rig is None or camera is None:
    raise RuntimeError("그라베 재현 Blend에서 메시, 리그 또는 검토 카메라를 찾지 못했습니다.")

scene = bpy.context.scene
scene.camera = camera
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1408
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = True
scene.render.use_freestyle = False
backdrop = bpy.data.objects.get("Review_Backdrop")
if backdrop is not None:
    backdrop.hide_render = True

bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
# 편집기에서 본 위치를 표시하는 사용자 지정 Icosphere는 캐릭터 부품이 아니므로 GLB에서 제외합니다.
for pose_bone in rig.pose.bones:
    pose_bone.custom_shape = None
custom_shape_object = bpy.data.objects.get("Icosphere")
if custom_shape_object is not None:
    bpy.data.objects.remove(custom_shape_object, do_unlink=True)
bpy.ops.export_scene.gltf(
    filepath=str(EXPORT_ROOT / "grave_reference_reproduction.glb"),
    export_format="GLB",
    use_selection=True,
    export_animations=False,
    export_apply=True,
)

render_view(
    camera,
    "02_side_grave_inferred_surface_rgba.png",
    Vector((-5.0, -1.35, 0.82)),
    Vector((0.0, 0.0, 0.82)),
    3.28,
)
render_view(
    camera,
    "04_three_quarter_grave_material_rgba.png",
    Vector((-3.8, -4.5, 1.55)),
    Vector((0.0, 0.0, 0.82)),
    3.08,
)
render_view(
    camera,
    "05_close_grave_suit_application_rgba.png",
    Vector((0.016, -5.0, 0.96)),
    Vector((0.016, 0.0, 0.96)),
    2.10,
)

print(f"GRAVE_REVIEW_GLB={EXPORT_ROOT / 'grave_reference_reproduction.glb'}")
print(f"GRAVE_REVIEW_RENDERS={RENDER_ROOT}")
