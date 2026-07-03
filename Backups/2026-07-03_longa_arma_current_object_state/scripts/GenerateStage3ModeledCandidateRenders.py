from __future__ import annotations

import math
import os
import sys


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


OUTPUT_ROOT = os.path.join(PROJECT_ROOT, "artSample", "stage3_reproduction_sample")
SLOT_RENDER_DIR = os.path.join(OUTPUT_ROOT, "modeled_slots")
TEXTURE_DIR = os.path.join(OUTPUT_ROOT, "modeled_textures")
BLENDER_DIR = os.path.join(OUTPUT_ROOT, "blender")

SLOT_WIDTH = 960
SLOT_HEIGHT = 640

SAMPLE_ITEMS = [
    ("01", "cockpit_helm_and_status", "조종실 조타 장치와 상태 화면"),
    ("02", "control_room_cctv_terminal", "통제실 단일 대형 CCTV 스크린"),
    ("03", "engine_room_power_terminal", "동력실 전력 단말"),
    ("04", "supply_room_storage_cabinet", "비품창고 보관장"),
    ("05", "cargo_hold_props_and_terminal", "운송창고 소품과 입출력 단말"),
    ("06", "armory_turret_grip_mount", "무기실 포탑 손잡이 마운트"),
    ("07", "first_person_equipment", "1인칭 장비와 양손 막대기"),
]

CAMERA_PRESETS = {
    "01": {
        "main": ((2.50, -4.55, 1.05), (0.0, -0.15, 0.10), 31.0),
        "left_close": ((-1.95, -3.15, 0.95), (-0.20, -0.35, 0.05), 44.0),
        "center_close": ((0.05, -3.05, 0.45), (0.0, -0.80, -0.16), 58.0),
        "screen_close": ((1.65, -2.85, 0.72), (0.78, -0.48, 0.10), 54.0),
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
        "main": ((0.52, -4.75, 0.04), (0.38, -0.88, -0.22), 22.0),
        "staff_full": ((1.75, -3.15, 0.18), (0.88, -1.35, -0.08), 38.0),
        "hook": ((1.35, -2.35, 0.88), (1.35, -1.35, 0.82), 74.0),
        "wrist": ((0.90, -2.25, -0.70), (0.70, -1.48, -0.86), 64.0),
    },
}


def ensure_dirs() -> None:
    for path in (OUTPUT_ROOT, SLOT_RENDER_DIR, TEXTURE_DIR, BLENDER_DIR):
        os.makedirs(path, exist_ok=True)


def configure_imported_generator() -> None:
    base.PROJECT_ROOT = PROJECT_ROOT
    base.SOURCE_REVIEW_DIR = os.path.join(PROJECT_ROOT, "artSample", "stage3_rework_review")
    base.SAMPLE_ROOT = OUTPUT_ROOT
    base.RENDER_DIR = SLOT_RENDER_DIR
    base.EXPORT_DIR = os.path.join(OUTPUT_ROOT, "modeled_exports")
    base.TEXTURE_DIR = TEXTURE_DIR
    base.BLENDER_DIR = BLENDER_DIR
    base.RENDER_WIDTH = SLOT_WIDTH
    base.RENDER_HEIGHT = SLOT_HEIGHT


def build_collections(mats):
    builders = {
        "01": base.build_cockpit,
        "02": base.build_control_room,
        "03": base.build_engine_room,
        "04": base.build_supply_room,
        "05": base.build_cargo_hold,
        "06": base.build_armory,
        "07": base.build_first_person,
    }

    collections = []
    by_id = {}
    for item_id, slug, _title in SAMPLE_ITEMS:
        collection = base.new_collection(f"Stage3_Reproduction_Modeled_{item_id}_{slug}")
        builders[item_id](collection, mats)
        collections.append(collection)
        by_id[item_id] = collection
    return collections, by_id


def render_slot(item_id: str, slug: str, slot_name: str, collection, collections, camera_settings) -> None:
    base.set_collection_visibility(collections, collection)
    base.add_render_lights(f"{item_id}_{slot_name}")
    loc, target, lens = camera_settings
    base.add_render_camera(f"Render_{item_id}_{slot_name}_camera", loc, target, lens)
    base.bpy.context.scene.render.filepath = os.path.join(
        SLOT_RENDER_DIR,
        f"{item_id}_{slug}_{slot_name}.png",
    )
    base.bpy.ops.render.render(write_still=True)


def main() -> None:
    ensure_dirs()
    configure_imported_generator()
    base.clear_scene()
    base.configure_scene()
    mats = base.create_materials()
    collections, by_id = build_collections(mats)

    for item_id, slug, _title in SAMPLE_ITEMS:
        for slot_name, camera_settings in CAMERA_PRESETS[item_id].items():
            render_slot(item_id, slug, slot_name, by_id[item_id], collections, camera_settings)

    base.set_collection_visibility(collections, collections[0])
    blend_path = os.path.join(BLENDER_DIR, "Stage3_Reproduction_ModeledCandidates_v001.blend")
    base.bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print("Stage 3 modeled candidate slots generated at " + SLOT_RENDER_DIR)


if __name__ == "__main__":
    main()
