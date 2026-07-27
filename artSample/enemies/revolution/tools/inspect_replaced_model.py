import bpy
import json
import math
import os
import sys
from mathutils import Vector


marker = sys.argv.index("--") + 1
source_path = sys.argv[marker]
sample_root = sys.argv[marker + 1]
render_root = os.path.join(sample_root, "renders")
os.makedirs(render_root, exist_ok=True)


def bounds_of(objects):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects
        if obj.type == "MESH"
        for corner in obj.bound_box
    ]
    low = Vector((
        min(point.x for point in points),
        min(point.y for point in points),
        min(point.z for point in points),
    ))
    high = Vector((
        max(point.x for point in points),
        max(point.y for point in points),
        max(point.z for point in points),
    ))
    return low, high


def point_at(obj, target):
    obj.rotation_euler = (
        Vector(target) - obj.location
    ).to_track_quat("-Z", "Y").to_euler()


def render_view(scene, camera, center, radius, angle_degrees, output_path):
    angle = math.radians(angle_degrees)
    camera.location = center + Vector((
        radius * math.sin(angle),
        -radius * math.cos(angle),
        radius * 0.12,
    ))
    point_at(camera, center)
    scene.render.filepath = output_path
    bpy.ops.render.render(write_still=True)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source_path)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
armatures = [
    obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"
]
if len(meshes) != 1 or len(armatures) != 1:
    raise RuntimeError(
        f"Expected one mesh and one armature; got {len(meshes)} and {len(armatures)}."
    )

mesh_object = meshes[0]
armature_object = armatures[0]
low, high = bounds_of(meshes)
dimensions = high - low
center = (low + high) * 0.5

report = {
    "source": source_path,
    "mesh_name": mesh_object.name,
    "vertices": len(mesh_object.data.vertices),
    "polygons": len(mesh_object.data.polygons),
    "loops": len(mesh_object.data.loops),
    "triangles": sum(
        len(polygon.vertices) - 2
        for polygon in mesh_object.data.polygons
    ),
    "uv_layers": [
        layer.name for layer in mesh_object.data.uv_layers
    ],
    "material_slots": [
        slot.material.name if slot.material else None
        for slot in mesh_object.material_slots
    ],
    "bones": len(armature_object.data.bones),
    "bone_names": [
        bone.name for bone in armature_object.data.bones
    ],
    "actions": [
        action.name for action in bpy.data.actions
    ],
    "bounds": {
        "minimum": list(low),
        "maximum": list(high),
        "dimensions": list(dimensions),
    },
    "vertex_groups": [
        group.name for group in mesh_object.vertex_groups
    ],
}
with open(
    os.path.join(sample_root, "SOURCE_MODEL_INSPECTION.json"),
    "w",
    encoding="utf-8",
) as handle:
    json.dump(report, handle, ensure_ascii=False, indent=2)

material = bpy.data.materials.new("ReplacedModelNeutral")
material.use_nodes = True
shader = material.node_tree.nodes.get("Principled BSDF")
shader.inputs["Base Color"].default_value = (0.24, 0.27, 0.30, 1.0)
shader.inputs["Metallic"].default_value = 0.55
shader.inputs["Roughness"].default_value = 0.42
mesh_object.data.materials.clear()
mesh_object.data.materials.append(material)

world = bpy.data.worlds.new("InspectionWorld")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (
    0.04,
    0.05,
    0.07,
    1.0,
)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.3
bpy.context.scene.world = world

for location, energy, size in [
    ((-4.5, -5.5, 6.5), 1250, 5.0),
    ((4.5, -1.5, 3.5), 850, 4.0),
    ((0.0, 5.0, 5.0), 1000, 3.0),
]:
    light_data = bpy.data.lights.new("InspectionLight", "AREA")
    light_data.energy = energy
    light_data.shape = "DISK"
    light_data.size = size
    light = bpy.data.objects.new("InspectionLight", light_data)
    bpy.context.collection.objects.link(light)
    light.location = location
    point_at(light, center)

camera_data = bpy.data.cameras.new("InspectionCamera")
camera = bpy.data.objects.new("InspectionCamera", camera_data)
bpy.context.collection.objects.link(camera)
bpy.context.scene.camera = camera
camera_data.type = "ORTHO"
camera_data.ortho_scale = max(dimensions.x, dimensions.z) * 1.22

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 800
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.view_settings.look = "AgX - Medium High Contrast"

radius = max(dimensions) * 1.65
for file_name, angle in [
    ("00_source_front_neutral.png", 0),
    ("00_source_three_quarter_neutral.png", -35),
    ("00_source_side_neutral.png", -90),
    ("00_source_rear_neutral.png", 180),
]:
    render_view(
        scene,
        camera,
        center,
        radius,
        angle,
        os.path.join(render_root, file_name),
    )
