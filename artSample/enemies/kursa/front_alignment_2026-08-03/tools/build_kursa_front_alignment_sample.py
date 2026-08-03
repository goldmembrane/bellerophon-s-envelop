import bpy
import bmesh
import json
import math
from collections import Counter
from pathlib import Path

from mathutils import Matrix, Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/front_alignment_2026-08-03"
APPROVED_SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
APPROVED_BLEND = (
    APPROVED_SAMPLE_ROOT / "blender/Kursa_Appearance_ReferenceSync.blend"
)
RUNTIME_FBX = (
    ROOT
    / "Assets/_Project/Art/Enemies/Kursa/ApprovedAppearance/Models/"
    / "Kursa_Appearance_RuntimeProjection.fbx"
)
RENDER_DIR = SAMPLE_ROOT / "renders"
BLENDER_DIR = SAMPLE_ROOT / "blender"
REPORT_PATH = SAMPLE_ROOT / "KURSA_FRONT_ALIGNMENT_SAMPLE.json"

MATERIAL_NAMES = (
    "Kursa_Armor_Gunmetal",
    "Kursa_Armor_BlueGray",
    "Kursa_Light_Steel",
    "Kursa_Dark_Mechanics",
    "Kursa_Torso_Mechanical_Plates",
    "Kursa_Hood_Navy_Cloth",
    "Kursa_Face_Metal_Blue_Optics",
    "Kursa_Shield_Worn_Gunmetal",
    "Kursa_Shield_Frame_Steel",
)
EYE_PATCH_SIZE = {
    "left": (3.45, 3.45),
    "right": (3.95, 3.95),
}
EYE_PATCH_DEPTH = 2.05
FACE_MATERIAL_INDEX = 6
LEFT_ORBIT_POLYGON = 3801
RIGHT_ORBIT_POLYGON = 3627
MODEL_FRONT_ARMATURE = Vector((0.0, -1.0, 0.0))
VISUAL_FACE_YAW_OFFSET_DEGREES = 22.0


def rounded_vector(value):
    return [round(float(component), 7) for component in value]


def point_at(item, target):
    item.rotation_euler = (Vector(target) - item.location).to_track_quat(
        "-Z", "Y"
    ).to_euler()


def evaluated_mesh(mesh_object):
    dependency_graph = bpy.context.evaluated_depsgraph_get()
    evaluated_object = mesh_object.evaluated_get(dependency_graph)
    result = evaluated_object.to_mesh()
    return evaluated_object, result


def mesh_bounds_world(mesh_object):
    evaluated_object, mesh = evaluated_mesh(mesh_object)
    try:
        points = [evaluated_object.matrix_world @ vertex.co for vertex in mesh.vertices]
        low = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
        high = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
        return low, high
    finally:
        evaluated_object.to_mesh_clear()


def face_geometry_frame(mesh_object, armature_object):
    evaluated_object, mesh = evaluated_mesh(mesh_object)
    to_armature = armature_object.matrix_world.inverted() @ evaluated_object.matrix_world
    face_vertex_indices = set()
    weighted_normal = Vector((0.0, 0.0, 0.0))
    weighted_center = Vector((0.0, 0.0, 0.0))
    total_area = 0.0
    polygon_count = 0
    left_orbit = None
    right_orbit = None
    try:
        for polygon in mesh.polygons:
            if polygon.material_index != FACE_MATERIAL_INDEX or len(polygon.vertices) < 3:
                continue
            points = [to_armature @ mesh.vertices[index].co for index in polygon.vertices]
            origin = points[0]
            polygon_area = 0.0
            polygon_normal = Vector((0.0, 0.0, 0.0))
            for index in range(1, len(points) - 1):
                cross = (points[index] - origin).cross(points[index + 1] - origin)
                triangle_area = cross.length * 0.5
                if triangle_area <= 1e-9:
                    continue
                polygon_area += triangle_area
                polygon_normal += cross
            if polygon_area <= 1e-9 or polygon_normal.length <= 1e-9:
                continue
            polygon_normal.normalize()
            polygon_center = sum(points, Vector()) / len(points)
            weighted_normal += polygon_normal * polygon_area
            weighted_center += polygon_center * polygon_area
            total_area += polygon_area
            polygon_count += 1
            face_vertex_indices.update(polygon.vertices)
        left_orbit_polygon = mesh.polygons[LEFT_ORBIT_POLYGON]
        right_orbit_polygon = mesh.polygons[RIGHT_ORBIT_POLYGON]
        left_orbit = sum(
            (to_armature @ mesh.vertices[index].co for index in left_orbit_polygon.vertices),
            Vector(),
        ) / len(left_orbit_polygon.vertices)
        right_orbit = sum(
            (to_armature @ mesh.vertices[index].co for index in right_orbit_polygon.vertices),
            Vector(),
        ) / len(right_orbit_polygon.vertices)
        if total_area <= 1e-9 or weighted_normal.length <= 1e-9:
            raise RuntimeError("The approved face-metal polygons have no usable area.")
        weighted_normal.normalize()
        if weighted_normal.dot(MODEL_FRONT_ARMATURE) < 0.0:
            weighted_normal = -weighted_normal
        surface_center = weighted_center / total_area
    finally:
        evaluated_object.to_mesh_clear()

    group_totals = Counter()
    total_weight = 0.0
    for vertex_index in face_vertex_indices:
        vertex = mesh_object.data.vertices[vertex_index]
        for membership in vertex.groups:
            name = mesh_object.vertex_groups[membership.group].name
            group_totals[name] += membership.weight
            total_weight += membership.weight
    normalized_weights = {
        name: round(weight / total_weight, 7)
        for name, weight in group_totals.most_common()
        if total_weight > 0.0
    }
    orbit_right = right_orbit - left_orbit
    orbit_right.z = 0.0
    if orbit_right.length <= 1e-9:
        raise RuntimeError("The actual left/right orbit landmarks overlap.")
    orbit_right.normalize()
    horizontal = Vector((orbit_right.y, -orbit_right.x, 0.0))
    if horizontal.dot(MODEL_FRONT_ARMATURE) < 0.0:
        horizontal = -horizontal
        orbit_right = -orbit_right
    center = (left_orbit + right_orbit) * 0.5
    signed_yaw = math.degrees(
        math.atan2(
            horizontal.cross(MODEL_FRONT_ARMATURE).z,
            horizontal.dot(MODEL_FRONT_ARMATURE),
        )
    )
    return {
        "center": center,
        "normal": horizontal,
        "horizontal_normal": horizontal,
        "surface_area_normal": weighted_normal,
        "surface_area_center": surface_center,
        "left_orbit": left_orbit,
        "right_orbit": right_orbit,
        "orbit_right": orbit_right,
        "signed_yaw_to_front_degrees": signed_yaw,
        "polygon_count": polygon_count,
        "vertex_count": len(face_vertex_indices),
        "normalized_skin_influence": normalized_weights,
    }


def rotate_pose_bone_about_head(pose_bone, rotation):
    head = pose_bone.head.copy()
    pose_bone.matrix = (
        Matrix.Translation(head)
        @ rotation.to_matrix().to_4x4()
        @ Matrix.Translation(-head)
        @ pose_bone.matrix
    )
    bpy.context.view_layer.update()


def align_pose_bone(pose_bone, target_direction):
    current_direction = pose_bone.tail - pose_bone.head
    target_direction = Vector(target_direction)
    if current_direction.length <= 1e-9 or target_direction.length <= 1e-9:
        raise RuntimeError(f"Invalid direction for {pose_bone.name}.")
    rotation = current_direction.normalized().rotation_difference(
        target_direction.normalized()
    )
    rotate_pose_bone_about_head(pose_bone, rotation)


def set_matrix_translation(matrix, translation):
    result = matrix.copy()
    result.translation = Vector(translation)
    return result


def solve_two_bone_elbow(shoulder, wrist, upper_length, lower_length, pole):
    shoulder = Vector(shoulder)
    wrist = Vector(wrist)
    pole = Vector(pole)
    axis = wrist - shoulder
    distance = axis.length
    minimum = abs(upper_length - lower_length) + 1e-5
    maximum = upper_length + lower_length - 1e-5
    if not minimum < distance < maximum:
        raise RuntimeError(
            f"Two-bone target distance {distance} is outside {minimum}..{maximum}."
        )
    axis.normalize()
    along = (
        upper_length * upper_length
        - lower_length * lower_length
        + distance * distance
    ) / (2.0 * distance)
    radius = math.sqrt(max(0.0, upper_length * upper_length - along * along))
    circle_center = shoulder + axis * along
    pole_direction = pole - circle_center
    pole_direction -= axis * pole_direction.dot(axis)
    if pole_direction.length <= 1e-9:
        raise RuntimeError("The two-bone pole is collinear with the target axis.")
    pole_direction.normalize()
    return circle_center + pole_direction * radius


def bone_points(armature_object, names):
    result = {}
    for name in names:
        bone = armature_object.pose.bones[name]
        result[name] = {
            "head_armature": rounded_vector(bone.head),
            "tail_armature": rounded_vector(bone.tail),
        }
    return result


def update_approved_eye_projection(mesh_object):
    evaluated_object, mesh = evaluated_mesh(mesh_object)
    patches = {}
    try:
        for label, polygon_index in (
            ("left", LEFT_ORBIT_POLYGON),
            ("right", RIGHT_ORBIT_POLYGON),
        ):
            polygon = mesh.polygons[polygon_index]
            points = [mesh.vertices[index].co.copy() for index in polygon.vertices]
            center = sum(points, Vector()) / len(points)
            surface_normal = (points[1] - points[0]).cross(points[2] - points[0])
            if surface_normal.length <= 1e-9:
                raise RuntimeError(f"The {label} orbit polygon is degenerate.")
            surface_normal.normalize()
            projection_normal = Vector((0.0, 0.0, 1.0))
            if surface_normal.dot(projection_normal) < 0.0:
                surface_normal = -surface_normal
            patches[label] = {
                "center": center,
                "projection_center": center.copy(),
                "surface_normal": surface_normal,
                "projection_normal": projection_normal,
                "size": EYE_PATCH_SIZE[label],
                "depth": EYE_PATCH_DEPTH,
            }
    finally:
        evaluated_object.to_mesh_clear()

    shared_eye_height = sum(
        patch["projection_center"].y for patch in patches.values()
    ) / len(patches)
    for patch in patches.values():
        patch["projection_center"].y = shared_eye_height

    material = mesh_object.material_slots[FACE_MATERIAL_INDEX].material
    if material is None or not material.use_nodes:
        raise RuntimeError("The approved face material node graph is missing.")
    nodes = material.node_tree.nodes

    def node(name):
        result = nodes.get(name)
        if result is None:
            raise RuntimeError(f"Approved eye projection node is missing: {name}")
        return result

    for label, names in {
        "left": (
            "Vector Math",
            "Map Range",
            "Vector Math.001",
            "Map Range.001",
            "Vector Math.002",
            "Math",
            "Math.002",
        ),
        "right": (
            "Vector Math.003",
            "Map Range.002",
            "Vector Math.004",
            "Map Range.003",
            "Vector Math.005",
            "Math.004",
            "Math.006",
        ),
    }.items():
        patch = patches[label]
        projection_normal = patch["projection_normal"]
        vertical = Vector((0.0, 1.0, 0.0))
        vertical -= projection_normal * vertical.dot(projection_normal)
        vertical.normalize()
        horizontal = vertical.cross(projection_normal).normalized()
        center = patch["projection_center"]
        width, height = patch["size"]
        horizontal_dot = node(names[0])
        horizontal_dot.inputs[1].default_value = tuple(horizontal)
        horizontal_range = node(names[1])
        horizontal_range.inputs["From Min"].default_value = center.dot(horizontal) - width * 0.5
        horizontal_range.inputs["From Max"].default_value = center.dot(horizontal) + width * 0.5
        vertical_dot = node(names[2])
        vertical_dot.inputs[1].default_value = tuple(vertical)
        vertical_range = node(names[3])
        vertical_range.inputs["From Min"].default_value = center.dot(vertical) - height * 0.5
        vertical_range.inputs["From Max"].default_value = center.dot(vertical) + height * 0.5
        normal_dot = node(names[4])
        normal_dot.inputs[1].default_value = tuple(patch["surface_normal"])
        plane_delta = node(names[5])
        plane_delta.inputs[1].default_value = patch["center"].dot(
            patch["surface_normal"]
        )
        plane_mask = node(names[6])
        plane_mask.inputs[1].default_value = patch["depth"]

    return {
        label: {
            "center_mesh_object": rounded_vector(patch["center"]),
            "projection_center_mesh_object": rounded_vector(
                patch["projection_center"]
            ),
            "surface_normal_mesh_object": rounded_vector(patch["surface_normal"]),
            "projection_normal_mesh_object": rounded_vector(
                patch["projection_normal"]
            ),
            "size": list(patch["size"]),
            "depth": patch["depth"],
        }
        for label, patch in patches.items()
    }




def apply_candidate_pose(mesh_object, armature_object):
    pose = armature_object.pose.bones
    tracked_names = (
        "LeftArm",
        "LeftForeArm",
        "LeftHand",
        "RightArm",
        "RightForeArm",
        "RightHand",
        "neck",
        "Head",
    )
    before_points = bone_points(armature_object, tracked_names)
    face_before = face_geometry_frame(mesh_object, armature_object)

    right_hand_matrix = pose["RightHand"].matrix.copy()
    right_shoulder = pose["RightArm"].head.copy()
    right_upper_length = pose["RightArm"].length
    right_lower_length = pose["RightForeArm"].length
    right_elbow_direction = Vector((-0.26, -0.03, -0.965))
    right_elbow = right_shoulder + right_elbow_direction.normalized() * right_upper_length
    right_wrist_direction = Vector((-0.10, 0.01, -0.995))
    right_wrist = right_elbow + right_wrist_direction.normalized() * right_lower_length
    align_pose_bone(pose["RightArm"], right_elbow - right_shoulder)
    align_pose_bone(pose["RightForeArm"], right_wrist - pose["RightForeArm"].head)
    pose["RightHand"].matrix = set_matrix_translation(right_hand_matrix, right_wrist)
    bpy.context.view_layer.update()

    left_hand_matrix = pose["LeftHand"].matrix.copy()
    left_shoulder = pose["LeftArm"].head.copy()
    left_wrist = pose["LeftHand"].head.copy()
    left_upper_length = pose["LeftArm"].length
    left_lower_length = pose["LeftForeArm"].length
    left_pole = left_shoulder + Vector((14.0, -6.0, -18.0))
    left_elbow = solve_two_bone_elbow(
        left_shoulder,
        left_wrist,
        left_upper_length,
        left_lower_length,
        left_pole,
    )
    align_pose_bone(pose["LeftArm"], left_elbow - left_shoulder)
    align_pose_bone(pose["LeftForeArm"], left_wrist - pose["LeftForeArm"].head)
    pose["LeftHand"].matrix = left_hand_matrix
    bpy.context.view_layer.update()

    accumulated_face_yaw = 0.0
    for _ in range(12):
        current_face = face_geometry_frame(mesh_object, armature_object)
        correction_degrees = current_face["signed_yaw_to_front_degrees"]
        if abs(correction_degrees) <= 0.05:
            break
        correction_radians = math.radians(correction_degrees)
        rotate_pose_bone_about_head(
            pose["Head"],
            Matrix.Rotation(correction_radians, 3, "Z").to_quaternion(),
        )
        accumulated_face_yaw += correction_degrees

    orbit_line_correction = accumulated_face_yaw
    rotate_pose_bone_about_head(
        pose["Head"],
        Matrix.Rotation(
            math.radians(VISUAL_FACE_YAW_OFFSET_DEGREES), 3, "Z"
        ).to_quaternion(),
    )
    accumulated_face_yaw += VISUAL_FACE_YAW_OFFSET_DEGREES

    face_after = face_geometry_frame(mesh_object, armature_object)
    eye_projection = update_approved_eye_projection(mesh_object)
    after_points = bone_points(armature_object, tracked_names)
    return {
        "before_bone_points": before_points,
        "after_bone_points": after_points,
        "right_arm_target": {
            "actual_shoulder_root": rounded_vector(right_shoulder),
            "elbow": rounded_vector(right_elbow),
            "wrist": rounded_vector(right_wrist),
            "hand_world_orientation_preserved": True,
        },
        "left_arm_target": {
            "actual_shoulder_root": rounded_vector(left_shoulder),
            "elbow": rounded_vector(left_elbow),
            "wrist_and_shield_grip": rounded_vector(left_wrist),
            "hand_and_shield_world_matrix_preserved": True,
        },
        "face_before": {
            "center": rounded_vector(face_before["center"]),
            "normal": rounded_vector(face_before["normal"]),
            "surface_area_normal": rounded_vector(
                face_before["surface_area_normal"]
            ),
            "left_orbit": rounded_vector(face_before["left_orbit"]),
            "right_orbit": rounded_vector(face_before["right_orbit"]),
            "legacy_orbit_line_yaw_degrees": round(
                face_before["signed_yaw_to_front_degrees"], 7
            ),
            "polygon_count": face_before["polygon_count"],
            "vertex_count": face_before["vertex_count"],
            "normalized_skin_influence": face_before["normalized_skin_influence"],
        },
        "face_after": {
            "center": rounded_vector(face_after["center"]),
            "normal": rounded_vector(face_after["normal"]),
            "surface_area_normal": rounded_vector(face_after["surface_area_normal"]),
            "left_orbit": rounded_vector(face_after["left_orbit"]),
            "right_orbit": rounded_vector(face_after["right_orbit"]),
            "legacy_orbit_line_yaw_degrees": round(
                face_after["signed_yaw_to_front_degrees"], 7
            ),
            "polygon_count": face_after["polygon_count"],
            "vertex_count": face_after["vertex_count"],
            "normalized_skin_influence": face_after["normalized_skin_influence"],
        },
        "accumulated_head_yaw_correction_degrees": round(
            accumulated_face_yaw, 7
        ),
        "automatic_orbit_line_correction_degrees": round(
            orbit_line_correction, 7
        ),
        "visual_front_offset_from_orbit_line_degrees": (
            VISUAL_FACE_YAW_OFFSET_DEGREES
        ),
        "face_alignment_method": (
            "Rotate only the existing Head pose bone. Preserve the imported mesh "
            "coordinates, topology, and skin weights, and update only the existing "
            "eye material projection after the final pose."
        ),
        "eye_projection_after_final_face_pose": eye_projection,
    }


def configure_camera_and_render(scene, camera, mesh_object):
    low, high = mesh_bounds_world(mesh_object)
    extent = high - low
    center = (low + high) * 0.5
    full_center = center.copy()
    full_center.z = low.z + extent.z * 0.51
    upper_center = center.copy()
    upper_center.z = low.z + extent.z * 0.69
    face_center = center.copy()
    face_center.z = low.z + extent.z * 0.88
    distance = max(extent.x, extent.y, extent.z) * 2.2

    camera.data.type = "ORTHO"
    camera.data.lens = 62
    camera.data.ortho_scale = extent.z * 1.08
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = -0.18

    def render(center_point, yaw, scale, name):
        radians = math.radians(yaw)
        direction = Vector((math.sin(radians), -math.cos(radians), 0.0))
        camera.location = center_point + direction * distance
        point_at(camera, center_point)
        camera.data.ortho_scale = scale
        scene.render.filepath = str(RENDER_DIR / name)
        bpy.ops.render.render(write_still=True)

    return {
        "render": render,
        "full_center": full_center,
        "upper_center": upper_center,
        "face_center": face_center,
        "full_scale": extent.z * 1.08,
        "upper_scale": extent.z * 0.66,
        "face_scale": extent.z * 0.29,
    }


def configure_symmetric_front_lighting(mesh_object):
    key = bpy.data.objects.get("Key")
    fill = bpy.data.objects.get("Fill")
    if key is None or key.type != "LIGHT":
        raise RuntimeError("The approved review light is missing: Key")
    if fill is None or fill.type != "LIGHT":
        raise RuntimeError("The approved review light is missing: Fill")
    key.data.energy = 210.0
    fill.data.energy = 150.0
    rim = bpy.data.objects.get("Rim")
    if rim is not None and rim.type == "LIGHT":
        rim.data.energy = 160.0


def make_emissive_material(name, color):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = color
    emission.inputs["Strength"].default_value = 4.0
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def render_without_shield(mesh_object, render_action):
    dependency_graph = bpy.context.evaluated_depsgraph_get()
    evaluated_object = mesh_object.evaluated_get(dependency_graph)
    review_mesh = bpy.data.meshes.new_from_object(
        evaluated_object,
        preserve_all_data_layers=True,
        depsgraph=dependency_graph,
    )
    review_object = bpy.data.objects.new("Review_Only_Kursa_NoShield", review_mesh)
    bpy.context.scene.collection.objects.link(review_object)
    review_object.matrix_world = evaluated_object.matrix_world.copy()
    editable = bmesh.new()
    editable.from_mesh(review_mesh)
    editable.faces.ensure_lookup_table()
    removable = [face for face in editable.faces if face.material_index in {7, 8}]
    bmesh.ops.delete(editable, geom=removable, context="FACES")
    editable.to_mesh(review_mesh)
    editable.free()
    mesh_object.hide_render = True
    try:
        render_action()
    finally:
        mesh_object.hide_render = False
        bpy.data.objects.remove(review_object, do_unlink=True)
        bpy.data.meshes.remove(review_mesh)


def add_landmark_spheres(armature_object, face_center):
    collection = bpy.data.collections.new("Review_Landmarks")
    bpy.context.scene.collection.children.link(collection)
    materials = {
        "shoulder": make_emissive_material("Landmark_Shoulder", (1.0, 0.12, 0.06, 1.0)),
        "elbow": make_emissive_material("Landmark_Elbow", (1.0, 0.78, 0.04, 1.0)),
        "wrist": make_emissive_material("Landmark_Wrist", (0.1, 0.9, 0.2, 1.0)),
        "face": make_emissive_material("Landmark_Face", (0.05, 0.6, 1.0, 1.0)),
    }
    specs = []
    pose = armature_object.pose.bones
    for side in ("Left", "Right"):
        specs.extend(
            (
                (f"{side}_ShoulderSocket", pose[f"{side}Arm"].head, "shoulder"),
                (f"{side}_Elbow", pose[f"{side}ForeArm"].head, "elbow"),
                (f"{side}_Wrist", pose[f"{side}Hand"].head, "wrist"),
            )
        )
    specs.append(("Face_Plane_Center", face_center, "face"))
    for name, armature_point, material_name in specs:
        world_point = armature_object.matrix_world @ Vector(armature_point)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, radius=0.018)
        sphere = bpy.context.object
        sphere.name = name
        sphere.location = world_point
        sphere.data.materials.append(materials[material_name])
        for parent_collection in list(sphere.users_collection):
            parent_collection.objects.unlink(sphere)
        collection.objects.link(sphere)
    return collection


def main():
    for directory in (RENDER_DIR, BLENDER_DIR):
        directory.mkdir(parents=True, exist_ok=True)
    if not APPROVED_BLEND.is_file() or not RUNTIME_FBX.is_file():
        raise FileNotFoundError("The approved sample blend or runtime FBX is missing.")

    bpy.ops.wm.open_mainfile(filepath=str(APPROVED_BLEND), load_ui=False)
    bpy.context.preferences.filepaths.save_version = 0
    scene = bpy.context.scene
    for item in list(scene.objects):
        if item.type == "ARMATURE" or item.name == "char1":
            bpy.data.objects.remove(item, do_unlink=True)
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)

    bpy.ops.import_scene.fbx(filepath=str(RUNTIME_FBX), use_anim=True)
    armatures = [item for item in scene.objects if item.type == "ARMATURE"]
    meshes = [
        item
        for item in scene.objects
        if item.type == "MESH" and item.name != "Review_Only_Floor"
    ]
    if len(armatures) != 1 or len(meshes) != 1:
        raise RuntimeError("The runtime import did not produce one armature and one mesh.")
    armature_object = armatures[0]
    mesh_object = meshes[0]
    mesh_object.name = "Kursa_FrontAlignment_Candidate"
    armature_object.name = "Kursa_FrontAlignment_Armature"

    if len(mesh_object.material_slots) != len(MATERIAL_NAMES):
        raise RuntimeError(
            f"Expected {len(MATERIAL_NAMES)} runtime material slots, got "
            f"{len(mesh_object.material_slots)}."
        )
    for index, material_name in enumerate(MATERIAL_NAMES):
        material = bpy.data.materials.get(material_name)
        if material is None:
            raise RuntimeError(f"Approved material is missing: {material_name}")
        mesh_object.material_slots[index].material = material

    if armature_object.animation_data is not None:
        armature_object.animation_data.action = None
    for pose_bone in armature_object.pose.bones:
        pose_bone.matrix_basis = Matrix.Identity(4)

    scene.frame_set(1)
    bpy.context.view_layer.update()
    camera = bpy.data.objects.get("Kursa_Review_Camera")
    floor = bpy.data.objects.get("Review_Only_Floor")
    if camera is None or floor is None:
        raise RuntimeError("The approved review camera or floor is missing.")
    floor.hide_render = False
    configure_symmetric_front_lighting(mesh_object)

    views = configure_camera_and_render(scene, camera, mesh_object)
    views["render"](
        views["full_center"], 0.0, views["full_scale"], "01_current_front.png"
    )
    views["render"](
        views["upper_center"], 0.0, views["upper_scale"], "02_current_upper_front.png"
    )
    views["render"](
        views["face_center"], 0.0, views["face_scale"], "03_current_face_front.png"
    )

    result = apply_candidate_pose(mesh_object, armature_object)
    aligned_face_frame = face_geometry_frame(mesh_object, armature_object)
    views = configure_camera_and_render(scene, camera, mesh_object)
    views["render"](
        views["full_center"], 0.0, views["full_scale"], "04_candidate_front.png"
    )
    views["render"](
        views["upper_center"], 0.0, views["upper_scale"], "05_candidate_upper_front.png"
    )
    render_without_shield(
        mesh_object,
        lambda: views["render"](
            views["face_center"],
            0.0,
            views["face_scale"],
            "06_candidate_face_front.png",
        ),
    )
    views["render"](
        views["upper_center"], -25.0, views["upper_scale"], "07_candidate_yaw_minus25.png"
    )
    views["render"](
        views["upper_center"], 25.0, views["upper_scale"], "08_candidate_yaw_plus25.png"
    )

    landmarks = add_landmark_spheres(
        armature_object,
        aligned_face_frame["center"],
    )
    views = configure_camera_and_render(scene, camera, mesh_object)
    views["render"](
        views["upper_center"], 0.0, views["upper_scale"], "09_candidate_landmarks_front.png"
    )
    landmarks.hide_render = True
    render_without_shield(
        mesh_object,
        lambda: views["render"](
            views["upper_center"],
            0.0,
            views["upper_scale"],
            "10_candidate_upper_front_no_shield.png",
        ),
    )
    render_without_shield(
        mesh_object,
        lambda: views["render"](
            views["upper_center"],
            -25.0,
            views["upper_scale"],
            "11_candidate_yaw_minus25_no_shield.png",
        ),
    )
    render_without_shield(
        mesh_object,
        lambda: views["render"](
            views["upper_center"],
            25.0,
            views["upper_scale"],
            "12_candidate_yaw_plus25_no_shield.png",
        ),
    )

    result.update(
        {
            "status": "ART_SAMPLE_USER_REVIEW_REQUIRED",
            "source_runtime_fbx": str(RUNTIME_FBX.relative_to(ROOT)).replace("\\", "/"),
            "reference_image": "image/KUŠkursa(쿠르사).png",
            "approved_appearance_source": str(APPROVED_BLEND.relative_to(ROOT)).replace(
                "\\", "/"
            ),
            "mesh_contract": {
                "vertices": len(mesh_object.data.vertices),
                "edges": len(mesh_object.data.edges),
                "polygons": len(mesh_object.data.polygons),
                "geometry_data_edited": False,
                "skin_weights_edited": False,
                "visible_face_rebuilt_from_front_reference": False,
                "reference_face_object_count": 0,
                "materials_reused_from_approved_sample": list(MATERIAL_NAMES),
            },
            "torso_decision": (
                "No large torso rotation was applied. The restored torso is already within the "
                "documented small yaw range; the candidate changes arm chains and actual face yaw."
            ),
            "coordinate_contract": {
                "blender_armature_front": [0.0, -1.0, 0.0],
                "unity_model_local_equivalent": "+Z",
                "render_projection": "orthographic",
            },
        }
    )
    REPORT_PATH.write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )

    camera.hide_render = True
    floor.hide_render = True
    blend_path = BLENDER_DIR / "Kursa_FrontAlignment_Sample.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), check_existing=False)
    print(
        "KURSA_FRONT_ALIGNMENT_SAMPLE_COMPLETE "
        + json.dumps(
            {
                "blend": str(blend_path),
                "report": str(REPORT_PATH),
                "face_before_orbit_line_yaw": result["face_before"][
                    "legacy_orbit_line_yaw_degrees"
                ],
                "face_after_orbit_line_yaw": result["face_after"][
                    "legacy_orbit_line_yaw_degrees"
                ],
                "final_head_yaw_correction": result[
                    "accumulated_head_yaw_correction_degrees"
                ],
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
