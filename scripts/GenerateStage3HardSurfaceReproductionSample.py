from __future__ import annotations

import json
import math
import os
import random
import sys

import bpy
from mathutils import Euler, Vector


def parse_project_root() -> str:
    args = sys.argv
    extra = args[args.index("--") + 1 :] if "--" in args else []
    for index, value in enumerate(extra):
        if value == "--project-root" and index + 1 < len(extra):
            return os.path.abspath(extra[index + 1])
    return os.getcwd()


PROJECT_ROOT = parse_project_root()
SCRIPTS_DIR = os.path.join(PROJECT_ROOT, "scripts")
if SCRIPTS_DIR not in sys.path:
    sys.path.insert(0, SCRIPTS_DIR)

import GenerateStage3BlenderApprovalSamples as base  # noqa: E402


SAMPLE_ROOT = os.path.join(PROJECT_ROOT, "artSample", "stage3_hardsurface_reproduction_sample")
SLOT_RENDER_DIR = os.path.join(SAMPLE_ROOT, "slots")
RENDER_DIR = os.path.join(SAMPLE_ROOT, "renders")
EXPORT_DIR = os.path.join(SAMPLE_ROOT, "exports")
TEXTURE_DIR = os.path.join(SAMPLE_ROOT, "textures")
BLENDER_DIR = os.path.join(SAMPLE_ROOT, "blender")
ANALYSIS_DIR = os.path.join(SAMPLE_ROOT, "analysis")

ITEM_ID = "01"
SLUG = "cockpit_helm_and_status"
VARIANT = "hardsurface-structure-v006"
FILE_SUFFIX = "hardsurface_structure_v006"
SLOT_RENDER_WIDTH = 960
SLOT_RENDER_HEIGHT = 640
TEXTURE_SIZE = 768

SLOT_CAMERA_PRESETS = {
    "main": ((2.18, -4.35, 0.95), (0.0, -0.10, 0.06), 34.5),
    "left_close": ((0.28, -3.72, 0.62), (0.0, -0.74, 0.02), 36.0),
    "center_close": ((0.02, -4.05, 0.16), (0.0, -0.82, -0.16), 33.0),
    "screen_close": ((0.08, -3.80, 0.34), (0.0, -0.74, 0.04), 36.0),
}


def configure_base() -> None:
    base.PROJECT_ROOT = PROJECT_ROOT
    base.SAMPLE_ROOT = SAMPLE_ROOT
    base.RENDER_DIR = SLOT_RENDER_DIR
    base.EXPORT_DIR = EXPORT_DIR
    base.TEXTURE_DIR = TEXTURE_DIR
    base.BLENDER_DIR = BLENDER_DIR
    base.RENDER_WIDTH = SLOT_RENDER_WIDTH
    base.RENDER_HEIGHT = SLOT_RENDER_HEIGHT


def ensure_dirs() -> None:
    for path in (SAMPLE_ROOT, SLOT_RENDER_DIR, RENDER_DIR, EXPORT_DIR, TEXTURE_DIR, BLENDER_DIR, ANALYSIS_DIR):
        os.makedirs(path, exist_ok=True)


def set_render_quality() -> None:
    scene = bpy.context.scene
    scene.render.resolution_x = SLOT_RENDER_WIDTH
    scene.render.resolution_y = SLOT_RENDER_HEIGHT
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = -0.10
    scene.view_settings.gamma = 1.0
    if scene.world is None:
        scene.world = bpy.data.worlds.new("Stage3HardSurfaceWorld")
    scene.world.color = (0.010, 0.011, 0.010)
    if hasattr(scene, "eevee"):
        if hasattr(scene.eevee, "taa_render_samples"):
            scene.eevee.taa_render_samples = 128
        if hasattr(scene.eevee, "use_bloom"):
            scene.eevee.use_bloom = True


def make_painted_texture(name: str, base_rgb, edge_rgb, grime_rgb, seed: int) -> bpy.types.Image:
    rng = random.Random(seed)
    image = bpy.data.images.new(name, TEXTURE_SIZE, TEXTURE_SIZE, alpha=True)
    pixels = [0.0] * (TEXTURE_SIZE * TEXTURE_SIZE * 4)
    chip_centers = [(rng.randrange(20, TEXTURE_SIZE - 20), rng.randrange(20, TEXTURE_SIZE - 20), rng.randrange(8, 34)) for _ in range(160)]
    grime_centers = [(rng.randrange(0, TEXTURE_SIZE), rng.randrange(0, TEXTURE_SIZE), rng.randrange(34, 110)) for _ in range(44)]

    for y in range(TEXTURE_SIZE):
        for x in range(TEXTURE_SIZE):
            fine = random.Random((x * 83492791) ^ (y * 19349663) ^ seed).random()
            coarse = random.Random(((x // 17) * 73856093) ^ ((y // 17) * 2971215073) ^ seed).random()
            vertical_grime = 0.16 if (x + seed) % 211 < 5 else 0.0
            worn = 0.0
            grime = 0.0
            for cx, cy, radius in chip_centers:
                distance = ((x - cx) * (x - cx) + (y - cy) * (y - cy)) ** 0.5
                if distance < radius:
                    worn = max(worn, 1.0 - distance / radius)
            for cx, cy, radius in grime_centers:
                distance = ((x - cx) * (x - cx) + (y - cy) * (y - cy)) ** 0.5
                if distance < radius:
                    grime = max(grime, 0.65 * (1.0 - distance / radius))
            edge_bias = 0.0
            if x < 32 or x > TEXTURE_SIZE - 33 or y < 32 or y > TEXTURE_SIZE - 33:
                edge_bias = 0.40 * (1.0 - min(x, y, TEXTURE_SIZE - 1 - x, TEXTURE_SIZE - 1 - y) / 32.0)
            scratch = 0.22 if ((x * 3 + y + seed) % 179 < 3 and fine > 0.34) else 0.0
            worn = max(worn * 0.74, edge_bias, scratch)
            grime = min(1.0, grime + vertical_grime + (0.18 if coarse < 0.08 else 0.0))
            noise = (fine - 0.5) * 0.13 + (coarse - 0.5) * 0.10
            rgb = []
            for channel in range(3):
                color = base_rgb[channel] + noise
                color = color * (1.0 - grime) + grime_rgb[channel] * grime
                color = color * (1.0 - worn) + edge_rgb[channel] * worn
                rgb.append(max(0.0, min(1.0, color)))
            offset = (y * TEXTURE_SIZE + x) * 4
            pixels[offset] = rgb[0]
            pixels[offset + 1] = rgb[1]
            pixels[offset + 2] = rgb[2]
            pixels[offset + 3] = 1.0

    image.pixels.foreach_set(pixels)
    image.update()
    image.filepath_raw = os.path.join(TEXTURE_DIR, name + ".png")
    image.file_format = "PNG"
    image.save()
    return image


def make_crt_texture(name: str, seed: int) -> bpy.types.Image:
    image = bpy.data.images.new(name, TEXTURE_SIZE, TEXTURE_SIZE, alpha=True)
    pixels = [0.0] * (TEXTURE_SIZE * TEXTURE_SIZE * 4)
    for y in range(TEXTURE_SIZE):
        for x in range(TEXTURE_SIZE):
            fine = random.Random((x * 73856093) ^ (y * 19349663) ^ seed).random()
            coarse = random.Random(((x // 26) * 83492791) ^ ((y // 18) * 2971215073) ^ seed).random()
            scanline = 0.10 if y % 9 in (0, 1) else 0.0
            vignette_x = abs((x / TEXTURE_SIZE) - 0.5) * 2.0
            vignette_y = abs((y / TEXTURE_SIZE) - 0.5) * 2.0
            vignette = min(0.55, (vignette_x * vignette_x + vignette_y * vignette_y) * 0.28)
            smudge = 0.0
            if 0.20 < x / TEXTURE_SIZE < 0.83 and 0.30 < y / TEXTURE_SIZE < 0.72:
                smudge = 0.08 * max(0.0, 1.0 - abs((x / TEXTURE_SIZE) - 0.52) * 2.1)
            grid = 0.030 if x % 72 < 2 or y % 88 < 2 else 0.0
            level = max(0.0, min(1.0, 0.32 + fine * 0.20 + coarse * 0.10 + scanline + grid + smudge - vignette))
            r = 0.004 + level * 0.055
            g = 0.030 + level * 0.260
            b = 0.018 + level * 0.105
            offset = (y * TEXTURE_SIZE + x) * 4
            pixels[offset] = r
            pixels[offset + 1] = g
            pixels[offset + 2] = b
            pixels[offset + 3] = 1.0
    image.pixels.foreach_set(pixels)
    image.update()
    image.filepath_raw = os.path.join(TEXTURE_DIR, name + ".png")
    image.file_format = "PNG"
    image.save()
    return image


def make_materials():
    mats = base.create_materials()
    dark_painted = make_painted_texture(
        "ST3_HS01_HandPainted_DarkWornMetal",
        (0.175, 0.168, 0.148),
        (0.76, 0.72, 0.58),
        (0.025, 0.025, 0.021),
        8101,
    )
    black_painted = make_painted_texture(
        "ST3_HS01_HandPainted_BlackRubberGrime",
        (0.030, 0.031, 0.028),
        (0.20, 0.19, 0.16),
        (0.006, 0.006, 0.005),
        8102,
    )
    screen_painted = make_crt_texture("ST3_HS01_HandPainted_DimCrtGlass_structure_v006", 8103)
    mats["hs_dark"] = base.make_material("MAT_HS01_HandPaintedDarkMetal", (0.18, 0.175, 0.155, 1), dark_painted, 0.72, 0.60, 0.0, 0.110, 104.0, True)
    mats["hs_edge"] = base.make_material("MAT_HS01_ExposedBrightEdges", (0.68, 0.65, 0.54, 1), None, 0.82, 0.36, 0.0, 0.020, 120.0, True)
    mats["hs_black"] = base.make_material("MAT_HS01_PaintedBlackRubber", (0.025, 0.025, 0.022, 1), black_painted, 0.0, 0.84, 0.0, 0.060, 130.0, True)
    mats["hs_screen"] = base.make_material("MAT_HS01_DimPaintedCrt", (0.010, 0.130, 0.060, 1), screen_painted, 0.0, 0.48, 0.18, 0.012, 48.0, True)
    mats["hs_screen_glow"] = base.make_material("MAT_HS01_SubtleCrtGlow", (0.030, 0.230, 0.105, 1), screen_painted, 0.0, 0.42, 0.38, 0.008, 42.0, True)
    mats["hs_view_glass"] = base.make_material("MAT_HS01_SmokedViewportGlass", (0.018, 0.024, 0.020, 0.22), None, 0.0, 0.36)
    mats["hs_view_glass"].use_nodes = True
    bsdf = mats["hs_view_glass"].node_tree.nodes.get("Principled BSDF")
    if bsdf and "Alpha" in bsdf.inputs:
        bsdf.inputs["Alpha"].default_value = 0.22
    mats["hs_view_glass"].blend_method = "BLEND"
    mats["hs_view_glass"].show_transparent_back = True
    mats["hs_grime"] = base.make_material("MAT_HS01_OilyBlackGrime", (0.010, 0.009, 0.007, 1), None, 0.0, 0.92)
    mats["hs_orange"] = base.make_material("MAT_HS01_AgedOrangeWarning", (0.64, 0.32, 0.080, 1), None, 0.18, 0.72)
    mats["hs_blockout"] = base.make_material("MAT_HS01_BlockoutClay", (0.18, 0.19, 0.18, 1), None, 0.0, 0.72)
    return mats


def rotated_point(center, offset, rot=(0.0, 0.0, 0.0)):
    return tuple(Vector(center) + (Euler(rot, "XYZ").to_matrix() @ Vector(offset)))


def chamfered_box(name: str, center, size, mat, col, chamfer_ratio=0.12, bevel=0.004, rot=(0.0, 0.0, 0.0)):
    sx, sy, sz = size
    hx, hy, hz = sx * 0.5, sy * 0.5, sz * 0.5
    cut = min(hx, hz) * chamfer_ratio
    outline = [
        (-hx + cut, -hz),
        (hx - cut, -hz),
        (hx, -hz + cut),
        (hx, hz - cut),
        (hx - cut, hz),
        (-hx + cut, hz),
        (-hx, hz - cut),
        (-hx, -hz + cut),
    ]
    verts = [(x, -hy, z) for x, z in outline] + [(x, hy, z) for x, z in outline]
    faces = [tuple(range(8)), tuple(reversed(range(8, 16)))]
    for index in range(8):
        faces.append((index, (index + 1) % 8, (index + 1) % 8 + 8, index + 8))
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = center
    obj.rotation_euler = rot
    obj.data.materials.append(mat)
    if bevel > 0.0:
        mod = obj.modifiers.new("hard edge micro bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        obj.modifiers.new("weighted hard normals", "WEIGHTED_NORMAL")
    col.objects.link(obj)
    return obj


def add_frame_edge_wear(prefix: str, col, mats, center, size, rot=(0.0, 0.0, 0.0), seed: int = 0, count: int = 40) -> None:
    rng = random.Random(seed)
    sx, sy, sz = size
    for index in range(count):
        horizontal = index % 2 == 0
        side = -1 if rng.random() < 0.5 else 1
        if horizontal:
            local_x = rng.uniform(-sx * 0.43, sx * 0.43)
            local_z = side * rng.uniform(sz * 0.41, sz * 0.49)
            chip_size = (rng.uniform(sx * 0.015, sx * 0.060), 0.004, rng.uniform(sz * 0.006, sz * 0.018))
        else:
            local_x = side * rng.uniform(sx * 0.41, sx * 0.49)
            local_z = rng.uniform(-sz * 0.43, sz * 0.43)
            chip_size = (rng.uniform(sx * 0.006, sx * 0.020), 0.004, rng.uniform(sz * 0.014, sz * 0.055))
        mat = mats["hs_edge"] if rng.random() < 0.58 else mats["hs_grime"]
        loc = rotated_point(center, (local_x, -sy * 0.54 - index * 0.00018, local_z), rot)
        base.box(f"{prefix}_edge_wear_{index:03d}", loc, chip_size, mat, col, 0.001, 1, rot)


def add_panel_seams(prefix: str, col, mats, center, size, rot=(0.0, 0.0, 0.0)) -> None:
    x, y, z = center
    sx, sy, sz = size
    for index, factor in enumerate((-0.31, 0.31)):
        loc = rotated_point((x, y, z), (factor * sx, -sy * 0.58, 0.0), rot)
        base.box(f"{prefix}_vertical_machined_seam_{index}", loc, (0.016, 0.004, sz * 0.72), mats["hs_black"], col, 0.001, 1, rot)
    for index, factor in enumerate((-0.28, 0.28)):
        loc = rotated_point((x, y, z), (0.0, -sy * 0.59, factor * sz), rot)
        base.box(f"{prefix}_horizontal_machined_seam_{index}", loc, (sx * 0.72, 0.004, 0.012), mats["hs_black"], col, 0.001, 1, rot)


def add_chip_strip(prefix: str, col, mats, x: float, y: float, z: float, width: float, height: float, seed: int, count: int = 24) -> None:
    rng = random.Random(seed)
    for index in range(count):
        px = x + rng.uniform(-width * 0.48, width * 0.48)
        pz = z + rng.uniform(-height * 0.48, height * 0.48)
        sx = rng.uniform(0.010, 0.048)
        sz = rng.uniform(0.004, 0.022)
        mat = mats["hs_grime"] if index % 3 else mats["hs_edge"]
        base.box(
            f"{prefix}_painted_chip_{index:03d}",
            (px, y - index * 0.00035, pz),
            (sx, 0.004, sz),
            mat,
            col,
            0.001,
            1,
            (0.0, 0.0, math.radians(rng.uniform(-9.0, 9.0))),
        )


def add_panel(prefix: str, col, mats, center, size, rot=(0.0, 0.0, 0.0), inset=True) -> None:
    x, y, z = center
    sx, sy, sz = size
    chamfered_box(prefix + "_outer_chamfered_plate", center, size, mats["hs_dark"], col, 0.090, 0.004, rot)
    if inset:
        chamfered_box(prefix + "_deep_recessed_inner_plate", (x, y - sy * 0.58, z), (sx * 0.76, 0.014, sz * 0.62), mats["hs_black"], col, 0.070, 0.002, rot)
    base.corner_bolts(prefix, center, sx, sz, y - sy * 0.64, mats["hs_edge"], col, 0.019)
    add_panel_seams(prefix, col, mats, center, size, rot)
    add_frame_edge_wear(prefix + "_paint", col, mats, center, size, rot, abs(hash(prefix)) % 10000, 20)


def monitor_module(prefix: str, col, mats, center, size, rot=(0.0, 0.0, 0.0), heavy_side=False) -> None:
    x, y, z = center
    sx, sy, sz = size
    chamfered_box(prefix + "_rear_angular_case", (x + 0.025, y + 0.030, z - 0.015), (sx * 1.24, sy * 1.76, sz * 1.34), mats["hs_black"], col, 0.115, 0.004, rot)
    chamfered_box(prefix + "_armored_front_frame", center, (sx * 1.12, sy * 1.34, sz * 1.14), mats["hs_dark"], col, 0.130, 0.006, rot)
    chamfered_box(prefix + "_inner_shadow_recess", (x, y - sy * 0.67, z), (sx * 0.91, sy * 0.130, sz * 0.82), mats["hs_black"], col, 0.105, 0.003, rot)
    chamfered_box(prefix + "_green_crt_glass", (x, y - sy * 0.78, z), (sx * 0.74, sy * 0.035, sz * 0.58), mats["hs_screen"], col, 0.070, 0.002, rot)
    for index, factor in enumerate((-0.46, 0.46)):
        loc = rotated_point(center, (factor * sx, -sy * 0.76, 0.0), rot)
        base.box(f"{prefix}_vertical_inner_frame_bar_{index}", loc, (sx * 0.028, sy * 0.016, sz * 0.74), mats["hs_dark"], col, 0.002, 1, rot)
    for index in range(9):
        base.box(
            f"{prefix}_faint_scanline_{index:02d}",
            (x, y - sy * 0.83, z - sz * 0.24 + index * sz * 0.060),
            (sx * 0.58, sy * 0.010, sz * 0.004),
            mats["hs_screen_glow"],
            col,
            0.0,
            1,
            rot,
        )
    for index, (dx, dz, length) in enumerate(((-0.20, 0.12, 0.42), (0.13, -0.10, 0.34), (0.02, 0.20, 0.30))):
        loc = rotated_point(center, (dx * sx, -sy * 0.825, dz * sz), rot)
        base.box(
            f"{prefix}_faint_glass_scratch_{index}",
            loc,
            (length * sx, sy * 0.006, sz * 0.004),
            mats["hs_screen_glow"],
            col,
            0.0,
            1,
            (rot[0], rot[1], rot[2] + math.radians(12.0 - index * 17.0)),
        )
    for index in range(4):
        base.box(
            f"{prefix}_bottom_status_pin_{index}",
            (x - sx * 0.24 + index * sx * 0.16, y - sy * 0.92, z - sz * 0.56),
            (sx * 0.038, sy * 0.020, sz * 0.026),
            mats["hs_screen_glow"] if index < 2 else mats["hs_orange"],
            col,
            0.002,
            1,
            rot,
        )
    base.box(prefix + "_bottom_label_recess", (x, y - sy * 0.92, z - sz * 0.61), (sx * 0.42, sy * 0.032, sz * 0.045), mats["hs_black"], col, 0.004, 1, rot)
    if heavy_side:
        for side in (-1, 1):
            base.box(f"{prefix}_side_vented_cheek_{side}", (x + side * sx * 0.66, y - sy * 0.08, z), (sx * 0.105, sy * 0.70, sz * 0.92), mats["hs_dark"], col, 0.006, 1, rot)
            for vent in range(4):
                base.box(
                    f"{prefix}_side_vent_slot_{side}_{vent}",
                    (x + side * sx * 0.66, y - sy * 0.45 + vent * sy * 0.14, z + sz * 0.22),
                    (sx * 0.018, sy * 0.030, sz * 0.16),
                    mats["hs_black"],
                    col,
                    0.002,
                    1,
                    rot,
                )
    base.corner_bolts(prefix + "_monitor", center, sx * 1.02, sz * 1.02, y - sy * 0.94, mats["hs_edge"], col, 0.020)
    add_frame_edge_wear(prefix + "_frame", col, mats, center, (sx * 1.06, sy, sz * 1.06), rot, abs(hash(prefix)) % 50000, 34)


def helm_ring(prefix: str, col, mats, center=(0.0, -0.78, -0.20), radius=0.66) -> None:
    cx, cy, cz = center
    for index, angle in enumerate(range(205, 516, 14)):
        normalized = angle % 360
        if 158 < normalized < 202:
            continue
        radians = math.radians(normalized)
        x = cx + math.cos(radians) * radius
        z = cz + math.sin(radians) * radius
        base.box(
            f"{prefix}_machined_ring_segment_{index:02d}",
            (x, cy, z),
            (0.205, 0.074, 0.058),
            mats["hs_dark"],
            col,
            0.005,
            1,
            (0.0, math.radians(90.0 - normalized), 0.0),
        )
        if index % 2 == 0:
            base.bolt(f"{prefix}_ring_exposed_bolt_{index:02d}", (x, cy - 0.060, z), mats["hs_edge"], col, 0.012)
    base.curve_pipe(f"{prefix}_left_bent_grip_arm", [(cx - 0.45, cy, cz - 0.04), (cx - 0.88, cy - 0.12, cz - 0.08), (cx - 1.04, cy - 0.12, cz - 0.34)], 0.030, mats["hs_edge"], col)
    base.curve_pipe(f"{prefix}_right_bent_grip_arm", [(cx + 0.45, cy, cz - 0.04), (cx + 0.88, cy - 0.12, cz - 0.08), (cx + 1.04, cy - 0.12, cz - 0.34)], 0.030, mats["hs_edge"], col)
    base.ribbed_grip(f"{prefix}_left_ribbed_grip", (cx - 1.06, cy - 0.12, cz - 0.08), 0.076, 0.54, col, mats, "Z")
    base.ribbed_grip(f"{prefix}_right_ribbed_grip", (cx + 1.06, cy - 0.12, cz - 0.08), 0.076, 0.54, col, mats, "Z")
    base.box(f"{prefix}_left_red_thumb_switch", (cx - 1.06, cy - 0.20, cz + 0.22), (0.070, 0.026, 0.046), mats["red"], col, 0.008, 1)
    base.box(f"{prefix}_right_red_thumb_switch", (cx + 1.06, cy - 0.20, cz + 0.22), (0.070, 0.026, 0.046), mats["red"], col, 0.008, 1)
    add_frame_edge_wear(prefix + "_ring", col, mats, (cx, cy - 0.035, cz), (radius * 2.0, 0.080, radius * 2.0), (0.0, 0.0, 0.0), 8181, 36)


def build_blockout(col, mats) -> None:
    base.box("hs01_blockout_room_shell", (0.0, 0.05, 0.20), (5.7, 3.6, 2.7), mats["hs_blockout"], col, 0.020, 1)
    base.box("hs01_blockout_center_console", (0.0, -0.36, -0.25), (1.45, 0.56, 1.20), mats["hs_blockout"], col, 0.020, 1)
    base.box("hs01_blockout_left_console_bank", (-1.35, -0.36, -0.30), (1.42, 0.48, 0.70), mats["hs_blockout"], col, 0.020, 1)
    base.box("hs01_blockout_right_console_bank", (1.42, -0.36, -0.30), (1.55, 0.48, 0.70), mats["hs_blockout"], col, 0.020, 1)
    base.box("hs01_blockout_main_monitor", (0.0, -0.38, 0.48), (1.42, 0.18, 0.70), mats["hs_blockout"], col, 0.020, 1)
    base.torus("hs01_blockout_helm_ring", (0.0, -0.82, -0.20), 0.66, 0.045, mats["hs_blockout"], col, (math.radians(90), 0.0, 0.0), 64)


def build_cargo_bay_depth(prefix: str, col, mats) -> None:
    base.box(prefix + "_distant_floor_plane", (0.0, 1.86, -0.62), (3.70, 1.62, 0.070), mats["hs_dark"], col, 0.006, 1)
    base.box(prefix + "_distant_rear_wall", (0.0, 2.66, 0.28), (3.70, 0.070, 1.54), mats["hs_black"], col, 0.006, 1)
    base.box(prefix + "_left_depth_wall", (-1.88, 1.88, 0.16), (0.080, 1.56, 1.46), mats["hs_dark"], col, 0.006, 1)
    base.box(prefix + "_right_depth_wall", (1.88, 1.88, 0.16), (0.080, 1.56, 1.46), mats["hs_dark"], col, 0.006, 1)
    for index, y in enumerate((1.18, 1.54, 1.92, 2.30)):
        base.box(prefix + f"_receding_floor_track_{index}", (-0.74, y, -0.56), (0.070, 0.28, 0.026), mats["hs_edge"], col, 0.002, 1)
        base.box(prefix + f"_receding_floor_track_r_{index}", (0.74, y, -0.56), (0.070, 0.28, 0.026), mats["hs_edge"], col, 0.002, 1)
        base.strip_light(prefix + f"_overhead_depth_light_{index}", (-1.18 + index * 0.78, y, 1.04), (0.28, 0.018, 0.030), col, mats)
    for rail_index, z in enumerate((0.02, 0.34, 0.66)):
        base.box(prefix + f"_back_rail_{rail_index}", (0.0, 1.18, z), (3.12, 0.030, 0.025), mats["hs_edge"], col, 0.002, 1)
    for rail_index, x in enumerate((-1.54, -0.82, -0.10, 0.62, 1.34)):
        base.box(prefix + f"_railing_post_{rail_index}", (x, 1.17, 0.32), (0.034, 0.032, 0.70), mats["hs_dark"], col, 0.002, 1)
    crate_specs = (
        (-1.22, 2.18, -0.25, 0.38, 0.28, 0.32),
        (-0.42, 2.36, -0.22, 0.50, 0.30, 0.34),
        (0.46, 2.12, -0.26, 0.42, 0.28, 0.30),
        (1.18, 2.42, -0.18, 0.46, 0.34, 0.38),
        (-1.42, 1.48, -0.30, 0.34, 0.26, 0.28),
        (1.36, 1.54, -0.30, 0.34, 0.26, 0.28),
    )
    for index, (x, y, z, sx, sy, sz) in enumerate(crate_specs):
        base.build_cargo_crate(prefix + f"_visible_cargo_stack_{index}", (x, y, z), (sx, sy, sz), col, mats, False)
    for index, x in enumerate((-1.65, -0.95, -0.26, 0.48, 1.10, 1.62)):
        base.box(prefix + f"_small_orange_status_light_{index}", (x, 1.08, -0.18 + (index % 2) * 0.24), (0.028, 0.012, 0.028), mats["hs_orange"], col, 0.002, 1)


def build_final_cockpit(col, mats) -> None:
    base.industrial_floor("hs01_main", col, mats, width=5.65, depth=3.90, z=-1.15)
    base.box("hs01_main_left_wall_plate_stack", (-2.82, 0.05, 0.12), (0.16, 3.20, 2.62), mats["hs_dark"], col, 0.026, 2)
    base.box("hs01_main_right_wall_plate_stack", (2.82, 0.05, 0.12), (0.16, 3.20, 2.62), mats["hs_dark"], col, 0.026, 2)
    base.box("hs01_main_rear_bulkhead_left", (-2.46, 1.20, 0.24), (0.74, 0.16, 2.58), mats["hs_dark"], col, 0.018, 1)
    base.box("hs01_main_rear_bulkhead_right", (2.46, 1.20, 0.24), (0.74, 0.16, 2.58), mats["hs_dark"], col, 0.018, 1)
    base.box("hs01_main_rear_bulkhead_top", (0.0, 1.19, 1.48), (4.30, 0.16, 0.34), mats["hs_dark"], col, 0.018, 1)
    base.box("hs01_main_rear_bulkhead_lower_sill", (0.0, 1.15, -0.30), (4.16, 0.16, 0.22), mats["hs_dark"], col, 0.018, 1)
    base.box("hs01_main_low_ceiling_black", (0.0, -0.02, 1.83), (5.62, 3.26, 0.12), mats["hs_black"], col, 0.012, 1)
    for index, x in enumerate((-2.15, -1.34, -0.54, 0.22, 1.05, 1.88, 2.40)):
        base.box(f"hs01_main_ceiling_rib_{index}", (x, -0.02, 1.67), (0.090, 3.05, 0.13), mats["hs_dark"], col, 0.006, 1)
    base.box("hs01_main_forward_octagon_outer_top", (0.0, 1.05, 1.42), (4.25, 0.16, 0.16), mats["hs_dark"], col, 0.026, 2)
    base.box("hs01_main_forward_octagon_outer_bottom", (0.0, 1.02, 0.20), (4.05, 0.16, 0.16), mats["hs_dark"], col, 0.026, 2)
    for side in (-1, 1):
        base.box(f"hs01_main_forward_octagon_side_{side}", (side * 2.05, 1.03, 0.82), (0.16, 0.16, 1.16), mats["hs_dark"], col, 0.026, 2, (0.0, 0.0, math.radians(side * 12.0)))
        base.box(f"hs01_main_forward_inner_side_gasket_{side}", (side * 1.78, 0.94, 0.82), (0.075, 0.050, 0.92), mats["hs_black"], col, 0.010, 1, (0.0, 0.0, math.radians(side * 12.0)))
    build_cargo_bay_depth("hs01_main_cargo_bay_depth", col, mats)
    base.box("hs01_main_forward_smoked_glass", (0.0, 0.90, 0.82), (3.34, 0.010, 0.78), mats["hs_view_glass"], col, 0.003, 1)
    for index, x in enumerate((-1.78, -1.08, -0.36, 0.44, 1.18, 1.86)):
        base.strip_light(f"hs01_main_window_strip_light_{index}", (x, 0.88, 1.24), (0.28, 0.020, 0.032), col, mats)
    for index, x in enumerate((-2.05, -1.32, -0.55, 0.28, 1.08, 1.90)):
        add_panel(f"hs01_main_rear_service_panel_{index}", col, mats, (x, 1.17, -0.44), (0.42, 0.040, 0.58), inset=True)

    monitor_module("hs01_main_center_crt", col, mats, (0.00, -0.20, 0.30), (1.08, 0.14, 0.56), (math.radians(-5.0), 0.0, 0.0), True)
    monitor_module("hs01_main_left_crt_a", col, mats, (-1.48, -0.25, -0.02), (0.62, 0.12, 0.40), (math.radians(-6.0), 0.0, math.radians(5.0)), True)
    monitor_module("hs01_main_left_crt_b", col, mats, (-0.86, -0.23, -0.01), (0.52, 0.11, 0.36), (math.radians(-6.0), 0.0, math.radians(2.0)), False)
    monitor_module("hs01_main_right_crt_a", col, mats, (1.02, -0.22, -0.02), (0.56, 0.12, 0.38), (math.radians(-6.0), 0.0, math.radians(-2.0)), False)
    monitor_module("hs01_main_right_crt_b", col, mats, (1.64, -0.21, -0.04), (0.62, 0.12, 0.42), (math.radians(-6.0), 0.0, math.radians(-5.0)), True)
    chamfered_box("hs01_main_center_pedestal", (0.0, -0.72, -0.76), (0.38, 0.30, 0.92), mats["hs_dark"], col, 0.080, 0.005)
    chamfered_box("hs01_main_pedestal_front_panel", (0.0, -0.91, -0.72), (0.30, 0.030, 0.56), mats["hs_black"], col, 0.060, 0.002)
    chamfered_box("hs01_main_pedestal_lower_panel", (0.0, -0.92, -1.03), (0.34, 0.034, 0.26), mats["hs_dark"], col, 0.060, 0.003)
    add_frame_edge_wear("hs01_main_pedestal", col, mats, (0.0, -0.91, -0.72), (0.36, 0.030, 0.56), (0.0, 0.0, 0.0), 9401, 24)
    helm_ring("hs01_main_floor_mounted_helm", col, mats, (0.0, -0.80, -0.20), 0.64)
    for side in (-1, 1):
        base.curve_pipe(f"hs01_main_side_heavy_hose_{side}", [(side * 2.18, 0.88, 0.82), (side * 2.00, 0.32, 0.18), (side * 1.86, -0.22, -0.72)], 0.042, mats["hs_black"], col)
        for index in range(5):
            base.box(
                f"hs01_main_side_cable_clamp_{side}_{index}",
                (side * 2.50, 0.70 - index * 0.25, -0.38),
                (0.052, 0.036, 0.58),
                mats["hs_edge"],
                col,
                0.004,
                1,
            )
    add_chip_strip("hs01_main_floor_oily_low_grime", col, mats, 0.0, -0.98, -0.98, 4.20, 0.36, 9101, 72)


def build_iso_models(col, mats) -> None:
    base.box("hs01_iso_black_backdrop_wall", (0.0, -0.40, 0.05), (4.10, 0.08, 2.75), mats["hs_black"], col, 0.010, 1)
    base.box("hs01_iso_black_backdrop_floor", (0.0, -0.86, -1.18), (4.10, 2.10, 0.08), mats["hs_black"], col, 0.010, 1)
    monitor_module("hs01_iso_terminal_rugged_case", col, mats, (0.0, -0.72, 0.18), (1.28, 0.17, 0.78), (math.radians(-8.0), 0.0, math.radians(-4.0)), True)
    chamfered_box("hs01_iso_terminal_lower_latch_plate", (0.0, -0.96, -0.52), (1.18, 0.15, 0.18), mats["hs_dark"], col, 0.100, 0.004)
    base.ribbed_grip("hs01_iso_terminal_front_handle", (-0.45, -1.02, -0.18), 0.050, 0.68, col, mats, "X")
    helm_ring("hs01_iso_helm_ring", col, mats, (0.0, -0.82, -0.14), 0.70)
    monitor_module("hs01_iso_screen_single", col, mats, (0.0, -0.74, 0.10), (1.16, 0.16, 0.72), (0.0, 0.0, 0.0), True)
    monitor_module("hs01_iso_screen_left_small", col, mats, (-0.92, -0.77, 0.06), (0.70, 0.12, 0.50), (0.0, 0.0, math.radians(1.5)), False)
    monitor_module("hs01_iso_screen_right_small", col, mats, (0.92, -0.77, 0.06), (0.70, 0.12, 0.50), (0.0, 0.0, math.radians(-1.5)), False)
    add_chip_strip("hs01_iso_floor_contact_grime", col, mats, 0.0, -1.04, -0.74, 3.25, 0.34, 9201, 64)


def configure_slot_visibility(col, slot_name: str) -> None:
    closeup = slot_name != "main"
    iso_prefixes = {
        "left_close": "hs01_iso_terminal",
        "center_close": "hs01_iso_helm",
        "screen_close": "hs01_iso_screen",
    }
    active_iso = iso_prefixes.get(slot_name)
    for obj in col.objects:
        name = obj.name
        obj.hide_render = False
        obj.hide_viewport = False
        if name.startswith("hs01_blockout_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if closeup and name.startswith("hs01_main_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if not closeup and name.startswith("hs01_iso_"):
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        if closeup and name.startswith("hs01_iso_") and active_iso and not name.startswith(active_iso):
            if not name.startswith("hs01_iso_black_backdrop"):
                obj.hide_render = True
                obj.hide_viewport = True


def render_slot(slot_name: str, final_col, collections, camera_settings) -> str:
    base.set_collection_visibility(collections, final_col)
    configure_slot_visibility(final_col, slot_name)
    base.add_render_lights("hs01_" + slot_name)
    for obj in bpy.context.scene.objects:
        if obj.type != "LIGHT" or not obj.name.startswith("Render_hs01_"):
            continue
        if obj.name.endswith("_soft_key"):
            obj.data.energy *= 1.05
            obj.data.color = (0.95, 0.91, 0.84)
        elif obj.name.endswith("_warm_rim"):
            obj.data.energy *= 0.86
        elif obj.name.endswith("_crt_green_fill"):
            obj.data.energy *= 0.045
    loc, target, lens = camera_settings
    base.add_render_camera("Render_hs01_" + slot_name + "_camera", loc, target, lens)
    filepath = os.path.join(SLOT_RENDER_DIR, f"{ITEM_ID}_{SLUG}_{slot_name}.png")
    bpy.context.scene.render.filepath = filepath
    bpy.ops.render.render(write_still=True)
    return filepath


def export_final_collection(col) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    selected = []
    for obj in col.objects:
        if obj.type in {"MESH", "CURVE", "FONT", "LIGHT"} and not obj.name.startswith("hs01_blockout_"):
            obj.select_set(True)
            selected.append(obj)
    if selected:
        bpy.context.view_layer.objects.active = selected[0]
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(EXPORT_DIR, f"FBX_{ITEM_ID}_{SLUG}_{FILE_SUFFIX}.fbx"),
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH", "EMPTY", "LIGHT"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        axis_forward="-Z",
        axis_up="Y",
    )
    try:
        bpy.ops.export_scene.gltf(
            filepath=os.path.join(EXPORT_DIR, f"GLB_{ITEM_ID}_{SLUG}_{FILE_SUFFIX}.glb"),
            use_selection=True,
            export_format="GLB",
        )
    except Exception as exc:  # noqa: BLE001
        print("GLB export skipped: " + str(exc))


def write_workflow_note() -> None:
    note = """# 01 조종실 구조 유사도 모델링 v006

이 산출물은 기존 true-model 루프를 폐기하고 새 흐름으로 다시 시작한 첫 작업입니다.

단계별 처리:

1. Reference Breakdown: 기준 보드의 01 조종실을 큰 main 뷰와 터미널/조타 링/스크린 closeup 슬롯으로 분리했습니다.
2. Blockout: `hs01_blockout_*` 오브젝트로 방, 콘솔, 모니터, 조타 링의 큰 덩어리를 먼저 만들었습니다.
3. Hard Surface Modeling: `hs01_main_*`, `hs01_iso_*` 오브젝트로 베벨 금속 패널, 프레임, 가스켓, 볼트, 케이블 클램프, 분절 조타 링을 실제 메시/커브로 구성했습니다.
4. Detail Modeling: 볼트, 링 세그먼트, 환기 슬롯, 상태 핀, 케이블, 손잡이, 패널 칩 지오메트리를 추가했습니다.
5. UV / Texture Layout: 원본 PNG projection 없이 직접 생성한 페인팅 텍스처를 금속/고무/CRT 재질에 연결했습니다.
6. Texture Painting: `ST3_HS01_HandPainted_*` 텍스처에 칩, 때, 모서리 마모, 얼룩 분포를 직접 페인트한 형태로 생성했습니다.
7. Material Authoring: albedo, roughness, metallic, bump, CRT emission을 분리했습니다.
8. Render Matching: 기준 보드와 같은 슬롯 구성을 만들 수 있도록 `main`, `left_close`, `center_close`, `screen_close` 렌더를 저장했습니다.

v006에서는 픽셀 단위 복제를 중단하고 전체 구조 유사도를 우선합니다. 메인 뷰의 평평한 후방 벽을 창 너머 화물칸 깊이 구조로 교체하고, 중앙 모니터를 줄여 창/후방 공간/조타 링의 관계가 읽히도록 조정했습니다.

승인 기준은 사용자가 조정한 95%입니다. 95% 미만이면 승인 후보가 아니며 Unity 적용도 금지됩니다.
"""
    note += """

# v006 structural update

v006 changes the target from pixel/detail reproduction to 95% structural similarity.
The main-view flat rear wall was replaced with a visible window and cargo-bay depth model.
The center monitor was reduced and pushed back so the window, rear space, and helm relationship can read closer to the art sample.
This output still requires explicit user approval before Unity application.
"""
    with open(os.path.join(ANALYSIS_DIR, "01_workflow_structure_v006.md"), "w", encoding="utf-8") as handle:
        handle.write(note)


def write_status_stub() -> None:
    status = {
        "variant": VARIANT,
        "item_id": ITEM_ID,
        "slug": SLUG,
        "approval_basis": "95% structural similarity, not pixel-level detail reproduction",
        "structural_criteria": "STRUCTURAL_APPROVAL_CRITERIA.md",
        "workflow": [
            "reference_breakdown",
            "blockout",
            "hard_surface_modeling",
            "detail_modeling",
            "uv_texture_layout",
            "texture_painting",
            "material_authoring",
            "structural_render_matching",
            "user_structural_approval_gate",
        ],
        "review_ready": True,
        "structural_review_document": "analysis/01_structural_review_v006.md",
        "approval_ready": False,
        "user_approval_required_before_unity": True,
        "unity_application_allowed": False,
        "rule": "The Blender artSample must satisfy the 95% structural-similarity direction and receive explicit user approval before Unity application.",
    }
    with open(os.path.join(SAMPLE_ROOT, "workflow_status.json"), "w", encoding="utf-8") as handle:
        json.dump(status, handle, ensure_ascii=False, indent=2)


def main() -> None:
    configure_base()
    ensure_dirs()
    base.clear_scene()
    base.configure_scene()
    set_render_quality()
    mats = make_materials()

    blockout_col = base.new_collection("Stage3_HS01_Blockout")
    final_col = base.new_collection("Stage3_HS01_Final_HardSurface")
    build_blockout(blockout_col, mats)
    build_final_cockpit(final_col, mats)
    build_iso_models(final_col, mats)

    collections = [blockout_col, final_col]
    blockout_col.hide_render = True
    blockout_col.hide_viewport = True
    for obj in blockout_col.objects:
        obj.hide_render = True
        obj.hide_viewport = True

    for slot_name, camera_settings in SLOT_CAMERA_PRESETS.items():
        render_slot(slot_name, final_col, collections, camera_settings)

    export_final_collection(final_col)
    base.set_collection_visibility(collections, final_col)
    configure_slot_visibility(final_col, "main")
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLENDER_DIR, "Stage3_HardSurfaceReproduction_01_structure_v006.blend"))
    write_workflow_note()
    write_status_stub()
    print("Stage 3 hard-surface reproduction sample generated at " + SAMPLE_ROOT)


if __name__ == "__main__":
    main()
