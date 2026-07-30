import bpy
from collections import Counter, defaultdict, deque
import colorsys
import math
from pathlib import Path

from mathutils import Vector


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE_FBX = ROOT / "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx"
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
RENDER_DIR = SAMPLE_ROOT / "renders"
PATCH_ANGLE_DEGREES = 38.0


def dominant_group(obj, polygon):
    weights = Counter()
    for vertex_index in polygon.vertices:
        for membership in obj.data.vertices[vertex_index].groups:
            weights[obj.vertex_groups[membership.group].name] += membership.weight
    return weights.most_common(1)[0][0] if weights else None


def point_at(obj, target):
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def flat_material(name, color):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (*color, 1.0)
    emission.inputs["Strength"].default_value = 1.0
    material.node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def head_surface_patches(mesh, head_polygon_indices):
    edge_to_polygons = defaultdict(list)
    for polygon_index in head_polygon_indices:
        polygon = mesh.polygons[polygon_index]
        vertices = list(polygon.vertices)
        for index, start in enumerate(vertices):
            end = vertices[(index + 1) % len(vertices)]
            edge_to_polygons[tuple(sorted((start, end)))].append(polygon_index)

    adjacency = defaultdict(set)
    cosine_limit = math.cos(math.radians(PATCH_ANGLE_DEGREES))
    for polygons in edge_to_polygons.values():
        if len(polygons) != 2:
            continue
        first, second = polygons
        if mesh.polygons[first].normal.dot(mesh.polygons[second].normal) >= cosine_limit:
            adjacency[first].add(second)
            adjacency[second].add(first)

    patches = []
    unseen = set(head_polygon_indices)
    while unseen:
        seed = min(unseen)
        queue = deque([seed])
        unseen.remove(seed)
        patch = []
        while queue:
            polygon_index = queue.popleft()
            patch.append(polygon_index)
            for neighbor in adjacency[polygon_index]:
                if neighbor in unseen:
                    unseen.remove(neighbor)
                    queue.append(neighbor)
        patches.append(sorted(patch))
    return sorted(patches, key=lambda patch: (-len(patch), patch[0]))


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
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))

    scene = bpy.context.scene
    mesh_obj = next(obj for obj in scene.objects if obj.type == "MESH")
    mesh = mesh_obj.data
    head_polygons = [
        polygon.index
        for polygon in mesh.polygons
        if dominant_group(mesh_obj, polygon) == "Head"
    ]
    patches = head_surface_patches(mesh, head_polygons)

    body_material = flat_material("Diagnostic_Body", (0.018, 0.022, 0.028))
    mesh.materials.clear()
    mesh.materials.append(body_material)

    patch_material_indices = {}
    for patch_index, patch in enumerate(patches):
        hue = (patch_index * 0.61803398875) % 1.0
        saturation = 0.62 + 0.18 * ((patch_index % 3) / 2.0)
        value = 0.86 + 0.12 * ((patch_index % 2))
        color = colorsys.hsv_to_rgb(hue, saturation, min(value, 1.0))
        material = flat_material(f"HeadPatch_{patch_index:02d}", color)
        mesh.materials.append(material)
        patch_material_indices[patch_index] = len(mesh.materials) - 1
        for polygon_index in patch:
            mesh.polygons[polygon_index].material_index = patch_material_indices[patch_index]

    mesh_obj.show_wire = True
    mesh_obj.show_all_edges = True

    head_vertex_indices = {
        vertex_index
        for polygon_index in head_polygons
        for vertex_index in mesh.polygons[polygon_index].vertices
    }
    world_points = [
        mesh_obj.matrix_world @ mesh.vertices[index].co
        for index in head_vertex_indices
    ]
    low = Vector(tuple(min(point[axis] for point in world_points) for axis in range(3)))
    high = Vector(tuple(max(point[axis] for point in world_points) for axis in range(3)))
    center = (low + high) * 0.5
    center.z -= 0.015
    extent = max(high.x - low.x, high.z - low.z)
    distance = extent * 3.8
    print(
        "Head world bounds:",
        tuple(round(value, 4) for value in low),
        tuple(round(value, 4) for value in high),
        "center:",
        tuple(round(value, 4) for value in center),
        "distance:",
        round(distance, 4),
    )

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.lens = 72
    scene.camera = camera

    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "FLAT"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = False
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "BOTH"
    scene.display.shading.curvature_ridge_factor = 1.5
    scene.display.shading.curvature_valley_factor = 1.5
    scene.display.shading.background_type = "VIEWPORT"
    scene.display.shading.background_color = (0.055, 0.065, 0.08)
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False

    for angle, name in (
        (0, "front"),
        (28, "three_quarter"),
        (90, "side"),
        (180, "rear"),
    ):
        render_view(
            scene,
            camera,
            center,
            distance,
            angle,
            0.02,
            f"09_head_topology_{name}.png",
        )

    report_path = SAMPLE_ROOT / "HEAD_SURFACE_PATCHES.json"
    report_path.write_text(
        __import__("json").dumps(
            {
                "source": "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx",
                "head_polygon_count": len(head_polygons),
                "patch_angle_degrees": PATCH_ANGLE_DEGREES,
                "patches": [
                    {
                        "patch_index": index,
                        "polygon_count": len(patch),
                        "polygon_indices": patch,
                    }
                    for index, patch in enumerate(patches)
                ],
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )
    print(
        f"Rendered head topology: {len(head_polygons)} polygons, "
        f"{len(patches)} surface patches."
    )


if __name__ == "__main__":
    main()
