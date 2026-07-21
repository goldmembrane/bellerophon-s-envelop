import bpy
import hashlib
import json
import math
import shutil
import struct
from collections import defaultdict
from mathutils import Vector
from pathlib import Path


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE_FBX = ROOT / "enemies model" / "dolore.fbx"
REFERENCE_STATIC = ROOT / "image" / "dolore(돌로레).png"
REFERENCE_ATTACK = ROOT / "image" / "dolore-attack.png"
SAMPLE_ROOT = ROOT / "artSample" / "enemies" / "dolore"
BLENDER_DIR = SAMPLE_ROOT / "blender"
EXPORT_DIR = SAMPLE_ROOT / "exports"
RENDER_DIR = SAMPLE_ROOT / "renders"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
REFERENCE_DIR = SAMPLE_ROOT / "reference"
TEXTURE_SIZE = 512


def ensure_directories():
    for path in (SAMPLE_ROOT, BLENDER_DIR, EXPORT_DIR, RENDER_DIR, TEXTURE_DIR, REFERENCE_DIR):
        path.mkdir(parents=True, exist_ok=True)
    shutil.copy2(REFERENCE_STATIC, REFERENCE_DIR / "dolore_reference.png")
    shutil.copy2(REFERENCE_ATTACK, REFERENCE_DIR / "dolore_attack_reference.png")


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE_FBX), use_anim=False)


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def mesh_signature(mesh):
    vertex_digest = hashlib.sha256()
    topology_digest = hashlib.sha256()
    for vertex in mesh.vertices:
        vertex_digest.update(struct.pack("<3d", *vertex.co))
    for polygon in mesh.polygons:
        topology_digest.update(struct.pack("<I", len(polygon.vertices)))
        for vertex_index in polygon.vertices:
            topology_digest.update(struct.pack("<I", vertex_index))
    return {
        "vertices": len(mesh.vertices),
        "edges": len(mesh.edges),
        "polygons": len(mesh.polygons),
        "vertex_position_sha256": vertex_digest.hexdigest().upper(),
        "polygon_topology_sha256": topology_digest.hexdigest().upper(),
    }


def transform_signature(obj):
    return {
        "location": [round(value, 9) for value in obj.location],
        "rotation_euler": [round(value, 9) for value in obj.rotation_euler],
        "scale": [round(value, 9) for value in obj.scale],
    }


def bounds_world(objects):
    points = []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        evaluated = obj.evaluated_get(depsgraph)
        points.extend(evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return minimum, maximum


def hash_noise(x, y, seed=0.0):
    value = math.sin(x * 127.1 + y * 311.7 + seed * 74.7) * 43758.5453123
    return value - math.floor(value)


def smoothstep(value):
    value = max(0.0, min(1.0, value))
    return value * value * (3.0 - 2.0 * value)


def value_noise(x, y, seed=0.0):
    ix = math.floor(x)
    iy = math.floor(y)
    fx = smoothstep(x - ix)
    fy = smoothstep(y - iy)
    a = hash_noise(ix, iy, seed)
    b = hash_noise(ix + 1, iy, seed)
    c = hash_noise(ix, iy + 1, seed)
    d = hash_noise(ix + 1, iy + 1, seed)
    return (a + (b - a) * fx) + ((c + (d - c) * fx) - (a + (b - a) * fx)) * fy


def fractal_noise(x, y, seed=0.0):
    total = 0.0
    amplitude = 0.58
    frequency = 1.0
    normalizer = 0.0
    for octave in range(4):
        total += value_noise(x * frequency, y * frequency, seed + octave * 11.0) * amplitude
        normalizer += amplitude
        amplitude *= 0.52
        frequency *= 2.03
    return total / normalizer


def save_texture(name, generator, non_color=False):
    existing = bpy.data.images.get(name)
    if existing:
        bpy.data.images.remove(existing)
    image = bpy.data.images.new(name, width=TEXTURE_SIZE, height=TEXTURE_SIZE, alpha=False)
    image.colorspace_settings.name = "Non-Color" if non_color else "sRGB"
    pixels = [0.0] * (TEXTURE_SIZE * TEXTURE_SIZE * 4)
    for y in range(TEXTURE_SIZE):
        v = y / max(1, TEXTURE_SIZE - 1)
        for x in range(TEXTURE_SIZE):
            u = x / max(1, TEXTURE_SIZE - 1)
            r, g, b = generator(u, v)
            index = (y * TEXTURE_SIZE + x) * 4
            pixels[index:index + 4] = (
                max(0.0, min(1.0, r)),
                max(0.0, min(1.0, g)),
                max(0.0, min(1.0, b)),
                1.0,
            )
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(TEXTURE_DIR / f"{name}.png")
    image.file_format = "PNG"
    image.update()
    image.save()
    return image


def create_body_textures():
    def fields(u, v):
        broad = fractal_noise(u * 4.2, v * 4.2, 2.0)
        warp = fractal_noise(u * 2.2, v * 3.4, 13.0)
        fiber = 0.5 + 0.5 * math.sin((v * 15.0 + warp * 3.0 + math.sin(u * 8.0) * 0.32) * math.tau)
        ridge = smoothstep((fiber - 0.54) / 0.34)
        pits = smoothstep((0.38 - fractal_noise(u * 19.0, v * 19.0, 29.0)) / 0.24)
        return broad, ridge, pits

    def albedo(u, v):
        broad, ridge, pits = fields(u, v)
        shadow = Vector((0.006, 0.040, 0.035))
        viridian = Vector((0.018, 0.255, 0.215))
        jade = Vector((0.075, 0.650, 0.520))
        color = shadow.lerp(viridian, 0.32 + broad * 0.50)
        color = color.lerp(jade, ridge * 0.58)
        color *= 1.0 - pits * 0.34
        return tuple(color)

    def roughness(u, v):
        broad, ridge, pits = fields(u, v)
        value = 0.55 - ridge * 0.20 + pits * 0.15 + (broad - 0.5) * 0.08
        return (value, value, value)

    def height(u, v):
        broad, ridge, pits = fields(u, v)
        value = 0.34 + broad * 0.16 + ridge * 0.40 - pits * 0.16
        return (value, value, value)

    return {
        "albedo": save_texture("dolore_body_albedo", albedo),
        "roughness": save_texture("dolore_body_roughness", roughness, non_color=True),
        "height": save_texture("dolore_body_height", height, non_color=True),
    }


def create_frame_textures():
    def fields(u, v):
        broad = fractal_noise(u * 5.0, v * 5.0, 31.0)
        fine = fractal_noise(u * 26.0, v * 26.0, 47.0)
        patina = smoothstep((broad * 0.76 + fine * 0.30 - 0.50) / 0.34)
        wear = smoothstep(((0.5 + 0.5 * math.sin((u * 7.0 + math.sin(v * 9.0) * 0.7) * math.tau)) + fine * 0.38 - 0.67) / 0.30)
        return broad, fine, patina, wear

    def albedo(u, v):
        broad, fine, patina, wear = fields(u, v)
        black_bronze = Vector((0.020, 0.014, 0.006))
        old_brass = Vector((0.300, 0.205, 0.065))
        verdigris = Vector((0.012, 0.280, 0.175))
        color = black_bronze.lerp(old_brass, 0.14 + wear * 0.58)
        color = color.lerp(verdigris, patina * 0.78)
        color *= 0.76 + fine * 0.30
        return tuple(color)

    def roughness(u, v):
        broad, fine, patina, wear = fields(u, v)
        value = 0.45 + patina * 0.27 + fine * 0.10 - wear * 0.18
        return (value, value, value)

    def height(u, v):
        broad, fine, patina, wear = fields(u, v)
        value = 0.34 + broad * 0.16 + wear * 0.28 + fine * 0.12
        return (value, value, value)

    return {
        "albedo": save_texture("dolore_frame_albedo", albedo),
        "roughness": save_texture("dolore_frame_roughness", roughness, non_color=True),
        "height": save_texture("dolore_frame_height", height, non_color=True),
    }


def create_portrait_texture():
    source = bpy.data.images.load(str(REFERENCE_STATIC), check_existing=False)
    source.colorspace_settings.name = "sRGB"
    source_width, source_height = source.size
    source_pixels = list(source.pixels)
    output_size = 512
    image = bpy.data.images.new("dolore_portrait", width=output_size, height=output_size, alpha=False)
    image.colorspace_settings.name = "sRGB"
    output = [0.0] * (output_size * output_size * 4)
    crop_min_x = int(source_width * 0.310)
    crop_max_x = int(source_width * 0.510)
    crop_min_y = int(source_height * 0.425)
    crop_max_y = int(source_height * 0.895)
    for y in range(output_size):
        v = y / max(1, output_size - 1)
        source_y = int(crop_min_y + v * (crop_max_y - crop_min_y - 1))
        for x in range(output_size):
            u = x / max(1, output_size - 1)
            source_x = int(crop_min_x + u * (crop_max_x - crop_min_x - 1))
            samples = []
            for oy in (-3, 0, 3):
                for ox in (-3, 0, 3):
                    sx = max(0, min(source_width - 1, source_x + ox))
                    sy = max(0, min(source_height - 1, source_y + oy))
                    index = (sy * source_width + sx) * 4
                    samples.append(source_pixels[index:index + 3])
            color = Vector((sum(s[0] for s in samples), sum(s[1] for s in samples), sum(s[2] for s in samples))) / len(samples)
            vignette = max(0.34, 1.0 - ((u - 0.5) ** 2 + (v - 0.5) ** 2) * 1.70)
            color *= vignette * (0.72 + fractal_noise(u * 5.0, v * 5.0, 91.0) * 0.16)
            index = (y * output_size + x) * 4
            output[index:index + 4] = (*color, 1.0)
    image.pixels.foreach_set(output)
    image.filepath_raw = str(TEXTURE_DIR / "dolore_portrait.png")
    image.file_format = "PNG"
    image.update()
    image.save()
    return image


def material_from_textures(name, textures, base_color, metallic, bump_strength, coat_weight=0.0):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (*base_color, 1.0)
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.inputs["Metallic"].default_value = metallic
    if "IOR Level" in shader.inputs:
        shader.inputs["IOR Level"].default_value = 0.28
    if "Coat Weight" in shader.inputs:
        shader.inputs["Coat Weight"].default_value = coat_weight
        shader.inputs["Coat Roughness"].default_value = 0.34
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = textures["albedo"]
    roughness = nodes.new("ShaderNodeTexImage")
    roughness.image = textures["roughness"]
    roughness.image.colorspace_settings.name = "Non-Color"
    height = nodes.new("ShaderNodeTexImage")
    height.image = textures["height"]
    height.image.colorspace_settings.name = "Non-Color"
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.07
    links.new(albedo.outputs["Color"], shader.inputs["Base Color"])
    links.new(roughness.outputs["Color"], shader.inputs["Roughness"])
    links.new(height.outputs["Color"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def create_portrait_material(image):
    material = bpy.data.materials.new("Dolore_Faded_Portrait")
    material.use_nodes = True
    material.diffuse_color = (0.18, 0.16, 0.11, 1.0)
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    shader.inputs["Roughness"].default_value = 0.82
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = image
    links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    if "Emission Color" in shader.inputs:
        links.new(texture.outputs["Color"], shader.inputs["Emission Color"])
        shader.inputs["Emission Strength"].default_value = 0.14
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return material


def connected_components(mesh):
    adjacency = defaultdict(set)
    for edge in mesh.edges:
        a, b = edge.vertices
        adjacency[a].add(b)
        adjacency[b].add(a)
    component_by_vertex = {}
    components = []
    for vertex in mesh.vertices:
        if vertex.index in component_by_vertex:
            continue
        stack = [vertex.index]
        component_index = len(components)
        component_vertices = []
        component_by_vertex[vertex.index] = component_index
        while stack:
            current = stack.pop()
            component_vertices.append(current)
            for neighbor in adjacency[current]:
                if neighbor not in component_by_vertex:
                    component_by_vertex[neighbor] = component_index
                    stack.append(neighbor)
        components.append(component_vertices)
    order = sorted(range(len(components)), key=lambda i: len(components[i]), reverse=True)
    rank = {old: new for new, old in enumerate(order)}
    return {vertex: rank[component] for vertex, component in component_by_vertex.items()}, [components[i] for i in order]


def assign_materials_and_uv(mesh_object, body_material, frame_material, portrait_material):
    mesh = mesh_object.data
    component_by_vertex, components = connected_components(mesh)
    mesh.materials.clear()
    mesh.materials.append(body_material)
    mesh.materials.append(frame_material)
    mesh.materials.append(portrait_material)
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    frame_components = {1, 2, 7, 8}
    portrait_component = components[1]
    minimum = Vector((
        min(mesh.vertices[index].co.x for index in portrait_component),
        min(mesh.vertices[index].co.y for index in portrait_component),
        min(mesh.vertices[index].co.z for index in portrait_component),
    ))
    maximum = Vector((
        max(mesh.vertices[index].co.x for index in portrait_component),
        max(mesh.vertices[index].co.y for index in portrait_component),
        max(mesh.vertices[index].co.z for index in portrait_component),
    ))
    for polygon in mesh.polygons:
        component = component_by_vertex[polygon.vertices[0]]
        center = polygon.center
        normalized_x = (center.x - minimum.x) / max(1e-6, maximum.x - minimum.x)
        normalized_y = (center.y - minimum.y) / max(1e-6, maximum.y - minimum.y)
        normalized_z = (center.z - minimum.z) / max(1e-6, maximum.z - minimum.z)
        portrait_surface = (
            component == 1
            and 0.12 < normalized_x < 0.88
            and 0.10 < normalized_y < 0.90
            and normalized_z > 0.31
            and abs(polygon.normal.z) > 0.28
        )
        polygon.material_index = 2 if portrait_surface else (1 if component in frame_components else 0)

    bpy.context.view_layer.objects.active = mesh_object
    mesh_object.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.025)
    bpy.ops.object.mode_set(mode="OBJECT")

    portrait_vertices = sorted({
        vertex_index
        for polygon in mesh.polygons
        if polygon.material_index == 2
        for vertex_index in polygon.vertices
    })
    if not portrait_vertices:
        raise RuntimeError("No portrait polygons were classified on the source mesh.")
    minimum_x = min(mesh.vertices[index].co.x for index in portrait_vertices)
    maximum_x = max(mesh.vertices[index].co.x for index in portrait_vertices)
    minimum_y = min(mesh.vertices[index].co.y for index in portrait_vertices)
    maximum_y = max(mesh.vertices[index].co.y for index in portrait_vertices)
    uv_layer = mesh.uv_layers.active.data
    for polygon in mesh.polygons:
        if polygon.material_index != 2:
            continue
        for loop_index in polygon.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            uv_layer[loop_index].uv = (
                (vertex.x - minimum_x) / max(1e-6, maximum_x - minimum_x),
                (vertex.y - minimum_y) / max(1e-6, maximum_y - minimum_y),
            )
    return components


def look_at(obj, target):
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def add_stage(model_bounds):
    minimum, maximum = model_bounds
    center = (minimum + maximum) * 0.5
    size = maximum - minimum
    stage_collection = bpy.data.collections.new("Review_Stage")
    bpy.context.scene.collection.children.link(stage_collection)
    plane_size = max(size.x, size.y, size.z) * 7.0
    bpy.ops.mesh.primitive_plane_add(size=plane_size, location=(center.x, center.y, minimum.z - 0.015))
    plane = bpy.context.object
    plane.name = "Review_Ground"
    for collection in list(plane.users_collection):
        collection.objects.unlink(plane)
    stage_collection.objects.link(plane)
    ground = bpy.data.materials.new("Review_Ground_Material")
    ground.use_nodes = True
    shader = ground.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (0.055, 0.070, 0.066, 1.0)
    shader.inputs["Roughness"].default_value = 0.86
    plane.data.materials.append(ground)

    world = bpy.data.worlds.new("Dolore_Review_World")
    bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.018, 0.024, 0.023, 1.0)
    background.inputs["Strength"].default_value = 0.30

    radius = max(size.x, size.y, size.z)
    energy_scale = max(0.000001, (radius / 2.0) ** 2)
    lights = [
        ("Key_Light", center + Vector((radius * 2.3, -radius * 2.8, radius * 2.5)), 1050.0 * energy_scale, (0.78, 0.90, 0.87), radius * 1.6),
        ("Warm_Fill", center + Vector((-radius * 2.0, -radius * 1.5, radius * 1.2)), 520.0 * energy_scale, (0.95, 0.66, 0.38), radius * 1.4),
        ("Teal_Rim", center + Vector((radius * 1.2, radius * 2.2, radius * 1.8)), 720.0 * energy_scale, (0.20, 0.70, 0.62), radius * 1.2),
    ]
    for name, location, energy, color, light_size in lights:
        data = bpy.data.lights.new(name, "AREA")
        data.energy = energy
        data.color = color
        data.shape = "DISK"
        data.size = light_size
        light = bpy.data.objects.new(name, data)
        light.location = location
        stage_collection.objects.link(light)
        look_at(light, center)

    camera_data = bpy.data.cameras.new("Dolore_Review_Camera")
    camera = bpy.data.objects.new("Dolore_Review_Camera", camera_data)
    camera.data.clip_start = max(0.000001, radius / 1000.0)
    camera.data.clip_end = max(1.0, radius * 1000.0)
    stage_collection.objects.link(camera)
    bpy.context.scene.camera = camera
    return stage_collection, camera, center, size


def configure_render():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"


def render_view(camera, name, location, target, lens=58.0):
    camera.location = location
    camera.data.lens = lens
    look_at(camera, target)
    bpy.context.scene.render.filepath = str(RENDER_DIR / f"{name}.png")
    bpy.ops.render.render(write_still=True)


def export_model(armature, mesh_object, stage_collection):
    stage_collection.hide_viewport = True
    stage_collection.hide_render = True
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    mesh_object.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_DIR / "Dolore_CurrentModel_ReferenceSync.fbx"),
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=False,
        use_mesh_modifiers=False,
        path_mode="COPY",
        embed_textures=False,
        apply_scale_options="FBX_SCALE_ALL",
    )
    bpy.ops.object.select_all(action="DESELECT")
    mesh_object.select_set(True)
    bpy.context.view_layer.objects.active = mesh_object
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_DIR / "Dolore_CurrentModel_ReferenceSync.glb"),
        export_format="GLB",
        use_selection=True,
        export_animations=False,
        export_skins=False,
        export_yup=True,
    )
    stage_collection.hide_viewport = False
    stage_collection.hide_render = False


def save_manifest(mesh_object, armature, components, before_signature, after_signature, before_transform, after_transform):
    minimum, maximum = bounds_world([mesh_object])
    manifest = {
        "sample": "Dolore source-model material synchronization",
        "source_fbx": str(SOURCE_FBX),
        "source_fbx_sha256": sha256(SOURCE_FBX),
        "source_reference": [str(REFERENCE_STATIC), str(REFERENCE_ATTACK)],
        "geometry_policy": "source_mesh_only_no_shape_changes",
        "mesh": mesh_object.name,
        "connected_components": len(components),
        "bones": len(armature.data.bones),
        "source_geometry_before_materials": before_signature,
        "source_geometry_after_materials": after_signature,
        "source_transform_before_materials": before_transform,
        "source_transform_after_materials": after_transform,
        "geometry_signature_preserved": before_signature == after_signature,
        "transform_preserved": before_transform == after_transform,
        "added_model_mesh_objects": 0,
        "model_size_m_unchanged": [round(value, 6) for value in (maximum - minimum)],
        "materials": [material.name for material in mesh_object.data.materials],
        "material_scope": ["body color and wet tissue response", "oxidized brass and verdigris frame", "faded portrait panel"],
        "static_export": "exports/Dolore_CurrentModel_ReferenceSync.fbx",
        "review_export": "exports/Dolore_CurrentModel_ReferenceSync.glb",
        "unity_applied": False,
    }
    with (SAMPLE_ROOT / "ASSET_MANIFEST.json").open("w", encoding="utf-8") as handle:
        json.dump(manifest, handle, ensure_ascii=False, indent=2)


def main():
    ensure_directories()
    reset_scene()
    armature = bpy.data.objects.get("Armature")
    mesh_object = bpy.data.objects.get("char1")
    if armature is None or mesh_object is None:
        raise RuntimeError("The source FBX must contain Armature and char1.")

    before_signature = mesh_signature(mesh_object.data)
    before_transform = transform_signature(mesh_object)
    armature.name = "Dolore_Rig"
    mesh_object.name = "Dolore_CurrentModel"

    body_textures = create_body_textures()
    frame_textures = create_frame_textures()
    portrait_image = create_portrait_texture()
    body_material = material_from_textures(
        "Dolore_Wet_Deep_Teal_Tissue", body_textures, (0.010, 0.145, 0.122), 0.0, 0.24, coat_weight=0.16
    )
    frame_material = material_from_textures(
        "Dolore_Oxidized_Brass_Frame", frame_textures, (0.10, 0.075, 0.025), 0.48, 0.18
    )
    portrait_material = create_portrait_material(portrait_image)
    components = assign_materials_and_uv(mesh_object, body_material, frame_material, portrait_material)

    after_signature = mesh_signature(mesh_object.data)
    after_transform = transform_signature(mesh_object)
    if before_signature != after_signature or before_transform != after_transform:
        raise RuntimeError("Source mesh geometry or object transform changed during material synchronization.")

    model_bounds = bounds_world([mesh_object])
    stage_collection, camera, center, size = add_stage(model_bounds)
    configure_render()
    distance = max(size.x, size.y, size.z) * 3.45
    render_view(camera, "01_reference_matched_three_quarter", center + Vector((distance * 0.58, -distance, size.z * 0.28)), center, 60.0)
    render_view(camera, "02_front", center + Vector((0.0, -distance, size.z * 0.12)), center, 62.0)
    render_view(camera, "03_side", center + Vector((distance, 0.0, size.z * 0.12)), center, 62.0)
    render_view(camera, "04_back", center + Vector((0.0, distance, size.z * 0.20)), center, 62.0)
    render_view(camera, "05_material_closeup", center + Vector((distance * 0.26, -distance * 0.72, size.z * 0.10)), center, 68.0)

    export_model(armature, mesh_object, stage_collection)
    save_manifest(mesh_object, armature, components, before_signature, after_signature, before_transform, after_transform)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER_DIR / "Dolore_CurrentModel_ReferenceSync.blend"))
    print("DOLORE_MATERIAL_SYNC_RESULT=PASS")


if __name__ == "__main__":
    main()
