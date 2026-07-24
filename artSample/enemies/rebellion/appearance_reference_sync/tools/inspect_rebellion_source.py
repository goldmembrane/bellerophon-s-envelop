import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE_GLB = (
    PROJECT_ROOT
    / "Assets"
    / "_Project"
    / "Art"
    / "Enemies"
    / "Rebellion"
    / "Models"
    / "Rebellion.glb"
)
SAMPLE_ROOT = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "appearance_reference_sync"
)
RENDER_DIR = SAMPLE_ROOT / "renders"
REPORT_PATH = SAMPLE_ROOT / "SOURCE_INSPECTION.json"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def world_bounds(objects):
    corners = []
    for obj in objects:
        if obj.type != "MESH":
            continue
        corners.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    if not corners:
        raise RuntimeError("Rebellion GLB has no mesh bounds.")
    minimum = Vector(
        (
            min(point.x for point in corners),
            min(point.y for point in corners),
            min(point.z for point in corners),
        )
    )
    maximum = Vector(
        (
            max(point.x for point in corners),
            max(point.y for point in corners),
            max(point.z for point in corners),
        )
    )
    return minimum, maximum


def look_at(camera, target):
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_area_light(name, location, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = location
    return obj


def render_views(mesh_objects, minimum, maximum):
    RENDER_DIR.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 960
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.color = (0.035, 0.035, 0.035)

    target = (minimum + maximum) * 0.5
    size = maximum - minimum
    scale = max(size.x, size.y, size.z)

    camera_data = bpy.data.cameras.new("Rebellion_Source_Inspection_Camera")
    camera = bpy.data.objects.new(
        "Rebellion_Source_Inspection_Camera", camera_data
    )
    bpy.context.scene.collection.objects.link(camera)
    camera.data.lens = 58.0
    scene.camera = camera

    add_area_light(
        "Inspection_Key",
        target + Vector((-scale * 1.4, -scale * 1.5, scale * 2.0)),
        1800.0,
        scale * 1.2,
        (1.0, 0.91, 0.80),
    )
    add_area_light(
        "Inspection_Fill",
        target + Vector((scale * 1.7, -scale * 0.5, scale * 0.8)),
        1000.0,
        scale,
        (0.58, 0.72, 1.0),
    )
    add_area_light(
        "Inspection_Rim",
        target + Vector((0.0, scale * 1.6, scale * 1.4)),
        1500.0,
        scale,
        (0.72, 0.83, 1.0),
    )

    views = {
        "raw_front_negative_y.png": Vector((0.0, -2.45, 0.55)),
        "raw_right_positive_x.png": Vector((2.45, 0.0, 0.55)),
        "raw_back_positive_y.png": Vector((0.0, 2.45, 0.55)),
        "raw_three_quarter.png": Vector((1.65, -1.85, 0.75)),
    }
    for filename, relative in views.items():
        camera.location = target + relative * scale
        look_at(camera, target)
        scene.render.filepath = str(RENDER_DIR / filename)
        bpy.ops.render.render(write_still=True)


def vec(value):
    return [round(float(component), 9) for component in value]


def main():
    if not SOURCE_GLB.exists():
        raise FileNotFoundError(SOURCE_GLB)
    clear_scene()
    bpy.ops.import_scene.gltf(filepath=str(SOURCE_GLB))
    imported_objects = list(bpy.context.scene.objects)
    mesh_objects = [obj for obj in imported_objects if obj.type == "MESH"]
    armatures = [obj for obj in imported_objects if obj.type == "ARMATURE"]
    minimum, maximum = world_bounds(mesh_objects)

    report = {
        "source_glb": str(SOURCE_GLB.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "mesh_objects": [
            {
                "name": obj.name,
                "vertices": len(obj.data.vertices),
                "polygons": len(obj.data.polygons),
                "materials": [
                    slot.material.name if slot.material else None
                    for slot in obj.material_slots
                ],
                "vertex_groups": len(obj.vertex_groups),
                "modifiers": [
                    {"name": modifier.name, "type": modifier.type}
                    for modifier in obj.modifiers
                ],
            }
            for obj in mesh_objects
        ],
        "armatures": [
            {
                "name": obj.name,
                "bones": len(obj.data.bones),
                "bone_names": [bone.name for bone in obj.data.bones],
            }
            for obj in armatures
        ],
        "bounds": {
            "min": vec(minimum),
            "max": vec(maximum),
            "size": vec(maximum - minimum),
        },
        "objects": [
            {"name": obj.name, "type": obj.type, "parent": obj.parent.name if obj.parent else None}
            for obj in imported_objects
        ],
    }
    SAMPLE_ROOT.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    render_views(mesh_objects, minimum, maximum)
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
