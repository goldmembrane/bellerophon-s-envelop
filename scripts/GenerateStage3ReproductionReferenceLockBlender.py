from __future__ import annotations

import os
import sys

import bpy
from mathutils import Vector


RENDER_WIDTH = 1536
RENDER_HEIGHT = 1024
BOARD_WIDTH = 15.36
BOARD_HEIGHT = 10.24

TARGETS = (
    ("01", "cockpit_helm_and_status", "조종실 조타 장치와 상태 화면"),
    ("02", "control_room_cctv_terminal", "통제실 단일 대형 CCTV 스크린"),
    ("03", "engine_room_power_terminal", "동력실 전력 단말"),
    ("04", "supply_room_storage_cabinet", "비품창고 보관장"),
    ("05", "cargo_hold_props_and_terminal", "운송창고 소품과 입출력 단말"),
    ("06", "armory_turret_grip_mount", "무기실 포탑 손잡이 마운트"),
    ("07", "first_person_equipment", "1인칭 장비와 양손 막대기"),
)


def parse_project_root() -> str:
    args = sys.argv
    extra = args[args.index("--") + 1 :] if "--" in args else []
    for index, value in enumerate(extra):
        if value == "--project-root" and index + 1 < len(extra):
            return os.path.abspath(extra[index + 1])
    return os.getcwd()


PROJECT_ROOT = parse_project_root()
REFERENCE_DIR = os.path.join(PROJECT_ROOT, "artSample", "stage3_rework_review")
OUTPUT_ROOT = os.path.join(PROJECT_ROOT, "artSample", "stage3_reproduction_sample")
RENDER_DIR = os.path.join(OUTPUT_ROOT, "renders")
BLENDER_DIR = os.path.join(OUTPUT_ROOT, "blender")


def ensure_dirs() -> None:
    os.makedirs(RENDER_DIR, exist_ok=True)
    os.makedirs(BLENDER_DIR, exist_ok=True)


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for datablocks in (
        bpy.data.meshes,
        bpy.data.materials,
        bpy.data.images,
        bpy.data.collections,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for item in list(datablocks):
            datablocks.remove(item)


def configure_scene() -> None:
    scene = bpy.context.scene
    for engine_name in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            scene.render.engine = engine_name
            break
        except TypeError:
            continue

    scene.render.resolution_x = RENDER_WIDTH
    scene.render.resolution_y = RENDER_HEIGHT
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0

    if scene.world is None:
        scene.world = bpy.data.worlds.new("Stage3ReferenceLockWorld")
    scene.world.color = (0.0, 0.0, 0.0)


def make_collection(name: str) -> bpy.types.Collection:
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    return collection


def make_emission_material(name: str, image_path: str) -> bpy.types.Material:
    image = bpy.data.images.load(image_path)
    image.colorspace_settings.name = "sRGB"

    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()

    texture = nodes.new(type="ShaderNodeTexImage")
    texture.image = image
    texture.extension = "CLIP"
    texture.interpolation = "Closest"

    emission = nodes.new(type="ShaderNodeEmission")
    emission.inputs["Strength"].default_value = 1.0

    output = nodes.new(type="ShaderNodeOutputMaterial")
    material.node_tree.links.new(texture.outputs["Color"], emission.inputs["Color"])
    material.node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def create_reference_board(item_id: str, slug: str, title: str) -> bpy.types.Collection:
    collection = make_collection(f"Stage3_ReferenceLock_{item_id}_{slug}")
    reference_path = os.path.join(REFERENCE_DIR, f"{item_id}_{slug}_review.png")
    if not os.path.exists(reference_path):
        raise FileNotFoundError(reference_path)

    material = make_emission_material(f"MAT_ReferenceLock_{item_id}_{slug}", reference_path)

    mesh = bpy.data.meshes.new(f"SM_ReferenceLock_{item_id}_{slug}")
    half_width = BOARD_WIDTH * 0.5
    half_height = BOARD_HEIGHT * 0.5
    vertices = (
        (-half_width, -half_height, 0.0),
        (half_width, -half_height, 0.0),
        (half_width, half_height, 0.0),
        (-half_width, half_height, 0.0),
    )
    faces = ((0, 1, 2, 3),)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    mesh.uv_layers.new(name="ReferenceUV")
    uv_data = mesh.uv_layers.active.data
    uv_values = ((0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0))
    for loop_index, uv in enumerate(uv_values):
        uv_data[loop_index].uv = uv

    board = bpy.data.objects.new(f"ReferenceLock_{item_id}_{slug}", mesh)
    board["ReviewIntent"] = "DCC camera-match baseline only; not modeled-asset completion."
    board["ReferenceTitle"] = title
    board.data.materials.append(material)
    collection.objects.link(board)

    return collection


def look_at(obj: bpy.types.Object, target: tuple[float, float, float]) -> None:
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_camera() -> bpy.types.Object:
    bpy.ops.object.camera_add(location=(0.0, 0.0, 10.0))
    camera = bpy.context.object
    camera.name = "ReferenceLock_OrthographicCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = BOARD_WIDTH
    look_at(camera, (0.0, 0.0, 0.0))
    bpy.context.scene.camera = camera
    return camera


def set_visible(collections: list[bpy.types.Collection], active: bpy.types.Collection) -> None:
    for collection in collections:
        visible = collection == active
        collection.hide_viewport = not visible
        collection.hide_render = not visible
        for obj in collection.objects:
            obj.hide_viewport = not visible
            obj.hide_render = not visible


def render_reference_locks(collections: list[bpy.types.Collection]) -> None:
    for (item_id, slug, _title), collection in zip(TARGETS, collections):
        set_visible(collections, collection)
        bpy.context.scene.render.filepath = os.path.join(
            RENDER_DIR,
            f"{item_id}_{slug}_dcc_reference_lock.png",
        )
        bpy.ops.render.render(write_still=True)


def main() -> None:
    ensure_dirs()
    clear_scene()
    configure_scene()
    collections = [create_reference_board(item_id, slug, title) for item_id, slug, title in TARGETS]
    add_camera()
    render_reference_locks(collections)
    set_visible(collections, collections[0])
    blend_path = os.path.join(BLENDER_DIR, "Stage3_Reproduction_ReferenceLock.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print("Stage 3 DCC reference-lock generated at " + OUTPUT_ROOT)


if __name__ == "__main__":
    main()
