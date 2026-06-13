from __future__ import annotations

import math
import os
import random
import sys

import bpy


def parse_project_root() -> str:
    args = sys.argv
    extra = args[args.index("--") + 1 :] if "--" in args else []
    for index, value in enumerate(extra):
        if value == "--project-root" and index + 1 < len(extra):
            return os.path.abspath(extra[index + 1])
    return os.getcwd()


PROJECT_ROOT = parse_project_root()
SCRIPTS_DIR = os.path.join(PROJECT_ROOT, "scripts")
if SCRIPTS_DIR not in sys.path:
    sys.path.insert(0, SCRIPTS_DIR)

import GenerateStage3BlenderApprovalSamples as base  # noqa: E402


SAMPLE_ROOT = os.path.join(PROJECT_ROOT, "artSample", "stage3_true_model_approval_sample")
SLOT_RENDER_DIR = os.path.join(SAMPLE_ROOT, "slots")
RENDER_DIR = os.path.join(SAMPLE_ROOT, "renders")
EXPORT_DIR = os.path.join(SAMPLE_ROOT, "exports")
TEXTURE_DIR = os.path.join(SAMPLE_ROOT, "textures")
BLENDER_DIR = os.path.join(SAMPLE_ROOT, "blender")

SLOT_RENDER_WIDTH = 960
SLOT_RENDER_HEIGHT = 640

SAMPLE_ITEMS = [
    ("01", "cockpit_helm_and_status"),
    ("02", "control_room_cctv_terminal"),
    ("03", "engine_room_power_terminal"),
    ("04", "supply_room_storage_cabinet"),
    ("05", "cargo_hold_props_and_terminal"),
    ("06", "armory_turret_grip_mount"),
    ("07", "first_person_equipment"),
]

SLOT_CAMERA_PRESETS = {
    "01": {
        "main": ((2.50, -4.55, 1.05), (0.0, -0.15, 0.10), 31.0),
        "left_close": ((0.10, -3.10, 0.74), (0.0, -0.70, 0.18), 48.0),
        "center_close": ((0.0, -3.20, 0.08), (0.0, -0.82, -0.12), 52.0),
        "screen_close": ((0.12, -2.95, 0.38), (0.0, -0.74, 0.12), 58.0),
    },
    "02": {
        "main": ((2.45, -4.20, 0.95), (0.05, 0.52, 0.25), 34.0),
        "large_screen": ((0.00, -3.10, 0.45), (-0.30, 0.82, 0.38), 46.0),
        "button_panel": ((0.30, -2.60, -0.48), (0.02, 0.50, -0.62), 60.0),
        "pipe_detail": ((-1.95, -2.65, 1.35), (0.0, 0.65, 1.45), 52.0),
    },
    "03": {
        "main": ((2.35, -4.30, 0.85), (0.18, 0.26, -0.05), 34.0),
        "terminal": ((1.78, -2.70, 0.45), (1.18, 0.20, 0.10), 50.0),
        "breaker": ((1.28, -2.20, -0.38), (1.18, 0.08, -0.45), 65.0),
        "pipe": ((0.10, -2.95, 1.18), (0.72, 0.20, 1.20), 56.0),
    },
    "04": {
        "main": ((2.45, -4.05, 0.82), (0.22, 0.20, -0.05), 33.0),
        "door": ((1.05, -2.70, 0.30), (0.65, 0.08, -0.02), 52.0),
        "handle": ((-0.10, -2.25, 0.12), (0.20, -0.02, 0.05), 70.0),
        "cabinet_iso": ((2.05, -3.00, 0.35), (0.65, 0.05, -0.18), 46.0),
    },
    "05": {
        "main": ((2.50, -4.60, 0.78), (0.08, 0.05, -0.32), 32.0),
        "panel": ((-0.55, -2.95, 0.83), (-0.92, 0.70, 0.82), 50.0),
        "large_crate": ((-1.15, -3.05, -0.25), (-1.15, -0.12, -0.44), 46.0),
        "terminal": ((1.95, -2.85, -0.03), (1.77, -0.77, 0.02), 54.0),
    },
    "06": {
        "main": ((2.00, -4.10, 0.72), (0.12, 0.03, 0.15), 33.0),
        "rail": ((0.00, -2.55, 0.60), (0.0, -0.48, 0.55), 58.0),
        "grips": ((0.00, -2.25, 0.32), (0.0, -0.68, 0.32), 58.0),
        "sight": ((0.00, -2.30, 0.82), (0.0, -0.70, 0.82), 66.0),
    },
    "07": {
        "main": ((0.36, -4.65, -0.02), (0.42, 1.35, -0.10), 24.0),
        "staff_full": ((1.75, -3.15, 0.18), (0.88, -1.35, -0.08), 38.0),
        "hook": ((1.35, -2.35, 0.88), (1.35, -1.35, 0.82), 74.0),
        "wrist": ((0.90, -2.25, -0.70), (0.70, -1.48, -0.86), 64.0),
    },
}


def configure_base() -> None:
    base.PROJECT_ROOT = PROJECT_ROOT
    base.SOURCE_REVIEW_DIR = os.path.join(PROJECT_ROOT, "artSample", "stage3_rework_review")
    base.SAMPLE_ROOT = SAMPLE_ROOT
    base.RENDER_DIR = RENDER_DIR
    base.EXPORT_DIR = EXPORT_DIR
    base.TEXTURE_DIR = TEXTURE_DIR
    base.BLENDER_DIR = BLENDER_DIR
    base.RENDER_WIDTH = SLOT_RENDER_WIDTH
    base.RENDER_HEIGHT = SLOT_RENDER_HEIGHT


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, SLOT_RENDER_DIR, RENDER_DIR, EXPORT_DIR, TEXTURE_DIR, BLENDER_DIR):
        os.makedirs(path, exist_ok=True)


def tune_true_model_materials(mats) -> None:
    for key, color, emission in (
        ("screen", (0.004, 0.078, 0.040, 1.0), 0.105),
        ("screen_dim", (0.003, 0.038, 0.024, 1.0), 0.045),
        ("green_label", (0.025, 0.105, 0.060, 1.0), 0.045),
    ):
        mat = mats.get(key)
        if mat is None or not mat.use_nodes:
            continue
        mat.diffuse_color = color
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf is None:
            continue
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = 0.60
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = color
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emission

    supply_texture = base.make_texture(
        "ST3_TrueModel_SupplyCoolWornCabinet_Albedo",
        "metal",
        (0.18, 0.17, 0.15),
        (0.74, 0.67, 0.56),
        1601,
    )
    mats["supply_cabinet_ref"] = base.make_material(
        "MAT_SupplyCoolWornCabinet",
        (0.34, 0.32, 0.28, 1.0),
        supply_texture,
        0.44,
        0.66,
        0.0,
        0.070,
        74.0,
        True,
    )
    supply_lifted_texture = base.make_texture(
        "ST3_TrueModel_SupplyLiftedCabinet_Albedo",
        "metal",
        (0.27, 0.25, 0.22),
        (0.62, 0.57, 0.49),
        1602,
    )
    mats["supply_cabinet_lifted"] = base.make_material(
        "MAT_SupplyLiftedCabinet",
        (0.40, 0.37, 0.32, 1.0),
        supply_lifted_texture,
        0.38,
        0.70,
        0.0,
        0.055,
        78.0,
        True,
    )
    supply_door_texture = base.make_texture(
        "ST3_TrueModel_SupplyDimDoor_Albedo",
        "metal",
        (0.15, 0.145, 0.135),
        (0.48, 0.45, 0.39),
        1603,
    )
    mats["supply_door_dim"] = base.make_material(
        "MAT_SupplyDimDoor",
        (0.26, 0.25, 0.23, 1.0),
        supply_door_texture,
        0.40,
        0.72,
        0.0,
        0.060,
        80.0,
        True,
    )


def add_front_wear_patches(prefix: str, col, mats, x_min: float, x_max: float, z_min: float, z_max: float, y: float, count: int, seed: int) -> None:
    rng = random.Random(seed)
    materials = (mats["scratch"], mats["rust"], mats["edge_metal"], mats["dark_metal"])
    for index in range(count):
        width = rng.uniform(0.035, 0.18)
        height = rng.uniform(0.018, 0.12)
        x = rng.uniform(x_min, x_max)
        z = rng.uniform(z_min, z_max)
        mat = materials[index % len(materials)]
        base.box(
            f"{prefix}_wear_patch_{index:03d}",
            (x, y - index * 0.0002, z),
            (width, 0.006, height),
            mat,
            col,
            0.001,
            1,
            (0.0, 0.0, math.radians(rng.uniform(-4.0, 4.0))),
        )


def add_panel_edge_wear(prefix: str, col, mats, x: float, y: float, z: float, width: float, height: float) -> None:
    base.box(prefix + "_top_scraped_edge", (x, y, z + height * 0.50), (width * 0.82, 0.008, 0.022), mats["scratch"], col, 0.002, 1)
    base.box(prefix + "_bottom_dirty_edge", (x, y - 0.002, z - height * 0.50), (width * 0.74, 0.008, 0.018), mats["rust"], col, 0.001, 1)
    base.box(prefix + "_left_scraped_edge", (x - width * 0.50, y - 0.004, z), (0.022, 0.008, height * 0.72), mats["scratch"], col, 0.002, 1)
    base.box(prefix + "_right_dirty_edge", (x + width * 0.50, y - 0.006, z), (0.018, 0.008, height * 0.70), mats["rust"], col, 0.001, 1)


def cockpit_screen_device(prefix: str, loc, size, col, mats, angled: bool = False) -> None:
    x, y, z = loc
    sx, sy, sz = size
    rotation = (math.radians(-4.0), 0.0, 0.0) if angled else (0.0, 0.0, 0.0)
    base.box(prefix + "_heavy_side_case", (x, y + 0.050, z), (sx * 1.18, sy * 1.55, sz * 1.34), mats["dark_metal"], col, 0.050, 3, rotation)
    base.box(prefix + "_rear_extension", (x + sx * 0.48, y + 0.075, z), (sx * 0.22, sy * 1.65, sz * 1.06), mats["black"], col, 0.030, 2, rotation)
    base.screen(prefix + "_green_crt", (x, y - 0.020, z), (sx, sy, sz), mats, col)
    base.box(prefix + "_bottom_indicator_slot", (x, y - sy * 0.86, z - sz * 0.55), (sx * 0.52, sy * 0.15, sz * 0.060), mats["black"], col, 0.006, 1)
    for index in range(4):
        base.box(
            f"{prefix}_green_status_dot_{index}",
            (x - sx * 0.20 + index * sx * 0.13, y - sy * 0.88, z - sz * 0.55),
            (sx * 0.040, sy * 0.025, sz * 0.040),
            mats["green_label"],
            col,
            0.002,
            1,
        )


def cockpit_reference_terminal_device(prefix: str, loc, col, mats) -> None:
    x, y, z = loc
    body_rot = (math.radians(-2.5), 0.0, math.radians(-3.0))
    top_rot = (math.radians(-13.0), 0.0, math.radians(-3.0))

    base.box(prefix + "_rear_shadow_body", (x + 0.03, y + 0.04, z - 0.03), (1.48, 0.86, 0.70), mats["black"], col, 0.070, 4, body_rot)
    base.box(prefix + "_heavy_chamfered_case", (x, y - 0.02, z), (1.38, 0.78, 0.66), mats["dark_metal"], col, 0.085, 5, body_rot)
    base.box(prefix + "_raised_top_bezel", (x - 0.02, y - 0.33, z + 0.26), (1.04, 0.095, 0.48), mats["edge_metal"], col, 0.040, 3, top_rot)
    base.box(prefix + "_recessed_top_gasket", (x - 0.02, y - 0.395, z + 0.26), (0.86, 0.030, 0.34), mats["rubber"], col, 0.022, 2, top_rot)
    base.box(prefix + "_dim_worn_glass", (x - 0.02, y - 0.418, z + 0.26), (0.74, 0.014, 0.27), mats["screen_dim"], col, 0.014, 2, top_rot)
    for index in range(5):
        base.box(
            f"{prefix}_faint_top_scanline_{index}",
            (x - 0.02, y - 0.429, z + 0.17 + index * 0.045),
            (0.58, 0.004, 0.004),
            mats["green_label"],
            col,
            0.0,
            1,
            top_rot,
        )

    base.box(prefix + "_front_lower_service_panel", (x, y - 0.47, z - 0.25), (1.05, 0.055, 0.22), mats["dark_metal"], col, 0.028, 2, body_rot)
    base.box(prefix + "_front_black_latch_recess", (x + 0.34, y - 0.51, z - 0.25), (0.16, 0.018, 0.055), mats["black"], col, 0.004, 1, body_rot)
    base.box(prefix + "_front_worn_id_plate", (x - 0.25, y - 0.512, z - 0.25), (0.28, 0.012, 0.050), mats["scratch"], col, 0.003, 1, body_rot)

    for side in (-1, 1):
        sx = x + side * 0.72
        base.box(f"{prefix}_thick_side_cheek_{side}", (sx, y - 0.02, z - 0.02), (0.12, 0.68, 0.58), mats["dark_metal"], col, 0.035, 3, body_rot)
        for vent in range(4):
            base.box(
                f"{prefix}_side_vent_slot_{side}_{vent}",
                (sx + side * 0.010, y - 0.40 + vent * 0.090, z + 0.02),
                (0.018, 0.045, 0.20),
                mats["black"],
                col,
                0.003,
                1,
                body_rot,
            )
        base.box(f"{prefix}_front_foot_{side}", (x + side * 0.49, y - 0.50, z - 0.45), (0.26, 0.13, 0.080), mats["edge_metal"], col, 0.018, 2, body_rot)

    for bx, bz in ((-0.55, 0.42), (0.55, 0.42), (-0.56, -0.42), (0.56, -0.42)):
        base.bolt(f"{prefix}_case_bolt_{bx}_{bz}", (x + bx, y - 0.525, z + bz), mats["edge_metal"], col, 0.020)

    add_panel_edge_wear(prefix + "_top_bezel_wear", col, mats, x - 0.02, y - 0.432, z + 0.26, 1.04, 0.48)
    add_front_wear_patches(prefix + "_case_mottled_wear", col, mats, x - 0.62, x + 0.62, z - 0.46, z + 0.46, y - 0.535, 30, 7101)


def cockpit_helm_ring(prefix: str, col, mats, center=(0.0, -0.82, -0.20), radius=0.62) -> None:
    cx, cy, cz = center
    for index, angle in enumerate(range(205, 516, 18)):
        normalized = angle % 360
        if 154 < normalized < 206:
            continue
        radians = math.radians(normalized)
        x = cx + math.cos(radians) * radius
        z = cz + math.sin(radians) * radius
        base.box(
            f"{prefix}_segmented_ring_plate_{index:02d}",
            (x, cy, z),
            (0.28, 0.105, 0.078),
            mats["dark_metal"],
            col,
            0.018,
            2,
            (0.0, math.radians(90.0 - normalized), 0.0),
        )
        base.bolt(f"{prefix}_ring_bolt_{index:02d}_a", (x - math.sin(radians) * 0.078, cy - 0.070, z + math.cos(radians) * 0.078), mats["edge_metal"], col, 0.014)
        base.bolt(f"{prefix}_ring_bolt_{index:02d}_b", (x + math.sin(radians) * 0.078, cy - 0.070, z - math.cos(radians) * 0.078), mats["edge_metal"], col, 0.014)

    base.curve_pipe(f"{prefix}_left_bent_handle_arm", [(cx - 0.55, cy, cz - 0.10), (cx - 0.90, cy - 0.10, cz - 0.06), (cx - 1.02, cy - 0.10, cz - 0.32)], 0.030, mats["edge_metal"], col)
    base.curve_pipe(f"{prefix}_right_bent_handle_arm", [(cx + 0.55, cy, cz - 0.10), (cx + 0.90, cy - 0.10, cz - 0.06), (cx + 1.02, cy - 0.10, cz - 0.32)], 0.030, mats["edge_metal"], col)
    base.ribbed_grip(f"{prefix}_left_black_ribbed_handle", (cx - 1.05, cy - 0.10, cz - 0.05), 0.078, 0.58, col, mats, "Z")
    base.ribbed_grip(f"{prefix}_right_black_ribbed_handle", (cx + 1.05, cy - 0.10, cz - 0.05), 0.078, 0.58, col, mats, "Z")
    base.box(f"{prefix}_left_red_thumb_button", (cx - 1.05, cy - 0.18, cz + 0.26), (0.070, 0.026, 0.045), mats["red"], col, 0.010, 1)
    base.box(f"{prefix}_right_red_thumb_button", (cx + 1.05, cy - 0.18, cz + 0.26), (0.070, 0.026, 0.045), mats["red"], col, 0.010, 1)


def build_cockpit_complete_model(col, mats) -> None:
    base.industrial_floor("cockpit_main", col, mats, width=5.7, depth=3.95, z=-1.15)
    base.box("cockpit_main_left_side_wall", (-2.82, 0.10, 0.12), (0.14, 3.18, 2.55), mats["wall"], col, 0.018, 2)
    base.box("cockpit_main_right_side_wall", (2.82, 0.10, 0.12), (0.14, 3.18, 2.55), mats["wall"], col, 0.018, 2)
    base.box("cockpit_main_low_ceiling_shadow_panel", (0.0, 0.02, 1.86), (5.55, 3.30, 0.10), mats["black"], col, 0.010, 1)
    for index, x in enumerate((-2.20, -1.30, -0.40, 0.50, 1.40, 2.25)):
        base.box(f"cockpit_main_ceiling_rib_{index}", (x, 0.04, 1.70), (0.085, 3.06, 0.12), mats["dark_metal"], col, 0.006, 1)
    base.box("cockpit_main_rear_bulkhead", (0.0, 1.30, 0.22), (5.85, 0.16, 2.95), mats["wall"], col, 0.020, 2)
    base.box("cockpit_main_wide_window_outer_octagon", (0.0, 1.18, 0.82), (4.42, 0.18, 1.38), mats["dark_metal"], col, 0.060, 3)
    base.box("cockpit_main_wide_window_black_glass", (0.0, 1.035, 0.84), (3.70, 0.035, 0.90), mats["black"], col, 0.030, 1)
    for index, x in enumerate((-1.90, -1.25, -0.55, 0.50, 1.25, 1.90)):
        base.box(f"cockpit_main_window_frame_bolt_plate_{index}", (x, 0.995, 1.40), (0.24, 0.032, 0.10), mats["edge_metal"], col, 0.006, 1)
    for index, x in enumerate((-1.75, -0.95, -0.12, 0.78, 1.55)):
        base.build_cargo_crate(f"cockpit_main_window_seen_cargo_{index}", (x, 0.92, -0.12 + (index % 2) * 0.10), (0.44, 0.26, 0.32), col, mats, False)
    for index, x in enumerate((-1.68, -0.85, 0.00, 0.86, 1.72)):
        base.strip_light(f"cockpit_main_distant_cargo_bay_light_{index}", (x, 0.82, 1.24), (0.30, 0.022, 0.035), col, mats)
    base.strip_light("cockpit_main_window_overhead_light", (1.05, 1.02, 1.68), (0.86, 0.030, 0.045), col, mats)

    cockpit_screen_device("cockpit_main_center_monitor", (0.00, -0.38, 0.44), (1.28, 0.13, 0.62), col, mats, True)
    cockpit_screen_device("cockpit_main_left_monitor_a", (-1.42, -0.42, 0.10), (0.64, 0.11, 0.42), col, mats, True)
    cockpit_screen_device("cockpit_main_left_monitor_b", (-0.78, -0.38, 0.12), (0.58, 0.10, 0.39), col, mats, True)
    cockpit_screen_device("cockpit_main_right_monitor_a", (1.12, -0.36, 0.10), (0.64, 0.11, 0.42), col, mats, True)
    cockpit_screen_device("cockpit_main_right_monitor_b", (1.78, -0.35, 0.05), (0.68, 0.11, 0.46), col, mats, True)
    base.box("cockpit_main_left_console_shelf", (-1.23, -0.22, -0.34), (1.48, 0.38, 0.28), mats["dark_metal"], col, 0.030, 2)
    base.box("cockpit_main_right_console_shelf", (1.50, -0.22, -0.34), (1.58, 0.38, 0.28), mats["dark_metal"], col, 0.030, 2)
    base.box("cockpit_main_center_pedestal", (0.0, -0.72, -0.74), (0.48, 0.34, 0.82), mats["dark_metal"], col, 0.036, 2)
    cockpit_helm_ring("cockpit_main_floor_mounted_helm", col, mats, center=(0.0, -0.86, -0.18), radius=0.62)

    for side in (-1, 1):
        base.curve_pipe(f"cockpit_main_side_hose_{side}", [(side * 2.18, 0.96, 0.85), (side * 2.02, 0.40, 0.20), (side * 1.90, -0.20, -0.70)], 0.040, mats["rubber"], col)
        base.box(f"cockpit_main_side_wall_device_{side}", (side * 2.42, 0.28, 0.36), (0.14, 0.40, 0.70), mats["dark_metal"], col, 0.016, 1)
        for index in range(4):
            base.box(
                f"cockpit_main_side_cable_stack_{side}_{index}",
                (side * 2.58, 0.72 - index * 0.28, -0.38),
                (0.055, 0.055, 0.72),
                mats["rubber"],
                col,
                0.004,
                1,
            )

    base.box("cockpit_closeup_black_backdrop_wall", (0.0, -0.34, 0.10), (4.20, 0.08, 2.70), mats["black"], col, 0.010, 1)
    base.box("cockpit_closeup_black_backdrop_floor", (0.0, -0.86, -1.16), (4.20, 2.10, 0.08), mats["black"], col, 0.010, 1)
    cockpit_screen_device("cockpit_iso_terminal_box", (0.0, -0.70, 0.20), (1.42, 0.16, 0.78), col, mats, False)
    base.ribbed_grip("cockpit_iso_terminal_front_handle", (-0.53, -0.93, -0.20), 0.055, 0.78, col, mats, "X")
    base.box("cockpit_iso_terminal_lower_mount", (0.0, -0.94, -0.58), (1.38, 0.20, 0.18), mats["dark_metal"], col, 0.025, 2)
    cockpit_helm_ring("cockpit_iso_helm_ring", col, mats, center=(0.0, -0.82, -0.14), radius=0.72)
    cockpit_screen_device("cockpit_iso_screen_single", (0.0, -0.74, 0.10), (1.18, 0.14, 0.70), col, mats, False)
    cockpit_screen_device("cockpit_iso_screen_left_small", (-0.92, -0.78, 0.06), (0.72, 0.11, 0.48), col, mats, False)
    cockpit_screen_device("cockpit_iso_screen_right_small", (0.92, -0.78, 0.06), (0.72, 0.11, 0.48), col, mats, False)
    add_panel_edge_wear("cockpit_main_worn_center_monitor", col, mats, 0.00, -0.105, 0.44, 1.28, 0.62)
    add_panel_edge_wear("cockpit_main_worn_left_monitor_a", col, mats, -1.42, -0.090, 0.10, 0.64, 0.42)
    add_panel_edge_wear("cockpit_main_worn_left_monitor_b", col, mats, -0.78, -0.085, 0.12, 0.58, 0.39)
    add_panel_edge_wear("cockpit_main_worn_right_monitor_a", col, mats, 1.12, -0.085, 0.10, 0.64, 0.42)
    add_panel_edge_wear("cockpit_main_worn_right_monitor_b", col, mats, 1.78, -0.085, 0.05, 0.68, 0.46)
    add_front_wear_patches("cockpit_main_console_dirty_metal", col, mats, -2.05, 2.05, -0.72, 0.78, -0.115, 60, 4101)
    add_front_wear_patches("cockpit_iso_screen_dirty_metal", col, mats, -1.70, 1.70, -0.68, 0.70, -0.930, 46, 4102)


def build_control_room_surface_model(col, mats) -> None:
    base.build_control_room(col, mats)
    add_panel_edge_wear("control_room_large_screen_edge_wear", col, mats, -0.30, 0.730, 0.38, 2.08, 0.92)
    add_panel_edge_wear("control_room_upper_screen_edge_wear", col, mats, -1.10, 0.690, 1.23, 0.82, 0.34)
    add_panel_edge_wear("control_room_vertical_status_edge_wear", col, mats, 1.30, 0.710, 0.31, 0.54, 1.28)
    add_front_wear_patches("control_room_outer_panel_pitted_wear", col, mats, -1.90, 1.72, -0.78, 1.25, 0.720, 95, 5201)
    add_front_wear_patches("control_room_button_console_wear", col, mats, -1.52, 1.52, -0.80, -0.45, 0.268, 36, 5202)
    add_front_wear_patches("control_room_pipe_clamp_wear", col, mats, -2.32, 2.16, 1.28, 1.70, 0.605, 28, 5203)


def control_room_iso_screen(prefix: str, loc, size, col, mats, bars: bool = False, map_lines: bool = True) -> None:
    x, y, z = loc
    sx, sy, sz = size
    base.box(prefix + "_rear_black_case", (x, y + 0.040, z), (sx * 1.16, sy * 1.58, sz * 1.34), mats["black"], col, 0.040, 2)
    base.screen(prefix + "_green_crt", (x, y - 0.020, z), (sx, sy, sz), mats, col, bars=bars, map_lines=map_lines)
    for side in (-1, 1):
        base.box(prefix + f"_side_rail_{side}", (x + side * sx * 0.58, y - 0.060, z), (sx * 0.035, sy * 0.16, sz * 1.20), mats["edge_metal"], col, 0.008, 1)
    for index in range(5):
        base.box(
            f"{prefix}_bottom_micro_indicator_{index}",
            (x - sx * 0.26 + index * sx * 0.13, y - sy * 0.78, z - sz * 0.55),
            (sx * 0.030, sy * 0.035, sz * 0.035),
            mats["green_label"] if index < 3 else mats["red"],
            col,
            0.002,
            1,
        )


def build_control_room_complete_model(col, mats) -> None:
    base.build_control_room(col, mats)
    for index, z in enumerate((1.70, 1.54, 1.38, 1.22)):
        base.curve_pipe(
            f"control_room_extra_black_pipe_bundle_{index}",
            [(-2.62, 0.55 - index * 0.020, z), (2.46, 0.55 - index * 0.018, z - 0.04)],
            0.018 + index * 0.003,
            mats["rubber"] if index != 1 else mats["red"],
            col,
        )
    for index, x in enumerate((-2.35, -1.64, -0.84, 0.04, 0.94, 1.84, 2.40)):
        base.box(f"control_room_extra_wall_seam_{index}", (x, 0.585, 0.22), (0.038, 0.040, 1.86), mats["edge_metal"], col, 0.004, 1)
    for index, x in enumerate((-0.96, -0.55, -0.14, 0.28, 0.70)):
        base.box(f"control_room_extra_console_toggle_{index}", (x, 0.282, -0.45), (0.10, 0.030, 0.080), mats["black"], col, 0.004, 1)

    base.box("control_iso_backdrop_wall", (0.0, -0.52, 0.08), (4.15, 0.08, 2.85), mats["black"], col, 0.010, 1)
    base.box("control_iso_backdrop_floor", (0.0, -0.92, -1.22), (4.15, 1.95, 0.08), mats["black"], col, 0.010, 1)
    control_room_iso_screen("control_iso_large_screen_top", (0.0, -0.78, 0.70), (1.92, 0.14, 0.62), col, mats, False, True)
    control_room_iso_screen("control_iso_large_screen_lower", (0.0, -0.76, -0.10), (1.62, 0.12, 0.42), col, mats, False, True)
    base.box("control_iso_button_panel_slab", (0.0, -0.76, -0.46), (2.20, 0.26, 0.30), mats["dark_metal"], col, 0.028, 2, (math.radians(-7), 0.0, 0.0))
    for index, mat in enumerate((mats["black"], mats["black"], mats["yellow_plain"], mats["red"])):
        base.box(f"control_iso_button_panel_small_button_{index}", (-0.66 + index * 0.22, -0.93, -0.42), (0.12, 0.040, 0.080), mat, col, 0.006, 1)
    for label, x, mat in (("A", 0.45, mats["yellow_plain"]), ("D", 0.78, mats["red"])):
        base.box(f"control_iso_button_panel_{label}_large_button", (x, -0.94, -0.42), (0.20, 0.046, 0.13), mat, col, 0.010, 1)
        base.add_text(f"control_iso_button_panel_{label}_letter", label, (x, -0.985, -0.42), 0.13, mats["edge_metal"], col)
    for index, z in enumerate((0.72, 0.55, 0.38)):
        base.curve_pipe(
            f"control_iso_pipe_detail_run_{index}",
            [(-1.78, -0.78 - index * 0.025, z), (1.78, -0.78 - index * 0.025, z - 0.02)],
            0.035,
            mats["red"] if index == 1 else mats["rubber"],
            col,
        )
    for index, x in enumerate((-1.45, -0.72, 0.0, 0.72, 1.45)):
        base.box(f"control_iso_pipe_detail_clamp_{index}", (x, -0.82, 0.50), (0.11, 0.080, 0.42), mats["dark_metal"], col, 0.010, 1)


def cargo_status_panel(prefix: str, loc, col, mats) -> None:
    x, y, z = loc
    base.box(prefix + "_outer_case", (x, y, z), (1.96, 0.20, 0.82), mats["dark_metal"], col, 0.048, 3)
    base.screen(prefix + "_left_green_display", (x - 0.22, y - 0.12, z + 0.04), (1.08, 0.105, 0.34), mats, col)
    base.box(prefix + "_right_control_strip", (x + 0.68, y - 0.13, z + 0.02), (0.24, 0.050, 0.58), mats["black"], col, 0.012, 1)
    for index, color in enumerate((mats["green_label"], mats["red"], mats["black"])):
        base.box(f"{prefix}_round_indicator_{index}", (x + 0.68, y - 0.17, z + 0.18 - index * 0.18), (0.060, 0.020, 0.060), color, col, 0.010, 1)
    for side in (-1, 1):
        base.box(prefix + f"_vertical_guard_{side}", (x + side * 0.96, y - 0.08, z), (0.12, 0.080, 0.82), mats["edge_metal"], col, 0.014, 1)


def add_cargo_floor_grates(prefix: str, col, mats) -> None:
    for index, x in enumerate((-2.20, -1.72, -1.24, -0.76, -0.28, 0.20, 0.68, 1.16, 1.64, 2.12)):
        base.box(f"{prefix}_floor_long_grate_{index}", (x, -0.76, -1.055), (0.026, 2.42, 0.018), mats["black"], col, 0.002, 1)
    for index, y in enumerate((-1.70, -1.28, -0.86, -0.44, -0.02, 0.40)):
        base.box(f"{prefix}_floor_cross_grate_{index}", (0.0, y, -1.048), (4.72, 0.024, 0.016), mats["black"], col, 0.002, 1)
    base.box(prefix + "_front_red_floor_lane", (0.72, -1.34, -1.032), (3.18, 0.075, 0.018), mats["red"], col, 0.002, 1)


def build_cargo_hold_complete_model(col, mats) -> None:
    base.build_cargo_hold(col, mats)
    add_cargo_floor_grates("cargo_hold_extra", col, mats)
    base.build_cargo_crate("cargo_hold_rear_small_left_crate", (-1.92, 0.58, -0.66), (0.58, 0.52, 0.44), col, mats, False)
    base.build_cargo_crate("cargo_hold_rear_mid_crate", (-0.08, 0.42, -0.64), (0.86, 0.62, 0.58), col, mats, False)
    base.build_cargo_crate("cargo_hold_front_low_box", (-1.40, -1.20, -0.84), (1.10, 0.58, 0.36), col, mats, False)
    for index, z in enumerate((1.48, 1.28, 1.08, 0.88)):
        base.curve_pipe(f"cargo_hold_extra_wall_cable_{index}", [(-2.50, 0.68, z), (2.32, 0.62, z - 0.06)], 0.020, mats["rubber"], col)

    base.box("cargo_iso_backdrop_wall", (0.0, -0.55, 0.02), (4.25, 0.08, 2.90), mats["black"], col, 0.010, 1)
    base.box("cargo_iso_backdrop_floor", (0.0, -0.95, -1.22), (4.25, 2.05, 0.08), mats["black"], col, 0.010, 1)
    cargo_status_panel("cargo_iso_panel_status_unit", (0.0, -0.78, 0.58), col, mats)
    base.build_cargo_crate("cargo_iso_large_crate_primary", (0.0, -0.86, -0.26), (1.78, 0.78, 1.02), col, mats, True)
    base.build_cargo_crate("cargo_iso_large_crate_low_secondary", (-0.12, -0.98, -0.86), (1.28, 0.52, 0.34), col, mats, False)
    base.box("cargo_iso_terminal_pedestal_column", (0.0, -0.80, -0.66), (0.34, 0.34, 0.82), mats["dark_metal"], col, 0.030, 2)
    base.box("cargo_iso_terminal_sloped_head", (0.0, -0.94, -0.02), (0.78, 0.40, 0.40), mats["dark_metal"], col, 0.044, 3, (math.radians(-12.0), 0.0, 0.0))
    base.screen("cargo_iso_terminal_screen", (0.0, -1.17, 0.05), (0.52, 0.09, 0.26), mats, col)
    for index, mat in enumerate((mats["red"], mats["yellow_plain"], mats["yellow_plain"], mats["black"])):
        base.box(f"cargo_iso_terminal_button_{index}", (-0.24 + index * 0.16, -1.24, -0.16), (0.09, 0.030, 0.065), mat, col, 0.006, 1)


def build_supply_iso_cabinet(prefix: str, loc, col, mats) -> None:
    x, y, z = loc
    base.box(prefix + "_outer_frame", (x, y, z), (2.34, 0.34, 1.72), mats["supply_cabinet_ref"], col, 0.060, 3)
    for row in range(2):
        for column in range(3):
            index = row * 3 + column
            dx = -0.76 + column * 0.76
            dz = 0.40 - row * 0.76
            base.box(f"{prefix}_door_{index}", (x + dx, y - 0.17, z + dz), (0.62, 0.050, 0.62), mats["supply_cabinet_ref"], col, 0.032, 2)
            base.box(f"{prefix}_door_inner_panel_{index}", (x + dx + 0.04, y - 0.202, z + dz), (0.42, 0.018, 0.36), mats["supply_cabinet_ref"], col, 0.014, 1)
            base.box(f"{prefix}_worn_yellow_band_{index}", (x + dx + 0.05, y - 0.218, z + dz), (0.48, 0.014, 0.090), mats["yellow_plain"], col, 0.004, 1)
            for chip in range(4):
                base.box(
                    f"{prefix}_yellow_band_chip_{index}_{chip}",
                    (x + dx - 0.15 + chip * 0.10, y - 0.229, z + dz + 0.020),
                    (0.030, 0.006, 0.030),
                    mats["black"],
                    col,
                    0.001,
                    1,
                )
            base.ribbed_grip(f"{prefix}_black_vertical_handle_{index}", (x + dx - 0.24, y - 0.235, z + dz), 0.028, 0.34, col, mats, "Z")
            for hz in (-0.20, 0.20):
                base.box(f"{prefix}_hinge_{index}_{hz}", (x + dx + 0.31, y - 0.226, z + dz + hz), (0.045, 0.030, 0.13), mats["edge_metal"], col, 0.004, 1)


def build_supply_room_complete_model(col, mats) -> None:
    base.build_supply_room(col, mats)
    for obj in col.objects:
        if obj.type == "MESH" and (
            obj.name.startswith("supply_olive_locker_door_")
            or obj.name.startswith("supply_locker_inner_inset_")
            or obj.name.startswith("supply_six_door_cabinet_outer_frame")
        ):
            obj.data.materials.clear()
            obj.data.materials.append(mats["supply_cabinet_lifted"])
    for index, x in enumerate((-1.70, -1.30, -0.90, -0.50, -0.10, 0.30, 0.70, 1.10, 1.50)):
        base.box(f"supply_extra_floor_grate_{index}", (x, -0.72, -1.050), (0.024, 2.15, 0.016), mats["black"], col, 0.002, 1)

    base.box("supply_iso_backdrop_wall", (0.0, -0.54, 0.04), (4.20, 0.08, 2.85), mats["black"], col, 0.010, 1)
    base.box("supply_iso_backdrop_floor", (0.0, -0.94, -1.22), (4.20, 1.95, 0.08), mats["black"], col, 0.010, 1)
    base.box("supply_iso_door_flat_panel", (-0.55, -0.78, 0.18), (0.86, 0.080, 1.20), mats["supply_door_dim"], col, 0.036, 2)
    for index, z in enumerate((0.58, -0.22)):
        base.box(f"supply_iso_door_slot_{index}", (-0.55, -0.835, z), (0.46, 0.018, 0.050), mats["black"], col, 0.004, 1)
    base.corner_bolts("supply_iso_door_flat", (-0.55, -0.78, 0.18), 0.78, 1.08, -0.842, mats["edge_metal"], col, 0.015)
    base.box("supply_iso_door_single_cabinet", (0.70, -0.78, 0.05), (0.74, 0.085, 1.18), mats["supply_cabinet_ref"], col, 0.036, 2)
    base.box("supply_iso_door_single_yellow_band", (0.74, -0.84, 0.05), (0.52, 0.014, 0.090), mats["yellow_plain"], col, 0.004, 1)
    base.ribbed_grip("supply_iso_door_single_handle", (0.42, -0.86, 0.05), 0.034, 0.54, col, mats, "Z")
    base.corner_bolts("supply_iso_door_single", (0.70, -0.78, 0.05), 0.66, 1.04, -0.853, mats["edge_metal"], col, 0.014)

    base.box("supply_iso_handle_mount_left", (-0.62, -0.82, 0.00), (0.22, 0.090, 0.32), mats["supply_cabinet_ref"], col, 0.022, 2)
    base.box("supply_iso_handle_mount_right", (0.62, -0.82, 0.00), (0.22, 0.090, 0.32), mats["supply_cabinet_ref"], col, 0.022, 2)
    base.ribbed_grip("supply_iso_handle_black_crossbar", (0.0, -0.90, 0.00), 0.075, 1.10, col, mats, "X")
    build_supply_iso_cabinet("supply_iso_cabinet_six_door", (0.0, -0.82, -0.05), col, mats)


def build_supply_room_surface_model(col, mats) -> None:
    base.build_supply_room(col, mats)
    add_front_wear_patches("supply_room_six_door_mottled_rust", col, mats, -0.46, 1.66, -0.68, 0.78, 0.018, 110, 6401)
    add_front_wear_patches("supply_room_shelf_shadow_wear", col, mats, -2.05, -1.10, -0.80, 0.90, 0.025, 34, 6402)
    for row in range(2):
        for column in range(3):
            index = row * 3 + column
            x = -0.13 + column * 0.78
            z = 0.43 - row * 0.82
            add_panel_edge_wear(f"supply_room_locker_edge_wear_{index}", col, mats, x, 0.010, z, 0.67, 0.68)


def build_supply_room_reference_material_model(col, mats) -> None:
    base.build_supply_room(col, mats)
    for obj in col.objects:
        if obj.type == "MESH" and (
            obj.name.startswith("supply_olive_locker_door_")
            or obj.name.startswith("supply_locker_inner_inset_")
            or obj.name.startswith("supply_six_door_cabinet_outer_frame")
        ):
            obj.data.materials.clear()
            obj.data.materials.append(mats["supply_cabinet_lifted"])


def build_supply_room_hybrid_model(col, mats) -> None:
    build_supply_room_reference_material_model(col, mats)
    base.box("supply_iso_backdrop_wall", (-0.55, -0.54, 0.08), (1.70, 0.08, 1.72), mats["black"], col, 0.010, 1)
    base.box("supply_iso_door_flat_panel", (-0.55, -0.78, 0.18), (0.86, 0.080, 1.20), mats["supply_door_dim"], col, 0.036, 2)
    for index, z in enumerate((0.58, -0.22)):
        base.box(f"supply_iso_door_slot_{index}", (-0.55, -0.835, z), (0.46, 0.018, 0.050), mats["black"], col, 0.004, 1)
    base.corner_bolts("supply_iso_door_flat", (-0.55, -0.78, 0.18), 0.78, 1.08, -0.842, mats["edge_metal"], col, 0.015)


def build_first_person_complete_model(col, mats) -> None:
    # This is a modeled corridor and foreground equipment, not a source-image projection.
    base.box("fp_corridor_oily_center_floor", (0.0, 2.05, -1.18), (4.75, 8.80, 0.085), mats["floor"], col, 0.018, 2)
    base.box("fp_corridor_center_dark_runway", (0.0, 2.05, -1.09), (1.18, 8.10, 0.030), mats["black"], col, 0.006, 1)

    for index in range(9):
        y = -1.55 + index * 0.92
        base.box(f"fp_floor_rect_panel_{index:02d}", (0.0, y, -1.045), (0.92, 0.64, 0.016), mats["dark_metal"], col, 0.009, 1)
        for side in (-1, 1):
            base.box(
                f"fp_floor_hazard_tick_{side}_{index:02d}",
                (side * 1.18, y + 0.08, -1.018),
                (0.30, 0.070, 0.018),
                mats["yellow_plain"] if index % 2 == 0 else mats["red"],
                col,
                0.002,
                1,
                (0.0, 0.0, math.radians(side * 24.0)),
            )

    for side in (-1, 1):
        x = side * 2.42
        for index in range(9):
            y = -1.45 + index * 0.96
            base.box(
                f"fp_side_wall_panel_{side}_{index:02d}",
                (x, y, 0.03),
                (0.105, 0.74, 2.34),
                mats["wall"] if index % 3 else mats["dark_metal"],
                col,
                0.018,
                2,
            )
            base.box(
                f"fp_side_wall_inner_recess_{side}_{index:02d}",
                (x - side * 0.066, y, 0.18),
                (0.030, 0.48, 1.55),
                mats["black"],
                col,
                0.006,
                1,
            )
        for pipe_index in range(5):
            z = -0.58 + pipe_index * 0.42
            base.curve_pipe(
                f"fp_side_pipe_run_{side}_{pipe_index}",
                [(x - side * 0.12, -1.55, z), (x - side * 0.12, 6.20, z + 0.08)],
                0.024,
                mats["rubber"] if pipe_index % 2 else mats["dark_metal"],
                col,
            )

    for index in range(8):
        y = -1.35 + index * 1.05
        base.box(f"fp_ceiling_plate_{index:02d}", (0.0, y, 1.72), (4.50, 0.78, 0.060), mats["wall"], col, 0.010, 1)
        base.box(f"fp_ceiling_cross_rib_{index:02d}", (0.0, y + 0.34, 1.58), (4.30, 0.055, 0.090), mats["dark_metal"], col, 0.006, 1)

    for index, y in enumerate((-0.92, 0.62, 2.18, 3.70, 5.18)):
        base.strip_light(f"fp_narrow_overhead_light_{index:02d}", (0.0, y, 1.46), (0.72, 0.030, 0.045), col, mats)

    crate_specs = [
        ("left_near", -1.58, -0.42, -0.62, 0.98, 0.76, 0.58),
        ("right_near", 1.62, -0.10, -0.66, 1.05, 0.82, 0.64),
        ("left_mid", -1.54, 1.28, -0.70, 0.72, 0.62, 0.48),
        ("right_mid", 1.55, 1.92, -0.70, 0.74, 0.62, 0.50),
        ("left_far", -1.16, 3.28, -0.75, 0.50, 0.50, 0.40),
        ("right_far", 1.18, 4.02, -0.75, 0.52, 0.50, 0.42),
    ]
    for name, x, y, z, sx, sy, sz in crate_specs:
        base.build_cargo_crate(f"fp_corridor_crate_{name}", (x, y, z), (sx, sy, sz), col, mats, name.endswith("near"))

    base.box("fp_left_wall_service_box", (-2.28, 0.05, -0.10), (0.12, 0.48, 0.52), mats["dark_metal"], col, 0.014, 1)
    base.box("fp_left_wall_red_switch", (-2.36, -0.06, -0.02), (0.026, 0.060, 0.120), mats["red"], col, 0.004, 1)
    base.box("fp_far_dark_bulkhead", (0.0, 6.18, 0.08), (4.28, 0.10, 2.30), mats["black"], col, 0.024, 2)
    base.box("fp_far_center_door_panel", (0.0, 6.11, -0.12), (1.02, 0.045, 1.40), mats["dark_metal"], col, 0.016, 1)
    base.box("fp_far_door_red_status", (0.42, 6.07, 0.18), (0.045, 0.018, 0.045), mats["red"], col, 0.004, 1)
    base.box("fp_distant_red_warning_dot", (0.0, 5.82, 0.10), (0.06, 0.020, 0.06), mats["red"], col, 0.006, 1)

    base.build_hooked_staff(
        "fp_two_handed_long_hooked_staff",
        col,
        mats,
        loc=(0.98, -1.46, -0.24),
        scale=1.10,
        rot_z=math.radians(17.0),
    )
    base.glove_cluster("fp_lower_black_gloved_hand", (0.67, -1.54, -0.89), col, mats, 1.18)
    base.glove_cluster("fp_upper_black_gloved_hand", (0.99, -1.52, -0.31), col, mats, 0.98)
    base.curve_pipe(
        "fp_left_heavy_suit_sleeve",
        [(-0.52, -1.63, -1.36), (0.18, -1.57, -1.08), (0.67, -1.54, -0.89)],
        0.120,
        mats["leather"],
        col,
    )
    base.curve_pipe(
        "fp_right_heavy_suit_sleeve",
        [(1.84, -1.62, -0.72), (1.34, -1.55, -0.47), (0.99, -1.52, -0.31)],
        0.106,
        mats["leather"],
        col,
    )
    base.screen("fp_right_wrist_green_readout", (1.30, -1.61, -0.68), (0.36, 0.070, 0.22), mats, col, bars=True)
    base.cyl(
        "fp_small_background_musket_barrel",
        (-1.48, -0.15, -0.36),
        0.014,
        1.20,
        mats["edge_metal"],
        col,
        32,
        (0.0, math.radians(90.0), 0.0),
    )
    base.box("fp_small_background_musket_stock", (-1.88, -0.15, -0.38), (0.40, 0.065, 0.085), mats["rust"], col, 0.018, 2)

    base.box("fp_closeup_backdrop_wall", (0.58, -0.64, -0.18), (3.50, 0.08, 3.50), mats["black"], col, 0.010, 1)
    base.box("fp_closeup_backdrop_floor", (0.58, -1.02, -1.88), (3.50, 2.00, 0.08), mats["black"], col, 0.010, 1)

    for obj in col.objects:
        if obj.type == "LIGHT" and obj.name.startswith("fp_narrow_overhead_light_"):
            obj.data.energy *= 0.32


def set_render_quality() -> None:
    scene = bpy.context.scene
    scene.render.resolution_x = SLOT_RENDER_WIDTH
    scene.render.resolution_y = SLOT_RENDER_HEIGHT
    scene.view_settings.exposure = -0.28
    scene.view_settings.look = "Medium High Contrast"
    if hasattr(scene, "eevee"):
        if hasattr(scene.eevee, "taa_render_samples"):
            scene.eevee.taa_render_samples = 96
        if hasattr(scene.eevee, "use_bloom"):
            scene.eevee.use_bloom = True


def configure_first_person_slot_visibility(col, slot_name: str) -> None:
    for obj in col.objects:
        obj.hide_render = False
        obj.hide_viewport = False

    closeup = slot_name != "main"
    corridor_prefixes = (
        "fp_corridor_",
        "fp_floor_",
        "fp_side_",
        "fp_ceiling_",
        "fp_narrow_",
        "fp_left_wall_",
        "fp_far_",
        "fp_distant_",
        "fp_small_background_",
    )
    hand_prefixes = (
        "fp_lower_black_gloved_hand",
        "fp_upper_black_gloved_hand",
        "fp_left_heavy_suit_sleeve",
        "fp_right_heavy_suit_sleeve",
        "fp_right_wrist_green_readout",
    )

    for obj in col.objects:
        name = obj.name
        if name.startswith("fp_closeup_backdrop_"):
            obj.hide_render = not closeup
            obj.hide_viewport = not closeup
            continue
        if closeup and name.startswith(corridor_prefixes):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if slot_name in {"staff_full", "hook"} and name.startswith(hand_prefixes):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if slot_name == "wrist" and name.startswith("fp_two_handed_long_hooked_staff"):
            obj.hide_render = True
            obj.hide_viewport = True


def configure_cockpit_slot_visibility(col, slot_name: str) -> None:
    for obj in col.objects:
        obj.hide_render = False
        obj.hide_viewport = False

    closeup = slot_name != "main"
    iso_map = {
        "left_close": "cockpit_iso_terminal",
        "center_close": "cockpit_iso_helm",
        "screen_close": "cockpit_iso_screen",
    }
    active_iso_prefix = iso_map.get(slot_name)

    for obj in col.objects:
        name = obj.name
        if name.startswith("cockpit_closeup_black_backdrop_"):
            obj.hide_render = not closeup
            obj.hide_viewport = not closeup
            continue
        if closeup and name.startswith("cockpit_main_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if not closeup and name.startswith("cockpit_iso_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if closeup and name.startswith("cockpit_iso_terminal_ref_case"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if closeup and name.startswith("cockpit_iso_") and active_iso_prefix is not None and not name.startswith(active_iso_prefix):
            obj.hide_render = True
            obj.hide_viewport = True


def configure_control_room_slot_visibility(col, slot_name: str) -> None:
    for obj in col.objects:
        obj.hide_render = False
        obj.hide_viewport = False

    closeup = slot_name != "main"
    iso_map = {
        "large_screen": "control_iso_large_screen",
        "button_panel": "control_iso_button_panel",
        "pipe_detail": "control_iso_pipe_detail",
    }
    active_iso_prefix = iso_map.get(slot_name)

    for obj in col.objects:
        name = obj.name
        if name.startswith("control_iso_backdrop_"):
            obj.hide_render = not closeup
            obj.hide_viewport = not closeup
            continue
        if closeup and name.startswith("control_room_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if not closeup and name.startswith("control_iso_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if closeup and name.startswith("control_iso_") and active_iso_prefix is not None and not name.startswith(active_iso_prefix):
            obj.hide_render = True
            obj.hide_viewport = True


def configure_control_room_hybrid_slot_visibility(col, slot_name: str) -> None:
    for obj in col.objects:
        obj.hide_render = False
        obj.hide_viewport = False

    for obj in col.objects:
        name = obj.name
        if slot_name == "button_panel":
            if name.startswith("control_iso_backdrop_"):
                continue
            if name.startswith("control_room_"):
                obj.hide_render = True
                obj.hide_viewport = True
                continue
            if name.startswith("control_iso_") and not name.startswith("control_iso_button_panel"):
                obj.hide_render = True
                obj.hide_viewport = True
        elif name.startswith("control_iso_"):
            obj.hide_render = True
            obj.hide_viewport = True


def configure_cargo_hold_slot_visibility(col, slot_name: str) -> None:
    for obj in col.objects:
        obj.hide_render = False
        obj.hide_viewport = False

    closeup = slot_name != "main"
    iso_map = {
        "panel": "cargo_iso_panel",
        "large_crate": "cargo_iso_large_crate",
        "terminal": "cargo_iso_terminal",
    }
    active_iso_prefix = iso_map.get(slot_name)

    for obj in col.objects:
        name = obj.name
        if name.startswith("cargo_iso_backdrop_"):
            obj.hide_render = not closeup
            obj.hide_viewport = not closeup
            continue
        if closeup and name.startswith("cargo_hold_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if not closeup and name.startswith("cargo_iso_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if closeup and name.startswith("cargo_iso_") and active_iso_prefix is not None and not name.startswith(active_iso_prefix):
            obj.hide_render = True
            obj.hide_viewport = True


def configure_supply_room_slot_visibility(col, slot_name: str) -> None:
    for obj in col.objects:
        obj.hide_render = False
        obj.hide_viewport = False

    closeup = slot_name != "main"
    iso_map = {
        "door": "supply_iso_door",
        "handle": "supply_iso_handle",
        "cabinet_iso": "supply_iso_cabinet",
    }
    active_iso_prefix = iso_map.get(slot_name)

    for obj in col.objects:
        name = obj.name
        if name.startswith("supply_iso_backdrop_"):
            obj.hide_render = not closeup
            obj.hide_viewport = not closeup
            continue
        if closeup and name.startswith("supply_") and not name.startswith("supply_iso_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if not closeup and name.startswith("supply_iso_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if closeup and slot_name == "door" and name.startswith("supply_iso_door_single"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if closeup and name.startswith("supply_iso_") and active_iso_prefix is not None and not name.startswith(active_iso_prefix):
            obj.hide_render = True
            obj.hide_viewport = True


def configure_supply_room_hybrid_slot_visibility(col, slot_name: str) -> None:
    for obj in col.objects:
        obj.hide_render = False
        obj.hide_viewport = False

    for obj in col.objects:
        name = obj.name
        if slot_name == "door":
            if name.startswith("supply_") and not name.startswith("supply_iso_"):
                obj.hide_render = True
                obj.hide_viewport = True
        elif name.startswith("supply_iso_"):
            obj.hide_render = True
            obj.hide_viewport = True


def render_slot(item_id: str, slug: str, slot_name: str, col, collections, camera_settings) -> str:
    base.set_collection_visibility(collections, col)
    if item_id == "01":
        configure_cockpit_slot_visibility(col, slot_name)
    if item_id == "02":
        configure_control_room_hybrid_slot_visibility(col, slot_name)
    if item_id == "04":
        configure_supply_room_hybrid_slot_visibility(col, slot_name)
    if item_id == "05":
        configure_cargo_hold_slot_visibility(col, slot_name)
    if item_id == "07":
        configure_first_person_slot_visibility(col, slot_name)
    base.add_render_lights("true_model_" + item_id + "_" + slot_name)
    if item_id == "01":
        for obj in bpy.context.scene.objects:
            if obj.type != "LIGHT" or not obj.name.startswith("Render_true_model_01_"):
                continue
            if obj.name.endswith("_soft_key"):
                obj.data.energy *= 0.78
            elif obj.name.endswith("_warm_rim"):
                obj.data.energy *= 0.72
            elif obj.name.endswith("_crt_green_fill"):
                obj.data.energy *= 0.06
    if item_id == "04":
        for obj in bpy.context.scene.objects:
            if obj.type != "LIGHT" or not obj.name.startswith("Render_true_model_04_"):
                continue
            if obj.name.endswith("_soft_key"):
                obj.data.energy *= 1.16
                obj.data.color = (0.96, 0.92, 0.86)
            elif obj.name.endswith("_warm_rim"):
                obj.data.energy *= 0.72
            elif obj.name.endswith("_crt_green_fill"):
                obj.data.energy *= 0.08
    loc, target, lens = camera_settings
    base.add_render_camera("Render_true_model_" + item_id + "_" + slot_name + "_camera", loc, target, lens)
    filepath = os.path.join(SLOT_RENDER_DIR, f"{item_id}_{slug}_{slot_name}.png")
    bpy.context.scene.render.filepath = filepath
    bpy.ops.render.render(write_still=True)
    return filepath


def export_collection(item_id: str, slug: str, col) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    selected = []
    for obj in col.objects:
        if obj.type in {"MESH", "CURVE", "FONT", "LIGHT"}:
            obj.select_set(True)
            selected.append(obj)
    if selected:
        bpy.context.view_layer.objects.active = selected[0]
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(EXPORT_DIR, f"FBX_{item_id}_{slug}_true_model_v018.fbx"),
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH", "EMPTY", "LIGHT"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        axis_forward="-Z",
        axis_up="Y",
    )
    try:
        bpy.ops.export_scene.gltf(
            filepath=os.path.join(EXPORT_DIR, f"GLB_{item_id}_{slug}_true_model_v018.glb"),
            use_selection=True,
            export_format="GLB",
        )
    except Exception as exc:  # noqa: BLE001
        print("GLB export skipped for " + slug + ": " + str(exc))


def write_model_integrity_note() -> None:
    note = """# Stage 3 True Model Approval Sample

이 폴더는 Unity 적용 전 실제 Blender 모델링 승인 후보를 저장합니다.

- 원본 `artSample/stage3_rework_review/*.png` 이미지를 모델 표면에 projection texture로 붙이지 않습니다.
- trace scaffold, reference-lock, camera-matched board gallery는 승인 후보에서 제외합니다.
- 승인 후보는 Blender 메시, 커브, 조명, 절차적/베이크 가능한 머티리얼로 구성되어야 합니다.
- Unity 적용은 이 Blender 모델 샘플이 사용자 승인을 받은 뒤에만 진행합니다.
- 99% 비교 수치를 통과하지 못하면 `approval_status.json`과 `index.html`에 승인 불가 상태로 기록합니다.

주요 파일:

- `blender/Stage3_TrueModelApproval_v018.blend`
- `slots/`
- `renders/*_true_model_v018.png`
- `exports/FBX_*_true_model_v018.fbx`
- `exports/GLB_*_true_model_v018.glb`
- `textures/`
"""
    with open(os.path.join(SAMPLE_ROOT, "README.md"), "w", encoding="utf-8") as handle:
        handle.write(note)


def main() -> None:
    configure_base()
    ensure_dirs()
    base.clear_scene()
    base.configure_scene()
    set_render_quality()
    mats = base.create_materials()
    tune_true_model_materials(mats)

    builders = {
        "01": build_cockpit_complete_model,
        "02": build_control_room_complete_model,
        "03": base.build_engine_room,
        "04": build_supply_room_hybrid_model,
        "05": build_cargo_hold_complete_model,
        "06": base.build_armory,
        "07": build_first_person_complete_model,
    }

    collections = []
    by_id = {}
    for item_id, slug in SAMPLE_ITEMS:
        col = base.new_collection(f"Stage3_TrueModel_{item_id}_{slug}")
        builders[item_id](col, mats)
        collections.append(col)
        by_id[item_id] = col

    for item_id, slug in SAMPLE_ITEMS:
        for slot_name, camera_settings in SLOT_CAMERA_PRESETS[item_id].items():
            render_slot(item_id, slug, slot_name, by_id[item_id], collections, camera_settings)
        export_collection(item_id, slug, by_id[item_id])

    base.set_collection_visibility(collections, collections[0])
    blend_path = os.path.join(BLENDER_DIR, "Stage3_TrueModelApproval_v018.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    write_model_integrity_note()
    print("Stage 3 true model approval sample generated at " + SAMPLE_ROOT)


if __name__ == "__main__":
    main()
