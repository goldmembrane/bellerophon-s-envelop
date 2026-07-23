import bpy
import hashlib
import json
import math
import shutil
import struct
from collections import Counter
from pathlib import Path
from mathutils import Vector


PROJECT = Path(r"D:\Bellerophon2\Bellerophon")
SOURCE = PROJECT / "enemies model" / "négatif.glb"
APPROVED = PROJECT / "artSample" / "enemies" / "negatif" / "appearance_reference_sync"
ROOT = PROJECT / "artSample" / "enemies" / "negatif" / "glb_appearance_sync"
TEXTURES = ROOT / "textures"
RENDERS = ROOT / "renders"
BLENDER = ROOT / "blender"
EXPORTS = ROOT / "exports"
SOURCE_DIR = ROOT / "source"

SPECS = [
    ("Negatif_Worn_Bronze", "negatif_worn_bronze", 0.84, 0.38),
    ("Negatif_Dark_Mechanism", "negatif_dark_mechanism", 0.98, 0.20),
    ("Negatif_Canvas_Sack", "negatif_canvas", 0.02, 0.42),
    ("Negatif_Leather_Strap", "negatif_leather", 0.01, 0.72),
    ("Negatif_Copper_Accent", "negatif_copper_accent", 0.76, 0.13),
    ("Negatif_Amber_Eye", "negatif_amber_eye", 0.12, 0.05),
]


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def signature(mesh):
    digest = hashlib.sha256()
    for vertex in mesh.vertices:
        digest.update(struct.pack("<3d", *vertex.co))
    for polygon in mesh.polygons:
        digest.update(struct.pack("<I", len(polygon.vertices)))
        for index in polygon.vertices:
            digest.update(struct.pack("<I", index))
    return digest.hexdigest().upper()


def snapshot(meshes, armature):
    return {
        obj.name: {
            "vertices": len(obj.data.vertices),
            "polygons": len(obj.data.polygons),
            "signature": signature(obj.data),
        }
        for obj in meshes
    } | {"bones": len(armature.data.bones)}


def make_material(name, prefix, metallic, bump_strength):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    output.location = (620, 0)
    shader.location = (330, 0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = 0.45
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])

    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = bpy.data.images.load(str(TEXTURES / f"{prefix}_albedo.png"), check_existing=True)
    albedo.location = (-520, 170)
    roughness = nodes.new("ShaderNodeTexImage")
    roughness.image = bpy.data.images.load(str(TEXTURES / f"{prefix}_roughness.png"), check_existing=True)
    roughness.image.colorspace_settings.name = "Non-Color"
    roughness.location = (-520, -40)
    height = nodes.new("ShaderNodeTexImage")
    height.image = bpy.data.images.load(str(TEXTURES / f"{prefix}_bump.png"), check_existing=True)
    height.image.colorspace_settings.name = "Non-Color"
    height.location = (-520, -250)
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.045
    bump.location = (80, -210)
    if name == "Negatif_Leather_Strap":
        strap_tint = nodes.new("ShaderNodeMixRGB")
        strap_tint.blend_type = "MULTIPLY"
        strap_tint.inputs["Fac"].default_value = 0.42
        strap_tint.inputs[2].default_value = (0.34, 0.085, 0.028, 1.0)
        strap_tint.location = (-190, 170)
        links.new(albedo.outputs["Color"], strap_tint.inputs[1])
        links.new(strap_tint.outputs["Color"], shader.inputs["Base Color"])
    elif name == "Negatif_Amber_Eye":
        shader.inputs["Base Color"].default_value = (1.0, 0.10, 0.002, 1.0)
    else:
        links.new(albedo.outputs["Color"], shader.inputs["Base Color"])
    links.new(roughness.outputs["Color"], shader.inputs["Roughness"])
    links.new(height.outputs["Color"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    if name == "Negatif_Amber_Eye":
        emission = shader.inputs.get("Emission Color") or shader.inputs.get("Emission")
        strength = shader.inputs.get("Emission Strength")
        if emission:
            emission.default_value = (1.0, 0.18, 0.004, 1.0)
        if strength:
            strength.default_value = 9.0
    return material


def smart_uv(obj):
    if obj.data.uv_layers:
        return
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.uv_layers.active.name = "Negatif_Glb_MaterialUV"


def classify_main(obj):
    counts = Counter()
    for polygon in obj.data.polygons:
        x, y, z = polygon.center
        ax = abs(x)
        # The GLB is authored with the snout toward -Y, cargo toward +Y and Z up.
        cargo = y > -0.05 and z > 0.58
        # Narrow bands follow the three raised cargo ridges in the source GLB.
        # Keeping the mask on those ridges makes the leather read as continuous
        # retaining straps instead of a broad patch painted across the sack.
        strap = cargo and (
            abs(y - 0.04) < 0.07
            or abs(y - 0.50) < 0.075
            or abs(y - 0.76) < 0.06
        )
        copper = y < -1.76 and z < 1.05 and ax < 0.42
        mechanism = (
            z < 0.56
            or y > 1.92
            or (-0.38 < y < 0.18 and z < 0.83)
        )
        if copper:
            slot = 4
        elif strap:
            slot = 3
        elif cargo:
            slot = 2
        elif mechanism:
            slot = 1
        else:
            slot = 0
        polygon.material_index = slot
        counts[SPECS[slot][0]] += 1
    return counts


def create_reference_eye(materials, armature, name, side, anchor, normal, parent_bone):
    # Each side uses its measured source-surface anchor, outward normal, and
    # dominant deformation bone so the two glowing eyes remain symmetric.
    anchor = Vector(anchor)
    normal = Vector(normal).normalized()
    parts = []

    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.100, depth=0.026)
    socket = bpy.context.object
    socket.name = name
    socket.data.materials.append(materials[1])
    parts.append(socket)

    bpy.ops.mesh.primitive_torus_add(
        major_segments=20,
        minor_segments=6,
        location=(0, 0, 0.020),
        major_radius=0.070,
        minor_radius=0.017,
    )
    ring = bpy.context.object
    ring.data.materials.append(materials[4])
    parts.append(ring)

    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=20,
        ring_count=10,
        location=(0, 0, 0.041),
        scale=(0.061, 0.061, 0.032),
    )
    lens = bpy.context.object
    lens.data.materials.append(materials[5])
    parts.append(lens)

    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = socket
    bpy.ops.object.join()
    eye = bpy.context.object
    eye.name = name
    eye.data.name = f"{name}Mesh"
    for polygon in eye.data.polygons:
        polygon.use_smooth = True

    eye.rotation_mode = "QUATERNION"
    eye.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(normal)
    eye.location = anchor + normal * 0.015
    bpy.context.view_layer.update()
    world_matrix = eye.matrix_world.copy()
    eye.parent = armature
    eye.parent_type = "BONE"
    eye.parent_bone = parent_bone
    eye.matrix_world = world_matrix
    eye["reference_part"] = "paired_glowing_eye"
    eye["reference_source"] = "image/négatif(네거티프).png"
    eye["side"] = side
    eye["surface_anchor"] = [round(value, 6) for value in anchor]
    eye["surface_normal"] = [round(value, 6) for value in normal]
    return eye


def create_reference_eyes(materials, armature):
    eye_specs = (
        (
            "Negatif_ReferenceEye_PositiveX",
            "+X",
            (0.265533, -1.553142, 1.198604),
            (0.883422, -0.058121, 0.464960),
            "Bone_025",
        ),
        (
            "Negatif_ReferenceEye_NegativeX",
            "-X",
            (-0.265533, -1.553142, 1.198604),
            (-0.883422, -0.058121, 0.464960),
            "Bone_025",
        ),
    )
    return [
        create_reference_eye(materials, armature, name, side, anchor, normal, parent_bone)
        for name, side, anchor, normal, parent_bone in eye_specs
    ]


def look_at(camera, target):
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()


def render(camera, location, filename):
    camera.location = location
    look_at(camera, (0, 0, 0.85))
    bpy.context.scene.render.filepath = str(RENDERS / filename)
    bpy.ops.render.render(write_still=True)


def main():
    for directory in (TEXTURES, RENDERS, BLENDER, EXPORTS, SOURCE_DIR):
        directory.mkdir(parents=True, exist_ok=True)
    source_hash = sha256(SOURCE)
    shutil.copy2(SOURCE, SOURCE_DIR / "Negatif_Source_Unmodified.glb")
    for texture in (APPROVED / "textures").glob("*.png"):
        shutil.copy2(texture, TEXTURES / texture.name)

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE))
    meshes = sorted(
        [obj for obj in bpy.context.scene.objects if obj.type == "MESH"],
        key=lambda obj: len(obj.data.vertices),
        reverse=True,
    )
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(meshes) != 2 or len(armatures) != 1:
        raise RuntimeError("Expected two meshes and one armature in Negatif GLB.")
    main_mesh, auxiliary_mesh = meshes
    armature = armatures[0]
    before = snapshot(meshes, armature)
    if (
        len(main_mesh.data.vertices) != 17771
        or len(main_mesh.data.polygons) != 6397
        or len(auxiliary_mesh.data.vertices) != 42
        or len(auxiliary_mesh.data.polygons) != 80
        or len(armature.data.bones) != 46
    ):
        raise RuntimeError("Negatif GLB source structure changed.")

    materials = [make_material(*spec) for spec in SPECS]
    for obj in meshes:
        smart_uv(obj)
        obj.data.materials.clear()
        for material in materials:
            obj.data.materials.append(material)
    counts = classify_main(main_mesh)
    for polygon in auxiliary_mesh.data.polygons:
        polygon.material_index = 1
        counts["Negatif_Dark_Mechanism"] += 1

    after = snapshot(meshes, armature)
    if before != after:
        raise RuntimeError("Appearance sample changed GLB mesh topology, coordinates, or rig.")
    eyes = create_reference_eyes(materials, armature)
    for eye in eyes:
        for polygon in eye.data.polygons:
            material = eye.data.materials[polygon.material_index]
            counts[material.name] += 1
    if any(counts[name] == 0 for name, *_ in SPECS):
        raise RuntimeError("A required approved material region is empty.")

    for obj in bpy.context.scene.objects:
        if obj.type == "ARMATURE":
            obj.hide_render = True

    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.055, 0.05, 0.045, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.34
    for name, location, energy, color, size in (
        ("Key", (4.5, -5.0, 6.0), 1250, (1.0, 0.83, 0.68), 4.0),
        ("Fill", (-4.0, -2.0, 3.5), 850, (0.62, 0.76, 1.0), 3.5),
        ("Rim", (0.0, 5.0, 5.0), 1100, (1.0, 0.42, 0.20), 3.0),
    ):
        bpy.ops.object.light_add(type="AREA", location=location)
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.color = color
        light.data.shape = "DISK"
        light.data.size = size
    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.data.lens = 58
    bpy.context.scene.camera = camera
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    render(camera, (4.8, -6.8, 3.2), "01_three_quarter.png")
    render(camera, (5.8, 0.0, 2.5), "02_side.png")
    render(camera, (0.0, -7.4, 2.4), "03_front.png")
    render(camera, (-4.8, -6.8, 3.2), "04_opposite_three_quarter.png")
    stale_back_render = RENDERS / "04_back_three_quarter.png"
    if stale_back_render.exists():
        stale_back_render.unlink()

    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER / "Negatif_Glb_AppearanceSync.blend"))
    backup = BLENDER / "Negatif_Glb_AppearanceSync.blend1"
    if backup.exists():
        backup.unlink()
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes + [armature] + eyes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = main_mesh
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORTS / "Negatif_Glb_AppearanceSync.glb"),
        export_format="GLB",
        use_selection=True,
        export_animations=False,
        export_cameras=False,
        export_lights=False,
    )
    if source_hash != sha256(SOURCE) or source_hash != sha256(SOURCE_DIR / "Negatif_Source_Unmodified.glb"):
        raise RuntimeError("Original GLB or sample copy hash changed.")

    manifest = {
        "status": "PENDING_USER_APPROVAL",
        "source": "enemies model/négatif.glb",
        "source_sha256": source_hash,
        "geometry_before": before,
        "geometry_after": after,
        "modeling_changed": True,
        "source_geometry_changed": False,
        "added_eyes": [
            {
                "object_name": eye.name,
                "side": eye["side"],
                "vertices": len(eye.data.vertices),
                "polygons": len(eye.data.polygons),
                "parent_bone": eye.parent_bone,
                "emission_strength": 9.0,
                "surface_anchor": list(eye["surface_anchor"]),
                "surface_normal": list(eye["surface_normal"]),
            }
            for eye in eyes
        ],
        "added_eye_object_count": len(eyes),
        "uv_added": True,
        "material_face_counts": dict(counts),
        "approved_texture_source": "artSample/enemies/negatif/appearance_reference_sync/textures",
    }
    (ROOT / "GEOMETRY_VALIDATION.json").write_text(
        json.dumps({"result": "PASS", **manifest}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    (ROOT / "APPROVAL_STATUS.json").write_text(
        json.dumps(
            {
                "status": "PENDING_USER_APPROVAL",
                "approved_for_unity": False,
                "modeling_modified": True,
                "source_glb_modified": False,
                "added_eye_object_count": len(eyes),
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )
    (ROOT / "README.md").write_text(
        "# 니게티프 GLB 외형 동기화 샘플\n\n"
        "새 `négatif.glb`의 원본 형상과 46본 리그는 유지하고, 기존 승인 샘플의 "
        "금속·기계부·캔버스·가죽끈·구리·주황 눈 재질과 18개 텍스처를 대응시켰습니다.\n\n"
        "가방끈은 원본 GLB의 실제 융기 세 곳을 따라 좁고 연속된 띠로 분류하고, "
        "가죽 재질에만 적갈색 명암과 요철 대비를 강화했습니다.\n\n"
        "코 뒤 앞쪽 얼굴 판 양쪽에 어두운 소켓, 마모 금속 링, 강하게 빛나는 "
        "호박색 렌즈로 구성된 원형 눈 메시를 한 개씩 총 두 개 추가했습니다. 기존의 "
        "사각 발광 면은 원래 금속 재질로 복구했습니다.\n\n"
        "- Unity 미반영\n- 사용자 승인 대기\n- 원본 GLB 메시·리그 변형 없음\n"
        "- 샘플 파생 메시: 좌우 발광 눈 개체 총 2개 추가\n"
        "- 기준 이미지 눈 비교: `renders/06_reference_eye_comparison.png`\n"
        "- 기존 승인 외형 비교: `renders/05_approved_sample_comparison.png`\n",
        encoding="utf-8",
    )
    (ROOT / "index.html").write_text(
        '<!doctype html><meta charset="utf-8"><title>니게티프 GLB 외형 샘플</title>'
        '<style>body{background:#171411;color:#eee;font-family:Malgun Gothic;margin:30px}'
        'img{width:48%;margin:1%;background:#333}h1{color:#f0bd7c}</style>'
        '<h1>니게티프 GLB 외형 동기화 · 승인 대기</h1>'
        '<p>기존 승인 재질을 새 GLB 형상에 적용했습니다. 가방끈은 원본의 세 융기를 따라 '
        '연속된 적갈색 가죽 띠로 구분하고, 원형 호박색 발광 눈을 코 뒤 앞쪽 얼굴 판 양쪽에 한 개씩 '
        '추가했습니다. Unity에는 반영하지 않았습니다.</p>'
        '<img src="renders/06_reference_eye_comparison.png">'
        '<img src="renders/05_approved_sample_comparison.png">'
        '<img src="renders/01_three_quarter.png"><img src="renders/02_side.png">'
        '<img src="renders/03_front.png"><img src="renders/04_opposite_three_quarter.png">',
        encoding="utf-8",
    )
    print("NEGATIF_GLB_SAMPLE_COMPLETE", json.dumps(manifest, ensure_ascii=False))


if __name__ == "__main__":
    main()
