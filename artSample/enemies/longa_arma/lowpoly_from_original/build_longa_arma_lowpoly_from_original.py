from __future__ import annotations

import json
import math
import random
from pathlib import Path

import bpy
from mathutils import Vector


REPO_ROOT = Path(__file__).resolve().parents[4]
SAMPLE_ROOT = Path(__file__).resolve().parent
ORIGINAL_BLEND = REPO_ROOT / "enemies model" / "longa arma.blend"
BLEND_PATH = SAMPLE_ROOT / "blender" / "longa_arma_lowpoly_from_original.blend"
FBX_PATH = SAMPLE_ROOT / "exports" / "longa_arma_lowpoly_from_original.fbx"
GLB_PATH = SAMPLE_ROOT / "exports" / "longa_arma_lowpoly_from_original.glb"
RENDER_DIR = SAMPLE_ROOT / "renders"
TEXTURE_DIR = SAMPLE_ROOT / "textures"
MANIFEST_PATH = SAMPLE_ROOT / "ASSET_MANIFEST.json"
README_PATH = SAMPLE_ROOT / "README.md"
STATUS_PATH = SAMPLE_ROOT / "LOWPOLY_FROM_ORIGINAL_STATUS_2026-07-03.md"
HTML_PATH = SAMPLE_ROOT / "index.html"

TARGET_FACE_COUNT = 12000


def ensure_dirs() -> None:
    for path in [BLEND_PATH.parent, FBX_PATH.parent, GLB_PATH.parent, RENDER_DIR, TEXTURE_DIR]:
        path.mkdir(parents=True, exist_ok=True)


def clean_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_collection(name: str, *, hide_viewport: bool = False, hide_render: bool = False) -> bpy.types.Collection:
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    collection.hide_viewport = hide_viewport
    collection.hide_render = hide_render
    return collection


def move_to_collection(obj: bpy.types.Object, collection: bpy.types.Collection) -> None:
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    collection.objects.link(obj)


def select_active(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def load_original(reference_collection: bpy.types.Collection) -> bpy.types.Object:
    if not ORIGINAL_BLEND.exists():
        raise FileNotFoundError(ORIGINAL_BLEND)

    with bpy.data.libraries.load(str(ORIGINAL_BLEND), link=False) as (data_from, data_to):
        data_to.objects = [name for name in data_from.objects if name == "mesh_node"]

    for obj in data_to.objects:
        if obj is None:
            continue
        reference_collection.objects.link(obj)
        obj.name = "REF_original_mesh_node"
        obj.hide_viewport = True
        obj.hide_render = True
        obj["reference_only"] = True
        return obj

    raise RuntimeError("mesh_node was not found in original Longa Arma blend.")


def create_texture(name: str, width: int, height: int, kind: str) -> Path:
    random.seed(name)
    image = bpy.data.images.new(name, width=width, height=height)
    pixels: list[float] = []
    for y in range(height):
        for x in range(width):
            u = x / max(1, width - 1)
            v = y / max(1, height - 1)
            noise = (
                0.35 * math.sin((u * 19.0 + v * 6.0) * math.pi)
                + 0.30 * math.sin((u * 39.0 - v * 17.0) * math.pi)
                + 0.20 * math.sin((u * 7.0 + v * 31.0) * math.pi)
                + 0.15 * random.random()
            )
            if kind == "body":
                vein = 1.0 if math.sin((u * 23.0 + v * 29.0) * math.pi) > 0.86 else 0.0
                wet = 1.0 if abs(math.sin((u * 4.0 - v * 3.0) * math.pi)) > 0.93 else 0.0
                r = 0.12 + 0.10 * noise + 0.04 * wet
                g = 0.34 + 0.24 * noise + 0.12 * vein + 0.08 * wet
                b = 0.19 + 0.12 * noise + 0.04 * vein
                a = 1.0
            elif kind == "blade":
                scratch = 1.0 if abs(math.sin((u * 96.0 + v * 13.0) * math.pi)) > 0.984 else 0.0
                edge = 0.35 if v < 0.12 or v > 0.88 else 0.0
                base = 0.09 + 0.10 * noise + 0.30 * scratch + edge
                r, g, b, a = base * 0.88, base * 0.94, base, 1.0
            else:
                r = 0.12 + 0.08 * noise
                g = 0.45 + 0.22 * noise
                b = 0.22 + 0.10 * noise
                a = 0.82
            pixels.extend([max(0.0, min(1.0, r)), max(0.0, min(1.0, g)), max(0.0, min(1.0, b)), a])

    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(TEXTURE_DIR / f"{name}.png")
    image.file_format = "PNG"
    image.save()
    return TEXTURE_DIR / f"{name}.png"


def material_from_texture(
    name: str,
    texture_path: Path,
    *,
    roughness: float,
    metallic: float = 0.0,
    alpha: float = 1.0,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (1.0, 1.0, 1.0, alpha)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(str(texture_path), check_existing=True)
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    if alpha < 1.0:
        mat.blend_method = "BLEND"
        bsdf.inputs["Alpha"].default_value = alpha
        mat.node_tree.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])

    noise = mat.node_tree.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 42.0
    noise.inputs["Detail"].default_value = 10.0
    bump = mat.node_tree.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.08
    bump.inputs["Distance"].default_value = 0.035
    mat.node_tree.links.new(noise.outputs["Fac"], bump.inputs["Height"])
    mat.node_tree.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def create_materials() -> tuple[bpy.types.Material, bpy.types.Material, bpy.types.Material]:
    body_tex = create_texture("longa_lowpoly_body_mottled_green", 1024, 1024, "body")
    blade_tex = create_texture("longa_lowpoly_dark_scratched_blade", 1024, 1024, "blade")
    slime_tex = create_texture("longa_lowpoly_glossy_slime_drips", 512, 512, "slime")
    return (
        material_from_texture("M_LongaLowPoly_WetMottledBody", body_tex, roughness=0.72, metallic=0.0),
        material_from_texture("M_LongaLowPoly_DarkCrescentBlade", blade_tex, roughness=0.46, metallic=0.18),
        material_from_texture("M_LongaLowPoly_GlossySlimeDrips", slime_tex, roughness=0.32, metallic=0.0, alpha=0.86),
    )


def duplicate_and_decimate(original: bpy.types.Object, model_collection: bpy.types.Collection) -> tuple[bpy.types.Object, dict]:
    low = original.copy()
    low.data = original.data.copy()
    low.name = "LongaArma_LowPoly_FromOriginal"
    low.data.name = "LongaArma_LowPoly_FromOriginal_mesh"
    low.hide_viewport = False
    low.hide_render = False
    model_collection.objects.link(low)
    low.matrix_world = original.matrix_world.copy()

    original_faces = len(original.data.polygons)
    ratio = min(1.0, TARGET_FACE_COUNT / max(1, original_faces))
    select_active(low)
    decimate = low.modifiers.new("LOWPOLY_preserve_original_silhouette", "DECIMATE")
    decimate.ratio = ratio
    if hasattr(decimate, "use_collapse_triangulate"):
        decimate.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=decimate.name)

    bpy.ops.object.shade_flat()
    for poly in low.data.polygons:
        poly.use_smooth = False

    low["source"] = str(ORIGINAL_BLEND.relative_to(REPO_ROOT)).replace("\\", "/")
    low["lowpoly_method"] = f"Blender Decimate ratio {ratio:.5f}, target {TARGET_FACE_COUNT} faces"
    low["not_animation_ready"] = "Static low-poly sample only. No rigging or animation in this pass."
    return low, {"originalFaces": original_faces, "ratio": ratio}


def unwrap_uv(obj: bpy.types.Object) -> None:
    select_active(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.025)
    bpy.ops.object.mode_set(mode="OBJECT")


def assign_materials(obj: bpy.types.Object, body: bpy.types.Material, blade: bpy.types.Material, slime: bpy.types.Material) -> dict:
    obj.data.materials.clear()
    obj.data.materials.append(body)
    obj.data.materials.append(blade)
    obj.data.materials.append(slime)

    verts = obj.data.vertices
    xs: list[float] = []
    ys: list[float] = []
    zs: list[float] = []
    centers: list[Vector] = []
    for poly in obj.data.polygons:
        center = sum((verts[i].co for i in poly.vertices), Vector()) / len(poly.vertices)
        centers.append(center)
        xs.append(center.x)
        ys.append(center.y)
        zs.append(center.z)

    x_min, x_max = min(xs), max(xs)
    y_min, y_max = min(ys), max(ys)
    z_min, z_max = min(zs), max(zs)
    x_range = max(0.0001, x_max - x_min)
    y_range = max(0.0001, y_max - y_min)
    z_range = max(0.0001, z_max - z_min)

    blade_count = 0
    slime_count = 0
    for poly, center in zip(obj.data.polygons, centers):
        is_blade = (
            center.x < x_min + x_range * 0.36
            and center.y < y_min + y_range * 0.50
            and center.z < z_min + z_range * 0.58
        )
        is_slime = (
            not is_blade
            and center.z < z_min + z_range * 0.22
            and abs(center.x) < x_range * 0.38
            and abs(center.y) < y_range * 0.44
        )
        if is_blade:
            poly.material_index = 1
            blade_count += 1
        elif is_slime:
            poly.material_index = 2
            slime_count += 1
        else:
            poly.material_index = 0

    obj["material_assignment"] = json.dumps(
        {
            "bodyFaceCount": len(obj.data.polygons) - blade_count - slime_count,
            "bladeFaceCount": blade_count,
            "slimeFaceCount": slime_count,
            "bladeHeuristic": "negative X/Y low quadrant on the original-decimated mesh; used only for material split, not geometry changes",
        },
        ensure_ascii=False,
    )
    return {"bodyFaces": len(obj.data.polygons) - blade_count - slime_count, "bladeFaces": blade_count, "slimeFaces": slime_count}


def setup_camera_and_lights() -> bpy.types.Object:
    bpy.ops.object.light_add(type="AREA", location=(0.0, -3.8, 4.0))
    key = bpy.context.object
    key.name = "AREA_key_softbox"
    key.data.energy = 650.0
    key.data.size = 5.5
    bpy.ops.object.light_add(type="POINT", location=(-2.2, 1.7, 2.4))
    rim = bpy.context.object
    rim.name = "POINT_wet_highlight"
    rim.data.energy = 120.0

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    bpy.context.scene.camera = camera
    camera.data.lens = 45.0
    bpy.context.scene.render.resolution_x = 1400
    bpy.context.scene.render.resolution_y = 900
    bpy.context.scene.eevee.taa_render_samples = 32
    return camera


def bounds_center(obj: bpy.types.Object) -> Vector:
    return sum((obj.matrix_world @ Vector(corner) for corner in obj.bound_box), Vector()) / 8.0


def aim_camera(camera: bpy.types.Object, target: Vector) -> None:
    direction = target - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_view(obj: bpy.types.Object, camera: bpy.types.Object, name: str, location: tuple[float, float, float], target_offset: tuple[float, float, float] = (0, 0, 0)) -> str:
    center = bounds_center(obj) + Vector(target_offset)
    max_dim = max(obj.dimensions)
    camera.location = Vector(location) * max_dim
    aim_camera(camera, center)
    bpy.context.scene.render.filepath = str(RENDER_DIR / name)
    bpy.ops.render.render(write_still=True)
    return f"renders/{name}"


def export_lowpoly(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
    )
    bpy.ops.export_scene.gltf(filepath=str(GLB_PATH), export_format="GLB", use_selection=True)


def write_docs(report: dict) -> None:
    manifest = {
        "assetId": "longa_arma_lowpoly_from_original",
        "createdAt": "2026-07-03",
        "sourceBlend": str(ORIGINAL_BLEND.relative_to(REPO_ROOT)).replace("\\", "/"),
        "purpose": "Static low-poly sample generated directly from the original Longa Arma mesh.",
        "outputs": {
            "blend": str(BLEND_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "fbx": str(FBX_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "glb": str(GLB_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
            "renders": report["renders"],
            "textures": [str(Path("textures") / path.name).replace("\\", "/") for path in sorted(TEXTURE_DIR.glob("*.png"))],
        },
        "geometry": report["geometry"],
        "materials": report["materials"],
        "notRigged": True,
        "notAnimated": True,
        "notUnityApplied": True,
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    README_PATH.write_text(
        f"""# Longa Arma Low-Poly From Original

- 기준 원본: `enemies model/longa arma.blend`
- 목적: 원본 `mesh_node`의 실루엣을 유지하면서 로우 폴리 정적 샘플을 만드는 것입니다.
- 이번 샘플은 리깅/애니메이션/Unity 적용이 아닙니다.

## 제작 방식

- 원본 단일 고밀도 메시를 복제했습니다.
- 복제본에 Blender Decimate를 적용해 face 수를 {report["geometry"]["originalFaces"]}에서 {report["geometry"]["lowpolyFaces"]}로 줄였습니다.
- 새 UV를 생성하고, 젖은 녹색 몸체/어두운 칼날/점액 재질을 새로 적용했습니다.
- 형상 자체는 원본 감량본을 사용했고, 임의로 다리나 칼날 팔을 새로 붙이지 않았습니다.

## 검토 기준

- 네 다리와 말형 몸체가 원본처럼 보이는지 확인해야 합니다.
- 왼쪽의 긴 칼날 팔이 추가 다리처럼 보이지 않고 한쪽 앞팔의 연장으로 읽히는지 확인해야 합니다.
- 세부 조형은 원본보다 줄었지만, 큰 실루엣은 원본과 일치해야 합니다.

## 산출물

- `blender/longa_arma_lowpoly_from_original.blend`
- `exports/longa_arma_lowpoly_from_original.fbx`
- `exports/longa_arma_lowpoly_from_original.glb`
- `renders/*.png`
- `textures/*.png`
""",
        encoding="utf-8",
    )

    STATUS_PATH.write_text(
        f"""# Longa Arma Low-Poly From Original Status - 2026-07-03

## Result

- Source: `enemies model/longa arma.blend`
- Source object: `mesh_node`
- Original vertices: {report["geometry"]["originalVertices"]}
- Original faces: {report["geometry"]["originalFaces"]}
- Low-poly vertices: {report["geometry"]["lowpolyVertices"]}
- Low-poly faces: {report["geometry"]["lowpolyFaces"]}
- Decimate ratio: {report["geometry"]["decimateRatio"]:.5f}
- FBX: `exports/longa_arma_lowpoly_from_original.fbx`
- GLB: `exports/longa_arma_lowpoly_from_original.glb`

## Possible

- 원본 메시에서 직접 로우 폴리 감량본을 생성했습니다.
- 기준 이미지의 핵심인 4족 몸체, 말형 머리, 긴 왼쪽 칼날 팔 실루엣을 원본 메시 기반으로 유지했습니다.
- 원본에 없던 UV/머티리얼/텍스처를 새로 추가했습니다.

## Not Done

- 리깅 없음.
- 애니메이션 없음.
- Unity 적용 없음.
- 원본 `enemies model/longa arma.blend`는 수정하지 않았습니다.
""",
        encoding="utf-8",
    )

    render_figures = "\n".join(
        f'<figure><img src="{render}" /><figcaption>{Path(render).name}</figcaption></figure>'
        for render in report["renders"]
    )
    HTML_PATH.write_text(
        f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Longa Arma Low-Poly From Original</title>
  <style>
    body {{ margin: 0; font-family: system-ui, sans-serif; background: #101312; color: #edf3ee; }}
    main {{ max-width: 1180px; margin: 0 auto; padding: 28px; }}
    h1 {{ margin: 0 0 10px; font-size: 28px; }}
    h2 {{ margin-top: 28px; font-size: 20px; }}
    p {{ color: #c2cec6; line-height: 1.55; }}
    .grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 16px; }}
    figure {{ margin: 0; background: #1a211f; border: 1px solid #2e3a35; border-radius: 6px; overflow: hidden; }}
    img {{ width: 100%; display: block; }}
    figcaption {{ padding: 10px 12px; color: #d6ded8; font-size: 14px; }}
    code {{ color: #d9ffc8; }}
  </style>
</head>
<body>
  <main>
    <h1>Longa Arma Low-Poly From Original</h1>
    <p>원본 <code>enemies model/longa arma.blend</code>의 <code>mesh_node</code>를 직접 감량한 정적 로우 폴리 샘플입니다. 리깅과 애니메이션은 포함하지 않았습니다.</p>
    <h2>생성 렌더</h2>
    <div class="grid">
      {render_figures}
    </div>
    <h2>기준 이미지</h2>
    <div class="grid">
      <figure><img src="../../../../image/longa arma(롱가 아르마).png" /><figcaption>front reference</figcaption></figure>
      <figure><img src="../../../../image/longa arma-beside.png" /><figcaption>side reference</figcaption></figure>
      <figure><img src="../../../../image/longa arma-back.png" /><figcaption>back reference</figcaption></figure>
    </div>
  </main>
</body>
</html>
""",
        encoding="utf-8",
    )


def build_report(obj: bpy.types.Object, original: bpy.types.Object, decimate_info: dict, material_counts: dict, renders: list[str]) -> dict:
    return {
        "geometry": {
            "originalVertices": len(original.data.vertices),
            "originalFaces": decimate_info["originalFaces"],
            "lowpolyVertices": len(obj.data.vertices),
            "lowpolyFaces": len(obj.data.polygons),
            "decimateRatio": decimate_info["ratio"],
            "dimensions": [round(value, 4) for value in obj.dimensions],
        },
        "materials": material_counts,
        "renders": renders,
    }


def main() -> None:
    ensure_dirs()
    clean_scene()

    reference_collection = make_collection("Reference_Original_Do_Not_Export", hide_viewport=True, hide_render=True)
    model_collection = make_collection("LowPoly_FromOriginal")
    original = load_original(reference_collection)
    lowpoly, decimate_info = duplicate_and_decimate(original, model_collection)
    body, blade, slime = create_materials()
    unwrap_uv(lowpoly)
    material_counts = assign_materials(lowpoly, body, blade, slime)

    camera = setup_camera_and_lights()
    renders = [
        render_view(lowpoly, camera, "01_side_pos_x.png", (2.40, 0.00, 0.45)),
        render_view(lowpoly, camera, "02_blade_side_neg_x.png", (-2.40, 0.00, 0.45)),
        render_view(lowpoly, camera, "03_front_neg_y.png", (0.00, -2.35, 0.38)),
        render_view(lowpoly, camera, "04_rear_pos_y.png", (0.00, 2.35, 0.45)),
    ]

    export_lowpoly(lowpoly)
    report = build_report(lowpoly, original, decimate_info, material_counts, renders)
    write_docs(report)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print("LONGA_ARMA_LOW_POLY_FROM_ORIGINAL_CREATED")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
