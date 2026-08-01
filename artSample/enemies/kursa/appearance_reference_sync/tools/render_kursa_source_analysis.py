import bpy
import colorsys
import json
import math
from pathlib import Path

from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"
INSPECTION = SAMPLE_ROOT / "SOURCE_MODEL_INSPECTION.json"
RENDER_DIR = SAMPLE_ROOT / "renders"
LEGEND = SAMPLE_ROOT / "COMPONENT_COLOR_LEGEND.json"


def bounds_of(objects):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects
        if obj.type == "MESH"
        for corner in obj.bound_box
    ]
    low = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    high = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    return low, high


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat(
        "-Z", "Y"
    ).to_euler()


def material(name, color, roughness=0.72, metallic=0.2, emission=False):
    item = bpy.data.materials.new(name)
    item.use_nodes = True
    shader = item.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metallic
    if emission:
        shader.inputs["Emission Color"].default_value = (*color, 1.0)
        shader.inputs["Emission Strength"].default_value = 0.35
    return item


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
    inspection = json.loads(INSPECTION.read_text(encoding="utf-8"))
    mesh_data = next(item for item in inspection["objects"] if item["type"] == "MESH")
    component_by_polygon = {
        polygon_index: component["component_id"]
        for component in mesh_data["connected_components"]
        for polygon_index in component["polygon_indices"]
    }

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    scene = bpy.context.scene
    mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH")
    low, high = bounds_of([mesh_obj])
    center = (low + high) * 0.5
    center.z = low.z + (high.z - low.z) * 0.51
    extent = high - low
    radius = max(extent.x, extent.y, extent.z)
    distance = radius * 1.78

    world = bpy.data.worlds.new("Kursa_Source_Analysis_World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (
        0.84,
        0.87,
        0.88,
        1.0,
    )
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.72

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.lens = 62
    scene.camera = camera

    for name, location, energy, size, color in (
        (
            "Key",
            center + Vector((-radius * 0.8, -radius, radius * 1.35)),
            360,
            radius * 1.35,
            (1.0, 0.91, 0.80),
        ),
        (
            "Fill",
            center + Vector((radius, -radius * 0.25, radius * 0.62)),
            170,
            radius * 1.1,
            (0.60, 0.78, 1.0),
        ),
        (
            "Rim",
            center + Vector((0.2 * radius, radius, radius * 1.05)),
            270,
            radius,
            (0.58, 0.73, 1.0),
        ),
    ):
        bpy.ops.object.light_add(type="AREA")
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.shape = "DISK"
        light.data.size = size
        light.data.color = color
        light.location = location
        point_at(light, center)

    bpy.ops.mesh.primitive_plane_add(
        size=7.0,
        location=(center.x, center.y, low.z - 0.006),
    )
    floor = bpy.context.object
    floor.data.materials.append(
        material("Kursa_Analysis_Floor", (0.68, 0.72, 0.73), 0.88, 0.0)
    )

    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.frame_set(1)

    mesh_obj.data.materials.clear()
    neutral = material("Kursa_Source_Neutral", (0.46, 0.49, 0.50), 0.58, 0.62)
    mesh_obj.data.materials.append(neutral)
    for polygon in mesh_obj.data.polygons:
        polygon.material_index = 0

    render_view(scene, camera, center, distance, 0, 0.06, "00_source_front_neutral.png")
    render_view(
        scene,
        camera,
        center,
        distance,
        32,
        0.08,
        "00_source_three_quarter_neutral.png",
    )
    render_view(scene, camera, center, distance, 90, 0.06, "00_source_side_neutral.png")
    render_view(scene, camera, center, distance, 180, 0.06, "00_source_rear_neutral.png")

    mesh_obj.data.materials.clear()
    color_legend = []
    for component in mesh_data["connected_components"]:
        component_id = component["component_id"]
        rgb = colorsys.hsv_to_rgb((component_id * 0.61803398875) % 1.0, 0.66, 0.84)
        mesh_obj.data.materials.append(
            material(f"Kursa_Component_{component_id:02d}", rgb, 0.68, 0.05, True)
        )
        color_legend.append(
            {
                "component_id": component_id,
                "rgb": [round(value, 6) for value in rgb],
                "hex": "#{:02X}{:02X}{:02X}".format(
                    *(round(value * 255) for value in rgb)
                ),
                "polygon_count": component["polygon_count"],
                "center_local": component["center_local"],
                "dominant_vertex_groups": component["dominant_vertex_groups"],
            }
        )
    for polygon in mesh_obj.data.polygons:
        polygon.material_index = component_by_polygon[polygon.index]

    render_view(scene, camera, center, distance, 0, 0.06, "00_component_mask_front.png")
    render_view(
        scene,
        camera,
        center,
        distance,
        32,
        0.08,
        "00_component_mask_three_quarter.png",
    )
    LEGEND.write_text(
        json.dumps(
            {
                "source_model": "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx",
                "component_count": len(color_legend),
                "note": (
                    "분석 렌더에서만 사용하는 기존 연결 표면 색상입니다. "
                    "메시 정점과 토폴로지는 변경하지 않았습니다."
                ),
                "components": color_legend,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "component_count": len(color_legend),
                "renders": 6,
                "bounds_world_min": [round(value, 6) for value in low],
                "bounds_world_max": [round(value, 6) for value in high],
            }
        )
    )


if __name__ == "__main__":
    main()
