import bpy
import json
from collections import defaultdict, deque
from pathlib import Path
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[5]
SOURCE_BLEND = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "front_artifact_removal"
    / "blender"
    / "Rebellion_FrontArtifactRemoved.blend"
)
OUTPUT_ROOT = (
    PROJECT_ROOT
    / "artSample"
    / "enemies"
    / "rebellion"
    / "attack_transition_rig_support"
)
REPORT_PATH = OUTPUT_ROOT / "analysis" / "DISC_REGION_ANALYSIS.json"
FRONT_RENDER = OUTPUT_ROOT / "analysis" / "disc_region_front.png"
SIDE_RENDER = OUTPUT_ROOT / "analysis" / "disc_region_side.png"
DISC_MATERIAL_NAME = "Rebellion_Worn_Disc_Steel"
MIN_POLYGON_CENTER_Z = 1.30
MAX_POLYGON_CENTER_RADIUS = 1.31
SEED_MINIMUM_Z = 1.43


def find_skinned_mesh():
    candidates = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
        and obj.vertex_groups
        and any(modifier.type == "ARMATURE" for modifier in obj.modifiers)
    ]
    if len(candidates) != 1:
        raise RuntimeError(f"Expected one skinned mesh, found {len(candidates)}")
    return candidates[0]


def world_center(mesh_object, polygon):
    return mesh_object.matrix_world @ polygon.center


def select_disc_polygons(mesh_object):
    mesh = mesh_object.data
    material_index = next(
        (
            index
            for index, slot in enumerate(mesh_object.material_slots)
            if slot.material and slot.material.name == DISC_MATERIAL_NAME
        ),
        None,
    )
    if material_index is None:
        raise RuntimeError(f"Missing material: {DISC_MATERIAL_NAME}")

    edge_to_polygons = defaultdict(list)
    polygon_edges = {}
    for polygon in mesh.polygons:
        edges = []
        vertices = list(polygon.vertices)
        for index, first in enumerate(vertices):
            second = vertices[(index + 1) % len(vertices)]
            edge = tuple(sorted((first, second)))
            edges.append(edge)
            edge_to_polygons[edge].append(polygon.index)
        polygon_edges[polygon.index] = edges

    allowed = set()
    seeds = set()
    for polygon in mesh.polygons:
        if polygon.material_index != material_index:
            continue
        center = world_center(mesh_object, polygon)
        radius = (center.x * center.x + center.y * center.y) ** 0.5
        if (
            center.z >= MIN_POLYGON_CENTER_Z
            and radius <= MAX_POLYGON_CENTER_RADIUS
        ):
            allowed.add(polygon.index)
            if center.z >= SEED_MINIMUM_Z:
                seeds.add(polygon.index)

    selected = set()
    queue = deque(sorted(seeds))
    while queue:
        polygon_index = queue.popleft()
        if polygon_index in selected or polygon_index not in allowed:
            continue
        selected.add(polygon_index)
        for edge in polygon_edges[polygon_index]:
            for neighbor in edge_to_polygons[edge]:
                if neighbor in allowed and neighbor not in selected:
                    queue.append(neighbor)
    return selected, material_index


def analyze_boundary(mesh_object, selected_polygons):
    selected_vertices = {
        vertex_index
        for polygon_index in selected_polygons
        for vertex_index in mesh_object.data.polygons[polygon_index].vertices
    }
    nonselected_vertices = {
        vertex_index
        for polygon in mesh_object.data.polygons
        if polygon.index not in selected_polygons
        for vertex_index in polygon.vertices
    }
    boundary_vertices = selected_vertices & nonselected_vertices
    positions = [
        mesh_object.matrix_world @ mesh_object.data.vertices[index].co
        for index in selected_vertices
    ]
    minimum = Vector(
        (
            min(position.x for position in positions),
            min(position.y for position in positions),
            min(position.z for position in positions),
        )
    )
    maximum = Vector(
        (
            max(position.x for position in positions),
            max(position.y for position in positions),
            max(position.z for position in positions),
        )
    )
    return selected_vertices, boundary_vertices, minimum, maximum


def material(name, color, metallic, roughness):
    result = bpy.data.materials.new(name)
    result.diffuse_color = (*color, 1.0)
    result.use_nodes = True
    node = result.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = (*color, 1.0)
    node.inputs["Metallic"].default_value = metallic
    node.inputs["Roughness"].default_value = roughness
    return result


def look_at(camera, target):
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_diagnostic_materials(mesh_object, selected_polygons):
    selected_material = material(
        "AttackTransition_DiscCandidate",
        (0.85, 0.04, 0.02),
        0.15,
        0.4,
    )
    other_material = material(
        "AttackTransition_Untouched",
        (0.025, 0.06, 0.10),
        0.2,
        0.55,
    )
    mesh_object.data.materials.clear()
    mesh_object.data.materials.append(other_material)
    mesh_object.data.materials.append(selected_material)
    for polygon in mesh_object.data.polygons:
        polygon.material_index = 1 if polygon.index in selected_polygons else 0
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH" and obj != mesh_object:
            obj.hide_render = True


def setup_render():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 700
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.035, 0.035, 0.035, 1.0)
    background.inputs["Strength"].default_value = 0.45

    camera_data = bpy.data.cameras.new("AttackTransition_DiagnosticCamera")
    camera = bpy.data.objects.new(
        "AttackTransition_DiagnosticCamera",
        camera_data,
    )
    scene.collection.objects.link(camera)
    camera.data.lens = 58.0
    scene.camera = camera

    for name, location, energy in (
        ("AttackTransition_Key", (-4.0, -5.0, 6.0), 1100.0),
        ("AttackTransition_Fill", (4.0, -2.0, 3.0), 750.0),
        ("AttackTransition_Rim", (0.0, 4.0, 4.0), 900.0),
    ):
        light_data = bpy.data.lights.new(name, "AREA")
        light_data.energy = energy
        light_data.shape = "DISK"
        light_data.size = 4.0
        light = bpy.data.objects.new(name, light_data)
        scene.collection.objects.link(light)
        light.location = location
    return camera


def render(camera, location, target, path):
    camera.location = location
    look_at(camera, target)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def main():
    if not SOURCE_BLEND.exists():
        raise FileNotFoundError(SOURCE_BLEND)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE_BLEND))
    mesh_object = find_skinned_mesh()
    selected_polygons, disc_material_index = select_disc_polygons(mesh_object)
    (
        selected_vertices,
        boundary_vertices,
        minimum,
        maximum,
    ) = analyze_boundary(mesh_object, selected_polygons)
    if not selected_polygons or not selected_vertices:
        raise RuntimeError("Disc region selection is empty.")

    OUTPUT_ROOT.joinpath("analysis").mkdir(parents=True, exist_ok=True)
    report = {
        "result": "PASS",
        "source": str(SOURCE_BLEND.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "selection_method": {
            "material": DISC_MATERIAL_NAME,
            "material_index": disc_material_index,
            "minimum_polygon_center_z": MIN_POLYGON_CENTER_Z,
            "maximum_polygon_center_radius": MAX_POLYGON_CENTER_RADIUS,
            "seed_minimum_z": SEED_MINIMUM_Z,
            "edge_connected_from_top_seeds": True,
        },
        "selected_polygons": len(selected_polygons),
        "selected_vertices": len(selected_vertices),
        "boundary_vertices_shared_with_unselected_faces": len(boundary_vertices),
        "bounds_min": [round(value, 6) for value in minimum],
        "bounds_max": [round(value, 6) for value in maximum],
        "selected_polygon_indices": sorted(selected_polygons),
        "selected_vertex_indices": sorted(selected_vertices),
        "boundary_vertex_indices": sorted(boundary_vertices),
    }
    REPORT_PATH.write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    setup_diagnostic_materials(mesh_object, selected_polygons)
    camera = setup_render()
    target = Vector((0.0, 0.0, 0.95))
    render(camera, (0.0, -5.2, 2.3), target, FRONT_RENDER)
    render(camera, (5.2, 0.0, 2.3), target, SIDE_RENDER)
    print(
        json.dumps(
            {
                "result": "PASS",
                "selected_polygons": len(selected_polygons),
                "selected_vertices": len(selected_vertices),
                "boundary_vertices": len(boundary_vertices),
                "bounds_min": report["bounds_min"],
                "bounds_max": report["bounds_max"],
                "front_render": str(FRONT_RENDER.relative_to(PROJECT_ROOT)).replace(
                    "\\", "/"
                ),
                "side_render": str(SIDE_RENDER.relative_to(PROJECT_ROOT)).replace(
                    "\\", "/"
                ),
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
