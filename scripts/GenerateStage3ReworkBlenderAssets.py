import math
import os
import random
import sys

import bpy


TEXTURE_SIZE = 512


def parse_project_root():
    args = sys.argv
    if "--" in args:
        extra = args[args.index("--") + 1 :]
    else:
        extra = []

    for index, value in enumerate(extra):
        if value == "--project-root" and index + 1 < len(extra):
            return os.path.abspath(extra[index + 1])

    return os.getcwd()


PROJECT_ROOT = parse_project_root()
ASSET_ROOT = os.path.join(PROJECT_ROOT, "Assets", "_Project", "Art", "Props", "Stage3Rework")
FBX_DIR = os.path.join(ASSET_ROOT, "Fbx")
TEXTURE_DIR = os.path.join(ASSET_ROOT, "Textures")
BLEND_DIR = os.path.join(ASSET_ROOT, "BlenderSource")


def ensure_dirs():
    for path in (ASSET_ROOT, FBX_DIR, TEXTURE_DIR, BLEND_DIR):
        os.makedirs(path, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for datablock in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.images,
        bpy.data.collections,
    ):
        for item in list(datablock):
            datablock.remove(item)


def collection(name):
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def link_to_collection(obj, col):
    for existing in obj.users_collection:
        existing.objects.unlink(obj)
    col.objects.link(obj)


def make_texture(name, style, base, accent, seed):
    random.seed(seed)
    image = bpy.data.images.new(name, TEXTURE_SIZE, TEXTURE_SIZE, alpha=True)
    pixels = []
    for y in range(TEXTURE_SIZE):
        for x in range(TEXTURE_SIZE):
            noise = random.Random((x * 73856093) ^ (y * 19349663) ^ seed).random()
            scratch = ((x + seed) % 71 == 0 and y % 9 < 5) or ((y + seed) % 89 == 0 and x % 11 < 8)
            diagonal = ((x + y + seed) % 37) < 5
            scanline = 0.12 if y % 9 < 2 else 0.0
            color = [
                base[0] * (0.72 + noise * 0.42),
                base[1] * (0.72 + noise * 0.42),
                base[2] * (0.72 + noise * 0.42),
                1.0,
            ]

            if style == "metal":
                if scratch:
                    color[0] = color[0] * 0.45 + accent[0] * 0.55
                    color[1] = color[1] * 0.45 + accent[1] * 0.55
                    color[2] = color[2] * 0.45 + accent[2] * 0.55
                if diagonal:
                    color[0] *= 0.72
                    color[1] *= 0.68
                    color[2] *= 0.62
            elif style == "screen":
                grid = x % 32 == 0 or y % 32 == 0
                glow = 0.35 + noise * 0.35 + scanline
                color = [
                    base[0] * (1.0 - glow) + accent[0] * glow,
                    base[1] * (1.0 - glow) + accent[1] * glow,
                    base[2] * (1.0 - glow) + accent[2] * glow,
                    1.0,
                ]
                if grid:
                    color[1] = min(1.0, color[1] + 0.18)
            elif style == "rubber":
                rib = 0.22 if (x + y) % 19 < 3 else 0.0
                color[0] = base[0] + rib * accent[0]
                color[1] = base[1] + rib * accent[1]
                color[2] = base[2] + rib * accent[2]
            elif style == "hazard":
                stripe = ((x + y * 2 + seed) // 34) % 2 == 0
                chosen = base if stripe else accent
                chip = noise < 0.12 or scratch
                color = [
                    chosen[0] * (0.55 if chip else 1.0),
                    chosen[1] * (0.55 if chip else 1.0),
                    chosen[2] * (0.55 if chip else 1.0),
                    1.0,
                ]
            elif style == "wood":
                grain = math.sin((x * 0.09) + (noise * 3.0)) * 0.12
                color[0] = base[0] + grain
                color[1] = base[1] + grain * 0.65
                color[2] = base[2] + grain * 0.35

            pixels.extend([max(0.0, min(1.0, c)) for c in color])

    image.pixels = pixels
    image.filepath_raw = os.path.join(TEXTURE_DIR, name + ".png")
    image.file_format = "PNG"
    image.save()
    return image


def make_material(name, image, color, metallic=0.0, roughness=0.55, emission=False):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        tex = nodes.new(type="ShaderNodeTexImage")
        tex.image = image
        mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        if emission and "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = color
            bsdf.inputs["Emission Strength"].default_value = 1.15
    return mat


def create_materials():
    textures = {
        "metal": make_texture("HD_Stage3_DarkWornMetal_Albedo", "metal", (0.20, 0.20, 0.18), (0.82, 0.78, 0.66), 1301),
        "bright_metal": make_texture("HD_Stage3_BrightScratchedMetal_Albedo", "metal", (0.45, 0.47, 0.43), (0.95, 0.92, 0.82), 1403),
        "rubber": make_texture("HD_Stage3_BlackRibbedRubber_Albedo", "rubber", (0.018, 0.019, 0.018), (0.08, 0.08, 0.075), 1511),
        "screen": make_texture("HD_Stage3_GreenCrtScreen_Albedo", "screen", (0.01, 0.09, 0.055), (0.30, 0.72, 0.46), 1601),
        "hazard": make_texture("HD_Stage3_RedBlackHazard_Albedo", "hazard", (0.75, 0.09, 0.045), (0.05, 0.045, 0.035), 1709),
        "yellow": make_texture("HD_Stage3_WornYellowPaint_Albedo", "hazard", (0.86, 0.58, 0.08), (0.12, 0.10, 0.06), 1801),
        "red": make_texture("HD_Stage3_WornRedPaint_Albedo", "hazard", (0.58, 0.06, 0.035), (0.09, 0.06, 0.045), 1901),
        "cargo": make_texture("HD_Stage3_BlueGrayCargoMetal_Albedo", "metal", (0.23, 0.28, 0.29), (0.74, 0.68, 0.54), 2003),
        "olive": make_texture("HD_Stage3_OliveCabinetPaint_Albedo", "metal", (0.23, 0.25, 0.18), (0.72, 0.66, 0.46), 2101),
        "wood": make_texture("HD_Stage3_WornWeaponWood_Albedo", "wood", (0.30, 0.16, 0.08), (0.60, 0.36, 0.18), 2207),
    }

    return {
        "metal": make_material("ST3_DarkWornMetal", textures["metal"], (0.25, 0.25, 0.23, 1), 0.65, 0.42),
        "bright_metal": make_material("ST3_BrightScratchedMetal", textures["bright_metal"], (0.62, 0.64, 0.58, 1), 0.8, 0.34),
        "rubber": make_material("ST3_BlackRibbedRubber", textures["rubber"], (0.035, 0.036, 0.033, 1), 0.0, 0.72),
        "screen": make_material("ST3_GreenCrtScreen", textures["screen"], (0.05, 0.42, 0.24, 1), 0.0, 0.25, True),
        "hazard": make_material("ST3_RedBlackHazard", textures["hazard"], (0.56, 0.08, 0.045, 1), 0.1, 0.58),
        "yellow": make_material("ST3_WornYellowPaint", textures["yellow"], (0.78, 0.56, 0.10, 1), 0.15, 0.58),
        "red": make_material("ST3_WornRedPaint", textures["red"], (0.58, 0.06, 0.04, 1), 0.15, 0.62),
        "cargo": make_material("ST3_BlueGrayCargoMetal", textures["cargo"], (0.30, 0.34, 0.34, 1), 0.55, 0.48),
        "olive": make_material("ST3_OliveCabinetPaint", textures["olive"], (0.28, 0.30, 0.22, 1), 0.45, 0.54),
        "wood": make_material("ST3_WornWeaponWood", textures["wood"], (0.30, 0.18, 0.10, 1), 0.0, 0.56),
    }


def set_origin_mesh_name(obj, mesh_name):
    obj.data.name = mesh_name
    return obj


def beveled_box(name, loc, scale, mat, col, bevel=0.035, segments=2, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        mod = obj.modifiers.new("wide chamfered industrial edges", "BEVEL")
        mod.width = bevel
        mod.segments = segments
        mod.affect = "EDGES"
        obj.modifiers.new("weighted scratched metal normals", "WEIGHTED_NORMAL")
    link_to_collection(obj, col)
    return obj


def cylinder(name, loc, radius, depth, mat, col, vertices=32, rot=(0, 0, 0), bevel=True):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new("soft worn cylinder rim", "BEVEL")
        mod.width = radius * 0.10
        mod.segments = 2
        obj.modifiers.new("weighted cylinder normals", "WEIGHTED_NORMAL")
    link_to_collection(obj, col)
    return obj


def torus(name, loc, major, minor, mat, col, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=72, minor_segments=12, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    obj.modifiers.new("weighted ring normals", "WEIGHTED_NORMAL")
    link_to_collection(obj, col)
    return obj


def bolt(name, loc, mat, col, radius=0.045, depth=0.025, rot=(0, 0, 0)):
    head = cylinder(name, loc, radius, depth, mat, col, 24, rot)
    slot = beveled_box(name + "_SlotCutLook", (loc[0], loc[1], loc[2] + depth * 0.55), (radius * 1.3, radius * 0.12, depth * 0.2), mat, col, 0.004, 1, rot)
    return head, slot


def four_corner_bolts(prefix, center, width, height, z, mat, col, radius=0.035):
    for index, (sx, sy) in enumerate(((-1, -1), (1, -1), (-1, 1), (1, 1)), start=1):
        bolt(prefix + " Bolt " + str(index), (center[0] + sx * width * 0.45, center[1] + sy * height * 0.45, z), mat, col, radius)


def add_screen(prefix, center, size, mats, col, horizontal=True):
    frame = beveled_box(prefix + " Frame", center, size, mats["metal"], col, 0.045, 3)
    inset = beveled_box(prefix + " Inset Black Gasket", (center[0], center[1], center[2] - 0.035), (size[0] * 0.86, size[1] * 0.74, size[2] * 0.45), mats["rubber"], col, 0.028, 2)
    screen = beveled_box(prefix + " Green CRT Glass", (center[0], center[1], center[2] - 0.07), (size[0] * 0.76, size[1] * 0.58, size[2] * 0.30), mats["screen"], col, 0.018, 2)
    four_corner_bolts(prefix, center, size[0], size[1], center[2] - 0.095, mats["bright_metal"], col, 0.032)
    if horizontal:
        beveled_box(prefix + " Bottom Status Strip", (center[0], center[1] - size[1] * 0.44, center[2] - 0.1), (size[0] * 0.36, size[1] * 0.055, size[2] * 0.35), mats["screen"], col, 0.006, 1)
    return frame, inset, screen


def create_hook_curve(name, mats, col):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 16
    curve.bevel_depth = 0.031
    curve.bevel_resolution = 5
    poly = curve.splines.new("BEZIER")
    poly.bezier_points.add(6)
    points = [
        (0.0, -1.42, 0.0),
        (0.0, -0.52, 0.0),
        (0.0, 0.48, 0.0),
        (0.08, 0.86, 0.0),
        (0.27, 1.02, 0.0),
        (0.43, 0.88, 0.0),
        (0.34, 0.58, 0.0),
    ]
    for p, co in zip(poly.bezier_points, points):
        p.co = co
        p.handle_left_type = "AUTO"
        p.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    obj.data.materials.append(mats["bright_metal"])
    col.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.object
    obj.name = name
    obj.data.name = "BM_HookedCrowbar_Body"
    obj.modifiers.new("weighted hook normals", "WEIGHTED_NORMAL")
    cylinder(name + " lower capped butt", (0.0, -1.48, 0.0), 0.04, 0.08, mats["bright_metal"], col, 32)
    cylinder(name + " upper collar ring", (0.02, 0.56, 0.0), 0.045, 0.035, mats["bright_metal"], col, 32)
    cylinder(name + " hook neck collar ring", (0.09, 0.79, 0.0), 0.043, 0.035, mats["bright_metal"], col, 32)
    return obj


def create_mesh_library(mats):
    col = collection("FBX_Stage3Rework_MeshLibrary")
    set_origin_mesh_name(beveled_box("BM_BeveledBox_Unit", (0, 0, 0), (1, 1, 1), mats["metal"], col, 0.045, 3), "BM_BeveledBox_Unit")
    set_origin_mesh_name(beveled_box("BM_PanelPlate_Unit", (1.6, 0, 0), (1, 1, 0.12), mats["metal"], col, 0.035, 3), "BM_PanelPlate_Unit")
    set_origin_mesh_name(beveled_box("BM_ScreenGlass_Unit", (3.2, 0, 0), (1, 1, 0.035), mats["screen"], col, 0.02, 2), "BM_ScreenGlass_Unit")
    set_origin_mesh_name(cylinder("BM_Cylinder_Unit", (4.8, 0, 0), 0.5, 1, mats["metal"], col, 40), "BM_Cylinder_Unit")
    set_origin_mesh_name(cylinder("BM_Bolt_Round", (6.2, 0, 0), 0.09, 0.06, mats["bright_metal"], col, 28), "BM_Bolt_Round")
    set_origin_mesh_name(cylinder("BM_CablePipe_Unit", (7.4, 0, 0), 0.07, 1, mats["rubber"], col, 32, (0, math.radians(90), 0)), "BM_CablePipe_Unit")
    set_origin_mesh_name(cylinder("BM_RibbedGrip_Unit", (8.8, 0, 0), 0.12, 1, mats["rubber"], col, 40), "BM_RibbedGrip_Unit")
    set_origin_mesh_name(torus("BM_CockpitWheelRing", (10.4, 0, 0), 0.48, 0.045, mats["metal"], col, (math.radians(90), 0, 0)), "BM_CockpitWheelRing")
    hook = create_hook_curve("BM_HookedCrowbar_Body", mats, col)
    hook.location.x = 12.0
    return col


def create_cockpit_preview(mats):
    col = collection("FBX_01_CockpitHelmAndStatus")
    add_screen("Cockpit Main Helm Screen", (0, 1.0, 0), (1.8, 0.82, 0.14), mats, col)
    beveled_box("Cockpit Rugged Helm Console", (0, 0.45, 0.18), (2.35, 0.55, 0.8), mats["metal"], col, 0.08, 3)
    torus("Cockpit segmented steering ring", (0, -0.3, -0.55), 0.62, 0.045, mats["metal"], col, (math.radians(90), 0, 0))
    cylinder("Cockpit left rubber grip", (-0.88, -0.34, -0.55), 0.09, 0.55, mats["rubber"], col, 32, (0, math.radians(90), 0))
    cylinder("Cockpit right rubber grip", (0.88, -0.34, -0.55), 0.09, 0.55, mats["rubber"], col, 32, (0, math.radians(90), 0))
    for i in range(4):
        add_screen("Cockpit Status Screen " + str(i + 1), (-1.7 + i * 1.1, -1.45, 0), (0.86, 0.52, 0.12), mats, col)
    return col


def create_cctv_preview(mats):
    col = collection("FBX_02_ControlRoomCCTV")
    beveled_box("CCTV wall plate with worn metal panels", (0, 0.55, 0.1), (3.7, 2.15, 0.18), mats["metal"], col, 0.055, 3)
    add_screen("CCTV Single Large Screen", (-0.25, 0.55, -0.02), (2.1, 1.1, 0.14), mats, col)
    add_screen("CCTV upper-left map helper", (-1.15, 1.48, -0.05), (0.9, 0.38, 0.12), mats, col)
    add_screen("CCTV right vertical status", (1.45, 0.58, -0.04), (0.55, 1.34, 0.12), mats, col, False)
    beveled_box("CCTV sloped button console", (0, -0.58, -0.05), (3.25, 0.38, 0.48), mats["metal"], col, 0.05, 3, (math.radians(-9), 0, 0))
    for i, mat in enumerate((mats["rubber"], mats["rubber"], mats["yellow"], mats["red"])):
        beveled_box("CCTV tactile function button " + str(i + 1), (-0.82 + i * 0.28, -0.58, -0.36), (0.18, 0.08, 0.14), mat, col, 0.012, 2)
    beveled_box("CCTV A Button", (0.62, -0.58, -0.36), (0.24, 0.11, 0.16), mats["yellow"], col, 0.014, 2)
    beveled_box("CCTV D Button", (0.96, -0.58, -0.36), (0.24, 0.11, 0.16), mats["red"], col, 0.014, 2)
    for i in range(4):
        cylinder("CCTV wall cable rail " + str(i + 1), (0.1, 1.85 - i * 0.13, -0.1), 0.035, 3.4, mats["rubber"], col, 24, (0, math.radians(90), 0))
    return col


def create_engine_preview(mats):
    col = collection("FBX_03_EnginePowerTerminal")
    beveled_box("Engine wall power cabinet", (0, 0.2, 0), (1.0, 1.65, 0.34), mats["metal"], col, 0.06, 3)
    add_screen("Engine power readout", (0, 0.65, -0.2), (0.68, 0.48, 0.08), mats, col)
    beveled_box("Engine red black warning strip", (0, 0.12, -0.24), (0.78, 0.12, 0.05), mats["hazard"], col, 0.012, 1)
    for x in (-0.24, 0.24):
        beveled_box("Engine breaker switch", (x, -0.45, -0.25), (0.22, 0.42, 0.08), mats["red"], col, 0.018, 2)
    cylinder("Engine top pipe elbow vertical", (0, 1.18, 0), 0.09, 0.7, mats["metal"], col, 32)
    cylinder("Engine lower cable bundle", (0, -0.95, 0), 0.07, 1.0, mats["rubber"], col, 32, (0, math.radians(90), 0))
    return col


def create_supply_preview(mats):
    col = collection("FBX_04_SupplyStorageCabinet")
    beveled_box("Supply six door cabinet frame", (0, 0, 0), (2.65, 1.85, 0.28), mats["metal"], col, 0.06, 3)
    for row in range(2):
        for column in range(3):
            x = -0.86 + column * 0.86
            y = 0.45 - row * 0.9
            beveled_box("Supply locker door " + str(row * 3 + column + 1), (x, y, -0.18), (0.74, 0.72, 0.08), mats["olive"], col, 0.035, 2)
            cylinder("Supply locker pull handle " + str(row * 3 + column + 1), (x - 0.22, y, -0.25), 0.045, 0.38, mats["rubber"], col, 24)
            beveled_box("Supply worn yellow door band " + str(row * 3 + column + 1), (x, y - 0.04, -0.295), (0.55, 0.08, 0.035), mats["yellow"], col, 0.006, 1)
    return col


def create_cargo_preview(mats):
    col = collection("FBX_05_CargoHoldPropsAndTerminal")
    beveled_box("Contract cargo heavy crate", (-0.8, 0.0, 0), (2.2, 1.05, 1.25), mats["cargo"], col, 0.07, 3)
    beveled_box("Contract cargo horizontal black strap", (-0.8, 0.0, -0.68), (2.35, 0.16, 0.08), mats["rubber"], col, 0.012, 1)
    beveled_box("Contract cargo vertical black strap", (-0.8, 0.0, -0.7), (0.18, 1.18, 0.08), mats["rubber"], col, 0.012, 1)
    beveled_box("Personal cargo smaller crate", (1.3, -0.22, -0.05), (1.15, 0.72, 0.86), mats["cargo"], col, 0.055, 3)
    beveled_box("Cargo wall status terminal", (-0.8, 1.05, -0.22), (1.75, 0.72, 0.16), mats["metal"], col, 0.045, 3)
    add_screen("Cargo terminal green display", (-0.95, 1.08, -0.34), (0.92, 0.36, 0.08), mats, col)
    beveled_box("Cargo pedestal diegetic terminal", (1.7, 0.42, -0.2), (0.58, 1.2, 0.42), mats["metal"], col, 0.04, 2)
    add_screen("Cargo pedestal sloped screen", (1.7, 1.05, -0.44), (0.78, 0.42, 0.08), mats, col)
    return col


def create_armory_preview(mats):
    col = collection("FBX_06_ArmoryTurretGripMount")
    beveled_box("Armory turret rail", (0, 0.32, 0), (2.5, 0.18, 0.18), mats["metal"], col, 0.035, 3)
    beveled_box("Armory center pivot block", (0, 0, -0.05), (0.52, 0.46, 0.38), mats["metal"], col, 0.04, 3)
    cylinder("Armory left grip", (-0.62, -0.42, -0.18), 0.11, 0.62, mats["rubber"], col, 40, (math.radians(10), 0, 0))
    cylinder("Armory right grip", (0.62, -0.42, -0.18), 0.11, 0.62, mats["rubber"], col, 40, (math.radians(-10), 0, 0))
    beveled_box("Armory sight hood", (0, 0.62, -0.22), (0.76, 0.42, 0.48), mats["metal"], col, 0.055, 3)
    cylinder("Armory red trigger handle", (0, -0.72, -0.32), 0.045, 0.9, mats["red"], col, 24, (0, math.radians(90), 0))
    return col


def create_first_person_preview(mats):
    col = collection("FBX_07_FirstPersonEquipment")
    create_hook_curve("First person hooked two hand stick body", mats, col)
    for i in range(12):
        cylinder("First person lower grip wrap " + str(i + 1), (0, -0.72 + i * 0.035, 0), 0.039, 0.016, mats["rubber"], col, 32)
    for i in range(10):
        cylinder("First person upper grip wrap " + str(i + 1), (0, -0.18 + i * 0.035, 0), 0.037, 0.016, mats["rubber"], col, 32)
    beveled_box("First person lower gloved hand", (-0.065, -0.52, -0.08), (0.22, 0.14, 0.12), mats["rubber"], col, 0.025, 2)
    beveled_box("First person upper gloved hand", (-0.065, -0.08, -0.08), (0.21, 0.13, 0.11), mats["rubber"], col, 0.025, 2)
    add_screen("First person integrated wrist readout", (-0.19, -0.70, -0.11), (0.38, 0.22, 0.06), mats, col)
    cylinder("First person musket barrel", (0.6, -0.48, 0), 0.018, 1.25, mats["bright_metal"], col, 32, (0, math.radians(90), 0))
    beveled_box("First person musket worn wood stock", (-0.2, -0.48, 0), (0.62, 0.08, 0.08), mats["wood"], col, 0.025, 2)
    add_screen("First person wrist suit readout", (-0.45, -0.88, -0.1), (0.44, 0.28, 0.06), mats, col)
    return col


def export_collection(col, filename):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = col.objects[0] if col.objects else None
    path = os.path.join(FBX_DIR, filename)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH", "EMPTY"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        axis_forward="-Z",
        axis_up="Y",
    )


def main():
    ensure_dirs()
    clear_scene()
    mats = create_materials()
    collections = [
        create_mesh_library(mats),
        create_cockpit_preview(mats),
        create_cctv_preview(mats),
        create_engine_preview(mats),
        create_supply_preview(mats),
        create_cargo_preview(mats),
        create_armory_preview(mats),
        create_first_person_preview(mats),
    ]

    source_path = os.path.join(BLEND_DIR, "Stage3Rework_All.blend")
    bpy.ops.wm.save_as_mainfile(filepath=source_path)

    export_collection(collections[0], "FBX_Stage3Rework_MeshLibrary.fbx")
    export_collection(collections[1], "FBX_01_CockpitHelmAndStatus.fbx")
    export_collection(collections[2], "FBX_02_ControlRoomCCTV.fbx")
    export_collection(collections[3], "FBX_03_EnginePowerTerminal.fbx")
    export_collection(collections[4], "FBX_04_SupplyStorageCabinet.fbx")
    export_collection(collections[5], "FBX_05_CargoHoldPropsAndTerminal.fbx")
    export_collection(collections[6], "FBX_06_ArmoryTurretGripMount.fbx")
    export_collection(collections[7], "FBX_07_FirstPersonEquipment.fbx")
    print("Stage 3 Blender rework assets generated at " + ASSET_ROOT)


if __name__ == "__main__":
    main()
