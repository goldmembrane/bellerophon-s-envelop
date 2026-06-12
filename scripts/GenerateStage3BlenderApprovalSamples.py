import math
import os
import random
import sys
import textwrap

import bpy
from mathutils import Vector


TEXTURE_SIZE = 768
RENDER_WIDTH = 1440
RENDER_HEIGHT = 900


SAMPLE_ITEMS = [
    ("01", "cockpit_helm_and_status", "조종실 조타 장치와 상태 화면"),
    ("02", "control_room_cctv_terminal", "통제실 CCTV 단말"),
    ("03", "engine_room_power_terminal", "동력실 전력 단말"),
    ("04", "supply_room_storage_cabinet", "비품실 보관 캐비닛"),
    ("05", "cargo_hold_props_and_terminal", "화물칸 화물 소품과 단말"),
    ("06", "armory_turret_grip_mount", "무기실 기둥형 수동 포탑과 전면 곡면 스크린"),
    ("07", "first_person_equipment", "1인칭 장비와 양손 막대기"),
]


def parse_project_root():
    args = sys.argv
    extra = args[args.index("--") + 1 :] if "--" in args else []
    for index, value in enumerate(extra):
        if value == "--project-root" and index + 1 < len(extra):
            return os.path.abspath(extra[index + 1])
    return os.getcwd()


PROJECT_ROOT = parse_project_root()
SOURCE_REVIEW_DIR = os.path.join(PROJECT_ROOT, "artSample", "stage3_rework_review")
SAMPLE_ROOT = os.path.join(PROJECT_ROOT, "artSample", "stage3_blender_rebuild_sample")
RENDER_DIR = os.path.join(SAMPLE_ROOT, "renders")
EXPORT_DIR = os.path.join(SAMPLE_ROOT, "exports")
TEXTURE_DIR = os.path.join(SAMPLE_ROOT, "textures")
BLENDER_DIR = os.path.join(SAMPLE_ROOT, "blender")


def ensure_dirs():
    for path in (SAMPLE_ROOT, RENDER_DIR, EXPORT_DIR, TEXTURE_DIR, BLENDER_DIR):
        os.makedirs(path, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.images,
        bpy.data.collections,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for item in list(datablocks):
            datablocks.remove(item)


def new_collection(name):
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def link_to_collection(obj, col):
    for existing in list(obj.users_collection):
        existing.objects.unlink(obj)
    col.objects.link(obj)


def configure_scene():
    scene = bpy.context.scene
    for engine_name in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            scene.render.engine = engine_name
            break
        except TypeError:
            continue
    scene.render.resolution_x = RENDER_WIDTH
    scene.render.resolution_y = RENDER_HEIGHT
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("Stage3ApprovalWorld") if not scene.world else scene.world
    scene.world.color = (0.012, 0.013, 0.013)
    if hasattr(scene, "eevee"):
        if hasattr(scene.eevee, "taa_render_samples"):
            scene.eevee.taa_render_samples = 64
        if hasattr(scene.eevee, "use_bloom"):
            scene.eevee.use_bloom = True
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = -0.15
    scene.view_settings.gamma = 1.0


def make_texture(name, style, base, accent, seed):
    rng = random.Random(seed)
    width = TEXTURE_SIZE
    height = TEXTURE_SIZE
    image = bpy.data.images.new(name, width, height, alpha=True)
    pixels = [0.0] * (width * height * 4)

    for y in range(height):
        for x in range(width):
            n = rng.random() * 0.02
            n += random.Random((x * 73856093) ^ (y * 19349663) ^ seed).random() * 0.42
            coarse = random.Random(((x // 18) * 73856093) ^ ((y // 18) * 19349663) ^ seed).random()
            mid = random.Random(((x // 7) * 83492791) ^ ((y // 7) * 2971215073) ^ (seed * 17)).random()
            pit = mid > 0.965 or (coarse > 0.88 and n > 0.32)
            diagonal = ((x + y * 2 + seed) // 34) % 2 == 0

            if style == "screen":
                scanline = 0.16 if y % 8 < 2 else 0.0
                grid = x % 64 < 2 or y % 64 < 2
                glow = 0.24 + n * 0.45 + scanline
                color = (
                    base[0] * (1.0 - glow) + accent[0] * glow,
                    base[1] * (1.0 - glow) + accent[1] * glow,
                    base[2] * (1.0 - glow) + accent[2] * glow,
                )
                if grid:
                    color = (color[0] * 0.8, min(1.0, color[1] + 0.18), color[2] * 0.88)
            elif style == "hazard":
                chosen = base if diagonal else accent
                chip = n > 0.38 or pit
                factor = 0.45 if chip else 0.9 + n * 0.25
                color = (chosen[0] * factor, chosen[1] * factor, chosen[2] * factor)
            elif style == "rubber":
                rib = 0.09 if (x + y + seed) % 23 < 4 else 0.0
                color = (base[0] + rib, base[1] + rib, base[2] + rib)
            elif style == "leather":
                crease = 0.10 if ((x * 5 + y * 2 + seed) % 67) < 3 else 0.0
                color = (base[0] + n * 0.15 + crease, base[1] + n * 0.10 + crease, base[2] + n * 0.08 + crease)
            else:
                factor = 0.58 + n * 0.95 + coarse * 0.14
                if pit:
                    factor = 0.38 + coarse * 0.12
                    color = (
                        base[0] * factor + accent[0] * 0.12,
                        base[1] * factor + accent[1] * 0.12,
                        base[2] * factor + accent[2] * 0.12,
                    )
                else:
                    rust = 0.060 if mid > 0.988 else 0.0
                    color = (
                        base[0] * factor + rust,
                        base[1] * factor + rust * 0.45,
                        base[2] * factor + rust * 0.22,
                    )

            index = (y * width + x) * 4
            pixels[index] = max(0.0, min(1.0, color[0]))
            pixels[index + 1] = max(0.0, min(1.0, color[1]))
            pixels[index + 2] = max(0.0, min(1.0, color[2]))
            pixels[index + 3] = 1.0

    image.pixels.foreach_set(pixels)
    image.update()
    image.filepath_raw = os.path.join(TEXTURE_DIR, name + ".png")
    image.file_format = "PNG"
    image.save()
    return image


def make_material(
    name,
    color,
    image=None,
    metallic=0.0,
    roughness=0.55,
    emission_strength=0.0,
    bump_strength=0.0,
    bump_scale=42.0,
    roughness_noise=False,
):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if image:
            tex = nodes.new(type="ShaderNodeTexImage")
            tex.image = image
            mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        if emission_strength > 0.0 and "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = color
            bsdf.inputs["Emission Strength"].default_value = emission_strength
        if bump_strength > 0.0:
            noise = nodes.new(type="ShaderNodeTexNoise")
            noise.inputs["Scale"].default_value = bump_scale
            noise.inputs["Detail"].default_value = 13.0
            noise.inputs["Roughness"].default_value = 0.64
            bump = nodes.new(type="ShaderNodeBump")
            bump.inputs["Strength"].default_value = bump_strength
            bump.inputs["Distance"].default_value = 0.070
            mat.node_tree.links.new(noise.outputs["Fac"], bump.inputs["Height"])
            mat.node_tree.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
            if roughness_noise:
                ramp = nodes.new(type="ShaderNodeValToRGB")
                ramp.color_ramp.elements[0].position = 0.18
                ramp.color_ramp.elements[0].color = (roughness * 0.75, roughness * 0.75, roughness * 0.75, 1)
                ramp.color_ramp.elements[1].position = 1.0
                ramp.color_ramp.elements[1].color = (min(1.0, roughness + 0.30), min(1.0, roughness + 0.30), min(1.0, roughness + 0.30), 1)
                mat.node_tree.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
                mat.node_tree.links.new(ramp.outputs["Color"], bsdf.inputs["Roughness"])
    return mat


def create_materials():
    textures = {
        "dark_metal": make_texture("ST3_Approval_DarkWornMetal_Albedo", "metal", (0.16, 0.16, 0.145), (0.76, 0.70, 0.58), 101),
        "edge_metal": make_texture("ST3_Approval_PolishedEdgeMetal_Albedo", "metal", (0.42, 0.43, 0.39), (0.96, 0.93, 0.78), 102),
        "wall": make_texture("ST3_Approval_BlackenedWallPanel_Albedo", "metal", (0.105, 0.11, 0.105), (0.46, 0.43, 0.34), 103),
        "floor": make_texture("ST3_Approval_DiamondPlateFloor_Albedo", "metal", (0.13, 0.14, 0.135), (0.62, 0.57, 0.44), 104),
        "screen": make_texture("ST3_Approval_GreenCrtGlass_Albedo", "screen", (0.008, 0.045, 0.030), (0.14, 0.48, 0.24), 105),
        "rubber": make_texture("ST3_Approval_BlackRibbedRubber_Albedo", "rubber", (0.018, 0.018, 0.016), (0.10, 0.10, 0.09), 106),
        "olive": make_texture("ST3_Approval_OliveChippedPaint_Albedo", "metal", (0.23, 0.25, 0.18), (0.72, 0.66, 0.46), 107),
        "cargo": make_texture("ST3_Approval_BlueGrayCargoMetal_Albedo", "metal", (0.20, 0.25, 0.27), (0.75, 0.68, 0.53), 108),
        "yellow": make_texture("ST3_Approval_WornYellowHazard_Albedo", "hazard", (0.88, 0.58, 0.06), (0.06, 0.055, 0.045), 109),
        "red": make_texture("ST3_Approval_WornRedPaint_Albedo", "hazard", (0.65, 0.07, 0.04), (0.06, 0.045, 0.04), 110),
        "leather": make_texture("ST3_Approval_BlackCreasedGlove_Albedo", "leather", (0.025, 0.024, 0.022), (0.12, 0.11, 0.09), 111),
    }

    return {
        "dark_metal": make_material("MAT_DarkWornMetal", (0.20, 0.20, 0.18, 1), textures["dark_metal"], 0.72, 0.48, 0.0, 0.060, 78.0, True),
        "edge_metal": make_material("MAT_BrightScratchedEdges", (0.64, 0.62, 0.52, 1), textures["edge_metal"], 0.82, 0.37, 0.0, 0.035, 96.0, True),
        "wall": make_material("MAT_BlackenedWallPanels", (0.11, 0.12, 0.11, 1), textures["wall"], 0.55, 0.68, 0.0, 0.075, 64.0, True),
        "floor": make_material("MAT_OilyDiamondPlateFloor", (0.14, 0.15, 0.14, 1), textures["floor"], 0.68, 0.48, 0.0, 0.055, 88.0, True),
        "screen": make_material("MAT_GreenCrtEmission", (0.020, 0.25, 0.12, 1), textures["screen"], 0.0, 0.32, 0.72),
        "screen_dim": make_material("MAT_DimCrtGlass", (0.012, 0.11, 0.06, 1), textures["screen"], 0.0, 0.46, 0.25),
        "rubber": make_material("MAT_RibbedBlackRubber", (0.025, 0.025, 0.022, 1), textures["rubber"], 0.0, 0.78, 0.0, 0.050, 120.0, True),
        "olive": make_material("MAT_OliveChippedCabinetPaint", (0.28, 0.30, 0.21, 1), textures["olive"], 0.42, 0.64, 0.0, 0.070, 70.0, True),
        "cargo": make_material("MAT_BlueGrayCargoMetal", (0.24, 0.29, 0.31, 1), textures["cargo"], 0.55, 0.58, 0.0, 0.060, 82.0, True),
        "yellow": make_material("MAT_WornYellowPaint", (0.72, 0.50, 0.08, 1), textures["yellow"], 0.25, 0.63),
        "yellow_plain": make_material("MAT_WornFlatYellowPaint", (0.56, 0.40, 0.10, 1), None, 0.18, 0.68),
        "red": make_material("MAT_WornRedPaint", (0.55, 0.055, 0.035, 1), textures["red"], 0.20, 0.65),
        "leather": make_material("MAT_BlackCreasedGlove", (0.030, 0.029, 0.026, 1), textures["leather"], 0.0, 0.76, 0.0, 0.050, 95.0, True),
        "rust": make_material("MAT_RustAndOldScratches", (0.26, 0.12, 0.055, 1), None, 0.0, 0.88),
        "scratch": make_material("MAT_ExposedScrapedMetal", (0.36, 0.34, 0.27, 1), None, 0.42, 0.48),
        "black": make_material("MAT_DeadBlackInset", (0.006, 0.006, 0.005, 1), None, 0.0, 0.84),
        "warm_light": make_material("MAT_WarmStripLight", (1.0, 0.76, 0.46, 1), None, 0.0, 0.24, 4.0),
        "green_label": make_material("MAT_SoftGreenLabel", (0.12, 0.43, 0.22, 1), None, 0.0, 0.42, 0.45),
    }


def shade_smooth(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try:
        bpy.ops.object.shade_smooth()
    except RuntimeError:
        pass
    obj.select_set(False)


def box(name, loc, scale, mat, col, bevel=0.0, segments=1, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0.0:
        mod = obj.modifiers.new("approval bevels", "BEVEL")
        mod.width = bevel
        mod.segments = segments
        mod.affect = "EDGES"
        obj.modifiers.new("weighted worn normals", "WEIGHTED_NORMAL")
    link_to_collection(obj, col)
    return obj


def cyl(name, loc, radius, depth, mat, col, vertices=32, rot=(0.0, 0.0, 0.0), bevel=True):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    shade_smooth(obj)
    if bevel:
        mod = obj.modifiers.new("worn rim bevel", "BEVEL")
        mod.width = max(0.004, radius * 0.08)
        mod.segments = 2
        obj.modifiers.new("weighted cylinder normals", "WEIGHTED_NORMAL")
    link_to_collection(obj, col)
    return obj


def cone(name, loc, radius1, radius2, depth, mat, col, vertices=32, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    shade_smooth(obj)
    obj.modifiers.new("weighted cone normals", "WEIGHTED_NORMAL")
    link_to_collection(obj, col)
    return obj


def torus(name, loc, major, minor, mat, col, rot=(0.0, 0.0, 0.0), segments=80):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major,
        minor_radius=minor,
        major_segments=segments,
        minor_segments=14,
        location=loc,
        rotation=rot,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    shade_smooth(obj)
    obj.modifiers.new("weighted torus normals", "WEIGHTED_NORMAL")
    link_to_collection(obj, col)
    return obj


def curve_pipe(name, points, radius, mat, col, resolution=3):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 16
    curve.bevel_depth = radius
    curve.bevel_resolution = resolution
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, co in zip(spline.points, points):
        point.co = (co[0], co[1], co[2], 1.0)
    obj = bpy.data.objects.new(name, curve)
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def bezier_pipe(name, points, radius, mat, col):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 24
    curve.bevel_depth = radius
    curve.bevel_resolution = 5
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for point, co in zip(spline.bezier_points, points):
        point.co = co
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    obj.data.materials.append(mat)
    col.objects.link(obj)
    return obj


def bar_between(name, start, end, width, thickness, mat, col, bevel=0.010):
    sx, sy, sz = start
    ex, ey, ez = end
    dx = ex - sx
    dz = ez - sz
    length = max(0.001, math.sqrt(dx * dx + dz * dz))
    center = ((sx + ex) * 0.5, (sy + ey) * 0.5, (sz + ez) * 0.5)
    angle_y = math.atan2(dx, dz)
    return box(name, center, (width, thickness, length), mat, col, bevel, 2, (0.0, angle_y, 0.0))


def tapered_bar_between(name, start, end, start_width, end_width, thickness, mat, col, bevel=0.006):
    sx, sy, sz = start
    ex, ey, ez = end
    dx = ex - sx
    dz = ez - sz
    length = max(0.001, math.sqrt(dx * dx + dz * dz))
    px = dz / length
    pz = -dx / length
    half_t = thickness * 0.5

    def cross(center, width):
        cx, cy, cz = center
        half_w = width * 0.5
        return [
            (cx - px * half_w, cy - half_t, cz - pz * half_w),
            (cx + px * half_w, cy - half_t, cz + pz * half_w),
            (cx + px * half_w, cy + half_t, cz + pz * half_w),
            (cx - px * half_w, cy + half_t, cz - pz * half_w),
        ]

    verts = cross(start, start_width) + cross(end, end_width)
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    if bevel > 0.0:
        mod = obj.modifiers.new("forged bevels", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        obj.modifiers.new("weighted forged normals", "WEIGHTED_NORMAL")
    col.objects.link(obj)
    return obj


def front_y(center_y, depth):
    return center_y - depth * 0.5 - 0.006


def front_plate(prefix, center, size, mat, col, bevel=0.025):
    return box(prefix, center, size, mat, col, bevel, 2)


def bolt(prefix, loc, mat, col, radius=0.035, depth=0.028):
    head = cyl(prefix, loc, radius, depth, mat, col, vertices=24, rot=(math.radians(90), 0, 0))
    box(prefix + "_slot", (loc[0], loc[1] - 0.017, loc[2]), (radius * 1.45, 0.006, radius * 0.20), mat, col, 0.002, 1)
    return head


def corner_bolts(prefix, center, width, height, y, mat, col, radius=0.03):
    for index, (sx, sz) in enumerate(((-1, -1), (1, -1), (-1, 1), (1, 1)), start=1):
        bolt(f"{prefix}_corner_bolt_{index}", (center[0] + sx * width * 0.43, y, center[2] + sz * height * 0.43), mat, col, radius)


def scatter_scratches(prefix, col, mats, center, width, height, y, count=28, seed=0):
    # Surface wear is handled by material noise/bump textures. Separate scratch strips read as drawn-on lines in approval renders.
    return


def broken_paint_pattern(prefix, loc, width, height, mat, col, seed=0):
    rng = random.Random(seed)
    x0, y0, z0 = loc
    band_mat = mat
    gap_mat = bpy.data.materials["MAT_OliveChippedCabinetPaint"] if "MAT_OliveChippedCabinetPaint" in bpy.data.materials else mat
    for i in range(3):
        z = z0 + rng.uniform(-height * 0.035, height * 0.035)
        box(
            f"{prefix}_wide_worn_paint_layer_{i}",
            (x0 + rng.uniform(-width * 0.035, width * 0.035), y0 - i * 0.002, z),
            (width * rng.uniform(0.76, 0.94), 0.009, height * rng.uniform(0.22, 0.30)),
            band_mat,
            col,
            0.002,
            1,
        )
    for i in range(9):
        box(
            f"{prefix}_paint_chipped_gap_{i}",
            (x0 + rng.uniform(-width * 0.38, width * 0.38), y0 - 0.010, z0 + rng.uniform(-height * 0.09, height * 0.09)),
            (rng.uniform(width * 0.035, width * 0.11), 0.004, height * rng.uniform(0.030, 0.080)),
            gap_mat,
            col,
            0.001,
            1,
        )


def add_text(prefix, text, loc, size, mat, col, rot=(math.radians(90), 0.0, 0.0)):
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = prefix
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.002
    obj.data.materials.append(mat)
    link_to_collection(obj, col)
    return obj


def screen(prefix, center, size, mats, col, bars=False, map_lines=False):
    frame = box(prefix + "_thick_chipped_frame", center, size, mats["dark_metal"], col, 0.050, 3)
    fy = front_y(center[1], size[1])
    box(prefix + "_black_rubber_gasket", (center[0], fy - 0.012, center[2]), (size[0] * 0.86, 0.030, size[2] * 0.76), mats["rubber"], col, 0.025, 2)
    glass = box(prefix + "_green_crt_glass", (center[0], fy - 0.030, center[2]), (size[0] * 0.75, 0.018, size[2] * 0.58), mats["screen"], col, 0.016, 2)
    corner_bolts(prefix, center, size[0], size[2], fy - 0.050, mats["edge_metal"], col, 0.030)
    for i in range(7):
        z = center[2] - size[2] * 0.25 + i * size[2] * 0.055
        box(prefix + f"_scanline_{i:02d}", (center[0], fy - 0.043, z), (size[0] * 0.55, 0.003, 0.0025), mats["green_label"], col, 0.0, 1)
    if bars:
        for i in range(10):
            z = center[2] - size[2] * 0.27 + i * size[2] * 0.058
            box(prefix + f"_status_bar_{i:02d}", (center[0], fy - 0.048, z), (size[0] * 0.48, 0.006, size[2] * 0.025), mats["green_label"], col, 0.002, 1)
    if map_lines:
        rng = random.Random(27)
        for i in range(10):
            x = center[0] + rng.uniform(-size[0] * 0.25, size[0] * 0.25)
            z = center[2] + rng.uniform(-size[2] * 0.20, size[2] * 0.20)
            w = rng.uniform(size[0] * 0.08, size[0] * 0.22)
            h = rng.uniform(size[2] * 0.04, size[2] * 0.15)
            box(prefix + f"_map_room_line_h_{i:02d}", (x, fy - 0.049, z), (w, 0.006, 0.006), mats["green_label"], col, 0.0, 1)
            box(prefix + f"_map_room_line_v_{i:02d}", (x - w * 0.5, fy - 0.050, z + h * 0.5), (0.006, 0.006, h), mats["green_label"], col, 0.0, 1)
    scatter_scratches(prefix, col, mats, center, size[0], size[2], fy - 0.055, count=14, seed=91)
    return frame, glass


def industrial_floor(prefix, col, mats, width=5.0, depth=4.0, z=-1.05):
    box(prefix + "_oily_diamond_floor_base", (0, 0, z), (width, depth, 0.08), mats["floor"], col, 0.020, 2)
    for x in (-width * 0.25, 0.0, width * 0.25):
        box(prefix + f"_floor_trench_{x:.1f}", (x, -0.15, z + 0.055), (0.035, depth * 0.82, 0.012), mats["black"], col, 0.002, 1)
    for i in range(8):
        box(prefix + f"_raised_floor_panel_{i}", (-width * 0.42 + i * width * 0.12, -0.55, z + 0.065), (0.34, 0.52, 0.016), mats["dark_metal"], col, 0.012, 1)
    box(prefix + "_black_recessed_hazard_strip", (0.0, -depth * 0.35, z + 0.078), (width * 0.86, 0.16, 0.012), mats["black"], col, 0.004, 1)
    for i in range(12):
        x = -width * 0.39 + i * width * 0.071
        box(prefix + f"_worn_yellow_hazard_patch_{i}", (x, -depth * 0.35, z + 0.089), (0.20, 0.11, 0.014), mats["yellow"], col, 0.002, 1, (0, 0, math.radians(-24)))


def wall_panels(prefix, col, mats, width=5.2, height=2.8, y=1.35, z=0.15):
    box(prefix + "_rear_blackened_wall", (0, y, z), (width, 0.12, height), mats["wall"], col, 0.020, 2)
    for i in range(6):
        x = -width * 0.42 + i * width * 0.17
        box(prefix + f"_rear_panel_{i}", (x, y - 0.068, z + 0.20), (0.62, 0.025, 1.65), mats["dark_metal"], col, 0.018, 2)
        scatter_scratches(prefix + f"_rear_panel_{i}", col, mats, (x, y - 0.082, z + 0.20), 0.62, 1.65, y - 0.092, 8, 200 + i)
    for i in range(5):
        curve_pipe(
            prefix + f"_overhead_pipe_{i}",
            [(-width * 0.48, y - 0.10 - i * 0.015, z + height * 0.43 - i * 0.08), (width * 0.48, y - 0.10 - i * 0.015, z + height * 0.43 - i * 0.08)],
            0.025,
            mats["rubber"] if i % 2 else mats["dark_metal"],
            col,
        )


def strip_light(prefix, loc, scale, col, mats):
    box(prefix + "_warm_cage", loc, scale, mats["warm_light"], col, 0.008, 1)
    for i in range(5):
        box(prefix + f"_black_light_guard_{i}", (loc[0] - scale[0] * 0.42 + i * scale[0] * 0.21, loc[1] - 0.015, loc[2]), (0.010, 0.025, scale[2] * 1.15), mats["black"], col, 0.001, 1)
    bpy.ops.object.light_add(type="POINT", location=(loc[0], loc[1] - 0.25, loc[2] - 0.25))
    light = bpy.context.object
    light.name = prefix + "_warm_point_light"
    light.data.energy = 180
    light.data.color = (1.0, 0.78, 0.52)
    light.data.shadow_soft_size = 3.0
    link_to_collection(light, col)


def ribbed_grip(prefix, loc, radius, length, col, mats, axis="Z"):
    rot = (0.0, 0.0, 0.0)
    if axis == "X":
        rot = (0.0, math.radians(90), 0.0)
    elif axis == "Y":
        rot = (math.radians(90), 0.0, 0.0)
    cyl(prefix + "_rubber_core", loc, radius, length, mats["rubber"], col, 40, rot)
    ring_count = max(4, int(length / 0.055))
    for i in range(ring_count):
        offset = -length * 0.42 + i * length * 0.84 / max(1, ring_count - 1)
        if axis == "Z":
            rloc = (loc[0], loc[1], loc[2] + offset)
            torus(prefix + f"_rib_{i:02d}", rloc, radius * 1.04, radius * 0.055, mats["edge_metal"], col, (0, 0, 0), 36)
        elif axis == "X":
            rloc = (loc[0] + offset, loc[1], loc[2])
            torus(prefix + f"_rib_{i:02d}", rloc, radius * 1.04, radius * 0.055, mats["edge_metal"], col, (0, math.radians(90), 0), 36)
        else:
            rloc = (loc[0], loc[1] + offset, loc[2])
            torus(prefix + f"_rib_{i:02d}", rloc, radius * 1.04, radius * 0.055, mats["edge_metal"], col, (math.radians(90), 0, 0), 36)


def build_segmented_helm_ring(col, mats):
    center = (0.0, -0.86, -0.14)
    radius = 0.60
    box("cockpit_helm_floor_mounted_base", (0.0, -0.77, -0.92), (0.72, 0.42, 0.20), mats["dark_metal"], col, 0.035, 3)
    cyl("cockpit_helm_lower_pedestal_post", (0.0, -0.82, -0.56), 0.105, 0.68, mats["dark_metal"], col, 40)
    box("cockpit_helm_bottom_ring_clamp", (0.0, -0.88, -0.70), (0.45, 0.16, 0.18), mats["edge_metal"], col, 0.025, 2)
    curve_pipe("cockpit_helm_left_mount_brace", [(-0.20, -0.88, -0.68), (-0.34, -0.90, -0.50), (-0.43, -0.89, -0.33)], 0.030, mats["edge_metal"], col)
    curve_pipe("cockpit_helm_right_mount_brace", [(0.20, -0.88, -0.68), (0.34, -0.90, -0.50), (0.43, -0.89, -0.33)], 0.030, mats["edge_metal"], col)
    for i, angle in enumerate([205, 230, 255, 280, 305, 330, 25, 50, 75, 100, 125, 150]):
        rad = math.radians(angle)
        x = center[0] + math.cos(rad) * radius
        z = center[2] + math.sin(rad) * radius
        segment = box(
            f"cockpit_helm_ring_segment_{i:02d}",
            (x, center[1], z),
            (0.30, 0.105, 0.085),
            mats["dark_metal"],
            col,
            0.018,
            2,
            (0.0, math.radians(90 - angle), 0.0),
        )
        scatter_scratches(segment.name, col, mats, (x, center[1] - 0.060, z), 0.28, 0.08, center[1] - 0.071, 2, 500 + i)
        bolt(f"cockpit_helm_segment_bolt_{i:02d}_a", (x - math.sin(rad) * 0.08, center[1] - 0.065, z + math.cos(rad) * 0.08), mats["edge_metal"], col, 0.018)
        bolt(f"cockpit_helm_segment_bolt_{i:02d}_b", (x + math.sin(rad) * 0.08, center[1] - 0.065, z - math.cos(rad) * 0.08), mats["edge_metal"], col, 0.018)
    ribbed_grip("cockpit_left_handle", (-0.98, -0.90, -0.02), 0.085, 0.56, col, mats, "Z")
    ribbed_grip("cockpit_right_handle", (0.98, -0.90, -0.02), 0.085, 0.56, col, mats, "Z")
    curve_pipe("cockpit_left_bent_grip_arm", [(-0.62, -0.86, -0.14), (-0.92, -0.93, -0.06), (-0.98, -0.90, -0.27)], 0.030, mats["edge_metal"], col)
    curve_pipe("cockpit_right_bent_grip_arm", [(0.62, -0.86, -0.14), (0.92, -0.93, -0.06), (0.98, -0.90, -0.27)], 0.030, mats["edge_metal"], col)
    box("cockpit_red_thumb_button_left", (-0.98, -0.98, 0.26), (0.07, 0.025, 0.045), mats["red"], col, 0.011, 2)
    box("cockpit_red_thumb_button_right", (0.98, -0.98, 0.26), (0.07, 0.025, 0.045), mats["red"], col, 0.011, 2)


def build_cockpit(col, mats):
    industrial_floor("cockpit", col, mats, width=5.4, depth=3.7, z=-1.15)
    wall_panels("cockpit", col, mats, width=5.4, height=3.0, y=1.30, z=0.10)
    strip_light("cockpit_overhead_strip_light", (0.0, -0.35, 1.92), (1.00, 0.035, 0.07), col, mats)
    box("cockpit_wide_forward_window_outer_frame", (0, 1.205, 0.88), (3.75, 0.22, 1.25), mats["dark_metal"], col, 0.055, 3)
    box("cockpit_forward_window_black_glass", (0, 1.075, 0.88), (3.15, 0.035, 0.88), mats["black"], col, 0.045, 2)
    for x in (-2.05, 2.05):
        curve_pipe(f"cockpit_side_hose_{x}", [(x, 1.04, 1.35), (x * 0.88, 0.70, 0.56), (x * 0.78, 0.25, -0.45)], 0.040, mats["rubber"], col)
    box("cockpit_central_status_console_body", (0, -0.15, -0.35), (1.35, 0.58, 1.15), mats["dark_metal"], col, 0.055, 3)
    box("cockpit_console_sloped_base", (0, -0.40, -0.86), (1.02, 0.46, 0.70), mats["dark_metal"], col, 0.044, 3, (math.radians(-8), 0, 0))
    screen("cockpit_large_center_crt", (0, -0.61, 0.42), (1.45, 0.16, 0.64), mats, col)
    for i, x in enumerate((-1.45, -0.85, 0.86, 1.46)):
        screen(f"cockpit_side_status_monitor_{i}", (x, -0.40, -0.03), (0.62, 0.13, 0.42), mats, col)
    build_segmented_helm_ring(col, mats)
    for x in (-1.8, 1.8):
        box(f"cockpit_side_console_{x}", (x, -0.30, -0.55), (1.05, 0.40, 0.36), mats["dark_metal"], col, 0.035, 2)
    scatter_scratches("cockpit_console_body", col, mats, (0, -0.46, -0.35), 1.3, 1.1, -0.73, 38, 310)


def build_control_room(col, mats):
    industrial_floor("control_room", col, mats, width=5.2, depth=3.8, z=-1.15)
    wall_panels("control_room", col, mats, width=5.5, height=3.0, y=1.28, z=0.12)
    strip_light("control_room_upper_strip_light", (0.25, -0.25, 1.98), (0.88, 0.035, 0.07), col, mats)
    box("control_room_terminal_wall_recess", (0, 1.08, 0.18), (3.80, 0.26, 2.20), mats["black"], col, 0.065, 3)
    box("control_room_cctv_heavy_outer_panel", (0, 0.94, 0.20), (3.45, 0.18, 1.90), mats["dark_metal"], col, 0.060, 3)
    screen("control_room_single_large_cctv_screen", (-0.30, 0.82, 0.38), (2.08, 0.15, 0.92), mats, col, map_lines=True)
    screen("control_room_upper_left_map_screen", (-1.10, 0.76, 1.23), (0.82, 0.13, 0.34), mats, col, map_lines=True)
    screen("control_room_right_vertical_status_screen", (1.30, 0.80, 0.31), (0.54, 0.13, 1.28), mats, col, bars=True)
    box("control_room_sloped_button_console", (0.02, 0.58, -0.68), (3.18, 0.42, 0.40), mats["dark_metal"], col, 0.045, 3, (math.radians(-8), 0, 0))
    for i, mat in enumerate((mats["black"], mats["black"], mats["yellow_plain"], mats["red"])):
        box(f"control_room_function_button_{i}", (-0.78 + i * 0.23, 0.32, -0.58), (0.14, 0.055, 0.12), mat, col, 0.010, 2)
    for label, x, mat in (("A", 0.65, mats["yellow_plain"]), ("D", 1.02, mats["red"])):
        box(f"control_room_{label}_large_button", (x, 0.31, -0.58), (0.23, 0.060, 0.16), mat, col, 0.014, 2)
        add_text(f"control_room_{label}_button_letter", label, (x, 0.265, -0.58), 0.15, mats["edge_metal"], col)
    for i in range(4):
        curve_pipe(
            f"control_room_red_black_pipe_run_{i}",
            [(-2.35, 0.68 - i * 0.018, 1.62 - i * 0.10), (2.20, 0.68 - i * 0.018, 1.62 - i * 0.10)],
            0.030,
            mats["red"] if i == 1 else mats["rubber"],
            col,
        )
    for i, x in enumerate((-1.90, -0.95, 0.00, 0.95, 1.90)):
        box(f"control_room_pipe_clamp_{i}", (x, 0.63, 1.45), (0.10, 0.085, 0.48), mats["dark_metal"], col, 0.010, 1)
    scatter_scratches("control_room_terminal_panel", col, mats, (0, 0.74, 0.20), 3.3, 1.8, 0.70, 58, 611)


def build_engine_room(col, mats):
    industrial_floor("engine_room", col, mats, width=5.0, depth=3.9, z=-1.15)
    wall_panels("engine_room", col, mats, width=5.4, height=3.0, y=1.30, z=0.10)
    strip_light("engine_room_cold_strip_light", (-0.30, -0.18, 1.95), (0.70, 0.035, 0.055), col, mats)
    box("engine_room_background_turbine_housing_left", (-1.70, 0.42, -0.28), (1.35, 1.10, 1.08), mats["dark_metal"], col, 0.060, 3, (0, math.radians(6), 0))
    cyl("engine_room_round_hatch_socket", (-1.50, -0.22, -0.20), 0.58, 0.48, mats["dark_metal"], col, 64, (math.radians(90), 0, 0))
    torus("engine_room_circular_cross_hatch_ring", (-1.50, -0.47, -0.20), 0.44, 0.035, mats["edge_metal"], col, (math.radians(90), 0, 0))
    cyl("engine_room_hatch_center_hub", (-1.50, -0.53, -0.20), 0.115, 0.09, mats["edge_metal"], col, 40, (math.radians(90), 0, 0))
    box("engine_room_hatch_horizontal_crossbar", (-1.50, -0.56, -0.20), (0.74, 0.055, 0.070), mats["edge_metal"], col, 0.018, 2)
    box("engine_room_hatch_vertical_crossbar", (-1.50, -0.56, -0.20), (0.070, 0.055, 0.74), mats["edge_metal"], col, 0.018, 2)
    box("engine_power_terminal_main_cabinet", (1.18, 0.40, 0.10), (1.05, 0.34, 1.68), mats["dark_metal"], col, 0.060, 3)
    screen("engine_power_terminal_green_readout", (1.18, 0.20, 0.57), (0.72, 0.12, 0.48), mats, col)
    box("engine_power_terminal_red_black_warning_strip", (1.18, 0.145, 0.08), (0.78, 0.035, 0.13), mats["red"], col, 0.010, 1)
    for i in range(5):
        box(f"engine_warning_black_diagonal_{i}", (0.88 + i * 0.14, 0.115, 0.08), (0.08, 0.008, 0.145), mats["black"], col, 0.001, 1, (0, 0, math.radians(-24)))
    for i, x in enumerate((0.93, 1.43)):
        box(f"engine_breaker_socket_{i}", (x, 0.13, -0.50), (0.22, 0.060, 0.46), mats["black"], col, 0.016, 2)
        box(f"engine_red_toggle_switch_{i}", (x, 0.075, -0.40), (0.13, 0.045, 0.24), mats["red"], col, 0.014, 2, (math.radians(-8), 0, 0))
    curve_pipe("engine_top_bent_pipe", [(1.18, 0.33, 1.05), (1.18, 0.30, 1.45), (0.62, 0.35, 1.72), (-0.20, 0.50, 1.72)], 0.050, mats["dark_metal"], col)
    for i in range(4):
        curve_pipe(f"engine_lower_cable_bundle_{i}", [(0.95 + i * 0.15, 0.25, -0.78), (0.95 + i * 0.10, 0.14, -1.05), (0.70 + i * 0.18, -0.20, -1.10)], 0.027, mats["rubber"], col)
    scatter_scratches("engine_cabinet_scratches", col, mats, (1.18, 0.19, 0.10), 1.0, 1.65, 0.07, 52, 712)


def build_supply_room(col, mats):
    industrial_floor("supply_room", col, mats, width=5.1, depth=3.8, z=-1.15)
    wall_panels("supply_room", col, mats, width=5.5, height=3.0, y=1.28, z=0.12)
    strip_light("supply_room_overhead_cage_light", (0.35, -0.20, 1.95), (0.92, 0.035, 0.07), col, mats)
    box("supply_left_shelf_vertical", (-2.05, 0.35, -0.10), (0.08, 0.72, 1.80), mats["dark_metal"], col, 0.018, 2)
    box("supply_left_shelf_back", (-1.55, 0.75, -0.10), (0.96, 0.08, 1.70), mats["black"], col, 0.015, 1)
    for z in (-0.80, -0.25, 0.32, 0.89):
        box(f"supply_left_shelf_board_{z}", (-1.55, 0.36, z), (1.02, 0.54, 0.05), mats["dark_metal"], col, 0.015, 1)
    for i, (x, z) in enumerate([(-1.70, -0.55), (-1.38, -0.55), (-1.55, 0.02), (-1.70, 0.58), (-1.36, 0.58)]):
        box(f"supply_shelf_small_crate_{i}", (x, 0.07, z), (0.32, 0.42, 0.22), mats["cargo"] if i % 2 else mats["olive"], col, 0.025, 2)
    box("supply_six_door_cabinet_outer_frame", (0.65, 0.33, -0.02), (2.42, 0.42, 1.90), mats["dark_metal"], col, 0.070, 3)
    for row in range(2):
        for column in range(3):
            idx = row * 3 + column
            x = -0.13 + column * 0.78
            z = 0.43 - row * 0.82
            box(f"supply_olive_locker_door_{idx}", (x, 0.075, z), (0.67, 0.055, 0.68), mats["olive"], col, 0.035, 3)
            box(f"supply_locker_inner_inset_{idx}", (x + 0.08, 0.033, z + 0.03), (0.38, 0.018, 0.42), mats["olive"], col, 0.018, 1)
            broken_paint_pattern(f"supply_worn_yellow_door_pattern_{idx}", (x + 0.05, 0.018, z), 0.58, 0.30, mats["yellow_plain"], col, 980 + idx)
            ribbed_grip(f"supply_black_tubular_handle_{idx}", (x - 0.24, -0.015, z), 0.032, 0.38, col, mats, "Z")
            for hz in (-0.22, 0.22):
                box(f"supply_right_hinge_{idx}_{hz}", (x + 0.34, 0.014, z + hz), (0.055, 0.035, 0.15), mats["edge_metal"], col, 0.006, 1)
            corner_bolts(f"supply_locker_{idx}", (x, z, z), 0.60, 0.60, 0.0, mats["edge_metal"], col, 0.016)
            scatter_scratches(f"supply_locker_door_{idx}", col, mats, (x, 0.0, z), 0.60, 0.62, -0.012, 9, 900 + idx)
    box("supply_foreground_green_crate_hint", (-1.25, -1.02, -0.82), (1.10, 0.75, 0.34), mats["olive"], col, 0.035, 2)


def build_cargo_crate(prefix, loc, scale, col, mats, large=True):
    box(prefix + "_main_blue_gray_body", loc, scale, mats["cargo"], col, 0.065 if large else 0.045, 3)
    fy = front_y(loc[1], scale[1])
    for x in (-scale[0] * 0.38, scale[0] * 0.38):
        box(prefix + f"_vertical_black_strap_{x}", (loc[0] + x, fy - 0.015, loc[2]), (0.13, 0.030, scale[2] * 1.05), mats["rubber"], col, 0.012, 1)
    box(prefix + "_upper_lid_band", (loc[0], fy - 0.018, loc[2] + scale[2] * 0.35), (scale[0] * 1.04, 0.032, 0.10), mats["dark_metal"], col, 0.012, 1)
    box(prefix + "_central_latch_socket", (loc[0], fy - 0.038, loc[2] - scale[2] * 0.02), (0.22, 0.055, 0.22), mats["black"], col, 0.010, 1)
    box(prefix + "_red_lock_tag", (loc[0], fy - 0.060, loc[2] - scale[2] * 0.18), (0.14, 0.028, 0.20), mats["red"], col, 0.010, 1)
    box(prefix + "_flat_red_shipping_label", (loc[0] - scale[0] * 0.24, fy - 0.061, loc[2] + scale[2] * 0.27), (scale[0] * 0.22, 0.012, scale[2] * 0.080), mats["red"], col, 0.004, 1)
    box(prefix + "_flat_yellow_shipping_label", (loc[0] + scale[0] * 0.18, fy - 0.062, loc[2] - scale[2] * 0.30), (scale[0] * 0.18, 0.012, scale[2] * 0.065), mats["yellow_plain"], col, 0.004, 1)
    for sx in (-1, 1):
        for sz in (-1, 1):
            box(prefix + f"_reinforced_corner_{sx}_{sz}", (loc[0] + sx * scale[0] * 0.43, fy - 0.026, loc[2] + sz * scale[2] * 0.40), (0.18, 0.05, 0.16), mats["edge_metal"], col, 0.012, 1)
    scatter_scratches(prefix + "_crate_face", col, mats, (loc[0], fy, loc[2]), scale[0], scale[2], fy - 0.05, 34 if large else 20, 1200 + int(loc[0] * 31))


def build_cargo_hold(col, mats):
    industrial_floor("cargo_hold", col, mats, width=5.8, depth=4.3, z=-1.15)
    wall_panels("cargo_hold", col, mats, width=5.8, height=3.0, y=1.30, z=0.12)
    strip_light("cargo_hold_long_wall_light", (-1.75, -0.05, 1.38), (0.12, 0.035, 0.70), col, mats)
    build_cargo_crate("cargo_hold_large_strapped_crate", (-1.15, -0.05, -0.45), (1.55, 0.88, 1.02), col, mats, True)
    build_cargo_crate("cargo_hold_secondary_gray_crate", (0.74, -0.18, -0.55), (1.05, 0.76, 0.72), col, mats, False)
    box("cargo_hold_wall_status_panel_body", (-0.92, 0.80, 0.82), (1.86, 0.18, 0.72), mats["dark_metal"], col, 0.048, 3)
    screen("cargo_hold_wall_terminal_green_display", (-0.98, 0.68, 0.84), (0.86, 0.11, 0.34), mats, col)
    for i, x in enumerate((0.12, 0.30)):
        box(f"cargo_hold_side_round_indicator_{i}", (x, 0.56, 0.82 - i * 0.22), (0.09, 0.028, 0.09), mats["green_label"] if i == 0 else mats["red"], col, 0.012, 2)
    box("cargo_hold_pedestal_column", (1.77, -0.42, -0.60), (0.36, 0.42, 0.90), mats["dark_metal"], col, 0.035, 2)
    box("cargo_hold_pedestal_sloped_head", (1.77, -0.63, 0.05), (0.70, 0.50, 0.42), mats["dark_metal"], col, 0.045, 3, (math.radians(-12), 0, 0))
    screen("cargo_hold_pedestal_screen", (1.77, -0.92, 0.10), (0.48, 0.10, 0.26), mats, col)
    for i, mat in enumerate((mats["red"], mats["yellow_plain"], mats["yellow_plain"], mats["black"])):
        box(f"cargo_hold_pedestal_button_{i}", (1.51 + i * 0.16, -1.00, -0.16), (0.10, 0.035, 0.07), mat, col, 0.008, 1)
    for i in range(5):
        curve_pipe(f"cargo_hold_wall_pipe_bundle_{i}", [(-2.55, 0.62 - i * 0.020, 1.18 - i * 0.12), (2.15, 0.62 - i * 0.020, 1.18 - i * 0.12)], 0.026, mats["rubber"], col)


def build_armory(col, mats):
    industrial_floor("armory", col, mats, width=5.2, depth=4.2, z=-1.15)
    wall_panels("armory", col, mats, width=5.4, height=3.0, y=1.32, z=0.12)
    strip_light("armory_overhead_caged_light", (0.0, -0.25, 1.94), (0.86, 0.035, 0.065), col, mats)

    # Original design: center pillar, rear stairs, turret handles on top, and a curved forward screen.
    box("armory_pillar_square_floor_plinth", (0.0, 0.10, -1.00), (1.22, 1.05, 0.24), mats["dark_metal"], col, 0.050, 3)
    cyl("armory_central_round_turret_pillar", (0.0, 0.12, -0.45), 0.42, 1.20, mats["dark_metal"], col, 56)
    torus("armory_pillar_top_service_ring", (0.0, 0.12, 0.16), 0.42, 0.032, mats["edge_metal"], col)
    torus("armory_pillar_lower_service_ring", (0.0, 0.12, -0.90), 0.42, 0.028, mats["edge_metal"], col)

    for i in range(5):
        box(
            f"armory_rear_access_stair_{i}",
            (-0.86, 0.78 + i * 0.17, -0.98 + i * 0.15),
            (0.88 - i * 0.06, 0.20, 0.08),
            mats["dark_metal"],
            col,
            0.018,
            2,
        )
    curve_pipe("armory_rear_left_stair_handrail", [(-1.34, 0.72, -0.82), (-1.28, 1.15, -0.36), (-1.20, 1.50, 0.02)], 0.024, mats["edge_metal"], col)
    curve_pipe("armory_rear_right_stair_handrail", [(-0.40, 0.72, -0.82), (-0.44, 1.15, -0.36), (-0.48, 1.50, 0.02)], 0.024, mats["edge_metal"], col)

    box("armory_turret_top_control_console", (0.0, -0.18, 0.48), (0.78, 0.50, 0.24), mats["dark_metal"], col, 0.040, 3)
    cyl("armory_turret_pivot_axis", (0.0, -0.48, 0.55), 0.15, 0.64, mats["edge_metal"], col, 48, (math.radians(90), 0, 0))
    box("armory_left_grip_yoke", (-0.38, -0.48, 0.56), (0.14, 0.14, 0.20), mats["edge_metal"], col, 0.020, 2)
    box("armory_right_grip_yoke", (0.38, -0.48, 0.56), (0.14, 0.14, 0.20), mats["edge_metal"], col, 0.020, 2)
    ribbed_grip("armory_left_forward_turret_handle", (-0.38, -0.72, 0.42), 0.065, 0.48, col, mats, "Y")
    ribbed_grip("armory_right_forward_turret_handle", (0.38, -0.72, 0.42), 0.065, 0.48, col, mats, "Y")
    curve_pipe("armory_red_safety_grab_bar", [(-0.60, -0.54, 0.08), (-0.50, -0.73, -0.03), (0.50, -0.73, -0.03), (0.60, -0.54, 0.08)], 0.030, mats["red"], col)
    box("armory_turret_small_sight_block", (0.0, -0.66, 0.90), (0.54, 0.30, 0.28), mats["dark_metal"], col, 0.035, 3)
    box("armory_turret_sight_dark_slot", (0.0, -0.84, 0.90), (0.36, 0.045, 0.13), mats["black"], col, 0.014, 2)

    for i in range(9):
        angle = math.radians(-32 + i * 8)
        x = math.sin(angle) * 1.65
        y = -0.88 + math.cos(angle) * 0.22
        panel_w = 0.36 if i not in (0, 8) else 0.28
        box(f"armory_curved_front_screen_panel_{i}", (x, y, 0.20), (panel_w, 0.035, 0.78), mats["screen_dim"], col, 0.012, 1, (0.0, 0.0, -angle))
        box(f"armory_curved_front_screen_frame_{i}", (x, y + 0.020, 0.20), (panel_w + 0.06, 0.025, 0.86), mats["dark_metal"], col, 0.010, 1, (0.0, 0.0, -angle))
    box("armory_curved_screen_lower_black_mount", (0.0, -0.55, -0.32), (3.05, 0.16, 0.18), mats["dark_metal"], col, 0.030, 2)
    for i in range(6):
        box(f"armory_screen_green_target_mark_{i}", (-0.95 + i * 0.38, -0.91, 0.20 + (i % 2) * 0.08), (0.14, 0.008, 0.010), mats["green_label"], col, 0.0, 1)

    box("armory_left_corridor_hint", (-2.05, 0.05, -0.60), (0.28, 1.12, 1.15), mats["black"], col, 0.025, 2)
    box("armory_right_corridor_hint", (2.05, 0.05, -0.60), (0.28, 1.12, 1.15), mats["black"], col, 0.025, 2)


def build_hooked_staff(prefix, col, mats, loc=(0, 0, 0), scale=1.0, rot_z=0.0):
    # Long two-handed shaft plus a flattened forged crowbar-like upper hook.
    x0, y0, z0 = loc
    ca = math.cos(rot_z)
    sa = math.sin(rot_z)

    def p(local_x, local_z):
        return (
            x0 + (local_x * ca + local_z * sa) * scale,
            y0,
            z0 + (-local_x * sa + local_z * ca) * scale,
        )

    shaft = curve_pipe(
        prefix + "_long_dark_metal_shaft",
        [p(0.0, -2.10), p(0.0, 0.86)],
        0.032 * scale,
        mats["dark_metal"],
        col,
        5,
    )
    hook_nodes = [
        p(0.00, 0.72),
        p(0.02, 0.97),
        p(0.16, 1.18),
        p(0.39, 1.26),
        p(0.58, 1.13),
        p(0.60, 0.94),
    ]
    bezier_pipe(
        prefix + "_continuous_forged_crowbar_hook",
        [Vector(point) for point in hook_nodes],
        0.040 * scale,
        mats["edge_metal"],
        col,
    )
    tapered_bar_between(
        prefix + "_tapered_downturned_pry_claw",
        hook_nodes[5],
        p(0.49, 0.68),
        0.080 * scale,
        0.020 * scale,
        0.075 * scale,
        mats["edge_metal"],
        col,
        0.010 * scale,
    )
    for z in (-1.70, -0.62, 0.70):
        torus(prefix + f"_thin_metal_collar_{z}", p(0.0, z), 0.038 * scale, 0.006 * scale, mats["edge_metal"], col, (0, rot_z, 0), 36)
    for i in range(15):
        z = -1.42 + i * 0.040
        torus(prefix + f"_lower_leather_wrap_{i:02d}", p(0.0, z), 0.038 * scale, 0.005 * scale, mats["rubber"], col, (0, rot_z, 0), 30)
    for i in range(10):
        z = -0.38 + i * 0.040
        torus(prefix + f"_upper_leather_wrap_{i:02d}", p(0.0, z), 0.037 * scale, 0.005 * scale, mats["rubber"], col, (0, rot_z, 0), 30)
    cyl(prefix + "_heavy_bottom_cap", p(0.0, -2.18), 0.043 * scale, 0.10 * scale, mats["edge_metal"], col, 36, (0, rot_z, 0))


def glove_cluster(prefix, loc, col, mats, scale=1.0):
    x, y, z = loc
    box(prefix + "_palm_black_glove", (x, y, z), (0.27 * scale, 0.20 * scale, 0.18 * scale), mats["leather"], col, 0.035 * scale, 2)
    for i in range(4):
        box(prefix + f"_curled_finger_{i}", (x + (-0.10 + i * 0.065) * scale, y - 0.07 * scale, z + 0.11 * scale), (0.045 * scale, 0.09 * scale, 0.14 * scale), mats["leather"], col, 0.018 * scale, 2)
    box(prefix + "_wrist_cuff_armored", (x - 0.04 * scale, y + 0.10 * scale, z - 0.19 * scale), (0.34 * scale, 0.18 * scale, 0.16 * scale), mats["dark_metal"], col, 0.020 * scale, 2)
    screen(prefix + "_small_wrist_crt", (x - 0.04 * scale, y - 0.01 * scale, z - 0.29 * scale), (0.25 * scale, 0.055 * scale, 0.16 * scale), mats, col)


def build_first_person(col, mats):
    industrial_floor("first_person_corridor", col, mats, width=4.8, depth=6.2, z=-1.15)
    wall_panels("first_person_corridor", col, mats, width=4.8, height=3.0, y=1.90, z=0.12)
    for i, y in enumerate((0.20, 1.00, 1.78)):
        strip_light(f"first_person_corridor_overhead_light_{i}", (0.0, y, 1.95), (0.78, 0.035, 0.06), col, mats)
    for i in range(4):
        build_cargo_crate(f"first_person_corridor_side_crate_{i}", (-1.65 if i % 2 == 0 else 1.55, -0.15 + i * 0.55, -0.78), (0.72, 0.58, 0.55), col, mats, False)
    # Camera-facing foreground equipment. The staff is deliberately long and thin, with the hook reading high in the frame.
    build_hooked_staff("first_person_two_handed_hooked_stick", col, mats, loc=(0.96, -1.34, -0.20), scale=1.10, rot_z=math.radians(22))
    glove_cluster("first_person_lower_hand", (0.68, -1.49, -0.86), col, mats, 1.12)
    glove_cluster("first_person_upper_hand", (0.96, -1.46, -0.28), col, mats, 0.95)
    curve_pipe("first_person_left_sleeve", [(-0.38, -1.55, -1.30), (0.28, -1.51, -1.02), (0.68, -1.49, -0.86)], 0.110, mats["leather"], col)
    curve_pipe("first_person_right_sleeve", [(1.58, -1.55, -0.62), (1.22, -1.49, -0.42), (0.96, -1.46, -0.28)], 0.092, mats["leather"], col)
    cyl("first_person_musket_barrel_background_reference", (-1.26, 0.28, -0.42), 0.016, 0.96, mats["edge_metal"], col, 32, (0, math.radians(90), 0))
    box("first_person_musket_wood_stock_background_reference", (-1.64, 0.28, -0.42), (0.40, 0.065, 0.075), mats["rust"], col, 0.018, 2)


def set_collection_visibility(collections, active):
    for col in collections:
        visible = col == active
        col.hide_viewport = not visible
        col.hide_render = not visible
        for obj in col.objects:
            obj.hide_viewport = not visible
            obj.hide_render = not visible


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_render_camera(name, loc, target, lens=36.0):
    bpy.ops.object.camera_add(location=loc)
    cam = bpy.context.object
    cam.name = name
    cam.data.lens = lens
    cam.data.sensor_width = 32
    look_at(cam, target)
    bpy.context.scene.camera = cam
    return cam


def add_render_lights(prefix):
    for obj in list(bpy.context.scene.objects):
        if obj.type in ("LIGHT", "CAMERA") and obj.name.startswith("Render_"):
            bpy.data.objects.remove(obj, do_unlink=True)
    bpy.ops.object.light_add(type="AREA", location=(-2.2, -3.4, 3.2))
    key = bpy.context.object
    key.name = "Render_" + prefix + "_soft_key"
    key.data.energy = 320
    key.data.size = 4.5
    bpy.ops.object.light_add(type="POINT", location=(2.4, -1.2, 1.2))
    rim = bpy.context.object
    rim.name = "Render_" + prefix + "_warm_rim"
    rim.data.energy = 95
    rim.data.color = (1.0, 0.64, 0.38)
    rim.data.shadow_soft_size = 5.0
    bpy.ops.object.light_add(type="POINT", location=(-1.8, -0.8, 0.7))
    green = bpy.context.object
    green.name = "Render_" + prefix + "_crt_green_fill"
    green.data.energy = 28
    green.data.color = (0.22, 0.88, 0.42)
    green.data.shadow_soft_size = 3.2


def render_collection(item_id, slug, col, collections):
    camera_settings = {
        "01": ((2.50, -4.55, 1.05), (0.0, -0.15, 0.10), 31.0),
        "02": ((2.45, -4.20, 0.95), (0.05, 0.52, 0.25), 34.0),
        "03": ((2.35, -4.30, 0.85), (0.18, 0.26, -0.05), 34.0),
        "04": ((2.45, -4.05, 0.82), (0.22, 0.20, -0.05), 33.0),
        "05": ((2.50, -4.60, 0.78), (0.08, 0.05, -0.32), 32.0),
        "06": ((2.00, -4.10, 0.72), (0.12, 0.03, 0.15), 33.0),
        "07": ((0.52, -4.75, 0.04), (0.38, -0.88, -0.22), 22.0),
    }
    set_collection_visibility(collections, col)
    add_render_lights(item_id)
    loc, target, lens = camera_settings[item_id]
    add_render_camera("Render_" + item_id + "_camera", loc, target, lens)
    filepath = os.path.join(RENDER_DIR, f"{item_id}_{slug}_blender_sample.png")
    bpy.context.scene.render.filepath = filepath
    bpy.ops.render.render(write_still=True)
    return filepath


def export_collection(item_id, slug, col):
    bpy.ops.object.select_all(action="DESELECT")
    selected = []
    for obj in col.objects:
        if obj.type in {"MESH", "CURVE", "FONT", "LIGHT"}:
            obj.select_set(True)
            selected.append(obj)
    if selected:
        bpy.context.view_layer.objects.active = selected[0]
    fbx_path = os.path.join(EXPORT_DIR, f"FBX_{item_id}_{slug}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH", "EMPTY", "LIGHT"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        axis_forward="-Z",
        axis_up="Y",
    )
    glb_path = os.path.join(EXPORT_DIR, f"GLB_{item_id}_{slug}.glb")
    try:
        bpy.ops.export_scene.gltf(filepath=glb_path, use_selection=True, export_format="GLB")
    except Exception as exc:
        print("GLB export skipped for " + slug + ": " + str(exc))


def write_html_and_readme(render_paths):
    rows = []
    for item_id, slug, korean_name in SAMPLE_ITEMS:
        render_name = os.path.basename(render_paths[item_id])
        if item_id == "06":
            rows.append(
                f"""
                <section class="sample">
                  <h2>{item_id}. {korean_name}</h2>
                  <div class="compare source-rebuild">
                    <div class="source-card">
                      <h3>원본 기획서 기준 재작성</h3>
                      <p>무기실은 중앙 기둥, 뒤편 계단, 기둥 위 포탑 핸들, 기둥 앞 가로 커브형 대형 스크린 구조입니다.</p>
                      <p>기존 06번 artSample의 벽면 장착형 포탑 구성은 폐기하고 이 구조를 새 승인 샘플 기준으로 사용합니다.</p>
                    </div>
                    <figure>
                      <img src="renders/{render_name}" alt="{korean_name} Blender 재제작 샘플">
                      <figcaption>Blender 모델링/텍스처링 승인 샘플</figcaption>
                    </figure>
                  </div>
                </section>
                """
            )
            continue
        reference = f"../stage3_rework_review/{item_id}_{slug}_review.png"
        rows.append(
            f"""
            <section class="sample">
              <h2>{item_id}. {korean_name}</h2>
              <div class="compare">
                <figure>
                  <img src="{reference}" alt="{korean_name} 원본 artSample">
                  <figcaption>승인 기준 artSample</figcaption>
                </figure>
                <figure>
                  <img src="renders/{render_name}" alt="{korean_name} Blender 재제작 샘플">
                  <figcaption>Blender 모델링/텍스처링 승인 샘플</figcaption>
                </figure>
              </div>
            </section>
            """
        )

    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Stage 3 Blender Rebuild Approval Sample</title>
  <style>
    :root {{
      color-scheme: dark;
      --bg: #0b0d0c;
      --panel: #151817;
      --line: #3a403b;
      --text: #e7e0d2;
      --muted: #9f9a8d;
      --accent: #6bd48b;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font-family: Arial, "Noto Sans KR", sans-serif;
      line-height: 1.55;
    }}
    header {{
      padding: 28px 34px 18px;
      border-bottom: 1px solid var(--line);
      background: #101312;
    }}
    h1 {{
      margin: 0 0 10px;
      font-size: 25px;
      letter-spacing: 0;
    }}
    p {{
      margin: 6px 0;
      color: var(--muted);
      max-width: 1080px;
    }}
    main {{
      padding: 22px 34px 42px;
      display: grid;
      gap: 24px;
    }}
    .sample {{
      border: 1px solid var(--line);
      background: var(--panel);
      padding: 16px;
    }}
    h2 {{
      margin: 0 0 14px;
      font-size: 18px;
      letter-spacing: 0;
    }}
    .compare {{
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 14px;
      align-items: start;
    }}
    .source-card {{
      min-height: 260px;
      border: 1px solid #303630;
      background: #0c0f0d;
      padding: 18px;
      color: var(--muted);
    }}
    .source-card h3 {{
      margin: 0 0 12px;
      color: var(--text);
      font-size: 16px;
    }}
    figure {{
      margin: 0;
      border: 1px solid #303630;
      background: #070807;
      padding: 8px;
    }}
    img {{
      display: block;
      width: 100%;
      height: auto;
    }}
    figcaption {{
      color: var(--muted);
      font-size: 13px;
      padding-top: 7px;
    }}
    .note {{
      color: var(--accent);
      font-weight: 700;
    }}
    @media (max-width: 900px) {{
      .compare {{ grid-template-columns: 1fr; }}
      header, main {{ padding-left: 16px; padding-right: 16px; }}
    }}
  </style>
</head>
<body>
  <header>
    <h1>Stage 3 Blender Rebuild Approval Sample</h1>
    <p class="note">이 디렉터리는 Unity 적용 전 승인용 샘플입니다. 아직 실제 씬, 프리팹, 런타임 자산에 연결하지 않았습니다.</p>
    <p>목표는 기존 `artSample/stage3_rework_review`의 7개 이미지를 분위기 참고가 아니라 Unity 구현 목표 원본으로 삼아, Blender 모델링과 절차적 텍스처링으로 다시 만든 승인용 1차 샘플을 확인하는 것입니다.</p>
    <p>Blender 원본은 `blender/Stage3_Blender_Rebuild_ApprovalSample.blend`, 내보내기 파일은 `exports/`, 절차적 텍스처는 `textures/`, 렌더는 `renders/`에 있습니다.</p>
  </header>
  <main>
    {''.join(rows)}
  </main>
</body>
</html>
"""
    with open(os.path.join(SAMPLE_ROOT, "index.html"), "w", encoding="utf-8") as handle:
        handle.write(html)

    readme = """# Stage 3 Blender Rebuild Approval Sample

이 폴더는 Stage 3 아트 재제작을 Unity에 넣기 전 검수하기 위한 Blender 승인 샘플입니다.

- 승인 기준 원본: `artSample/stage3_rework_review/`
- Blender 원본: `blender/Stage3_Blender_Rebuild_ApprovalSample.blend`
- FBX/GLB 샘플: `exports/`
- 절차적 텍스처 PNG: `textures/`
- Blender 렌더 PNG: `renders/`
- 비교 미리보기: `index.html`

표면의 선형 스크래치 지오메트리는 사용하지 않습니다. 금속, 벽, 바닥, 고무, 장갑 재질은 절차적 알베도와 Blender 노이즈 범프/거칠기 변화로 거친 표면을 표현합니다.

06번 무기실은 기존 06번 이미지가 아니라 원본 기획서의 중앙 기둥, 뒤편 계단, 기둥 위 포탑 핸들, 전면 가로 곡면 스크린 구조를 기준으로 새로 그린 샘플입니다.

아직 Unity 씬, 프리팹, 런타임 자산에는 연결하지 않았습니다. 승인 후에만 이 샘플을 기준으로 Unity에 적용하고, 적용 후에는 승인된 샘플과 Unity 캡처를 반복 대조합니다.
"""
    with open(os.path.join(SAMPLE_ROOT, "README.md"), "w", encoding="utf-8") as handle:
        handle.write(readme)


def main():
    ensure_dirs()
    clear_scene()
    configure_scene()
    mats = create_materials()

    builders = {
        "01": build_cockpit,
        "02": build_control_room,
        "03": build_engine_room,
        "04": build_supply_room,
        "05": build_cargo_hold,
        "06": build_armory,
        "07": build_first_person,
    }

    collections = []
    collection_by_id = {}
    for item_id, slug, _ in SAMPLE_ITEMS:
        col = new_collection(f"Stage3_Approval_{item_id}_{slug}")
        builders[item_id](col, mats)
        collections.append(col)
        collection_by_id[item_id] = col

    render_paths = {}
    for item_id, slug, _ in SAMPLE_ITEMS:
        render_paths[item_id] = render_collection(item_id, slug, collection_by_id[item_id], collections)
        export_collection(item_id, slug, collection_by_id[item_id])

    set_collection_visibility(collections, collections[0])
    blend_path = os.path.join(BLENDER_DIR, "Stage3_Blender_Rebuild_ApprovalSample.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    write_html_and_readme(render_paths)
    print("Stage 3 Blender approval sample generated at " + SAMPLE_ROOT)


if __name__ == "__main__":
    main()
