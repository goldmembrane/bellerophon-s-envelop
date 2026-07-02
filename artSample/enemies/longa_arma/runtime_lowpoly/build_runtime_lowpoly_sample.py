from __future__ import annotations

import json
import math
import shutil
from datetime import datetime
from pathlib import Path

import bpy
from mathutils import Vector


TARGET_TRIANGLES = 12000
RENDER_WIDTH = 1400
RENDER_HEIGHT = 1000


SCRIPT_PATH = Path(__file__).resolve()
PROJECT_ROOT = SCRIPT_PATH.parents[4]
OUTPUT_ROOT = SCRIPT_PATH.parent
SOURCE_BLEND = PROJECT_ROOT / "Assets/_Project/Art/Enemies/LongaArma/Models/longa_arma.blend"
SOURCE_TEXTURE_ROOT = PROJECT_ROOT / "artSample/enemies/longa_arma/textures"

BLENDER_OUTPUT = OUTPUT_ROOT / "blender/longa_arma_runtime_lowpoly.blend"
FBX_OUTPUT = OUTPUT_ROOT / "exports/longa_arma_runtime_lowpoly.fbx"
GLB_OUTPUT = OUTPUT_ROOT / "exports/longa_arma_runtime_lowpoly.glb"
RENDER_ROOT = OUTPUT_ROOT / "renders"
TEXTURE_OUTPUT_ROOT = OUTPUT_ROOT / "textures"
MANIFEST_OUTPUT = OUTPUT_ROOT / "asset_manifest.json"
README_OUTPUT = OUTPUT_ROOT / "README.md"
STATS_OUTPUT = OUTPUT_ROOT / "polycount_stats.json"


TEXTURE_FILES = [
    "longa_arma_wet_green_albedo.png",
    "longa_arma_wet_green_roughness.png",
    "longa_arma_wet_green_bump.png",
    "longa_arma_dark_blade_albedo.png",
    "longa_arma_dark_blade_roughness.png",
]


RUNTIME_ARMATURE_NAME = "LongaArma_Runtime_Rig"
RUNTIME_BONE_NAMES = [
    "LongaRoot",
    "LongaSpine",
    "LongaChest",
    "LongaHead",
    "LongaBladeArm",
    "LongaBladeArmForearm",
    "LongaBladeArmTip",
    "LongaFrontRightLeg",
    "LongaFrontRightLowerLeg",
    "LongaFrontRightFoot",
    "LongaFrontLeftLeg",
    "LongaFrontLeftLowerLeg",
    "LongaFrontLeftFoot",
    "LongaRearRightLeg",
    "LongaRearRightLowerLeg",
    "LongaRearRightFoot",
    "LongaRearLeftLeg",
    "LongaRearLeftLowerLeg",
    "LongaRearLeftFoot",
]


ANIMATION_SHAPE_KEYS = [
    "Idle_Breath_BodySway",
    "Move_LimpingBladeArm_Drag",
    "Move_Crawl_AlternateStep",
    "Move_FrontRight_LegReach",
    "Move_FrontRight_LegPush",
    "Move_FrontLeft_LegReach",
    "Move_RearRight_LegReach",
    "Move_RearLeft_LegReach",
    "Move_BladeArm_SlowDrag",
    "Attack_LeftBlade_SlamWindup",
    "Attack_FrontLeg_SlamDrag",
    "Attack_UpperBody_Rise",
    "Attack_Forelimbs_ForwardSlam",
    "Attack_GroundDrag_Pullback",
    "Hit_HeadBack_Flinch",
    "Hit_HeadSide_Shake",
    "Consume_HeadBack_Windup",
    "Consume_HeadForward_BiteSlam",
    "Consume_Peck_Impact",
    "Death_Melt_FlatLiquidSpread",
    "Death_Puddle_Final",
]


def ensure_dirs() -> None:
    for path in [BLENDER_OUTPUT.parent, FBX_OUTPUT.parent, GLB_OUTPUT.parent, RENDER_ROOT, TEXTURE_OUTPUT_ROOT]:
        path.mkdir(parents=True, exist_ok=True)


def copy_textures() -> None:
    for texture_name in TEXTURE_FILES:
        source = SOURCE_TEXTURE_ROOT / texture_name
        target = TEXTURE_OUTPUT_ROOT / texture_name
        if source.exists():
            shutil.copy2(source, target)


def require_source_mesh() -> bpy.types.Object:
    if not SOURCE_BLEND.exists():
        raise FileNotFoundError(f"Missing source blend: {SOURCE_BLEND}")

    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not mesh_objects:
        raise RuntimeError("No mesh object found in Longa Arma source blend.")

    mesh_objects.sort(key=lambda obj: len(obj.data.polygons), reverse=True)
    obj = mesh_objects[0]
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    return obj


def clear_scene_except(target: bpy.types.Object) -> None:
    for obj in list(bpy.context.scene.objects):
        if obj != target:
            bpy.data.objects.remove(obj, do_unlink=True)


def remove_shape_keys(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if obj.data.shape_keys is None:
        return

    bpy.ops.object.shape_key_remove(all=True)


def clean_mesh(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    try:
        bpy.ops.mesh.merge_by_distance(distance=0.00008)
    except (AttributeError, TypeError):
        bpy.ops.mesh.remove_doubles(threshold=0.00008)
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")


def center_on_floor(obj: bpy.types.Object) -> None:
    mesh = obj.data
    xs = [vertex.co.x for vertex in mesh.vertices]
    ys = [vertex.co.y for vertex in mesh.vertices]
    zs = [vertex.co.z for vertex in mesh.vertices]
    offset = Vector((
        (min(xs) + max(xs)) * 0.5,
        (min(ys) + max(ys)) * 0.5,
        min(zs),
    ))
    for vertex in mesh.vertices:
        vertex.co -= offset
    mesh.update()
    obj.location = Vector((0.0, 0.0, 0.0))


def clamp01(value: float) -> float:
    return max(0.0, min(1.0, value))


def smoothstep(edge0: float, edge1: float, value: float) -> float:
    if abs(edge1 - edge0) < 0.00001:
        return 1.0 if value >= edge1 else 0.0

    t = clamp01((value - edge0) / (edge1 - edge0))
    return t * t * (3.0 - 2.0 * t)


def normalized(value: float, minimum: float, size: float) -> float:
    return clamp01((value - minimum) / max(size, 0.0001))


def side_sign(value: float) -> float:
    return -1.0 if value < 0.0 else 1.0


def add_animation_shape_keys(obj: bpy.types.Object) -> None:
    """Adds broad single-mesh deformation targets used by Unity review clips."""
    if obj.data.shape_keys is not None:
        remove_shape_keys(obj)

    bounds = mesh_bounds(obj)
    min_vec = bounds["min"]
    size = bounds["size"]
    center = bounds["center"]

    obj.shape_key_add(name="Basis")
    shape_keys = {name: obj.shape_key_add(name=name) for name in ANIMATION_SHAPE_KEYS}

    for index, vertex in enumerate(obj.data.vertices):
        co = vertex.co.copy()
        xn = normalized(co.x, min_vec.x, size.x)
        yn = normalized(co.y, min_vec.y, size.y)
        zn = normalized(co.z, min_vec.z, size.z)

        side = side_sign(co.y)
        upper = smoothstep(0.34, 0.92, zn)
        low = 1.0 - smoothstep(0.20, 0.46, zn)
        foot = 1.0 - smoothstep(0.02, 0.20, zn)
        blade = smoothstep(0.34, 0.02, xn) * (1.0 - smoothstep(0.34, 0.58, zn))
        body = smoothstep(0.18, 0.36, xn) * (1.0 - smoothstep(0.78, 0.96, xn)) * smoothstep(0.18, 0.70, zn)
        torso = body * (1.0 - smoothstep(0.72, 1.0, abs(yn - 0.5) * 2.0))
        head = smoothstep(0.64, 0.94, xn) * smoothstep(0.44, 0.90, zn)
        neck = smoothstep(0.52, 0.78, xn) * smoothstep(0.42, 0.86, zn)
        right_front_leg = low * smoothstep(0.42, 0.72, xn) * smoothstep(0.52, 0.95, yn)
        non_blade_leg = low * (1.0 - blade) * smoothstep(0.26, 0.92, xn)
        front_right_leg = low * smoothstep(0.52, 0.66, xn) * (1.0 - smoothstep(0.82, 0.98, xn)) * smoothstep(0.60, 0.82, yn)
        rear_right_leg = low * smoothstep(0.72, 0.86, xn) * smoothstep(0.60, 0.82, yn)
        front_left_leg = low * smoothstep(0.50, 0.66, xn) * (1.0 - smoothstep(0.78, 0.94, xn)) * (1.0 - smoothstep(0.38, 0.58, yn))
        rear_left_leg = low * smoothstep(0.66, 0.82, xn) * (1.0 - smoothstep(0.38, 0.58, yn))
        forelimb_slam = clamp01(blade + right_front_leg + front_right_leg * 0.75)
        upper_body = clamp01(body + neck + head) * smoothstep(0.34, 0.86, zn)

        from_center_x = co.x - center.x
        from_center_y = co.y - center.y

        shape_keys["Idle_Breath_BodySway"].data[index].co = co + Vector((
            from_center_x * 0.025 * torso,
            from_center_y * 0.085 * torso,
            0.050 * torso - 0.008 * foot,
        ))

        shape_keys["Move_LimpingBladeArm_Drag"].data[index].co = co + Vector((
            0.075 * non_blade_leg * side - 0.030 * blade,
            0.030 * low * side,
            0.060 * non_blade_leg * (1.0 if side > 0.0 else -0.28) - 0.025 * blade,
        ))

        shape_keys["Move_Crawl_AlternateStep"].data[index].co = co + Vector((
            -0.070 * non_blade_leg * side - 0.012 * blade,
            -0.026 * low * side,
            0.055 * non_blade_leg * (1.0 if side < 0.0 else -0.22) - 0.015 * blade,
        ))

        shape_keys["Move_FrontRight_LegReach"].data[index].co = co + Vector((
            -0.115 * front_right_leg,
            0.030 * front_right_leg,
            0.110 * front_right_leg,
        ))

        shape_keys["Move_FrontRight_LegPush"].data[index].co = co + Vector((
            0.090 * front_right_leg,
            -0.020 * front_right_leg,
            -0.050 * front_right_leg,
        ))

        shape_keys["Move_FrontLeft_LegReach"].data[index].co = co + Vector((
            -0.105 * front_left_leg,
            -0.028 * front_left_leg,
            0.095 * front_left_leg,
        ))

        shape_keys["Move_RearRight_LegReach"].data[index].co = co + Vector((
            0.090 * rear_right_leg,
            0.025 * rear_right_leg,
            0.085 * rear_right_leg,
        ))

        shape_keys["Move_RearLeft_LegReach"].data[index].co = co + Vector((
            0.095 * rear_left_leg,
            -0.024 * rear_left_leg,
            0.082 * rear_left_leg,
        ))

        shape_keys["Move_BladeArm_SlowDrag"].data[index].co = co + Vector((
            -0.060 * blade,
            -0.018 * blade,
            -0.045 * blade,
        ))

        shape_keys["Attack_LeftBlade_SlamWindup"].data[index].co = co + Vector((
            -0.055 * blade - 0.020 * right_front_leg,
            0.020 * upper * side,
            0.170 * (blade + right_front_leg) + 0.080 * upper * body,
        ))

        shape_keys["Attack_FrontLeg_SlamDrag"].data[index].co = co + Vector((
            -0.130 * blade - 0.095 * right_front_leg,
            0.018 * (blade + right_front_leg) * side,
            -0.130 * (blade + right_front_leg) - 0.030 * body,
        ))

        shape_keys["Attack_UpperBody_Rise"].data[index].co = co + Vector((
            -0.080 * upper_body - 0.030 * forelimb_slam,
            0.020 * upper_body * side,
            0.220 * upper_body + 0.140 * forelimb_slam,
        ))

        shape_keys["Attack_Forelimbs_ForwardSlam"].data[index].co = co + Vector((
            -0.200 * forelimb_slam - 0.060 * upper_body,
            0.030 * forelimb_slam * side,
            -0.190 * forelimb_slam + 0.060 * upper_body,
        ))

        shape_keys["Attack_GroundDrag_Pullback"].data[index].co = co + Vector((
            0.190 * forelimb_slam - 0.055 * upper_body,
            -0.020 * forelimb_slam * side,
            -0.090 * forelimb_slam - 0.045 * body,
        ))

        shape_keys["Hit_HeadBack_Flinch"].data[index].co = co + Vector((
            -0.100 * head - 0.045 * neck,
            0.018 * upper * side,
            0.070 * head + 0.020 * body,
        ))

        shape_keys["Hit_HeadSide_Shake"].data[index].co = co + Vector((
            -0.025 * head,
            0.135 * head + 0.038 * neck,
            0.014 * upper,
        ))

        shape_keys["Consume_HeadBack_Windup"].data[index].co = co + Vector((
            -0.115 * head - 0.055 * neck,
            0.012 * upper * side,
            0.110 * head + 0.040 * neck,
        ))

        shape_keys["Consume_HeadForward_BiteSlam"].data[index].co = co + Vector((
            0.145 * head + 0.060 * neck,
            -0.012 * head * side,
            -0.155 * head - 0.050 * neck,
        ))

        shape_keys["Consume_Peck_Impact"].data[index].co = co + Vector((
            0.070 * head,
            0.010 * math.sin((co.x + co.y) * 18.0) * head,
            -0.210 * head - 0.035 * body,
        ))

        mid_melt_z = 0.085 + co.z * 0.24
        mid_spread = Vector((
            center.x + from_center_x * 1.22,
            center.y + from_center_y * 1.32,
            mid_melt_z,
        ))
        shape_keys["Death_Melt_FlatLiquidSpread"].data[index].co = co.lerp(mid_spread, smoothstep(0.02, 0.94, zn))

        puddle_noise = 0.010 * math.sin(co.x * 32.0 + co.y * 17.0)
        final_puddle = Vector((
            center.x + from_center_x * 1.72,
            center.y + from_center_y * 1.92,
            0.030 + co.z * 0.055 + puddle_noise,
        ))
        shape_keys["Death_Puddle_Final"].data[index].co = co.lerp(final_puddle, smoothstep(0.00, 0.88, zn))

    obj.data.update()


def create_runtime_armature(obj: bpy.types.Object) -> bpy.types.Object:
    bounds = mesh_bounds(obj)
    min_vec = bounds["min"]
    size = bounds["size"]
    center = bounds["center"]

    armature_data = bpy.data.armatures.new(RUNTIME_ARMATURE_NAME)
    armature_obj = bpy.data.objects.new(RUNTIME_ARMATURE_NAME, armature_data)
    bpy.context.collection.objects.link(armature_obj)
    armature_obj.location = (0.0, 0.0, 0.0)
    armature_data.display_type = "STICK"

    bpy.context.view_layer.objects.active = armature_obj
    armature_obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for edit_bone in list(armature_data.edit_bones):
        armature_data.edit_bones.remove(edit_bone)

    def add_bone(name: str, head: tuple[float, float, float], tail: tuple[float, float, float], parent: bpy.types.EditBone | None = None) -> bpy.types.EditBone:
        bone = armature_data.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        bone.roll = 0.0
        if parent is not None:
            bone.parent = parent
            bone.use_connect = False
        return bone

    root = add_bone("LongaRoot", (center.x, center.y, min_vec.z + size.z * 0.16), (center.x, center.y, min_vec.z + size.z * 0.42))
    spine = add_bone("LongaSpine", (min_vec.x + size.x * 0.22, center.y, min_vec.z + size.z * 0.42), (min_vec.x + size.x * 0.58, center.y, min_vec.z + size.z * 0.54), root)
    chest = add_bone("LongaChest", (min_vec.x + size.x * 0.48, center.y, min_vec.z + size.z * 0.54), (min_vec.x + size.x * 0.78, center.y, min_vec.z + size.z * 0.78), spine)
    add_bone("LongaHead", (min_vec.x + size.x * 0.70, center.y, min_vec.z + size.z * 0.72), (min_vec.x + size.x * 0.98, center.y, min_vec.z + size.z * 0.78), chest)
    blade_arm = add_bone("LongaBladeArm", (min_vec.x + size.x * 0.32, min_vec.y + size.y * 0.44, min_vec.z + size.z * 0.38), (min_vec.x + size.x * 0.23, min_vec.y + size.y * 0.38, min_vec.z + size.z * 0.25), chest)
    blade_forearm = add_bone("LongaBladeArmForearm", (min_vec.x + size.x * 0.23, min_vec.y + size.y * 0.38, min_vec.z + size.z * 0.25), (min_vec.x + size.x * 0.13, min_vec.y + size.y * 0.32, min_vec.z + size.z * 0.13), blade_arm)
    add_bone("LongaBladeArmTip", (min_vec.x + size.x * 0.13, min_vec.y + size.y * 0.32, min_vec.z + size.z * 0.13), (min_vec.x + size.x * 0.02, min_vec.y + size.y * 0.28, min_vec.z + size.z * 0.04), blade_forearm)

    front_right_leg = add_bone("LongaFrontRightLeg", (min_vec.x + size.x * 0.56, min_vec.y + size.y * 0.78, min_vec.z + size.z * 0.45), (min_vec.x + size.x * 0.58, min_vec.y + size.y * 0.82, min_vec.z + size.z * 0.31), chest)
    front_right_lower = add_bone("LongaFrontRightLowerLeg", (min_vec.x + size.x * 0.58, min_vec.y + size.y * 0.82, min_vec.z + size.z * 0.31), (min_vec.x + size.x * 0.60, min_vec.y + size.y * 0.86, min_vec.z + size.z * 0.15), front_right_leg)
    add_bone("LongaFrontRightFoot", (min_vec.x + size.x * 0.60, min_vec.y + size.y * 0.86, min_vec.z + size.z * 0.15), (min_vec.x + size.x * 0.66, min_vec.y + size.y * 0.90, min_vec.z + size.z * 0.04), front_right_lower)

    front_left_leg = add_bone("LongaFrontLeftLeg", (min_vec.x + size.x * 0.56, min_vec.y + size.y * 0.22, min_vec.z + size.z * 0.45), (min_vec.x + size.x * 0.54, min_vec.y + size.y * 0.18, min_vec.z + size.z * 0.31), chest)
    front_left_lower = add_bone("LongaFrontLeftLowerLeg", (min_vec.x + size.x * 0.54, min_vec.y + size.y * 0.18, min_vec.z + size.z * 0.31), (min_vec.x + size.x * 0.52, min_vec.y + size.y * 0.14, min_vec.z + size.z * 0.15), front_left_leg)
    add_bone("LongaFrontLeftFoot", (min_vec.x + size.x * 0.52, min_vec.y + size.y * 0.14, min_vec.z + size.z * 0.15), (min_vec.x + size.x * 0.48, min_vec.y + size.y * 0.10, min_vec.z + size.z * 0.04), front_left_lower)

    rear_right_leg = add_bone("LongaRearRightLeg", (min_vec.x + size.x * 0.78, min_vec.y + size.y * 0.76, min_vec.z + size.z * 0.42), (min_vec.x + size.x * 0.82, min_vec.y + size.y * 0.81, min_vec.z + size.z * 0.29), spine)
    rear_right_lower = add_bone("LongaRearRightLowerLeg", (min_vec.x + size.x * 0.82, min_vec.y + size.y * 0.81, min_vec.z + size.z * 0.29), (min_vec.x + size.x * 0.85, min_vec.y + size.y * 0.86, min_vec.z + size.z * 0.14), rear_right_leg)
    add_bone("LongaRearRightFoot", (min_vec.x + size.x * 0.85, min_vec.y + size.y * 0.86, min_vec.z + size.z * 0.14), (min_vec.x + size.x * 0.91, min_vec.y + size.y * 0.90, min_vec.z + size.z * 0.04), rear_right_lower)

    rear_left_leg = add_bone("LongaRearLeftLeg", (min_vec.x + size.x * 0.78, min_vec.y + size.y * 0.24, min_vec.z + size.z * 0.42), (min_vec.x + size.x * 0.81, min_vec.y + size.y * 0.19, min_vec.z + size.z * 0.29), spine)
    rear_left_lower = add_bone("LongaRearLeftLowerLeg", (min_vec.x + size.x * 0.81, min_vec.y + size.y * 0.19, min_vec.z + size.z * 0.29), (min_vec.x + size.x * 0.84, min_vec.y + size.y * 0.14, min_vec.z + size.z * 0.14), rear_left_leg)
    add_bone("LongaRearLeftFoot", (min_vec.x + size.x * 0.84, min_vec.y + size.y * 0.14, min_vec.z + size.z * 0.14), (min_vec.x + size.x * 0.90, min_vec.y + size.y * 0.10, min_vec.z + size.z * 0.04), rear_left_lower)

    bpy.ops.object.mode_set(mode="OBJECT")

    obj.vertex_groups.clear()
    groups = {bone_name: obj.vertex_groups.new(name=bone_name) for bone_name in RUNTIME_BONE_NAMES}

    for vertex in obj.data.vertices:
        co = vertex.co
        xn = normalized(co.x, min_vec.x, size.x)
        yn = normalized(co.y, min_vec.y, size.y)
        zn = normalized(co.z, min_vec.z, size.z)

        low = 1.0 - smoothstep(0.20, 0.48, zn)
        blade = smoothstep(0.36, 0.04, xn) * (1.0 - smoothstep(0.34, 0.62, zn))
        head = smoothstep(0.68, 0.94, xn) * smoothstep(0.45, 0.88, zn)
        chest_weight = smoothstep(0.42, 0.78, xn) * smoothstep(0.34, 0.92, zn)
        front_right = low * smoothstep(0.50, 0.66, xn) * (1.0 - smoothstep(0.82, 0.98, xn)) * smoothstep(0.60, 0.82, yn)
        front_left = low * smoothstep(0.48, 0.66, xn) * (1.0 - smoothstep(0.78, 0.96, xn)) * (1.0 - smoothstep(0.38, 0.58, yn))
        rear_right = low * smoothstep(0.72, 0.88, xn) * smoothstep(0.58, 0.82, yn)
        rear_left = low * smoothstep(0.68, 0.88, xn) * (1.0 - smoothstep(0.40, 0.60, yn))
        foot_factor = 1.0 - smoothstep(0.08, 0.20, zn)
        lower_factor = smoothstep(0.10, 0.24, zn) * (1.0 - smoothstep(0.26, 0.44, zn))
        upper_factor = max(0.0, 1.0 - foot_factor - lower_factor * 0.80)
        arm_tip_factor = 1.0 - smoothstep(0.08, 0.19, zn)
        arm_forearm_factor = smoothstep(0.10, 0.25, zn) * (1.0 - smoothstep(0.25, 0.42, zn))
        arm_upper_factor = max(0.0, 1.0 - arm_tip_factor - arm_forearm_factor * 0.80)

        weights = {
            "LongaBladeArm": blade * arm_upper_factor,
            "LongaBladeArmForearm": blade * arm_forearm_factor,
            "LongaBladeArmTip": blade * arm_tip_factor,
            "LongaFrontRightLeg": front_right * upper_factor,
            "LongaFrontRightLowerLeg": front_right * lower_factor,
            "LongaFrontRightFoot": front_right * foot_factor,
            "LongaFrontLeftLeg": front_left * upper_factor,
            "LongaFrontLeftLowerLeg": front_left * lower_factor,
            "LongaFrontLeftFoot": front_left * foot_factor,
            "LongaRearRightLeg": rear_right * upper_factor,
            "LongaRearRightLowerLeg": rear_right * lower_factor,
            "LongaRearRightFoot": rear_right * foot_factor,
            "LongaRearLeftLeg": rear_left * upper_factor,
            "LongaRearLeftLowerLeg": rear_left * lower_factor,
            "LongaRearLeftFoot": rear_left * foot_factor,
            "LongaHead": head,
            "LongaChest": chest_weight * (1.0 - max(front_right, front_left, blade, head) * 0.65),
            "LongaSpine": 0.45 * (1.0 - max(blade, front_right, front_left, rear_right, rear_left, head)),
            "LongaRoot": 0.08,
        }

        strongest_weights = sorted(
            ((group_name, max(0.0, weight)) for group_name, weight in weights.items()),
            key=lambda item: item[1],
            reverse=True,
        )[:4]

        total = sum(weight for _group_name, weight in strongest_weights)
        if total <= 0.0001:
            groups["LongaSpine"].add([vertex.index], 1.0, "ADD")
            continue

        for group_name, weight in strongest_weights:
            normalized_weight = weight / total
            if normalized_weight > 0.001:
                groups[group_name].add([vertex.index], normalized_weight, "ADD")

    armature_modifier = obj.modifiers.new("RuntimeRig_Armature", "ARMATURE")
    armature_modifier.object = armature_obj
    obj.parent = armature_obj
    obj.matrix_parent_inverse = armature_obj.matrix_world.inverted()
    obj.data.update()
    return armature_obj


def decimate_mesh(obj: bpy.types.Object, source_face_count: int) -> None:
    ratio = min(1.0, max(0.015, TARGET_TRIANGLES / max(1, source_face_count)))
    decimate = obj.modifiers.new("Runtime_LowPoly_Decimate", "DECIMATE")
    decimate.ratio = ratio
    decimate.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=decimate.name)

    triangulate = obj.modifiers.new("Runtime_LowPoly_Triangulate", "TRIANGULATE")
    bpy.ops.object.modifier_apply(modifier=triangulate.name)

    weighted_normal = obj.modifiers.new("Runtime_LowPoly_WeightedNormals", "WEIGHTED_NORMAL")
    weighted_normal.keep_sharp = True
    try:
        weighted_normal.weight = 50
    except AttributeError:
        pass
    bpy.ops.object.modifier_apply(modifier=weighted_normal.name)
    bpy.ops.object.shade_smooth()


def load_image(texture_name: str, non_color: bool = False) -> bpy.types.Image | None:
    path = TEXTURE_OUTPUT_ROOT / texture_name
    if not path.exists():
        return None

    image = bpy.data.images.load(str(path), check_existing=True)
    if non_color:
        image.colorspace_settings.name = "Non-Color"
    return image


def create_textured_material(
    name: str,
    albedo_texture: str,
    roughness_texture: str | None,
    normal_texture: str | None,
    fallback_color: tuple[float, float, float, float],
    metallic: float,
    roughness: float,
) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    principled = nodes.get("Principled BSDF")
    if principled is None:
        return material

    if "Base Color" in principled.inputs:
        principled.inputs["Base Color"].default_value = fallback_color
    if "Metallic" in principled.inputs:
        principled.inputs["Metallic"].default_value = metallic
    if "Roughness" in principled.inputs:
        principled.inputs["Roughness"].default_value = roughness

    albedo = load_image(albedo_texture)
    if albedo is not None and "Base Color" in principled.inputs:
        albedo_node = nodes.new("ShaderNodeTexImage")
        albedo_node.image = albedo
        links.new(albedo_node.outputs["Color"], principled.inputs["Base Color"])

    roughness_image = load_image(roughness_texture, non_color=True) if roughness_texture else None
    if roughness_image is not None and "Roughness" in principled.inputs:
        roughness_node = nodes.new("ShaderNodeTexImage")
        roughness_node.image = roughness_image
        links.new(roughness_node.outputs["Color"], principled.inputs["Roughness"])

    normal_image = load_image(normal_texture, non_color=True) if normal_texture else None
    if normal_image is not None and "Normal" in principled.inputs:
        normal_node = nodes.new("ShaderNodeTexImage")
        normal_node.image = normal_image
        normal_map_node = nodes.new("ShaderNodeNormalMap")
        try:
            normal_map_node.inputs["Strength"].default_value = 0.55
        except KeyError:
            pass
        links.new(normal_node.outputs["Color"], normal_map_node.inputs["Color"])
        links.new(normal_map_node.outputs["Normal"], principled.inputs["Normal"])

    return material


def create_preview_material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    if principled is not None and "Base Color" in principled.inputs:
        principled.inputs["Base Color"].default_value = color
    if principled is not None and "Roughness" in principled.inputs:
        principled.inputs["Roughness"].default_value = 0.72
    return material


def assign_materials(obj: bpy.types.Object) -> None:
    body = create_textured_material(
        "M_LongaArma_RuntimeLowPoly_WetBody",
        "longa_arma_wet_green_albedo.png",
        "longa_arma_wet_green_roughness.png",
        "longa_arma_wet_green_bump.png",
        (0.23, 0.43, 0.36, 1.0),
        metallic=0.0,
        roughness=0.78,
    )
    blade = create_textured_material(
        "M_LongaArma_RuntimeLowPoly_DarkBlade",
        "longa_arma_dark_blade_albedo.png",
        "longa_arma_dark_blade_roughness.png",
        None,
        (0.055, 0.058, 0.055, 1.0),
        metallic=0.12,
        roughness=0.46,
    )

    obj.data.materials.clear()
    obj.data.materials.append(body)
    obj.data.materials.append(blade)

    bounds = mesh_bounds(obj)
    min_x, max_x = bounds["min"].x, bounds["max"].x
    min_z, max_z = bounds["min"].z, bounds["max"].z
    size_x = max(0.0001, max_x - min_x)
    size_z = max(0.0001, max_z - min_z)

    vertices = obj.data.vertices
    for polygon in obj.data.polygons:
        center = sum((vertices[index].co for index in polygon.vertices), Vector()) / len(polygon.vertices)
        x_normalized = (center.x - min_x) / size_x
        z_normalized = (center.z - min_z) / size_z
        polygon.material_index = 1 if x_normalized < 0.30 and z_normalized < 0.64 else 0


def mesh_bounds(obj: bpy.types.Object) -> dict[str, Vector]:
    coords = [vertex.co.copy() for vertex in obj.data.vertices]
    min_vec = Vector((min(v.x for v in coords), min(v.y for v in coords), min(v.z for v in coords)))
    max_vec = Vector((max(v.x for v in coords), max(v.y for v in coords), max(v.z for v in coords)))
    return {"min": min_vec, "max": max_vec, "size": max_vec - min_vec, "center": (min_vec + max_vec) * 0.5}


def configure_render_scene() -> tuple[bpy.types.Camera, bpy.types.Light, bpy.types.Light]:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = RENDER_WIDTH
    scene.render.resolution_y = RENDER_HEIGHT
    scene.eevee.taa_render_samples = 64
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0.55
    scene.view_settings.gamma = 1.0
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.color = (0.055, 0.062, 0.058)

    camera_data = bpy.data.cameras.new("LongaArma_RuntimeLowPoly_Camera")
    camera_data.type = "ORTHO"
    camera = bpy.data.objects.new("LongaArma_RuntimeLowPoly_Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    scene.camera = camera

    key_data = bpy.data.lights.new("LongaArma_RuntimeLowPoly_Key", "AREA")
    key_data.energy = 900
    key_data.size = 4.0
    key = bpy.data.objects.new("LongaArma_RuntimeLowPoly_Key", key_data)
    bpy.context.collection.objects.link(key)
    key.location = (-3.0, -4.0, 4.0)

    fill_data = bpy.data.lights.new("LongaArma_RuntimeLowPoly_Fill", "POINT")
    fill_data.energy = 180
    fill = bpy.data.objects.new("LongaArma_RuntimeLowPoly_Fill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = (3.0, 4.0, 2.6)

    return camera, key, fill


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def set_camera_view(camera: bpy.types.Object, bounds: dict[str, Vector], direction: Vector, vertical_bias: float = 0.04) -> None:
    size = bounds["size"]
    center = bounds["center"] + Vector((0.0, 0.0, size.z * vertical_bias))
    distance = max(size.x, size.y, size.z) * 3.0
    camera.location = center + direction.normalized() * distance
    look_at(camera, center)
    aspect = RENDER_WIDTH / RENDER_HEIGHT
    if abs(direction.x) > 0.5:
        visible_width = size.y
    elif abs(direction.y) > 0.5:
        visible_width = size.x
    else:
        visible_width = max(size.x, size.y)
    camera.data.ortho_scale = max(max(size.x, size.y, size.z) * 1.24, size.z * 1.42, visible_width / aspect * 1.48)


def render_view(camera: bpy.types.Object, bounds: dict[str, Vector], name: str, direction: Vector) -> None:
    set_camera_view(camera, bounds, direction)
    bpy.context.scene.render.filepath = str(RENDER_ROOT / f"{name}.png")
    bpy.ops.render.render(write_still=True)


def render_wireframe(obj: bpy.types.Object, camera: bpy.types.Object, bounds: dict[str, Vector]) -> None:
    wire_obj = obj.copy()
    wire_obj.data = obj.data.copy()
    wire_obj.name = "LongaArma_RuntimeLowPoly_WireframePreview"
    bpy.context.collection.objects.link(wire_obj)
    wire_obj.data.materials.clear()
    wire_obj.data.materials.append(create_preview_material("M_LongaArma_RuntimeLowPoly_Wire", (0.86, 0.92, 0.88, 1.0)))
    for polygon in wire_obj.data.polygons:
        polygon.material_index = 0

    wire_modifier = wire_obj.modifiers.new("Wireframe_Density_Preview", "WIREFRAME")
    wire_modifier.thickness = 0.003
    wire_modifier.use_replace = True

    obj.hide_render = True
    set_camera_view(camera, bounds, Vector((-1.0, -0.65, 0.18)))
    bpy.context.scene.render.filepath = str(RENDER_ROOT / "wireframe_density.png")
    bpy.ops.render.render(write_still=True)
    obj.hide_render = False
    bpy.data.objects.remove(wire_obj, do_unlink=True)


def select_export_objects(obj: bpy.types.Object, armature_obj: bpy.types.Object | None) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    if armature_obj is not None:
        armature_obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def export_assets(obj: bpy.types.Object, armature_obj: bpy.types.Object | None) -> None:
    select_export_objects(obj, armature_obj)
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_OUTPUT),
        use_selection=True,
        object_types={"MESH", "ARMATURE"},
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=False,
        add_leaf_bones=False,
    )
    select_export_objects(obj, armature_obj)
    bpy.ops.export_scene.gltf(
        filepath=str(GLB_OUTPUT),
        export_format="GLB",
        use_selection=True,
        export_materials="EXPORT",
    )
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_OUTPUT))


def write_docs(stats: dict[str, object]) -> None:
    manifest = {
        "enemyId": "longa_arma",
        "sampleId": "runtime_lowpoly",
        "createdAt": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "status": "pending_user_review",
        "sourceModel": "Assets/_Project/Art/Enemies/LongaArma/Models/longa_arma.blend",
        "sourceModelUntouched": True,
        "goal": "Runtime low-poly art sample that keeps the approved Longa Arma silhouette while lowering mesh cost for multiple in-ship instances.",
        "meshStats": stats,
        "materials": [
            "M_LongaArma_RuntimeLowPoly_WetBody",
            "M_LongaArma_RuntimeLowPoly_DarkBlade",
        ],
        "files": [
            "README.md",
            "asset_manifest.json",
            "polycount_stats.json",
            "build_runtime_lowpoly_sample.py",
            "blender/longa_arma_runtime_lowpoly.blend",
            "exports/longa_arma_runtime_lowpoly.fbx",
            "exports/longa_arma_runtime_lowpoly.glb",
            "renders/front.png",
            "renders/side.png",
            "renders/back.png",
            "renders/three_quarter.png",
            "renders/wireframe_density.png",
        ] + [f"textures/{name}" for name in TEXTURE_FILES if (TEXTURE_OUTPUT_ROOT / name).exists()],
    }
    MANIFEST_OUTPUT.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    STATS_OUTPUT.write_text(json.dumps(stats, ensure_ascii=False, indent=2), encoding="utf-8")

    README_OUTPUT.write_text(
        "# Longa Arma 런타임 저폴리 샘플\n\n"
        f"- 생성 시각: {manifest['createdAt']}\n"
        "- 목적: 기존 Longa Arma 고밀도 모델의 말형 머리, 네 다리 짐승 실루엣, 왼팔 칼날, 젖은 녹청색 몸체, 어두운 칼날 인상을 유지하면서 런타임 다수 배치에 맞게 메시 밀도를 낮춘 검토용 샘플입니다.\n"
        "- 원본 외부 모델 `enemies model/longa arma.blend`는 수정하지 않았습니다.\n"
        "- Unity 런타임 씬, 프리팹, AI, 히트박스에는 아직 연결하지 않았습니다.\n\n"
        "## 폴리곤 수\n\n"
        f"- 원본 Unity 적용 사본: {stats['sourceVertices']:,} vertices / {stats['sourcePolygons']:,} polygons\n"
        f"- 저폴리 샘플: {stats['lowVertices']:,} vertices / {stats['lowTriangles']:,} triangles\n"
        f"- 삼각형 감소율: {stats['triangleReductionPercent']:.1f}%\n\n"
        "## 검토 파일\n\n"
        "- 정면 렌더: `renders/front.png`\n"
        "- 측면 렌더: `renders/side.png`\n"
        "- 후면 렌더: `renders/back.png`\n"
        "- 3분기 렌더: `renders/three_quarter.png`\n"
        "- 와이어프레임 밀도 렌더: `renders/wireframe_density.png`\n"
        "- Blender 파일: `blender/longa_arma_runtime_lowpoly.blend`\n"
        "- 내보내기 파일: `exports/longa_arma_runtime_lowpoly.fbx`, `exports/longa_arma_runtime_lowpoly.glb`\n"
        "- 생성 스크립트: `build_runtime_lowpoly_sample.py`\n\n"
        "## 애니메이션 변형 타깃\n\n"
        "- 단일 표시 메쉬에 런타임 검토용 Armature와 Shape Key를 포함했습니다.\n"
        "- 이동/공격은 짐승형 동작에 맞게 Armature 본을 기준으로 구동합니다.\n"
        "- 포함된 본: `" + "`, `".join(RUNTIME_BONE_NAMES) + "`\n"
        "- 포함된 Shape Key: `" + "`, `".join(ANIMATION_SHAPE_KEYS) + "`\n"
        "- Shape Key는 대기 호흡, 피격/섭취 보조 변형, 액체화 같은 비관절 변형에 사용합니다.\n\n"
        "## 주의\n\n"
        "- 이 샘플은 런타임 최적화용 외형 검토 샘플입니다.\n"
        "- 다리 기어감과 공격 내리찍기는 저폴리 단일 SkinnedMesh의 본 애니메이션으로 처리합니다. 실제 전투 이동, 피격 판정, 섭취 판정은 Unity 게임플레이 로직에서 별도로 연결해야 합니다.\n",
        encoding="utf-8",
    )


def main() -> None:
    ensure_dirs()
    copy_textures()
    obj = require_source_mesh()
    clear_scene_except(obj)
    obj.name = "LongaArma_Runtime_LowPoly"
    obj.data.name = "LongaArma_Runtime_LowPoly_Mesh"
    source_vertices = len(obj.data.vertices)
    source_polygons = len(obj.data.polygons)

    remove_shape_keys(obj)
    clean_mesh(obj)
    decimate_mesh(obj, source_polygons)
    clean_mesh(obj)
    center_on_floor(obj)
    assign_materials(obj)
    add_animation_shape_keys(obj)
    armature_obj = create_runtime_armature(obj)

    bounds = mesh_bounds(obj)
    low_vertices = len(obj.data.vertices)
    low_triangles = len(obj.data.polygons)
    stats = {
        "sourceVertices": source_vertices,
        "sourcePolygons": source_polygons,
        "lowVertices": low_vertices,
        "lowTriangles": low_triangles,
        "triangleReductionPercent": round((1.0 - (low_triangles / max(1, source_polygons))) * 100.0, 2),
        "dimensionsBlenderUnits": {
            "xLength": round(bounds["size"].x, 4),
            "yWidth": round(bounds["size"].y, 4),
            "zHeight": round(bounds["size"].z, 4),
        },
        "visibleMeshObjectCount": 1,
        "shapeKeysRemovedForRuntimeSample": False,
        "runtimeAnimationShapeKeys": ANIMATION_SHAPE_KEYS,
        "runtimeArmatureName": RUNTIME_ARMATURE_NAME,
        "runtimeArmatureBoneNames": RUNTIME_BONE_NAMES,
        "animationSystem": "single visible skinned mesh with runtime armature bones; Shape Keys remain for breathing/liquid deformation support",
    }

    camera, _key, _fill = configure_render_scene()
    render_view(camera, bounds, "front", Vector((-1.0, 0.0, 0.06)))
    render_view(camera, bounds, "side", Vector((0.0, -1.0, 0.06)))
    render_view(camera, bounds, "back", Vector((1.0, 0.0, 0.06)))
    render_view(camera, bounds, "three_quarter", Vector((-1.0, -0.65, 0.10)))
    render_wireframe(obj, camera, bounds)
    export_assets(obj, armature_obj)
    write_docs(stats)
    print("LONGA_RUNTIME_LOWPOLY_SAMPLE_CREATED")
    print(json.dumps(stats, ensure_ascii=False))


if __name__ == "__main__":
    main()
