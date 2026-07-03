from __future__ import annotations

import importlib.util
import json
import math
import random
import shutil
from datetime import date
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[1]
BASE_SCRIPT = PROJECT_ROOT / "scripts" / "GenerateParvumSample.py"
SAMPLE_NAME = "parvum_physics_rig_sample"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / SAMPLE_NAME
BLENDER_DIR = SAMPLE_ROOT / "blender"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
TEXTURE_DIR = SAMPLE_ROOT / "textures"


def load_base_module():
    spec = importlib.util.spec_from_file_location("parvum_base_sample", BASE_SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load base parvum sample script: {BASE_SCRIPT}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.SAMPLE_NAME = SAMPLE_NAME
    module.SAMPLE_ROOT = SAMPLE_ROOT
    module.BLENDER_DIR = BLENDER_DIR
    module.RENDER_DIR = RENDER_DIR
    module.EXPORT_DIR = EXPORT_DIR
    module.TEXTURE_DIR = TEXTURE_DIR
    module.LEGACY_GIF_DIR = SAMPLE_ROOT / "animations"
    module.LEGACY_FRAME_DIR = SAMPLE_ROOT / "animation_frames"
    return module


base = load_base_module()


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, BLENDER_DIR, RENDER_DIR, EXPORT_DIR, TEXTURE_DIR):
        path.mkdir(parents=True, exist_ok=True)


def clean_generated_files() -> None:
    for directory in (BLENDER_DIR, RENDER_DIR, EXPORT_DIR, TEXTURE_DIR):
        if not directory.exists():
            continue
        for item in directory.iterdir():
            if item.is_file():
                item.unlink()
            elif item.is_dir():
                shutil.rmtree(item)
    for path in (
        SAMPLE_ROOT / "index.html",
        SAMPLE_ROOT / "README.md",
        SAMPLE_ROOT / "ASSET_MANIFEST.json",
        SAMPLE_ROOT / "APPROVAL_STATUS.json",
        SAMPLE_ROOT / "TEXTURE_ANALYSIS.md",
        SAMPLE_ROOT / "PHYSICS_RIG_NOTES.md",
    ):
        path.unlink(missing_ok=True)


def add_deformable_sphere(
    name: str,
    parent: bpy.types.Object,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    *,
    segments: int = 96,
    rings: int = 48,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    obj.parent = parent
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.object.shade_smooth()
    return obj


def add_shape_keys(obj: bpy.types.Object, *, role_scale: float = 1.0) -> None:
    obj.shape_key_add(name="Basis")
    basis = [vertex.co.copy() for vertex in obj.data.vertices]

    def write_key(name: str, transform) -> None:
        key = obj.shape_key_add(name=name)
        for index, point in enumerate(key.data):
            point.co = transform(basis[index].copy())
        key.value = 0.0

    def ripple_amount(co: Vector) -> float:
        return math.sin(co.x * 24.0 + co.y * 11.0) * 0.010 * role_scale

    write_key(
        "Idle_Pulse_Surface_Jiggle",
        lambda co: Vector((co.x * 1.018, co.y * 1.012, co.z * 1.030 + ripple_amount(co))),
    )
    write_key(
        "Move_Squash_Forward_Slosh",
        lambda co: Vector((co.x * 1.075, co.y * 0.935 - max(0.0, -co.y) * 0.040, co.z * 0.890 + ripple_amount(co) * 0.55)),
    )
    write_key(
        "Attack_Bite_Core_Kick",
        lambda co: Vector((co.x * 0.990, co.y - max(0.0, -co.y) * 0.075, co.z * 1.035 + ripple_amount(co) * 0.45)),
    )
    write_key(
        "Hit_Recoil_Side_Wave",
        lambda co: Vector((co.x + (0.018 if co.x > 0.0 else -0.006), co.y * 0.965 + max(0.0, -co.y) * 0.030, co.z * 0.980)),
    )
    write_key(
        "Death_Flatten_Liquid_Spread",
        lambda co: Vector((co.x * 1.260, co.y * 1.180, co.z * 0.350 - 0.020)),
    )


def set_shape_key_values(obj: bpy.types.Object, values: dict[str, float]) -> None:
    if obj.data is None or not hasattr(obj.data, "shape_keys") or obj.data.shape_keys is None:
        return
    for key in obj.data.shape_keys.key_blocks:
        if key.name != "Basis":
            key.value = values.get(key.name, 0.0)


def material_set() -> dict[str, bpy.types.Material]:
    textures = base.create_procedural_textures()
    mats = base.build_materials(textures)
    mats["proxy_body"] = base.material("transparent blue rigidbody proxy", (0.08, 0.55, 1.0, 0.18), roughness=0.26, alpha=0.18)
    mats["proxy_joint"] = base.material("transparent amber joint range proxy", (1.0, 0.58, 0.08, 0.26), roughness=0.34, alpha=0.26)
    mats["anchor"] = base.material(
        "yellow physics anchor marker",
        (1.0, 0.84, 0.10, 1.0),
        roughness=0.32,
        emission=(1.0, 0.78, 0.12, 1.0),
        emission_strength=0.35,
    )
    mats["rig_line"] = base.material("cyan joint link line", (0.20, 0.95, 1.0, 1.0), roughness=0.22)
    mats["label"] = base.material("warm sample label text", (0.96, 0.90, 0.74, 1.0), roughness=0.48)
    mats["gum"] = base.noisy_material(
        "dark wet red gum tissue",
        (0.22, 0.045, 0.032, 1.0),
        high=(0.62, 0.15, 0.08, 1.0),
        roughness=0.32,
        scale=22.0,
        detail=9.0,
    )
    mats["fleck"] = base.material("chalky off white residue patch", (0.82, 0.84, 0.68, 1.0), roughness=0.78)
    mats["internal_green"] = base.material("dark translucent internal green swirl", (0.0, 0.18, 0.08, 0.62), roughness=0.38, alpha=0.62)
    return mats


def add_label(parent: bpy.types.Object, text: str, loc: tuple[float, float, float], mats: dict[str, bpy.types.Material]) -> bpy.types.Object:
    bpy.ops.object.text_add(location=loc, rotation=(math.radians(68.0), 0.0, 0.0))
    obj = bpy.context.object
    obj.name = "label " + text
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = 0.023
    obj.data.materials.append(mats["label"])
    obj.parent = parent
    return obj


def add_flat_fleck(
    parent: bpy.types.Object,
    name: str,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    rot: tuple[float, float, float],
    mats: dict[str, bpy.types.Material],
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mats["fleck"])
    obj.parent = parent
    bpy.ops.object.shade_smooth()
    return obj


def add_embedded_tooth(
    parent: bpy.types.Object,
    name: str,
    loc: tuple[float, float, float],
    mats: dict[str, bpy.types.Material],
    *,
    length: float,
    radius: float,
    points_down: bool,
    x_lean: float = 0.0,
) -> bpy.types.Object:
    radius1, radius2 = (0.0, radius) if points_down else (radius, 0.0)
    bpy.ops.mesh.primitive_cone_add(vertices=11, radius1=radius1, radius2=radius2, depth=length, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler[0] = x_lean
    obj.data.materials.append(mats["tooth"])
    obj.parent = parent
    bpy.ops.object.shade_smooth()
    return obj


def add_proxy_sphere(
    parent: bpy.types.Object,
    name: str,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
) -> bpy.types.Object:
    obj = base.add_uv_sphere(name, parent, loc, scale, mat, segments=48, rings=18)
    obj.display_type = "WIRE"
    obj.show_wire = True
    obj.show_in_front = True
    obj["UnityMotionRole"] = "PhysicsProxy"
    return obj


def add_anchor(parent: bpy.types.Object, name: str, loc: tuple[float, float, float], mats: dict[str, bpy.types.Material]) -> bpy.types.Object:
    obj = base.add_uv_sphere(name, parent, loc, (0.014, 0.014, 0.014), mats["anchor"], segments=16, rings=8)
    obj["UnityMotionRole"] = "JointAnchor"
    return obj


def add_control_armature(parent: bpy.types.Object) -> bpy.types.Object:
    bpy.ops.object.armature_add(enter_editmode=True, location=(0.0, 0.0, 0.0))
    armature = bpy.context.object
    armature.name = "parvum physics control armature"
    armature.data.name = "ParvumPhysicsControlArmature"
    armature.show_in_front = True
    armature.parent = parent

    bones = armature.data.edit_bones
    root = bones[0]
    root.name = "Root_Body"
    root.head = (0.0, 0.0, 0.030)
    root.tail = (0.0, 0.0, 0.205)

    def new_bone(name: str, head: tuple[float, float, float], tail: tuple[float, float, float], parent_bone) -> None:
        bone = bones.new(name)
        bone.head = head
        bone.tail = tail
        bone.parent = parent_bone
        bone.use_connect = False

    new_bone("Body_Core_Slosh", (0.0, -0.005, 0.160), (0.0, -0.015, 0.370), root)
    new_bone("Left_Lobe_Jiggle", (-0.118, -0.020, 0.090), (-0.244, -0.030, 0.135), root)
    new_bone("Right_Lobe_Jiggle", (0.118, -0.020, 0.090), (0.244, -0.030, 0.135), root)
    new_bone("Rear_Mass_Lag", (0.0, 0.085, 0.115), (0.0, 0.250, 0.160), root)
    new_bone("Mouth_Root_Drive", (0.0, -0.205, 0.180), (0.0, -0.365, 0.190), root)
    new_bone("Upper_Jaw_Limited", (0.0, -0.350, 0.228), (0.0, -0.430, 0.258), root)
    new_bone("Lower_Jaw_Limited", (0.0, -0.350, 0.145), (0.0, -0.430, 0.118), root)
    new_bone("Tongue_Tip_Follow", (0.0, -0.400, 0.158), (0.0, -0.486, 0.150), root)
    bpy.ops.object.mode_set(mode="OBJECT")
    armature["UnityMotionRole"] = "ControlBonesForJiggleIKJointMotion"
    return armature


def build_model(root: bpy.types.Object, mats: dict[str, bpy.types.Material]) -> dict[str, object]:
    floor = base.add_box("dark neutral sample floor", root, (0.0, 0.0, -0.010), (1.35, 1.18, 0.020), mats["floor"])
    model_root = base.add_empty("parvum physics ready model root", root)

    body_core = add_deformable_sphere(
        "Body_Core low translucent mound with shape keys",
        model_root,
        (0.0, 0.008, 0.205),
        (0.315, 0.335, 0.245),
        mats["slime"],
    )
    add_shape_keys(body_core, role_scale=1.0)
    base.add_noise_displace(body_core, "large slow slime surface undulation", strength=0.014, scale=0.68, detail=2.4)

    outer_skin = add_deformable_sphere(
        "Outer_Gel_Skin transparent layered surface",
        model_root,
        (0.0, 0.006, 0.212),
        (0.331, 0.352, 0.252),
        mats["outer_slime"],
        segments=96,
        rings=42,
    )
    add_shape_keys(outer_skin, role_scale=0.65)
    base.add_noise_displace(outer_skin, "fine glossy slime meniscus displacement", strength=0.006, scale=0.42, detail=3.6)

    puddle_parts = [
        base.add_uv_sphere("thin front floor slime skirt", model_root, (0.0, -0.135, 0.018), (0.358, 0.285, 0.015), mats["puddle"], segments=64, rings=12),
        base.add_uv_sphere("thin rear floor slime skirt", model_root, (0.0, 0.168, 0.017), (0.328, 0.230, 0.014), mats["puddle"], segments=64, rings=12),
        base.add_uv_sphere("thin left floor slime skirt", model_root, (-0.218, -0.020, 0.019), (0.190, 0.285, 0.014), mats["puddle"], segments=56, rings=12),
        base.add_uv_sphere("thin right floor slime skirt", model_root, (0.218, -0.020, 0.019), (0.190, 0.285, 0.014), mats["puddle"], segments=56, rings=12),
    ]

    left_lobe = add_deformable_sphere("Left_Lobe separate jiggle mass", model_root, (-0.235, -0.018, 0.135), (0.170, 0.260, 0.120), mats["slime"], segments=72, rings=28)
    right_lobe = add_deformable_sphere("Right_Lobe separate jiggle mass", model_root, (0.235, -0.018, 0.135), (0.170, 0.260, 0.120), mats["slime"], segments=72, rings=28)
    rear_mass = add_deformable_sphere("Rear_Mass delayed heavy slime lobe", model_root, (0.0, 0.235, 0.158), (0.250, 0.185, 0.135), mats["slime"], segments=72, rings=28)
    front_cradle = add_deformable_sphere("Front_Mouth_Cradle slime bridge into lip", model_root, (0.0, -0.250, 0.175), (0.205, 0.108, 0.122), mats["slime"], segments=72, rings=24)
    upper_mantle = add_deformable_sphere("Upper_Lip_Covered_By_Slime_Mantle", model_root, (0.0, -0.287, 0.252), (0.182, 0.090, 0.058), mats["slime"], segments=64, rings=20)
    lower_throat = add_deformable_sphere("Lower_Mouth_Throat_Slime_Connection", model_root, (0.0, -0.332, 0.126), (0.170, 0.104, 0.062), mats["slime"], segments=64, rings=20)
    side_cowl = add_deformable_sphere("Side_Mouth_Slime_Cowl_Connects_Lip_To_Body", model_root, (0.0, -0.348, 0.184), (0.155, 0.068, 0.112), mats["outer_slime"], segments=64, rings=20)

    for obj, amount, scale in (
        (left_lobe, 0.010, 0.58),
        (right_lobe, 0.010, 0.58),
        (rear_mass, 0.012, 0.62),
        (front_cradle, 0.008, 0.50),
        (upper_mantle, 0.006, 0.45),
        (lower_throat, 0.007, 0.47),
        (side_cowl, 0.005, 0.43),
    ):
        add_shape_keys(obj, role_scale=0.55)
        base.add_noise_displace(obj, obj.name + " local gel deformation", strength=amount, scale=scale, detail=2.2)

    mouth_root = base.add_empty("Mouth_Root integrated forward bite assembly", model_root)
    snout = add_deformable_sphere(
        "Scaly_Snout protruding but embedded in slime",
        mouth_root,
        (0.0, -0.392, 0.244),
        (0.132, 0.052, 0.043),
        mats["snout"],
        segments=72,
        rings=28,
    )
    base.add_noise_displace(snout, "scaly snout raised pore displacement", strength=0.006, scale=0.115, detail=5.2)

    lip_ring = base.add_torus(
        "Oval_Mouth_Lip_Ring embedded in scaly snout",
        mouth_root,
        (0.0, -0.421, 0.178),
        mats["snout"],
        major_radius=0.092,
        minor_radius=0.012,
        scale=(1.12, 0.50, 0.85),
        rot=(math.radians(90.0), 0.0, 0.0),
    )
    mouth_cavity = add_deformable_sphere("Deep_Black_Wet_Mouth_Cavity", mouth_root, (0.0, -0.435, 0.177), (0.110, 0.012, 0.079), mats["mouth"], segments=48, rings=20)
    tongue = add_deformable_sphere("Red_Tongue low inside mouth", mouth_root, (0.0, -0.455, 0.143), (0.064, 0.020, 0.017), mats["tongue"], segments=48, rings=16)
    upper_gum = add_deformable_sphere("Upper_Dark_Tooth_Root_Shadow", mouth_root, (0.0, -0.432, 0.219), (0.098, 0.014, 0.012), mats["mouth"], segments=40, rings=12)
    lower_gum = add_deformable_sphere("Lower_Dark_Tooth_Root_Shadow", mouth_root, (0.0, -0.432, 0.131), (0.098, 0.014, 0.012), mats["mouth"], segments=40, rings=12)

    tooth_objects: list[bpy.types.Object] = []
    top_xs = [-0.084, -0.068, -0.052, -0.035, -0.018, 0.000, 0.019, 0.038, 0.056, 0.073, 0.088]
    bottom_xs = [-0.078, -0.058, -0.038, -0.018, 0.002, 0.022, 0.044, 0.066, 0.084]
    for index, x in enumerate(top_xs, start=1):
        tooth_objects.append(
            add_embedded_tooth(
                mouth_root,
                f"Upper embedded irregular tooth {index:02d}",
                (x, -0.438, 0.213 - abs(x) * 0.020),
                mats,
                length=0.083 + (0.016 if index in (3, 6, 9) else 0.000),
                radius=0.0058,
                points_down=True,
                x_lean=math.radians(-4.0 + index * 0.8),
            )
        )
    for index, x in enumerate(bottom_xs, start=1):
        tooth_objects.append(
            add_embedded_tooth(
                mouth_root,
                f"Lower embedded irregular tooth {index:02d}",
                (x, -0.438, 0.144 + abs(x) * 0.014),
                mats,
                length=0.069 + (0.012 if index in (2, 6) else 0.000),
                radius=0.0056,
                points_down=False,
                x_lean=math.radians(3.5 - index * 0.7),
            )
        )

    for index, x in enumerate((-0.044, 0.044), start=1):
        base.add_uv_sphere(f"dark nostril recessed pore {index}", mouth_root, (x, -0.438, 0.259), (0.009, 0.003, 0.005), mats["scale_dark"], segments=16, rings=8)

    random.seed(6281)
    for index in range(48):
        x = random.uniform(-0.128, 0.128)
        z = random.uniform(0.218, 0.268)
        y = -0.440 + random.uniform(-0.002, 0.005)
        base.add_uv_sphere(f"raised snout scale pore {index + 1:02d}", mouth_root, (x, y, z), (0.0037, 0.0012, 0.0024), mats["scale_dark"], segments=8, rings=5)

    swirl_lines = [
        [(-0.190, -0.205, 0.116), (-0.108, -0.155, 0.175), (0.010, -0.120, 0.205), (0.140, -0.080, 0.174)],
        [(-0.166, 0.030, 0.146), (-0.060, 0.060, 0.225), (0.064, 0.040, 0.266), (0.174, 0.010, 0.205)],
        [(-0.120, 0.160, 0.136), (-0.036, 0.222, 0.198), (0.066, 0.212, 0.216), (0.150, 0.168, 0.170)],
        [(-0.090, -0.038, 0.286), (-0.022, -0.012, 0.335), (0.062, -0.034, 0.320), (0.116, -0.076, 0.270)],
    ]
    swirl_objects = [base.add_curve(f"dark green internal slime swirl {index}", model_root, points, mats["internal_green"], bevel_depth=0.0032) for index, points in enumerate(swirl_lines, start=1)]

    flecks = [
        add_flat_fleck(model_root, "flush chalky residue front right 01", (0.148, -0.230, 0.228), (0.031, 0.003, 0.014), (0.08, 0.38, -0.15), mats),
        add_flat_fleck(model_root, "flush chalky residue front left 02", (-0.165, -0.188, 0.132), (0.022, 0.003, 0.012), (-0.18, -0.42, 0.12), mats),
        add_flat_fleck(model_root, "flush chalky residue upper 03", (0.030, -0.055, 0.382), (0.042, 0.004, 0.016), (0.22, 0.08, 0.34), mats),
        add_flat_fleck(model_root, "flush chalky residue right lobe 04", (0.262, -0.020, 0.176), (0.026, 0.003, 0.014), (0.04, -0.92, 0.15), mats),
        add_flat_fleck(model_root, "flush chalky residue rear high 05", (-0.050, 0.248, 0.278), (0.038, 0.004, 0.016), (-0.18, 0.10, -0.25), mats),
        add_flat_fleck(model_root, "flush chalky residue rear right 06", (0.170, 0.210, 0.170), (0.025, 0.004, 0.013), (0.16, -0.36, 0.40), mats),
    ]

    rig_root = base.add_empty("Physics_Rig_Overlay visual planning root", root)
    armature = add_control_armature(rig_root)
    proxies = [
        add_proxy_sphere(rig_root, "Proxy_Body_Core main Rigidbody collider", (0.0, 0.006, 0.205), (0.330, 0.355, 0.252), mats["proxy_body"]),
        add_proxy_sphere(rig_root, "Proxy_Left_Lobe jiggle collider", (-0.235, -0.018, 0.135), (0.174, 0.264, 0.123), mats["proxy_body"]),
        add_proxy_sphere(rig_root, "Proxy_Right_Lobe jiggle collider", (0.235, -0.018, 0.135), (0.174, 0.264, 0.123), mats["proxy_body"]),
        add_proxy_sphere(rig_root, "Proxy_Rear_Mass delayed collider", (0.0, 0.235, 0.158), (0.254, 0.190, 0.138), mats["proxy_body"]),
        add_proxy_sphere(rig_root, "Proxy_Mouth_Root limited bite collider", (0.0, -0.392, 0.195), (0.158, 0.078, 0.095), mats["proxy_joint"]),
    ]

    anchors = [
        add_anchor(rig_root, "Anchor_Root_Body", (0.0, 0.0, 0.205), mats),
        add_anchor(rig_root, "Anchor_Body_Core_Top", (0.0, -0.010, 0.385), mats),
        add_anchor(rig_root, "Anchor_Left_Lobe", (-0.250, -0.020, 0.145), mats),
        add_anchor(rig_root, "Anchor_Right_Lobe", (0.250, -0.020, 0.145), mats),
        add_anchor(rig_root, "Anchor_Rear_Mass", (0.0, 0.275, 0.168), mats),
        add_anchor(rig_root, "Anchor_Mouth_Root", (0.0, -0.365, 0.190), mats),
        add_anchor(rig_root, "Anchor_Upper_Jaw", (0.0, -0.424, 0.246), mats),
        add_anchor(rig_root, "Anchor_Lower_Jaw", (0.0, -0.424, 0.122), mats),
        add_anchor(rig_root, "Anchor_Tongue_Tip", (0.0, -0.485, 0.150), mats),
    ]

    link_pairs = [
        ((0.0, 0.0, 0.205), (0.0, -0.010, 0.385)),
        ((0.0, 0.0, 0.205), (-0.250, -0.020, 0.145)),
        ((0.0, 0.0, 0.205), (0.250, -0.020, 0.145)),
        ((0.0, 0.0, 0.205), (0.0, 0.275, 0.168)),
        ((0.0, 0.0, 0.205), (0.0, -0.365, 0.190)),
        ((0.0, -0.365, 0.190), (0.0, -0.424, 0.246)),
        ((0.0, -0.365, 0.190), (0.0, -0.424, 0.122)),
        ((0.0, -0.365, 0.190), (0.0, -0.485, 0.150)),
    ]
    rig_lines = [
        base.add_curve(f"joint link line {index:02d}", rig_root, [start, end], mats["rig_line"], bevel_depth=0.0028)
        for index, (start, end) in enumerate(link_pairs, start=1)
    ]

    labels = [
        add_label(rig_root, "Root Rigidbody", (0.0, 0.065, 0.435), mats),
        add_label(rig_root, "Jiggle lobes", (-0.300, -0.075, 0.245), mats),
        add_label(rig_root, "Mouth limited joint", (0.000, -0.515, 0.285), mats),
        add_label(rig_root, "Shape keys: pulse / squash / recoil / flatten", (0.020, 0.405, 0.300), mats),
    ]

    all_objects = [
        model_root,
        floor,
        body_core,
        outer_skin,
        left_lobe,
        right_lobe,
        rear_mass,
        front_cradle,
        upper_mantle,
        lower_throat,
        side_cowl,
        mouth_root,
        snout,
        lip_ring,
        mouth_cavity,
        tongue,
        upper_gum,
        lower_gum,
        armature,
        *puddle_parts,
        *tooth_objects,
        *swirl_objects,
        *flecks,
        *proxies,
        *anchors,
        *rig_lines,
        *labels,
    ]

    animated = [
        body_core,
        outer_skin,
        left_lobe,
        right_lobe,
        rear_mass,
        front_cradle,
        upper_mantle,
        lower_throat,
        side_cowl,
        mouth_root,
        snout,
        lip_ring,
        tongue,
        upper_gum,
        lower_gum,
    ]
    for obj in animated:
        obj["ParvumMotionPart"] = True

    return {
        "root": root,
        "model_root": model_root,
        "floor": floor,
        "body_core": body_core,
        "outer_skin": outer_skin,
        "left_lobe": left_lobe,
        "right_lobe": right_lobe,
        "rear_mass": rear_mass,
        "front_cradle": front_cradle,
        "upper_mantle": upper_mantle,
        "lower_throat": lower_throat,
        "side_cowl": side_cowl,
        "mouth_root": mouth_root,
        "snout": snout,
        "lip_ring": lip_ring,
        "tongue": tongue,
        "upper_gum": upper_gum,
        "lower_gum": lower_gum,
        "flecks": flecks,
        "swirls": swirl_objects,
        "rig_overlay": [rig_root, armature, *proxies, *anchors, *rig_lines, *labels],
        "all": all_objects,
        "export": [
            model_root,
            body_core,
            outer_skin,
            left_lobe,
            right_lobe,
            rear_mass,
            front_cradle,
            upper_mantle,
            lower_throat,
            side_cowl,
            mouth_root,
            snout,
            lip_ring,
            mouth_cavity,
            tongue,
            upper_gum,
            lower_gum,
            *puddle_parts,
            *tooth_objects,
            *swirl_objects,
            *flecks,
            armature,
            *proxies,
            *anchors,
            *rig_lines,
        ],
    }


def set_rig_visibility(parts: dict[str, object], visible: bool) -> None:
    for obj in parts["rig_overlay"]:
        if isinstance(obj, bpy.types.Object):
            obj.hide_viewport = not visible
            obj.hide_render = not visible


def capture_rest(parts: dict[str, object]) -> dict[bpy.types.Object, tuple[Vector, Vector, Vector]]:
    objects = [
        parts["body_core"],
        parts["outer_skin"],
        parts["left_lobe"],
        parts["right_lobe"],
        parts["rear_mass"],
        parts["front_cradle"],
        parts["upper_mantle"],
        parts["lower_throat"],
        parts["side_cowl"],
        parts["mouth_root"],
        parts["snout"],
        parts["lip_ring"],
        parts["tongue"],
        parts["upper_gum"],
        parts["lower_gum"],
        *parts["flecks"],
        *parts["swirls"],
    ]
    return {obj: (obj.location.copy(), obj.scale.copy(), obj.rotation_euler.copy()) for obj in objects}


def restore_pose(rest: dict[bpy.types.Object, tuple[Vector, Vector, Vector]], parts: dict[str, object]) -> None:
    for obj, (loc, scale, rot) in rest.items():
        obj.location = loc.copy()
        obj.scale = scale.copy()
        obj.rotation_euler = rot.copy()
        set_shape_key_values(obj, {})


def apply_pose(name: str, rest: dict[bpy.types.Object, tuple[Vector, Vector, Vector]], parts: dict[str, object]) -> None:
    restore_pose(rest, parts)
    body = parts["body_core"]
    outer = parts["outer_skin"]
    left = parts["left_lobe"]
    right = parts["right_lobe"]
    rear = parts["rear_mass"]
    mouth = parts["mouth_root"]
    lip = parts["lip_ring"]
    tongue = parts["tongue"]
    upper_gum = parts["upper_gum"]
    lower_gum = parts["lower_gum"]
    lower_throat = parts["lower_throat"]
    side_cowl = parts["side_cowl"]

    if name == "idle":
        keys = {"Idle_Pulse_Surface_Jiggle": 1.0}
        for obj in (body, outer, left, right, rear, lower_throat, side_cowl):
            set_shape_key_values(obj, keys)
        body.location.z += 0.006
        outer.location.z += 0.008
        left.scale.y *= 1.018
        right.scale.y *= 0.986
        rear.location.y += 0.008
        lower_throat.location.z += 0.004
        side_cowl.location.z += 0.004
    elif name == "move":
        keys = {"Move_Squash_Forward_Slosh": 1.0}
        for obj in (body, outer, left, right, rear, lower_throat, side_cowl):
            set_shape_key_values(obj, keys)
        body.location.y -= 0.010
        outer.location.y -= 0.012
        left.location.y += 0.012
        right.location.y -= 0.010
        rear.location.y += 0.024
        lower_throat.location.y -= 0.010
        side_cowl.location.y -= 0.010
        mouth.location.y -= 0.010
    elif name == "attack":
        keys = {"Attack_Bite_Core_Kick": 1.0}
        for obj in (body, outer, lower_throat, side_cowl):
            set_shape_key_values(obj, keys)
        body.location.y -= 0.014
        outer.location.y -= 0.018
        lower_throat.location.y -= 0.012
        lower_throat.location.z += 0.004
        side_cowl.location.y -= 0.012
        side_cowl.location.z += 0.003
        mouth.location.y -= 0.024
        mouth.location.z += 0.004
        lip.scale.z *= 1.055
        upper_gum.location.z += 0.007
        lower_gum.location.z -= 0.007
        tongue.location.y -= 0.017
        tongue.scale.y *= 1.22
    elif name == "hit":
        keys = {"Hit_Recoil_Side_Wave": 1.0}
        for obj in (body, outer, left, right, lower_throat, side_cowl):
            set_shape_key_values(obj, keys)
        body.location.x += 0.016
        outer.location.x += 0.022
        lower_throat.location.x += 0.010
        side_cowl.location.x += 0.012
        mouth.location.y += 0.014
        mouth.location.x += 0.010
        mouth.rotation_euler.z += math.radians(3.5)
        lip.scale *= 0.975
    elif name == "death":
        keys = {"Death_Flatten_Liquid_Spread": 1.0}
        for obj in (body, outer, left, right, rear, parts["front_cradle"], parts["upper_mantle"], lower_throat, side_cowl):
            set_shape_key_values(obj, keys)
        body.location.z -= 0.086
        outer.location.z -= 0.090
        left.location.z -= 0.050
        right.location.z -= 0.050
        rear.location.z -= 0.050
        front_cradle = parts["front_cradle"]
        front_cradle.location.z -= 0.052
        front_cradle.scale.x *= 1.20
        upper_mantle = parts["upper_mantle"]
        upper_mantle.location.z -= 0.052
        upper_mantle.scale.x *= 1.16
        lower_throat.location.z -= 0.045
        lower_throat.scale.x *= 1.18
        lower_throat.scale.y *= 1.10
        side_cowl.location.z -= 0.050
        side_cowl.scale.x *= 1.16
        side_cowl.scale.y *= 1.10
        for fleck in parts["flecks"]:
            fleck.location.z = 0.035 + fleck.location.z * 0.32
            fleck.scale.x *= 1.10
            fleck.scale.y *= 1.10
            fleck.scale.z *= 0.35
        for swirl in parts["swirls"]:
            swirl.scale.x *= 1.10
            swirl.scale.y *= 1.10
            swirl.scale.z *= 0.32
            swirl.location.z -= 0.020
        mouth.location.z -= 0.055
        mouth.scale.z *= 0.72
        mouth.rotation_euler.x += math.radians(5.0)


def add_camera(name: str, loc: tuple[float, float, float], target: tuple[float, float, float], *, ortho_scale: float) -> bpy.types.Object:
    return base.add_camera(name, loc, target, ortho_scale=ortho_scale)


def render_camera(camera: bpy.types.Object, output_path: Path) -> None:
    bpy.context.scene.camera = camera
    bpy.context.scene.render.filepath = str(output_path)
    bpy.ops.render.render(write_still=True)


def configure_scene() -> None:
    base.reset_scene()
    base.configure_rendering()
    scene = bpy.context.scene
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 820
    if scene.render.engine == "CYCLES":
        scene.cycles.samples = 36
    scene.frame_start = 1
    scene.frame_end = 150
    scene.timeline_markers.new("Idle pulse", frame=1)
    scene.timeline_markers.new("Move squash", frame=35)
    scene.timeline_markers.new("Attack bite", frame=70)
    scene.timeline_markers.new("Hit recoil", frame=105)
    scene.timeline_markers.new("Death flatten", frame=140)


def create_pose_keyframes(parts: dict[str, object], rest: dict[bpy.types.Object, tuple[Vector, Vector, Vector]]) -> None:
    frames = [(1, "idle"), (35, "move"), (70, "attack"), (105, "hit"), (140, "death")]
    animated_objects = [
        parts["body_core"],
        parts["outer_skin"],
        parts["left_lobe"],
        parts["right_lobe"],
        parts["rear_mass"],
        parts["front_cradle"],
        parts["upper_mantle"],
        parts["lower_throat"],
        parts["side_cowl"],
        parts["mouth_root"],
        parts["lip_ring"],
        parts["tongue"],
        parts["upper_gum"],
        parts["lower_gum"],
    ]
    for frame, pose_name in frames:
        bpy.context.scene.frame_set(frame)
        apply_pose(pose_name, rest, parts)
        for obj in animated_objects:
            obj.keyframe_insert(data_path="location", frame=frame)
            obj.keyframe_insert(data_path="scale", frame=frame)
            obj.keyframe_insert(data_path="rotation_euler", frame=frame)
            if obj.data and getattr(obj.data, "shape_keys", None) is not None and obj.data.shape_keys is not None:
                for key in obj.data.shape_keys.key_blocks:
                    if key.name != "Basis":
                        key.keyframe_insert(data_path="value", frame=frame)
    bpy.context.scene.frame_set(1)
    restore_pose(rest, parts)


def render_sample_set(parts: dict[str, object], rest: dict[bpy.types.Object, tuple[Vector, Vector, Vector]]) -> None:
    cameras = {
        "front": add_camera("front reference", (0.0, -1.34, 0.275), (0.0, -0.090, 0.205), ortho_scale=0.90),
        "side": add_camera("side reference", (-1.34, -0.105, 0.285), (0.0, -0.110, 0.205), ortho_scale=0.99),
        "back": add_camera("back reference", (0.0, 1.24, 0.270), (0.0, 0.040, 0.205), ortho_scale=0.86),
        "top": add_camera("top anchor map", (0.0, -0.015, 1.46), (0.0, -0.015, 0.170), ortho_scale=1.05),
        "rig": add_camera("physics proxy overview", (0.76, -1.24, 0.590), (0.0, -0.045, 0.215), ortho_scale=1.12),
        "pose": add_camera("pose review", (0.36, -1.26, 0.340), (0.0, -0.100, 0.180), ortho_scale=0.91),
    }

    render_jobs = [
        ("rest", "front", "01_front_reference_match.png", False),
        ("rest", "side", "02_side_reference_match.png", False),
        ("rest", "back", "03_back_reference_match.png", False),
        ("rest", "top", "04_top_anchor_map.png", True),
        ("rest", "rig", "05_physics_proxy_overview.png", True),
        ("idle", "pose", "06_idle_pulse_pose.png", False),
        ("move", "pose", "07_move_squash_pose.png", False),
        ("attack", "pose", "08_attack_bite_pose.png", False),
        ("hit", "pose", "09_hit_recoil_pose.png", False),
        ("death", "pose", "10_death_flatten_pose.png", False),
    ]

    pose_frames = {
        "rest": 1,
        "idle": 1,
        "move": 35,
        "attack": 70,
        "hit": 105,
        "death": 140,
    }

    for pose_name, camera_name, output_name, show_rig in render_jobs:
        bpy.context.scene.frame_set(pose_frames[pose_name])
        if pose_name == "rest":
            restore_pose(rest, parts)
        else:
            apply_pose(pose_name, rest, parts)
        set_rig_visibility(parts, show_rig)
        bpy.context.view_layer.update()
        render_camera(cameras[camera_name], RENDER_DIR / output_name)

    set_rig_visibility(parts, False)
    restore_pose(rest, parts)


def export_assets(parts: dict[str, object]) -> None:
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "parvum_physics_rig_sample.blend"))
    bpy.ops.object.select_all(action="DESELECT")
    export_objects = [obj for obj in parts["export"] if isinstance(obj, bpy.types.Object)]
    for obj in export_objects:
        obj.select_set(True)
    if export_objects:
        bpy.context.view_layer.objects.active = export_objects[0]
    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_DIR / "parvum_physics_rig_sample.fbx"),
        use_selection=True,
        add_leaf_bones=False,
        bake_space_transform=False,
    )
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_DIR / "parvum_physics_rig_sample.glb"),
        export_format="GLB",
        use_selection=True,
        export_animations=True,
    )


def generated_files() -> list[str]:
    return [
        "blender/parvum_physics_rig_sample.blend",
        "exports/parvum_physics_rig_sample.fbx",
        "exports/parvum_physics_rig_sample.glb",
        "textures/parvum_slime_albedo.png",
        "textures/parvum_slime_roughness.png",
        "textures/parvum_white_fleck_mask.png",
        "textures/parvum_snout_scale_albedo.png",
        "textures/parvum_snout_scale_bump.png",
        "textures/parvum_tooth_albedo.png",
        "textures/parvum_tongue_albedo.png",
        "renders/01_front_reference_match.png",
        "renders/02_side_reference_match.png",
        "renders/03_back_reference_match.png",
        "renders/04_top_anchor_map.png",
        "renders/05_physics_proxy_overview.png",
        "renders/06_idle_pulse_pose.png",
        "renders/07_move_squash_pose.png",
        "renders/08_attack_bite_pose.png",
        "renders/09_hit_recoil_pose.png",
        "renders/10_death_flatten_pose.png",
        "README.md",
        "ASSET_MANIFEST.json",
        "APPROVAL_STATUS.json",
        "TEXTURE_ANALYSIS.md",
        "PHYSICS_RIG_NOTES.md",
        "index.html",
    ]


def write_docs() -> None:
    texture_notes = """# 파르붐 보강 샘플 텍스처/머티리얼 분석

## 이미지 기준 반영

- 기준 이미지는 낮고 넓은 녹색 반투명 점액 덩어리, 회녹색 비늘 주둥이, 검은 입 안쪽, 누런 이빨, 붉은 혀, 표면의 흰색 박락으로 구성되어 있습니다.
- 이번 보강 샘플은 기존 승인 샘플보다 몸체를 더 넓고 낮게 만들고, 주둥이와 윗입술이 몸체에서 떠 보이지 않도록 앞쪽 점액 브리지와 윗입술 덮개를 추가했습니다.
- 측면에서 입이 공중에 떠 보이지 않도록 아래쪽 목 연결 점액과 측면 점액 덮개를 추가했습니다.
- 기준 이미지처럼 더 위협적으로 보이도록 이빨 길이를 늘리되, 잇몸 뿌리는 입 안에 묻히도록 유지했습니다.
- 혀는 유지하고, 입 안에서 붉은 두 줄로 보이던 위/아래 잇몸 브리지는 검은 입 안쪽 재질로 바꿔 보이지 않게 했습니다.
- 표면 물방울 오브젝트는 넣지 않았습니다. 흰색 요소는 물방울이 아니라 기준 이미지의 벗겨진 잔여물처럼 몸체 표면에 붙은 평평한 박락 패치로만 배치했습니다.
- 몸체 내부의 선은 흰색이 아니라 어두운 녹색 반투명 흐름선으로 제한했습니다.

## 직접 생성 텍스처

- `parvum_slime_albedo.png`: 녹색 점액 내부 흐름과 색 변화.
- `parvum_slime_roughness.png`: 젖은 표면의 고광택/거칠기 변화.
- `parvum_white_fleck_mask.png`: 흰색 박락 참고 텍스처.
- `parvum_snout_scale_albedo.png`: 회녹색 비늘 주둥이 색 변화.
- `parvum_snout_scale_bump.png`: 비늘/모공 요철 범프.
- `parvum_tooth_albedo.png`: 누런 이빨 얼룩.
- `parvum_tongue_albedo.png`: 젖은 붉은 혀 색 변화.
"""
    (SAMPLE_ROOT / "TEXTURE_ANALYSIS.md").write_text(texture_notes, encoding="utf-8")

    rig_notes = """# 파르붐 물리 보조 모션용 모델링 보강 계획 반영

## Blender 샘플에 포함한 구조

- `Body_Core low translucent mound with shape keys`: 중심 점액 덩어리입니다. 튕김, 전진 눌림, 공격 반동, 피격 파동, 사망 납작화용 Shape Key를 포함합니다.
- `Outer_Gel_Skin transparent layered surface`: 투명 외피입니다. 중심 몸체와 같은 Shape Key 이름을 포함해 액체 표면이 따로 출렁일 수 있게 했습니다.
- `Left_Lobe`, `Right_Lobe`, `Rear_Mass`: Jiggle Physics 또는 Configurable Joint 보조체로 분리하기 위한 덩어리 경계입니다.
- `Mouth_Root`, `Upper_Jaw`, `Lower_Jaw`, `Tongue_Tip`: 공격/피격 시 과하게 떠오르지 않도록 제한 조인트를 걸 수 있는 입 부분 제어 기준입니다.
- `parvum physics control armature`: Blender에서 확인 가능한 제어 본입니다. Unity 적용 시 Animation Rigging, Motion Path, Joint/Jiggle 보조 모션의 기준점으로 사용할 수 있습니다.
- `Proxy_*`: Unity Rigidbody/Collider/Jiggle 보조체 분리를 위한 시각 프록시입니다. 실제 Unity 적용 전 승인 검토용 표시이며 런타임 연결은 아직 하지 않았습니다.

## 상태 포즈

- `06_idle_pulse_pose.png`: 정지 상태의 미세한 액체 호흡/출렁임.
- `07_move_squash_pose.png`: 이동 상태의 전방 눌림과 후방 지연.
- `08_attack_bite_pose.png`: 입은 전방으로 제한된 범위만 움직이고 중심 몸체가 함께 튕기는 공격 포즈.
- `09_hit_recoil_pose.png`: 입이 공중으로 뜨지 않도록 입 움직임은 줄이고 몸체 측면 파동을 강조한 피격 포즈.
- `10_death_flatten_pose.png`: 점액이 바닥으로 퍼지는 사망 포즈.

## Unity 적용 전제

- 이 샘플은 `artSample/` 승인용입니다. 승인 전에는 Unity 씬, 프리팹, 런타임 에셋에 연결하지 않습니다.
- 승인 후에는 Motion Path Animation Editor로 루트 이동/공격 경로를 잡고, Jiggle Physics와 Animation Rigging/Joint 보조체로 몸체와 입의 지연 모션을 나눠 적용하는 구조가 적합합니다.
"""
    (SAMPLE_ROOT / "PHYSICS_RIG_NOTES.md").write_text(rig_notes, encoding="utf-8")

    manifest = {
        "sample": SAMPLE_NAME,
        "objectId": "ENEMY-SEED-PARVUM",
        "title": "파르붐 물리 보조 모션용 모델링 보강 샘플",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "generatedDate": date.today().isoformat(),
        "sourceBasis": [
            "image/parvum(파르붐).png",
            "image/parvum-beside.png",
            "image/parvum-back.png",
            "artSample/enemies/parvum 승인 샘플",
            "docs/GAME_DESIGN_SOURCE.txt: 파르붐은 액체 덩어리에 입이 달린 소형 씨앗체",
            "Motion Path + Jiggle/IK/Joint 기반 물리 보조 모션 구조 계획",
        ],
        "modelingReinforcement": {
            "shapeKeys": [
                "Idle_Pulse_Surface_Jiggle",
                "Move_Squash_Forward_Slosh",
                "Attack_Bite_Core_Kick",
                "Hit_Recoil_Side_Wave",
                "Death_Flatten_Liquid_Spread",
            ],
            "controlBones": [
                "Root_Body",
                "Body_Core_Slosh",
                "Left_Lobe_Jiggle",
                "Right_Lobe_Jiggle",
                "Rear_Mass_Lag",
                "Mouth_Root_Drive",
                "Upper_Jaw_Limited",
                "Lower_Jaw_Limited",
                "Tongue_Tip_Follow",
            ],
            "physicsProxies": [
                "Proxy_Body_Core",
                "Proxy_Left_Lobe",
                "Proxy_Right_Lobe",
                "Proxy_Rear_Mass",
                "Proxy_Mouth_Root",
            ],
        },
        "visualCorrections": [
            "표면 물방울 오브젝트 제외",
            "윗입술과 주둥이를 점액 브리지로 덮어 공중에 떠 보이지 않게 보강",
            "측면에서 입이 공중에 떠 보이지 않도록 아래쪽 목 연결 점액과 측면 점액 덮개 추가",
            "이빨 길이 증가, 잇몸 뿌리 매립 유지",
            "혀는 유지하고 입 안의 붉은 위/아래 잇몸선 제거",
            "몸체 내부 흰색 선 제거, 어두운 녹색 내부 흐름선만 사용",
            "몸체를 기준 이미지처럼 더 낮고 넓은 점액 덩어리로 조정",
            "흰색 박락은 몸체 표면에 붙은 평평한 잔여물로만 표현",
        ],
        "textureMaterialWork": {
            "textureApplied": True,
            "materialApplied": True,
            "proceduralTexturesCreated": True,
            "animationPreviewGifRequired": False,
            "blenderAnimationDataIncluded": True,
        },
        "generatedFiles": generated_files(),
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    approval = {
        "sample": SAMPLE_NAME,
        "objectId": "ENEMY-SEED-PARVUM",
        "approvalState": "미승인",
        "unityApplicationAllowed": False,
        "requiresUserApprovalBeforeUnity": True,
        "note": "사용자 승인 전에는 Unity 씬, 프리팹, 런타임 에셋에 연결하지 않습니다.",
    }
    (SAMPLE_ROOT / "APPROVAL_STATUS.json").write_text(json.dumps(approval, ensure_ascii=False, indent=2), encoding="utf-8")

    readme = """# parvum_physics_rig_sample

파르붐 적대 개체의 물리 보조 모션 적용을 전제로 한 모델링 보강 샘플입니다.

## 목적

- 기준 이미지에 더 가까운 낮고 넓은 반투명 점액 실루엣을 잡습니다.
- 입/주둥이/윗입술이 몸체에서 떠 보이지 않도록 앞쪽 점액 브리지, 아래쪽 목 연결 점액, 측면 점액 덮개를 포함합니다.
- 기준 이미지처럼 이빨 길이를 늘리되 뿌리는 잇몸 안에 묻히도록 유지합니다.
- 혀는 유지하고, 입 안에 붉은 두 줄로 보이던 위/아래 잇몸선은 제거합니다.
- Unity 적용 시 Motion Path, Jiggle Physics, Animation Rigging, Joint 보조 모션을 나눠 걸 수 있도록 중심 몸체, 외피, 좌우/후방 덩어리, 입 루트를 구분합니다.
- 몸체 전체가 덩어리째만 움직이지 않도록 Shape Key와 제어 본/프록시 기준점을 포함합니다.

## 포함 파일

- Blender 원본: `blender/parvum_physics_rig_sample.blend`
- 범용 확인 파일: `exports/parvum_physics_rig_sample.fbx`, `exports/parvum_physics_rig_sample.glb`
- 기준 이미지 비교 렌더: `renders/01_front_reference_match.png`, `renders/02_side_reference_match.png`, `renders/03_back_reference_match.png`
- 물리 구조 렌더: `renders/04_top_anchor_map.png`, `renders/05_physics_proxy_overview.png`
- 상태 포즈 렌더: `renders/06_idle_pulse_pose.png`부터 `renders/10_death_flatten_pose.png`
- 분석 문서: `TEXTURE_ANALYSIS.md`, `PHYSICS_RIG_NOTES.md`

## 승인 상태

현재 상태는 `미승인`입니다. 사용자 승인 전에는 Unity 씬, 프리팹, 런타임 에셋에 연결하지 않습니다.
"""
    (SAMPLE_ROOT / "README.md").write_text(readme, encoding="utf-8")

    comparison_rows = [
        ("정면", "../../../image/parvum(파르붐).png", "renders/01_front_reference_match.png", "낮고 넓은 점액 몸체, 입/이빨/주둥이 통합 여부"),
        ("측면", "../../../image/parvum-beside.png", "renders/02_side_reference_match.png", "주둥이 돌출, 몸체와 윗입술 연결, 바닥 접촉부"),
        ("후면", "../../../image/parvum-back.png", "renders/03_back_reference_match.png", "입 없는 후면 실루엣, 흰색 박락 패치, 투명 점액 질감"),
    ]
    comparison_html = "\n".join(
        f"""
      <article>
        <h3>{title}</h3>
        <div class="pair">
          <figure><a href="{reference}"><img src="{reference}" alt="{title} 기준 이미지"></a><figcaption>기준 이미지</figcaption></figure>
          <figure><a href="{render}"><img src="{render}" alt="{title} 보강 샘플"></a><figcaption>보강 샘플</figcaption></figure>
        </div>
        <p>{caption}</p>
      </article>"""
        for title, reference, render, caption in comparison_rows
    )
    render_cards = "\n".join(
        f'      <figure><a href="renders/{name}"><img src="renders/{name}" alt="{label}"></a><figcaption>{label}</figcaption></figure>'
        for name, label in [
            ("04_top_anchor_map.png", "04 상단 앵커 맵"),
            ("05_physics_proxy_overview.png", "05 물리 프록시 개요"),
            ("06_idle_pulse_pose.png", "06 정지 출렁임"),
            ("07_move_squash_pose.png", "07 이동 눌림"),
            ("08_attack_bite_pose.png", "08 공격 포즈"),
            ("09_hit_recoil_pose.png", "09 피격 포즈"),
            ("10_death_flatten_pose.png", "10 사망 납작화"),
        ]
    )
    texture_cards = "\n".join(
        f'      <figure><a href="textures/{name}"><img src="textures/{name}" alt="{label}"></a><figcaption>{label}</figcaption></figure>'
        for name, label in [
            ("parvum_slime_albedo.png", "점액 알베도"),
            ("parvum_slime_roughness.png", "점액 거칠기"),
            ("parvum_white_fleck_mask.png", "흰색 박락 마스크"),
            ("parvum_snout_scale_albedo.png", "주둥이 비늘 알베도"),
            ("parvum_snout_scale_bump.png", "주둥이 비늘 범프"),
            ("parvum_tooth_albedo.png", "이빨 알베도"),
            ("parvum_tongue_albedo.png", "혀 알베도"),
        ]
    )
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>parvum physics rig sample</title>
  <style>
    body {{ margin: 0; background: #101412; color: #ece8dc; font-family: Arial, sans-serif; }}
    main {{ max-width: 1280px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin: 0 0 8px; font-size: 28px; }}
    h2 {{ margin: 28px 0 12px; font-size: 20px; }}
    h3 {{ margin: 0 0 10px; font-size: 17px; }}
    p {{ color: #cec7b7; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }}
    .texture-grid {{ display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; }}
    .pair {{ display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }}
    article {{ border: 1px solid #3c4a42; background: #18201c; padding: 14px; margin-bottom: 14px; }}
    figure {{ margin: 0; border: 1px solid #35443b; background: #0b0f0d; padding: 8px; }}
    img {{ width: 100%; display: block; }}
    figcaption {{ margin-top: 8px; color: #d8d0bd; font-size: 14px; }}
    code {{ color: #c9efc8; }}
    @media (max-width: 860px) {{ .grid, .texture-grid, .pair {{ grid-template-columns: 1fr; }} }}
  </style>
</head>
<body>
<main>
  <h1>파르붐 물리 보조 모션용 모델링 보강 샘플</h1>
  <p>기준 이미지의 외형, 재질, 질감, 텍스처를 더 맞추면서 Unity에서 Motion Path, Jiggle Physics, Animation Rigging, Joint 보조 모션을 적용할 수 있도록 Shape Key, 제어 본, 물리 프록시, 앵커를 포함한 승인용 샘플입니다.</p>
  <p>승인 상태: <code>미승인</code>, Unity 적용 허용: <code>false</code></p>

  <h2>기준 이미지 비교</h2>
{comparison_html}

  <h2>물리 구조 및 상태 포즈</h2>
  <section class="grid">
{render_cards}
  </section>

  <h2>직접 생성 텍스처</h2>
  <section class="texture-grid">
{texture_cards}
  </section>

  <h2>검토 문서</h2>
  <p><code>TEXTURE_ANALYSIS.md</code>와 <code>PHYSICS_RIG_NOTES.md</code>에 이미지 분석, 모델링 보강, Unity 적용 전제를 정리했습니다.</p>
</main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def main() -> None:
    ensure_dirs()
    clean_generated_files()
    configure_scene()
    mats = material_set()
    base.add_render_lights()
    root = base.add_empty("parvum physics rig artSample root")
    parts = build_model(root, mats)
    set_rig_visibility(parts, False)
    rest = capture_rest(parts)
    create_pose_keyframes(parts, rest)
    render_sample_set(parts, rest)
    export_assets(parts)
    write_docs()


if __name__ == "__main__":
    main()
    bpy.ops.wm.quit_blender()
