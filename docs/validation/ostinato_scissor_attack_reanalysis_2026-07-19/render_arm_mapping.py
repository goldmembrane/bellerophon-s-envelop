import bpy
from mathutils import Vector


OUTPUT_PATH = (
    "D:/Bellerophon2/Bellerophon/docs/validation/"
    "ostinato_scissor_attack_reanalysis_2026-07-19/"
    "Ostinato_ArmMapping_LeftRed_RightBlue.png"
)
ARMATURE_NAME = "Ostinato_CurrentModel_Armature"


def make_emission_material(name, color):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Emission Color"].default_value = (*color, 1.0)
    bsdf.inputs["Emission Strength"].default_value = 8.0
    bsdf.inputs["Roughness"].default_value = 0.35
    return material


def add_bone_chain(armature, names, material, prefix):
    points = []
    for index, bone_name in enumerate(names):
        pose_bone = armature.pose.bones[bone_name]
        head = armature.matrix_world @ pose_bone.head
        tail = armature.matrix_world @ pose_bone.tail
        if index == 0:
            points.append(head)
        points.append(tail)

    curve_data = bpy.data.curves.new(f"{prefix}_Curve", "CURVE")
    curve_data.dimensions = "3D"
    curve_data.bevel_depth = 0.022
    curve_data.bevel_resolution = 4
    spline = curve_data.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for spline_point, point in zip(spline.points, points):
        spline_point.co = (*point, 1.0)
    chain = bpy.data.objects.new(f"{prefix}_Chain", curve_data)
    curve_data.materials.append(material)
    bpy.context.collection.objects.link(chain)

    hand = armature.pose.bones[names[-1]]
    for suffix, position, radius in (
        ("HandHead", armature.matrix_world @ hand.head, 0.055),
        ("HandTail", armature.matrix_world @ hand.tail, 0.042),
    ):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=radius, location=position)
        marker = bpy.context.object
        marker.name = f"{prefix}_{suffix}"
        marker.data.materials.append(material)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


scene = bpy.context.scene
scene.frame_set(1)
armature = bpy.data.objects[ARMATURE_NAME]

for obj in list(bpy.data.objects):
    if obj.type in {"CAMERA", "LIGHT"}:
        bpy.data.objects.remove(obj, do_unlink=True)

left_material = make_emission_material("Mapping_Left_Red", (1.0, 0.015, 0.015))
right_material = make_emission_material("Mapping_Right_Blue", (0.015, 0.18, 1.0))
add_bone_chain(
    armature,
    ("LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand"),
    left_material,
    "Mapping_Left",
)
add_bone_chain(
    armature,
    ("RightShoulder", "RightArm", "RightForeArm", "RightHand"),
    right_material,
    "Mapping_Right",
)

bpy.ops.object.camera_add(location=(-3.65, -3.85, 2.75))
camera = bpy.context.object
camera.name = "Mapping_Camera"
camera.data.lens = 57
look_at(camera, (0.0, 0.0, 0.92))
scene.camera = camera

for name, location, energy, size in (
    ("Mapping_Key", (2.8, -3.2, 4.8), 1050.0, 4.0),
    ("Mapping_Fill", (-3.0, -1.0, 2.8), 750.0, 3.5),
    ("Mapping_Rim", (0.0, 3.0, 4.0), 900.0, 3.0),
):
    light_data = bpy.data.lights.new(name, "AREA")
    light_data.energy = energy
    light_data.shape = "DISK"
    light_data.size = size
    light = bpy.data.objects.new(name, light_data)
    light.location = location
    look_at(light, (0.0, 0.0, 0.9))
    scene.collection.objects.link(light)

scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = OUTPUT_PATH
scene.render.film_transparent = False
scene.world.color = (0.018, 0.018, 0.022)
scene.render.image_settings.color_mode = "RGBA"
bpy.ops.render.render(write_still=True)
