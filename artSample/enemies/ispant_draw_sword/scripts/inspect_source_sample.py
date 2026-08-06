from pathlib import Path

import bpy


PROJECT_ROOT = Path(__file__).resolve().parents[4]
SOURCE_BLEND = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "ispant_armed"
    / "Ispant_Armed_Appearance_Sample.blend"
)
CURRENT_DRAW_FBX = (
    PROJECT_ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Enemies"
    / "Ispant"
    / "Animations"
    / "Ispant_DrawSword.fbx"
)


bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
print(
    "OBJECTS|"
    + "|".join(
        "{}:{}:{}".format(
            obj.name,
            obj.type,
            tuple(round(value, 5) for value in obj.dimensions),
        )
        for obj in bpy.context.scene.objects
    )
)
print("MATERIALS|" + "|".join(material.name for material in bpy.data.materials))
print(
    "ARMATURES|"
    + "|".join(
        "{}:{}".format(obj.name, len(obj.data.bones))
        for obj in bpy.context.scene.objects
        if obj.type == "ARMATURE"
    )
)
print(
    "SOURCE_UV|"
    + "|".join(
        "{}:{}".format(
            obj.name,
            ",".join(layer.name for layer in obj.data.uv_layers),
        )
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
    )
)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(CURRENT_DRAW_FBX))
print(
    "CURRENT_OBJECTS|"
    + "|".join(
        "{}:{}:{}:{}".format(
            obj.name,
            obj.type,
            tuple(round(value, 5) for value in obj.dimensions),
            ",".join(slot.material.name if slot.material else "NULL" for slot in obj.material_slots),
        )
        for obj in bpy.context.scene.objects
    )
)
print(
    "CURRENT_ACTIONS|"
    + "|".join(
        "{}:{}".format(action.name, tuple(action.frame_range))
        for action in bpy.data.actions
    )
)
print(
    "CURRENT_UV|"
    + "|".join(
        "{}:{}".format(
            obj.name,
            ",".join(layer.name for layer in obj.data.uv_layers),
        )
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
    )
)
