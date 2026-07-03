from __future__ import annotations

import json
import math
import random
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


REPO_ROOT = Path(__file__).resolve().parents[4]
SAMPLE_ROOT = Path(__file__).resolve().parent
ORIGINAL_BLEND = REPO_ROOT / "enemies model" / "longa arma.blend"
BLEND_PATH = SAMPLE_ROOT / "blender" / "longa_arma_animation_ready_model.blend"
FBX_PATH = SAMPLE_ROOT / "exports" / "longa_arma_animation_ready_model.fbx"
GLB_PATH = SAMPLE_ROOT / "exports" / "longa_arma_animation_ready_model.glb"
MANIFEST_PATH = SAMPLE_ROOT / "ASSET_MANIFEST.json"
README_PATH = SAMPLE_ROOT / "README.md"
STATUS_PATH = SAMPLE_ROOT / "ANIMATION_READY_MODEL_STATUS_2026-07-03.md"
HTML_PATH = SAMPLE_ROOT / "index.html"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
RENDER_DIR = SAMPLE_ROOT / "renders"


FORWARD = Vector((0.0, -1.0, 0.0))
UP = Vector((0.0, 0.0, 1.0))
RIGHT = Vector((1.0, 0.0, 0.0))


def ensure_dirs() -> None:
    for path in [BLEND_PATH.parent, FBX_PATH.parent, GLB_PATH.parent, TEXTURE_DIR, RENDER_DIR]:
        path.mkdir(parents=True, exist_ok=True)


def clean_scene_after_open() -> None:
    for obj in list(bpy.context.scene.objects):
        obj.select_set(True)
    bpy.ops.object.delete()


def make_collection(name: str, *, hide_viewport: bool = False, hide_render: bool = False) -> bpy.types.Collection:
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    collection.hide_viewport = hide_viewport
    collection.hide_render = hide_render
    return collection


def move_to_collection(obj: bpy.types.Object, collection: bpy.types.Collection) -> None:
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    collection.objects.link(obj)


def create_texture(name: str, width: int, height: int, kind: str) -> Path:
    random.seed(name)
    image = bpy.data.images.new(name, width=width, height=height)
    pixels: list[float] = []
    for y in range(height):
        for x in range(width):
            u = x / max(1, width - 1)
            v = y / max(1, height - 1)
            n = (
                0.45 * math.sin((u * 17.0 + v * 7.0) * math.pi)
                + 0.30 * math.sin((u * 41.0 - v * 19.0) * math.pi)
                + 0.25 * random.random()
            )
            if kind == "flesh":
                crack = 1.0 if (math.sin((u * 23.0 + v * 31.0) * math.pi) > 0.93) else 0.0
                r = 0.05 + 0.06 * n - 0.025 * crack
                g = 0.20 + 0.22 * n - 0.06 * crack
                b = 0.12 + 0.10 * n - 0.045 * crack
                a = 1.0
            elif kind == "blade":
                scratch = 1.0 if abs(math.sin((u * 90.0 + v * 10.0) * math.pi)) > 0.985 else 0.0
                base = 0.10 + 0.10 * n + 0.22 * scratch
                r, g, b, a = base, base * 1.02, base * 1.05, 1.0
            else:
                r = 0.05 + 0.05 * n
                g = 0.22 + 0.18 * n
                b = 0.12 + 0.09 * n
                a = 0.58 + 0.20 * max(0.0, n)
            pixels.extend([max(0.0, min(1.0, r)), max(0.0, min(1.0, g)), max(0.0, min(1.0, b)), a])
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(TEXTURE_DIR / f"{name}.png")
    image.file_format = "PNG"
    image.save()
    return TEXTURE_DIR / f"{name}.png"


def material_from_texture(name: str, texture_path: Path, roughness: float, metallic: float = 0.0, alpha: float = 1.0) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (1.0, 1.0, 1.0, alpha)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(str(texture_path), check_existing=True)
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    if alpha < 1.0:
        mat.blend_method = "BLEND"
        mat.use_screen_refraction = True
        bsdf.inputs["Alpha"].default_value = alpha
        mat.node_tree.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    bump = mat.node_tree.nodes.new("ShaderNodeBump")
    noise = mat.node_tree.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 38.0
    noise.inputs["Detail"].default_value = 9.0
    bump.inputs["Strength"].default_value = 0.09
    bump.inputs["Distance"].default_value = 0.045
    mat.node_tree.links.new(noise.outputs["Fac"], bump.inputs["Height"])
    mat.node_tree.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def make_plain_mat(name: str, color: tuple[float, float, float, float], roughness: float = 0.6) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = color
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    if color[3] < 1.0:
        mat.blend_method = "BLEND"
        bsdf.inputs["Alpha"].default_value = color[3]
    return mat


def select_active(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def create_ellipsoid(
    name: str,
    location: tuple[float, float, float],
    scale: tuple[float, float, float],
    material: bpy.types.Material,
    collection: bpy.types.Collection,
    subdivisions: int = 2,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_mesh"
    obj.scale = scale
    obj.data.materials.append(material)
    select_active(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    return obj


def add_shape_keys_for_body(obj: bpy.types.Object) -> None:
    select_active(obj)
    bpy.ops.object.shape_key_add(from_mix=False)
    obj.data.shape_keys.key_blocks[0].name = "Basis"
    breathe = obj.shape_key_add(name="ANIM_idle_body_breathe", from_mix=False)
    recoil = obj.shape_key_add(name="ANIM_hit_recoil_compress", from_mix=False)
    flatten = obj.shape_key_add(name="ANIM_death_flatten_widen", from_mix=False)
    for i, vertex in enumerate(obj.data.vertices):
        base = vertex.co.copy()
        breathe.data[i].co = Vector((base.x * 1.035, base.y * 1.015, base.z * 1.055))
        recoil.data[i].co = Vector((base.x * 0.93, base.y * 0.90, base.z * 1.08))
        flatten.data[i].co = Vector((base.x * 1.45, base.y * 1.25, base.z * 0.18 - 0.12))


def circle_basis(direction: Vector) -> tuple[Vector, Vector]:
    direction = direction.normalized()
    ref = UP
    if abs(direction.dot(ref)) > 0.92:
        ref = RIGHT
    axis_a = direction.cross(ref).normalized()
    axis_b = direction.cross(axis_a).normalized()
    return axis_a, axis_b


def create_tapered_segment(
    name: str,
    start: Vector,
    end: Vector,
    radius_start: float,
    radius_end: float,
    material: bpy.types.Material,
    collection: bpy.types.Collection,
    sides: int = 7,
) -> bpy.types.Object:
    direction = end - start
    length = direction.length
    axis_a, axis_b = circle_basis(direction)
    verts: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []
    for ring, (offset, radius) in enumerate([(0.0, radius_start), (length, radius_end)]):
        center = direction.normalized() * offset
        for i in range(sides):
            angle = (math.tau * i / sides) + (0.2 if ring else 0.0)
            p = center + axis_a * math.cos(angle) * radius + axis_b * math.sin(angle) * radius
            verts.append(tuple(p))
    for i in range(sides):
        faces.append((i, (i + 1) % sides, sides + ((i + 1) % sides), sides + i))
    faces.append(tuple(range(sides - 1, -1, -1)))
    faces.append(tuple(range(sides, sides * 2)))
    mesh = bpy.data.meshes.new(f"{name}_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = start
    obj.data.materials.append(material)
    collection.objects.link(obj)
    return obj


def create_foot(
    name: str,
    start: Vector,
    end: Vector,
    width: float,
    height: float,
    material: bpy.types.Material,
    collection: bpy.types.Collection,
) -> bpy.types.Object:
    direction = (end - start).normalized()
    side, _ = circle_basis(direction)
    length = (end - start).length
    verts = [
        tuple(side * -width * 0.5 + Vector((0, 0, 0))),
        tuple(side * width * 0.5 + Vector((0, 0, 0))),
        tuple(direction * length + side * width * 0.42 + Vector((0, 0, -height * 0.15))),
        tuple(direction * length + side * -width * 0.42 + Vector((0, 0, -height * 0.15))),
        tuple(direction * length * 0.42 + Vector((0, 0, height))),
    ]
    faces = [(0, 1, 4), (1, 2, 4), (2, 3, 4), (3, 0, 4), (0, 3, 2, 1)]
    mesh = bpy.data.meshes.new(f"{name}_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = start
    obj.data.materials.append(material)
    collection.objects.link(obj)
    return obj


def create_blade(
    name: str,
    root: Vector,
    material: bpy.types.Material,
    collection: bpy.types.Collection,
) -> bpy.types.Object:
    verts = [
        (0.00, 0.00, 0.00),
        (-0.16, -0.30, 0.08),
        (-0.34, -0.70, 0.14),
        (-0.30, -1.08, 0.10),
        (-0.08, -1.34, 0.02),
        (0.16, -1.02, -0.08),
        (0.18, -0.54, -0.09),
        (0.08, -0.12, -0.04),
        (0.00, -0.58, 0.028),
        (0.02, -0.58, -0.028),
    ]
    faces = [
        (0, 1, 8, 7),
        (1, 2, 8),
        (2, 3, 4, 8),
        (4, 5, 9, 8),
        (5, 6, 9),
        (6, 7, 8, 9),
        (0, 7, 6, 5, 4, 3, 2, 1),
    ]
    mesh = bpy.data.meshes.new(f"{name}_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = root
    obj.data.materials.append(material)
    collection.objects.link(obj)
    return obj


def create_puddle_target(material: bpy.types.Material, collection: bpy.types.Collection) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=18, radius=1.0, depth=0.035, location=(0.0, 0.04, 0.022))
    obj = bpy.context.object
    obj.name = "ANIM_DEATH_puddle_target_mesh"
    obj.data.name = "ANIM_DEATH_puddle_target_meshData"
    obj.scale = (0.82, 1.10, 1.0)
    obj.data.materials.append(material)
    obj.hide_viewport = True
    obj.hide_render = True
    select_active(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    obj["animation_usage"] = "Use as final visible swap target for Death_MeltPuddle instead of forcing all limb bones into a liquid shape."
    return obj


def assign_rigid_bone(obj: bpy.types.Object, bone_name: str, armature: bpy.types.Object) -> None:
    group = obj.vertex_groups.new(name=bone_name)
    group.add(list(range(len(obj.data.vertices))), 1.0, "ADD")
    obj.parent = armature
    obj.matrix_parent_inverse = armature.matrix_world.inverted()
    mod = obj.modifiers.new("RigidAnimationReadyArmature", "ARMATURE")
    mod.object = armature
    obj["animation_bone"] = bone_name
    obj["weighting_policy"] = "Rigid 1.0 weight to avoid the tearing seen in the failed smooth-skinned runtime_lowpoly pass."


def create_armature(parts: dict[str, tuple[Vector, Vector]], collection: bpy.types.Collection) -> bpy.types.Object:
    arm_data = bpy.data.armatures.new("LongaArma_AnimationReady_RigGuideData")
    arm_obj = bpy.data.objects.new("LongaArma_AnimationReady_RigGuide", arm_data)
    collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    arm_obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for index, (name, (head, tail)) in enumerate(parts.items()):
        bone = arm_data.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        bone.roll = 0.0
        if index and name != "DEF_root":
            bone.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    arm_obj["purpose"] = "Animation-ready guide rig. Mesh pieces use rigid single-bone weights for readable creature motion."
    return arm_obj


def set_origins_from_bones(objects: dict[str, bpy.types.Object], starts: dict[str, Vector]) -> None:
    for name, obj in objects.items():
        if name not in starts:
            continue
        bpy.context.scene.cursor.location = starts[name]
        select_active(obj)
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")


def create_model(model_collection: bpy.types.Collection, rig_collection: bpy.types.Collection) -> tuple[bpy.types.Object, list[bpy.types.Object], dict]:
    flesh_tex = create_texture("longa_animation_ready_flesh_mottle", 512, 512, "flesh")
    blade_tex = create_texture("longa_animation_ready_blade_scratches", 512, 512, "blade")
    goo_tex = create_texture("longa_animation_ready_translucent_goo", 512, 512, "goo")

    flesh_mat = material_from_texture("M_LongaAnimReady_WetMottledFlesh", flesh_tex, roughness=0.82)
    dark_flesh_mat = material_from_texture("M_LongaAnimReady_DarkUndersideFlesh", flesh_tex, roughness=0.9)
    blade_mat = material_from_texture("M_LongaAnimReady_DarkScratchedBlade", blade_tex, roughness=0.58, metallic=0.08)
    goo_mat = material_from_texture("M_LongaAnimReady_TranslucentJointGoo", goo_tex, roughness=0.35, alpha=0.72)
    eye_mat = make_plain_mat("M_LongaAnimReady_AmberEyes", (1.0, 0.42, 0.08, 1.0), roughness=0.42)

    bone_defs: dict[str, tuple[Vector, Vector]] = {
        "DEF_root": (Vector((0.0, 0.02, 0.16)), Vector((0.0, 0.02, 0.48))),
        "DEF_body": (Vector((0.0, 0.10, 0.48)), Vector((0.0, -0.34, 0.56))),
        "DEF_chest": (Vector((0.0, -0.38, 0.54)), Vector((0.0, -0.66, 0.66))),
        "DEF_pelvis": (Vector((0.0, 0.36, 0.47)), Vector((0.0, 0.58, 0.43))),
        "DEF_neck": (Vector((0.0, -0.68, 0.65)), Vector((0.0, -0.86, 0.74))),
        "DEF_head": (Vector((0.0, -0.86, 0.74)), Vector((0.0, -1.06, 0.68))),
        "DEF_jaw": (Vector((0.0, -1.00, 0.64)), Vector((0.0, -1.12, 0.58))),
        "DEF_blade_upper_l": (Vector((-0.25, -0.44, 0.55)), Vector((-0.58, -0.56, 0.43))),
        "DEF_blade_fore_l": (Vector((-0.58, -0.56, 0.43)), Vector((-0.78, -0.86, 0.24))),
        "DEF_blade_tip_l": (Vector((-0.78, -0.86, 0.24)), Vector((-0.91, -1.34, 0.04))),
        "DEF_front_right_upper": (Vector((0.24, -0.42, 0.47)), Vector((0.34, -0.38, 0.24))),
        "DEF_front_right_lower": (Vector((0.34, -0.38, 0.24)), Vector((0.38, -0.52, 0.08))),
        "DEF_front_right_foot": (Vector((0.38, -0.52, 0.08)), Vector((0.44, -0.70, 0.02))),
        "DEF_front_left_upper": (Vector((-0.23, -0.34, 0.43)), Vector((-0.34, -0.22, 0.23))),
        "DEF_front_left_lower": (Vector((-0.34, -0.22, 0.23)), Vector((-0.38, -0.32, 0.08))),
        "DEF_front_left_foot": (Vector((-0.38, -0.32, 0.08)), Vector((-0.44, -0.46, 0.02))),
        "DEF_rear_right_upper": (Vector((0.24, 0.42, 0.43)), Vector((0.38, 0.48, 0.23))),
        "DEF_rear_right_lower": (Vector((0.38, 0.48, 0.23)), Vector((0.42, 0.30, 0.08))),
        "DEF_rear_right_foot": (Vector((0.42, 0.30, 0.08)), Vector((0.52, 0.16, 0.02))),
        "DEF_rear_left_upper": (Vector((-0.24, 0.42, 0.43)), Vector((-0.38, 0.50, 0.22))),
        "DEF_rear_left_lower": (Vector((-0.38, 0.50, 0.22)), Vector((-0.42, 0.34, 0.08))),
        "DEF_rear_left_foot": (Vector((-0.42, 0.34, 0.08)), Vector((-0.52, 0.18, 0.02))),
    }
    armature = create_armature(bone_defs, rig_collection)

    created: dict[str, bpy.types.Object] = {}

    created["PART_body_core"] = create_ellipsoid("PART_body_core", (0.0, 0.04, 0.48), (0.36, 0.55, 0.30), flesh_mat, model_collection, 2)
    created["PART_chest_lift_mass"] = create_ellipsoid("PART_chest_lift_mass", (0.0, -0.43, 0.57), (0.31, 0.33, 0.32), flesh_mat, model_collection, 2)
    created["PART_pelvis_drag_mass"] = create_ellipsoid("PART_pelvis_drag_mass", (0.0, 0.48, 0.43), (0.32, 0.29, 0.25), dark_flesh_mat, model_collection, 2)
    created["PART_neck_hinge"] = create_tapered_segment("PART_neck_hinge", Vector((0.0, -0.67, 0.63)), Vector((0.0, -0.84, 0.74)), 0.13, 0.10, flesh_mat, model_collection)
    created["PART_head_skull"] = create_ellipsoid("PART_head_skull", (0.0, -0.97, 0.69), (0.17, 0.25, 0.16), flesh_mat, model_collection, 2)
    created["PART_jaw_peck_block"] = create_tapered_segment("PART_jaw_peck_block", Vector((0.0, -1.02, 0.62)), Vector((0.0, -1.18, 0.56)), 0.08, 0.045, dark_flesh_mat, model_collection, 6)
    created["PART_blade_upper_l"] = create_tapered_segment("PART_blade_upper_l", bone_defs["DEF_blade_upper_l"][0], bone_defs["DEF_blade_upper_l"][1], 0.105, 0.085, flesh_mat, model_collection)
    created["PART_blade_fore_l"] = create_tapered_segment("PART_blade_fore_l", bone_defs["DEF_blade_fore_l"][0], bone_defs["DEF_blade_fore_l"][1], 0.085, 0.052, flesh_mat, model_collection)
    created["PART_blade_scythe_l"] = create_blade("PART_blade_scythe_l", bone_defs["DEF_blade_tip_l"][0], blade_mat, model_collection)

    leg_specs = [
        ("front_right", 0.085, 0.070),
        ("front_left", 0.078, 0.062),
        ("rear_right", 0.083, 0.067),
        ("rear_left", 0.083, 0.067),
    ]
    for prefix, upper_radius, lower_radius in leg_specs:
        upper = f"DEF_{prefix}_upper"
        lower = f"DEF_{prefix}_lower"
        foot = f"DEF_{prefix}_foot"
        created[f"PART_{prefix}_upper"] = create_tapered_segment(
            f"PART_{prefix}_upper", bone_defs[upper][0], bone_defs[upper][1], upper_radius, lower_radius, flesh_mat, model_collection
        )
        created[f"PART_{prefix}_lower"] = create_tapered_segment(
            f"PART_{prefix}_lower", bone_defs[lower][0], bone_defs[lower][1], lower_radius, 0.045, dark_flesh_mat, model_collection
        )
        created[f"PART_{prefix}_foot"] = create_foot(
            f"PART_{prefix}_foot", bone_defs[foot][0], bone_defs[foot][1], 0.13, 0.055, dark_flesh_mat, model_collection
        )

    goo_points = {
        "GOO_shoulder_blade_l": (-0.25, -0.44, 0.55, 0.13, "DEF_chest"),
        "GOO_front_right_shoulder": (0.24, -0.42, 0.47, 0.105, "DEF_chest"),
        "GOO_front_left_shoulder": (-0.23, -0.34, 0.43, 0.095, "DEF_chest"),
        "GOO_rear_right_hip": (0.24, 0.42, 0.43, 0.105, "DEF_pelvis"),
        "GOO_rear_left_hip": (-0.24, 0.42, 0.43, 0.105, "DEF_pelvis"),
        "GOO_neck_collar": (0.0, -0.68, 0.64, 0.12, "DEF_chest"),
    }
    goo_assignments: dict[str, str] = {}
    for name, (x, y, z, r, bone) in goo_points.items():
        obj = create_ellipsoid(name, (x, y, z), (r * 1.15, r, r * 0.82), goo_mat, model_collection, 1)
        created[name] = obj
        goo_assignments[name] = bone

    for prefix, upper_radius, lower_radius in leg_specs:
        knee = bone_defs[f"DEF_{prefix}_lower"][0]
        ankle = bone_defs[f"DEF_{prefix}_foot"][0]
        knee_obj = create_ellipsoid(
            f"GOO_{prefix}_knee",
            tuple(knee),
            (lower_radius * 1.20, lower_radius * 1.10, lower_radius),
            goo_mat,
            model_collection,
            1,
        )
        ankle_obj = create_ellipsoid(
            f"GOO_{prefix}_ankle",
            tuple(ankle),
            (0.060, 0.052, 0.045),
            goo_mat,
            model_collection,
            1,
        )
        created[knee_obj.name] = knee_obj
        created[ankle_obj.name] = ankle_obj
        goo_assignments[knee_obj.name] = f"DEF_{prefix}_lower"
        goo_assignments[ankle_obj.name] = f"DEF_{prefix}_foot"

    blade_elbow = create_ellipsoid(
        "GOO_blade_elbow_l",
        tuple(bone_defs["DEF_blade_fore_l"][0]),
        (0.085, 0.070, 0.065),
        goo_mat,
        model_collection,
        1,
    )
    blade_wrist = create_ellipsoid(
        "GOO_blade_wrist_l",
        tuple(bone_defs["DEF_blade_tip_l"][0]),
        (0.070, 0.058, 0.050),
        goo_mat,
        model_collection,
        1,
    )
    created[blade_elbow.name] = blade_elbow
    created[blade_wrist.name] = blade_wrist
    goo_assignments[blade_elbow.name] = "DEF_blade_fore_l"
    goo_assignments[blade_wrist.name] = "DEF_blade_tip_l"

    for side, x in [("left", -0.075), ("right", 0.075)]:
        eye = create_ellipsoid(f"PART_eye_{side}", (x, -1.205, 0.705), (0.034, 0.021, 0.031), eye_mat, model_collection, 1)
        created[f"PART_eye_{side}"] = eye

    for name in ["PART_body_core", "PART_chest_lift_mass", "PART_pelvis_drag_mass"]:
        add_shape_keys_for_body(created[name])

    puddle = create_puddle_target(goo_mat, model_collection)
    created[puddle.name] = puddle

    bone_map = {
        "PART_body_core": "DEF_body",
        "PART_chest_lift_mass": "DEF_chest",
        "PART_pelvis_drag_mass": "DEF_pelvis",
        "PART_neck_hinge": "DEF_neck",
        "PART_head_skull": "DEF_head",
        "PART_jaw_peck_block": "DEF_jaw",
        "PART_blade_upper_l": "DEF_blade_upper_l",
        "PART_blade_fore_l": "DEF_blade_fore_l",
        "PART_blade_scythe_l": "DEF_blade_tip_l",
        "PART_eye_left": "DEF_head",
        "PART_eye_right": "DEF_head",
    }
    for prefix, _, _ in leg_specs:
        bone_map[f"PART_{prefix}_upper"] = f"DEF_{prefix}_upper"
        bone_map[f"PART_{prefix}_lower"] = f"DEF_{prefix}_lower"
        bone_map[f"PART_{prefix}_foot"] = f"DEF_{prefix}_foot"
    bone_map.update(goo_assignments)

    starts = {name: head for name, (head, _tail) in bone_defs.items()}
    part_start_map = {
        "PART_body_core": starts["DEF_body"],
        "PART_chest_lift_mass": starts["DEF_chest"],
        "PART_pelvis_drag_mass": starts["DEF_pelvis"],
        "PART_neck_hinge": starts["DEF_neck"],
        "PART_head_skull": starts["DEF_head"],
        "PART_jaw_peck_block": starts["DEF_jaw"],
        "PART_blade_upper_l": starts["DEF_blade_upper_l"],
        "PART_blade_fore_l": starts["DEF_blade_fore_l"],
        "PART_blade_scythe_l": starts["DEF_blade_tip_l"],
    }
    for prefix, _, _ in leg_specs:
        part_start_map[f"PART_{prefix}_upper"] = starts[f"DEF_{prefix}_upper"]
        part_start_map[f"PART_{prefix}_lower"] = starts[f"DEF_{prefix}_lower"]
        part_start_map[f"PART_{prefix}_foot"] = starts[f"DEF_{prefix}_foot"]
    set_origins_from_bones(created, part_start_map)

    for obj_name, bone_name in bone_map.items():
        assign_rigid_bone(created[obj_name], bone_name, armature)

    for obj in created.values():
        obj["sample_stage"] = "animation_ready_model"
        obj["export_target"] = not obj.hide_viewport

    readiness = {
        "modelingPurpose": "Animation-ready Longa Arma model, not final animation clips.",
        "deformationStrategy": "Segmented rigid-weight parts with overlapping goo collars, plus Shape Keys for body/impact/death deformation.",
        "motionTargets": {
            "Idle": "PART_body_core and PART_chest_lift_mass Shape Keys for visible body breathing.",
            "Move": "Four separate leg chains can be keyed independently without smooth-weight tearing.",
            "Attack": "DEF_chest, DEF_blade_* and DEF_front_right_* are separated for upper-body lift, blade raise, right-front-leg raise and slam.",
            "Hit": "Head/jaw rigid bones plus body recoil Shape Key support side head shake and body flinch.",
            "Consume": "Neck, head and jaw are separate for backward head tilt and forward peck.",
            "Death": "Body flatten Shape Keys plus hidden ANIM_DEATH_puddle_target_mesh for puddle swap/transition.",
        },
    }
    armature["animation_readiness"] = json.dumps(readiness, ensure_ascii=False)

    model_objects = [obj for obj in created.values() if not obj.hide_viewport]
    model_objects.append(armature)
    return armature, model_objects, readiness


def load_original_reference(reference_collection: bpy.types.Collection) -> dict:
    if not ORIGINAL_BLEND.exists():
        return {"loaded": False, "reason": str(ORIGINAL_BLEND)}
    with bpy.data.libraries.load(str(ORIGINAL_BLEND), link=False) as (data_from, data_to):
        data_to.objects = [name for name in data_from.objects if name == "mesh_node"]
    loaded = []
    for obj in data_to.objects:
        if obj is None:
            continue
        reference_collection.objects.link(obj)
        obj.name = f"REF_original_{obj.name}"
        obj.hide_viewport = True
        obj.hide_render = True
        obj["reference_only"] = True
        loaded.append(
            {
                "name": obj.name,
                "type": obj.type,
                "vertices": len(obj.data.vertices) if obj.type == "MESH" else 0,
                "faces": len(obj.data.polygons) if obj.type == "MESH" else 0,
                "dimensions": [round(v, 4) for v in obj.dimensions],
            }
        )
    return {"loaded": bool(loaded), "objects": loaded}


def reset_pose(armature: bpy.types.Object) -> None:
    for pb in armature.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = (1.0, 1.0, 1.0)
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.data.shape_keys:
            for key in obj.data.shape_keys.key_blocks:
                key.value = 0.0
    bpy.context.view_layer.update()


def pose_for_check(armature: bpy.types.Object, pose_name: str) -> None:
    reset_pose(armature)
    pb = armature.pose.bones
    if pose_name == "move_crawl_check":
        pb["DEF_chest"].rotation_euler[0] = math.radians(-7)
        pb["DEF_body"].rotation_euler[2] = math.radians(4)
        pb["DEF_front_right_upper"].rotation_euler[0] = math.radians(28)
        pb["DEF_front_right_lower"].rotation_euler[0] = math.radians(-42)
        pb["DEF_front_left_upper"].rotation_euler[0] = math.radians(-18)
        pb["DEF_front_left_lower"].rotation_euler[0] = math.radians(28)
        pb["DEF_rear_right_upper"].rotation_euler[0] = math.radians(-24)
        pb["DEF_rear_right_lower"].rotation_euler[0] = math.radians(30)
        pb["DEF_rear_left_upper"].rotation_euler[0] = math.radians(22)
        pb["DEF_rear_left_lower"].rotation_euler[0] = math.radians(-34)
        pb["DEF_blade_upper_l"].rotation_euler[1] = math.radians(-18)
    elif pose_name == "attack_lift_check":
        pb["DEF_chest"].rotation_euler[0] = math.radians(-35)
        pb["DEF_neck"].rotation_euler[0] = math.radians(15)
        pb["DEF_head"].rotation_euler[0] = math.radians(12)
        pb["DEF_blade_upper_l"].rotation_euler[0] = math.radians(-52)
        pb["DEF_blade_fore_l"].rotation_euler[0] = math.radians(38)
        pb["DEF_blade_tip_l"].rotation_euler[2] = math.radians(-18)
        pb["DEF_front_right_upper"].rotation_euler[0] = math.radians(-45)
        pb["DEF_front_right_lower"].rotation_euler[0] = math.radians(38)
        pb["DEF_rear_left_upper"].rotation_euler[0] = math.radians(18)
        pb["DEF_rear_right_upper"].rotation_euler[0] = math.radians(18)
        if bpy.data.objects.get("PART_chest_lift_mass").data.shape_keys:
            bpy.data.objects["PART_chest_lift_mass"].data.shape_keys.key_blocks["ANIM_idle_body_breathe"].value = 0.45
    elif pose_name == "death_puddle_check":
        for obj_name in ["PART_body_core", "PART_chest_lift_mass", "PART_pelvis_drag_mass"]:
            obj = bpy.data.objects.get(obj_name)
            if obj and obj.data.shape_keys:
                obj.data.shape_keys.key_blocks["ANIM_death_flatten_widen"].value = 1.0
        pb["DEF_chest"].rotation_euler[0] = math.radians(22)
        pb["DEF_neck"].rotation_euler[0] = math.radians(30)
        pb["DEF_head"].rotation_euler[0] = math.radians(36)
        pb["DEF_blade_upper_l"].rotation_euler[0] = math.radians(28)
        pb["DEF_blade_fore_l"].rotation_euler[0] = math.radians(18)
    bpy.context.view_layer.update()


def setup_camera_and_lights() -> bpy.types.Camera:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -4.0, 4.2))
    key = bpy.context.object
    key.name = "AREA_key_softbox"
    key.data.energy = 500.0
    key.data.size = 5.0
    bpy.ops.object.light_add(type="POINT", location=(-2.0, 1.5, 2.2))
    rim = bpy.context.object
    rim.name = "POINT_green_rim"
    rim.data.energy = 80.0
    bpy.ops.object.camera_add(location=(1.9, -3.0, 1.35), rotation=(math.radians(64), 0.0, math.radians(34)))
    camera = bpy.context.object
    bpy.context.scene.camera = camera
    camera.data.lens = 31.0
    bpy.context.scene.render.resolution_x = 1400
    bpy.context.scene.render.resolution_y = 900
    bpy.context.scene.eevee.taa_render_samples = 32
    return camera.data


def aim_camera_at(camera_obj: bpy.types.Object, target: Vector) -> None:
    direction = target - camera_obj.location
    camera_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_pose(armature: bpy.types.Object, pose_name: str, filename: str, camera_loc: tuple[float, float, float]) -> str:
    pose_for_check(armature, pose_name)
    camera_obj = bpy.context.scene.camera
    camera_obj.location = camera_loc
    puddle = bpy.data.objects.get("ANIM_DEATH_puddle_target_mesh")
    if puddle:
        puddle.hide_viewport = pose_name != "death_puddle_check"
        puddle.hide_render = pose_name != "death_puddle_check"
    aim_camera_at(camera_obj, Vector((-0.08, -0.34, 0.46)))
    bpy.context.scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)
    return f"renders/{filename}"


def write_docs(report: dict) -> None:
    manifest = {
        "assetId": "longa_arma_animation_ready_model",
        "createdAt": "2026-07-03",
        "sourceBlend": str(ORIGINAL_BLEND.relative_to(REPO_ROOT)).replace("\\", "/"),
        "purpose": "Animation-ready modeling sample for Longa Arma.",
        "outputs": {
            "blend": str(BLEND_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "fbx": str(FBX_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "glb": str(GLB_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "renders": report["renders"],
            "textures": [str(Path("textures") / p.name).replace("\\", "/") for p in TEXTURE_DIR.glob("*.png")],
        },
        "animationReadyParts": report["parts"],
        "notUnityApplied": True,
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    README_PATH.write_text(
        """# Longa Arma Animation-Ready Model Sample

- 기준 원본: `enemies model/longa arma.blend`
- 목적: 대기, 이동, 공격, 피격, 섭취, 사망 애니메이션을 나중에 제대로 넣을 수 있도록 모델 구조를 다시 만든 샘플입니다.
- 이번 산출물은 최종 애니메이션 클립이 아니라 애니메이션 가능한 모델링 구조입니다.

## 핵심 구조

- 단일 고밀도 원본 메시를 그대로 쓰지 않고, 몸통, 가슴, 골반, 목, 머리, 턱, 네 다리, 왼쪽 칼날 팔을 별도 파트로 재구성했습니다.
- 각 파트는 `LongaArma_AnimationReady_RigGuide`의 해당 `DEF_*` 본에 rigid 1.0 weight로 연결됩니다.
- 관절부는 `GOO_*` 점액/살점 덩어리로 겹쳐서 큰 동작에서도 찢어져 보이는 문제를 줄이도록 구성했습니다.
- 몸통, 가슴, 골반에는 대기/피격/사망용 Shape Key가 들어 있습니다.
- 사망 웅덩이는 본으로 억지 변형하지 않고 `ANIM_DEATH_puddle_target_mesh`로 전환할 수 있게 별도 타깃 메시를 포함했습니다.

## 애니메이션 가능 기준

- 대기: `PART_body_core`, `PART_chest_lift_mass`의 호흡 Shape Key로 몸통 모핑 가능
- 이동: 네 다리 체인을 각각 따로 키잉 가능
- 공격: 상체 리프트, 왼쪽 칼날 팔, 오른 앞다리 리프트/내리찍기 가능
- 피격: 머리/턱 본과 몸통 압축 Shape Key로 고개 흔들림과 recoil 가능
- 섭취: 목, 머리, 턱 파트로 뒤젖힘과 전방 peck 가능
- 사망: 몸통 flatten Shape Key와 웅덩이 타깃 메시로 녹아내림/웅덩이 전환 가능

## 산출물

- `blender/longa_arma_animation_ready_model.blend`
- `exports/longa_arma_animation_ready_model.fbx`
- `exports/longa_arma_animation_ready_model.glb`
- `renders/01_neutral_front.png`
- `renders/02_side_structure.png`
- `renders/03_move_crawl_structure_check.png`
- `renders/04_attack_lift_structure_check.png`
- `renders/05_death_puddle_structure_check.png`
- `textures/*.png`

## 주의

- 기존 `runtime_lowpoly` 결과는 이번 애니메이션 작업 기준에서 제외했습니다.
- 이 샘플은 Unity에 적용하지 않았습니다.
- 실제 애니메이션 클립은 이 구조가 승인된 뒤 별도 단계로 제작해야 합니다.
""",
        encoding="utf-8",
    )

    STATUS_PATH.write_text(
        f"""# Longa Arma Animation-Ready Model Status - 2026-07-03

## Result

- Source: `enemies model/longa arma.blend`
- Original source mesh: {report["originalReference"]}
- New low-poly visible mesh objects: {report["visibleMeshObjectCount"]}
- New visible vertices: {report["visibleVertexCount"]}
- New visible faces: {report["visibleFaceCount"]}
- Guide rig bones: {report["guideBoneCount"]}
- Exported FBX: `exports/longa_arma_animation_ready_model.fbx`
- Exported GLB: `exports/longa_arma_animation_ready_model.glb`

## Possible

- Rebuilt Longa Arma as an animation-ready segmented model instead of a single smooth-skinned mesh.
- Added rigid-weight body parts for four independent legs, neck/head/jaw, and the left blade arm.
- Added overlapping goo collars to hide joint gaps during large readable motions.
- Added body/chest/pelvis Shape Keys for idle breathing, hit compression, and death flattening.
- Added a separate hidden puddle target mesh for death transition.
- Generated structure-check renders for neutral, side, crawl, attack lift, and death flatten states.

## Not Done

- No final animation clips were authored in this stage.
- No Unity scene, prefab, Animator, bridge, smoke, PlayMode, EditMode, build, or Git command was run.
- This sample still needs user visual approval before Unity runtime application.
""",
        encoding="utf-8",
    )

    image_items = "\n".join(f'<figure><img src="{render}" /><figcaption>{Path(render).name}</figcaption></figure>' for render in report["renders"])
    HTML_PATH.write_text(
        f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Longa Arma Animation-Ready Model</title>
  <style>
    body {{ margin: 0; font-family: system-ui, sans-serif; background: #111615; color: #e8eee9; }}
    main {{ max-width: 1180px; margin: 0 auto; padding: 28px; }}
    h1 {{ font-size: 28px; margin: 0 0 10px; }}
    p {{ color: #b9c5bd; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 16px; }}
    figure {{ margin: 0; background: #1b2321; border: 1px solid #2f3b37; border-radius: 6px; overflow: hidden; }}
    img {{ width: 100%; display: block; }}
    figcaption {{ padding: 10px 12px; color: #cfd8d2; font-size: 14px; }}
    code {{ color: #d6ffc9; }}
  </style>
</head>
<body>
  <main>
    <h1>Longa Arma Animation-Ready Model</h1>
    <p>원본 <code>enemies model/longa arma.blend</code>를 기준으로 애니메이션을 넣을 수 있게 분절형 로우폴리 구조로 재제작한 샘플입니다. 이 페이지는 최종 애니메이션이 아니라 모델 구조 검토용입니다.</p>
    <div class="grid">
      {image_items}
    </div>
  </main>
</body>
</html>
""",
        encoding="utf-8",
    )


def export_selected(objects: list[bpy.types.Object]) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[-1]
    bpy.ops.export_scene.fbx(filepath=str(FBX_PATH), use_selection=True, add_leaf_bones=False, bake_anim=False)
    bpy.ops.export_scene.gltf(filepath=str(GLB_PATH), export_format="GLB", use_selection=True)


def build_report(model_objects: list[bpy.types.Object], armature: bpy.types.Object, original_reference: dict, renders: list[str], readiness: dict) -> dict:
    visible_meshes = [obj for obj in model_objects if obj.type == "MESH" and not obj.hide_viewport]
    return {
        "originalReference": original_reference,
        "visibleMeshObjectCount": len(visible_meshes),
        "visibleVertexCount": sum(len(obj.data.vertices) for obj in visible_meshes),
        "visibleFaceCount": sum(len(obj.data.polygons) for obj in visible_meshes),
        "guideBoneCount": len(armature.data.bones),
        "parts": [obj.name for obj in visible_meshes],
        "renders": renders,
        "readiness": readiness,
    }


def main() -> None:
    ensure_dirs()
    clean_scene_after_open()

    reference_collection = make_collection("Reference_Original_Do_Not_Export", hide_viewport=True, hide_render=True)
    model_collection = make_collection("AnimationReady_Model")
    rig_collection = make_collection("AnimationReady_RigGuide")
    original_reference = load_original_reference(reference_collection)

    armature, export_objects, readiness = create_model(model_collection, rig_collection)
    setup_camera_and_lights()

    renders = [
        render_pose(armature, "neutral", "01_neutral_front.png", (2.05, -3.35, 1.25)),
        render_pose(armature, "neutral", "02_side_structure.png", (3.25, 0.05, 1.12)),
        render_pose(armature, "move_crawl_check", "03_move_crawl_structure_check.png", (1.95, -3.35, 1.22)),
        render_pose(armature, "attack_lift_check", "04_attack_lift_structure_check.png", (2.05, -3.35, 1.45)),
        render_pose(armature, "death_puddle_check", "05_death_puddle_structure_check.png", (2.05, -3.25, 1.18)),
    ]
    reset_pose(armature)

    export_selected(export_objects)
    report = build_report(export_objects, armature, original_reference, renders, readiness)
    write_docs(report)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print("LONGA_ARMA_ANIMATION_READY_MODEL_CREATED")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
