import json
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


ROOT = Path(__file__).resolve().parents[1]
PROJECT_ROOT = ROOT.parents[2]

BLENDER_DIR = ROOT / "blender"
EXPORT_DIR = ROOT / "exports"
RENDER_DIR = ROOT / "renders"
TEXTURE_DIR = ROOT / "textures"

for directory in (BLENDER_DIR, EXPORT_DIR, RENDER_DIR, TEXTURE_DIR):
    directory.mkdir(parents=True, exist_ok=True)

BASE_TRANSFORMS = {}
WING_PANEL_THICKNESS = 0.14

DEATH_SEQUENCE_STEPS = [
    {
        "pose": "Death_01_Hover_Start",
        "render": "10_death_01_hover_start",
        "label": "공중 부유 상태에서 시작",
    },
    {
        "pose": "Death_02_Tilt_Loss_Of_Lift",
        "render": "11_death_02_body_tilt_loss_of_lift",
        "label": "몸체가 한쪽으로 기울어짐",
    },
    {
        "pose": "Death_03_Wings_Limp_Fold",
        "render": "12_death_03_wings_limp_fold",
        "label": "날개가 접히거나 힘이 빠짐",
    },
    {
        "pose": "Death_04_Rigidbody_Drop",
        "render": "13_death_04_rigidbody_drop",
        "label": "Rigidbody/Collider 기준 바닥 낙하",
    },
    {
        "pose": "Death_05_Impact_Settle",
        "render": "14_death_05_impact_settle",
        "label": "바닥에 기울어진 자세로 충돌/정착",
    },
    {
        "pose": "Death_06_Final_Still",
        "render": "15_death_06_final_still_pose",
        "label": "최종적으로 움직임이 줄어든 사망 포즈 유지",
    },
]

DEATH_POSE_CONFIGS = {
    "Death_01_Hover_Start": {
        "shape": 0.0,
        "drop": 0.0,
        "roll": 0.0,
        "pitch": 0.0,
        "wing_y": 0.0,
        "wing_x": 0.0,
        "wing_z": 0.0,
    },
    "Death_02_Tilt_Loss_Of_Lift": {
        "shape": 0.20,
        "drop": 0.08,
        "roll": 0.30,
        "pitch": -0.08,
        "wing_y": 0.20,
        "wing_x": 0.16,
        "wing_z": 0.06,
    },
    "Death_03_Wings_Limp_Fold": {
        "shape": 0.45,
        "drop": 0.20,
        "roll": 0.55,
        "pitch": -0.16,
        "wing_y": 0.48,
        "wing_x": 0.34,
        "wing_z": 0.12,
    },
    "Death_04_Rigidbody_Drop": {
        "shape": 0.70,
        "drop": 0.44,
        "roll": 0.82,
        "pitch": -0.26,
        "wing_y": 0.66,
        "wing_x": 0.48,
        "wing_z": 0.18,
    },
    "Death_05_Impact_Settle": {
        "shape": 0.90,
        "drop": 0.66,
        "roll": 1.02,
        "pitch": -0.34,
        "wing_y": 0.76,
        "wing_x": 0.56,
        "wing_z": 0.20,
    },
    "Death_06_Final_Still": {
        "shape": 1.0,
        "drop": 0.72,
        "roll": 1.10,
        "pitch": -0.38,
        "wing_y": 0.82,
        "wing_x": 0.62,
        "wing_z": 0.22,
    },
}


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for data in (bpy.data.meshes, bpy.data.materials, bpy.data.images, bpy.data.curves):
        for block in list(data):
            if block.users == 0:
                data.remove(block)


def clamp(value, low=0.0, high=1.0):
    return max(low, min(high, value))


def noise2(u, v, scale=10.0):
    return (
        math.sin((u * scale + math.sin(v * scale * 1.7)) * math.tau) * 0.52
        + math.cos((v * scale * 0.73 + u * 2.9) * math.tau) * 0.31
        + math.sin((u + v) * scale * 3.1) * 0.17
    )


def create_texture(name, width, height, generator):
    image = bpy.data.images.new(name, width=width, height=height, alpha=True)
    pixels = []
    for y in range(height):
        v = y / max(1, height - 1)
        for x in range(width):
            u = x / max(1, width - 1)
            pixels.extend(generator(u, v))
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(TEXTURE_DIR / f"{name}.png")
    image.file_format = "PNG"
    image.save()
    return image


def make_textures():
    textures = {
        "body": create_texture(
            "fuga2_wet_green_bumpy_body_albedo",
            768,
            768,
            lambda u, v: (
                clamp(0.36 + 0.08 * noise2(u, v, 8) + 0.04 * math.sin(v * 22.0)),
                clamp(0.50 + 0.09 * noise2(u + 0.23, v, 9)),
                clamp(0.39 + 0.08 * noise2(u, v + 0.37, 7)),
                1.0,
            ),
        ),
        "body_bump": create_texture(
            "fuga2_body_wart_bump",
            768,
            768,
            lambda u, v: (
                clamp(0.36 + 0.44 * max(0.0, noise2(u, v, 27))),
                clamp(0.36 + 0.44 * max(0.0, noise2(u + 0.17, v + 0.11, 23))),
                clamp(0.36 + 0.44 * max(0.0, noise2(u + 0.31, v + 0.29, 31))),
                1.0,
            ),
        ),
        "feather": create_texture(
            "fuga2_olive_feather_albedo",
            768,
            768,
            lambda u, v: (
                clamp(0.23 + 0.16 * (1.0 - v) + 0.08 * math.sin(u * 32.0)),
                clamp(0.34 + 0.14 * (1.0 - v) + 0.05 * noise2(u, v, 16)),
                clamp(0.25 + 0.10 * (1.0 - v) + 0.03 * math.sin(v * 20.0)),
                1.0,
            ),
        ),
        "inner_feather": create_texture(
            "fuga2_inner_brown_olive_feather_albedo",
            768,
            768,
            lambda u, v: (
                clamp(0.35 + 0.12 * math.sin(u * 21.0) + 0.05 * noise2(u, v, 11)),
                clamp(0.32 + 0.12 * (1.0 - v) + 0.04 * noise2(u + 0.12, v, 14)),
                clamp(0.20 + 0.06 * noise2(u, v + 0.19, 9)),
                1.0,
            ),
        ),
        "shell": create_texture(
            "fuga2_lower_shell_leaf_albedo",
            512,
            512,
            lambda u, v: (
                clamp(0.39 + 0.11 * math.sin(u * 35.0) + 0.05 * noise2(u, v, 12)),
                clamp(0.36 + 0.08 * math.sin(v * 19.0)),
                clamp(0.25 + 0.05 * noise2(u + 0.2, v, 10)),
                1.0,
            ),
        ),
        "eye": create_texture(
            "fuga2_golden_eye_albedo",
            384,
            384,
            lambda u, v: (
                clamp(0.76 + 0.16 * (1.0 - abs(u - 0.5) * 2.0)),
                clamp(0.53 + 0.15 * noise2(u, v, 11)),
                clamp(0.14 + 0.10 * v),
                1.0,
            ),
        ),
    }
    return textures


def material_with_texture(name, image, roughness=0.45, metallic=0.0, bump_image=None, bump_strength=0.12):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    coord = nodes.new("ShaderNodeTexCoord")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = image
    links.new(coord.outputs["Generated"], tex.inputs["Vector"])
    links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    if bump_image is not None:
        bump_tex = nodes.new("ShaderNodeTexImage")
        bump_tex.image = bump_image
        bump_tex.image.colorspace_settings.name = "Non-Color"
        bump = nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = bump_strength
        bump.inputs["Distance"].default_value = 0.045
        links.new(coord.outputs["Generated"], bump_tex.inputs["Vector"])
        links.new(bump_tex.outputs["Color"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def make_flat_material(name, color, roughness=0.5, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def smooth_object(obj):
    if hasattr(obj.data, "polygons"):
        for poly in obj.data.polygons:
            poly.use_smooth = True
    return obj


def add_uv_ellipsoid(name, location, scale, material, rotation=(0, 0, 0), segments=48, rings=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    smooth_object(obj)
    return obj


def parent_keep_transform(obj, parent):
    bpy.context.view_layer.update()
    matrix = obj.matrix_world.copy()
    obj.parent = parent
    obj.matrix_parent_inverse = parent.matrix_world.inverted()
    obj.matrix_world = matrix


def create_body_mesh(body_mat):
    verts = []
    faces = []
    lat_count = 46
    lon_count = 72
    for i in range(lat_count + 1):
        theta = -math.pi / 2 + math.pi * i / lat_count
        for j in range(lon_count):
            phi = math.tau * j / lon_count
            x = math.cos(theta) * math.cos(phi)
            y = math.cos(theta) * math.sin(phi)
            z = math.sin(theta)
            front = max(0.0, -y)
            rear = max(0.0, y)
            lower_taper = clamp((-z - 0.18) / 0.82)
            top_ridge = clamp((z - 0.18) / 0.82)
            face_bulge = front * clamp((z + 0.35) / 1.0)
            lump = 0.045 * math.sin(6.0 * phi + 2.8 * theta) + 0.032 * math.sin(13.0 * theta + 2.1 * phi)
            ridge = 0.13 * top_ridge * (0.5 + 0.5 * math.sin(5.0 * phi))
            sx = 0.55 * (1.0 - 0.40 * lower_taper) * (1.0 + lump + ridge)
            sy = (0.42 + 0.18 * front + 0.08 * rear) * (1.0 - 0.30 * lower_taper) * (1.0 + lump)
            sz = 0.76 * (1.0 + 0.04 * face_bulge)
            vx = x * sx
            vy = y * sy - 0.01
            vz = z * sz + 1.08
            if z < -0.48:
                vz -= 0.10 * lower_taper
            verts.append((vx, vy, vz))
    for i in range(lat_count):
        for j in range(lon_count):
            a = i * lon_count + j
            b = i * lon_count + (j + 1) % lon_count
            c = (i + 1) * lon_count + (j + 1) % lon_count
            d = (i + 1) * lon_count + j
            faces.append((a, b, c, d))
    mesh = bpy.data.meshes.new("Fuga2_Continuous_Bumpy_Toad_Shell_Body_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Fuga2_Continuous_Bumpy_Toad_Shell_Body", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(body_mat)
    smooth_object(obj)
    return obj


def add_body_shape_keys(body):
    body.shape_key_add(name="Basis")
    pose_specs = {
        "Idle_Hover_Breathing_Surface_Pulse": (0.012, -0.012, 0.030),
        "Move_Wingbeat_Forward_Glide": (0.018, -0.055, -0.010),
        "Attack_Wing_Slap_Front_Lunge": (0.026, -0.125, 0.030),
        "Hit_Recoil_Altitude_Drop": (-0.020, 0.070, -0.075),
        "Death_Folded_Wings_Fall": (0.000, 0.025, -0.210),
    }
    for name, offsets in pose_specs.items():
        key = body.shape_key_add(name=name)
        for point in key.data:
            x, y, z = point.co
            front = max(0.0, -y)
            height = max(0.0, z - 0.50)
            point.co.x = x + offsets[0] * (0.4 + front)
            point.co.y = y + offsets[1] * (0.45 + abs(x))
            point.co.z = z + offsets[2] * (0.45 + height)


def create_leaf_mesh(name, root, tip, width_root, width_mid, material, curve_offset=0.0):
    root = Vector(root)
    tip = Vector(tip)
    axis = tip - root
    length = max(axis.length, 0.001)
    axis_n = axis.normalized()
    side = Vector((-axis_n.z, 0.0, axis_n.x)).normalized()
    mid = root.lerp(tip, 0.52) + Vector((0.0, curve_offset, 0.0))
    q1 = root.lerp(tip, 0.24) + Vector((0.0, curve_offset * 0.45, 0.0))
    q3 = root.lerp(tip, 0.76) + Vector((0.0, curve_offset * 0.45, 0.0))
    verts = [
        tuple(root - side * width_root * 0.38),
        tuple(q1 - side * width_mid * 0.62),
        tuple(mid - side * width_mid),
        tuple(q3 - side * width_mid * 0.70),
        tuple(tip),
        tuple(q3 + side * width_mid * 0.70),
        tuple(mid + side * width_mid),
        tuple(q1 + side * width_mid * 0.62),
        tuple(root + side * width_root * 0.38),
        tuple(mid + Vector((0.0, -0.018, 0.012))),
    ]
    faces = [
        (0, 1, 9),
        (1, 2, 9),
        (2, 3, 9),
        (3, 4, 9),
        (4, 5, 9),
        (5, 6, 9),
        (6, 7, 9),
        (7, 8, 9),
        (8, 0, 9),
        (0, 8, 7, 6, 5, 4, 3, 2, 1),
    ]
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return obj


def create_pointed_feather_mesh(name, root, tip, width_root, width_mid, thickness, material):
    root = Vector(root)
    tip = Vector(tip)
    axis = tip - root
    if axis.length < 0.001:
        axis = Vector((0.001, 0.0, 0.0))
    axis_n = axis.normalized()
    side_a = Vector((-axis_n.z, 0.0, axis_n.x))
    if side_a.length < 0.001:
        side_a = Vector((1.0, 0.0, 0.0))
    side_a.normalize()
    side_b = axis_n.cross(side_a)
    if side_b.length < 0.001:
        side_b = Vector((0.0, 1.0, 0.0))
    side_b.normalize()

    ring_specs = [
        (0.00, width_root * 0.45, thickness * 0.55),
        (0.18, width_mid * 0.90, thickness * 0.95),
        (0.48, width_mid, thickness),
        (0.76, width_mid * 0.55, thickness * 0.65),
        (0.96, width_mid * 0.18, thickness * 0.25),
        (1.00, 0.001, 0.001),
    ]
    radial_segments = 8
    verts = []
    for t, width, depth in ring_specs:
        center = root.lerp(tip, t)
        center += side_b * (math.sin(t * math.pi) * thickness * 0.25)
        for s in range(radial_segments):
            angle = math.tau * s / radial_segments
            verts.append(tuple(center + side_a * math.cos(angle) * width + side_b * math.sin(angle) * depth))

    faces = []
    for r in range(len(ring_specs) - 1):
        base = r * radial_segments
        next_base = (r + 1) * radial_segments
        for s in range(radial_segments):
            faces.append((base + s, base + (s + 1) % radial_segments, next_base + (s + 1) % radial_segments, next_base + s))
    faces.append(tuple(range(radial_segments - 1, -1, -1)))
    last = (len(ring_specs) - 1) * radial_segments
    faces.append(tuple(last + s for s in range(radial_segments)))

    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    smooth_object(obj)
    return obj


def add_curve_line(name, points, material, bevel=0.006):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 8
    curve.bevel_depth = bevel
    curve.bevel_resolution = 2
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, co in zip(spline.points, points):
        point.co = (co[0], co[1], co[2], 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def add_feather_details(feather, root, tip, dark_mat, parent):
    root = Vector(root)
    tip = Vector(tip)
    mid_points = [tuple(root.lerp(tip, t)) for t in (0.12, 0.35, 0.58, 0.82, 0.97)]
    line = add_curve_line(feather.name + "_Dark_Midrib", mid_points, dark_mat, bevel=0.004)
    parent_keep_transform(line, parent)
    axis = tip - root
    if axis.length > 0.001:
        axis_n = axis.normalized()
        side_vec = Vector((-axis_n.z, 0.0, axis_n.x))
        if side_vec.length > 0.001:
            side_vec.normalize()
            outline_width = min(0.075, axis.length * 0.035)
            for sign in (-1, 1):
                edge_points = []
                for t in (0.08, 0.30, 0.56, 0.82, 0.98):
                    taper = 1.0 - 0.70 * t
                    edge_points.append(tuple(root.lerp(tip, t) + side_vec * sign * outline_width * taper))
                edge = add_curve_line(
                    f"{feather.name}_Dark_Separated_Edge_{sign}",
                    edge_points,
                    dark_mat,
                    bevel=0.0045,
                )
                parent_keep_transform(edge, parent)
            for branch_index, t in enumerate((0.30, 0.46, 0.62, 0.78)):
                center = root.lerp(tip, t)
                branch_len = 0.050 * (1.0 - t) + 0.012
                for sign in (-1, 1):
                    end = center + side_vec * sign * branch_len
                    branch = add_curve_line(
                        f"{feather.name}_Fine_Dark_Barb_{branch_index}_{sign}",
                        [tuple(center), tuple(end)],
                        dark_mat,
                        bevel=0.0018,
                    )
                    parent_keep_transform(branch, parent)


def create_serrated_wing_panel(name, side, material):
    outline_positive = [
        (0.50, 0.20, 1.05),
        (0.72, 0.42, 1.28),
        (1.04, 0.78, 1.60),
        (1.46, 1.16, 1.92),
        (1.94, 1.58, 2.18),
        (2.42, 2.02, 2.38),
        (2.92, 2.46, 2.54),
        (2.74, 2.34, 2.30),
        (3.08, 2.58, 2.22),
        (2.72, 2.38, 2.02),
        (3.00, 2.54, 1.88),
        (2.62, 2.28, 1.66),
        (2.82, 2.38, 1.46),
        (2.42, 2.08, 1.26),
        (2.58, 2.10, 1.06),
        (2.04, 1.66, 0.88),
        (2.12, 1.60, 0.70),
        (1.54, 1.10, 0.66),
        (1.06, 0.68, 0.76),
        (0.72, 0.38, 0.90),
    ]
    center = Vector((side * 1.45, 1.28, 1.45))
    verts = [tuple(center)]
    outline = [Vector((side * x, y, z)) for x, y, z in outline_positive]
    verts.extend(tuple(p) for p in outline)
    faces = []
    count = len(outline)
    for i in range(count):
        faces.append((0, i + 1, ((i + 1) % count) + 1))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return obj, [tuple(p) for p in outline]


def polygon_normal(points):
    normal = Vector((0.0, 0.0, 0.0))
    count = len(points)
    for i, current in enumerate(points):
        nxt = points[(i + 1) % count]
        normal.x += (current.y - nxt.y) * (current.z + nxt.z)
        normal.y += (current.z - nxt.z) * (current.x + nxt.x)
        normal.z += (current.x - nxt.x) * (current.y + nxt.y)
    if normal.length < 0.001:
        return Vector((0.0, -1.0, 0.0))
    normal.normalize()
    return normal


def create_connected_wing_base_mesh(name, side, material):
    outline_positive = [
        (0.40, 0.04, 0.76),
        (0.40, 0.08, 1.42),
        (0.58, 0.26, 1.70),
        (0.90, 0.62, 1.96),
        (1.28, 1.00, 2.14),
        (1.72, 1.36, 2.26),
        (2.18, 1.68, 2.34),
        (2.72, 2.00, 2.32),
        (2.98, 2.16, 2.08),
        (2.62, 1.96, 1.92),
        (2.96, 2.20, 1.70),
        (2.54, 1.98, 1.50),
        (2.82, 2.08, 1.26),
        (2.42, 1.84, 1.04),
        (2.58, 1.86, 0.82),
        (2.02, 1.46, 0.66),
        (1.48, 1.04, 0.58),
        (1.00, 0.62, 0.62),
        (0.62, 0.26, 0.70),
    ]
    outline = [Vector((side * x, y, z)) for x, y, z in outline_positive]
    center = Vector((side * 1.40, 1.10, 1.38))
    normal = polygon_normal(outline)
    half_thickness = WING_PANEL_THICKNESS * 0.5

    front_center = center + normal * half_thickness
    back_center = center - normal * half_thickness
    front_outline = [p + normal * half_thickness for p in outline]
    back_outline = [p - normal * half_thickness for p in outline]

    verts = [tuple(front_center)]
    verts.extend(tuple(p) for p in front_outline)
    back_center_index = len(verts)
    verts.append(tuple(back_center))
    back_start = len(verts)
    verts.extend(tuple(p) for p in back_outline)

    faces = []
    count = len(outline)
    for i in range(count):
        front_a = i + 1
        front_b = ((i + 1) % count) + 1
        back_a = back_start + i
        back_b = back_start + ((i + 1) % count)
        faces.append((0, front_a, front_b))
        faces.append((back_center_index, back_b, back_a))
        faces.append((front_a, back_a, back_b, front_b))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    obj["wingPanelThickness"] = WING_PANEL_THICKNESS
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return obj, [tuple(p) for p in outline]


def create_wing(side, body_mat, feather_mat, inner_feather_mat, dark_mat):
    side_name = "Left" if side < 0 else "Right"
    root_empty = bpy.data.objects.new(f"Fuga2_{side_name}_Wing_Root_For_Pose", None)
    bpy.context.collection.objects.link(root_empty)
    root_empty.empty_display_type = "SPHERE"
    root_empty.empty_display_size = 0.12
    root_empty.location = (side * 0.48, 0.22, 1.12)
    root_empty.hide_render = True

    base_panel, base_outline = create_connected_wing_base_mesh(
        f"Fuga2_{side_name}_Single_Connected_Broad_Wing_Base",
        side,
        inner_feather_mat,
    )
    parent_keep_transform(base_panel, root_empty)

    base_outline_line = add_curve_line(
        f"Fuga2_{side_name}_Single_Base_Outer_Serrated_Edge",
        base_outline + [base_outline[0]],
        dark_mat,
        bevel=0.0035,
    )
    parent_keep_transform(base_outline_line, root_empty)

    return root_empty


def add_face(body_mat, eye_mat, dark_mat):
    add_uv_ellipsoid(
        "Fuga2_Continuous_Face_To_Body_Neck_Bridge",
        (0.0, -0.73, 1.05),
        (0.58, 0.34, 0.36),
        body_mat,
        rotation=(0.04, 0.0, 0.0),
        segments=48,
        rings=20,
    )
    add_uv_ellipsoid(
        "Fuga2_Continuous_Dorsal_Head_Back_Join",
        (0.0, -0.64, 1.30),
        (0.42, 0.26, 0.20),
        body_mat,
        rotation=(0.12, 0.0, 0.0),
        segments=40,
        rings=16,
    )
    add_uv_ellipsoid(
        "Fuga2_Continuous_Lower_Jaw_To_Chest_Join",
        (0.0, -0.70, 0.76),
        (0.44, 0.25, 0.30),
        body_mat,
        rotation=(-0.12, 0.0, 0.0),
        segments=40,
        rings=16,
    )
    snout = add_uv_ellipsoid(
        "Fuga2_Broad_Front_Snout_Bulge_With_Wavy_Mouth",
        (0.0, -1.05, 1.08),
        (0.68, 0.24, 0.26),
        body_mat,
        segments=48,
        rings=20,
    )
    lower = add_uv_ellipsoid(
        "Fuga2_Tapered_Lower_Throat_Mass",
        (0.0, -0.58, 0.58),
        (0.38, 0.28, 0.42),
        body_mat,
        segments=40,
        rings=18,
    )
    for side in (-1, 1):
        add_uv_ellipsoid(
            f"Fuga2_{'Left' if side < 0 else 'Right'}_Heavy_Eye_Brow_Ridge",
            (side * 0.39, -1.03, 1.35),
            (0.24, 0.14, 0.105),
            body_mat,
            rotation=(0.15, 0.0, side * 0.25),
            segments=28,
            rings=12,
        )
        add_uv_ellipsoid(
            f"Fuga2_{'Left' if side < 0 else 'Right'}_Golden_Vertical_Slit_Eye",
            (side * 0.43, -1.27, 1.25),
            (0.165, 0.048, 0.205),
            eye_mat,
            rotation=(0.0, side * 0.12, 0.0),
            segments=40,
            rings=18,
        )
        add_uv_ellipsoid(
            f"Fuga2_{'Left' if side < 0 else 'Right'}_Black_Vertical_Pupil",
            (side * 0.432, -1.315, 1.25),
            (0.034, 0.012, 0.170),
            dark_mat,
            rotation=(0.0, side * 0.12, 0.0),
            segments=20,
            rings=10,
        )
    mouth_points = []
    for i in range(17):
        t = i / 16.0
        x = -0.50 + 1.00 * t
        y = -1.285 - 0.018 * math.sin(t * math.pi)
        z = 1.005 + 0.028 * math.sin(t * math.tau * 2.0)
        mouth_points.append((x, y, z))
    add_curve_line("Fuga2_Subtle_Dark_Wavy_Mouth_Recess", mouth_points, dark_mat, bevel=0.010)
    return [snout, lower]


def add_surface_warts(body_mat):
    for i in range(145):
        a = (i * 2.3999632) % math.tau
        z_norm = ((i * 37) % 100) / 100.0
        z = 0.72 + z_norm * 0.74
        radius = 0.48 * (1.0 - 0.38 * abs(z - 1.08))
        x = math.cos(a) * radius * (0.65 + 0.35 * z_norm)
        y = math.sin(a) * radius * 0.70 - 0.13
        if y > 0.34:
            y *= 0.62
        if y < -0.50 and abs(x) > 0.40:
            continue
        scale = 0.019 + 0.034 * (((i * 19) % 31) / 31.0)
        add_uv_ellipsoid(
            f"Fuga2_Raised_Wet_Wart_{i:03d}",
            (x, y, z),
            (scale * 1.18, scale * 0.88, scale * 0.96),
            body_mat,
            segments=12,
            rings=6,
        )


def add_head_lumps(body_mat, dark_mat):
    lump_specs = [
        (0.00, -1.05, 1.36, 0.070, 0.040, 0.046),
        (-0.15, -1.05, 1.32, 0.060, 0.036, 0.040),
        (0.15, -1.05, 1.32, 0.060, 0.036, 0.040),
        (-0.30, -1.00, 1.23, 0.050, 0.032, 0.036),
        (0.30, -1.00, 1.23, 0.050, 0.032, 0.036),
        (-0.08, -1.17, 1.20, 0.046, 0.026, 0.032),
        (0.08, -1.17, 1.20, 0.046, 0.026, 0.032),
        (-0.24, -1.11, 1.10, 0.040, 0.026, 0.030),
        (0.24, -1.11, 1.10, 0.040, 0.026, 0.030),
        (0.00, -1.12, 1.25, 0.052, 0.034, 0.038),
    ]
    for index, spec in enumerate(lump_specs):
        x, y, z, sx, sy, sz = spec
        add_uv_ellipsoid(
            f"Fuga2_Front_Crown_Raised_Wet_Lump_{index:02d}",
            (x, y, z),
            (sx, sy, sz),
            body_mat,
            rotation=(0.18 * math.sin(index), 0.11 * math.cos(index), 0.0),
            segments=18,
            rings=9,
        )

    crack_paths = [
        [(-0.18, -1.18, 1.16), (-0.10, -1.20, 1.12), (0.00, -1.19, 1.10), (0.10, -1.20, 1.12), (0.18, -1.18, 1.16)],
        [(-0.28, -1.09, 1.25), (-0.14, -1.12, 1.30), (0.00, -1.10, 1.35), (0.14, -1.12, 1.30), (0.28, -1.09, 1.25)],
    ]
    for index, points in enumerate(crack_paths):
        add_curve_line(f"Fuga2_Front_Face_Subtle_Dark_Crack_{index:02d}", points, dark_mat, bevel=0.004)


def add_lower_shell_leaves(shell_mat, dark_mat):
    for i in range(5):
        offset = i - 2
        x = offset * 0.18
        root = (x, -0.54 + 0.016 * abs(offset), 0.56 + 0.020 * abs(offset))
        tip = (x + offset * 0.020, -0.74, 0.08 + 0.035 * abs(offset))
        leaf = create_leaf_mesh(
            f"Fuga2_Lower_Engraved_Shell_Leaf_{i:02d}",
            root,
            tip,
            0.15,
            0.105,
            shell_mat,
            curve_offset=-0.010,
        )
        add_feather_details(leaf, root, tip, dark_mat, bpy.context.collection.objects.get(leaf.name) or leaf)
        center = [Vector(root).lerp(Vector(tip), t) for t in (0.05, 0.30, 0.60, 0.88)]
        add_curve_line(f"{leaf.name}_Central_Dark_Engraving", [tuple(p) for p in center], dark_mat, bevel=0.004)
        for side in (-1, 1):
            branch = []
            for t in (0.22, 0.38, 0.54, 0.70):
                p = Vector(root).lerp(Vector(tip), t)
                branch.append((p.x + side * 0.035 * math.sin(t * math.pi), p.y - 0.012, p.z))
            add_curve_line(f"{leaf.name}_{side}_Curled_Engraving", branch, dark_mat, bevel=0.003)


def create_scene():
    clear_scene()
    textures = make_textures()
    body_mat = material_with_texture(
        "M_Fuga2_Wet_Green_Warty_Amphibian_Shell_Body",
        textures["body"],
        roughness=0.29,
        bump_image=textures["body_bump"],
        bump_strength=0.20,
    )
    feather_mat = material_with_texture("M_Fuga2_Dark_Olive_Outer_Feathers", textures["feather"], roughness=0.82)
    inner_feather_mat = material_with_texture("M_Fuga2_Brown_Olive_Inner_Feathers", textures["inner_feather"], roughness=0.84)
    shell_mat = material_with_texture("M_Fuga2_Rough_Engraved_Lower_Shell_Leaves", textures["shell"], roughness=0.48)
    eye_mat = material_with_texture("M_Fuga2_Golden_Vertical_Slit_Eyes", textures["eye"], roughness=0.19)
    dark_mat = make_flat_material("M_Fuga2_Dark_Recess_And_Pupil", (0.030, 0.026, 0.020, 1.0), roughness=0.72)

    body = create_body_mesh(body_mat)
    add_body_shape_keys(body)
    add_face(body_mat, eye_mat, dark_mat)
    add_head_lumps(body_mat, dark_mat)
    add_surface_warts(body_mat)
    left_root = create_wing(-1, body_mat, feather_mat, inner_feather_mat, dark_mat)
    right_root = create_wing(1, body_mat, feather_mat, inner_feather_mat, dark_mat)
    add_lower_shell_leaves(shell_mat, dark_mat)

    hidden = bpy.data.objects.new("Hidden_Rigidbody_Hover_Collider_Bounds", None)
    bpy.context.collection.objects.link(hidden)
    hidden.empty_display_type = "CUBE"
    hidden.empty_display_size = 1.1
    hidden.location = (0, -0.04, 0.98)
    hidden.hide_render = True

    target = bpy.data.objects.new("Hidden_MotionPath_Wing_Slap_Attack_Goal", None)
    bpy.context.collection.objects.link(target)
    target.empty_display_type = "SPHERE"
    target.empty_display_size = 0.18
    target.location = (0, -1.25, 1.08)
    target.hide_render = True

    bpy.ops.object.light_add(type="AREA", location=(0, -4.0, 4.2))
    light = bpy.context.object
    light.name = "Fuga2_Large_Soft_Front_Light"
    light.data.energy = 430
    light.data.size = 5.0
    bpy.ops.object.light_add(type="AREA", location=(-3.2, 2.5, 3.2))
    rim = bpy.context.object
    rim.name = "Fuga2_Cool_Back_Rim_Light"
    rim.data.energy = 120
    rim.data.size = 4.0

    bpy.context.scene.world = bpy.data.worlds.new("Fuga2_Pale_Reference_Background")
    bpy.context.scene.world.color = (0.82, 0.86, 0.74)

    for obj in bpy.context.scene.objects:
        BASE_TRANSFORMS[obj.name] = obj.matrix_world.copy()
    return body, left_root, right_root


def reset_transforms():
    for obj in bpy.context.scene.objects:
        matrix = BASE_TRANSFORMS.get(obj.name)
        if matrix is not None:
            obj.matrix_world = matrix.copy()


def reset_shape_keys(body):
    if body.data.shape_keys:
        for key in body.data.shape_keys.key_blocks:
            key.value = 0.0


def apply_death_model_transform(config):
    pivot = Vector((0.0, -0.05, 1.06))
    rotation = (
        Matrix.Rotation(config["roll"], 4, "Y")
        @ Matrix.Rotation(config["pitch"], 4, "X")
    )
    transform = (
        Matrix.Translation(Vector((0.0, 0.0, -config["drop"])))
        @ Matrix.Translation(pivot)
        @ rotation
        @ Matrix.Translation(-pivot)
    )
    for obj in bpy.context.scene.objects:
        if obj.parent is not None:
            continue
        if obj.type in {"CAMERA", "LIGHT"}:
            continue
        if obj.hide_render and not obj.name.endswith("_Wing_Root_For_Pose"):
            continue
        obj.matrix_world = transform @ obj.matrix_world


def apply_pose(body, pose_name):
    reset_transforms()
    reset_shape_keys(body)
    death_config = DEATH_POSE_CONFIGS.get(pose_name)
    if pose_name == "Death_Folded_Wings_Fall":
        death_config = DEATH_POSE_CONFIGS["Death_06_Final_Still"]
    if death_config and body.data.shape_keys and "Death_Folded_Wings_Fall" in body.data.shape_keys.key_blocks:
        body.data.shape_keys.key_blocks["Death_Folded_Wings_Fall"].value = death_config["shape"]
    elif pose_name and body.data.shape_keys and pose_name in body.data.shape_keys.key_blocks:
        body.data.shape_keys.key_blocks[pose_name].value = 1.0

    for obj in bpy.context.scene.objects:
        if not obj.name.endswith("_Wing_Root_For_Pose"):
            continue
        side = -1 if "_Left_" in obj.name else 1
        if pose_name == "Move_Wingbeat_Forward_Glide":
            obj.rotation_euler.rotate_axis("Y", side * -0.16)
            obj.rotation_euler.rotate_axis("X", -0.08)
        elif pose_name == "Attack_Wing_Slap_Front_Lunge":
            obj.rotation_euler.rotate_axis("Y", side * 0.26)
            obj.rotation_euler.rotate_axis("X", -0.24)
        elif pose_name == "Hit_Recoil_Altitude_Drop":
            obj.rotation_euler.rotate_axis("Y", side * -0.22)
            obj.rotation_euler.rotate_axis("X", 0.10)
        elif death_config:
            obj.rotation_euler.rotate_axis("Y", side * death_config["wing_y"])
            obj.rotation_euler.rotate_axis("X", death_config["wing_x"])
            obj.rotation_euler.rotate_axis("Z", side * death_config["wing_z"])

    if death_config:
        apply_death_model_transform(death_config)


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def set_camera(name, location, target, ortho_scale):
    camera_data = bpy.data.cameras.new(name)
    camera = bpy.data.objects.new(name, camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = Vector(location)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale
    look_at(camera, target)
    bpy.context.scene.camera = camera
    return camera


def add_death_review_floor():
    mat = make_flat_material("M_Temp_Fuga_Death_Review_Floor", (0.23, 0.27, 0.24, 1.0), roughness=0.82)
    bpy.ops.mesh.primitive_plane_add(size=4.8, location=(0.0, -0.08, 0.12))
    floor = bpy.context.object
    floor.name = "Temp_Fuga_Death_Review_Floor"
    floor.data.materials.append(mat)
    return floor, mat


def remove_temp_floor(floor, mat):
    mesh = floor.data
    bpy.data.objects.remove(floor, do_unlink=True)
    if mesh.users == 0:
        bpy.data.meshes.remove(mesh)
    if mat.users == 0:
        bpy.data.materials.remove(mat)


def render_image(render_name, camera_location, target=(0, -0.08, 1.05), ortho_scale=3.45, show_floor=False):
    floor = None
    floor_mat = None
    if show_floor:
        floor, floor_mat = add_death_review_floor()
    camera = set_camera("Camera_" + render_name, camera_location, target, ortho_scale)
    bpy.context.scene.render.filepath = str(RENDER_DIR / f"{render_name}.png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)
    if floor is not None and floor_mat is not None:
        remove_temp_floor(floor, floor_mat)


def render_pose(body, render_name, pose_name, camera_location, target=(0, -0.08, 1.05), ortho_scale=3.45, show_floor=False):
    apply_pose(body, pose_name)
    render_image(render_name, camera_location, target, ortho_scale, show_floor=show_floor)


def render_set(body):
    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 80
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 720
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    render_pose(body, "01_front_fuga2_reference_match", None, (0, -6.4, 1.16), ortho_scale=6.35)
    render_pose(body, "02_side_fuga2_beside_reference_match", None, (6.4, -0.20, 1.14), ortho_scale=5.85)
    render_pose(body, "03_back_fuga2_back_reference_match", None, (0, 6.4, 1.14), ortho_scale=6.35)
    render_pose(body, "04_three_quarter_runtime_hover_visual", None, (-5.1, -5.2, 1.42), ortho_scale=5.85)
    render_pose(body, "05_idle_hover_pose", "Idle_Hover_Breathing_Surface_Pulse", (0, -6.4, 1.18), ortho_scale=6.05)
    render_pose(body, "06_move_forward_glide_pose", "Move_Wingbeat_Forward_Glide", (-5.1, -5.2, 1.24), ortho_scale=5.85)
    render_pose(body, "07_attack_wing_slap_pose", "Attack_Wing_Slap_Front_Lunge", (5.1, -5.2, 1.18), ortho_scale=5.85)
    render_pose(body, "08_hit_recoil_altitude_drop_pose", "Hit_Recoil_Altitude_Drop", (-5.1, -5.1, 1.00), ortho_scale=5.85)
    render_pose(
        body,
        "09_death_fall_folded_wings_pose",
        "Death_Folded_Wings_Fall",
        (5.1, -5.1, 0.84),
        target=(0, -0.05, 0.72),
        ortho_scale=5.85,
        show_floor=True,
    )
    for step in DEATH_SEQUENCE_STEPS:
        render_pose(
            body,
            step["render"],
            step["pose"],
            (5.1, -5.1, 0.92),
            target=(0, -0.05, 0.76),
            ortho_scale=5.85,
            show_floor=True,
        )
    apply_pose(body, None)


def export_files():
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "fuga_sample.blend"))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(filepath=str(EXPORT_DIR / "fuga_sample.fbx"), use_selection=False, add_leaf_bones=False)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT_DIR / "fuga_sample.glb"), export_format="GLB")


def write_text_files_legacy_unused():
    approval = {
        "enemyId": "fuga",
        "approvalState": "검토 필요",
        "primaryReferenceImages": [
            "image/fuga2(푸가).png",
            "image/fuga2-back.png",
            "image/fuga2-beside.png",
        ],
        "secondaryReferenceImages": [
            "image/fuga(푸가).png",
            "image/fuga-back.png",
            "image/fuga-beside.png",
        ],
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "notes": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 에셋, AI, 피격 판정, UI 흐름에 연결하지 않습니다.",
    }
    manifest = {
        "enemyId": "fuga",
        "sampleRoot": "artSample/enemies/fuga",
        "primaryReference": "fuga2",
        "createdBy": "build_fuga_sample.py",
        "modelFiles": [
            "blender/fuga_sample.blend",
            "exports/fuga_sample.fbx",
            "exports/fuga_sample.glb",
        ],
        "renders": sorted([p.name for p in RENDER_DIR.glob("*.png")]),
        "textures": sorted([p.name for p in TEXTURE_DIR.glob("*.png")]),
        "approval": approval,
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
    }
    (ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")
    (ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    (ROOT / "README.md").write_text(
        """# 푸가 모델링 샘플

이 샘플은 `image/fuga2(푸가).png`, `image/fuga2-back.png`, `image/fuga2-beside.png`를 1차 재현 기준으로 제작한 검토용 적대 개체 아트 샘플입니다. `fuga(푸가).png` 계열은 1차 형태를 흔들지 않는 보조 비교 참고로만 둡니다.

## 재현 목표

- 젖은 녹회색 사마귀 질감의 두꺼비형 머리와 소라 껍질 같은 중앙 몸체.
- 좌우로 크게 펼친 조류형 날개와 층상 깃.
- 날개 안쪽의 어두운 올리브/갈색 깃층과 바깥쪽의 청록색 깃.
- 황금색 세로 동공 눈.
- 하단에 매달린 소라 껍질 조각형 장식 부위.
- 공중에서 떠 있는 비행 씨앗체 실루엣.

## 산출물

- `blender/fuga_sample.blend`
- `exports/fuga_sample.fbx`
- `exports/fuga_sample.glb`
- `textures/`
- `renders/`
- `index.html`
- `TEXTURE_ANALYSIS.md`
- `PHYSICS_RIG_NOTES.md`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 승인 상태

`APPROVAL_STATUS.json` 기준 `requiresUserApprovalBeforeUnity=true`, `unityApplicationAllowed=false` 상태입니다. 사용자 승인 전에는 Unity 씬, 프리팹, 런타임 에셋, AI, 피격 판정, UI 흐름에 연결하지 않습니다.
""",
        encoding="utf-8",
    )
    (ROOT / "TEXTURE_ANALYSIS.md").write_text(
        """# 푸가 텍스처/머티리얼 분석

## 1차 기준 이미지

- `image/fuga2(푸가).png`
- `image/fuga2-back.png`
- `image/fuga2-beside.png`

## 표면 분석

- 몸체: 젖은 녹회색 표면, 불규칙한 혹과 사마귀 같은 요철, 강한 하이라이트가 필요합니다.
- 머리/눈 주변: 눈두덩이 두껍고 얼굴 전면이 두꺼비처럼 납작하게 돌출되어야 합니다.
- 날개: 바깥쪽은 차가운 청록색 깃, 안쪽은 어두운 올리브와 갈색이 섞인 층상 깃입니다. 깃은 단일 판이 아니라 겹겹이 쌓인 잎형 조각처럼 보여야 합니다.
- 눈: 황금색 세로 동공이 정면 시선을 만듭니다.
- 하단 장식: 잎 또는 소라 껍질 조각처럼 보이며, 새겨진 선과 거친 금속/껍질 roughness가 필요합니다.

## 생성 텍스처

- `fuga2_wet_green_bumpy_body_albedo.png`
- `fuga2_body_wart_bump.png`
- `fuga2_olive_feather_albedo.png`
- `fuga2_inner_brown_olive_feather_albedo.png`
- `fuga2_lower_shell_leaf_albedo.png`
- `fuga2_golden_eye_albedo.png`
""",
        encoding="utf-8",
    )
    (ROOT / "PHYSICS_RIG_NOTES.md").write_text(
        """# 푸가 물리/애니메이션 적용 계획

- 푸가는 비행 씨앗체이므로 런타임 루트 이동은 `Rigidbody + Collider` 기준으로 처리합니다.
- Motion Path는 실제 Transform 직접 이동 도구가 아니라 호버 위치, 접근 경로, 날개 타격 목표를 편집하는 기준으로만 사용합니다.
- 실제 이동은 `Rigidbody.linearVelocity`, velocity 제어, 또는 `AddForce` 계열로 추종합니다.
- 같은 Transform을 Motion Path, Rigidbody, AnimationClip, IK, Joint, 보조 흔들림이 동시에 직접 움직이지 않게 역할을 분리합니다.
- 샘플에는 Unity 적용 검토를 위한 Shape Key 이름을 포함했습니다.
  - `Idle_Hover_Breathing_Surface_Pulse`
  - `Move_Wingbeat_Forward_Glide`
  - `Attack_Wing_Slap_Front_Lunge`
  - `Hit_Recoil_Altitude_Drop`
  - `Death_Folded_Wings_Fall`
- Unity 적용 시 정적 비교 1개체와 대기, 이동, 공격, 피격, 사망 상태를 분리해 확인 가능하게 배치합니다.
""",
        encoding="utf-8",
    )
    comparison_rows = [
        ("image/fuga2(푸가).png", "renders/01_front_fuga2_reference_match.png", "정면"),
        ("image/fuga2-beside.png", "renders/02_side_fuga2_beside_reference_match.png", "측면"),
        ("image/fuga2-back.png", "renders/03_back_fuga2_back_reference_match.png", "후면"),
    ]
    comparison_html = "\n".join(
        f"""
      <article class="comparison">
        <h3>{label}</h3>
        <figure><img src="../../../{ref}" alt="{label} 기준 이미지"><figcaption>기준 이미지: {ref}</figcaption></figure>
        <figure><img src="{render}" alt="{label} 생성 렌더"><figcaption>생성 렌더: {render}</figcaption></figure>
      </article>"""
        for ref, render, label in comparison_rows
    )
    render_links = "\n".join(
        f'<figure><img src="renders/{p.name}" alt="{p.stem}"><figcaption>{p.name}</figcaption></figure>'
        for p in sorted(RENDER_DIR.glob("*.png"))
    )
    death_sequence_links = "\n".join(
        f'<figure><img src="renders/{step["render"]}.png" alt="{step["label"]}"><figcaption>{step["label"]}<br>{step["render"]}.png</figcaption></figure>'
        for step in DEATH_SEQUENCE_STEPS
    )
    texture_links = "\n".join(
        f'<figure><img src="textures/{p.name}" alt="{p.stem}"><figcaption>{p.name}</figcaption></figure>'
        for p in sorted(TEXTURE_DIR.glob("*.png"))
    )
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <title>푸가 모델링 샘플</title>
  <style>
    body {{ margin: 0; font-family: Arial, sans-serif; background: #18201a; color: #edf2e6; }}
    main {{ max-width: 1180px; margin: 0 auto; padding: 28px; }}
    h1, h2, h3 {{ margin: 0 0 14px; }}
    section {{ margin: 30px 0; }}
    .grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 16px; }}
    .comparison {{ display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-bottom: 22px; background: #222b24; border: 1px solid #435041; padding: 14px; }}
    .comparison h3 {{ grid-column: 1 / -1; }}
    figure {{ margin: 0; background: #111711; border: 1px solid #364236; padding: 10px; }}
    img {{ width: 100%; height: auto; display: block; }}
    figcaption {{ margin-top: 8px; font-size: 13px; color: #cdd7c5; word-break: break-all; }}
    p, li {{ line-height: 1.55; }}
    code {{ color: #d7e6c8; }}
    @media (max-width: 760px) {{ .comparison {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>푸가 모델링 샘플</h1>
  <p>1차 재현 기준은 <code>fuga2(푸가).png</code>, <code>fuga2-back.png</code>, <code>fuga2-beside.png</code>입니다. 사용자 승인 전에는 Unity에 연결하지 않습니다.</p>

  <section>
    <h2>기준 이미지와 생성 렌더 비교</h2>
{comparison_html}
  </section>

  <section>
    <h2>생성 렌더</h2>
    <div class="grid">{render_links}</div>
  </section>

  <section>
    <h2>사망 모션 검토 시퀀스</h2>
    <p>Unity 적용 단계에서는 낙하 이동을 <code>Rigidbody + Collider</code> 기준으로 처리하고, 이 렌더들은 자세와 시간 흐름 검토용으로 사용합니다.</p>
    <div class="grid">{death_sequence_links}</div>
  </section>

  <section>
    <h2>사용 텍스처</h2>
    <div class="grid">{texture_links}</div>
  </section>
</main>
</body>
</html>
"""
    (ROOT / "index.html").write_text(html, encoding="utf-8")


def write_text_files():
    approval = {
        "enemyId": "fuga",
        "approvalState": "검토 필요",
        "primaryReferenceImages": [
            "image/fuga2(푸가).png",
            "image/fuga2-back.png",
            "image/fuga2-beside.png",
        ],
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "notes": "2026-07-01 날개 두께와 사망 모션 검토 시퀀스를 갱신했습니다. 사용자 승인 전에는 Unity 런타임 씬, 프리팹, 에셋, AI, 피격 판정, UI 흐름에 연결하지 않습니다.",
    }
    manifest = {
        "enemyId": "fuga",
        "sampleRoot": "artSample/enemies/fuga",
        "primaryReference": "fuga2",
        "createdBy": "build_fuga_sample.py",
        "modelFiles": [
            "blender/fuga_sample.blend",
            "exports/fuga_sample.fbx",
            "exports/fuga_sample.glb",
        ],
        "renders": sorted([p.name for p in RENDER_DIR.glob("*.png")]),
        "textures": sorted([p.name for p in TEXTURE_DIR.glob("*.png")]),
        "runtimeScaleIntent": {
            "heightMeters": 0.60,
            "widthMeters": 0.40,
            "depthMeters": 0.20,
            "source": "docs/GAME_DESIGN_SOURCE.txt의 푸가 수치",
        },
        "wingVisibilityUpdate": {
            "updatedOn": "2026-07-01",
            "singleBroadWingBaseThickness": WING_PANEL_THICKNESS,
            "reason": "특정 각도에서 단일 면 날개가 실루엣처럼 보이는 문제를 줄이기 위해 기반 날개를 양면과 측면을 가진 두께 있는 메쉬로 갱신했습니다.",
        },
        "deathMotionPreview": {
            "updatedOn": "2026-07-01",
            "intendedRuntimeMovement": "Rigidbody + Collider 기준 낙하",
            "steps": [
                {"pose": step["pose"], "render": step["render"] + ".png", "label": step["label"]}
                for step in DEATH_SEQUENCE_STEPS
            ],
        },
        "approval": approval,
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
    }
    (ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")
    (ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    (ROOT / "README.md").write_text(
        """# 푸가 모델링 샘플

이 샘플은 `image/fuga2(푸가).png`, `image/fuga2-back.png`, `image/fuga2-beside.png`를 1차 재현 기준으로 제작한 적대 개체 아트 샘플입니다. `fuga(푸가).png` 계열은 형태를 뒤집지 않는 보조 참고로만 사용했습니다.

## 재현 목표

- 젖은 녹회색 피부와 거친 돌기를 가진 두꺼비형 중앙 머리.
- 좌우로 크게 펼쳐진 조류형 날개와 겹겹이 분리된 깃.
- 특정 각도에서 실루엣만 보이지 않도록 두께를 키운 단일 기반 날개 메쉬.
- 날개 안쪽의 어두운 올리브/갈색 깃층과 바깥쪽 녹회색 깃.
- 황금색 세로 동공 눈과 얇은 물결형 입.
- 하단에 매달린 소라 껍질 또는 잎 장식 부품.
- 공중에서 떠 있는 비행 씨앗체의 실루엣.

## Unity 반영 의도

- 승인 후 `CargoRunMvp` 복도 오브젝트 하단에 비행 적대 개체로 배치하는 것을 전제로 합니다.
- 런타임 루트 이동은 `Rigidbody + Collider` 기준으로 처리하고, Motion Path는 목표/경로 편집 기준으로 사용합니다.
- 정적 비교, 대기, 이동, 공격, 피격, 사망 상태를 분리해 확인할 수 있도록 Shape Key 이름을 포함했습니다.
- 사망 모션 검토 렌더는 공중 부유 시작, 몸체 기울어짐, 날개 힘 빠짐, 바닥 낙하, 충돌/정착, 최종 정지 포즈의 6단계로 구성했습니다.
- 샘플 단계에서는 Unity 런타임 씬, 프리팹, 에셋, AI, 피격 판정, UI 흐름에 연결하지 않았습니다.

## 산출물

- `blender/fuga_sample.blend`
- `exports/fuga_sample.fbx`
- `exports/fuga_sample.glb`
- `textures/`
- `renders/`
- `index.html`
- `TEXTURE_ANALYSIS.md`
- `PHYSICS_RIG_NOTES.md`
- `ASSET_MANIFEST.json`
- `APPROVAL_STATUS.json`

## 승인 상태

`APPROVAL_STATUS.json` 기준 `requiresUserApprovalBeforeUnity=true`, `unityApplicationAllowed=false` 상태입니다. 사용자의 명시 승인 전에는 Unity에 적용하지 않습니다.
""",
        encoding="utf-8",
    )
    (ROOT / "TEXTURE_ANALYSIS.md").write_text(
        """# 푸가 텍스처/머티리얼 분석

## 1차 기준 이미지

- `image/fuga2(푸가).png`
- `image/fuga2-back.png`
- `image/fuga2-beside.png`

## 표면 분석

- 몸체: 젖은 녹회색 피부, 불규칙한 돌기, 점액성 하이라이트가 필요합니다.
- 머리와 눈 주변: 눈두덩이 두껍고 얼굴 전면이 두꺼비처럼 낮게 돌출되어야 합니다.
- 날개: 바깥쪽은 차가운 녹회색 깃, 안쪽은 어두운 올리브/갈색이 섞인 층상 깃입니다. 깃은 단일 면이 아니라 겹겹이 쌓인 판형 조각처럼 보여야 합니다.
- 눈: 황금색 세로 동공이 정면 시선을 만듭니다.
- 하단 장식: 돌 또는 소라 껍질 같은 매달린 장식이며, 새겨진 문양과 거친 roughness가 필요합니다.

## 생성 텍스처

- `fuga2_wet_green_bumpy_body_albedo.png`
- `fuga2_body_wart_bump.png`
- `fuga2_olive_feather_albedo.png`
- `fuga2_inner_brown_olive_feather_albedo.png`
- `fuga2_lower_shell_leaf_albedo.png`
- `fuga2_golden_eye_albedo.png`

## 한계와 확인 필요 사항

- 기준 이미지의 정확한 뒷면 구조와 장식 부착 방식은 보이는 이미지에서 추론했습니다.
- Unity 적용 시에는 이 샘플을 분위기 참고가 아니라 재현 대상으로 두고, 렌더 비교를 통해 추가 보정해야 합니다.
""",
        encoding="utf-8",
    )
    (ROOT / "PHYSICS_RIG_NOTES.md").write_text(
        """# 푸가 물리/애니메이션 적용 계획

- 푸가는 비행 씨앗체이므로 적용 단계의 루트 이동은 `Rigidbody + Collider` 기준으로 처리합니다.
- Motion Path는 실제 Transform 직접 이동 도구가 아니라 호버 위치, 접근 경로, 공격 목표를 편집하는 기준으로만 사용합니다.
- 실제 이동은 `Rigidbody.linearVelocity`, velocity 제어, 또는 `AddForce` 계열로 추종합니다.
- 같은 Transform을 Motion Path, Rigidbody, AnimationClip, IK, Joint, 보조 흔들림이 동시에 직접 움직이지 않게 역할을 분리합니다.
- 샘플에는 Unity 적용 검토를 위한 Shape Key 이름을 포함했습니다.
  - `Idle_Hover_Breathing_Surface_Pulse`
  - `Move_Wingbeat_Forward_Glide`
  - `Attack_Wing_Slap_Front_Lunge`
  - `Hit_Recoil_Altitude_Drop`
  - `Death_Folded_Wings_Fall`
- 사망 모션은 다음 흐름으로 Unity 적용 단계에서 구성합니다.
  1. 공중 부유 상태에서 시작합니다.
  2. 몸체가 한쪽으로 기울어집니다.
  3. 날개가 접히거나 힘이 빠집니다.
  4. `Rigidbody + Collider` 기준으로 바닥 쪽으로 낙하합니다.
  5. 바닥에 기울어진 자세로 충돌/정착합니다.
  6. 최종적으로 움직임이 줄어든 사망 포즈를 유지합니다.
- 샘플 렌더 `10_death_01_hover_start.png`부터 `15_death_06_final_still_pose.png`까지는 위 흐름을 Unity 구현 전 검토하기 위한 정적 시퀀스입니다.
- Unity 적용 시 정적 비교 1개체와 대기, 이동, 공격, 피격, 사망 상태를 분리해 확인 가능하게 배치합니다.
""",
        encoding="utf-8",
    )
    comparison_rows = [
        ("../../../image/fuga2(푸가).png", "renders/01_front_fuga2_reference_match.png", "정면"),
        ("../../../image/fuga2-beside.png", "renders/02_side_fuga2_beside_reference_match.png", "측면"),
        ("../../../image/fuga2-back.png", "renders/03_back_fuga2_back_reference_match.png", "후면"),
    ]
    comparison_html = "\n".join(
        f"""
      <article class="comparison">
        <h3>{label}</h3>
        <figure><img src="{ref}" alt="{label} 기준 이미지"><figcaption>기준 이미지: {ref}</figcaption></figure>
        <figure><img src="{render}" alt="{label} 생성 렌더"><figcaption>생성 렌더: {render}</figcaption></figure>
      </article>"""
        for ref, render, label in comparison_rows
    )
    render_links = "\n".join(
        f'<figure><img src="renders/{p.name}" alt="{p.stem}"><figcaption>{p.name}</figcaption></figure>'
        for p in sorted(RENDER_DIR.glob("*.png"))
    )
    death_sequence_links = "\n".join(
        f'<figure><img src="renders/{step["render"]}.png" alt="{step["label"]}"><figcaption>{step["label"]}<br>{step["render"]}.png</figcaption></figure>'
        for step in DEATH_SEQUENCE_STEPS
    )
    texture_links = "\n".join(
        f'<figure><img src="textures/{p.name}" alt="{p.stem}"><figcaption>{p.name}</figcaption></figure>'
        for p in sorted(TEXTURE_DIR.glob("*.png"))
    )
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <title>푸가 모델링 샘플</title>
  <style>
    body {{ margin: 0; font-family: Arial, sans-serif; background: #18201a; color: #edf2e6; }}
    main {{ max-width: 1180px; margin: 0 auto; padding: 28px; }}
    h1, h2, h3 {{ margin: 0 0 14px; }}
    section {{ margin: 30px 0; }}
    .grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 16px; }}
    .comparison {{ display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-bottom: 22px; background: #222b24; border: 1px solid #435041; padding: 14px; }}
    .comparison h3 {{ grid-column: 1 / -1; }}
    figure {{ margin: 0; background: #111711; border: 1px solid #364236; padding: 10px; }}
    img {{ width: 100%; height: auto; display: block; }}
    figcaption {{ margin-top: 8px; font-size: 13px; color: #cdd7c5; word-break: break-all; }}
    p, li {{ line-height: 1.55; }}
    code {{ color: #d7e6c8; }}
    @media (max-width: 760px) {{ .comparison {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>푸가 모델링 샘플</h1>
  <p>1차 재현 기준은 <code>fuga2(푸가).png</code>, <code>fuga2-back.png</code>, <code>fuga2-beside.png</code>입니다. 사용자 승인 전에는 Unity에 연결하지 않습니다.</p>

  <section>
    <h2>기준 이미지와 생성 렌더 비교</h2>
{comparison_html}
  </section>

  <section>
    <h2>생성 렌더</h2>
    <div class="grid">{render_links}</div>
  </section>

  <section>
    <h2>사망 모션 검토 시퀀스</h2>
    <p>Unity 적용 단계에서는 낙하 이동을 <code>Rigidbody + Collider</code> 기준으로 처리하고, 이 렌더들은 자세와 시간 흐름 검토용으로 사용합니다.</p>
    <div class="grid">{death_sequence_links}</div>
  </section>

  <section>
    <h2>사용 텍스처</h2>
    <div class="grid">{texture_links}</div>
  </section>
</main>
</body>
</html>
"""
    (ROOT / "index.html").write_text(html, encoding="utf-8")


def main():
    body, _left, _right = create_scene()
    render_set(body)
    export_files()
    write_text_files()


if __name__ == "__main__":
    main()
