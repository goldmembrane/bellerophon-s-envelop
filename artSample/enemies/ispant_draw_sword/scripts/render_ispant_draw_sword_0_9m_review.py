import importlib.util
from pathlib import Path

import bpy


PROJECT_ROOT = Path(__file__).resolve().parents[4]
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / "ispant_draw_sword"
REVISION_DIR = SAMPLE_ROOT / "length_0_9m_revision"
BLEND = REVISION_DIR / "Ispant_DrawSword_0_9m_ArtSample.blend"
OUTPUT = REVISION_DIR / "Ispant_DrawSword_0_9m_Full.png"
HELPER_PATH = SAMPLE_ROOT / "scripts" / "build_ispant_draw_sword_art_sample.py"


def main():
    spec = importlib.util.spec_from_file_location("ispant_draw_sword_review_helper", HELPER_PATH)
    helper = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(helper)
    bpy.ops.wm.open_mainfile(filepath=str(BLEND))
    visible_meshes = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
        and obj.name not in {"Ispant_DrawSword_RigidSword", "Ispant_DrawSword_RigidSheath"}
    ]
    helper.render_full(OUTPUT, visible_meshes)
    print("ISPANT_DRAW_SWORD_0_9M_REVIEW_RENDER=PASS")
    print(f"ISPANT_DRAW_SWORD_0_9M_REVIEW_RENDER_OUTPUT={OUTPUT}")


if __name__ == "__main__":
    main()
