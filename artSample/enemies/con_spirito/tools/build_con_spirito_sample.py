import json
import math
import os
import random
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[4]
SAMPLE_ROOT = PROJECT_ROOT / "artSample" / "enemies" / "con_spirito"
SOURCE_FBX = PROJECT_ROOT / "Assets" / "_Project" / "Art" / "Enemies" / "ConSpirito" / "Models" / "con_spirito_original.fbx"
REFERENCE_IMAGE = PROJECT_ROOT / "image" / "con spirito(콘 스피리토).png"

TEXTURE_DIR = SAMPLE_ROOT / "textures"
RENDER_DIR = SAMPLE_ROOT / "renders"
EXPORT_DIR = SAMPLE_ROOT / "exports"
BLEND_DIR = SAMPLE_ROOT / "blender"
REFERENCE_COPY = SAMPLE_ROOT / "reference_con_spirito.png"


def ensure_dirs():
    for path in (TEXTURE_DIR, RENDER_DIR, EXPORT_DIR, BLEND_DIR):
        path.mkdir(parents=True, exist_ok=True)


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for collection in (bpy.data.meshes, bpy.data.materials, bpy.data.images, bpy.data.textures):
        for item in list(collection):
            if item.users == 0:
                collection.remove(item)


def copy_reference():
    shutil.copyfile(REFERENCE_IMAGE, REFERENCE_COPY)


def clamp(value, low=0.0, high=1.0):
    return max(low, min(high, value))


def make_image_texture(name, path, width, height, pixel_func):
    image = bpy.data.images.new(name, width, height, alpha=True)
    pixels = [0.0] * (width * height * 4)
    rng = random.Random(7319)
    for y in range(height):
        v = y / max(1, height - 1)
        for x in range(width):
            u = x / max(1, width - 1)
            r, g, b, a = pixel_func(u, v, rng)
            index = (y * width + x) * 4
            pixels[index] = clamp(r)
            pixels[index + 1] = clamp(g)
            pixels[index + 2] = clamp(b)
            pixels[index + 3] = clamp(a)
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    return image


def streak(u, v, scale=1.0):
    wave = math.sin((u * 17.0 + v * 5.5) * math.tau)
    wave += 0.55 * math.sin((u * 41.0 - v * 10.0) * math.tau)
    wave += 0.25 * math.sin((u * 73.0 + v * 37.0) * math.tau)
    return wave * scale


def body_fur_pixel(u, v, rng):
    fiber = 0.012 * math.sin((u * 64.0 + math.sin(v * 8.0) * 0.6) * math.tau)
    fiber += 0.010 * math.sin((u * 31.0 - v * 5.0) * math.tau)
    long_shadow = 0.026 * math.sin((u * 5.0 + v * 1.5) * math.tau)
    mottling = 0.085 * rng.random()
    highlight = max(0.0, math.sin((u * 9.0 - v * 2.0) * math.tau)) * 0.026
    base = 0.47 + fiber + long_shadow + mottling + highlight
    return (
        base + 0.20,
        0.045 + base * 0.16,
        0.060 + base * 0.18,
        1.0,
    )


def dark_fur_pixel(u, v, rng):
    fiber = 0.025 * math.sin((u * 48.0 + v * 9.0) * math.tau)
    fiber += 0.020 * math.sin((u * 17.0 - v * 4.0) * math.tau)
    base = 0.20 + fiber + 0.045 * rng.random()
    return (
        base + 0.12,
        0.018 + base * 0.10,
        0.028 + base * 0.11,
        1.0,
    )


def hoof_pixel(u, v, rng):
    shine = max(0.0, math.sin((u * 4.0 + v * 1.5) * math.tau)) * 0.035
    base = 0.145 + shine + 0.035 * rng.random()
    return (base + 0.120, 0.025 + base * 0.20, 0.035 + base * 0.23, 1.0)


def inner_ear_pixel(u, v, rng):
    fold = 0.10 * math.sin((u * 8.0 + v * 3.0) * math.tau)
    base = 0.34 + fold + 0.04 * rng.random()
    return (base + 0.18, 0.055 + base * 0.16, 0.075 + base * 0.20, 1.0)


def amber_eye_pixel(u, v, rng):
    du = u - 0.5
    dv = v - 0.5
    radius = math.sqrt(du * du + dv * dv)
    glow = clamp(1.0 - radius * 2.6)
    return (0.10 + glow * 0.65, 0.055 + glow * 0.36, 0.020 + glow * 0.08, 1.0)


def bump_pixel(u, v, rng):
    fiber = 0.5 + streak(u, v, 0.08)
    cut = 0.04 * rng.random()
    value = clamp(fiber + cut)
    return (value, value, value, 1.0)


def create_textures():
    textures = {
        "body": make_image_texture(
            "con_spirito_blood_red_fur_albedo",
            TEXTURE_DIR / "con_spirito_blood_red_fur_albedo.png",
            768,
            768,
            body_fur_pixel,
        ),
        "dark": make_image_texture(
            "con_spirito_dark_wine_leg_albedo",
            TEXTURE_DIR / "con_spirito_dark_wine_leg_albedo.png",
            512,
            512,
            dark_fur_pixel,
        ),
        "hoof": make_image_texture(
            "con_spirito_black_maroon_hoof_nose_albedo",
            TEXTURE_DIR / "con_spirito_black_maroon_hoof_nose_albedo.png",
            384,
            384,
            hoof_pixel,
        ),
        "ear": make_image_texture(
            "con_spirito_inner_ear_dark_rose_albedo",
            TEXTURE_DIR / "con_spirito_inner_ear_dark_rose_albedo.png",
            384,
            384,
            inner_ear_pixel,
        ),
        "eye": make_image_texture(
            "con_spirito_gloss_amber_eye_albedo",
            TEXTURE_DIR / "con_spirito_gloss_amber_eye_albedo.png",
            256,
            256,
            amber_eye_pixel,
        ),
        "bump": make_image_texture(
            "con_spirito_fur_direction_bump",
            TEXTURE_DIR / "con_spirito_fur_direction_bump.png",
            768,
            768,
            bump_pixel,
        ),
    }
    return textures


def link_texture_material(name, image, roughness=0.72, metallic=0.0, alpha=1.0, bump_image=None, bump_strength=0.06):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (1.0, 1.0, 1.0, alpha)
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    principled = nodes.get("Principled BSDF")
    texcoord = nodes.new(type="ShaderNodeTexCoord")
    image_node = nodes.new(type="ShaderNodeTexImage")
    image_node.image = image
    links.new(texcoord.outputs["Generated"], image_node.inputs["Vector"])
    if principled:
        if "Base Color" in principled.inputs:
            links.new(image_node.outputs["Color"], principled.inputs["Base Color"])
        if "Alpha" in principled.inputs:
            principled.inputs["Alpha"].default_value = alpha
        if "Roughness" in principled.inputs:
            principled.inputs["Roughness"].default_value = roughness
        if "Metallic" in principled.inputs:
            principled.inputs["Metallic"].default_value = metallic
        if bump_image is not None and "Normal" in principled.inputs:
            bump_tex = nodes.new(type="ShaderNodeTexImage")
            bump_tex.image = bump_image
            bump_tex.image.colorspace_settings.name = "Non-Color"
            bump = nodes.new(type="ShaderNodeBump")
            if "Strength" in bump.inputs:
                bump.inputs["Strength"].default_value = bump_strength
            if "Distance" in bump.inputs:
                bump.inputs["Distance"].default_value = 0.045
            links.new(texcoord.outputs["Generated"], bump_tex.inputs["Vector"])
            links.new(bump_tex.outputs["Color"], bump.inputs["Height"])
            links.new(bump.outputs["Normal"], principled.inputs["Normal"])
    return material


def create_materials(textures):
    return {
        "body": link_texture_material(
            "ConSpirito_BloodRed_RoughFur",
            textures["body"],
            roughness=0.82,
            bump_image=textures["bump"],
            bump_strength=0.030,
        ),
        "dark": link_texture_material(
            "ConSpirito_DarkWine_LowerFur",
            textures["dark"],
            roughness=0.84,
            bump_image=textures["bump"],
            bump_strength=0.024,
        ),
        "hoof": link_texture_material(
            "ConSpirito_BlackMaroon_GlossHoofNose",
            textures["hoof"],
            roughness=0.36,
        ),
        "ear": link_texture_material(
            "ConSpirito_DarkRose_InnerEar",
            textures["ear"],
            roughness=0.68,
            bump_image=textures["bump"],
            bump_strength=0.04,
        ),
        "eye": link_texture_material(
            "ConSpirito_Amber_GlossEye",
            textures["eye"],
            roughness=0.18,
        ),
    }


def import_model():
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX))
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not mesh_objects:
        raise RuntimeError(f"No mesh objects imported from {SOURCE_FBX}")
    for obj in mesh_objects:
        obj.name = f"ConSpiritoColored_{obj.name}"
    return mesh_objects


def mesh_bounds(mesh_objects):
    min_v = Vector((float("inf"), float("inf"), float("inf")))
    max_v = Vector((float("-inf"), float("-inf"), float("-inf")))
    for obj in mesh_objects:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            min_v.x = min(min_v.x, world.x)
            min_v.y = min(min_v.y, world.y)
            min_v.z = min(min_v.z, world.z)
            max_v.x = max(max_v.x, world.x)
            max_v.y = max(max_v.y, world.y)
            max_v.z = max(max_v.z, world.z)
    center = (min_v + max_v) * 0.5
    size = max_v - min_v
    return min_v, max_v, center, size


def local_bounds(obj):
    min_v = Vector((float("inf"), float("inf"), float("inf")))
    max_v = Vector((float("-inf"), float("-inf"), float("-inf")))
    for vertex in obj.data.vertices:
        co = vertex.co
        min_v.x = min(min_v.x, co.x)
        min_v.y = min(min_v.y, co.y)
        min_v.z = min(min_v.z, co.z)
        max_v.x = max(max_v.x, co.x)
        max_v.y = max(max_v.y, co.y)
        max_v.z = max(max_v.z, co.z)
    return min_v, max_v, max_v - min_v


def polygon_center(mesh, polygon):
    center = Vector((0.0, 0.0, 0.0))
    for vertex_index in polygon.vertices:
        center += mesh.vertices[vertex_index].co
    return center / max(1, len(polygon.vertices))


def assign_reference_materials(mesh_objects, materials):
    body_index = 0

    for obj in mesh_objects:
        mesh = obj.data
        mesh.materials.clear()
        for key in ("body", "dark", "hoof", "ear", "eye"):
            mesh.materials.append(materials[key])

        for polygon in mesh.polygons:
            polygon.material_index = body_index

        mesh.update()


def shade_and_prepare(mesh_objects):
    for obj in mesh_objects:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        try:
            bpy.ops.object.shade_smooth()
        finally:
            obj.select_set(False)


def setup_lighting():
    bpy.context.scene.world = bpy.data.worlds.new("ConSpirito_World") if bpy.context.scene.world is None else bpy.context.scene.world
    bpy.context.scene.world.color = (0.035, 0.035, 0.038)
    bpy.ops.object.light_add(type="AREA", location=(0.0, -4.0, 5.0))
    key = bpy.context.object
    key.name = "ConSpirito_KeyLight"
    key.data.energy = 550
    key.data.size = 4.5
    bpy.ops.object.light_add(type="POINT", location=(-3.0, 3.0, 2.0))
    fill = bpy.context.object
    fill.name = "ConSpirito_RedFur_FillLight"
    fill.data.energy = 70
    fill.data.color = (1.0, 0.30, 0.26)


def set_render_engine():
    for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "CYCLES"):
        try:
            bpy.context.scene.render.engine = engine
            break
        except TypeError:
            continue
    if bpy.context.scene.render.engine == "CYCLES":
        bpy.context.scene.cycles.samples = 48


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def create_camera(name, location, target, ortho_scale):
    camera_data = bpy.data.cameras.new(name)
    camera = bpy.data.objects.new(name, camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = Vector(location)
    look_at(camera, target)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale
    camera.data.clip_start = 0.00001
    camera.data.clip_end = 1000.0
    bpy.context.scene.camera = camera
    return camera


def render_to(path, camera, width=1600, height=900):
    bpy.context.scene.camera = camera
    bpy.context.scene.render.resolution_x = width
    bpy.context.scene.render.resolution_y = height
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def create_canvas(name, width, height, color=(0.10, 0.10, 0.10, 1.0)):
    image = bpy.data.images.new(name, width, height, alpha=True)
    pixels = [0.0] * (width * height * 4)
    for index in range(0, len(pixels), 4):
        pixels[index] = color[0]
        pixels[index + 1] = color[1]
        pixels[index + 2] = color[2]
        pixels[index + 3] = color[3]
    image.pixels.foreach_set(pixels)
    return image


def paste_image_fit(canvas, source_path, box_x, box_y, box_w, box_h, background=None):
    source = bpy.data.images.load(str(source_path), check_existing=False)
    source_w, source_h = source.size
    scale = min(box_w / max(1, source_w), box_h / max(1, source_h))
    draw_w = max(1, int(source_w * scale))
    draw_h = max(1, int(source_h * scale))
    start_x = box_x + (box_w - draw_w) // 2
    start_y = box_y + (box_h - draw_h) // 2

    canvas_w, canvas_h = canvas.size
    canvas_pixels = list(canvas.pixels)
    source_pixels = list(source.pixels)

    if background is not None:
        for y in range(box_y, box_y + box_h):
            if y < 0 or y >= canvas_h:
                continue
            for x in range(box_x, box_x + box_w):
                if x < 0 or x >= canvas_w:
                    continue
                index = (y * canvas_w + x) * 4
                canvas_pixels[index] = background[0]
                canvas_pixels[index + 1] = background[1]
                canvas_pixels[index + 2] = background[2]
                canvas_pixels[index + 3] = background[3]

    for y in range(draw_h):
        dest_y = start_y + y
        if dest_y < 0 or dest_y >= canvas_h:
            continue
        src_y = min(source_h - 1, int(y / scale))
        for x in range(draw_w):
            dest_x = start_x + x
            if dest_x < 0 or dest_x >= canvas_w:
                continue
            src_x = min(source_w - 1, int(x / scale))
            source_index = (src_y * source_w + src_x) * 4
            dest_index = (dest_y * canvas_w + dest_x) * 4
            alpha = source_pixels[source_index + 3]
            inv_alpha = 1.0 - alpha
            canvas_pixels[dest_index] = source_pixels[source_index] * alpha + canvas_pixels[dest_index] * inv_alpha
            canvas_pixels[dest_index + 1] = source_pixels[source_index + 1] * alpha + canvas_pixels[dest_index + 1] * inv_alpha
            canvas_pixels[dest_index + 2] = source_pixels[source_index + 2] * alpha + canvas_pixels[dest_index + 2] * inv_alpha
            canvas_pixels[dest_index + 3] = 1.0

    canvas.pixels.foreach_set(canvas_pixels)


def save_canvas(canvas, path):
    canvas.filepath_raw = str(path)
    canvas.file_format = "PNG"
    canvas.save()


def compose_side_by_side(reference_path, render_path, output_path):
    canvas = create_canvas("con_spirito_reference_side_by_side_canvas", 2200, 1000, (0.12, 0.12, 0.12, 1.0))
    paste_image_fit(canvas, reference_path, 50, 50, 1010, 900, background=(1.0, 1.0, 1.0, 1.0))
    paste_image_fit(canvas, render_path, 1140, 50, 1010, 900, background=(0.18, 0.18, 0.18, 1.0))
    save_canvas(canvas, output_path)


def compose_texture_breakdown(output_path):
    canvas = create_canvas("con_spirito_texture_breakdown_canvas", 1600, 900, (0.075, 0.055, 0.055, 1.0))
    texture_files = [
        TEXTURE_DIR / "con_spirito_blood_red_fur_albedo.png",
        TEXTURE_DIR / "con_spirito_dark_wine_leg_albedo.png",
        TEXTURE_DIR / "con_spirito_black_maroon_hoof_nose_albedo.png",
        TEXTURE_DIR / "con_spirito_inner_ear_dark_rose_albedo.png",
        TEXTURE_DIR / "con_spirito_gloss_amber_eye_albedo.png",
        TEXTURE_DIR / "con_spirito_fur_direction_bump.png",
    ]
    boxes = [
        (70, 80, 430, 330),
        (585, 80, 430, 330),
        (1100, 80, 430, 330),
        (70, 490, 430, 330),
        (585, 490, 430, 330),
        (1100, 490, 430, 330),
    ]
    for texture_path, box in zip(texture_files, boxes):
        paste_image_fit(canvas, texture_path, box[0], box[1], box[2], box[3], background=(0.035, 0.025, 0.025, 1.0))
    save_canvas(canvas, output_path)


def make_reference_plane(image_path, view_dir, center, size, side_axis, offset_sign=-1.0):
    image = bpy.data.images.load(str(image_path))
    ratio = image.size[0] / max(1, image.size[1])
    plane_height = size.z * 1.05
    plane_width = plane_height * ratio
    plane_center = Vector(center)
    plane_center[side_axis] += offset_sign * (size[side_axis] * 0.85 + plane_width * 0.55)
    plane_center.z = center.z

    bpy.ops.mesh.primitive_plane_add(size=1.0, location=plane_center)
    plane = bpy.context.object
    plane.name = "ConSpirito_ReferenceImagePlane"
    plane.scale = (plane_width * 0.5, plane_height * 0.5, 1.0)
    plane.rotation_euler = Vector(view_dir).to_track_quat("Z", "Y").to_euler()

    material = bpy.data.materials.new("ConSpirito_ReferenceImage")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    principled = nodes.get("Principled BSDF")
    texture = nodes.new(type="ShaderNodeTexImage")
    texture.image = image
    if principled and "Base Color" in principled.inputs:
        links.new(texture.outputs["Color"], principled.inputs["Base Color"])
    plane.data.materials.append(material)
    return plane


def hide_objects(objects, hidden):
    for obj in objects:
        obj.hide_render = hidden
        obj.hide_viewport = hidden


def render_model_set(mesh_objects):
    min_v, max_v, center, size = mesh_bounds(mesh_objects)
    length_axis = 0 if size.x >= size.y else 1
    side_axis = 1 if length_axis == 0 else 0
    height = max(size.z, 0.01)
    side_distance = max(size.x, size.y, size.z) * 3.0
    three_quarter = Vector((1.0, -1.0, 0.55)).normalized()
    if length_axis == 1:
        side_vector = Vector((1.0, 0.0, 0.24)).normalized()
    else:
        side_vector = Vector((0.0, -1.0, 0.24)).normalized()
    front_vector = Vector((0.0, -1.0, 0.22)).normalized() if length_axis == 0 else Vector((-1.0, 0.0, 0.22)).normalized()

    side_camera = create_camera(
        "ConSpirito_Side_MaterialCamera",
        center + side_vector * side_distance,
        center + Vector((0, 0, height * 0.05)),
        max(size.x, size.y, size.z) * 1.35,
    )
    render_to(RENDER_DIR / "01_side_reference_color_application.png", side_camera, 1600, 900)

    front_camera = create_camera(
        "ConSpirito_Front_MaterialCamera",
        center + front_vector * side_distance,
        center + Vector((0, 0, height * 0.05)),
        max(size.x, size.y, size.z) * 1.18,
    )
    render_to(RENDER_DIR / "02_front_current_model_material.png", front_camera, 1600, 900)

    quarter_camera = create_camera(
        "ConSpirito_ThreeQuarter_MaterialCamera",
        center + three_quarter * side_distance,
        center + Vector((0, 0, height * 0.05)),
        max(size.x, size.y, size.z) * 1.22,
    )
    render_to(RENDER_DIR / "03_three_quarter_red_fur_material.png", quarter_camera, 1600, 900)

    compose_side_by_side(
        REFERENCE_COPY,
        RENDER_DIR / "01_side_reference_color_application.png",
        RENDER_DIR / "04_reference_side_by_side_overview.png",
    )
    compose_texture_breakdown(RENDER_DIR / "05_texture_material_breakdown.png")


def create_texture_breakdown_planes(center, size, view_dir, horizontal_axis):
    texture_files = [
        TEXTURE_DIR / "con_spirito_blood_red_fur_albedo.png",
        TEXTURE_DIR / "con_spirito_dark_wine_leg_albedo.png",
        TEXTURE_DIR / "con_spirito_black_maroon_hoof_nose_albedo.png",
        TEXTURE_DIR / "con_spirito_inner_ear_dark_rose_albedo.png",
        TEXTURE_DIR / "con_spirito_fur_direction_bump.png",
    ]
    planes = []
    spacing = max(size[horizontal_axis] * 0.27, size.z * 0.34, 0.4)
    for index, texture_path in enumerate(texture_files):
        image = bpy.data.images.load(str(texture_path))
        bpy.ops.mesh.primitive_plane_add(size=1.0)
        plane = bpy.context.object
        plane.name = f"ConSpirito_TextureBreakdown_{index + 1:02d}"
        plane.location = Vector(center)
        plane.location[horizontal_axis] += (index - 2) * spacing
        plane.location.z += size.z * 0.24
        plane.scale = (spacing * 0.42, spacing * 0.42, 1.0)
        plane.rotation_euler = Vector(view_dir).to_track_quat("Z", "Y").to_euler()
        material = bpy.data.materials.new(f"ConSpirito_TextureBreakdown_Mat_{index + 1:02d}")
        material.use_nodes = True
        nodes = material.node_tree.nodes
        links = material.node_tree.links
        principled = nodes.get("Principled BSDF")
        texture = nodes.new(type="ShaderNodeTexImage")
        texture.image = image
        if principled and "Base Color" in principled.inputs:
            links.new(texture.outputs["Color"], principled.inputs["Base Color"])
        plane.data.materials.append(material)
        plane.hide_render = True
        plane.hide_viewport = True
        planes.append(plane)
    return planes


def export_model(mesh_objects):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in mesh_objects:
        obj.select_set(True)
        parent = obj.parent
        while parent is not None:
            parent.select_set(True)
            parent = parent.parent
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_DIR / "con_spirito_current_model_colored_sample.glb"),
        export_format="GLB",
        use_selection=True,
        export_apply=False,
        export_materials="EXPORT",
    )


def save_blend():
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_DIR / "con_spirito_current_model_colored_sample.blend"))


def write_manifest(mesh_objects):
    min_v, max_v, center, size = mesh_bounds(mesh_objects)
    data = {
        "enemy_id": "con_spirito",
        "source_fbx": str(SOURCE_FBX.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "reference_image": str(REFERENCE_IMAGE.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "sample_root": str(SAMPLE_ROOT.relative_to(PROJECT_ROOT)).replace("\\", "/"),
        "modeling_policy": "Current model geometry was imported as-is; sample changes are material and texture assignment only.",
        "reference_features": [
            "unified bright blood-red fur across body, rear, head, legs, and tail",
            "body and rear color are intentionally matched to the brighter body material",
            "material assignment keeps the imported mesh and rig unchanged",
            "weak rough fur grain is retained without separate dark rear material",
        ],
        "mesh_summary": {
            "mesh_objects": len(mesh_objects),
            "bounds_center": [round(center.x, 4), round(center.y, 4), round(center.z, 4)],
            "bounds_size": [round(size.x, 4), round(size.y, 4), round(size.z, 4)],
            "vertices": sum(len(obj.data.vertices) for obj in mesh_objects),
            "polygons": sum(len(obj.data.polygons) for obj in mesh_objects),
        },
        "outputs": {
            "documents": [
                "index.html",
                "README.md",
                "TEXTURE_ANALYSIS.md",
                "APPROVAL_STATUS.json",
                "ASSET_MANIFEST.json",
                "reference_con_spirito.png",
            ],
            "renders": [
                "renders/01_side_reference_color_application.png",
                "renders/02_front_current_model_material.png",
                "renders/03_three_quarter_red_fur_material.png",
                "renders/04_reference_side_by_side_overview.png",
                "renders/05_texture_material_breakdown.png",
            ],
            "textures": [
                "textures/con_spirito_blood_red_fur_albedo.png",
                "textures/con_spirito_dark_wine_leg_albedo.png",
                "textures/con_spirito_black_maroon_hoof_nose_albedo.png",
                "textures/con_spirito_inner_ear_dark_rose_albedo.png",
                "textures/con_spirito_gloss_amber_eye_albedo.png",
                "textures/con_spirito_fur_direction_bump.png",
            ],
            "exports": [
                "exports/con_spirito_current_model_colored_sample.glb",
                "blender/con_spirito_current_model_colored_sample.blend",
            ],
        },
    }
    (SAMPLE_ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


def main():
    ensure_dirs()
    reset_scene()
    copy_reference()
    textures = create_textures()
    materials = create_materials(textures)
    mesh_objects = import_model()
    assign_reference_materials(mesh_objects, materials)
    shade_and_prepare(mesh_objects)
    setup_lighting()
    set_render_engine()
    render_model_set(mesh_objects)
    export_model(mesh_objects)
    write_manifest(mesh_objects)
    save_blend()


if __name__ == "__main__":
    main()
