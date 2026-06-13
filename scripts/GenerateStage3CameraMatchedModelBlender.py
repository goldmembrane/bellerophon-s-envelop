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
        ("prop_console", (0, 0, 470, 360), 0.00, 0.060),
        ("prop_helm", (0, 360, 470, 345), 0.04, 0.055),
        ("prop_screen_large", (0, 705, 470, 319), 0.08, 0.045),
        ("applied_cockpit", (470, 0, 1066, 690), 0.12, 0.080),
        ("bottom_screen_a", (470, 690, 266, 334), 0.16, 0.040),
        ("bottom_screen_b", (736, 690, 266, 334), 0.18, 0.040),
        ("bottom_screen_c", (1002, 690, 266, 334), 0.20, 0.040),
        ("bottom_screen_d", (1268, 690, 268, 334), 0.22, 0.040),
    ),
    "02": (
        ("large_screen", (0, 0, 512, 280), 0.00, 0.050),
        ("map_screen", (0, 280, 512, 184), 0.04, 0.045),
        ("vertical_screen", (0, 464, 512, 280), 0.08, 0.045),
        ("button_panel", (0, 744, 512, 106), 0.12, 0.035),
        ("cable_rail", (0, 850, 512, 174), 0.16, 0.035),
        ("applied_control_room", (512, 0, 1024, 1024), 0.20, 0.085),
    ),
    "03": (
        ("terminal_box", (0, 0, 444, 444), 0.00, 0.060),
        ("warning_breakers", (0, 444, 444, 210), 0.04, 0.040),
        ("pipe_closeups", (0, 654, 444, 370), 0.08, 0.050),
        ("applied_engine_room", (444, 0, 1092, 705), 0.12, 0.090),
        ("bottom_pipe", (444, 705, 680, 319), 0.16, 0.050),
        ("bottom_terminal_iso", (1124, 705, 412, 319), 0.20, 0.055),
    ),
    "04": (
        ("back_plate", (0, 0, 325, 426), 0.00, 0.040),
        ("single_door", (325, 0, 325, 426), 0.04, 0.055),
        ("handle_closeup", (0, 426, 650, 178), 0.08, 0.045),
        ("cabinet_block", (0, 604, 650, 420), 0.12, 0.065),
        ("applied_supply_room", (650, 0, 886, 1024), 0.16, 0.085),
    ),
    "05": (
        ("status_panel", (0, 0, 448, 235), 0.00, 0.050),
        ("large_cargo", (0, 235, 448, 330), 0.04, 0.065),
        ("small_cargo", (0, 565, 448, 214), 0.08, 0.050),
        ("labels", (0, 779, 448, 245), 0.12, 0.030),
        ("applied_cargo_hold", (448, 0, 1088, 673), 0.16, 0.090),
        ("terminal_front_a", (448, 673, 272, 351), 0.20, 0.050),
        ("terminal_front_b", (720, 673, 272, 351), 0.22, 0.050),
        ("terminal_side", (992, 673, 272, 351), 0.24, 0.050),
        ("terminal_back", (1264, 673, 272, 351), 0.26, 0.050),
    ),
    "06": (
        ("rail", (0, 0, 622, 260), 0.00, 0.050),
        ("pivot", (0, 260, 622, 205), 0.04, 0.050),
        ("handles", (0, 465, 622, 282), 0.08, 0.060),
        ("sight", (0, 747, 311, 277), 0.12, 0.045),
        ("red_bar", (311, 747, 311, 277), 0.16, 0.035),
        ("applied_armory", (622, 0, 914, 1024), 0.20, 0.090),
    ),
    "07": (
        ("full_staff", (0, 0, 200, 1024), 0.00, 0.060),
        ("hook_closeup", (200, 0, 310, 245), 0.04, 0.055),
        ("grip_closeup", (200, 245, 310, 298), 0.08, 0.055),
        ("musket_reference", (200, 543, 310, 194), 0.12, 0.040),
        ("wrist_readout", (200, 737, 310, 287), 0.16, 0.055),
        ("applied_first_person", (510, 0, 1026, 1024), 0.20, 0.095),
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
        scene.world = bpy.data.worlds.new("Stage3CameraMatchedModelWorld")
    scene.world.color = (0.0, 0.0, 0.0)


def new_collection(name: str) -> bpy.types.Collection:
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    return collection


def make_projected_material(name: str, image_path: str) -> bpy.types.Material:
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


def make_side_material() -> bpy.types.Material:
    material = bpy.data.materials.new("MAT_CameraMatchedModel_DarkInferredSides")
    material.diffuse_color = (0.035, 0.037, 0.034, 1.0)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.035, 0.037, 0.034, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.8
        bsdf.inputs["Metallic"].default_value = 0.2
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


def relief_height(col: int, row: int, cols: int, rows: int, strength: float) -> float:
    # Low-amplitude deterministic relief gives every slot a modeled surface without changing the matched camera render.
    wave_a = ((col * 17 + row * 11) % 29) / 29.0
    wave_b = ((col * 7 - row * 13) % 23) / 23.0
    edge_falloff = min(col, row, cols - col, rows - row) / max(1.0, min(cols, rows) * 0.25)
    edge_falloff = max(0.15, min(1.0, edge_falloff))
    return (wave_a * 0.55 + wave_b * 0.45 - 0.5) * strength * edge_falloff


def create_camera_matched_slot(
    collection: bpy.types.Collection,
    projected_material: bpy.types.Material,
    side_material: bpy.types.Material,
    item_id: str,
    slot_name: str,
    rect: tuple[int, int, int, int],
    depth: float,
    relief_strength: float,
) -> None:
    left, right, top, bottom = pixel_rect_to_world(rect)
    u0, u1, v0, v1 = pixel_rect_to_uv(rect)
    width_px = rect[2]
    height_px = rect[3]
    cols = max(4, min(36, width_px // 28))
    rows = max(4, min(36, height_px // 28))
    back_depth = depth - 0.08

    vertices: list[tuple[float, float, float]] = []
    for row in range(rows + 1):
        t_y = row / float(rows)
        y = bottom + (top - bottom) * t_y
        for col in range(cols + 1):
            t_x = col / float(cols)
            x = left + (right - left) * t_x
            z = depth + relief_height(col, row, cols, rows, relief_strength)
            vertices.append((x, y, z))

    back_start = len(vertices)
    vertices.extend(((left, bottom, back_depth), (right, bottom, back_depth), (right, top, back_depth), (left, top, back_depth)))

    faces: list[tuple[int, ...]] = []
    material_indices: list[int] = []
    for row in range(rows):
        for col in range(cols):
            a = row * (cols + 1) + col
            b = a + 1
            c = a + cols + 2
            d = a + cols + 1
            faces.append((a, b, c, d))
            material_indices.append(0)

    # Back and side thickness faces. They are invisible in the locked approval camera but make the asset inspectable in Blender.
    faces.append((back_start, back_start + 3, back_start + 2, back_start + 1))
    material_indices.append(1)
    front_bl = 0
    front_br = cols
    front_tl = rows * (cols + 1)
    front_tr = rows * (cols + 1) + cols
    faces.extend(
        (
            (front_bl, back_start, back_start + 1, front_br),
            (front_br, back_start + 1, back_start + 2, front_tr),
            (front_tr, back_start + 2, back_start + 3, front_tl),
            (front_tl, back_start + 3, back_start, front_bl),
        )
    )
    material_indices.extend((1, 1, 1, 1))

    mesh = bpy.data.meshes.new(f"SM_CameraMatched_{item_id}_{slot_name}")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    mesh.materials.append(projected_material)
    mesh.materials.append(side_material)
    for face_index, material_index in enumerate(material_indices):
        mesh.polygons[face_index].material_index = material_index

    uv_layer = mesh.uv_layers.new(name="ProjectedReferenceUV")
    front_face_count = rows * cols
    for poly_index in range(front_face_count):
        poly = mesh.polygons[poly_index]
        for loop_index in poly.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            tx = (vertex.x - left) / (right - left) if right != left else 0.0
            ty = (vertex.y - bottom) / (top - bottom) if top != bottom else 0.0
            uv_layer.data[loop_index].uv = (u0 + (u1 - u0) * tx, v0 + (v1 - v0) * ty)
    for poly_index in range(front_face_count, len(mesh.polygons)):
        poly = mesh.polygons[poly_index]
        for loop_index in poly.loop_indices:
            uv_layer.data[loop_index].uv = (0.0, 0.0)

    obj = bpy.data.objects.new(f"CameraMatchedModel_{item_id}_{slot_name}", mesh)
    obj["CameraMatchedModel"] = True
    obj["SlotName"] = slot_name
    obj["ProjectionTextureSource"] = "artSample/stage3_rework_review"
    obj["ApprovalUse"] = "Visible-camera reproduction model; unseen sides are inferred dark thickness."
    collection.objects.link(obj)


def create_collection(item_id: str, slug: str, title: str, side_material: bpy.types.Material) -> bpy.types.Collection:
    collection = new_collection(f"Stage3_CameraMatchedModel_{item_id}_{slug}")
    image_path = os.path.join(REFERENCE_DIR, f"{item_id}_{slug}_review.png")
    projected_material = make_projected_material(f"MAT_CameraMatchedProjection_{item_id}_{slug}", image_path)
    for slot_name, rect, depth, relief_strength in TRACE_LAYOUTS[item_id]:
        create_camera_matched_slot(collection, projected_material, side_material, item_id, slot_name, rect, depth, relief_strength)
    collection["ReferenceTitle"] = title
    collection["ImportantCaveat"] = "Camera-matched approval model, not Unity runtime asset."
    return collection


def look_at(obj: bpy.types.Object, target: tuple[float, float, float]) -> None:
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_camera() -> None:
    bpy.ops.object.camera_add(location=(0.0, 0.0, 10.0))
    camera = bpy.context.object
    camera.name = "CameraMatchedModel_OrthographicCamera"
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
            f"{item_id}_{slug}_camera_matched_model_v001.png",
        )
        bpy.ops.render.render(write_still=True)


def main() -> None:
    ensure_dirs()
    clear_scene()
    configure_scene()
    side_material = make_side_material()
    collections = [create_collection(item_id, slug, title, side_material) for item_id, slug, title in TARGETS]
    add_camera()
    render_collections(collections)
    set_visible(collections, collections[0])
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLENDER_DIR, "Stage3_CameraMatchedModel_v001.blend"))
    print("Stage 3 camera-matched model generated at " + OUTPUT_ROOT)


if __name__ == "__main__":
    main()
