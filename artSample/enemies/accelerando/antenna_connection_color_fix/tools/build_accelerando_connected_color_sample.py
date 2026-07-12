import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


PROJECT_ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE_GLB = PROJECT_ROOT / "enemies model" / "accelerando.glb"
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / "accelerando" / "antenna_connection_color_fix"
EXPORT_DIR = SAMPLE_ROOT / "exports"
RENDER_DIR = SAMPLE_ROOT / "renders"
REFERENCE_FRONT = PROJECT_ROOT / "image" / "accelerando(아첼레란도).png"
REFERENCE_SIDE = PROJECT_ROOT / "image" / "accelerando-beside.png"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def create_material(name, color_a, color_b=None, metallic=0.0, roughness=0.6, noise_scale=36.0, bump_strength=0.05):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness

    if color_b is None:
        bsdf.inputs["Base Color"].default_value = color_a
    else:
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = noise_scale
        noise.inputs["Detail"].default_value = 10.0
        noise.inputs["Roughness"].default_value = 0.62

        ramp = nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.25
        ramp.color_ramp.elements[0].color = color_a
        ramp.color_ramp.elements[1].position = 1.0
        ramp.color_ramp.elements[1].color = color_b
        links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])

        bump = nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = bump_strength
        bump.inputs["Distance"].default_value = 0.08
        links.new(noise.outputs["Fac"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    return material


def import_source_mesh():
    bpy.ops.import_scene.gltf(filepath=str(SOURCE_GLB))
    for obj in list(bpy.context.scene.objects):
        if obj.name != "Mesh1.0":
            bpy.data.objects.remove(obj, do_unlink=True)

    mesh_obj = bpy.data.objects["Mesh1.0"]
    mesh_obj.name = "Accelerando_ConnectedColored_Body"
    mesh_obj.data.name = "Accelerando_ConnectedColored_Mesh"
    return mesh_obj


def remove_flat_display_plate(mesh_obj):
    mesh = mesh_obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.faces.ensure_lookup_table()
    delete_faces = []
    for face in bm.faces:
        max_z = max(vertex.co.z for vertex in face.verts)
        max_lateral = max(max(abs(vertex.co.x), abs(vertex.co.y)) for vertex in face.verts)
        if max_z < 0.145 and max_lateral > 1.72:
            delete_faces.append(face)
    if delete_faces:
        bmesh.ops.delete(bm, geom=delete_faces, context="FACES")
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def remove_original_mace_rods(mesh_obj):
    mesh = mesh_obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.faces.ensure_lookup_table()
    visited = set()
    delete_faces = set()

    for face in bm.faces:
        if face in visited:
            continue

        stack = [face]
        component_faces = []
        component_verts = set()
        visited.add(face)

        while stack:
            current = stack.pop()
            component_faces.append(current)
            for vertex in current.verts:
                component_verts.add(vertex)
                for linked_face in vertex.link_faces:
                    if linked_face not in visited:
                        visited.add(linked_face)
                        stack.append(linked_face)

        min_corner = Vector((
            min(vertex.co.x for vertex in component_verts),
            min(vertex.co.y for vertex in component_verts),
            min(vertex.co.z for vertex in component_verts),
        ))
        max_corner = Vector((
            max(vertex.co.x for vertex in component_verts),
            max(vertex.co.y for vertex in component_verts),
            max(vertex.co.z for vertex in component_verts),
        ))
        size = max_corner - min_corner
        center = (min_corner + max_corner) * 0.5
        abs_center_x = abs(center.x)

        is_slender_rod_component = (
            0.94 < abs_center_x < 1.12
            and -1.26 < center.y < -1.10
            and 0.80 < center.z < 0.92
            and size.z > 0.88
            and size.x < 0.04
            and 0.50 < size.y < 0.62
        )
        is_upper_rod_cap_component = (
            0.94 < abs_center_x < 1.12
            and -1.26 < center.y < -1.10
            and 1.03 < center.z < 1.20
            and size.z < 0.22
            and size.x < 0.46
            and size.y < 0.46
        )
        if is_slender_rod_component or is_upper_rod_cap_component:
            delete_faces.update(component_faces)

    if delete_faces:
        bmesh.ops.delete(bm, geom=list(delete_faces), context="FACES")
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def assign_accelerando_materials(mesh_obj, flesh, shell, metal):
    mesh = mesh_obj.data
    mesh.materials.clear()
    mesh.materials.append(flesh)
    mesh.materials.append(shell)
    mesh.materials.append(metal)

    for polygon in mesh.polygons:
        center = polygon.center
        lateral = abs(center.x)
        front_depth = center.y
        is_metal_mace_or_rod = (lateral > 1.05 and center.z < 1.12 and front_depth < 0.18)
        is_shell_or_armored_antenna = center.z > 0.78 or (lateral > 0.48 and center.z > 0.56 and front_depth < 0.15)
        if is_metal_mace_or_rod:
            polygon.material_index = 2
        elif is_shell_or_armored_antenna:
            polygon.material_index = 1
        else:
            polygon.material_index = 0


def make_curve_object(name, points, material, bevel_depth, resolution=3):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = resolution
    curve.bevel_depth = bevel_depth
    curve.bevel_resolution = 5
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for point, co in zip(spline.bezier_points, points):
        point.co = Vector(co)
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def add_torus_link(name, location, material, rotation_z=0.0, scale=(0.72, 1.34, 1.0), radius=0.064):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=radius,
        minor_radius=0.018,
        major_segments=18,
        minor_segments=8,
        location=location,
        rotation=(math.radians(90), 0.0, rotation_z),
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    return obj


def add_socket_sphere(name, location, material, radius=0.075, scale=(1.0, 1.0, 1.0)):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=20,
        ring_count=10,
        radius=radius,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    return obj


def add_chain_between(start, end, side_name, material):
    start = Vector(start)
    end = Vector(end)
    count = 12
    for i in range(count):
        t = i / (count - 1)
        position = start.lerp(end, t)
        position.y += math.sin(t * math.pi) * -0.075
        position.z += math.sin(t * math.pi) * -0.045
        add_torus_link(
            f"Accelerando_{side_name}_ConnectedChain_Link_{i + 1:02d}",
            position,
            material,
            rotation_z=math.radians(95 if i % 2 else 8),
            scale=(0.62, 1.46, 1.0),
            radius=0.062,
        )


def add_connection_geometry(flesh, shell, metal):
    for sign, side in [(-1.0, "Left"), (1.0, "Right")]:
        chain_start = (sign * 1.04, -1.22, 1.30)
        chain_end = (sign * 1.43, -1.22, 0.43)
        add_chain_between(chain_start, chain_end, side, metal)

        add_torus_link(
            f"Accelerando_{side}_AntennaTip_Ring",
            chain_start,
            metal,
            rotation_z=math.radians(18),
            scale=(0.78, 1.16, 1.0),
            radius=0.075,
        )
        add_torus_link(
            f"Accelerando_{side}_MaceSocket_Ring",
            chain_end,
            metal,
            rotation_z=math.radians(8),
            scale=(0.90, 1.20, 1.0),
            radius=0.082,
        )


def shade_and_cleanup():
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            try:
                bpy.ops.object.shade_flat()
            finally:
                obj.select_set(False)


def calculate_bounds(objects):
    corners = []
    for obj in objects:
        if obj.type not in {"MESH", "CURVE"}:
            continue
        corners.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    min_corner = Vector((min(v.x for v in corners), min(v.y for v in corners), min(v.z for v in corners)))
    max_corner = Vector((max(v.x for v in corners), max(v.y for v in corners), max(v.z for v in corners)))
    return min_corner, max_corner


def setup_render_scene():
    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 96
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "High Contrast"
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 1000

    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.color = (0.78, 0.76, 0.71)

    floor_mat = create_material(
        "RenderOnly_WarmStoneFloor",
        (0.64, 0.60, 0.53, 1.0),
        (0.82, 0.79, 0.71, 1.0),
        metallic=0.0,
        roughness=0.72,
        noise_scale=18.0,
        bump_strength=0.025,
    )
    bpy.ops.mesh.primitive_plane_add(size=6.0, location=(0, 0, -0.012))
    floor = bpy.context.object
    floor.name = "RenderOnly_Floor"
    floor.data.materials.append(floor_mat)

    light_data = bpy.data.lights.new("Key_Area_Light", "AREA")
    light_data.energy = 900
    light_data.size = 4.5
    light = bpy.data.objects.new("Key_Area_Light", light_data)
    bpy.context.collection.objects.link(light)
    light.location = (0, -4.8, 5.8)

    rim_data = bpy.data.lights.new("Rim_Area_Light", "AREA")
    rim_data.energy = 250
    rim_data.size = 3
    rim = bpy.data.objects.new("Rim_Area_Light", rim_data)
    bpy.context.collection.objects.link(rim)
    rim.location = (-4, 3.2, 3.8)

    camera_data = bpy.data.cameras.new("Render_Camera")
    camera = bpy.data.objects.new("Render_Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    camera.data.type = "ORTHO"
    camera.data.lens = 70
    return camera, floor


def look_at(camera, target):
    direction = target - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_view(camera, model_objects, filename, direction, ortho_multiplier=1.28):
    min_corner, max_corner = calculate_bounds(model_objects)
    center = (min_corner + max_corner) * 0.5
    size = max_corner - min_corner
    distance = max(size.x, size.y, size.z) * 3.6
    camera.location = center + Vector(direction).normalized() * distance + Vector((0, 0, size.z * 0.12))
    look_at(camera, center + Vector((0, 0, size.z * 0.08)))
    camera.data.ortho_scale = max(size.x, size.y, size.z) * ortho_multiplier
    bpy.context.scene.render.filepath = str(RENDER_DIR / filename)
    bpy.ops.render.render(write_still=True)


def export_sample(model_objects):
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    for obj in model_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = model_objects[0]
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_DIR / "accelerando_connected_colored_sample.glb"),
        export_format="GLB",
        use_selection=True,
    )
    bpy.ops.wm.save_as_mainfile(filepath=str(EXPORT_DIR / "accelerando_connected_colored_sample.blend"))


def create_contact_sheet():
    try:
        from PIL import Image, ImageDraw
    except ModuleNotFoundError:
        print("PIL is not available in Blender Python; skipping contact sheet.")
        return

    paths = [
        REFERENCE_FRONT,
        RENDER_DIR / "accelerando_connected_colored_front.png",
        REFERENCE_SIDE,
        RENDER_DIR / "accelerando_connected_colored_side.png",
        RENDER_DIR / "accelerando_connected_colored_oblique.png",
    ]
    labels = [
        "Reference front",
        "Sample front",
        "Reference side",
        "Sample side",
        "Sample oblique",
    ]
    thumbs = []
    for path in paths:
        img = Image.open(path).convert("RGB")
        img.thumbnail((520, 325), Image.Resampling.LANCZOS)
        canvas = Image.new("RGB", (540, 365), (238, 236, 230))
        canvas.paste(img, ((540 - img.width) // 2, 30 + (325 - img.height) // 2))
        thumbs.append(canvas)

    sheet = Image.new("RGB", (1080, 1095), (222, 219, 212))
    draw = ImageDraw.Draw(sheet)
    for i, (thumb, label) in enumerate(zip(thumbs, labels)):
        x = 0 if i % 2 == 0 else 540
        y = (i // 2) * 365
        sheet.paste(thumb, (x, y))
        draw.text((x + 18, y + 10), label, fill=(28, 28, 26))
    sheet.save(RENDER_DIR / "accelerando_connected_colored_contact_sheet.png")


def main():
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    RENDER_DIR.mkdir(parents=True, exist_ok=True)
    clear_scene()

    flesh = create_material(
        "Accelerando wet taupe flesh",
        (0.20, 0.16, 0.13, 1.0),
        (0.48, 0.39, 0.32, 1.0),
        metallic=0.0,
        roughness=0.26,
        noise_scale=56.0,
        bump_strength=0.105,
    )
    shell = create_material(
        "Accelerando dark worn shell plates",
        (0.035, 0.031, 0.026, 1.0),
        (0.20, 0.155, 0.11, 1.0),
        metallic=0.0,
        roughness=0.68,
        noise_scale=42.0,
        bump_strength=0.065,
    )
    metal = create_material(
        "Accelerando rusty iron mace and chain",
        (0.025, 0.024, 0.021, 1.0),
        (0.28, 0.13, 0.065, 1.0),
        metallic=0.82,
        roughness=0.46,
        noise_scale=72.0,
        bump_strength=0.115,
    )

    mesh_obj = import_source_mesh()
    remove_flat_display_plate(mesh_obj)
    remove_original_mace_rods(mesh_obj)
    assign_accelerando_materials(mesh_obj, flesh, shell, metal)
    add_connection_geometry(flesh, shell, metal)
    shade_and_cleanup()

    model_objects = [obj for obj in bpy.context.scene.objects if obj.type in {"MESH", "CURVE"}]
    export_sample(model_objects)

    camera, floor = setup_render_scene()
    render_objects = [obj for obj in model_objects if obj.name != floor.name]
    render_view(camera, render_objects, "accelerando_connected_colored_front.png", (0, -1, 0))
    render_view(camera, render_objects, "accelerando_connected_colored_side.png", (1, 0, 0))
    render_view(camera, render_objects, "accelerando_connected_colored_oblique.png", (1, -1, 0), 1.34)
    create_contact_sheet()


if __name__ == "__main__":
    main()
