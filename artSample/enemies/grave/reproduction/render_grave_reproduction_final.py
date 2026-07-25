from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parent
OUTPUT_PATH = ROOT / "review" / "grave_reproduction_front_rgba.png"

bpy.context.scene.render.filepath = str(OUTPUT_PATH)
bpy.context.scene.render.film_transparent = True
bpy.ops.render.render(write_still=True)
print(f"GRAVE_FINAL_RENDER={OUTPUT_PATH}")
