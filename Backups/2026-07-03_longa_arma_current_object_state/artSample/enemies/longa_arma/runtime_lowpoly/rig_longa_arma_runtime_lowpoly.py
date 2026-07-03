from __future__ import annotations

import json
import math
from datetime import datetime
from pathlib import Path

import bpy
from mathutils import Vector


SCRIPT_PATH = Path(__file__).resolve()
OUTPUT_ROOT = SCRIPT_PATH.parent
BLEND_PATH = OUTPUT_ROOT / "blender" / "longa_arma_runtime_lowpoly.blend"
RIGGED_FBX_PATH = OUTPUT_ROOT / "exports" / "longa_arma_runtime_lowpoly_rigged.fbx"
RIGGED_GLB_PATH = OUTPUT_ROOT / "exports" / "longa_arma_runtime_lowpoly_rigged.glb"
RIG_REPORT_PATH = OUTPUT_ROOT / "rigging_report_2026-07-03.json"
RIG_STATUS_PATH = OUTPUT_ROOT / "RIGGING_STATUS_2026-07-03.md"
RENDER_ROOT = OUTPUT_ROOT / "renders"

MESH_NAME = "LongaArma_Runtime_LowPoly"
OLD_RIG_NAMES = {"LongaArma_Runtime_Rig", "LongaArma_ARP_Detailed_Rig"}
RIG_NAME = "LongaArma_ARP_Detailed_Rig"

RENDER_WIDTH = 1400
RENDER_HEIGHT = 1000

SHAPE_KEY_NAMES = [
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

DEFORM_BONES = [
    "DEF_root",
    "DEF_pelvis",
    "DEF_spine_01",
    "DEF_spine_02",
    "DEF_chest",
    "DEF_neck_01",
    "DEF_head",
    "DEF_mouth_tip",
    "DEF_belly_mass",
    "DEF_blade_shoulder_l",
    "DEF_blade_upper_l",
    "DEF_blade_fore_l",
    "DEF_blade_tip_l",
    "DEF_front_right_upper",
    "DEF_front_right_lower",
    "DEF_front_right_foot",
    "DEF_front_right_toe",
    "DEF_front_left_upper",
    "DEF_front_left_lower",
    "DEF_front_left_foot",
    "DEF_front_left_toe",
    "DEF_rear_right_upper",
    "DEF_rear_right_lower",
    "DEF_rear_right_foot",
    "DEF_rear_right_toe",
    "DEF_rear_left_upper",
    "DEF_rear_left_lower",
    "DEF_rear_left_foot",
    "DEF_rear_left_toe",
]

CONTROL_BONES = [
    "CTRL_root",
    "CTRL_pelvis",
    "CTRL_chest",
    "CTRL_spine_lift",
    "CTRL_head",
    "CTRL_mouth",
    "CTRL_body_morph",
    "CTRL_blade_ik_l",
    "CTRL_blade_pole_l",
    "CTRL_front_right_ik",
    "CTRL_front_right_pole",
    "CTRL_front_left_ik",
    "CTRL_front_left_pole",
    "CTRL_rear_right_ik",
    "CTRL_rear_right_pole",
    "CTRL_rear_left_ik",
    "CTRL_rear_left_pole",
]

CONTROL_MARKERS = [
    "CTRL_root",
    "CTRL_pelvis",
    "CTRL_chest",
    "CTRL_spine_lift",
    "CTRL_head",
    "CTRL_body_morph",
    "CTRL_blade_ik_l",
    "CTRL_blade_pole_l",
    "CTRL_front_right_ik",
    "CTRL_front_left_ik",
    "CTRL_rear_right_ik",
    "CTRL_rear_left_ik",
]

FINAL_ACTION_SPECS = [
    ("LongaArma_Static_Review", 30, False),
    ("LongaArma_Idle", 48, True),
    ("LongaArma_Move_Crawl", 48, True),
    ("LongaArma_Attack_SlamDrag", 62, False),
    ("LongaArma_Hit_Recoil", 32, False),
    ("LongaArma_Consume_Peck", 46, False),
    ("LongaArma_Death_MeltPuddle", 66, False),
]


def clamp01(value: float) -> float:
    return max(0.0, min(1.0, value))


def smoothstep(edge0: float, edge1: float, value: float) -> float:
    if abs(edge1 - edge0) < 0.00001:
        return 1.0 if value >= edge1 else 0.0
    t = clamp01((value - edge0) / (edge1 - edge0))
    return t * t * (3.0 - 2.0 * t)


def normalized(value: float, minimum: float, size: float) -> float:
    return clamp01((value - minimum) / max(size, 0.0001))


def ensure_dirs() -> None:
    RENDER_ROOT.mkdir(parents=True, exist_ok=True)
    RIGGED_FBX_PATH.parent.mkdir(parents=True, exist_ok=True)


def mesh_bounds(obj: bpy.types.Object) -> dict[str, Vector]:
    coords = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    min_vec = Vector((min(v.x for v in coords), min(v.y for v in coords), min(v.z for v in coords)))
    max_vec = Vector((max(v.x for v in coords), max(v.y for v in coords), max(v.z for v in coords)))
    return {"min": min_vec, "max": max_vec, "size": max_vec - min_vec, "center": (min_vec + max_vec) * 0.5}


def evaluated_mesh_bounds(obj: bpy.types.Object) -> dict[str, Vector]:
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(depsgraph)
    coords = [evaluated.matrix_world @ vertex.co for vertex in evaluated.data.vertices]
    min_vec = Vector((min(v.x for v in coords), min(v.y for v in coords), min(v.z for v in coords)))
    max_vec = Vector((max(v.x for v in coords), max(v.y for v in coords), max(v.z for v in coords)))
    return {"min": min_vec, "max": max_vec, "size": max_vec - min_vec, "center": (min_vec + max_vec) * 0.5}


def local_point(bounds: dict[str, Vector], xn: float, yn: float, zn: float) -> tuple[float, float, float]:
    min_vec = bounds["min"]
    size = bounds["size"]
    return (
        min_vec.x + size.x * xn,
        min_vec.y + size.y * yn,
        min_vec.z + size.z * zn,
    )


def load_mesh() -> bpy.types.Object:
    if not BLEND_PATH.exists():
        raise FileNotFoundError(f"Missing blend file: {BLEND_PATH}")
    bpy.ops.wm.open_mainfile(filepath=str(BLEND_PATH))
    mesh = bpy.data.objects.get(MESH_NAME)
    if mesh is None:
        meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
        if not meshes:
            raise RuntimeError("No mesh object was found in the Longa Arma blend.")
        meshes.sort(key=lambda obj: len(obj.data.polygons), reverse=True)
        mesh = meshes[0]
        mesh.name = MESH_NAME
    return mesh


def discard_previous_runtime_rig(mesh: bpy.types.Object) -> None:
    for modifier in list(mesh.modifiers):
        if modifier.type == "ARMATURE":
            mesh.modifiers.remove(modifier)

    for obj in list(bpy.data.objects):
        if obj.type == "ARMATURE" and (obj.name in OLD_RIG_NAMES or obj.name.startswith("LongaArma_ARP_Detailed_Rig")):
            bpy.data.objects.remove(obj, do_unlink=True)

    for armature_data in list(bpy.data.armatures):
        if armature_data.name in OLD_RIG_NAMES or armature_data.name.startswith("LongaArma_ARP_Detailed_Rig"):
            bpy.data.armatures.remove(armature_data, do_unlink=True)

    for obj in list(bpy.data.objects):
        if obj.name.startswith("CTRL_VIS_"):
            bpy.data.objects.remove(obj, do_unlink=True)

    mesh.vertex_groups.clear()

    for action in list(bpy.data.actions):
        if action.name.startswith("LongaArma_"):
            bpy.data.actions.remove(action)


def create_material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    existing = bpy.data.materials.get(name)
    if existing is not None:
        existing.diffuse_color = color
        return existing
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    return material


def add_edit_bone(
    armature_data: bpy.types.Armature,
    name: str,
    head: tuple[float, float, float],
    tail: tuple[float, float, float],
    parent: bpy.types.EditBone | None = None,
    deform: bool = True,
) -> bpy.types.EditBone:
    bone = armature_data.edit_bones.new(name)
    bone.head = head
    bone.tail = tail
    bone.roll = 0.0
    bone.use_deform = deform
    if parent is not None:
        bone.parent = parent
        bone.use_connect = False
    return bone


def create_detailed_rig(mesh: bpy.types.Object) -> bpy.types.Object:
    bounds = mesh_bounds(mesh)
    armature_data = bpy.data.armatures.new(RIG_NAME)
    rig = bpy.data.objects.new(RIG_NAME, armature_data)
    bpy.context.collection.objects.link(rig)
    rig.show_in_front = True
    armature_data.display_type = "BBONE"
    armature_data.show_names = True

    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    root = add_edit_bone(armature_data, "DEF_root", local_point(bounds, 0.50, 0.50, 0.08), local_point(bounds, 0.50, 0.50, 0.28))
    pelvis = add_edit_bone(armature_data, "DEF_pelvis", local_point(bounds, 0.20, 0.50, 0.34), local_point(bounds, 0.34, 0.50, 0.40), root)
    spine_01 = add_edit_bone(armature_data, "DEF_spine_01", local_point(bounds, 0.34, 0.50, 0.40), local_point(bounds, 0.48, 0.50, 0.47), pelvis)
    spine_02 = add_edit_bone(armature_data, "DEF_spine_02", local_point(bounds, 0.48, 0.50, 0.47), local_point(bounds, 0.62, 0.50, 0.57), spine_01)
    chest = add_edit_bone(armature_data, "DEF_chest", local_point(bounds, 0.62, 0.50, 0.57), local_point(bounds, 0.76, 0.50, 0.68), spine_02)
    neck = add_edit_bone(armature_data, "DEF_neck_01", local_point(bounds, 0.76, 0.50, 0.68), local_point(bounds, 0.88, 0.50, 0.74), chest)
    head = add_edit_bone(armature_data, "DEF_head", local_point(bounds, 0.86, 0.50, 0.73), local_point(bounds, 0.99, 0.50, 0.76), neck)
    add_edit_bone(armature_data, "DEF_mouth_tip", local_point(bounds, 0.90, 0.50, 0.63), local_point(bounds, 0.99, 0.50, 0.58), head)
    add_edit_bone(armature_data, "DEF_belly_mass", local_point(bounds, 0.32, 0.50, 0.22), local_point(bounds, 0.62, 0.50, 0.24), root)

    blade_shoulder = add_edit_bone(armature_data, "DEF_blade_shoulder_l", local_point(bounds, 0.58, 0.28, 0.52), local_point(bounds, 0.44, 0.20, 0.39), chest)
    blade_upper = add_edit_bone(armature_data, "DEF_blade_upper_l", local_point(bounds, 0.44, 0.20, 0.39), local_point(bounds, 0.28, 0.14, 0.24), blade_shoulder)
    blade_fore = add_edit_bone(armature_data, "DEF_blade_fore_l", local_point(bounds, 0.28, 0.14, 0.24), local_point(bounds, 0.12, 0.10, 0.10), blade_upper)
    add_edit_bone(armature_data, "DEF_blade_tip_l", local_point(bounds, 0.12, 0.10, 0.10), local_point(bounds, 0.02, 0.08, 0.03), blade_fore)

    limb_specs = {
        "front_right": (0.58, 0.82, chest),
        "front_left": (0.58, 0.18, chest),
        "rear_right": (0.27, 0.80, pelvis),
        "rear_left": (0.27, 0.20, pelvis),
    }
    for name, (xn, yn, parent) in limb_specs.items():
        side_offset = 0.05 if yn > 0.5 else -0.05
        upper = add_edit_bone(
            armature_data,
            f"DEF_{name}_upper",
            local_point(bounds, xn, yn, 0.42),
            local_point(bounds, xn + 0.02, yn + side_offset, 0.27),
            parent,
        )
        lower = add_edit_bone(
            armature_data,
            f"DEF_{name}_lower",
            local_point(bounds, xn + 0.02, yn + side_offset, 0.27),
            local_point(bounds, xn + 0.03, yn + side_offset * 1.45, 0.12),
            upper,
        )
        foot = add_edit_bone(
            armature_data,
            f"DEF_{name}_foot",
            local_point(bounds, xn + 0.03, yn + side_offset * 1.45, 0.12),
            local_point(bounds, xn + 0.09, yn + side_offset * 1.75, 0.04),
            lower,
        )
        add_edit_bone(
            armature_data,
            f"DEF_{name}_toe",
            local_point(bounds, xn + 0.09, yn + side_offset * 1.75, 0.04),
            local_point(bounds, xn + 0.16, yn + side_offset * 1.90, 0.025),
            foot,
        )

    ctrl_root = add_edit_bone(armature_data, "CTRL_root", local_point(bounds, 0.50, 0.50, 0.00), local_point(bounds, 0.50, 0.50, 0.18), None, False)
    add_edit_bone(armature_data, "CTRL_pelvis", local_point(bounds, 0.23, 0.50, 0.50), local_point(bounds, 0.23, 0.50, 0.66), ctrl_root, False)
    add_edit_bone(armature_data, "CTRL_chest", local_point(bounds, 0.68, 0.50, 0.74), local_point(bounds, 0.68, 0.50, 0.92), ctrl_root, False)
    add_edit_bone(armature_data, "CTRL_spine_lift", local_point(bounds, 0.52, 0.50, 0.86), local_point(bounds, 0.52, 0.50, 1.05), ctrl_root, False)
    add_edit_bone(armature_data, "CTRL_head", local_point(bounds, 0.92, 0.50, 0.88), local_point(bounds, 0.92, 0.50, 1.05), ctrl_root, False)
    add_edit_bone(armature_data, "CTRL_mouth", local_point(bounds, 0.99, 0.50, 0.62), local_point(bounds, 1.02, 0.50, 0.70), ctrl_root, False)
    add_edit_bone(armature_data, "CTRL_body_morph", local_point(bounds, 0.42, 0.50, 1.00), local_point(bounds, 0.42, 0.50, 1.16), ctrl_root, False)
    add_edit_bone(armature_data, "CTRL_blade_ik_l", local_point(bounds, 0.02, 0.08, 0.04), local_point(bounds, 0.02, 0.08, 0.18), ctrl_root, False)
    add_edit_bone(armature_data, "CTRL_blade_pole_l", local_point(bounds, 0.18, 0.00, 0.34), local_point(bounds, 0.18, 0.00, 0.50), ctrl_root, False)

    for name, (xn, yn, _parent) in limb_specs.items():
        side_offset = 0.05 if yn > 0.5 else -0.05
        add_edit_bone(
            armature_data,
            f"CTRL_{name}_ik",
            local_point(bounds, xn + 0.10, yn + side_offset * 1.75, 0.035),
            local_point(bounds, xn + 0.10, yn + side_offset * 1.75, 0.17),
            ctrl_root,
            False,
        )
        add_edit_bone(
            armature_data,
            f"CTRL_{name}_pole",
            local_point(bounds, xn + 0.03, yn + side_offset * 2.25, 0.34),
            local_point(bounds, xn + 0.03, yn + side_offset * 2.25, 0.50),
            ctrl_root,
            False,
        )

    bpy.ops.object.mode_set(mode="OBJECT")

    for bone in armature_data.bones:
        bone.use_deform = bone.name.startswith("DEF_")

    create_bone_collections(armature_data)
    add_constraints(rig)
    create_shape_key_controls(mesh, rig)
    bind_mesh(mesh, rig)
    add_control_markers(rig)
    return rig


def create_bone_collections(armature_data: bpy.types.Armature) -> None:
    try:
        for collection in list(armature_data.collections):
            armature_data.collections.remove(collection)
        deform_collection = armature_data.collections.new("DEF_Deform")
        control_collection = armature_data.collections.new("CTRL_Animator")
        for bone in armature_data.bones:
            target = deform_collection if bone.name.startswith("DEF_") else control_collection
            target.assign(bone)
    except Exception:
        pass


def add_copy_rotation(rig: bpy.types.Object, owner: str, target: str, influence: float = 1.0) -> None:
    pose_bone = rig.pose.bones[owner]
    constraint = pose_bone.constraints.new("COPY_ROTATION")
    constraint.name = f"Copy {target}"
    constraint.target = rig
    constraint.subtarget = target
    constraint.target_space = "LOCAL"
    constraint.owner_space = "LOCAL"
    constraint.influence = influence


def add_ik(rig: bpy.types.Object, owner: str, target: str, pole: str, chain_count: int, pole_angle: float) -> None:
    pose_bone = rig.pose.bones[owner]
    constraint = pose_bone.constraints.new("IK")
    constraint.name = f"IK {target}"
    constraint.target = rig
    constraint.subtarget = target
    constraint.pole_target = rig
    constraint.pole_subtarget = pole
    constraint.chain_count = chain_count
    constraint.use_rotation = True
    constraint.pole_angle = math.radians(pole_angle)


def add_constraints(rig: bpy.types.Object) -> None:
    add_copy_rotation(rig, "DEF_pelvis", "CTRL_pelvis", 0.75)
    add_copy_rotation(rig, "DEF_chest", "CTRL_chest", 0.85)
    add_copy_rotation(rig, "DEF_head", "CTRL_head", 0.90)
    add_copy_rotation(rig, "DEF_mouth_tip", "CTRL_mouth", 0.80)

    add_ik(rig, "DEF_blade_tip_l", "CTRL_blade_ik_l", "CTRL_blade_pole_l", 4, -90.0)
    for name, pole_angle in {
        "front_right": -90.0,
        "front_left": 90.0,
        "rear_right": -90.0,
        "rear_left": 90.0,
    }.items():
        add_ik(rig, f"DEF_{name}_toe", f"CTRL_{name}_ik", f"CTRL_{name}_pole", 4, pole_angle)


def create_shape_key_controls(mesh: bpy.types.Object, rig: bpy.types.Object) -> None:
    body_morph = rig.pose.bones.get("CTRL_body_morph")
    if body_morph is None or mesh.data.shape_keys is None:
        return

    key_blocks = mesh.data.shape_keys.key_blocks
    for shape_name in SHAPE_KEY_NAMES:
        if shape_name not in key_blocks:
            continue
        prop_name = "shape_" + shape_name
        body_morph[prop_name] = 0.0
        try:
            body_morph.id_properties_ui(prop_name).update(min=0.0, max=1.0, soft_min=0.0, soft_max=1.0)
        except Exception:
            pass

        shape_key = key_blocks[shape_name]
        shape_key.value = 0.0
        try:
            shape_key.driver_remove("value")
        except (RuntimeError, TypeError):
            pass
        driver = shape_key.driver_add("value").driver
        driver.type = "SCRIPTED"
        driver.expression = "value"
        variable = driver.variables.new()
        variable.name = "value"
        variable.type = "SINGLE_PROP"
        target = variable.targets[0]
        target.id = rig
        target.data_path = f'pose.bones["CTRL_body_morph"]["{prop_name}"]'


def bone_weight(xn: float, yn: float, zn: float, center: tuple[float, float, float], radius: tuple[float, float, float]) -> float:
    dx = (xn - center[0]) / max(radius[0], 0.0001)
    dy = (yn - center[1]) / max(radius[1], 0.0001)
    dz = (zn - center[2]) / max(radius[2], 0.0001)
    return math.exp(-(dx * dx + dy * dy + dz * dz))


def bind_mesh(mesh: bpy.types.Object, rig: bpy.types.Object) -> None:
    mesh.vertex_groups.clear()
    groups = {bone_name: mesh.vertex_groups.new(name=bone_name) for bone_name in DEFORM_BONES}
    bounds = mesh_bounds(mesh)
    min_vec = bounds["min"]
    size = bounds["size"]

    for vertex in mesh.data.vertices:
        world = mesh.matrix_world @ vertex.co
        xn = normalized(world.x, min_vec.x, size.x)
        yn = normalized(world.y, min_vec.y, size.y)
        zn = normalized(world.z, min_vec.z, size.z)

        weights = {
            "DEF_root": 0.05,
            "DEF_pelvis": bone_weight(xn, yn, zn, (0.22, 0.50, 0.38), (0.22, 0.46, 0.24)),
            "DEF_spine_01": bone_weight(xn, yn, zn, (0.39, 0.50, 0.43), (0.18, 0.42, 0.22)),
            "DEF_spine_02": bone_weight(xn, yn, zn, (0.54, 0.50, 0.51), (0.18, 0.40, 0.23)),
            "DEF_chest": bone_weight(xn, yn, zn, (0.68, 0.50, 0.60), (0.18, 0.38, 0.25)),
            "DEF_neck_01": bone_weight(xn, yn, zn, (0.80, 0.50, 0.70), (0.11, 0.24, 0.18)),
            "DEF_head": bone_weight(xn, yn, zn, (0.93, 0.50, 0.70), (0.12, 0.24, 0.20)),
            "DEF_mouth_tip": bone_weight(xn, yn, zn, (0.98, 0.50, 0.58), (0.08, 0.18, 0.14)),
            "DEF_belly_mass": bone_weight(xn, yn, zn, (0.48, 0.50, 0.20), (0.32, 0.45, 0.15)),
            "DEF_blade_shoulder_l": bone_weight(xn, yn, zn, (0.52, 0.25, 0.45), (0.14, 0.16, 0.18)),
            "DEF_blade_upper_l": bone_weight(xn, yn, zn, (0.37, 0.17, 0.30), (0.15, 0.14, 0.17)),
            "DEF_blade_fore_l": bone_weight(xn, yn, zn, (0.22, 0.12, 0.16), (0.16, 0.12, 0.14)),
            "DEF_blade_tip_l": bone_weight(xn, yn, zn, (0.06, 0.09, 0.05), (0.14, 0.10, 0.09)),
        }

        for name, cx, cy in [
            ("front_right", 0.60, 0.84),
            ("front_left", 0.60, 0.16),
            ("rear_right", 0.28, 0.82),
            ("rear_left", 0.28, 0.18),
        ]:
            weights[f"DEF_{name}_upper"] = bone_weight(xn, yn, zn, (cx, cy, 0.36), (0.13, 0.15, 0.18))
            weights[f"DEF_{name}_lower"] = bone_weight(xn, yn, zn, (cx + 0.02, cy, 0.20), (0.12, 0.14, 0.14))
            weights[f"DEF_{name}_foot"] = bone_weight(xn, yn, zn, (cx + 0.06, cy, 0.08), (0.12, 0.15, 0.09))
            weights[f"DEF_{name}_toe"] = bone_weight(xn, yn, zn, (cx + 0.12, cy, 0.04), (0.12, 0.13, 0.07))

        strongest = sorted(weights.items(), key=lambda item: item[1], reverse=True)[:5]
        total = sum(value for _name, value in strongest)
        if total <= 0.0001:
            groups["DEF_spine_01"].add([vertex.index], 1.0, "ADD")
            continue
        for bone_name, value in strongest:
            normalized_weight = value / total
            if normalized_weight > 0.005:
                groups[bone_name].add([vertex.index], normalized_weight, "ADD")

    armature_modifier = mesh.modifiers.new("LongaArma_ARP_DetailedRig_Armature", "ARMATURE")
    armature_modifier.object = rig
    mesh.parent = rig
    mesh.matrix_parent_inverse = rig.matrix_world.inverted()
    mesh.data.update()


def add_control_markers(rig: bpy.types.Object) -> None:
    materials = {
        "core": create_material("M_RigMarker_Core_Yellow", (1.0, 0.82, 0.18, 1.0)),
        "ik": create_material("M_RigMarker_IK_Cyan", (0.12, 0.86, 1.0, 1.0)),
        "pole": create_material("M_RigMarker_Pole_Magenta", (1.0, 0.22, 0.86, 1.0)),
    }
    for bone_name in CONTROL_MARKERS:
        pose_bone = rig.pose.bones.get(bone_name)
        if pose_bone is None:
            continue
        position = rig.matrix_world @ pose_bone.head
        radius = 0.035 if "ik" in bone_name.lower() else 0.028
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=radius, location=position)
        marker = bpy.context.object
        marker.name = "CTRL_VIS_" + bone_name
        marker.data.name = marker.name + "_Mesh"
        material_key = "pole" if "pole" in bone_name.lower() else "ik" if "ik" in bone_name.lower() else "core"
        marker.data.materials.append(materials[material_key])
        constraint = marker.constraints.new("COPY_LOCATION")
        constraint.name = "Follow " + bone_name
        constraint.target = rig
        constraint.subtarget = bone_name


def set_pose_defaults(rig: bpy.types.Object) -> None:
    for pose_bone in rig.pose.bones:
        pose_bone.rotation_mode = "XYZ"
        pose_bone.location = (0.0, 0.0, 0.0)
        pose_bone.rotation_euler = (0.0, 0.0, 0.0)
        pose_bone.scale = (1.0, 1.0, 1.0)
    body_morph = rig.pose.bones.get("CTRL_body_morph")
    if body_morph is not None:
        for key in body_morph.keys():
            if key.startswith("shape_"):
                body_morph[key] = 0.0


def apply_pose(
    rig: bpy.types.Object,
    transforms: dict[str, dict[str, tuple[float, float, float]]],
    shapes: dict[str, float] | None = None,
) -> None:
    set_pose_defaults(rig)
    for bone_name, transform in transforms.items():
        pose_bone = rig.pose.bones.get(bone_name)
        if pose_bone is None:
            continue
        if "loc" in transform:
            pose_bone.location = transform["loc"]
        if "rot" in transform:
            pose_bone.rotation_euler = tuple(math.radians(value) for value in transform["rot"])
        if "scale" in transform:
            pose_bone.scale = transform["scale"]
    body_morph = rig.pose.bones.get("CTRL_body_morph")
    if body_morph is not None:
        for shape_name, value in (shapes or {}).items():
            prop_name = "shape_" + shape_name
            if prop_name in body_morph:
                body_morph[prop_name] = clamp01(value)


def key_controls(rig: bpy.types.Object, frame: int) -> None:
    for bone_name in CONTROL_BONES:
        pose_bone = rig.pose.bones.get(bone_name)
        if pose_bone is None:
            continue
        pose_bone.keyframe_insert("location", frame=frame)
        pose_bone.keyframe_insert("rotation_euler", frame=frame)
        pose_bone.keyframe_insert("scale", frame=frame)
    body_morph = rig.pose.bones.get("CTRL_body_morph")
    if body_morph is not None:
        for key in body_morph.keys():
            if key.startswith("shape_"):
                body_morph.keyframe_insert(data_path=f'["{key}"]', frame=frame)


def create_rig_pose_actions(rig: bpy.types.Object) -> dict[str, int]:
    rig.animation_data_create()
    action_frames = {}
    pose_specs = [
        (
            "LongaArma_RigPose_QuadrupedNeutral",
            20,
            {},
            {"Idle_Breath_BodySway": 0.15},
        ),
        (
            "LongaArma_RigPose_UpperBodyLift",
            24,
            {
                "CTRL_chest": {"rot": (-42.0, 0.0, 12.0)},
                "CTRL_head": {"rot": (28.0, 0.0, -16.0)},
                "CTRL_blade_ik_l": {"loc": (0.10, -0.14, 0.62), "rot": (0.0, -18.0, 0.0)},
                "CTRL_front_right_ik": {"loc": (0.06, 0.02, 0.44)},
                "CTRL_front_left_ik": {"loc": (-0.03, 0.00, 0.02)},
                "CTRL_rear_right_ik": {"loc": (-0.04, 0.00, 0.00)},
                "CTRL_rear_left_ik": {"loc": (-0.02, 0.00, 0.00)},
            },
            {"Attack_UpperBody_Rise": 1.0, "Attack_LeftBlade_SlamWindup": 1.0},
        ),
        (
            "LongaArma_RigPose_AttackSlamContact",
            28,
            {
                "CTRL_chest": {"rot": (30.0, 0.0, -10.0)},
                "CTRL_head": {"rot": (-24.0, 0.0, 8.0)},
                "CTRL_blade_ik_l": {"loc": (0.34, -0.06, -0.04), "rot": (0.0, 0.0, -24.0)},
                "CTRL_front_right_ik": {"loc": (0.26, 0.01, -0.02)},
                "CTRL_front_left_ik": {"loc": (-0.02, 0.00, 0.00)},
                "CTRL_rear_right_ik": {"loc": (-0.05, 0.01, 0.00)},
                "CTRL_rear_left_ik": {"loc": (-0.05, -0.01, 0.00)},
            },
            {"Attack_Forelimbs_ForwardSlam": 1.0, "Attack_FrontLeg_SlamDrag": 1.0, "Attack_GroundDrag_Pullback": 0.45},
        ),
        (
            "LongaArma_RigPose_DeathPuddleCheck",
            20,
            {
                "CTRL_chest": {"rot": (10.0, 0.0, 0.0)},
                "CTRL_head": {"rot": (-18.0, 0.0, 0.0)},
            },
            {"Death_Melt_FlatLiquidSpread": 1.0, "Death_Puddle_Final": 1.0},
        ),
    ]

    for action_name, frame_end, transforms, shapes in pose_specs:
        action = bpy.data.actions.new(action_name)
        action.use_fake_user = True
        action["rig_pose_only"] = True
        action["arp_export"] = True
        rig.animation_data.action = action
        bpy.context.scene.frame_set(0)
        apply_pose(rig, {}, {})
        key_controls(rig, 0)
        bpy.context.scene.frame_set(frame_end)
        apply_pose(rig, transforms, shapes)
        key_controls(rig, frame_end)
        action.frame_range = (0, frame_end)
        action_frames[action_name] = frame_end

    apply_pose(rig, {}, {})
    bpy.context.scene.frame_set(0)
    rig.animation_data.action = bpy.data.actions.get("LongaArma_RigPose_QuadrupedNeutral")
    return action_frames


def create_action(rig: bpy.types.Object, action_name: str, frame_end: int, loop: bool) -> bpy.types.Action:
    action = bpy.data.actions.new(action_name)
    action.use_fake_user = True
    action["arp_export"] = True
    action["longa_arma_final_motion"] = True
    action["longa_arma_loop"] = loop
    action.frame_range = (0, frame_end)
    rig.animation_data_create()
    rig.animation_data.action = action
    return action


def insert_motion_key(
    rig: bpy.types.Object,
    frame: int,
    transforms: dict[str, dict[str, tuple[float, float, float]]] | None = None,
    shapes: dict[str, float] | None = None,
) -> None:
    bpy.context.scene.frame_set(frame)
    apply_pose(rig, transforms or {}, shapes or {})
    key_controls(rig, frame)


def create_final_animation_actions(rig: bpy.types.Object) -> dict[str, int]:
    action_frames: dict[str, int] = {}
    builders = {
        "LongaArma_Static_Review": build_static_review_action,
        "LongaArma_Idle": build_idle_action,
        "LongaArma_Move_Crawl": build_move_crawl_action,
        "LongaArma_Attack_SlamDrag": build_attack_slam_drag_action,
        "LongaArma_Hit_Recoil": build_hit_recoil_action,
        "LongaArma_Consume_Peck": build_consume_peck_action,
        "LongaArma_Death_MeltPuddle": build_death_melt_puddle_action,
    }
    for action_name, frame_end, loop in FINAL_ACTION_SPECS:
        builders[action_name](rig, frame_end, loop)
        action_frames[action_name] = frame_end

    bpy.context.scene.frame_set(0)
    apply_pose(rig, {}, {})
    rig.animation_data.action = bpy.data.actions.get("LongaArma_Static_Review")
    return action_frames


def build_static_review_action(rig: bpy.types.Object, frame_end: int, loop: bool) -> None:
    create_action(rig, "LongaArma_Static_Review", frame_end, loop)
    insert_motion_key(rig, 0)
    insert_motion_key(rig, frame_end)


def build_idle_action(rig: bpy.types.Object, frame_end: int, loop: bool) -> None:
    create_action(rig, "LongaArma_Idle", frame_end, loop)
    for frame, breath, chest_pitch, head_yaw in [
        (0, 0.00, 0.0, 0.0),
        (8, 0.35, -2.0, -2.0),
        (16, 0.85, -4.5, 2.0),
        (24, 1.00, -5.5, 4.0),
        (32, 0.55, -2.0, -3.0),
        (40, 0.18, 1.0, 1.5),
        (48, 0.00, 0.0, 0.0),
    ]:
        insert_motion_key(
            rig,
            frame,
            {
                "CTRL_chest": {"rot": (chest_pitch, 0.0, 1.2 * math.sin(frame * 0.20))},
                "CTRL_head": {"rot": (-chest_pitch * 0.35, 0.0, head_yaw)},
                "CTRL_body_morph": {"loc": (0.0, 0.0, 0.018 * breath)},
            },
            {"Idle_Breath_BodySway": breath},
        )


def gait_target(frame: int, frame_end: int, phase: float, stride: float, lift: float, lateral: float) -> tuple[float, float, float]:
    progress = ((frame / frame_end) + phase) % 1.0
    if progress < 0.34:
        swing = smoothstep(0.0, 1.0, progress / 0.34)
        x = -stride * 0.62 + stride * 1.36 * swing
        z = math.sin(swing * math.pi) * lift
        y = math.sin(swing * math.pi) * lateral
    else:
        stance = smoothstep(0.0, 1.0, (progress - 0.34) / 0.66)
        x = stride * 0.74 - stride * 1.36 * stance
        z = -0.018 * math.sin(stance * math.pi)
        y = lateral * 0.15 * math.sin(stance * math.pi)
    return (x, y, z)


def blade_drag_target(frame: int, frame_end: int) -> tuple[float, float, float]:
    progress = ((frame / frame_end) + 0.16) % 1.0
    if progress < 0.42:
        swing = smoothstep(0.0, 1.0, progress / 0.42)
        return (-0.18 + 0.24 * swing, -0.050 * math.sin(swing * math.pi), 0.055 * math.sin(swing * math.pi) - 0.010)

    drag = smoothstep(0.0, 1.0, (progress - 0.42) / 0.58)
    return (0.06 - 0.30 * drag, -0.030 * math.sin(drag * math.pi), -0.060 - 0.020 * math.sin(drag * math.pi))


def build_move_crawl_action(rig: bpy.types.Object, frame_end: int, loop: bool) -> None:
    create_action(rig, "LongaArma_Move_Crawl", frame_end, loop)
    samples = list(range(0, frame_end + 1, 4))
    if samples[-1] != frame_end:
        samples.append(frame_end)

    for frame in samples:
        progress = frame / frame_end
        body_sway = math.sin(progress * math.pi * 2.0)
        transforms = {
            "CTRL_chest": {"rot": (-4.0 + 2.0 * body_sway, 0.0, 5.0 * math.sin(progress * math.pi * 4.0))},
            "CTRL_head": {"rot": (3.0 * body_sway, 0.0, -4.0 * math.sin(progress * math.pi * 4.0 + 0.5))},
            "CTRL_front_right_ik": {"loc": gait_target(frame, frame_end, 0.00, 0.22, 0.17, 0.020)},
            "CTRL_front_left_ik": {"loc": gait_target(frame, frame_end, 0.53, 0.18, 0.11, -0.014)},
            "CTRL_rear_right_ik": {"loc": gait_target(frame, frame_end, 0.74, 0.19, 0.13, 0.016)},
            "CTRL_rear_left_ik": {"loc": gait_target(frame, frame_end, 0.28, 0.20, 0.14, -0.018)},
            "CTRL_blade_ik_l": {"loc": blade_drag_target(frame, frame_end), "rot": (0.0, 0.0, -7.0 * math.sin(progress * math.pi * 2.0))},
        }
        shapes = {
            "Move_Crawl_AlternateStep": 0.50 + 0.50 * math.sin(progress * math.pi * 2.0),
            "Move_BladeArm_SlowDrag": 0.65 + 0.35 * math.sin((progress + 0.16) * math.pi * 2.0),
            "Move_LimpingBladeArm_Drag": 0.45 + 0.35 * math.sin((progress + 0.30) * math.pi * 2.0),
        }
        insert_motion_key(rig, frame, transforms, shapes)


def build_attack_slam_drag_action(rig: bpy.types.Object, frame_end: int, loop: bool) -> None:
    create_action(rig, "LongaArma_Attack_SlamDrag", frame_end, loop)
    key_specs = [
        (0, {}, {}),
        (
            10,
            {
                "CTRL_chest": {"rot": (-14.0, 0.0, 6.0)},
                "CTRL_head": {"rot": (12.0, 0.0, -8.0)},
                "CTRL_blade_ik_l": {"loc": (-0.02, -0.08, 0.22)},
                "CTRL_front_right_ik": {"loc": (0.02, 0.01, 0.12)},
            },
            {"Attack_UpperBody_Rise": 0.25, "Attack_LeftBlade_SlamWindup": 0.20},
        ),
        (
            24,
            {
                "CTRL_chest": {"rot": (-44.0, 0.0, 13.0)},
                "CTRL_head": {"rot": (28.0, 0.0, -16.0)},
                "CTRL_blade_ik_l": {"loc": (0.08, -0.15, 0.66), "rot": (0.0, -20.0, 8.0)},
                "CTRL_front_right_ik": {"loc": (0.08, 0.03, 0.48)},
                "CTRL_front_left_ik": {"loc": (-0.04, 0.00, 0.02)},
                "CTRL_rear_right_ik": {"loc": (-0.06, 0.01, -0.01)},
                "CTRL_rear_left_ik": {"loc": (-0.04, -0.01, -0.01)},
            },
            {"Attack_UpperBody_Rise": 1.0, "Attack_LeftBlade_SlamWindup": 1.0},
        ),
        (
            34,
            {
                "CTRL_chest": {"rot": (28.0, 0.0, -12.0)},
                "CTRL_head": {"rot": (-22.0, 0.0, 8.0)},
                "CTRL_blade_ik_l": {"loc": (0.36, -0.05, -0.05), "rot": (0.0, 0.0, -22.0)},
                "CTRL_front_right_ik": {"loc": (0.28, 0.01, -0.03)},
                "CTRL_rear_right_ik": {"loc": (-0.08, 0.02, -0.02)},
                "CTRL_rear_left_ik": {"loc": (-0.08, -0.02, -0.02)},
            },
            {"Attack_Forelimbs_ForwardSlam": 1.0, "Attack_FrontLeg_SlamDrag": 1.0},
        ),
        (
            46,
            {
                "CTRL_chest": {"rot": (14.0, 0.0, -5.0)},
                "CTRL_head": {"rot": (-10.0, 0.0, 3.0)},
                "CTRL_blade_ik_l": {"loc": (-0.18, -0.04, -0.06), "rot": (0.0, 0.0, -8.0)},
                "CTRL_front_right_ik": {"loc": (-0.05, 0.01, -0.02)},
                "CTRL_front_left_ik": {"loc": (-0.06, 0.00, 0.00)},
            },
            {"Attack_GroundDrag_Pullback": 1.0, "Attack_FrontLeg_SlamDrag": 0.50},
        ),
        (62, {}, {}),
    ]
    for frame, transforms, shapes in key_specs:
        insert_motion_key(rig, frame, transforms, shapes)


def build_hit_recoil_action(rig: bpy.types.Object, frame_end: int, loop: bool) -> None:
    create_action(rig, "LongaArma_Hit_Recoil", frame_end, loop)
    key_specs = [
        (0, {}, {}),
        (5, {"CTRL_chest": {"loc": (-0.08, 0.0, 0.02), "rot": (-8.0, 0.0, -12.0)}, "CTRL_head": {"rot": (4.0, 0.0, -28.0)}}, {"Hit_HeadBack_Flinch": 1.0, "Hit_HeadSide_Shake": 0.85}),
        (10, {"CTRL_chest": {"loc": (-0.05, 0.0, 0.01), "rot": (-4.0, 0.0, 10.0)}, "CTRL_head": {"rot": (0.0, 0.0, 24.0)}}, {"Hit_HeadBack_Flinch": 0.45, "Hit_HeadSide_Shake": 0.40}),
        (17, {"CTRL_chest": {"loc": (-0.03, 0.0, 0.0), "rot": (2.0, 0.0, -5.0)}, "CTRL_head": {"rot": (0.0, 0.0, -12.0)}}, {"Hit_HeadSide_Shake": 0.25}),
        (32, {}, {}),
    ]
    for frame, transforms, shapes in key_specs:
        insert_motion_key(rig, frame, transforms, shapes)


def build_consume_peck_action(rig: bpy.types.Object, frame_end: int, loop: bool) -> None:
    create_action(rig, "LongaArma_Consume_Peck", frame_end, loop)
    key_specs = [
        (0, {}, {}),
        (12, {"CTRL_chest": {"rot": (-16.0, 0.0, 2.0)}, "CTRL_head": {"rot": (34.0, 0.0, 0.0)}, "CTRL_mouth": {"rot": (18.0, 0.0, 0.0)}}, {"Consume_HeadBack_Windup": 1.0}),
        (20, {"CTRL_chest": {"rot": (10.0, 0.0, -2.0)}, "CTRL_head": {"rot": (-36.0, 0.0, 2.0)}, "CTRL_mouth": {"rot": (-14.0, 0.0, 0.0)}}, {"Consume_HeadForward_BiteSlam": 1.0, "Consume_Peck_Impact": 0.85}),
        (25, {"CTRL_head": {"rot": (-18.0, 0.0, -5.0)}, "CTRL_mouth": {"rot": (8.0, 0.0, 0.0)}}, {"Consume_Peck_Impact": 0.20}),
        (31, {"CTRL_head": {"rot": (-30.0, 0.0, 4.0)}, "CTRL_mouth": {"rot": (-10.0, 0.0, 0.0)}}, {"Consume_HeadForward_BiteSlam": 0.70, "Consume_Peck_Impact": 0.75}),
        (46, {}, {}),
    ]
    for frame, transforms, shapes in key_specs:
        insert_motion_key(rig, frame, transforms, shapes)


def build_death_melt_puddle_action(rig: bpy.types.Object, frame_end: int, loop: bool) -> None:
    create_action(rig, "LongaArma_Death_MeltPuddle", frame_end, loop)
    key_specs = [
        (0, {}, {}),
        (16, {"CTRL_chest": {"rot": (12.0, 0.0, -8.0)}, "CTRL_head": {"rot": (-22.0, 0.0, 8.0)}, "CTRL_blade_ik_l": {"loc": (-0.08, -0.03, -0.04)}}, {"Death_Melt_FlatLiquidSpread": 0.30}),
        (34, {"CTRL_chest": {"loc": (0.0, 0.0, -0.18), "rot": (24.0, 0.0, 3.0), "scale": (1.10, 0.82, 0.72)}, "CTRL_head": {"loc": (-0.04, 0.0, -0.16), "rot": (-38.0, 0.0, -4.0)}, "CTRL_blade_ik_l": {"loc": (-0.16, -0.02, -0.12)}}, {"Death_Melt_FlatLiquidSpread": 0.90, "Death_Puddle_Final": 0.25}),
        (50, {"CTRL_chest": {"loc": (0.0, 0.0, -0.30), "rot": (8.0, 0.0, 0.0), "scale": (1.24, 0.62, 0.42)}, "CTRL_head": {"loc": (-0.08, 0.0, -0.26), "rot": (-62.0, 0.0, 0.0)}, "CTRL_blade_ik_l": {"loc": (-0.20, -0.01, -0.18)}}, {"Death_Melt_FlatLiquidSpread": 1.0, "Death_Puddle_Final": 0.70}),
        (66, {"CTRL_chest": {"loc": (0.0, 0.0, -0.36), "scale": (1.35, 0.46, 0.28)}, "CTRL_head": {"loc": (-0.10, 0.0, -0.32), "rot": (-72.0, 0.0, 0.0)}, "CTRL_blade_ik_l": {"loc": (-0.24, 0.0, -0.22)}}, {"Death_Melt_FlatLiquidSpread": 1.0, "Death_Puddle_Final": 1.0}),
    ]
    for frame, transforms, shapes in key_specs:
        insert_motion_key(rig, frame, transforms, shapes)


def configure_arp_assist(rig: bpy.types.Object, action_frames: dict[str, int]) -> dict[str, object]:
    scene = bpy.context.scene
    metadata: dict[str, object] = {
        "addonEnabled": "bl_ext.user_default.auto_rig_pro" in bpy.context.preferences.addons,
        "operatorNamespaceAvailable": hasattr(bpy.ops, "arp"),
        "backgroundMode": bool(bpy.app.background),
        "usedFor": [
            "detected installed Auto-Rig Pro extension",
            "registered final animation and rig-pose actions with arp_export flags",
            "set Game Engine Export scene properties where available",
            "recorded which ARP UI operators were unsafe in background mode",
        ],
        "operatorResults": [],
        "limitations": [
            "Auto-Rig Pro Smart marker one-click rigging was not applied because Longa Arma is a non-humanoid asymmetric quadruped.",
            "ARP append_arp presets require a 3D View UI context in this Blender install and fail in background mode with bpy.context.space_data.overlay access.",
        ],
    }

    if not metadata["operatorNamespaceAvailable"]:
        return metadata

    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig

    for prop_name, value in [
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
        if hasattr(scene, prop_name):
            try:
                setattr(scene, prop_name, value)
            except Exception as exc:
                metadata["operatorResults"].append({"operation": "set_scene_prop:" + prop_name, "ok": False, "error": str(exc)})

    for action_name in action_frames:
        action = bpy.data.actions.get(action_name)
        if action is not None:
            action["arp_export"] = True

    if hasattr(scene, "arp_export_actlist"):
        try:
            while len(scene.arp_export_actlist):
                scene.arp_export_actlist.remove(0)
            action_list = scene.arp_export_actlist.add()
            action_list.name = "LongaArmaDetailedRigPoseSet"
            action_list.exportable = True
            for action_name in action_frames:
                item = action_list.actions.add()
                item.action = bpy.data.actions.get(action_name)
            metadata["actionManagerListName"] = "LongaArmaDetailedRigPoseSet"
        except Exception as exc:
            metadata["operatorResults"].append({"operation": "scene.arp_export_actlist", "ok": False, "error": str(exc)})

    if bpy.app.background:
        metadata["operatorResults"].append(
            {
                "operation": "ui_dependent_arp_operators",
                "ok": False,
                "skipped": True,
                "error": "Skipped in background mode after append_arp and UI/icon-dependent ARP operators proved unsafe without a 3D View context.",
            }
        )
        return metadata

    operator_calls = [
        ("layers_add_defaults", lambda: bpy.ops.arp.layers_add_defaults()),
        ("assign_colors", lambda: bpy.ops.arp.assign_colors()),
        ("set_custom_shape", lambda: bpy.ops.arp.set_custom_shape(custom_shape_name="cs_box", scale=1.05)),
        ("check_rig_export", lambda: bpy.ops.arp.check_rig_export()),
        ("ge_save_preset", lambda: bpy.ops.arp.ge_save_preset(preset_name="LongaArmaDetailedRigPoseSet", filepath=str(OUTPUT_ROOT / "exports" / "longa_arma_arp_ge_preset.py"))),
        ("export_config", lambda: bpy.ops.arp.export_config(filepath=str(OUTPUT_ROOT / "exports" / "longa_arma_arp_export_config.bmap"))),
    ]
    for operation, callback in operator_calls:
        try:
            result = callback()
            metadata["operatorResults"].append({"operation": operation, "ok": True, "result": str(result)})
        except Exception as exc:
            metadata["operatorResults"].append({"operation": operation, "ok": False, "error": str(exc)[:400]})

    return metadata


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def configure_render_scene(mesh: bpy.types.Object) -> bpy.types.Object:
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE"
    except Exception:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = RENDER_WIDTH
    scene.render.resolution_y = RENDER_HEIGHT
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0.35

    for obj in list(bpy.data.objects):
        if obj.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(obj, do_unlink=True)

    bounds = mesh_bounds(mesh)
    center = bounds["center"]
    camera_data = bpy.data.cameras.new("LongaArma_RigReview_Camera")
    camera = bpy.data.objects.new("LongaArma_RigReview_Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    scene.camera = camera
    camera.data.lens = 58
    camera.data.dof.use_dof = False

    key_data = bpy.data.lights.new("LongaArma_RigReview_Key", "AREA")
    key = bpy.data.objects.new("LongaArma_RigReview_Key", key_data)
    bpy.context.collection.objects.link(key)
    key.location = (center.x - 2.4, center.y - 2.0, center.z + 3.0)
    key.data.energy = 520
    key.data.size = 4.2

    fill_data = bpy.data.lights.new("LongaArma_RigReview_Fill", "POINT")
    fill = bpy.data.objects.new("LongaArma_RigReview_Fill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = (center.x + 1.5, center.y + 2.0, center.z + 1.4)
    fill.data.energy = 70
    return camera


def render_pose(mesh: bpy.types.Object, rig: bpy.types.Object, action_name: str, frame: int, file_name: str, direction: Vector) -> str:
    camera = bpy.context.scene.camera
    if camera is None:
        camera = configure_render_scene(mesh)
    action = bpy.data.actions.get(action_name)
    rig.animation_data_create()
    rig.animation_data.action = action
    if action is not None and hasattr(rig.animation_data, "action_slot") and len(action.slots):
        rig.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)
    bounds = evaluated_mesh_bounds(mesh)
    center = bounds["center"]
    distance = max(bounds["size"].x, bounds["size"].y, bounds["size"].z) * 2.35
    direction = direction.normalized()
    camera.location = center + direction * distance + Vector((0.0, 0.0, bounds["size"].z * 0.28))
    look_at(camera, center + Vector((0.0, 0.0, bounds["size"].z * 0.20)))
    output = RENDER_ROOT / file_name
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    return str(output.relative_to(OUTPUT_ROOT)).replace("\\", "/")


def export_rigged_assets(mesh: bpy.types.Object, rig: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(
        filepath=str(RIGGED_FBX_PATH),
        use_selection=True,
        object_types={"MESH", "ARMATURE"},
        apply_scale_options="FBX_SCALE_ALL",
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_all_actions=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
    )
    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.gltf(
        filepath=str(RIGGED_GLB_PATH),
        export_format="GLB",
        use_selection=True,
        export_materials="EXPORT",
        export_animations=True,
        export_animation_mode="ACTIONS",
        export_force_sampling=True,
        export_morph=True,
        export_morph_animation=True,
    )


def validate_rig(mesh: bpy.types.Object, rig: bpy.types.Object, action_frames: dict[str, int]) -> dict[str, object]:
    deform_count = len([bone for bone in rig.data.bones if bone.use_deform])
    control_count = len([bone for bone in rig.data.bones if not bone.use_deform])
    ik_constraints = []
    copy_constraints = []
    for pose_bone in rig.pose.bones:
        for constraint in pose_bone.constraints:
            if constraint.type == "IK":
                ik_constraints.append(pose_bone.name + ":" + constraint.name)
            if constraint.type.startswith("COPY"):
                copy_constraints.append(pose_bone.name + ":" + constraint.name)

    shape_driver_count = 0
    if mesh.data.shape_keys is not None and mesh.data.shape_keys.animation_data is not None:
        drivers = mesh.data.shape_keys.animation_data.drivers or []
        shape_driver_count = len(drivers)

    return {
        "rigName": rig.name,
        "meshName": mesh.name,
        "deformBoneCount": deform_count,
        "controlBoneCount": control_count,
        "totalBoneCount": len(rig.data.bones),
        "ikConstraintCount": len(ik_constraints),
        "ikConstraints": ik_constraints,
        "copyConstraintCount": len(copy_constraints),
        "shapeKeyDriverCount": shape_driver_count,
        "vertexGroupCount": len(mesh.vertex_groups),
        "armatureModifier": [modifier.name for modifier in mesh.modifiers if modifier.type == "ARMATURE"],
        "animationActions": action_frames,
    }


def write_status_doc(report: dict[str, object]) -> None:
    possible = report["possible"]
    not_possible = report["notPossible"]
    validation = report["validation"]
    arp = report["autoRigPro"]
    operator_lines = []
    for result in arp.get("operatorResults", []):
        state = "OK" if result.get("ok") else "FAIL"
        operator_lines.append(f"- {state}: `{result.get('operation')}` - `{result.get('result') or result.get('error')}`")
    operator_text = "\n".join(operator_lines) if operator_lines else "- recorded operator result 없음"

    RIG_STATUS_PATH.write_text(
        "# Longa Arma Runtime Lowpoly Rigging Status - 2026-07-03\n\n"
        "## Result\n\n"
        f"- Rig: `{validation['rigName']}`\n"
        f"- Deform bones: {validation['deformBoneCount']}\n"
        f"- Control bones: {validation['controlBoneCount']}\n"
        f"- IK constraints: {validation['ikConstraintCount']}\n"
        f"- Shape Key drivers: {validation['shapeKeyDriverCount']}\n"
        f"- Animation actions: `{', '.join(validation['animationActions'].keys())}`\n\n"
        "## Possible\n\n"
        + "".join(f"- {item}\n" for item in possible)
        + "\n## Not Possible / Not Completed\n\n"
        + "".join(f"- {item}\n" for item in not_possible)
        + "\n## Auto-Rig Pro Operator Log\n\n"
        + operator_text
        + "\n",
        encoding="utf-8",
    )


def main() -> None:
    ensure_dirs()
    mesh = load_mesh()
    discard_previous_runtime_rig(mesh)
    rig = create_detailed_rig(mesh)
    final_action_frames = create_final_animation_actions(rig)
    rig_pose_frames = create_rig_pose_actions(rig)
    action_frames = {**final_action_frames, **rig_pose_frames}
    arp_metadata = configure_arp_assist(rig, action_frames)
    configure_render_scene(mesh)

    renders = [
        render_pose(mesh, rig, "LongaArma_Static_Review", 30, "animation_static_review.png", Vector((-1.0, -0.42, 0.08))),
        render_pose(mesh, rig, "LongaArma_Idle", 24, "animation_idle_body_morph.png", Vector((-1.0, -0.42, 0.08))),
        render_pose(mesh, rig, "LongaArma_Move_Crawl", 16, "animation_move_crawl.png", Vector((-1.0, -0.52, 0.10))),
        render_pose(mesh, rig, "LongaArma_Attack_SlamDrag", 34, "animation_attack_slamdrag_contact.png", Vector((-1.0, -0.52, 0.10))),
        render_pose(mesh, rig, "LongaArma_Hit_Recoil", 8, "animation_hit_recoil.png", Vector((-1.0, -0.42, 0.12))),
        render_pose(mesh, rig, "LongaArma_Consume_Peck", 20, "animation_consume_peck.png", Vector((-1.0, -0.42, 0.12))),
        render_pose(mesh, rig, "LongaArma_Death_MeltPuddle", 66, "animation_death_meltpuddle.png", Vector((-1.0, -0.48, 0.18))),
        render_pose(mesh, rig, "LongaArma_RigPose_QuadrupedNeutral", 20, "rig_overview_quadruped_neutral.png", Vector((-1.0, -0.42, 0.08))),
        render_pose(mesh, rig, "LongaArma_RigPose_UpperBodyLift", 24, "rig_upper_body_lift_test.png", Vector((-1.0, -0.52, 0.12))),
        render_pose(mesh, rig, "LongaArma_RigPose_AttackSlamContact", 28, "rig_attack_slam_contact_test.png", Vector((-1.0, -0.52, 0.10))),
        render_pose(mesh, rig, "LongaArma_RigPose_DeathPuddleCheck", 20, "rig_death_puddle_morph_test.png", Vector((-1.0, -0.48, 0.18))),
    ]
    export_rigged_assets(mesh, rig)
    validation = validate_rig(mesh, rig, action_frames)

    report = {
        "createdAt": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "sourceBlend": str(BLEND_PATH.relative_to(OUTPUT_ROOT)).replace("\\", "/"),
        "savedBlend": str(BLEND_PATH.relative_to(OUTPUT_ROOT)).replace("\\", "/"),
        "exports": [
            str(RIGGED_FBX_PATH.relative_to(OUTPUT_ROOT)).replace("\\", "/"),
            str(RIGGED_GLB_PATH.relative_to(OUTPUT_ROOT)).replace("\\", "/"),
        ],
        "renders": renders,
        "finalAnimationActions": final_action_frames,
        "rigPoseActions": rig_pose_frames,
        "validation": validation,
        "autoRigPro": arp_metadata,
        "possible": [
            "Created a replacement detailed Blender armature for the current runtime_lowpoly model.",
            "Separated deform bones and animator control bones with DEF_/CTRL_ naming.",
            "Added four independent leg IK targets and pole targets for quadruped crawl posing.",
            "Added left blade-arm IK target and pole target for lift, slam, and floor-drag posing.",
            "Added chest, pelvis, head, mouth, spine-lift, and body-morph controls needed by the requested motion set.",
            "Connected existing Shape Keys to CTRL_body_morph custom properties for body breathing, attack morph, consume peck, hit recoil, and death melt checks.",
            "Generated final static, idle, move, attack, hit, consume, and death Blender Actions on the detailed rig.",
            "Generated animation preview renders and rig-only review renders.",
            "Registered final animation and rig-pose actions with Auto-Rig Pro export flags and Game Engine Export scene properties where available.",
            "Exported rigged FBX and GLB review files.",
            "Prepared the rigged FBX and final animation states for the separate Unity refresh/bridge application step.",
        ],
        "notPossible": [
            "Auto-Rig Pro append_arp preset creation could not be executed in background mode because the add-on requires a 3D View UI overlay context.",
            "Auto-Rig Pro UI-dependent layer/color/custom-shape/export operators were skipped in the final background run after a native Blender crash during direct operator use.",
            "Auto-Rig Pro Smart marker automatic rigging was not used; this non-humanoid asymmetric quadruped needs a custom control layout.",
            "Death puddle visual quality is still provisional and may require shape-key sculpt cleanup after Unity review.",
            "Legacy generated .anim and controller files from the previous pass remain on disk, but the current scene placement uses the new final clip/controller names.",
            "Unity application is not performed by this Blender generation script; it is recorded by the separate approved bridge run.",
            "Harness, EditMode, PlayMode, Build, and Git commands were not run.",
        ],
    }

    RIG_REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    write_status_doc(report)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print("LONGA_ARMA_DETAILED_RIG_CREATED")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
