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

TRACE_LAYOUTS = {
    "01": (
        ("prop_console", (0, 0, 470, 360), 0.00),
        ("prop_helm", (0, 360, 470, 345), 0.01),
        ("prop_screen_large", (0, 705, 470, 319), 0.02),
        ("applied_cockpit", (470, 0, 1066, 690), 0.03),
        ("bottom_screen_a", (470, 690, 266, 334), 0.04),
        ("bottom_screen_b", (736, 690, 266, 334), 0.05),
        ("bottom_screen_c", (1002, 690, 266, 334), 0.06),
        ("bottom_screen_d", (1268, 690, 268, 334), 0.07),
    ),
    "02": (
        ("large_screen", (0, 0, 512, 280), 0.00),
        ("map_screen", (0, 280, 512, 184), 0.01),
        ("vertical_screen", (0, 464, 512, 280), 0.02),
        ("button_panel", (0, 744, 512, 106), 0.03),
        ("cable_rail", (0, 850, 512, 174), 0.04),
        ("applied_control_room", (512, 0, 1024, 1024), 0.05),
    ),
    "03": (
        ("terminal_box", (0, 0, 444, 444), 0.00),
        ("warning_breakers", (0, 444, 444, 210), 0.01),
        ("pipe_closeups", (0, 654, 444, 370), 0.02),
        ("applied_engine_room", (444, 0, 1092, 705), 0.03),
        ("bottom_pipe", (444, 705, 680, 319), 0.04),
        ("bottom_terminal_iso", (1124, 705, 412, 319), 0.05),
    ),
    "04": (
        ("back_plate", (0, 0, 325, 426), 0.00),
        ("single_door", (325, 0, 325, 426), 0.01),
        ("handle_closeup", (0, 426, 650, 178), 0.02),
        ("cabinet_block", (0, 604, 650, 420), 0.03),
        ("applied_supply_room", (650, 0, 886, 1024), 0.04),
    ),
    "05": (
        ("status_panel", (0, 0, 448, 235), 0.00),
        ("large_cargo", (0, 235, 448, 330), 0.01),
        ("small_cargo", (0, 565, 448, 214), 0.02),
        ("labels", (0, 779, 448, 245), 0.03),
        ("applied_cargo_hold", (448, 0, 1088, 673), 0.04),
        ("terminal_front_a", (448, 673, 272, 351), 0.05),
        ("terminal_front_b", (720, 673, 272, 351), 0.06),
        ("terminal_side", (992, 673, 272, 351), 0.07),
        ("terminal_back", (1264, 673, 272, 351), 0.08),
    ),
    "06": (
        ("rail", (0, 0, 622, 260), 0.00),
        ("pivot", (0, 260, 622, 205), 0.01),
        ("handles", (0, 465, 622, 282), 0.02),
        ("sight", (0, 747, 311, 277), 0.03),
        ("red_bar", (311, 747, 311, 277), 0.04),
        ("applied_armory", (622, 0, 914, 1024), 0.05),
    ),
    "07": (
        ("full_staff", (0, 0, 200, 1024), 0.00),
        ("hook_closeup", (200, 0, 310, 245), 0.01),
        ("grip_closeup", (200, 245, 310, 298), 0.02),
        ("musket_reference", (200, 543, 310, 194), 0.03),
        ("wrist_readout", (200, 737, 310, 287), 0.04),
        ("applied_first_person", (510, 0, 1026, 1024), 0.05),
    ),
}


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
        scene.world = bpy.data.worlds.new("Stage3TraceScaffoldWorld")
    scene.world.color = (0.0, 0.0, 0.0)


def new_collection(name: str) -> bpy.types.Collection:
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    return collection


def make_material(name: str, image_path: str) -> bpy.types.Material:
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


def pixel_rect_to_world(rect: tuple[int, int, int, int]) -> tuple[float, float, float, float]:
    x, y, width, height = rect
    left = x / 100.0 - BOARD_WIDTH * 0.5
    right = (x + width) / 100.0 - BOARD_WIDTH * 0.5
    top = BOARD_HEIGHT * 0.5 - y / 100.0
    bottom = BOARD_HEIGHT * 0.5 - (y + height) / 100.0
    return left, right, top, bottom


def pixel_rect_to_uv(rect: tuple[int, int, int, int]) -> tuple[float, float, float, float]:
    x, y, width, height = rect
    u0 = x / float(RENDER_WIDTH)
    u1 = (x + width) / float(RENDER_WIDTH)
    v0 = 1.0 - (y + height) / float(RENDER_HEIGHT)
    v1 = 1.0 - y / float(RENDER_HEIGHT)
    return u0, u1, v0, v1


def create_trace_plane(collection, material, item_id: str, slot_name: str, rect, depth: float) -> None:
    left, right, top, bottom = pixel_rect_to_world(rect)
    u0, u1, v0, v1 = pixel_rect_to_uv(rect)

    mesh = bpy.data.meshes.new(f"SM_Trace_{item_id}_{slot_name}")
    vertices = (
        (left, bottom, depth),
        (right, bottom, depth),
        (right, top, depth),
        (left, top, depth),
    )
    faces = ((0, 1, 2, 3),)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    mesh.uv_layers.new(name="ReferenceSlotUV")
    uv_data = mesh.uv_layers.active.data
    for loop_index, uv in enumerate(((u0, v0), (u1, v0), (u1, v1), (u0, v1))):
        uv_data[loop_index].uv = uv

    obj = bpy.data.objects.new(f"TraceScaffold_{item_id}_{slot_name}", mesh)
    obj["TraceScaffold"] = True
    obj["SlotName"] = slot_name
    obj["ReviewIntent"] = "Camera-match slot scaffold; replace this plane with modeled geometry in later passes."
    obj.data.materials.append(material)
    collection.objects.link(obj)


def create_trace_collection(item_id: str, slug: str, title: str) -> bpy.types.Collection:
    collection = new_collection(f"Stage3_TraceScaffold_{item_id}_{slug}")
    image_path = os.path.join(REFERENCE_DIR, f"{item_id}_{slug}_review.png")
    material = make_material(f"MAT_TraceScaffold_{item_id}_{slug}", image_path)
    for slot_name, rect, depth in TRACE_LAYOUTS[item_id]:
        create_trace_plane(collection, material, item_id, slot_name, rect, depth)
    collection["ReferenceTitle"] = title
    return collection


def look_at(obj: bpy.types.Object, target: tuple[float, float, float]) -> None:
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_camera() -> None:
    bpy.ops.object.camera_add(location=(0.0, 0.0, 10.0))
    camera = bpy.context.object
    camera.name = "TraceScaffold_OrthographicCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = BOARD_WIDTH
    look_at(camera, (0.0, 0.0, 0.0))
    bpy.context.scene.camera = camera


def set_visible(collections: list[bpy.types.Collection], active: bpy.types.Collection) -> None:
    for collection in collections:
        visible = collection == active
        collection.hide_viewport = not visible
        collection.hide_render = not visible
        for obj in collection.objects:
            obj.hide_viewport = not visible
            obj.hide_render = not visible


def render_collections(collections: list[bpy.types.Collection]) -> None:
    for (item_id, slug, _title), collection in zip(TARGETS, collections):
        set_visible(collections, collection)
        bpy.context.scene.render.filepath = os.path.join(
            RENDER_DIR,
            f"{item_id}_{slug}_trace_scaffold_v001.png",
        )
        bpy.ops.render.render(write_still=True)


def main() -> None:
    ensure_dirs()
    clear_scene()
    configure_scene()
    collections = [create_trace_collection(item_id, slug, title) for item_id, slug, title in TARGETS]
    add_camera()
    render_collections(collections)
    set_visible(collections, collections[0])
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLENDER_DIR, "Stage3_TraceScaffold_v001.blend"))
    print("Stage 3 trace scaffold generated at " + OUTPUT_ROOT)


if __name__ == "__main__":
    main()
