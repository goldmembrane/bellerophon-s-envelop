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


ANIMATION_ACTION_SPECS = [
    {
        "name": "LongaArma_Static_Review",
        "frames": 30,
        "loop": False,
        "description": "Reference standing pose for side-by-side review.",
    },
    {
        "name": "LongaArma_Idle",
        "frames": 48,
        "loop": True,
        "description": "Body morph breathing with light chest and head sway.",
    },
    {
        "name": "LongaArma_Move_LimpingBladeArm",
        "frames": 48,
        "loop": True,
        "description": "Four separate beast crawl leg cycles with a slower dragging blade arm.",
    },
    {
        "name": "LongaArma_Attack_Slam",
        "frames": 62,
        "loop": True,
        "description": "Raise upper body, lift left blade arm and right foreleg, slam forward, then drag back along the floor.",
    },
    {
        "name": "LongaArma_Hit_Recoil",
        "frames": 32,
        "loop": True,
        "description": "Head side shake with a backward body recoil.",
    },
    {
        "name": "LongaArma_Consume_BiteSlam",
        "frames": 46,
        "loop": True,
        "description": "Head winds back, snaps forward, and pecks downward at the target.",
    },
    {
        "name": "LongaArma_Death_Melt",
        "frames": 66,
        "loop": True,
        "description": "Whole body melts into a flat liquid puddle.",
    },
]


ACTION_PREVIEW_SAMPLES = {
    "LongaArma_Idle": 24,
    "LongaArma_Move_LimpingBladeArm": 16,
    "LongaArma_Attack_Slam": 34,
    "LongaArma_Hit_Recoil": 8,
    "LongaArma_Consume_BiteSlam": 22,
    "LongaArma_Death_Melt": 66,
}


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

        puddle_noise = 0.003 * math.sin(co.x * 32.0 + co.y * 17.0)
        final_puddle = Vector((
            center.x + from_center_x * 0.28 + 0.018 * math.sin(co.y * 11.0),
            center.y + from_center_y * 0.40 + 0.022 * math.cos(co.x * 9.0),
            0.014 + co.z * 0.018 + puddle_noise,
        ))
        shape_keys["Death_Puddle_Final"].data[index].co = final_puddle

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


def shape_property_name(shape_name: str) -> str:
    return f"arp_shape_{shape_name}"


def ensure_shape_key_drivers(obj: bpy.types.Object, armature_obj: bpy.types.Object) -> None:
    if obj.data.shape_keys is None:
        raise RuntimeError("Longa Arma runtime mesh must have Shape Keys before creating animation drivers.")

    key_blocks = obj.data.shape_keys.key_blocks
    for shape_name in ANIMATION_SHAPE_KEYS:
        if shape_name not in key_blocks:
            raise RuntimeError(f"Missing Shape Key for animation driver: {shape_name}")

        prop_name = shape_property_name(shape_name)
        armature_obj[prop_name] = 0.0
        try:
            armature_obj.id_properties_ui(prop_name).update(min=0.0, max=1.0, soft_min=0.0, soft_max=1.0)
        except Exception:
            pass

        shape_key = key_blocks[shape_name]
        shape_key.value = 0.0
        try:
            shape_key.driver_remove("value")
        except (TypeError, RuntimeError):
            pass

        driver = shape_key.driver_add("value").driver
        driver.type = "SCRIPTED"
        driver.expression = "value"
        variable = driver.variables.new()
        variable.name = "value"
        variable.type = "SINGLE_PROP"
        target = variable.targets[0]
        target.id = armature_obj
        target.data_path = f'["{prop_name}"]'


def clear_longa_actions() -> None:
    action_names = {spec["name"] for spec in ANIMATION_ACTION_SPECS}
    for action in list(bpy.data.actions):
        if action.name in action_names or action.name.startswith("LongaArma_"):
            bpy.data.actions.remove(action)


def reset_animation_state(armature_obj: bpy.types.Object) -> None:
    for pose_bone in armature_obj.pose.bones:
        pose_bone.rotation_mode = "XYZ"
        pose_bone.location = (0.0, 0.0, 0.0)
        pose_bone.rotation_euler = (0.0, 0.0, 0.0)
        pose_bone.scale = (1.0, 1.0, 1.0)

    for shape_name in ANIMATION_SHAPE_KEYS:
        armature_obj[shape_property_name(shape_name)] = 0.0


def pose_entry(
    loc: tuple[float, float, float] | None = None,
    rot: tuple[float, float, float] | None = None,
    scale: tuple[float, float, float] | None = None,
) -> dict[str, tuple[float, float, float]]:
    data: dict[str, tuple[float, float, float]] = {}
    if loc is not None:
        data["loc"] = loc
    if rot is not None:
        data["rot"] = rot
    if scale is not None:
        data["scale"] = scale
    return data


def add_pose(
    pose: dict[str, dict[str, tuple[float, float, float]]],
    bone_name: str,
    loc: tuple[float, float, float] | None = None,
    rot: tuple[float, float, float] | None = None,
    scale: tuple[float, float, float] | None = None,
) -> None:
    pose[bone_name] = pose_entry(loc=loc, rot=rot, scale=scale)


def insert_action_key(
    armature_obj: bpy.types.Object,
    frame: int,
    bone_pose: dict[str, dict[str, tuple[float, float, float]]] | None = None,
    shape_values: dict[str, float] | None = None,
) -> None:
    bpy.context.scene.frame_set(frame)
    reset_animation_state(armature_obj)

    for bone_name, values in (bone_pose or {}).items():
        pose_bone = armature_obj.pose.bones.get(bone_name)
        if pose_bone is None:
            raise RuntimeError(f"Cannot key missing Longa Arma bone: {bone_name}")

        if "loc" in values:
            pose_bone.location = values["loc"]
        if "rot" in values:
            pose_bone.rotation_euler = values["rot"]
        if "scale" in values:
            pose_bone.scale = values["scale"]

    for shape_name, value in (shape_values or {}).items():
        if shape_name not in ANIMATION_SHAPE_KEYS:
            raise RuntimeError(f"Cannot key unknown Longa Arma Shape Key driver: {shape_name}")
        armature_obj[shape_property_name(shape_name)] = clamp01(value)

    bpy.context.view_layer.update()

    for pose_bone in armature_obj.pose.bones:
        pose_bone.keyframe_insert(data_path="location", frame=frame)
        pose_bone.keyframe_insert(data_path="rotation_euler", frame=frame)
        pose_bone.keyframe_insert(data_path="scale", frame=frame)

    for shape_name in ANIMATION_SHAPE_KEYS:
        armature_obj.keyframe_insert(data_path=f'["{shape_property_name(shape_name)}"]', frame=frame)


def prepare_action(armature_obj: bpy.types.Object, action_name: str, frame_end: int, loop: bool) -> bpy.types.Action:
    armature_obj.animation_data_create()
    action = bpy.data.actions.new(action_name)
    action.use_fake_user = True
    action["arp_export"] = True
    action["longa_arma_loop"] = loop
    action["longa_arma_frame_start"] = 0
    action["longa_arma_frame_end"] = frame_end
    armature_obj.animation_data.action = action
    bpy.context.scene.frame_start = 0
    bpy.context.scene.frame_end = frame_end
    return action


def smooth01(value: float) -> float:
    t = clamp01(value)
    return t * t * (3.0 - 2.0 * t)


def lerp_float(start: float, end: float, alpha: float) -> float:
    return start + (end - start) * alpha


def leg_cycle(progress: float, stride_x: float, lift_z: float, lateral_y: float, support_drop_z: float) -> tuple[
    tuple[float, float, float],
    tuple[float, float, float],
    tuple[float, float, float],
    tuple[float, float, float],
    tuple[float, float, float],
    tuple[float, float, float],
]:
    if progress < 0.36:
        swing = smooth01(progress / 0.36)
        foot_x = lerp_float(-0.60 * stride_x, 0.78 * stride_x, swing)
        foot_z = math.sin(swing * math.pi) * lift_z
        foot_y = math.sin(swing * math.pi) * lateral_y
        upper_rot = math.radians(8.0 + 12.0 * math.sin(swing * math.pi))
        lower_rot = math.radians(-12.0 - 16.0 * math.sin(swing * math.pi))
        foot_rot = math.radians(7.0 * math.sin(swing * math.pi))
    else:
        stance = smooth01((progress - 0.36) / 0.64)
        foot_x = lerp_float(0.78 * stride_x, -0.60 * stride_x, stance)
        foot_z = -support_drop_z * math.sin(stance * math.pi)
        foot_y = 0.20 * lateral_y * math.sin(stance * math.pi)
        upper_rot = math.radians(-5.0 + 7.0 * math.sin(stance * math.pi))
        lower_rot = math.radians(5.0 - 5.0 * math.sin(stance * math.pi))
        foot_rot = math.radians(-3.0 * math.sin(stance * math.pi))

    upper_loc = (foot_x * 0.18, foot_y * 0.22, foot_z * 0.24)
    lower_loc = (foot_x * 0.52, foot_y * 0.56, foot_z * 0.58)
    foot_loc = (foot_x, foot_y, foot_z)
    upper_euler = (0.0, upper_rot, 0.0)
    lower_euler = (0.0, lower_rot, 0.0)
    foot_euler = (foot_rot, 0.0, 0.0)
    return upper_loc, lower_loc, foot_loc, upper_euler, lower_euler, foot_euler


def add_leg_cycle_pose(
    pose: dict[str, dict[str, tuple[float, float, float]]],
    bone_names: tuple[str, str, str],
    progress: float,
    stride_x: float,
    lift_z: float,
    lateral_y: float,
    support_drop_z: float,
) -> None:
    upper_loc, lower_loc, foot_loc, upper_euler, lower_euler, foot_euler = leg_cycle(
        progress,
        stride_x,
        lift_z,
        lateral_y,
        support_drop_z,
    )
    add_pose(pose, bone_names[0], loc=upper_loc, rot=upper_euler)
    add_pose(pose, bone_names[1], loc=lower_loc, rot=lower_euler)
    add_pose(pose, bone_names[2], loc=foot_loc, rot=foot_euler)


def add_blade_drag_pose(
    pose: dict[str, dict[str, tuple[float, float, float]]],
    progress: float,
) -> None:
    if progress < 0.42:
        swing = smooth01(progress / 0.42)
        tip_x = lerp_float(0.055, -0.105, swing)
        tip_z = math.sin(swing * math.pi) * 0.060 - 0.010
    else:
        drag = smooth01((progress - 0.42) / 0.58)
        tip_x = lerp_float(-0.105, 0.105, drag)
        tip_z = -0.052 - 0.016 * math.sin(drag * math.pi)

    side_y = -0.038 * math.sin(progress * math.pi)
    add_pose(pose, "LongaBladeArm", loc=(tip_x * 0.25, side_y * 0.42, tip_z * 0.32), rot=(math.radians(-4.0), math.radians(5.0), 0.0))
    add_pose(pose, "LongaBladeArmForearm", loc=(tip_x * 0.56, side_y * 0.70, tip_z * 0.72), rot=(math.radians(-7.0), math.radians(10.0), 0.0))
    add_pose(pose, "LongaBladeArmTip", loc=(tip_x, side_y, tip_z), rot=(math.radians(-10.0), math.radians(14.0), 0.0))


def build_static_action(armature_obj: bpy.types.Object) -> bpy.types.Action:
    action = prepare_action(armature_obj, "LongaArma_Static_Review", 30, False)
    insert_action_key(armature_obj, 0)
    insert_action_key(armature_obj, 30)
    return action


def build_idle_action(armature_obj: bpy.types.Object) -> bpy.types.Action:
    action = prepare_action(armature_obj, "LongaArma_Idle", 48, True)
    for frame, breath in [(0, 0.0), (12, 0.72), (24, 1.0), (36, 0.45), (48, 0.0)]:
        pose = {
            "LongaRoot": pose_entry(loc=(0.0, 0.0, 0.010 * breath)),
            "LongaSpine": pose_entry(loc=(-0.010 * breath, 0.000, 0.016 * breath), rot=(0.0, math.radians(-1.5 * breath), 0.0)),
            "LongaChest": pose_entry(loc=(-0.018 * breath, 0.004 * math.sin(frame / 48.0 * math.pi * 2.0), 0.028 * breath), rot=(math.radians(0.8 * breath), math.radians(-2.2 * breath), math.radians(0.8 * math.sin(frame / 48.0 * math.pi * 2.0)))),
            "LongaHead": pose_entry(loc=(-0.012 * breath, 0.000, 0.014 * breath), rot=(math.radians(1.0 * breath), math.radians(-1.4 * breath), 0.0)),
        }
        insert_action_key(armature_obj, frame, pose, {"Idle_Breath_BodySway": breath})
    return action


def build_move_action(armature_obj: bpy.types.Object) -> bpy.types.Action:
    action = prepare_action(armature_obj, "LongaArma_Move_LimpingBladeArm", 48, True)
    limb_specs = [
        (("LongaRearLeftLeg", "LongaRearLeftLowerLeg", "LongaRearLeftFoot"), 0.00, 0.120, 0.105, -0.025, 0.020),
        (("LongaFrontLeftLeg", "LongaFrontLeftLowerLeg", "LongaFrontLeftFoot"), 0.25, -0.105, 0.085, -0.020, 0.018),
        (("LongaRearRightLeg", "LongaRearRightLowerLeg", "LongaRearRightFoot"), 0.50, 0.118, 0.103, 0.026, 0.020),
        (("LongaFrontRightLeg", "LongaFrontRightLowerLeg", "LongaFrontRightFoot"), 0.75, -0.128, 0.122, 0.023, 0.018),
    ]

    for frame in range(0, 49, 4):
        cycle = frame / 48.0
        pose: dict[str, dict[str, tuple[float, float, float]]] = {}
        add_pose(pose, "LongaRoot", loc=(0.0, 0.006 * math.sin(cycle * math.pi * 4.0), 0.014 * math.sin((cycle + 0.08) * math.pi * 4.0)))
        add_pose(pose, "LongaSpine", loc=(0.014 * math.sin(cycle * math.pi * 2.0), 0.004 * math.sin((cycle + 0.25) * math.pi * 2.0), 0.014 * math.sin((cycle + 0.18) * math.pi * 2.0)), rot=(0.0, math.radians(1.4 * math.sin(cycle * math.pi * 2.0)), math.radians(1.2 * math.sin((cycle + 0.2) * math.pi * 2.0))))
        add_pose(pose, "LongaChest", loc=(-0.012 + 0.018 * math.sin((cycle + 0.25) * math.pi * 2.0), 0.005 * math.sin((cycle + 0.1) * math.pi * 2.0), 0.018 * math.sin((cycle + 0.33) * math.pi * 2.0)), rot=(math.radians(1.6 * math.sin((cycle + 0.1) * math.pi * 2.0)), math.radians(-1.6 + 2.0 * math.sin((cycle + 0.25) * math.pi * 2.0)), 0.0))

        for bones, phase, stride_x, lift_z, lateral_y, support_drop_z in limb_specs:
            progress = (cycle - phase) % 1.0
            add_leg_cycle_pose(pose, bones, progress, stride_x, lift_z, lateral_y, support_drop_z)

        add_blade_drag_pose(pose, (cycle - 0.25) % 1.0)
        insert_action_key(armature_obj, frame, pose)

    return action


def build_attack_action(armature_obj: bpy.types.Object) -> bpy.types.Action:
    action = prepare_action(armature_obj, "LongaArma_Attack_Slam", 62, True)
    key_specs = [
        (0, {}, {}),
        (10, {
            "LongaRoot": pose_entry(loc=(-0.010, 0.0, 0.025)),
            "LongaChest": pose_entry(loc=(-0.055, 0.0, 0.150), rot=(math.radians(-2.0), math.radians(-9.0), 0.0)),
            "LongaHead": pose_entry(loc=(-0.045, 0.0, 0.100), rot=(math.radians(-3.0), math.radians(-12.0), 0.0)),
            "LongaBladeArm": pose_entry(loc=(-0.040, -0.012, 0.120), rot=(math.radians(-4.0), math.radians(-12.0), math.radians(-4.0))),
            "LongaBladeArmForearm": pose_entry(loc=(-0.080, -0.024, 0.240), rot=(math.radians(-6.0), math.radians(-18.0), math.radians(-6.0))),
            "LongaBladeArmTip": pose_entry(loc=(-0.130, -0.034, 0.320), rot=(math.radians(-8.0), math.radians(-22.0), math.radians(-8.0))),
            "LongaFrontRightLeg": pose_entry(loc=(-0.035, 0.018, 0.090), rot=(0.0, math.radians(-8.0), 0.0)),
            "LongaFrontRightLowerLeg": pose_entry(loc=(-0.070, 0.032, 0.180), rot=(0.0, math.radians(-12.0), 0.0)),
            "LongaFrontRightFoot": pose_entry(loc=(-0.110, 0.045, 0.250), rot=(math.radians(5.0), math.radians(-16.0), 0.0)),
        }, {"Attack_UpperBody_Rise": 0.35, "Attack_LeftBlade_SlamWindup": 0.35}),
        (22, {
            "LongaRoot": pose_entry(loc=(-0.020, 0.0, 0.070)),
            "LongaChest": pose_entry(loc=(-0.130, 0.0, 0.360), rot=(math.radians(-4.0), math.radians(-23.0), 0.0)),
            "LongaHead": pose_entry(loc=(-0.120, 0.0, 0.285), rot=(math.radians(-5.0), math.radians(-28.0), 0.0)),
            "LongaBladeArm": pose_entry(loc=(-0.075, -0.030, 0.250), rot=(math.radians(-10.0), math.radians(-24.0), math.radians(-8.0))),
            "LongaBladeArmForearm": pose_entry(loc=(-0.160, -0.052, 0.465), rot=(math.radians(-14.0), math.radians(-34.0), math.radians(-12.0))),
            "LongaBladeArmTip": pose_entry(loc=(-0.300, -0.074, 0.670), rot=(math.radians(-18.0), math.radians(-42.0), math.radians(-16.0))),
            "LongaFrontRightLeg": pose_entry(loc=(-0.060, 0.030, 0.210), rot=(0.0, math.radians(-18.0), 0.0)),
            "LongaFrontRightLowerLeg": pose_entry(loc=(-0.145, 0.052, 0.385), rot=(0.0, math.radians(-28.0), 0.0)),
            "LongaFrontRightFoot": pose_entry(loc=(-0.255, 0.074, 0.540), rot=(math.radians(10.0), math.radians(-36.0), 0.0)),
            "LongaRearLeftLeg": pose_entry(loc=(0.035, -0.015, -0.035), rot=(0.0, math.radians(8.0), 0.0)),
            "LongaRearRightLeg": pose_entry(loc=(0.035, 0.015, -0.035), rot=(0.0, math.radians(8.0), 0.0)),
        }, {"Attack_UpperBody_Rise": 1.0, "Attack_LeftBlade_SlamWindup": 1.0}),
        (34, {
            "LongaRoot": pose_entry(loc=(0.018, 0.0, -0.020)),
            "LongaChest": pose_entry(loc=(0.020, 0.0, 0.045), rot=(math.radians(7.0), math.radians(11.0), 0.0)),
            "LongaHead": pose_entry(loc=(0.090, 0.0, -0.020), rot=(math.radians(11.0), math.radians(13.0), 0.0)),
            "LongaBladeArm": pose_entry(loc=(0.060, -0.018, -0.025), rot=(math.radians(8.0), math.radians(18.0), math.radians(-4.0))),
            "LongaBladeArmForearm": pose_entry(loc=(0.145, -0.042, -0.080), rot=(math.radians(14.0), math.radians(28.0), math.radians(-7.0))),
            "LongaBladeArmTip": pose_entry(loc=(0.300, -0.065, -0.155), rot=(math.radians(18.0), math.radians(36.0), math.radians(-10.0))),
            "LongaFrontRightLeg": pose_entry(loc=(0.040, 0.020, -0.030), rot=(0.0, math.radians(14.0), 0.0)),
            "LongaFrontRightLowerLeg": pose_entry(loc=(0.110, 0.040, -0.085), rot=(0.0, math.radians(22.0), 0.0)),
            "LongaFrontRightFoot": pose_entry(loc=(0.210, 0.060, -0.140), rot=(math.radians(-8.0), math.radians(28.0), 0.0)),
        }, {"Attack_Forelimbs_ForwardSlam": 1.0, "Attack_UpperBody_Rise": 0.35}),
        (44, {
            "LongaRoot": pose_entry(loc=(-0.025, 0.0, -0.040)),
            "LongaChest": pose_entry(loc=(-0.075, 0.0, -0.030), rot=(math.radians(5.0), math.radians(-5.0), 0.0)),
            "LongaHead": pose_entry(loc=(0.010, 0.0, -0.055), rot=(math.radians(7.0), math.radians(-2.0), 0.0)),
            "LongaBladeArm": pose_entry(loc=(0.120, -0.015, -0.045), rot=(math.radians(6.0), math.radians(24.0), math.radians(-4.0))),
            "LongaBladeArmForearm": pose_entry(loc=(0.245, -0.036, -0.095), rot=(math.radians(10.0), math.radians(33.0), math.radians(-7.0))),
            "LongaBladeArmTip": pose_entry(loc=(0.430, -0.060, -0.125), rot=(math.radians(13.0), math.radians(39.0), math.radians(-9.0))),
            "LongaFrontRightFoot": pose_entry(loc=(0.270, 0.052, -0.110), rot=(math.radians(-6.0), math.radians(22.0), 0.0)),
        }, {"Attack_GroundDrag_Pullback": 1.0, "Attack_FrontLeg_SlamDrag": 0.85}),
        (62, {}, {}),
    ]

    for frame, pose, shapes in key_specs:
        insert_action_key(armature_obj, frame, pose, shapes)

    return action


def build_hit_action(armature_obj: bpy.types.Object) -> bpy.types.Action:
    action = prepare_action(armature_obj, "LongaArma_Hit_Recoil", 32, True)
    key_specs = [
        (0, {}, {}),
        (3, {
            "LongaRoot": pose_entry(loc=(-0.060, 0.0, 0.030)),
            "LongaChest": pose_entry(loc=(-0.100, -0.020, 0.040), rot=(math.radians(-3.0), math.radians(-8.0), math.radians(-7.0))),
            "LongaHead": pose_entry(loc=(-0.080, -0.075, 0.060), rot=(math.radians(-5.0), math.radians(-15.0), math.radians(-18.0))),
            "LongaBladeArmTip": pose_entry(loc=(-0.030, -0.035, 0.020), rot=(math.radians(-4.0), math.radians(-6.0), math.radians(-8.0))),
        }, {"Hit_HeadBack_Flinch": 1.0, "Hit_HeadSide_Shake": 0.80}),
        (7, {
            "LongaRoot": pose_entry(loc=(-0.030, 0.0, 0.010)),
            "LongaChest": pose_entry(loc=(-0.055, 0.018, 0.015), rot=(math.radians(1.0), math.radians(5.0), math.radians(6.0))),
            "LongaHead": pose_entry(loc=(-0.030, 0.075, 0.020), rot=(math.radians(3.0), math.radians(8.0), math.radians(18.0))),
            "LongaBladeArmTip": pose_entry(loc=(0.020, 0.026, -0.015), rot=(math.radians(3.0), math.radians(4.0), math.radians(6.0))),
        }, {"Hit_HeadBack_Flinch": 0.45, "Hit_HeadSide_Shake": 1.0}),
        (12, {
            "LongaChest": pose_entry(loc=(-0.020, -0.010, 0.010), rot=(0.0, math.radians(-2.0), math.radians(-3.0))),
            "LongaHead": pose_entry(loc=(-0.010, -0.035, 0.010), rot=(0.0, math.radians(-3.0), math.radians(-9.0))),
        }, {"Hit_HeadBack_Flinch": 0.22, "Hit_HeadSide_Shake": 0.45}),
        (32, {}, {}),
    ]
    for frame, pose, shapes in key_specs:
        insert_action_key(armature_obj, frame, pose, shapes)
    return action


def build_consume_action(armature_obj: bpy.types.Object) -> bpy.types.Action:
    action = prepare_action(armature_obj, "LongaArma_Consume_BiteSlam", 46, True)
    key_specs = [
        (0, {}, {}),
        (8, {
            "LongaChest": pose_entry(loc=(-0.040, 0.0, 0.085), rot=(math.radians(-2.0), math.radians(-8.0), 0.0)),
            "LongaHead": pose_entry(loc=(-0.120, 0.0, 0.145), rot=(math.radians(-4.0), math.radians(-23.0), 0.0)),
            "LongaBladeArmTip": pose_entry(loc=(-0.020, -0.020, -0.040), rot=(math.radians(-5.0), math.radians(4.0), 0.0)),
        }, {"Consume_HeadBack_Windup": 1.0}),
        (16, {
            "LongaChest": pose_entry(loc=(-0.020, 0.0, 0.040), rot=(math.radians(2.0), math.radians(3.0), 0.0)),
            "LongaHead": pose_entry(loc=(0.030, 0.0, 0.015), rot=(math.radians(4.0), math.radians(6.0), 0.0)),
        }, {"Consume_HeadBack_Windup": 0.45}),
        (22, {
            "LongaRoot": pose_entry(loc=(0.016, 0.0, -0.025)),
            "LongaChest": pose_entry(loc=(0.020, 0.0, -0.050), rot=(math.radians(5.0), math.radians(11.0), 0.0)),
            "LongaHead": pose_entry(loc=(0.165, 0.0, -0.205), rot=(math.radians(12.0), math.radians(24.0), 0.0)),
        }, {"Consume_HeadForward_BiteSlam": 1.0, "Consume_Peck_Impact": 1.0}),
        (29, {
            "LongaChest": pose_entry(loc=(0.000, 0.0, -0.020), rot=(math.radians(3.0), math.radians(7.0), 0.0)),
            "LongaHead": pose_entry(loc=(0.090, 0.0, -0.110), rot=(math.radians(8.0), math.radians(16.0), 0.0)),
        }, {"Consume_HeadForward_BiteSlam": 0.55, "Consume_Peck_Impact": 0.0}),
        (34, {
            "LongaHead": pose_entry(loc=(0.140, 0.0, -0.175), rot=(math.radians(10.0), math.radians(20.0), 0.0)),
        }, {"Consume_HeadForward_BiteSlam": 0.85, "Consume_Peck_Impact": 0.65}),
        (46, {}, {}),
    ]
    for frame, pose, shapes in key_specs:
        insert_action_key(armature_obj, frame, pose, shapes)
    return action


def build_death_action(armature_obj: bpy.types.Object) -> bpy.types.Action:
    action = prepare_action(armature_obj, "LongaArma_Death_Melt", 66, True)
    key_specs = [
        (0, {}, {}),
        (12, {
            "LongaRoot": pose_entry(loc=(0.0, 0.0, -0.060), scale=(1.06, 1.06, 0.82)),
            "LongaChest": pose_entry(loc=(-0.010, 0.0, -0.120), rot=(math.radians(6.0), math.radians(3.0), 0.0), scale=(1.08, 1.08, 0.74)),
            "LongaHead": pose_entry(loc=(0.020, 0.0, -0.110), rot=(math.radians(7.0), math.radians(8.0), 0.0), scale=(1.05, 1.05, 0.70)),
            "LongaBladeArmTip": pose_entry(loc=(0.060, -0.010, -0.070), scale=(1.08, 1.08, 0.72)),
        }, {"Death_Melt_FlatLiquidSpread": 0.35}),
        (28, {
            "LongaRoot": pose_entry(loc=(0.0, 0.0, -0.220), scale=(1.18, 1.28, 0.42)),
            "LongaSpine": pose_entry(loc=(0.000, 0.0, -0.240), scale=(1.20, 1.32, 0.32)),
            "LongaChest": pose_entry(loc=(0.010, 0.0, -0.310), rot=(math.radians(9.0), 0.0, 0.0), scale=(1.22, 1.38, 0.28)),
            "LongaHead": pose_entry(loc=(0.065, 0.0, -0.300), rot=(math.radians(12.0), math.radians(8.0), 0.0), scale=(1.15, 1.25, 0.25)),
            "LongaBladeArm": pose_entry(loc=(0.060, -0.010, -0.220), scale=(1.16, 1.24, 0.30)),
            "LongaBladeArmForearm": pose_entry(loc=(0.095, -0.020, -0.260), scale=(1.18, 1.28, 0.24)),
            "LongaBladeArmTip": pose_entry(loc=(0.130, -0.030, -0.285), scale=(1.20, 1.32, 0.20)),
        }, {"Death_Melt_FlatLiquidSpread": 1.0, "Death_Puddle_Final": 0.08}),
        (44, {
            "LongaRoot": pose_entry(loc=(0.0, 0.0, -0.400), scale=(1.20, 1.34, 0.16)),
            "LongaSpine": pose_entry(loc=(0.010, 0.0, -0.430), scale=(1.24, 1.40, 0.12)),
            "LongaChest": pose_entry(loc=(0.020, 0.0, -0.470), scale=(1.28, 1.46, 0.10)),
            "LongaHead": pose_entry(loc=(0.090, 0.0, -0.455), scale=(1.22, 1.34, 0.10)),
            "LongaBladeArm": pose_entry(loc=(0.055, -0.006, -0.410), scale=(0.76, 0.92, 0.10)),
            "LongaBladeArmForearm": pose_entry(loc=(0.085, -0.010, -0.415), scale=(0.72, 0.88, 0.10)),
            "LongaBladeArmTip": pose_entry(loc=(0.110, -0.014, -0.420), scale=(0.68, 0.84, 0.10)),
        }, {"Death_Melt_FlatLiquidSpread": 0.60, "Death_Puddle_Final": 0.74}),
        (66, {
            "LongaRoot": pose_entry(loc=(0.0, 0.0, -0.510), scale=(1.28, 1.50, 0.08)),
            "LongaSpine": pose_entry(loc=(0.015, 0.0, -0.530), scale=(1.31, 1.56, 0.06)),
            "LongaChest": pose_entry(loc=(0.025, 0.0, -0.545), scale=(1.34, 1.62, 0.05)),
            "LongaHead": pose_entry(loc=(0.095, 0.0, -0.535), scale=(1.28, 1.48, 0.05)),
            "LongaBladeArm": pose_entry(loc=(0.030, -0.004, -0.520), scale=(0.48, 0.66, 0.05)),
            "LongaBladeArmForearm": pose_entry(loc=(0.050, -0.006, -0.525), scale=(0.42, 0.60, 0.05)),
            "LongaBladeArmTip": pose_entry(loc=(0.070, -0.008, -0.525), scale=(0.36, 0.54, 0.05)),
        }, {"Death_Melt_FlatLiquidSpread": 0.0, "Death_Puddle_Final": 1.0}),
    ]
    for frame, pose, shapes in key_specs:
        insert_action_key(armature_obj, frame, pose, shapes)
    return action


def create_runtime_actions(armature_obj: bpy.types.Object, obj: bpy.types.Object) -> dict[str, bpy.types.Action]:
    ensure_shape_key_drivers(obj, armature_obj)
    clear_longa_actions()

    actions = {
        "LongaArma_Static_Review": build_static_action(armature_obj),
        "LongaArma_Idle": build_idle_action(armature_obj),
        "LongaArma_Move_LimpingBladeArm": build_move_action(armature_obj),
        "LongaArma_Attack_Slam": build_attack_action(armature_obj),
        "LongaArma_Hit_Recoil": build_hit_action(armature_obj),
        "LongaArma_Consume_BiteSlam": build_consume_action(armature_obj),
        "LongaArma_Death_Melt": build_death_action(armature_obj),
    }

    armature_obj.animation_data.action = actions["LongaArma_Static_Review"]
    reset_animation_state(armature_obj)
    bpy.context.scene.frame_set(0)
    return actions


def configure_auto_rig_pro_for_actions(actions: dict[str, bpy.types.Action]) -> dict[str, object]:
    metadata: dict[str, object] = {
        "addonEnabled": "bl_ext.user_default.auto_rig_pro" in bpy.context.preferences.addons,
        "guessMarkersOperatorAvailable": hasattr(bpy.ops.arp, "guess_markers"),
        "gameEngineExportOperatorAvailable": hasattr(bpy.ops.arp, "arp_export_fbx_panel"),
        "usedFor": "Action export registration and FBX/GLB export settings; Smart marker auto-rig is not used because Longa Arma is a non-humanoid asymmetric quadruped.",
    }

    if not metadata["addonEnabled"]:
        raise RuntimeError("Auto-Rig Pro is not enabled in Blender; cannot build Longa Arma ARP-assisted animation sample.")

    scene = bpy.context.scene
    for attr_name, value in [
        ("arp_bake_anim", True),
        ("arp_bake_type", "ACTIONS"),
        ("arp_bake_only_active", False),
        ("arp_ignore_linked_actions", True),
        ("arp_simplify_fac", 0.0),
        ("arp_ge_bake_sample", 1.0),
        ("arp_ge_startend_keying", True),
        ("arp_ge_startend_keying_sk", True),
        ("arp_export_act_name", "NONE"),
        ("arp_export_use_actlist", True),
        ("arp_export_separate_fbx", False),
        ("arp_units_x100", False),
        ("arp_global_scale", 1.0),
    ]:
        if hasattr(scene, attr_name):
            setattr(scene, attr_name, value)

    if hasattr(bpy.ops.arp, "ge_deselect_allactions"):
        bpy.ops.arp.ge_deselect_allactions()

    for action in actions.values():
        action["arp_export"] = True

    if hasattr(scene, "arp_export_actlist"):
        while len(scene.arp_export_actlist):
            scene.arp_export_actlist.remove(0)
        action_list = scene.arp_export_actlist.add()
        action_list.name = "LongaArmaRuntimeLowPoly"
        action_list.exportable = True
        for action in actions.values():
            item = action_list.actions.add()
            item.action = action

    metadata["registeredActions"] = list(actions.keys())
    metadata["actionManagerListName"] = "LongaArmaRuntimeLowPoly"
    return metadata


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


def render_animation_state_previews(
    camera: bpy.types.Object,
    bounds: dict[str, Vector],
    armature_obj: bpy.types.Object,
    actions: dict[str, bpy.types.Action],
) -> list[str]:
    rendered_files: list[str] = []
    if armature_obj.animation_data is None:
        return rendered_files

    scene = bpy.context.scene
    previous_action = armature_obj.animation_data.action

    for action_name, frame in ACTION_PREVIEW_SAMPLES.items():
        action = actions.get(action_name)
        if action is None:
            continue

        armature_obj.animation_data.action = action
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        is_death_preview = action_name == "LongaArma_Death_Melt"
        preview_scale = 1.15 if is_death_preview else 2.35
        preview_direction = Vector((0.0, -0.10, 1.0)) if is_death_preview else Vector((-1.0, -0.65, 0.10))
        preview_vertical_bias = 0.02 if is_death_preview else 0.08
        preview_bounds = dict(bounds)
        preview_bounds["size"] = bounds["size"] * preview_scale
        preview_bounds["min"] = bounds["center"] - preview_bounds["size"] * 0.5
        preview_bounds["max"] = bounds["center"] + preview_bounds["size"] * 0.5
        set_camera_view(camera, preview_bounds, preview_direction, vertical_bias=preview_vertical_bias)
        output_name = "animation_preview_" + action_name.replace("LongaArma_", "").lower() + ".png"
        scene.render.filepath = str(RENDER_ROOT / output_name)
        bpy.ops.render.render(write_still=True)
        rendered_files.append(f"renders/{output_name}")

    armature_obj.animation_data.action = previous_action
    scene.frame_set(0)
    bpy.context.view_layer.update()
    return rendered_files


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
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
    )
    select_export_objects(obj, armature_obj)
    bpy.ops.export_scene.gltf(
        filepath=str(GLB_OUTPUT),
        export_format="GLB",
        use_selection=True,
        export_materials="EXPORT",
        export_animations=True,
        export_animation_mode="ACTIONS",
        export_force_sampling=True,
        export_morph=True,
        export_morph_animation=True,
    )
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_OUTPUT))


def write_docs(stats: dict[str, object]) -> None:
    preview_files = [str(path) for path in stats.get("animationPreviewFiles", [])]
    action_lines = "".join(
        f"- `{spec['name']}`: {spec['description']} ({spec['frames']} frames, loop={str(spec['loop']).lower()})\n"
        for spec in ANIMATION_ACTION_SPECS
    )
    preview_lines = "".join(f"- `{path}`\n" for path in preview_files)
    arp_metadata = stats.get("autoRigPro", {})
    arp_used_for = arp_metadata.get("usedFor", "Auto-Rig Pro metadata was not recorded.") if isinstance(arp_metadata, dict) else "Auto-Rig Pro metadata was not recorded."

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
        ]
        + preview_files
        + [f"textures/{name}" for name in TEXTURE_FILES if (TEXTURE_OUTPUT_ROOT / name).exists()],
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
        "## Blender Action\n\n"
        + action_lines
        + "\n"
        "## 애니메이션 프리뷰 렌더\n\n"
        + (preview_lines if preview_lines else "- 생성된 프리뷰 렌더가 없습니다.\n")
        + "\n"
        "## Auto-Rig Pro 사용\n\n"
        f"- Auto-Rig Pro 활성화: `{arp_metadata.get('addonEnabled') if isinstance(arp_metadata, dict) else 'unknown'}`\n"
        f"- ARP Action Manager 등록: `{arp_metadata.get('actionManagerListName') if isinstance(arp_metadata, dict) else 'unknown'}`\n"
        f"- 사용 방식: {arp_used_for}\n"
        "- 롱가 아르마는 비대칭 사족 괴물형이므로 휴머노이드 Smart marker 자동 리깅은 적용하지 않았고, 커스텀 사족 리그 Action을 ARP export 설정과 Action export flag에 등록했습니다.\n\n"
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
    actions = create_runtime_actions(armature_obj, obj)
    arp_metadata = configure_auto_rig_pro_for_actions(actions)

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
        "animationSystem": "Auto-Rig Pro registered custom quadruped Action set on a single visible skinned mesh; Shape Key drivers remain for breathing and liquid deformation support",
        "runtimeAnimationActions": ANIMATION_ACTION_SPECS,
        "autoRigPro": arp_metadata,
    }

    camera, _key, _fill = configure_render_scene()
    render_view(camera, bounds, "front", Vector((-1.0, 0.0, 0.06)))
    render_view(camera, bounds, "side", Vector((0.0, -1.0, 0.06)))
    render_view(camera, bounds, "back", Vector((1.0, 0.0, 0.06)))
    render_view(camera, bounds, "three_quarter", Vector((-1.0, -0.65, 0.10)))
    render_wireframe(obj, camera, bounds)
    stats["animationPreviewFiles"] = render_animation_state_previews(camera, bounds, armature_obj, actions)
    export_assets(obj, armature_obj)
    write_docs(stats)
    print("LONGA_RUNTIME_LOWPOLY_SAMPLE_CREATED")
    print(json.dumps(stats, ensure_ascii=False))


if __name__ == "__main__":
    main()
