import bpy
import colorsys
import json
import math
from pathlib import Path

from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx"
INSPECTION_JSON = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
RENDER_DIR = SAMPLE_ROOT / "renders"


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat(
        "-Z", "Y"
    ).to_euler()


def bounds_of(objects):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects
        if obj.type == "MESH"
        for corner in obj.bound_box
    ]
    return (
        Vector(tuple(min(point[axis] for point in points) for axis in range(3))),
        Vector(tuple(max(point[axis] for point in points) for axis in range(3))),
    )


def material(name, color, metallic=0.0, roughness=0.5, emission=False):
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    shader = result.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if emission:
        shader.inputs["Emission Color"].default_value = (*color, 1.0)
        shader.inputs["Emission Strength"].default_value = 0.45
    return result


def render_view(scene, camera, center, distance, angle, elevation, filename):
    radians = math.radians(angle)
    camera.location = center + Vector(
        (
            distance * math.sin(radians),
            -distance * math.cos(radians),
            distance * elevation,
        )
    )
    point_at(camera, center)
    scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def main():
    RENDER_DIR.mkdir(parents=True, exist_ok=True)
    inspection = json.loads(INSPECTION_JSON.read_text(encoding="utf-8"))
    component_data = next(
        item for item in inspection["objects"] if item["type"] == "MESH"
    )["connected_components"]

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    scene = bpy.context.scene
    mesh = next(obj for obj in scene.objects if obj.type == "MESH")
    armature = next(obj for obj in scene.objects if obj.type == "ARMATURE")

    neutral = material("Source_Neutral", (0.31, 0.36, 0.40), 0.48, 0.52)
    mesh.data.materials.clear()
    mesh.data.materials.append(neutral)
    for polygon in mesh.data.polygons:
        polygon.material_index = 0

    low, high = bounds_of([mesh])
    center = (low + high) * 0.5
    center.z = low.z + (high.z - low.z) * 0.53
    radius = max((high - low).x, (high - low).y, (high - low).z)
    distance = radius * 2.15

    world = bpy.data.worlds.new("AnalysisWorld")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (
        0.025,
        0.032,
        0.040,
        1.0,
    )
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.28

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.lens = 58
    scene.camera = camera

    bpy.ops.object.light_add(type="AREA")
    key = bpy.context.object
    key.name = "Key"
    key.data.energy = 900
    key.data.shape = "DISK"
    key.data.size = radius * 1.4
    key.location = center + Vector((-radius, -radius, radius * 1.5))
    point_at(key, center)

    bpy.ops.object.light_add(type="AREA")
    fill = bpy.context.object
    fill.name = "Fill"
    fill.data.energy = 600
    fill.data.size = radius
    fill.location = center + Vector((radius, -radius * 0.4, radius * 0.4))
    point_at(fill, center)

    bpy.ops.object.light_add(type="AREA")
    rim = bpy.context.object
    rim.name = "Rim"
    rim.data.energy = 1000
    rim.data.size = radius
    rim.location = center + Vector((0.0, radius, radius))
    point_at(rim, center)

    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"

    render_view(
        scene, camera, center, distance, 0, 0.08, "00_source_front_neutral.png"
    )
    render_view(
        scene,
        camera,
        center,
        distance,
        32,
        0.10,
        "00_source_three_quarter_neutral.png",
    )
    render_view(
        scene, camera, center, distance, 90, 0.08, "00_source_side_neutral.png"
    )
    render_view(
        scene, camera, center, distance, 180, 0.08, "00_source_rear_neutral.png"
    )

    mesh.data.materials.clear()
    component_colors = []
    for component in component_data:
        component_id = component["component_id"]
        hue = (component_id * 0.61803398875) % 1.0
        color = colorsys.hsv_to_rgb(hue, 0.72, 0.92)
        component_colors.append(
            {
                "component_id": component_id,
                "rgb": [round(channel, 6) for channel in color],
            }
        )
        mesh.data.materials.append(
            material(
                f"Component_{component_id:02d}",
                color,
                metallic=0.0,
                roughness=0.75,
                emission=True,
            )
        )
        for polygon_index in component["polygon_indices"]:
            mesh.data.polygons[polygon_index].material_index = component_id

    world.node_tree.nodes["Background"].inputs["Color"].default_value = (
        0.005,
        0.005,
        0.008,
        1.0,
    )
    key.hide_render = True
    fill.hide_render = True
    rim.hide_render = True
    render_view(
        scene, camera, center, distance, 0, 0.08, "00_component_mask_front.png"
    )
    render_view(
        scene,
        camera,
        center,
        distance,
        32,
        0.10,
        "00_component_mask_three_quarter.png",
    )

    (SAMPLE_ROOT / "COMPONENT_COLOR_LEGEND.json").write_text(
        json.dumps(
            {
                "source": "SOURCE_MODEL_INSPECTION.json",
                "colors": component_colors,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )

    armature.hide_viewport = False
    print(
        json.dumps(
            {
                "renders": 6,
                "component_count": len(component_data),
                "bounds": {
                    "min": list(low),
                    "max": list(high),
                },
            }
        )
    )


if __name__ == "__main__":
    main()
