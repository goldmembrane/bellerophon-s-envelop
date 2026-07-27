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


def material(name, color):
    value = bpy.data.materials.new(name)
    value.use_nodes = True
    nodes = value.node_tree.nodes
    links = value.node_tree.links
    for node in list(nodes):
        nodes.remove(node)
    output = nodes.new("ShaderNodeOutputMaterial")
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = color
    emission.inputs["Strength"].default_value = 1.0
    links.new(
        emission.outputs["Emission"],
        output.inputs["Surface"],
    )
    return value


def point_at(obj, target):
    obj.rotation_euler = (
        Vector(target) - obj.location
    ).to_track_quat("-Z", "Y").to_euler()


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source_path)
mesh_objects = [
    obj
    for obj in bpy.context.scene.objects
    if obj.type == "MESH"
]
if len(mesh_objects) != 1:
    raise RuntimeError(
        f"Expected one mesh, found {len(mesh_objects)}."
    )
mesh_object = mesh_objects[0]

with open(
    os.path.join(sample_root, "MESH_COMPONENTS.json"),
    "r",
    encoding="utf-8",
) as handle:
    components = json.load(handle)["components"]

component_by_polygon = {}
for component in components:
    for polygon_index in component["polygon_indices"]:
        component_by_polygon[polygon_index] = (
            component["component_id"]
        )

materials = [
    material(
        "OtherGeometry",
        (0.035, 0.045, 0.055, 1.0),
    ),
    material(
        "TorsoPrimary_Component0",
        (0.85, 0.06, 0.04, 1.0),
    ),
    material(
        "TorsoSecondary_Component1",
        (0.04, 0.78, 0.18, 1.0),
    ),
    material(
        "TorsoOptic_Component7And9",
        (0.03, 0.32, 1.0, 1.0),
    ),
    material(
        "TorsoCopperComponents",
        (1.0, 0.32, 0.02, 1.0),
    ),
]
mesh_object.data.materials.clear()
for value in materials:
    mesh_object.data.materials.append(value)

for polygon in mesh_object.data.polygons:
    component_id = component_by_polygon[
        polygon.index
    ]
    material_index = 0
    if component_id == 0:
        material_index = 1
    elif component_id == 1:
        material_index = 2
    elif component_id in {7, 9}:
        material_index = 3
    elif component_id in {44, 48, 54, 57}:
        material_index = 4
    polygon.material_index = material_index

points = [
    mesh_object.matrix_world @ Vector(corner)
    for corner in mesh_object.bound_box
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
center = (low + high) * 0.5
dimensions = high - low
radius = max(dimensions) * 1.65

camera_data = bpy.data.cameras.new(
    "TorsoMaskCamera"
)
camera = bpy.data.objects.new(
    "TorsoMaskCamera",
    camera_data,
)
bpy.context.collection.objects.link(camera)
bpy.context.scene.camera = camera
camera_data.type = "ORTHO"
camera_data.ortho_scale = (
    max(dimensions.x, dimensions.z) *
    1.22
)
camera.location = center + Vector((
    0.0,
    -radius,
    radius * 0.12,
))
point_at(camera, center)

world = bpy.data.worlds.new("MaskWorld")
world.use_nodes = True
world.node_tree.nodes[
    "Background"
].inputs["Color"].default_value = (
    0.01,
    0.012,
    0.015,
    1.0,
)
world.node_tree.nodes[
    "Background"
].inputs["Strength"].default_value = 0.1
bpy.context.scene.world = world

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.view_settings.look = "AgX - Medium High Contrast"
scene.render.filepath = os.path.join(
    render_root,
    "09_torso_component_masks.png",
)
bpy.ops.render.render(write_still=True)
